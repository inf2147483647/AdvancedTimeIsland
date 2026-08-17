namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 地方时每年时间范围规则设置（带经度）
/// </summary>
public class LocalSolarYearlyTimeRangeRuleSettings : YearlyTimeRangeRuleSettings
{
    /// <summary>
    /// 经度（-180到180）
    /// </summary>
    public double Longitude { get; set; } = 120.0;
}



