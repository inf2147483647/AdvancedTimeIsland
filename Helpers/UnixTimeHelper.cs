﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;

namespace AdvancedTimeIsland.Helpers;

public static class UnixTimeHelper
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime JulianCalendarLastDay = new(1582, 10, 4, 23, 59, 59, DateTimeKind.Utc);

    private static readonly DateTime GregorianCalendarFirstDay = new(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc);

    private const double JulianGregorianOffsetDays = 10.0;

    private static readonly TimeSpan JulianGregorianOffset = TimeSpan.FromDays(JulianGregorianOffsetDays);

    public static bool IsNonExistentDate1582October(DateTime dateTime)
    {
        var local = dateTime.ToLocalTime();
        return local.Year == 1582 && local.Month == 10 && local.Day >= 5 && local.Day <= 14;
    }

    private static bool IsJulianLeapYear(int year)
    {
        return year > 0 && year % 4 == 0;
    }

    private static bool IsGregorianLeapYear(int year)
    {
        if (year <= 0) return false;
        if (year % 400 == 0) return true;
        if (year % 100 == 0) return false;
        return year % 4 == 0;
    }

    private static int GetJulianDaysInFebruary(int year)
    {
        return IsJulianLeapYear(year) ? 29 : 28;
    }

    private static int GetJulianDaysInMonth(int year, int month)
    {
        if (month == 2)
            return GetJulianDaysInFebruary(year);

        if (month == 1 || month == 3 || month == 5 || month == 7 ||
            month == 8 || month == 10 || month == 12)
            return 31;

        return 30;
    }

    public static bool IsValidJulianDate(int year, int month, int day)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;

        return day <= GetJulianDaysInMonth(year, month);
    }

    private static DateTime AdjustDateForJulianGregorianDiff(DateTime dateTime)
    {
        if (dateTime.Year >= 1582)
            return dateTime;

        var year = dateTime.Year;
        var month = dateTime.Month;
        var day = dateTime.Day;

        if (month == 2 && day == 29)
        {
            if (IsJulianLeapYear(year) && !IsGregorianLeapYear(year))
            {
                return new DateTime(year, 3, 1, dateTime.Hour, dateTime.Minute, dateTime.Second,
                    dateTime.Millisecond, dateTime.Kind);
            }
        }

        return dateTime;
    }

    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return (long)ToUnixTimestampDouble(dateTime);
    }

    public static double ToUnixTimestampDouble(DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
            : dateTime.ToUniversalTime();

        // 如果UTC时间是1582-10-4或更早（但年份>=1582），需要加10天偏移
        // 因为本地时间1582-10-4实际上对应的是UTC删除日期范围内的某个时间
        if (utcDateTime.Year == 1582 && utcDateTime.Month == 10 && utcDateTime.Day <= 4)
        {
            return (utcDateTime.Add(JulianGregorianOffset) - UnixEpoch).TotalSeconds;
        }

        // 如果UTC时间在1582年之前，需要处理儒略历/格里高利历的差异
        DateTime adjustedUtcTime;
        if (utcDateTime.Year < 1582)
        {
            adjustedUtcTime = utcDateTime.Subtract(JulianGregorianOffset);
            adjustedUtcTime = AdjustDateForJulianGregorianDiff(adjustedUtcTime);
            return (adjustedUtcTime - UnixEpoch).TotalSeconds;
        }

        return (utcDateTime - UnixEpoch).TotalSeconds;
    }

    public static double ToUnixTimestampUtcDouble(DateTime utcDateTime)
    {
        // 如果UTC时间是1582-10-4或更早（但年份>=1582），需要加10天偏移
        if (utcDateTime.Year == 1582 && utcDateTime.Month == 10 && utcDateTime.Day <= 4)
        {
            return (utcDateTime.Add(JulianGregorianOffset) - UnixEpoch).TotalSeconds;
        }

        DateTime adjustedUtcTime;
        if (utcDateTime.Year < 1582)
        {
            adjustedUtcTime = utcDateTime.Subtract(JulianGregorianOffset);
            adjustedUtcTime = AdjustDateForJulianGregorianDiff(adjustedUtcTime);
            return (adjustedUtcTime - UnixEpoch).TotalSeconds;
        }

        return (utcDateTime - UnixEpoch).TotalSeconds;
    }

    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return FromUnixTimestampUtc(timestamp).ToLocalTime();
    }

    public static DateTime FromUnixTimestamp(double timestamp)
    {
        return FromUnixTimestampUtc((long)timestamp).ToLocalTime();
    }

    public static DateTime FromUnixTimestampUtc(long timestamp)
    {
        var utcTime = UnixEpoch.AddSeconds(timestamp);
        var localTime = utcTime.ToLocalTime();

        // 如果本地时间落在1582年10月5-14日范围内（这10天不存在），映射到1582-10-4
        if (IsNonExistentDate1582October(localTime))
        {
            return new DateTime(1582, 10, 4, utcTime.Hour, utcTime.Minute, utcTime.Second,
                utcTime.Millisecond, DateTimeKind.Utc);
        }

        // 如果UTC时间在1582年之前，需要处理儒略历/格里高利历的差异
        DateTime adjustedUtcTime;
        if (utcTime.Year < 1582)
        {
            adjustedUtcTime = utcTime.Subtract(JulianGregorianOffset);
            adjustedUtcTime = AdjustDateForJulianGregorianDiff(adjustedUtcTime);
            return adjustedUtcTime;
        }

        return utcTime;
    }

    public static long GetCurrentUnixTimestamp()
    {
        return ToUnixTimestamp(DateTime.Now);
    }

    public static double GetCurrentUnixTimestampDouble()
    {
        return ToUnixTimestampDouble(DateTime.Now);
    }

    public static bool TryParseExactTime(string timeString, out DateTime result)
    {
        result = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(timeString))
            return false;

        var formats = new[]
        {
            "yyyy-MM-dd-HH-mm-ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss"
        };

        return DateTime.TryParseExact(timeString, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out result);
    }

    public static string ToExactTimeString(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
    }

    public static bool TryParseExactTimeToUnixTimestamp(string timeString, out long timestamp)
    {
        timestamp = 0;

        if (!TryParseExactTime(timeString, out var dateTime))
            return false;

        if (IsNonExistentDate1582October(dateTime))
            return false;

        timestamp = ToUnixTimestamp(dateTime);
        return true;
    }
}
