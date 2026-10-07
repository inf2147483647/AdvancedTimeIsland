using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每年时间范围规则设置
/// 格式：MM-DD-hh-mm-ss
/// </summary>
public class YearlyTimeRangeRuleSettings
{
    /// <summary>开始时间-月 (01-12)。</summary>
    public string StartTimeMonth { get; set; } = string.Empty;

    /// <summary>开始时间-日 (01-31)。</summary>
    public string StartTimeDay { get; set; } = string.Empty;

    /// <summary>开始时间-时 (00-23)。</summary>
    public string StartTimeHour { get; set; } = string.Empty;

    /// <summary>开始时间-分 (00-59)。</summary>
    public string StartTimeMinute { get; set; } = string.Empty;

    /// <summary>开始时间-秒 (00-59)。</summary>
    public string StartTimeSecond { get; set; } = string.Empty;

    /// <summary>结束时间-月 (01-12)。</summary>
    public string EndTimeMonth { get; set; } = string.Empty;

    /// <summary>结束时间-日 (01-31)。</summary>
    public string EndTimeDay { get; set; } = string.Empty;

    /// <summary>结束时间-时 (00-23)。</summary>
    public string EndTimeHour { get; set; } = string.Empty;

    /// <summary>结束时间-分 (00-59)。</summary>
    public string EndTimeMinute { get; set; } = string.Empty;

    /// <summary>结束时间-秒 (00-59)。</summary>
    public string EndTimeSecond { get; set; } = string.Empty;

    /// <summary>开始时间 (MM-DD-hh-mm-ss)，由分量拼出；任一分量为空则为空（未设置）。</summary>
    public string StartTime
    {
        get => TimePartsHelper.Build(StartTimeMonth, StartTimeDay, StartTimeHour, StartTimeMinute, StartTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 5);
            StartTimeMonth = parts[0];
            StartTimeDay = parts[1];
            StartTimeHour = parts[2];
            StartTimeMinute = parts[3];
            StartTimeSecond = parts[4];
        }
    }

    /// <summary>结束时间 (MM-DD-hh-mm-ss)，由分量拼出。</summary>
    public string EndTime
    {
        get => TimePartsHelper.Build(EndTimeMonth, EndTimeDay, EndTimeHour, EndTimeMinute, EndTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 5);
            EndTimeMonth = parts[0];
            EndTimeDay = parts[1];
            EndTimeHour = parts[2];
            EndTimeMinute = parts[3];
            EndTimeSecond = parts[4];
        }
    }
}
