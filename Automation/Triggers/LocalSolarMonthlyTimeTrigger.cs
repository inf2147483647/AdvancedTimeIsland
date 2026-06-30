using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.local_solar_monthly_time", "地方时每月", "\uecdd")]
public class LocalSolarMonthlyTimeTrigger : TimeTriggerBase<LocalSolarMonthlyTimeRangeRuleSettings>
{
    protected override DateTime GetCurrentTime()
    {
        var localTime = base.GetCurrentTime();
        var standardMeridian = TimeZoneInfo.Local.BaseUtcOffset.TotalHours * 15;
        var timeDifferenceMinutes = (Settings.Longitude - standardMeridian) * 4;
        return localTime.AddMinutes(timeDifferenceMinutes);
    }

    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime))
            return false;

        var timeParts = Settings.StartTime.Split('-');
        if (timeParts.Length < 4) return false;

        if (!int.TryParse(timeParts[0], out int day) ||
            !int.TryParse(timeParts[1], out int hour) ||
            !int.TryParse(timeParts[2], out int minute) ||
            !int.TryParse(timeParts[3], out int second))
            return false;

        return now.Day == day &&
               now.Hour == hour &&
               now.Minute == minute &&
               now.Second == second;
    }
}
