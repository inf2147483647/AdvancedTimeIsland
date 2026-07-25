using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.time_zone_exact_time_interval", "区时精确时间范围间隔触发", "\uece3")]
public class TimeZoneExactTimeIntervalTrigger : TimeTriggerBase<TimeZoneExactTimeIntervalTriggerSettings>
{
    protected override DateTime GetCurrentTime()
    {
        return Plugin.GetTimeZoneTime(ExactTimeService.GetCurrentLocalDateTime(), Settings.TimeZoneId);
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
