namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历每月时间范围间隔触发设置
/// </summary>
public class LunarMonthlyTimeIntervalTriggerSettings
{
    public int StartDay { get; set; } = 0;
    public string StartTime { get; set; } = string.Empty;

    public int EndDay { get; set; } = 0;
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// 触发间隔值
    /// </summary>
    public decimal Interval { get; set; } = 1m;

    /// <summary>
    /// 间隔单位：Second, Minute, Hour, Day, Week, Month, Year
    /// </summary>
    public string IntervalUnit { get; set; } = "Minute";
}
