using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.monthly_time_interval", "每月时间范围间隔触发", "\uecd9")]
public class MonthlyTimeIntervalTrigger : TimeTriggerBase<MonthlyTimeIntervalTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime) || string.IsNullOrEmpty(Settings.EndTime))
            return false;

        if (!IntervalTriggerHelper.TryParseMonthlyTime(Settings.StartTime, now.Year, now.Month, out var startTimeThisMonth) ||
            !IntervalTriggerHelper.TryParseMonthlyTime(Settings.EndTime, now.Year, now.Month, out var endTimeThisMonth))
            return false;

        DateTime startTime, endTime;

        if (startTimeThisMonth > endTimeThisMonth)
        {
            if (now >= startTimeThisMonth)
            {
                startTime = startTimeThisMonth;
                endTime = endTimeThisMonth.AddMonths(1);
            }
            else
            {
                startTime = startTimeThisMonth.AddMonths(-1);
                endTime = endTimeThisMonth;
            }
        }
        else
        {
            startTime = startTimeThisMonth;
            endTime = endTimeThisMonth;
        }

        return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
    }
}
