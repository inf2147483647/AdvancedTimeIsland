namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历年倒数第n天规则设置
/// 格式：L_MM-n-hh-mm-ss
/// 在每农历年(L_MM)的倒数第n天(hh-mm-ss)触发
/// </summary>
public class LunarLastDayRuleSettings
{
    public int LunarMonth { get; set; } = 0;
    public int DaysFromEnd { get; set; } = 1;
    public string TargetTime { get; set; } = string.Empty;
}
