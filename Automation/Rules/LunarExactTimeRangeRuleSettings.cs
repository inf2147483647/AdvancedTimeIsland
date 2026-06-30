namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历精确时间范围规则设置
/// 格式：[L_YYYY-L_MM-L_DD-hh-mm-ss]₁ ~ [L_YYYY-L_MM-L_DD-hh-mm-ss]₂
/// </summary>
public class LunarExactTimeRangeRuleSettings
{
    public int StartLunarYear { get; set; } = 0;
    public int StartLunarYearRangeEnd { get; set; } = 0;
    public int StartLunarMonth { get; set; } = 0;
    public bool StartIsLeapMonth { get; set; } = false;
    public int StartLunarDay { get; set; } = 0;
    public string StartTargetTime { get; set; } = string.Empty;

    public int EndLunarYear { get; set; } = 0;
    public int EndLunarYearRangeEnd { get; set; } = 0;
    public int EndLunarMonth { get; set; } = 0;
    public bool EndIsLeapMonth { get; set; } = false;
    public int EndLunarDay { get; set; } = 0;
    public string EndTargetTime { get; set; } = string.Empty;
}
