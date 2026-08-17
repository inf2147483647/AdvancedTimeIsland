namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历日期范围规则设置
/// 格式：L_YYYY-L_MM-L_DD-hh-mm-ss（考虑闰月）
/// </summary>
public class LunarTimeRangeRuleSettings
{
    public int LunarYear { get; set; } = 0;
    public int LunarYearRangeEnd { get; set; } = 0;
    public int LunarMonth { get; set; } = 0;
    public bool IsLeapMonth { get; set; } = false;
    public int LunarDay { get; set; } = 0;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
