namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 在指定时间范围内 规则设置。
/// 周期（Period）决定判断哪个时间单位，范围表达式（Expression）描述该单位允许取值的集合。
/// </summary>
public class SpecifiedTimeRangeRuleSettings
{
    /// <summary>
    /// 周期：0=每年 1=每月 2=每周 3=每天 4=每小时 5=每分钟。
    /// 顺序与 <see cref="Helpers.SpecifiedTimeRangeHelper.PeriodNames"/> 一致。
    /// </summary>
    public int Period { get; set; }

    /// <summary>
    /// 周期内单位的序号，目前每个周期只有 1 个单位（恒为 0），保留以便后续扩展。
    /// </summary>
    public int Unit { get; set; }

    /// <summary>
    /// 范围表达式，如 "1,3-5,7~11"。多段用逗号分隔，段内用减号(-)或波浪号(~)表示闭区间。
    /// </summary>
    public string Expression { get; set; } = string.Empty;
}
