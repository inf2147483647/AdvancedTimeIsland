using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.timezone_weekly_time", "区时每周", "\uece5")]
public class TimeZoneWeeklyTimeTrigger : TimeTriggerBase<TimeZoneWeeklyTimeRangeRuleSettings>
{
    protected override DateTime GetCurrentTime()
    {
        var localTime = base.GetCurrentTime();
        return Plugin.GetTimeZoneTime(localTime, Settings.TimeZone);
    }

    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime))
            return false;

        var timeParts = Settings.StartTime.Split('-');
        if (timeParts.Length < 3) return false;

        if (!int.TryParse(timeParts[0], out int hour) ||
            !int.TryParse(timeParts[1], out int minute) ||
            !int.TryParse(timeParts[2], out int second))
            return false;

        return (int)now.DayOfWeek == Settings.StartDayOfWeek &&
               now.Hour == hour &&
               now.Minute == minute &&
               now.Second == second;
    }
}
