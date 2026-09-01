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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 悬浮时间表服务：在桌面放置半透明悬浮窗显示当前课表
/// </summary>
public class FloatingScheduleService : IDisposable, IHostedService
{
    private readonly PluginSettings _settings;
    private readonly ILogger<FloatingScheduleService> _logger;
    private Window? _window;
    private Border? _containerBorder;
    private Grid? _tableGrid;
    // 【修复：进度条有时总是满的】
    //  根因：使用 WPF ProgressBar + 手写 ControlTemplate 时，PART_Indicator.Width 需要依赖 "PART_Track 实际宽度 * (Value-Min)/Range"
    //         的内部 UpdateIndicator 机制，而 FrameworkElementFactory 构建出的命名部件结构在部分 DPI / 首次布局时序下，
    //         PART_Indicator 要么继承 Grid.Stretch 永远满宽，要么 Width 永远 0。
    //  修复方案：彻底放弃 ProgressBar 控件，改为自绘 2 层 Border（背景铺满 + 前景左对齐）。
    //         前景 Border 的 Width 在 UpdateProgress 里手动按 "ActualWidth * ratio" 赋值，100% 可控、不依赖 WPF 控件内部机制。
    private Border? _currentProgressIndicator;           // 当前上课行进度条 —— 前景（填充条）Border
    private FrameworkElement? _currentProgressHost;      // 当前上课行进度条 —— 承载容器（通常是外层 Grid 含 前景+背景 两 Border，用于 ActualWidth 取基准）
    private Border? _currentBreakProgressIndicator;      // 当前课间行进度条 —— 前景
    private FrameworkElement? _currentBreakProgressHost; // 当前课间行进度条 —— 承载容器
    private int _currentProgressClassIndex = -1;
    private List<object> _currentRowLayoutItems = new List<object>();
    private object? _currentBreakLayoutItem;            // Breaking 命中时的 SDK 课间 LayoutItem（UpdateProgress 计算百分比用）
    // 【★ 连续课间分别走进度】课间项列表（TimeType==1，按 Start 升序），RefreshSchedule 构建时从 classPlan 收集。
    //  连续课间 B1→B2→B3（首尾相接）时，课对空隙 = 总长度（进度条会走总长度）。
    //  用本列表按"now ∈ 哪个课间项的 [Start, End)"定位 → 每个课间分别走进度。
    private readonly List<object> _breakItemsWpf = new List<object>();
    // 【课表分隔线·档案组件】档案中用户手动插入的"分隔线"对象（TimeType==2，StartTime==EndTime 时间点语义），
    //  按 Start 升序；RefreshSchedule 构建时从 classPlan 收集。分隔线只画在档案定义的位置（不是每个课间都画）。
    private readonly List<object> _separatorItemsWpf = new List<object>();
    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed;
    private CancellationTokenSource? _startRetryCts;

    // 拖拽移动相关（用户文档 二.5 方案A：交给系统原生 HTCAPTION 拖拽，彻底简化；只保留必要的"前/后冻结"状态）
    private SizeToContent _preDragSizeToContent;
    private bool _preDragTopmost;
    private bool _allowClose;

    // Win32 API 用于置底
    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_TOP = new IntPtr(0);
    private const UInt32 SWP_NOSIZE = 0x0001;
    private const UInt32 SWP_NOMOVE = 0x0002;
    private const UInt32 SWP_NOACTIVATE = 0x0010;
    private const UInt32 SWP_SHOWWINDOW = 0x0040;
    private const UInt32 SWP_NOZORDER = 0x0004;
    private const UInt32 SWP_NOSENDCHANGING = 0x0400; // 禁止窗口收到 WM_WINDOWPOSCHANGING/NCHITTEST 回调，避免回跳
    // 【★ 彻底置底】SWP_NOOWNERZORDER/SWP_NOREPOSITION = 0x0200（同值），防止 owner 窗口被连带重排；对齐 ClassIsland Bottommost。
    private const UInt32 SWP_NOOWNERZORDER = 0x0200;
    private const UInt32 SWP_NOREPOSITION = 0x0200;
    // 【★ 彻底置底】WS_EX_NOACTIVATE = 0x08000000：置底窗口点击/拖拽不激活 → 永不被 Windows 提升 z-order
    //  （激活提升是置底失效主因：即使每 1ms 重设也压不住，DispatcherTimer 1ms 实际受系统时钟分辨率 ~15.6ms 限制）。
    private const int WS_EX_NOACTIVATE = 0x08000000;

    // ========== 悬浮窗层级重设频率（Mode=0 OnWindowZOrderChanged）Win32 子类化 ==========
    private const int GWLP_WNDPROC_WPF = -4;
    private const uint WM_WINDOWPOSCHANGED_WPF = 0x0047;
    private delegate IntPtr TopmostWndProcWpf(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    // 【修复 Issue 2】32/64 位双 EntryPoint 兼容包装：
    //   64 位：user32.dll 导出 SetWindowLongPtrW/A，用 SetWindowLongPtr；
    //   32 位：SetWindowLongPtr 是 C 宏（不导出），等价于 SetWindowLong（IntPtr 返回签名的 EntryPoint 可正确保存指针大小的旧 WndProc 值）。
    //  两个 DllImport EntryPoint 均使用 IntPtr 返回（与 nint/native int 隐式互转），Attach/Detach 四调用点保持 SetWindowLongPtrWpf(...) 名称不变。
    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLong32Wpf(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64Wpf(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    private static IntPtr SetWindowLongPtrWpf(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64Wpf(hWnd, nIndex, dwNewLong)
            : SetWindowLong32Wpf(hWnd, nIndex, dwNewLong);
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // 【修复闪烁·z-order 条件断言】GetWindow(GW_HWNDFIRST/GW_HWNDLAST)：判断本窗口当前是否已处于
    //  topmost/bottommost 链首/末位。稳态时定时器 Tick 跳过 SetWindowPos，消除"每 50ms/1ms 重设
    //  z-order → DWM 重合成 → 闪烁"（同 Avalonia 端）。
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    private const uint GW_HWNDFIRST_WPF = 0;
    private const uint GW_HWNDLAST_WPF = 2;

    // Per-Monitor V2 DPI 兜底：防止在宿主 DPI 上下文未正确继承时出现缩放错位
    // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")]
    private static extern bool SetWindowDpiAwarenessContext(IntPtr hWnd, IntPtr value);
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    // 用户文档 要点6：WS_EX_COMPOSITED 启用 DWM 双缓冲合成（半透明悬浮窗拖动必加）
    //                 WS_EX_LAYERED    分层窗口标志（AllowsTransparency=true 已经是分层窗，显性设置给 DWM 明确提示）
    private const int WS_EX_COMPOSITED = 0x02000000;
    private const int WS_EX_LAYERED    = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;  // 点击穿透：系统命中测试直接跳过本窗口

    // ========== SDK API 兼容反射辅助方法（兼容不同版本 ClassIsland 1.x SDK 属性名差异）==========
    private static TimeSpan ReflectGetStartTime(object item)
    {
        if (item == null) return TimeSpan.Zero;
        var t = item.GetType();
        // 优先尝试 StartTime (TimeSpan)
        var pi = t.GetProperty("StartTime", BindingFlags.Instance | BindingFlags.Public);
        if (pi != null && pi.PropertyType == typeof(TimeSpan))
        {
            var v = pi.GetValue(item);
            if (v != null) return (TimeSpan)v;
        }
        // 回退 StartSecond (string → 秒数 → TimeSpan)
        var pi2 = t.GetProperty("StartSecond", BindingFlags.Instance | BindingFlags.Public);
        if (pi2 != null)
        {
            var v = pi2.GetValue(item)?.ToString();
            if (int.TryParse(v, out var sec)) return TimeSpan.FromSeconds(sec);
        }
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
        if (pi2 != null)
        {
            var v = pi2.GetValue(item)?.ToString();
            if (int.TryParse(v, out var sec)) return TimeSpan.FromSeconds(sec);
        }
        return TimeSpan.Zero;
    }

    // 【★ 连续课间即时切换】用真实时间 now 定位"当前课间空隙"（相邻课对之间，now ∈ [C_i.End, C_{i+1}.Start)）
    //  返回前一课索引 i（在其后插入课间行）；找不到返回 -1。out 返回空隙时间区间。
    private int FindBreakGapIndexByRealTimeWpf(TimeSpan now, out TimeSpan gapStart, out TimeSpan gapEnd)
    {
        gapStart = default;
        gapEnd = default;
        for (int i = 0; i < _currentRowLayoutItems.Count - 1; i++)
        {
            var cEnd = ReflectGetEndTime(_currentRowLayoutItems[i]);
            var cNextStart = ReflectGetStartTime(_currentRowLayoutItems[i + 1]);
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

    // 【★ 连续课间分别走进度】用真实时间 now 在 _breakItemsWpf 中定位"now ∈ [Start, End)"的课间项。
    //  连续课间 B1→B2→B3（首尾相接）各自独立区间 → 每个课间分别走进度（而非课对空隙总长度）。
    //  返回命中的课间项；找不到返回 null。out 返回该课间项的时间区间。
    private object? FindBreakItemByRealTimeWpf(TimeSpan now, out TimeSpan bs, out TimeSpan be)
    {
        bs = default;
        be = default;
        foreach (var b in _breakItemsWpf)
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
        var t = item.GetType();
        var pi = t.GetProperty("TimeType", BindingFlags.Instance | BindingFlags.Public);
        if (pi != null && pi.PropertyType == typeof(int))
        {
            return (int)(pi.GetValue(item) ?? 0);
        }
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

    private static object? ReflectGetCurrentTimeLayoutItem(object classInfo)
    {
        if (classInfo == null) return null;
        var t = classInfo.GetType();
        var pi = t.GetProperty("CurrentTimeLayoutItem", BindingFlags.Instance | BindingFlags.Public);
        return pi?.GetValue(classInfo);
    }

    private static Dictionary<Guid, Subject> ReflectBuildSubjectsMap(object subjectsDict)
    {
        var result = new Dictionary<Guid, Subject>();
        if (subjectsDict == null) return result;
        var enumerable = subjectsDict as IEnumerable;
        if (enumerable == null) return result;
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
            Guid g;
            if (rawKey is Guid gg) g = gg;
            else if (Guid.TryParse(rawKey?.ToString(), out var parsed)) g = parsed;
            else continue;
            result[g] = val;
        }
        return result;
    }

    private static ArrayList ReflectGetValidTimeLayoutItems(object classPlan)
    {
        var list = new ArrayList();
        if (classPlan == null) return list;
        var t = classPlan.GetType();
        var pi = t.GetProperty("ValidTimeLayoutItems", BindingFlags.Instance | BindingFlags.Public);
        if (pi == null) return list;
        var enumerable = pi.GetValue(classPlan) as IEnumerable;
        if (enumerable == null) return list;
        foreach (var x in enumerable) list.Add(x);
        return list;
    }

    private static IList ReflectGetClasses(object classPlan)
    {
        if (classPlan == null) return Array.Empty<object>();
        var t = classPlan.GetType();
        var pi = t.GetProperty("Classes", BindingFlags.Instance | BindingFlags.Public);
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

    private static Guid ReflectGetSubjectId(object classInfo)
    {
        if (classInfo == null) return Guid.Empty;
        var t = classInfo.GetType();
        var pi = t.GetProperty("SubjectId", BindingFlags.Instance | BindingFlags.Public);
        if (pi == null) return Guid.Empty;
        var v = pi.GetValue(classInfo);
        if (v is Guid g) return g;
        if (Guid.TryParse(v?.ToString(), out var parsed)) return parsed;
        return Guid.Empty;
    }

    private static bool ReflectGetIsEnabled(object classInfo)
    {
        if (classInfo == null) return false;
        var t = classInfo.GetType();
        var pi = t.GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public);
        if (pi != null && pi.PropertyType == typeof(bool))
        {
            return (bool)(pi.GetValue(classInfo) ?? true);
        }
        return true;
    }

    private static object? ReflectGetCurrentTimeLayoutItemService(object lessonsService)
    {
        if (lessonsService == null) return null;
        var t = lessonsService.GetType();
        var pi = t.GetProperty("CurrentTimeLayoutItem", BindingFlags.Instance | BindingFlags.Public);
        return pi?.GetValue(lessonsService);
    }

    private static string FormatHhMm(TimeSpan ts) => $"{ts.Hours:D2}:{ts.Minutes:D2}";

    public FloatingScheduleService(PluginSettings settings, ILogger<FloatingScheduleService> logger)
    {
        _settings = settings;
        _logger = logger;
        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += OnRefreshTimerTick;

        // ========== 指针移入淡化：单独高频轮询计时器，解决原 500ms 与进度刷新共用 Timer 的延迟问题 ==========
        //  50ms 粒度：≈ 人眼 20Hz 感知下限，远高于原 500ms (2Hz)；同时为 250ms 动画留出至少 5 帧步长保证过渡顺滑。
        //  判定不准确的根因 2：原无去抖 —— Windows DWM 合成/多监视器移动光标时，屏幕坐标会在边界处 ±1 抖动，
        //   导致 IsPointerInWindowWpf 在窗口边缘"进→出→进→出"抖动。因此引入 _fadeStableCount：
        //   —— 连续 N 次命中同方向 → 正式切换 faded；阈值 FadeStableThresholdTicks（3 次 = 150ms，仍远快于旧 500ms）
        _hoverFadeTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _hoverFadeTimer.Tick += (_, _) =>
        {
            ApplyHoverFade();
            // 【贴边自动隐藏】复用同一 50ms 轮询推进状态机（不新增计时器）
            try { UpdateEdgeHideWpf(); } catch { /* ignore */ }
        };
    }

    // 指针淡化高频轮询（DispatcherPriority.Normal：比后台进度刷新高，保证鼠标交互优先响应）
    private readonly DispatcherTimer _hoverFadeTimer;
    private const int FadeStableThresholdTicks = 3;     // 50ms × 3 = 150ms 稳定判定阈值（消除 ±1 像素抖动）
    private int _fadeStableCount;                       // 连续多少 Tick 观察到同一个"faded"状态
    private bool _fadeLastObserved;                     // 上一次观察到的 faded 值（用于累加稳定计数）

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startRetryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = TryStartWithRetryAsync(_startRetryCts.Token);
        return Task.CompletedTask;
    }

    private async Task TryStartWithRetryAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (cancellationToken.IsCancellationRequested) return;

            // 等待宿主就绪并且 ILessonsService / IProfileService 可解析
            var lessonsService = IAppHost.TryGetService<ILessonsService>();
            var profileService = IAppHost.TryGetService<IProfileService>();
            if (IAppHost.Host != null && lessonsService != null && profileService != null)
            {
                StartInternal();
                _logger.LogInformation("悬浮时间表服务已启动");
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        _logger.LogWarning("悬浮时间表服务未能在允许次数内就绪，可能部分服务不可用；仍然启动以监听设置变更。");
        StartInternal();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _startRetryCts?.Cancel();
            Stop();
        }
        catch { }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 启动服务，根据设置决定是否显示窗口
    /// </summary>
    private void StartInternal()
    {
        if (_disposed) return;

        _settings.PropertyChanged += OnSettingsPropertyChanged;
        ThemeHelper.ThemeChanged += OnThemeChanged;

        if (_settings.EnableFloatingSchedule)
        {
            // TryStartWithRetryAsync 是从异步 Task.Run/后台 Task 调用过来的（StartAsync fire-and-forget）。
            // 直接在后台线程 new Window / Show 会被 WPF Dispatcher 静默失败，或窗口 Show 后没有激活渲染 → 用户看到"开关打开但悬浮窗不出现，只有重新 toggle 开关触发 OnPropertyChanged 在 UI 线程才生效"。
            // 修复：确保所有 UI 操作走 _window.Dispatcher (Application.Current.Dispatcher 兜底)。
            void RunOnUiThread(Action action)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher
                                 ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
                if (dispatcher == null || dispatcher.CheckAccess()) action();
                else dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Normal);
            }

            RunOnUiThread(() =>
            {
                if (_disposed) return;
                EnsureWindow();        // new Window + _window.Content = _containerBorder（必须 UI 线程）
                RefreshSchedule();     // 填充 _containerBorder.Child 内容（含背景刷 Alpha）
                _refreshTimer.Start(); // 进度更新 Timer（DispatcherTimer，本身需 UI 线程）
                ApplyWindowLayer();    // Topmost/Bottom 先设一次（Show 前仅 Win32 SetWindowPos 不生效，但 Topmost 属性会被构造函数覆盖作为 fallback）
                ShowWindow();          // Show：使 hwnd 存在，SizeToContent 按内容计算窗口尺寸
                ApplyWindowLayer();    // Show 后再次调用 Win32 SetWindowPos 才真正落实 z-order（HWND_TOPMOST/BOTTOM），否则"看起来没显示"其实跑到 z-order 底部被桌面图标挡住？
                // ===== Vibe Review Fix (Issue1) =====
                // ShowWindow 触发 OnWindowLoaded 时会直接写 exStyle |= WS_EX_TRANSPARENT（当 ClickThrough=true），
                //  但 OnWindowLoaded 内部同步 lastApplied 之前，我们这里再兜底调一次 ApplyClickThrough/ApplyHoverFade(true)
                //  保证：(a) 冷启动 ClickThrough=true 时 lastApplied 立刻与真实 hwnd 对齐；
                //        (b) 与 OnSettingsPropertyChanged EnableFloatingSchedule 分支行为完全对称，避免两处启动路径行为差异。
                ApplyClickThrough();
                ApplyHoverFade(force: true);
                // 显示后立即启动指针淡化高频轮询 Timer（之前是在 _refreshTimer 每 500ms 顺带判定，延迟太大）
                _hoverFadeTimer.Start();
                // 【修复：调试时间不立刻刷新】冷启动时也订阅宿主 Settings.PropertyChanged（与开关打开分支完全对称）
                EnsureHostSettingsSubscriptionWpf();
                // 【h4】悬浮窗层级重设频率 Attach：Show 后 hwnd 已建立，按 Settings 模式启动 4 路触发之一
                AttachTopmostRefreshWpf(_window!, _settings.FloatingScheduleTopmostRefreshMode);
            });
        }
    }

    /// <summary>
    /// 停止服务并关闭窗口
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        _settings.PropertyChanged -= OnSettingsPropertyChanged;
        ThemeHelper.ThemeChanged -= OnThemeChanged;
        // 【h4】悬浮窗层级重设频率 Detach：先解子类/停 Timer/退订宿主事件（hwnd 仍有效时先解 Win32 钩）
        DetachTopmostRefreshWpf();
        // 【修复：调试时间不立刻刷新 - 退订宿主 Settings PropertyChanged 防止内存泄漏】
        //  SettingsService 是宿主 singleton，若插件实例作为 PropertyChanged.target 被它持有，插件/悬浮窗会在 Stop 后无法 GC。
        DetachHostSettingsSubscriptionWpf();
        _refreshTimer.Stop();
        _hoverFadeTimer?.Stop();
        // 【5s 硬兜底复位（WPF）】Stop 清零计数，下次 EnableFloatingSchedule=true 从零开始同步
        _hardSyncTickCounterWpf = 0;
        // 【修复：课间向上位移 0.5-1s】Stop 时 Cancel ENTER/EXIT 动画飞在任务（EXIT 调清标志：与 5s 硬清理/DebugTime 清理完全同构）
        //   —— CloseWindow 前先停掉所有在跑 Storyboard（避免 window Closed 后 onCompleted 再 Post RefreshSchedule 炸）
        try { _breakRowEnterCtsWpf?.Cancel(); } catch { /* ignore */ }
        try
        {
            var oldEnter = System.Threading.Interlocked.Exchange(ref _breakRowEnterCtsWpf, null);
            oldEnter?.Dispose();
        } catch { /* ignore */ }
        _breakRowExitAnimatingWpf = false;
        CloseWindow();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.EnableFloatingSchedule))
        {
            // 防御性切 UI 线程：设置 UI 的 PropertyChanged 通常由 SettingsControl.IsOn 在 UI 线程触发，
            // 但兼容未来可能从外部后台线程直接写属性值的情况。
            void RunOnUi(Action action)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess()) action();
                else dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Normal);
            }

            if (_settings.EnableFloatingSchedule)
            {
                RunOnUi(() =>
                {
                    EnsureWindow();
                    RefreshSchedule();
                    _refreshTimer.Start();
                    ShowWindow();          // Show 先让 hwnd 建立、尺寸计算（SizeToContent=WidthAndHeight）
                    ApplyWindowLayer();    // Show 后置 Win32 SetWindowPos(HWND_BOTTOM/TOPMOST)，确保 z-order 正确
                    ApplyClickThrough();   // 打开时同步点击穿透状态
                    ApplyHoverFade(force: true); // 打开时同步指针淡化状态
                    _hoverFadeTimer.Start();  // 高频指针淡化判定独立计时器启动
                    // 【修复：调试时间不立刻刷新】开关打开时立刻订阅宿主 Settings.PropertyChanged
                    EnsureHostSettingsSubscriptionWpf();
                    // 【h4】开关打开：Attach 悬浮窗层级重设触发源（4 模式）
                    AttachTopmostRefreshWpf(_window!, _settings.FloatingScheduleTopmostRefreshMode);
                });
            }
            else
            {
                RunOnUi(() =>
                {
                    // 【h4】开关关闭：先 Detach Topmost 刷新触发源
                    DetachTopmostRefreshWpf();
                    // 【修复：调试时间不立刻刷新 - 防内存泄漏】开关关闭时先 Detach 宿主 Settings 订阅（SettingsService 是宿主 singleton）
                    DetachHostSettingsSubscriptionWpf();
                    _refreshTimer.Stop();
                    _hoverFadeTimer?.Stop();
                    HideWindow();
                });
            }
        }
        else if (e.PropertyName == nameof(PluginSettings.TimeOffsetSeconds))
        {
            // 【★ 时间跳变：插件全局时间偏移变化 → 立即 RefreshSchedule 重建】
            //  TimeBaseService.GetCurrentTime() 每次读最新偏移，但 FloatingScheduleService 不知道偏移变了；
            //  若不刷新，UpdateProgress 会用"旧高亮课 + 新时间"算进度条（继承旧进度/卡 100%）。
            //  刷新后高亮行真实时间定位切到目标课，RefreshSchedule 末尾集中 Apply 立即写新进度。
            void RunOnUi(Action action)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess()) action();
                else dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Normal);
            }
            RunOnUi(RefreshSchedule);
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleWindowLayer))
        {
            void RunOnUi(Action action)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess()) action();
                else dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Normal);
            }
            RunOnUi(ApplyWindowLayer);
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleTopmostRefreshMode))
        {
            // 模式切换：先 Detach 旧 → ReAttach 新（窗口存在时） → Apply 一次立即生效
            void RunOnUi(Action action)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess()) action();
                else dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Normal);
            }
            RunOnUi(() =>
            {
                DetachTopmostRefreshWpf();
                if (_window != null && _settings.EnableFloatingSchedule)
                {
                    AttachTopmostRefreshWpf(_window, _settings.FloatingScheduleTopmostRefreshMode);
                }
                ApplyWindowLayer();
            });
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleOpacity))
        {
            // 背景不透明度仅作用于卡片背景刷 Alpha，需整体刷新 UI 应用颜色；同时指针淡化需要重新应用 Opacity。
            RefreshSchedule();
            ApplyHoverFade(force: true);
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleFontScale))
        {
            RefreshSchedule();
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleEnableFullTeacherName))
        {
            RefreshSchedule();
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleClickThrough))
        {
            ApplyClickThrough();
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleEdgeHide))
        {
            // 贴边自动隐藏开关变更：即时响应（UI 线程）——关闭恢复位置并停止逻辑，开启立即评估贴边
            void RunOnUi(Action action)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess()) action();
                else dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Normal);
            }
            RunOnUi(() =>
            {
                if (!_settings.FloatingScheduleEdgeHide) RestoreEdgePositionWpf();
                else UpdateEdgeHideWpf();
            });
        }
        else if (e.PropertyName == nameof(PluginSettings.FloatingScheduleHoverFade) ||
                 e.PropertyName == nameof(PluginSettings.FloatingScheduleHoverFadeReverse))
        {
            ApplyHoverFade(force: true);
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_window != null && _window.IsVisible)
        {
            RefreshSchedule();
        }
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        try
        {
            UpdateProgress();
        }
        catch
        {
            // 防止计时器异常导致崩溃
        }
    }

    private void EnsureWindow()
    {
        if (_window != null) return;

        _allowClose = false;

        _window = new Window
        {
            Width = 1,
            Height = 1,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = _settings.FloatingScheduleWindowLayer == FloatingScheduleWindowLayer.Topmost,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            Left = _settings.FloatingSchedulePositionX,
            Top = _settings.FloatingSchedulePositionY,
            // ===== 修复：超长教师名把窗口撑成横条（用户最新要求：教师列必须完整展示 15 汉字）=====
            // 规格层 MaxWidth = 820 px 兜底（820 ≈ 外 24 + ColSpacing 14 + 课师Margin 16 + 课程 180 + 教师 322.5 + 时间 141 + 余量 22.5）
            // 分层：外层挡住野蛮生长；单一事实来源=教师列自己的 pt×系数（随 FontScale 缩放，展示层不回写规格层）。
            //   FontScale=32 → teacher=29pt×21.5=623.5 px；820 - 24-14-16-623.5-141 = 1.5 px → 课程列 = 0 但仍 ≥ 0
            //   FontScale=18 → teacher=15×21.5=322.5 px；课程列余 300+ px（14+ 字课程）
            MaxWidth = 820
        };

        _window.Loaded += OnWindowLoaded;
        _window.Closing += OnWindowClosing;
        _window.LocationChanged += OnWindowLocationChanged;

        _containerBorder = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 12),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 3,
                Opacity = 0.3,
                Color = Colors.Black
            }
        };

        // 允许拖拽整个容器区域（重置为「方案A：交给系统原生 HTCAPTION 拖拽」，Window.DragMove() 内部走系统拖拽链路，代码极简、无抖动）
        _containerBorder.MouseLeftButtonDown += OnContainerMouseLeftButtonDown;
        // 【修复触摸无法拖动】WPF 的 MouseLeftButtonDown 对触摸输入不触发（触摸走 Touch/Pointer 事件），
        //  触摸屏用户按下后 DragMove 链路根本不会启动 → 追加 Touch 事件手动实现拖拽。
        _containerBorder.TouchDown += OnContainerTouchDown;
        _containerBorder.TouchMove += OnContainerTouchMove;
        _containerBorder.TouchUp += OnContainerTouchUp;

        _window.Content = _containerBorder;

        ApplyOpacity();
        ApplyThemeColors();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 从 Alt+Tab 切换列表中隐藏；同时应用 用户文档 要点6 扩展样式：WS_EX_COMPOSITED | WS_EX_LAYERED
        //  —— WS_EX_COMPOSITED 让 DWM 对该 HWND 做双缓冲合成，拖动半透明窗口时减少闪烁/抖动；
        //  —— WS_EX_LAYERED    通知 DWM 这是分层透明窗，避免走旧的 GDI 重绘路径；
        //  —— WS_EX_TOOLWINDOW 继续保留，Alt+Tab 不显示；清除 WS_EX_APPWINDOW。
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(_window!).Handle;

            // 用户文档 要点2：为单个窗口显式声明 Per-Monitor V2 DPI 意识，避免宿主进程继承默认导致混合 DPI 漂移
            try { SetWindowDpiAwarenessContext(hwnd, DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { /* ignore */ }
            try { SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { /* ignore */ }

            int exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_COMPOSITED | WS_EX_LAYERED;
            // 已启用点击穿透则 Loaded 时立即加上 WS_EX_TRANSPARENT，避免先可交互再切穿透造成体验不一致
            if (_settings.FloatingScheduleClickThrough) exStyle |= WS_EX_TRANSPARENT;
            exStyle &= ~WS_EX_APPWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);

            // ===== Vibe Review Fix (Issue1, root cause) =====
            //  这里直接写了 TRANSPARENT 但不走 ApplyClickThrough 防抖分支，必须同步 last-applied 状态，
            //   否则用户冷启动 ClickThrough=true 时，ApplyClickThrough() 在 Show 前 hwnd=0 场景直接 return
            //   → Loaded 写入后 _lastWpfClickThroughApplied 仍为 false → 用户切 ClickThrough=false
            //   → ApplyClickThrough 判定 want(false)==last(false) 提前 return，WS_EX_TRANSPARENT 残留。
            _lastWpfClickThroughApplied = _settings.FloatingScheduleClickThrough;
        }
        catch
        {
            // 在部分环境下可能失败，忽略
        }

        // 加载完成后首次应用淡化状态（此时 ActualWidth / Win32 矩形已可用）
        try { ApplyHoverFade(force: true); } catch { /* ignore */ }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            if (_window != null && _window.IsVisible)
            {
                HideWindow();
            }
        }
    }

    private void OnWindowLocationChanged(object? sender, EventArgs e)
    {
        if (_window == null) return;
        // 【触摸/鼠标拖动不上报】拖拽过程中每帧写 Left/Top 都会触发本回调（每帧做 JSON 写盘 =
        //  触摸拖动卡顿/延迟的主要开销）。拖拽中直接返回不上报：
        //  - 触摸收尾（OnContainerTouchUp）会先复位 _touchDragIdWpf 再补应用尾帧 → 触发本回调保存最终位置；
        //  - 鼠标收尾 DragMove finally 复位 _mouseDraggingWpf 后再 ClampWindowToScreenWpf → 同样保存最终位置。
        if (_touchDragIdWpf >= 0 || _mouseDraggingWpf) return;

        // 保存位置；【贴边自动隐藏】仅当位置变化由"滑出/滑回动画"驱动（非用户拖拽）时，
        //  不持久化隐藏位、改写正常位，避免重启后窗口停在屏幕外；用户主动拖拽时保存实际位置。
        double lx = _window.Left, ty = _window.Top;
        if ((_edgeHiddenWpf || _edgeAnimatingWpf) && _touchDragIdWpf < 0 && !_mouseDraggingWpf)
        {
            lx = _edgeNormalLeftWpf;
            ty = _edgeNormalTopWpf;
        }
        _settings.FloatingSchedulePositionX = (int)Math.Round(lx);
        _settings.FloatingSchedulePositionY = (int)Math.Round(ty);
    }

    private void OnContainerMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_window == null || _containerBorder == null) return;

        // 【新增】点击穿透模式下，禁止 WPF 处理任何拖拽（否则 DragMove 会强制吞掉本应穿透的消息）
        if (_settings.FloatingScheduleClickThrough) return;
        // 【触摸/鼠标互斥】触摸拖拽进行中（触摸被提升为鼠标消息时两链路可能并行）：跳过，交给触摸链路
        if (_touchDragIdWpf >= 0) return;

        // ========== 用户文档 二.5 方案A（重置：彻底简化）：交给系统原生 HTCAPTION 拖拽 ==========
        //  为什么比之前的手动 SetWindowPos 方案好？
        //  1) 系统已经做好了 Per-Monitor V2 DPI、多屏跨 DPI、Snap 吸附兼容、DWM 合成时序优化（30 年 Win32 官方优化）；
        //  2) 代码量从 300+ 行复杂手动节流/缓存/捕获/DllImport → 降到 3 行核心：DragMove()
        //  3) 不再有"两套拖拽并行（文档二.5元凶）、双重缩放（文档一.3.2）、坐标单位冲突（文档一.2）、消息重入（文档二.6）"
        //  保留的有益优化：拖动前冻结 SizeToContent.Manual（防止 Auto 尺寸在拖的过程中做重测量造成合成抖动）
        _mouseDraggingWpf = true;   // 标记模态拖拽中：贴边隐藏状态机在此期间暂停
        // 【拖动期间不运行贴边隐藏倒计时】拖动开始即取消挂起的延迟滑出（到期时间戳标志清空）：
        //  否则延迟=0 时倒计时随时到期、把窗口滑出隐藏，与正在进行的拖动打架。
        //  拖动结束后 UpdateEdgeHideWpf 会按"松手位置"重新评估，非贴边则不隐藏。
        _edgeSlideOutPendingWpf = false;
        _preDragSizeToContent = _window.SizeToContent;
        _preDragTopmost = _window.Topmost;
        if (_preDragSizeToContent != SizeToContent.Manual) _window.SizeToContent = SizeToContent.Manual;

        try
        {
            // DragMove() 是同步阻塞方法，内部由 WPF 进入模态消息循环并调用系统 HTCAPTION 拖拽；
            //  当用户松开鼠标左键时 DragMove() 返回，后面紧接着执行"结束恢复"逻辑。
            _window.DragMove();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Window.DragMove() 提前退出（一般是按下后立即被其他窗口抢焦点或释放鼠标），安全忽略。");
        }
        finally
        {
            // 【健壮性】无论 DragMove 正常返回还是抛异常，都复位拖拽标志，避免贴边逻辑被永久暂停
            _mouseDraggingWpf = false;
        }

        // ===== DragMove 返回 = 一次拖动结束，恢复状态 =====
        try
        {
            if (_window.SizeToContent != _preDragSizeToContent) _window.SizeToContent = _preDragSizeToContent;
            if (_window.Topmost != _preDragTopmost) _window.Topmost = _preDragTopmost;
            // 结束后重新应用 z-order 层（DragMove 内部移动过程中不改变 z-order，结束后确保仍是设置里的 Topmost/Bottom）
            ApplyWindowLayer();
            // 【不出屏】系统 DragMove 允许把窗口拖出屏幕；返回后对最终位置 clamp 回所在屏幕工作区
            //  （隐藏/动画态跳过：那两种状态窗口本就该部分移出屏幕）
            ClampWindowToScreenWpf();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "拖动结束恢复 SizeToContent/Topmost/z-order 出错，忽略。");
        }

        // 拖拽结束 → 立即评估贴边自动隐藏
        try { UpdateEdgeHideWpf(); } catch { /* ignore */ }

        e.Handled = true;
    }

    // ========== 【修复触摸无法拖动】Touch 事件手动拖拽链路 ==========
    //  WPF 的 MouseLeftButtonDown 对触摸输入不触发（触摸走 Touch/Pointer 事件），
    //  因此为 _containerBorder 追加 TouchDown/TouchMove/TouchUp：
    //  TouchDown 记录触点相对窗口左上角的 DIP 偏移并 CaptureTouch；
    //  TouchMove 把触点屏幕坐标换算为 DIP（PresentationSource.TransformFromDevice）更新 Window.Left/Top；
    //  TouchUp 释放捕获并恢复 SizeToContent/z-order（与鼠标链路"冻结/恢复"语义一致）。
    //  鼠标 DragMove 链路保持原样不变。
    private int _touchDragIdWpf = -1;                 // 当前拖拽触摸点 Id（-1 = 无触摸拖拽）
    private double _touchOffsetXWpf;                  // 触点相对窗口左上角的 DIP 偏移 X
    private double _touchOffsetYWpf;                  // 触点相对窗口左上角的 DIP 偏移 Y
    private SizeToContent _preTouchSizeToContent;     // 触摸拖拽前 SizeToContent（结束后恢复）
    private bool _preTouchTopmost;                    // 触摸拖拽前 Topmost（结束后恢复）
    // 【拖动 CPU 优化 + 不出屏】触摸 Move 事件高频（120~240Hz），每事件写 Left/Top 触发布局+渲染 →
    //  节流到 ~60fps；尾帧记入 pending，TouchUp 时补应用。目标位置 clamp 到所在屏幕工作区（窗口完整可见）。
    private long _lastTouchMoveTickWpf;
    private double _pendingTouchLeftWpf, _pendingTouchTopWpf;
    private bool _pendingTouchValidWpf;
    // 【修复：触摸拖动后桌面图标/悬浮窗严重闪烁】拖动期间每 ~16ms 写 Left/Top 移动窗口，同时
    //  _topmostRefreshTimerWpf（50ms/1ms 定时）频繁 SetWindowPos(HWND_TOPMOST/BOTTOM) 重设 z-order，
    //  与拖动位移争抢 DWM 桌面合成 → 桌面反复重绘闪烁。拖动期间抑制 z-order/exstyle 重设，结束恢复。
    private bool _suppressTopmostRefreshWpf;

    private void OnContainerTouchDown(object sender, TouchEventArgs e)
    {
        if (_window == null || _containerBorder == null) return;
        // 点击穿透模式下禁止拖拽（与鼠标链路首行 return 一致）
        if (_settings.FloatingScheduleClickThrough) return;
        // 鼠标模态 DragMove 进行中（触摸被提升为鼠标消息时两链路可能并行）：跳过，交给鼠标链路
        if (_mouseDraggingWpf) return;
        // 已有触点在拖：忽略多指后续触点
        if (_touchDragIdWpf >= 0) return;

        try
        {
            var p = e.GetTouchPoint(_window).Position;
            _touchOffsetXWpf = p.X;
            _touchOffsetYWpf = p.Y;
            _touchDragIdWpf = e.TouchDevice.Id;
            _suppressTopmostRefreshWpf = true;   // 【修复：拖动闪烁】拖动期间抑制 z-order/exstyle 重设（TouchUp 恢复）
            // 【拖动期间不运行贴边隐藏倒计时】拖动开始取消挂起的延迟滑出（同鼠标链路，防 delay=0 与拖动打架）
            _edgeSlideOutPendingWpf = false;
            _lastTouchMoveTickWpf = 0;        // 【CPU 节流】首个 Move 事件立即应用
            _pendingTouchValidWpf = false;
            // 捕获触点：手指移出窗口边界仍能持续收到 Move/Up
            _containerBorder.CaptureTouch(e.TouchDevice);

            // 拖拽期间冻结 SizeToContent（与鼠标链路同样处理：防止 Auto 尺寸在拖的过程中重测量造成合成抖动）
            _preTouchSizeToContent = _window.SizeToContent;
            _preTouchTopmost = _window.Topmost;
            if (_preTouchSizeToContent != SizeToContent.Manual) _window.SizeToContent = SizeToContent.Manual;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TouchDown 启动触摸拖拽失败，安全忽略。");
            _touchDragIdWpf = -1;
        }
        e.Handled = true;
    }

    private void OnContainerTouchMove(object sender, TouchEventArgs e)
    {
        if (_touchDragIdWpf < 0 || _window == null) return;
        if (e.TouchDevice.Id != _touchDragIdWpf) return;
        try
        {
            // 触点相对窗口的 DIP 坐标 → PointToScreen 得屏幕物理像素 → TransformFromDevice 换算屏幕 DIP，
            // 再减去按下时记录的相对偏移即得窗口新位置（手指始终"按住"窗口内同一点）
            var rel = e.GetTouchPoint(_window).Position;
            var screenDevice = _window.PointToScreen(rel);
            var src = PresentationSource.FromVisual(_window);
            Point screenDip;
            if (src?.CompositionTarget != null)
                screenDip = src.CompositionTarget.TransformFromDevice.Transform(screenDevice);
            else
                screenDip = screenDevice;   // 兜底：拿不到 DPI 变换信息时按 1:1 处理
            double targetLeft = screenDip.X - _touchOffsetXWpf;
            double targetTop = screenDip.Y - _touchOffsetYWpf;
            ClampDragTargetWpf(ref targetLeft, ref targetTop);   // 【不出屏】限制在工作区内

            // 【CPU 节流】触摸 Move 高频（120~240Hz）→ 限制 ~60fps 写位置；尾帧记 pending，TouchUp 补应用
            long now = Environment.TickCount64;
            if (now - _lastTouchMoveTickWpf < 16)
            {
                _pendingTouchLeftWpf = targetLeft;
                _pendingTouchTopWpf = targetTop;
                _pendingTouchValidWpf = true;
                e.Handled = true;
                return;
            }
            _lastTouchMoveTickWpf = now;
            _pendingTouchValidWpf = false;
            _window.Left = targetLeft;
            _window.Top = targetTop;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TouchMove 移动窗口失败，安全忽略。");
        }
        e.Handled = true;
    }

    /// <summary>
    /// 【拖动不出屏】把拖拽目标位置（DIP）clamp 到窗口所在屏幕工作区，保证时间表完整显示在屏幕内。
    /// clamp 后窗口贴在工作区边缘（距离=0 &lt; 8px 阈值）→ 拖到边缘松手仍正常触发贴边滑出。
    /// 取不到工作区/尺寸信息时原样返回（功能降级但不阻塞拖动）。
    /// </summary>
    private void ClampDragTargetWpf(ref double left, ref double top)
    {
        try
        {
            if (_window == null) return;
            if (!TryGetWorkAreaDipWpf(out var wl, out var wt, out var wr, out var wb)) return;
            double w = _window.ActualWidth > 0 ? _window.ActualWidth : _window.Width;
            double h = _window.ActualHeight > 0 ? _window.ActualHeight : _window.Height;
            if (w <= 0 || h <= 0) return;
            double maxL = wl + Math.Max(0, wr - wl - w);
            double maxT = wt + Math.Max(0, wb - wt - h);
            left = Math.Clamp(left, wl, maxL);
            top = Math.Clamp(top, wt, maxT);
        }
        catch { /* 保持原值 */ }
    }

    /// <summary>
    /// 【不出屏】把窗口当前位置 clamp 回所在屏幕工作区（系统 DragMove 结束后调用）。
    /// 隐藏/滑移动画态跳过——那两种状态窗口本就部分移出屏幕，由贴边状态机管理。
    /// </summary>
    private void ClampWindowToScreenWpf()
    {
        try
        {
            if (_window == null) return;
            if (_edgeHiddenWpf || _edgeAnimatingWpf) return;
            double l = _window.Left, t = _window.Top;
            ClampDragTargetWpf(ref l, ref t);
            if (Math.Abs(l - _window.Left) > 0.5) _window.Left = l;
            if (Math.Abs(t - _window.Top) > 0.5) _window.Top = t;
        }
        catch { /* 忽略 */ }
    }

    private void OnContainerTouchUp(object sender, TouchEventArgs e)
    {
        if (_touchDragIdWpf < 0 || _window == null) return;
        if (e.TouchDevice.Id != _touchDragIdWpf) return;
        _touchDragIdWpf = -1;
        _suppressTopmostRefreshWpf = false;   // 【修复：拖动闪烁】恢复 z-order 刷新（紧随其后的 ApplyWindowLayer 会重设一次）

        try { _containerBorder?.ReleaseTouchCapture(e.TouchDevice); } catch { /* ignore */ }

        // 【CPU 节流】补应用被节流丢弃的尾帧位置，保证窗口最终停在手指松开处
        if (_pendingTouchValidWpf)
        {
            try { _window.Left = _pendingTouchLeftWpf; _window.Top = _pendingTouchTopWpf; } catch { }
            _pendingTouchValidWpf = false;
        }

        // 恢复触摸拖拽前状态（与鼠标 DragMove 结束恢复逻辑同构）
        try
        {
            if (_window.SizeToContent != _preTouchSizeToContent) _window.SizeToContent = _preTouchSizeToContent;
            if (_window.Topmost != _preTouchTopmost) _window.Topmost = _preTouchTopmost;
            ApplyWindowLayer();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "触摸拖拽结束恢复 SizeToContent/Topmost/z-order 出错，忽略。");
        }

        // 拖拽结束 → 立即评估贴边自动隐藏
        try { UpdateEdgeHideWpf(); } catch { /* ignore */ }
        e.Handled = true;
    }

    // ========== 贴边自动隐藏（FloatingScheduleEdgeHide）==========
    //  规则：窗口距所在屏幕工作区任一边缘 < 8px → 沿该边滑出，只留约 6px 可见条；
    //        光标进入可见条（= 隐藏态窗口矩形，屏幕内部分即 6px 条）→ 200ms 平移动画滑回；
    //        滑回后光标离开窗口区域 → 再次滑出隐藏。
    //  实现：复用 50ms 指针淡化轮询 Timer 做状态机推进（不新增计时器、不阻塞 UI）；
    //        动画用 DoubleAnimation 操作 Window.Left/Top；
    //        屏幕工作区优先 Win32 MonitorFromWindow+GetMonitorInfo（多屏正确，不引入 WinForms 依赖）。
    private const double EdgeHideNearThresholdDip = 1.0;   // 距屏幕边缘 < 1px 判定贴边（需真正贴到边才隐藏）
    private const double EdgeHideVisibleStripDip = 6.0;    // 隐藏后保留可见条约 6px
    private const int EdgeHideAnimMs = 200;                // 滑入/滑出平移动画时长

    private bool _edgeHiddenWpf;            // 当前是否处于"滑出隐藏"状态
    private int _edgeSideWpf;               // 贴靠边：0=无 1=左 2=右 3=上 4=下
    private double _edgeNormalLeftWpf;      // 隐藏前的正常位置 Left
    private double _edgeNormalTopWpf;       // 隐藏前的正常位置 Top
    private bool _edgeAnimatingWpf;         // 滑移动画进行中（轮询期间跳过，防重入）
    private bool _mouseDraggingWpf;         // 鼠标 DragMove 模态循环进行中（期间暂停贴边逻辑）
    // 【贴边隐藏延迟】判定"贴边且光标已离开"后，等待 FloatingScheduleEdgeHideDelay 秒再滑出隐藏
    //  （给用户移开光标/继续操作的时间；延迟期间光标回到窗口内或窗口被拖走则取消）。
    //  实现：50ms 轮询状态机 + 截止时间戳（不新增计时器）。
    private bool _edgeSlideOutPendingWpf;   // 是否已安排一次延迟滑出
    private DateTime _edgeSlideOutDueWpf;   // 延迟滑出的到期时刻（UtcNow 基准）
    private double _edgeHiddenTargetLeftWpf; // 延迟到期后要滑到的隐藏位 Left
    private double _edgeHiddenTargetTopWpf;  // 延迟到期后要滑到的隐藏位 Top

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT_WPF
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO_WPF
    {
        public int cbSize;
        public RECT_WPF rcMonitor;
        public RECT_WPF rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO_WPF lpmi);

    private const uint MONITOR_DEFAULTTONEAREST_WPF = 2;

    /// <summary>
    /// 取窗口所在屏幕（最近监视器）的工作区，物理像素 → 经窗口 PresentationSource 换算为 DIP。
    /// </summary>
    private bool TryGetWorkAreaDipWpf(out double left, out double top, out double right, out double bottom)
    {
        left = top = right = bottom = 0;
        if (_window == null) return false;
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(_window).Handle;
            if (hwnd == IntPtr.Zero) return false;
            var mi = new MONITORINFO_WPF();
            mi.cbSize = Marshal.SizeOf<MONITORINFO_WPF>();
            var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST_WPF);
            if (hMon == IntPtr.Zero || !GetMonitorInfo(hMon, ref mi)) return false;

            var src = PresentationSource.FromVisual(_window);
            if (src?.CompositionTarget == null)
            {
                // 兜底：拿不到 DPI 变换信息时按 1:1 处理（96 DPI 常见场景）
                left = mi.rcWork.Left; top = mi.rcWork.Top; right = mi.rcWork.Right; bottom = mi.rcWork.Bottom;
                return true;
            }
            var f = src.CompositionTarget.TransformFromDevice;
            var tl = f.Transform(new Point(mi.rcWork.Left, mi.rcWork.Top));
            var br = f.Transform(new Point(mi.rcWork.Right, mi.rcWork.Bottom));
            left = tl.X; top = tl.Y; right = br.X; bottom = br.Y;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检测窗口当前贴靠的屏幕边缘。返回 0=不贴边；out 返回"若隐藏应滑到的目标位置"。
    /// </summary>
    private int DetectEdgeWpf(out double hideLeft, out double hideTop)
    {
        hideLeft = hideTop = 0;
        if (_window == null) return 0;
        if (!TryGetWorkAreaDipWpf(out var wl, out var wt, out var wr, out var wb)) return 0;

        double w = _window.ActualWidth > 0 ? _window.ActualWidth : _window.Width;
        double h = _window.ActualHeight > 0 ? _window.ActualHeight : _window.Height;
        if (w <= 0 || h <= 0) return 0;

        double dl = _window.Left - wl;
        double dr = wr - (_window.Left + w);
        double dt = _window.Top - wt;
        double db = wb - (_window.Top + h);

        // 取距离最小且 < 阈值的一边
        int side = 0;
        double best = EdgeHideNearThresholdDip;
        if (dl < best) { best = dl; side = 1; }
        if (dr < best) { best = dr; side = 2; }
        if (dt < best) { best = dt; side = 3; }
        if (db < best) { best = db; side = 4; }
        if (side == 0) return 0;

        hideLeft = _window.Left;
        hideTop = _window.Top;
        switch (side)
        {
            case 1: hideLeft = wl - w + EdgeHideVisibleStripDip; break;   // 左：滑出，右缘留 6px 可见条
            case 2: hideLeft = wr - EdgeHideVisibleStripDip; break;       // 右：滑出，左缘留 6px 可见条
            case 3: hideTop = wt - h + EdgeHideVisibleStripDip; break;    // 上：滑出，下缘留 6px
            case 4: hideTop = wb - EdgeHideVisibleStripDip; break;        // 下：滑出，上缘留 6px
        }
        return side;
    }

    /// <summary>
    /// 200ms DoubleAnimation 平移窗口到目标位置；动画结束清动画时钟并落地本地值（与淡化动画同套路）。
    /// </summary>
    private void AnimateWindowPosWpf(double toLeft, double toTop)
    {
        if (_window == null) return;
        bool leftChanged = Math.Abs(_window.Left - toLeft) > 0.5;
        bool topChanged = Math.Abs(_window.Top - toTop) > 0.5;
        if (!leftChanged && !topChanged) return;

        _edgeAnimatingWpf = true;
        int remaining = (leftChanged ? 1 : 0) + (topChanged ? 1 : 0);
        var win = _window;
        void FinishOne()
        {
            if (System.Threading.Interlocked.Decrement(ref remaining) != 0) return;
            try
            {
                // 清除动画时钟 → 写本地值落地，避免动画值优先级导致后续直接赋值失效
                win.BeginAnimation(Window.LeftProperty, null);
                win.BeginAnimation(Window.TopProperty, null);
                win.Left = toLeft;
                win.Top = toTop;
            }
            catch { /* ignore */ }
            _edgeAnimatingWpf = false;
        }

        var dur = TimeSpan.FromMilliseconds(EdgeHideAnimMs);
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        if (leftChanged)
        {
            var a = new DoubleAnimation(toLeft, dur) { FillBehavior = FillBehavior.HoldEnd, EasingFunction = ease };
            a.Completed += (_, _) => FinishOne();
            win.BeginAnimation(Window.LeftProperty, a);
        }
        if (topChanged)
        {
            var b = new DoubleAnimation(toTop, dur) { FillBehavior = FillBehavior.HoldEnd, EasingFunction = ease };
            b.Completed += (_, _) => FinishOne();
            win.BeginAnimation(Window.TopProperty, b);
        }
    }

    /// <summary>
    /// 立即恢复贴边前正常位置（开关关闭 / Stop 时调用，无动画）。
    /// </summary>
    private void RestoreEdgePositionWpf()
    {
        if (_window == null) return;
        if (!_edgeHiddenWpf && _edgeSideWpf == 0) return;
        bool wasHidden = _edgeHiddenWpf;
        _edgeHiddenWpf = false;
        _edgeSideWpf = 0;
        _edgeSlideOutPendingWpf = false;   // 【贴边隐藏延迟】取消挂起的延迟滑出
        if (!wasHidden) return;
        try
        {
            _window.BeginAnimation(Window.LeftProperty, null);
            _window.BeginAnimation(Window.TopProperty, null);
            _window.Left = _edgeNormalLeftWpf;
            _window.Top = _edgeNormalTopWpf;
        }
        catch { /* ignore */ }
    }

    /// <summary>读取"贴边隐藏延迟时间"（秒，0~60，精确 0.1）；异常/未设时回退默认 3 秒。</summary>
    private double GetEdgeHideDelaySecWpf()
    {
        try { return Math.Clamp(_settings.FloatingScheduleEdgeHideDelay, 0.0, 60.0); }
        catch { return 3.0; }
    }

    /// <summary>
    /// 贴边隐藏状态机推进（50ms Timer Tick 调用 + 拖拽结束/开关变更即时调用）。
    /// 与点击穿透（WS_EX_TRANSPARENT）/指针淡化（Opacity）完全独立，互不干扰。
    /// </summary>
    private void UpdateEdgeHideWpf()
    {
        if (_window == null || !_window.IsVisible) return;
        // 拖拽中（鼠标模态 DragMove / 触摸）或滑移动画进行中：跳过本轮
        if (_mouseDraggingWpf || _touchDragIdWpf >= 0 || _edgeAnimatingWpf) return;

        if (!_settings.FloatingScheduleEdgeHide)
        {
            // 开关关闭：若仍处隐藏态则恢复位置并停止逻辑；同时取消挂起的延迟滑出
            _edgeSlideOutPendingWpf = false;
            RestoreEdgePositionWpf();
            return;
        }

        if (_edgeHiddenWpf)
        {
            // 隐藏中：窗口在屏幕内的部分即 ~6px 可见条，光标进入（命中窗口矩形）→ 滑回正常位
            if (IsPointerInWindowWpf())
            {
                _edgeHiddenWpf = false;
                AnimateWindowPosWpf(_edgeNormalLeftWpf, _edgeNormalTopWpf);
            }
            return;
        }

        var side = DetectEdgeWpf(out var hl, out var ht);
        if (side == 0)
        {
            // 不再贴边（被拖走/分辨率变化等）：清理状态 + 取消挂起的延迟滑出
            _edgeSideWpf = 0;
            _edgeSlideOutPendingWpf = false;
            return;
        }
        _edgeSideWpf = side;

        // 【悬停不阻止隐藏】不再因"光标在窗口内"取消/跳过滑出——鼠标悬停时间表照样会在延迟到期后滑出。
        //  唯一阻止隐藏的是"正在拖动"（本方法开头 _mouseDraggingWpf || _touchDragIdWpf>=0 已 return）。
        //  记录正常位（滑回目标）与滑出目标位，安排/等待延迟，到期后才真正滑出隐藏。
        _edgeNormalLeftWpf = _window.Left;
        _edgeNormalTopWpf = _window.Top;
        if (!_edgeSlideOutPendingWpf)
        {
            _edgeSlideOutPendingWpf = true;
            _edgeSlideOutDueWpf = DateTime.UtcNow.AddSeconds(GetEdgeHideDelaySecWpf());
            _edgeHiddenTargetLeftWpf = hl;
            _edgeHiddenTargetTopWpf = ht;
            return;
        }
        if (DateTime.UtcNow >= _edgeSlideOutDueWpf)
        {
            _edgeSlideOutPendingWpf = false;
            _edgeHiddenWpf = true;
            AnimateWindowPosWpf(_edgeHiddenTargetLeftWpf, _edgeHiddenTargetTopWpf);
        }
    }

    private void ShowWindow()
    {
        try
        {
            _window?.Show();
            ApplyWindowLayer();
        }
        catch
        {
            // 忽略
        }
    }

    private void HideWindow()
    {
        try
        {
            _window?.Hide();
        }
        catch
        {
            // 忽略
        }
    }

    private void CloseWindow()
    {
        _allowClose = true;
        try
        {
            _window?.Close();
        }
        catch
        {
            // 忽略
        }
        _window = null;
        _currentProgressIndicator = null;
        _currentProgressHost = null;
        _currentBreakProgressIndicator = null;
        _currentBreakProgressHost = null;
        _currentBreakLayoutItem = null;
        _currentProgressClassIndex = -1;
        _currentRowLayoutItems.Clear();
        // 【贴边自动隐藏 / 触摸拖拽】窗口销毁：复位全部瞬态，防止下次启动残留旧状态
        _edgeHiddenWpf = false;
        _edgeSideWpf = 0;
        _edgeAnimatingWpf = false;
        _mouseDraggingWpf = false;
        _touchDragIdWpf = -1;
    }

    private void ApplyOpacity()
    {
        // 已废弃：现在不透明度仅作用卡片背景刷 Alpha（由 ApplyThemeColors / RefreshSchedule 触发），
        // 不再设置整窗 Window.Opacity，以保证文字与进度条保持 100% 可读性。
    }

    private void ApplyThemeColors()
    {
        if (_containerBorder == null) return;

        var isDark = ThemeHelper.IsDarkTheme();
        // 背景不透明度(0~1) * 255 => Alpha，仅作用卡片背景，文字/进度条保持完全不透明
        var bgAlpha = (byte)Math.Round(Math.Clamp(_settings.FloatingScheduleOpacity, 0.0, 1.0) * 255);
        // 容器背景色，深色主题深灰，浅色主题白色
        Color bgColor = isDark
            ? Color.FromArgb(bgAlpha, 0x20, 0x20, 0x20)
            : Color.FromArgb(bgAlpha, 0xFF, 0xFF, 0xFF);
        _containerBorder.Background = new SolidColorBrush(bgColor);
    }

    private void ApplyWindowLayer()
    {
        if (_window == null) return;
        // 【修复 Issue 1】重入计数器 +1（return/throw 也 finally -1）—— 同 Ava 端语义
        System.Threading.Interlocked.Increment(ref _inApplyWindowLayerWpf);
        try
        {
            if (_suppressTopmostRefreshWpf) return;   // 【修复：拖动闪烁】拖动期间抑制 z-order/exstyle 重设（TouchUp 时恢复并重设一次）
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(_window).Handle;
                if (hwnd == IntPtr.Zero) return;

                if (_settings.FloatingScheduleWindowLayer == FloatingScheduleWindowLayer.Topmost)
                {
                    // 置顶：清除 WS_EX_NOACTIVATE（允许交互激活），SetWindowPos 提到最前
                    //  【对齐 ClassIsland】完整 SWP 标志（SWP_NOSENDCHANGING/SWP_NOOWNERZORDER/SWP_NOREPOSITION）
                    //  防止递归 WM_WINDOWPOSCHANGING 与 owner 窗口被连带重排。
                    try
                    {
                        var exT = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
                        // 【修复闪烁】先读当前 exStyle，仅当确实带 WS_EX_NOACTIVATE 时才清除：
                        //  Every50Ms/Every1Ms 层级刷新模式下，每 Tick 无条件 SetWindowLong 写入相同扩展样式
                        //  会强制 DWM 重新评估分层窗口 → 悬浮窗高频闪烁。
                        if ((exT & WS_EX_NOACTIVATE) != 0)
                            SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)(exT & ~WS_EX_NOACTIVATE));
                    }
                    catch { }
                    _window.Topmost = true;
                    // 【修复闪烁·z-order 条件断言】已在 z-order 链顶端（GW_HWNDFIRST==hwnd）→ 跳过 SetWindowPos，
                    //  消除每 Tick 重设 z-order 导致的 DWM 重合成闪烁；仅被其它 topmost 窗口抢占时才重设一次。
                    bool needTopWpf = true;
                    try { needTopWpf = GetWindow(hwnd, GW_HWNDFIRST_WPF) != hwnd; } catch { }
                    if (needTopWpf)
                        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW |
                            SWP_NOSENDCHANGING | SWP_NOOWNERZORDER | SWP_NOREPOSITION);
                }
                else
                {
                    // 【★ 彻底置底】（与 Ava 端同构）
                    //  1) 加 WS_EX_NOACTIVATE：置底窗口点击/拖拽不激活 → 永不被 Windows 提升 z-order
                    //     （激活提升是置底失效主因：即使每 1ms 重设，激活提升发生在两次重设之间且优先级更高）
                    //  2) 完整 SWP 标志（对齐 ClassIsland Bottommost）：SWP_NOSENDCHANGING/SWP_NOOWNERZORDER/
                    //     SWP_NOREPOSITION 防止递归 WM_WINDOWPOSCHANGING 与 owner 窗口被连带重排。
                    try
                    {
                        var exB = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
                        // 【修复闪烁】同上：仅当当前不带 WS_EX_NOACTIVATE 时才置位，消除每 Tick 冗余 Win32 样式写入。
                        if ((exB & WS_EX_NOACTIVATE) == 0)
                            SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)(exB | WS_EX_NOACTIVATE));
                    }
                    catch { }
                    _window.Topmost = false;
                    // 【修复闪烁·z-order 条件断言】已在 z-order 链底端（GW_HWNDLAST==hwnd）→ 跳过重设（同上）。
                    bool needBottomWpf = true;
                    try { needBottomWpf = GetWindow(hwnd, GW_HWNDLAST_WPF) != hwnd; } catch { }
                    if (needBottomWpf)
                        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW |
                            SWP_NOSENDCHANGING | SWP_NOOWNERZORDER | SWP_NOREPOSITION);
                }
            }
            catch
            {
                // 忽略 Win32 调用异常
            }
        }
        finally
        {
            System.Threading.Interlocked.Decrement(ref _inApplyWindowLayerWpf);
        }
    }

    // ========== 点击穿透：切换 WS_EX_TRANSPARENT（参考 CI2 主窗口同样的做法，系统级命中跳过）==========
    //  经验（1677042）：主导机制必须只选用一种 —— 这里选择 Win32 窗口级 WS_EX_TRANSPARENT，
    //   因为 IsHitTestVisible=false 只会让 WPF 内部不接命中，但系统层仍会挡住下面的 HWND；
    //   只有 WS_EX_TRANSPARENT | WS_EX_LAYERED 组合才能让 DWM 真实"穿透到下方物品"。
    //  拖拽禁用已在 OnContainerMouseLeftButtonDown 首行 return 处理。
    private void ApplyClickThrough()
    {
        if (_window == null) return;
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(_window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var want = _settings.FloatingScheduleClickThrough;
            // last-applied 防抖：同状态重复调会让 DWM 合成层产生无意义的闪烁
            if (want == _lastWpfClickThroughApplied) return;

            int exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
            if (want)
            {
                // WS_EX_LAYERED 是 WS_EX_TRANSPARENT 生效的前提（OnWindowLoaded 已加过，但这里二次保险）
                exStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
                // WPF 命中同步关闭（双重保险：防止在部分非标准 DPI 下 Win32 穿透暂时失效时，WPF 自身仍能保持透明）
                if (_containerBorder != null) _containerBorder.IsHitTestVisible = false;
            }
            else
            {
                // 仅移除 TRANSPARENT，保留 COMPOSITED / LAYERED / TOOLWINDOW 等其它扩展样式
                //   （防止合成重建造成闪烁）
                exStyle &= ~WS_EX_TRANSPARENT;
                if (_containerBorder != null) _containerBorder.IsHitTestVisible = true;
            }
            SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);
            _lastWpfClickThroughApplied = want;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ApplyClickThrough 切换 WS_EX_TRANSPARENT 失败，安全忽略。");
        }
    }

    // ========== 指针移入淡化：严格对齐 CI2 的 UpdateFadeStatus 逻辑 ==========
    //  CI2 判定规则 (MainWindowLine.cs L503-509)：
    //     IsLineFaded = Settings.IsMouseInFadingEnabled && ( IsMouseIn ^ IsMouseInFadingReversed )
    //  淡化 Opacity 采用 CI2 Style 的 0.05 (整窗)
    //
    //  ===== 本次三项修复 =====
    //   ①. 解决"判定延迟 & 不准确"：
    //       —— 旧版：与 500ms 进度刷新共用 Timer → 最快 500ms 响应（偶感知 1s 级延迟）；
    //       —— 新版：单独 DispatcherTimer(Normal, 50ms) 高频轮询 + 连续 N 帧去抖（FadeStableThresholdTicks=3 ≈150ms），
    //         消除 DWM 合成 / 多监视器坐标 ±1px 抖动造成的"边缘闪烁/判定错误"，仍比旧版快 ~3x。
    //   ②. 切换加 250ms 过渡动画：用户要求 0.25s 过渡，用 DoubleAnimation。
    //   ③. force=true 或 淡化主开关关闭 时，**立即** 把 Opacity 落到最终值，避免"关了淡化还在慢慢恢复"的反直觉体验。
    private const double FadeTargetOpacityFaded = 0.05;
    private const double FadeTargetOpacityNormal = 1.0;
    private const int FadeTransitionMs = 250;  // 用户要求 0.25s

    private void ApplyHoverFade(bool force = false)
    {
        if (_window == null) return;
        try
        {
            bool observed;   // 本轮"观察到"的 faded（未加去抖，用来累计稳定计数）
            if (!_settings.FloatingScheduleHoverFade)
            {
                observed = false;
            }
            else
            {
                var pointerIn = IsPointerInWindowWpf();
                observed = pointerIn ^ _settings.FloatingScheduleHoverFadeReverse;
            }

            // 去抖稳定计数（仅"非强制 + 非主开关切换"才走，主开关关/force 直接落地）
            bool shouldApply = true;
            bool faded = observed;
            if (!force && _settings.FloatingScheduleHoverFade)
            {
                if (observed == _fadeLastObserved)
                {
                    _fadeStableCount = Math.Min(_fadeStableCount + 1, FadeStableThresholdTicks + 1);
                }
                else
                {
                    _fadeLastObserved = observed;
                    _fadeStableCount = 1;
                }

                // 未达到稳定阈值 → 不切换 faded（但仍沿用上一轮已 applied 值）
                if (_fadeStableCount < FadeStableThresholdTicks)
                    shouldApply = false;
            }
            else
            {
                // force 或 主开关关闭 直接对齐状态
                _fadeLastObserved = observed;
                _fadeStableCount = FadeStableThresholdTicks;
            }

            bool masterDisabled = !_settings.FloatingScheduleHoverFade;
            bool targetChanged = (faded != _lastWpfFadedApplied);

            // last-applied 防抖：状态没变 且 不是 force/主开关关 → 直接跳过，不触发动画也不写 Opacity
            if (!force && !masterDisabled && (!shouldApply || !targetChanged))
                return;

            // 主开关关闭 或 force 模式：立即跳到最终值（取消正在进行的 250ms 过渡动画）
            if (masterDisabled || force)
            {
                // WPF：想让本地值取代动画值，必须先清除动画（BeginAnimation(dp, null) 或 ApplyAnimationClock(null)）
                _window.BeginAnimation(UIElement.OpacityProperty, null);
                _window.Opacity = faded ? FadeTargetOpacityFaded : FadeTargetOpacityNormal;
                _lastWpfFadedApplied = faded;
                return;
            }

            // —— 正常路径（主开关开 & 状态切换）：250ms DoubleAnimation 过渡到目标值——
            double from = _window.Opacity;
            double to = faded ? FadeTargetOpacityFaded : FadeTargetOpacityNormal;
            if (Math.Abs(from - to) < 1e-6)
            {
                // 已经在目标值（极小概率：动画被外部干扰但本地 Opacity 恰好是 to）
                _lastWpfFadedApplied = faded;
                return;
            }

            var anim = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(FadeTransitionMs),
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            // 动画结束后同步落地到本地值，避免 WPF 动画/本地值优先级造成"下次写 Opacity 无效"
            anim.Completed += (_, _) =>
            {
                try
                {
                    _window.BeginAnimation(UIElement.OpacityProperty, null);
                    _window.Opacity = to;
                }
                catch { /* ignore */ }
            };
            _window.BeginAnimation(UIElement.OpacityProperty, anim);
            _lastWpfFadedApplied = faded;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ApplyHoverFade 失败，安全忽略。");
        }
    }

    // ========== 判定指针是否在悬浮窗屏幕矩形内（Win32 全局光标 vs _containerBorder.PointToScreen）==========
    //  为什么不用 Mouse.DirectlyOver / IsMouseOver：点击穿透模式下 WPF 接收不到任何鼠标消息，这些属性永远为 false。
    //  必须走系统级 GetCursorPos，与 CI2 MainWindowLine.GetMouseStatusByPos (L619-637) 完全等价。
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    private bool IsPointerInWindowWpf()
    {
        if (_window == null || _containerBorder == null) return false;
        try
        {
            if (!GetCursorPos(out var pt)) return false;

            // WPF PointToScreen 会按 Per-Monitor V2 自动转成物理像素矩形（与 GetCursorPos 单位一致）
            var topLeft = _containerBorder.PointToScreen(new Point(0, 0));
            double w = _containerBorder.ActualWidth;
            double h = _containerBorder.ActualHeight;
            if (w <= 0 || h <= 0)
            {
                // 兜底：_containerBorder 还没完成布局时退化为窗口矩形
                topLeft = _window.PointToScreen(new Point(0, 0));
                w = _window.ActualWidth;
                h = _window.ActualHeight;
                if (w <= 0 || h <= 0) return false;
            }
            var botRight = _containerBorder.PointToScreen(new Point(w, h));

            return pt.x >= (int)Math.Round(topLeft.X) &&
                   pt.x <= (int)Math.Round(botRight.X) &&
                   pt.y >= (int)Math.Round(topLeft.Y) &&
                   pt.y <= (int)Math.Round(botRight.Y);
        }
        catch
        {
            return false;
        }
    }

    private static Color GetAccentColor()
    {
        try
        {
            // 优先尝试从 ClassIsland 设置中获取 PrimaryColor
            var themeSvc = IAppHost.TryGetService<IThemeService>();
            // 使用动态资源方式：MaterialDesign 的 PrimaryAccentColor
            if (Application.Current?.TryFindResource("PrimaryHueMidBrush") is Brush brush
                && brush is SolidColorBrush scb)
            {
                return scb.Color;
            }
            if (Application.Current?.TryFindResource("PrimaryColor") is Color c)
            {
                return c;
            }
        }
        catch
        {
            // 忽略
        }
        // 回退默认强调色（蓝）
        return Color.FromRgb(0x00, 0x78, 0xD4);
    }

    private void RefreshSchedule()
    {
        if (_window == null || _containerBorder == null) return;
        // 【修复：初始化时处于课间无法显示时间表】冷启动 sentinel 识别：
        //   RefreshSchedule 顶部会把 _lastWpfRefreshStateCode 重置为 -1（见下方），因此必须在重置前捕获旧值。
        //   如果本方法执行前 snapshot == -1，代表"之前从未成功构建过 UI"（插件冷启动/Stop→Start 后首次重建），
        //   此时即使处于 Breaking 时段也必须跳过 ENTER 动画（直接落地 Opacity=1/Y=0），否则 AnimateBreakRowEnterWpf
        //   会先把 3 个课间元素 Opacity=0 → 用户启动瞬间看不到课间/进度条 → 误认为「时间表卡住不更新」；
        //   且 250ms 动画值优先级 > 本地值，任何异常导致 Completed 未触发时元素永久保持透明 0。
        bool isColdStartWpf = _lastWpfRefreshStateCode == -1;
        // 先清理上一次构建结果快照（防止本帧抛异常但缓存留旧值，下一帧无法命中 needRefresh）
        _currentProgressClassIndex = -1;
        _currentRowLayoutItems.Clear();
        _currentProgressIndicator = null;
        _currentProgressHost = null;
        _currentBreakProgressIndicator = null;
        _currentBreakProgressHost = null;
        _currentBreakLayoutItem = null;
        // 【修复 #1】顶部同步清零课间行视觉引用：防止 try 中段抛异常进入 catch 后，字段仍持有已脱离视觉树的旧 FE 引用造成短期内存滞留
        _currentBreakRowVisualsWpf = null;
        _lastWpfRefreshStateCode = -1;
        _lastWpfRefreshBreakStartTicks = -1;
        _lastWpfRefreshBreakEndTicks = -1;
        // 课间插入行可视化元素（EXIT 动画时用）：初始化 null。仅当本次 RefreshSchedule 真正插入了课间行时才赋值。
        List<FrameworkElement>? builtBreakVisuals = null;

        try
        {
            ApplyThemeColors();
            // 字号：课程名=fontSize，其它文字按相对差值：表头-2/时间-1/老师-3
            var fontSize = (int)Math.Round(Math.Clamp(_settings.FloatingScheduleFontScale, 8, 32));
            var isDark = ThemeHelper.IsDarkTheme();
            var textForeground = isDark ? Brushes.White : Brushes.Black;
            var subtextForeground = isDark
                ? new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA))
                : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            var separatorColor = isDark
                ? Color.FromRgb(0x44, 0x44, 0x44)
                : Color.FromRgb(0xD0, 0xD0, 0xD0);

            // 获取服务
            var lessonsService = IAppHost.TryGetService<ILessonsService>();
            var profileService = IAppHost.TryGetService<IProfileService>();

            // 取今日课表
            // 【修复：调试时间调整不立刻刷新时间表 - Date 锚点】
            //  原 DateTime.Today = 本地系统日期，不随 DebugTimeOffsetSeconds 跨天变化。
            //  改为 GetClassIslandNow().Date（走宿主 ExactTimeService，含 DebugTimeOffsetSeconds + TimeOffsetSeconds 偏移），
            //  跨天调试时 ClassPlan 立刻抓目标日期课表，而不是停留在系统日期课。
            DateTime todayBaseWpf = GetClassIslandNow().Date;
            ClassPlan? classPlan = null;
            if (lessonsService != null)
            {
                classPlan = lessonsService.GetClassPlanByDate(todayBaseWpf);
                // 有时 CurrentClassPlan 已经被加载，优先使用当前的
                if (lessonsService.CurrentClassPlan != null)
                {
                    classPlan = lessonsService.CurrentClassPlan;
                }
            }

            // 取科目映射
            Dictionary<Guid, Subject> subjectsMap = ReflectBuildSubjectsMap(profileService?.Profile?.Subjects!);

            // 主容器
            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                MinWidth = 220
            };

            // ===== 标题行 =====
            var titlePanel = new Grid();
            titlePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titlePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "今日时间表",
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Foreground = textForeground,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            Grid.SetColumn(titleText, 0);
            titlePanel.Children.Add(titleText);

            rootPanel.Children.Add(titlePanel);

            // ===== 判断是否有课程 =====
            bool hasClasses = false;
            // 使用弱类型 object 以兼容不同版本 SDK 中 ClassInfo / TimeLayoutItem 属性差异
            List<(object? ClassInfo, Subject? Subject, object LayoutItem)> classRows = new();

            if (classPlan != null)
            {
                // 使用 ValidTimeLayoutItems 过滤有效条目，并只取上课类型 (TimeType == 0)
                var validItemsRaw = ReflectGetValidTimeLayoutItems(classPlan);
                var validItems = new List<object>();
                // 【★ 连续课间分别走进度】同步收集课间项（TimeType==1），供课间进度条"每个课间分别走"
                // 【课表分隔线·档案组件】同步收集档案分隔线对象（TimeType==2），供分隔线绘制
                _breakItemsWpf.Clear();
                _separatorItemsWpf.Clear();
                foreach (var x in validItemsRaw)
                {
                    if (x == null) continue;
                    if (ReflectGetTimeType(x) == 0) validItems.Add(x);
                    else if (ReflectGetTimeType(x) == 1) _breakItemsWpf.Add(x);
                    else if (ReflectGetTimeType(x) == 2) _separatorItemsWpf.Add(x);
                }
                validItems.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));
                _breakItemsWpf.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));
                _separatorItemsWpf.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));

                var classesList = ReflectGetClasses(classPlan);

                for (int i = 0; i < validItems.Count; i++)
                {
                    var layoutItem = validItems[i];
                    var layoutStart = ReflectGetStartTime(layoutItem);
                    var layoutEnd = ReflectGetEndTime(layoutItem);

                    // 找到匹配的 ClassInfo（使用 Index 映射）
                    object? classInfo = null;
                    foreach (var c in classesList)
                    {
                        if (c == null) continue;
                        var curLi = ReflectGetCurrentTimeLayoutItem(c);
                        if (curLi == null) continue;
                        var cs = ReflectGetStartTime(curLi);
                        var ce = ReflectGetEndTime(curLi);
                        if (cs == layoutStart && ce == layoutEnd) { classInfo = c; break; }
                    }

                    // 如果找不到匹配，回退使用 Classes[i]
                    if (classInfo == null && i < classesList.Count)
                    {
                        classInfo = classesList[i];
                    }

                    if (classInfo == null || !ReflectGetIsEnabled(classInfo)) continue;

                    Subject? subject = null;
                    var sid = ReflectGetSubjectId(classInfo);
                    if (sid != Guid.Empty && subjectsMap.TryGetValue(sid, out var s))
                    {
                        subject = s;
                    }

                    classRows.Add((classInfo, subject, layoutItem));
                }

                hasClasses = classRows.Count > 0;
            }

            if (!hasClasses)
            {
                // 占位符
                var placeholder = new TextBlock
                {
                    Text = "今天没有课程",
                    FontSize = fontSize,
                    Foreground = subtextForeground,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 14, 0, 8)
                };
                rootPanel.Children.Add(placeholder);
            }
            else
            {
                // ===== 创建表格 =====
                var table = new Grid { Margin = new Thickness(0, 0, 0, 0) };
                // 两列：课程 / 时间
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // 表头
                int rowIndex = 0;
                table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var headerCourse = new Border
                {
                    Padding = new Thickness(2, 2, 8, 4),
                    BorderBrush = new SolidColorBrush(separatorColor),
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                var headerCourseText = new TextBlock
                {
                    Text = "课程",
                    FontSize = Math.Max(8, fontSize - 2),
                    FontWeight = FontWeights.Bold,
                    Foreground = subtextForeground
                };
                headerCourse.Child = headerCourseText;
                Grid.SetRow(headerCourse, rowIndex);
                Grid.SetColumn(headerCourse, 0);
                table.Children.Add(headerCourse);

                var headerTime = new Border
                {
                    Padding = new Thickness(8, 2, 2, 4),
                    BorderBrush = new SolidColorBrush(separatorColor),
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                var headerTimeText = new TextBlock
                {
                    Text = "时间",
                    FontSize = Math.Max(8, fontSize - 2),
                    FontWeight = FontWeights.Bold,
                    Foreground = subtextForeground
                };
                headerTime.Child = headerTimeText;
                Grid.SetRow(headerTime, rowIndex);
                Grid.SetColumn(headerTime, 1);
                table.Children.Add(headerTime);

                // 确定当前课程索引 + 课间休息插入位置
                int currentOnClassIndex = -1;
                var nowTimeOfDay = GetClassIslandNow().TimeOfDay;
                var curStateWpf = lessonsService?.CurrentState ?? TimeState.None;

                if (lessonsService != null && curStateWpf == TimeState.OnClass)
                {
                    // 【★ 时间跳变/连续上课 高亮定位】真实时间主选：
                    //  - 连续上课 C1→C2：now 一过 C1.End 即定位 C2（比等 SDK 推进更准）
                    //  - 时间跳变（改调试偏移/插件 TimeOffsetSeconds）：SDK CurrentTimeLayoutItem 不反映插件时间
                    //    → 若只用 SDK 匹配会高亮旧课，进度条按旧课区间+新时间算（继承旧进度/卡 100%）
                    //    → 必须用"真实时间 now 落在课表行的 [Start, End) 区间"定位目标课，进度条立即显示目标课新进度。
                    for (int i = 0; i < classRows.Count; i++)
                    {
                        var stRow = ReflectGetStartTime(classRows[i].LayoutItem);
                        var edRow = ReflectGetEndTime(classRows[i].LayoutItem);
                        if (nowTimeOfDay >= stRow && nowTimeOfDay < edRow) { currentOnClassIndex = i; break; }
                    }
                    // SDK 匹配兜底：真实时间定位失败（如 SDK 特殊课程/时间异常）时，回退到 SDK CurrentTimeLayoutItem 匹配
                    if (currentOnClassIndex < 0)
                    {
                        var curItem = ReflectGetCurrentTimeLayoutItemService(lessonsService);
                        if (curItem != null)
                        {
                            var curS = ReflectGetStartTime(curItem);
                            var curE = ReflectGetEndTime(curItem);
                            for (int i = 0; i < classRows.Count; i++)
                            {
                                if (ReflectGetStartTime(classRows[i].LayoutItem) == curS &&
                                    ReflectGetEndTime(classRows[i].LayoutItem) == curE)
                                {
                                    currentOnClassIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }

                // ---------- 当前课高亮底色：强调色 × 40% 透明度（Alpha ≈ 0x66）----------
                // 不再使用之前硬编码的深浅蓝 2 套：直接拿应用 Primary 强调色，满足"当前课整行高亮=强调色40%透明度"。
                var accentColorNow = GetAccentColor();
                const double wpfHighlightAlpha = 0.40;
                byte wpfHighlightA = (byte)Math.Clamp((int)Math.Round(wpfHighlightAlpha * 255), 0, 255);
                var highlightBg = new SolidColorBrush(
                    Color.FromArgb(wpfHighlightA, accentColorNow.R, accentColorNow.G, accentColorNow.B));

                // ---------- 课间休息插入行判定（与 Avalonia 完全一致的 5 条规则）----------
                //  【修复：连续多课间（B1-B2-B3）不显示后续课间】
                //   旧算法：要求"classRows[i].End == 当前课间.Start && classRows[i+1].Start == 当前课间.End"同时命中才能插。
                //   ——3 连续课间时，B2/B3 既不等于前课 End 又不等于后课 Start → breakInsertAfterClassIdx=-1 → 不显示。
                //   新算法（鲁棒兼容任意多连续课间，单课间场景与旧算法等价，无破坏性变更）：
                //     a) i_max_end_le_bs  = classRows 中最后一个 End <= 当前课间.Start 的索引（= 前一节上课）
                //     b) i_min_start_ge_be = classRows 中第一个 Start >= 当前课间.End 的索引（= 后一节上课）
                //     c) 合法：i_max_end_le_bs >= 0 && i_min_start_ge_be < Count && i_max_end_le_bs < i_min_start_ge_be
                //     d) 插入位置 = i_max_end_le_bs（在前一节上课后面插入当前课间行）
                int breakInsertAfterClassIdx = -1;
                TimeSpan breakStartWpf = default;
                TimeSpan breakEndWpf = default;
                string breakNameWpf = "课间休息";
                object? breakLayoutItemWpf = null;
                if (curStateWpf == TimeState.Breaking && classRows.Count >= 2 && lessonsService != null)
                {
                    // 【★ 连续课间分别走进度】首选：真实时间定位当前课间项（连续课间 B1→B2→B3 各自独立区间，
                    //  课间行/进度条分别显示每个课间，而非课对空隙总长度）；找不到再回退 SDK CurrentTimeLayoutItem。
                    var realBi = FindBreakItemByRealTimeWpf(nowTimeOfDay, out var bsReal, out var beReal);
                    var bLi = realBi ?? ReflectGetCurrentTimeLayoutItemService(lessonsService);
                    if (bLi != null && ReflectGetTimeType(bLi) == 1)
                    {
                        // 时间区间：真实时间定位命中用其区间，否则用 SDK item 区间
                        var bs = realBi != null ? bsReal : ReflectGetStartTime(bLi);
                        var be = realBi != null ? beReal : ReflectGetEndTime(bLi);
                        var firstStart = ReflectGetStartTime(classRows[0].LayoutItem);
                        var lastEnd = ReflectGetEndTime(classRows[classRows.Count - 1].LayoutItem);
                        if (bs >= firstStart && be <= lastEnd)
                        {
                            // ---- 新算法（鲁棒兼容连续课间）：按"前后上课课"定位插入位置 ----
                            //  a) 前一节课：最后一个 End <= bs 的上课行（classRows 时间升序，遇到 > 就 break）
                            int iMaxEndLeBs = -1;
                            for (int k = 0; k < classRows.Count; k++)
                            {
                                if (ReflectGetEndTime(classRows[k].LayoutItem) <= bs) iMaxEndLeBs = k;
                                else break;
                            }
                            //  b) 后一节课：第一个 Start >= be 的上课行（反向扫描，遇到 < 就 break）
                            int iMinStartGeBe = classRows.Count;
                            for (int k = classRows.Count - 1; k >= 0; k--)
                            {
                                if (ReflectGetStartTime(classRows[k].LayoutItem) >= be) iMinStartGeBe = k;
                                else break;
                            }
                            //  c) 合法性 + 插入
                            if (iMaxEndLeBs >= 0 && iMinStartGeBe < classRows.Count &&
                                iMaxEndLeBs < iMinStartGeBe)
                            {
                                breakInsertAfterClassIdx = iMaxEndLeBs;
                                breakStartWpf = bs;
                                breakEndWpf = be;
                                breakNameWpf = ReflectGetBreakNameText(bLi);
                                breakLayoutItemWpf = bLi;
                            }
                        }
                        // 【★ 连续课间即时切换（空隙兜底）】（WPF 1:1 镜像 Ava）
                        //  真实时间课间项定位失败（如 _breakItemsWpf 空/时间异常）且 SDK item 超时 →
                        //  回退课对空隙定位，保证 SDK 滞后时课间行仍能切到下一段。
                        if (realBi == null && nowTimeOfDay.TotalSeconds - be.TotalSeconds > EndOverrunToleranceSecWpf)
                        {
                            int gapIdx = FindBreakGapIndexByRealTimeWpf(nowTimeOfDay, out var gs, out var ge);
                            if (gapIdx >= 0)
                            {
                                breakInsertAfterClassIdx = gapIdx;
                                breakStartWpf = gs;
                                breakEndWpf = ge;
                                breakNameWpf = ReflectGetBreakNameText(bLi);   // 名称沿用 SDK 课间 item（连续课间名称通常一致）
                            }
                            else
                            {
                                // 真实时间已不在任何空隙（SDK 滞后但实际已到上课/放学）→ 不显示课间行
                                breakInsertAfterClassIdx = -1;
                            }
                        }
                    }
                }

                // 数据行 + 状态快照缓存（与 Avalonia 同策略：UpdateProgress 用它检测状态/课间变化 → 立刻整表重建）
                _currentProgressClassIndex = currentOnClassIndex;
                _currentRowLayoutItems = classRows.Select(r => (object)r.LayoutItem).ToList();
                _currentProgressIndicator = null;
                _currentProgressHost = null;
                _lastWpfRefreshStateCode = (int)curStateWpf;
                _lastWpfRefreshBreakStartTicks = breakInsertAfterClassIdx >= 0 ? breakStartWpf.Ticks : -1;
                _lastWpfRefreshBreakEndTicks   = breakInsertAfterClassIdx >= 0 ? breakEndWpf.Ticks   : -1;
                // 【修复：调试时间不立刻刷新 - Date 兜底快照】
                //  记录本次 RefreshSchedule 构建时的调试时间 Date（非系统 DateTime.Today），
                //  500ms Tick 检测 dateChanged 时以此为基线，跨天调试跳转即使 PropertyChanged 丢失也能下一 Tick 强制 needRefresh=true。
                _lastWpfRefreshDate = GetClassIslandNow().Date;

                // ---------- 分隔线（3px）位置计算 ----------
                // 【课表分隔线·档案组件】数据源 = 档案中用户手动插入的分隔线对象（_separatorItemsWpf，TimeType==2），
                //  而不是每个课间都画线。对每个分隔线 s，找最后一个 End <= s.Start 的课程索引 i（classRows 时间升序，遇 > 即 break）；
                //  要求 i+1 存在（分隔线必须夹在两节课之间）。多条分隔线命中同一 i → HashSet 天然去重，只插一条。
                //  主题色：深色=白 / 浅色=黑；主题变化由 OnThemeChanged → RefreshSchedule 整表重建自动更新，
                //  无需单独订阅事件（重建即全新控件，无泄漏风险）。
                var breakSeparatorAfterWpf = new HashSet<int>();
                foreach (var s in _separatorItemsWpf)
                {
                    var sT = ReflectGetStartTime(s);
                    int idx = -1;
                    for (int k = 0; k < classRows.Count; k++)
                    {
                        if (ReflectGetEndTime(classRows[k].LayoutItem) <= sT) idx = k;
                        else break;
                    }
                    if (idx >= 0 && idx < classRows.Count - 1)
                        breakSeparatorAfterWpf.Add(idx);
                }

                for (int i = 0; i < classRows.Count; i++)
                {
                    rowIndex++;
                    table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var (classInfo, subject, layoutItem) = classRows[i];
                    bool isCurrent = i == currentOnClassIndex;

                    // 单元格底边框
                    var borderThickness = i < classRows.Count - 1
                        ? new Thickness(0, 0, 0, 1)
                        : new Thickness(0);

                    // ===== 课程名列 =====
                    var courseCell = new Border
                    {
                        Padding = new Thickness(2, 6, 8, 6),
                        BorderBrush = new SolidColorBrush(separatorColor),
                        BorderThickness = borderThickness,
                        Background = isCurrent ? highlightBg : Brushes.Transparent
                    };

                    var coursePanel = new Grid();
                    coursePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    coursePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // 课程名 + 老师（正右方，同一行两列并排，垂直居中对齐）
                    var courseNameGrid = new Grid();
                    courseNameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    courseNameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var courseName = subject?.Name ?? "?";
                    var nameText = new TextBlock
                    {
                        Text = courseName,
                        FontSize = fontSize,
                        FontWeight = isCurrent ? FontWeights.SemiBold : FontWeights.Normal,
                        Foreground = textForeground,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        TextWrapping = TextWrapping.NoWrap
                    };
                    Grid.SetColumn(nameText, 0);
                    courseNameGrid.Children.Add(nameText);

                    // 老师名
                    var teacherTitle = string.Empty;
                    if (subject != null && !string.IsNullOrWhiteSpace(subject.TeacherName))
                    {
                        if (_settings.FloatingScheduleEnableFullTeacherName)
                        {
                            teacherTitle = subject.TeacherName.Trim();
                        }
                        else
                        {
                            var surname = subject.GetFirstName();
                            if (!string.IsNullOrWhiteSpace(surname))
                            {
                                teacherTitle = surname + "老师";
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(teacherTitle))
                    {
                        int teacherFontSizeWpf = Math.Max(8, fontSize - 3);
                        // ===== 修复：教师列完整展示 15 汉字（用户最新要求，Ava/WPF 1:1 对齐）=====
                        // 单一事实来源：teacherMaxWpf = teacherFontSizeWpf pt × 21.5
                        //   CJK 方块 YaHei = pt × (96/72) ≈ pt×1.333 px/字；15 字 = pt×20 纯理论；
                        //   实测 30 字符普通外教姓名 "Dr. Chris... Saar PhD" = 316.13 px / 15 pt = 21.08 →
                        //   加 2% 安全（不同字体 Fallback/高 DPI 缩放/YAHEI 连字）→ 系数 21.5。
                        //   15 pt 基准 = 322.5 px：
                        //     - 15 字纯 CJK = 300 px ≤ 322.5 ✅ 完整无省略
                        //     - 10 字少民 = 169.63 px（仅 52.6%）仍富余
                        //     - 英文名 ≤ 30 字符（普通外籍姓名）= 316 px ≤ 322.5 ✅ 完整
                        //     - 英文名 ≥ 35 字末尾裁剪 + ToolTip 完整显示，保持"迷你悬浮"定位
                        //   FontScale=8 → 8×21.5=172 px（足 8 字中文，用户目标 15 字在 FontScale=18 时生效）
                        //   FontScale=32 → 29×21.5=623.5 px；外层 820 兜底不横条
                        double teacherMaxWpf = teacherFontSizeWpf * 21.5;
                        var teacherText = new TextBlock
                        {
                            Text = teacherTitle,
                            FontSize = teacherFontSizeWpf,
                            Foreground = subtextForeground,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            TextWrapping = TextWrapping.NoWrap,
                            MaxWidth = teacherMaxWpf,
                            Margin = new Thickness(8, 0, 0, 0)
                        };
                        Grid.SetColumn(teacherText, 1);
                        courseNameGrid.Children.Add(teacherText);
                    }

                    Grid.SetRow(courseNameGrid, 0);
                    coursePanel.Children.Add(courseNameGrid);

                    // 当前课程进度条
                    if (isCurrent)
                    {
                        var accentColor = GetAccentColor();
                        // 【修复：进度条有时总是满的】使用自绘 Grid(2层Border) 代替 ProgressBar+自定义 ControlTemplate：
                        //   - 外层：高度 3px，负责整体占位；
                        //   - 底层(背景)：跨整个容器，强调色 25% 透明度；
                        //   - 顶层(前景)：HorizontalAlignment=Left，每 Tick 手动写 Width = ratio * ActualWidth。
                        //   完全绕开 WPF ProgressBar 的 "PART_Track.ActualWidth 未就绪 → Indicator 继承 Grid Stretch 而满格" 的布局时序 bug。
                        // 【★ 修复：进度条比例改为 Grid Star 列宽驱动（与 Ava 端同构）】
                        //  之前 indicator.Width = host.ActualWidth * ratio：RefreshSchedule 重建后 host 未布局
                        //  （ActualWidth=0）→ 进度条 Width 从 0 假重置，待 LayoutUpdated 才写回正确比例；
                        //  时间跳变/5s 硬刷新频繁重建时，用户感知"跳变后比例显示错误 / 流速不对（忽快忽慢/倒退）"。
                        //  现改为两列比例：Column0 = 进度比例（indicator Stretch 自动占该列全宽），Column1 = 剩余空白。
                        //  比例由 Grid 布局自动分配 → 重建后布局瞬间即正确，无假重置、无延迟、无依赖时序。
                        var host = new Grid
                        {
                            Height = 3.0,
                            Margin = new Thickness(0, 3, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            ClipToBounds = true
                        };
                        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
                        var bg = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(
                                0x40, accentColor.R, accentColor.G, accentColor.B)),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                        Grid.SetColumnSpan(bg, 2);   // 背景铺满整个进度条区域
                        var indicator = new Border
                        {
                            Background = new SolidColorBrush(accentColor),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,   // 占满 Column0（比例列）
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                        Grid.SetColumn(indicator, 0);
                        host.Children.Add(bg);
                        host.Children.Add(indicator);
                        _currentProgressHost = host;
                        _currentProgressIndicator = indicator;

                        Grid.SetRow(host, 1);
                        coursePanel.Children.Add(host);
                    }

                    courseCell.Child = coursePanel;
                    Grid.SetRow(courseCell, rowIndex);
                    Grid.SetColumn(courseCell, 0);
                    table.Children.Add(courseCell);

                    // ===== 时间列 =====
                    var timeStr =
                        $"{FormatHhMm(ReflectGetStartTime(layoutItem))} - {FormatHhMm(ReflectGetEndTime(layoutItem))}";

                    var timeCell = new Border
                    {
                        Padding = new Thickness(8, 6, 2, 6),
                        BorderBrush = new SolidColorBrush(separatorColor),
                        BorderThickness = borderThickness,
                        Background = isCurrent ? highlightBg : Brushes.Transparent
                    };
                    var timeText = new TextBlock
                    {
                        Text = timeStr,
                        FontSize = Math.Max(8, fontSize - 1),
                        Foreground = isCurrent ? textForeground : subtextForeground,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    timeCell.Child = timeText;
                    Grid.SetRow(timeCell, rowIndex);
                    Grid.SetColumn(timeCell, 1);
                    table.Children.Add(timeCell);

                    // ---------- 课间休息插入行（仅 Breaking 且在两节课之间，否则不插入）----------
                    //   ★ guard：breakLayoutItemWpf != null 对齐 Ava L1789，避免 SDK 异常返回 null 时后续代码抛 NullReference
                    if (i == breakInsertAfterClassIdx && breakLayoutItemWpf != null)
                    {
                        rowIndex++;
                        table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        // 课间底色：淡灰 8% 透明度；不使用强调色以免与当前课高亮混淆
                        byte breakA = (byte)Math.Clamp((int)Math.Round(0.08 * 255), 0, 255);
                        byte breakG = isDark ? (byte)0xFF : (byte)0x00;
                        var breakBg = new SolidColorBrush(Color.FromArgb(breakA, breakG, breakG, breakG));

                        // 左：课间名（BreakNameText / 默认"课间休息"），使用次级灰字
                        var breakCellL = new Border
                        {
                            Padding = new Thickness(2, 4, 8, 4),
                            BorderBrush = new SolidColorBrush(separatorColor),
                            BorderThickness = new Thickness(0, 0, 0, 1),
                            Background = breakBg
                        };
                        var breakTb = new TextBlock
                        {
                            Text = breakNameWpf,
                            FontSize = Math.Max(8, fontSize - 1),
                            Foreground = subtextForeground,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        breakCellL.Child = breakTb;
                        Grid.SetRow(breakCellL, rowIndex);
                        Grid.SetColumn(breakCellL, 0);
                        table.Children.Add(breakCellL);

                        // 右：课间时间 = 上节课End ~ 下节课Start（= SDK 课间项目 Start/End 保证一致）
                        var breakCellR = new Border
                        {
                            Padding = new Thickness(8, 4, 2, 4),
                            BorderBrush = new SolidColorBrush(separatorColor),
                            BorderThickness = new Thickness(0, 0, 0, 1),
                            Background = breakBg
                        };
                        var breakTimeStr = $"{FormatHhMm(breakStartWpf)} - {FormatHhMm(breakEndWpf)}";
                        var breakTimeTb = new TextBlock
                        {
                            Text = breakTimeStr,
                            FontSize = Math.Max(8, fontSize - 1),
                            Foreground = subtextForeground,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        breakCellR.Child = breakTimeTb;
                        Grid.SetRow(breakCellR, rowIndex);
                        Grid.SetColumn(breakCellR, 1);
                        table.Children.Add(breakCellR);

                        // 课间进度条：位于插入行下方，横跨两列；自绘 Grid+两层 Border（与当前课进度条同一套，避免 ProgressBar 布局时序 bug）
                        // 【★ 修复：进度条比例改为 Grid Star 列宽驱动（与 Ava 端同构，消除重建后 Width=0 假重置/比例错误/流速不对）】
                        var breakHost = new Grid
                        {
                            Height = 3.0,
                            Margin = new Thickness(0, 3, 0, 3),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            ClipToBounds = true
                        };
                        breakHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        breakHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
                        var breakPbBg = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(
                                0x40, accentColorNow.R, accentColorNow.G, accentColorNow.B)),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                        Grid.SetColumnSpan(breakPbBg, 2);   // 背景铺满整个进度条区域
                        var breakIndicator = new Border
                        {
                            Background = new SolidColorBrush(accentColorNow),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,   // 占满 Column0（比例列）
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                        Grid.SetColumn(breakIndicator, 0);
                        breakHost.Children.Add(breakPbBg);
                        breakHost.Children.Add(breakIndicator);
                        _currentBreakProgressHost = breakHost;
                        _currentBreakProgressIndicator = breakIndicator;
                        _currentBreakLayoutItem = breakLayoutItemWpf;
                        rowIndex++;
                        table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        Grid.SetRow(breakHost, rowIndex);
                        Grid.SetColumn(breakHost, 0);
                        Grid.SetColumnSpan(breakHost, 2);
                        table.Children.Add(breakHost);

                        // ==== 把"插入行"的三个视觉元素登记到列表：左Cell + 右Cell + 进度条Host（后续 ENTER/EXIT 动画以这三个为目标）====
                        //   注：每个元素独立 Opacity + RenderTransform.Y，避免"共用同一个父容器 TranslateTransform 导致 Row Height 动画跳动"
                        builtBreakVisuals = new List<FrameworkElement>(capacity: 3)
                        {
                            breakCellL,
                            breakCellR,
                            breakHost,
                        };
                    }

                    // ---------- 课间分隔线（2px，70% 不透明）：第 i 节课与下一节课之间存在课间 → 在该行间隙插横向分隔线 ----------
                    //  放在课间插入行之后，保证"课程行 → (课间行) → 分隔线 → 下一课程行"顺序；
                    //  仅追加新行，不改动既有 rowIndex/进度条/高亮锚定逻辑。
                    if (breakSeparatorAfterWpf.Contains(i))
                    {
                        rowIndex++;
                        table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        var sepLine = new Border
                        {
                            Height = 2,
                            // 深色主题白色、浅色主题黑色（随主题重建自动更新）
                            Background = isDark ? Brushes.White : Brushes.Black,
                            Opacity = 0.7,   // 【分隔线】70% 不透明
                            Margin = new Thickness(0, 1, 0, 1),
                            CornerRadius = new CornerRadius(1),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        Grid.SetRow(sepLine, rowIndex);
                        Grid.SetColumn(sepLine, 0);
                        Grid.SetColumnSpan(sepLine, 2);   // 跨整行（两列）
                        table.Children.Add(sepLine);
                    }
                }

                rootPanel.Children.Add(table);
            }

            // 更新到窗口
            _containerBorder.Child = rootPanel;
            _tableGrid = rootPanel.Children.OfType<Grid>().FirstOrDefault();

            // ===== 课间行 ENTER 动画 & 登记 =====
            //  规则：本次 RefreshSchedule 成功构建"插入行"（builtBreakVisuals != null）
            //        且 上一帧不是 break（调用时刻快照 prevWasBreakWpf == false） → 播放 250ms 进入动画
            //  例外 1：如果当前正处于"EXIT 动画"（课间→上课移除动画），说明这里的 RefreshSchedule 是"非-break 的重建"（非 insert），
            //          因此不应该把 builtBreakVisuals 绑定到持久字段，也不播放 enter（它本应为 null 已由前面逻辑保证）。
            //  例外 2：冷启动 isColdStartWpf（_lastWpfRefreshStateCode == -1 首次构建 UI）—— 即使启动时正处于 Breaking，
            //          也必须跳过 ENTER 动画直接显示最终态（否则 250ms Opacity=0 起点 + 动画值优先级，用户会误判「时间表卡住不更新」）。
            //  【★ 修复：初始化时课间文本 250ms 透明卡在上一课里】
            //     时序 bug：启动时 StartInternal.Post 显式调用 RefreshSchedule #1（sentinel=-1 → isColdStartWpf=true → 跳过 ENTER，正确）；
            //               紧接着 500ms Timer 第一 Tick UpdateProgress：stateOrBreakChanged = Breaking code - (-1) = true → Post RefreshSchedule #2；
            //               RefreshSchedule #2 入口 _lastWpfRefreshStateCode 已被 #1 写为 Breaking int → isColdStartWpf=false；
            //               同时 #1 由 StartInternal.Post 直接调，不走 UpdateProgress finally，同步 _wasBreakLastTickWpf=true 没执行；
            //               所以 RefreshSchedule #2 看到 _wasBreakLastTickWpf=false（字段初始值）→ 命中 ENTER → 课间行 250ms Opacity=0 淡入！
            //               250ms 内用户只有上一课 C0 文本可见 → 误认为"课间文本卡在上一课文本里"。
            //     修复策略（与 Ava L1871-L1913 严格 1:1 镜像）：
            //        用局部变量 prevWasBreakWpf 先保存 ENTER 判断时的原始快照（=本次 RefreshSchedule 被调时刻的上一帧快照），
            //        再立刻把字段 _wasBreakLastTickWpf 写为 true（只要本次构建确实产出了课间行）。
            //        这样 ENTER 判断用"调用时刻的快照"保证正常"非 break→break"场景仍正确命中 ENTER；
            //        而写字段保证下一次 RefreshSchedule 调用时看到"上次已在 break 中"→ 跳过 ENTER（修复初始化 2 次重建的 bug）。
            // ===== 用户要求：删除淡化渐变动画以外的所有动画 WPF =====
            //  课间行 ENTER 跳过 250ms Opacity 淡入 + TranslateY -24→0，立即写终态 Opacity=1 / TT.Y=0（淡化仅用于 Hover，保留）
            _currentBreakRowVisualsWpf = builtBreakVisuals;
            if (builtBreakVisuals != null)
            {
                _ = _wasBreakLastTickWpf; // 保留读取兼容旧 3 步快照字段模式（ENTER 已删除）
                _wasBreakLastTickWpf = true;
                foreach (var el in builtBreakVisuals)
                {
                    try
                    {
                        // 清 WPF 动画时钟：之前版本可能有 Storyboard 值优先级残留（用户切回"动画关"的兼容）
                        el.BeginAnimation(UIElement.OpacityProperty, null);
                        el.Opacity = 1.0;
                    }
                    catch { /* ignore */ }
                    try
                    {
                        if (el.RenderTransform is System.Windows.Media.TranslateTransform ttW)
                        {
                            ttW.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
                            ttW.Y = 0.0;
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            else
            {
                // 本次没构建课间行（非 Breaking）→ 写 false 保留状态机语义
                _wasBreakLastTickWpf = false;
                _currentBreakProgressIndicator = null;
                _currentBreakProgressHost = null;
                _currentBreakLayoutItem = null;
            }

            // ===== 【★ 用户报告：悬浮窗初始化以进度条 0% 为假快照 → 后续异常】集中兜底（WPF 端 1:1 镜像 Ava）=====
            //  根因：RefreshSchedule 多处调用后只有 DebugTime/needRefresh 两个分支手动 Apply 真实 ratio，
            //        其余（冷启动 StartInternal / Settings 打开开关 / 5s 硬清理 / EXIT 同步）遗漏 →
            //        新 indicator Width = 构造默认 0、indicator.Tag 未缓存 ratio →
            //        LayoutUpdated 首次触发（host ActualWidth 就绪）时 L1679 ApplyProgressRatio 内 cached=Tag ?? 0.0 → 写 Width=0（假 0%）。
            //  修复：RefreshSchedule 末尾无条件 Apply 一次本帧真实 ratio，宿主 ActualWidth 仍 0 时走 Tag 缓存分支，
            //        LayoutUpdated 回调后就能读到最新 ratio，不会再 fallback 0。
            try
            {
                var svc = IAppHost.TryGetService<ILessonsService>();
                if (svc != null)
                {
                    var s = svc.CurrentState;
                    bool onC = s == TimeState.OnClass;
                    bool brk = s == TimeState.Breaking;

                    // 课间进度条（真实时间定位当前课间空隙，SDK 滞后时也能正确推进/重置）
                    if (brk)
                    {
                        var nn = GetClassIslandNow().TimeOfDay;
                        var biB = FindBreakItemByRealTimeWpf(nn, out var gsB, out var geB);
                        if (biB != null && _currentBreakProgressIndicator != null)
                        {
                            double tb = (geB - gsB).TotalSeconds;
                            if (tb > 0)
                            {
                                double rb = Math.Clamp((nn - gsB).TotalSeconds / tb, 0.0, 1.0);
                                ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, rb);
                            }
                            else ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);
                        }
                        else if (_currentBreakProgressIndicator != null)
                            ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);
                    }
                    else if (_currentBreakProgressIndicator != null)
                        ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);

                    // 当前课进度条
                    if (onC)
                    {
                        // 高亮行：RefreshSchedule 上方已保证 onC 时 _currentProgressClassIndex 非 -1（SDK 匹配 + 真实时间兜底）
                        if (_currentProgressClassIndex >= 0 &&
                            _currentProgressClassIndex < _currentRowLayoutItems.Count &&
                            _currentProgressIndicator != null)
                        {
                            var rowLi = _currentRowLayoutItems[_currentProgressClassIndex];
                            var st = ReflectGetStartTime(rowLi);
                            var ed = ReflectGetEndTime(rowLi);
                            double t = (ed - st).TotalSeconds;
                            if (t <= 0) { ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0); }
                            else
                            {
                                var nn = GetClassIslandNow().TimeOfDay;
                                double p = Math.Clamp((nn - st).TotalSeconds / t, 0.0, 1.0);
                                ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, p);
                            }
                        }
                        else if (_currentProgressIndicator != null)
                            ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0);
                    }
                    else if (_currentProgressIndicator != null)
                        ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0);
                }
            }
            catch { /* 绝对兜底：即使 Apply 失败，下一次 Timer Tick UpdateProgress 也会修正，不影响 RefreshSchedule 主流程 */ }
        }
        catch (Exception ex)
        {
            // 错误回退
            try
            {
                var darkFallback = ThemeHelper.IsDarkTheme();
                // 字号（错误面板也要按新字号规则）
                var errFontSize = (int)Math.Round(Math.Clamp(_settings.FloatingScheduleFontScale, 8, 32));
                var errPanel = new StackPanel
                {
                    MinWidth = 200
                };
                errPanel.Children.Add(new TextBlock
                {
                    Text = "悬浮时间表",
                    FontSize = errFontSize,
                    FontWeight = FontWeights.Bold,
                    Foreground = darkFallback ? Brushes.White : Brushes.Black,
                    Margin = new Thickness(0, 0, 0, 6)
                });
                errPanel.Children.Add(new TextBlock
                {
                    Text = "加载中...",
                    FontSize = Math.Max(8, errFontSize - 2),
                    Foreground = darkFallback
                        ? new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA))
                        : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
                });
                _containerBorder.Child = errPanel;
            }
            catch
            {
                // 忽略
            }
        }
    }

    // 【★ 修复：进度条有时总是满的 & 课间→上课卡住100%】辅助：把 ratio(0~1) 反映到自绘进度条（Grid Star 列宽驱动）
    private static void ApplyProgressRatio(FrameworkElement host, Border indicator, double ratio)
    {
        // 【★ 修复：进度条比例改为 Grid Star 列宽驱动（与 Ava 端同构）】
        //  host 是两列 Grid（Column0=进度比例 / Column1=剩余）。直接设置两列 Star 宽度 →
        //  Grid 布局自动按比例分配 indicator 宽度，不依赖 host.ActualWidth 是否就绪、无需 LayoutUpdated 延迟写回：
        //  - RefreshSchedule 重建后布局瞬间即正确（消除"重建后 Width=0 假重置/比例显示错误"）
        //  - 时间跳变/5s 硬刷新频繁重建时进度条连续正确（消除"流速不对/忽快忽慢/倒退"）
        if (host is not Grid hostGrid || hostGrid.ColumnDefinitions.Count < 2) return;
        ratio = double.IsNaN(ratio) ? 0.0 : Math.Clamp(ratio, 0.0, 1.0);
        hostGrid.ColumnDefinitions[0].Width = new GridLength(ratio, GridUnitType.Star);
        hostGrid.ColumnDefinitions[1].Width = new GridLength(Math.Max(0.0, 1.0 - ratio), GridUnitType.Star);
    }

    // RefreshSchedule 构建结果快照（用于 500ms Tick 检测状态/课间变化 → 立刻整表重建）
    private int _lastWpfRefreshStateCode = -1;
    private long _lastWpfRefreshBreakStartTicks = -1;
    private long _lastWpfRefreshBreakEndTicks = -1;

    // 点击穿透 / 指针淡化 last-applied 防抖（避免每 Tick 重复调 Win32/重复刷 Opacity）
    private bool _lastWpfClickThroughApplied = false;
    private bool _lastWpfFadedApplied = false;

    // ============ 课间插入/移除动画（用户：进入/离开 Breaking 时 0.25s 过渡）============
    //   —— ENTER：RefreshSchedule 构建新插入行时，对左/右 Cell + 进度条 容器执行 Opacity 0→1 + TranslateY -24px→0（250ms CubicEaseOut）
    //   —— EXIT：当 UpdateProgress 检测到 Breaking→非Breaking 需要移除插入行时，跳过"needRefresh 整表重建"，
    //            先对当前已渲染的 插入行 UI 执行 Opacity 1→0 + TranslateY 0→24px；动画 Completed 再触发 RefreshSchedule 真移除。
    //            这样避免"刷新前控件瞬间消失"的"突兀感"，也不需要在 RefreshSchedule 保留旧 UI。
    //   —— ENTER/EXIT 动画使用统一封装 AnimateBreakRowEnter / AnimateBreakRowExit。
    private const int BreakRowAnimationMs = 250;   // 用户需求：0.25s
    private const double BreakRowEnterTranslatePx = -24.0;   // 从上方 24px 飘入（ENTER：Y 负 → 0）
    private const double BreakRowExitTranslatePx = 24.0;    // 从原位向下 24px 飘出（EXIT：0 → Y 正）
    private bool _wasBreakLastTickWpf = false;
    private bool _breakRowExitAnimatingWpf = false;
    private int _breakRowExitPendingStateCode = -1;
    private long _breakRowExitPendingBreakStart = -1;
    private long _breakRowExitPendingBreakEnd = -1;
    // 【修复：课间向上位移 0.5-1s】ENTER 动画专用取消令牌：
    //   WPF Storyboard 虽不能跨线程 Cancel，但可在 ENTER 动画"写起点（Opacity=0 + TT.Y=-24px）之前"检查 token，
    //   与 Ava 端 Enter CTS 7 处清理完全同构：RefreshSchedule ENTER 前/5s 硬清理/DebugTime 清理/Stop/EXIT 分支
    //   共 5 处会 Cancel → 飞在的 Enter 动画即使 Synchronous FireEnter 已经在 Dispatcher 队列也会直接跳过，
    //   绝不覆盖"新 RefreshSchedule 已经写入的正确课间 UI（Y=0, Opacity=1）"造成向上位移闪烁。
    private CancellationTokenSource? _breakRowEnterCtsWpf;
    // 当前已渲染的"课间插入行"可视化元素（EXIT 动画时用）：左上/右上 Cell、下方进度条 host；会在 RefreshSchedule 插入行后赋值；非 Breaking 时为 null。
    private List<FrameworkElement>? _currentBreakRowVisualsWpf;

    // ========== 调试时间即时刷新（WPF 端，严格与 Ava 端对齐）==========
    //  上次 RefreshSchedule 成功构建课表时的调试时间 Date（MinValue = 从未 Refresh 过）。
    //  - 用途：500ms Tick dateChanged 兜底；
    //  - 说明：GetClassIslandNow().Date 包含 DebugTimeOffsetSeconds/TimeOffsetSeconds 偏移，而非本地 DateTime.Today。
    private DateTime _lastWpfRefreshDate = DateTime.MinValue;
    // 宿主 SettingsService：IAppHost.TryGetService<SettingsService>() 结果（WPF SDK 直接引用了 ClassIsland.Models，可用反射拿 Settings 属性 + INotifyPropertyChanged 订阅）
    private object? _hostSettingsServiceWpf;
    // 宿主 SettingsService.Settings（INotifyPropertyChanged 源）：保存引用以便 Detach 时 -=PropertyChanged
    private System.ComponentModel.INotifyPropertyChanged? _hostSettingsObjWpf;
    private PropertyChangedEventHandler? _hostSettingsChangedHandlerWpf;

    // ========== 悬浮窗层级重设频率 4 模式（WPF 端，严格与 Ava 端对齐）==========
    //   0 OnWindowZOrderChanged → Win32 子类化 WM_WINDOWPOSCHANGED
    //   1 OnForegroundWindowChanged → 反射宿主 IWindowPlatformService.ForegroundWindowChanged
    //   2 Every50Ms / 3 Every1Ms → DispatcherTimer
    private DispatcherTimer? _topmostRefreshTimerWpf;
    private object? _windowPlatformServiceWpf;
    private Delegate? _foregroundWindowChangedHandlerWpf;
    // ForegroundWindowChanged 订阅方式：0 = IWindowRuleService.ForegroundWindowChanged event；1 = IWindowPlatformService.Register/Unregister 方法
    private int _fgSubModeWpf = 0;
    private System.Reflection.MethodInfo? _fgUnregisterMethodWpf;   // 方式 1：UnregisterForegroundWindowChangedEvent 方法，Detach 时调用
    private IntPtr _topmostOldWndProcWpf = IntPtr.Zero;
    private IntPtr _topmostHookedHwndWpf = IntPtr.Zero;
    private TopmostWndProcWpf? _topmostWndProcDelegateWpf;
    private FloatingTopmostRefreshMode _currentTopmostModeWpf = (FloatingTopmostRefreshMode)(-1);
    // 【修复 Issue 1】WPF ApplyWindowLayer 重入计数器（与 Ava 端语义一致）
    //  >0：ApplyWindowLayer 执行栈中（含内部 SetWindowPos 同步触发 WM_WINDOWPOSCHANGED）→ Mode 0 WndProc 抑制 Post 防止死循环
    private int _inApplyWindowLayerWpf = 0;

    // ========== 5s 全量同步硬兜底（WPF 端，严格与 Ava 端对齐）==========
    //  策略（Experience 1279696 双定时器分层）：复用 500ms 现有 Tick，每 10 次 = 5s 全量 SDK 状态校验。
    private const int HardSyncTickIntervalWpf = 10;  // 500ms × 10 = 5 秒
    private int _hardSyncTickCounterWpf = 0;
    // 【★ 时间跳变 → 进度条立即更新兜底】上次 UpdateProgress Tick 的 now（用于检测跳变 >2s → 立即强制刷新，
    //  覆盖"宿主 Settings.PropertyChanged 事件丢失导致 OnHostSettingsDebugTimeChanged 未触发、进度条只能等 5s 硬刷新"的场景）
    private DateTime _lastTickNowWpf = DateTime.MinValue;
    // 宿主 OnHostSettingsDebugTimeChanged 最近一次刷新的 TickCount（UpdateProgress 跳变检测据此跳过，避免与宿主刷新重复重建闪 0）
    private long _lastHostTimeChangeRefreshTicksWpf = long.MinValue;
    // 【★ 连续课间即时切换】SDK 课间 item 超时容忍（真实 now > SDK item.End + 此值 → 视为 SDK 滞后，改用真实时间空隙定位）
    private const double EndOverrunToleranceSecWpf = 0.05;

    private static void AnimateBreakRowEnterWpf(IEnumerable<FrameworkElement> elements, CancellationToken ct = default)
    {
        var list = elements as List<FrameworkElement> ?? elements.ToList();
        if (list.Count == 0) return;
        // 【修复：课间向上位移 0.5-1s】WPF 端取消路径：在"写 Opacity=0 + TT.Y=-24 起点"前检查 token。
        //   —— 若在 RefreshSchedule #N 重建后，旧 FireEnter 的 ct 被 Cancel，则直接跳过整个动画，
        //      不覆写新 RefreshSchedule 已经写入的 Opacity=1/Y=0 最终态（对应 Ava 端 ct.ThrowIfCancellationRequested）。
        if (ct.IsCancellationRequested) { return; }
        var sb = new Storyboard { FillBehavior = FillBehavior.Stop, Duration = TimeSpan.FromMilliseconds(BreakRowAnimationMs) };
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        foreach (var el in list)
        {
            if (el == null) continue;
            if (ct.IsCancellationRequested)
            {
                // 进入循环途中被 Cancel：保证已处理到一半的元素也落到最终态 1/0（不残留 0/-24 造成位移闪烁）
                try { el.BeginAnimation(UIElement.OpacityProperty, null); } catch { /* ignore */ }
                try { el.Opacity = 1; } catch { /* ignore */ }
                if (el.RenderTransform is System.Windows.Media.TranslateTransform ttR)
                {
                    try { ttR.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null); } catch { /* ignore */ }
                    try { ttR.Y = 0; } catch { /* ignore */ }
                }
                return;
            }
            // 初始化起始值：完全透明 + 向上 24px 偏移
            el.Opacity = 0;
            el.RenderTransform = new TranslateTransform(0, BreakRowEnterTranslatePx);

            var opacityAnim = new DoubleAnimationUsingKeyFrames();
            opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BreakRowAnimationMs)), ease));
            Storyboard.SetTarget(opacityAnim, el);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(opacityAnim);

            var translateYAnim = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(translateYAnim, el);
            Storyboard.SetTargetProperty(translateYAnim, new PropertyPath("RenderTransform.Y"));
            translateYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(BreakRowEnterTranslatePx, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            translateYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BreakRowAnimationMs)), ease));
            sb.Children.Add(translateYAnim);
        }
        // Completed 落地：Opacity=1 / Y=0，避免 Storyboard 动画值优先级 > 本地值导致后续再改没反应
        sb.Completed += (_, _) =>
        {
            foreach (var el in list)
            {
                try
                {
                    el.BeginAnimation(UIElement.OpacityProperty, null);
                    el.Opacity = 1;
                    if (el.RenderTransform is TranslateTransform tt)
                    {
                        tt.BeginAnimation(TranslateTransform.YProperty, null);
                        tt.Y = 0;
                    }
                }
                catch { /* ignore */ }
            }
        };
        // 最后：仍可能在 sb.Begin 之前被 Cancel（外部路径调 _breakRowEnterCtsWpf.Cancel）
        if (ct.IsCancellationRequested)
        {
            // 尚未启动即取消：对所有元素写最终态，不残留起点。
            foreach (var el in list)
            {
                try { el.BeginAnimation(UIElement.OpacityProperty, null); } catch { /* ignore */ }
                try { el.Opacity = 1; } catch { /* ignore */ }
                if (el.RenderTransform is TranslateTransform tt)
                {
                    try { tt.BeginAnimation(TranslateTransform.YProperty, null); } catch { /* ignore */ }
                    try { tt.Y = 0; } catch { /* ignore */ }
                }
            }
            return;
        }
        sb.Begin();
    }

    private void AnimateBreakRowExitWpf(IEnumerable<FrameworkElement> elements, Action onCompleted)
    {
        var list = elements as List<FrameworkElement> ?? elements.ToList();
        if (list.Count == 0) { onCompleted?.Invoke(); return; }
        var sb = new Storyboard { FillBehavior = FillBehavior.HoldEnd, Duration = TimeSpan.FromMilliseconds(BreakRowAnimationMs) };
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        foreach (var el in list)
        {
            if (el == null) continue;
            // 起始值取当前值（正常情况 = Opacity 1 / Y 0）
            double startOpacity = Math.Clamp(el.Opacity, 0, 1);
            double startY = (el.RenderTransform as TranslateTransform)?.Y ?? 0;
            if (!(el.RenderTransform is TranslateTransform))
            {
                el.RenderTransform = new TranslateTransform(0, startY);
            }

            var opacityAnim = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(opacityAnim, el);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
            opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(startOpacity, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BreakRowAnimationMs)), ease));
            sb.Children.Add(opacityAnim);

            var translateYAnim = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(translateYAnim, el);
            Storyboard.SetTargetProperty(translateYAnim, new PropertyPath("RenderTransform.Y"));
            translateYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(startY, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            translateYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(BreakRowExitTranslatePx, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BreakRowAnimationMs)), ease));
            sb.Children.Add(translateYAnim);
        }
        bool done = false;
        void Finish() { if (done) return; done = true; try { sb.Stop(); } catch { /* ignore */ } onCompleted?.Invoke(); }
        sb.Completed += (_, _) => Finish();
        try
        {
            sb.Begin();
            // 兜底 250ms + 20ms：防止 Storyboard.Completed 未按预期触发（极端渲染阻塞）导致卡死 EXIT 无法移除
            _ = Task.Delay(BreakRowAnimationMs + 20).ContinueWith(_ =>
                _window?.Dispatcher.BeginInvoke(new Action(Finish)), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        catch
        {
            Finish();
        }
    }

    private void UpdateProgress()
    {
        if (_window == null || !_window.IsVisible) return;

        bool breaking = false;  // 提升到 try 外以便 finally 内同步 _wasBreakLastTickWpf
        try
        {
            // ========== 指针移入淡化：每 Tick 先重判定（与 CI2 实现模型完全一致）==========
            //  原因：点击穿透模式下 WPF 接收不到 MouseEnter/MouseLeave 事件，必须每 500ms 拉一次全局光标
            ApplyHoverFade();

            // 【修复：调试时间不立刻刷新 - Date 跳变兜底】
            //  每 Tick 先取一次完整调试时间。dateChanged 检测（跨天调试/NTP 回跳）→ 强制 stateOrBreakChanged=true。
            var now = GetClassIslandNow();

            var lessonsService = IAppHost.TryGetService<ILessonsService>();
            if (lessonsService == null)
            {
                RefreshSchedule();
                return;
            }

            var curStateWpf = lessonsService.CurrentState;
            bool onClass = curStateWpf == TimeState.OnClass;
            breaking = curStateWpf == TimeState.Breaking;

            // ====== 悬浮窗随时间状态变化（新增：Breaking 插入行+课间进度条）======
            //  状态(None/PrepareOnClass/OnClass/Breaking/AfterSchool)任一切换；或课间项目变化（跨课间）立刻重建。
            int stateCode = (int)curStateWpf;
            long breakStartTicks = -1;
            long breakEndTicks = -1;
            object? curBreakLi = null;
            if (breaking)
            {
                curBreakLi = ReflectGetCurrentTimeLayoutItemService(lessonsService);
                if (curBreakLi != null && ReflectGetTimeType(curBreakLi) == 1)
                {
                    breakStartTicks = ReflectGetStartTime(curBreakLi).Ticks;
                    breakEndTicks = ReflectGetEndTime(curBreakLi).Ticks;
                }
            }
            bool stateOrBreakChanged = stateCode != _lastWpfRefreshStateCode ||
                                       breakStartTicks != _lastWpfRefreshBreakStartTicks ||
                                       breakEndTicks != _lastWpfRefreshBreakEndTicks;

            // 【★ 时间跳变 → 进度条立即更新兜底】（WPF 1:1 镜像 Ava）
            //  若 now 与上次 Tick 差距超过 2s（正常 500ms Tick 差距 ~0.5s），视为时间跳变
            //  （宿主调试偏移 / 系统时间 / NTP 同步导致）。若宿主 Settings.PropertyChanged 事件丢失
            //  （订阅失败 / 宿主版本差异 / 事件被吞）→ OnHostSettingsDebugTimeChanged 未触发 →
            //  进度条只能等 5s 硬刷新才用新时间重建（用户感知"时间跳变进度条不立即更新"）。
            //  修复：跳变时强制 stateOrBreakChanged=true → needRefresh → RefreshSchedule 真实时间定位 + Apply。
            //  去重：最近 1s 内宿主已触发刷新（OnHostSettingsDebugTimeChanged 执行过）→ 跳过，避免重复重建闪 0。
            if (_lastTickNowWpf != DateTime.MinValue &&
                Math.Abs((now - _lastTickNowWpf).TotalSeconds) > 2.0 &&
                Environment.TickCount64 - _lastHostTimeChangeRefreshTicksWpf > 1000)
            {
                stateOrBreakChanged = true;
            }
            _lastTickNowWpf = now;

            // 【修复：调试时间不立刻刷新 - Date 跳变兜底强制 Refresh】
            //  跨天调试场景（DebugTimeOffsetSeconds ±86400）：即使宿主 Settings PropertyChanged 事件丢失，
            //  500ms Tick 检测到 now.Date 与上次 Refresh 快照 _lastWpfRefreshDate 不一致 → 强制 needRefresh=true。
            //  _lastWpfRefreshDate==MinValue 代表首次启动（sentinel=-1 已强制 Refresh），此时不触发避免重复。
            if (_lastWpfRefreshDate != DateTime.MinValue && now.Date != _lastWpfRefreshDate)
                stateOrBreakChanged = true;

            // 【★ 连续课间即时切换触发源（保留 breaking 分支）】（WPF 1:1 镜像 Ava）
            //  课间行是动态插入的，连续课间 B1→B2 时 SDK CurrentTimeLayoutItem 滞后 1-2 Tick 仍返回 B1：
            //  真实 now 超过 SDK 课间 item.End + 容忍 → 强制 needRefresh → RefreshSchedule 用真实时间空隙
            //  定位 B2 → 课间行立即切到 B2 + 进度条正确重置。（连续上课无此问题：课程行总在列表里，
            //  高亮切换由 5s 硬刷新兜底，无需 500ms 级即时检测，故不再对 onClass 分支做超时强制。）
            if (!stateOrBreakChanged && breaking && curBreakLi != null)
            {
                var eBrkWpf = ReflectGetEndTime(curBreakLi);
                if (now.TimeOfDay.TotalSeconds - eBrkWpf.TotalSeconds > EndOverrunToleranceSecWpf)
                    stateOrBreakChanged = true;
            }

            // 【5s 全量强制刷新（WPF，用户：任何情况下每 5s 刷新，而不是兜底）】
            //  Experience 1279696 双定时器分层（低频做重数据刷新） + Experience 1034846 到点必刷（不得用比对/EXIT 阻断）。
            //   · 500ms Tick（高频轻量）：进度条 ratio + state/break/date 即时 needRefresh 触发（保持 500ms 灵敏度）
            //   · 每 10 Tick = 5s（低频全量）：【无条件】RefreshSchedule 整表重建。无论是否刚刷新过/EXIT 在进行/状态变过。
            //  保护：到点先清 EXIT 所有标志（即使没在 EXIT 也安全清零）→ WPF EXIT 无 CTS 机制，但 UpdateProgress EXIT onCompleted 开头 guard：
            //         `if (!_breakRowExitAnimatingWpf) return;` → 清标志后 250ms onCompleted 到点直接 return，不会用旧 pending 脏覆盖新 RefreshSchedule 写入的正确快照。
            //  串行性：WPF DispatcherTimer Tick 在 UI 线程跑，RefreshSchedule 也在 UI 线程，无重入（回调返回前不会再 Tick）。
            _hardSyncTickCounterWpf = (_hardSyncTickCounterWpf + 1) % HardSyncTickIntervalWpf;
            if (_hardSyncTickCounterWpf == 0)
            {
                //  (1) 强制清理 EXIT 动画：与 OnHostSettingsDebugTimeChangedWpf Post 开头完全一致的复位块（无条件执行，安全）
                // 【修复：课间向上位移 0.5-1s】同步 Cancel ENTER CTS + Dispose（镜像 Ava 5s 硬清理块）
                // 【★ 修复：3-10s 向上位移（5s 硬清理 ENTER 假命中）】
                //   根因：镜像 Ava 端 P0 级错误：L1994 原先无条件 `_wasBreakLastTickWpf = false`；
                //         本函数顶部 L1926 `breaking = curStateWpf == TimeState.Breaking` 已根据 SDK 真实状态算出本 Tick 布尔快照，
                //         若用户仍处于 Breaking 时段（breaking=true），紧接着 RefreshSchedule ENTER 块快照 prevWasBreakWpf = 被抹零 false →
                //         `!prevWasBreakWpf && !EXIT && !ColdStart=true` → ENTER 误命中 → 写 Opacity=0 / TT.Y=-24 起点 → 用户"先正常 3-10s → 向上位移"。
                //   修复：写入本 Tick 真实 breaking 快照（非冷启动的 DebugTime/Stop 重置合理地写 false，因为 sentinel=-1 会 isColdStart 短路 ENTER）。
                try { _breakRowEnterCtsWpf?.Cancel(); } catch { /* ignore */ }
                try
                {
                    var oldEnter = System.Threading.Interlocked.Exchange(ref _breakRowEnterCtsWpf, null);
                    oldEnter?.Dispose();
                } catch { /* ignore */ }
                _breakRowExitAnimatingWpf = false;
                // 【★ 关键修复行】5s 硬清理块不再一刀切 false，写本 Tick 真实 breaking 快照。
                _wasBreakLastTickWpf = breaking;
                _breakRowExitPendingStateCode = -1;
                _breakRowExitPendingBreakStart = -1;
                _breakRowExitPendingBreakEnd = -1;
                _currentBreakRowVisualsWpf = null;
                _currentBreakProgressIndicator = null;
                _currentBreakProgressHost = null;
                _currentBreakLayoutItem = null;

                //  (2) 【核心要求】任何情况下每 5s 全量重建时间表
                RefreshSchedule();
                // 【★ 修复：5s 硬刷新不生效（双重重建闪 0）】（WPF 1:1 镜像 Ava）
                //  5s 硬刷新已无条件 RefreshSchedule（覆盖本 Tick 一切状态变化），若此处 stateOrBreakChanged 仍为 true
                //  （本 Tick 状态变化 / 时间跳变检测 / dateChanged 触发）→ 下方 needRefresh 会再 Post 一次 RefreshSchedule
                //  → 同 Tick 双重重建：进度条每次重建 Width 先归 0 再恢复，用户感知"5s 硬刷新不生效/进度条闪 0"。
                //  修复：无条件重建后立即置 false，仅保留下方 indicator 状态一致性检查触发。
                stateOrBreakChanged = false;
            }

            // ===== 用户：删除淡化渐变动画以外的所有动画 WPF =====
            //   课间 EXIT 跳过 250ms TranslateY/淡出 Storyboard：立即复位标志 + 写快照 + RefreshSchedule 重建（无过渡）。
            //   淡化仅用于 Hover Opacity 1↔0.5（保留 ApplyHoverFade），与课间行移除无关。
            bool willExitBreakRow = false;
            if (_wasBreakLastTickWpf && !breaking && !_breakRowExitAnimatingWpf &&
                _currentBreakRowVisualsWpf != null && _currentBreakRowVisualsWpf.Count > 0)
            {
                willExitBreakRow = true;
                stateOrBreakChanged = false;
                try { _breakRowEnterCtsWpf?.Cancel(); } catch { /* ignore */ }
                try
                {
                    var oldEnter = System.Threading.Interlocked.Exchange(ref _breakRowEnterCtsWpf, null);
                    oldEnter?.Dispose();
                } catch { /* ignore */ }
                _breakRowExitAnimatingWpf = false;
                _wasBreakLastTickWpf = false;
                _currentBreakRowVisualsWpf = null;
                _lastWpfRefreshStateCode = stateCode;
                _lastWpfRefreshBreakStartTicks = breakStartTicks;
                _lastWpfRefreshBreakEndTicks = breakEndTicks;
                try { RefreshSchedule(); } catch { /* ignore */ }
                _currentBreakProgressIndicator = null;
                _currentBreakProgressHost = null;
            }

            bool needRefresh = stateOrBreakChanged ||
                               (onClass && _currentProgressIndicator == null) ||
                               (!onClass && _currentProgressIndicator != null) ||
                               (breaking && _currentBreakProgressIndicator == null) ||
                               (!breaking && _currentBreakProgressIndicator != null);
            // EXIT 动画窗口期间：任何 needRefresh（包括 break indicator 非空触发的）都延后到动画结束回调统一处理，
            // 避免课间行控件在动画进行中被提前销毁导致"动画播放一半突然消失"。
            if (_breakRowExitAnimatingWpf)
                needRefresh = false;

            if (needRefresh)
            {
                // 刷新前把快照写好，避免排队期间下一帧重复 Post RefreshSchedule
                _lastWpfRefreshStateCode = stateCode;
                _lastWpfRefreshBreakStartTicks = breakStartTicks;
                _lastWpfRefreshBreakEndTicks = breakEndTicks;
                RefreshSchedule();

                // ===== 【修复课间→上课卡 100% 根因 #1】=====
                //  之前这里直接 `return`：RefreshSchedule 重建的新当前课/课间进度条 Width 保持构造默认 0，
                //  必须等下一帧（500ms 后）UpdateProgress 再写。若课间结束过渡到上课"刚好跨越 SDK 整点"：
                //   - 旧课间 ratio 在 break 最后一帧 = 1.0 且 ApplyProgressRatio 成功写完 Width = 全宽；
                //   - RefreshSchedule 构建新的当前课进度条新控件（indicator.Width=0），但没立刻写 ratio；
                //   - 用户视角 = 看起来进度条一直停在 100% 不更新。
                //  修复：RefreshSchedule 后不再 return，让"本 Tick 后半段 ApplyProgressRatio 同步跑一次"，
                //   结合上方 ApplyProgressRatio 内"LayoutUpdated 只有真成功写 Width 才取消订阅"的兜底，
                //   无论宿主 ActualWidth 是否就绪，本次都能把正确的最新 ratio 落下去。

                // RefreshSchedule 会重置 `_currentBreakLayoutItem`/lesson 当前 item，所以重新取 curBreakLi 与 layoutItem
                if (breaking)
                {
                    curBreakLi = ReflectGetCurrentTimeLayoutItemService(lessonsService!);
                    if (curBreakLi != null && ReflectGetTimeType(curBreakLi) != 1) curBreakLi = null;
                }
                // onClass/breaking 本身不需要重算（它们来自 curStateWpf，RefreshSchedule 内已按该快照构建高亮/插入行）
            }

            // ========== 课间进度条（Breaking 命中时：真实时间定位当前课间项计算百分比，每个课间分别走进度） ==========
            if (breaking && _currentBreakProgressIndicator != null)
            {
                // 复用 UpdateProgress 开头已拿到的 now（含最新 DebugTimeOffset 偏移）
                var nn = now.TimeOfDay;
                var biCur = FindBreakItemByRealTimeWpf(nn, out var breakStart, out var breakEnd);
                if (biCur != null)
                {
                    var breakTotal = (breakEnd - breakStart).TotalSeconds;
                    if (breakTotal > 0)
                    {
                        var bElapsed = (nn - breakStart).TotalSeconds;
                        double ratio = Math.Clamp(bElapsed / breakTotal, 0.0, 1.0);
                        ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, ratio);
                    }
                    else
                    {
                        ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);
                    }
                }
                else
                {
                    // 真实时间不在任何课间项（SDK Breaking 但实际已到上课/放学）→ 课间进度条 0
                    ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);
                }
            }
            else if (_currentBreakProgressIndicator != null)
            {
                ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);
            }

            if (!onClass || _currentProgressIndicator == null)
            {
                return;
            }

            // 计算上课进度条（用高亮行区间 + 真实时间，不依赖 SDK 滞后 item）
            //  RefreshSchedule / needRefresh 已保证 onClass 时 _currentProgressClassIndex 非 -1（SDK 匹配 + 真实时间兜底）
            if (_currentProgressClassIndex < 0 || _currentProgressClassIndex >= _currentRowLayoutItems.Count)
            {
                ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0);
                return;
            }
            var layoutItem = _currentRowLayoutItems[_currentProgressClassIndex];
            if (layoutItem == null)
            {
                ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0);
                return;
            }
            var start = ReflectGetStartTime(layoutItem);
            var end = ReflectGetEndTime(layoutItem);
            var totalSec = (end - start).TotalSeconds;
            if (totalSec <= 0)
            {
                ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0);
                return;
            }

            var nowTimeOfDay = GetClassIslandNow().TimeOfDay;
            var elapsed = (nowTimeOfDay - start).TotalSeconds;
            var progress = Math.Clamp(elapsed / totalSec, 0.0, 1.0);
            ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, progress);
        }
        catch
        {
            // 忽略更新异常
        }
        finally
        {
            // EXIT 动画窗口不更新快照（EXIT 动画结束回调内已手动复位，避免 250ms 窗口内下一帧误判）
            if (!_breakRowExitAnimatingWpf)
                _wasBreakLastTickWpf = breaking;
        }
    }

    // ===================== 宿主 SettingsService 调试时间变更订阅（WPF 端，与 Ava 端逻辑严格对齐）=====================
    //  目的：用户在 ClassIsland 设置-调试-调试时间偏移 修改 DebugTimeOffsetSeconds / TimeOffsetSeconds
    //        时 <=1 UI 帧内立刻 RefreshSchedule；而非等 500ms Timer 下一帧（用户感知"时间表没立刻刷新"）。
    //  退订：Stop / EnableFloatingSchedule=false 分支 Detach，防止宿主 singleton SettingsService 强引用插件对象导致内存泄漏。

    /// <summary>
    /// WPF 版：尝试拿宿主 SettingsService 并订阅 Settings.PropertyChanged。
    ///  【修复 #2 (Code Review)：Assembly 扫描优先级优化】
    ///   第一优先：ILessonsService 实现类 Assembly（宿主 dll）单 dll 按类名 "SettingsService" 查找 —— LessonsService 与 SettingsService
    ///             100% 同在宿主 ClassIsland.dll / ClassIsland.Core.dll，单 dll 遍历必命中，避免前一版"先 AppDomain 所有 Assemblies × 2 命名空间" N×2 次 asm.GetType() 开销。
    ///   第二 fallback：AppDomain.CurrentDomain.GetAssemblies() 按已知命名空间全扫（仅 LessonsAssembly 名命不中的极端 SDK 拆分场景才启用）。
    /// </summary>
    private void EnsureHostSettingsSubscriptionWpf()
    {
        if (_hostSettingsChangedHandlerWpf != null) return;   // 已订阅，避免重复 +=

        try
        {
            // 先尝试 IAppHost.Host 服务提供器路径（与 Ava 完全一致，作为首选）
            object? settingsSvc = null;
            var sp = IAppHost.Host?.Services;
            if (sp != null)
            {
                // ===== 第 1 优先：LessonsService 实现类单 dll 名匹配（100% 命中场景，只扫 1 个 dll）=====
                var lsSvc = IAppHost.TryGetService<ILessonsService>();
                if (lsSvc != null)
                {
                    var lsAsm = lsSvc.GetType().Assembly;
                    foreach (var t in lsAsm.GetTypes())
                    {
                        if (t.Name == "SettingsService" && !t.IsInterface && !t.IsAbstract)
                        {
                            settingsSvc = sp.GetService(t);
                            if (settingsSvc != null) break;
                        }
                    }
                }

                // ===== 第 2 fallback：AppDomain 全 Assemblies × 已知命名空间（极端 SDK 拆分 dll 才用，避免 N×2 次 asm.GetType() 常态开销）=====
                if (settingsSvc == null)
                {
                    Type? tSettings = null;
                    foreach (var ns in new[] { "ClassIsland.Services.SettingsService",
                                               "ClassIsland.Core.Services.SettingsService" })
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            tSettings = asm.GetType(ns, throwOnError: false, ignoreCase: false);
                            if (tSettings != null) break;
                        }
                        if (tSettings != null)
                        {
                            settingsSvc = sp.GetService(tSettings);
                            if (settingsSvc != null) break;
                        }
                    }
                }
            }

            if (settingsSvc == null)
            {
                System.Diagnostics.Debug.WriteLine("[AdvancedTimeIsland] EnsureHostSettingsSubscriptionWpf: 获取 SettingsService 失败，调试时间将依赖 500ms Tick 兜底刷新");
                return;
            }

            // 反射拿 SettingsService.Settings 属性（实现了 INotifyPropertyChanged）
            var propSettings = settingsSvc.GetType().GetProperty("Settings",
                BindingFlags.Instance | BindingFlags.Public);
            if (propSettings == null)
            {
                System.Diagnostics.Debug.WriteLine("[AdvancedTimeIsland] EnsureHostSettingsSubscriptionWpf: .Settings 属性不存在，调试时间将依赖 500ms Tick 兜底刷新");
                return;
            }
            var settingsObj = propSettings.GetValue(settingsSvc);
            if (settingsObj is not System.ComponentModel.INotifyPropertyChanged npcSettings)
            {
                System.Diagnostics.Debug.WriteLine("[AdvancedTimeIsland] EnsureHostSettingsSubscriptionWpf: Settings 未实现 INotifyPropertyChanged，调试时间将依赖 500ms Tick 兜底刷新");
                return;
            }

            _hostSettingsServiceWpf = settingsSvc;
            _hostSettingsObjWpf = npcSettings;
            _hostSettingsChangedHandlerWpf = OnHostSettingsDebugTimeChangedWpf;
            npcSettings.PropertyChanged += _hostSettingsChangedHandlerWpf;
            System.Diagnostics.Debug.WriteLine("[AdvancedTimeIsland] EnsureHostSettingsSubscriptionWpf: 订阅宿主 Settings.PropertyChanged 成功（调试时间即时刷新已启用）");
        }
        catch (Exception)
        {
            // 失败兜底：清零所有订阅字段，下次开关/重启可能命中（或永久用 500ms Tick 兜底，功能不丢失只是延迟）
            _hostSettingsChangedHandlerWpf = null;
            _hostSettingsObjWpf = null;
            _hostSettingsServiceWpf = null;
        }
    }

    /// <summary>
    /// WPF 版：Detach 宿主 Settings PropertyChanged 订阅（Stop / 关闭开关时调用）。
    /// 安全：重复调用或从未 Attach 均不抛错。
    /// </summary>
    private void DetachHostSettingsSubscriptionWpf()
    {
        try
        {
            if (_hostSettingsChangedHandlerWpf != null && _hostSettingsObjWpf != null)
            {
                _hostSettingsObjWpf.PropertyChanged -= _hostSettingsChangedHandlerWpf;
            }
        }
        catch { /* 忽略 Detach 异常（例如 Settings 实例已被宿主销毁，但 -= 仍抛） */ }
        finally
        {
            _hostSettingsChangedHandlerWpf = null;
            _hostSettingsObjWpf = null;
            _hostSettingsServiceWpf = null;
        }
    }

    // ===================== 悬浮窗层级重设频率 4 模式 Attach/Detach（WPF 端，严格对齐 Ava）=====================
    //  生命周期：StartInternal ShowWindow 后 Attach；Stop / EnableFloatingSchedule=false Detach；
    //            FloatingScheduleTopmostRefreshMode PropertyChanged → Detach 旧 + ReAttach 新 + Apply 一次。

    // 反射宿主服务类型查找（与 Ava FindHostServiceType 语义一致：先扫 ClassIsland.* Assembly，再全扫）
    private static Type? FindHostServiceTypeWpf(string typeName)
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                if (asm.GetName().Name?.StartsWith("ClassIsland") == true)
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
        catch { /* ignore */ }
        return null;
    }

    /// <summary>
    /// WPF 版：Detach 悬浮窗层级重设全部触发源（三件套：Timer.Stop/-=Event/Win32 解子类）。安全可重入。
    /// </summary>
    private void DetachTopmostRefreshWpf()
    {
        try
        {
            // Timer（Mode 2/3）
            try { _topmostRefreshTimerWpf?.Stop(); } catch { /* ignore */ }
            _topmostRefreshTimerWpf = null;

            // Mode 1：ForegroundWindowChanged 退订（区分 event / Register 方法两种订阅方式）
            if (_windowPlatformServiceWpf != null && _foregroundWindowChangedHandlerWpf != null)
            {
                try
                {
                    if (_fgSubModeWpf == 1 && _fgUnregisterMethodWpf != null)
                    {
                        // IWindowPlatformService.UnregisterForegroundWindowChangedEvent(handler)
                        _fgUnregisterMethodWpf.Invoke(_windowPlatformServiceWpf, new object[] { _foregroundWindowChangedHandlerWpf });
                    }
                    else
                    {
                        // IWindowRuleService.ForegroundWindowChanged -= handler
                        var evt = _windowPlatformServiceWpf.GetType().GetEvent("ForegroundWindowChanged",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        evt?.RemoveEventHandler(_windowPlatformServiceWpf, _foregroundWindowChangedHandlerWpf);
                    }
                }
                catch { /* ignore */ }
            }
            _windowPlatformServiceWpf = null;
            _foregroundWindowChangedHandlerWpf = null;
            _fgSubModeWpf = 0;
            _fgUnregisterMethodWpf = null;

            // Mode 0：Win32 解子类
            if (_topmostOldWndProcWpf != IntPtr.Zero && _topmostHookedHwndWpf != IntPtr.Zero)
            {
                try
                {
                    SetWindowLongPtrWpf(_topmostHookedHwndWpf, GWLP_WNDPROC_WPF, _topmostOldWndProcWpf);
                }
                catch { /* ignore */ }
            }
            _topmostOldWndProcWpf = IntPtr.Zero;
            _topmostHookedHwndWpf = IntPtr.Zero;
            _topmostWndProcDelegateWpf = null;

            _currentTopmostModeWpf = (FloatingTopmostRefreshMode)(-1);
        }
        catch { /* 防御：Detach 全流程不抛 */ }
    }

    /// <summary>
    /// WPF 版：按 mode Attach 一路触发源。w 必须已 Show（确保 WindowInteropHelper.Handle 非零）。
    /// </summary>
    // Mode 0/1 共用：订阅宿主"前台窗口变化"事件（ForegroundWindowChanged）。
    //  注意：宿主 IWindowPlatformService 的 ForegroundWindowChanged 是 Register/UnregisterForegroundWindowChangedEvent
    //        **方法**（非 event）；真正的 event 在 IWindowRuleService.ForegroundWindowChanged
    //        （ClassIsland.Core.Abstractions.Services.IWindowRuleService，宿主 MainWindow 也用它）。
    //  同时尝试 1.x/2.x 命名空间；失败仅 Debug 输出（Mode 2/3 定时器 + ApplyWindowLayer PropertyChanged 仍兜底）。
    private void AttachForegroundWindowChangedWpf()
    {
        if (IAppHost.Host?.Services == null) return;
        try
        {
            var handlerMethod = new Action<object?, EventArgs?>(OnForegroundWindowChangedForTopmostWpf);
            // 首选：IWindowRuleService.ForegroundWindowChanged event（ClassIsland.Core.Abstractions.Services.IWindowRuleService）
            Type? tRuleSvc = FindHostServiceTypeWpf("ClassIsland.Core.Abstractions.Services.IWindowRuleService")
                          ?? FindHostServiceTypeWpf("ClassIsland.Core.Services.IWindowRuleService")
                          ?? FindHostServiceTypeWpf("ClassIsland.Services.WindowRuleService")
                          ?? FindHostServiceTypeWpf("ClassIsland.Core.Services.WindowRuleService");
            if (tRuleSvc != null)
            {
                var svc = IAppHost.Host.Services.GetService(tRuleSvc);
                var evt = svc?.GetType().GetEvent("ForegroundWindowChanged",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (evt?.EventHandlerType != null)
                {
                    var deleg = Delegate.CreateDelegate(evt.EventHandlerType, this, handlerMethod.Method, throwOnBindFailure: false);
                    if (deleg != null)
                    {
                        evt.AddEventHandler(svc, deleg);
                        _windowPlatformServiceWpf = svc;
                        _foregroundWindowChangedHandlerWpf = deleg;
                        _fgSubModeWpf = 0;
                        _currentTopmostModeWpf = FloatingTopmostRefreshMode.OnForegroundWindowChanged;
                        return;
                    }
                }
            }
            // 兜底：IWindowPlatformService.Register/UnregisterForegroundWindowChangedEvent 方法（1.x 兼容）
            Type? tWinPlatform = FindHostServiceTypeWpf("ClassIsland.Services.IWindowPlatformService")
                              ?? FindHostServiceTypeWpf("ClassIsland.Core.Services.IWindowPlatformService")
                              ?? FindHostServiceTypeWpf("ClassIsland.Services.WindowPlatformService")
                              ?? FindHostServiceTypeWpf("ClassIsland.Core.Services.WindowPlatformService");
            if (tWinPlatform != null)
            {
                var svcP = IAppHost.Host.Services.GetService(tWinPlatform);
                var mReg = svcP?.GetType().GetMethod("RegisterForegroundWindowChangedEvent",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var mUnreg = svcP?.GetType().GetMethod("UnregisterForegroundWindowChangedEvent",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (svcP != null && mReg != null && mUnreg != null && mReg.GetParameters().Length == 1)
                {
                    var paramType = mReg.GetParameters()[0].ParameterType;   // EventHandler<ForegroundWindowChangedEventArgs>
                    var delegP = Delegate.CreateDelegate(paramType, this, handlerMethod.Method, throwOnBindFailure: false);
                    if (delegP != null)
                    {
                        mReg.Invoke(svcP, new object[] { delegP });
                        _windowPlatformServiceWpf = svcP;
                        _foregroundWindowChangedHandlerWpf = delegP;
                        _fgSubModeWpf = 1;
                        _fgUnregisterMethodWpf = mUnreg;
                        _currentTopmostModeWpf = FloatingTopmostRefreshMode.OnForegroundWindowChanged;
                        return;
                    }
                }
            }
        }
        catch { /* 忽略：订阅失败仅日志级，Mode 2/3 定时器兜底 */ }
    }

    private void AttachTopmostRefreshWpf(Window w, FloatingTopmostRefreshMode mode)
    {
        if (w == null) return;
        DetachTopmostRefreshWpf();   // Clean start

        try
        {
            switch (mode)
            {
                case FloatingTopmostRefreshMode.OnWindowZOrderChanged:
                    {
                        var hwnd = new System.Windows.Interop.WindowInteropHelper(w).Handle;
                        if (hwnd != IntPtr.Zero)
                        {
                            _topmostWndProcDelegateWpf = TopmostWndProcHookWpf;
                            _topmostOldWndProcWpf = SetWindowLongPtrWpf(hwnd, GWLP_WNDPROC_WPF,
                                System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_topmostWndProcDelegateWpf));
                            _topmostHookedHwndWpf = hwnd;
                        }
                        // 同时订阅 ForegroundWindowChanged（兜底）：窗口层级变化主要由前台窗口变化引起
                        AttachForegroundWindowChangedWpf();
                        _currentTopmostModeWpf = FloatingTopmostRefreshMode.OnWindowZOrderChanged;
                    }
                    break;

                case FloatingTopmostRefreshMode.OnForegroundWindowChanged:
                    // 宿主 IWindowRuleService.ForegroundWindowChanged（修复：IWindowPlatformService 的 ForegroundWindowChanged 是方法非 event）
                    AttachForegroundWindowChangedWpf();
                    break;

                case FloatingTopmostRefreshMode.Every50Ms:
                case FloatingTopmostRefreshMode.Every1Ms:
                case FloatingTopmostRefreshMode.Every2s:
                    {
                        // 周期化层级刷新：50ms / 1ms / 2s（Every2s 为最省资源的低频档）
                        var intervalMs = mode switch
                        {
                            FloatingTopmostRefreshMode.Every50Ms => 50,
                            FloatingTopmostRefreshMode.Every1Ms => 1,
                            _ => 2000
                        };
                        _topmostRefreshTimerWpf = new DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background)
                        {
                            Interval = TimeSpan.FromMilliseconds(intervalMs)
                        };
                        _topmostRefreshTimerWpf.Tick += (_, _) => ApplyWindowLayer();
                        _topmostRefreshTimerWpf.Start();
                        _currentTopmostModeWpf = mode;
                    }
                    break;
            }
        }
        catch
        {
            DetachTopmostRefreshWpf();   // 失败兜底：避免半截 Attach 泄漏
        }
    }

    // ---- Mode 0 WndProc 钩子 ----
    private IntPtr TopmostWndProcHookWpf(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (msg == WM_WINDOWPOSCHANGED_WPF && lParam != IntPtr.Zero)
            {
                int flagsOffset = IntPtr.Size * 2 + 16;   // WINDOWPOS: HWND*2 + int(x,y,cx,cy) = 4 ints + flags(UINT) at offset IntPtr.Size*2 + 16
                uint flags = (uint)System.Runtime.InteropServices.Marshal.ReadInt32(lParam, flagsOffset);
                // 【修复 Issue 1】同 Ava 端：_inApplyWindowLayerWpf > 0 表示本消息是 ApplyWindowLayer→SetWindowPos 自己触发的，抑制重入
                if ((flags & SWP_NOZORDER) == 0
                    && System.Threading.Volatile.Read(ref _inApplyWindowLayerWpf) == 0)
                {
                    // RunOnUi 调用 ApplyWindowLayer
                    var disp = System.Windows.Application.Current?.Dispatcher;
                    if (disp != null && !disp.CheckAccess())
                        disp.BeginInvoke(ApplyWindowLayer, System.Windows.Threading.DispatcherPriority.Background);
                    else
                        ApplyWindowLayer();
                }
            }
        }
        catch { /* 防御：任何异常不阻断 CallWindowProc */ }

        if (_topmostOldWndProcWpf != IntPtr.Zero)
            return CallWindowProc(_topmostOldWndProcWpf, hWnd, msg, wParam, lParam);
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    // ---- Mode 1 ForegroundWindowChanged 回调 ----
    private void OnForegroundWindowChangedForTopmostWpf(object? sender, EventArgs? e)
    {
        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp == null) return;
        if (disp.CheckAccess()) ApplyWindowLayer();
        else disp.BeginInvoke(ApplyWindowLayer, System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// WPF 版：宿主 Settings 变化回调（调试时间相关属性）→ UI 线程 Post 强制 RefreshSchedule。
    /// 覆盖属性：DebugTimeOffsetSeconds / TimeOffsetSeconds / DebugTimeSpeed / ExactTimeServer / IsExactTimeEnabled。
    /// </summary>
    private void OnHostSettingsDebugTimeChangedWpf(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;
        bool isTimeRelated =
            e.PropertyName == "DebugTimeOffsetSeconds" ||
            e.PropertyName == "TimeOffsetSeconds" ||
            e.PropertyName == "DebugTimeSpeed" ||
            e.PropertyName == "ExactTimeServer" ||
            e.PropertyName == "IsExactTimeEnabled";
        if (!isTimeRelated) return;

        // WPF Dispatcher 切 UI 线程（与 Ava DispatcherPriority.Loaded 等价，排在 DebugPage 赋值之后下一 UI 帧）
        var disp = _window?.Dispatcher ?? System.Windows.Application.Current?.Dispatcher;
        if (disp == null) return;
        disp.BeginInvoke(new Action(() =>
        {
            if (_window == null || _containerBorder == null) return;
            try
            {
                // 【修复 #1 (Code Review)：调试时间变更 Post 前取消正在进行的 EXIT 动画竞态】
                //  WPF EXIT 动画用 Storyboard + Task.Delay(250+20ms) 双保险，无法像 Ava 用 CTS 中途取消。
                //  解法：这里先清 EXIT 所有标志 + pending 值，再在 UpdateProgress 的 EXIT onCompleted 开头加 guard：
                //  if (!_breakRowExitAnimatingWpf) return; —— 这样 250ms 后 Storyboard 到点时，
                //  onCompleted 会立刻 return，不会再用旧 pending 脏覆盖新写入的正确 state/Date 快照。
                // 【修复：课间向上位移 0.5-1s】同步 Cancel ENTER CTS + Dispose：
                //   调试时间跳变是 RefreshSchedule 连续重建 + FireEnter 异步排队最常见场景（与 Ava 端 ENTER CTS 清理完全同构）。
                try { _breakRowEnterCtsWpf?.Cancel(); } catch { /* ignore */ }
                try
                {
                    var oldEnter = System.Threading.Interlocked.Exchange(ref _breakRowEnterCtsWpf, null);
                    oldEnter?.Dispose();
                } catch { /* ignore */ }
                _breakRowExitAnimatingWpf = false;
                _wasBreakLastTickWpf = false;
                _breakRowExitPendingStateCode = -1;
                _breakRowExitPendingBreakStart = -1;
                _breakRowExitPendingBreakEnd = -1;
                _currentBreakRowVisualsWpf = null;
                _currentBreakProgressIndicator = null;
                _currentBreakProgressHost = null;
                _currentBreakLayoutItem = null;

                // ① 失效所有缓存 sentinel：保证 RefreshSchedule 跳过动画（冷启动分支 isColdStartWpf=true）、且下一次 Tick 检测一定命中 needRefresh
                _lastWpfRefreshStateCode = -1;
                _lastWpfRefreshBreakStartTicks = -1;
                _lastWpfRefreshBreakEndTicks = -1;
                _lastWpfRefreshDate = DateTime.MinValue;

                // ② 同步 RefreshSchedule：重建整表（Date 锚用 GetClassIslandNow().Date = 最新调试偏移后的今天）
                RefreshSchedule();
                // 记录宿主刷新时间戳：UpdateProgress 时间跳变检测据此跳过（1s 内不重复重建，避免闪 0）
                _lastHostTimeChangeRefreshTicksWpf = Environment.TickCount64;

                // ③ 同步 Apply 一次"当前真实 ratio"（与 needRefresh 分支镜像逻辑，防止进度条 Width 构造默认 0 导致"瞬间为空"）
                try
                {
                    var svc = IAppHost.TryGetService<ILessonsService>();
                    if (svc == null) return;
                    var s = svc.CurrentState;
                    bool onC = s == TimeState.OnClass;
                    bool brk = s == TimeState.Breaking;

                    // 课间进度条（真实时间定位当前课间空隙，SDK 滞后时也能正确推进/重置）
                    if (brk)
                    {
                        var nn = GetClassIslandNow().TimeOfDay;
                        var biB = FindBreakItemByRealTimeWpf(nn, out var gsB, out var geB);
                        if (biB != null && _currentBreakProgressIndicator != null)
                        {
                            double tb = (geB - gsB).TotalSeconds;
                            if (tb > 0)
                            {
                                double rb = Math.Clamp((nn - gsB).TotalSeconds / tb, 0.0, 1.0);
                                ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, rb);
                            }
                            else ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);
                        }
                        else if (_currentBreakProgressIndicator != null)
                            ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);
                    }
                    else if (_currentBreakProgressIndicator != null)
                        ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, 0.0);

                    // 当前课进度条（用 RefreshSchedule 刚定位的高亮行区间 + 真实时间，不依赖 SDK 滞后 item）
                    if (onC)
                    {
                        if (_currentProgressClassIndex >= 0 &&
                            _currentProgressClassIndex < _currentRowLayoutItems.Count &&
                            _currentProgressIndicator != null)
                        {
                            var rowLi = _currentRowLayoutItems[_currentProgressClassIndex];
                            var st = ReflectGetStartTime(rowLi);
                            var ed = ReflectGetEndTime(rowLi);
                            double t = (ed - st).TotalSeconds;
                            if (t <= 0) { ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0); }
                            else
                            {
                                var nn = GetClassIslandNow().TimeOfDay;
                                double p = Math.Clamp((nn - st).TotalSeconds / t, 0.0, 1.0);
                                ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, p);
                            }
                        }
                        else ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0);
                    }
                    else ApplyProgressRatio(_currentProgressHost!, _currentProgressIndicator, 0.0);
                }
                catch { /* 忽略：下一帧 UpdateProgress 兜底写 ratio */ }
            }
            catch { /* 忽略：Post Refresh 异常，下一帧仍会兜底 */ }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
