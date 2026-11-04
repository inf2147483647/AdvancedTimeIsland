using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedTimeIsland.Shared;

public class ServicesFetcherService : IHostedService
{
    public ServicesFetcherService(IExactTimeService exactTimeService, ILogger<Plugin> logger, Services.TimeBaseService timeBaseService)
    {
        GlobalConstants.HostInterfaces.ExactTimeService = exactTimeService;
        GlobalConstants.HostInterfaces.PluginLogger = logger;
        logger.Log(LogLevel.Information, "AdvancedTimeIsland services fetched!");

        // 启动自检发现插件包与宿主 FA 大版本不匹配时，同步写入宿主日志便于排查
        if (Plugin.HostCompatibilityWarning is { Length: > 0 } compatibilityWarning)
        {
            logger.Log(LogLevel.Error, "插件包与宿主版本不匹配：{Warning}", compatibilityWarning);
        }
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
