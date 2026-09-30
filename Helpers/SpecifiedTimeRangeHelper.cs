using System;
using System.Collections.Generic;
using System.Globalization;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// “在指定时间范围内”规则的范围表达式解析与匹配。
///
/// 表达式由若干段组成，段之间用半角逗号（也兼容全角逗号、顿号、分号）分隔；
/// 每段是单个数值，或 “起点-终点”“起点~终点” 形式的闭区间，例如 "1,3-5,7~11"。
/// 判断方式是把当前时间换算成所选周期单位上的整数后再做集合匹配，因此区间两端都会被取到：
/// “每年”的“月份”输入 1~3，等价于 1 月 1 日 0 时 0 分 0 秒 到 3 月 31 日 23 时 59 分 59 秒。
/// </summary>
public static class SpecifiedTimeRangeHelper
{
    /// <summary>
    /// 周期显示名。下标即设置类 <c>SpecifiedTimeRangeRuleSettings.Period</c> 的取值，顺序与下拉框一致。
    /// </summary>
    public static readonly string[] PeriodNames = { "每年", "每月", "每周", "每天", "每小时", "每分钟" };

    /// <summary>
    /// 每个周期可判断的单位显示名，顺序与 <see cref="PeriodNames"/> 一一对应。
    /// 每个周期目前只提供 1 个单位，保留为下拉以便后续扩展。
    /// </summary>
    public static readonly string[][] UnitNames =
    {
        new[] { "月份" }, // 每年
        new[] { "日期" }, // 每月
        new[] { "星期" }, // 每周
        new[] { "小时" }, // 每天
        new[] { "分钟" }, // 每小时
        new[] { "秒" },   // 每分钟
    };

    /// <summary>各周期单位的取值区间（闭区间），顺序与 <see cref="PeriodNames"/> 一一对应。</summary>
    private static readonly (int Min, int Max)[] ValueRanges =
    {
        (1, 12), // 月份
        (1, 31), // 日期
        (1, 7),  // 星期（周一=1，周日=7）
        (0, 23), // 小时
        (0, 59), // 分钟
        (0, 59), // 秒
    };

    /// <summary>段与段之间的分隔符。</summary>
    private static readonly char[] SegmentSeparators = { ',', '，', '、', ';', '；' };

    /// <summary>区间起止之间的分隔符。</summary>
    private static readonly char[] RangeSeparators = { '-', '~', '～', '－', '–', '—' };

    /// <summary>把越界的周期值收敛到合法范围。</summary>
    public static int ClampPeriod(int period) =>
        period < 0 || period >= PeriodNames.Length ? 0 : period;

    /// <summary>取指定周期单位的取值区间。</summary>
    public static (int Min, int Max) GetValueRange(int period) => ValueRanges[ClampPeriod(period)];

    /// <summary>取指定周期下第 <paramref name="unit"/> 个单位的显示名（越界时回退到第一个）。</summary>
    public static string GetUnitName(int period, int unit)
    {
        var names = UnitNames[ClampPeriod(period)];
        return unit >= 0 && unit < names.Length ? names[unit] : names[0];
    }

    /// <summary>按指定周期单位取 <paramref name="now"/> 当前所处的数值。</summary>
    public static int GetCurrentValue(int period, DateTime now) => ClampPeriod(period) switch
    {
        0 => now.Month,
        1 => now.Day,
        2 => ((int)now.DayOfWeek + 6) % 7 + 1, // 周一=1 …… 周日=7
        3 => now.Hour,
        4 => now.Minute,
        _ => now.Second,
    };

    /// <summary>
    /// 解析表达式。成功时返回按输入顺序排列的闭区间列表，失败时 <paramref name="error"/> 给出原因。
    /// </summary>
    public static bool TryParse(string? expression, int period, out List<(int Start, int End)> ranges, out string error)
    {
        ranges = new List<(int Start, int End)>();
        error = string.Empty;
        period = ClampPeriod(period);

        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "请填写范围表达式";
            return false;
        }

        var (min, max) = GetValueRange(period);
        var segments = expression.Split(SegmentSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            error = "请填写范围表达式";
            return false;
        }

        foreach (var rawSegment in segments)
        {
            var segment = rawSegment.Trim();
            if (segment.Length == 0)
            {
                continue;
            }

            var values = new List<int>();
            foreach (var rawPart in segment.Split(RangeSeparators, StringSplitOptions.None))
            {
                var part = rawPart.Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    error = $"“{segment}”不是有效的时间数值";
                    return false;
                }

                values.Add(value);
            }

            if (values.Count == 0 || values.Count > 2)
            {
                error = $"“{segment}”应为单个数值或“起点-终点”形式的范围";
                return false;
            }

            var start = values[0];
            var end = values[values.Count - 1];
            if (start > end)
            {
                (start, end) = (end, start);
            }

            if (start < min || end > max)
            {
                error = $"{GetUnitName(period, 0)}的取值应在 {min}-{max} 之间";
                return false;
            }

            ranges.Add((start, end));
        }

        if (ranges.Count == 0)
        {
            error = "请填写范围表达式";
            return false;
        }

        return true;
    }

    /// <summary>判断 <paramref name="now"/> 是否落在表达式描述的范围内（表达式非法时视为不满足）。</summary>
    public static bool Matches(string? expression, int period, DateTime now) =>
        TryParse(expression, period, out var ranges, out _) &&
        Contains(ranges, GetCurrentValue(period, now));

    /// <summary>判断数值是否落在任一段闭区间内。</summary>
    public static bool Contains(List<(int Start, int End)> ranges, int value)
    {
        foreach (var (start, end) in ranges)
        {
            if (value >= start && value <= end)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>取指定周期的输入示例，用作控件内的提示文案。</summary>
    public static string GetExample(int period) => ClampPeriod(period) switch
    {
        0 => "例如：1,3-5,7~11。输入 1~3 表示从 1 月 1 日到 3 月 31 日",
        1 => "例如：1,10,15~20。输入 1~3 表示每月 1 日到 3 日",
        2 => "例如：1,3~5。输入 1~5 表示从周一到周五",
        3 => "例如：8,12~13。输入 12~13 表示从 12:00:00 到 13:59:59",
        4 => "例如：0,15,30~45。表示每个小时内的第几分钟",
        _ => "例如：0,30。表示每分钟内的第几秒",
    };
}
