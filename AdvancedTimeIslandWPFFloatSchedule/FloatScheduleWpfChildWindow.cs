// 子进程悬浮窗（WPF 版）：与 Avalonia 版子进程 FloatScheduleChildWindow 同构 —— 窗口属性/交互语义
// （统一指针拖拽、贴边隐藏状态机、悬停淡化、层级竞争防护、点击穿透三件套、防截图、随机标题、本地推进）
// 全部照搬，数据源换成插件管道推送的渲染模型（FloatScheduleWpfRenderer 渲染，与进程内实现同规格）。
// 与 Avalonia 版的差异（框架层，非语义）：
//  - 位置以 DIP 表达（WPF 的 Window.Left/Top 即 DIP，与 WPF 进程内实现 FloatingScheduleService 的存档口径一致）；
//  - 指针命中/贴边条判定走 Win32 物理像素（GetCursorPos / PointToScreen），避免与 DIP 混算；
//  - 拖拽为自实现（鼠标 + 触摸），不用 Window.DragMove 的模态消息循环，便于被贴边/看门狗逻辑打断；
//  - 无 Avalonia 代际校验（WPF 子进程是框架依赖应用）。
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AdvancedTimeIsland.Shared.FloatingSchedule;

namespace AdvancedTimeIsland.FloatScheduleWpfChild;

internal sealed class FloatScheduleWpfChildWindow : Window
{
    private const string DefaultTitle = "AdvancedTimeIsland FloatingSchedule";

    private readonly FloatSchedulePipeClient _pipe = new();
    private CancellationTokenSource _cts = new();

    private readonly Border _card;
    private FloatScheduleWpfProgressRefs? _refs;
    private FloatScheduleRenderModel? _lastModel;
    private FloatScheduleWindowSettings _snap = new();
    private bool _initialPositionApplied;
    private bool _hostVisible = true;          // 插件 HideMode 判定结果（false=被要求隐藏）
    private bool _firstDataReceived;
    private int _exitRequested;                // 托盘"退出"幂等标记
    private bool _allowClose;

    public FloatScheduleWpfChildWindow()
    {
        Title = DefaultTitle;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        Opacity = 1.0;
        MinWidth = 150;
        MaxWidth = 820;
        // 窗口级字体兜底（模型带 FontFamilySource 时渲染器在内容根上覆盖）
        FontFamily = new FontFamily("HarmonyOS Sans SC, Microsoft YaHei UI");

        _card = new Border
        {
            CornerRadius = new CornerRadius(FloatScheduleWpfRenderer.CardCornerRadius),
            Padding = FloatScheduleWpfRenderer.CardPadding,
            BorderThickness = FloatScheduleWpfRenderer.CardBorderThickness,
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x2D, 0x2D, 0x30)),
        };
        Content = _card;

        _card.MouseLeftButtonDown += Card_MouseLeftButtonDown;
        _card.MouseMove += Card_MouseMove;
        _card.MouseLeftButtonUp += Card_MouseLeftButtonUp;
        _card.LostMouseCapture += (_, _) => { if (_mouseDragActive) EndDrag(); };
        _card.TouchDown += Card_TouchDown;
        _card.TouchMove += Card_TouchMove;
        _card.TouchUp += Card_TouchUp;

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
        LocationChanged += OnLocationChanged;
        // 【修复：贴边隐藏后内容变矮/变窄 → 窗口整块跑出屏幕】
        //   悬浮窗是 SizeToContent：内容变化会改变窗口尺寸，而贴边隐藏位是按旧尺寸算的（left/top 含"减尺寸"项），
        //   尺寸变小后窗口连可见条一起被推出屏幕 → 用户看到"时间表突然完全不见"。
        //   双钩（LayoutUpdated + SizeChanged）：任一先到都会按最终尺寸重算一次（未贴边/尺寸未变时零开销早退）。
        LayoutUpdated += (_, _) => RefreshEdgeGeometryOnResize();
        SizeChanged += (_, _) => RefreshEdgeGeometryOnResize();
        Activated += (_, _) => { try { if (_snap.ClickThrough) ApplyClickThrough(force: true); else ApplyExStylesSafe(); } catch { } };

        _pipe.MessageReceived += OnPipeMessage;
        _pipe.ConnectedChanged += OnPipeConnectedChanged;
    }

    private IntPtr Hwnd
    {
        get
        {
            try { return new WindowInteropHelper(this).Handle; }
            catch { return IntPtr.Zero; }
        }
    }

    /// <summary>管道连接状态（供看门狗误报守卫读取）。</summary>
    public bool PipeConnected => _pipe.Connected;

    // ===================== 启动 =====================

    public void StartWithPipe()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => _pipe.RunAsync(_cts.Token));
        StartStartupGuard();
        StartLocalDrive();   // 抗宿主异常的本地推进
    }

    // 启动兜底：跟随启停=开 时，若 60s 内始终没能建立连接并收到数据（宿主侧异常），自动退出，
    //  避免留下永久重连的"孤儿"悬浮窗进程。
    private DispatcherTimer? _startupGuardTimer;
    private void StartStartupGuard()
    {
        if (_startupGuardTimer != null) return;
        _startupGuardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _startupGuardTimer.Tick += (_, _) =>
        {
            StopStartupGuard();
            if (Program.FollowHostLifetime && !_firstDataReceived) CloseAndExit();
        };
        _startupGuardTimer.Start();
    }
    private void StopStartupGuard()
    {
        try { _startupGuardTimer?.Stop(); } catch { }
        _startupGuardTimer = null;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = Hwnd;
        if (hwnd != IntPtr.Zero)
        {
            try { FloatScheduleNative.SetWindowDpiAwarenessContext(hwnd, FloatScheduleNative.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { }
            try { FloatScheduleNative.SetProcessDpiAwarenessContext(FloatScheduleNative.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { }
        }
        ApplyExStylesSafe();
        ApplyPreventCapture();
        ApplyWindowLayer();
        ApplyClickThrough(force: true);
        ApplyHoverFade(force: true);
        StartOrStopHoverTimer();
        ReattachTopmostRefresh();
        ScheduleEdgeEval();
    }

    // ===================== 管道消息 =====================

    private void OnPipeConnectedChanged()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_pipe.Connected)
            {
                StopReconnectGuard();
                // 已连接但插件迟迟不发 Init（异常场景）→ 30s 兜底退出，避免孤儿隐藏窗口
                if (!_firstDataReceived) StartInitTimeoutGuard();
            }
            else if (_firstDataReceived)
            {
                // 断线：本地推进器（500ms 常驻）会在推送停更 6s 后自动接管；跟随启停=开 时加"60s 未重连即退出"兜底。
                if (Program.FollowHostLifetime) StartReconnectGuard();
            }
        }));
    }

    private DispatcherTimer? _reconnectGuardTimer;
    private void StartReconnectGuard()
    {
        if (_reconnectGuardTimer != null) return;
        _reconnectGuardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _reconnectGuardTimer.Tick += (_, _) =>
        {
            StopReconnectGuard();
            if (!_pipe.Connected) CloseAndExit();
        };
        _reconnectGuardTimer.Start();
    }
    private void StopReconnectGuard()
    {
        try { _reconnectGuardTimer?.Stop(); } catch { }
        _reconnectGuardTimer = null;
    }

    private DispatcherTimer? _initGuardTimer;
    private void StartInitTimeoutGuard()
    {
        if (_initGuardTimer != null) return;
        _initGuardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _initGuardTimer.Tick += (_, _) =>
        {
            StopInitTimeoutGuard();
            if (!_firstDataReceived) CloseAndExit();
        };
        _initGuardTimer.Start();
    }
    private void StopInitTimeoutGuard()
    {
        try { _initGuardTimer?.Stop(); } catch { }
        _initGuardTimer = null;
    }

    private void OnPipeMessage(FloatScheduleIpcMsgType type, System.Text.Json.JsonElement data)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                switch (type)
                {
                    case FloatScheduleIpcMsgType.Init:
                    {
                        var init = FloatScheduleIpc.Deserialize<FloatScheduleInitPayload>(data);
                        // 协议版本校验：插件被更新后（跟随启停=关 时旧子进程可能仍在运行并被 adopt），
                        //   新旧协议可能不兼容 —— 版本不匹配则主动退出，让插件启动与自身匹配的新版本实例。
                        if (init?.Settings != null && init.Settings.ProtocolVersion != FloatScheduleIpc.ProtocolVersion)
                        {
                            CloseAndExit(FloatScheduleIpc.ExitCodeSelfUpdate);
                            break;
                        }
                        // 自身构建指纹校验（自检"我是否已被新版插件淘汰"）：不一致 ⇒ 运行中的是旧构建 → 主动退出，
                        //   让插件立刻用新构建重启一个新子进程；否则新功能/修复永远不会生效。
                        if (init?.Settings != null && !string.IsNullOrEmpty(init.Settings.ExeStamp))
                        {
                            var selfHash = Program.SelfExeHash;
                            if (!string.IsNullOrEmpty(selfHash) && selfHash != init.Settings.ExeStamp)
                            {
                                CloseAndExit(FloatScheduleIpc.ExitCodeSelfUpdate);
                                break;
                            }
                        }
                        if (init?.Settings != null) ApplySettings(init.Settings, isInit: true);
                        if (init?.Model != null) ApplyModel(init.Model);
                        if (!_firstDataReceived)
                        {
                            _firstDataReceived = true;
                            StopInitTimeoutGuard();
                            StopStartupGuard();
                            StopReconnectGuard();
                            if (_hostVisible) { try { Show(); } catch { } ApplyWindowLayer(); ArmPositionReportingAfterSettle(); }
                            // 托盘图标（原生 Win32 实现，独立于本窗口，无需等待窗口就绪）
                            try { FloatScheduleWpfApp.Self?.EnsureTrayIcon(); } catch { }
                        }
                        // Ready（PID 握手）必须**每次连接**都发：本进程可能早于 ClassIsland 启动
                        //  （跟随启停=关 时上一代子进程冻结存活），此时 _firstDataReceived 已是 true。
                        try { _pipe.Send(FloatScheduleIpcMsgType.Ready, new FloatScheduleReadyPayload { Pid = Environment.ProcessId }); } catch { }
                        break;
                    }
                    case FloatScheduleIpcMsgType.Settings:
                    {
                        var s = FloatScheduleIpc.Deserialize<FloatScheduleWindowSettings>(data);
                        if (s != null) ApplySettings(s, isInit: false);
                        break;
                    }
                    case FloatScheduleIpcMsgType.Model:
                    {
                        var m = FloatScheduleIpc.Deserialize<FloatScheduleRenderModel>(data);
                        if (m != null) ApplyModel(m);
                        break;
                    }
                    case FloatScheduleIpcMsgType.Progress:
                    {
                        var p = FloatScheduleIpc.Deserialize<FloatScheduleProgressPayload>(data);
                        if (p != null) ApplyProgress(p.ClassRatio, p.BreakRatio);
                        break;
                    }
                    case FloatScheduleIpcMsgType.Visible:
                    {
                        var v = FloatScheduleIpc.Deserialize<FloatScheduleVisiblePayload>(data);
                        if (v != null) SetHostVisible(v.Visible);
                        break;
                    }
                    case FloatScheduleIpcMsgType.Shutdown:
                        CloseAndExit();
                        break;
                }
            }
            catch { /* 单条消息异常不致命 */ }
        }));
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        try { Hide(); } catch { }
    }

    /// <summary>关闭窗口并退出本进程。
    /// exitCode 默认 0；自检发现"已被新版插件淘汰"时传 <see cref="FloatScheduleIpc.ExitCodeSelfUpdate"/>。</summary>
    private void CloseAndExit(int exitCode = 0)
    {
        _allowClose = true;
        try { _cts.Cancel(); } catch { }
        try { Close(); } catch { }
        Environment.Exit(exitCode);
    }

    /// <summary>宿主退出（跟随启停=开）：放行窗口关闭，供 <see cref="FloatScheduleWpfApp.RequestExitGracefully"/> 走正常 Shutdown。
    /// 若不先放行，本窗口的 Closing 守卫（防误关）会取消关闭 → 宿主退出路径只能在 3s 兜底里强杀进程。</summary>
    public void AllowCloseForExit() => _allowClose = true;

    /// <summary>用户从托盘图标选择"退出"：先告知插件（否则会被当作"子进程意外退出"而自动重启），
    /// 插件收到 ExitRequested 后关闭独立进程模式并回退到进程内渲染；随后本进程自行退出。</summary>
    public void RequestExitByUser()
    {
        if (Interlocked.Exchange(ref _exitRequested, 1) != 0) return;
        try { _pipe.Send(FloatScheduleIpcMsgType.ExitRequested, null); } catch { }
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        t.Tick += (_, _) =>
        {
            try { t.Stop(); } catch { }
            CloseAndExit();
        };
        t.Start();
    }

    // ===================== 数据应用 =====================

    private void ApplyModel(FloatScheduleRenderModel model)
    {
        _lastModel = model;
        _modelUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();   // 刷新"数据新鲜度"
        // 宿主推送到达 → 交回推送驱动：重置本地推进已应用状态，避免与推送结果互相覆盖
        _localClassIndex = int.MinValue;
        _localBreakStart = -1;
        RenderModel(model);
    }

    private void ApplyProgress(double classRatio, double breakRatio)
    {
        if (_refs == null) return;
        FloatScheduleWpfRenderer.ApplyProgressRatio(_refs.ClassProgressHost, _refs.ClassProgressIndicator, classRatio);
        FloatScheduleWpfRenderer.ApplyProgressRatio(_refs.BreakProgressHost, _refs.BreakProgressIndicator, breakRatio);
    }

    private void ApplySettings(FloatScheduleWindowSettings s, bool isInit)
    {
        var prev = _snap;
        _snap = s;
        Program.FollowHostLifetime = s.FollowHostLifetime;   // 热更新（看门狗触发时读最新值）

        // 【修复：重启时位置被重置】必须在**第一次收到任意设置快照**时应用位置（Init 的 Settings 可能为 null）。
        if (!_initialPositionApplied)
        {
            TryApplySavedPosition(s.PositionX, s.PositionY);
            _initialPositionApplied = true;
        }

        ApplyExStylesSafe();
        ApplyClickThrough(force: true);
        ApplyPreventCapture();
        ApplyWindowLayer();
        ReattachTopmostRefresh();

        if (prev.RandomTitle != s.RandomTitle || prev.RandomTitleEnhanced != s.RandomTitleEnhanced || isInit)
        {
            ApplyRandomTitle();
            StartOrStopRandomTitleTimer();
        }
        if (prev.HoverFade != s.HoverFade || prev.HoverFadeReverse != s.HoverFadeReverse)
            ApplyHoverFade(force: true);
        StartOrStopHoverTimer();
        if (!s.EdgeHide) DisableEdgeHide();
        else if (Math.Abs(prev.EdgeHideDelay - s.EdgeHideDelay) > 0.001) ScheduleEdgeEval();
    }

    /// <summary>应用存档位置（DIP）。窗口尚未创建句柄时 Left/Top 亦可写，WPF 会在首次显示时生效。</summary>
    private void TryApplySavedPosition(int x, int y)
    {
        try
        {
            Left = x;
            Top = y;
        }
        catch { }
    }

    private void SetHostVisible(bool visible)
    {
        if (_hostVisible == visible) return;
        _hostVisible = visible;
        if (!_firstDataReceived) return;
        try
        {
            if (visible) { Show(); ApplyWindowLayer(); ArmPositionReportingAfterSettle(); }
            else { try { if (_edgeHidden) { Left = _edgeDockedLeft; Top = _edgeDockedTop; } } catch { } Hide(); }
        }
        catch { }
    }

    // ===================== 层级（置顶/置底 + 竞争防护） =====================

    private bool _suppressTopmostRefresh;
    private long _lastOpportunisticPushTick;
    private bool _bottomContentionYielded;
    private readonly Queue<long> _opportunisticPushTicks = new();
    private const int OpportunisticBottomMinIntervalMs = 1500;
    private const int BottomContentionPushThreshold = 4;
    private static readonly TimeSpan BottomContentionWindow = TimeSpan.FromSeconds(10);

    private void ApplyWindowLayer(bool opportunistic = false)
    {
        if (_suppressTopmostRefresh) return;
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        try
        {
            ApplyExStylesSafe();
            if (_snap.Layer == 1)   // Topmost
            {
                bool needTop = true;
                try { needTop = FloatScheduleNative.GetWindow(hwnd, FloatScheduleNative.GW_HWNDFIRST) != hwnd; } catch { }
                if (needTop)
                    FloatScheduleNative.SetWindowPos(hwnd, FloatScheduleNative.HWND_TOPMOST, 0, 0, 0, 0,
                        FloatScheduleNative.SWP_NOMOVE | FloatScheduleNative.SWP_NOSIZE | FloatScheduleNative.SWP_NOACTIVATE |
                        FloatScheduleNative.SWP_NOSENDCHANGING | FloatScheduleNative.SWP_NOOWNERZORDER);
            }
            else
            {
                bool needBottom = FloatScheduleNative.IsRaisedAboveForeignWindow(hwnd);
                if (needBottom && (!opportunistic || AllowOpportunisticBottomPush()))
                    FloatScheduleNative.SetWindowPos(hwnd, FloatScheduleNative.HWND_BOTTOM, 0, 0, 0, 0,
                        FloatScheduleNative.SWP_NOMOVE | FloatScheduleNative.SWP_NOSIZE | FloatScheduleNative.SWP_NOACTIVATE |
                        FloatScheduleNative.SWP_NOSENDCHANGING | FloatScheduleNative.SWP_NOOWNERZORDER);
            }
        }
        catch { }
    }

    private bool AllowOpportunisticBottomPush()
    {
        if (_bottomContentionYielded) return false;
        var now = Environment.TickCount64;
        if (now - _lastOpportunisticPushTick < OpportunisticBottomMinIntervalMs) return false;
        _lastOpportunisticPushTick = now;
        _opportunisticPushTicks.Enqueue(now);
        var windowMs = (long)BottomContentionWindow.TotalMilliseconds;
        while (_opportunisticPushTicks.Count > 0 && now - _opportunisticPushTicks.Peek() > windowMs)
            _opportunisticPushTicks.Dequeue();
        if (_opportunisticPushTicks.Count >= BottomContentionPushThreshold)
        {
            _bottomContentionYielded = true;
            _opportunisticPushTicks.Clear();
        }
        return true;
    }

    // 层级重设触发源（Mode 0/1 依赖宿主事件，子进程退化为 50ms + z-order 条件断言，稳态零 SetWindowPos）
    private DispatcherTimer? _topmostRefreshTimer;
    private void ReattachTopmostRefresh()
    {
        try
        {
            var intervalMs = _snap.TopmostRefreshMode switch
            {
                3 => 1,
                4 => 2000,
                _ => 50,
            };
            if (_topmostRefreshTimer == null)
            {
                _topmostRefreshTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(intervalMs) };
                _topmostRefreshTimer.Tick += (_, _) => ApplyWindowLayer(opportunistic: true);
            }
            else
            {
                _topmostRefreshTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
            }
            if (!_topmostRefreshTimer.IsEnabled) _topmostRefreshTimer.Start();
        }
        catch { }
    }

    // ===================== 扩展样式 / 点击穿透 / 防截图 =====================

    private void ApplyExStylesSafe()
    {
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        try
        {
            int current = (int)(long)FloatScheduleNative.GetWindowLong(hwnd, FloatScheduleNative.GWL_EXSTYLE);
            int target = FloatScheduleNative.ComputeDesiredExStyle(current, _snap.ClickThrough, _snap.PreventCapture, _snap.Layer);
            FloatScheduleNative.ApplyExStyles(hwnd, target);
        }
        catch { }
    }

    private bool _lastAppliedClickThrough;
    private void ApplyClickThrough(bool force = false)
    {
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        bool through = _snap.ClickThrough;
        if (!force && _lastAppliedClickThrough == through) return;
        _lastAppliedClickThrough = through;
        try
        {
            // 统一由 ComputeDesiredExStyle 收敛全部扩展样式位（TRANSPARENT 必须与 LAYERED 同存）。
            // 注意：不要在此调用 SetLayeredWindowAttributes——Win8.x 上会使悬浮窗完全不可见。
            ApplyExStylesSafe();
            if (through && FloatScheduleNative.GetForegroundWindow() == hwnd)
            {
                // 若本窗口当前是前台窗口，把焦点交给 z-order 下方第一个可见窗口（对齐 ClassIsland）
                FloatScheduleNative.MoveFocusToWindowBehind(hwnd);
            }
        }
        catch { }
        try { _card.IsHitTestVisible = !through; } catch { }
    }

    private void ApplyPreventCapture()
    {
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        try
        {
            if (_snap.PreventCapture)
            {
                // 【WPF 透明窗限制】AllowsTransparency 走分层窗口合成，WDA_EXCLUDEFROMCAPTURE 对分层窗无效
                //  （表现为截图中仍是黑色矩形）→ 分层窗退化为 WDA_MONITOR（整窗从截图中隐去）。
                int exStyle = (int)(long)FloatScheduleNative.GetWindowLong(hwnd, FloatScheduleNative.GWL_EXSTYLE);
                bool isLayered = (exStyle & FloatScheduleNative.WS_EX_LAYERED) != 0;
                if (isLayered)
                {
                    FloatScheduleNative.SetWindowDisplayAffinity(hwnd, FloatScheduleNative.WDA_MONITOR);
                }
                else if (!FloatScheduleNative.SetWindowDisplayAffinity(hwnd, FloatScheduleNative.WDA_EXCLUDEFROMCAPTURE))
                {
                    FloatScheduleNative.SetWindowDisplayAffinity(hwnd, FloatScheduleNative.WDA_MONITOR);
                }
            }
            else
            {
                FloatScheduleNative.SetWindowDisplayAffinity(hwnd, FloatScheduleNative.WDA_NONE);
            }
        }
        catch { }
    }

    // ===================== 随机标题 =====================

    private static string GetRandomTitleString()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-={}|:\"<>?[]\\;',./~`";
        var bytes = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var len = 8 + (Math.Abs(BitConverter.ToInt32(bytes, 0)) % 24);
        Span<byte> rand = stackalloc byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(rand);
        var sb = new System.Text.StringBuilder(len);
        for (int i = 0; i < len; i++)
            sb.Append(chars[rand[i % rand.Length] % chars.Length]);
        return sb.ToString();
    }

    private void ApplyRandomTitle()
    {
        try { Title = _snap.RandomTitle ? GetRandomTitleString() : DefaultTitle; } catch { }
    }

    private DispatcherTimer? _randomTitleTimer;
    private void StartOrStopRandomTitleTimer()
    {
        var shouldRun = _snap.RandomTitle && _snap.RandomTitleEnhanced;
        try
        {
            if (shouldRun)
            {
                _randomTitleTimer ??= new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
                _randomTitleTimer.Tick -= RandomTitleTick;
                _randomTitleTimer.Tick += RandomTitleTick;
                if (!_randomTitleTimer.IsEnabled) _randomTitleTimer.Start();
            }
            else
            {
                _randomTitleTimer?.Stop();
            }
        }
        catch { }
    }
    private void RandomTitleTick(object? sender, EventArgs e) => ApplyRandomTitle();

    // ===================== 拖拽（自实现：鼠标 + 触摸，位置以 DIP 计算） =====================

    private bool _mouseDragActive;
    private Point _dragStartPointer;          // 窗口内 DIP
    private double _dragStartLeft, _dragStartTop;
    private long _lastDragMoveTick;
    private double _dragPendingLeft, _dragPendingTop;
    private bool _dragPendingValid;
    private int _touchDragId = -1;
    private Point _touchStartPointer;
    private double _touchStartLeft, _touchStartTop;

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_snap.ClickThrough) return;
        if (_mouseDragActive || _touchDragId >= 0) return;
        try
        {
            if (!BeginDrag(e.GetPosition(this), out _dragStartLeft, out _dragStartTop)) return;
            _dragStartPointer = e.GetPosition(this);
            _mouseDragActive = true;
            CaptureMouse();
            e.Handled = true;
        }
        catch { EndDrag(); }
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_mouseDragActive) return;
        try
        {
            var p = e.GetPosition(this);
            double left = _dragStartLeft + (p.X - _dragStartPointer.X);
            double top = _dragStartTop + (p.Y - _dragStartPointer.Y);
            ApplyDragTarget(left, top);
            e.Handled = true;
        }
        catch { EndDrag(); }
    }

    private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_mouseDragActive) return;
        EndDrag();
        e.Handled = true;
    }

    private void Card_TouchDown(object sender, TouchEventArgs e)
    {
        if (_snap.ClickThrough) return;
        if (_mouseDragActive || _touchDragId >= 0) return;
        try
        {
            var p = e.GetTouchPoint(this).Position;
            if (!BeginDrag(p, out _touchStartLeft, out _touchStartTop)) return;
            _touchStartPointer = p;
            _touchDragId = e.TouchDevice.Id;
            try { e.TouchDevice.Capture(_card); } catch { }
            e.Handled = true;
        }
        catch { EndDrag(); }
    }

    private void Card_TouchMove(object sender, TouchEventArgs e)
    {
        if (_touchDragId < 0 || e.TouchDevice.Id != _touchDragId) return;
        try
        {
            var p = e.GetTouchPoint(this).Position;
            ApplyDragTarget(
                _touchStartLeft + (p.X - _touchStartPointer.X),
                _touchStartTop + (p.Y - _touchStartPointer.Y));
            e.Handled = true;
        }
        catch { EndDrag(); }
    }

    private void Card_TouchUp(object sender, TouchEventArgs e)
    {
        if (_touchDragId < 0 || e.TouchDevice.Id != _touchDragId) return;
        EndDrag();
        e.Handled = true;
    }

    /// <summary>拖拽开始：复位贴边状态与位置上报挂起项（拖动期间不得被判为贴边隐藏）。</summary>
    private bool BeginDrag(Point pointerInWindow, out double startLeft, out double startTop)
    {
        startLeft = 0;
        startTop = 0;
        if (_edgeAnimating) return false;
        startLeft = Left;
        startTop = Top;
        _suppressTopmostRefresh = true;
        try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
        _edgeSlideOutPending = false;
        _lastDragMoveTick = 0;
        _dragPendingValid = false;
        // 【修复：从贴边隐藏态拖出后位置不落库】拖动是用户明确的位置意图，先清掉"隐藏/动画"标记再上报。
        if (_edgeHidden || _edgeAnimating)
        {
            _edgeAnimating = false;
            _edgeHidden = false;
            StopEdgeSlideTimer();
            try { _edgeEvalCts?.Cancel(); } catch { }
        }
        return true;
    }

    private void ApplyDragTarget(double left, double top)
    {
        ClampDragTargetToScreen(ref left, ref top);
        long now = Environment.TickCount64;
        if (now - _lastDragMoveTick < 16)
        {
            _dragPendingLeft = left;
            _dragPendingTop = top;
            _dragPendingValid = true;
            return;
        }
        _lastDragMoveTick = now;
        _dragPendingValid = false;
        Left = left;
        Top = top;
    }

    private void EndDrag()
    {
        if (!_mouseDragActive && _touchDragId < 0) return;
        _mouseDragActive = false;
        _touchDragId = -1;
        _suppressTopmostRefresh = false;
        try { ReleaseMouseCapture(); } catch { }
        try
        {
            if (_dragPendingValid)
            {
                Left = _dragPendingLeft;
                Top = _dragPendingTop;
                _dragPendingValid = false;
            }
            ApplyWindowLayer();
            ReportPosition(force: true);
            ScheduleEdgeEval();
        }
        catch { }
    }

    private void ClampDragTargetToScreen(ref double left, ref double top)
    {
        try
        {
            if (!ResolveEdgeWorkArea(out var wl, out var wt, out var wr, out var wb)) return;
            double w = ActualWidth > 0 ? ActualWidth : Width;
            double h = ActualHeight > 0 ? ActualHeight : Height;
            if (w <= 0 || h <= 0) return;
            left = Math.Clamp(left, wl, Math.Max(wl, wr - w));
            top = Math.Clamp(top, wt, Math.Max(wt, wb - h));
        }
        catch { }
    }

    /// <summary>
    /// 上报窗口位置给插件（插件据此持久化到设置，下次启动/重启时恢复）。
    /// 【修复：重启时位置被重置】窗口刚 Show / 首次布局（SizeToContent 测量）期间位置存在瞬态值，
    ///   若此时上报会把存档位置覆盖成垃圾值 → 下次重启窗口跳到错误位置。
    ///   故显示后先静默一小段"稳定期"再开始上报；用户主动拖动（force=true）不受此限制。
    /// </summary>
    private void ReportPosition(bool force = false)
    {
        try
        {
            if (_edgeAnimating || _edgeHidden) return;     // 隐藏态/动画中位置不是用户位置
            if (!force && !_positionReportArmed) return;   // 稳定期内不落库，避免瞬态位置污染存档
            if (double.IsNaN(Left) || double.IsNaN(Top)) return;
            _pipe.Send(FloatScheduleIpcMsgType.Position, new FloatSchedulePositionPayload
            {
                X = (int)Math.Round(Left),
                Y = (int)Math.Round(Top),
            });
        }
        catch { }
    }

    private const int PositionReportSettleMs = 1000;
    private bool _positionReportArmed;
    private DispatcherTimer? _positionArmTimer;

    private void ArmPositionReportingAfterSettle()
    {
        if (_positionReportArmed) return;
        try
        {
            _positionArmTimer ??= new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(PositionReportSettleMs)
            };
            _positionArmTimer.Tick -= PositionArmTimer_Tick;
            _positionArmTimer.Tick += PositionArmTimer_Tick;
            if (!_positionArmTimer.IsEnabled) _positionArmTimer.Start();
        }
        catch { }
    }

    private void PositionArmTimer_Tick(object? sender, EventArgs e)
    {
        try { _positionArmTimer?.Stop(); } catch { }
        _positionReportArmed = true;
        // 到点后补报一次当前（已稳定）位置：覆盖"稳定期内被系统钳制/纠正"的情况
        ReportPosition(force: true);
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (_mouseDragActive || _touchDragId >= 0) return;
        if (_hostVisible && IsVisible && !_edgeAnimating && !_edgeHidden)
            ReportPosition();
        ScheduleEdgeEval();
    }

    // ===================== 贴边自动隐藏（状态机对齐主项目，几何以 DIP 计算） =====================

    private const int EdgeHideThreshold = 1;          // 距边缘 ≤1 DIP 视为贴边
    private const int EdgeHideVisibleStripDip = 6;    // 滑出后保留的可见条
    private const int EdgeHideAnimMs = 200;
    private const int EdgeHideEvalDelayMs = 250;

    private bool _edgeDocked;
    private bool _edgeHidden;
    private bool _edgeAnimating;
    private int _edgeSide;                 // 0=不贴边 1=左 2=右 3=上 4=下
    private double _edgeDockedLeft, _edgeDockedTop;
    private double _edgeHiddenTargetLeft, _edgeHiddenTargetTop;
    private double _edgeLastSizeW, _edgeLastSizeH;
    private (double Left, double Top, double Right, double Bottom)? _edgeWorkArea;
    private CancellationTokenSource? _edgeEvalCts;
    private CancellationTokenSource? _edgeSlideOutDelayCts;
    private bool _edgeSlideOutPending;
    private DispatcherTimer? _edgeSlideTimer;
    private double _edgeAnimFromLeft, _edgeAnimFromTop;
    private double _edgeAnimTargetLeft, _edgeAnimTargetTop;
    private bool _edgeAnimWillHide;
    private long _edgeAnimStartTicks;

    private void ScheduleEdgeEval()
    {
        if (!_snap.EdgeHide) return;
        if (_edgeAnimating) return;
        try { _edgeEvalCts?.Cancel(); } catch { }
        _edgeEvalCts = new CancellationTokenSource();
        _ = EdgeEvalDelayedAsync(_edgeEvalCts.Token);
    }

    private async Task EdgeEvalDelayedAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(EdgeHideEvalDelayMs, ct);
            if (!ct.IsCancellationRequested)
                Dispatcher.BeginInvoke(new Action(() => { try { EvaluateEdgeDock(); } catch { } }));
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    /// <summary>取窗口所在屏幕工作区（DIP）：能查到（窗口在屏上）就用实时值并刷新缓存；查不到（贴边隐藏后离屏）用缓存兜底。</summary>
    private bool ResolveEdgeWorkArea(out double left, out double top, out double right, out double bottom)
    {
        if (TryGetWorkAreaDip(out left, out top, out right, out bottom))
        {
            _edgeWorkArea = (left, top, right, bottom);
            return true;
        }
        if (_edgeWorkArea is { } cached)
        {
            left = cached.Left;
            top = cached.Top;
            right = cached.Right;
            bottom = cached.Bottom;
            return true;
        }
        left = top = right = bottom = 0;
        return false;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    /// <summary>窗口所在监视器的工作区，物理像素 → DIP（经窗口 PresentationSource 换算）。</summary>
    private bool TryGetWorkAreaDip(out double left, out double top, out double right, out double bottom)
    {
        left = top = right = bottom = 0;
        try
        {
            var hwnd = Hwnd;
            if (hwnd == IntPtr.Zero) return false;
            var mi = new MONITORINFO();
            mi.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>();
            var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hMon == IntPtr.Zero || !GetMonitorInfo(hMon, ref mi)) return false;

            var src = PresentationSource.FromVisual(this);
            if (src?.CompositionTarget == null)
            {
                left = mi.rcWork.Left; top = mi.rcWork.Top; right = mi.rcWork.Right; bottom = mi.rcWork.Bottom;
                return true;
            }
            var f = src.CompositionTarget.TransformFromDevice;
            var tl = f.Transform(new Point(mi.rcWork.Left, mi.rcWork.Top));
            var br = f.Transform(new Point(mi.rcWork.Right, mi.rcWork.Bottom));
            left = tl.X; top = tl.Y; right = br.X; bottom = br.Y;
            return true;
        }
        catch { return false; }
    }

    private double CurrentWidth => ActualWidth > 0 ? ActualWidth : Width;
    private double CurrentHeight => ActualHeight > 0 ? ActualHeight : Height;

    private void EvaluateEdgeDock()
    {
        if (!IsVisible || !_snap.EdgeHide || _edgeAnimating || _mouseDragActive || _touchDragId >= 0) return;
        try
        {
            if (!ResolveEdgeWorkArea(out var wl, out var wt, out var wr, out var wb)) return;
            double w = CurrentWidth, h = CurrentHeight;
            if (w <= 0 || h <= 0) return;

            double posLeft = Left, posTop = Top;
            if (_edgeHidden && Math.Abs(posLeft - _edgeHiddenTargetLeft) < 0.5 && Math.Abs(posTop - _edgeHiddenTargetTop) < 0.5)
                return;

            double dLeft = posLeft - wl;
            double dRight = wr - (posLeft + w);
            double dTop = posTop - wt;
            double dBottom = wb - (posTop + h);
            int side = 0;
            double best = EdgeHideThreshold;
            if (dLeft < best) { best = dLeft; side = 1; }
            if (dRight < best) { best = dRight; side = 2; }
            if (dTop < best) { best = dTop; side = 3; }
            if (dBottom < best) { best = dBottom; side = 4; }

            if (side == 0)
            {
                if (_edgeDocked)
                {
                    _edgeDocked = false;
                    _edgeHidden = false;
                    _edgeSide = 0;
                }
                try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
                _edgeSlideOutPending = false;
                return;
            }

            // 贴边：非隐藏态下记录"用户位置"为滑回目标（隐藏态不得覆盖，否则会把屏幕外隐藏位当成用户位置）
            if (!_edgeDocked || (!_edgeHidden && (Math.Abs(posLeft - _edgeHiddenTargetLeft) > 0.5 || Math.Abs(posTop - _edgeHiddenTargetTop) > 0.5)))
            {
                _edgeDockedLeft = posLeft;
                _edgeDockedTop = posTop;
                _edgeSide = side;
                _edgeDocked = true;
            }

            if (!TryComputeEdgeHiddenPos(side, _edgeDockedLeft, _edgeDockedTop, w, h, wl, wt, wr, wb,
                    out var hl, out var ht))
                return;
            _edgeHiddenTargetLeft = hl;
            _edgeHiddenTargetTop = ht;
            _edgeLastSizeW = w;
            _edgeLastSizeH = h;
            // 【自愈】隐藏态下窗口必须正好停在"按当前尺寸算出的隐藏位"：尺寸变化后旧位置会让窗口
            //   整块移出屏幕（连 6px 可见条都没有），这里立即纠正。
            if (_edgeHidden && (Math.Abs(posLeft - hl) > 0.5 || Math.Abs(posTop - ht) > 0.5))
            {
                try { Left = hl; Top = ht; } catch { }
            }
            if (!_edgeHidden && !_edgeSlideOutPending)
                ScheduleEdgeSlideOut();
        }
        catch { }
    }

    /// <summary>计算"滑出隐藏位"：沿贴靠边移出，只保留 <see cref="EdgeHideVisibleStripDip"/> 的可见条。
    /// 【必须按当前窗口尺寸算】左/上边的隐藏位含"减宽/减高"项，尺寸变化后旧隐藏位不再成立。</summary>
    private static bool TryComputeEdgeHiddenPos(int side, double dockedLeft, double dockedTop, double w, double h,
        double wl, double wt, double wr, double wb, out double hideLeft, out double hideTop)
    {
        hideLeft = dockedLeft;
        hideTop = dockedTop;
        switch (side)
        {
            case 1: hideLeft = wl - w + EdgeHideVisibleStripDip; break;   // 左：滑出，右缘留可见条
            case 2: hideLeft = wr - EdgeHideVisibleStripDip; break;       // 右：滑出，左缘留可见条
            case 3: hideTop = wt - h + EdgeHideVisibleStripDip; break;    // 上：滑出，下缘留可见条
            case 4: hideTop = wb - EdgeHideVisibleStripDip; break;        // 下：滑出，上缘留可见条
            default: return false;
        }
        return true;
    }

    /// <summary>
    /// 【修复：内容变矮/变窄后贴边隐藏的窗口整块跑出屏幕】尺寸变化时按新尺寸重算隐藏位；
    /// 隐藏态则直接把窗口移到新隐藏位。返回是否成功算出隐藏位（false 时不提交尺寸缓存，以便后续重试）。
    /// </summary>
    private bool ApplyEdgeHiddenPos(double w, double h)
    {
        if (_edgeSide == 0) return false;
        if (!ResolveEdgeWorkArea(out var wl, out var wt, out var wr, out var wb)) return false;
        if (!TryComputeEdgeHiddenPos(_edgeSide, _edgeDockedLeft, _edgeDockedTop, w, h, wl, wt, wr, wb,
                out var hl, out var ht))
            return false;
        _edgeHiddenTargetLeft = hl;
        _edgeHiddenTargetTop = ht;
        if (_edgeHidden && !_edgeAnimating)
        {
            try { Left = hl; Top = ht; } catch { }
        }
        else if (_edgeAnimating && _edgeAnimWillHide)
        {
            _edgeAnimTargetLeft = hl;
            _edgeAnimTargetTop = ht;
        }
        return true;
    }

    private void RefreshEdgeGeometryOnResize()
    {
        try
        {
            if (!_edgeDocked || _mouseDragActive || _touchDragId >= 0) return;   // 未贴边 / 拖拽中：与隐藏位无关
            double w = CurrentWidth, h = CurrentHeight;
            if (w <= 0 || h <= 0) return;
            if (Math.Abs(w - _edgeLastSizeW) < 0.5 && Math.Abs(h - _edgeLastSizeH) < 0.5) return;   // 尺寸没变 → 零开销返回
            // 【关键修复】只有应用成功才提交尺寸缓存，否则一次失败后永远判定"尺寸没变"，窗口永久停在屏幕外。
            if (!ApplyEdgeHiddenPos(w, h)) return;
            _edgeLastSizeW = w;
            _edgeLastSizeH = h;
        }
        catch { }
    }

    private void ScheduleEdgeSlideOut()
    {
        if (!_snap.EdgeHide) return;
        if (_mouseDragActive || _touchDragId >= 0) return;
        try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
        _edgeSlideOutDelayCts = new CancellationTokenSource();
        _edgeSlideOutPending = true;
        _ = EdgeSlideOutDelayedAsync(_edgeSlideOutDelayCts.Token);
    }

    private async Task EdgeSlideOutDelayedAsync(CancellationToken ct)
    {
        int delayMs = GetEdgeHideDelayMs();
        try
        {
            if (delayMs > 0) await Task.Delay(delayMs, ct);
            else if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) { return; }
        catch { return; }
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _edgeSlideOutPending = false;
            try
            {
                if (!_snap.EdgeHide || !IsVisible) return;
                if (!_edgeDocked || _edgeHidden || _edgeAnimating || _mouseDragActive || _touchDragId >= 0) return;
                EdgeSlideTo(_edgeHiddenTargetLeft, _edgeHiddenTargetTop, willBeHidden: true);
            }
            catch { }
        }));
    }

    private int GetEdgeHideDelayMs()
    {
        try { return (int)Math.Round(Math.Clamp(_snap.EdgeHideDelay, 0.0, 60.0) * 1000.0); }
        catch { return 3000; }
    }

    private void EdgeSlideTo(double targetLeft, double targetTop, bool willBeHidden)
    {
        StopEdgeSlideTimer();
        _edgeAnimating = true;
        double fromLeft = Left, fromTop = Top;
        if (Math.Abs(fromLeft - targetLeft) < 0.5 && Math.Abs(fromTop - targetTop) < 0.5)
        {
            _edgeAnimating = false;
            _edgeHidden = willBeHidden;
            return;
        }
        _edgeAnimFromLeft = fromLeft;
        _edgeAnimFromTop = fromTop;
        _edgeAnimTargetLeft = targetLeft;
        _edgeAnimTargetTop = targetTop;
        _edgeAnimWillHide = willBeHidden;
        _edgeAnimStartTicks = Environment.TickCount64;
        _edgeSlideTimer ??= new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1) };
        _edgeSlideTimer.Tick -= EdgeSlideTimer_Tick;
        _edgeSlideTimer.Tick += EdgeSlideTimer_Tick;
        _edgeSlideTimer.Start();
    }

    private void EdgeSlideTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            double t = (Environment.TickCount64 - _edgeAnimStartTicks) / (double)EdgeHideAnimMs;
            if (t >= 1.0)
            {
                StopEdgeSlideTimer();
                Left = _edgeAnimTargetLeft;
                Top = _edgeAnimTargetTop;
                _edgeHidden = _edgeAnimWillHide;
                _edgeAnimating = false;
                return;
            }
            double eased = 1 - Math.Pow(1 - t, 3);
            Left = _edgeAnimFromLeft + (_edgeAnimTargetLeft - _edgeAnimFromLeft) * eased;
            Top = _edgeAnimFromTop + (_edgeAnimTargetTop - _edgeAnimFromTop) * eased;
        }
        catch
        {
            try { StopEdgeSlideTimer(); Left = _edgeAnimTargetLeft; Top = _edgeAnimTargetTop; _edgeHidden = _edgeAnimWillHide; } catch { }
            _edgeAnimating = false;
        }
    }

    private void StopEdgeSlideTimer()
    {
        try { if (_edgeSlideTimer != null && _edgeSlideTimer.IsEnabled) _edgeSlideTimer.Stop(); } catch { }
    }

    private void DisableEdgeHide()
    {
        try { _edgeEvalCts?.Cancel(); } catch { }
        StopEdgeSlideTimer();
        try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
        _edgeSlideOutPending = false;
        try
        {
            if (_edgeDocked && IsVisible)
            {
                if (_edgeHidden || _edgeAnimating)
                {
                    Left = _edgeDockedLeft;
                    Top = _edgeDockedTop;
                }
                ReportPosition(force: true);
            }
        }
        catch { }
        _edgeDocked = false;
        _edgeHidden = false;
        _edgeAnimating = false;
        _edgeSide = 0;
        // 尺寸/工作区缓存归零：下次重新贴边时按当时尺寸与屏幕实时重算
        _edgeLastSizeW = 0;
        _edgeLastSizeH = 0;
        _edgeWorkArea = null;
    }

    /// <summary>指针是否落在"贴边隐藏后露出的可见条"内（工作区内那部分窗口矩形）。</summary>
    private bool IsPointerInEdgeStrip()
    {
        if (!IsVisible) return false;
        try
        {
            if (!FloatScheduleNative.GetCursorPos(out var pt)) return false;
            if (!TryGetWindowRectDevice(out var rc)) return false;
            if (!TryGetWorkAreaDevice(out var wa)) return false;
            int x1 = Math.Max(rc.Left, wa.Left), y1 = Math.Max(rc.Top, wa.Top);
            int x2 = Math.Min(rc.Right, wa.Right), y2 = Math.Min(rc.Bottom, wa.Bottom);
            if (x2 <= x1 || y2 <= y1) return false;
            return pt.X >= x1 && pt.X < x2 && pt.Y >= y1 && pt.Y < y2;
        }
        catch { return false; }
    }

    private bool TryGetWindowRectDevice(out RECT rc)
    {
        rc = default;
        try
        {
            var hwnd = Hwnd;
            if (hwnd == IntPtr.Zero) return false;
            return GetWindowRect(hwnd, out rc);
        }
        catch { return false; }
    }

    private bool TryGetWorkAreaDevice(out RECT wa)
    {
        wa = default;
        try
        {
            var hwnd = Hwnd;
            if (hwnd == IntPtr.Zero) return false;
            var mi = new MONITORINFO();
            mi.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>();
            var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hMon == IntPtr.Zero || !GetMonitorInfo(hMon, ref mi)) return false;
            wa = mi.rcWork;
            return true;
        }
        catch { return false; }
    }

    private void UpdateEdgeHover()
    {
        if (!_snap.EdgeHide || !IsVisible) return;
        if (!_edgeDocked || _edgeAnimating) return;
        try
        {
            // 【悬停不阻止隐藏】指针在窗口内不取消滑出；唯一阻止隐藏的是"正在拖动"。
            //  只有在"已隐藏"状态下指针进入可见条才滑回。
            if (_edgeHidden && IsPointerInEdgeStrip())
            {
                EdgeSlideTo(_edgeDockedLeft, _edgeDockedTop, willBeHidden: false);
                return;
            }
            // 【滑出重挂】滑回后 50ms Tick 会重新挂起延迟滑出，保证"移开指针 → 延迟 → 再次贴边隐藏"闭环
            if (!_edgeHidden && !_edgeSlideOutPending)
                ScheduleEdgeSlideOut();
        }
        catch { }
    }

    // ===================== 悬停淡化（50ms 轮询 + 步进过渡） =====================

    private const int FadeStableThresholdTicks = 3;
    private const double FadeTargetFaded = 0.05;
    private const double FadeTargetNormal = 1.0;

    private DispatcherTimer? _hoverTimer;
    private int _fadeStableCount;
    private bool _fadeLastObserved;
    private bool _lastFadedApplied;
    private DispatcherTimer? _fadeAnimTimer;
    private double _fadeAnimFrom, _fadeAnimTo;
    private long _fadeAnimStartTicks;
    private int _preventCaptureRecheckTick;

    private void StartOrStopHoverTimer()
    {
        try
        {
            _hoverTimer ??= new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(50) };
            _hoverTimer.Tick -= HoverTimer_Tick;
            _hoverTimer.Tick += HoverTimer_Tick;
            if (!_hoverTimer.IsEnabled) _hoverTimer.Start();
        }
        catch { }
    }

    private void HoverTimer_Tick(object? sender, EventArgs e)
    {
        ApplyHoverFade();
        UpdateEdgeHover();
        // 【修复：贴边隐藏位随尺寸自愈（关键）】50ms 常驻兜底重算隐藏位：LayoutUpdated/SizeChanged 可能
        //   在"平台窗口真正改变尺寸"之前就拿到旧尺寸而早退，此后不再触发 → 窗口整块移出屏幕。
        RefreshEdgeGeometryOnResize();
        // 防截图周期重设兜底（50ms×100=5s）
        _preventCaptureRecheckTick = (_preventCaptureRecheckTick + 1) % 100;
        if (_preventCaptureRecheckTick == 0) ApplyPreventCapture();
    }

    /// <summary>判定指针是否在悬浮窗屏幕矩形内（点击穿透时 WPF 收不到鼠标消息，必须走系统级 GetCursorPos）。</summary>
    private bool IsPointerInWindow()
    {
        try
        {
            if (!IsVisible) return false;
            if (!FloatScheduleNative.GetCursorPos(out var pt)) return false;
            var tl = _card.PointToScreen(new Point(0, 0));
            double w = _card.ActualWidth, h = _card.ActualHeight;
            if (w <= 0 || h <= 0)
            {
                tl = PointToScreen(new Point(0, 0));
                w = ActualWidth;
                h = ActualHeight;
                if (w <= 0 || h <= 0) return false;
            }
            var br = _card.PointToScreen(new Point(w, h));
            return pt.X >= (int)Math.Round(tl.X) && pt.X <= (int)Math.Round(br.X)
                && pt.Y >= (int)Math.Round(tl.Y) && pt.Y <= (int)Math.Round(br.Y);
        }
        catch { return false; }
    }

    private void ApplyHoverFade(bool force = false)
    {
        try
        {
            bool observed;
            if (!_snap.HoverFade) observed = false;
            else observed = IsPointerInWindow() ^ _snap.HoverFadeReverse;

            bool shouldApply = true;
            bool masterDisabled = !_snap.HoverFade;
            if (!force && !masterDisabled)
            {
                if (observed == _fadeLastObserved) _fadeStableCount = Math.Min(_fadeStableCount + 1, FadeStableThresholdTicks + 1);
                else { _fadeLastObserved = observed; _fadeStableCount = 1; }
                if (_fadeStableCount < FadeStableThresholdTicks) shouldApply = false;
            }
            else
            {
                _fadeLastObserved = observed;
                _fadeStableCount = FadeStableThresholdTicks;
            }

            bool faded = observed;
            bool targetChanged = faded != _lastFadedApplied;
            if (!force && !masterDisabled && (!shouldApply || !targetChanged)) return;

            double targetValue = faded ? FadeTargetFaded : FadeTargetNormal;
            if (masterDisabled || force)
            {
                StopFadeAnim();
                Opacity = targetValue;
                _lastFadedApplied = faded;
                return;
            }
            _lastFadedApplied = faded;
            StartFadeAnim(targetValue);
        }
        catch { }
    }

    private void StartFadeAnim(double to)
    {
        _fadeAnimFrom = Opacity;
        if (Math.Abs(_fadeAnimFrom - to) < 1e-6)
        {
            Opacity = to;
            return;
        }
        _fadeAnimTo = to;
        _fadeAnimStartTicks = Environment.TickCount64;
        _fadeAnimTimer ??= new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        _fadeAnimTimer.Tick -= FadeAnim_Tick;
        _fadeAnimTimer.Tick += FadeAnim_Tick;
        _fadeAnimTimer.Start();
    }

    private void FadeAnim_Tick(object? sender, EventArgs e)
    {
        try
        {
            const int durationMs = 250;
            double t = (Environment.TickCount64 - _fadeAnimStartTicks) / (double)durationMs;
            if (t >= 1.0)
            {
                StopFadeAnim();
                Opacity = _fadeAnimTo;
                return;
            }
            double eased = 1 - Math.Pow(1 - t, 3);
            Opacity = _fadeAnimFrom + (_fadeAnimTo - _fadeAnimFrom) * eased;
        }
        catch
        {
            StopFadeAnim();
            try { Opacity = _fadeAnimTo; } catch { }
        }
    }

    private void StopFadeAnim()
    {
        try { if (_fadeAnimTimer != null && _fadeAnimTimer.IsEnabled) _fadeAnimTimer.Stop(); } catch { }
    }

    // ===================== 本地推进（抗宿主异常的核心机制） =====================
    //  宿主 Task 级崩溃 / UI 线程卡死 / 进程退出都会让推送停止 —— 若子进程只会"冻结最后一帧"，
    //  就会出现换课不切、课间行不出现、高亮不动、进度停住。机制：常驻 500ms 推进器，
    //  推送新鲜（<StaleThresholdMs）时完全由推送驱动；一旦停更则按本地时钟自主重算当前状态并重渲染。
    private const long StaleThresholdMs = 6000;
    private long _modelUtcMs;
    private int _localClassIndex = int.MinValue;
    private double _localBreakStart = -1;
    private DispatcherTimer? _localDriveTimer;

    private void StartLocalDrive()
    {
        if (_localDriveTimer != null) return;
        _localDriveTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
        _localDriveTimer.Tick += (_, _) => TickLocalDrive();
        _localDriveTimer.Start();
    }

    /// <summary>按本地时钟推算"ClassIsland 当前时间"（当日秒）：以最近模型里的锚点 + 本地流逝时间。</summary>
    private double ComputeLocalNowSec()
    {
        var a = _lastModel?.Anchor;
        if (a == null) return -1;
        double elapsed = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - a.SentUtcMs) / 1000.0;
        return a.NowSecOfDay + elapsed;
    }

    private void TickLocalDrive()
    {
        try
        {
            var m = _lastModel;
            if (m == null) return;
            // 【明日课表不走进度条】本地推进只适用于当天课表（"当前课"语义只对当天成立）。
            if (m.ShowTomorrow) return;
            // 推送新鲜 → 交回宿主驱动（避免双源冲突）
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _modelUtcMs < StaleThresholdMs) return;

            double nowSec = ComputeLocalNowSec();
            if (nowSec < 0) return;

            // 1) 定位当前课（左闭右开：边界点归下一段）
            int classIdx = -1;
            for (int i = 0; i < m.Rows.Count; i++)
            {
                var r = m.Rows[i];
                if (r.EndSec > r.StartSec && nowSec >= r.StartSec && nowSec < r.EndSec) { classIdx = i; break; }
            }

            // 2) 定位当前课间
            FloatScheduleBreakModel? curBreak = null;
            foreach (var b in m.AllBreaks)
            {
                if (b.EndSec > b.StartSec && nowSec >= b.StartSec && nowSec < b.EndSec) { curBreak = b; break; }
            }

            // 3) 进度比例
            double classRatio = 0, breakRatio = 0;
            if (classIdx >= 0)
            {
                var r = m.Rows[classIdx];
                classRatio = Math.Clamp((nowSec - r.StartSec) / (r.EndSec - r.StartSec), 0.0, 1.0);
            }
            if (curBreak != null)
                breakRatio = Math.Clamp((nowSec - curBreak.StartSec) / (curBreak.EndSec - curBreak.StartSec), 0.0, 1.0);

            double breakStartKey = curBreak?.StartSec ?? -1;
            bool stateChanged = classIdx != _localClassIndex || Math.Abs(breakStartKey - _localBreakStart) > 0.001;
            _localClassIndex = classIdx;
            _localBreakStart = breakStartKey;

            if (stateChanged)
            {
                // 状态变化（换课 / 课间开始结束）→ 用本地重算结果重渲染整表
                RenderModel(BuildLocalModel(m, classIdx, curBreak, classRatio, breakRatio));
            }
            else
            {
                ApplyProgress(classRatio, breakRatio);
            }
        }
        catch { /* 本地推进异常不影响窗口其他能力 */ }
    }

    /// <summary>基于最近一次宿主模型，用本地时间重算出的"当前状态模型"（仅覆盖随状态变化的字段）。</summary>
    private static FloatScheduleRenderModel BuildLocalModel(
        FloatScheduleRenderModel src, int classIdx, FloatScheduleBreakModel? curBreak,
        double classRatio, double breakRatio)
    {
        var m = new FloatScheduleRenderModel
        {
            FontSize = src.FontSize,
            FontFamilySource = src.FontFamilySource,
            AccentArgb = src.AccentArgb,
            CardBackgroundArgb = src.CardBackgroundArgb,
            BorderArgb = src.BorderArgb,
            TextArgb = src.TextArgb,
            SubTextArgb = src.SubTextArgb,
            HighlightArgb = src.HighlightArgb,
            BreakRowBackgroundArgb = src.BreakRowBackgroundArgb,
            SeparatorArgb = src.SeparatorArgb,
            ShowTomorrow = src.ShowTomorrow,
            PlaceholderText = src.PlaceholderText,
            Rows = src.Rows,
            CurrentClassIndex = classIdx,
            Break = curBreak,
            ClassProgressRatio = classRatio,
            BreakProgressRatio = breakRatio,
            Anchor = src.Anchor,
        };
        m.SeparatorAfterClassIndex.AddRange(src.SeparatorAfterClassIndex);
        m.AllBreaks.AddRange(src.AllBreaks);
        return m;
    }

    /// <summary>仅重渲染内容（不刷新"数据新鲜度"时间戳，供本地推进使用）。</summary>
    private void RenderModel(FloatScheduleRenderModel model)
    {
        FloatScheduleWpfRenderer.ApplyCardStyle(_card, model);
        var (root, refs) = FloatScheduleWpfRenderer.Build(model);
        _card.Child = root;
        _refs = refs;
        FloatScheduleWpfRenderer.ApplyProgressRatio(refs.ClassProgressHost, refs.ClassProgressIndicator, model.ClassProgressRatio);
        FloatScheduleWpfRenderer.ApplyProgressRatio(refs.BreakProgressHost, refs.BreakProgressIndicator, model.BreakProgressRatio);
    }
}
