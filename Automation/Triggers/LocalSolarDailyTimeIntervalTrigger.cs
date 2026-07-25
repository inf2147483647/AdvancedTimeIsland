using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.local_solar_daily_time_interval", "地方时每天时间范围间隔触发", "\uece0")]
public class LocalSolarDailyTimeIntervalTrigger : TimeTriggerBase<LocalSolarDailyTimeIntervalTriggerSettings>
{
    protected override DateTime GetCurrentTime()
    {
        return Plugin.GetLocalSolarTime(ExactTimeService.GetCurrentLocalDateTime(), Settings.Longitude);
    }

    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime) || string.IsNullOrEmpty(Settings.EndTime))
            return false;

        if (!IntervalTriggerHelper.TryParseDailyTime(Settings.StartTime, now.Year, now.Month, now.Day, out var startTimeToday) ||
            !IntervalTriggerHelper.TryParseDailyTime(Settings.EndTime, now.Year, now.Month, now.Day, out var endTimeToday))
            return false;

        DateTime startTime, endTime;

        if (startTimeToday > endTimeToday)
        {
            if (now >= startTimeToday)
            {
                startTime = startTimeToday;
                endTime = endTimeToday.AddDays(1);
            }
            else
            {
                startTime = startTimeToday.AddDays(-1);
                endTime = endTimeToday;
            }
        }
        else
        {
            startTime = startTimeToday;
            endTime = endTimeToday;
        }

        return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
    }
}
