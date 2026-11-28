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
// 【采样来源】只有一种：GDI BitBlt 抓**整屏**（所有下方窗口 + 壁纸）。采样期间持续把悬浮窗
//   排除出捕获（WDA_EXCLUDEFROMCAPTURE），因此抓到的就是自己正下方的合成画面，不会拍到自己。
//   DXGI Desktop Duplication / 仅下方窗口（WGC / PrintWindow）/ 完整桌面等方案已按需求删除，
//   连同 Vortice 依赖一起去掉。
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
    readonly object _lock = new();
    /// <summary>抓帧串行化锁：同一时刻只允许一个抓帧进入 GDI。</summary>
    readonly object _captureLock = new();

    // ===== 采样期间的"持续排除自身"状态 =====
    // 逐帧"排除→抓→恢复"会让 DWM 来不及合成，必须每帧 DwmFlush（16~60ms），这是延迟主因。
    // 改为采样期间持续排除：只在开始采样时 flush 一次；停止采样（无调用）400ms 后自动恢复。
    readonly object _affinityLock = new();
    IntPtr _excludedHwnd;
    uint _originalAffinity;

    // ===== 抓帧 / 折射缓冲 =====
    byte[]? _src;                    // 采样抓帧缓冲（尺寸 = 卡片）
    int _srcW, _srcH;                // _src 的尺寸
    byte[]? _dst;                    // 前台折射结果（尺寸 = 卡片）—— 供 Commit 上传位图
    int _dstW, _dstH;
    WriteableBitmap? _bmp;           // 卡片背景位图（按尺寸缓存复用，不逐帧新建）
    ImageBrush? _brush;              // 背景画笔（整幅位图直接铺满卡片）
    Border? _layer;                  // 最近一次 Apply 挂进可视树的那个 Border（刷新时就地换 Background）
    int _w = -1, _h = -1;            // 卡片物理像素尺寸
    bool _dirty;

    // ===== 采集方式：已归中为唯一的 GDI BitBlt 整屏采集 =====
    bool _preventCapture;

    /// <summary>用户"阻止截图"是否开启（由宿主 / 子进程在每次采样前同步）。
    /// GDI 抓屏只保留该状态供宿主查询；采样本身不再因此切换采集方式（GDI 抓到的一直是自己背后的画面）。</summary>
    public bool PreventCapture
    {
        get { lock (_lock) return _preventCapture; }
        set
        {
            // UI 线程写入；采样线程可能正持有 _lock 抓帧 → 拿不到就跳过，宿主下一 tick 会重试。
            if (!Monitor.TryEnter(_lock)) return;
            try { _preventCapture = value; }
            finally { Monitor.Exit(_lock); }
        }
    }

    /// <summary>已有可用折射内容。【UI 线程读取，无锁】：_brush 只由 UI 线程的 Commit 写入。</summary>
    public bool HasContent => _brush != null;

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
        // 【UI 线程调用，无锁】_brush 只由 UI 线程的 Commit 写入；这里绝不等待采样线程的 _lock，
        // 否则抓帧卡住时会把 UI 线程一起拖死（表现为程序无响应）。
        var brush = _brush;
        if (brush == null) return null;
        _layer = new Border
        {
            CornerRadius = new CornerRadius(cornerRadius),
            IsHitTestVisible = false,
            Background = brush,
        };
        return _layer;
    }

    /// <summary>当前是否正持有采样期间的"排除捕获"（方案2 经典采集）。宿主周期重设防截图时应避免把它覆盖掉。</summary>
    public bool IsCaptureExclusionActive
    {
        get { lock (_affinityLock) return _excludedHwnd != IntPtr.Zero; }
    }

    /// <summary>
    /// 立即恢复采样期间对窗口设置的捕获排除（把窗口还给截图/录屏）。
    /// 宿主在切换材质/关闭玻璃采样时调用（对齐 cf78b44 的 RestoreAffinity 语义）。
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
    }

    // 确保窗口处于"排除捕获"态（对齐 cf78b44 的 EnsureExcludedFromCapture）：
    // 只设置、不释放，采样期间持续保持；停止采样由宿主显式 Release。
    bool EnsureCaptureExclusion(IntPtr hwnd)
    {
        lock (_affinityLock)
        {
            if (_excludedHwnd == hwnd) return true;
            if (_excludedHwnd != IntPtr.Zero) RestoreExclusionLocked();
            if (!FloatScheduleScreenCapture.TryExcludeFromCapture(hwnd, out uint original)) return false;
            _excludedHwnd = hwnd;
            _originalAffinity = original;
            return true;
        }
    }

    /// <summary>
    /// 【后台线程】用 GDI BitBlt 抓卡片背后的整屏画面 → 折射，结果留在内部缓冲。
    /// 采样期间持续把悬浮窗排除出捕获，停止采样 400ms 后自动恢复。
    /// </summary>
    public bool CaptureAndRefract(IntPtr hwnd, int w, int h, double cornerPx)
    {
        if (w < 4 || h < 4) return false;
        if (!FloatScheduleScreenCapture.TryGetWindowOrigin(hwnd, out int ox, out int oy)) return false;

        // GDI 抓屏前确保自身已排除出捕获：BitBlt 抓到的就是"自己正下方的合成画面"。
        // 【必须在 _lock 之外】首次设置排除后要等 DWM 合成一帧，持锁会把 UI 线程的 Commit 卡住。
        // 远程会话同样排除：不排除会把自己（含上一帧玻璃内容）一起抓进来，形成自我递归。
        EnsureCaptureExclusion(hwnd);

        lock (_lock)
        {
                _w = w; _h = h;
                if (_src == null || _srcW != w || _srcH != h)
                {
                    _src = new byte[w * h * 4];
                    _srcW = w; _srcH = h;
                }
                if (_dst == null || _dstW != w || _dstH != h)
                {
                    _dst = new byte[w * h * 4];
                    _dstW = w; _dstH = h;
                }

                bool capOk;
                lock (_captureLock)
                    capOk = FloatScheduleScreenCapture.TryCaptureScreenRegion(ox, oy, w, h, _src!);
                if (!capOk) return false;

                // 【纯黑防御】自身被排除出捕获时，DWM 有时会把窗口区域返回为纯黑（截图工具重配捕获会话后）。
                // 纯黑不是桌面内容：丢弃本帧、保留上一帧，绝不把黑块提交上去。
                if (IsNearlyAllBlack(_src!, w, h)) return false;

                // cf78b44：pad=0、卡片铺满整幅；keepOutsidePixels=true 让圆角外的像素保持抓到的桌面
                // （圆角裁剪由承载 Border 的 CornerRadius 负责）。
                // transparentCenter=true：鸿蒙风格 —— 只有贴边一带折射，中部全透明，直接露出真实桌面。
                FloatScheduleScreenCapture.RefractForCard(_src!, _dst!, w, h, cornerPx, 0, 0, w, h, true, transparentCenter: true);
                _dirty = true;
                return true;
        }
    }

    // 判断抓到的帧是否几乎全黑（自身被排除出捕获时，屏幕采集会把窗口区域返回为纯黑）。
    static bool IsNearlyAllBlack(byte[] bgra, int w, int h)
    {
        if (bgra == null || w <= 0 || h <= 0) return false;
        int count = w * h;
        int step = Math.Max(1, count / 4096);   // 最多采样约 4096 个点
        int total = 0, black = 0;
        for (int i = 0; i < count; i += step)
        {
            int o = i * 4;
            total++;
            if (bgra[o] < 8 && bgra[o + 1] < 8 && bgra[o + 2] < 8) black++;
        }
        return total > 0 && black >= total * 0.9;
    }

    /// <summary>【UI 线程】把结果原位写回位图并就地刷新背景（不触碰内容树）。第一次就绪时置 NeedsApply。</summary>
    public void Commit()
    {
        // 【绝不阻塞 UI】采样线程可能正持有 _lock 做抓帧/折射（PrintWindow 等系统调用甚至可能卡住）。
        // 拿不到锁就跳过这一帧：下一帧再提交，绝不让 UI 线程等待。
        if (!Monitor.TryEnter(_lock)) return;
        try { CommitLocked(); }
        finally { Monitor.Exit(_lock); }
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
}
