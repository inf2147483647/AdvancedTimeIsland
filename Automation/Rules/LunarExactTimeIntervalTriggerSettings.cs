namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历精确时间范围间隔触发设置
/// </summary>
public class LunarExactTimeIntervalTriggerSettings
{
    public int StartLunarYear { get; set; } = 0;
    public int StartLunarMonth { get; set; } = 0;
    public bool StartIsLeapMonth { get; set; } = false;
    public int StartLunarDay { get; set; } = 0;
    public string StartTargetTime { get; set; } = string.Empty;

    public int EndLunarYear { get; set; } = 0;
    public int EndLunarMonth { get; set; } = 0;
    public bool EndIsLeapMonth { get; set; } = false;
    public int EndLunarDay { get; set; } = 0;
    public string EndTargetTime { get; set; } = string.Empty;

    /// <summary>
    /// 触发间隔值
    /// </summary>
    public decimal Interval { get; set; } = 1m;

    /// <summary>
    /// 间隔单位：Second, Minute, Hour, Day, Week, Month, Year
    /// </summary>
    public string IntervalUnit { get; set; } = "Minute";
}
