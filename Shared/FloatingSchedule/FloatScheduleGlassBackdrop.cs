// 液态玻璃"真折射"背景的持有者（共享层，纯 System/Avalonia）。
//
// 【为什么要单独抽出来】早先的写法是：每 500ms 抓完帧就 RenderModel(_lastModel)/PublishModelAv(_lastModelAv)
//   重套材质 —— 等于每 500ms 把整棵可视树（内容 + 所有装饰图层）推倒重建，动画被打断、布局全程重跑，
//   卡片于是只剩采样频率那点帧率（用户报的"时间表看起来只有 2fps"）。
// 正确形态：**采样只换背景，绝不碰内容树**——位图与缓冲按尺寸缓存复用，刷新只做
//   "抓帧 → 折射 → 原位写回同一张 WriteableBitmap → 就地换该 Border 的 Background"，
//   文字/进度条/分隔线等全部保持全帧率渲染。
// 注意：采样式折射的固有属性是"玻璃里的桌面画面按采样率步进"，这部分无法做到 60fps。
//
// 【采样来源】默认采集**完整桌面**（所有下方窗口 + 壁纸）：采样瞬间把悬浮窗临时排除出捕获
//   （WDA_EXCLUDEFROMCAPTURE，抓完立即恢复），因此不会拍到自己；远程会话自动退回"仅下方窗口"
//   （Win11 WGC / Win10 PrintWindow），避免远端看不到悬浮窗。
//
// 【不再有"拖动位移补偿"】此前为了拖动跟手，会抓"卡片外扩一圈"的缓冲、拖动期间只做 GPU 取样矩形平移，
//   仅在累计位移超阈值时后台重烘焙。外扩取材导致**折射边缘不跟随窗口移动**（边缘按旧烘焙基准走），
//   现已整体移除：每次采样都按**窗口当前所在区域**抓帧并整幅折射，折射边缘与窗口严格对齐；
//   拖动期间照常按采样率抓帧，代价是背景内容有采样周期级的延迟（不再有外扩缓冲与后台重烘焙线程）。
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public sealed class FloatScheduleGlassBackdrop
{
    /// <summary>DXGI 连续失败达到该次数后进入冷却（改走 GDI），冷却期结束再重试 DXGI。</summary>
    const int DxgiFailThreshold = 3;
    /// <summary>DXGI 冷却时长（毫秒）：被 OBS 等独占 / 桌面复制暂不可用时定期重试恢复。</summary>
    const long DxgiRetryIntervalMs = 10_000;

    readonly object _lock = new();
    /// <summary>抓帧串行化锁：同一时刻只允许一个抓帧进入后端（DXGI/GDI）。</summary>
    readonly object _captureLock = new();

    // ===== 完整桌面采样的"持续排除自身"状态 =====
    // 逐帧"排除→抓→恢复"会让 DWM 来不及合成，必须每帧 DwmFlush（16~60ms），这是延迟主因。
    // 改为采样期间持续排除：只在开始采样时 flush 一次；停止采样（无调用）400ms 后自动恢复。
    readonly object _affinityLock = new();
    IntPtr _excludedHwnd;
    uint _originalAffinity;
    Timer? _exclusionIdleTimer;
    const int ExclusionIdleMs = 400;

    // ===== 抓帧 / 折射缓冲 =====
    byte[]? _src;                    // 采样抓帧缓冲（尺寸 = 卡片）
    int _srcW, _srcH;                // _src 的尺寸
    bool _srcValid;                  // _src 是否已装填过可用内容
    byte[]? _dst;                    // 前台折射结果（尺寸 = 卡片）—— 供 Commit 上传位图
    int _dstW, _dstH;
    WriteableBitmap? _bmp;           // 卡片背景位图（按尺寸缓存复用，不逐帧新建）
    ImageBrush? _brush;              // 背景画笔（整幅位图直接铺满卡片）
    Border? _layer;                  // 最近一次 Apply 挂进可视树的那个 Border（刷新时就地换 Background）
    int _w = -1, _h = -1;            // 卡片物理像素尺寸
    bool _lastNewFrame;              // 本次抓帧是否拿到了新帧（DXGI 明确告知"桌面无变化"时为 false）
    int _capX = int.MinValue, _capY, _capW, _capH;   // 上次成功抓取的屏幕区域
    bool _dirty;

    // ===== 捕获方式（0=完整桌面 1=DXGI 屏幕 2=GDI 屏幕 3=仅下方窗口）与 DXGI 回退状态 =====
    int _captureMode;
    FloatScheduleDxgiCapture? _dxgi;
    FloatScheduleWindowCapture? _windowCapture;
    long _dxgiRetryAfterTicks;
    int _dxgiFailStreak;

    /// <summary>捕获方式：0=完整桌面（屏幕采集，采样瞬间临时排除悬浮窗自身，含所有下方窗口与壁纸；
    /// 远程会话自动退回"仅下方窗口"）；1=DXGI 屏幕采集；2=GDI BitBlt 屏幕采集；3=仅采集下方窗口
    /// （Win11 WGC / Win10 PrintWindow）。运行期可切。</summary>
    public int CaptureMode
    {
        get { lock (_lock) return _captureMode; }
        set
        {
            lock (_lock)
            {
                if (_captureMode == value) return;
                _captureMode = value;
                if (_captureMode != 0) ReleaseCaptureExclusion();
                if (_captureMode != 3)
                {
                    try { _windowCapture?.Dispose(); } catch { }
                    _windowCapture = null;
                }
                if (_captureMode == 2)   // GDI 模式不用 DXGI → 释放后端（下次切回再惰性重建）
                {
                    lock (_captureLock)
                    {
                        try { _dxgi?.Dispose(); } catch { }
                        _dxgi = null;
                        _dxgiFailStreak = 0;
                        _dxgiRetryAfterTicks = 0;
                    }
                }
            }
        }
    }

    /// <summary>已有可用折射内容。</summary>
    public bool HasContent { get { lock (_lock) return _brush != null; } }

    /// <summary>首次拿到内容、需要宿主重套一次材质（仅这一次；之后刷新不再重建内容树）。</summary>
    public bool NeedsApply { get; private set; }

    public void ClearNeedsApply() => NeedsApply = false;

    /// <summary>
    /// 取"卡片背景层"。每次 Apply 都返回**新的** Border（但底层位图/画笔复用），
    /// 避免同一个控件被反复 re-parent 造成异常；返回 null 表示暂无内容（材质层回退近似边缘光）。
    /// Border 的 CornerRadius 负责圆角裁剪。
    /// </summary>
    public Border? TakeLayer(double cornerRadius)
    {
        lock (_lock)
        {
            if (_brush == null) return null;
            _layer = new Border
            {
                CornerRadius = new CornerRadius(cornerRadius),
                IsHitTestVisible = false,
                Background = _brush,
            };
            return _layer;
        }
    }

    /// <summary>
    /// 立即恢复采样期间对窗口设置的捕获排除（把窗口还给截图/录屏）。
    /// 宿主在关闭防截图逻辑、切换材质或开启"允许截图"时应调用；平时由空闲计时器自动恢复。
    /// </summary>
    public void ReleaseCaptureExclusion()
    {
        lock (_affinityLock)
        {
            if (_excludedHwnd == IntPtr.Zero) return;
            RestoreExclusionLocked();
        }
    }

    void RestoreExclusionLocked()
    {
        FloatScheduleScreenCapture.RestoreCaptureAffinity(_excludedHwnd, _originalAffinity);
        _excludedHwnd = IntPtr.Zero;
        try { _exclusionIdleTimer?.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
    }

    // 确保窗口在采样期间被排除出捕获；第一次设置时等一帧 DWM 合成，之后持续复用，不再逐帧 flush。
    bool EnsureCaptureExclusion(IntPtr hwnd)
    {
        lock (_affinityLock)
        {
            if (_excludedHwnd == hwnd)
            {
                try { _exclusionIdleTimer?.Change(ExclusionIdleMs, Timeout.Infinite); } catch { }
                return true;
            }
            if (_excludedHwnd != IntPtr.Zero) RestoreExclusionLocked();
            if (!FloatScheduleScreenCapture.TryExcludeFromCapture(hwnd, out uint original)) return false;
            _excludedHwnd = hwnd;
            _originalAffinity = original;
            FloatScheduleScreenCapture.WaitForComposition();
            _exclusionIdleTimer ??= new Timer(_ => ReleaseCaptureExclusion(), null, Timeout.Infinite, Timeout.Infinite);
            try { _exclusionIdleTimer.Change(ExclusionIdleMs, Timeout.Infinite); } catch { }
            return true;
        }
    }

    /// <summary>
    /// 【后台线程】按当前捕获方式采样卡片背后的画面 → 折射，结果留在内部缓冲。
    /// 默认采集完整桌面（所有下方窗口 + 壁纸）：采样期间持续把悬浮窗排除出捕获，
    /// 停止采样 400ms 后自动恢复；远程会话退回"仅下方窗口"。
    /// 桌面内容与上次相同则直接返回 false（常态几乎零开销）。
    /// </summary>
    public bool CaptureAndRefract(IntPtr hwnd, int w, int h, double cornerPx)
    {
        if (w < 4 || h < 4) return false;
        if (!FloatScheduleScreenCapture.TryGetWindowOrigin(hwnd, out int ox, out int oy)) return false;

        // 完整桌面模式：确保自身已排除出捕获（首次会等 DWM 合成一帧，之后持续复用）。
        // 【必须在 _lock 之外】首次 DwmFlush 会阻塞 ~16ms；若持 _lock，UI 线程的 Commit 会被卡住。
        int mode = _captureMode;
        bool tempExcluded = false;
        if (mode == 0 && !FloatScheduleScreenCapture.IsRemoteSession())
            tempExcluded = EnsureCaptureExclusion(hwnd);
        bool wasExcluded = tempExcluded;

        lock (_lock)
        {
                _w = w; _h = h;
                if (_src == null || _srcW != w || _srcH != h)
                {
                    _src = new byte[w * h * 4];
                    _srcW = w; _srcH = h;
                    _srcValid = false;   // 缓冲被重建 → 里面的内容不再可信
                }
                if (_dst == null || _dstW != w || _dstH != h)
                {
                    _dst = new byte[w * h * 4];
                    _dstW = w; _dstH = h;
                }

                bool capOk = false, newFrame = false;
                if (mode == 0)
                {
                    if (tempExcluded)
                    {
                        // 完整桌面：所有下方窗口 + 壁纸。排除状态在采样期间持续保持（见 EnsureCaptureExclusion）。
                        lock (_captureLock)
                        {
                            capOk = TryCaptureRegionAuto(ox, oy, w, h, _src, ref _capX, ref _capY, ref _capW, ref _capH, ref _srcValid);
                            newFrame = _lastNewFrame;
                        }
                    }
                    else
                    {
                        // 远程会话 / 排除失败：退回"仅下方窗口"，保证远端仍能看到悬浮窗。
                        var target = FloatScheduleWindowCapture.FindWindowBelow(hwnd, ox, oy, w, h);
                        _windowCapture ??= new FloatScheduleWindowCapture();
                        capOk = target != IntPtr.Zero && _windowCapture.TryCapture(target, ox, oy, w, h, _src);
                        newFrame = capOk;
                        if (capOk) { _srcValid = true; _capX = int.MinValue; }
                    }
                    if (!capOk && !wasExcluded)
                    {
                        // 未排除自身时的最后兜底：普通屏幕采集（远程会话下这是唯一可用路径）。
                        lock (_captureLock)
                        {
                            capOk = TryCaptureRegionAuto(ox, oy, w, h, _src, ref _capX, ref _capY, ref _capW, ref _capH, ref _srcValid);
                            newFrame = _lastNewFrame;
                        }
                    }
                }
                else if (mode == 3)
                {
                    // 仅采集悬浮窗下方的单个窗口（Win11 WGC / Win10 PrintWindow）。
                    var target = FloatScheduleWindowCapture.FindWindowBelow(hwnd, ox, oy, w, h);
                    _windowCapture ??= new FloatScheduleWindowCapture();
                    capOk = target != IntPtr.Zero && _windowCapture.TryCapture(target, ox, oy, w, h, _src);
                    newFrame = capOk;
                    if (capOk)
                    {
                        _srcValid = true;
                        _capX = int.MinValue;   // 区域跟踪失效，回退屏幕采集时强制重抓
                    }
                    else
                    {
                        lock (_captureLock)
                        {
                            capOk = TryCaptureRegionAuto(ox, oy, w, h, _src, ref _capX, ref _capY, ref _capW, ref _capH, ref _srcValid);
                            newFrame = _lastNewFrame;
                        }
                    }
                }
                else
                {
                    lock (_captureLock)
                    {
                        capOk = TryCaptureRegionAuto(ox, oy, w, h, _src, ref _capX, ref _capY, ref _capW, ref _capH, ref _srcValid);
                        newFrame = _lastNewFrame;
                    }
                }
                if (!capOk) return false;

                // 抓帧后端给出的**权威信号**：只有屏幕采集（DXGI）明确告知"桌面无新帧"时才跳过折射上传；
                // GDI 抓到即视为新帧，窗口采集每次都要重折射（卡片位置变化会改变裁剪区域）。
                if (!newFrame && _brush != null) return false;

                FloatScheduleScreenCapture.RefractForCard(_src!, _dst!, w, h, cornerPx);
                _dirty = true;
                return true;
        }
    }

    /// <summary>【UI 线程】把结果原位写回位图并就地刷新背景（不触碰内容树）。第一次就绪时置 NeedsApply。</summary>
    public void Commit()
    {
        lock (_lock) CommitLocked();
    }

    // 【须持有 _lock】实际提交：把前台缓冲 _dst 拷进位图、就地换 Background。
    void CommitLocked()
    {
        if (!_dirty || _dst == null || _dstW < 4 || _dstH < 4 || _w < 4 || _h < 4) return;
        _dirty = false;
        int dw = _dstW, dh = _dstH;
        if (_bmp == null || _bmp.PixelSize.Width != dw || _bmp.PixelSize.Height != dh)
            _bmp = new WriteableBitmap(new PixelSize(dw, dh), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = _bmp.Lock())
        {
            if (fb.RowBytes == dw * 4)
            {
                Marshal.Copy(_dst, 0, fb.Address, dw * dh * 4);
            }
            else
            {
                for (int y = 0; y < dh; y++) Marshal.Copy(_dst, y * dw * 4, fb.Address + y * fb.RowBytes, dw * 4);
            }
        }
        // 重建画笔（同一张位图）→ 保证 Background 变更触发重绘，且不需要任何内容树重建。
        // 位图尺寸 == 卡片区域 → 不需要 SourceRect，整幅铺满即可（Border 的 CornerRadius 负责圆角）。
        _brush = new ImageBrush(_bmp) { Stretch = Stretch.Fill };
        if (_layer != null) _layer.Background = _brush;   // 就地换背景
        else NeedsApply = true;                            // 还没有挂载的层 → 让宿主重套一次材质
    }

    // ===================== 内部实现 =====================

    /// <summary>
    /// 抓屏幕上 (x,y,w,h) 到 dst：DXGI 优先，不可用/失败时回退 GDI BitBlt。
    /// 返回 true 表示 dst 现在持有该区域的**有效**内容（可能是刚抓到的新帧，也可能是"桌面无变化"时沿用的上一帧）。
    /// 区域跟踪与"dst 是否有内容"由调用方通过 ref 传入。
    /// 须在 _captureLock 内调用。
    /// </summary>
    bool TryCaptureRegionAuto(int x, int y, int w, int h, byte[] dst,
        ref int capX, ref int capY, ref int capW, ref int capH, ref bool dstValid)
    {
        // 目标区域是否发生变化：区域变了就必须重新渲染（哪怕桌面本身没变），
        // 否则"桌面无新帧"时沿用旧缓冲会渲染出**错位**内容。
        bool regionChanged = x != capX || y != capY || w != capW || h != capH;
        if (_captureMode != 2)
        {
            long now = Environment.TickCount64;
            if (now >= _dxgiRetryAfterTicks)   // 冷却期内直接走 GDI，避免每次采样都在失败的 DXGI 上白花时间
            {
                _dxgi ??= new FloatScheduleDxgiCapture();
                if (_dxgi.TryCapture(x, y, w, h, dst))
                {
                    _dxgiFailStreak = 0;
                    dstValid = true;
                    _lastNewFrame = true;
                    capX = x; capY = y; capW = w; capH = h;
                    return true;
                }
                // 桌面无变化（AcquireNextFrame 超时）但上一帧仍代表这块区域 → 沿用缓冲里已有内容。
                // 注意：只有**区域没变**时才允许沿用；区域变了则必须当作"有新内容"重新渲染，
                //   否则会把旧区域的像素按新位置渲染出来（错位）。
                if (_dxgi.LastFrameValid && dstValid)
                {
                    _dxgiFailStreak = 0;
                    _lastNewFrame = regionChanged;
                    return true;
                }
                if (!_dxgi.LastAcquireTimedOut)
                {
                    // 硬失败（初始化失败 / 被其他进程独占 / AccessLost / 旋转输出等）→ 连续失败到阈值即冷却
                    if (++_dxgiFailStreak >= DxgiFailThreshold)
                    {
                        try { _dxgi.Dispose(); } catch { }
                        _dxgi = null;
                        _dxgiFailStreak = 0;
                        _dxgiRetryAfterTicks = now + DxgiRetryIntervalMs;
                    }
                }
            }
        }

        // GDI 回退：单次固定 ~24ms，但保证"一定能拿到内容"
        if (FloatScheduleScreenCapture.TryCaptureScreenRegion(x, y, w, h, dst))
        {
            dstValid = true;
            _lastNewFrame = true;   // GDI 每次都是实抓，视为新帧
            capX = x; capY = y; capW = w; capH = h;
            return true;
        }
        return false;
    }
}
