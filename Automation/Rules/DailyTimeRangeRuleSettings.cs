namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每天时间范围规则设置
/// 格式：hh-mm-ss
/// </summary>
public class DailyTimeRangeRuleSettings
{
    /// <summary>
    /// 开始时间 (hh-mm-ss)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 (hh-mm-ss)
    /// </summary>
    public string EndTime { get; set; } = string.Empty;
}
