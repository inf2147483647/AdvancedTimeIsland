namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 绝对时间戳范围规则设置
/// 单位为秒，支持小数，精确到3位小数，需64位有符号浮点数
/// </summary>
public class UnixTimestampRangeRuleSettings
{
    public double StartTimestamp { get; set; } = 0;

    public double EndTimestamp { get; set; } = 0;
}
