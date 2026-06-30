namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历每月范围规则设置
/// 格式：[L_DD-hh-mm-ss]₁ ~ [L_DD-hh-mm-ss]₂
/// </summary>
public class LunarMonthlyTimeRangeRuleSettings
{
    public int StartDay { get; set; } = 0;
    public string StartTime { get; set; } = string.Empty;

    public int EndDay { get; set; } = 0;
    public string EndTime { get; set; } = string.Empty;
}
