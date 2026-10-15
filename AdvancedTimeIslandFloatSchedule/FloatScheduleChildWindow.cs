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
using Avalonia.Animation.Easings;
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

    // 液态玻璃真折射（路线 B）：抓窗口背后的桌面像素 → 共享折射核心 → 作卡片背景。
    // 刷新只做"换背景"，绝不重建内容树（重建是此前把整表压到 2fps 的根因）。
    readonly FloatScheduleGlassBackdrop _glass = new();
    bool _glassBusy;
    DispatcherTimer? _glassTimer;
    // 拖动期间的高频"取帧"定时器：只做"取后台已发布的最新折射结果并换背景"，不做任何折射计算。
    DispatcherTimer? _glassDragTimer;

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
            Padding = new Thickness(0),   // 内边距由材质层内部承载（材质需铺满整卡）
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
        // 【修复：贴边隐藏后内容变矮/变窄 → 窗口整块跑出屏幕】
        //   悬浮窗 SizeToContent：内容变化会改变窗口尺寸，而贴边隐藏位是按旧尺寸算的（left/top 含"减尺寸"项），
        //   尺寸变小后窗口连可见条一起被推出屏幕 → 用户看到"时间表突然完全不见"。
        // 这里在每次布局完成后按新尺寸重算隐藏位（未贴边时零开销早退，且尺寸未变时不重复处理）。
        // 双钩（LayoutUpdated + Resized）：前者覆盖"内容重排但窗口尺寸尚未变"的中间态，
        // 后者是"窗口客户区尺寸真的变了"的权威信号，任一先到都会按最终尺寸重算一次。
        LayoutUpdated += (_, _) => RefreshEdgeGeometryOnResize();
        Resized += (_, _) => RefreshEdgeGeometryOnResize();
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
        StartFpsTimers();    // 高级外观：FPS 角标本地统计（写值按 _snap.ShowFps 门控）
        StartGlassTimer();   // 高级外观：液态玻璃真折射的背景采样（低频，材质=3 且可见时才抓）
    }

    // ===== 高级外观：子进程 FPS（16ms 帧计数 + 500ms 写值；近似渲染帧率）=====
    DispatcherTimer? _fpsFrameTimer, _fpsWriteTimer;
    int _fpsFrames;
    void StartFpsTimers()
    {
        if (_fpsFrameTimer != null) return;
        _fpsFrameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _fpsFrameTimer.Tick += (_, _) => { if (_snap.ShowFps) _fpsFrames++; };
        _fpsFrameTimer.Start();
        _fpsWriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _fpsWriteTimer.Tick += (_, _) =>
        {
            if (!_snap.ShowFps || _refs?.FpsText == null) return;
            double fps = _fpsFrames * 2.0;   // 500ms 窗口 → ×2
            _fpsFrames = 0;
            try { _refs.FpsText.Text = $"{fps:F1}/{fps:F1}"; } catch { }
        };
        _fpsWriteTimer.Start();
    }

    // ===== 高级外观：液态玻璃"真折射"（路线 B）=====
    // 透明窗口拿不到 DWM 合成的桌面像素，唯一来源是屏幕捕获：抓"窗口所占区域"→ 共享折射核心（SDF 折射 +
    // 色散 + 镜面高光）→ 位图作卡片背景。抓帧时临时强制 WDA_EXCLUDEFROMCAPTURE，防止抓到自己形成回环。
    // 刷新策略：按设置里的采样率（Hz，默认 30）；**刷新路径只换背景画笔，绝不重建内容树**（重建内容/材质是卡顿根因）。
    void StartGlassTimer()
    {
        if (_glassTimer != null) return;
        _glassTimer = new DispatcherTimer { Interval = GlassInterval() };
        _glassTimer.Tick += (_, _) =>
        {
            _glassTimer.Interval = GlassInterval();   // 采样率设置变更后热跟随（无需重启）
            if (_snap.Material == 3 && _hostVisible) _ = RefreshGlassBackdropAsync();
            // 采样停（材质切走/窗口隐藏）→ 把捕获亲和性恢复成用户"防截图"设置；采样期间则由抓帧侧保持"排除捕获"
            else FloatScheduleScreenCapture.RestoreAffinity(Hwnd, _snap.PreventCapture);
        };
        _glassTimer.Start();
    }

    // 采样率 → 定时器间隔（Hz 1~60；越高越跟手，抓屏 + 折射 CPU 开销随之上升）
    TimeSpan GlassInterval()
        => TimeSpan.FromMilliseconds(1000.0 / Math.Clamp(_snap.GlassSampleRateHz, 1, 60));

    // 拖动补偿期间启动高频取帧定时器：每 ~8ms 检查后台是否发布了新折射结果，有则换背景。
    //   折射在后台线程完成，这里只做"取最新结果 + 换 Background"（极廉价）；无新结果时不做任何 UI 操作。
    void StartGlassDragTimer()
    {
        if (_glassDragTimer != null) return;
        _glassDragTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(8) };
        _glassDragTimer.Tick += (_, _) => { try { _glass.TryCommitLatest(); } catch { } };
        _glassDragTimer.Start();
    }

    void StopGlassDragTimer()
    {
        try { _glassDragTimer?.Stop(); } catch { }
        _glassDragTimer = null;
    }

    // 【UI 线程】拖动一帧：读窗口真实位置 → **只改 GPU 取样矩形**（零抓帧/零位图写/零新建 Brush，见
    //   FloatScheduleGlassBackdrop.SetDragTransform），累计位移达阈值时由 _glass 内部请求后台重烘焙。
    void UpdateGlassDragTransform()
    {
        if (_snap.Material != 3 || !_snap.GlassDragCompensation) return;
        double sc = RenderScaling <= 0 ? 1.0 : RenderScaling;
        _glass.UpdateDragTransform(_snap.CornerRadius * sc);
    }

    // 卡片背景层：每次 Apply 返回新的 Border（底层位图/画笔由 _glass 复用），无内容时返回 null 走近似边缘光
    Border? GlassBackdropLayer() => _snap.Material == 3 ? _glass.TakeLayer(_snap.CornerRadius) : null;

    async Task RefreshGlassBackdropAsync()
    {
        if (_glassBusy || _snap.Material != 3 || !_hostVisible) return;
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        var size = _card.Bounds.Size;
        if (size.Width < 4 || size.Height < 4) return;
        double sc = RenderScaling <= 0 ? 1.0 : RenderScaling;
        int w = (int)Math.Round(size.Width * sc), h = (int)Math.Round(size.Height * sc);
        double cornerPx = _snap.CornerRadius * sc;
        // 捕获方式 / 拖动补偿设置热跟随（子进程经 settings 消息热下发）
        _glass.CaptureMode = _snap.GlassCaptureMode;
        _glass.DragCompensationEnabled = _snap.GlassDragCompensation;
        // 拖动补偿开启时抓"比卡片大一圈"的区域（供拖动期间偏移取材，免抓帧）
        int pad = _snap.GlassDragCompensation ? FloatScheduleGlassBackdrop.DefaultDragCompensationPadPx : 0;
        long gen = _glass.ContentGeneration;   // 记录代次：若期间开始了拖动则丢弃本次结果
        _glassBusy = true;
        try
        {
            // 抓帧 + 逐像素折射放后台线程（纯计算，不碰 UI 对象）；
            // 亲和性（"排除捕获"）由 CaptureAndRefract 内部按状态迁移维护，不在这里每帧来回切
            await Task.Run(() => _glass.CaptureAndRefract(hwnd, w, h, cornerPx));
            // 【修复：拖动起始闪一帧旧内容】本次采样若是在拖动开始**之前**启动的，完成时已进入拖动，
            //   提交它会让背景瞬间回到拖动前的位置（1 帧）。代次变化即说明拖动已开始 → 丢弃。
            if (_glass.ContentGeneration != gen) return;
            // 只原位更新位图并就地换背景；绝不调用 RenderModel（那会重建整棵可视树）
            _glass.Commit();
            if (_glass.NeedsApply && _lastModel != null)   // 首次拿到内容时才重套一次材质
            {
                _glass.ClearNeedsApply();
                RenderModel(_lastModel);
            }
        }
        catch { }
        finally { _glassBusy = false; }
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
                            CloseAndExit(FloatScheduleIpc.ExitCodeSelfUpdate);
                            break;
                        }
                        // 自身构建指纹校验（自检"我是否已被新版插件淘汰"）：
                        //   插件下发它包内 exe 的内容指纹；与本进程启动瞬间采集的指纹不一致 ⇒ 运行中的是旧构建
                        //   （典型：插件更新后，跟随启停=关 时冻结存活的旧子进程被新宿主 adopt）
                        //   ⇒ 主动退出，让插件立刻用新构建重启一个新子进程；否则新功能/修复永远不会生效。
                        if (init?.Settings != null && !string.IsNullOrEmpty(init.Settings.ExeStamp))
                        {
                            var selfHash = Program.SelfExeHash;
                            if (!string.IsNullOrEmpty(selfHash) && selfHash != init.Settings.ExeStamp)
                            {
                                try
                                {
                                    Console.Error.WriteLine(
                                        "ATI.FloatSchedule self-update detected (running build is obsolete); " +
                                        "exiting with ExitCodeSelfUpdate so host restarts a fresh child");
                                }
                                catch { }
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

    /// <summary>关闭窗口并退出本进程。
    /// exitCode 默认 0；自检发现"已被新版插件淘汰"时传 <see cref="FloatScheduleIpc.ExitCodeSelfUpdate"/>，
    /// 宿主据此识别为计划内重启（不受"意外退出"限频约束）并立刻用新构建重启。</summary>
    void CloseAndExit(int exitCode = 0)
    {
        try { _cts.Cancel(); } catch { }
        try { Close(); } catch { }
        Environment.Exit(exitCode);
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

        // 高级外观：材质/圆角/边距/强度/图片变化 → 用最近模型重渲染卡片（即时生效，无需新数据）
        if (isInit || prev.Material != s.Material || prev.CornerRadius != s.CornerRadius
            || prev.MaterialIntensity != s.MaterialIntensity || prev.CustomBackgroundPath != s.CustomBackgroundPath
            || prev.MarginTop != s.MarginTop || prev.MarginBottom != s.MarginBottom
            || prev.MarginLeft != s.MarginLeft || prev.MarginRight != s.MarginRight)
        {
            ApplyTransparency();   // 材质联动窗口透明级别（磨砂/亚克力/液态玻璃需 AcrylicBlur）
            if (_lastModel != null) { try { RenderModel(_lastModel); } catch { } }
        }
    }

    /// <summary>
    /// 高级外观：按当前材质联动窗口透明级别。
    /// 磨砂(1)/亚克力(2) 走系统合成材质（ExperimentalAcrylicBorder），
    /// 需窗口 TransparencyLevelHint 含 AcrylicBlur/Blur 才能采样桌面背景做模糊；
    /// 液态玻璃(3) 刻意保持纯 Transparent（Apple 取向"折射而非模糊"，背景保持清晰）；其余同样纯 Transparent。
    /// </summary>
    void ApplyTransparency()
    {
        try
        {
            int m = _snap.Material;
            TransparencyLevelHint = (m == 1 || m == 2)
                ? new[] { WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None }
                : new[] { WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None };
        }
        catch { }
    }

    void SetHostVisible(bool visible)
    {
        if (_hostVisible == visible) return;
        _hostVisible = visible;
        if (!_firstDataReceived) return;
        try
        {
            if (visible)
            {
                // 入场：窗口先以"完全移出 + 透明"状态显示，再滑入 + 淡入
                if (!StartHideShow(false)) Show();
                ApplyWindowLayer();
                ArmPositionReportingAfterSettle();
            }
            else
            {
                // 退场：播内容位移 + 淡化的过渡动画，动画结束后才真正 Hide()
                if (!StartHideShow(true))
                {
                    // 取不到几何时的回退：先恢复到贴边前位置再隐藏，避免恢复时闪现
                    try { if (_edgeHiddenAv) Position = _edgeDockedPosAv; } catch { }
                    Hide();
                }
            }
        }
        catch { }
    }

    // ===================== 隐藏/显示过渡动画（对齐 ClassIsland 主界面 MainWindow.VisibilityAnimation） =====================
    //  对齐 ClassIsland 的部分：250ms；入场 cubic-bezier(0.25,1,0.5,1)（减速）、出场 cubic-bezier(0.4,0,1,1)（加速）；
    //  内容透明度同时 0↔1；动画结束后才 Hide()；打断时从当前进度向新目标补间（隐式动画语义），不会漂移或抽动。
    //  运动方式（本插件差异）：**窗口整体向上滑到屏幕顶端之外**，而不是仅在窗口内平移内容——
    //  窗口会把内容裁剪在自身边界内，只平移内容只会"原地消失"，无法产生"滑到屏幕顶端再滑出"的效果。
    static readonly TimeSpan HideShowDuration = TimeSpan.FromMilliseconds(250);
    static readonly Easing HideShowEntranceEasing = Easing.Parse("0.25, 1, 0.5, 1");
    static readonly Easing HideShowExitEasing = Easing.Parse("0.4, 0, 1, 1");
    const double HideShowDistanceExtra = 1.0;
    bool _hideAnimActive;
    bool _hideAnimTargetHidden;
    bool _hiddenByAnim;
    double _hideAnimProgress;
    double _hideAnimFrom;
    double _hideAnimTarget;
    long _hideAnimStartTicks;
    int _hideAnimDurationMs;
    Easing? _hideAnimEasing;
    Vector _hideAnimDelta;                    // 完全滑出屏幕顶端所需的 Y 位移增量（负值）
    PixelPoint? _hideAnimBasePos;             // 本次过渡的基准位置（用户放置位）
    DispatcherTimer? _hideAnimTimer;

    /// <summary>
    /// 计算"完全滑出屏幕顶端"所需的窗口 Y 位移增量：
    /// 目标 Y = 工作区上边缘 − 内容高度 − 1（即内容底边刚好越过屏幕顶端）。
    /// 之所以移动**窗口**而不是只移动内容：窗口会把内容裁剪在自身边界内，
    /// 只平移内容只会"原地消失"，无法产生"滑到屏幕顶端再滑出"的效果。
    /// </summary>
    bool TryResolveHideShowDelta(out Vector delta)
    {
        delta = default;
        var waResolved = ResolveEdgeWorkArea();
        if (waResolved is not { } wa) return false;
        if (!TryGetWindowDeviceSize(out int w, out int h)) return false;

        double targetY = wa.Y - h - HideShowDistanceExtra;
        delta = new Vector(0, targetY - Position.Y);
        return true;
    }

    /// <summary>按进度 p（0=完全可见，1=完全滑出屏幕顶端）应用窗口位移与内容透明度。</summary>
    void ApplyHideShowProgress(double p)
    {
        try
        {
            if (_hideAnimBasePos is { } basePos)
            {
                Position = new PixelPoint(
                    basePos.X,
                    (int)Math.Round(basePos.Y + _hideAnimDelta.Y * p));
            }
        }
        catch { }
        try { _card.Opacity = 1 - p; } catch { }
    }

    /// <summary>
    /// 过渡动画期间禁止窗口交互（拖动/点击）：动画中窗口会滑到屏幕顶端之外，
    /// 此时若允许拖拽会与动画争夺位置，并可能把屏幕外的坐标当成用户位置上报给插件（位置异常）。
    /// </summary>
    void SuppressInteraction()
    {
        try { _card.IsHitTestVisible = false; } catch { }
    }

    /// <summary>过渡动画结束后按"点击穿透"设置恢复交互（穿透开启时仍不可交互）。</summary>
    void RestoreInteraction()
    {
        try { _card.IsHitTestVisible = !_snap.ClickThrough; } catch { }
    }

    /// <summary>
    /// 开始 / 改向"隐藏或显示"过渡动画（250ms + ClassIsland 的两条贝塞尔缓动，透明度同时 0↔1）。
    /// hidden=true → 退场：窗口整体向上滑到屏幕顶端之外，动画结束后 Hide()；
    /// hidden=false → 入场：窗口先以"完全滑出 + 透明"状态显示，再滑回用户放置位并淡入（避免闪出一帧完整内容）。
    /// 动画进行中再次调用表示"改向"：从当前进度向新目标补间，不跳变（等价于 ClassIsland 的隐式动画）。
    /// 返回 false 表示取不到几何，调用方回退为即时 Hide/Show。
    /// </summary>
    bool StartHideShow(bool hidden)
    {
        try
        {
            if (!_hideAnimActive)
            {
                // 新的一次过渡：先确定基准位置（用户放置位）与"完全滑出屏幕顶端"的位移量
                if (!hidden && !IsVisible)
                {
                    // 入场且窗口当前不可见：先恢复到用户位置（贴边隐藏态下当前位是隐藏位）
                    try
                    {
                        if (_edgeHiddenAv)
                        {
                            Position = _edgeDockedPosAv;
                            _edgeHiddenAv = false;
                        }
                    }
                    catch { }
                    try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
                    _edgeSlideOutPending = false;
                }
                _hideAnimBasePos = Position;
                if (!TryResolveHideShowDelta(out var delta)) { RestoreInteraction(); return false; }
                _hideAnimDelta = delta;
                SuppressInteraction();   // 动画期间禁止交互，避免与动画争夺位置

                if (!hidden)
                {
                    // 入场：先落到"完全滑出 + 透明"状态再显示，避免先闪出一帧完整内容
                    _hideAnimProgress = 1;
                    ApplyHideShowProgress(1);
                    Show();
                }
                else
                {
                    _hideAnimProgress = 0;
                }
            }
            else if (TryResolveHideShowDelta(out var deltaNow))
            {
                // 改向：基准与位移量沿用本次过渡，只更新目标（几何可能已变化，同步刷新位移量）
                _hideAnimDelta = deltaNow;
            }

            _hideAnimFrom = _hideAnimProgress;
            _hideAnimTargetHidden = hidden;
            _hideAnimTarget = hidden ? 1 : 0;
            _hideAnimDurationMs = (int)HideShowDuration.TotalMilliseconds;
            _hideAnimEasing = hidden ? HideShowExitEasing : HideShowEntranceEasing;
            _hideAnimStartTicks = Environment.TickCount64;
            _hideAnimActive = true;
            _hideAnimTimer ??= new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1) };
            _hideAnimTimer.Tick -= HideShowTimer_Tick;
            _hideAnimTimer.Tick += HideShowTimer_Tick;
            _hideAnimTimer.Start();
            return true;
        }
        catch
        {
            _hideAnimActive = false;
            RestoreInteraction();
            return false;
        }
    }

    void HideShowTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            double u = (Environment.TickCount64 - _hideAnimStartTicks) / (double)_hideAnimDurationMs;
            if (u >= 1.0)
            {
                FinishHideShow();
                return;
            }
            double eased = _hideAnimEasing?.Ease(u) ?? u;
            _hideAnimProgress = _hideAnimFrom + (_hideAnimTarget - _hideAnimFrom) * eased;
            ApplyHideShowProgress(_hideAnimProgress);
        }
        catch
        {
            try { FinishHideShow(); } catch { }
        }
    }

    /// <summary>
    /// 过渡动画完成：落到目标进度。目标是隐藏 → 标记隐藏态、Hide()，并把窗口位置复位到用户放置位、
    /// 内容复位为"完全可见"（此时窗口不可见，不会闪出；下次入场再从"完全滑出"状态滑回）。
    /// 目标是显示 → 清除隐藏态。全程保持"动画进行中"标志，避免复位过程中的位置变化被上报持久化。
    /// </summary>
    void FinishHideShow()
    {
        StopHideShowTimer();
        if (!_hideAnimActive) return;
        _hideAnimProgress = _hideAnimTarget;
        ApplyHideShowProgress(_hideAnimProgress);
        if (_hideAnimTargetHidden)
        {
            _hiddenByAnim = true;
            // 贴边滑出态下窗口停在"贴边位"，隐藏期间尺寸自愈会依据旧标记把它挪回屏外 → 下次显示落在屏外。
            //  这里复位"已滑出"标记（保留贴边记录），显示后由贴边状态机按原延迟重新滑出。
            if (_edgeDockedAv)
            {
                _edgeHiddenAv = false;
                _edgeHoverTicks = 0;
                _edgeHoverLastIn = false;
                try { _edgeSlideOutDelayCts?.Cancel(); } catch { }
                _edgeSlideOutPending = false;
            }
            try { Hide(); } catch { }
            // 隐藏后再复位：窗口位置回到用户放置位、内容恢复完全可见、进度归零
            //（必须仍在"动画进行中"标志下执行，否则位置变化会被上报给插件持久化）
            _hideAnimProgress = 0;
            try
            {
                if (_hideAnimBasePos is { } basePos) Position = basePos;
            }
            catch { }
            try { _card.Opacity = 1; } catch { }
        }
        else
        {
            _hiddenByAnim = false;
            RestoreInteraction();   // 动画结束：按点击穿透设置恢复交互
        }
        _hideAnimActive = false;
    }

    /// <summary>取消过渡动画并让窗口立即回到用户放置位的"完全可见"状态（用户开始拖动）。幂等。</summary>
    void CancelHideShow()
    {
        StopHideShowTimer();
        _hiddenByAnim = false;
        _hideAnimProgress = 0;
        try
        {
            if (_hideAnimBasePos is { } basePos) Position = basePos;
        }
        catch { }
        try { _card.Opacity = 1; } catch { }
        _hideAnimActive = false;
        RestoreInteraction();
    }

    void StopHideShowTimer()
    {
        try { if (_hideAnimTimer != null && _hideAnimTimer.IsEnabled) _hideAnimTimer.Stop(); } catch { }
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
            // 统一由 ComputeDesiredExStyle 收敛全部扩展样式位（TRANSPARENT 必须与 LAYERED 同存）。
            // 注意：不要在此调用 SetLayeredWindowAttributes——Win8.x 上会使悬浮窗完全不可见
            //  （原因见 FloatScheduleNative.ApplyExStyles 注释，对齐主项目 ApplyClickThrough）。
            ApplyExStylesSafe();
            if (through && FloatScheduleNative.GetForegroundWindow() == hwnd)
            {
                // 若本窗口当前是前台窗口，把焦点交给 z-order 下方第一个可见窗口（对齐 ClassIsland）
                FloatScheduleNative.MoveFocusToWindowBehind(hwnd);
            }
        }
        catch { }
        try { _card.IsHitTestVisible = !through && !_hideAnimActive; } catch { }   // 过渡动画期间保持不可交互
    }

    void ApplyPreventCapture()
    {
        var hwnd = Hwnd;
        if (hwnd == IntPtr.Zero) return;
        try
        {
            // 液态玻璃抓帧期间必须保持"排除捕获"：否则这个 5 秒兜底会把抓帧侧强制打开的
            // EXCLUDEFROMCAPTURE 冲成 WDA_NONE，接下来几帧就会把悬浮窗自己拍进背景 → 卡片上出现
            // "实字 + 淡影"的自采样残影（DXGI 的重复帧还会让残影持续一段时间）。
            if (_snap.PreventCapture || _snap.Material == 3)
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
        // 隐藏/显示过渡动画期间禁止拖拽：动画正在移动窗口，此时接管位置会造成位置异常。
        if (_hideAnimActive) return;
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
            _glass.DragInProgress = true;   // 拖动开始：拖动位移补偿开启时暂停采样抓帧（背景由位移补偿维护）
            StartGlassDragTimer();          // 拖动开始时启动高频取帧定时器（只取后台结果换背景）
            _suppressTopmostRefresh = true;
            CancelHideShow();   // 【隐藏/显示过渡动画】拖动开始：取消动画并让内容立即完全可见
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
            CompensateGlassDrag();   // 液态玻璃：请求后台按位移偏移取材重做折射（不抓帧、不占 UI 帧预算）
            e.Handled = true;
        }
        catch { EndDrag(); }
    }

    /// <summary>
    /// 拖动位移补偿（新路径）：逐帧只做**纯 GPU 平移**（改取样矩形，不抓帧/不折射/不写位图），
    /// 由后台线程在累计位移达阈值时重烘焙整幅折射图；UI 线程随即取一次最新已发布结果换背景。
    /// 仅在 材质=液态玻璃 且 拖动位移补偿=开 时生效。
    /// </summary>
    void CompensateGlassDrag()
    {
        if (_snap.Material != 3 || !_snap.GlassDragCompensation) return;
        try
        {
            UpdateGlassDragTransform();          // 逐帧：只改 GPU 平移（廉价、零分配）
            _glass.TryCommitLatest();            // 取一次后台重烘焙的最新结果（若已发布）
        }
        catch { }
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
            _glass.DragInProgress = false;     // 拖动结束：恢复采样抓帧
            _glass.EndDragCompensation();      // 作废在途拖动补偿结果（避免旧帧覆盖"松手后重抓"的新内容）
            StopGlassDragTimer();
            if (SizeToContent != _preDragSizeMode) SizeToContent = _preDragSizeMode;
            ApplyWindowLayer();
            // 立即按最终位置重抓一帧（不占 UI 线程：抓帧 + 折射都在后台）；避免松手瞬间残留旧位置背景。
            if (_snap.Material == 3 && _hostVisible) _ = RefreshGlassBackdropAsync();
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
            if (_edgeAnimating || _edgeHiddenAv || _hideAnimActive) return;   // 隐藏态/动画中位置不是用户位置
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
        // 过渡动画驱动的位置变化不上报：动画期间窗口会滑到屏幕顶端之外，上报会被插件当成用户位置持久化
        if (_hostVisible && IsVisible && !_edgeAnimating && !_edgeHiddenAv && !_hideAnimActive)
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
        if (!IsVisible || !_snap.EdgeHide || _edgeAnimating || _hideAnimActive) return;   // 过渡动画中不评估贴边（避免动画期间窗口被挪动）
        try
        {
            // 工作区用缓存兜底：窗口已离屏（贴边隐藏）时 ScreenFromWindow 为 null，不能因此放弃评估
            var waResolved = ResolveEdgeWorkArea();
            if (waResolved is not { } wa) return;
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

            // 贴边：当前位置若不是隐藏目标位（= 用户拖到的原始贴边位置）→ 记录为滑回目标。
            // 【隐藏态不得覆盖】隐藏时 pos 就是隐藏位而非用户位置：尺寸变化等会造成它与
            //   _edgeHiddenPosAv 暂时不一致，若无条件覆盖会把"用户位置"污染成屏幕外的隐藏位。
            if (!_edgeDockedAv || (!_edgeHiddenAv && pos != _edgeHiddenPosAv))
            {
                _edgeDockedPosAv = pos;
                _edgeSideAv = side;
                _edgeDockedAv = true;
                _edgeHoverTicks = 0;
                _edgeHoverLastIn = true;
            }

            if (!TryComputeEdgeHiddenPos(side, _edgeDockedPosAv, w, h, wa, out var hidden))
                return;
            _edgeHiddenPosAv = hidden;
            _edgeLastSizeW = w;
            _edgeLastSizeH = h;
            // 【自愈】隐藏态下窗口必须正好停在"按当前尺寸算出的隐藏位"：尺寸变化后旧位置会让窗口
            //   整块移出屏幕（连 6px 可见条都没有），这里立即纠正，使状态无需等下一次滑出就恢复正确。
            if (_edgeHiddenAv && pos != hidden)
            {
                try { Position = hidden; } catch { }
            }
            if (!_edgeHiddenAv && !_edgeSlideOutPending)
                ScheduleEdgeSlideOut();
        }
        catch { }
    }

    /// <summary>
    /// 计算"滑出隐藏位"：沿贴靠边移出，只保留 <see cref="EdgeHideVisibleStrip"/> 像素可见条。
    /// 【必须按当前窗口尺寸算】left/top 的隐藏位含"减尺寸"项（边缘 - 宽/高 + 可见条），
    /// 尺寸变化后旧隐藏位不再成立 —— 尺寸变小时窗口连可见条一起被推出屏幕（见 RefreshEdgeGeometryOnResize）。
    /// </summary>
    static bool TryComputeEdgeHiddenPos(string side, PixelPoint docked, int w, int h, PixelRect wa, out PixelPoint pos)
    {
        int hx = docked.X, hy = docked.Y;
        switch (side)
        {
            case "left": hx = wa.X - w + EdgeHideVisibleStrip; break;
            case "right": hx = wa.X + wa.Width - EdgeHideVisibleStrip; break;
            case "top": hy = wa.Y - h + EdgeHideVisibleStrip; break;
            case "bottom": hy = wa.Y + wa.Height - EdgeHideVisibleStrip; break;
            default: pos = docked; return false;
        }
        pos = new PixelPoint(hx, hy);
        return true;
    }

    // 贴边状态下最近一次已知的窗口设备像素尺寸（用于尺寸变化检测）
    int _edgeLastSizeW, _edgeLastSizeH;
    // 贴边时记录下来的屏幕工作区（隐藏位计算的权威来源，见 ResolveEdgeWorkArea）
    PixelRect? _edgeWorkArea;

    /// <summary>
    /// 取"隐藏位计算所用的屏幕工作区"。
    /// 【关键修复：离屏后查不到屏幕导致自愈永久卡死】
    ///   贴边隐藏后窗口被移出屏幕，Avalonia 的 <c>Screens.ScreenFromWindow</c> 此时会返回 null
    ///   （窗口矩形与任何显示器都不相交）。而"内容变矮需要重算隐藏位"恰恰发生在这种离屏状态下，
    ///   若此刻才去查屏幕 → null → 算不出隐藏位 → 窗口永久停在屏幕外（连 6px 可见条都没有）。
    ///   故：能查到屏幕（窗口在屏上时）就用实时值并刷新缓存；查不到就用"贴边时记录的"工作区兜底。
    /// </summary>
    PixelRect? ResolveEdgeWorkArea()
    {
        try
        {
            var screen = Screens?.ScreenFromWindow(this);
            if (screen != null)
            {
                _edgeWorkArea = screen.WorkingArea;
                return _edgeWorkArea;
            }
        }
        catch { }
        return _edgeWorkArea;
    }

    /// <summary>
    /// 【修复：内容变矮/变窄后贴边隐藏的窗口整块跑出屏幕】
    ///   贴边隐藏位是按"当时的窗口尺寸"算出来的，而悬浮窗是 SizeToContent：内容一变化（上课后课间行消失、
    ///   课表行数变化、从有课切到无课占位等）窗口宽高就会变。旧隐藏位对旧尺寸成立：
    ///     · 贴上边隐藏时 隐藏Y = 工作区上边 - 高度 + 6px → 高度变矮 → 窗口底边跑到屏幕上方 → 完全不可见；
    ///     · 贴左边隐藏时 隐藏X = 工作区左边 - 宽度 + 6px → 宽度变窄 → 窗口右边跑到屏幕左侧 → 完全不可见。
    ///   故布局（尺寸）变化时按新尺寸重算隐藏位；处于隐藏态则直接把窗口移到新隐藏位（可见条保持 6px）。
    /// </summary>
    void RefreshEdgeGeometryOnResize()
    {
        try
        {
            if (!_edgeDockedAv || _dragActive) return;      // 未贴边 / 拖拽中：与隐藏位无关
            if (_hideAnimActive) return;                    // 过渡动画期间窗口位置由动画独占驱动
            if (!TryGetWindowDeviceSize(out int w, out int h)) return;
            if (w == _edgeLastSizeW && h == _edgeLastSizeH) return;   // 尺寸没变 → 零开销返回（避免布局回调循环）
            // 【关键修复】只有应用成功才提交尺寸缓存：否则一旦某次应用失败（如窗口已离屏、算不出隐藏位），
            //   缓存却已更新 → 此后永远判定"尺寸没变" → 再也不会重试，窗口永久停在屏幕外。
            if (!ApplyEdgeHiddenPos(w, h)) return;
            _edgeLastSizeW = w;
            _edgeLastSizeH = h;
        }
        catch { }
    }

    /// <summary>按给定尺寸重算并应用隐藏位：隐藏态立即落位；滑出动画中则同步动画目标（旧目标只对旧尺寸成立）。
    /// 返回是否成功算出了隐藏位（false = 算不出，调用方不得提交尺寸缓存，以便后续重试）。</summary>
    bool ApplyEdgeHiddenPos(int w, int h)
    {
        if (_edgeSideAv is not { } side) return false;
        // 工作区用缓存兜底：窗口已离屏时 ScreenFromWindow 为 null（正是需要重算的场景）
        var waResolved = ResolveEdgeWorkArea();
        if (waResolved is not { } work) return false;
        if (!TryComputeEdgeHiddenPos(side, _edgeDockedPosAv, w, h, work, out var hidden)) return false;
        _edgeHiddenPosAv = hidden;
        // 隐藏滑出/入场动画进行中不得直接写 Position（位置归动画独占，否则两处逐帧争抢 → 抽动）
        if (_edgeHiddenAv && !_edgeAnimating)
        {
            try { Position = hidden; } catch { }
        }
        else if (_edgeAnimating && _edgeAnimWillHide)
        {
            _edgeAnimTarget = hidden;
        }
        return true;
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
        if (!_edgeDockedAv || _edgeHiddenAv || _edgeAnimating || _dragActive || _hideAnimActive) return;
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
        // 尺寸缓存归零：下次重新贴边时强制按当时尺寸重算隐藏位（避免沿用上一次贴边的尺寸基线）
        _edgeLastSizeW = 0;
        _edgeLastSizeH = 0;
        _edgeWorkArea = null;   // 工作区缓存同理：下次重新贴边时按当时的屏幕实时采集
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
        if (!_edgeDockedAv || _edgeAnimating || _hideAnimActive) return;
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
        // 【修复：贴边隐藏位随尺寸自愈（关键）】50ms 常驻兜底重算隐藏位。
        //   为什么不能只靠 LayoutUpdated / Resized：实测"下课(高393)→上课(矮362)"变矮时，
        //   尺寸监听在"平台窗口真正改变尺寸"之前就拿到了旧尺寸（早退出、未重算），此后不再触发，
        //   隐藏位永久停在 −387 → 窗口底边 −25 → 整块移出屏幕（可见条也消失）。
        //   这里每 50ms 比对一次尺寸，尺寸真的变了就必定在下一拍纠正（未贴边时零开销早退）。
        RefreshEdgeGeometryOnResize();
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
        // 高级外观：材质/圆角/边距（卡片级，与进程内同源）；真实悬浮窗为透明窗口 → 走系统亚克力路径
        // 内容内边距内移（材质层铺满整卡，避免非半透明材质出现空心边框）
        FloatScheduleMaterialApplier.Apply(_card, _snap.Material, _snap.MaterialIntensity, _snap.CornerRadius,
            model.CardBackgroundArgb, _snap.CustomBackgroundPath, root,
            osBackdrop: _snap.Material == 1 || _snap.Material == 2,   // 磨砂/亚克力走系统亚克力；液态玻璃走自绘"折射"
            contentPadding: new Thickness(_snap.MarginLeft, _snap.MarginTop, _snap.MarginRight, _snap.MarginBottom),
            backdropLayer: GlassBackdropLayer(), backdropIsRefracted: _glass.HasContent && _snap.Material == 3);
        FloatScheduleMaterialApplier.ApplyBorder(_card, _snap.BorderEnabled, _snap.BorderPreset, _snap.BorderColorEnabled, _snap.BorderColor);
        _refs = refs;
        FloatScheduleRenderer.ApplyProgressRatio(refs.ClassProgressHost, refs.ClassProgressIndicator, model.ClassProgressRatio);
        FloatScheduleRenderer.ApplyProgressRatio(refs.BreakProgressHost, refs.BreakProgressIndicator, model.BreakProgressRatio);
    }
}
