using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.local_solar_minutely_time", "地方时每分钟", "\uece1")]
public class LocalSolarMinutelyTimeTrigger : TimeTriggerBase<LocalSolarMinutelyTimeRangeRuleSettings>
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
        if (string.IsNullOrEmpty(Settings.StartSecond))
            return false;

        if (!int.TryParse(Settings.StartSecond, out int second))
            return false;

        return now.Second == second;
    }
}
