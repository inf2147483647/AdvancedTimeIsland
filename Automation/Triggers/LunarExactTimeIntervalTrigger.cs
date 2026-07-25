using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.lunar_exact_time_interval", "农历精确时间范围间隔触发", "\uece8")]
public class LunarExactTimeIntervalTrigger : TimeTriggerBase<LunarExactTimeIntervalTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (Settings.StartLunarYear <= 0 || Settings.StartLunarMonth <= 0 || Settings.StartLunarDay <= 0 ||
            Settings.EndLunarYear <= 0 || Settings.EndLunarMonth <= 0 || Settings.EndLunarDay <= 0 ||
            string.IsNullOrWhiteSpace(Settings.StartTargetTime) || string.IsNullOrWhiteSpace(Settings.EndTargetTime))
            return false;

        var startParts = Settings.StartTargetTime.Split('-');
        var endParts = Settings.EndTargetTime.Split('-');
        if (startParts.Length < 3 || endParts.Length < 3) return false;

        if (!int.TryParse(startParts[0], out int startHour) ||
            !int.TryParse(startParts[1], out int startMinute) ||
            !int.TryParse(startParts[2], out int startSecond))
            return false;

        if (!int.TryParse(endParts[0], out int endHour) ||
            !int.TryParse(endParts[1], out int endMinute) ||
            !int.TryParse(endParts[2], out int endSecond))
            return false;

        var startTime = LunarCalendarHelper.LunarToSolar(
            Settings.StartLunarYear, Settings.StartLunarMonth, Settings.StartIsLeapMonth, Settings.StartLunarDay,
            startHour, startMinute, startSecond);

        var endTime = LunarCalendarHelper.LunarToSolar(
            Settings.EndLunarYear, Settings.EndLunarMonth, Settings.EndIsLeapMonth, Settings.EndLunarDay,
            endHour, endMinute, endSecond);

        if (!startTime.HasValue || !endTime.HasValue)
            return false;

        return IntervalTriggerHelper.CheckIntervalTrigger(now, startTime.Value, endTime.Value, Settings.Interval, Settings.IntervalUnit);
    }
}
