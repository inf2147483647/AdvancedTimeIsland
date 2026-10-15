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
// 【捕获方式】DXGI Desktop Duplication 优先（亚毫秒、非阻塞），不可用/连续失败时自动回退 GDI BitBlt
//   （单次固定 ~24ms），并在冷却期后定期重试恢复 DXGI。抓帧走同一份缓冲 → 折射核心完全不变。
//
// =====================================================================================
// 【本文件的核心：消除拖动时"透射内容抖动"】
//   根因（前几轮修复都没解决的）：**内容更新与窗口移动不在同一合成帧**。
//     · 窗口移动由窗口管理器/合成器在某一帧应用；
//     · 背景换图要等 Commit() 之后的下一次渲染才生成新纹理。
//   两者天然不同步 → 拖动时有时背景超前 1px、有时窗口超前 1px → 内容相对卡片来回抖 1px。
//   换线程/统一坐标/双缓冲都治不了，因为**每帧换图 = 每帧制造一次不同步机会**。
//
//   正确解法：**逐帧位移交给 GPU 变换（与窗口移动在同一合成帧内），折射只按采样率重烘焙**：
//     ① 抓帧时连**外扩区**一起折射，得到一张**比卡片大**的"已折射图"（卡片在其中位于 (pad,pad) 处）；
//     ② 拖动时 UI 线程**只做一次廉价的平移变换** —— 改 ImageBrush 的取样矩形（SourceRect）指向
//        已折射大图内的卡片子矩形。**不抓帧、不折射、不写位图、不新建 Brush/位图**；
//     ③ 后台工作线程在"自上次烘焙以来的累计位移 ≥ 阈值（8 物理像素）"时**重烘焙**（重抓外扩 + 重折射整幅），
//        发布后 UI 侧把平移量按新烘焙基准归零（新烘焙已对齐当前位置 → 归零不产生视觉跳变）；
//     ④ 于是透镜最多滞后"阈值"像素，而**逐帧运动是纯 GPU 变换** → 抖动从机制上消失。
//   为什么选"改 ImageBrush.SourceRect"而不是"Clip 容器 + TranslateTransform"：
//     SourceRect 是绝对像素矩形，直接指向已折射大图里的卡片子区域，配合 Border 自身的 CornerRadius 圆角裁剪，
//     无需额外几何 Clip；且它是**纯属性写入**（无分配、无布局、无渲染树改动），比挂 TranslateTransform 更轻。
// =====================================================================================
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

    /// <summary>拖动位移补偿的默认抓帧外扩量（物理像素）：上下左右各留这么多，供 GPU 平移取材。</summary>
    public const int DefaultDragCompensationPadPx = 96;

    /// <summary>
    /// 重烘焙阈值（物理像素）：自上次烘焙以来累计位移的 max(|dx|,|dy|) 达到该值即重烘焙。
    /// 透镜滞后不超过该值，而逐帧运动始终是纯 GPU 平移。
    /// </summary>
    public const int RebakeThresholdPx = 8;

    /// <summary>两次重烘焙之间的最短间隔（毫秒）：避免 GDI 抓帧（~24ms）跟不上时把重烘焙排成一长串。</summary>
    const int RebakeMinIntervalMs = 60;

    readonly object _lock = new();
    /// <summary>抓帧串行化锁：让"采样抓帧"与"拖动重烘焙抓帧"不会同时进入抓帧后端（DXGI/GDI）。</summary>
    readonly object _captureLock = new();

    // ===== 烘焙（抓帧 + 折射整幅含外扩）=====
    byte[]? _src;                    // 采样抓帧缓冲（含外扩边：尺寸 = 卡片 + 2*pad）
    int _srcW, _srcH;                // _src 的尺寸（含外扩）
    bool _srcValid;                  // _src 是否已装填过可用内容
    byte[]? _dst;                    // 前台折射结果（含外扩，尺寸 = _src 尺寸）—— 供 Commit 上传位图
    int _dstW, _dstH;
    WriteableBitmap? _bmp;           // 卡片背景位图（含外扩，按尺寸缓存复用，不逐帧新建）
    ImageBrush? _brush;              // 背景画笔：SourceRect 指向已折射大图内的卡片子矩形
    Border? _layer;                  // 最近一次 Apply 挂进可视树的那个 Border（刷新时就地换 Background）
    int _w = -1, _h = -1;            // 卡片物理像素尺寸
    bool _lastNewFrame;              // 本次抓帧是否拿到了新帧（DXGI 明确告知"桌面无变化"时为 false）
    int _capX = int.MinValue, _capY, _capW, _capH;   // 采样上次成功抓取的屏幕区域
    bool _dirty;

    // ===== 外扩几何 / 烘焙基准 =====
    int _pad;                        // 本次烘焙的有效外扩量（物理像素）
    IntPtr _hwnd;                    // 最近一次抓帧的窗口句柄
    int _reqPad;                     // 最近一次请求的外扩量
    int _refWinX, _refWinY;          // 当前已发布烘焙所对应的窗口原点（屏幕物理像素）—— GPU 平移的基准
    bool _paddedValid;               // 外扩缓冲是否可用于 GPU 平移（pad>0 且内容有效）
    int _dragDx, _dragDy;            // 当前施加的 GPU 平移量（相对烘焙基准，物理像素）

    // ===== 拖动重烘焙：后台线程 =====
    readonly object _dragLock = new();
    byte[]? _capBack;                // 工作线程抓帧缓冲（含外扩），仅工作线程访问
    int _capBackW, _capBackH;
    bool _capBackValid;              // _capBack 是否已对该区域写入过有效内容
    int _wcapX = int.MinValue, _wcapY, _wcapW, _wcapH;   // 工作线程自己的区域跟踪
    byte[]? _dstBack;                // 工作线程折射结果（含外扩），仅工作线程访问
    long _dragSession;               // 拖动补偿会话号：拖动结束/重开时递增 → 作废所有在途结果
    long _dragReqVersion;            // UI 线程递增的请求版本（最新优先）
    long _dragProcessedVersion;      // 工作线程已处理到的请求版本（用于合并过期请求）
    long _dragFrameVersion;          // 工作线程每次成功发布递增（"有新结果"的版本号）
    long _dragCommittedVersion;      // UI 线程已提交到的版本（与之相等即无需再 Commit）
    double _dragReqCorner;           // 最新请求的圆角（物理像素）
    bool _dragWorkerStop;            // 通知工作线程退出（不调也能靠 IsBackground 随进程退出）
    Thread? _dragWorker;
    AutoResetEvent? _dragWake;
    long _lastRebakeTick;            // 上次重烘焙时刻（最短间隔用）

    // ===== 捕获方式（0=DXGI 优先 1=GDI BitBlt）与 DXGI 回退状态 =====
    int _captureMode;
    FloatScheduleDxgiCapture? _dxgi;
    long _dxgiRetryAfterTicks;
    int _dxgiFailStreak;

    /// <summary>捕获方式：0=DXGI Desktop Duplication（优先）1=GDI BitBlt（回退）。运行期可切。</summary>
    public int CaptureMode
    {
        get { lock (_lock) return _captureMode; }
        set
        {
            lock (_lock)
            {
                if (_captureMode == value) return;
                _captureMode = value;
                if (_captureMode != 0)   // 切到 GDI → 释放 DXGI 后端（下次切回再惰性重建）
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

    /// <summary>拖动位移补偿开关（关=拖动期间不动背景，等采样定时器重抓）。</summary>
    public bool DragCompensationEnabled { get; set; } = true;

    /// <summary>
    /// 宿主"正在拖动窗口"标记（由宿主在拖拽开始/结束时置位）。
    /// 拖动 + 拖动位移补偿开启时，采样抓帧会被跳过：拖动期间背景完全由
    /// GPU 平移（<see cref="SetDragTransform"/>）+ 后台重烘焙（<see cref="RequestDragCompensation"/>）维护，
    /// 既避免每帧重抓，也避免"目标区域每帧都变"造成的 DXGI 重复重建。
    /// </summary>
    bool _dragInProgress;
    long _contentGen;                // 内容代次：拖动开始即递增 → 作废"拖动前启动、拖动后才完成"的采样结果

    /// <summary>
    /// 是否正处于拖动中。
    /// 置 true 时会递增内容代次，使在途的采样抓帧结果作废 —— 否则"拖动前一瞬间启动"的那次采样
    /// 会在拖动开始后才完成并提交，把背景瞬间换回拖动前位置的内容（表现为拖动起始闪 1 帧旧位置）。
    /// </summary>
    public bool DragInProgress
    {
        get => _dragInProgress;
        set
        {
            if (_dragInProgress == value) return;
            _dragInProgress = value;
            if (value) { lock (_lock) _contentGen++; }
        }
    }

    /// <summary>内容代次。调用方在异步抓帧前记录、完成后比对：若已变化说明期间开始了拖动，应丢弃该次结果。</summary>
    public long ContentGeneration { get { lock (_lock) return _contentGen; } }

    /// <summary>已有可用折射内容。</summary>
    public bool HasContent { get { lock (_lock) return _brush != null; } }

    /// <summary>是否具备"拖动 GPU 平移 + 重烘焙"条件（抓到了带外扩边的缓冲）。</summary>
    public bool CanCompensateDrag { get { lock (_lock) return _paddedValid && _srcValid; } }

    /// <summary>首次拿到内容、需要宿主重套一次材质（仅这一次；之后刷新不再重建内容树）。</summary>
    public bool NeedsApply { get; private set; }

    public void ClearNeedsApply() => NeedsApply = false;

    /// <summary>
    /// 取"卡片背景层"。每次 Apply 都返回**新的** Border（但底层位图/画笔复用），
    /// 避免同一个控件被反复 re-parent 造成异常；返回 null 表示暂无内容（材质层回退近似边缘光）。
    /// Border 的 CornerRadius 负责圆角裁剪；背景画笔的 SourceRect 只取样卡片子矩形 → 外扩区不外漏。
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
    /// 【后台线程】抓窗口背后的桌面 → 折射，结果留在内部缓冲。
    /// 桌面内容与上次相同则直接返回 false（常态几乎零开销）。
    /// 前置：本方法内部会确保窗口处于"排除捕获"态（状态迁移式，不会每帧切换 DWM 亲和性）。
    /// </summary>
    public bool CaptureAndRefract(IntPtr hwnd, int w, int h, double cornerPx)
        => CapturePaddedAndRefract(hwnd, w, h, cornerPx, 0);

    /// <summary>
    /// 【后台线程】抓"窗口所占区域 + 四周各外扩 pad 物理像素"的桌面 → 折射**整幅**（含外扩），结果留在内部缓冲。
    /// 卡片在这张"已折射大图"里位于 (effPad, effPad)，拖动时 UI 侧只需改取样矩形即可纯 GPU 平移。
    /// 返回 true 表示缓冲已更新（调用方应随后在 UI 线程 Commit）。
    /// </summary>
    public bool CapturePaddedAndRefract(IntPtr hwnd, int w, int h, double cornerPx, int pad)
    {
        if (w < 4 || h < 4) return false;
        // 拖动期间不抓帧（背景由 GPU 平移 + 重烘焙维护）——但**仅当补偿真的可用**时才暂停。
        // 贴到屏幕边缘时外扩量会被夹到 0（ClampPadToMonitor 保证外扩区不跨出同一显示器），
        // 此时若仍暂停抓帧，两个路径都不更新 → 表现为"贴边后内容冻结"。故补偿不可用时继续采样抓帧。
        if (DragInProgress && DragCompensationEnabled && CanCompensateDrag) return false;
        if (!FloatScheduleScreenCapture.EnsureExcludedFromCapture(hwnd)) return false;   // 拿不到排除态就不能抓（会抓到自己）
        if (!FloatScheduleScreenCapture.TryGetWindowOrigin(hwnd, out int ox, out int oy)) return false;
        _hwnd = hwnd; _reqPad = pad;   // 记下来，供拖动重烘焙沿用

        lock (_lock)
        {
            // 有效外扩量：不超过所在显示器边界（保证外扩区域仍在同一显示器/DXGI output 内）
            int effPad = FloatScheduleScreenCapture.ClampPadToMonitor(hwnd, ox, oy, w, h, pad);
            int pw = w + effPad * 2, ph = h + effPad * 2;
            int x0 = ox - effPad, y0 = oy - effPad;

            _w = w; _h = h;   // 卡片物理尺寸
            if (_src == null || _srcW != pw || _srcH != ph)
            {
                _src = new byte[pw * ph * 4];
                _srcW = pw; _srcH = ph;
                _srcValid = false;   // 缓冲被重建 → 里面的内容不再可信
            }
            if (_dst == null || _dstW != pw || _dstH != ph)
            {
                _dst = new byte[pw * ph * 4];   // 前后台同为外扩尺寸
                _dstW = pw; _dstH = ph;
            }

            bool capOk, newFrame;
            lock (_captureLock)
            {
                capOk = TryCaptureRegionAuto(x0, y0, pw, ph, _src, ref _capX, ref _capY, ref _capW, ref _capH, ref _srcValid);
                newFrame = _lastNewFrame;
            }
            if (!capOk) return false;

            // 记载几何：卡片在缓冲内的基准位置是 (effPad, effPad)，对应屏幕原点 (ox, oy)
            _pad = effPad;
            _refWinX = ox; _refWinY = oy;
            _paddedValid = effPad > 0;

            // 抓帧后端给出的**权威信号**：只有 DXGI 明确告知"桌面无新帧"时才跳过折射上传；GDI 抓到即视为新帧。
            if (!newFrame && _brush != null) return false;

            RefractPaddedLocked(cornerPx);
            _dragDx = 0; _dragDy = 0;   // 新烘焙已对齐当前位置 → GPU 平移量归零
            _dirty = true;
            return true;
        }
    }

    // 【须持有 _lock】把 _src 整幅（含外扩）折射进前台 _dst：卡片位于 (pad, pad)、尺寸 w×h。
    // keepOutsidePixels=true：卡片之外（含外扩区）保持抓到的原始桌面像素 —— 拖动取样矩形平移后露出的
    // 外扩区才会显示真实桌面而非透明（否则卡片边缘出现透明空洞）；圆角裁剪由承载 Border 的 CornerRadius 负责。
    void RefractPaddedLocked(double cornerPx)
    {
        FloatScheduleScreenCapture.RefractForCard(_src!, _dst!, _srcW, _srcH, cornerPx, _pad, _pad, _w, _h, true);
    }

    /// <summary>
    /// 【UI 线程】把逐帧位移施加到背景上——**只改 ImageBrush 的取样矩形**（绝对像素），
    /// 不抓帧、不折射、不写位图、不新建 Brush。dxPx/dyPx 为"相对当前烘焙基准"的位移（物理像素）。
    /// 取样矩形被夹在 [0, 2*pad] 内 → 平移量在 pad 范围内时子矩形**永不越界**。
    /// </summary>
    #region debug-point G1
    // 插桩：拖动抖动取证（会话 glass-drag-jitter）。只写日志，不改业务逻辑。
    // 证据目录在仓库外，避免被 watch/打包吸收；可用 TRAE_DEBUG_DIR 覆盖。
    static readonly string _dbgPath = System.IO.Path.Combine(
        Environment.GetEnvironmentVariable("TRAE_DEBUG_DIR")
            ?? @"C:\Users\seewo\AppData\Local\Temp\trae-debug\glass-drag-jitter",
        "trae-debug-log-glass-drag-jitter.ndjson");
    static void Dbg(string ev, string fields)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_dbgPath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(_dbgPath,
                "{\"ts\":" + Environment.TickCount64 + ",\"ev\":\"" + ev + "\"," + fields + "}\n");
        }
        catch { }
    }
    #endregion

    public void SetDragTransform(double dxPx, double dyPx)
    {
        lock (_lock)
        {
            if (_brush == null || _pad <= 0) return;
            int dx = (int)Math.Round(dxPx), dy = (int)Math.Round(dyPx);
            dx = Math.Clamp(dx, -_pad, _pad);
            dy = Math.Clamp(dy, -_pad, _pad);
            if (dx == _dragDx && dy == _dragDy) return;   // 窗口静止时连续调用 → 完全一致的高效短路
            _dragDx = dx; _dragDy = dy;
            _brush.SourceRect = new RelativeRect(_pad + dx, _pad + dy, _w, _h, RelativeUnit.Absolute);
            #region debug-point G1
            Dbg("transform",
                "\"reqDx\":" + dxPx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"reqDy\":" + dyPx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"appDx\":" + dx + ",\"appDy\":" + dy +
                ",\"srcRectX\":" + (_pad + dx) + ",\"srcRectY\":" + (_pad + dy) +
                ",\"pad\":" + _pad + ",\"w\":" + _w + ",\"h\":" + _h);
            #endregion
        }
    }

    /// <summary>
    /// 【UI 线程】拖动一帧的完整廉价处理：读窗口真实位置 → 只改 GPU 取样矩形；累计位移达阈值时请求后台重烘焙。
    /// 位置与烘焙基准**统一取自 Win32 GetWindowRect**（窗口静止时两次读数严格相等 → 位移不会 0/1 交替）。
    /// </summary>
    public void UpdateDragTransform(double cornerPx)
    {
        IntPtr hwnd; int baseX, baseY, pad;
        lock (_lock)
        {
            if (_brush == null || _pad <= 0 || _hwnd == IntPtr.Zero) return;
            hwnd = _hwnd; baseX = _refWinX; baseY = _refWinY; pad = _pad;
        }
        if (!FloatScheduleScreenCapture.TryGetWindowOrigin(hwnd, out int wx, out int wy)) return;
        int dx = wx - baseX, dy = wy - baseY;
        SetDragTransform(dx, dy);
        // 阈值不超过外扩余量 → 取样矩形永远不越界（保证不会显示到外扩区/错位内容）
        int thresh = Math.Min(RebakeThresholdPx, Math.Max(1, pad));
        bool rebake = Math.Max(Math.Abs(dx), Math.Abs(dy)) >= thresh;
        #region debug-point G1
        Dbg("frame",
            "\"wx\":" + wx + ",\"wy\":" + wy +
            ",\"baseX\":" + baseX + ",\"baseY\":" + baseY +
            ",\"dx\":" + dx + ",\"dy\":" + dy +
            ",\"pad\":" + pad + ",\"thresh\":" + thresh + ",\"rebake\":" + (rebake ? 1 : 0));
        #endregion
        if (rebake) RequestDragCompensation(cornerPx);
    }

    /// <summary>
    /// 【UI 线程】请求一次后台重烘焙（**非阻塞**）：把"重抓外扩 + 重折射整幅"交给后台工作线程，UI 立即返回。
    /// 工作线程会自行判断"自上次烘焙以来的累计位移是否达到阈值"，未达阈值则跳过（合并过期请求）。
    /// </summary>
    public void RequestDragCompensation(double cornerPx)
    {
        bool needStart;
        lock (_dragLock)
        {
            _dragReqCorner = cornerPx;
            _dragReqVersion++;                     // 版本号只增；工作线程总取最新版本（过期请求自动丢弃）
            needStart = _dragWorker == null;
        }
        if (needStart) StartDragWorker();
        _dragWake?.Set();
    }

    /// <summary>
    /// 【UI 线程】若后台已发布新的重烘焙结果 → 就地换背景（Commit）；返回 true 表示本次提交了新帧。
    /// 版本号未变（无新结果）时直接返回，不做任何 UI 操作。
    /// </summary>
    public bool TryCommitLatest()
    {
        lock (_lock)
        {
            if (_dragFrameVersion == _dragCommittedVersion) return false;
            _dragCommittedVersion = _dragFrameVersion;
            CommitLocked();
            return true;
        }
    }

    /// <summary>
    /// 【UI 线程】结束拖动补偿：递增会话号作废所有在途结果，并丢弃"已发布但尚未提交"的旧帧，
    /// 避免松手后后台仍在跑的上一次拖动的旧结果，覆盖紧接着"按新位置重抓"的新内容。
    /// </summary>
    public void EndDragCompensation()
    {
        lock (_dragLock)
        {
            _dragSession++;
            _dragProcessedVersion = _dragReqVersion;    // 丢弃尚未处理的旧请求
        }
        lock (_lock)
        {
            _dragCommittedVersion = _dragFrameVersion;  // 丢弃尚未提交的旧结果
        }
    }

    // 【后台线程】启动重烘焙工作线程（惰性、单例；IsBackground 保证不阻塞进程退出）。
    void StartDragWorker()
    {
        lock (_dragLock)
        {
            if (_dragWorker != null) return;
            _dragWake = new AutoResetEvent(false);
            _dragWorkerStop = false;
            var t = new Thread(DragWorkerLoop) { IsBackground = true, Name = "ATI-GlassDragRebake" };
            _dragWorker = t;
            t.Start();
        }
    }

    // 【后台线程】重烘焙工作循环：等到"有新请求" → 取最新请求版本（合并过期）→ 判断阈值 → 重烘焙 → 发布。
    void DragWorkerLoop()
    {
        var wake = _dragWake!;
        while (true)
        {
            wake.WaitOne();
            if (_dragWorkerStop) return;
            long ver, session;
            double corner;
            lock (_dragLock)
            {
                if (_dragWorkerStop) return;
                ver = _dragReqVersion;
                if (ver == _dragProcessedVersion) continue;   // 无新请求（或被 EndDragCompensation 作废）→ 等下一次唤醒
                _dragProcessedVersion = ver;
                session = _dragSession;
                corner = _dragReqCorner;
            }
            try { if (RebakeAndPublish(session, corner)) _lastRebakeTick = Environment.TickCount64; }
            catch { /* 后台计算异常不得影响 UI，忽略本次 */ }
        }
    }

    // 【后台线程】重烘焙：累定位移达阈值 → 重抓外扩区 + 重折射整幅 → 快速拷进前台缓冲并发布版本号。
    // 抓帧与折射在**不持 _lock** 下完成（只写工作线程私有缓冲），UI 线程读 _dst 的 Commit 不受阻塞。
    bool RebakeAndPublish(long session, double cornerPx)
    {
        IntPtr hwnd; int w, h, reqPad, baseX, baseY;
        lock (_lock)
        {
            if (session != _dragSession) return false;                 // 会话已结束 → 丢弃
            if (!_paddedValid || !_srcValid) return false;             // 无外扩缓冲（贴边 pad=0）→ 交给采样路径
            if (_w < 4 || _h < 4 || _hwnd == IntPtr.Zero) return false;
            hwnd = _hwnd; w = _w; h = _h; reqPad = _reqPad; baseX = _refWinX; baseY = _refWinY;
        }

        if (!FloatScheduleScreenCapture.TryGetWindowOrigin(hwnd, out int wx, out int wy)) return false;
        int dx = wx - baseX, dy = wy - baseY;
        int thresh = Math.Min(RebakeThresholdPx, Math.Max(1, Math.Max(1, reqPad)));
        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) < thresh) return false;   // 未达阈值 → 逐帧 GPU 平移足够，不重烘焙
        if (Environment.TickCount64 - _lastRebakeTick < RebakeMinIntervalMs) return false;   // 最短间隔（防 GDI 抓帧排队）

        if (!FloatScheduleScreenCapture.EnsureExcludedFromCapture(hwnd)) return false;
        int effPad = FloatScheduleScreenCapture.ClampPadToMonitor(hwnd, wx, wy, w, h, reqPad);
        int pw = w + effPad * 2, ph = h + effPad * 2;
        int x0 = wx - effPad, y0 = wy - effPad;

        // 工作线程私有缓冲（单线程 owner，无需锁）
        if (_capBack == null || _capBackW != pw || _capBackH != ph)
        {
            _capBack = new byte[pw * ph * 4];
            _capBackW = pw; _capBackH = ph;
            _capBackValid = false;
            _wcapX = int.MinValue;   // 强制重新渲染（新缓冲没有内容）
        }
        if (_dstBack == null || _dstBack.Length < pw * ph * 4) _dstBack = new byte[pw * ph * 4];

        bool capOk; bool newFrame;
        lock (_captureLock)
        {
            capOk = TryCaptureRegionAuto(x0, y0, pw, ph, _capBack, ref _wcapX, ref _wcapY, ref _wcapW, ref _wcapH, ref _capBackValid);
            newFrame = _lastNewFrame;
        }
        if (!capOk) return false;
        // 仅当"本次确有新帧"或"该区域此前已写入过 _capBack"时 _capBack 才可信（否则可能沿用别人的缓冲 → 花屏）
        if (!newFrame && !_capBackValid) return false;
        _wcapX = x0; _wcapY = y0; _wcapW = pw; _wcapH = ph;

        FloatScheduleScreenCapture.RefractForCard(_capBack, _dstBack, pw, ph, cornerPx, effPad, effPad, w, h, true);

        lock (_lock)
        {
            if (session != _dragSession) return false;                 // 返回期间会话结束 → 丢弃这份结果
            if (_w != w || _h != h) return false;                      // 返回期间卡片尺寸变化 → 丢弃
            if (_dst == null || _dstW != pw || _dstH != ph) { _dst = new byte[pw * ph * 4]; _dstW = pw; _dstH = ph; }
            Buffer.BlockCopy(_dstBack!, 0, _dst, 0, pw * ph * 4);      // 快速发布到前台缓冲
            _pad = effPad; _refWinX = wx; _refWinY = wy; _paddedValid = effPad > 0;
            _dragDx = 0; _dragDy = 0;                                  // 新烘焙已对齐当前位置 → 平移量归零
            _dirty = true;
            _dragFrameVersion++;                                       // 发布新版本号，UI 据此判定"有新结果"
        }
        return true;
    }

    /// <summary>【UI 线程】把结果原位写回位图并就地刷新背景（不触碰内容树）。第一次就绪时置 NeedsApply。</summary>
    public void Commit()
    {
        lock (_lock) CommitLocked();
    }

    // 【须持有 _lock】实际提交：把前台缓冲 _dst 拷进位图、就地换 Background。
    // 与后台工作线程"发布"共用 _lock：拷贝期间工作线程无法改写 _dst → 绝不会读到半新半旧的帧。
    void CommitLocked()
    {
        if (!_dirty || _dst == null || _dstW < 4 || _dstH < 4 || _w < 4 || _h < 4) return;
        _dirty = false;
        _dragCommittedVersion = _dragFrameVersion;   // 与"发布版本号"对齐，避免重复提交同一帧
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
        // 归零/对齐：以"当前窗口位置 - 烘焙基准"重算平移量。新烘焙基准即抓帧时的窗口位置，
        //   故此处偏移≈0；若发布后窗口又移动了几像素，也一并补齐（保持与窗口严格对齐，不跳变）。
        int ox = _dragDx, oy = _dragDy;
        if (_pad > 0 && _hwnd != IntPtr.Zero && FloatScheduleScreenCapture.TryGetWindowOrigin(_hwnd, out int wx, out int wy))
        {
            ox = Math.Clamp(wx - _refWinX, -_pad, _pad);
            oy = Math.Clamp(wy - _refWinY, -_pad, _pad);
        }
        _dragDx = ox; _dragDy = oy;
        // 重建画笔（同一张位图）→ 保证 Background 变更触发重绘，且不需要任何内容树重建。
        // SourceRect = 已折射大图内的卡片子矩形（(pad,pad) 处偏移 → 外扩区不外漏；Border 的 CornerRadius 负责圆角）。
        _brush = new ImageBrush(_bmp)
        {
            Stretch = Stretch.Fill,
            SourceRect = new RelativeRect(_pad + ox, _pad + oy, _w, _h, RelativeUnit.Absolute),
        };
        if (_layer != null) _layer.Background = _brush;   // 就地换背景
        else NeedsApply = true;                            // 还没有挂载的层 → 让宿主重套一次材质
    }

    // ===================== 内部实现 =====================

    /// <summary>
    /// 抓屏幕上 (x,y,w,h) 到 dst：DXGI 优先，不可用/失败时回退 GDI BitBlt。
    /// 返回 true 表示 dst 现在持有该区域的**有效**内容（可能是刚抓到的新帧，也可能是"桌面无变化"时沿用的上一帧）。
    /// 区域跟踪与"dst 是否有内容"由调用方通过 ref 传入 —— 采样路径与拖动重烘焙各有独立缓冲，互不污染。
    /// 须在 _captureLock 内调用。
    /// </summary>
    bool TryCaptureRegionAuto(int x, int y, int w, int h, byte[] dst,
        ref int capX, ref int capY, ref int capW, ref int capH, ref bool dstValid)
    {
        // 目标区域是否发生变化：区域变了就必须重新渲染（哪怕桌面本身没变），
        // 否则"桌面无新帧"时沿用旧缓冲会渲染出**错位**内容。
        bool regionChanged = x != capX || y != capY || w != capW || h != capH;
        if (_captureMode == 0)
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
