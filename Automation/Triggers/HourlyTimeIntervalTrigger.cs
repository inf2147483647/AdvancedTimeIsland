using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.hourly_time_interval", "每小时时间范围间隔触发", "\uecdb")]
public class HourlyTimeIntervalTrigger : TimeTriggerBase<HourlyTimeIntervalTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime) || string.IsNullOrEmpty(Settings.EndTime))
            return false;

        if (!IntervalTriggerHelper.TryParseHourlyTime(Settings.StartTime, now.Year, now.Month, now.Day, now.Hour, out var startTimeThisHour) ||
            !IntervalTriggerHelper.TryParseHourlyTime(Settings.EndTime, now.Year, now.Month, now.Day, now.Hour, out var endTimeThisHour))
            return false;

        DateTime startTime, endTime;

        if (startTimeThisHour > endTimeThisHour)
        {
            if (now >= startTimeThisHour)
            {
                startTime = startTimeThisHour;
                endTime = endTimeThisHour.AddHours(1);
            }
            else
            {
                startTime = startTimeThisHour.AddHours(-1);
                endTime = endTimeThisHour;
            }
        }
        else
        {
            startTime = startTimeThisHour;
            endTime = endTimeThisHour;
        }

        return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
    }
}
