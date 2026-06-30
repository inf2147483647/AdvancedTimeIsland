namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每月时间范围规则设置
/// 格式：DD-hh-mm-ss
/// </summary>
public class MonthlyTimeRangeRuleSettings
{
    /// <summary>
    /// 开始时间 (DD-hh-mm-ss)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 (DD-hh-mm-ss)
    /// </summary>
    public string EndTime { get; set; } = string.Empty;
}



