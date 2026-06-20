﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// Unix时间戳与DateTime互转工具类
/// 注意：Unix时间戳单位为秒
/// </summary>
public static class UnixTimeHelper
{
    /// <summary>
    /// Unix纪元时间 (1970-01-01 00:00:00 UTC)
    /// </summary>
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 将DateTime转换为Unix时间戳（秒）
    /// </summary>
    /// <param name="dateTime">要转换的时间</param>
    /// <returns>Unix时间戳（秒）</returns>
    public static long ToUnixTimestamp(DateTime dateTime)
    {
        // 转换为UTC时间
        var utcTime = dateTime.ToUniversalTime();
        // 计算与Unix纪元的差值（秒）
        return (long)(utcTime - UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// 将DateTime转换为Unix时间戳（秒，带小数）
    /// </summary>
    /// <param name="dateTime">要转换的时间</param>
    /// <returns>Unix时间戳（秒，带毫秒精度）</returns>
    public static double ToUnixTimestampDouble(DateTime dateTime)
    {
        var utcTime = dateTime.ToUniversalTime();
        return (utcTime - UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// 将Unix时间戳（秒）转换为DateTime
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>DateTime（本地时间）</returns>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return UnixEpoch.AddSeconds(timestamp).ToLocalTime();
    }

    /// <summary>
    /// 将Unix时间戳（秒，带小数）转换为DateTime
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒，带小数）</param>
    /// <returns>DateTime（本地时间）</returns>
    public static DateTime FromUnixTimestamp(double timestamp)
    {
        return UnixEpoch.AddSeconds(timestamp).ToLocalTime();
    }

    /// <summary>
    /// 将Unix时间戳（秒）转换为DateTime（UTC时间）
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>DateTime（UTC时间）</returns>
    public static DateTime FromUnixTimestampUtc(long timestamp)
    {
        return UnixEpoch.AddSeconds(timestamp);
    }

    /// <summary>
    /// 获取当前Unix时间戳（秒）
    /// </summary>
    /// <returns>当前Unix时间戳（秒）</returns>
    public static long GetCurrentUnixTimestamp()
    {
        return ToUnixTimestamp(DateTime.Now);
    }

    /// <summary>
    /// 获取当前Unix时间戳（秒，带毫秒精度）
    /// </summary>
    /// <returns>当前Unix时间戳（秒，带小数）</returns>
    public static double GetCurrentUnixTimestampDouble()
    {
        return ToUnixTimestampDouble(DateTime.Now);
    }

    /// <summary>
    /// 解析精确时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    /// <param name="timeString">时间字符串</param>
    /// <param name="result">解析结果</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParseExactTime(string timeString, out DateTime result)
    {
        result = DateTime.MinValue;
        
        if (string.IsNullOrWhiteSpace(timeString))
            return false;

        // 支持格式：YYYY-MM-DD-hh-mm-ss
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

    /// <summary>
    /// 将DateTime格式化为精确时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    /// <param name="dateTime">要格式化的时间</param>
    /// <returns>格式化后的字符串</returns>
    public static string ToExactTimeString(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
    }

    /// <summary>
    /// 将精确时间字符串转换为Unix时间戳
    /// </summary>
    /// <param name="timeString">时间字符串 (YYYY-MM-DD-hh-mm-ss)</param>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>是否转换成功</returns>
    public static bool TryParseExactTimeToUnixTimestamp(string timeString, out long timestamp)
    {
        timestamp = 0;
        
        if (!TryParseExactTime(timeString, out var dateTime))
            return false;

        timestamp = ToUnixTimestamp(dateTime);
        return true;
    }
}
