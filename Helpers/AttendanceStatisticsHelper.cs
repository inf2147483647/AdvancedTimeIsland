using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 一个分段（周或月）的在校统计结果。
/// </summary>
public sealed class PeriodStatistics
{
    /// <summary>分段标题，如"第 3 周"、"2026年9月"。</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>分段起始日期（含）。</summary>
    public DateTime Start { get; set; }

    /// <summary>分段结束日期（含）。</summary>
    public DateTime End { get; set; }

    /// <summary>区间内的在校日总数。</summary>
    public int InSchoolDays { get; set; }

    /// <summary>区间内已过去的在校日数。</summary>
    public int ElapsedInSchoolDays { get; set; }

    /// <summary>区间内的在校时长（小时）。</summary>
    public double Hours { get; set; }

    /// <summary>区间文案，如"9/1–9/7"。</summary>
    public string RangeText => $"{Start:M/d}–{End:M/d}";
}

/// <summary>
/// 学期在校时间统计结果。
/// </summary>
public sealed class AttendanceStatistics
{
    /// <summary>学期开始日（含）。</summary>
    public DateTime StartDate { get; set; }

    /// <summary>学期结束日（含）。</summary>
    public DateTime EndDate { get; set; }

    /// <summary>参与统计的当前日期。</summary>
    public DateTime Today { get; set; }

    /// <summary>学期区间的自然日总数。</summary>
    public int TotalCalendarDays { get; set; }

    /// <summary>学期内的在校日总数。</summary>
    public int TotalInSchoolDays { get; set; }

    /// <summary>截至当前日期已经历的在校日数（含当天）。</summary>
    public int ElapsedInSchoolDays { get; set; }

    /// <summary>剩余在校日数。</summary>
    public int RemainingInSchoolDays => Math.Max(0, TotalInSchoolDays - ElapsedInSchoolDays);

    /// <summary>每日标准在校时长（小时）。</summary>
    public double DailyHours { get; set; }

    /// <summary>学期在校总时长（小时）。</summary>
    public double TotalHours => TotalInSchoolDays * DailyHours;

    /// <summary>已在校时长（小时）。</summary>
    public double ElapsedHours => ElapsedInSchoolDays * DailyHours;

    /// <summary>剩余在校时长（小时）。</summary>
    public double RemainingHours => RemainingInSchoolDays * DailyHours;

    /// <summary>在校天数进度百分比（0–100）。</summary>
    public double ProgressPercent =>
        TotalInSchoolDays <= 0 ? 0 : Math.Min(100, ElapsedInSchoolDays * 100.0 / TotalInSchoolDays);

    /// <summary>当前日期是否早于学期开始日。</summary>
    public bool NotStarted => Today.Date < StartDate.Date;

    /// <summary>当前日期是否晚于学期结束日。</summary>
    public bool Finished => Today.Date > EndDate.Date;

    /// <summary>按周分段的统计结果。</summary>
    public List<PeriodStatistics> Weekly { get; } = new();

    /// <summary>按月分段的统计结果。</summary>
    public List<PeriodStatistics> Monthly { get; } = new();
}

/// <summary>
/// 在校日判定与统计计算的核心算法。
/// </summary>
public static class AttendanceStatisticsHelper
{
    /// <summary>学期区间最大统计跨度（天），防止误配置导致超长遍历。</summary>
    private const int MaxSpanDays = 3650;

    /// <summary>
    /// 解析学期开始日：自动模式下取 ClassIsland 的单周（学期）开始时间。
    /// </summary>
    /// <returns>学期开始日；无法确定时返回 null。</returns>
    public static DateTime? ResolveSemesterStart(AttendanceStatisticsConfig config)
    {
        if (config.StartSource == SemesterStartSource.Manual)
        {
            return config.ManualStartDate.Date;
        }

        var fromClassIsland = SemesterStartService.CurrentSemesterStartDate;
        return fromClassIsland?.Date;
    }

    /// <summary>
    /// 解析学期结束日：按周数推算时，以开始日为第一周第一天，总周数 × 7 天的最后一天为结束日。
    /// </summary>
    public static DateTime ResolveSemesterEnd(DateTime semesterStart, AttendanceStatisticsConfig config)
    {
        if (config.EndSource == SemesterEndSource.ManualDate)
        {
            return config.ManualEndDate.Date;
        }

        return semesterStart.Date.AddDays(config.TotalWeeks * 7 - 1);
    }

    /// <summary>
    /// 判断指定日期是否为在校日。
    /// 优先级：自定义日期（计入/排除）→ 寒暑假 → 法定节假日与调休补班（补班优先于周末）→ 启用时间表为空 → 周末。
    /// </summary>
    public static bool IsInSchoolDay(DateTime date, AttendanceStatisticsConfig config, AttendanceCalendarData data,
        IReadOnlyCollection<DateTime>? emptyScheduleDates = null)
    {
        var included = ToDateSet(data.CustomIncluded);
        var excluded = ToDateSet(data.CustomExcluded);
        var holidays = ToDateSet(data.Holidays);
        var makeup = ToDateSet(data.MakeupDays);
        return IsInSchoolDay(date, config, included, excluded, holidays, makeup, data.Vacations,
            emptyScheduleDates == null ? null : new HashSet<DateTime>(emptyScheduleDates.Select(d => d.Date)));
    }

    /// <summary>
    /// 计算学期在校时间统计结果。
    /// </summary>
    /// <param name="semesterStart">学期开始日。</param>
    /// <param name="semesterEnd">学期结束日。</param>
    /// <param name="today">参与统计的当前日期。</param>
    /// <param name="config">统计口径配置。</param>
    /// <param name="data">节假日等数据。</param>
    /// <param name="dailyHours">每日在校时长（小时），用于天数换算时长；手动/自动模式的结果由调用方解析后传入。</param>
    /// <param name="emptyScheduleDates">启用时间表为空的日期，这些日期计为非在校日。</param>
    public static AttendanceStatistics Compute(DateTime semesterStart, DateTime semesterEnd, DateTime today,
        AttendanceStatisticsConfig config, AttendanceCalendarData data, double dailyHours,
        IReadOnlyCollection<DateTime>? emptyScheduleDates = null)
    {
        var start = semesterStart.Date;
        var end = semesterEnd.Date;
        if (end < start)
        {
            (start, end) = (end, start);
        }
        if ((end - start).TotalDays > MaxSpanDays)
        {
            end = start.AddDays(MaxSpanDays);
        }

        var todayDate = today.Date;
        var stats = new AttendanceStatistics
        {
            StartDate = start,
            EndDate = end,
            Today = todayDate,
            TotalCalendarDays = (end - start).Days + 1,
            DailyHours = dailyHours
        };

        var included = ToDateSet(data.CustomIncluded);
        var excluded = ToDateSet(data.CustomExcluded);
        var holidays = ToDateSet(data.Holidays);
        var makeup = ToDateSet(data.MakeupDays);
        var emptySchedule = emptyScheduleDates == null
            ? null
            : new HashSet<DateTime>(emptyScheduleDates.Select(d => d.Date));

        var weekly = new Dictionary<int, PeriodStatistics>();
        var monthly = new Dictionary<DateTime, PeriodStatistics>();

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var inSchool = IsInSchoolDay(date, config, included, excluded, holidays, makeup, data.Vacations, emptySchedule);

            var weekIndex = (date - start).Days / 7;
            var weekPeriod = GetOrAdd(weekly, weekIndex, () => new PeriodStatistics
            {
                Label = $"第 {weekIndex + 1} 周",
                Start = start.AddDays(weekIndex * 7),
                End = Min(start.AddDays(weekIndex * 7 + 6), end)
            });

            var monthKey = new DateTime(date.Year, date.Month, 1);
            var monthPeriod = GetOrAdd(monthly, monthKey, () => new PeriodStatistics
            {
                Label = $"{date.Year}年{date.Month}月",
                Start = Max(monthKey, start),
                End = Min(monthKey.AddMonths(1).AddDays(-1), end)
            });

            if (!inSchool)
            {
                continue;
            }

            stats.TotalInSchoolDays++;
            weekPeriod.InSchoolDays++;
            monthPeriod.InSchoolDays++;
            if (date <= todayDate)
            {
                stats.ElapsedInSchoolDays++;
                weekPeriod.ElapsedInSchoolDays++;
                monthPeriod.ElapsedInSchoolDays++;
            }
        }

        foreach (var period in weekly.Values)
        {
            period.Hours = period.InSchoolDays * dailyHours;
            stats.Weekly.Add(period);
        }
        stats.Weekly.Sort((a, b) => a.Start.CompareTo(b.Start));

        foreach (var period in monthly.Values)
        {
            period.Hours = period.InSchoolDays * dailyHours;
            stats.Monthly.Add(period);
        }
        stats.Monthly.Sort((a, b) => a.Start.CompareTo(b.Start));

        return stats;
    }

    private static bool IsInSchoolDay(DateTime date, AttendanceStatisticsConfig config,
        HashSet<DateTime> included, HashSet<DateTime> excluded,
        HashSet<DateTime> holidays, HashSet<DateTime> makeup, List<VacationRange> vacations,
        HashSet<DateTime>? emptyScheduleDates)
    {
        var d = date.Date;

        // 优先级 1：自定义日期（最高，强制计入/排除，可覆盖寒暑假与法定节假日）。
        if (config.RespectCustomDates)
        {
            if (included.Contains(d))
            {
                return true;
            }
            if (excluded.Contains(d))
            {
                return false;
            }
        }

        // 优先级 2：寒暑假区间（整段排除，区间内的调休补班日也一并排除）。
        if (config.ExcludeVacations && IsInVacation(d, vacations))
        {
            return false;
        }

        // 优先级 3：法定节假日与调休补班（同一档，调休补班判定在前）。
        // 补班判定必须先于周末判定，保证补班日即使落在周六/周日也被计为在校日。
        if (config.CountMakeupDays && makeup.Contains(d))
        {
            return true;
        }
        if (config.ExcludeHolidays && holidays.Contains(d))
        {
            return false;
        }

        // 优先级 4：启用时间表为空（当天课表没有任何启用课程）计为非在校日。
        if (emptyScheduleDates != null && emptyScheduleDates.Contains(d))
        {
            return false;
        }

        // 优先级 5：周末。
        if (d.DayOfWeek == DayOfWeek.Saturday && config.ExcludeSaturday)
        {
            return false;
        }
        if (d.DayOfWeek == DayOfWeek.Sunday && config.ExcludeSunday)
        {
            return false;
        }

        return true;
    }

    private static bool IsInVacation(DateTime date, List<VacationRange> vacations)
    {
        foreach (var vacation in vacations)
        {
            if (vacation.Contains(date))
            {
                return true;
            }
        }
        return false;
    }

    private static HashSet<DateTime> ToDateSet(List<HolidayDay>? days)
    {
        var set = new HashSet<DateTime>();
        if (days == null)
        {
            return set;
        }
        foreach (var day in days)
        {
            set.Add(day.Date.Date);
        }
        return set;
    }

    private static PeriodStatistics GetOrAdd(Dictionary<int, PeriodStatistics> map, int key, Func<PeriodStatistics> factory)
    {
        if (!map.TryGetValue(key, out var value))
        {
            value = factory();
            map[key] = value;
        }
        return value;
    }

    private static PeriodStatistics GetOrAdd(Dictionary<DateTime, PeriodStatistics> map, DateTime key, Func<PeriodStatistics> factory)
    {
        if (!map.TryGetValue(key, out var value))
        {
            value = factory();
            map[key] = value;
        }
        return value;
    }

    private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;

    private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;
}