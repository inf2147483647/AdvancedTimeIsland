using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.exact_time", "精确时间是", "\uecd0")]
public class ExactTimeTrigger : TimeTriggerBase<ExactTimeRuleSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.TargetTime))
            return false;

        var parts = Settings.TargetTime.Split('-');
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
