namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 区时每小时时间范围规则设置（带时区）
/// </summary>
public class TimeZoneHourlyTimeRangeRuleSettings : HourlyTimeRangeRuleSettings
{
    /// <summary>
    /// 时区ID
    /// </summary>
    public string TimeZoneId { get; set; } = "China Standard Time";
}



