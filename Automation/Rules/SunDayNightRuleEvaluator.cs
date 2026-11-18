using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Shared;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 判断当前处于白昼还是黑夜，时间取自主界面「日出日落」组件。
/// </summary>
public static class SunDayNightRuleEvaluator
{
    public static bool IsSatisfied(object? settings)
    {
        if (settings is not SunDayNightRuleSettings ruleSettings)
        {
            return false;
        }

        var now = GlobalConstants.HostInterfaces.ExactTimeService?.GetCurrentLocalDateTime() ?? DateTime.Now;
        var (sunrise, sunset) = SunriseSunsetProvider.Calculate(now);
        var isDaytime = SunriseSunsetProvider.IsDaytime(now.TimeOfDay, sunrise, sunset);

        return ruleSettings.IsSunset ? !isDaytime : isDaytime;
    }
}
