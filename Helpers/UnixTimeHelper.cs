using System;
using Lunar;

namespace AdvancedTimeIsland.Helpers;

public static class UnixTimeHelper
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const double JulianDayUnixEpoch = 2440587.5;

    public static bool IsNonExistentDate1582October(DateTime dateTime)
    {
        return dateTime.Year == 1582 && dateTime.Month == 10 && dateTime.Day >= 5 && dateTime.Day <= 14;
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

        return ToUnixTimestampUtcDouble(utcDateTime);
    }

    public static double ToUnixTimestampUtcDouble(DateTime utcDateTime)
    {
        if (IsNonExistentDate1582October(utcDateTime))
        {
            throw new ArgumentException("1582年10月5日至14日在历史上不存在，无法转换为时间戳");
        }

        // 秒级部分：Solar.JulianDay 只精确到秒，且儒略日换算存在 ~1e-6 秒的浮点误差。
        // 取整到最近的整秒，消除“整秒却得到 x.999998”的问题（否则 ToUnixTimestamp 截断后会整整差 1 秒）。
        var secondPrecision = new DateTime(
            utcDateTime.Year, utcDateTime.Month, utcDateTime.Day,
            utcDateTime.Hour, utcDateTime.Minute, utcDateTime.Second, utcDateTime.Kind);
        var solar = Solar.FromDate(secondPrecision);
        var wholeSeconds = Math.Round((solar.JulianDay - JulianDayUnixEpoch) * 86400);

        // 亚秒部分（毫秒/更低精度）：Solar.FromDate 不保留，需单独补回，否则设置页宣称的“支持小数”会失效。
        var subSecond = (utcDateTime.Ticks % TimeSpan.TicksPerSecond) / (double)TimeSpan.TicksPerSecond;
        return wholeSeconds + subSecond;
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
        var julianDay = timestamp / 86400.0 + JulianDayUnixEpoch;
        var solar = Solar.FromJulianDay(julianDay);
        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second, DateTimeKind.Utc);
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

        if (IsNonExistentDate1582October(dateTime))
            return false;

        timestamp = ToUnixTimestamp(dateTime);
        return true;
    }
}