namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每小时时间范围规则设置
/// 格式：mm-ss
/// </summary>
public class HourlyTimeRangeRuleSettings
{
    /// <summary>
    /// 开始时间 (mm-ss)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 (mm-ss)
    /// </summary>
    public string EndTime { get; set; } = string.Empty;
}
