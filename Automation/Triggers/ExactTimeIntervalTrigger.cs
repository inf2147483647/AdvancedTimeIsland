using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.exact_time_interval", "精确时间范围间隔触发", "\uecd7")]
public class ExactTimeIntervalTrigger : TimeTriggerBase<ExactTimeIntervalTriggerSettings>
{
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
