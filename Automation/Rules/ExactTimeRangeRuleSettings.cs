using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 精确时间范围规则设置
/// 格式：yyyy-MM-dd-HH-mm-ss
/// </summary>
public class ExactTimeRangeRuleSettings
{
    /// <summary>开始时间-年 (如 2026)。</summary>
    public string StartTimeYear { get; set; } = string.Empty;

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

    /// <summary>结束时间-年。</summary>
    public string EndTimeYear { get; set; } = string.Empty;

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

    /// <summary>开始时间 (yyyy-MM-dd-HH-mm-ss)，由分量拼出；任一分量为空则为空（未设置）。</summary>
    public string StartTime
    {
        get => TimePartsHelper.Build(StartTimeYear, StartTimeMonth, StartTimeDay, StartTimeHour, StartTimeMinute, StartTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 6);
            StartTimeYear = parts[0];
            StartTimeMonth = parts[1];
            StartTimeDay = parts[2];
            StartTimeHour = parts[3];
            StartTimeMinute = parts[4];
            StartTimeSecond = parts[5];
        }
    }

    /// <summary>结束时间 (yyyy-MM-dd-HH-mm-ss)，由分量拼出。</summary>
    public string EndTime
    {
        get => TimePartsHelper.Build(EndTimeYear, EndTimeMonth, EndTimeDay, EndTimeHour, EndTimeMinute, EndTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 6);
            EndTimeYear = parts[0];
            EndTimeMonth = parts[1];
            EndTimeDay = parts[2];
            EndTimeHour = parts[3];
            EndTimeMinute = parts[4];
            EndTimeSecond = parts[5];
        }
    }
}
