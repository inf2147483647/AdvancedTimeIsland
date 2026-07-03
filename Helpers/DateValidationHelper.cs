using System;

namespace AdvancedTimeIsland.Helpers;

public static class DateValidationHelper
{
    // ===== 1582年历法改革相关代码已注释掉 =====
    // lunar-csharp 的 Solar 类已处理1582年历法改革（消失的10天），无需手动处理
    // private static readonly DateTime GregorianTransitionStart = new(1582, 10, 5);
    // private static readonly DateTime GregorianTransitionEnd = new(1582, 10, 14);

    public static bool IsInvalidGregorianTransitionDate(int year, int month, int day)
    {
        // lunar-csharp 已处理1582年历法改革，此方法保留但始终返回false
        // if (year != 1582) return false;
        // if (month != 10) return false;
        // return day >= 5 && day <= 14;
        return false;
    }

    public static bool IsInvalidGregorianTransitionDate(DateTime dateTime)
    {
        return IsInvalidGregorianTransitionDate(dateTime.Year, dateTime.Month, dateTime.Day);
    }

    public static DateTime FixInvalidDate(int year, int month, int day, int hour, int minute, int second)
    {
        // 1582年修正已注释掉 - lunar-csharp 已处理
        // if (!IsInvalidGregorianTransitionDate(year, month, day)) { ... }
        // var fixedDay = day + 10;

        return new DateTime(year, month, day, hour, minute, second);
    }

    public static bool TryParseSafeDateTime(int year, int month, int day, int hour, int minute, int second, out DateTime result)
    {
        result = DateTime.MinValue;

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;

        // 1582年检查已注释掉 - lunar-csharp 已处理
        // if (IsInvalidGregorianTransitionDate(year, month, day)) return false;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        if (day > daysInMonth)
            return false;

        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        result = new DateTime(year, month, day, hour, minute, second);
        return true;
    }

    public static DateTime AdjustDateForGregorianTransition(DateTime dateTime)
    {
        // 1582年修正已注释掉 - lunar-csharp 已处理
        // if (!IsInvalidGregorianTransitionDate(dateTime)) return dateTime;
        // return dateTime.AddDays(10);

        return dateTime;
    }

    public static DateTime AdjustDateAfterAddition(DateTime originalDate, double daysToAdd)
    {
        var result = originalDate.AddDays(daysToAdd);

        // 1582年修正已注释掉 - lunar-csharp 已处理
        // if (IsInvalidGregorianTransitionDate(result)) { ... }

        return result;
    }

    public static DateTime AdjustDateAfterAddition(DateTime originalDate, TimeSpan timeSpan)
    {
        var result = originalDate.Add(timeSpan);

        // 1582年修正已注释掉 - lunar-csharp 已处理
        // if (IsInvalidGregorianTransitionDate(result)) { ... }

        return result;
    }

    public static bool TryAdjustDateForGregorianTransition(int year, int month, int day, out int adjustedYear, out int adjustedMonth, out int adjustedDay)
    {
        adjustedYear = year;
        adjustedMonth = month;
        adjustedDay = day;

        // 1582年修正已注释掉 - lunar-csharp 已处理
        // if (!IsInvalidGregorianTransitionDate(year, month, day)) return true;
        // adjustedDay += 10;

        return true;
    }
}
