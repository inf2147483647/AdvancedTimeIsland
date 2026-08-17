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

    public async Task StartAsync(CancellationToken cancellationToken)
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