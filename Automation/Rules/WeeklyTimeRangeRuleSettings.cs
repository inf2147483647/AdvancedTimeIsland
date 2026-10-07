using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每周时间范围规则设置
/// 格式：hh-mm-ss（星期另由 Start/EndDayOfWeek 表示）
/// </summary>
public class WeeklyTimeRangeRuleSettings
{
    public int StartDayOfWeek { get; set; } = 0;
    public int EndDayOfWeek { get; set; } = 0;

    /// <summary>开始时间-时 (00-23)。</summary>
    public string StartTimeHour { get; set; } = string.Empty;

    /// <summary>开始时间-分 (00-59)。</summary>
    public string StartTimeMinute { get; set; } = string.Empty;

    /// <summary>开始时间-秒 (00-59)。</summary>
    public string StartTimeSecond { get; set; } = string.Empty;

    /// <summary>结束时间-时 (00-23)。</summary>
    public string EndTimeHour { get; set; } = string.Empty;

    /// <summary>结束时间-分 (00-59)。</summary>
    public string EndTimeMinute { get; set; } = string.Empty;

    /// <summary>结束时间-秒 (00-59)。</summary>
    public string EndTimeSecond { get; set; } = string.Empty;

    /// <summary>开始时间 (hh-mm-ss)，由分量拼出；任一分量为空则为空（未设置）。</summary>
    public string StartTime
    {
        get => TimePartsHelper.Build(StartTimeHour, StartTimeMinute, StartTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 3);
            StartTimeHour = parts[0];
            StartTimeMinute = parts[1];
            StartTimeSecond = parts[2];
        }
    }

    /// <summary>结束时间 (hh-mm-ss)，由分量拼出。</summary>
    public string EndTime
    {
        get => TimePartsHelper.Build(EndTimeHour, EndTimeMinute, EndTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 3);
            EndTimeHour = parts[0];
            EndTimeMinute = parts[1];
            EndTimeSecond = parts[2];
        }
    }
}
