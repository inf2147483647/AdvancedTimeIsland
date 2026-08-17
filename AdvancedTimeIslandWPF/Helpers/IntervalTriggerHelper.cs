using System;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 间隔触发辅助类
/// 用于计算时间范围内的间隔触发
/// </summary>
public static class IntervalTriggerHelper
{
    /// <summary>
    /// 检查当前时间是否在时间范围内，并且是间隔周期的整数倍点
    /// </summary>
    /// <param name="now">当前时间</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="interval">间隔值</param>
    /// <param name="intervalUnit">间隔单位</param>
    /// <returns>是否触发</returns>
    public static bool CheckIntervalTrigger(DateTime now, DateTime startTime, DateTime endTime, decimal interval, string intervalUnit)
    {
        if (interval <= 0)
            return false;

        if (now < startTime || now >= endTime)
            return false;

        var timeSpan = now - startTime;
        var intervalSpan = GetIntervalTimeSpan(interval, intervalUnit, startTime);

        if (intervalSpan <= TimeSpan.Zero)
            return false;

        var totalTicks = timeSpan.Ticks;
        var intervalTicks = intervalSpan.Ticks;

        if (intervalTicks == 0)
            return false;

        var remainder = totalTicks % intervalTicks;
        var tolerance = TimeSpan.FromSeconds(0.5).Ticks;

        return remainder < tolerance || Math.Abs(remainder - intervalTicks) < tolerance;
    }

    /// <summary>
    /// 获取间隔对应的TimeSpan
    /// </summary>
    /// <param name="interval">间隔值</param>
    /// <param name="intervalUnit">间隔单位</param>
    /// <param name="referenceTime">参考时间（用于计算月/年）</param>
    /// <returns>间隔时间跨度</returns>
    public static TimeSpan GetIntervalTimeSpan(decimal interval, string intervalUnit, DateTime referenceTime)
    {
        switch (intervalUnit)
        {
            case "Second":
                return TimeSpan.FromSeconds((double)interval);
            case "Minute":
                return TimeSpan.FromMinutes((double)interval);
            case "Hour":
                return TimeSpan.FromHours((double)interval);
            case "Day":
                return TimeSpan.FromDays((double)interval);
            case "Week":
                return TimeSpan.FromDays((double)(interval * 7));
            case "Month":
                var months = (int)Math.Floor(interval);
                var fractionalMonths = interval - months;
                var result = referenceTime.AddMonths(months) - referenceTime;
                if (fractionalMonths > 0)
                {
                    var daysInMonth = DateTime.DaysInMonth(referenceTime.Year, referenceTime.Month);
                    result = result.Add(TimeSpan.FromDays((double)(fractionalMonths * daysInMonth)));
                }
                return result;
            case "Year":
                var years = (int)Math.Floor(interval);
                var fractionalYears = interval - years;
                var yearResult = referenceTime.AddYears(years) - referenceTime;
                if (fractionalYears > 0)
                {
                    var daysInYear = DateTime.IsLeapYear(referenceTime.Year) ? 366 : 365;
                    yearResult = yearResult.Add(TimeSpan.FromDays((double)(fractionalYears * daysInYear)));
                }
                return yearResult;
            default:
                return TimeSpan.FromMinutes((double)interval);
        }
    }

    /// <summary>
    /// 解析精确时间字符串
    /// </summary>
    public static bool TryParseExactTime(string timeStr, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;

        var parts = timeStr.Split('-');
        if (parts.Length < 6) return false;

        if (!int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day) ||
            !int.TryParse(parts[3], out int hour) ||
            !int.TryParse(parts[4], out int minute) ||
            !int.TryParse(parts[5], out int second))
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析每年时间字符串 (MM-DD-hh-mm-ss)
    /// </summary>
    public static bool TryParseYearlyTime(string timeStr, int year, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;

        var parts = timeStr.Split('-');
        if (parts.Length < 5) return false;

        if (!int.TryParse(parts[0], out int month) ||
            !int.TryParse(parts[1], out int day) ||
            !int.TryParse(parts[2], out int hour) ||
            !int.TryParse(parts[3], out int minute) ||
            !int.TryParse(parts[4], out int second))
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析每月时间字符串 (DD-hh-mm-ss)
    /// </summary>
    public static bool TryParseMonthlyTime(string timeStr, int year, int month, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;

        var parts = timeStr.Split('-');
        if (parts.Length < 4) return false;

        if (!int.TryParse(parts[0], out int day) ||
            !int.TryParse(parts[1], out int hour) ||
            !int.TryParse(parts[2], out int minute) ||
            !int.TryParse(parts[3], out int second))
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析每天时间字符串 (hh-mm-ss)
    /// </summary>
    public static bool TryParseDailyTime(string timeStr, int year, int month, int day, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;

        var parts = timeStr.Split('-');
        if (parts.Length < 3) return false;

        if (!int.TryParse(parts[0], out int hour) ||
            !int.TryParse(parts[1], out int minute) ||
            !int.TryParse(parts[2], out int second))
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析每小时时间字符串 (mm-ss)
    /// </summary>
    public static bool TryParseHourlyTime(string timeStr, int year, int month, int day, int hour, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;

        var parts = timeStr.Split('-');
        if (parts.Length < 2) return false;

        if (!int.TryParse(parts[0], out int minute) ||
            !int.TryParse(parts[1], out int second))
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
