using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历精确时间范围规则设置
/// 格式：[L_YYYY-L_MM-L_DD-hh-mm-ss]₁ ~ [L_YYYY-L_MM-L_DD-hh-mm-ss]₂
/// </summary>
public class LunarExactTimeRangeRuleSettings
{
    public int StartLunarYear { get; set; } = 0;
    public int StartLunarYearRangeEnd { get; set; } = 0;
    public int StartLunarMonth { get; set; } = 0;
    public bool StartIsLeapMonth { get; set; } = false;
    public int StartLunarDay { get; set; } = 0;

    /// <summary>开始时间-时 (00-23)。</summary>
    public string StartTargetTimeHour { get; set; } = string.Empty;

    /// <summary>开始时间-分 (00-59)。</summary>
    public string StartTargetTimeMinute { get; set; } = string.Empty;

    /// <summary>开始时间-秒 (00-59)。</summary>
    public string StartTargetTimeSecond { get; set; } = string.Empty;

    public int EndLunarYear { get; set; } = 0;
    public int EndLunarYearRangeEnd { get; set; } = 0;
    public int EndLunarMonth { get; set; } = 0;
    public bool EndIsLeapMonth { get; set; } = false;
    public int EndLunarDay { get; set; } = 0;

    /// <summary>结束时间-时 (00-23)。</summary>
    public string EndTargetTimeHour { get; set; } = string.Empty;

    /// <summary>结束时间-分 (00-59)。</summary>
    public string EndTargetTimeMinute { get; set; } = string.Empty;

    /// <summary>结束时间-秒 (00-59)。</summary>
    public string EndTargetTimeSecond { get; set; } = string.Empty;

    /// <summary>开始时间 (hh-mm-ss)，由分量拼出；任一分量为空则为空（未设置）。</summary>
    public string StartTargetTime
    {
        get => TimePartsHelper.Build(StartTargetTimeHour, StartTargetTimeMinute, StartTargetTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 3);
            StartTargetTimeHour = parts[0];
            StartTargetTimeMinute = parts[1];
            StartTargetTimeSecond = parts[2];
        }
    }

    /// <summary>结束时间 (hh-mm-ss)，由分量拼出。</summary>
    public string EndTargetTime
    {
        get => TimePartsHelper.Build(EndTargetTimeHour, EndTargetTimeMinute, EndTargetTimeSecond);
        set
        {
            var parts = TimePartsHelper.Split(value, 3);
            EndTargetTimeHour = parts[0];
            EndTargetTimeMinute = parts[1];
            EndTargetTimeSecond = parts[2];
        }
    }
}
