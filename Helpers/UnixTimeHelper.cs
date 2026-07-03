using System;

namespace AdvancedTimeIsland.Helpers;

public static class UnixTimeHelper
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ===== 以下1582年偏移相关代码已注释掉 =====
    // lunar-csharp 的 Solar 类已处理1582年历法改革（消失的10天），无需手动偏移
    // private static readonly DateTime JulianCalendarLastDay = new(1582, 10, 4, 23, 59, 59, DateTimeKind.Utc);
    // private static readonly DateTime GregorianCalendarFirstDay = new(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc);
    // private const double JulianGregorianOffsetDays = 10.0;
    // private static readonly TimeSpan JulianGregorianOffset = TimeSpan.FromDays(JulianGregorianOffsetDays);

    public static bool IsNonExistentDate1582October(DateTime dateTime)
    {
        // lunar-csharp 已处理1582年历法改革，此方法保留但始终返回false
        // var local = dateTime.ToLocalTime();
        // return local.Year == 1582 && local.Month == 10 && local.Day >= 5 && local.Day <= 14;
        return false;
    }

    // ===== 以下儒略历相关方法已注释掉，lunar-csharp 的 Solar 类已内置处理 =====
    // private static bool IsJulianLeapYear(int year) { return year > 0 && year % 4 == 0; }
    // private static bool IsGregorianLeapYear(int year) { ... }
    // private static int GetJulianDaysInFebruary(int year) { ... }
    // private static int GetJulianDaysInMonth(int year, int month) { ... }
    // public static bool IsValidJulianDate(int year, int month, int day) { ... }
    // private static DateTime AdjustDateForJulianGregorianDiff(DateTime dateTime) { ... }

    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return (long)ToUnixTimestampDouble(dateTime);
    }

    public static double ToUnixTimestampDouble(DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
            : dateTime.ToUniversalTime();

        // 1582年偏移代码已注释掉 - lunar-csharp 的 Solar 类已处理历法改革
        // if (utcDateTime.Year == 1582 && utcDateTime.Month == 10 && utcDateTime.Day <= 4) { ... }
        // if (utcDateTime.Year < 1582) { ... }

        return (utcDateTime - UnixEpoch).TotalSeconds;
    }

    public static double ToUnixTimestampUtcDouble(DateTime utcDateTime)
    {
        // 1582年偏移代码已注释掉 - lunar-csharp 的 Solar 类已处理历法改革
        // if (utcDateTime.Year == 1582 && utcDateTime.Month == 10 && utcDateTime.Day <= 4) { ... }
        // if (utcDateTime.Year < 1582) { ... }

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

        // 1582年偏移代码已注释掉 - lunar-csharp 的 Solar 类已处理历法改革
        // var localTime = utcTime.ToLocalTime();
        // if (IsNonExistentDate1582October(localTime)) { ... }
        // if (utcTime.Year < 1582) { ... }

        return utcTime;
    }

    public static long GetCurrentUnixTimestamp()
    {
        return ToUnixTimestamp(DateTime.Now);
    }

    public static long GetCurrentUnixTimestamp(DateTime time)
    {
        return ToUnixTimestamp(time);
    }

    public static double GetCurrentUnixTimestampDouble()
    {
        return ToUnixTimestampDouble(DateTime.Now);
    }

    public static double GetCurrentUnixTimestampDouble(DateTime time)
    {
        return ToUnixTimestampDouble(time);
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

        // 1582年检查已注释掉 - lunar-csharp 已处理
        // if (IsNonExistentDate1582October(dateTime)) return false;

        timestamp = ToUnixTimestamp(dateTime);
        return true;
    }
}
