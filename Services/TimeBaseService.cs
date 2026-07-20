using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Shared;
using ClassIsland.Core.Abstractions.Services;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 同步状态
/// </summary>
public enum SyncStatus
{
    Syncing,
    Success,
    Failed
}

/// <summary>
/// 同步状态事件参数
/// </summary>
public class SyncStatusEventArgs : EventArgs
{
    public SyncStatus Status { get; set; }
    public DateTime? SyncTime { get; set; }
    public string? SyncSource { get; set; }
}

/// <summary>
/// 时间基准管理服务
/// 基于NTP服务器时间并应用插件独立的时间偏移
/// </summary>
public class TimeBaseService
{
    public static TimeBaseService? Instance { get; private set; }

    /// <summary>
    /// 同步状态变化事件
    /// </summary>
    public event EventHandler<SyncStatusEventArgs>? SyncStatusChanged;

    private readonly PluginSettings _settings;
    private DateTime _lastNtpSyncTime = DateTime.MinValue;
    private TimeSpan _timeOffset = TimeSpan.Zero;
    private System.Timers.Timer? _syncTimer;
    private readonly object _lockObj = new object();

    // 系统时间跳跃检测
    private DateTime _lastSystemTimeForCheck = DateTime.MinValue;
    private System.Timers.Timer? _timeJumpCheckTimer;

    // 连续失败追踪
    private int _consecutiveFailures = 0;
    private const int MaxConsecutiveFailuresBeforeFallback = 24 * 60; // 约24小时（按5分钟一次计算需要288次）
    private bool _hasValidSync = false;
    private DateTime _lastSuccessfulSyncTime = DateTime.MinValue;

    // 检测时间跳跃的阈值（秒）
    private const int TimeJumpThresholdSeconds = 2;

    private static bool? _is32BitProcess;

    private static bool Is32BitProcess
    {
        get
        {
            if (_is32BitProcess.HasValue)
                return _is32BitProcess.Value;
            _is32BitProcess = IntPtr.Size == 4;
            return _is32BitProcess.Value;
        }
    }

    private IExactTimeService? ExactTimeService => GlobalConstants.HostInterfaces.ExactTimeService;

    public TimeBaseService(PluginSettings settings)
    {
        _settings = settings;
        Instance = this;
        _settings.PropertyChanged += OnSettingsChanged;

        try
        {
            StartTimeJumpCheckTimer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TimeBaseService] 启动时间跳跃检测定时器失败: {ex.Message}");
        }

        try
        {
            StartSyncTimer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TimeBaseService] 启动定时同步定时器失败: {ex.Message}");
        }

        try
        {
            _ = SyncTimeAsync(isStartup: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TimeBaseService] 启动时同步时间失败: {ex.Message}");
        }
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.NtpSyncIntervalMinutes) ||
            e.PropertyName == nameof(PluginSettings.NtpServer))
        {
            StartSyncTimer();
            _ = SyncTimeAsync(isStartup: false);
        }
    }

    /// <summary>
    /// 启动时间跳跃检测定时器
    /// </summary>
    private void StartTimeJumpCheckTimer()
    {
        _timeJumpCheckTimer?.Dispose();
        // 每秒检测一次系统时间是否发生跳跃
        _timeJumpCheckTimer = new System.Timers.Timer(1000);
        _timeJumpCheckTimer.Elapsed += OnTimeJumpCheck;
        _timeJumpCheckTimer.Start();
    }

    private void OnTimeJumpCheck(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var currentSystemTime = DateTime.Now;

        lock (_lockObj)
        {
            if (_lastSystemTimeForCheck != DateTime.MinValue)
            {
                // 计算预期的时间差（正常情况下应该是1秒）
                var expectedDiff = 1.0;
                var actualDiff = (currentSystemTime - _lastSystemTimeForCheck).TotalSeconds;

                // 检测时间跳跃：如果实际时间差与预期差值超过阈值
                if (Math.Abs(actualDiff - expectedDiff) > TimeJumpThresholdSeconds)
                {
                    // 检测到系统时间被修改，立即尝试同步
                    System.Diagnostics.Debug.WriteLine($"[TimeBaseService] 检测到系统时间跳跃: {actualDiff}秒，触发立即同步");
                    _ = SyncTimeAsync(isTimeJumpRecovery: true);
                }
            }

            _lastSystemTimeForCheck = currentSystemTime;
        }
    }

    private void StartSyncTimer()
    {
        lock (_lockObj)
        {
            _syncTimer?.Dispose();
            var interval = Math.Max(1, _settings.NtpSyncIntervalMinutes) * 60 * 1000;
            _syncTimer = new System.Timers.Timer(interval);
            _syncTimer.Elapsed += async (s, e) => await SyncTimeAsync(isStartup: false);
            _syncTimer.Start();
        }
    }

    private async Task SyncTimeAsync(bool isStartup = false, bool isTimeJumpRecovery = false, CancellationToken cancellationToken = default)
    {
        try
        {
            OnSyncStatusChanged(SyncStatus.Syncing, null);

            DateTime syncTime;
            string syncSource;

            if (Is32BitProcess)
            {
                syncTime = await SyncFromClassIslandAsync(cancellationToken);
                syncSource = "ClassIsland";
            }
            else
            {
                try
                {
                    syncTime = await GetNtpTimeAsync(_settings.NtpServer, cancellationToken);
                    syncSource = _settings.NtpServer;
                }
                catch (Exception ntpEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[TimeBaseService] NTP同步失败，尝试从ClassIsland同步: {ntpEx.Message}");
                    syncTime = await SyncFromClassIslandAsync(cancellationToken);
                    syncSource = "ClassIsland";
                }
            }

            lock (_lockObj)
            {
                var classIslandTime = GetClassIslandTime();
                _lastNtpSyncTime = classIslandTime;
                _timeOffset = syncTime - classIslandTime;
                _hasValidSync = true;
                _consecutiveFailures = 0;
                _lastSuccessfulSyncTime = syncTime;
            }

            OnSyncStatusChanged(SyncStatus.Success, syncTime, syncSource);
        }
        catch (OperationCanceledException)
        {
            OnSyncStatusChanged(SyncStatus.Failed, null);
            System.Diagnostics.Debug.WriteLine("[TimeBaseService] 同步被取消");
        }
        catch (Exception ex)
        {
            lock (_lockObj)
            {
                _consecutiveFailures++;
            }

            try
            {
                await SyncFromClassIslandAsync(cancellationToken);
                OnSyncStatusChanged(SyncStatus.Success, GetClassIslandTime(), "ClassIsland");
                return;
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeBaseService] 回退到ClassIsland同步也失败: {fallbackEx.Message}");
            }

            OnSyncStatusChanged(SyncStatus.Failed, null);

            System.Diagnostics.Debug.WriteLine($"[TimeBaseService] 同步失败 #{_consecutiveFailures}: {ex.Message}");
        }
    }

    private async Task<DateTime> SyncFromClassIslandAsync(CancellationToken cancellationToken)
    {
        var exactTimeService = ExactTimeService;
        if (exactTimeService == null)
        {
            throw new Exception("ClassIsland时间服务不可用");
        }

        await Task.Run(() =>
        {
            exactTimeService.Sync();
        }, cancellationToken);

        var syncTime = exactTimeService.GetCurrentLocalDateTime();
        
        lock (_lockObj)
        {
            _timeOffset = syncTime - syncTime;
            _hasValidSync = true;
            _consecutiveFailures = 0;
            _lastSuccessfulSyncTime = syncTime;
        }

        return syncTime;
    }

    private DateTime GetClassIslandTime()
    {
        var exactTimeService = ExactTimeService;
        if (exactTimeService != null)
        {
            try
            {
                return exactTimeService.GetCurrentLocalDateTime();
            }
            catch
            {
                // ignore
            }
        }
        return DateTime.Now;
    }

    /// <summary>
    /// 手动立即同步时间
    /// </summary>
    /// <param name="timeout">超时时间，默认10秒</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SyncTimeNowAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);
        
        try
        {
            await SyncTimeAsync(isStartup: false, cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[TimeBaseService] 同步超时 ({effectiveTimeout.TotalSeconds}秒)");
        }
    }

    private void OnSyncStatusChanged(SyncStatus status, DateTime? syncTime, string? syncSource = null)
    {
        SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs { Status = status, SyncTime = syncTime, SyncSource = syncSource });

        if (status == SyncStatus.Success && syncTime.HasValue)
        {
            _settings.LastSyncTime = syncTime.Value;
            _settings.LastSyncStatus = "Success";
            _settings.LastSyncSource = syncSource;
        }
        else if (status == SyncStatus.Failed)
        {
            _settings.LastSyncTime = GetClassIslandTime();
            _settings.LastSyncStatus = "Failed";
            _settings.LastSyncSource = null;
        }
    }

    private async Task<DateTime> GetNtpTimeAsync(string server, CancellationToken cancellationToken = default)
    {
        const int ntpPort = 123;
        const int timeout = 5000;

        using var udpClient = new UdpClient(AddressFamily.InterNetwork);
        udpClient.Client.ReceiveTimeout = timeout;

        var ntpData = new byte[48];
        ntpData[0] = 0x1B;

        var addresses = await Dns.GetHostAddressesAsync(server);
        var ipv4Addr = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) 
            ?? throw new Exception($"无法解析 {server} 的IPv4地址");
        
        await udpClient.SendAsync(ntpData, ntpData.Length, new IPEndPoint(ipv4Addr, ntpPort));
        
        var receiveTask = udpClient.ReceiveAsync();
        var completedTask = await Task.WhenAny(receiveTask, Task.Delay(timeout, cancellationToken));
        
        if (completedTask != receiveTask)
        {
            throw new OperationCanceledException("NTP接收超时");
        }
        
        var result = receiveTask.Result;

        var data = result.Buffer;
        ulong intPart = (ulong)data[40] << 24 | (ulong)data[41] << 16 | (ulong)data[42] << 8 | data[43];
        ulong fractPart = (ulong)data[44] << 24 | (ulong)data[45] << 16 | (ulong)data[46] << 8 | data[47];

        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        var ntpTime = new DateTime(1900, 1, 1).AddMilliseconds(milliseconds);

        return ntpTime.ToLocalTime();
    }

    /// <summary>
    /// 获取当前时间（NTP时间 + 插件时间偏移）
    /// </summary>
    /// <returns>当前时间</returns>
    public DateTime GetCurrentTime()
    {
        try
        {
            bool useFallback;
            DateTime baseTime;

            var classIslandTime = GetClassIslandTime();

            lock (_lockObj)
            {
                useFallback = !_hasValidSync || _consecutiveFailures >= MaxConsecutiveFailuresBeforeFallback;

                if (_lastNtpSyncTime != DateTime.MinValue)
                {
                    baseTime = classIslandTime.Add(_timeOffset);
                }
                else
                {
                    baseTime = classIslandTime;
                }
            }

            if (useFallback)
            {
                var offset = TimeSpan.FromSeconds(_settings.TimeOffsetSeconds);
                return classIslandTime.Add(offset);
            }

            var pluginOffset = TimeSpan.FromSeconds(_settings.TimeOffsetSeconds);
            return baseTime.Add(pluginOffset);
        }
        catch
        {
            return GetClassIslandTime();
        }
    }

    /// <summary>
    /// 获取原始服务器时间（不加插件偏移）
    /// </summary>
    /// <returns>原始服务器时间</returns>
    public DateTime GetRawServerTime()
    {
        try
        {
            bool useFallback = false;

            var classIslandTime = GetClassIslandTime();

            lock (_lockObj)
            {
                if (!_hasValidSync || _consecutiveFailures >= MaxConsecutiveFailuresBeforeFallback)
                {
                    useFallback = true;
                }
            }

            if (useFallback)
            {
                return classIslandTime;
            }

            DateTime baseTime;
            lock (_lockObj)
            {
                if (_lastNtpSyncTime != DateTime.MinValue)
                {
                    baseTime = classIslandTime.Add(_timeOffset);
                }
                else
                {
                    baseTime = classIslandTime;
                }
            }
            return baseTime;
        }
        catch
        {
            return GetClassIslandTime();
        }
    }

    /// <summary>
    /// 获取系统时间 + 插件偏移（不使用NTP）
    /// </summary>
    /// <returns>系统时间 + 插件偏移</returns>
    public DateTime GetPluginOffsetSystemTime()
    {
        var offset = TimeSpan.FromSeconds(_settings.TimeOffsetSeconds);
        return GetClassIslandTime().Add(offset);
    }

    /// <summary>
    /// 异步获取当前时间
    /// </summary>
    /// <returns>当前时间</returns>
    public async Task<DateTime> GetCurrentTimeAsync()
    {
        return await Task.Run(() => GetCurrentTime()).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步获取原始服务器时间
    /// </summary>
    /// <returns>原始服务器时间</returns>
    public async Task<DateTime> GetRawServerTimeAsync()
    {
        return await Task.Run(() => GetRawServerTime()).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步获取系统时间 + 插件偏移
    /// </summary>
    /// <returns>系统时间 + 插件偏移</returns>
    public async Task<DateTime> GetPluginOffsetSystemTimeAsync()
    {
        return await Task.Run(() => GetPluginOffsetSystemTime()).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取精确时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    /// <returns>精确时间字符串</returns>
    public string GetExactTimeString()
    {
        return GetCurrentTime().ToString("yyyy-MM-dd-HH-mm-ss");
    }

    /// <summary>
    /// 异步获取精确时间字符串
    /// </summary>
    /// <returns>精确时间字符串</returns>
    public async Task<string> GetExactTimeStringAsync()
    {
        var time = await GetCurrentTimeAsync().ConfigureAwait(false);
        return time.ToString("yyyy-MM-dd-HH-mm-ss");
    }

    /// <summary>
    /// 获取当前 Unix 时间戳（秒）
    /// </summary>
    /// <returns>Unix 时间戳</returns>
    public long GetCurrentUnixTimestamp()
    {
        return Helpers.UnixTimeHelper.ToUnixTimestamp(GetCurrentTime());
    }

    /// <summary>
    /// 异步获取当前 Unix 时间戳（秒）
    /// </summary>
    /// <returns>Unix 时间戳</returns>
    public async Task<long> GetCurrentUnixTimestampAsync()
    {
        var time = await GetCurrentTimeAsync().ConfigureAwait(false);
        return Helpers.UnixTimeHelper.ToUnixTimestamp(time);
    }

    /// <summary>
    /// 获取同步状态信息
    /// </summary>
    public SyncInfo GetSyncInfo()
    {
        lock (_lockObj)
        {
            return new SyncInfo
            {
                HasValidSync = _hasValidSync,
                ConsecutiveFailures = _consecutiveFailures,
                LastSuccessfulSyncTime = _lastSuccessfulSyncTime,
                IsUsingFallback = !_hasValidSync || _consecutiveFailures >= MaxConsecutiveFailuresBeforeFallback
            };
        }
    }
}

/// <summary>
/// 同步状态信息
/// </summary>
public class SyncInfo
{
    public bool HasValidSync { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime LastSuccessfulSyncTime { get; set; }
    public bool IsUsingFallback { get; set; }
}



