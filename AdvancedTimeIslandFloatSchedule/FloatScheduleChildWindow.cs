// 子进程悬浮窗：窗口属性/交互语义对齐主项目 Services\FloatingScheduleService.cs 的进程内实现
// （统一指针拖拽、贴边隐藏状态机、悬停淡化、层级竞争防护、点击穿透三件套、防截图、随机标题），
// 数据源换成插件管道推送的渲染模型（FloatScheduleRenderer 共享渲染，保证两种模式逐帧一致）。
// 差异说明：
//  - TopmostRefreshMode 0/1（依赖宿主事件/子类化）退化为 50ms 定时器 + z-order 条件断言
//    （稳态零 SetWindowPos 调用，行为等价）；
//  - 位置仅 Init 时应用（拖拽后子进程为唯一事实源并经管道回报插件）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AdvancedTimeIsland.Shared.FloatingSchedule;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AdvancedTimeIsland.FloatScheduleChild;

internal sealed class FloatScheduleChildWindow : Window
{
    const string DefaultTitle = "AdvancedTimeIsland FloatingSchedule";

    readonly FloatSchedulePipeClient _pipe = new();
    CancellationTokenSource _cts = new();

    Border _card;
    FloatScheduleProgressRefs? _refs;
    FloatScheduleRenderModel? _lastModel;
    FloatScheduleWindowSettings _snap = new();
    bool _initialPositionApplied;
    bool _hostVisible = true;          // 插件 HideMode 判定结果（false=被要求隐藏）
    bool _firstDataReceived;
    int _exitRequested;                // 托盘"退出"幂等标记（Command 与 Click 双挂，防止重复处理）

    public FloatScheduleChildWindow()
    {
        Title = DefaultTitle;
        ExtendClientAreaToDecorationsHint = true;
        CanResize = false;
        ShowInTaskbar = false;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None };
        SizeToContent = SizeToContent.WidthAndHeight;
        Opacity = 1.0;
        MinWidth = 150;
        MaxWidth = 820;
#if NET10_0_OR_GREATER
        // Avalonia 12：SystemDecorations 已过时（枚举类型换为 WindowDecorations）
        WindowDecorations = WindowDecorations.None;
#else
        SystemDecorations = SystemDecorations.None;
#endif
        // 窗口级字体兜底（模型带 FontFamilySource 时渲染器在内容根上覆盖）
        FontFamily = new FontFamily("HarmonyOS Sans SC, Microsoft YaHei UI");

        _card = new Border
        {
            CornerRadius = new CornerRadius(FloatScheduleRenderer.CardCornerRadius),
            Padding = FloatScheduleRenderer.CardPadding,
            Margin = new Thickness(0),
            BorderThickness = FloatScheduleRenderer.CardBorderThickness,
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x2D, 0x2D, 0x30)),
        };
        Content = _card;

        _card.PointerPressed += Card_PointerPressed;
        _card.PointerReleased += Card_PointerReleased;
        _card.PointerMoved += Card_PointerMoved;
        _card.PointerCaptureLost += Card_PointerCaptureLost;

        Opened += OnOpened;
        PositionChanged += OnPositionChanged;
        Activated += (_, _) => { try { if (_snap.ClickThrough) ApplyClickThrough(force: true); else ApplyExStylesSafe(); } catch { } };

        _pipe.MessageReceived += OnPipeMessage;
        _pipe.ConnectedChanged += OnPipeConnectedChanged;
    }

    IntPtr Hwnd
    {
        get { try { return this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero; } catch { return IntPtr.Zero; } }
    }

    /// <summary>管道连接状态（供看门狗误报守卫读取）。</summary>
    public bool PipeConnected => _pipe.Connected;

    // ===================== 启动 =====================

    public void StartWithPipe()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => _pipe.RunAsync(_cts.Token));
        StartStartupGuard();
        StartLocalDrive();   // 抗宿主异常的本地推进（宿主推送停更时按本地时钟维持课表状态）
    }

    // 启动兜底：跟随启停=开 时，若 60s 内始终没能建立连接并收到数据（宿主侧异常），自动退出，
    //  避免留下永久重连的"孤儿"悬浮窗进程（这类进程会与宿主侧重启逻辑互相干扰）。
    DispatcherTimer? _startupGuardTimer;
    void StartStartupGuard()
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
    void StopStartupGuard()
    {
        try { _startupGuardTimer?.Stop(); } catch { }
        _startupGuardTimer = null;
    }

    void OnOpened(object? sender, EventArgs e)
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

    void OnPipeConnectedChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_pipe.Connected)
            {
                StopReconnectGuard();
                // 已连接但插件迟迟不发 Init（异常场景）→ 30s 兜底退出，避免孤儿隐藏窗口
                if (!_firstDataReceived) StartInitTimeoutGuard();
            }
            else if (_firstDataReceived)
            {
                // 断线：本地推进器（500ms 常驻）会在推送停更 6s 后自动接管，按本地时钟继续维护
                //   课表状态（换课 / 课间行 / 高亮 / 进度），因此这里无需额外切换显示模式。
                //   跟随启停=开 → 宿主多半已退出，加"60s 未重连即退出"兜底，避免变成僵尸；
                //   跟随启停=关 → 保持存活与自主推进，等待宿主重启后被 adopt。
                if (Program.FollowHostLifetime) StartReconnectGuard();
            }
        });
    }

    // 断线重连兜底（跟随启停=开）：60s 内未能重连成功则主动退出。
    DispatcherTimer? _reconnectGuardTimer;
    void StartReconnectGuard()
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
    void StopReconnectGuard()
    {
        try { _reconnectGuardTimer?.Stop(); } catch { }
        _reconnectGuardTimer = null;
    }

    DispatcherTimer? _initGuardTimer;
    void StartInitTimeoutGuard()
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

    void StopInitTimeoutGuard()
    {
        try { _initGuardTimer?.Stop(); } catch { }
        _initGuardTimer = null;
    }

    void OnPipeMessage(FloatScheduleIpcMsgType type, System.Text.Json.JsonElement data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                switch (type)
                {
                    case FloatScheduleIpcMsgType.Init:
                    {
                        var init = FloatScheduleIpc.Deserialize<FloatScheduleInitPayload>(data);
                        // 协议版本校验：插件被更新后（跟随启停=关 时旧子进程可能仍在运行并被 adopt），
                        //   新旧协议可能不兼容 —— 版本不匹配则主动退出，让插件启动与自身匹配的新版本实例，
                        //   避免"旧子进程接管新插件"造成的错乱。
                        if (init?.Settings != null && init.Settings.ProtocolVersion != FloatScheduleIpc.ProtocolVersion)
                        {
                            CloseAndExit();
                            break;
                        }
                        // exe 指纹校验：插件更新后其包内 exe 已变化，而本进程运行的仍是旧副本 ——
                        //   主动退出，让插件启动与新版插件匹配的子进程（否则新功能/修复不会生效）。
                        if (init?.Settings != null && !string.IsNullOrEmpty(init.Settings.ExeStamp))
                        {
                            var selfStamp = GetSelfExeStamp();
                            if (selfStamp != null && selfStamp != init.Settings.ExeStamp)
                            {
                                CloseAndExit();
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
                            try { FloatScheduleApp.Self?.EnsureTrayIcon(); } catch { }
                        }
                        // 【修复：连接后插件显示 PID -1】Ready（PID 握手）必须**每次连接**都发，而不是只在首次收到数据时发：
                        //  本进程早于 ClassIsland 启动（跟随启停=关 时上一代子进程冻结存活 / 手动先开独立程序）时，
                        //  _firstDataReceived 可能已是 true（连过上一代宿主）→ 原先不再发 Ready →
                        //  新宿主的插件收不到 PID，状态栏显示"已连接 (PID -1)"。
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
        });
    }

    void CloseAndExit()
    {
        try { _cts.Cancel(); } catch { }
        try { Close(); } catch { }
        Environment.Exit(0);
    }

    /// <summary>本进程 exe 的"版本指纹"（长度-最后写入时间），用于判断自身是否已被新版插件淘汰。
    /// 本进程运行的是临时目录副本；插件侧下发的指纹同样取自该副本文件，故正常运行必然一致。</summary>
    static string? GetSelfExeStamp()
    {
        try
        {
            var p = Environment.ProcessPath;
            if (string.IsNullOrEmpty(p)) return null;
            var fi = new FileInfo(p);
            return fi.Exists ? $"{fi.Length}-{fi.LastWriteTimeUtc.Ticks}" : null;
        }
        catch { return null; }
    }

    /// <summary>用户从托盘图标选择"退出"。
    /// 必须先告知插件：否则插件会把这次退出当作"子进程意外退出"而自动重启（限频 3 次/30s），
    /// 用户会看到悬浮窗"关不掉"。插件收到 ExitRequested 后会关闭独立进程模式并回退到进程内渲染
    /// （悬浮课表继续显示，只是改由 ClassIsland 进程绘制），且停止跟踪本进程。
    /// 然后再退出本进程；即使插件无响应也会在 300ms 后自行退出，不留残留。</summary>
    public void RequestExitByUser()
    {
        // 幂等：菜单项的 Command 与 Click 都挂了（双保险），可能先后触发，这里只处理第一次。
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

    void ApplyModel(FloatScheduleRenderModel model)
    {
        _lastModel = model;
        _modelUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();   // 刷新"数据新鲜度"
        // 宿主推送到达 → 交回推送驱动：重置本地推进已应用状态，避免与推送结果互相覆盖
        _localClassIndex = int.MinValue;
        _localBreakStart = -1;
        RenderModel(model);
    }

    void ApplyProgress(double classRatio, double breakRatio)
    {
        if (_refs == null) return;
        FloatScheduleRenderer.ApplyProgressRatio(_refs.ClassProgressHost, _refs.ClassProgressIndicator, classRatio);
        FloatScheduleRenderer.ApplyProgressRatio(_refs.BreakProgressHost, _refs.BreakProgressIndicator, breakRatio);
    }

    void ApplySettings(FloatScheduleWindowSettings s, bool isInit)
    {
        var prev = _snap;
        _snap = s;
        Program.FollowHostLifetime = s.FollowHostLifetime;   // 热更新（看门狗触发时读最新值）

        // 【修复：重启时位置被重置（根因一）】必须在**第一次收到任意设置快照**时应用位置，
        //  而不是只在 Init 里应用：插件启动时子进程可能先于设置快照建立连接（Init 的 Settings 为 null），
        //  此时若跳过应用，窗口会用平台默认位置显示，并把该默认位置回报给插件 → 覆盖掉存档位置。
        //  后续 Connected 事件补发的 Settings 消息携带同样的位置，在这里补应用即可纠正。
        if (!_initialPositionApplied)
        {
            try { Position = new PixelPoint(s.PositionX, s.PositionY); } catch { }
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
        else if (prev.EdgeHideDelay != s.EdgeHideDelay) ScheduleEdgeEval();
    }

    void SetHostVisible(bool visible)
    {
        if (_hostVisible == visible) return;
        _hostVisible = visible;
        if (!_firstDataReceived) return;
        try
        {
            if (visible) { Show(); ApplyWindowLayer(); ArmPositionReportingAfterSettle(); }
            else { try { if (_edgeHiddenAv) Position = _edgeDockedPosAv; } catch { } Hide(); }
        }
        catch { }
    }

    // ===================== 层级（置顶/置底 + 竞争防护，对齐 ApplyWindowLayer） =====================

    bool _suppressTopmostRefresh;
    long _lastOpportunisticPushTick;
    bool _bottomContentionYielded;
    readonly Queue<long> _opportunisticPushTicks = new();
    const int OpportunisticBottomMinIntervalMs = 1500;
    const int BottomContentionPushThreshold = 4;
    static readonly TimeSpan BottomContentionWindow = TimeSpan.FromSeconds(10);

    void ApplyWindowLayer(bool opportunistic = false)
    {
        if (_suppressTopmostRefresh) return;
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) { try { Topmost = _snap.Layer == 1; } catch { } return; }
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

    bool AllowOpportunisticBottomPush()
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

    // 层级重设触发源：Mode 2/3/4 = 50ms/1ms/2s 定时器；Mode 0/1（依赖宿主事件/子类化）退化为 50ms 定时器
    //（z-order 条件断言保证稳态零 SetWindowPos → 无闪烁，行为等价）。
    DispatcherTimer? _topmostRefreshTimer;
    void ReattachTopmostRefresh()
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
            else _topmostRefreshTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
            if (!_topmostRefreshTimer.IsEnabled) _topmostRefreshTimer.Start();
        }
        catch { }
    }

    // ===================== 扩展样式 / 点击穿透 / 防截图 =====================

    void ApplyExStylesSafe()
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

    bool _lastAppliedClickThrough;
    void ApplyClickThrough(bool force = false)
    {
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        bool through = _snap.ClickThrough;
        if (!force && _lastAppliedClickThrough == through) return;
        _lastAppliedClickThrough = through;
        try
        {
            ApplyExStylesSafe();
            if (through)
            {
                FloatScheduleNative.SetLayeredWindowAttributes(hwnd, 0, 255, FloatScheduleNative.LWA_ALPHA);
                if (FloatScheduleNative.GetForegroundWindow() == hwnd)
                    FloatScheduleNative.MoveFocusToWindowBehind(hwnd);
            }
        }
        catch { }
        try { _card.IsHitTestVisible = !through; } catch { }
    }

    void ApplyPreventCapture()
    {
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        try
        {
            if (_snap.PreventCapture)
            {
                if (!FloatScheduleNative.SetWindowDisplayAffinity(hwnd, FloatScheduleNative.WDA_EXCLUDEFROMCAPTURE))
                    FloatScheduleNative.SetWindowDisplayAffinity(hwnd, FloatScheduleNative.WDA_MONITOR);
            }
            else FloatScheduleNative.SetWindowDisplayAffinity(hwnd, FloatScheduleNative.WDA_NONE);
        }
        catch { }
    }

    // ===================== 随机标题 =====================

    static string GetRandomTitleString()
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

    void ApplyRandomTitle()
    {
        try { Title = _snap.RandomTitle ? GetRandomTitleString() : DefaultTitle; } catch { }
    }

    DispatcherTimer? _randomTitleTimer;
    void StartOrStopRandomTitleTimer()
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
            else _randomTitleTimer?.Stop();
        }
        catch { }
    }
    void RandomTitleTick(object? sender, EventArgs e) => ApplyRandomTitle();

    // ===================== 拖拽（统一指针事件模型，对齐主项目语义） =====================

    bool _dragActive;
    IPointer? _dragPointer;
    PixelPoint _dragStartScreenPx;
    PixelPoint _dragStartWindowPx;
    long _lastDragMoveTick;
    PixelPoint _dragPendingPos;
    bool _dragPendingValid;
    SizeToContent _preDragSizeMode;

    void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_snap.ClickThrough) return;
        var point = e.GetCurrentPoint(_card);
        bool isMouseLeft = e.Pointer.Type == PointerType.Mouse && point.Properties.IsLeftButtonPressed;
        bool isTouchOrPen = e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen;
        if (!isMouseLeft && !isTouchOrPen) return;
        if (_dragActive) return;
        try
        {
            _preDragSizeMode = SizeToContent;
            if (_preDragSizeMode != SizeToContent.Manual) SizeToContent = SizeToContent.Manual;
            _dragActive = true;
            _dragPointer = e.Pointer;
            _suppressTopmostRefresh = true;
            try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
            _edgeSlideOutPending = false;
            _lastDragMoveTick = 0;
            _dragPendingValid = false;
            _dragStartScreenPx = this.PointToScreen(e.GetPosition(this));
            _dragStartWindowPx = Position;
            try { e.Pointer.Capture(_card); } catch { }
            try { e.Handled = true; } catch { }
        }
        catch { EndDrag(); }
    }

    void Card_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragActive) return;
        if (_dragPointer != null && !ReferenceEquals(e.Pointer, _dragPointer)) return;
        try
        {
            var curScreenPx = this.PointToScreen(e.GetPosition(this));
            var target = new PixelPoint(
                _dragStartWindowPx.X + (curScreenPx.X - _dragStartScreenPx.X),
                _dragStartWindowPx.Y + (curScreenPx.Y - _dragStartScreenPx.Y));
            target = ClampDragTargetToScreen(target);
            long now = Environment.TickCount64;
            if (now - _lastDragMoveTick < 16)
            {
                _dragPendingPos = target;
                _dragPendingValid = true;
                e.Handled = true;
                return;
            }
            _lastDragMoveTick = now;
            _dragPendingValid = false;
            Position = target;
            e.Handled = true;
        }
        catch { EndDrag(); }
    }

    void Card_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragActive && (_dragPointer == null || ReferenceEquals(e.Pointer, _dragPointer)))
        {
            EndDrag();
            try { e.Handled = true; } catch { }
        }
    }

    void Card_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_dragActive) return;
        if (_dragPointer != null && !ReferenceEquals(e.Pointer, _dragPointer)) return;
        EndDrag();
    }

    void EndDrag()
    {
        if (!_dragActive) return;
        _dragActive = false;
        _suppressTopmostRefresh = false;
        try { _dragPointer?.Capture(null); } catch { }
        _dragPointer = null;
        try
        {
            if (_dragPendingValid) { Position = _dragPendingPos; _dragPendingValid = false; }
            if (SizeToContent != _preDragSizeMode) SizeToContent = _preDragSizeMode;
            ApplyWindowLayer();
            // 【修复：从贴边隐藏态拖出后位置不落库】拖动是用户明确的位置意图，必须先清掉"隐藏/动画"标记再上报：
            //  否则 ReportPosition 会因 _edgeHiddenAv 直接返回 → 新位置不落库 → 下次重启窗口跳回旧位置
            //  （用户所见"位置被重置"）。清标记后 ScheduleEdgeEval 会按松手位置重新判定贴边/不贴边。
            if (_edgeHiddenAv || _edgeAnimating)
            {
                _edgeAnimating = false;
                _edgeHiddenAv = false;
                StopEdgeSlideTimer();
                try { _edgeEvalCts?.Cancel(); } catch { }
                try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
                _edgeSlideOutPending = false;
            }
            ReportPosition(force: true);
            ScheduleEdgeEval();
        }
        catch { }
    }

    PixelPoint ClampDragTargetToScreen(PixelPoint target)
    {
        try
        {
            var screen = Screens?.ScreenFromWindow(this);
            if (screen == null) return target;
            if (!TryGetWindowDeviceSize(out int w, out int h)) return target;
            var wa = screen.WorkingArea;
            int maxX = wa.X + Math.Max(0, wa.Width - w);
            int maxY = wa.Y + Math.Max(0, wa.Height - h);
            return new PixelPoint(
                Math.Clamp(target.X, wa.X, maxX),
                Math.Clamp(target.Y, wa.Y, maxY));
        }
        catch { return target; }
    }

    /// <summary>
    /// 上报窗口位置给插件（插件据此持久化到设置，下次启动/重启时恢复）。
    /// 【修复：重启时位置被重置（根因二）】窗口刚 Show / 首次布局（SizeToContent 测量）期间位置存在瞬态值
    ///  （HWND 刚创建的默认级联位、内容测量后的重定位），若此时上报会把存档位置覆盖成垃圾值
    ///  → 下次重启窗口跳到错误位置。故显示后先静默一小段"稳定期"再开始上报；
    ///  用户主动拖动（force=true）不受此限制，始终上报。
    /// </summary>
    void ReportPosition(bool force = false)
    {
        try
        {
            if (_edgeAnimating || _edgeHiddenAv) return;   // 隐藏态/动画中位置不是用户位置
            if (!force && !_positionReportArmed) return;   // 稳定期内不落库，避免瞬态位置污染存档
            _pipe.Send(FloatScheduleIpcMsgType.Position, new FloatSchedulePositionPayload { X = Position.X, Y = Position.Y });
        }
        catch { }
    }

    // 【修复：重启时位置被重置】位置上报稳定期：窗口显示后等待布局稳定再开始上报（并在到点后补报一次，
    //  以便把系统对"越界存档位置"的钳制结果落库）。
    const int PositionReportSettleMs = 1000;
    bool _positionReportArmed;
    DispatcherTimer? _positionArmTimer;

    void ArmPositionReportingAfterSettle()
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

    void PositionArmTimer_Tick(object? sender, EventArgs e)
    {
        try { _positionArmTimer?.Stop(); } catch { }
        _positionReportArmed = true;
        // 到点后补报一次当前（已稳定）位置：覆盖"稳定期内被系统钳制/纠正"的情况
        ReportPosition(force: true);
    }

    // ===================== 贴边自动隐藏（状态机对齐主项目） =====================

    const int EdgeHideThreshold = 1;
    const int EdgeHideVisibleStrip = 6;
    const int EdgeHideAnimMs = 200;
    const int EdgeHideEvalDelayMs = 250;

    bool _edgeDockedAv;
    bool _edgeHiddenAv;
    bool _edgeAnimating;
    string? _edgeSideAv;
    PixelPoint _edgeDockedPosAv;
    PixelPoint _edgeHiddenPosAv;
    CancellationTokenSource? _edgeEvalCts;
    DispatcherTimer? _edgeSlideTimer;
    PixelPoint _edgeAnimFrom;
    PixelPoint _edgeAnimTarget;
    bool _edgeAnimWillHide;
    long _edgeAnimStartTicks;
    int _edgeHoverTicks;
    bool _edgeHoverLastIn;
    CancellationTokenSource? _edgeSlideOutDelayCts;
    bool _edgeSlideOutPending;

    void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (_dragActive) return;
        if (_hostVisible && IsVisible && !_edgeAnimating && !_edgeHiddenAv)
            ReportPosition();
        ScheduleEdgeEval();
    }

    void ScheduleEdgeEval()
    {
        if (!_snap.EdgeHide) return;
        if (_edgeAnimating) return;
        try { _edgeEvalCts?.Cancel(); } catch { }
        _edgeEvalCts = new CancellationTokenSource();
        _ = EdgeEvalDelayedAsync(_edgeEvalCts.Token);
    }

    async Task EdgeEvalDelayedAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(EdgeHideEvalDelayMs, ct);
            if (!ct.IsCancellationRequested) EvaluateEdgeDock();
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    bool TryGetWindowDeviceSize(out int w, out int h)
    {
        w = h = 0;
        try
        {
            var tl = _card.PointToScreen(new Point(0, 0));
            var br = _card.PointToScreen(new Point(_card.Bounds.Width, _card.Bounds.Height));
            w = br.X - tl.X;
            h = br.Y - tl.Y;
            return w > 0 && h > 0;
        }
        catch { return false; }
    }

    void EvaluateEdgeDock()
    {
        if (!IsVisible || !_snap.EdgeHide || _edgeAnimating) return;
        try
        {
            var screen = Screens?.ScreenFromWindow(this);
            if (screen == null) return;
            var wa = screen.WorkingArea;
            if (!TryGetWindowDeviceSize(out int w, out int h)) return;
            var pos = Position;
            if (_edgeHiddenAv && pos == _edgeHiddenPosAv) return;

            int dLeft = pos.X - wa.X;
            int dRight = (wa.X + wa.Width) - (pos.X + w);
            int dTop = pos.Y - wa.Y;
            int dBottom = (wa.Y + wa.Height) - (pos.Y + h);
            string? side = null;
            int best = EdgeHideThreshold;
            if (dLeft < best) { best = dLeft; side = "left"; }
            if (dRight < best) { best = dRight; side = "right"; }
            if (dTop < best) { best = dTop; side = "top"; }
            if (dBottom < best) { best = dBottom; side = "bottom"; }

            if (side == null)
            {
                if (_edgeDockedAv)
                {
                    _edgeDockedAv = false;
                    _edgeHiddenAv = false;
                    _edgeSideAv = null;
                    _edgeHoverTicks = 0;
                    _edgeHoverLastIn = false;
                }
                try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
                _edgeSlideOutPending = false;
                return;
            }

            if (!_edgeDockedAv || pos != _edgeHiddenPosAv)
            {
                _edgeDockedPosAv = pos;
                _edgeSideAv = side;
                _edgeDockedAv = true;
                _edgeHoverTicks = 0;
                _edgeHoverLastIn = true;
            }

            int hx = _edgeDockedPosAv.X, hy = _edgeDockedPosAv.Y;
            switch (side)
            {
                case "left": hx = wa.X - w + EdgeHideVisibleStrip; break;
                case "right": hx = wa.X + wa.Width - EdgeHideVisibleStrip; break;
                case "top": hy = wa.Y - h + EdgeHideVisibleStrip; break;
                case "bottom": hy = wa.Y + wa.Height - EdgeHideVisibleStrip; break;
            }
            _edgeHiddenPosAv = new PixelPoint(hx, hy);
            if (!_edgeHiddenAv && !_edgeSlideOutPending)
                ScheduleEdgeSlideOut();
        }
        catch { }
    }

    void ScheduleEdgeSlideOut()
    {
        if (!_snap.EdgeHide) return;
        if (_dragActive) return;
        try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
        _edgeSlideOutDelayCts = new CancellationTokenSource();
        _edgeSlideOutPending = true;
        _ = EdgeSlideOutDelayedAsync(_edgeSlideOutDelayCts.Token);
    }

    async Task EdgeSlideOutDelayedAsync(CancellationToken ct)
    {
        int delayMs = GetEdgeHideDelayMs();
        try
        {
            if (delayMs > 0) await Task.Delay(delayMs, ct);
            else if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) { return; }
        catch { return; }
        _edgeSlideOutPending = false;
        if (!_snap.EdgeHide || !IsVisible) return;
        if (!_edgeDockedAv || _edgeHiddenAv || _edgeAnimating || _dragActive) return;
        EdgeSlideTo(_edgeHiddenPosAv, willBeHidden: true);
    }

    int GetEdgeHideDelayMs()
    {
        try { return (int)Math.Round(Math.Clamp(_snap.EdgeHideDelay, 0.0, 60.0) * 1000.0); }
        catch { return 3000; }
    }

    void EdgeSlideTo(PixelPoint target, bool willBeHidden)
    {
        StopEdgeSlideTimer();
        _edgeAnimating = true;
        var from = Position;
        if (from == target)
        {
            _edgeAnimating = false;
            _edgeHiddenAv = willBeHidden;
            return;
        }
        _edgeAnimFrom = from;
        _edgeAnimTarget = target;
        _edgeAnimWillHide = willBeHidden;
        _edgeAnimStartTicks = Environment.TickCount64;
        _edgeSlideTimer ??= new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1) };
        _edgeSlideTimer.Tick -= EdgeSlideTimer_Tick;
        _edgeSlideTimer.Tick += EdgeSlideTimer_Tick;
        _edgeSlideTimer.Start();
    }

    void EdgeSlideTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            double t = (Environment.TickCount64 - _edgeAnimStartTicks) / (double)EdgeHideAnimMs;
            if (t >= 1.0)
            {
                StopEdgeSlideTimer();
                Position = _edgeAnimTarget;
                _edgeHiddenAv = _edgeAnimWillHide;
                _edgeHoverTicks = 0;
                _edgeHoverLastIn = !_edgeAnimWillHide;
                _edgeAnimating = false;
                return;
            }
            double eased = 1 - Math.Pow(1 - t, 3);
            Position = new PixelPoint(
                (int)Math.Round(_edgeAnimFrom.X + (_edgeAnimTarget.X - _edgeAnimFrom.X) * eased),
                (int)Math.Round(_edgeAnimFrom.Y + (_edgeAnimTarget.Y - _edgeAnimFrom.Y) * eased));
        }
        catch
        {
            try { StopEdgeSlideTimer(); Position = _edgeAnimTarget; _edgeHiddenAv = _edgeAnimWillHide; } catch { }
            _edgeAnimating = false;
        }
    }

    void StopEdgeSlideTimer()
    {
        try { if (_edgeSlideTimer != null && _edgeSlideTimer.IsEnabled) _edgeSlideTimer.Stop(); } catch { }
    }

    void DisableEdgeHide()
    {
        try { _edgeEvalCts?.Cancel(); } catch { }
        StopEdgeSlideTimer();
        try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
        _edgeSlideOutPending = false;
        try
        {
            if (_edgeDockedAv && IsVisible)
            {
                if (_edgeHiddenAv || _edgeAnimating) Position = _edgeDockedPosAv;
                ReportPosition(force: true);
            }
        }
        catch { }
        _edgeDockedAv = false;
        _edgeHiddenAv = false;
        _edgeAnimating = false;
        _edgeSideAv = null;
        _edgeHoverTicks = 0;
        _edgeHoverLastIn = false;
    }

    bool IsPointerInEdgeStrip()
    {
        if (!IsVisible) return false;
        try
        {
            if (!FloatScheduleNative.GetCursorPos(out var pt)) return false;
            var screen = Screens?.ScreenFromWindow(this);
            if (screen == null) return false;
            var wa = screen.WorkingArea;
            if (!TryGetWindowDeviceSize(out int w, out int h)) return false;
            var pos = Position;
            int x1 = Math.Max(pos.X, wa.X), y1 = Math.Max(pos.Y, wa.Y);
            int x2 = Math.Min(pos.X + w, wa.X + wa.Width), y2 = Math.Min(pos.Y + h, wa.Y + wa.Height);
            if (x2 <= x1 || y2 <= y1) return false;
            return pt.X >= x1 && pt.X < x2 && pt.Y >= y1 && pt.Y < y2;
        }
        catch { return false; }
    }

    void UpdateEdgeHover()
    {
        if (!_snap.EdgeHide || !IsVisible) return;
        if (!_edgeDockedAv || _edgeAnimating) return;
        try
        {
            bool inStrip = IsPointerInEdgeStrip();
            if (inStrip == _edgeHoverLastIn) _edgeHoverTicks++;
            else { _edgeHoverLastIn = inStrip; _edgeHoverTicks = 1; }
            if (_edgeHoverTicks < 2) return;
            if (_edgeHiddenAv && inStrip)
            {
                EdgeSlideTo(_edgeDockedPosAv, willBeHidden: false);
                return;
            }
            if (!_edgeHiddenAv && !_edgeSlideOutPending)
                ScheduleEdgeSlideOut();
        }
        catch { }
    }

    // ===================== 悬停淡化（50ms 轮询 + 步进过渡，不引 Avalonia.Animation 降低代际差异面） =====================

    const int FadeStableThresholdTicks = 3;
    const double FadeTargetFaded = 0.05;
    const double FadeTargetNormal = 1.0;

    DispatcherTimer? _hoverTimer;
    int _fadeStableCount;
    bool _fadeLastObserved;
    bool _lastFadedApplied;
    DispatcherTimer? _fadeAnimTimer;
    double _fadeAnimFrom, _fadeAnimTo;
    long _fadeAnimStartTicks;
    int _preventCaptureRecheckTick;

    void StartOrStopHoverTimer()
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

    void HoverTimer_Tick(object? sender, EventArgs e)
    {
        ApplyHoverFade();
        UpdateEdgeHover();
        // 防截图周期重设兜底（50ms×100=5s，对齐主进程 UpdateProgress 内兜底语义）
        _preventCaptureRecheckTick = (_preventCaptureRecheckTick + 1) % 100;
        if (_preventCaptureRecheckTick == 0) ApplyPreventCapture();
    }

    bool IsPointerInWindow()
    {
        try
        {
            if (!IsVisible) return false;
            if (!FloatScheduleNative.GetCursorPos(out var pt)) return false;
            if (!_card.IsEffectivelyVisible) return false;
            var tl = _card.PointToScreen(new Point(0, 0));
            var br = _card.PointToScreen(new Point(_card.Bounds.Width, _card.Bounds.Height));
            return tl.X <= pt.X && pt.X < br.X && tl.Y <= pt.Y && pt.Y < br.Y;
        }
        catch { return false; }
    }

    void ApplyHoverFade(bool force = false)
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
            else { _fadeLastObserved = observed; _fadeStableCount = FadeStableThresholdTicks; }

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

    void StartFadeAnim(double to)
    {
        _fadeAnimFrom = Opacity;
        if (Math.Abs(_fadeAnimFrom - to) < 1e-6) { Opacity = to; return; }
        _fadeAnimTo = to;
        _fadeAnimStartTicks = Environment.TickCount64;
        _fadeAnimTimer ??= new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        _fadeAnimTimer.Tick -= FadeAnim_Tick;
        _fadeAnimTimer.Tick += FadeAnim_Tick;
        _fadeAnimTimer.Start();
    }

    void FadeAnim_Tick(object? sender, EventArgs e)
    {
        try
        {
            const int durationMs = 250;
            double t = (Environment.TickCount64 - _fadeAnimStartTicks) / (double)durationMs;
            if (t >= 1.0) { StopFadeAnim(); Opacity = _fadeAnimTo; return; }
            double eased = 1 - Math.Pow(1 - t, 3);
            Opacity = _fadeAnimFrom + (_fadeAnimTo - _fadeAnimFrom) * eased;
        }
        catch { StopFadeAnim(); try { Opacity = _fadeAnimTo; } catch { } }
    }

    void StopFadeAnim()
    {
        try { if (_fadeAnimTimer != null && _fadeAnimTimer.IsEnabled) _fadeAnimTimer.Stop(); } catch { }
    }

    // ===================== 本地推进（抗宿主异常的核心机制） =====================
    //  背景：独立进程的价值是"不受 ClassIsland 卡顿/崩溃影响"。但数据来自宿主推送，
    //   宿主 Task 级崩溃 / UI 线程卡死 / 进程退出都会让推送停止 —— 若子进程只会"冻结最后一帧"，
    //   就会出现换课不切、课间行不出现、高亮不动、进度停住等现象（即"被宿主异常波及"）。
    //  机制：常驻 500ms 推进器。宿主推送新鲜（<StaleThresholdMs）时完全由推送驱动；
    //   一旦推送停更，立即改为按本地时钟自主重算"当前课索引 / 当前课间 / 进度"，
    //   并在状态发生变化时用共享渲染器重渲染 —— 于是课表能继续正确切换，直至宿主恢复后自动交回。
    //  说明：该机制同时覆盖"跟随启停=关 时宿主退出后的冻结续走"，故不再需要单独的冻结模式。
    const long StaleThresholdMs = 6000;   // 宿主每 5s 至少一次全量推送，超过 6s 视为停更
    long _modelUtcMs;                     // 最近一次收到宿主 Model 的本地时刻
    int _localClassIndex = int.MinValue;  // 本地推进已应用的高亮行索引（int.MinValue=尚未应用）
    double _localBreakStart = -1;         // 本地推进已应用的课间起始秒（-1=当前无课间）
    DispatcherTimer? _localDriveTimer;

    void StartLocalDrive()
    {
        if (_localDriveTimer != null) return;
        _localDriveTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
        _localDriveTimer.Tick += (_, _) => TickLocalDrive();
        _localDriveTimer.Start();
    }

    /// <summary>按本地时钟推算"ClassIsland 当前时间"（当日秒）：以最近模型里的锚点 + 本地流逝时间。</summary>
    double ComputeLocalNowSec()
    {
        var a = _lastModel?.Anchor;
        if (a == null) return -1;
        double elapsed = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - a.SentUtcMs) / 1000.0;
        return a.NowSecOfDay + elapsed;
    }

    void TickLocalDrive()
    {
        try
        {
            var m = _lastModel;
            if (m == null) return;
            // 【明日课表不走进度条】本地推进只适用于当天课表（"当前课"语义只对当天成立）：
            //  否则宿主推送停更后，这里会拿"今天的时钟"去匹配明天的课程行 → 明天课表凭空出现当前课高亮与
            //  随时间推进的进度条（与今天课表一样），因此明日课表一律冻结为纯列表展示。
            if (m.ShowTomorrow) return;
            // 推送新鲜 → 交回宿主驱动（避免双源冲突：Progress 消息由插件每 500ms 推送）
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
                var local = BuildLocalModel(m, classIdx, curBreak, classRatio, breakRatio);
                RenderModel(local);
            }
            else
            {
                ApplyProgress(classRatio, breakRatio);
            }
        }
        catch { /* 本地推进异常不影响窗口其他能力 */ }
    }

    /// <summary>基于最近一次宿主模型，用本地时间重算出的"当前状态模型"（仅覆盖随状态变化的字段）。</summary>
    static FloatScheduleRenderModel BuildLocalModel(
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
    void RenderModel(FloatScheduleRenderModel model)
    {
        FloatScheduleRenderer.ApplyCardStyle(_card, model);
        var (root, refs) = FloatScheduleRenderer.Build(model);
        _card.Child = root;
        _refs = refs;
        FloatScheduleRenderer.ApplyProgressRatio(refs.ClassProgressHost, refs.ClassProgressIndicator, model.ClassProgressRatio);
        FloatScheduleRenderer.ApplyProgressRatio(refs.BreakProgressHost, refs.BreakProgressIndicator, model.BreakProgressRatio);
    }
}
