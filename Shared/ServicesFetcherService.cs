using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedTimeIsland.Shared;

public class ServicesFetcherService : IHostedService
{
    public ServicesFetcherService(IExactTimeService exactTimeService, ILogger<Plugin> logger)
    {
        GlobalConstants.HostInterfaces.ExactTimeService = exactTimeService;
        GlobalConstants.HostInterfaces.PluginLogger = logger;
        logger.Log(LogLevel.Information, "AdvancedTimeIsland services fetched!");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
