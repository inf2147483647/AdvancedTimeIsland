namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 区时精确时间范围规则设置（带时区）
/// </summary>
public class TimeZoneExactTimeRuleSettings : ExactTimeRangeRuleSettings
{
    /// <summary>
    /// 时区ID
    /// </summary>
    public string TimeZoneId { get; set; } = "China Standard Time";
}



