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

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

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
        _hoverFadeTimer.Tick += (_, _) => ApplyHoverFade();
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
        _refreshTimer.Stop();
        _hoverFadeTimer?.Stop();
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
                });
            }
            else
            {
                RunOnUi(() =>
                {
                    _refreshTimer.Stop();
                    _hoverFadeTimer?.Stop();
                    HideWindow();
                });
            }
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
            Top = _settings.FloatingSchedulePositionY
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
        // 保存位置
        _settings.FloatingSchedulePositionX = (int)Math.Round(_window.Left);
        _settings.FloatingSchedulePositionY = (int)Math.Round(_window.Top);
    }

    private void OnContainerMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_window == null || _containerBorder == null) return;

        // 【新增】点击穿透模式下，禁止 WPF 处理任何拖拽（否则 DragMove 会强制吞掉本应穿透的消息）
        if (_settings.FloatingScheduleClickThrough) return;

        // ========== 用户文档 二.5 方案A（重置：彻底简化）：交给系统原生 HTCAPTION 拖拽 ==========
        //  为什么比之前的手动 SetWindowPos 方案好？
        //  1) 系统已经做好了 Per-Monitor V2 DPI、多屏跨 DPI、Snap 吸附兼容、DWM 合成时序优化（30 年 Win32 官方优化）；
        //  2) 代码量从 300+ 行复杂手动节流/缓存/捕获/DllImport → 降到 3 行核心：DragMove()
        //  3) 不再有"两套拖拽并行（文档二.5元凶）、双重缩放（文档一.3.2）、坐标单位冲突（文档一.2）、消息重入（文档二.6）"
        //  保留的有益优化：拖动前冻结 SizeToContent.Manual（防止 Auto 尺寸在拖的过程中做重测量造成合成抖动）
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

        // ===== DragMove 返回 = 一次拖动结束，恢复状态 =====
        try
        {
            if (_window.SizeToContent != _preDragSizeToContent) _window.SizeToContent = _preDragSizeToContent;
            if (_window.Topmost != _preDragTopmost) _window.Topmost = _preDragTopmost;
            // 结束后重新应用 z-order 层（DragMove 内部移动过程中不改变 z-order，结束后确保仍是设置里的 Topmost/Bottom）
            ApplyWindowLayer();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "拖动结束恢复 SizeToContent/Topmost/z-order 出错，忽略。");
        }

        e.Handled = true;
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

        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(_window).Handle;
            if (hwnd == IntPtr.Zero) return;

            if (_settings.FloatingScheduleWindowLayer == FloatingScheduleWindowLayer.Topmost)
            {
                _window.Topmost = true;
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
                _window.Topmost = false;
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch
        {
            // 忽略 Win32 调用异常
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
            ClassPlan? classPlan = null;
            if (lessonsService != null)
            {
                classPlan = lessonsService.GetClassPlanByDate(DateTime.Today);
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
                foreach (var x in validItemsRaw)
                {
                    if (x != null && ReflectGetTimeType(x) == 0) validItems.Add(x);
                }
                validItems.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));

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
                var nowTimeOfDay = Plugin.GetCurrentTime().TimeOfDay;
                var curStateWpf = lessonsService?.CurrentState ?? TimeState.None;

                if (lessonsService != null && curStateWpf == TimeState.OnClass)
                {
                    // 根据 CurrentTimeLayoutItem 找到当前行
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

                // ---------- 当前课高亮底色：强调色 × 40% 透明度（Alpha ≈ 0x66）----------
                // 不再使用之前硬编码的深浅蓝 2 套：直接拿应用 Primary 强调色，满足"当前课整行高亮=强调色40%透明度"。
                var accentColorNow = GetAccentColor();
                const double wpfHighlightAlpha = 0.40;
                byte wpfHighlightA = (byte)Math.Clamp((int)Math.Round(wpfHighlightAlpha * 255), 0, 255);
                var highlightBg = new SolidColorBrush(
                    Color.FromArgb(wpfHighlightA, accentColorNow.R, accentColorNow.G, accentColorNow.B));

                // ---------- 课间休息插入行判定（与 Avalonia 完全一致的 5 条规则）----------
                int breakInsertAfterClassIdx = -1;
                TimeSpan breakStartWpf = default;
                TimeSpan breakEndWpf = default;
                string breakNameWpf = "课间休息";
                object? breakLayoutItemWpf = null;
                if (curStateWpf == TimeState.Breaking && classRows.Count >= 2 && lessonsService != null)
                {
                    var bLi = ReflectGetCurrentTimeLayoutItemService(lessonsService);
                    if (bLi != null && ReflectGetTimeType(bLi) == 1)
                    {
                        var bs = ReflectGetStartTime(bLi);
                        var be = ReflectGetEndTime(bLi);
                        var firstStart = ReflectGetStartTime(classRows[0].LayoutItem);
                        var lastEnd = ReflectGetEndTime(classRows[classRows.Count - 1].LayoutItem);
                        if (bs >= firstStart && be <= lastEnd)
                        {
                            for (int i = 0; i < classRows.Count - 1; i++)
                            {
                                var prevEnd = ReflectGetEndTime(classRows[i].LayoutItem);
                                var nextStart = ReflectGetStartTime(classRows[i + 1].LayoutItem);
                                if (prevEnd == bs && nextStart == be)
                                {
                                    breakInsertAfterClassIdx = i;
                                    breakStartWpf = bs;
                                    breakEndWpf = be;
                                    breakNameWpf = ReflectGetBreakNameText(bLi);
                                    breakLayoutItemWpf = bLi;
                                    break;
                                }
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
                        var teacherText = new TextBlock
                        {
                            Text = teacherTitle,
                            FontSize = Math.Max(8, fontSize - 3),
                            Foreground = subtextForeground,
                            VerticalAlignment = VerticalAlignment.Center,
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
                        var host = new Grid
                        {
                            Height = 3.0,
                            Margin = new Thickness(0, 3, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            ClipToBounds = true
                        };
                        var bg = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(
                                0x40, accentColor.R, accentColor.G, accentColor.B)),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                        var indicator = new Border
                        {
                            Background = new SolidColorBrush(accentColor),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Width = 0   // 初始 0；下一帧 UpdateProgress 按比例赋值
                        };
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
                    if (i == breakInsertAfterClassIdx)
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
                        var breakHost = new Grid
                        {
                            Height = 3.0,
                            Margin = new Thickness(0, 3, 0, 3),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            ClipToBounds = true
                        };
                        var breakPbBg = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(
                                0x40, accentColorNow.R, accentColorNow.G, accentColorNow.B)),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                        var breakIndicator = new Border
                        {
                            Background = new SolidColorBrush(accentColorNow),
                            CornerRadius = new CornerRadius(1.5),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Width = 0
                        };
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
                }

                rootPanel.Children.Add(table);
            }

            // 更新到窗口
            _containerBorder.Child = rootPanel;
            _tableGrid = rootPanel.Children.OfType<Grid>().FirstOrDefault();

            // ===== 课间行 ENTER 动画 & 登记 =====
            //  规则：本次 RefreshSchedule 成功构建"插入行"（builtBreakVisuals != null）
            //        且 上一帧不是 break（_wasBreakLastTickWpf == false） → 播放 250ms 进入动画
            //  例外：如果当前正处于"EXIT 动画"（课间→上课移除动画），说明这里的 RefreshSchedule 是"非-break 的重建"（非 insert），
            //         因此不应该把 builtBreakVisuals 绑定到持久字段，也不播放 enter（它本应为 null 已由前面逻辑保证）。
            _currentBreakRowVisualsWpf = builtBreakVisuals;
            if (builtBreakVisuals != null && !_wasBreakLastTickWpf && !_breakRowExitAnimatingWpf)
            {
                try { AnimateBreakRowEnterWpf(builtBreakVisuals); } catch { /* ignore */ }
            }
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

    // 【修复：进度条有时总是满的 & 课间→上课卡住100%】辅助：把 ratio(0~1) 反映到自绘进度条（前景 Border.Width = ratio * host.ActualWidth）
    //  关键修复 1：host.ActualWidth=0 时订阅 LayoutUpdated 后，**只有真正成功写 Width 后才 -= 事件**；宿主 ActualWidth 仍 0 时保留订阅等下一帧（否则新宿主永远收不到下一次回调，indicator.Width 永久留在初始值 —— 若前一状态 Width 刚好是容器全宽就变成"卡100%"）。
    //  关键修复 2：每轮都允许覆盖 indicator.Tag 缓存 ratio（即使已订阅过 LayoutUpdated），这样"课间→上课切换首帧 RefreshSchedule 之后立刻再 Apply 新 ratio=0.x"会覆盖先前缓存的旧值；最终 LayoutUpdated 应用的是**最新** ratio，不会出现"新进度条沿用旧值导致卡满"。
    //  关键修复 3：用 ConditionalWeakTable 做"宿主 → 订阅中 handler"的弱引用追踪，不污染 FrameworkElement.Tag（Tag 可能被 UI 模板复用）。
    private static readonly ConditionalWeakTable<FrameworkElement, object> _wpfPendingLayoutHandlers = new();

    private static void ApplyProgressRatio(FrameworkElement host, Border indicator, double ratio)
    {
        if (host == null || indicator == null) return;
        ratio = double.IsNaN(ratio) ? 0.0 : Math.Clamp(ratio, 0.0, 1.0);
        var aw = host.ActualWidth;
        if (aw > 0.0)
        {
            indicator.Width = aw * ratio;
            // 宿主宽度就绪：清除之前可能订阅的 LayoutUpdated（防御性，避免残留回调对后续 Tick 再无效触发）
            if (_wpfPendingLayoutHandlers.TryGetValue(host, out var obj) && obj is EventHandler pendingHandler)
            {
                host.LayoutUpdated -= pendingHandler;
                _wpfPendingLayoutHandlers.Remove(host);
            }
        }
        else
        {
            // 每轮 Apply 都把 ratio 刷到 indicator.Tag：保证下一次 LayoutUpdated 回调用的是最新 ratio
            indicator.Tag = ratio;

            // 已经订阅过（通过 weak-table 追踪）：不重复订阅事件
            if (_wpfPendingLayoutHandlers.TryGetValue(host, out _))
                return;

            EventHandler? handler = null;
            handler = (_, _) =>
            {
                if (host == null || indicator == null)
                {
                    if (handler != null) host.LayoutUpdated -= handler;
                    _wpfPendingLayoutHandlers.Remove(host);
                    return;
                }
                double widthNow = host.ActualWidth;
                if (widthNow <= 0) return;   // 宿主尚未 Ready：保持订阅，等待下一次 LayoutUpdated
                try
                {
                    var cached = indicator.Tag is double r ? r : 0.0;
                    indicator.Width = widthNow * cached;
                }
                finally
                {
                    // 成功应用一次：取消订阅 + 移除弱表追踪
                    host.LayoutUpdated -= handler;
                    _wpfPendingLayoutHandlers.Remove(host);
                }
            };
            host.LayoutUpdated += handler;
            if (_wpfPendingLayoutHandlers.TryGetValue(host, out _))
                _wpfPendingLayoutHandlers.Remove(host);
            _wpfPendingLayoutHandlers.Add(host, handler);
        }
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
    // 当前已渲染的"课间插入行"可视化元素（EXIT 动画时用）：左上/右上 Cell、下方进度条 host；会在 RefreshSchedule 插入行后赋值；非 Breaking 时为 null。
    private List<FrameworkElement>? _currentBreakRowVisualsWpf;

    private static void AnimateBreakRowEnterWpf(IEnumerable<FrameworkElement> elements)
    {
        var list = elements as List<FrameworkElement> ?? elements.ToList();
        if (list.Count == 0) return;
        var sb = new Storyboard { FillBehavior = FillBehavior.Stop, Duration = TimeSpan.FromMilliseconds(BreakRowAnimationMs) };
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        foreach (var el in list)
        {
            if (el == null) continue;
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

            var lessonsService = IAppHost.TryGetService<ILessonsService>();
            if (lessonsService == null)
            {
                RefreshSchedule();
                return;
            }

            var curStateWpf = lessonsService.CurrentState;
            bool onClass = curStateWpf == TimeState.OnClass;
            breaking = curStateWpf == TimeState.Breaking;
            bool indexValid = _currentProgressClassIndex >= 0 &&
                              _currentProgressClassIndex < _currentRowLayoutItems.Count;

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

            // ======== 【新增】课间行 EXIT 动画：Breaking→非 Breaking 先移除动画再 RefreshSchedule ========
            //  根因：needRefresh=true 会立即 RefreshSchedule 把课间插入行 UI 直接清空（控件瞬间消失，无动画）。
            //  策略：识别 Breaking→非Breaking 过渡 → 阻断本帧 needRefresh → 先对现有课间行 3 个视觉元素播放 250ms
            //        EXIT 动画，Completed/兜底 Task.Delay 到点再回调 RefreshSchedule 真正移除 + 写快照。
            bool willExitBreakRow = false;
            if (_wasBreakLastTickWpf && !breaking && !_breakRowExitAnimatingWpf &&
                _currentBreakRowVisualsWpf != null && _currentBreakRowVisualsWpf.Count > 0)
            {
                willExitBreakRow = true;
                _breakRowExitAnimatingWpf = true;
                _breakRowExitPendingStateCode = stateCode;
                _breakRowExitPendingBreakStart = breakStartTicks;
                _breakRowExitPendingBreakEnd = breakEndTicks;
                // 阻断本次 stateOrBreakChanged：延后到 EXIT 动画完成再移除插入行
                stateOrBreakChanged = false;

                var visualsToExit = _currentBreakRowVisualsWpf;
                AnimateBreakRowExitWpf(visualsToExit, () =>
                {
                    // 动画结束：① 把 pending 快照写入全局（防止下一帧 UpdateProgress 再触发 needRefresh）
                    _lastWpfRefreshStateCode = _breakRowExitPendingStateCode;
                    _lastWpfRefreshBreakStartTicks = _breakRowExitPendingBreakStart;
                    _lastWpfRefreshBreakEndTicks = _breakRowExitPendingBreakEnd;
                    // ② 真正 RefreshSchedule：此时 breaking=false，不会再构建课间插入行
                    RefreshSchedule();
                    // ③ 复位标志 + 清除已离场控件引用
                    _breakRowExitAnimatingWpf = false;
                    _wasBreakLastTickWpf = false;
                    _currentBreakRowVisualsWpf = null;
                    // ④ 防止极端帧间竞态：同步清零课间进度 indicator/host（RefreshSchedule 已重建，这里双保险）
                    _currentBreakProgressIndicator = null;
                    _currentBreakProgressHost = null;
                });
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

            if (!needRefresh && onClass && indexValid)
            {
                // 当前上课段与高亮行不一致时也需要整表重建
                var expected = _currentRowLayoutItems[_currentProgressClassIndex];
                var curLi = ReflectGetCurrentTimeLayoutItemService(lessonsService);
                if (curLi != null &&
                    (ReflectGetStartTime(curLi) != ReflectGetStartTime(expected) ||
                     ReflectGetEndTime(curLi) != ReflectGetEndTime(expected)))
                {
                    needRefresh = true;
                }
            }

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
                // indexValid 以 RefreshSchedule 重建后的 _currentProgressClassIndex / _currentRowLayoutItems 重算
                indexValid = _currentProgressClassIndex >= 0 &&
                             _currentProgressClassIndex < _currentRowLayoutItems.Count;
                // onClass/breaking 本身不需要重算（它们来自 curStateWpf，RefreshSchedule 内已按该快照构建高亮/插入行）
            }

            // ========== 课间进度条（Breaking 命中时：按 SDK 当前课间项目 start/end 计算百分比）==========
            if (breaking && curBreakLi != null && _currentBreakProgressIndicator != null)
            {
                var breakStart = ReflectGetStartTime(curBreakLi);
                var breakEnd = ReflectGetEndTime(curBreakLi);
                var breakTotal = (breakEnd - breakStart).TotalSeconds;
                if (breakTotal > 0)
                {
                    var now = Plugin.GetCurrentTime().TimeOfDay;
                    var bElapsed = (now - breakStart).TotalSeconds;
                    double ratio = Math.Clamp(bElapsed / breakTotal, 0.0, 1.0);
                    ApplyProgressRatio(_currentBreakProgressHost!, _currentBreakProgressIndicator, ratio);
                }
                else
                {
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

            // 计算上课进度条
            var layoutItem = ReflectGetCurrentTimeLayoutItemService(lessonsService);
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

            var nowTimeOfDay = Plugin.GetCurrentTime().TimeOfDay;
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
