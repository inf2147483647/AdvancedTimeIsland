using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.timezone_exact_time", "区时精确时间", "\uece2")]
public class TimeZoneExactTimeTrigger : TimeTriggerBase<TimeZoneExactTimeRuleSettings>
{
    protected override DateTime GetCurrentTime()
    {
        var localTime = base.GetCurrentTime();
        return Plugin.GetTimeZoneTime(localTime, Settings.TimeZoneId);
    }

    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime))
            return false;

        var parts = Settings.StartTime.Split('-');
        if (parts.Length < 6) return false;

        if (!int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day) ||
            !int.TryParse(parts[3], out int hour) ||
            !int.TryParse(parts[4], out int minute) ||
            !int.TryParse(parts[5], out int second))
            return false;

        return now.Year == year &&
               now.Month == month &&
               now.Day == day &&
               now.Hour == hour &&
               now.Minute == minute &&
               now.Second == second;
    }
}
