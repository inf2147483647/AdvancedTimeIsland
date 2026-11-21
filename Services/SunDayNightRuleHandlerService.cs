using System;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using Microsoft.Extensions.Hosting;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 接管宿主内置「是否日出/日落」条件（规则 Id 保持不变），
/// 确保本插件的「白昼/黑夜」处理器在宿主天气处理器之后执行，从而覆盖其结果。
/// </summary>
public class SunDayNightRuleHandlerService : IHostedService
{
    public const string RuleId = "classisland.weather.sunRiseSet";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 先强制宿主天气服务完成构造，让它把内置处理器挂到同名规则上。
        var weatherServiceType = Type.GetType(
            "ClassIsland.Core.Abstractions.Services.IWeatherService, ClassIsland.Core");
        if (weatherServiceType != null)
        {
            IAppHost.Host?.Services.GetService(weatherServiceType);
        }

        // 追加到处理器链末尾：委托调用返回最后一个处理器的结果，因此以本插件的结果为准。
        IAppHost.TryGetService<IRulesetService>()?.RegisterRuleHandler(RuleId, SunDayNightRuleEvaluator.IsSatisfied);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
