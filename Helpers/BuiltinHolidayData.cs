using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Models;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 内置的中国法定节假日与调休补班数据（依据国务院办公厅历年放假安排通知整理）。
/// 用于首次运行时的初始化，以及联网更新失败时的兜底数据。
/// 用户可在设置页中手动增删改，联网更新只覆盖内置与联网来源的条目，不会破坏手动编辑的内容。
/// </summary>
public static class BuiltinHolidayData
{
    /// <summary>内置数据覆盖的年份。</summary>
    public static IReadOnlyList<int> SupportedYears { get; } = new[] { 2024, 2025, 2026 };

    /// <summary>创建内置的法定节假日（放假日）条目。</summary>
    public static List<HolidayDay> CreateHolidays()
    {
        var list = new List<HolidayDay>();

        // ==================== 2024 ====================
        AddRange(list, new DateTime(2024, 1, 1), 1, "元旦");
        AddRange(list, new DateTime(2024, 2, 10), 8, "春节");
        AddRange(list, new DateTime(2024, 4, 4), 3, "清明节");
        AddRange(list, new DateTime(2024, 5, 1), 5, "劳动节");
        AddRange(list, new DateTime(2024, 6, 10), 1, "端午节");
        AddRange(list, new DateTime(2024, 9, 15), 3, "中秋节");
        AddRange(list, new DateTime(2024, 10, 1), 7, "国庆节");

        // ==================== 2025 ====================
        AddRange(list, new DateTime(2025, 1, 1), 1, "元旦");
        AddRange(list, new DateTime(2025, 1, 28), 8, "春节");
        AddRange(list, new DateTime(2025, 4, 4), 3, "清明节");
        AddRange(list, new DateTime(2025, 5, 1), 5, "劳动节");
        AddRange(list, new DateTime(2025, 5, 31), 3, "端午节");
        AddRange(list, new DateTime(2025, 10, 1), 8, "国庆节 中秋节");

        // ==================== 2026 ====================
        AddRange(list, new DateTime(2026, 1, 1), 3, "元旦");
        AddRange(list, new DateTime(2026, 2, 15), 9, "春节");
        AddRange(list, new DateTime(2026, 4, 4), 3, "清明节");
        AddRange(list, new DateTime(2026, 5, 1), 5, "劳动节");
        AddRange(list, new DateTime(2026, 6, 19), 3, "端午节");
        AddRange(list, new DateTime(2026, 9, 25), 3, "中秋节");
        AddRange(list, new DateTime(2026, 10, 1), 7, "国庆节");

        return list;
    }

    /// <summary>创建内置的调休补班日条目。</summary>
    public static List<HolidayDay> CreateMakeupDays()
    {
        var list = new List<HolidayDay>();

        // ==================== 2024 ====================
        AddDay(list, new DateTime(2024, 2, 4), "春节调休");
        AddDay(list, new DateTime(2024, 2, 18), "春节调休");
        AddDay(list, new DateTime(2024, 4, 7), "清明调休");
        AddDay(list, new DateTime(2024, 4, 28), "劳动节调休");
        AddDay(list, new DateTime(2024, 5, 11), "劳动节调休");
        AddDay(list, new DateTime(2024, 9, 14), "中秋调休");
        AddDay(list, new DateTime(2024, 9, 29), "国庆调休");
        AddDay(list, new DateTime(2024, 10, 12), "国庆调休");

        // ==================== 2025 ====================
        AddDay(list, new DateTime(2025, 1, 26), "春节调休");
        AddDay(list, new DateTime(2025, 2, 8), "春节调休");
        AddDay(list, new DateTime(2025, 4, 27), "劳动节调休");
        AddDay(list, new DateTime(2025, 9, 28), "国庆调休");
        AddDay(list, new DateTime(2025, 10, 11), "国庆调休");

        // ==================== 2026 ====================
        AddDay(list, new DateTime(2026, 1, 4), "元旦调休");
        AddDay(list, new DateTime(2026, 2, 14), "春节调休");
        AddDay(list, new DateTime(2026, 2, 28), "春节调休");
        AddDay(list, new DateTime(2026, 5, 9), "劳动节调休");
        AddDay(list, new DateTime(2026, 9, 20), "国庆调休");
        AddDay(list, new DateTime(2026, 10, 10), "国庆调休");

        return list;
    }

    private static void AddRange(List<HolidayDay> list, DateTime start, int days, string name)
    {
        for (var i = 0; i < days; i++)
        {
            AddDay(list, start.AddDays(i), name, true);
        }
    }

    private static void AddDay(List<HolidayDay> list, DateTime date, string name, bool isOffDay = false)
    {
        list.Add(new HolidayDay
        {
            Date = date.Date,
            Name = name,
            IsOffDay = isOffDay,
            Source = HolidayEntrySource.Builtin
        });
    }
}