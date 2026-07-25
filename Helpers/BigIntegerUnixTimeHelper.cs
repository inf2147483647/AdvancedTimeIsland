using System;
using System.Numerics;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 使用 BigInteger 进行时间戳转换，彻底避免精度损失
/// 直接从整数日期组件计算，不经过 double 中间值
/// </summary>
public static class BigIntegerUnixTimeHelper
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const long SecondsPerDay = 86400;
    private const long MillisecondsPerDay = 86400000;

    /// <summary>
    /// 将 DateTime 转换为 Unix 时间戳（毫秒级别 BigInteger）
    /// </summary>
    public static BigInteger ToUnixTimestampBigInteger(DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
            : dateTime.ToUniversalTime();

        return ToUnixTimestampUtcBigInteger(utcDateTime);
    }

    /// <summary>
    /// 将 UTC DateTime 转换为 Unix 时间戳（毫秒级别 BigInteger）
    /// 使用整数算法直接计算，避免 double 精度损失
    /// </summary>
    public static BigInteger ToUnixTimestampUtcBigInteger(DateTime utcDateTime)
    {
        if (UnixTimeHelper.IsNonExistentDate1582October(utcDateTime))
        {
            throw new ArgumentException("1582年10月5日至14日在历史上不存在，无法转换为时间戳");
        }

        // 计算从 0001-01-01 到指定日期的天数
        var daysFromZero = CalculateDaysFromZero(utcDateTime.Year, utcDateTime.Month, utcDateTime.Day);
        
        // 计算从 1970-01-01 到指定日期的天数
        var daysFromEpoch = daysFromZero - 719163; // 1970-01-01 距离 0001-01-01 的天数
        
        // 计算时间部分的毫秒数
        var timeMs = (BigInteger)utcDateTime.Hour * 3600000 + 
                     (BigInteger)utcDateTime.Minute * 60000 + 
                     (BigInteger)utcDateTime.Second * 1000 + 
                     utcDateTime.Millisecond;
        
        // 总毫秒数 = 天数 * 86400000 + 时间毫秒
        return daysFromEpoch * MillisecondsPerDay + timeMs;
    }

    /// <summary>
    /// 计算从 0001-01-01 到指定日期的天数（使用整数算法）
    /// </summary>
    private static BigInteger CalculateDaysFromZero(int year, int month, int day)
    {
        // 累加完整年份的天数
        BigInteger totalDays = 0;
        
        for (int y = 1; y < year; y++)
        {
            totalDays += IsLeapYear(y) ? 366 : 365;
        }
        
        // 累加当前年份中到指定月份前的天数
        int[] daysInMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        for (int m = 1; m < month; m++)
        {
            totalDays += daysInMonth[m];
            if (m == 2 && IsLeapYear(year))
                totalDays += 1;
        }
        
        // 加上当月天数
        totalDays += day;
        
        return totalDays;
    }

    /// <summary>
    /// 判断闰年
    /// </summary>
    private static bool IsLeapYear(int year)
    {
        if (year % 4 != 0) return false;
        if (year % 100 != 0) return true;
        return year % 400 == 0;
    }

    /// <summary>
    /// 将 Unix 时间戳（毫秒级别 BigInteger）转换为 DateTime
    /// </summary>
    public static DateTime FromUnixTimestampBigInteger(BigInteger timestampMs)
    {
        return FromUnixTimestampUtcBigInteger(timestampMs).ToLocalTime();
    }

    /// <summary>
    /// 将 Unix 时间戳（毫秒级别 BigInteger）转换为 UTC DateTime
    /// </summary>
    public static DateTime FromUnixTimestampUtcBigInteger(BigInteger timestampMs)
    {
        // 分离天数和时间部分
        var daysFromEpoch = timestampMs / MillisecondsPerDay;
        var remainingMs = timestampMs % MillisecondsPerDay;
        
        // 计算从 0001-01-01 开始的总天数
        var daysFromZero = daysFromEpoch + 719163;
        
        // 从天数计算年月日
        var (year, month, day) = CalculateDateFromDays(daysFromZero);
        
        // 从剩余毫秒计算时分秒
        var hours = (int)(remainingMs / 3600000);
        remainingMs %= 3600000;
        var minutes = (int)(remainingMs / 60000);
        remainingMs %= 60000;
        var seconds = (int)(remainingMs / 1000);
        var milliseconds = (int)(remainingMs % 1000);
        
        return new DateTime(year, month, day, hours, minutes, seconds, milliseconds, DateTimeKind.Utc);
    }

    /// <summary>
    /// 从天数计算年月日（从 0001-01-01 开始）
    /// </summary>
    private static (int Year, int Month, int Day) CalculateDateFromDays(BigInteger daysFromZero)
    {
        int year = 1;
        int month = 1;
        int day = (int)daysFromZero;
        
        // 找到年份
        while (true)
        {
            var daysInYear = IsLeapYear(year) ? 366 : 365;
            if (day <= daysInYear)
                break;
            day -= daysInYear;
            year++;
        }
        
        // 找到月份
        int[] daysInMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        for (int m = 1; m <= 12; m++)
        {
            var dim = daysInMonth[m];
            if (m == 2 && IsLeapYear(year))
                dim++;
            if (day <= dim)
            {
                month = m;
                break;
            }
            day -= dim;
        }
        
        return (year, month, day);
    }

    /// <summary>
    /// 获取当前 Unix 时间戳（毫秒级别 BigInteger）
    /// </summary>
    public static BigInteger GetCurrentUnixTimestampBigInteger()
    {
        return ToUnixTimestampBigInteger(DateTime.Now);
    }

    /// <summary>
    /// 获取当前 Unix 时间戳（毫秒级别 BigInteger）
    /// </summary>
    public static BigInteger GetCurrentUnixTimestampBigInteger(DateTime time)
    {
        return ToUnixTimestampBigInteger(time);
    }

    /// <summary>
    /// 将 DateTime 转换为 Unix 时间戳字符串（毫秒）
    /// </summary>
    public static string ToUnixTimestampString(DateTime dateTime)
    {
        return ToUnixTimestampBigInteger(dateTime).ToString();
    }

    /// <summary>
    /// 将 Unix 时间戳字符串转换为 DateTime
    /// </summary>
    public static bool TryParseUnixTimestampString(string timestampStr, out DateTime result)
    {
        result = DateTime.MinValue;
        
        if (!BigInteger.TryParse(timestampStr, out var timestamp))
            return false;
            
        result = FromUnixTimestampBigInteger(timestamp);
        return true;
    }

    /// <summary>
    /// 计算两个时间之间的差值（毫秒）
    /// </summary>
    public static BigInteger GetTimeDifference(DateTime start, DateTime end)
    {
        var startTs = ToUnixTimestampBigInteger(start);
        var endTs = ToUnixTimestampBigInteger(end);
        return endTs - startTs;
    }

    /// <summary>
    /// 在 DateTime 上添加指定的毫秒数
    /// </summary>
    public static DateTime AddMilliseconds(DateTime dateTime, BigInteger milliseconds)
    {
        var currentTs = ToUnixTimestampBigInteger(dateTime);
        var newTs = currentTs + milliseconds;
        return FromUnixTimestampBigInteger(newTs);
    }

    /// <summary>
    /// 在 DateTime 上添加指定的秒数
    /// </summary>
    public static DateTime AddSeconds(DateTime dateTime, BigInteger seconds)
    {
        return AddMilliseconds(dateTime, seconds * 1000);
    }

    /// <summary>
    /// 在 DateTime 上添加指定的天数
    /// </summary>
    public static DateTime AddDays(DateTime dateTime, BigInteger days)
    {
        return AddSeconds(dateTime, days * SecondsPerDay);
    }

    /// <summary>
    /// 计算两个日期之间的天数（使用 BigInteger 精确计算）
    /// </summary>
    public static BigInteger DaysBetween(DateTime start, DateTime end)
    {
        var diff = GetTimeDifference(start, end);
        return diff / MillisecondsPerDay;
    }
}
