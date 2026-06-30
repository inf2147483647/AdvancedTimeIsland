namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历每月规则设置
/// 格式：L_DD-hh-mm-ss
/// 选"三十"则每大月触发，选"初一～廿九"则每月触发
/// </summary>
public class LunarMonthlyRuleSettings
{
    public int LunarDay { get; set; } = 0;
    public string TargetTime { get; set; } = string.Empty;
}
