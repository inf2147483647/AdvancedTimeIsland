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
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    // 用户文档 要点6：WS_EX_COMPOSITED 启用 DWM 双缓冲合成；WS_EX_LAYERED 分层透明窗提示（长期有益，仍保留）
    private const int WS_EX_COMPOSITED_AV = 0x02000000;
    private const int WS_EX_LAYERED_AV    = 0x00080000;
    private const int WS_EX_TRANSPARENT_AV = 0x00000020;   // 点击穿透：Win32 消息投递前系统跳过命中测试，直接透到下一层窗口

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
    private List<Control>? _currentBreakRowVisualsAv;

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
        // （重置方案A → 系统原生拖拽在窗口销毁时由系统自动释放，不需要手动清理；此处只做收尾）
        try { _window?.Close(); } catch { }
        _window = null;
        _containerBorder = null;
        _currentProgressIndicator = null;
        _currentProgressHost = null;
        _currentBreakProgressIndicator = null;
        _currentBreakProgressHost = null;
        _currentBreakLayoutItem = null;
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
            if (_window?.IsVisible == true)
            {
                _settings.FloatingSchedulePositionX = e.Point.X;
                _settings.FloatingSchedulePositionY = e.Point.Y;
            }
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

        // 拖拽（重置为方案A → 系统原生 HTCAPTION 拖拽：PointerPressed 时注册释放通知 + 触发系统拖拽开始）
        //  Windows：经典 ReleaseCapture + SendMessage(WM_NCLBUTTONDOWN, HTCAPTION)
        //  非 Windows：Avalonia Window.DragMove()（跨平台等价实现）
        _containerBorder.PointerPressed += ContainerBorder_PointerPressed;
        _containerBorder.PointerReleased += ContainerBorder_PointerReleased;

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
        var layer = _settings.FloatingScheduleWindowLayer;
#if WINDOWS
        try
        {
            var hwnd = _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (hwnd != IntPtr.Zero)
            {
                var insert = layer == FloatingScheduleWindowLayer.Topmost ? HWND_TOPMOST : HWND_BOTTOM;
                SetWindowPos(hwnd, insert, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                return;
            }
        }
        catch { }
#endif
        // 非 Windows 或 hwnd 获取失败：只支持 Topmost 属性
        try { _window.Topmost = layer == FloatingScheduleWindowLayer.Topmost; } catch { }
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

    // ===================== 拖拽（重置：用户文档 二.5 方案A — 交给系统原生 HTCAPTION 拖拽，代码极简）=====================
    // 为什么彻底放弃之前的手动 SetCapture+GetCursorPos+SetWindowPos（方案B）？
    //   1) 多轮迭代后代码量从 30 行 膨胀到 600+ 行（子类化/GC handle/DllImport 12 项/节流/缓存/去重/捕获有效性校验……）—— 用户明确指令"越来越复杂需要重置"；
    //   2) 方案A（系统原生 HTCAPTION）已在 Win32 内优化了 30 年：Per-Monitor V2 DPI 自动、跨屏自动、Snap 兼容自动、DWM 合成时序自动、32/64位自动、客户区边界自动；
    //   3) 文档二.5 硬性规则：两套拖拽只能二选一；本方案A是系统独占 HTCAPTION 链路，完全避免"两套并行（头号元凶）、坐标单位冲突、双重缩放回环"等人为 bug。
    //
    // 保留的有益优化（仍然执行）：Down 前切 SizeToContent.Manual（防 Auto 布局重排合成抖动）；结束后恢复 SizeToContent + ApplyWindowLayer 补 z-order + 保存当前 Position。

    private void ContainerBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_window == null) return;
        // 【新增】点击穿透模式下，完全禁止任何本地交互（也包含拖拽），保证用户点击一定会穿透到下方窗口。
        if (_settings.FloatingScheduleClickThrough) return;
        var props = e.GetCurrentPoint(_containerBorder).Properties;
        // 仅响应鼠标左键或触摸
        if (!props.IsLeftButtonPressed && e.Pointer.Type != PointerType.Touch) return;

        // 1) 先冻结 SizeToContent.Auto：否则拖动过程中若 Auto 测量触发，会让透明悬浮窗每移动一次重测量 → 桌面合成器抖动
        _preDragAvSizeMode = _window.SizeToContent;
        if (_preDragAvSizeMode != SizeToContent.Manual) _window.SizeToContent = SizeToContent.Manual;

        try
        {
#if WINDOWS
            // Windows：经典 ReleaseCapture + SendMessage(WM_NCLBUTTONDOWN, HTCAPTION)
            //   效果完全等价于 WPF Window.DragMove()：同步阻塞，系统内部进入模态消息循环处理 HTCAPTION 拖拽，
            //   直到用户松开鼠标左键 SendMessage 才返回；所有坐标/DPI/跨屏/合成由系统处理。
            if (OperatingSystem.IsWindows())
            {
                // Avalonia 11 (含 net8.0/net10.0) Window.TryGetPlatformHandle 签名：无参，返回 IPlatformHandle?
                //  注：老版本是 TryGetPlatformHandle(out handle) 返回 bool；这里用 TryGetPlatformHandle() ?.Handle 同时兼容两种写法
                //   （Avalonia 11.x 中 Window.TryGetPlatformHandle() 返回 IPlatformHandle? ，可直接读 Handle）
                var platHandle = _window.TryGetPlatformHandle();
                var hwnd = platHandle?.Handle ?? IntPtr.Zero;
                if (hwnd != IntPtr.Zero)
                {
                    ReleaseCapture();
                    SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
                }
                else
                {
                    // 理论不会发生；兜底走 Avalonia 跨平台 BeginMoveDrag(e)
                    _window.BeginMoveDrag(e);
                }
            }
            else
#endif
            {
                // 非 Windows（Linux/macOS）：走 Avalonia 跨平台 Window.BeginMoveDrag(PointerPressedEventArgs)
                //   （等价于 WPF DragMove()；Avalonia 11+ 官方跨平台系统原生拖拽 API）
                _window.BeginMoveDrag(e);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "启动系统原生拖拽 DragMove/SendMessage(HTCAPTION) 提前退出（按下后立即失焦/释放等）安全忽略。");
        }
        finally
        {
            // 兜底恢复（若用户按下后立即失焦，原生拖拽可能不进入 Released 路径）
            try
            {
                if (_window != null && _window.SizeToContent != _preDragAvSizeMode)
                    _window.SizeToContent = _preDragAvSizeMode;
            }
            catch { /* ignore */ }
        }

        try { e.Handled = true; } catch { }
    }

    private void ContainerBorder_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_window == null) return;

        try
        {
            // 恢复 SizeToContent（拖动期间冻结 Manual 防止测量抖动）
            if (_window.SizeToContent != _preDragAvSizeMode) _window.SizeToContent = _preDragAvSizeMode;
            // 结束后再应用一次 z-order：拖动过程中系统原生 HTCAPTION 不改变 z-order，但若用户设置 Bottommost/Topmost 层变化，ApplyWindowLayer 强制补齐
            try { ApplyWindowLayer(); } catch (Exception ex) { _logger.LogDebug(ex, "拖拽结束 ApplyWindowLayer 出错，忽略。"); }
            // 保存当前悬浮窗位置（下次启动初始化 Left/Position 用）
            _settings.FloatingSchedulePositionX = _window.Position.X;
            _settings.FloatingSchedulePositionY = _window.Position.Y;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "拖拽结束恢复 SizeToContent/SavePosition 出错，忽略。");
        }

        try { e.Handled = true; } catch { }
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
                    });
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        HideWindow();
                        StartOrStopTimer();
                    });
                }
                break;
            case nameof(PluginSettings.FloatingScheduleWindowLayer):
                Dispatcher.UIThread.Post(ApplyWindowLayer);
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
                _hoverFadeTimer.Tick += (_, _) => ApplyHoverFade();
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
        var bg = new Border
        {
            Background = bgBrush,
            CornerRadius = new CornerRadius(1.5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var indicator = new Border
        {
            Background = accentBrush,
            CornerRadius = new CornerRadius(1.5),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            Width = 0
        };
        host.Children.Add(bg);
        host.Children.Add(indicator);
        return (host, indicator);
    }

    // AVA 端 LayoutUpdated 待触发 handler 弱引用追踪（Avalonia Layoutable 没有 Tag 属性，用 ConditionalWeakTable）
    private static readonly ConditionalWeakTable<Layoutable, object> _avPendingLayoutHandlers = new();

    private static void ApplyProgressRatioAv(Layoutable? host, Border? indicator, double ratio)
    {
        if (host == null || indicator == null) return;
        ratio = double.IsNaN(ratio) ? 0.0 : Math.Clamp(ratio, 0.0, 1.0);
        var aw = host.Bounds.Width;
        if (aw > 0)
        {
            indicator.Width = aw * ratio;
            // 宿主宽度就绪：清理之前尚未触发的 LayoutUpdated 订阅
            if (_avPendingLayoutHandlers.TryGetValue(host, out var o) && o is EventHandler pendingHandler)
            {
                host.LayoutUpdated -= pendingHandler;
                _avPendingLayoutHandlers.Remove(host);
            }
        }
        else
        {
            // 每轮 Apply 刷新 indicator.Tag ratio 缓存：保证"后续 LayoutUpdated 回调"读的是最新值（不是 RefreshSchedule 刚构造完时 stale 默认 0）
            indicator.Tag = ratio;

            // 已经订阅过（通过 weak-table 追踪）：不重复订阅事件，直接 return
            if (_avPendingLayoutHandlers.TryGetValue(host, out _))
                return;

            EventHandler? handler = null;
            handler = (_, _) =>
            {
                if (host == null || indicator == null)
                {
                    if (handler != null) host.LayoutUpdated -= handler;
                    _avPendingLayoutHandlers.Remove(host);
                    return;
                }
                double widthNow = host.Bounds.Width;
                if (widthNow <= 0) return;   // 宿主 Bounds 仍未就绪：保持订阅（关键修复：不再在未成功应用前 -=）
                try
                {
                    var cached = indicator.Tag is double r ? r : 0.0;
                    indicator.Width = widthNow * cached;
                }
                finally
                {
                    host.LayoutUpdated -= handler;
                    _avPendingLayoutHandlers.Remove(host);
                }
            };
            host.LayoutUpdated += handler;
            if (_avPendingLayoutHandlers.TryGetValue(host, out _))
                _avPendingLayoutHandlers.Remove(host);
            _avPendingLayoutHandlers.Add(host, handler);
        }
    }

    // ===================== 课表 UI 构建 =====================
    private void RefreshSchedule()
    {
        if (_window == null || _containerBorder == null) return;

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

            // ---- 获取今日课表（优先：ILessonsService.CurrentClassPlan / GetClassPlanByDate(DateTime.Today)）----
            object? classPlan = ReflectProp(_lessonsService, "CurrentClassPlan");
            if (classPlan == null)
            {
                classPlan = InvokeGeneric(_lessonsService, "GetCurrentClassPlan");
                if (classPlan == null)
                {
                    classPlan = InvokeGeneric(_lessonsService, "GetClassPlanByDate", DateTime.Today);
                    if (classPlan == null)
                    {
                        classPlan = InvokeGeneric(_lessonsService, "GetClassPlan", DateTime.Today);
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
                foreach (var x in validRaw)
                    if (x != null && ReflectGetTimeType(x) == 0) validItems.Add(x);
                validItems.Sort((a, b) => ReflectGetStartTime(a).CompareTo(ReflectGetStartTime(b)));

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
            var now = Plugin.GetCurrentTime().TimeOfDay;
            var curStateRaw = ReflectProp(_lessonsService, "CurrentState");
            TimeState curState = curStateRaw != null ? (TimeState)curStateRaw : TimeState.None;
            bool isOnClass = curState == TimeState.OnClass;
            bool isBreaking = curState == TimeState.Breaking;

            if (isOnClass)
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

            // ---------- 课间休息插入判定（严格按需求：课间休息中才插入；未开始/已结束不处理）----------
            //   判定规则：
            //    1) 必须处于 Breaking 状态；
            //    2) SDK.CurrentTimeLayoutItem.TimeType == 1（是一个课间项目）；
            //    3) 该课间的 StartTime >= 第一节课的 StartTime（未开始时不显示，避免课前假课间）；
            //    4) 该课间的 EndTime   <= 最后一节课的 EndTime（已结束后不显示，避免放学后假课间）；
            //    5) 找到「相邻课程对 classRows[i] 与 classRows[i+1]」：前者 End == 课间 Start；后者 Start == 课间 End；
            //       满足时 => 展示位置 = 在 classRows[i] 之后插入一行"课间休息"。
            int breakInsertAfterClassIdx = -1;
            object? breakLayoutItem = null;
            TimeSpan breakStart = default;
            TimeSpan breakEnd = default;
            string breakNameText = "课间休息";
            if (isBreaking && _currentClassRows.Count >= 2)
            {
                var bLi = ReflectProp(_lessonsService, "CurrentTimeLayoutItem");
                if (bLi != null && ReflectGetTimeType(bLi) == 1)
                {
                    var bs = ReflectGetStartTime(bLi);
                    var be = ReflectGetEndTime(bLi);
                    var firstStart = ReflectGetStartTime(_currentClassRows[0].LayoutItem);
                    var lastEnd = ReflectGetEndTime(_currentClassRows[_currentClassRows.Count - 1].LayoutItem);
                    if (bs >= firstStart && be <= lastEnd)
                    {
                        for (int i = 0; i < _currentClassRows.Count - 1; i++)
                        {
                            var prevEnd = ReflectGetEndTime(_currentClassRows[i].LayoutItem);
                            var nextStart = ReflectGetStartTime(_currentClassRows[i + 1].LayoutItem);
                            if (prevEnd == bs && nextStart == be)
                            {
                                breakInsertAfterClassIdx = i;
                                breakLayoutItem = bLi;
                                breakStart = bs;
                                breakEnd = be;
                                breakNameText = ReflectGetBreakNameText(bLi);
                                break;
                            }
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
                        var surname = subject!.GetFirstName();
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
                    var teacherTb = new TextBlock
                    {
                        Text = teacherLine,
                        FontSize = Math.Max(8, fontSize - 3),
                        Foreground = subFg,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
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
            }

            _containerBorder.Child = grid;
            _rootContent = grid;
            // ---- 【新增课间动画】登记新建课间行视觉单元；如需 ENTER 动画 fire-and-forget 启动 ----
            //   只在"上一帧不是 Breaking && 当前不是 EXIT 动画进行中 && 本次构建确实产出了课间行 3 元素"时播放 ENTER，
            //   避免"SDK 内部课间切课间（同 break）RefreshSchedule 重建"、"EXIT 动画结束 RefreshSchedule 又回到 Breaking"
            //   等场景下不必要的重放动画。
            _currentBreakRowVisualsAv = builtBreakVisualsAv;
            if (builtBreakVisualsAv != null && !_wasBreakLastTickAv && !_breakRowExitAnimatingAv)
            {
                // 【修复 #3】去掉冗余 enterCts（从未 Cancel/CancelAfter），直接使用 EXIT CTS Token 作为 ENTER 的唯一外部取消源：
                //   - 如果 ENTER 动画 250ms 窗口内 SDK 触发 EXIT：Token 会被 Cancel → ENTER 立即落地最终 Opacity=1/Y=0，视觉衔接平滑；
                //   - 没有 EXIT 则传 CancellationToken.None，等同于 ENTER 动画不会被外部中断。
                //   省去原 linkedCTS 的额外分配与 40ms 超时双重叠加（AnimateBreakRowEnterAv 内部已有 290ms 渲染兜底）。
                async void FireEnter()
                {
                    try
                    {
                        var ct = _breakRowExitCtsAv?.Token ?? CancellationToken.None;
                        await AnimateBreakRowEnterAv(builtBreakVisualsAv, ct);
                    }
                    catch { /* 动画失败兜底：不抛到未捕获域 */ }
                }
                Dispatcher.UIThread.Post(FireEnter, DispatcherPriority.Loaded);
            }
            // ---- 构建完成后记录"本次刷新时的状态快照" ----
            //  下一帧 UpdateProgress 以这里作为基线；这样 Breaking→OnClass 或 OnClass→Breaking 等状态切换能立刻触发重建。
            _lastRefreshStateCode = (int)curState;
            _lastRefreshBreakStartTicks = breakInsertAfterClassIdx >= 0 && breakLayoutItem != null ? breakStart.Ticks : -1;
            _lastRefreshBreakEndTicks   = breakInsertAfterClassIdx >= 0 && breakLayoutItem != null ? breakEnd.Ticks   : -1;
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

            var curStateRaw = ReflectProp(_lessonsService, "CurrentState");
            TimeState curState = curStateRaw != null ? (TimeState)curStateRaw : TimeState.None;
            bool onClass = curState == TimeState.OnClass;
            breaking = curState == TimeState.Breaking;
            var curLi = onClass ? ReflectProp(_lessonsService, "CurrentTimeLayoutItem") : null;

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

            // ======== 【新增 Ava】课间行 EXIT 动画：Breaking→非 Breaking 先移除动画再 RefreshSchedule ========
            if (_wasBreakLastTickAv && !breaking && !_breakRowExitAnimatingAv &&
                _currentBreakRowVisualsAv != null && _currentBreakRowVisualsAv.Count > 0)
            {
                _breakRowExitAnimatingAv = true;
                _breakRowExitPendingStateCode = stateCode;
                _breakRowExitPendingBreakStart = breakStartTicks;
                _breakRowExitPendingBreakEnd = breakEndTicks;
                // 阻断本次 stateOrBreakChanged 触发 needRefresh，延后到 EXIT 动画结束回调统一重建
                stateOrBreakChanged = false;

                // 取消旧 EXIT CTS（安全地释放上一轮可能还残留的 token 引用）
                try { _breakRowExitCtsAv?.Cancel(); } catch { /* ignore */ }
                var newCts = new CancellationTokenSource();
                _breakRowExitCtsAv = newCts;

                var visualsToExit = _currentBreakRowVisualsAv;
                // async void fire-and-forget：动画 250ms，结束后 Post RefreshSchedule。
                //   Finish 回调 100% 触发（AnimateBreakRowExitAv 用 WhenAny+Task.Delay 双保险），因此不会泄漏。
                async void FireExit()
                {
                    try
                    {
                        await AnimateBreakRowExitAv(visualsToExit, newCts.Token, () =>
                        {
                            // 必须在 UI 线程写快照 + RefreshSchedule + 复位标志
                            Dispatcher.UIThread.Post(() =>
                            {
                                _lastRefreshStateCode = _breakRowExitPendingStateCode;
                                _lastRefreshBreakStartTicks = _breakRowExitPendingBreakStart;
                                _lastRefreshBreakEndTicks = _breakRowExitPendingBreakEnd;
                                // 【修复 #2】复位标志顺序放在 RefreshSchedule 之前：
                                //   若 SDK 状态在 EXIT 250ms 窗口内极端抖动回 Breaking，RefreshSchedule 内部 ENTER 判断能正确走到播放动画，
                                //   而不会因 _breakRowExitAnimatingAv==true / _wasBreakLastTickAv==true 被挡住。
                                _breakRowExitAnimatingAv = false;
                                _wasBreakLastTickAv = false;
                                // 清除已离场旧 visuals 引用：防止 RefreshSchedule 构建的新课间行（若存在）被此字段残留值"误判仍有旧行"
                                _currentBreakRowVisualsAv = null;
                                _currentBreakProgressIndicator = null;
                                _currentBreakProgressHost = null;
                                RefreshSchedule();
                                // Ava Post 中 RefreshSchedule 本身同步完成，紧接跑一次 ratio（对齐 needRefresh 分支的行为）
                                try
                                {
                                    var svc = _lessonsService;
                                    if (svc != null)
                                    {
                                        var sRaw = ReflectProp(svc, "CurrentState");
                                        TimeState s = sRaw != null ? (TimeState)sRaw : TimeState.None;
                                        bool onC = s == TimeState.OnClass;
                                        bool brk = s == TimeState.Breaking;
                                        if (brk)
                                        {
                                            var bx = ReflectProp(svc, "CurrentTimeLayoutItem");
                                            if (bx != null && ReflectGetTimeType(bx) == 1 && _currentBreakProgressIndicator != null)
                                            {
                                                var bs = ReflectGetStartTime(bx);
                                                var be = ReflectGetEndTime(bx);
                                                double tb = (be - bs).TotalSeconds;
                                                if (tb > 0)
                                                {
                                                    var nn = Plugin.GetCurrentTime().TimeOfDay;
                                                    double rb = Math.Clamp((nn - bs).TotalSeconds / tb, 0.0, 1.0);
                                                    ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, rb);
                                                }
                                                else ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                                            }
                                            else if (_currentBreakProgressIndicator != null)
                                                ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                                        }
                                        else if (_currentBreakProgressIndicator != null)
                                            ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                                        if (onC)
                                        {
                                            var lx = ReflectProp(svc, "CurrentTimeLayoutItem");
                                            if (lx != null && _currentProgressIndicator != null)
                                            {
                                                var st = ReflectGetStartTime(lx);
                                                var ed = ReflectGetEndTime(lx);
                                                double t = (ed - st).TotalSeconds;
                                                if (t <= 0) { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); }
                                                else
                                                {
                                                    var nn = Plugin.GetCurrentTime().TimeOfDay;
                                                    double p = Math.Clamp((nn - st).TotalSeconds / t, 0.0, 1.0);
                                                    ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, p);
                                                }
                                            }
                                            else ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                                        }
                                        else ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                                    }
                                }
                                catch { /* 忽略 */ }
                                // 复位 EXIT CTS 状态
                                if (ReferenceEquals(_breakRowExitCtsAv, newCts))
                                {
                                    try { newCts.Dispose(); } catch { /* ignore */ }
                                    _breakRowExitCtsAv = null;
                                }
                            }, DispatcherPriority.Loaded);
                        });
                    }
                    catch
                    {
                        // 极端异常：强制复位 EXIT 标志，避免永久卡死"动画中"状态
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (ReferenceEquals(_breakRowExitCtsAv, newCts))
                            {
                                try { newCts.Dispose(); } catch { /* ignore */ }
                                _breakRowExitCtsAv = null;
                            }
                            // 【修复 #2 兜底分支】与 onCompleted 同顺序：先写快照 + 复位标志 + 清旧引用，再 RefreshSchedule
                            _breakRowExitAnimatingAv = false;
                            _wasBreakLastTickAv = false;
                            _currentBreakRowVisualsAv = null;
                            _currentBreakProgressIndicator = null;
                            _currentBreakProgressHost = null;
                            _lastRefreshStateCode = _breakRowExitPendingStateCode;
                            _lastRefreshBreakStartTicks = _breakRowExitPendingBreakStart;
                            _lastRefreshBreakEndTicks = _breakRowExitPendingBreakEnd;
                            // 紧急兜底：直接 RefreshSchedule 让 UI 与当前状态对齐（哪怕跳过动画）
                            try { RefreshSchedule(); }
                            catch { /* ignore */ }
                        }, DispatcherPriority.Loaded);
                    }
                }
                Dispatcher.UIThread.Post(FireExit, DispatcherPriority.Loaded);
            }

            // ---- 时间表状态变化 → 触发 RefreshSchedule 重建高亮行与进度条位置 ----
            bool needRefresh = stateOrBreakChanged;
            bool indexValid = _currentOnClassIndex >= 0 && _currentOnClassIndex < _currentClassRows.Count;
            if (!needRefresh && onClass != indexValid)
            {
                // 上课状态与索引存在性不一致（例如：进入上课但仍没高亮索引，或下课了还有进度条）
                needRefresh = true;
            }
            else if (!needRefresh && onClass && indexValid && curLi != null)
            {
                // 已上课且当前索引有效：校验当前 LayoutItem 的 Start/End 是否与高亮行匹配
                var expectedStart = ReflectGetStartTime(_currentClassRows[_currentOnClassIndex].LayoutItem);
                var expectedEnd = ReflectGetEndTime(_currentClassRows[_currentOnClassIndex].LayoutItem);
                if (ReflectGetStartTime(curLi) != expectedStart || ReflectGetEndTime(curLi) != expectedEnd)
                    needRefresh = true;
            }
            // EXIT 动画窗口期间：任何 needRefresh（break indicator 非空触发的、LayoutItem 不匹配触发的等）全部延后到动画结束回调，
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

                        // -- 课间进度条 --
                        if (breaking2)
                        {
                            var b2 = ReflectProp(svc, "CurrentTimeLayoutItem");
                            if (b2 != null && ReflectGetTimeType(b2) == 1 && _currentBreakProgressIndicator != null)
                            {
                                var bs = ReflectGetStartTime(b2);
                                var be = ReflectGetEndTime(b2);
                                double totalB = (be - bs).TotalSeconds;
                                if (totalB > 0)
                                {
                                    var n = Plugin.GetCurrentTime().TimeOfDay;
                                    double ratioB = Math.Clamp((n - bs).TotalSeconds / totalB, 0.0, 1.0);
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

                        // -- 当前课进度条 --
                        if (onClass2)
                        {
                            var l2 = ReflectProp(svc, "CurrentTimeLayoutItem");
                            if (l2 != null && _currentProgressIndicator != null)
                            {
                                var start = ReflectGetStartTime(l2);
                                var end = ReflectGetEndTime(l2);
                                double total = (end - start).TotalSeconds;
                                if (total <= 0) { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); return; }
                                var n = Plugin.GetCurrentTime().TimeOfDay;
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

            // ========== 课间进度条（Breaking 命中时：按 SDK 当前课间项目 start/end 计算百分比）==========
            if (breaking && bLi != null && _currentBreakProgressIndicator != null)
            {
                var breakStart = ReflectGetStartTime(bLi);
                var breakEnd = ReflectGetEndTime(bLi);
                var breakTotal = (breakEnd - breakStart).TotalSeconds;
                if (breakTotal > 0)
                {
                    var now = Plugin.GetCurrentTime().TimeOfDay;
                    var bElapsed = (now - breakStart).TotalSeconds;
                    double ratio = Math.Clamp(bElapsed / breakTotal, 0.0, 1.0);
                    ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, ratio);
                }
                else
                {
                    ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
                }
            }
            else if (_currentBreakProgressIndicator != null)
            {
                ApplyProgressRatioAv(_currentBreakProgressHost, _currentBreakProgressIndicator, 0.0);
            }

            // ========== 当前课进度条（OnClass 命中时） ==========
            if (_currentProgressIndicator == null || !onClass || curLi == null)
            {
                ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0);
                return;
            }

            var start = ReflectGetStartTime(curLi);
            var end = ReflectGetEndTime(curLi);
            var total = (end - start).TotalSeconds;
            if (total <= 0) { ApplyProgressRatioAv(_currentProgressHost, _currentProgressIndicator, 0.0); return; }

            var now2 = Plugin.GetCurrentTime().TimeOfDay;
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
        EnsureBreakRowTransformAv(elements);

        // 统一先落地"动画起点"本地值，避免第 0 帧闪现控件默认 Opacity=1/Y=0
        foreach (var el in elements)
        {
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
