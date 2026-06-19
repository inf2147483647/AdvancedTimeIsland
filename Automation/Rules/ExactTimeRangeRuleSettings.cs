namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 精确时间范围规则设置
/// </summary>
public class ExactTimeRangeRuleSettings
{
    /// <summary>
    /// 开始时间 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string EndTime { get; set; } = string.Empty;
}
