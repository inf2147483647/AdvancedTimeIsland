using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

public class StartupDelayService : IHostedService
{
    private readonly ILogger<StartupDelayService>? _logger;
    private readonly TimeBaseService? _timeBaseService;
    private readonly SharedRenderClockService? _sharedRenderClockService;

    public StartupDelayService(ILogger<StartupDelayService>? logger = null,
                               TimeBaseService? timeBaseService = null,
                               SharedRenderClockService? sharedRenderClockService = null)
    {
        _logger = logger;
        _timeBaseService = timeBaseService;
        _sharedRenderClockService = sharedRenderClockService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 【启动速度优化】原实现直接 await Task.Delay(1000)，而宿主 Host.StartAsync 是**顺序 await**
        //   每个 IHostedService 的 —— 这会让注册在其后的所有服务（含悬浮窗）至少晚 1 秒才启动。
        //   这里改为后台延迟执行：延迟本意（让时间服务晚于宿主 UI 就绪再启动，避免争抢资源）保持不变，
        //   但不再阻塞宿主启动链。
        _ = Task.Run(() => RunDelayedAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task RunDelayedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(1000, cancellationToken);

            if (_timeBaseService != null)
            {
                try
                {
                    _timeBaseService.StartTimers();
                    _ = _timeBaseService.SyncTimeNowAsync();
                    _logger?.LogInformation("TimeBaseService started");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to start TimeBaseService");
                }
            }

            if (_sharedRenderClockService != null)
            {
                try
                {
                    await _sharedRenderClockService.StartAsync(cancellationToken);
                    _logger?.LogInformation("SharedRenderClockService started");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to start SharedRenderClockService");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("StartupDelayService cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in StartupDelayService");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}