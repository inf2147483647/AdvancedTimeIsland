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
    /// 计算指定日期在一周中的序号（周一=0，周二=1，……，周日=6）。
    /// </summary>
    /// <remarks>
    /// <see cref="DateTime.DayOfWeek"/> 以周日=0……周六=6，与各“每周”设置下拉框的排列顺序
    /// （周一……周日，下标 0-6）不一致，判断星期时需用本方法统一转换。
    /// </remarks>
    /// <param name="date">目标日期。</param>
    /// <returns>星期序号（周一=0……周日=6）。</returns>
    public static int GetMondayBasedDayIndex(DateTime date) => ((int)date.DayOfWeek + 6) % 7;

    /// <summary>
    /// 判断指定时刻是否落在以「周内连续时间轴」表示的每周时间范围内。
    /// </summary>
    /// <remarks>
    /// 语义与「每年 / 每月时间范围」一致：<b>起点含、终点不含</b>。星期按
    /// <see cref="GetMondayBasedDayIndex"/> 的约定（周一=0……周日=6）。终点早于起点时自动顺延：
    /// 同一天则跨到次日（如 周一 22:00 → 周一 02:00），不同天则跨到下周的结束星期
    /// （如 周六 22:00 → 周一 02:00）；起止完全相同（同一天且同一时刻）视为零长区间，恒不命中。
    /// <para>
    /// 与旧版「日范围 ∩ 当天时段」不同，本方法把整个区间视为一条连续时间轴：
    /// 周一 08:00 → 周五 17:00 覆盖周一 08:00 至周五 17:00 之间的全部时刻（夜间同样命中），
    /// 因此「周六 00:00 → 周一 00:00」可正确覆盖整个周末（2880 分钟）。
    /// </para>
    /// </remarks>
    /// <param name="now">要判断的时刻。</param>
    /// <param name="startDayOfWeek">开始星期（周一=0……周日=6）。</param>
    /// <param name="startHour">开始时（0-23）。</param>
    /// <param name="startMinute">开始分（0-59）。</param>
    /// <param name="startSecond">开始秒（0-59）。</param>
    /// <param name="endDayOfWeek">结束星期（周一=0……周日=6）。</param>
    /// <param name="endHour">结束时（0-23）。</param>
    /// <param name="endMinute">结束分（0-59）。</param>
    /// <param name="endSecond">结束秒（0-59）。</param>
    /// <returns>命中返回 true；参数非法或区间长度为零返回 false。</returns>
    public static bool IsInWeeklyRange(
        DateTime now,
        int startDayOfWeek, int startHour, int startMinute, int startSecond,
        int endDayOfWeek, int endHour, int endMinute, int endSecond)
    {
        if (startDayOfWeek is < 0 or > 6 || endDayOfWeek is < 0 or > 6)
            return false;

        if (!IsValidTimeOfDay(startHour, startMinute, startSecond) ||
            !IsValidTimeOfDay(endHour, endMinute, endSecond))
            return false;

        // 同一天且同一时刻 = 零长区间，任何时刻都不命中。
        if (startDayOfWeek == endDayOfWeek &&
            startHour == endHour && startMinute == endMinute && startSecond == endSecond)
            return false;

        // 以 now 所在周的周一 00:00:00 为原点，把「星期 + 时刻」铺成一条 7 天时间轴。
        var weekStart = now.Date.AddDays(-GetMondayBasedDayIndex(now));
        var start = weekStart.AddDays(startDayOfWeek)
            .AddHours(startHour).AddMinutes(startMinute).AddSeconds(startSecond);
        var end = weekStart.AddDays(endDayOfWeek)
            .AddHours(endHour).AddMinutes(endMinute).AddSeconds(endSecond);

        if (end < start)
        {
            // 同一天但结束时刻更早（如周一 22:00 → 周一 02:00）：跨到次日。
            // 不同天（如周六 22:00 → 周一 02:00）：跨到下一周的结束星期。
            end = startDayOfWeek == endDayOfWeek ? end.AddDays(1) : end.AddDays(7);
        }

        // now 可能落在上一周开始、一直延续到本周的跨周区间内。
        if (now < start)
        {
            start = start.AddDays(-7);
            end = end.AddDays(-7);
        }

        return now >= start && now < end;
    }

    private static bool IsValidTimeOfDay(int hour, int minute, int second) =>
        hour is >= 0 and < 24 && minute is >= 0 and < 60 && second is >= 0 and < 60;

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