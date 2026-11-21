using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.sunrise_time", "日出时", "\uE706")]
public class SunriseTimeTrigger : SunEventTriggerBase
{
    protected override bool IsSunrise => true;
}
