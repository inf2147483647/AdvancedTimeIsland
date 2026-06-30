using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.timezone_hourly_time", "区时每小时", "\uece7")]
public class TimeZoneHourlyTimeTrigger : TimeTriggerBase<TimeZoneHourlyTimeRangeRuleSettings>
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
        if (timeParts.Length < 2) return false;

        if (!int.TryParse(timeParts[0], out int minute) ||
            !int.TryParse(timeParts[1], out int second))
            return false;

        return now.Minute == minute &&
               now.Second == second;
    }
}
