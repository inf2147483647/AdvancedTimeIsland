using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.yearly_time", "每年", "\uecd1")]
public class YearlyTimeTrigger : TimeTriggerBase<YearlyTimeRangeRuleSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime))
            return false;

        var parts = Settings.StartTime.Split('-');
        if (parts.Length < 5) return false;

        if (!int.TryParse(parts[0], out int month) ||
            !int.TryParse(parts[1], out int day) ||
            !int.TryParse(parts[2], out int hour) ||
            !int.TryParse(parts[3], out int minute) ||
            !int.TryParse(parts[4], out int second))
            return false;

        return now.Month == month &&
               now.Day == day &&
               now.Hour == hour &&
               now.Minute == minute &&
               now.Second == second;
    }
}
