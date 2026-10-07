using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每月时间范围规则设置
/// 格式：DD-hh-mm-ss
/// </summary>
public class MonthlyTimeRangeRuleSettings
{
    /// <summary>开始时间-日 (01-31)。</summary>
    public string StartTimeDay { get; set; } = string.Empty;

    /// <summary>开始时间-时 (00-23)。</summary>
    public string StartTimeHour { get; set; } = string.Empty;

    /// <summary>开始时间-分 (00-59)。</summary>
    public string StartTimeMinute { get; set; } = string.Empty;

    /// <summary>开始时间-秒 (00-59)。</summary>
    public string StartTimeSecond { get; set; } = string.Empty;

    /// <summary>结束时间-日 (01-31)。</summary>
    public string EndTimeDay { get; set; } = string.Empty;

    /// <summary>结束时间-时 (00-23)。</summary>
    public string EndTimeHour { get; set; } = string.Empty;

    /// <summary>结束时间-分 (00-59)。</summary>
    public string EndTimeMinute { get; set; } = string.Empty;

    /// <summary>结束时间-秒 (00-59)。</summary>
    public string EndTimeSecond { get; set; } = string.Empty;

    /// <summary>开始时间 (DD-hh-mm-ss)，由分量拼出；任一分量为空则为空（未设置）。</summary>
    public string StartTime
    {
        get => TimePartsHelper.Build(StartTimeDay, StartTimeHour, StartTimeMinute, StartTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 4);
            StartTimeDay = parts[0];
            StartTimeHour = parts[1];
            StartTimeMinute = parts[2];
            StartTimeSecond = parts[3];
        }
    }

    /// <summary>结束时间 (DD-hh-mm-ss)，由分量拼出。</summary>
    public string EndTime
    {
        get => TimePartsHelper.Build(EndTimeDay, EndTimeHour, EndTimeMinute, EndTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 4);
            EndTimeDay = parts[0];
            EndTimeHour = parts[1];
            EndTimeMinute = parts[2];
            EndTimeSecond = parts[3];
        }
    }
}
