namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 「当前处于白昼/黑夜」条件的设置。
/// 属性名沿用宿主原「是否日出/日落」条件的 <c>IsSunset</c>，以兼容已有自动化配置。
/// </summary>
public class SunDayNightRuleSettings
{
    /// <summary>true 表示判断黑夜（日落到次日日出），false 表示判断白昼（日出到日落）。</summary>
    public bool IsSunset { get; set; }
}
