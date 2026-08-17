namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每年时间范围规则设置
/// 格式：MM-DD-hh-mm-ss
/// </summary>
public class YearlyTimeRangeRuleSettings
{
    /// <summary>
    /// 开始时间 (MM-DD-hh-mm-ss)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 (MM-DD-hh-mm-ss)
    /// </summary>
    public string EndTime { get; set; } = string.Empty;
}



