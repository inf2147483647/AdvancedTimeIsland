namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每农历年规则设置
/// 格式：L_MM-L_DD-hh-mm-ss
/// </summary>
public class LunarYearlyRuleSettings
{
    public int LunarMonth { get; set; } = 0;
    public int LunarDay { get; set; } = 0;
    public string TargetTime { get; set; } = string.Empty;
}
