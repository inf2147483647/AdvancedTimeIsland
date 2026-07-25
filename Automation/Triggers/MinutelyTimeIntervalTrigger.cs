using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.minutely_time_interval", "每分钟时间范围间隔触发", "\uecdc")]
public class MinutelyTimeIntervalTrigger : TimeTriggerBase<MinutelyTimeIntervalTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartSecond) || string.IsNullOrEmpty(Settings.EndSecond))
            return false;

        if (!int.TryParse(Settings.StartSecond, out int startSecond) ||
            !int.TryParse(Settings.EndSecond, out int endSecond))
            return false;

        var startTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, startSecond);
        var endTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, endSecond);

        DateTime startTime, endTime;

        if (startTimeThisMinute > endTimeThisMinute)
        {
            if (now >= startTimeThisMinute)
            {
                startTime = startTimeThisMinute;
                endTime = endTimeThisMinute.AddMinutes(1);
            }
            else
            {
                startTime = startTimeThisMinute.AddMinutes(-1);
                endTime = endTimeThisMinute;
            }
        }
        else
        {
            startTime = startTimeThisMinute;
            endTime = endTimeThisMinute;
        }

        return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
    }
}
