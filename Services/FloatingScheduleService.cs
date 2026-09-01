using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

public class FloatingScheduleService : IHostedService, IDisposable
{
    // ===================== Win32 平台特定 API（仅 Windows 下生效，保留长期有益的部分；已重置拖拽方案A → 删除 GetCursorPos/子类化等手动拖拽 API）=====================
#if WINDOWS
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // 注意：GetWindowLong / SetWindowLong 32 位返回值 int，但在 .NET 7+ / net10.0 + Avalonia 双 TFM 上使用 IntPtr 作为第三个参数/返回类型
    //   可同时兼容 32/64 位 且 与 nint (net8.0 net7.0 默认) 隐式互转，避免 CS1503 nint → int 转换错误。
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_TOP_AV = new IntPtr(0);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOZORDER_AV = 0x0004;
    private const uint SWP_NOSENDCHANGING_AV = 0x0400;
    // 【★ 彻底置底】SetWindowPos 完整标志：SWP_NOOWNERZORDER/SWP_NOREPOSITION = 0x0200（同值），
    //  防止递归影响 owner 窗口/重排；对齐 ClassIsland WindowPlatformService.Bottommost 实现。
    private const uint SWP_NOOWNERZORDER_AV = 0x0200;
    private const uint SWP_NOREPOSITION_AV = 0x0200;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    // 用户文档 要点6：WS_EX_COMPOSITED 启用 DWM 双缓冲合成；WS_EX_LAYERED 分层透明窗提示（长期有益，仍保留）
    private const int WS_EX_COMPOSITED_AV = 0x02000000;
    private const int WS_EX_LAYERED_AV    = 0x00080000;
    private const int WS_EX_TRANSPARENT_AV = 0x00000020;   // 点击穿透：Win32 消息投递前系统跳过命中测试，直接透到下一层窗口
    // 【★ 彻底置底】WS_EX_NOACTIVATE = 0x08000000：窗口点击/拖拽不激活（不获得焦点）。
    //  置底窗口一旦被激活，Windows 会强制提升其 z-order（SetWindowPos 压回后仍会再被提升），
    //  即使每 1ms 重设也"压不住"（DispatcherTimer 1ms 实际受系统时钟分辨率 ~15.6ms 限制）。
    //  置底时加该位 → 窗口永不激活 → 永不被提升 → 真正彻底置底。
    private const int WS_EX_NOACTIVATE_AV = 0x08000000;

    // Per-Monitor V2 DPI 兜底（长期有益，保留）
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")]
    private static extern bool SetWindowDpiAwarenessContext(IntPtr hWnd, IntPtr value);
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2_AV = new IntPtr(-4);

    // ========= 用户文档 二.5 方案A（系统原生 HTCAPTION 拖拽）触发接口 =========
    // 经典"ReleaseCapture + SendMessage(WM_NCLBUTTONDOWN, HTCAPTION)"：告诉 Win32 系统现在开始一次 HTCAPTION 标题栏拖拽。
    // 系统原生处理所有坐标、DPI、跨屏、合成时序——最稳定且代码极简。
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    private const uint WM_NCLBUTTONDOWN = 0x00A1;

    // ========== 悬浮窗层级重设频率（Mode=0 OnWindowZOrderChanged）Win32 子类化 ==========
    //  检测 WM_WINDOWPOSCHANGED 中 flags & SWP_NOZORDER==0 → 触发 ApplyWindowLayer。
    //  用 SetWindowLongPtr(GWLP_WNDPROC) 子类化 + 保存委托引用防止 GC 回收。
    private const int GWLP_WNDPROC_AV = -4;
    private const uint WM_WINDOWPOSCHANGED_AV = 0x0047;
    private delegate IntPtr TopmostWndProcAv(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    private static readonly IntPtr HTCAPTION = new IntPtr(2);  // HTTRANSPARENT=-1 / HTCLIENT=1 / HTCAPTION=2
#endif

    private readonly PluginSettings _settings;
    private readonly ILogger<FloatingScheduleService> _logger;

    private Window? _window;
    private Border? _containerBorder;
    private Control? _rootContent;
    // 【修复：进度条有时总是满的（对齐 WPF 同一根因）】
    //  Avalonia ProgressBar 虽然不使用 WPF 的命名部件，但其自带模板的 IsVisible/IsIndeterminate 切换 +
    //   FluentAvalonia 2/3 双兼容的主题重写也偶尔出现 "Indicator Width = ActualWidth 不随 Value 重算" 的问题。
    //  与 WPF 端统一：自绘 Grid 里放 背景 Border(Stretch) + 前景 Border(Left,Width 手动写)。
    private Border? _currentProgressIndicator;       // 当前上课行进度条 —— 前景 Border
    private Layoutable? _currentProgressHost;        // 当前上课行进度条 —— 承载容器（用 ActualWidth 做基准）
    private Border? _currentBreakProgressIndicator;  // 当前课间行进度条 —— 前景
    private Layoutable? _currentBreakProgressHost;   // 当前课间行进度条 —— 承载容器
    private List<(object? ClassInfo, Subject? Subject, object LayoutItem)> _currentClassRows = new();
    private int _currentOnClassIndex = -1;
    private object? _currentBreakLayoutItem;        // 若 Breaking 命中则写入，UpdateProgress 计算进度用
    // 【★ 连续课间分别走进度】课间项列表（TimeType==1，按 Start 升序），RefreshSchedule 构建时从 classPlan 收集。
    //  连续课间 B1→B2→B3（首尾相接）时，课对空隙 = 总长度（进度条会走总长度）。
    //  用本列表按"now ∈ 哪个课间项的 [Start, End)"定位 → 每个课间分别走进度。
    private readonly List<object> _breakItemsAv = new();

    // 【课表分隔线·档案组件】档案中用户手动插入的"分隔线"对象（TimeType==2，StartTime==EndTime 时间点语义），
    //  按 Start 升序；RefreshSchedule 构建时从 classPlan 收集。分隔线只画在档案定义的位置（不是每个课间都画）。
    private readonly List<object> _separatorItemsAv = new();

    // 【课表行分隔线】跨两列的 3px 分隔线控件集合（每个档案分隔线组件对应一条），
    //  深色主题白色 / 浅色主题黑色；订阅 ActualThemeVariantChanged 动态更新颜色，Stop 退订防泄漏。
    private readonly List<Border> _breakSeparatorLinesAv = new();
    private EventHandler? _breakSeparatorThemeHandlerAv;

    private CancellationTokenSource? _retryCts;
    private DispatcherTimer? _timer;                      // 500ms 进度刷新（原保持不变）
    private DispatcherTimer? _hoverFadeTimer;             // 50ms 高频轮询：指针淡化判定
    private bool _allowClose = false;

    // 指针淡化去抖 + last-applied：同 WPF 端策略，50ms × FadeStableThresholdTicks(3) ≈150ms 稳定门槛
    private const int FadeStableThresholdTicksAv = 3;
    private const double FadeTargetOpacityFadedAv = 0.05;
    private const double FadeTargetOpacityNormalAv = 1.0;
    private const int FadeTransitionMsAv = 250;          // 用户要求：0.25s 过渡动画
    private int _fadeAvStableCount;
    private bool _fadeAvLastObserved;
    private bool _lastFadedApplied = false;
    private CancellationTokenSource? _fadeAvCts;   // 取消正在进行的 Opacity 过渡动画（用户切换 faded 方向或关闭淡化时立即落地）
    private object? _lessonsService;
    private object? _profileService;
    private PropertyChangedEventHandler? _settingsChangedHandler;
    private EventHandler? _themeChangedHandler;

    // 拖拽状态（重置方案A → 只保留 SizeToContent 冻结状态 + Windows hwnd 引用；其余全删）
    private SizeToContent _preDragAvSizeMode;

    // ========== 统一指针拖拽（鼠标 / 触摸 / 数位板笔 共用一套） ==========
    //  参考 UWP/WinUI 的"统一 Pointer 事件模型"：不再为每种输入设备分别写逻辑，
    //  也不再走 Win32 HTCAPTION（只支持鼠标）或 Window.BeginMoveDrag（触摸会异步抛
    //  "BeginMoveDrag Failed" 崩溃）。改为：PointerPressed 里 e.Pointer.Capture(容器) 锁定指针，
    //  PointerMoved 里按"指针屏幕像素位移"直接写 Window.Position，PointerReleased / PointerCaptureLost 收尾。
    //  Avalonia 的鼠标/触摸/笔都会触发同一套 PointerXxx 路由事件，故一套代码即可同时支持三种设备。
    //
    //  坐标：Window.Position 与 PointToScreen 均为物理像素；用 PointToScreen(e.GetPosition(_window))
    //  取指针真实屏幕像素位置（该值与窗口自身位置无关，见 PointerMoved 推导），位移 = 当前屏幕位 - 按下屏幕位，
    //  叠加到按下时的窗口位置即得新位置。全程以"按下瞬间"为基准做绝对位移，避免增量累加漂移与 DPI 反馈回环。
    private bool _dragActiveAv;
    private IPointer? _dragPointerAv;
    private PixelPoint _dragStartScreenPxAv;    // 按下瞬间指针的屏幕物理像素位置
    private PixelPoint _dragStartWindowPxAv;    // 按下瞬间窗口位置（物理像素）

    // ========== 贴边自动隐藏（FloatingScheduleEdgeHide） ==========
    //  窗口贴近任一屏幕边缘 <8px → 沿该边滑出屏幕，只保留约 6px 可见条；
    //  光标进入可见条 → 200ms 平移滑回原位；光标离开且仍贴边 → 再滑回隐藏。
    //  仅改 Position，不动 z-order / 扩展样式，与 WS_EX_NOACTIVATE 置底模式和点击穿透共存。
    private const int EdgeHideThresholdAv = 8;      // 判定"贴边"的距离阈值（设备像素）
    private const int EdgeHideVisibleStripAv = 6;   // 隐藏后保留的可见条宽度（设备像素）
    private const int EdgeHideAnimMsAv = 200;       // 滑入/滑出动画时长
    private const int EdgeHideEvalDelayMsAv = 250;  // PositionChanged 防抖：拖拽过程中位置持续变化，停止 250ms 后才评估贴边
    private bool _edgeDockedAv;                     // 已贴边（记录原始位置，等待滑出/已滑出/已滑回 循环中）
    private bool _edgeHiddenAv;                     // 当前处于"滑出隐藏"状态
    private bool _edgeAnimatingAv;                  // 滑入/滑出动画进行中（期间 PositionChanged 不持久化、不重复评估）
    private int _edgeSlideGenAv;                    // 动画代际号：旧动画被取消后其 finally 不误清新动画状态标志
    private string? _edgeSideAv;                    // 贴靠的边："left"/"right"/"top"/"bottom"
    private PixelPoint _edgeDockedPosAv;            // 贴边前的原始位置（滑回目标）
    private PixelPoint _edgeHiddenPosAv;            // 滑出隐藏后的目标位置
    private CancellationTokenSource? _edgeAnimCtsAv;    // 取消正在进行的滑入/滑出动画
    private CancellationTokenSource? _edgeEvalCtsAv;    // 取消防抖中的延迟评估
    // 【贴边隐藏延迟】判定贴边后，等待 FloatingScheduleEdgeHideDelay 秒再滑出隐藏（给用户移开光标的时间）。
    private CancellationTokenSource? _edgeSlideOutDelayCtsAv;   // 取消"延迟滑出隐藏"等待
    private bool _edgeSlideOutPendingAv;                        // 是否已安排一次延迟滑出（防 50ms 轮询重复调度）

    // ========== 课间行 ENTER / EXIT 动画（Avalonia 端，严格与 WPF 端对齐 250ms + TranslateY±24 + CubicEase）==========
    private const int BreakRowAnimationMsAv = 250;
    private const double BreakRowEnterTranslatePxAv = -24.0;
    private const double BreakRowExitTranslatePxAv = 24.0;
    private bool _wasBreakLastTickAv;
    private bool _breakRowExitAnimatingAv;
    private int _breakRowExitPendingStateCode = -1;
    private long _breakRowExitPendingBreakStart = -1;
    private long _breakRowExitPendingBreakEnd = -1;
    private CancellationTokenSource? _breakRowExitCtsAv;
    // 【新增】ENTER 动画专用 CTS：与 EXIT CTS 对称，防止"连续 RefreshSchedule 重建/EXIT 清理/5s 硬清理/DebugTime 清理"
    //        路径下，旧 FireEnter 动画还在异步等待执行（DispatcherPriority.Loaded Post）→ 拿到的控件是"前一次 RefreshSchedule
    //        构建的旧 UI，已被新 RefreshSchedule 替换出 Children，但 AnimateBreakRowEnterAv 起点 Opacity=0 + TT.Y=-24
    //        会先写旧控件本地值 → Avalonia 动画优先级会覆盖同控件后续 Write → 用户看到「课间先正常 0.5-1s 再向上位移 24px 淡入」。
    // 保证：RefreshSchedule #N 开始前，任何 RefreshSchedule #<N 排队的 ENTER CTS 被 Cancel → AnimateBreakRowEnterAv
    //      开头 ct.ThrowIfCancellationRequested 会抛 OperationCanceled → Finish 兜底写 Y=0/Opacity=1，控件不残留 Y=-24。
    private CancellationTokenSource? _breakRowEnterCtsAv;
    private List<Control>? _currentBreakRowVisualsAv;

    // ========== 调试时间即时刷新（响应宿主 SettingsService.DebugTimeOffsetSeconds / TimeOffsetSeconds 变化）==========
    //  兜底：上次 RefreshSchedule 成功构建课表时的 Date。用于 500ms Tick 中检测"日期跳变"（跨天调试）但 PropertyChanged 丢失场景。
    //  初始值 = MinValue：首次启动不触发"dateChanged 强制 Refresh"（否则 sentinel=-1 已经会 Refresh，重复调浪费）。
    private DateTime _lastRefreshDate = DateTime.MinValue;
    // 宿主 SettingsService：反射 IAppHost.GetService<SettingsService>() 结果。Stop/关闭开关必须 Detach，防止插件被 SettingsService（宿主 singleton）强引用滞留内存。
    private object? _hostSettingsServiceAv;
    // 宿主 SettingsService.Settings：INotifyPropertyChanged 源，保存引用以便 Detach 时 -=
    private System.ComponentModel.INotifyPropertyChanged? _hostSettingsObjAv;
    private PropertyChangedEventHandler? _hostSettingsChangedHandlerAv;

    // ========== 悬浮窗层级重设频率 4 模式（Avalonia 端）==========
    //   0 OnWindowZOrderChanged → Win32 子类化 WM_WINDOWPOSCHANGED（非 Windows 退化 Mode 1）
    //   1 OnForegroundWindowChanged → 反射宿主 IWindowPlatformService.ForegroundWindowChanged
    //   2 Every50Ms / 3 Every1Ms → DispatcherTimer
    private DispatcherTimer? _topmostRefreshTimerAv;
    private object? _windowPlatformServiceAv;                // Mode 1：宿主 IWindowPlatformService/IWindowRuleService 实例，Detach 需 -= 事件/Unregister
    private Delegate? _foregroundWindowChangedHandlerAv;     // Mode 1：实际 EventHandler<FWCEA>，保存为 Delegate 以便反射 -=
    // ForegroundWindowChanged 订阅方式：0 = IWindowRuleService.ForegroundWindowChanged event；1 = IWindowPlatformService.Register/Unregister 方法
    private int _fgSubModeAv = 0;
    private System.Reflection.MethodInfo? _fgUnregisterMethodAv;   // 方式 1：UnregisterForegroundWindowChangedEvent 方法，Detach 时调用
    // Mode 0（Win32 子类化）：保存旧 WndProc、子类化 hwnd、以及 WndProc 委托引用（防止 GC 回收导致 CallbackOnCollectedDelegate）
    private IntPtr _topmostOldWndProcAv = IntPtr.Zero;
    private IntPtr _topmostHookedHwndAv = IntPtr.Zero;
    private TopmostWndProcAv? _topmostWndProcDelegateAv;
    // Mode 0/1/2/3 当前激活模式（便于 Detach 时判定走哪路清理，避免误清理）
    private FloatingTopmostRefreshMode _currentTopmostModeAv = (FloatingTopmostRefreshMode)(-1);
    // 【修复 Issue 1】ApplyWindowLayer 重入计数器：防止 Mode 0 WndProc 钩子"自己 Apply→SetWindowPos→WM_WINDOWPOSCHANGED→Post Apply" 无限死循环
    //  >0 表示当前调用栈在 ApplyWindowLayer 内部；Mode 0 WndProc 检测到 >0 时抑制 Post。
    private int _inApplyWindowLayerAv = 0;

    // ========== 5s 全量同步硬兜底（用户方案：每 5s 检查一次当前时间表状态并同步）==========
    //  策略（对齐 Experience 1279696 "双定时器分层"思想）：复用现有 500ms 轻量 Timer，
    //   - 500ms Tick：只更新进度条 ratio + 常规 state/breakTicks/date 变化检测（高频、轻量）
    //   - 每 10 Tick = 5s：额外跑一次"完整 SDK 状态一致性校验"（全量比对 CurrentState + LayoutItem + Date + 高亮行 Start/End）
    //     → 不一致就强制 RefreshSchedule（解决极罕见的"PropertyChanged 丢失 + Date 没变 + state/breakTicks 恰好相同但 UI 错位"的一致性黑洞）
    private const int HardSyncTickIntervalAv = 10;   // 500ms * 10 = 5 秒
    private int _hardSyncTickCounterAv = 0;
    // 【★ 时间跳变 → 进度条立即更新兜底】上次 UpdateProgress Tick 的 now（用于检测跳变 >2s → 立即强制刷新，
    //  覆盖"宿主 Settings.PropertyChanged 事件丢失导致 OnHostSettingsDebugTimeChanged 未触发、进度条只能等 5s 硬刷新"的场景）
    private DateTime _lastTickNowAv = DateTime.MinValue;
    // 宿主 OnHostSettingsDebugTimeChanged 最近一次刷新的 TickCount（UpdateProgress 跳变检测据此跳过，避免与宿主刷新重复重建闪 0）
    private long _lastHostTimeChangeRefreshTicksAv = long.MinValue;
    // 【★ 连续课间即时切换】SDK 课间 item 超时容忍（真实 now > SDK item.End + 此值 → 视为 SDK 滞后，改用真实时间空隙定位）
    private const double EndOverrunToleranceSecAv = 0.05;

    public FloatingScheduleService(PluginSettings settings, ILogger<FloatingScheduleService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    // ===================== SDK API 兼容反射辅助方法 =====================
    private static TimeSpan ReflectGetStartTime(object item)
    {
        if (item == null) return TimeSpan.Zero;
        var t = item.GetType();
        var pi = t.GetProperty("StartTime", BindingFlags.Instance | BindingFlags.Public);
        if (pi != null && pi.PropertyType == typeof(TimeSpan))
        {
            var v = pi.GetValue(item);
            if (v != null) return (TimeSpan)v;
        }
        var pi2 = t.GetProperty("StartSecond", BindingFlags.Instance | BindingFlags.Public);
        if (pi2 != null && int.TryParse(pi2.GetValue(item)?.ToString(), out var sec))
            return TimeSpan.FromSeconds(sec);
        return TimeSpan.Zero;
    }

    private static TimeSpan ReflectGetEndTime(object item)
    {
        if (item == null) return TimeSpan.Zero;
        var t = item.GetType();
        var pi = t.GetProperty("EndTime", BindingFlags.Instance | BindingFlags.Public);
        if (pi != null && pi.PropertyType == typeof(TimeSpan))
        {
            var v = pi.GetValue(item);
            if (v != null) return (TimeSpan)v;
        }
        var pi2 = t.GetProperty("EndSecond", BindingFlags.Instance | BindingFlags.Public);
        if (pi2 != null && int.TryParse(pi2.GetValue(item)?.ToString(), out var sec))
            return TimeSpan.FromSeconds(sec);
        return TimeSpan.Zero;
    }

    // 【★ 连续课间即时切换】用真实时间 now 定位"当前课间空隙"（相邻课对之间，now ∈ [C_i.End, C_{i+1}.Start)）
    //  返回前一课索引 i（在其后插入课间行）；找不到返回 -1。out 返回空隙时间区间。
    private int FindBreakGapIndexByRealTimeAv(TimeSpan now, out TimeSpan gapStart, out TimeSpan gapEnd)
    {
        gapStart = default;
        gapEnd = default;
        for (int i = 0; i < _currentClassRows.Count - 1; i++)
        {
            var cEnd = ReflectGetEndTime(_currentClassRows[i].LayoutItem);
            var cNextStart = ReflectGetStartTime(_currentClassRows[i + 1].LayoutItem);
            // 左闭右开：now ∈ [C_i.End, C_{i+1}.Start) 视为处于这两节课之间的课间
            if (now >= cEnd && now < cNextStart)
            {
                gapStart = cEnd;
                gapEnd = cNextStart;
                return i;
            }
        }
        return -1;
    }

    // 【★ 连续课间分别走进度】用真实时间 now 在 _breakItemsAv 中定位"now ∈ [Start, End)"的课间项。
    //  连续课间 B1→B2→B3（首尾相接）各自独立区间 → 每个课间分别走进度（而非课对空隙总长度）。
    //  返回命中的课间项；找不到返回 null。out 返回该课间项的时间区间。
    private object? FindBreakItemByRealTimeAv(TimeSpan now, out TimeSpan bs, out TimeSpan be)
    {
        bs = default;
        be = default;
        foreach (var b in _breakItemsAv)
        {
            var st = ReflectGetStartTime(b);
            var ed = ReflectGetEndTime(b);
            // 左闭右开：边界点（now == 课间 End）归属下一段，保证 B1→B2 无缝切到 B2
            if (now >= st && now < ed)
            {
                bs = st;
                be = ed;
                return b;
            }
        }
        return null;
    }

    // 【★ 时间跳变/连续上课 高亮定位】用真实时间 now 在 _currentClassRows 中定位"now ∈ [Start, End)"的课行索引；找不到返回 -1
    //  左闭右开：边界点（now == 某课 End）归属下一段，保证 C1.End == C2.Start 的连续课无缝切到 C2。
    //  用途：RefreshSchedule 高亮行主选（含插件/调试时间偏移，反映"插件当前时间应上的课"），
    //        时间跳变（改调试偏移/插件 TimeOffsetSeconds）时 SDK 不反映插件时间 → 必须用真实时间定位，
    //        否则进度条会按 SDK 旧课继承旧进度；SDK 匹配仅作兜底（真实时间定位失败时）。
    private int FindClassIndexByRealTimeAv(TimeSpan now)
    {
        for (int i = 0; i < _currentClassRows.Count; i++)
        {
            var st = ReflectGetStartTime(_currentClassRows[i].LayoutItem);
            var ed = ReflectGetEndTime(_currentClassRows[i].LayoutItem);
            if (now >= st && now < ed) return i;
        }
        return -1;
    }

    // 【★ 悬浮窗时间跟随 ClassIsland（便于调试）】
    //  时间基准直接取宿主 ExactTimeService.GetCurrentLocalDateTime()：
    //    - 包含宿主调试偏移 DebugTimeOffsetSeconds（用户在 ClassIsland 调试页调时间 → 悬浮窗课表/进度条即时反映）
    //    - 包含宿主 TimeOffsetSeconds（时钟页全局偏移）
    //  不再叠加插件 TimeBaseService 的 NTP 同步偏移（_timeOffset）与插件 TimeOffsetSeconds——
    //    NTP 链会使悬浮窗时间与 ClassIsland 调试时间产生偏差，调试不直观。
    private DateTime GetClassIslandNow()
    {
        try
        {
            var tbs = TimeBaseService.Instance;
            if (tbs != null) return tbs.GetClassIslandTime();
        }
        catch { /* ignore */ }
        return DateTime.Now;
    }

    private static int ReflectGetTimeType(object item)
    {
        if (item == null) return -1;
        var pi = item.GetType().GetProperty("TimeType", BindingFlags.Instance | BindingFlags.Public);
        if (pi != null && pi.PropertyType == typeof(int))
            return (int)(pi.GetValue(item) ?? 0);
        return 0;
    }

    private static string ReflectGetBreakNameText(object item)
    {
        if (item == null) return "课间休息";
        try
        {
            var pi = item.GetType().GetProperty("BreakNameText", BindingFlags.Instance | BindingFlags.Public);
            if (pi != null && pi.GetValue(item) is string s && !string.IsNullOrEmpty(s)) return s;
        }
        catch { /* ignore */ }
        try
        {
            var pi = item.GetType().GetProperty("BreakName", BindingFlags.Instance | BindingFlags.Public);
            if (pi != null && pi.GetValue(item) is string s && !string.IsNullOrEmpty(s)) return s;
        }
        catch { /* ignore */ }
        return "课间休息";
    }

    private static Dictionary<Guid, Subject> ReflectBuildSubjectsMap(object? subjectsDict)
    {
        var result = new Dictionary<Guid, Subject>();
        if (subjectsDict == null) return result;
        if (subjectsDict is not IEnumerable enumerable) return result;
        foreach (var kv in enumerable)
        {
            if (kv == null) continue;
            var kvt = kv.GetType();
            var keyP = kvt.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
            var valP = kvt.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            if (keyP == null || valP == null) continue;
            var rawKey = keyP.GetValue(kv);
            var val = valP.GetValue(kv) as Subject;
            if (val == null) continue;
            if (rawKey is Guid g) result[g] = val;
            else if (Guid.TryParse(rawKey?.ToString(), out var parsed)) result[parsed] = val;
        }
        return result;
    }

    private static ArrayList ReflectGetValidTimeLayoutItems(object? classPlan)
    {
        var list = new ArrayList();
        if (classPlan == null) return list;
        var pi = classPlan.GetType().GetProperty("ValidTimeLayoutItems", BindingFlags.Instance | BindingFlags.Public);
        if (pi == null) return list;
        if (pi.GetValue(classPlan) is IEnumerable en)
        {
            foreach (var x in en) list.Add(x);
        }
        return list;
    }

    private static IList ReflectGetClasses(object? classPlan)
    {
        if (classPlan == null) return Array.Empty<object>();
        var pi = classPlan.GetType().GetProperty("Classes", BindingFlags.Instance | BindingFlags.Public);
        if (pi == null) return Array.Empty<object>();
        var v = pi.GetValue(classPlan);
        if (v is IList list) return list;
        if (v is IEnumerable en)
        {
            var al = new ArrayList();
            foreach (var x in en) al.Add(x);
            return al;
        }
        return Array.Empty<object>();
    }

    private static Guid ReflectGetSubjectId(object? classInfo)
    {
        if (classInfo == null) return Guid.Empty;
        var pi = classInfo.GetType().GetProperty("SubjectId", BindingFlags.Instance | BindingFlags.Public);
        if (pi == null) return Guid.Empty;
        var v = pi.GetValue(classInfo);
        if (v is Guid g) return g;
        if (Guid.TryParse(v?.ToString(), out var parsed)) return parsed;
        return Guid.Empty;
    }

    private static bool ReflectGetIsEnabled(object? classInfo)
    {
        if (classInfo == null) return false;
        var pi = classInfo.GetType().GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public);
        if (pi != null && pi.PropertyType == typeof(bool))
            return (bool)(pi.GetValue(classInfo) ?? true);
        return true;
    }

    private static object? ReflectGetCurLayoutItemOfClass(object? classInfo)
    {
        if (classInfo == null) return null;
        var pi = classInfo.GetType().GetProperty("CurrentTimeLayoutItem", BindingFlags.Instance | BindingFlags.Public);
        return pi?.GetValue(classInfo);
    }

    private static object? ReflectProp(object? target, string propName)
    {
        if (target == null) return null;
        return target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target);
    }

    private static string FormatHhMm(TimeSpan ts) => $"{ts.Hours:D2}:{ts.Minutes:D2}";

    // 反射宿主服务类型查找（仿 FontSizeSyncService 模式）
    private static Type? FindHostServiceType(string typeName)
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                if (asm.GetName().Name == "ClassIsland" || asm.GetName().Name?.StartsWith("ClassIsland") == true)
                {
                    var t = asm.GetType(typeName);
                    if (t != null) return t;
                }
            }
            foreach (var asm in assemblies)
            {
                var t = asm.GetType(typeName);
                if (t != null) return t;
            }
        }
        catch { }
        return null;
    }

    private static object? InvokeGeneric(object? instance, string methodName, params object[] args)
    {
        if (instance == null) return null;
        try
        {
            var mi = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (mi == null)
            {
                // 尝试通过参数匹配
                foreach (var m in instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (m.Name != methodName || m.GetParameters().Length != args.Length) continue;
                    try { return m.Invoke(instance, args); } catch { /* continue */ }
                }
                return null;
            }
            return mi.Invoke(instance, args);
        }
        catch { return null; }
    }

    // ===================== IHostedService =====================
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _retryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Issue 3 修复：外层 async lambda 兜底观察所有异常（包括 JIT 级 TypeLoadException）
        _ = Task.Run(async () =>
        {
            try
            {
                await TryStartWithRetryAsync(_retryCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "FloatingScheduleService: 启动流程抛出未预期异常，悬浮时间表不可用");
            }
        }, _retryCts.Token);

        // 监听设置变化
        _settingsChangedHandler = OnSettingsPropertyChanged;
        _settings.PropertyChanged += _settingsChangedHandler;

        // 监听主题变化（Issue 2 修复：使用命名 handler，StopAsync 中可取消订阅）
        _themeChangedHandler = (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                try { RefreshSchedule(); } catch { }
            });
        };
        Avalonia.Application.Current!.ActualThemeVariantChanged += _themeChangedHandler;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _retryCts?.Cancel();
            Stop();
            if (_settingsChangedHandler != null)
                _settings.PropertyChanged -= _settingsChangedHandler;
            if (_themeChangedHandler != null && Avalonia.Application.Current != null)
                Avalonia.Application.Current.ActualThemeVariantChanged -= _themeChangedHandler;
            _themeChangedHandler = null;
        }
        catch { }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { Stop(); }
        catch { }
        GC.SuppressFinalize(this);
    }

    private async Task TryStartWithRetryAsync(CancellationToken ct)
    {
        for (int attempt = 0; attempt < 25; attempt++)
        {
            if (ct.IsCancellationRequested) return;
            if (IAppHost.Host != null)
            {
                try
                {
                    var tLessons = FindHostServiceType("ClassIsland.Services.LessonsService")
                                 ?? FindHostServiceType("ClassIsland.Core.Services.LessonsService");
                    var tProfile = FindHostServiceType("ClassIsland.Services.ProfileService")
                                 ?? FindHostServiceType("ClassIsland.Core.Services.ProfileService");

                    if (tLessons != null && tProfile != null)
                    {
                        var lessonsSvc = IAppHost.Host.Services.GetService(tLessons);
                        var profileSvc = IAppHost.Host.Services.GetService(tProfile);
                        if (lessonsSvc != null && profileSvc != null)
                        {
                            _lessonsService = lessonsSvc;
                            _profileService = profileSvc;
                            _logger.LogInformation("FloatingScheduleService: LessonsService & ProfileService 获取成功");
                            StartInternal();
                            return;
                        }
                    }

                    // 尝试公开接口（兼容未来版本）
                    // Issue 1 修复：使用反射 FindHostServiceType 获取 Type，
                    // 避免直接 typeof() 导致 JIT 级 TypeLoadException（方法级别 JIT 无法被内部 try-catch 保护）
                    try
                    {
                        var sp = IAppHost.Host.Services;
                        var iLessonsType = FindHostServiceType("ClassIsland.Core.Abstractions.Services.ILessonsService");
                        var iProfileType = FindHostServiceType("ClassIsland.Core.Abstractions.Services.IProfileService");
                        if (iLessonsType != null && iProfileType != null)
                        {
                            var lessonsAny = sp.GetService(iLessonsType);
                            var profileAny = sp.GetService(iProfileType);
                            if (lessonsAny != null && profileAny != null)
                            {
                                _lessonsService = lessonsAny;
                                _profileService = profileAny;
                                _logger.LogInformation("FloatingScheduleService: ILessonsService & IProfileService 公开接口获取成功");
                                StartInternal();
                                return;
                            }
                        }
                    }
                    catch { /* ignore */ }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FloatingScheduleService: 解析服务失败，继续重试");
                }
            }
            await Task.Delay(500, ct);
        }
        _logger.LogWarning("FloatingScheduleService: 25次重试仍未能获取宿主服务");
    }

    private void StartInternal()
    {
        // 所有 UI 相关操作必须切到 UI 线程（TryStartWithRetryAsync 运行在后台线程）。
        // 修复：当 EnableFloatingSchedule=true 插件初始化后悬浮窗不显示、直到重新 toggle 开关才出现的问题——
        // 原因为在非 UI 线程直接 new Window/Show，会导致操作静默失败（无可见窗口，未抛 catch 外）或窗口未真正激活显示。
        Dispatcher.UIThread.Post(() =>
        {
            if (_settings == null) return;
            if (_settings.EnableFloatingSchedule)
            {
                EnsureWindow();
                ShowWindow();       // 内部含 ApplyWindowLayer（后置 Topmost/Bottom z-order，必须 Show 后 hwnd 存在才生效）
                RefreshSchedule();  // 填 UI 内容：_containerBorder.Child 赋值；SizeToContent 决定窗口尺寸
                ApplyWindowLayer(); // 再次兜底：RefreshSchedule 会重建 _containerBorder 子树，确保 Win32 z-order 正确
                StartOrStopTimer(); // _window.IsVisible==true 才会启动
                // 【修复：调试时间不立刻刷新 - 即时事件监听】
                //  窗口建立后立刻订阅宿主 SettingsService.Settings.PropertyChanged，
                //  当 DebugTimeOffsetSeconds / TimeOffsetSeconds 变化时立刻 UI 线程 Post Refresh（<=1 UI 帧，远快于 500ms Timer）。
                EnsureHostSettingsSubscriptionAv();
                // 【h4】悬浮窗层级重设频率 Attach：窗口 Show 后 hwnd 已建立，按 Settings.FloatingScheduleTopmostRefreshMode 启动 4 路触发之一
                AttachTopmostRefreshAv(_window!, _settings.FloatingScheduleTopmostRefreshMode);
            }
        });
    }

    // ===================== 窗口管理 =====================
    private void Stop()
    {
        _allowClose = true;
        try { _timer?.Stop(); } catch { }
        _timer = null;
        try { _hoverFadeTimer?.Stop(); } catch { }
        _hoverFadeTimer = null;
        _fadeAvCts?.Cancel();
        _fadeAvCts = null;
        // 【贴边隐藏】Stop 取消滑入/滑出与防抖 CTS，防止异步动画回调访问已销毁窗口
        try { _edgeAnimCtsAv?.Cancel(); } catch { }
        try { _edgeEvalCtsAv?.Cancel(); } catch { }
        // 【h4】悬浮窗层级重设频率 Detach：先解 Win32 子类（hwnd 仍有效）+ 停 Timer + 退订宿主事件，防止宿主 singleton 强引用泄漏
        DetachTopmostRefreshAv();
        // 【修复：调试时间不立刻刷新 - 退订宿主 Settings PropertyChanged 防止内存泄漏】
        //  SettingsService 是宿主 singleton，若插件实例作为 PropertyChanged.target 被它持有，插件/悬浮窗会在关闭开关后无法 GC。
        DetachHostSettingsSubscriptionAv();
        // （重置方案A → 系统原生拖拽在窗口销毁时由系统自动释放，不需要手动清理；此处只做收尾）
        try { _window?.Close(); } catch { }
        _window = null;
        _containerBorder = null;
        _currentProgressIndicator = null;
        _currentProgressHost = null;
        _currentBreakProgressIndicator = null;
        _currentBreakProgressHost = null;
        _currentBreakLayoutItem = null;
        // 【课表行休息分隔线】退订主题事件 + 清空线登记，防止 Application 单例强引用插件导致泄漏
        UnsubscribeBreakSeparatorThemeAv();
        _breakSeparatorLinesAv.Clear();
        // 【5s 硬兜底复位】Stop 时清零计数，下次 EnableFloatingSchedule=true 从零开始同步
        _hardSyncTickCounterAv = 0;
        // 【修复：课间向上位移 0.5-1s】Stop 时 Cancel+Dispose 仍在飞的 ENTER/EXIT 动画 CTS，避免异步动画回调访问旧 UI（Dispose 后可能已 null/detached）
        try { _breakRowEnterCtsAv?.Cancel(); } catch { /* ignore */ }
        try { _breakRowExitCtsAv?.Cancel(); } catch { /* ignore */ }
        try
        {
            var oldEnter = System.Threading.Interlocked.Exchange(ref _breakRowEnterCtsAv, null);
            oldEnter?.Dispose();
        } catch { /* ignore */ }
        try
        {
            var oldExit = System.Threading.Interlocked.Exchange(ref _breakRowExitCtsAv, null);
            oldExit?.Dispose();
        } catch { /* ignore */ }
    }

    // ===================== 宿主 SettingsService 调试时间变更订阅（Ava 端）=====================
    //  目的：当用户在 ClassIsland 设置-调试-调试时间偏移（DebugPage / ClockSettingsPage）修改
    //        DebugTimeOffsetSeconds / TimeOffsetSeconds 时，<=1 UI 帧内立刻 Post RefreshSchedule，
    //        而不是等 500ms Timer 下一 Tick 检测到 stateOrBreakChanged 才触发（用户感知为"时间表没立刻刷新"）。
    //  退订：必须在 Stop() / EnableFloatingSchedule=false 分支 Detach，防止宿主 singleton 强引用插件对象导致内存泄漏。

    /// <summary>
    /// 尝试拿宿主 SettingsService 并订阅 Settings.PropertyChanged。
    /// 拿不到时仅 LogWarning，不会抛——下一 Tick 的 dateChanged 兜底 + sentinel=-1 强制 Refresh 仍可保证功能（延迟<=500ms）。
    /// </summary>
    private void EnsureHostSettingsSubscriptionAv()
    {
        // 已订阅：直接返回，避免重复 += 造成多次回调
        if (_hostSettingsChangedHandlerAv != null) return;
        if (IAppHost.Host == null) return;

        try
        {
            var sp = IAppHost.Host.Services;
            // 与 TryStartWithRetryAsync 保持一致：先用 FindHostServiceType 拿 Type，避免直接 typeof JIT TypeLoadException 无法被 try 保护
            var tSettingsSvc = FindHostServiceType("ClassIsland.Services.SettingsService")
                            ?? FindHostServiceType("ClassIsland.Core.Services.SettingsService");
            object? settingsSvc = null;
            if (tSettingsSvc != null)
                settingsSvc = sp.GetService(tSettingsSvc);

            if (settingsSvc == null)
            {
                // 兜底：部分宿主可能把 SettingsService 注册为实现自身 interface，尝试反射公开接口
                _logger.LogWarning("EnsureHostSettingsSubscriptionAv: 获取 SettingsService 失败，调试时间将依赖 500ms Tick 兜底刷新");
                return;
            }

            // 反射拿 .Settings 属性（类型 = ClassIsland.Models.Settings，实现了 INotifyPropertyChanged）
            var propSettings = settingsSvc.GetType().GetProperty("Settings",
                BindingFlags.Instance | BindingFlags.Public);
            if (propSettings == null)
            {
                _logger.LogWarning("EnsureHostSettingsSubscriptionAv: SettingsService.Settings 属性未找到，调试时间将依赖 500ms Tick 兜底刷新");
                return;
            }
            var settingsObj = propSettings.GetValue(settingsSvc);
            if (settingsObj is not System.ComponentModel.INotifyPropertyChanged npcSettings)
            {
                _logger.LogWarning("EnsureHostSettingsSubscriptionAv: SettingsService.Settings 未实现 INotifyPropertyChanged，调试时间将依赖 500ms Tick 兜底刷新");
                return;
            }

            // 成功：保存引用，构造 handler 并 +=
            _hostSettingsServiceAv = settingsSvc;
            _hostSettingsObjAv = npcSettings;
            _hostSettingsChangedHandlerAv = OnHostSettingsDebugTimeChangedAv;
            npcSettings.PropertyChanged += _hostSettingsChangedHandlerAv;
            _logger.LogInformation("EnsureHostSettingsSubscriptionAv: 已订阅宿主 SettingsService.Settings.PropertyChanged（调试时间即时刷新已启用）");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EnsureHostSettingsSubscriptionAv 异常，调试时间将依赖 500ms Tick 兜底刷新");
            // 失败兜底：把部分已赋值字段清零，避免 Detach 时 -= 空目标或错误目标
            _hostSettingsChangedHandlerAv = null;
            _hostSettingsObjAv = null;
            _hostSettingsServiceAv = null;
        }
    }

    /// <summary>
    /// Detach 宿主 Settings PropertyChanged 订阅（Stop / 关闭 EnableFloatingSchedule 时调用）。
    /// 安全：重复调用或从未 Attach 都不会抛错。
    /// </summary>
    private void DetachHostSettingsSubscriptionAv()
    {
        try
        {
            if (_hostSettingsChangedHandlerAv != null && _hostSettingsObjAv != null)
            {
                _hostSettingsObjAv.PropertyChanged -= _hostSettingsChangedHandlerAv;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DetachHostSettingsSubscriptionAv -= PropertyChanged 异常（忽略）");
        }
        finally
        {
            // 全部清零，保证下次 Ensure 会重新 Attach
            _hostSettingsChangedHandlerAv = null;
            _hostSettingsObjAv = null;
            _hostSettingsServiceAv = null;
        }
    }

    // ===================== 悬浮窗层级重设频率 4 模式 Attach/Detach（Avalonia 端）=====================
    //  生命周期：StartInternal 创建窗口 Show 后 Attach；Stop / EnableFloatingSchedule=false Detach；
    //            FloatingScheduleTopmostRefreshMode PropertyChanged → Detach 旧 + ReAttach 新 + Apply 一次。
    //  设计原则（对齐 Experience 1279696 分层定时器 + 对称释放）：
    //    - 定时器 1ms / 50ms 只做轻量 ApplyWindowLayer（仅 Win32 SetWindowPos 或 _window.Topmost 赋值），不触 UI 重建，保证不卡。
    //    - 所有 Attach 路径必须可被 Detach 对称清理：Timer.Stop + 置空；事件 -=；Win32 解子类。
    //    - 非 Windows 下 Mode=0 退化为 Mode=1（ForegroundWindowChanged）。

    /// <summary>
    /// Mode 0/1 共用：订阅宿主"前台窗口变化"事件（ForegroundWindowChanged）。
    ///  注意：宿主 IWindowPlatformService 的 ForegroundWindowChanged 是 Register/UnregisterForegroundWindowChangedEvent
    ///        **方法**（非 event）；真正的 event 在 IWindowRuleService.ForegroundWindowChanged
    ///        （ClassIsland.Core.Abstractions.Services.IWindowRuleService，宿主 MainWindow 也用它）。
    ///  同时尝试 1.x/2.x 命名空间；失败仅 LogDebug（Mode 2/3 定时器 + ApplyWindowLayer PropertyChanged 仍兜底）。
    /// </summary>
    private void AttachForegroundWindowChangedAv()
    {
        if (IAppHost.Host?.Services == null) { _logger.LogDebug("AttachForegroundWindowChangedAv: IAppHost.Host.Services 为空，跳过"); return; }
        try
        {
            var handlerMethod = new Action<object?, EventArgs?>(OnForegroundWindowChangedForTopmostAv);
            // 首选：IWindowRuleService.ForegroundWindowChanged event（ClassIsland.Core.Abstractions.Services.IWindowRuleService）
            var tRuleSvc = FindHostServiceType("ClassIsland.Core.Abstractions.Services.IWindowRuleService")
                        ?? FindHostServiceType("ClassIsland.Core.Services.IWindowRuleService")
                        ?? FindHostServiceType("ClassIsland.Services.WindowRuleService")
                        ?? FindHostServiceType("ClassIsland.Core.Services.WindowRuleService");
            if (tRuleSvc != null)
            {
                var svc = IAppHost.Host.Services.GetService(tRuleSvc);
                var evt = svc?.GetType().GetEvent("ForegroundWindowChanged", BindingFlags.Instance | BindingFlags.Public);
                if (evt?.EventHandlerType != null)
                {
                    var deleg = Delegate.CreateDelegate(evt.EventHandlerType, this, handlerMethod.Method, throwOnBindFailure: false);
                    if (deleg != null)
                    {
                        evt.AddEventHandler(svc, deleg);
                        _windowPlatformServiceAv = svc;
                        _foregroundWindowChangedHandlerAv = deleg;
                        _fgSubModeAv = 0;
                        _currentTopmostModeAv = FloatingTopmostRefreshMode.OnForegroundWindowChanged;
                        _logger.LogDebug("AttachForegroundWindowChangedAv: 已订阅 IWindowRuleService.ForegroundWindowChanged");
                        return;
                    }
                }
            }
            // 兜底：IWindowPlatformService.Register/UnregisterForegroundWindowChangedEvent 方法（1.x 兼容）
            var tWinPlatform = FindHostServiceType("ClassIsland.Services.IWindowPlatformService")
                            ?? FindHostServiceType("ClassIsland.Core.Services.IWindowPlatformService")
                            ?? FindHostServiceType("ClassIsland.Services.WindowPlatformService")
                            ?? FindHostServiceType("ClassIsland.Core.Services.WindowPlatformService");
            if (tWinPlatform != null)
            {
                var svcP = IAppHost.Host.Services.GetService(tWinPlatform);
                var mReg = svcP?.GetType().GetMethod("RegisterForegroundWindowChangedEvent", BindingFlags.Instance | BindingFlags.Public);
                var mUnreg = svcP?.GetType().GetMethod("UnregisterForegroundWindowChangedEvent", BindingFlags.Instance | BindingFlags.Public);
                if (svcP != null && mReg != null && mUnreg != null && mReg.GetParameters().Length == 1)
                {
                    var paramType = mReg.GetParameters()[0].ParameterType;   // EventHandler<ForegroundWindowChangedEventArgs>
                    var delegP = Delegate.CreateDelegate(paramType, this, handlerMethod.Method, throwOnBindFailure: false);
                    if (delegP != null)
                    {
                        mReg.Invoke(svcP, new object[] { delegP });
                        _windowPlatformServiceAv = svcP;
                        _foregroundWindowChangedHandlerAv = delegP;
                        _fgSubModeAv = 1;
                        _fgUnregisterMethodAv = mUnreg;
                        _currentTopmostModeAv = FloatingTopmostRefreshMode.OnForegroundWindowChanged;
                        _logger.LogDebug("AttachForegroundWindowChangedAv: 已注册 IWindowPlatformService.RegisterForegroundWindowChangedEvent");
                        return;
                    }
                }
            }
            _logger.LogDebug("AttachForegroundWindowChangedAv: ForegroundWindowChanged 订阅失败（event + Register 方法均不可用）");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "AttachForegroundWindowChangedAv 异常（忽略）");
        }
    }

    /// <summary>
    /// Detach 悬浮窗层级重设所有触发源（4 模式清理三件套）。安全：重复调用/从未 Attach 都不会抛。
    /// </summary>
    private void DetachTopmostRefreshAv()
    {
        try
        {
            // ---- 定时器（Mode 2/3）Stop + 置空 ----
            try { _topmostRefreshTimerAv?.Stop(); } catch { /* ignore */ }
            _topmostRefreshTimerAv = null;

            // ---- Mode 1：宿主 ForegroundWindowChanged 退订（区分 event / Register 方法两种订阅方式）----
            if (_windowPlatformServiceAv != null && _foregroundWindowChangedHandlerAv != null)
            {
                try
                {
                    if (_fgSubModeAv == 1 && _fgUnregisterMethodAv != null)
                    {
                        // IWindowPlatformService.UnregisterForegroundWindowChangedEvent(handler)
                        _fgUnregisterMethodAv.Invoke(_windowPlatformServiceAv, new object[] { _foregroundWindowChangedHandlerAv });
                    }
                    else
                    {
                        // IWindowRuleService.ForegroundWindowChanged -= handler
                        var evt = _windowPlatformServiceAv.GetType().GetEvent("ForegroundWindowChanged",
                            BindingFlags.Instance | BindingFlags.Public);
                        evt?.RemoveEventHandler(_windowPlatformServiceAv, _foregroundWindowChangedHandlerAv);
                    }
                }
                catch (Exception ex) { _logger.LogDebug(ex, "DetachTopmostRefreshAv 退订 ForegroundWindowChanged 异常（忽略）"); }
            }
            _windowPlatformServiceAv = null;
            _foregroundWindowChangedHandlerAv = null;
            _fgSubModeAv = 0;
            _fgUnregisterMethodAv = null;

            // ---- Mode 0：Win32 解子类（恢复旧 WndProc）----
#if WINDOWS
            if (_topmostOldWndProcAv != IntPtr.Zero && _topmostHookedHwndAv != IntPtr.Zero)
            {
                try
                {
                    SetWindowLong(_topmostHookedHwndAv, GWLP_WNDPROC_AV, _topmostOldWndProcAv);
                }
                catch (Exception ex) { _logger.LogDebug(ex, "DetachTopmostRefreshAv Win32 解子类异常（忽略）"); }
            }
#endif
            _topmostOldWndProcAv = IntPtr.Zero;
            _topmostHookedHwndAv = IntPtr.Zero;
            _topmostWndProcDelegateAv = null;   // 释放委托引用，允许 GC

            _currentTopmostModeAv = (FloatingTopmostRefreshMode)(-1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DetachTopmostRefreshAv 未知异常（忽略，功能兜底仍由 ApplyWindowLayer PropertyChanged 触发）");
        }
    }

    /// <summary>
    /// 按 mode 选择一路触发源并 Attach。w 必须已 Show（hwnd 存在以便 Win32 子类化）。
    /// </summary>
    private void AttachTopmostRefreshAv(Window w, FloatingTopmostRefreshMode mode)
    {
        if (w == null) return;
        // 确保 clean start（即使调用方忘记先 Detach，我们也先清一遍，防止双模式叠加造成重复 Apply 或泄漏）
        DetachTopmostRefreshAv();

        try
        {
            switch (mode)
            {
                case FloatingTopmostRefreshMode.OnWindowZOrderChanged:
#if WINDOWS
                    {
                        var hwnd = w.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                        if (hwnd != IntPtr.Zero)
                        {
                            // 子类化：拦截 WM_WINDOWPOSCHANGED（捕获"窗口自身 z-order 变化"）
                            _topmostWndProcDelegateAv = TopmostWndProcHookAv;
                            _topmostOldWndProcAv = SetWindowLong(hwnd, GWLP_WNDPROC_AV,
                                System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_topmostWndProcDelegateAv));
                            _topmostHookedHwndAv = hwnd;
                        }
                    }
#endif
                    // 同时订阅 ForegroundWindowChanged（兜底）：
                    //  窗口层级变化（被激活/被 Topmost 顶掉）主要由"前台窗口变化"引起，
                    //  WM_WINDOWPOSCHANGED 只在"自身 z-order 变化"时触发（子类化可能被宿主窗口系统覆盖）。
                    AttachForegroundWindowChangedAv();
                    _currentTopmostModeAv = FloatingTopmostRefreshMode.OnWindowZOrderChanged;
                    _logger.LogDebug("AttachTopmostRefreshAv: Mode=OnWindowZOrderChanged（Win32 子类化 + ForegroundWindowChanged 兜底）");
                    break;

                case FloatingTopmostRefreshMode.OnForegroundWindowChanged:
                    // 宿主 IWindowRuleService.ForegroundWindowChanged（修复：IWindowPlatformService 的 ForegroundWindowChanged 是方法非 event）
                    AttachForegroundWindowChangedAv();
                    break;

                case FloatingTopmostRefreshMode.Every50Ms:
                case FloatingTopmostRefreshMode.Every1Ms:
                    {
                        var intervalMs = mode == FloatingTopmostRefreshMode.Every50Ms ? 50 : 1;
                        _topmostRefreshTimerAv = new DispatcherTimer(DispatcherPriority.Background)
                        {
                            Interval = TimeSpan.FromMilliseconds(intervalMs)
                        };
                        _topmostRefreshTimerAv.Tick += (_, _) => ApplyWindowLayer();
                        _topmostRefreshTimerAv.Start();
                        _currentTopmostModeAv = mode;
                        _logger.LogDebug("AttachTopmostRefreshAv: Mode=Every{0}Ms 已启动 DispatcherTimer", intervalMs);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AttachTopmostRefreshAv mode={0} 异常（功能降级：仅保留 FloatingScheduleWindowLayer PropertyChanged 即时 Apply）", mode);
            // 失败兜底：完全 Detach 所有已半截 Attach 的资源，避免泄漏或重复触发
            DetachTopmostRefreshAv();
        }
    }

    // ---- Mode 0 Win32 子类化 WndProc 回调 ----
#if WINDOWS
    private IntPtr TopmostWndProcHookAv(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // 防御：任何异常都不吞 CallWindowProc，必须保证旧 WndProc 被调用，否则窗口会失去所有消息泵（卡死/无响应）
        try
        {
            if (msg == WM_WINDOWPOSCHANGED_AV && lParam != IntPtr.Zero)
            {
                // WINDOWPOS.flags 位于结构第 6 个字段：偏移 sizeof(HWND)*2 + sizeof(int)*4 = IntPtr.Size*2 + 16
                //   实际上 (HWND hwnd=IntPtr, HWND hwndInsertAfter=IntPtr, int x, int y, int cx, int cy, UINT flags)
                //   所以 flags 偏移 = IntPtr.Size * 2 + sizeof(int) * 4 = IntPtr.Size*2 + 16
                int flagsOffset = IntPtr.Size * 2 + 16;
                uint flags = (uint)System.Runtime.InteropServices.Marshal.ReadInt32(lParam, flagsOffset);
                // flags & SWP_NOZORDER == 0 表示这次 WINDOWPOS 消息包含 z-order 变化，需要重设 Topmost/Bottom
                // 【修复 Issue 1】若 _inApplyWindowLayerAv > 0：本 WM_WINDOWPOSCHANGED 是 ApplyWindowLayer→SetWindowPos 同步发出的（自己改 z-order），抑制 Post 防止死循环
                //                  == 0：本消息来自外部系统/其他窗口改 z-order，正常重设
                if ((flags & SWP_NOZORDER_AV) == 0
                    && System.Threading.Volatile.Read(ref _inApplyWindowLayerAv) == 0)
                {
                    Dispatcher.UIThread.Post(ApplyWindowLayer, DispatcherPriority.Background);
                }
            }
        }
        catch { /* 防御：任何异常忽略，保证流程进入 CallWindowProc */ }

        // 转发给旧 WndProc
        if (_topmostOldWndProcAv != IntPtr.Zero)
            return CallWindowProc(_topmostOldWndProcAv, hWnd, msg, wParam, lParam);
        // 兜底（理论上不会到，因为 _topmostOldWndProcAv=Zero 表示未子类化/已解子类）
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
#endif

    // ---- Mode 1 ForegroundWindowChanged 回调 ----
    private void OnForegroundWindowChangedForTopmostAv(object? sender, EventArgs? e)
    {
        Dispatcher.UIThread.Post(ApplyWindowLayer, DispatcherPriority.Background);
    }

    /// <summary>
    /// 宿主 Settings.Settings 属性变化回调：
    ///  当变化属性名属于"影响 ExactTimeService 返回值"的集合
    ///   → 立刻 UI 线程 Post：失效本地缓存 sentinel → RefreshSchedule → 同步 Apply 一次 ratio。
    /// </summary>
    private void OnHostSettingsDebugTimeChangedAv(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;
        // 覆盖所有"会改变 GetClassIslandNow() 结果"的设置项：
        //   - DebugTimeOffsetSeconds：调试页加减偏移（不保存，用户调时间表立刻看效果的核心）
        //   - TimeOffsetSeconds：时钟页全局时间偏移
        //   - DebugTimeSpeed：调试页时间倍速（倍速变化会导致进度条百分比跳变，需要重建进度条 Width）
        //   - ExactTimeServer / IsExactTimeEnabled：启用/禁用 NTP 或切换 NTP 服务器，基准时间跳变
        bool isTimeRelated =
            e.PropertyName == "DebugTimeOffsetSeconds" ||
            e.PropertyName == "TimeOffsetSeconds" ||
            e.PropertyName == "DebugTimeSpeed" ||
            e.PropertyName == "ExactTimeServer" ||
            e.PropertyName == "IsExactTimeEnabled";
        if (!isTimeRelated) return;

        // Post 到 UI 线程，优先级 = Loaded：等待宿主 DebugPage 的 DebugTimeOffsetSeconds 赋值 + PropertyChanged 同步走完，
        // 这样 GetClassIslandNow() 能拿到最新偏移；同时比用户"再操作下一步"更早执行（下一帧可见）。
        Dispatcher.UIThread.Post(() =>
        {
            if (_window == null || _containerBorder == null) return;
            try
            {
                // 【修复 #1 (Code Review)：调试时间变更 Post 前取消正在进行的 EXIT 动画，防止 EXIT onCompleted 用旧 pending 快照脏覆盖新 Refresh 的正确快照】
                //  场景：课间→上课 EXIT 250ms 窗口中途，用户突然调 DebugTimeOffsetSeconds。
                //  若不取消 EXIT：EXIT onCompleted 会执行 `_lastRefreshStateCode = _breakRowExitPendingStateCode`（旧值），
                //  覆盖本方法刚写入的"新调试时间"正确 state 快照 → 紧接着 EXIT onCompleted 又 RefreshSchedule 校正回来，
                //  用户视角 = UI 闪烁两次 + 冗余整表重建 2 次。
                //  处理：Cancel EXIT CTS（使 AnimateBreakRowExitAv 的 WhenAny(token) 立刻取消 → 旧 onCompleted 永远不会被触发），
                //        并清所有 EXIT 动画标志 + 旧 visuals/indicator 引用。
                // 【修复：课间向上位移 0.5-1s】同时 Cancel ENTER CTS：在"时间跳变"场景，若前一 RefreshSchedule #N-1
                //   还在排队的 FireEnter（DispatcherPriority.Loaded 异步）没执行完，会拿旧 UI 的控件引用写 TT.Y=-24 / Opacity=0，
                //   而本方法紧接着 RefreshSchedule #N 会新建一套 children → 旧 children 从 visual tree 移除但动画的
                //   FillMode.Forward 仍会保留本地值覆盖 → 用户"先正常 → 0.5-1s 向上位移"。这里 Cancel ENTER CTS 彻底封死。
                try { _breakRowExitCtsAv?.Cancel(); } catch { /* ignore */ }
                try { _breakRowEnterCtsAv?.Cancel(); } catch { /* ignore */ }
                // （Dispose 旧 CTS：防止 Token 引用滞留旧动画对象）
                try
                {
                    if (_breakRowExitCtsAv != null)
                    {
                        var oldCts = _breakRowExitCtsAv;
                        _breakRowExitCtsAv = null;
                        oldCts.Dispose();
                    }
                }
                catch { /* ignore */ }
                try
                {
                    if (_breakRowEnterCtsAv != null)
                    {
                        var oldEnter = _breakRowEnterCtsAv;
                        _breakRowEnterCtsAv = null;
                        oldEnter.Dispose();
                    }
                }
                catch { /* ignore */ }
                _breakRowExitAnimatingAv = false;
                _wasBreakLastTickAv = false;
                _breakRowExitPendingStateCode = -1;
                _breakRowExitPendingBreakStart = -1;
                _breakRowExitPendingBreakEnd = -1;
                _currentBreakRowVisualsAv = null;
                _currentBreakProgressIndicator = null;
                _currentBreakProgressHost = null;

                // ① 失效缓存 sentinel：
                //   - _lastRefreshStateCode = -1：RefreshSchedule 内部 isColdStartAv=true，跳过任何课间 ENTER 动画（直接显示最终态，避免 250ms Opacity=0 起点造成"时间表瞬间消失"误判）。
                //   - _lastRefreshBreakStartTicks / _lastRefreshBreakEndTicks = -1：保证下一次 UpdateProgress 检测 stateOrBreakChanged 即使 PropertyChanged 丢了事件也会命中。
                //   - _lastRefreshDate = MinValue：sentinel Date 也失效，Date 兜底检测不会再误触发。
                _lastRefreshStateCode = -1;
                _lastRefreshBreakStartTicks = -1;
                _lastRefreshBreakEndTicks = -1;
                _lastRefreshDate = DateTime.MinValue;

                // ② 同步 RefreshSchedule：重建整表（Date 锚用 GetClassIslandNow().Date = 最新调试偏移后的今天）
                RefreshSchedule();
                // 记录宿主刷新时间戳：UpdateProgress 时间跳变检测据此跳过（1s 内不重复重建，避免闪 0）
                _lastHostTimeChangeRefreshTicksAv = Environment.TickCount64;

                // ③ 同步 Apply 一次"当前真实 ratio"（needRefresh 分支的镜像逻辑）：
                //   原因：RefreshSchedule 构建的新进度条 indicator Width = 构造默认 0；若不立刻写 ratio，
                //         用户视角"进度条一瞬间为空"，下一帧（500ms 后）UpdateProgress 才补上。
                try
                {
                    var svc = _lessonsService;
                    if (svc == null) return;
                    var sRaw = ReflectProp(svc, "CurrentState");
                    TimeState s = sRaw != null ? (TimeState)sRaw : TimeState.None;
                    bool onC = s == TimeState.OnClass;
                    bool brk = s == TimeState.Breaking;

                    // -- 课间进度条（真实时间定位当前课间空隙，SDK 滞后时也能正确推进/重置）--
                    if (brk)
                    {
                        var nn = GetClassIslandNow().TimeOfDay;
                        var biB = FindBreakItemByRealTimeAv(nn, out var gsB, out var geB);
                        if (biB != null && _currentBreakProgressIndicator != null)
                        {
                            double tb = (geB - gsB).TotalSeconds;
                            if (tb > 0)
                            {
                                double rb = Math.Clamp((nn - gsB).TotalSeconds / tb, 0.0, 1.0);
                                ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, rb);
                            }
                            else ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                        }
                        else if (_currentBreakProgressIndicator != null)
                            ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                    }
                    else if (_currentBreakProgressIndicator != null)
                        ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);

                    // -- 当前课进度条（用 RefreshSchedule 刚定位的高亮行区间 + 真实时间，不依赖 SDK 滞后 item）--
                    if (onC)
                    {
                        if (_currentOnClassIndex >= 0 && _currentOnClassIndex < _currentClassRows.Count &&
                            _currentProgressIndicator != null)
                        {
                            var rowLi = _currentClassRows[_currentOnClassIndex].LayoutItem;
                            var st = ReflectGetStartTime(rowLi);
                            var ed = ReflectGetEndTime(rowLi);
                            double t = (ed - st).TotalSeconds;
                            if (t <= 0) { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); }
                            else
                            {
                                var nn = GetClassIslandNow().TimeOfDay;
                                double p = Math.Clamp((nn - st).TotalSeconds / t, 0.0, 1.0);
                                ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, p);
                            }
                        }
                        else ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                    }
                    else ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                }
                catch { /* 忽略：下一帧 UpdateProgress 会兜底继续写 ratio */ }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OnHostSettingsDebugTimeChangedAv Post RefreshSchedule 异常（忽略）");
            }
        }, DispatcherPriority.Loaded);
    }

    private void EnsureWindow()
    {
        if (_window != null) return;
        _window = new Window
        {
            Title = "AdvancedTimeIsland FloatingSchedule",
            ExtendClientAreaToDecorationsHint = true,
            CanResize = false,
            ShowInTaskbar = false,
            ShowActivated = false,
            Background = Brushes.Transparent,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None },
            SizeToContent = SizeToContent.WidthAndHeight,
            Opacity = 1.0, // 整窗保持不透明；背景不透明度仅作用于卡片背景刷 Alpha（文字/进度条不被淡化）
            Position = new PixelPoint(_settings.FloatingSchedulePositionX, _settings.FloatingSchedulePositionY),
            MinWidth = 150,
            // ===== 修复：超长教师名把窗口撑成 600-800 px 横条（用户最新要求：教师列必须完整展示 15 汉字）=====
            // 规格层 MaxWidth = 820 px 兜底（820 ≈ 外框 26 + ColSpacing 14 + 课师Margin 16 + 课程列 180 + 教师 322.5 + 时间 141 + 浮点余量 20.5）
            // 分层（Experience 100019838）：外层只挡住窗口野蛮生长，单一事实宽度来源=内层教师列 pt×系数。
            //   - FontScale=32 极端 → teacher=29 pt × 21.5 = 623.5 px；820 - 26-14-16-623.5-141 = -0.5（课程列仍由外层 Window.MaxWidth 裁剪至 Star 列 ≥ 0，不反向撑出窗口）
            //   - FontScale=默认 18 → teacher=15×21.5=322.5 px；820 - 26-14-16-322.5-141 = 300.5 px 课程列（14+ 字中文，充足）
            MaxWidth = 820
        };
        // 跨版本 Avalonia 兼容：SystemDecorations 枚举在不同 SDK 内嵌的 Avalonia 中
        // 命名空间/类型名可能变化，用反射尝试设置 None 值隐藏系统标题栏
        try
        {
            var sysDecProp = typeof(Window).GetProperty("SystemDecorations",
                BindingFlags.Public | BindingFlags.Instance);
            if (sysDecProp != null && sysDecProp.PropertyType.IsEnum)
            {
                var noneVal = Enum.Parse(sysDecProp.PropertyType, "None", true);
                sysDecProp.SetValue(_window, noneVal);
            }
        }
        catch { }
        _window.Opened += (_, _) =>
        {
            ApplyWindowLayer();
#if WINDOWS
            try { HideFromAltTabWin32(_window); } catch { }
#endif
            // 初次打开时强制应用点击穿透 + 指针淡化（Host 可能在打开前就保存了配置）
            ApplyClickThrough();
            ApplyHoverFade(force: true);
            // 【贴边隐藏】初次打开若保存的位置已贴边，防抖 250ms（等 SizeToContent 完成布局）后评估滑出
            ScheduleEdgeEvalAv();
        };
        _window.Closing += (_, e) =>
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                _window.Hide();
            }
        };
        _window.PositionChanged += (_, e) =>
        {
            // 方案A 系统原生 HTCAPTION 拖拽：不再手动维护 _isDragging（已删除）；
            // 任何位置变化（含最大化/最小化/系统拖拽）都直接持久化到设置，简化逻辑。
            // 【贴边隐藏】滑入/滑出动画过程中与隐藏态下不持久化，避免把"隐藏位"当成用户位置保存。
            if (_window?.IsVisible == true && !_edgeAnimatingAv && !_edgeHiddenAv)
            {
                _settings.FloatingSchedulePositionX = e.Point.X;
                _settings.FloatingSchedulePositionY = e.Point.Y;
            }
            // 【贴边隐藏】非拖拽中（防抖 250ms 后）评估是否贴边 → 滑出隐藏
            ScheduleEdgeEvalAv();
        };

        _containerBorder = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0),
            Background = ThemeHelper.GetCardBackgroundBrush(),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            BorderThickness = new Thickness(1)
        };

        // 拖拽（统一指针事件模型：鼠标 / 触摸 / 数位板笔 共用一套手动拖拽，见 ContainerBorder_PointerPressed 注释）
        _containerBorder.PointerPressed += ContainerBorder_PointerPressed;
        _containerBorder.PointerReleased += ContainerBorder_PointerReleased;
        _containerBorder.PointerMoved += ContainerBorder_PointerMoved;
        _containerBorder.PointerCaptureLost += ContainerBorder_PointerCaptureLost;

        _window.Content = _containerBorder;
    }

    private void ShowWindow()
    {
        if (_window == null) EnsureWindow();
        if (_window == null) return;
        if (!_window.IsVisible)
        {
            try { _window.Show(); } catch { }
        }
        ApplyWindowLayer();
    }

    private void HideWindow()
    {
        try { _window?.Hide(); } catch { }
    }

    private void ApplyWindowLayer()
    {
        if (_window == null) return;
        // 【修复 Issue 1】重入计数器 +1（嵌套/异常安全：即使中途 return/throw 也 finally -1）
        //   Mode 0 WM_WINDOWPOSCHANGED 钩子检测本字段 >0 时抑制 Post，防止"自己 Apply→SetWindowPos→WM_WINDOWPOSCHANGED→Post Apply"无限循环
        System.Threading.Interlocked.Increment(ref _inApplyWindowLayerAv);
        try
        {
            var layer = _settings.FloatingScheduleWindowLayer;
#if WINDOWS
            try
            {
                var hwnd = _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (hwnd != IntPtr.Zero)
                {
                    if (layer == FloatingScheduleWindowLayer.Topmost)
                    {
                        // 置顶：清除 WS_EX_NOACTIVATE（允许交互激活），SetWindowPos 提到最前
                        //  【对齐 ClassIsland】完整 SWP 标志（SWP_NOSENDCHANGING/SWP_NOOWNERZORDER/SWP_NOREPOSITION）
                        //  防止递归 WM_WINDOWPOSCHANGING 与 owner 窗口被连带重排。
                        // 【修复闪烁】每 50ms/1ms Tick 都会走到这里；若目标扩展样式与当前值相同则跳过 SetWindowLong，
                        //  避免反复写入相同 GWL_EXSTYLE 强制 DWM 重评估 WS_EX_LAYERED/COMPOSITED 合成分层造成视觉闪烁。
                        try
                        {
                            var exT = (int)(long)GetWindowLong(hwnd, GWL_EXSTYLE);
                            var targetT = exT & ~WS_EX_NOACTIVATE_AV;
                            if (targetT != exT)
                                SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)targetT);
                        }
                        catch { }
                        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE |
                            SWP_NOSENDCHANGING_AV | SWP_NOOWNERZORDER_AV | SWP_NOREPOSITION_AV);
                    }
                    else
                    {
                        // 【★ 彻底置底】
                        //  1) 加 WS_EX_NOACTIVATE：置底窗口点击/拖拽不激活 → 永不被 Windows 提升 z-order
                        //     （激活提升是置底失效的主因：即使每 1ms 重设，激活提升发生在两次重设之间且优先级更高）
                        //  2) 完整 SWP 标志（对齐 ClassIsland Bottommost）：SWP_NOSENDCHANGING/SWP_NOOWNERZORDER/
                        //     SWP_NOREPOSITION 防止递归 WM_WINDOWPOSCHANGING 与 owner 窗口被连带重排。
                        // 【修复闪烁】同上：值未变化时不写 GWL_EXSTYLE，防止 DWM 反复重评估合成分层导致闪烁
                        try
                        {
                            var exB = (int)(long)GetWindowLong(hwnd, GWL_EXSTYLE);
                            var targetB = exB | WS_EX_NOACTIVATE_AV;
                            if (targetB != exB)
                                SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)targetB);
                        }
                        catch { }
                        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE |
                            SWP_NOSENDCHANGING_AV | SWP_NOOWNERZORDER_AV | SWP_NOREPOSITION_AV);
                    }
                    return;
                }
            }
            catch { }
#endif
            // 非 Windows 或 hwnd 获取失败：只支持 Topmost 属性
            try { _window.Topmost = layer == FloatingScheduleWindowLayer.Topmost; } catch { }
        }
        finally
        {
            System.Threading.Interlocked.Decrement(ref _inApplyWindowLayerAv);
        }
    }

#if WINDOWS
    private static void HideFromAltTabWin32(Window w)
    {
        var hwnd = w.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;

        // 用户文档 要点2：Per-Monitor V2 DPI 兜底（与 WPF 端 OnWindowLoaded 对齐）
        try { SetWindowDpiAwarenessContext(hwnd, DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2_AV); } catch { /* ignore */ }
        try { SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2_AV); } catch { /* ignore */ }

        // 用户文档 要点6：Alt+Tab 隐藏 TOOLWINDOW；同时加 WS_EX_COMPOSITED+WS_EX_LAYERED 减少 DWM 合成抖动闪烁
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        // IntPtr → int 时先转 long 防 32/64 位不一致（Avalonia GetWindowLong 返回签名是 IntPtr，与 WPF 端同）
        int style = (int)(long)exStyle;
        style |= WS_EX_TOOLWINDOW | WS_EX_COMPOSITED_AV | WS_EX_LAYERED_AV;
        SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)style);
    }
#endif

    // ===================== 拖拽（统一指针事件模型：鼠标 / 触摸 / 数位板笔 共用一套）=====================
    //  为什么放弃 Win32 HTCAPTION（方案A）与 Window.BeginMoveDrag：
    //   1) HTCAPTION（ReleaseCapture + SendMessage WM_NCLBUTTONDOWN/HTCAPTION）只走"鼠标"消息链路，
    //      触摸（WM_POINTER/WM_TOUCH）与数位板笔无法进入该模态拖拽循环 → 触摸屏拖不动；
    //   2) Window.BeginMoveDrag 在 Avalonia Win32 后端内部 Post 到 Dispatcher 异步执行"捕获鼠标"，
    //      触摸指针无法完成捕获 → 异步抛 InvalidOperationException("BeginMoveDrag Failed")，try/catch 捕不到
    //      → 冒泡成宿主 ClassIsland Critical 崩溃（2026/9/1 9:48:59 日志）。
    //  统一模型（参考 UWP/WinUI PointerPressed/Moved/Released + Capture）：
    //   Avalonia 的鼠标、触摸、笔都触发同一套 PointerXxx 路由事件，故一套手动拖拽即可同时支持三种设备：
    //   Down 时 Capture 指针并记录"按下瞬间指针屏幕像素位 + 窗口像素位"，Move 时按指针屏幕位移写 Window.Position，
    //   Up / CaptureLost 收尾。坐标全程用物理像素（PointToScreen 与 Window.Position 同单位），DPI/跨屏天然正确。

    private void ContainerBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_window == null) return;
        // 点击穿透模式下，完全禁止任何本地交互（也包含拖拽），保证用户点击一定会穿透到下方窗口。
        if (_settings.FloatingScheduleClickThrough) return;

        var point = e.GetCurrentPoint(_containerBorder);
        // 仅响应：鼠标左键按下 / 触摸 / 笔（数位板）。右键、中键不拖拽。
        bool isMouseLeft = e.Pointer.Type == PointerType.Mouse && point.Properties.IsLeftButtonPressed;
        bool isTouchOrPen = e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen;
        if (!isMouseLeft && !isTouchOrPen) return;

        // 已有拖拽进行中（另一指针正在拖）→ 忽略多指/多设备的第二触点，避免争抢位置。
        if (_dragActiveAv) return;

        try
        {
            // 冻结 SizeToContent.Auto：否则拖动过程中若 Auto 测量触发，会让透明悬浮窗每移动一次重测量 → 桌面合成器抖动
            _preDragAvSizeMode = _window.SizeToContent;
            if (_preDragAvSizeMode != SizeToContent.Manual) _window.SizeToContent = SizeToContent.Manual;

            _dragActiveAv = true;
            _dragPointerAv = e.Pointer;
            // 按下瞬间：指针真实屏幕像素位置 + 窗口像素位置（作为绝对位移基准）
            _dragStartScreenPxAv = _window.PointToScreen(e.GetPosition(_window));
            _dragStartWindowPxAv = _window.Position;

            try { e.Pointer.Capture(_containerBorder); }
            catch (Exception ex) { _logger.LogDebug(ex, "统一拖拽 Capture 失败，将依赖事件冒泡继续。"); }

            try { e.Handled = true; } catch { }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "统一拖拽启动失败，安全忽略。");
            EndDragAv();   // 回滚状态（含恢复 SizeToContent）
        }
        // 注意：不在这里恢复 SizeToContent —— 拖拽在 PointerPressed 返回后仍要继续，
        //   结束统一由 PointerReleased / PointerCaptureLost → EndDragAv 处理。
    }

    // 【统一拖拽】PointerMoved：指针真实屏幕像素位移叠加到按下时的窗口位置。
    //   PointToScreen(e.GetPosition(_window)) 恒等于"指针当前真实屏幕像素位"（与窗口自身位置无关），
    //   故位移 = 当前 - 按下，纯指针移动量；新窗口位 = 按下窗口位 + 位移。以按下瞬间为绝对基准，避免增量漂移/DPI 回环。
    private void ContainerBorder_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragActiveAv || _window == null) return;
        if (_dragPointerAv != null && !ReferenceEquals(e.Pointer, _dragPointerAv)) return;

        try
        {
            var curScreenPx = _window.PointToScreen(e.GetPosition(_window));
            _window.Position = new PixelPoint(
                _dragStartWindowPxAv.X + (curScreenPx.X - _dragStartScreenPxAv.X),
                _dragStartWindowPxAv.Y + (curScreenPx.Y - _dragStartScreenPxAv.Y));
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "统一拖拽更新窗口位置失败，结束本次拖拽。");
            EndDragAv();
        }
    }

    // 【统一拖拽】指针失去捕获（如系统弹窗抢走）→ 立即收尾，防止状态残留。
    private void ContainerBorder_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_dragActiveAv) return;
        if (_dragPointerAv != null && !ReferenceEquals(e.Pointer, _dragPointerAv)) return;
        EndDragAv();
    }

    // 【统一拖拽】收尾：释放标记 + 恢复 SizeToContent + 补 z-order + 保存位置 + 贴边评估。
    //   幂等：_dragActiveAv=false 时直接返回（Released 与 CaptureLost 可能先后触发）。
    private void EndDragAv()
    {
        if (!_dragActiveAv) return;
        _dragActiveAv = false;
        try { _dragPointerAv?.Capture(null); } catch { }
        _dragPointerAv = null;

        if (_window == null) return;
        try
        {
            if (_window.SizeToContent != _preDragAvSizeMode) _window.SizeToContent = _preDragAvSizeMode;
            try { ApplyWindowLayer(); } catch (Exception ex) { _logger.LogDebug(ex, "拖拽结束 ApplyWindowLayer 出错，忽略。"); }
            _settings.FloatingSchedulePositionX = _window.Position.X;
            _settings.FloatingSchedulePositionY = _window.Position.Y;
            ScheduleEdgeEvalAv();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "拖拽结束恢复 SizeToContent/SavePosition 出错，忽略。");
        }
    }

    private void ContainerBorder_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_window == null) return;

        // 【统一拖拽】本次拖拽由该指针发起 → 结束并收尾（EndDragAv 内恢复 SizeToContent + 补 z-order + 存位置 + 贴边评估）。
        //   非拖拽发起指针的 Released（如点击穿透外的杂散释放）→ 无状态需处理，直接忽略。
        if (_dragActiveAv && (_dragPointerAv == null || ReferenceEquals(e.Pointer, _dragPointerAv)))
        {
            EndDragAv();
            try { e.Handled = true; } catch { }
        }
    }

    // ===================== 设置变更 =====================
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;
        switch (e.PropertyName)
        {
            case nameof(PluginSettings.EnableFloatingSchedule):
                if (_settings.EnableFloatingSchedule)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        EnsureWindow();
                        ShowWindow();
                        RefreshSchedule();
                        ApplyWindowLayer();
                        ApplyClickThrough();      // 窗口打开时强制应用点击穿透状态
                        ApplyHoverFade(force: true); // 并同步指针淡化状态
                        StartOrStopTimer();
                        // 【修复：调试时间不立刻刷新】开关打开时立刻订阅宿主 Settings.PropertyChanged
                        EnsureHostSettingsSubscriptionAv();
                        // 【h4】开关打开时 Attach 悬浮窗层级重设触发源（4 模式，按 Settings.FloatingScheduleTopmostRefreshMode）
                        AttachTopmostRefreshAv(_window!, _settings.FloatingScheduleTopmostRefreshMode);
                    });
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        // 【h4】开关关闭先 Detach Topmost 刷新（解 Win32 子类 / 停 Timer / - = ForegroundWindowChanged）
                        DetachTopmostRefreshAv();
                        // 【修复：调试时间不立刻刷新 - 防内存泄漏】开关关闭时先 Detach 宿主 Settings 订阅（SettingsService 是宿主 singleton）
                        DetachHostSettingsSubscriptionAv();
                        HideWindow();
                        StartOrStopTimer();
                    });
                }
                break;
            case nameof(PluginSettings.TimeOffsetSeconds):
                // 【★ 时间跳变：插件全局时间偏移变化 → 立即 RefreshSchedule 重建】
                //  TimeBaseService.GetCurrentTime() 每次读最新偏移，但 FloatingScheduleService 不知道偏移变了；
                //  若不刷新，UpdateProgress 会用"旧高亮课 + 新时间"算进度条（继承旧进度/卡 100%）。
                //  刷新后高亮行真实时间定位切到目标课，RefreshSchedule 末尾集中 Apply 立即写新进度。
                Dispatcher.UIThread.Post(RefreshSchedule);
                break;
            case nameof(PluginSettings.FloatingScheduleWindowLayer):
                Dispatcher.UIThread.Post(ApplyWindowLayer);
                break;
            case nameof(PluginSettings.FloatingScheduleTopmostRefreshMode):
                // 模式切换：先 Detach 旧模式全部触发源 → ReAttach 新模式（窗口存在时） → 再 Apply 一次立即生效
                Dispatcher.UIThread.Post(() =>
                {
                    DetachTopmostRefreshAv();
                    if (_window != null && _settings.EnableFloatingSchedule)
                    {
                        AttachTopmostRefreshAv(_window, _settings.FloatingScheduleTopmostRefreshMode);
                    }
                    ApplyWindowLayer();
                });
                break;
            case nameof(PluginSettings.FloatingScheduleOpacity):
                // 背景不透明度作用在卡片背景刷 Alpha，需要重建 UI 刷新颜色；
                // 同时"指针淡化"会叠加到容器 Opacity，这里也强制重算一次。
                Dispatcher.UIThread.Post(() =>
                {
                    RefreshSchedule();
                    ApplyHoverFade(force: true);
                });
                break;
            case nameof(PluginSettings.FloatingScheduleFontScale):
            case nameof(PluginSettings.FloatingScheduleEnableFullTeacherName):
                Dispatcher.UIThread.Post(RefreshSchedule);
                break;
            case nameof(PluginSettings.FloatingScheduleClickThrough):
                Dispatcher.UIThread.Post(ApplyClickThrough);
                break;
            case nameof(PluginSettings.FloatingScheduleHoverFade):
            case nameof(PluginSettings.FloatingScheduleHoverFadeReverse):
                Dispatcher.UIThread.Post(() => ApplyHoverFade(force: true));
                break;
            case nameof(PluginSettings.FloatingScheduleEdgeHide):
                // 【贴边隐藏】开启：立即调度一次评估（当前若已贴边则滑出）；关闭：恢复贴边前位置并清空全部状态
                Dispatcher.UIThread.Post(() =>
                {
                    if (_settings.FloatingScheduleEdgeHide)
                        EvaluateEdgeDockAv();
                    else
                        DisableEdgeHideAv();
                });
                break;
        }
    }

    // ==================================== 点击穿透（参考 ClassIsland 窗口管理 + Win32 WS_EX_TRANSPARENT）====================================
    //   - Windows：直接改 HWND GWL_EXSTYLE 的 WS_EX_TRANSPARENT 位（命中测试系统级透明，直接穿透到下层窗口）
    //   - 非 Windows（Linux/macOS X11/Wayland）：Avalonia 跨平台兜底——把根容器 InputElement.IsHitTestVisible=false
    //   - 两种模式都会在关闭时恢复，防止 AltTab 隐藏/拖拽链路残留样式
    private bool _lastAppliedClickThrough = false;
    private void ApplyClickThrough()
    {
        if (_window == null) return;
        bool through = _settings.FloatingScheduleClickThrough;
        if (_lastAppliedClickThrough == through) return;
        _lastAppliedClickThrough = through;

#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var hwnd = _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (hwnd != IntPtr.Zero)
                {
                    // Avalonia P/Invoke 签名现在用 IntPtr 返回/第三个参数以兼容 net8.0/net10.0 的 nint
                    //  强转 (int) 安全：GWL_EXSTYLE 返回 32 位 DWORD 位集，高 32 位为 0。
                    int style = (int)(long)GetWindowLong(hwnd, GWL_EXSTYLE);
                    if (through) style |= (WS_EX_TRANSPARENT_AV | WS_EX_LAYERED_AV);
                    else          style &= ~WS_EX_TRANSPARENT_AV;
                    // WS_EX_LAYERED 保留（Alt+Tab 隐藏时已经设置过），减少 DWM 合成抖动
                    SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)style);
                }
            }
            catch (Exception ex) { _logger.LogDebug(ex, "切换悬浮窗 WS_EX_TRANSPARENT 失败，安全忽略。"); }
            return;
        }
#endif
        // 非 Windows：跨平台兜底
        try
        {
            if (_containerBorder != null)
                _containerBorder.IsHitTestVisible = !through;
        }
        catch { /* ignore */ }
    }

    // ==================================== 贴边自动隐藏（FloatingScheduleEdgeHide）====================================
    //  触发评估：拖拽结束（PointerReleased）与 PositionChanged（250ms 防抖，等效"非拖拽中"）。
    //  行为：窗口与任一屏幕工作区边缘距离 <8px → 记录原始贴边位置，沿该边滑出（200ms 平移动画），
    //        只保留约 6px 可见条；50ms _hoverFadeTimer Tick 轮询光标：进入可见条 → 滑回原位；离开 → 再滑回隐藏。
    //  约束：只改 Window.Position，不碰 z-order / GWL_EXSTYLE（与 WS_EX_NOACTIVATE 置底、点击穿透共存）。
    private int _edgeHoverTicksAv;                  // 光标在/离开可见条的连续稳定 Tick 计数（去抖）
    private bool _edgeHoverLastInAv;                // 上一次 Tick 光标是否在可见条/窗口内

    /// <summary>
    /// 取窗口在屏幕上的设备像素尺寸（宽,高）。
    /// Window.Position / Screen.WorkingArea 都是设备像素，而 FrameSize/Bounds 是 DIP，
    /// 非 100% 缩放下直接混用会错位；用容器 PointToScreen（返回设备像素）换算得到真实设备像素尺寸。
    /// 悬浮窗无边框、content 填满窗口，故 container 的屏幕矩形 ≈ 窗口矩形。
    /// </summary>
    private bool TryGetWindowDeviceSizeAv(out int w, out int h)
    {
        w = h = 0;
        try
        {
            if (_window == null || _containerBorder == null) return false;
            var tl = _containerBorder.PointToScreen(new Point(0, 0));
            var br = _containerBorder.PointToScreen(
                new Point(_containerBorder.Bounds.Width, _containerBorder.Bounds.Height));
            w = br.X - tl.X;
            h = br.Y - tl.Y;
            return w > 0 && h > 0;
        }
        catch { return false; }
    }

    /// <summary>PositionChanged / PointerReleased 后防抖调度贴边评估（拖拽过程中位置持续变化，停止 250ms 后才评估）。</summary>
    private void ScheduleEdgeEvalAv()
    {
        if (!_settings.FloatingScheduleEdgeHide) return;
        if (_edgeAnimatingAv) return;   // 滑入/滑出动画自身触发的 PositionChanged 不再评估
        // 注意：只 Cancel 旧 CTS 不 Dispose —— 旧动画 Task.Delay(ct) 可能仍在 await，
        //  Cancel 后立刻 Dispose 会让其抛 ObjectDisposedException 而非 OperationCanceledException。无链接注册的 CTS 交给 GC。
        try { _edgeEvalCtsAv?.Cancel(); } catch { /* ignore */ }
        _edgeEvalCtsAv = new CancellationTokenSource();
        _ = EdgeEvalDelayedAv(_edgeEvalCtsAv.Token);
    }

    private async Task EdgeEvalDelayedAv(CancellationToken ct)
    {
        try
        {
            await Task.Delay(EdgeHideEvalDelayMsAv, ct);
            if (!ct.IsCancellationRequested) EvaluateEdgeDockAv();
        }
        catch (OperationCanceledException) { /* 新一次拖拽/移动接管评估，正常取消 */ }
        catch (Exception ex) { _logger.LogDebug(ex, "贴边隐藏延迟评估异常，忽略。"); }
    }

    /// <summary>评估当前窗口是否贴边：贴边 → 记录原始位置并滑出隐藏；不贴边 → 解除贴边状态。</summary>
    private void EvaluateEdgeDockAv()
    {
        if (_window == null || !_window.IsVisible) return;
        if (!_settings.FloatingScheduleEdgeHide) return;
        if (_edgeAnimatingAv) return;
        try
        {
            // 取窗口所在屏幕（对齐 ClassIsland MainWindow 用法：TopLevel.Screens 实例属性 + ScreenFromWindow）
            var screen = _window.Screens?.ScreenFromWindow(_window);
            if (screen == null) return;
            var wa = screen.WorkingArea;
            if (!TryGetWindowDeviceSizeAv(out int w, out int h)) return;
            var pos = _window.Position;
            // 已处于隐藏态且位置就是隐藏目标 → 无需处理
            if (_edgeHiddenAv && pos == _edgeHiddenPosAv) return;

            // 与四边的距离（可为负=已越过边缘）；取 <阈值 中最小的边
            int dLeft = pos.X - wa.X;
            int dRight = (wa.X + wa.Width) - (pos.X + w);
            int dTop = pos.Y - wa.Y;
            int dBottom = (wa.Y + wa.Height) - (pos.Y + h);
            string? side = null;
            int best = EdgeHideThresholdAv;
            if (dLeft < best) { best = dLeft; side = "left"; }
            if (dRight < best) { best = dRight; side = "right"; }
            if (dTop < best) { best = dTop; side = "top"; }
            if (dBottom < best) { best = dBottom; side = "bottom"; }

            if (side == null)
            {
                // 不贴边 → 解除贴边状态（窗口已在用户放置位置，无需移动）
                if (_edgeDockedAv)
                {
                    _edgeDockedAv = false;
                    _edgeHiddenAv = false;
                    _edgeSideAv = null;
                    _edgeHoverTicksAv = 0;
                    _edgeHoverLastInAv = false;
                }
                // 【贴边隐藏延迟】不再贴边 → 取消挂起的延迟滑出
                try { _edgeSlideOutDelayCtsAv?.Cancel(); } catch { }
                _edgeSlideOutPendingAv = false;
                return;
            }

            // 贴边：当前位置若不是隐藏目标位（= 用户拖到的原始贴边位置）→ 记录为滑回目标
            if (!_edgeDockedAv || pos != _edgeHiddenPosAv)
            {
                _edgeDockedPosAv = pos;
                _edgeSideAv = side;
                _edgeDockedAv = true;
                _edgeHoverTicksAv = 0;
                _edgeHoverLastInAv = true;   // 刚拖完视为光标在窗口附近，避免立即回滑
            }

            // 计算滑出后的隐藏位置：沿贴靠边移出，只保留 EdgeHideVisibleStripAv 像素可见条
            int hx = _edgeDockedPosAv.X, hy = _edgeDockedPosAv.Y;
            switch (side)
            {
                case "left": hx = wa.X - w + EdgeHideVisibleStripAv; break;
                case "right": hx = wa.X + wa.Width - EdgeHideVisibleStripAv; break;
                case "top": hy = wa.Y - h + EdgeHideVisibleStripAv; break;
                case "bottom": hy = wa.Y + wa.Height - EdgeHideVisibleStripAv; break;
            }
            _edgeHiddenPosAv = new PixelPoint(hx, hy);
            // 【贴边隐藏延迟】刚判定贴边不立即滑出，等待用户设置的延迟秒数（默认 3s）后再滑出；
            //  等待期间若光标进入可见条 / 用户拖走 / 关闭开关，延迟任务会被取消或重新校验后放弃。
            if (!_edgeHiddenAv && !_edgeSlideOutPendingAv)
                ScheduleEdgeSlideOutAv();
        }
        catch (Exception ex) { _logger.LogDebug(ex, "贴边隐藏评估异常，忽略。"); }
    }

    /// <summary>发起一次滑入/滑出平移动画（200ms，CubicEaseOut 风格插值；不阻塞 UI 线程）。</summary>
    private void EdgeSlideToAv(PixelPoint target, bool willBeHidden)
    {
        if (_window == null) return;
        try { _edgeAnimCtsAv?.Cancel(); } catch { }
        try { _edgeAnimCtsAv?.Dispose(); } catch { }
        _edgeAnimCtsAv = new CancellationTokenSource();
        _edgeAnimatingAv = true;
        int myGen = ++_edgeSlideGenAv;
        var from = _window.Position;
        if (from == target)
        {
            // 已在目标位：直接落状态，不起动画
            _edgeAnimatingAv = false;
            _edgeHiddenAv = willBeHidden;
            return;
        }
        _ = RunEdgeSlideAsync(_window, from, target, myGen, willBeHidden, _edgeAnimCtsAv.Token);
    }

    private async Task RunEdgeSlideAsync(Window w, PixelPoint from, PixelPoint to, int myGen, bool willBeHidden, CancellationToken ct)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                double t = sw.ElapsedMilliseconds / (double)EdgeHideAnimMsAv;
                if (t >= 1.0) { w.Position = to; break; }
                double eased = 1 - Math.Pow(1 - t, 3);   // CubicEaseOut
                w.Position = new PixelPoint(
                    (int)Math.Round(from.X + (to.X - from.X) * eased),
                    (int)Math.Round(from.Y + (to.Y - from.Y) * eased));
                // await 续体经 Avalonia DispatcherSynchronizationContext 回到 UI 线程写 Position，无需 Post
                await Task.Delay(16, ct);
            }
            _edgeHiddenAv = willBeHidden;
            _edgeHoverTicksAv = 0;
            _edgeHoverLastInAv = !willBeHidden;  // 滑出后先按"光标不在条内"起算，滑回后按"在窗口内"起算
        }
        catch (OperationCanceledException)
        {
            // 方向切换：新动画从当前中间位置接管，正常取消
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "贴边滑入/滑出动画异常，直接落到目标位置。");
            try { w.Position = to; _edgeHiddenAv = willBeHidden; } catch { }
        }
        finally
        {
            // 只有"仍是最新一次动画"才清标志（被新动画取消的旧动画不得覆盖新动画的 animating 状态）
            if (myGen == _edgeSlideGenAv) _edgeAnimatingAv = false;
        }
    }

    /// <summary>光标是否在当前窗口"屏幕内可见部分"（隐藏态=6px 可见条）内。仅 Windows 有全局光标查询。</summary>
    private bool IsPointerInEdgeStripAv()
    {
        if (_window == null || !_window.IsVisible) return false;
#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (!GetCursorPos(out var pt)) return false;
                var screen = _window.Screens?.ScreenFromWindow(_window);
                if (screen == null) return false;
                var wa = screen.WorkingArea;
                if (!TryGetWindowDeviceSizeAv(out int w, out int h)) return false;
                var pos = _window.Position;
                // 窗口矩形与工作区交集 = 用户实际可见部分（隐藏态下即约 6px 可见条）
                int x1 = Math.Max(pos.X, wa.X), y1 = Math.Max(pos.Y, wa.Y);
                int x2 = Math.Min(pos.X + w, wa.X + wa.Width), y2 = Math.Min(pos.Y + h, wa.Y + wa.Height);
                if (x2 <= x1 || y2 <= y1) return false;
                return pt.X >= x1 && pt.X < x2 && pt.Y >= y1 && pt.Y < y2;
            }
            catch { return false; }
        }
#endif
        // 非 Windows：无全局光标轮询兜底（与 IsPointerInWindowAv 同策略），贴边隐藏退化为"只隐藏不滑回"
        return false;
    }

    /// <summary>50ms 轮询（挂在 _hoverFadeTimer Tick 上）：光标进入可见条 → 滑回原位；离开且仍贴边 → 再滑回隐藏。</summary>
    private void UpdateEdgeHoverAv()
    {
        if (!_settings.FloatingScheduleEdgeHide) return;
        if (_window == null || !_window.IsVisible) return;
        if (!_edgeDockedAv || _edgeAnimatingAv) return;
        try
        {
            bool inStrip = IsPointerInEdgeStripAv();
            if (inStrip == _edgeHoverLastInAv) _edgeHoverTicksAv++;
            else { _edgeHoverLastInAv = inStrip; _edgeHoverTicksAv = 1; }
            if (_edgeHoverTicksAv < 2) return;   // ~100ms 去抖，防光标贴边抖动来回触发

            if (_edgeHiddenAv && inStrip)
            {
                // 滑回原位；同时取消任何挂起的延迟滑出（光标已回来）
                try { _edgeSlideOutDelayCtsAv?.Cancel(); } catch { }
                _edgeSlideOutPendingAv = false;
                EdgeSlideToAv(_edgeDockedPosAv, willBeHidden: false);   // 滑回原位
            }
            else if (!_edgeHiddenAv && !inStrip && !_edgeSlideOutPendingAv)
            {
                // 【贴边隐藏延迟】光标离开可见条 → 延迟用户设置的秒数后再滑回隐藏（而非立即）
                ScheduleEdgeSlideOutAv();
            }
            else if (!_edgeHiddenAv && inStrip && _edgeSlideOutPendingAv)
            {
                // 光标又回到可见条 → 取消挂起的延迟滑出
                try { _edgeSlideOutDelayCtsAv?.Cancel(); } catch { }
                _edgeSlideOutPendingAv = false;
            }
        }
        catch (Exception ex) { _logger.LogDebug(ex, "贴边隐藏光标轮询异常，忽略。"); }
    }

    /// <summary>读取"贴边隐藏延迟时间"（秒，0~60，精确 0.1）并换算为毫秒；异常/未设时回退默认 3 秒。</summary>
    private int GetEdgeHideDelayMsAv()
    {
        try
        {
            var sec = Math.Clamp(_settings.FloatingScheduleEdgeHideDelay, 0.0, 60.0);
            return (int)Math.Round(sec * 1000.0);
        }
        catch { return 3000; }
    }

    /// <summary>
    /// 安排一次"延迟滑出隐藏"：等待 GetEdgeHideDelayMsAv() 毫秒后，若仍贴边、未隐藏、光标不在可见条内、
    /// 且此间未被新的滑回请求取消，则执行滑出。重复调用会取消上一次未完成的等待（以最后一次为准）。
    /// </summary>
    private void ScheduleEdgeSlideOutAv()
    {
        if (!_settings.FloatingScheduleEdgeHide) return;
        try { _edgeSlideOutDelayCtsAv?.Cancel(); } catch { }
        _edgeSlideOutDelayCtsAv = new CancellationTokenSource();
        _edgeSlideOutPendingAv = true;
        _ = EdgeSlideOutDelayedAv(_edgeSlideOutDelayCtsAv.Token);
    }

    private async Task EdgeSlideOutDelayedAv(CancellationToken ct)
    {
        int delayMs = GetEdgeHideDelayMsAv();
        try
        {
            if (delayMs > 0) await Task.Delay(delayMs, ct);
            else ct.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) { return; }   // 被取消（滑回/新一次调度/关闭）——不再滑出
        catch (Exception ex) { _logger.LogDebug(ex, "贴边隐藏延迟等待异常，忽略。"); return; }

        _edgeSlideOutPendingAv = false;
        // 延迟到点后重新校验状态：仍贴边、未隐藏、未在动画中、光标不在可见条内 → 才滑出
        if (!_settings.FloatingScheduleEdgeHide) return;
        if (_window == null || !_window.IsVisible) return;
        if (!_edgeDockedAv || _edgeHiddenAv || _edgeAnimatingAv) return;
        if (IsPointerInEdgeStripAv()) return;   // 光标已回到可见条 → 取消滑出（改由 hover 轮询滑回）
        EdgeSlideToAv(_edgeHiddenPosAv, willBeHidden: true);
    }

    /// <summary>关闭贴边隐藏：取消动画与防抖、恢复贴边前位置并持久化、清空全部状态。</summary>
    private void DisableEdgeHideAv()
    {
        try { _edgeEvalCtsAv?.Cancel(); } catch { }
        try { _edgeAnimCtsAv?.Cancel(); } catch { }
        try { _edgeSlideOutDelayCtsAv?.Cancel(); } catch { }   // 【贴边隐藏延迟】取消未完成的延迟滑出
        _edgeSlideOutPendingAv = false;
        try
        {
            if (_edgeDockedAv && _window != null && _window.IsVisible)
            {
                // 恢复窗口到贴边前位置（动画中/隐藏态时位置不是用户位置）
                if (_edgeHiddenAv || _edgeAnimatingAv)
                    _window.Position = _edgeDockedPosAv;
                _settings.FloatingSchedulePositionX = _edgeDockedPosAv.X;
                _settings.FloatingSchedulePositionY = _edgeDockedPosAv.Y;
            }
        }
        catch (Exception ex) { _logger.LogDebug(ex, "关闭贴边隐藏恢复位置异常，忽略。"); }
        _edgeDockedAv = false;
        _edgeHiddenAv = false;
        _edgeAnimatingAv = false;
        _edgeSideAv = null;
        _edgeHoverTicksAv = 0;
        _edgeHoverLastInAv = false;
    }

    // ==================================== 指针移入淡化（参考 ClassIsland MainWindow.UpdateFadeStatus/GetMouseStatusByPos）====================================
    //  淡化规则与 ClassIsland 完全一致：
    //     IsFaded = 启用淡化  &&  (IsPointerIn  ^  IsMouseInFadingReversed)
    //  淡化时整窗 Opacity 降到 0.05（与 ClassIsland 0.05 同）；否则=1（文字/进度条不淡化，背景Alpha仍由用户设置作用于卡片背景刷）
    //
    //  ===== 本次三项修复（与 WPF 端完全对称）=====
    //   ①. 判定延迟 & 不准确：单独 50ms Timer + 连续 3 次(≈150ms)稳定去抖（消除 DWM 合成 ±1px 抖动），比原 500ms 共用 Tick 快 ~3x。
    //   ②. 切换 250ms 过渡动画：AnimateOpacityAv() 用 Avalonia.Animation.Animation (无 XAML 无 FA 主题依赖，FA2/FA3 全兼容)。
    //   ③. force=true 或 主开关关闭：立即取消动画 + Opacity 落最终值（避免"关淡化还在慢慢恢复"反直觉）。
    private void ApplyHoverFade(bool force = false)
    {
        if (_window == null) return;
        try
        {
            bool observed;
            if (!_settings.FloatingScheduleHoverFade)
            {
                observed = false;
            }
            else
            {
                bool inWindow = IsPointerInWindowAv();
                bool reversed = _settings.FloatingScheduleHoverFadeReverse;
                observed = (inWindow ^ reversed);
            }

            bool shouldApply = true;
            bool faded = observed;
            bool masterDisabled = !_settings.FloatingScheduleHoverFade;

            if (!force && !masterDisabled)
            {
                // 去抖稳定计数（与 WPF 完全一致：达到阈值才切换）
                if (observed == _fadeAvLastObserved)
                {
                    _fadeAvStableCount = Math.Min(_fadeAvStableCount + 1, FadeStableThresholdTicksAv + 1);
                }
                else
                {
                    _fadeAvLastObserved = observed;
                    _fadeAvStableCount = 1;
                }
                if (_fadeAvStableCount < FadeStableThresholdTicksAv)
                    shouldApply = false;
            }
            else
            {
                _fadeAvLastObserved = observed;
                _fadeAvStableCount = FadeStableThresholdTicksAv;
            }

            bool targetChanged = (faded != _lastFadedApplied);

            // 无变化 且 非强制 非主开关切换 → 跳过
            if (!force && !masterDisabled && (!shouldApply || !targetChanged))
                return;

            double targetValue = faded ? FadeTargetOpacityFadedAv : FadeTargetOpacityNormalAv;

            // 主开关关 / force → 立即取消动画 + 立刻落到最终值
            if (masterDisabled || force)
            {
                _fadeAvCts?.Cancel();
                _fadeAvCts?.Dispose();
                _fadeAvCts = null;
                _window.Opacity = targetValue;
                _lastFadedApplied = faded;
                return;
            }

            // 正常过渡路径：250ms 过渡动画
            _lastFadedApplied = faded;
            AnimateOpacityAv(_window, targetValue, TimeSpan.FromMilliseconds(FadeTransitionMsAv));
        }
        catch
        {
            // ignore
        }
    }

    // Avalonia Opacity 0.25s 过渡动画：纯代码 Avalonia.Animation (无 Transitions 属性依赖，FA2/FA3 100% 兼容)
    //   —— 每次新的动画发起前先 Cancel 旧（_fadeAvCts），保证"切方向后立刻改终点，不走老路"。
    private void AnimateOpacityAv(Visual target, double toOpacity, TimeSpan duration)
    {
        if (target == null) return;
        try
        {
            // 先取消旧动画：保证新动画起点用当前 Opacity（防止两动画对同一 dp 叠加）
            _fadeAvCts?.Cancel();
            _fadeAvCts?.Dispose();
            _fadeAvCts = new CancellationTokenSource();

            double fromOpacity = target.Opacity;
            if (Math.Abs(fromOpacity - toOpacity) < 1e-6)
            {
                target.Opacity = toOpacity;
                return;
            }

            var animation = new Avalonia.Animation.Animation
            {
                Duration = duration,
                FillMode = FillMode.Forward,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        KeyTime = TimeSpan.FromMilliseconds(0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = fromOpacity } }
                    },
                    new KeyFrame
                    {
                        KeyTime = duration,
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = toOpacity } }
                    }
                }
            };

            // Fire & Forget：await 不会阻塞；_fadeAvCts 被 Cancel/Dispose → RunAsync 抛 OCE → catch 吞
            _ = RunOpacityAnimationAsync(animation, target, toOpacity, _fadeAvCts.Token);
        }
        catch
        {
            // 动画系统异常兜底：直接跳到目标值（体验降级但可见性一致）
            try { target.Opacity = toOpacity; } catch { /* ignore */ }
        }
    }

    private static async Task RunOpacityAnimationAsync(
        Avalonia.Animation.Animation anim,
        Visual target,
        double finalOpacity,
        CancellationToken ct)
    {
        try
        {
            await anim.RunAsync(target, ct);
            // 动画结束后把最终值直接写进本地属性，避免下一帧"动画值 < 本地值"优先级差异导致回跳
            if (!ct.IsCancellationRequested)
                target.Opacity = finalOpacity;
        }
        catch (OperationCanceledException)
        {
            // 方向切换/关闭淡化：正常取消，不做任何事（新动画会从当前 Opacity 接上）
        }
        catch
        {
            try { target.Opacity = finalOpacity; } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// 参考 ClassIsland MainWindowLine.GetMouseStatusByPos：屏幕坐标下判断系统鼠标指针是否在悬浮窗渲染矩形内（考虑容器实际尺寸、DPI）。
    /// 由于悬浮窗本身可能处于点击穿透、收不到 PointerEntered/Exited 事件，因此只能每 Tick 拉取一次全局光标位置。
    /// </summary>
    private bool IsPointerInWindowAv()
    {
        try
        {
            if (_window == null || !_window.IsVisible) return false;
            if (_containerBorder == null) return false;
#if WINDOWS
            if (OperatingSystem.IsWindows() && _window != null)
            {
                if (!GetCursorPos(out var pt)) return false;
                // 用容器边框本身在屏幕上的矩形做判断（比 Window.ClientRect 更精确，Avalonia 装饰/阴影可能在外层）
                if (!_containerBorder.IsEffectivelyVisible) return false;
                var topLeft = _containerBorder.PointToScreen(new Point(0, 0));
                var bottomRight = _containerBorder.PointToScreen(
                    new Point(_containerBorder.Bounds.Width, _containerBorder.Bounds.Height));
                return (topLeft.X <= pt.X && pt.X < bottomRight.X &&
                        topLeft.Y <= pt.Y && pt.Y < bottomRight.Y);
            }
#endif
            // 非 Windows（不使用 MouseDevice 兜底：因 FA2/FA3 双版本 + Avalonia 11 多 TFM Mouse API 差异过大编译不稳定）
            //  ClassIsland 运行平台 = Windows，此处返回 false 等价于"指针永远在窗外"，淡化反向开关在窗外才会生效，行为可接受。
            return false;
        }
        catch
        {
            return false;
        }
    }

#if WINDOWS
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }
#endif

    private void StartOrStopTimer()
    {
        var shouldRun = _settings.EnableFloatingSchedule && _window?.IsVisible == true;
        if (shouldRun)
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _timer.Tick += (_, _) => UpdateProgress();
            }
            if (!_timer.IsEnabled) _timer.Start();

            // ========== 指针淡化高频轮询：50ms 独立 Timer，解决原 500ms 进度刷新共用导致的延迟 ==========
            if (_hoverFadeTimer == null)
            {
                _hoverFadeTimer = new DispatcherTimer(DispatcherPriority.Normal)
                {
                    Interval = TimeSpan.FromMilliseconds(50)
                };
                _hoverFadeTimer.Tick += (_, _) =>
                {
                    ApplyHoverFade();
                    UpdateEdgeHoverAv();   // 【贴边隐藏】复用 50ms 轮询：光标进入可见条滑回 / 离开滑回隐藏
                };
            }
            if (!_hoverFadeTimer.IsEnabled) _hoverFadeTimer.Start();
        }
        else
        {
            if (_timer != null && _timer.IsEnabled) _timer.Stop();
            if (_hoverFadeTimer != null && _hoverFadeTimer.IsEnabled) _hoverFadeTimer.Stop();
        }
    }

    // ===================== 强调色 =====================
    private static Color GetAccentColor()
    {
        try
        {
            var app = Application.Current;
            if (app != null)
            {
                var variant = app.ActualThemeVariant ?? ThemeVariant.Light;
                // 优先从 Styles 中查找资源
                if (app.Styles.TryGetResource("SystemAccentColor", variant, out var r1) && r1 is Color c1) return c1;
                if (app.Styles.TryGetResource("AccentFillColorDefaultBrush", variant, out var r2) && r2 is ISolidColorBrush scb1) return scb1.Color;
                if (app.TryFindResource("SystemAccentColor", out var r3) && r3 is Color c2) return c2;
                if (app.TryFindResource("AccentFillColorDefaultBrush", out var r4) && r4 is ISolidColorBrush scb2) return scb2.Color;
            }
        }
        catch { }
        return Color.Parse("#0078D4");
    }

    private static ISolidColorBrush GetAccentBrush() => new SolidColorBrush(GetAccentColor());

    // 【修复：进度条有时总是满的】统一自绘：Grid(高度/占位/裁剪) → 底层背景 Border(Stretch) + 顶层前景 Border(Left, Width 手动写)。
    //  每 Tick 显式写入前景.Width = host.ActualWidth * ratio，100% 可控，不依赖 ProgressBar 自带模板。
    private static (Grid host, Border indicator) CreateSelfDrawnProgressBar(Color accentColor, double height, Thickness margin)
    {
        var accentBrush = new SolidColorBrush(accentColor);
        var bgBrush = new SolidColorBrush(Color.FromArgb(0x40, accentColor.R, accentColor.G, accentColor.B));

        var host = new Grid
        {
            Height = height,
            Margin = margin,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = true
        };
        // 【★ 修复：进度条比例改为 Grid Star 列宽驱动（不依赖 host 布局时序/手动 Width）】
        //  之前 indicator.Width = host.Bounds.Width * ratio：RefreshSchedule 重建后 host 未布局
        //  （Bounds.Width=0）→ 进度条 Width 从 0 假重置，待 LayoutUpdated 才写回正确比例；
        //  时间跳变/5s 硬刷新频繁重建时，用户感知"跳变后比例显示错误 / 流速不对（忽快忽慢/倒退）"。
        //  现改为两列比例：Column0 = 进度比例（indicator Stretch 自动占该列全宽），Column1 = 剩余空白。
        //  比例由 Grid 布局自动分配 → 重建后布局瞬间即正确，无假重置、无延迟、无依赖时序。
        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
        var bg = new Border
        {
            Background = bgBrush,
            CornerRadius = new CornerRadius(1.5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumnSpan(bg, 2);   // 背景铺满整个进度条区域
        var indicator = new Border
        {
            Background = accentBrush,
            CornerRadius = new CornerRadius(1.5),
            HorizontalAlignment = HorizontalAlignment.Stretch,   // 占满 Column0（比例列）
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumn(indicator, 0);
        host.Children.Add(bg);
        host.Children.Add(indicator);
        return (host, indicator);
    }

    private static void ApplyProgressRatioAv(Layoutable? host, Border? indicator, double ratio)
    {
        // 【★ 修复：进度条比例改为 Grid Star 列宽驱动】
        //  host 是 CreateSelfDrawnProgressBar 创建的 Grid（含 2 列：Column0=进度比例 / Column1=剩余）。
        //  直接设置两列 Star 宽度 → Grid 布局自动按比例分配 indicator 宽度，
        //  不依赖 host.Bounds.Width 是否就绪、无需 LayoutUpdated 延迟写回：
        //  - RefreshSchedule 重建后布局瞬间即正确（消除"重建后 Width=0 假重置/比例显示错误"）
        //  - 时间跳变/5s 硬刷新频繁重建时进度条连续正确（消除"流速不对/忽快忽慢/倒退"）
        if (host is not Grid hostGrid || hostGrid.ColumnDefinitions.Count < 2) return;
        ratio = double.IsNaN(ratio) ? 0.0 : Math.Clamp(ratio, 0.0, 1.0);
        hostGrid.ColumnDefinitions[0].Width = new GridLength(ratio, GridUnitType.Star);
        hostGrid.ColumnDefinitions[1].Width = new GridLength(Math.Max(0.0, 1.0 - ratio), GridUnitType.Star);
    }

    // ===================== 课表行休息分隔线（3px）辅助 =====================
    /// <summary>分隔线画刷：深色主题白色、浅色主题黑色（随主题深浅自适应）。</summary>
    private static IBrush GetBreakSeparatorBrushAv()
    {
        try
        {
            var app = Application.Current;
            bool dark = app?.ActualThemeVariant == ThemeVariant.Dark;
            return dark ? Brushes.White : Brushes.Black;
        }
        catch { return Brushes.Black; }
    }

    /// <summary>首次创建分隔线时订阅 ActualThemeVariantChanged，动态更新已建线颜色（幂等，重复调用只订阅一次）。</summary>
    private void EnsureBreakSeparatorThemeSubAv()
    {
        if (_breakSeparatorThemeHandlerAv != null) return;
        var app = Application.Current;
        if (app == null) return;
        _breakSeparatorThemeHandlerAv = (_, _) => UpdateBreakSeparatorColorsAv();
        app.ActualThemeVariantChanged += _breakSeparatorThemeHandlerAv;
    }

    /// <summary>退订主题事件并清空线控件登记（无分隔线 / Stop 时调用，防泄漏）。</summary>
    private void UnsubscribeBreakSeparatorThemeAv()
    {
        if (_breakSeparatorThemeHandlerAv == null) return;
        try
        {
            var appAv = Application.Current;
            if (appAv != null)
                appAv.ActualThemeVariantChanged -= _breakSeparatorThemeHandlerAv;
        }
        catch { }
        _breakSeparatorThemeHandlerAv = null;
    }

    /// <summary>主题深浅切换时把当前所有分隔线的颜色刷新一遍（白色↔黑色）。</summary>
    private void UpdateBreakSeparatorColorsAv()
    {
        try
        {
            if (_breakSeparatorLinesAv.Count == 0) return;
            var brush = GetBreakSeparatorBrushAv();
            foreach (var line in _breakSeparatorLinesAv)
            {
                try { line.Background = brush; } catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }
    }

    // ===================== 课表 UI 构建 =====================
    private void RefreshSchedule()
    {
        if (_window == null || _containerBorder == null) return;

        // 【修复：初始化时处于课间无法显示时间表】冷启动 sentinel 识别（必须在顶部重置 -1 之前捕获）
        //   _lastRefreshStateCode == -1 表示"从未成功构建过 UI"（冷启动 / Stop→Start 首次重建），
        //   此时若处于 Breaking 时段：跳过 ENTER 250ms 淡入动画直接落地 Opacity=1/Y=0，防止 250ms 内
        //   课间行 Opacity 被先置 0 → 用户启动时感知为「课间行消失 / 时间表卡住不更新」；
        //   同时动画时钟 FillMode.Forward 如未正确走完，元素永久保持透明造成 bug 现象。
        bool isColdStartAv = _lastRefreshStateCode == -1;

        var accentColor = GetAccentColor();
        var accentBrush = new SolidColorBrush(accentColor);
        // 字号：课程名=fontSize；其余按相对差值：表头-2、时间-1、老师-3
        var fontSize = (int)Math.Round(Math.Clamp(_settings.FloatingScheduleFontScale, 8, 32));
        // 背景不透明度：0~1 → SolidColorBrush Alpha (0=全透明, 255=不透明)
        var bgOpacity = Math.Clamp(_settings.FloatingScheduleOpacity, 0.0, 1.0);
        _currentClassRows.Clear();
        _currentOnClassIndex = -1;
        _currentProgressIndicator = null;
        _currentProgressHost = null;
        _currentBreakProgressIndicator = null;
        _currentBreakProgressHost = null;
        _currentBreakLayoutItem = null;
        _breakSeparatorLinesAv.Clear();   // 【课表行休息分隔线】本次重建的线控件重新登记（旧线随 _containerBorder.Child 替换出树）
        List<Control>? builtBreakVisualsAv = null;   // 【新增课间动画】本次构建命中的课间行 3 个视觉单元（若未命中课间则为 null）
        // 进度刷新缓存重置：保证 UpdateProgress 在本帧"按本次 RefreshSchedule 构建结果"作为基线比较
        _lastRefreshStateCode = -1;
        _lastRefreshBreakStartTicks = -1;
        _lastRefreshBreakEndTicks = -1;

        try
        {
            IBrush bgRaw = ThemeHelper.GetCardBackgroundBrush();
            IBrush? bg = bgRaw;
            // 把主题色转成带用户自定义透明度 Alpha 的背景刷
            if (bgRaw is SolidColorBrush scbBg)
            {
                var c = scbBg.Color;
                byte alpha = (byte)Math.Round(bgOpacity * 255);
                bg = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
            }
            var sep = ThemeHelper.GetSeparatorBrush();
            var textFg = ThemeHelper.GetTextBrush();
            var subFg = ThemeHelper.GetSubTextBrush();
            // ---------- 当前课高亮：强调色 × 40% 透明度（严格按需求：强调色的 40% Alpha ≈ 0x66/0xFF）----------
            // 不再使用之前硬编码的"浅蓝/浅紫"：任何主题下都是用户系统强调色 × 0.40，避免与应用强调色脱节。
            const double highlightAlphaRatio = 0.40;
            byte highlightAlpha = (byte)Math.Clamp((int)Math.Round(highlightAlphaRatio * 255), 0, 255);
            var highlightBg = new SolidColorBrush(Color.FromArgb(highlightAlpha, accentColor.R, accentColor.G, accentColor.B));

            _containerBorder.Background = bg;
            _containerBorder.BorderBrush = sep;

            // ---- 获取今日课表（优先：ILessonsService.CurrentClassPlan / GetClassPlanByDate(调试偏移后的今天)）----
            //  【修复：调试时间调整不立刻刷新时间表 - Date 锚点】
            //  原来用 DateTime.Today（本地系统日期，不会随 DebugTimeOffsetSeconds 跨天变化），
            //  改为 GetClassIslandNow().Date（走宿主 ExactTimeService，包含 DebugTimeOffsetSeconds + TimeOffsetSeconds 偏移），
            //  这样"调试把日期调到另一天（跨86400秒）"时 ClassPlan 能立刻抓目标日期课表，而不是停留在系统日期课。
            DateTime todayBase = GetClassIslandNow().Date;
            object? classPlan = ReflectProp(_lessonsService, "CurrentClassPlan");
            if (classPlan == null)
            {
                classPlan = InvokeGeneric(_lessonsService, "GetCurrentClassPlan");
                if (classPlan == null)
                {
                    classPlan = InvokeGeneric(_lessonsService, "GetClassPlanByDate", todayBase);
                    if (classPlan == null)
                    {
                        classPlan = InvokeGeneric(_lessonsService, "GetClassPlan", todayBase);
                    }
                }
            }

            Dictionary<Guid, Subject> subjectsMap = ReflectBuildSubjectsMap(ReflectProp(_profileService, "Profile") is { } p
                ? ReflectProp(p, "Subjects")
                : ReflectProp(_profileService, "Subjects"));

            bool hasClasses = false;
            if (classPlan != null)
            {
                var validRaw = ReflectGetValidTimeLayoutItems(classPlan);
                var validItems = new List<object>();
                // 【★ 连续课间分别走进度】同步收集课间项（TimeType==1），供课间进度条"每个课间分别走"
                // 【课表分隔线·档案组件】同步收集档案分隔线对象（TimeType==2），供分隔线绘制
                _breakItemsAv.Clear();
                _separatorItemsAv.Clear();
                foreach (var x in validRaw)
                {
                    if (x == null) continue;
                    if (ReflectGetTimeType(x) == 0) validItems.Add(x);
                    else if (ReflectGetTimeType(x) == 1) _breakItemsAv.Add(x);
                    else if (ReflectGetTimeType(x) == 2) _separatorItemsAv.Add(x);
                }
                validItems.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));
                _breakItemsAv.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));
                _separatorItemsAv.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));

                var classesList = ReflectGetClasses(classPlan);

                for (int i = 0; i < validItems.Count; i++)
                {
                    var layoutItem = validItems[i];
                    var layoutStart = ReflectGetStartTime(layoutItem);
                    var layoutEnd = ReflectGetEndTime(layoutItem);

                    object? classInfo = null;
                    foreach (var c in classesList)
                    {
                        if (c == null) continue;
                        var curLi = ReflectGetCurLayoutItemOfClass(c);
                        if (curLi == null) continue;
                        if (ReflectGetStartTime(curLi) == layoutStart && ReflectGetEndTime(curLi) == layoutEnd)
                        { classInfo = c; break; }
                    }
                    if (classInfo == null && i < classesList.Count)
                        classInfo = classesList[i];
                    if (classInfo == null || !ReflectGetIsEnabled(classInfo)) continue;

                    Subject? subject = null;
                    var sid = ReflectGetSubjectId(classInfo);
                    if (sid != Guid.Empty && subjectsMap.TryGetValue(sid, out var sbj))
                        subject = sbj;

                    _currentClassRows.Add((classInfo, subject, layoutItem));
                }
                hasClasses = _currentClassRows.Count > 0;
            }

            // ---- 定位当前课程索引 + 当前课间休息位置 ----
            var now = GetClassIslandNow().TimeOfDay;
            var curStateRaw = ReflectProp(_lessonsService, "CurrentState");
            TimeState curState = curStateRaw != null ? (TimeState)curStateRaw : TimeState.None;
            bool isOnClass = curState == TimeState.OnClass;
            bool isBreaking = curState == TimeState.Breaking;

            if (isOnClass)
            {
                // 【★ 时间跳变/连续上课 高亮定位】真实时间主选：
                //  - 连续上课 C1→C2：now 一过 C1.End 即定位 C2（比等 SDK 推进更准）
                //  - 时间跳变（改调试偏移/插件 TimeOffsetSeconds）：SDK CurrentTimeLayoutItem 不反映插件时间
                //    → 若只用 SDK 匹配会高亮旧课，进度条按旧课区间+新时间算（继承旧进度/卡 100%）
                //    → 必须用"真实时间 now 落在课表行的 [Start, End) 区间"定位目标课，进度条立即显示目标课新进度。
                _currentOnClassIndex = FindClassIndexByRealTimeAv(now);
                // SDK 匹配兜底：真实时间定位失败（如 SDK 特殊课程/时间异常）时，回退到 SDK CurrentTimeLayoutItem 匹配
                if (_currentOnClassIndex < 0)
                {
                    var curLi = ReflectProp(_lessonsService, "CurrentTimeLayoutItem");
                    if (curLi != null)
                    {
                        var cs = ReflectGetStartTime(curLi);
                        var ce = ReflectGetEndTime(curLi);
                        for (int i = 0; i < _currentClassRows.Count; i++)
                        {
                            if (ReflectGetStartTime(_currentClassRows[i].LayoutItem) == cs &&
                                ReflectGetEndTime(_currentClassRows[i].LayoutItem) == ce)
                            { _currentOnClassIndex = i; break; }
                        }
                    }
                }
            }

            // ---------- 课间休息插入判定（严格按需求：课间休息中才插入；未开始/已结束不处理）----------
            //   判定规则：
            //    1) 必须处于 Breaking 状态；
            //    2) SDK.CurrentTimeLayoutItem.TimeType == 1（是一个课间项目）；
            //    3) 该课间的 StartTime >= 第一节课的 StartTime（未开始时不显示，避免课前假课间）；
            //    4) 该课间的 EndTime   <= 最后一节课的 EndTime（已结束后不显示，避免放学后假课间）；
            //    5) 【修复：连续多课间（B1-B2-B3）不显示后续课间】
            //       旧算法：要求"相邻课程对 classRows[i].End==课间.Start && classRows[i+1].Start==课间.End 同时命中"才能插入。
            //       ——3 个连续课间时，B2/B3 既不等于前一节课 End 又不等于后一节课 Start → 永远找不着插入位置（breakInsertAfterClassIdx=-1）。
            //       新算法（鲁棒兼容任意多连续课间，且对"单课间"场景与原算法完全等价，无破坏性变更）：
            //         a) i_max_end_le_bs  = classRows 中最后一个满足 End <= 当前课间 Start 的索引  (= 前一个上课课)
            //         b) i_min_start_ge_be = classRows 中第一个满足 Start >= 当前课间 End 的索引   (= 后一个上课课)
            //         c) 合法：i_max_end_le_bs >= 0 && i_min_start_ge_be < Count && i_max_end_le_bs < i_min_start_ge_be
            //         d) 插入位置 = i_max_end_le_bs（在上一节课后面插入当前课间行）
            int breakInsertAfterClassIdx = -1;
            object? breakLayoutItem = null;
            TimeSpan breakStart = default;
            TimeSpan breakEnd = default;
            string breakNameText = "课间休息";
            if (isBreaking && _currentClassRows.Count >= 2)
            {
                // 【★ 连续课间分别走进度】首选：真实时间定位当前课间项（连续课间 B1→B2→B3 各自独立区间，
                //  课间行/进度条分别显示每个课间，而非课对空隙总长度）；找不到再回退 SDK CurrentTimeLayoutItem。
                var realBi = FindBreakItemByRealTimeAv(now, out var bsReal, out var beReal);
                var bLi = realBi ?? ReflectProp(_lessonsService, "CurrentTimeLayoutItem");
                if (bLi != null && ReflectGetTimeType(bLi) == 1)
                {
                    // 时间区间：真实时间定位命中用其区间，否则用 SDK item 区间
                    var bs = realBi != null ? bsReal : ReflectGetStartTime(bLi);
                    var be = realBi != null ? beReal : ReflectGetEndTime(bLi);
                    var firstStart = ReflectGetStartTime(_currentClassRows[0].LayoutItem);
                    var lastEnd = ReflectGetEndTime(_currentClassRows[_currentClassRows.Count - 1].LayoutItem);
                    if (bs >= firstStart && be <= lastEnd)
                    {
                        // ---- 新算法（鲁棒兼容连续课间）：按"前后上课课"定位插入位置 ----
                        //  a) 前一节课：最后一个 End <= bs 的上课行
                        int iMaxEndLeBs = -1;
                        for (int k = 0; k < _currentClassRows.Count; k++)
                        {
                            if (ReflectGetEndTime(_currentClassRows[k].LayoutItem) <= bs) iMaxEndLeBs = k;
                            else break;   // classRows 按时间升序，一旦超就不用继续
                        }
                        //  b) 后一节课：第一个 Start >= be 的上课行
                        int iMinStartGeBe = _currentClassRows.Count;
                        for (int k = _currentClassRows.Count - 1; k >= 0; k--)
                        {
                            if (ReflectGetStartTime(_currentClassRows[k].LayoutItem) >= be) iMinStartGeBe = k;
                            else break;   // 反向扫描同样：时间升序，小于 be 就停
                        }
                        //  c) 合法性：前一节课 必须在 后一节课 之前
                        if (iMaxEndLeBs >= 0 && iMinStartGeBe < _currentClassRows.Count &&
                            iMaxEndLeBs < iMinStartGeBe)
                        {
                            //  d) 插入位置 = 在前一节课后面（无论是 1 个课间 中间 还是 N 个连续课间 中间，都正确）
                            breakInsertAfterClassIdx = iMaxEndLeBs;
                            breakLayoutItem = bLi;
                            breakStart = bs;
                            breakEnd = be;
                            breakNameText = ReflectGetBreakNameText(bLi);
                        }
                    }
                    // 【★ 连续课间即时切换（空隙兜底）】真实时间课间项定位失败（如 _breakItemsAv 空/时间异常）
                    //  且 SDK item 超时 → 回退课对空隙定位，保证 SDK 滞后时课间行仍能切到下一段。
                    if (realBi == null && now.TotalSeconds - be.TotalSeconds > EndOverrunToleranceSecAv)
                    {
                        int gapIdx = FindBreakGapIndexByRealTimeAv(now, out var gs, out var ge);
                        if (gapIdx >= 0)
                        {
                            breakInsertAfterClassIdx = gapIdx;
                            breakStart = gs;
                            breakEnd = ge;
                            breakNameText = ReflectGetBreakNameText(bLi);   // 名称沿用 SDK 课间 item（连续课间名称通常一致）
                        }
                        else
                        {
                            // 真实时间已不在任何空隙（SDK 滞后但实际已到上课/放学）→ 不显示课间行
                            breakInsertAfterClassIdx = -1;
                        }
                    }
                }
            }

            // ---- 若无课，显示占位符 ----
            if (!hasClasses)
            {
                var noClass = new TextBlock
                {
                    Text = "今天没有课程",
                    FontSize = fontSize,
                    Foreground = subFg,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 6)
                };
                _containerBorder.Child = noClass;
                _rootContent = noClass;
                StartOrStopTimer();
                return;
            }

            // ---- 构建 Grid ----
            // 【课表分隔线·档案组件】先算"分隔线插入位置集合"（HashSet 天然去重）：
            //  数据源 = 档案中用户手动插入的分隔线对象（_separatorItemsAv，TimeType==2，StartTime==EndTime 时间点语义），
            //  而不是每个课间都画线。对每个分隔线 s，找 _currentClassRows 中最后一个 End <= s.Start 的索引 i → 在第 i 行之后插一条线；
            //  多条分隔线命中同一 i（如都落在同一节课后）→ 只插一条线。
            //  注意：线作为独立 Grid 行插入，_currentClassRows 索引与 _currentOnClassIndex 均不受影响。
            var sepAfterRowsAv = new HashSet<int>();
            foreach (var s in _separatorItemsAv)
            {
                var sStart = ReflectGetStartTime(s);
                int iMaxEndLeBs = -1;
                for (int k = 0; k < _currentClassRows.Count; k++)
                {
                    if (ReflectGetEndTime(_currentClassRows[k].LayoutItem) <= sStart) iMaxEndLeBs = k;
                    else break;   // classRows 按时间升序，一旦超过就不用继续
                }
                // 只在"两节课之间"插线：i 必须是有效行且不是最后一行
                if (iMaxEndLeBs >= 0 && iMaxEndLeBs < _currentClassRows.Count - 1)
                    sepAfterRowsAv.Add(iMaxEndLeBs);
            }

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto, *"),
                RowDefinitions = new RowDefinitions(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ColumnSpacing = 14,
                RowSpacing = 0
            };

            // 表头行
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var header1 = new TextBlock
            {
                Text = "课程",
                FontSize = Math.Max(8, fontSize - 2),
                FontWeight = FontWeight.Bold,
                Foreground = textFg,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetColumn(header1, 0); Grid.SetRow(header1, 0); grid.Children.Add(header1);
            var header2 = new TextBlock
            {
                Text = "时间",
                FontSize = Math.Max(8, fontSize - 2),
                FontWeight = FontWeight.Bold,
                Foreground = textFg,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetColumn(header2, 1); Grid.SetRow(header2, 0); grid.Children.Add(header2);

            // 分隔线行
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var sepLine = new Border
            {
                Height = 1,
                Background = sep,
                Margin = new Thickness(0, 0, 0, 6),
                CornerRadius = new CornerRadius(0.5)
            };
            Grid.SetColumnSpan(sepLine, 2);
            Grid.SetRow(sepLine, 1); grid.Children.Add(sepLine);

            // 课程行
            for (int i = 0; i < _currentClassRows.Count; i++)
            {
                var row = _currentClassRows[i];
                var subject = row.Subject;
                var layoutItem = row.LayoutItem;
                var isCurrent = i == _currentOnClassIndex;

                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var rIdx = grid.RowDefinitions.Count - 1;

                IBrush rowBg = Brushes.Transparent;
                if (isCurrent) rowBg = highlightBg;

                var courseName = subject?.Name ?? "(未安排)";
                // 教师名：空 TeacherName 不显示；关闭=姓氏+"老师"；开启=全名（例：张三）
                string? teacherLine = null;
                var teacherFull = subject?.TeacherName;
                if (!string.IsNullOrWhiteSpace(teacherFull))
                {
                    if (_settings.FloatingScheduleEnableFullTeacherName)
                    {
                        teacherLine = teacherFull.Trim();
                    }
                    else
                    {
                        // 【新增：外国姓名国家前缀】优先本地 TeacherNameHelper：
                        //   例如 【美国】William Jefferson Clinton → 剥除【美国】→ 取最后单词 Clinton → "Clinton 老师"；
                        //   中文名（含复姓）保持 SDK 语义（张三 → 张 / 欧阳夏雪 → 欧阳）；
                        //   Helper 返回空白时回退 SDK Subject.GetFirstName()，保证与历史行为完全兼容。
                        var surnameFromHelper = TeacherNameHelper.ExtractTeacherShortName(teacherFull);
                        var surname = !string.IsNullOrWhiteSpace(surnameFromHelper)
                            ? surnameFromHelper
                            : subject!.GetFirstName();
                        if (!string.IsNullOrWhiteSpace(surname))
                        {
                            teacherLine = surname + "老师";
                        }
                    }
                }

                // ---- 课程列 ----
                // 教师名从"课程名下方"改为"学科名正右方"：同一行两列 Grid（左课程名 * / 右教师名 Auto 垂直居中对齐）
                var coursePanel = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("*, Auto"),
                    RowDefinitions = RowDefinitions.Parse("Auto"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = rowBg,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                Grid.SetColumn(coursePanel, 0);
                Grid.SetRow(coursePanel, rIdx);

                var courseTb = new TextBlock
                {
                    Text = courseName,
                    FontSize = fontSize,
                    FontWeight = isCurrent ? FontWeight.Bold : FontWeight.Normal,
                    Foreground = textFg,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(courseTb, 0); Grid.SetRow(courseTb, 0);
                coursePanel.Children.Add(courseTb);

                if (!string.IsNullOrEmpty(teacherLine))
                {
                    int teacherFontSize = Math.Max(8, fontSize - 3);
                    // ===== 修复：教师列完整展示 15 汉字（用户最新要求）=====
                    // 单一事实来源：教师列宽 MaxWidth = teacherFontSizePt × 21.5
                    //   推导：CJK 全角 YaHei 方块字 = fontSize pt × (96/72) = px ≈ pt×1.333 px/字；15 字 = pt×1.333×15 = pt×20；
                    //   加 7.5% 余量（少民·点号半角/阿语混排/不同 DPI/YAHEI ·≈0.8字 实测 15字ASCII 200.7px=13.38pt?→实际 15字英文名 316px /15pt=21.04 需要 21.1，取 21.5 给 1.5% 安全）→ 系数 21.5。
                    //   基准 15 pt → 322.5 px：
                    //     * 4 字欧阳夏雪 80 px（占 24.8%，大量余量）
                    //     * 10 字少民阿卜杜拉·本·塔里克 169.63 px（52.6%，仍有一半余量）
                    //     * 15 字纯 CJK 15×20=300 px ≤ 322.5 ✅ 完整
                    //     * 15 字 ASCII 极端半角 200.7 px ≤ 322.5 ✅ 完整
                    //     * 英文名 30 字符 Dr. Christopher van der Saar PhD = 316.13 px ≤ 322.5 ✅ 完整（普通外籍教师姓名范畴）
                    //   FontScale=8 → teacher 8×21.5=172 px；FontScale=32 → teacher 29×21.5=623.5 px，外层 820 兜底完整。
                    var teacherMaxW = teacherFontSize * 21.5;
                    var teacherTb = new TextBlock
                    {
                        Text = teacherLine,
                        FontSize = teacherFontSize,
                        Foreground = subFg,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        TextWrapping = TextWrapping.NoWrap,
                        MaxWidth = teacherMaxW,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    Grid.SetColumn(teacherTb, 1);
                    Grid.SetRow(teacherTb, 0);
                    coursePanel.Children.Add(teacherTb);
                }

                // 当前课：进度条放在课程行的下边缘
                if (isCurrent)
                {
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var pIdx = grid.RowDefinitions.Count - 1;
                    var (progHost, progInd) = CreateSelfDrawnProgressBar(accentColor, 2.5, new Thickness(0, 2, 0, 0));
                    _currentProgressHost = progHost;
                    _currentProgressIndicator = progInd;
                    Grid.SetColumn(progHost, 0);
                    Grid.SetColumnSpan(progHost, 2);
                    Grid.SetRow(progHost, pIdx);
                    grid.Children.Add(progHost);
                }

                grid.Children.Add(coursePanel);

                // ---- 时间列 ----
                var timeStr =
                    $"{FormatHhMm(ReflectGetStartTime(layoutItem))} - {FormatHhMm(ReflectGetEndTime(layoutItem))}";
                var timeTb = new TextBlock
                {
                    Text = timeStr,
                    FontSize = Math.Max(8, fontSize - 1),
                    Foreground = textFg,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = rowBg,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                Grid.SetColumn(timeTb, 1);
                Grid.SetRow(timeTb, rIdx);
                grid.Children.Add(timeTb);

                // ---------- 课间休息插入行（仅当：i == breakInsertAfterClassIdx，即当前处于 Breaking 且正好在这两节课之间）----------
                //   规则：课间休息才显示；时间表未开始/已结束（breakInsertAfterClassIdx == -1）绝对不插行；
                //        课间结束 → 下一个 Tick 的 needRefresh → RefreshSchedule 重绘，本插入行自然消失。
                if (i == breakInsertAfterClassIdx && breakLayoutItem != null)
                {
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var brIdx = grid.RowDefinitions.Count - 1;
                    // 整行底色：次级文字透明度 × 0.08 的淡灰，明确区分"课间"与"课程"；不使用强调色避免与当前课高亮混淆。
                    byte breakAlpha = (byte)Math.Clamp((int)Math.Round(0.08 * 255), 0, 255);
                    byte grayBase = ThemeHelper.IsDarkTheme() ? (byte)0xFF : (byte)0x00;
                    IBrush breakBg = new SolidColorBrush(Color.FromArgb(breakAlpha, grayBase, grayBase, grayBase));
                    // 左：课间名称（默认 "课间休息"，或自定义 BreakName），使用次级灰字
                    //   【动画对齐 WPF】用 Border 包 TextBlock 作为独立动画单元，与 WPF breakCellL 对应
                    var breakTb = new TextBlock
                    {
                        Text = breakNameText,
                        FontSize = Math.Max(8, fontSize - 1),
                        Foreground = subFg,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 3, 0, 3)
                    };
                    var breakCellL = new Border
                    {
                        Background = breakBg,
                        Child = breakTb,
                    };
                    Grid.SetColumn(breakCellL, 0);
                    Grid.SetRow(breakCellL, brIdx);
                    grid.Children.Add(breakCellL);
                    // 右：时间区间（正好是"上一节End - 下一节Start"；使用 SDK 真实课间项目 Start/End 保证与课表 100% 一致）
                    var breakTimeStr = $"{FormatHhMm(breakStart)} - {FormatHhMm(breakEnd)}";
                    var breakTimeTb = new TextBlock
                    {
                        Text = breakTimeStr,
                        FontSize = Math.Max(8, fontSize - 1),
                        Foreground = subFg,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 3, 0, 3)
                    };
                    var breakCellR = new Border
                    {
                        Background = breakBg,
                        Child = breakTimeTb,
                    };
                    Grid.SetColumn(breakCellR, 1);
                    Grid.SetRow(breakCellR, brIdx);
                    grid.Children.Add(breakCellR);
                    // 课间进度条：位于插入行下方，横跨两列；自绘 Grid+两层 Border，100% 可控不依赖 ProgressBar 自带模板
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var pbIdx = grid.RowDefinitions.Count - 1;
                    var (breakHost, breakInd) = CreateSelfDrawnProgressBar(accentColor, 2.5, new Thickness(0, 2, 0, 0));
                    _currentBreakProgressHost = breakHost;
                    _currentBreakProgressIndicator = breakInd;
                    _currentBreakLayoutItem = breakLayoutItem;
                    Grid.SetColumn(breakHost, 0);
                    Grid.SetColumnSpan(breakHost, 2);
                    Grid.SetRow(breakHost, pbIdx);
                    grid.Children.Add(breakHost);
                    // 【新增课间动画】登记 3 个独立视觉单元到 List，供 ENTER 动画遍历播放
                    builtBreakVisualsAv = new List<Control>(capacity: 3)
                    {
                        breakCellL, breakCellR, breakHost,
                    };
                }

                // 【课表行休息分隔线】第 i 行与第 i+1 行之间存在课间 → 追加一条 3px 分隔线行（跨两列）。
                //  作为独立 Grid 行插在本行所有子行（进度条/课间行）之后，_currentClassRows 索引与高亮/进度定位不受影响。
                if (sepAfterRowsAv.Contains(i))
                {
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var sepRowIdx = grid.RowDefinitions.Count - 1;
                    var sepBorderAv = new Border
                    {
                        Height = 3,
                        Background = GetBreakSeparatorBrushAv(),
                        Margin = new Thickness(0, 1, 0, 1)
                    };
                    Grid.SetColumnSpan(sepBorderAv, 2);
                    Grid.SetRow(sepBorderAv, sepRowIdx);
                    grid.Children.Add(sepBorderAv);
                    _breakSeparatorLinesAv.Add(sepBorderAv);
                    EnsureBreakSeparatorThemeSubAv();   // 首次建线时订阅主题切换（Stop 退订防泄漏）
                }
            }

            _containerBorder.Child = grid;
            _rootContent = grid;
            // 【课表行休息分隔线】本次重建无分隔线 → 退订主题事件（旧线已随 Child 替换出树，防泄漏）
            if (_breakSeparatorLinesAv.Count == 0)
                UnsubscribeBreakSeparatorThemeAv();
            // ---- 【新增课间动画】登记新建课间行视觉单元；如需 ENTER 动画 fire-and-forget 启动 ----
            //   只在"上一帧不是 Breaking && 当前不是 EXIT 动画进行中 && 本次构建确实产出了课间行 3 元素"时播放 ENTER，
            //   避免"SDK 内部课间切课间（同 break）RefreshSchedule 重建"、"EXIT 动画结束 RefreshSchedule 又回到 Breaking"
            //   等场景下不必要的重放动画。
            //   【例外：isColdStartAv 冷启动】跳过 ENTER，直接写本地 Opacity=1/Y=0，保证用户能立刻看到最终态
            //   【★ 修复：初始化时课间文本 250ms 透明卡在上一课里】
            //      时序 bug：启动时 StartInternal.Post 显式调用 RefreshSchedule #1（sentinel=-1 → isColdStartAv=true → 跳过 ENTER，正确）；
            //                紧接着 500ms Timer 第一 Tick UpdateProgress：stateOrBreakChanged = Breaking code - (-1) = true → Post RefreshSchedule #2；
            //                RefreshSchedule #2 入口 _lastRefreshStateCode 已被 #1 写为 Breaking int → isColdStartAv=false；
            //                同时 #1 由 StartInternal.Post 直接调，不走 UpdateProgress，finally 同步 _wasBreakLastTickAv=true 没执行；
            //                所以 RefreshSchedule #2 看到 _wasBreakLastTickAv=false（字段初始值）→ 命中 ENTER → 课间行 250ms Opacity=0 淡入！
            //                250ms 内用户只有上一课 C0 文本可见 → 误认为"课间文本卡在上一课文本里"。
            //      修复策略：用局部变量 prevWasBreakAv 先保存 ENTER 判断时的原始快照（=本次 RefreshSchedule 被调用时刻的上一帧快照），
            //                再立刻把字段 _wasBreakLastTickAv 写为 true（只要本次构建确实产出了课间行）。
            //                这样 ENTER 判断用"调用时刻的快照"保证正常"非 break→break"场景仍正确命中 ENTER；
            //                而写字段保证下一次 RefreshSchedule 调用时看到"上次已在 break 中"→ 跳过 ENTER（修复初始化 2 次重建的 bug）。
            _currentBreakRowVisualsAv = builtBreakVisualsAv;
            if (builtBreakVisualsAv != null)
            {
                // (1) 先快照"本次 RefreshSchedule 被调时刻的上一帧状态"（保留：保证 3 步快照语义，避免初始化连续重建误判其他分支）
                _ = _wasBreakLastTickAv; // （保留读取以兼容旧快照模式语义，实际 ENTER 动画已删除）

                // (2) ★ 立刻写字段 = true：只要本次构建产出课间行 → 锁定"在 break 中"
                //     —— 下次 RefreshSchedule（下一 Tick / 5s 强制 / PropertyChanged Post）看到"上一帧已是 break"
                _wasBreakLastTickAv = true;

                // (3) 用户要求：删除「淡化渐变以外的所有动画」→ ENTER 不再执行任何 250ms TranslateY/淡入，
                //     直接落地最终淡化可见状态：Opacity=1 / TT.Y=0（无论冷启动或过渡，立即渲染）。
                EnsureBreakRowTransformAv(builtBreakVisualsAv);
                foreach (var el in builtBreakVisualsAv)
                {
                    try { el.Opacity = 1.0; } catch { /* ignore */ }
                    try
                    {
                        if (el.RenderTransform is TranslateTransform ttAv) ttAv.Y = 0.0;
                    }
                    catch { /* ignore */ }
                }
            }
            else
            {
                // 本次没构建课间行（非 Breaking：None/OnClass/AfterSchool...）→ 写 false，保留状态机语义（EXIT 已删，仅作标记）
                _wasBreakLastTickAv = false;
                _currentBreakProgressIndicator = null;
                _currentBreakProgressHost = null;
                _currentBreakLayoutItem = null;
            }
            // ---- 构建完成后记录"本次刷新时的状态快照" ----
            //  下一帧 UpdateProgress 以这里作为基线；这样 Breaking→OnClass 或 OnClass→Breaking 等状态切换能立刻触发重建。
            _lastRefreshStateCode = (int)curState;
            _lastRefreshBreakStartTicks = breakInsertAfterClassIdx >= 0 && breakLayoutItem != null ? breakStart.Ticks : -1;
            _lastRefreshBreakEndTicks   = breakInsertAfterClassIdx >= 0 && breakLayoutItem != null ? breakEnd.Ticks   : -1;
            // 【修复：调试时间不立刻刷新 - Date 兜底快照】
            //  记录本次 RefreshSchedule 构建时的调试时间 Date（非系统 DateTime.Today），
            //  500ms Tick 检测 dateChanged 时以此为基线，跨天调试跳转即使 PropertyChanged 事件丢失也能在下一 Tick 强制 needRefresh。
            _lastRefreshDate = GetClassIslandNow().Date;

            // ===== 【★ 用户报告：悬浮窗初始化以进度条 0% 为假快照 → 后续异常】集中兜底 =====
            //  根因（与 Experience 601376 "多写入源覆盖"同构）：
            //   RefreshSchedule 有 6 处调用（StartInternal 冷启动 / Settings 开开关 / 5s 硬清理 / EXIT 同步 / DebugTime / needRefresh Post），
            //   但仅 DebugTime(L717-773) 和 needRefresh(L2224-2329) 两处知道"RefreshSchedule 之后必须立刻写真实 ratio 到新 UI indicator"，
            //   其余 4 处漏掉 → 新 UI indicator Width = 构造默认 0、indicator.Tag = null →
            //   首次 LayoutUpdated 触发时 L1438 fallback `Tag is double r ? r : 0.0` = 0.0 → indicator.Width = 0 * host.Width = 0（假 0% 快照）。
            //   500ms 后 UpdateProgress 才修正，期间 0% 会被用户感知为"进度条卡住"，并与后续硬校验/PropertyChanged 触发的 RefreshSchedule 叠加造成视觉跳变或状态误判。
            //  修复（集中兜底 = Experience 601376 "清空数据源同步 progress/currentIndex" 思想一致）：
            //   在 RefreshSchedule 末尾（StartOrStopTimer 之前）**无条件同步 Apply 一次"本帧真实 ratio"**。此时 host.Bounds.Width 通常仍=0（尚未布局），
            //   ApplyProgressRatioAv 走 else 分支把 ratio 缓存到 indicator.Tag → 布局就绪后 LayoutUpdated 回调 L1438 读 cached=Tag=真实值，不会再 fallback 到 0.0。
            try
            {
                var svc = _lessonsService;
                if (svc != null)
                {
                    var sRaw = ReflectProp(svc, "CurrentState");
                    TimeState s = sRaw != null ? (TimeState)sRaw : TimeState.None;
                    bool onC = s == TimeState.OnClass;
                    bool brk = s == TimeState.Breaking;

                    // -- 课间进度条 --
                    if (brk)
                    {
                        // 课间进度条：真实时间定位当前课间空隙（SDK 滞后时也能正确推进/重置）
                        var nn = GetClassIslandNow().TimeOfDay;
                        var biB = FindBreakItemByRealTimeAv(nn, out var gsB, out var geB);
                        if (biB != null && _currentBreakProgressIndicator != null)
                        {
                            double tb = (geB - gsB).TotalSeconds;
                            if (tb > 0)
                            {
                                double breakRatio = Math.Clamp((nn - gsB).TotalSeconds / tb, 0.0, 1.0);
                                ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, breakRatio);
                            }
                            else ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                        }
                        else if (_currentBreakProgressIndicator != null)
                            ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                    }
                    else if (_currentBreakProgressIndicator != null)
                        ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);

                    // -- 当前课进度条（用高亮行区间 + 真实时间，不再依赖 SDK 滞后 item）--
                    if (onC)
                    {
                        // 高亮行：RefreshSchedule 上方已保证 onC 时 _currentOnClassIndex 非 -1（SDK 匹配确定高亮）
                        if (_currentOnClassIndex >= 0 && _currentOnClassIndex < _currentClassRows.Count &&
                            _currentProgressIndicator != null)
                        {
                            var rowLi = _currentClassRows[_currentOnClassIndex].LayoutItem;
                            var st = ReflectGetStartTime(rowLi);
                            var ed = ReflectGetEndTime(rowLi);
                            double t = (ed - st).TotalSeconds;
                            if (t <= 0) { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); }
                            else
                            {
                                var nn = GetClassIslandNow().TimeOfDay;
                                double classProgressRatio = Math.Clamp((nn - st).TotalSeconds / t, 0.0, 1.0);
                                ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, classProgressRatio);
                            }
                        }
                        else if (_currentProgressIndicator != null)
                            ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                    }
                    else if (_currentProgressIndicator != null)
                        ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                }
            }
            catch { /* 绝对兜底：即使 Apply 失败，下一次 500ms UpdateProgress Tick 也会修正，不能影响 RefreshSchedule 主流程 */ }

            StartOrStopTimer();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RefreshSchedule 异常");
            try
            {
                var isDark = ThemeHelper.IsDarkTheme();
                var errorPanel = new StackPanel
                {
                    MinWidth = 200
                };
                errorPanel.Children.Add(new TextBlock
                {
                    Text = "悬浮时间表",
                    FontSize = fontSize,
                    FontWeight = FontWeight.Bold,
                    Foreground = isDark ? Brushes.White : Brushes.Black,
                    Margin = new Thickness(0, 0, 0, 6)
                });
                errorPanel.Children.Add(new TextBlock
                {
                    Text = "加载中...",
                    FontSize = Math.Max(8, fontSize - 2),
                    Foreground = isDark ? Brushes.LightGray : Brushes.DimGray
                });
                _containerBorder.Child = errorPanel;
            }
            catch { /* 极端兜底 */ }
        }
    }

    // ===================== 进度刷新 =====================
    // 最后一次 RefreshSchedule 时缓存的"状态+当前课间 Start/End"：用于 500ms Tick 检测状态或课间位置变化，
    //  一旦变化立刻 Post RefreshSchedule 重建整表（保证插入行出现/消失与高亮位置切换即时响应）。
    private int _lastRefreshStateCode = -1;
    private long _lastRefreshBreakStartTicks = -1;
    private long _lastRefreshBreakEndTicks = -1;

    private void UpdateProgress()
    {
        if (_lessonsService == null) return;
        bool breaking = false;   // 提升到 try 外，供 finally 同步 _wasBreakLastTickAv 快照
        try
        {
            // ---- 指针淡化每帧先判断（悬浮窗可能在点击穿透/或 PointerEntered 不可用，只能每帧拉）----
            ApplyHoverFade();

            // 【修复：调试时间不立刻刷新 - Date 跳变兜底】
            //  每 Tick 先取一次"调试时间完整 DateTime"（包含 DebugTimeOffsetSeconds/TimeOffsetSeconds 偏移）。
            //  - 用途1：dateChanged 检测（跨天调试、NTP 回跳、系统时间改）→ 强制 stateOrBreakChanged=true。
            //  - 用途2：后续所有 .TimeOfDay 比率计算可复用 now，避免每 Tick 调多次 GetClassIslandNow()（减少宿主 ExactTimeService 开销）。
            var now = GetClassIslandNow();

            var curStateRaw = ReflectProp(_lessonsService, "CurrentState");
            TimeState curState = curStateRaw != null ? (TimeState)curStateRaw : TimeState.None;
            bool onClass = curState == TimeState.OnClass;
            breaking = curState == TimeState.Breaking;

            // ====== 需求 1：悬浮窗随时间状态变化而变化（含课间插入行显示/消失）======
            //  比"仅 OnClass 索引校验"更强：当状态在 None/OnClass/Breaking/AfterSchool 之间切换，
            //  或 Breaking 模式下 当前课间 Start/End 与上次刷新时不一致（跨课间），立即整表重建。
            int stateCode = (int)curState;
            long breakStartTicks = -1;
            long breakEndTicks = -1;
            object? bLi = null;
            if (breaking)
            {
                bLi = ReflectProp(_lessonsService, "CurrentTimeLayoutItem");
                if (bLi != null && ReflectGetTimeType(bLi) == 1)
                {
                    breakStartTicks = ReflectGetStartTime(bLi).Ticks;
                    breakEndTicks = ReflectGetEndTime(bLi).Ticks;
                }
            }
            bool stateOrBreakChanged = stateCode != _lastRefreshStateCode ||
                                       breakStartTicks != _lastRefreshBreakStartTicks ||
                                       breakEndTicks != _lastRefreshBreakEndTicks;

            // 【★ 时间跳变 → 进度条立即更新兜底】
            //  若 now 与上次 Tick 差距超过 2s（正常 500ms Tick 差距 ~0.5s），视为时间跳变
            //  （宿主调试偏移 / 系统时间 / NTP 同步导致）。若宿主 Settings.PropertyChanged 事件丢失
            //  （订阅失败 / 宿主版本差异 / 事件被吞）→ OnHostSettingsDebugTimeChanged 未触发 →
            //  进度条只能等 5s 硬刷新才用新时间重建（用户感知"时间跳变进度条不立即更新"）。
            //  修复：跳变时强制 stateOrBreakChanged=true → needRefresh → RefreshSchedule 真实时间定位 + Apply。
            //  去重：最近 1s 内宿主已触发刷新（OnHostSettingsDebugTimeChanged 执行过）→ 跳过，避免重复重建闪 0。
            if (_lastTickNowAv != DateTime.MinValue &&
                Math.Abs((now - _lastTickNowAv).TotalSeconds) > 2.0 &&
                Environment.TickCount64 - _lastHostTimeChangeRefreshTicksAv > 1000)
            {
                stateOrBreakChanged = true;
            }
            _lastTickNowAv = now;

            // 【修复：调试时间不立刻刷新 - Date 跳变兜底强制 Refresh】
            //  场景：用户在调试页调 DebugTimeOffsetSeconds += 86400（跨1天），或系统时间被 NTP 回拨/拨快超过一天。
            //  即使宿主 SettingsService.Settings.PropertyChanged 事件丢失（极端场景），500ms Tick 内检测到 now.Date 与
            //  上次 RefreshSchedule 记录的 _lastRefreshDate 不一致 → 直接把 stateOrBreakChanged 置 true，
            //  这样 needRefresh=true 会触发 RefreshSchedule，ClassPlan 会用新 todayBase 抓取目标日期课表。
            //  注意：_lastRefreshDate==MinValue 代表首次启动（从未 Refresh 成功过），此时不强制（sentinel=-1 已经会强制 Refresh）。
            if (_lastRefreshDate != DateTime.MinValue && now.Date != _lastRefreshDate)
                stateOrBreakChanged = true;

            // 【★ 连续课间即时切换触发源（保留 breaking 分支）】
            //  课间行是动态插入的，连续课间 B1→B2 时 SDK CurrentTimeLayoutItem 滞后 1-2 Tick 仍返回 B1：
            //  真实 now 超过 SDK 课间 item.End + 容忍 → 强制 needRefresh → RefreshSchedule 用真实时间空隙
            //  定位 B2 → 课间行立即切到 B2 + 进度条正确重置。（连续上课无此问题：课程行总在列表里，
            //  高亮切换由 5s 硬刷新兜底，无需 500ms 级即时检测，故不再对 onClass 分支做超时强制。）
            if (!stateOrBreakChanged && breaking && bLi != null)
            {
                var eBrk = ReflectGetEndTime(bLi);
                if (now.TimeOfDay.TotalSeconds - eBrk.TotalSeconds > EndOverrunToleranceSecAv)
                    stateOrBreakChanged = true;
            }

            // 【5s 全量强制刷新（用户：任何情况下每 5s 刷新，而不是兜底）】
            //  对齐 Experience 1279696 双定时器分层（低频做重数据刷新） + Experience 1034846 到点必刷（不得用比对/动画阻断）：
            //   · 500ms Tick（高频轻量）：进度条 ratio 更新 + state/breakTicks/date 变化 → 即时 needRefresh 触发（保持 500ms 级灵敏度）
            //   · 每 10 Tick = 5s（低频全量）：【无条件】强制 RefreshSchedule 重建整表
            //     ✅ 无论"是否刚刷新过/EXIT 动画是否在进行/SDK 状态是否变化"—— 每 5s 必刷，用户要求绝对保证时间表与宿主最新 ClassPlan/LessonsService 状态同步。
            //     保护：5s 到点先强制清理正在进行的 EXIT 动画（Cancel CTS + 清所有标志/pending），避免 250ms 后 onCompleted 脏覆盖（与 DebugTime Post 开头完全对称）；
            //           串行性保证：DispatcherTimer Tick 与 RefreshSchedule 都在 UI 线程跑，无重入（回调返回前 Timer 不会再发下一 Tick）。
            _hardSyncTickCounterAv = (_hardSyncTickCounterAv + 1) % HardSyncTickIntervalAv;
            if (_hardSyncTickCounterAv == 0)
            {
                //  (1) 强制清理 EXIT 动画：与 OnHostSettingsDebugTimeChangedAv Post 开头完全一致的复位块（无条件执行，即使 EXIT 没在跑也安全）
                // 【修复：课间向上位移 0.5-1s】同步清理 ENTER CTS：每 10 Tick = 5s 强制重建前，Cancel 任何前 5s 内排队的 FireEnter
                //    （DispatcherPriority.Loaded 异步，若 UI 线程繁忙，排队 FireEnter 可能在 RefreshSchedule #N 替换 UI 之后才执行）
                // 【★ 修复：3-10s 向上位移（5s 硬清理 ENTER 假命中）】
                //   根因：之前此处无条件 `_wasBreakLastTickAv = false;`，但本 Tick 顶部 L2043 刚根据 SDK CurrentState 算出 breaking 布尔快照
                //         （此刻用户若仍在 Breaking 时段，breaking=true）；紧接着 RefreshSchedule ENTER 块的 prevWasBreakAv=L1910 会读
                //         清理后被抹零的字段 → 取到"假非-break→break 过渡"的 prevWasBreakAv=false → 命中 ENTER 写 Y=-24/Opacity=0 起点。
                //         时间窗口：启动后第 1 次 5s 到点、第 2 次 10s 到点、CrossPlugin 3s 彩蛋定时器若触发 PropertyChanged→RefreshSchedule →
                //         与用户报告「先正常显示，约 3-10s 后向上位移」完全一致。
                //   修复：_wasBreakLastTickAv 不得一刀切 false，必须写=本 Tick 刚算出的真实 breaking 快照 → 若此刻 Breaking=true → ENTER 快照
                //         prevWasBreakAv=true → ENTER 判断短路不触发 → 不写 Y=-24 起点。DebugTime/Stop 两处写 false 合理（sentinel=-1 使 isColdStartAv=true 短路 ENTER）。
                try
                {
                    try { _breakRowExitCtsAv?.Cancel(); } catch { /* ignore */ }
                    try { _breakRowEnterCtsAv?.Cancel(); } catch { /* ignore */ }
                    try
                    {
                        var oldCts = System.Threading.Interlocked.Exchange(ref _breakRowExitCtsAv, null);
                        oldCts?.Dispose();
                    }
                    catch { /* ignore */ }
                    try
                    {
                        var oldEnter = System.Threading.Interlocked.Exchange(ref _breakRowEnterCtsAv, null);
                        oldEnter?.Dispose();
                    }
                    catch { /* ignore */ }
                    _breakRowExitAnimatingAv = false;
                    // 【★ 关键修复行】写入本 Tick 的真实 breaking 快照（不是永远 false）
                    _wasBreakLastTickAv = breaking;
                    _breakRowExitPendingStateCode = -1;
                    _breakRowExitPendingBreakStart = -1;
                    _breakRowExitPendingBreakEnd = -1;
                    _currentBreakRowVisualsAv = null;
                    _currentBreakProgressIndicator = null;
                    _currentBreakProgressHost = null;
                }
                catch { /* ignore：所有引用/CTS 操作本不抛，这里只是绝对兜底保证下面 RefreshSchedule 必执行 */ }

                //  (2) 【核心要求】任何情况下每 5s 全量重建时间表
                RefreshSchedule();
                // 【★ 修复：5s 硬刷新不生效（双重重建闪 0）】
                //  5s 硬刷新已无条件 RefreshSchedule（覆盖本 Tick 一切状态变化），若此处 stateOrBreakChanged 仍为 true
                //  （本 Tick 状态变化 / 时间跳变检测 / dateChanged 触发）→ 下方 needRefresh 会再 Post 一次 RefreshSchedule
                //  → 同 Tick 双重重建：进度条每次重建 Width 先归 0 再恢复，用户感知"5s 硬刷新不生效/进度条闪 0"。
                //  修复：无条件重建后立即置 false，仅保留 `onClass != indexValid` 的一致性兜底触发。
                stateOrBreakChanged = false;
            }

            // ======== 【用户：删除淡化渐变动画以外的所有动画 Ava】课间 EXIT 不再播放 250ms TranslateY/淡出，直接整表重建 ========
            //   原实现：Breaking→非Breaking 先 EXIT 250ms 动画再回调 RefreshSchedule。现改为同步：
            //   立即写 pending 快照 + 复位 EXIT/ENTER 标志 + 调 RefreshSchedule()；淡化 Opacity 1↔0 的保留不影响（课间行 UI 被从 Children 移除自然"消失"，淡化仅用于 Hover）。
            if (_wasBreakLastTickAv && !breaking && !_breakRowExitAnimatingAv &&
                _currentBreakRowVisualsAv != null && _currentBreakRowVisualsAv.Count > 0)
            {
                stateOrBreakChanged = false;
                try { _breakRowExitCtsAv?.Cancel(); } catch { /* ignore */ }
                try { _breakRowEnterCtsAv?.Cancel(); } catch { /* ignore */ }
                try
                {
                    var oldExit = System.Threading.Interlocked.Exchange(ref _breakRowExitCtsAv, null);
                    oldExit?.Dispose();
                } catch { /* ignore */ }
                try
                {
                    var oldEnter = System.Threading.Interlocked.Exchange(ref _breakRowEnterCtsAv, null);
                    oldEnter?.Dispose();
                } catch { /* ignore */ }
                _breakRowExitAnimatingAv = false;
                _wasBreakLastTickAv = false;
                _currentBreakRowVisualsAv = null;
                _currentBreakProgressIndicator = null;
                _currentBreakProgressHost = null;
                _lastRefreshStateCode = stateCode;
                _lastRefreshBreakStartTicks = breakStartTicks;
                _lastRefreshBreakEndTicks = breakEndTicks;
                try { RefreshSchedule(); } catch { /* ignore */ }
            }

            // ---- 时间表状态变化 → 触发 RefreshSchedule 重建高亮行与进度条位置 ----
            //  连续上课（C1→C2）：课程行总在列表里，高亮/进度条切换由 5s 硬刷新兜底，无需额外 LayoutItem 比对；
            //  连续课间（B1→B2）：由上方 breaking 超时触发源 + RefreshSchedule 真实时间空隙定位即时切换。
            bool needRefresh = stateOrBreakChanged;
            bool indexValid = _currentOnClassIndex >= 0 && _currentOnClassIndex < _currentClassRows.Count;
            if (!needRefresh && onClass != indexValid)
            {
                // 上课状态与索引存在性不一致（例如：进入上课但仍没高亮索引，或下课了还有进度条）
                needRefresh = true;
            }
            // EXIT 动画窗口期间：任何 needRefresh（break indicator 非空触发的、状态不一致触发的等）全部延后到动画结束回调，
            //  避免课间行控件在动画播放途中被提前销毁导致"动画半截突然消失"。
            if (_breakRowExitAnimatingAv)
                needRefresh = false;

            if (needRefresh)
            {
                // ===== 把 RefreshSchedule + 本次进度计算 + ApplyProgressRatioAv 打包成**同一个** Dispatcher Post 连续执行 =====
                //   原因 1 (之前 bug)：RefreshSchedule 在 Post A，Apply 逻辑在 return 后立刻跑 BEFORE Post A，导致"本 tick Apply 作用到旧 UI/旧 ratio"；
                //   原因 2 (用户报告卡 100%)：课间→上课 tick 前一帧结束时，break progress 已 = 1.0（Width 已满容器）。
                //      若 RefreshSchedule 后没"立刻写新 UI 的正确 ratio"，新进度条 Width 保持构造默认 0，下一帧才补。
                //      加上旧 ApplyProgressRatioAv 的 LayoutUpdated 在 Bounds.Width<=0 时直接 return 却已 -= 事件，导致新 UI 永远拿不到回调。
                //      现在：LayoutUpdated 只有**成功写 Width**才 -=；并在 Post 中 RefreshSchedule 紧接就写 ratio（双保险）。
                int capturedStateCode = stateCode;
                long capturedBreakStartTicks = breakStartTicks;
                long capturedBreakEndTicks = breakEndTicks;
                bool capturedOnClass = onClass;
                bool capturedBreaking = breaking;

                Dispatcher.UIThread.Post(() =>
                {
                    // 在 RefreshSchedule 将要执行前把缓存同步好，避免 Post 排队期间下一帧再次命中 needRefresh 造成重复刷新。
                    _lastRefreshStateCode = capturedStateCode;
                    _lastRefreshBreakStartTicks = capturedBreakStartTicks;
                    _lastRefreshBreakEndTicks = capturedBreakEndTicks;
                    RefreshSchedule();

                    // ===== RefreshSchedule 同步完成后：立刻 Apply 一次"本帧真实 ratio" =====
                    //   注意：SDK 状态/时间可能在 Post 排队间隔中再变，故**必须重取**再算，不能用外面抓包的值（那样会在跨边界时错）。
                    try
                    {
                        var svc = _lessonsService;
                        if (svc == null) return;
                        var s2Raw = ReflectProp(svc, "CurrentState");
                        TimeState s2 = s2Raw != null ? (TimeState)s2Raw : TimeState.None;
                        bool onClass2 = s2 == TimeState.OnClass;
                        bool breaking2 = s2 == TimeState.Breaking;

                        // -- 课间进度条（真实时间定位当前课间空隙，SDK 滞后时也能正确推进/重置）--
                        if (breaking2)
                        {
                            var n = GetClassIslandNow().TimeOfDay;
                            var biB2 = FindBreakItemByRealTimeAv(n, out var gsB2, out var geB2);
                            if (biB2 != null && _currentBreakProgressIndicator != null)
                            {
                                double totalB = (geB2 - gsB2).TotalSeconds;
                                if (totalB > 0)
                                {
                                    double ratioB = Math.Clamp((n - gsB2).TotalSeconds / totalB, 0.0, 1.0);
                                    ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, ratioB);
                                }
                                else ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                            }
                            else if (_currentBreakProgressIndicator != null)
                            {
                                ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                            }
                        }
                        else if (_currentBreakProgressIndicator != null)
                        {
                            ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                        }

                        // -- 当前课进度条（用高亮行区间 + 真实时间，不依赖 SDK 滞后 item）--
                        if (onClass2)
                        {
                            // RefreshSchedule 刚执行完，_currentOnClassIndex 已含真实时间兜底（onClass 时非 -1）
                            if (_currentOnClassIndex >= 0 && _currentOnClassIndex < _currentClassRows.Count &&
                                _currentProgressIndicator != null)
                            {
                                var rowLi = _currentClassRows[_currentOnClassIndex].LayoutItem;
                                var start = ReflectGetStartTime(rowLi);
                                var end = ReflectGetEndTime(rowLi);
                                double total = (end - start).TotalSeconds;
                                if (total <= 0) { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); return; }
                                var n = GetClassIslandNow().TimeOfDay;
                                double prog = Math.Clamp((n - start).TotalSeconds / total, 0.0, 1.0);
                                ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, prog);
                            }
                            else { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); }
                        }
                        else
                        {
                            ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                        }
                    }
                    catch { /* 安全忽略：下一帧 UpdateProgress 正常会兜底继续写 */ }
                });

                // 关键：needRefresh 分支不再对"旧 UI"再 Apply 任何东西（RefreshSchedule 马上重建）。直接 return。
                return;
            }

            // ========== 课间进度条（Breaking 命中时：真实时间定位当前课间项计算百分比，每个课间分别走进度） ==========
            if (breaking && _currentBreakProgressIndicator != null)
            {
                // 复用 UpdateProgress 开头已拿到的 now（含最新 DebugTimeOffset 偏移）
                var nn = now.TimeOfDay;
                var biCur = FindBreakItemByRealTimeAv(nn, out var breakStart, out var breakEnd);
                if (biCur != null)
                {
                    var breakTotal = (breakEnd - breakStart).TotalSeconds;
                    if (breakTotal > 0)
                    {
                        var bElapsed = (nn - breakStart).TotalSeconds;
                        double ratio = Math.Clamp(bElapsed / breakTotal, 0.0, 1.0);
                        ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, ratio);
                    }
                    else
                    {
                        ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                    }
                }
                else
                {
                    // 真实时间不在任何课间项（SDK Breaking 但实际已到上课/放学）→ 课间进度条 0
                    ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                }
            }
            else if (_currentBreakProgressIndicator != null)
            {
                ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
            }

            // ========== 当前课进度条（OnClass 命中时，用高亮行区间 + 真实时间，不依赖 SDK 滞后 item） ==========
            if (_currentProgressIndicator == null || !onClass ||
                _currentOnClassIndex < 0 || _currentOnClassIndex >= _currentClassRows.Count)
            {
                ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                return;
            }

            var rowLiCur = _currentClassRows[_currentOnClassIndex].LayoutItem;
            var start = ReflectGetStartTime(rowLiCur);
            var end = ReflectGetEndTime(rowLiCur);
            var total = (end - start).TotalSeconds;
            if (total <= 0) { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); return; }

            var now2 = GetClassIslandNow().TimeOfDay;
            var elapsed = (now2 - start).TotalSeconds;
            var progress = Math.Clamp(elapsed / total, 0.0, 1.0);
            ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, progress);
        }
        catch { /* 安全忽略 */ }
        finally
        {
            if (!_breakRowExitAnimatingAv)
                _wasBreakLastTickAv = breaking;
        }
    }

    // ===================== 课间行 ENTER / EXIT 动画辅助（Avalonia Animation，纯代码不依赖主题资源）=====================
    /// <summary>
    /// 为一组课间行控件（左名/右时/底部进度 host）统一设置 RenderTransform = TranslateTransform，
    /// 保证后续 TranslateY 动画可用；Avalonia Visual 默认 RenderTransform=null，必须先赋值否则对 (TranslateTransform.Y) 的动画会抛 NRE。
    /// </summary>
    private static void EnsureBreakRowTransformAv(IEnumerable<Control> elements)
    {
        foreach (var el in elements)
        {
            if (el.RenderTransform is not TranslateTransform)
                el.RenderTransform = new TranslateTransform(0, 0);
        }
    }

    /// <summary>
    /// ENTER：Opacity 0 → 1 + TranslateY -24 → 0（CubicEaseOut，250ms）。
    /// 动画结束后本地值写回 Opacity / Y，避免 Avalonia 动画值 HoldEnd 优先级高于本地值导致后续刷新失效。
    /// </summary>
    private static async Task AnimateBreakRowEnterAv(List<Control> elements, CancellationToken ct)
    {
        if (elements.Count == 0) return;
        // 【修复：课间向上位移 0.5-1s】Ava 端取消路径：在写 Opacity=0/TT.Y=-24 起点之前抛。
        //   —— 与 WPF AnimateBreakRowEnterWpf 首行 ct.IsCancellationRequested 完全同构。
        //      只要 RefreshSchedule #N 重建前 Cancel ENTER CTS → 本 FireEnter 到这里立刻走 OperationCanceled
        //      → 外层 catch 吞 → 不再写 -24/0 → 不闪烁位移。
        if (ct.IsCancellationRequested) { throw new OperationCanceledException(ct); }
        EnsureBreakRowTransformAv(elements);

        // 统一先落地"动画起点"本地值，避免第 0 帧闪现控件默认 Opacity=1/Y=0
        foreach (var el in elements)
        {
            // 循环途中（理论上 UI 线程不会被抢，但绝对兜底）也检查
            if (ct.IsCancellationRequested)
            {
                // 走到一半 Cancel：立即把已经落到起点的元素拉回最终态 1/0（防"一半位移一半不位移"中间态残留）
                foreach (var e in elements)
                {
                    try
                    {
                        e.Opacity = 1.0;
                        if (e.RenderTransform is TranslateTransform ttR) ttR.Y = 0.0;
                    }
                    catch { /* ignore */ }
                }
                throw new OperationCanceledException(ct);
            }
            el.Opacity = 0.0;
            if (el.RenderTransform is TranslateTransform tt) tt.Y = BreakRowEnterTranslatePxAv;
        }

        var duration = TimeSpan.FromMilliseconds(BreakRowAnimationMsAv);
        var easeOut = new CubicEaseOut();
        var opacityAnim = new Avalonia.Animation.Animation
        {
            Duration = duration,
            Easing = easeOut,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    KeyTime = TimeSpan.Zero,
                    Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.0 } }
                },
                new KeyFrame
                {
                    KeyTime = duration,
                    Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
                },
            }
        };
        var translateYAnim = new Avalonia.Animation.Animation
        {
            Duration = duration,
            Easing = easeOut,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    KeyTime = TimeSpan.Zero,
                    Setters = { new Setter { Property = TranslateTransform.YProperty, Value = BreakRowEnterTranslatePxAv } }
                },
                new KeyFrame
                {
                    KeyTime = duration,
                    Setters = { new Setter { Property = TranslateTransform.YProperty, Value = 0.0 } }
                },
            }
        };

        var tasks = new List<Task>(capacity: elements.Count * 2);
        try
        {
            foreach (var el in elements)
            {
                ct.ThrowIfCancellationRequested();
                var tt = (TranslateTransform)el.RenderTransform!;
                tasks.Add(opacityAnim.RunAsync(el, ct));
                tasks.Add(translateYAnim.RunAsync(tt, ct));
            }
            // 250ms + 20ms 兜底：防止 RunAsync 在极端渲染阻塞下不返回
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(BreakRowAnimationMsAv + 40));
            try { await Task.WhenAll(tasks).WaitAsync(cts.Token); } catch (OperationCanceledException) { /* 正常结束或取消 */ }
        }
        catch (OperationCanceledException) { /* 用户取消：落地默认终态即可 */ }

        // 动画结束（或取消/异常）：清动画时钟 + 本地值写回，防止后续 RefreshSchedule / UpdateProgress 写本地值被动画优先级吞噬
        foreach (var el in elements)
        {
            el.Opacity = 1.0;
            if (el.RenderTransform is TranslateTransform tt) tt.Y = 0.0;
        }
    }

    /// <summary>
    /// EXIT：Opacity 当前值 → 0 + TranslateY 当前值 → +24（CubicEaseIn，250ms）。
    /// 外部传入 CTS 可由"下一次 needRefresh 强制刷新"触发提前终止（例如用户手动换天/手动触发整表重建）。
    /// </summary>
    private async Task AnimateBreakRowExitAv(List<Control> elements, CancellationToken ct, Action onCompleted)
    {
        bool done = false;
        void Finish()
        {
            if (done) return; done = true;
            // 本地值落地：动画结束控件应该在"全透明 + 已滑到最下方"，后续 RefreshSchedule 会销毁它们；
            // 但仍要写本地值 + 取消动画引用，避免极端下 Reuse/缓存 UI 继承动画状态。
            foreach (var el in elements)
            {
                try { el.Opacity = 0.0; } catch { /* ignore */ }
                if (el.RenderTransform is TranslateTransform tt) { try { tt.Y = BreakRowExitTranslatePxAv; } catch { /* ignore */ } }
            }
            onCompleted?.Invoke();
        }

        try
        {
            EnsureBreakRowTransformAv(elements);
            var duration = TimeSpan.FromMilliseconds(BreakRowAnimationMsAv);
            var easeIn = new CubicEaseIn();

            var opacityAnim = new Avalonia.Animation.Animation
            {
                Duration = duration,
                Easing = easeIn,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        KeyTime = TimeSpan.Zero,
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
                    },
                    new KeyFrame
                    {
                        KeyTime = duration,
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.0 } }
                    },
                }
            };
            var translateYAnim = new Avalonia.Animation.Animation
            {
                Duration = duration,
                Easing = easeIn,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        KeyTime = TimeSpan.Zero,
                        Setters = { new Setter { Property = TranslateTransform.YProperty, Value = 0.0 } }
                    },
                    new KeyFrame
                    {
                        KeyTime = duration,
                        Setters = { new Setter { Property = TranslateTransform.YProperty, Value = BreakRowExitTranslatePxAv } }
                    },
                }
            };

            var tasks = new List<Task>(capacity: elements.Count * 2);
            foreach (var el in elements)
            {
                ct.ThrowIfCancellationRequested();
                var tt = (TranslateTransform)el.RenderTransform!;
                tasks.Add(opacityAnim.RunAsync(el, ct));
                tasks.Add(translateYAnim.RunAsync(tt, ct));
            }
            // Double guard: Task.WhenAll + 250ms+20ms Delay，任一先到都 Finish
            var delay = Task.Delay(TimeSpan.FromMilliseconds(BreakRowAnimationMsAv + 20), ct);
            var whenAll = Task.WhenAll(tasks);
            var finished = await Task.WhenAny(whenAll, delay);
            try { await finished; } catch (OperationCanceledException) { /* 取消 */ }
            Finish();
        }
        catch (OperationCanceledException)
        {
            Finish();
        }
        catch
        {
            Finish();
        }
    }
}
