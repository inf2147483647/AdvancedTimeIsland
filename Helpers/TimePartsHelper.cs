using System;
using System.Linq;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 时间范围内“时-分-秒”等时间字符串与可独立选择的分量之间的转换。
///
/// 各时间范围设置类把时间拆成独立分量（年 / 月 / 日 / 时 / 分 / 秒），便于在 SAI 积木上以下拉方式
/// 选择、避免用键盘手打完整字符串；分量再由此类拼回原有的时间字符串，或把时间字符串拆回分量。
/// 由于对外的 <c>StartTime</c> / <c>EndTime</c> 字符串仍由分量派生，桌面端设置控件与规则判断逻辑
/// 都无需改动。
/// </summary>
public static class TimePartsHelper
{
    /// <summary>
    /// 把分量按顺序拼成 “a-b-c” 形式的时间字符串。
    /// 任一分量为空则返回空字符串（表示“未设置”，与原有的空时间字符串语义一致）。
    /// </summary>
    public static string Build(params string?[] parts) =>
        parts.Length == 0 || parts.Any(string.IsNullOrWhiteSpace)
            ? string.Empty
            : string.Join("-", parts.Select(part => part!.Trim().PadLeft(2, '0')));

    /// <summary>
    /// 把时间字符串按 '-' 拆成 <paramref name="count"/> 个分量；分量不足处补空字符串。
    /// </summary>
    public static string[] Split(string? value, int count)
    {
        var result = new string[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            var parts = value.Split('-');
            for (int i = 0; i < count && i < parts.Length; i++)
            {
                result[i] = parts[i].Trim();
            }
        }

        return result;
    }
}
