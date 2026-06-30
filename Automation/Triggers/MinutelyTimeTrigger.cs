using System;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.minutely_time", "每分钟", "\uecd6")]
public class MinutelyTimeTrigger : TimeTriggerBase<MinutelyTimeRangeRuleSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartSecond))
            return false;

        if (!int.TryParse(Settings.StartSecond, out int second))
            return false;

        return now.Second == second;
    }
}
