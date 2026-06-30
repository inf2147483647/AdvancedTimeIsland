using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.lunar_monthly_time", "农历每月", "\uecda")]
public class LunarMonthlyTimeTrigger : TimeTriggerBase<LunarMonthlyRuleSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrWhiteSpace(Settings.TargetTime))
            return false;

        var timeParts = Settings.TargetTime.Split('-');
        if (timeParts.Length < 3)
            return false;

        if (!int.TryParse(timeParts[0], out int hour) ||
            !int.TryParse(timeParts[1], out int minute) ||
            !int.TryParse(timeParts[2], out int second))
            return false;

        try
        {
            var lunarDay = LunarCalendarHelper.GetLunarDay(now);

            if (lunarDay != Settings.LunarDay)
                return false;

            return now.Hour == hour &&
                   now.Minute == minute &&
                   now.Second == second;
        }
        catch
        {
            return false;
        }
    }
}
