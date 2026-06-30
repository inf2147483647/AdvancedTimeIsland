using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.monthly_time", "每月", "\uecd2")]
public class MonthlyTimeTrigger : TimeTriggerBase<MonthlyTimeRangeRuleSettings>
{
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
