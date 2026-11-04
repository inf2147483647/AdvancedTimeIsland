// 拖动"位移帧"与渲染帧的同步器（共享层，纯 System + Avalonia.Controls，主项目与独立子进程同源编译）。
//
// 【要解决的问题】拖动悬浮窗时，"窗口位置"由 Win32 SetWindowPos 即时提交、"玻璃背景"由 Avalonia 渲染管线提交，
//   而 Avalonia 在 Win32 上的渲染节拍（11.x：写死 60fps 的 DefaultRenderTimer；12.x：SleepLoopRenderTimer
//   且频率跟随屏幕刷新率）与显示器合成帧相位自由 —— 位置提交时刻与渲染帧没有任何关系，
//   于是两者会落在不同的 DWM 合成帧，表现为拖动时背景相对卡片 ±1 帧抖动。
//   且 Avalonia 与宿主都没有 present / sync interval 开关可调（宿主 CompositionMode 为 WinUIComposition，
//   该路径没有交换链参数），所以只能从"提交时机"入手。
//
// 【做法】用 TopLevel.RequestAnimationFrame 拿到**渲染帧钩子**：它的回调由 Avalonia 的动画时钟
//   （MediaContext.RenderCore → _clock.Pulse）在**每个渲染帧的渲染之前、UI 线程上同步**执行。
//   因此只要在这个回调里同时提交"窗口位置 → 背景取样矩形 → 采样换图"，它们就会在同一次渲染里一起上屏：
//   拖动位移帧与渲染帧严格一一对应，抖动从"随机相位"变成固定相位（代价是固定的 1 帧延迟）。
//   回调末尾再请求下一帧 → 拖动期间以渲染帧频率持续节拍；停止请求后渲染循环自动回到空闲（不空转、不耗电）。
//
// 【为什么不用阻塞式垂直同步（DwmFlush）】UI 线程上阻塞等待会把拖动与输入卡住，且任一帧工作量越过刷新周期
//   就整帧掉到下一个 vblank（60→30 台阶；本项目 GDI 回退抓帧单次 ~24ms 尤其致命）。
//   本方案不阻塞任何线程、不起后台线程、不跨线程 Post，开销仅为每渲染帧一次廉价回调。
//
// 【调用方契约】
//   · Start/Stop 必须在 **UI 线程**调用（TopLevel.RequestAnimationFrame 内部有线程校验）。
//   · 回调在 UI 线程、渲染之前同步执行；同一帧至多一次，不存在排队堆积。
//   · 回调内只应做"提交位置 / 提交背景"这类廉价操作（它每帧都会触发）。
//   · 窗口销毁等异常会静默停用（IsRunning 变 false），宿主应回退到"立即提交"路径。
using System;
using Avalonia.Controls;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

/// <summary>
/// 拖动位移帧同步器：把"窗口位置 + 玻璃背景"的提交挂到 Avalonia 的渲染帧上
/// （回调在 UI 线程、渲染之前同步执行），使位移与内容落在同一帧。
/// </summary>
public sealed class FloatScheduleRenderFrameSync
{
    readonly TopLevel _topLevel;
    readonly Action _onFrame;
    bool _running;

    /// <summary>创建同步器；<paramref name="onFrame"/> 在 UI 线程、每个渲染帧的渲染之前被调用一次。</summary>
    public FloatScheduleRenderFrameSync(TopLevel topLevel, Action onFrame)
    {
        _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));
        _onFrame = onFrame ?? throw new ArgumentNullException(nameof(onFrame));
    }

    /// <summary>是否正在按渲染帧节拍运行。</summary>
    public bool IsRunning => _running;

    public void Start()
    {
        if (_running) return;
        _running = true;
        RequestNext();
    }

    public void Stop() => _running = false;

    void RequestNext()
    {
        if (!_running) return;
        try { _topLevel.RequestAnimationFrame(OnFrame); }
        catch { _running = false; }   // 窗口已销毁 / 平台不支持 → 静默停用，宿主回退"立即提交"
    }

    #region debug-point F1
    // 临时插桩：拖动节拍取证（会话 vsync-frames）。只写日志，不改业务逻辑；定位后整段移除。
    // 证据目录在仓库外，可用 TRAE_DEBUG_DIR 覆盖。
    public static readonly string DbgPath = System.IO.Path.Combine(
        Environment.GetEnvironmentVariable("TRAE_DEBUG_DIR")
            ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "trae-debug", "glass-drag-jitter"),
        "trae-debug-log-vsync-frames.ndjson");

    public static void Dbg(string ev, string fields)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(DbgPath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(DbgPath,
                "{\"ts\":" + Environment.TickCount64 + ",\"ev\":\"" + ev + "\"," + fields + "}\n");
        }
        catch { }
    }
    #endregion

    long _seq;          // 渲染帧序号（自 Start 起）
    long _lastTickMs;   // 上次渲染帧回调时刻

    // 【渲染帧·UI 线程·渲染之前】先让宿主提交"位置 + 背景"，再请求下一帧，形成以渲染帧为节拍的连续同步。
    void OnFrame(TimeSpan _)
    {
        if (!_running) return;
        #region debug-point F1
        long nowMs = Environment.TickCount64;
        Dbg("frame", "\"seq\":" + (++_seq) + ",\"dt\":" + (_lastTickMs == 0 ? 0 : nowMs - _lastTickMs));
        _lastTickMs = nowMs;
        #endregion
        try { _onFrame(); } catch { /* 提交异常不得影响渲染 */ }
        RequestNext();
    }
}
