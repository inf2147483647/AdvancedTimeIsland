using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.local_solar_yearly_time_interval", "地方时每年时间范围间隔触发", "\uecde")]
public class LocalSolarYearlyTimeIntervalTrigger : TimeTriggerBase<LocalSolarYearlyTimeIntervalTriggerSettings>
{
    protected override DateTime GetCurrentTime()
    {
        return Plugin.GetLocalSolarTime(ExactTimeService.GetCurrentLocalDateTime(), Settings.Longitude);
    }

    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime) || string.IsNullOrEmpty(Settings.EndTime))
            return false;

        if (!IntervalTriggerHelper.TryParseYearlyTime(Settings.StartTime, now.Year, out var startTimeThisYear) ||
            !IntervalTriggerHelper.TryParseYearlyTime(Settings.EndTime, now.Year, out var endTimeThisYear))
            return false;

        DateTime startTime, endTime;

        if (startTimeThisYear > endTimeThisYear)
        {
            if (now >= startTimeThisYear)
            {
                startTime = startTimeThisYear;
                endTime = endTimeThisYear.AddYears(1);
            }
            else
            {
                startTime = startTimeThisYear.AddYears(-1);
                endTime = endTimeThisYear;
            }
        }
        else
        {
            startTime = startTimeThisYear;
            endTime = endTimeThisYear;
        }

        return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
    }
}
