using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每小时时间范围规则设置
/// 格式：mm-ss
/// </summary>
public class HourlyTimeRangeRuleSettings
{
    /// <summary>开始时间-分 (00-59)。</summary>
    public string StartTimeMinute { get; set; } = string.Empty;

    /// <summary>开始时间-秒 (00-59)。</summary>
    public string StartTimeSecond { get; set; } = string.Empty;

    /// <summary>结束时间-分 (00-59)。</summary>
    public string EndTimeMinute { get; set; } = string.Empty;

    /// <summary>结束时间-秒 (00-59)。</summary>
    public string EndTimeSecond { get; set; } = string.Empty;

    /// <summary>开始时间 (mm-ss)，由分量拼出；任一分量为空则为空（未设置）。</summary>
    public string StartTime
    {
        get => TimePartsHelper.Build(StartTimeMinute, StartTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 2);
            StartTimeMinute = parts[0];
            StartTimeSecond = parts[1];
        }
    }

    /// <summary>结束时间 (mm-ss)，由分量拼出。</summary>
    public string EndTime
    {
        get => TimePartsHelper.Build(EndTimeMinute, EndTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 2);
            EndTimeMinute = parts[0];
            EndTimeSecond = parts[1];
        }
    }
}
