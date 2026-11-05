using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.local_solar_weekly_time", "地方时每周", "\uE362")]
public class LocalSolarWeeklyTimeTrigger : TimeTriggerBase<LocalSolarWeeklyTimeRangeRuleSettings>
{
    protected override DateTime GetCurrentTime()
    {
        return Plugin.GetLocalSolarTime(base.GetCurrentTime(), Settings.Longitude);
    }

    protected override bool CheckTrigger(DateTime now)
    {
        if (string.IsNullOrEmpty(Settings.StartTime))
            return false;

        var timeParts = Settings.StartTime.Split('-');
        if (timeParts.Length < 3) return false;

        if (!int.TryParse(timeParts[0], out int hour) ||
            !int.TryParse(timeParts[1], out int minute) ||
            !int.TryParse(timeParts[2], out int second))
            return false;

        return WeekNumberHelper.GetMondayBasedDayIndex(now) == Settings.StartDayOfWeek &&
               now.Hour == hour &&
               now.Minute == minute &&
               now.Second == second;
    }
}
