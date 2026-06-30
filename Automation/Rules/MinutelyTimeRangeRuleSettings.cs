namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每分钟时间范围规则设置
/// 格式：ss
/// </summary>
public class MinutelyTimeRangeRuleSettings
{
    /// <summary>
    /// 开始秒数 (ss)
    /// </summary>
    public string StartSecond { get; set; } = string.Empty;

    /// <summary>
    /// 结束秒数 (ss)
    /// </summary>
    public string EndSecond { get; set; } = string.Empty;
}



