using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.lunar_monthly_time_interval", "农历每月时间范围间隔触发", "\uecea")]
public class LunarMonthlyTimeIntervalTrigger : TimeTriggerBase<LunarMonthlyTimeIntervalTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (Settings.StartDay <= 0 || Settings.EndDay <= 0 ||
            string.IsNullOrWhiteSpace(Settings.StartTime) || string.IsNullOrWhiteSpace(Settings.EndTime))
            return false;

        var startParts = Settings.StartTime.Split('-');
        var endParts = Settings.EndTime.Split('-');
        if (startParts.Length < 3 || endParts.Length < 3) return false;

        if (!int.TryParse(startParts[0], out int startHour) ||
            !int.TryParse(startParts[1], out int startMinute) ||
            !int.TryParse(startParts[2], out int startSecond))
            return false;

        if (!int.TryParse(endParts[0], out int endHour) ||
            !int.TryParse(endParts[1], out int endMinute) ||
            !int.TryParse(endParts[2], out int endSecond))
            return false;

        try
        {
            var currentLunarYear = LunarCalendarHelper.GetLunarYear(now);
            var currentLunarMonth = LunarCalendarHelper.GetLunarMonth(now);
            var isLeapMonth = LunarCalendarHelper.IsLeapMonth(now);

            var startTimeThisMonth = LunarCalendarHelper.LunarToSolar(
                currentLunarYear, currentLunarMonth, isLeapMonth, Settings.StartDay,
                startHour, startMinute, startSecond);

            var endTimeThisMonth = LunarCalendarHelper.LunarToSolar(
                currentLunarYear, currentLunarMonth, isLeapMonth, Settings.EndDay,
                endHour, endMinute, endSecond);

            if (!startTimeThisMonth.HasValue || !endTimeThisMonth.HasValue)
                return false;

            DateTime startTime, endTime;

            if (startTimeThisMonth > endTimeThisMonth)
            {
                if (now >= startTimeThisMonth)
                {
                    startTime = startTimeThisMonth.Value;
                    endTime = endTimeThisMonth.Value.AddMonths(1);
                }
                else
                {
                    startTime = startTimeThisMonth.Value.AddMonths(-1);
                    endTime = endTimeThisMonth.Value;
                }
            }
            else
            {
                startTime = startTimeThisMonth.Value;
                endTime = endTimeThisMonth.Value;
            }

            return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
        }
        catch
        {
            return false;
        }
    }
}
