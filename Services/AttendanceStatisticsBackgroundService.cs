using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 在校统计后台计算服务：插件初始化后在后台线程完成一次整学期统计并缓存，
/// 之后设置页与组件在未强制刷新时都只读取 <see cref="AttendanceCalendarService.CachedStatistics"/>，
/// 避免每次进入页面都在 UI 线程同步扫描整学期课表。
/// </summary>
public class AttendanceStatisticsBackgroundService : IHostedService
{
    /// <summary>
    /// 启动阶段的重试间隔：ClassIsland 宿主初始化需要时间，过早计算拿不到学期开始日。
    /// 仅在拿不到统计结果（如学期开始日尚未就绪）时继续重试，拿到结果即停止。
    /// </summary>
    private static readonly TimeSpan[] StartupRetryDelays =
    {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20)
    };

    private readonly AttendanceCalendarService _calendar;
    private readonly SharedRenderClockService _clock;
    private readonly ILogger<AttendanceStatisticsBackgroundService>? _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly IDisposable _clockSubscription;
    private DateTime _lastKnownDate = DateTime.MinValue;

    public AttendanceStatisticsBackgroundService(AttendanceCalendarService calendar,
        SharedRenderClockService clock, ILogger<AttendanceStatisticsBackgroundService>? logger)
    {
        _calendar = calendar;
        _clock = clock;
        _logger = logger;

        // 跨天后统计结果需要更新：跟随插件共用的渲染时钟监听日期变化，不额外占用定时器。
        _clockSubscription = _clock.Subscribe(OnClockTick);
        _clock.EnsureStarted();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AttendanceCalendarService.DataChanged += OnCalendarChanged;
        SemesterStartService.SemesterStartDateChanged += OnCalendarChanged;
        _ = ComputeOnceAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AttendanceCalendarService.DataChanged -= OnCalendarChanged;
        SemesterStartService.SemesterStartDateChanged -= OnCalendarChanged;
        _clockSubscription.Dispose();
        _cts.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>数据或统计口径变化：作废缓存并在后台重新统计。</summary>
    private void OnCalendarChanged(object? sender, EventArgs e)
    {
        _calendar.InvalidateStatistics();
        _ = _calendar.GetStatisticsAsync(DateTime.Now);
    }

    /// <summary>跨天时在后台重算一次（缓存按日期失效）。</summary>
    private void OnClockTick(DateTime now)
    {
        var date = now.Date;
        if (date == _lastKnownDate)
        {
            return;
        }

        _lastKnownDate = date;
        _ = _calendar.GetStatisticsAsync(now);
    }

    /// <summary>初始化后统计一次；仅在拿不到结果时按递增间隔重试。</summary>
    private async Task ComputeOnceAsync(CancellationToken token)
    {
        try
        {
            foreach (var delay in StartupRetryDelays)
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
                var stats = await _calendar.GetStatisticsAsync(DateTime.Now).ConfigureAwait(false);
                if (stats != null)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 插件停止，正常退出。
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "在校统计初始化计算失败");
        }
    }
}
