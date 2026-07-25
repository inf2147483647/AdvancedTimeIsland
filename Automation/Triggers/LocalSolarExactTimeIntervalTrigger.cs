using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.local_solar_exact_time_interval", "地方时精确时间范围间隔触发", "\uecdd")]
public class LocalSolarExactTimeIntervalTrigger : TimeTriggerBase<LocalSolarExactTimeIntervalTriggerSettings>
{
    protected override DateTime GetCurrentTime()
    {
        return Plugin.GetLocalSolarTime(ExactTimeService.GetCurrentLocalDateTime(), Settings.Longitude);
    }

    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime) || string.IsNullOrEmpty(Settings.EndTime))
            return false;

        if (!IntervalTriggerHelper.TryParseExactTime(Settings.StartTime, out var startTime) ||
            !IntervalTriggerHelper.TryParseExactTime(Settings.EndTime, out var endTime))
            return false;

        return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
    }
}
