using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.sunset_time", "日落时", "\uE708")]
public class SunsetTimeTrigger : SunEventTriggerBase
{
    protected override bool IsSunrise => false;
}
