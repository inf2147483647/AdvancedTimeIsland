namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 地方时精确时间范围规则设置（带经度）
/// </summary>
public class LocalSolarExactTimeRuleSettings : ExactTimeRangeRuleSettings
{
    /// <summary>
    /// 经度（-180到180）
    /// </summary>
    public double Longitude { get; set; } = 120.0;
}



