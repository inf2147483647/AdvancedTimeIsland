namespace AdvancedTimeIsland.Automation.Rules;

public class WeeklyTimeRangeRuleSettings
{
    public int StartDayOfWeek { get; set; } = 0;
    public int EndDayOfWeek { get; set; } = 0;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
