using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.timezone_yearly_time", "区时每年", "\uece3")]
public class TimeZoneYearlyTimeTrigger : TimeTriggerBase<TimeZoneYearlyTimeRangeRuleSettings>
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

        var timeParts = Settings.StartTime.Split('-');
        if (timeParts.Length < 5) return false;

        if (!int.TryParse(timeParts[0], out int month) ||
            !int.TryParse(timeParts[1], out int day) ||
            !int.TryParse(timeParts[2], out int hour) ||
            !int.TryParse(timeParts[3], out int minute) ||
            !int.TryParse(timeParts[4], out int second))
            return false;

        return now.Month == month &&
               now.Day == day &&
               now.Hour == hour &&
               now.Minute == minute &&
               now.Second == second;
    }
}
