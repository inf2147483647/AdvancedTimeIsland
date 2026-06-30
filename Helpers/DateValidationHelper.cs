using System;

namespace AdvancedTimeIsland.Helpers;

public static class DateValidationHelper
{
    private static readonly DateTime GregorianTransitionStart = new(1582, 10, 5);
    private static readonly DateTime GregorianTransitionEnd = new(1582, 10, 14);

    public static bool IsInvalidGregorianTransitionDate(int year, int month, int day)
    {
        if (year != 1582) return false;
        if (month != 10) return false;
        return day >= 5 && day <= 14;
    }

    public static bool IsInvalidGregorianTransitionDate(DateTime dateTime)
    {
        return IsInvalidGregorianTransitionDate(dateTime.Year, dateTime.Month, dateTime.Day);
    }

    public static DateTime FixInvalidDate(int year, int month, int day, int hour, int minute, int second)
    {
        if (!IsInvalidGregorianTransitionDate(year, month, day))
        {
            return new DateTime(year, month, day, hour, minute, second);
        }

        var fixedDay = day + 10;
        return new DateTime(year, month, fixedDay, hour, minute, second);
    }

    public static bool TryParseSafeDateTime(int year, int month, int day, int hour, int minute, int second, out DateTime result)
    {
        result = DateTime.MinValue;

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;

        if (IsInvalidGregorianTransitionDate(year, month, day))
            return false;

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
        if (!IsInvalidGregorianTransitionDate(dateTime))
            return dateTime;

        return dateTime.AddDays(10);
    }

    public static DateTime AdjustDateAfterAddition(DateTime originalDate, double daysToAdd)
    {
        var result = originalDate.AddDays(daysToAdd);

        if (IsInvalidGregorianTransitionDate(result))
        {
            return new DateTime(result.Year, result.Month, 4, result.Hour, result.Minute, result.Second, result.Millisecond);
        }

        return result;
    }

    public static DateTime AdjustDateAfterAddition(DateTime originalDate, TimeSpan timeSpan)
    {
        var result = originalDate.Add(timeSpan);

        if (IsInvalidGregorianTransitionDate(result))
        {
            return new DateTime(result.Year, result.Month, 4, result.Hour, result.Minute, result.Second, result.Millisecond);
        }

        return result;
    }

    public static bool TryAdjustDateForGregorianTransition(int year, int month, int day, out int adjustedYear, out int adjustedMonth, out int adjustedDay)
    {
        adjustedYear = year;
        adjustedMonth = month;
        adjustedDay = day;

        if (!IsInvalidGregorianTransitionDate(year, month, day))
            return true;

        adjustedDay += 10;
        return true;
    }
}


