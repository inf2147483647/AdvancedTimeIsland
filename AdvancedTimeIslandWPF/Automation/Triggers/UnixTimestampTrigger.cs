using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.unix_timestamp", "绝对时间", PackIconKind.Counter)]
public class UnixTimestampTrigger : TimeTriggerBase<UnixTimestampTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        var currentTimestamp = UnixTimeHelper.ToUnixTimestamp(now);
        return currentTimestamp == Settings.TargetTimestamp;
    }
}
