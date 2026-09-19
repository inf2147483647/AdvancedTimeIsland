// 时间表悬浮窗"独立进程模式"宿主侧服务（运行于 ClassIsland 进程内）。
// 职责：
//  1. 命名管道服务器（固定名，插件侧为 Server）：子进程为 Client，"跟随启停=关"时幸存的子进程
//     可在 ClassIsland 重启后自动重连（adopt），恢复数据推送；
//  2. 子进程生命周期：启动（含随机进程名副本）、意外退出限频重启、强制重启、模式关闭/插件退出时终止；
//  3. 数据推送：缓存最新 settings/model/visible，连接建立时以 Init 一次性补发，之后增量推送；
//  4. 状态机：Off/Starting/Connected/Frozen/Failed，供设置页状态文本展示；失败自动回退进程内渲染。
// 仅 Windows 生效；非 Windows 平台 ShouldUseIndependent() 恒为 false（设置页整组禁用）。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Shared.FloatingSchedule;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

public class FloatScheduleHostProcessService : IHostedService, IDisposable
{
    public enum ChildStatus { Off, Starting, Connected, Frozen, Failed }

    /// <summary>供设置页读取状态（DI 单例，构造时写入）。</summary>
    public static FloatScheduleHostProcessService? Instance { get; private set; }

    private readonly PluginSettings _settings;
    private readonly ILogger<FloatScheduleHostProcessService> _logger;

    private readonly object _stateLock = new();
    private readonly object _writeLock = new();

    private NamedPipeServerStream? _pipeServer;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;
    // 【诊断修复 6】_childProcess / _stopRequested 会被启动线程、ThreadPool(Exited)、UI 线程交叉读写，
    // 加 volatile 保证可见性，避免"旧值判定"导致的重复重启/状态抖动。
    private volatile Process? _childProcess;
    private volatile bool _stopRequested;       // 主动停止（关闭模式/插件退出），抑制 Exited 自动重启
    private volatile bool _pipeConnected;
    private volatile int _childPid = -1;   // Ready 回报的真实 PID（adopt 场景没有 Process 句柄）

    // 缓存：连接建立（含 adopt 重连）时以 Init 一次性补发，保证子进程永远能拿到最新数据
    private FloatScheduleWindowSettings? _cachedSettings;
    private FloatScheduleRenderModel? _cachedModel;
    private bool _cachedVisible = true;

    // 失败判定/限频重启：3 次/30s
    private readonly Queue<long> _startTicks = new();
    private const int MaxStartAttempts = 3;
    private static readonly TimeSpan StartAttemptWindow = TimeSpan.FromSeconds(30);
    private const int ReadyTimeoutMs = 20000;

    private string? _failReason;

    public event Action? Connected;
    public event Action<int, int>? PositionReported;
    public event Action? StatusChanged;
    /// <summary>用户在子进程托盘图标点了"退出"（上层据此关闭悬浮时间表相关设置）。</summary>
    public event Action? ExitRequestedByUser;

    public FloatScheduleHostProcessService(PluginSettings settings, ILogger<FloatScheduleHostProcessService> logger)
    {
        _settings = settings;
        _logger = logger;
        Instance = this;
    }

    // ===================== 对外状态 =====================

    public ChildStatus Status { get; private set; } = ChildStatus.Off;
    public bool IsChildConnected => _pipeConnected;

    /// <summary>独立进程模式是否应生效：开关开 + Windows + 未失败。子进程未连接时 FloatingScheduleService 仍走进程内渲染。</summary>
    public bool ShouldUseIndependent() =>
        _settings.FloatingScheduleIndependentProcess && OperatingSystem.IsWindows() && Status != ChildStatus.Failed;

    public string StatusText
    {
        get
        {
            if (!_settings.FloatingScheduleIndependentProcess) return "未启用";
            if (!OperatingSystem.IsWindows()) return "仅 Windows 支持";
            return Status switch
            {
                ChildStatus.Starting => "启动中...",
                ChildStatus.Connected => "已连接 (PID " + (_childPid > 0 ? _childPid : (_childProcess != null && !_childProcess.HasExited ? _childProcess.Id : -1)) + ")",
                ChildStatus.Frozen => "冻结中（ClassIsland 已退出，本地继续走进度）",
                ChildStatus.Failed => "失败：" + (_failReason ?? "未知原因") + "（已回退进程内渲染）",
                _ => "未启用",
            };
        }
    }

    private void SetStatus(ChildStatus s, string? failReason = null)
    {
        ChildStatus old;
        lock (_stateLock)
        {
            old = Status;
            if (Status == s && (failReason == null || _failReason == failReason)) return;
            Status = s;
            if (failReason != null) _failReason = failReason;
            if (s != ChildStatus.Failed) _failReason = null;
        }
        // 状态变迁日志：便于现场核对"重启期间状态是否单调（Off→Starting→Connected）"、定位抖动来源
        _logger.LogDebug("FloatSchedule 子进程状态: {Old} -> {New}{Reason}",
            old, s, failReason == null ? "" : "（" + failReason + "）");
        try { StatusChanged?.Invoke(); } catch { }
    }

    // ===================== 生命周期 =====================

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (ShouldUseIndependent())
        {
            EnsureAcceptLoopRunning();
            _ = EnsureChildAsync();
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // 插件退出（ClassIsland 正常关闭）：跟随启停=开 → 发 shutdown + 兜底 Kill（子进程消失）；
        //                        跟随启停=关 → 只关管道服务器，子进程进入冻结显示。
        // 经闸门串行收尾（与并发中的启动流程不会交错）；限时取闸门，避免退出被长时间阻塞。
        bool gotGate = await _lifecycleGate.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        try
        {
            if (_settings.FloatingScheduleFollowHostLifetime)
            {
                await StopChildUnderGateAsync(kill: true).ConfigureAwait(false);
            }
            else
            {
                _stopRequested = true;   // 抑制 Exited 重启，但不杀子进程（子进程进入冻结显示）
                ClosePipe();
                SetStatus(ChildStatus.Frozen);
            }
        }
        catch (Exception ex) { _logger.LogDebug(ex, "FloatSchedule StopAsync 收尾异常（忽略）"); }
        finally { if (gotGate) _lifecycleGate.Release(); }

        try { _cts?.Cancel(); } catch { }
        try { _pipeServer?.Dispose(); } catch { }
        _pipeServer = null;
    }

    public void Dispose()
    {
        try
        {
            _stopRequested = true;
            ClosePipe();
            try { _pipeServer?.Dispose(); } catch { }
            _pipeServer = null;
            try { _childProcess?.Dispose(); } catch { }
            _childProcess = null;
            GC.SuppressFinalize(this);
        }
        catch { }
    }

    // ===================== 对外发送 API（均可从 UI 线程调用，内部加锁异步写） =====================

    public void CacheModel(FloatScheduleRenderModel model)
    {
        lock (_stateLock) _cachedModel = model;
    }

    public void SendModel(FloatScheduleRenderModel model)
    {
        lock (_stateLock) _cachedModel = model;
        WriteLine(FloatScheduleIpc.Encode(FloatScheduleIpcMsgType.Model, model));
    }

    public void SendSettings(FloatScheduleWindowSettings settings)
    {
        lock (_stateLock) _cachedSettings = settings;
        WriteLine(FloatScheduleIpc.Encode(FloatScheduleIpcMsgType.Settings, settings));
    }

    public void SendVisible(bool visible)
    {
        bool changed;
        lock (_stateLock) { changed = _cachedVisible != visible; _cachedVisible = visible; }
        if (changed)
            WriteLine(FloatScheduleIpc.Encode(FloatScheduleIpcMsgType.Visible, new FloatScheduleVisiblePayload { Visible = visible }));
    }

    public void SendProgress(double classRatio, double breakRatio)
    {
        WriteLine(FloatScheduleIpc.Encode(FloatScheduleIpcMsgType.Progress,
            new FloatScheduleProgressPayload { ClassRatio = classRatio, BreakRatio = breakRatio }));
    }

    private void WriteLine(string line)
    {
        if (!_pipeConnected) return;   // 断线丢弃（重连后由 Init 补发缓存，无需积压）
        try
        {
            lock (_writeLock)
            {
                _writer?.WriteLine(line);
                _writer?.Flush();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FloatSchedule 管道写入失败（视为断线，等待子进程重连）");
            MarkDisconnected();
        }
    }

    // ===================== 子进程启动 / 重启 / 停止 =====================

    /// <summary>确保子进程在跑：先等 2s 观察 incoming 连接（adopt 已存活的孤儿子进程），无则启动新进程。
    /// 防重入：多调用源（StartAsync / 设置开关 / Exited 重启）并发时只允许一个启动流程在跑；
    /// 已有存活子进程时直接返回（杜绝"已存在一个悬浮窗进程仍反复启动新进程"）。</summary>
    // =====【治本】进程生命周期串行闸门 =====
    //  启动 / 停止 / 重启 / 模式切换 / 意外退出自动重启 全部经此闸门排队，
    //  保证任意时刻只有一个生命周期事务在执行 —— 从根本上消除 Start 与 Stop 交错、
    //  重复 spawn、状态与进程对象不一致等并发缺陷。UI 侧禁用按钮只是体验优化，不再是正确性依赖。
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    // 重启进行中标记（仅供 UI 展示；正确性由 _lifecycleGate 保证）
    private int _restartInProgressInt;
    // 管道 listener 就绪信号（握手：确保 listener 已开始监听再启动子进程，避免"连不上→反复重试"）
    private volatile TaskCompletionSource<bool>? _listenerReadyTcs;

    public async Task EnsureChildAsync()
    {
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try { await EnsureChildUnderGateAsync().ConfigureAwait(false); }
        finally { _lifecycleGate.Release(); }
    }

    /// <summary>确保子进程在跑（调用方必须已持有 _lifecycleGate）。
    /// 已有存活子进程 / 管道已连接 → 直接返回；否则确保 listener 就绪后启动。
    /// allowAdoptWait=true 时先等 2s 观察是否有孤儿子进程重连（模式刚开启 / 插件刚启动场景）；
    /// 主动重启时传 false —— 旧进程刚被杀，不存在可 adopt 的实例，白等只会拖慢重启。</summary>
    private async Task EnsureChildUnderGateAsync(bool allowAdoptWait = true)
    {
        if (!ShouldUseIndependent()) return;
        if (_pipeConnected) return;
        var alive = _childProcess;
        if (alive != null)
        {
            bool exited = true;
            try { exited = alive.HasExited; } catch { }
            if (!exited) return;
            // 【防僵尸·句柄】已退出：释放 Process 句柄并解除事件订阅后清引用 ——
            //   长时间运行 + 反复启停时若不释放，Process（含内部等待句柄）会持续累积。
            _childProcess = null;
            try { alive.Exited -= ChildExited; } catch { }
            try { alive.Dispose(); } catch { }
        }
        SetStatus(ChildStatus.Starting);

        // 管道监听单例 + 就绪握手（必须早于 spawn，否则子进程首次连接必然失败）
        EnsureAcceptLoopRunning();
        await WaitListenerReadyAsync(2000).ConfigureAwait(false);

        // adopt：仅当存在"同名但未被本服务跟踪"的进程时才等待其重连 ——
        //   典型来源：跟随启停=关 时，上一代宿主退出后冻结存活的子进程（它正每 1s 重连，等新宿主接管）。
        //   子进程重连周期最坏 = 连接超时 3s + 退避 1s = 4s，故给 5s 窗口；
        //   无残留时直接跳过等待，避免给插件首次启动平白增加延迟。
        if (allowAdoptWait && HasOrphanChildProcess())
        {
            _logger.LogInformation("FloatSchedule 检测到未连接的子进程实例，等待其重连（adopt）");
            for (int i = 0; i < 50 && !_pipeConnected; i++)
                await Task.Delay(100).ConfigureAwait(false);
            if (_pipeConnected)
            {
                _logger.LogInformation("FloatSchedule 已接管上一代子进程（adopt 成功）");
                return;
            }
        }

        // 【防僵尸·残留】到这里仍无连接：磁盘上存在的同名进程必是异常残留
        //   （正常孤儿会在上面的 2s 窗口内重连上），清掉再启动 —— 否则新实例会被残留实例的
        //   单实例 Mutex 挡回（退出码 42 往返），或与残留实例并存形成"僵尸悬浮窗"。
        KillOrphanChildren();

        try
        {
            var exePath = ResolveChildExePath();
            if (exePath == null)
            {
                SetStatus(ChildStatus.Failed, "子进程文件 AdvancedTimeIslandFloatSchedule.exe 不存在（插件包不完整？）");
                return;
            }

            var (connected, exitCode) = await SpawnAndAwaitConnectAsync(exePath).ConfigureAwait(false);

            // 单实例命中（42）：存在无法重连上的残留实例（孤儿）→ 按名清理后重试一次
            if (!connected && exitCode == FloatScheduleIpc.ExitCodeAnotherInstance)
            {
                _logger.LogInformation("FloatSchedule 检测到未连接的残留子进程实例（退出码 42），清理后重试一次");
                KillOrphanChildren();
                await Task.Delay(300).ConfigureAwait(false);
                (connected, exitCode) = await SpawnAndAwaitConnectAsync(exePath).ConfigureAwait(false);
            }

            if (!connected)
            {
                string reason = exitCode switch
                {
                    FloatScheduleIpc.ExitCodeMissingRuntime => "ClassIsland 安装目录缺少 Avalonia/Skia 运行库",
                    FloatScheduleIpc.ExitCodeAnotherInstance => "清理残留实例后仍无法建立连接",
                    null => "子进程启动后未连接（可能被安全软件拦截）",
                    _ => $"子进程退出，退出码 {exitCode}",
                };
                SetStatus(ChildStatus.Failed, reason);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FloatSchedule 子进程启动异常");
            SetStatus(ChildStatus.Failed, "子进程启动异常: " + ex.Message);
        }
    }

    /// <summary>等待管道 listener 真正开始监听（轮询就绪信号；超时即放弃，由子进程重连兜底）。
    /// 目的：消除"子进程已启动但 listener 尚未建好"→ 首次连接失败 → 子进程退避重试 → 表现为启动慢/反复。
    /// listener 每次（重）建时会把就绪信号重置为未完成，故重建期间等待方会正确继续等。</summary>
    private async Task WaitListenerReadyAsync(int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            var tcs = _listenerReadyTcs;
            if (tcs == null) { await Task.Delay(50).ConfigureAwait(false); continue; }
            if (tcs.Task.IsCompletedSuccessfully) return;
            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    /// <summary>启动一个子进程并等待其管道连接。返回（是否连接，退出码）。
    /// 注意：这里不做限频记账 —— 记账只用于"意外退出"限频（见 ChildExited），
    /// 若每次 spawn 都记账，用户连点几次"重启"就会把配额用尽而误熔断到 Failed。</summary>
    private async Task<(bool connected, int? exitCode)> SpawnAndAwaitConnectAsync(string exePath)
    {
        // "停止意图"清零放在真正 spawn 之前：保证此前的主动停止意图覆盖到 spawn 为止
        _stopRequested = false;

        // 记录本次运行副本的"版本指纹"：插件更新后（exe 变化）旧子进程据此自我识别并退出，
        //   避免跟随启停=关 时旧版本子进程被 adopt 后继续接管新插件。
        ChildExeStamp = ComputeExeStamp(exePath) ?? "";

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = BuildChildArguments(),
        };
        var p = Process.Start(psi);
        if (p == null) return (false, null);
        _childProcess = p;
        p.EnableRaisingEvents = true;
        p.Exited += ChildExited;

        // 【防僵尸·内核级保障】跟随启停=开 → 绑定 Job Object：宿主进程无论正常退出、崩溃
        //   还是被任务管理器强杀，内核都会在宿主结束时终止该子进程，杜绝"僵尸悬浮窗"。
        //   跟随启停=关 时不绑定（该设置下子进程本应脱离宿主继续冻结显示，绑定会导致其被误杀）。
        //   注意：绑定关系在进程创建后无法解除，因此该设置变更时上层会重启子进程
        //   （见 FloatingScheduleService.OnSettingsPropertyChanged 的 FollowHostLifetime 分支），
        //   否则"开→关"后冻结存活语义会失效。
        if (_settings.FloatingScheduleFollowHostLifetime)
        {
            try
            {
                if (!FloatScheduleJobObject.TryAssign(p))
                    _logger.LogDebug("FloatSchedule 子进程未加入 Job Object（降级为子进程侧看门狗保障）");
            }
            catch (Exception ex) { _logger.LogDebug(ex, "FloatSchedule Job Object 绑定异常（忽略）"); }
        }

        // 等待管道连接（冷启动首次 JIT Avalonia 可能较慢，给 20s）
        for (int i = 0; i < ReadyTimeoutMs / 100 && !_pipeConnected; i++)
            await Task.Delay(100).ConfigureAwait(false);
        if (_pipeConnected) return (true, null);
        return (false, TryGetExitCode(p));
    }

    // 残留进程探测结果缓存：HasOrphanChildProcess 扫描一次后把命中 PID 记下，
    //  供随后的 KillOrphanChildren 直接使用 —— 避免在启动路径上对系统做两次全量进程枚举（每次含逐进程句柄打开）。
    private List<int> _detectedOrphanPids = new();
    private bool _orphanScanDone;

    private static bool IsChildProcessName(string name) =>
        string.Equals(name, "AdvancedTimeIslandFloatSchedule", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("ati_", StringComparison.OrdinalIgnoreCase);

    /// <summary>是否存在"同名但未被本服务跟踪"的子进程（上一代冻结存活的子进程 / 异常残留）。
    /// 用于决定是否需要进入 adopt 等待：没有残留就无需等待，可直接启动。同时记录 PID 供后续清理复用。</summary>
    private bool HasOrphanChildProcess()
    {
        _detectedOrphanPids = new List<int>();
        _orphanScanDone = true;
        try
        {
            var current = _childProcess;
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (IsChildProcessName(p.ProcessName))
                    {
                        if (current != null)
                        {
                            try { if (p.Id == current.Id) continue; } catch { }
                        }
                        _detectedOrphanPids.Add(p.Id);
                    }
                }
                catch { }
                finally { try { p.Dispose(); } catch { } }
            }
        }
        catch { }
        return _detectedOrphanPids.Count > 0;
    }

    /// <summary>清理残留子进程（不含当前跟踪进程）。
    /// 优先复用 HasOrphanChildProcess 的扫描结果；未探测过时才退化为按名全量扫描。</summary>
    private void KillOrphanChildren()
    {
        List<int>? pids = null;
        if (_orphanScanDone)
        {
            pids = _detectedOrphanPids;
            _detectedOrphanPids = new List<int>();
            _orphanScanDone = false;
        }

        try
        {
            if (pids != null)
            {
                foreach (var pid in pids)
                {
                    try
                    {
                        using var p = Process.GetProcessById(pid);   // 已退出会抛异常 → 忽略
                        p.Kill(entireProcessTree: true);
                        p.WaitForExit(2000);
                    }
                    catch { }
                }
                return;
            }

            var current = _childProcess;
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (IsChildProcessName(p.ProcessName))
                    {
                        if (current != null)
                        {
                            try { if (p.Id == current.Id) continue; } catch { }
                        }
                        try { p.Kill(entireProcessTree: true); p.WaitForExit(2000); } catch { }
                    }
                }
                catch { }
                finally { try { p.Dispose(); } catch { } }
            }
        }
        catch { }
    }

    /// <summary>强制重启子进程（设置页按钮 / 随机进程名切换）。
    /// 请求合并：已有重启在执行/排队时直接返回（重启是幂等操作，重复执行只会浪费）。
    /// 关键区别：这里能安全合并，是因为整段 Stop→Start 都在 _lifecycleGate 内串行执行 ——
    /// 正确性由闸门保证，而不是靠"拒绝请求"回避并发。</summary>
    public async Task RestartChildAsync()
    {
        if (Interlocked.CompareExchange(ref _restartInProgressInt, 1, 0) != 0) return;
        try
        {
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await StopChildUnderGateAsync(kill: true).ConfigureAwait(false);
                SetStatus(ChildStatus.Off);
                lock (_stateLock) { _startTicks.Clear(); }   // 用户主动重启 = 显式期望恢复，重置意外退出配额
                await EnsureChildUnderGateAsync(allowAdoptWait: false).ConfigureAwait(false);
            }
            finally { _lifecycleGate.Release(); }
        }
        finally { Interlocked.Exchange(ref _restartInProgressInt, 0); }
    }

    /// <summary>是否正在执行"强制重启"（设置页据此临时禁用重启按钮）。</summary>
    public bool IsRestarting => Volatile.Read(ref _restartInProgressInt) != 0;

    /// <summary>当前运行副本 exe 的"版本指纹"（长度-最后写入时间）。下发给子进程用于自我版本校验。</summary>
    public string ChildExeStamp { get; private set; } = "";

    private static string? ComputeExeStamp(string exePath)
    {
        try
        {
            var fi = new FileInfo(exePath);
            return fi.Exists ? $"{fi.Length}-{fi.LastWriteTimeUtc.Ticks}" : null;
        }
        catch { return null; }
    }

    /// <summary>独立模式开关关闭：终止子进程并回到 Off（进程内渲染由 FloatingScheduleService 恢复）。</summary>
    public void NotifyModeDisabled()
    {
        lock (_stateLock) { _startTicks.Clear(); }
        SetStatus(ChildStatus.Off);
        _ = Task.Run(async () =>
        {
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try { await StopChildUnderGateAsync(kill: true).ConfigureAwait(false); }
            finally { _lifecycleGate.Release(); }
        });
    }

    /// <summary>停止子进程（调用方必须已持有 _lifecycleGate）。</summary>
    private async Task StopChildUnderGateAsync(bool kill)
    {
        _stopRequested = kill;
        if (_pipeConnected)
        {
            try { WriteLine(FloatScheduleIpc.Encode(FloatScheduleIpcMsgType.Shutdown, null)); } catch { }
            // 给子进程 1.5s 优雅退出窗口
            var p = _childProcess;
            if (p != null && !p.HasExited)
            {
                try { await p.WaitForExitAsync(new CancellationTokenSource(1500).Token); } catch { }
            }
        }
        ClosePipe();
        var proc = _childProcess;
        _childProcess = null;
        if (proc != null)
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                    // 【治本】必须确认旧进程真正退出后再返回：
                    //   否则新进程可能因旧进程尚未释放"单实例 Mutex"而立刻以退出码 42 结束，
                    //   进而走"清理残留 + 重试"往返 —— 表现为重启缓慢、甚至反复启停。
                    using var killCts = new CancellationTokenSource(2000);
                    try { await proc.WaitForExitAsync(killCts.Token).ConfigureAwait(false); } catch { }
                }
                else
                {
                    try { await proc.WaitForExitAsync(new CancellationTokenSource(2000).Token).ConfigureAwait(false); } catch { }
                }
            }
            catch { }
            try { proc.Dispose(); } catch { }
        }
    }

    private void ChildExited(object? sender, EventArgs e)
    {
        // 只处理"当前代"子进程的退出：重启会替换 _childProcess，旧进程的 Exited 由 ThreadPool 异步投递，
        //  若不校验会把新进程误当"意外退出"而重复触发自动重启。
        if (!ReferenceEquals(sender, _childProcess)) return;
        if (_stopRequested) return;
        if (!ShouldUseIndependent()) return;
        var p = _childProcess;
        int? code = p != null ? TryGetExitCode(p) : null;
        if (code == FloatScheduleIpc.ExitCodeAnotherInstance) return;   // adopt 路径在 EnsureChildUnderGateAsync 处理

        // 意外退出 → 经闸门限频自动重启（3 次/30s，超限 Failed 回退进程内）
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000).ConfigureAwait(false);
            if (_stopRequested || !ShouldUseIndependent()) return;
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (_stateLock)
                {
                    _startTicks.Enqueue(Environment.TickCount64);
                    TrimStartTicks();
                    if (_startTicks.Count > MaxStartAttempts)
                    {
                        SetStatus(ChildStatus.Failed, "子进程反复异常退出，已暂停自动重启");
                        return;
                    }
                }
                await EnsureChildUnderGateAsync(allowAdoptWait: false).ConfigureAwait(false);
            }
            finally { _lifecycleGate.Release(); }
        });
    }

    private int? TryGetExitCode(Process p)
    {
        try { return p.HasExited ? p.ExitCode : (int?)null; }
        catch { return null; }
    }

    private void TrimStartTicks()
    {
        var now = Environment.TickCount64;
        while (_startTicks.Count > 0 && now - _startTicks.Peek() > (long)StartAttemptWindow.TotalMilliseconds)
            _startTicks.Dequeue();
    }

    private string BuildChildArguments()
    {
        var hostDir = "";
        try { hostDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ""; } catch { }
        var sb = new StringBuilder();
        sb.Append("--host-dir \"").Append(hostDir).Append('"');
        sb.Append(" --pipe ").Append(FloatScheduleIpc.PipeName);
        sb.Append(" --parent-pid ").Append(Environment.ProcessId);
        sb.Append(_settings.FloatingScheduleFollowHostLifetime ? " --follow-lifetime 1" : " --follow-lifetime 0");
        sb.Append(_settings.FloatingScheduleSingleInstanceProtection ? " --single-instance 1" : " --single-instance 0");
        return sb.ToString();
    }

    /// <summary>
    /// 解析用于启动的子进程 exe 路径。
    /// 【必须从临时目录副本运行 —— 否则会锁死插件目录导致"更新插件"损坏】
    ///   若直接运行插件目录下的 exe，Windows 会锁定该文件；而"跟随启停=关"时子进程会在
    ///   ClassIsland 退出后继续存活，此时更新插件（宿主删除/替换 Plugins\{id} 目录）会因文件
    ///   被占用而失败或只删掉一部分 → 插件目录严重损坏。
    ///   故无论是否开启"随机进程名"，一律把 exe 复制到 %TEMP%\AdvancedTimeIslandFloatSchedule\ 下运行：
    ///     - 随机进程名=关：副本沿用原名（任务管理器进程名仍是 AdvancedTimeIslandFloatSchedule）
    ///     - 随机进程名=开：副本随机命名（进程名随机，进一步规避按进程名拦截）
    ///   副本不占用插件目录，插件可被正常更新；旧副本在每次启动前尽量清理。
    /// </summary>
    private string? ResolveChildExePath()
    {
        try
        {
            var srcDir = Path.GetDirectoryName(typeof(FloatScheduleHostProcessService).Assembly.Location);
            if (string.IsNullOrEmpty(srcDir)) return null;
            var srcExe = Path.Combine(srcDir, "AdvancedTimeIslandFloatSchedule.exe");
            if (!File.Exists(srcExe)) return null;

            var runDir = Path.Combine(Path.GetTempPath(), "AdvancedTimeIslandFloatSchedule");
            try { Directory.CreateDirectory(runDir); } catch { }
            CleanupOldExeCopies(runDir);

            var name = _settings.FloatingScheduleRandomProcessName
                ? "ati_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant() + ".exe"
                : "AdvancedTimeIslandFloatSchedule.exe";
            var dst = Path.Combine(runDir, name);
            try { if (File.Exists(dst)) File.Delete(dst); } catch { /* 仍被旧子进程占用 → 交由 File.Copy 覆盖判定 */ }
            File.Copy(srcExe, dst, overwrite: true);
            return dst;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ResolveChildExePath 异常");
            return null;
        }
    }

    /// <summary>清理运行目录内的旧 exe 副本（被旧子进程占用的跳过；spawn 前已尽量清理孤儿进程）。</summary>
    private void CleanupOldExeCopies(string runDir)
    {
        try
        {
            foreach (var f in Directory.GetFiles(runDir, "*.exe"))
            {
                try { File.Delete(f); } catch { /* 占用中 → 跳过 */ }
            }
        }
        catch { }
    }

    // ===================== 管道服务器 =====================

    // 【诊断修复 5】accept 循环单例：原先仅凭 `_pipeServer == null` 判定，而循环在断开后的
    //   500ms 重建延迟期间 `_pipeServer` 已为 null → 期间任何 EnsureChildAsync 都会再起一个循环，
    //   两个循环用同名管道（maxNumberOfServerInstances=1）互相争抢 → 连接时通时断 → 状态反复跳变。
    private int _acceptLoopRunning;
    private void EnsureAcceptLoopRunning()
    {
        var cts = _cts;
        if (cts == null || cts.IsCancellationRequested) return;
        // 先挂一个"未完成"信号：保证调用方即使早于循环启动也能正确等待（循环顶部会重置为新的未完成信号）
        if (_listenerReadyTcs == null)
            _listenerReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _acceptLoopRunning, 1, 0) != 0) return;
        _ = Task.Run(async () =>
        {
            try { await PipeAcceptLoopAsync(cts.Token).ConfigureAwait(false); }
            finally { Interlocked.Exchange(ref _acceptLoopRunning, 0); }
        });
    }

    private async Task PipeAcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // 新一轮监听：重置就绪信号（等待方据此正确等待本次重建完成）
            _listenerReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            NamedPipeServerStream? server = null;
            try
            {
                server = new NamedPipeServerStream(
                    FloatScheduleIpc.PipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                _pipeServer = server;
                _listenerReadyTcs?.TrySetResult(true);   // 握手：已开始监听，可安全 spawn 子进程
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

                var reader = new StreamReader(server, Encoding.UTF8);
                StreamWriter writer;
                lock (_writeLock)
                {
                    writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = false };
                    _writer = writer;
                }
                _pipeConnected = true;
                SetStatus(ChildStatus.Connected);

                // Init：一次性补发缓存（子进程渲染首帧 + 应用行为快照）
                FloatScheduleWindowSettings? settings;
                FloatScheduleRenderModel? model;
                bool visible;
                lock (_stateLock) { settings = _cachedSettings; model = _cachedModel; visible = _cachedVisible; }
                WriteLine(FloatScheduleIpc.Encode(FloatScheduleIpcMsgType.Init,
                    new FloatScheduleInitPayload { Settings = settings, Model = model }));
                if (!visible)
                    WriteLine(FloatScheduleIpc.Encode(FloatScheduleIpcMsgType.Visible, new FloatScheduleVisiblePayload { Visible = false }));

                try { Connected?.Invoke(); } catch (Exception ex) { _logger.LogDebug(ex, "Connected 事件回调异常（忽略）"); }

                // 读循环：Ready / Position
                string? line;
                while (!ct.IsCancellationRequested && (line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
                {
                    HandleChildLine(line);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "FloatSchedule 管道 accept 循环异常（0.5s 后重建 listener）");
            }
            finally
            {
                MarkDisconnected();
                try { server?.Dispose(); } catch { }
                if (_pipeServer == server) _pipeServer = null;
            }
            if (!ct.IsCancellationRequested)
            {
                // listener 重建间隔：缩短到 150ms（配合就绪握手，让重启/重连更快恢复）
                try { await Task.Delay(150, ct).ConfigureAwait(false); } catch { break; }
            }
        }
    }

    private void HandleChildLine(string line)
    {
        if (!FloatScheduleIpc.TryDecode(line, out var type, out var data)) return;
        switch (type)
        {
            case FloatScheduleIpcMsgType.Ready:
                var ready = FloatScheduleIpc.Deserialize<FloatScheduleReadyPayload>(data);
                _childPid = ready?.Pid ?? -1;
                _logger.LogDebug("FloatSchedule 子进程 Ready: {Pid}", _childPid);
                break;
            case FloatScheduleIpcMsgType.Position:
                var pos = FloatScheduleIpc.Deserialize<FloatSchedulePositionPayload>(data);
                if (pos != null)
                {
                    try { PositionReported?.Invoke(pos.X, pos.Y); }
                    catch (Exception ex) { _logger.LogDebug(ex, "PositionReported 回调异常（忽略）"); }
                }
                break;
            case FloatScheduleIpcMsgType.Error:
                var err = FloatScheduleIpc.Deserialize<FloatScheduleErrorPayload>(data);
                _logger.LogWarning("FloatSchedule 子进程错误: code={Code} msg={Msg}", err?.Code ?? -1, err?.Message ?? "?");
                break;
            case FloatScheduleIpcMsgType.ExitRequested:
                // 用户从子进程托盘图标选择"退出"：这是主动退出，绝不能被当作"意外退出"自动重启
                //（否则用户会看到悬浮窗关不掉）。先抑制自动重启，再通知上层关闭相关设置。
                _logger.LogInformation("FloatSchedule 用户通过托盘图标请求退出悬浮课表");
                _stopRequested = true;
                try { ExitRequestedByUser?.Invoke(); }
                catch (Exception ex) { _logger.LogDebug(ex, "ExitRequestedByUser 回调异常（忽略）"); }
                break;
        }
    }

    private void MarkDisconnected()
    {
        if (!_pipeConnected) return;
        _pipeConnected = false;
        lock (_writeLock)
        {
            try { _writer?.Dispose(); } catch { }
            _writer = null;
        }
        // 【诊断修复 1】主动停止/重启路径（_stopRequested=true）不改状态：
        //   此时断开是插件自己杀进程造成的，状态由调用方（RestartChildAsync/NotifyModeDisabled）负责设置，
        //   否则会出现"已连接 → 冻结中 → 启动中 → 已连接"的状态抖动。
        if (_stopRequested) return;
        if (Status != ChildStatus.Connected) return;
        // 【诊断修复 2】不再置 Frozen：Frozen 的语义是"宿主退出、子进程冻结显示"，而插件自己就是宿主，
        //   子进程断开只会是崩溃/被杀，它会 1s 重连（或插件重启它）→ 统一置 Starting 等待恢复。
        //   Frozen 仅由 StopAsync（插件正常退出且跟随启停=关）设置。
        SetStatus(ChildStatus.Starting);
    }

    private void ClosePipe()
    {
        _pipeConnected = false;
        lock (_writeLock)
        {
            try { _writer?.Dispose(); } catch { }
            _writer = null;
        }
        try { _pipeServer?.Dispose(); } catch { }
        _pipeServer = null;
    }
}
