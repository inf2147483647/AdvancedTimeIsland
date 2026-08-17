namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 精确农历日期规则设置
/// 格式：L_YYYY-L_MM-L_DD-hh-mm-ss（考虑闰月）
/// </summary>
public class LunarExactTimeRuleSettings
{
    public int LunarYear { get; set; } = 0;
    public int LunarYearRangeEnd { get; set; } = 0;
    public int LunarMonth { get; set; } = 0;
    public bool IsLeapMonth { get; set; } = false;
    public int LunarDay { get; set; } = 0;
    public string TargetTime { get; set; } = string.Empty;
}
