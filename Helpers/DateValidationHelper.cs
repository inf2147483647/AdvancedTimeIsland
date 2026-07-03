using System;

namespace AdvancedTimeIsland.Helpers;

public static class DateValidationHelper
{
    public static bool IsInvalidGregorianTransitionDate(int year, int month, int day)
    {
        return year == 1582 && month == 10 && day >= 5 && day <= 14;
    }

    public static bool IsInvalidGregorianTransitionDate(DateTime dateTime)
    {
        return IsInvalidGregorianTransitionDate(dateTime.Year, dateTime.Month, dateTime.Day);
    }

    public static DateTime FixInvalidDate(int year, int month, int day, int hour, int minute, int second)
    {
        if (!IsInvalidGregorianTransitionDate(year, month, day))
        {
            return LunarHelper.CreateSafeDateTime(year, month, day, hour, minute, second);
        }
        var fixedDay = day + 10;
        return LunarHelper.CreateSafeDateTime(year, month, fixedDay, hour, minute, second);
    }

    public static bool TryParseSafeDateTime(int year, int month, int day, int hour, int minute, int second, out DateTime result)
    {
        result = DateTime.MinValue;

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;

        if (IsInvalidGregorianTransitionDate(year, month, day))
            return false;

        var daysInMonth = Lunar.Util.SolarUtil.GetDaysOfMonth(year, month);
        if (day > daysInMonth)
            return false;

        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        result = LunarHelper.CreateSafeDateTime(year, month, day, hour, minute, second);
        return true;
    }

    public static DateTime AdjustDateForGregorianTransition(DateTime dateTime)
    {
        if (!IsInvalidGregorianTransitionDate(dateTime))
            return dateTime;

        return LunarHelper.SolarAddDays(dateTime, 10);
    }

    public static DateTime AdjustDateAfterAddition(DateTime originalDate, double daysToAdd)
    {
        return LunarHelper.SolarAddDays(originalDate, daysToAdd);
    }

    public static DateTime AdjustDateAfterAddition(DateTime originalDate, TimeSpan timeSpan)
    {
        return LunarHelper.SolarAddTimeSpan(originalDate, timeSpan);
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