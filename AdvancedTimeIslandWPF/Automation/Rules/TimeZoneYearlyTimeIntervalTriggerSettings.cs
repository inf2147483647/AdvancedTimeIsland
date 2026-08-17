namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 区时每年时间范围间隔触发设置
/// 格式：MM-DD-hh-mm-ss
/// </summary>
public class TimeZoneYearlyTimeIntervalTriggerSettings
{
    /// <summary>
    /// 开始时间 (MM-DD-hh-mm-ss)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 (MM-DD-hh-mm-ss)
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
