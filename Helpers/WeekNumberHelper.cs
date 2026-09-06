using System;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 周数计算辅助类。
/// </summary>
public static class WeekNumberHelper
{
    /// <summary>
    /// 计算指定日期所处年份的周数。
    /// </summary>
    /// <param name="date">目标日期。</param>
    /// <param name="firstDayOfWeek">一周的第一天，取值 0-6，对应周日到周六，默认为周日。</param>
    /// <returns>周数（1 为开始）。</returns>
    public static int GetYearWeekNumber(DateTime date, int firstDayOfWeek = 0)
    {
        firstDayOfWeek = ((firstDayOfWeek % 7) + 7) % 7;
        // 向回偏移到本周"第一天"所在日期。
        var start = date.Date.AddDays(-(((int)date.DayOfWeek - firstDayOfWeek + 7) % 7));
        var year = start.Year;
        var firstOfYear = new DateTime(year, 1, 1);
        // 当年第一个"第一天"（即 >= 1 月 1 日的第一个 firstDayOfWeek）。
        var firstStart = firstOfYear.AddDays(((firstDayOfWeek - (int)firstOfYear.DayOfWeek + 7) % 7));
        return (int)((start - firstStart).Days / 7) + 1;
    }

    /// <summary>
    /// 计算指定日期从学期开始日起的周数。
    /// </summary>
    /// <remarks>
    /// 以学期开始日为一周的第一天（即学期开始日所在周为第一周）。
    /// 若日期早于学期开始日，返回 0。
    /// </remarks>
    /// <param name="date">目标日期。</param>
    /// <param name="semesterStartDate">学期开始日。</param>
    /// <returns>周数（1 为开始，0 表示早于学期开始日）。</returns>
    public static int GetSemesterWeekNumber(DateTime date, DateTime semesterStartDate)
    {
        var days = (date.Date - semesterStartDate.Date).Days;
        return days < 0 ? 0 : (days / 7) + 1;
    }
}