namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历每年范围规则设置
/// 格式：[L_MM-L_DD-hh-mm-ss]₁ ~ [L_MM-L_DD-hh-mm-ss]₂
/// </summary>
public class LunarYearlyTimeRangeRuleSettings
{
    public int StartMonth { get; set; } = 0;
    public int StartDay { get; set; } = 0;
    public bool StartIsLeapMonth { get; set; } = false;
    public string StartTime { get; set; } = string.Empty;

    public int EndMonth { get; set; } = 0;
    public int EndDay { get; set; } = 0;
    public bool EndIsLeapMonth { get; set; } = false;
    public string EndTime { get; set; } = string.Empty;
}
