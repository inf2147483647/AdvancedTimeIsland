using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.lunar_last_day_time", "农历年倒数第n天", "\uecdb")]
public class LunarLastDayTimeTrigger : TimeTriggerBase<LunarLastDayRuleSettings>
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
            var lunarYear = LunarCalendarHelper.GetLunarYear(now);
            var lunarMonth = LunarCalendarHelper.GetLunarMonth(now);

            if (lunarMonth != Settings.LunarMonth)
                return false;

            var daysInLunarMonth = LunarCalendarHelper.GetDaysInLunarMonth(lunarYear, Settings.LunarMonth);
            var targetLunarDay = daysInLunarMonth - Settings.DaysFromEnd + 1;

            if (targetLunarDay < 1)
                targetLunarDay = 1;

            var lunarDay = LunarCalendarHelper.GetLunarDay(now);
            if (lunarDay != targetLunarDay)
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
