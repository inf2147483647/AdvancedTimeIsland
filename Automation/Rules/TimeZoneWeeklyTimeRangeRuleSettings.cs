using System.Text.Json.Serialization;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 区时每周时间范围规则设置（带时区）
/// </summary>
public class TimeZoneWeeklyTimeRangeRuleSettings : WeeklyTimeRangeRuleSettings
{
    /// <summary>
    /// 插件默认时区 Id（与其它「区时」每日 / 每小时 / 每月 / 每年 / 精确时间规则一致）。
    /// </summary>
    public const string DefaultTimeZoneId = "China Standard Time";

    /// <summary>
    /// 时区 ID（Windows 时区 Id，如 <c>China Standard Time</c>），取值来自 <see cref="TimeZoneInfo.GetSystemTimeZones"/>。
    /// </summary>
    public string TimeZoneId { get; set; } = DefaultTimeZoneId;

    /// <summary>
    /// 2.0.5.4 及更早版本的时区字段，<b>仅作旧档案反序列化迁移入口</b>：
    /// 值为合法系统时区 Id 时才写入 <see cref="TimeZoneId"/>，否则丢弃
    /// （旧默认值 <c>"UTC+8"</c> 不是系统时区 Id，会让下拉框永远选不中、运行时静默回退本地时间）；
    /// 读取恒为 <c>null</c>，序列化时忽略，避免废弃字段写回档案。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Obsolete("已由 TimeZoneId 取代，仅用于旧档案反序列化迁移。")]
    public string? TimeZone
    {
        get => null;
        set
        {
            if (IsValidTimeZoneId(value))
            {
                TimeZoneId = value!;
            }
        }
    }

    /// <summary>
    /// 判断给定 Id 是否为当前系统可识别的时区 Id。
    /// </summary>
    public static bool IsValidTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
    }
}
