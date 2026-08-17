namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 地方时每分钟时间范围间隔触发设置
/// 格式：ss
/// </summary>
public class LocalSolarMinutelyTimeIntervalTriggerSettings
{
    /// <summary>
    /// 开始秒
    /// </summary>
    public string StartSecond { get; set; } = string.Empty;

    /// <summary>
    /// 结束秒
    /// </summary>
    public string EndSecond { get; set; } = string.Empty;

    /// <summary>
    /// 触发间隔值
    /// </summary>
    public decimal Interval { get; set; } = 1m;

    /// <summary>
    /// 间隔单位：Second, Minute, Hour, Day, Week, Month, Year
    /// </summary>
    public string IntervalUnit { get; set; } = "Second";

    /// <summary>
    /// 经度
    /// </summary>
    public double Longitude { get; set; } = 116.4;
}
