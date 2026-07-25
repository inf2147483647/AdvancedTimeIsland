using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.lunar_yearly_time_interval", "农历每年时间范围间隔触发", "\uece9")]
public class LunarYearlyTimeIntervalTrigger : TimeTriggerBase<LunarYearlyTimeIntervalTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (Settings.StartMonth <= 0 || Settings.StartDay <= 0 ||
            Settings.EndMonth <= 0 || Settings.EndDay <= 0 ||
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

            var startTimeThisYear = LunarCalendarHelper.LunarToSolar(
                currentLunarYear, Settings.StartMonth, Settings.StartIsLeapMonth, Settings.StartDay,
                startHour, startMinute, startSecond);

            var endTimeThisYear = LunarCalendarHelper.LunarToSolar(
                currentLunarYear, Settings.EndMonth, Settings.EndIsLeapMonth, Settings.EndDay,
                endHour, endMinute, endSecond);

            if (!startTimeThisYear.HasValue || !endTimeThisYear.HasValue)
                return false;

            DateTime startTime, endTime;

            if (startTimeThisYear > endTimeThisYear)
            {
                if (now >= startTimeThisYear)
                {
                    startTime = startTimeThisYear.Value;
                    var endTimeNextYear = LunarCalendarHelper.LunarToSolar(
                        currentLunarYear + 1, Settings.EndMonth, Settings.EndIsLeapMonth, Settings.EndDay,
                        endHour, endMinute, endSecond);
                    endTime = endTimeNextYear ?? endTimeThisYear.Value.AddYears(1);
                }
                else
                {
                    var startTimeLastYear = LunarCalendarHelper.LunarToSolar(
                        currentLunarYear - 1, Settings.StartMonth, Settings.StartIsLeapMonth, Settings.StartDay,
                        startHour, startMinute, startSecond);
                    startTime = startTimeLastYear ?? startTimeThisYear.Value.AddYears(-1);
                    endTime = endTimeThisYear.Value;
                }
            }
            else
            {
                startTime = startTimeThisYear.Value;
                endTime = endTimeThisYear.Value;
            }

            return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime, endTime, Settings.Interval, Settings.IntervalUnit);
        }
        catch
        {
            return false;
        }
    }
}
