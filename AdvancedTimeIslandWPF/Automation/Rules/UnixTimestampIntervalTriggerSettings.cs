namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 绝对时间戳范围间隔触发设置
/// 单位为秒，支持小数，精确到3位小数
/// </summary>
public class UnixTimestampIntervalTriggerSettings
{
    /// <summary>
    /// 开始时间戳（单位：秒）
    /// </summary>
    public double StartTimestamp { get; set; } = 0;

    /// <summary>
    /// 结束时间戳（单位：秒）
    /// </summary>
    public double EndTimestamp { get; set; } = 0;

    /// <summary>
    /// 触发间隔时间戳（单位：秒）
    /// </summary>
    public double IntervalTimestamp { get; set; } = 60;
}
