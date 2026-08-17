namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历每年时间范围间隔触发设置
/// </summary>
public class LunarYearlyTimeIntervalTriggerSettings
{
    public int StartMonth { get; set; } = 0;
    public int StartDay { get; set; } = 0;
    public bool StartIsLeapMonth { get; set; } = false;
    public string StartTime { get; set; } = string.Empty;

    public int EndMonth { get; set; } = 0;
    public int EndDay { get; set; } = 0;
    public bool EndIsLeapMonth { get; set; } = false;
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
