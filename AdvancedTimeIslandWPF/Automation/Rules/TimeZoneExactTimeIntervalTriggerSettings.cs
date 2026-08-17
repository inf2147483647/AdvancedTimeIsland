namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 区时精确时间范围间隔触发设置
/// 格式：YYYY-MM-DD-hh-mm-ss
/// </summary>
public class TimeZoneExactTimeIntervalTriggerSettings
{
    /// <summary>
    /// 开始时间 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// 触发间隔值
    /// </summary>
    public decimal Interval { get; set; } = 1m;

    /// <summary>
    /// 间隔单位：Second, Minute, Hour, Day, Week, Month, Year
    /// </summary>
    public string IntervalUnit { get; set; } = "Minute";

    /// <summary>
    /// 时区ID
    /// </summary>
    public string TimeZoneId { get; set; } = "China Standard Time";
}
