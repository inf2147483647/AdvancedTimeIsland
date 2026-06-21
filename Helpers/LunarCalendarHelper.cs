using System;
using System.Globalization;

namespace AdvancedTimeIsland.Helpers;

public static class LunarCalendarHelper
{
    private static readonly ChineseLunisolarCalendar _calendar = new();

    // 农历支持范围
    private static readonly DateTime _minLunarDate = new(1901, 2, 19);
    private static readonly DateTime _maxLunarDate = new(2101, 1, 28, 23, 59, 59);

    /// <summary>
    /// 判断日期是否在农历支持范围内
    /// </summary>
    public static bool IsDateSupported(DateTime date)
    {
        return date >= _minLunarDate && date <= _maxLunarDate;
    }

    private static readonly string[] _tiangan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    private static readonly string[] _dizhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    /// <summary>
    /// 公历日期转农历字符串
    /// </summary>
    public static string SolarToLunar(DateTime date)
    {
        try
        {
            if (!IsDateSupported(date))
            {
                return "农历不支持此日期范围(1901-02-19 ~ 2101-01-28)";
            }
            var lunarYear = _calendar.GetYear(date);
            var lunarMonth = _calendar.GetMonth(date);
            var lunarDay = _calendar.GetDayOfMonth(date);

            // 判断是否是闰月
            var leapMonth = _calendar.GetLeapMonth(lunarYear);
            var isLeapMonth = false;
            var actualMonth = lunarMonth;

            if (leapMonth > 0 && lunarMonth == leapMonth)
            {
                isLeapMonth = true;
                actualMonth = lunarMonth - 1;
            }
            else if (leapMonth > 0 && lunarMonth > leapMonth)
            {
                actualMonth = lunarMonth - 1;
            }

            var tg = _tiangan[(lunarYear - 4) % 10];
            var dz = _dizhi[(lunarYear - 4) % 12];
            var monthStr = isLeapMonth ? $"闰{actualMonth}月" : $"{actualMonth}月";
            var dayStr = GetLunarDayString(lunarDay);

            return $"{tg}{dz}年 {monthStr} {dayStr}";
        }
        catch
        {
            return "农历转换失败";
        }
    }

    /// <summary>
    /// 农历日期转公历日期
    /// </summary>
    public static DateTime? LunarToSolar(int lunarYear, int lunarMonth, bool isLeapMonth, int lunarDay, int hour, int minute, int second)
    {
        try
        {
            // 如果有闰月，需要调整月份值
            var leapMonth = _calendar.GetLeapMonth(lunarYear);
            var month = lunarMonth;

            if (leapMonth > 0)
            {
                if (isLeapMonth && lunarMonth == leapMonth - 1)
                {
                    month = leapMonth;
                }
                else if (lunarMonth >= leapMonth)
                {
                    month = lunarMonth + 1;
                }
            }

            return _calendar.ToDateTime(lunarYear, month, lunarDay, hour, minute, second, 0);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取农历日期的天干地支年份
    /// </summary>
    public static string GetLunarYearName(int lunarYear)
    {
        var tg = _tiangan[(lunarYear - 4) % 10];
        var dz = _dizhi[(lunarYear - 4) % 12];
        return $"{tg}{dz}";
    }

    /// <summary>
    /// 获取农历日期的天干
    /// </summary>
    public static string GetTiangan(int lunarYear)
    {
        return _tiangan[(lunarYear - 4) % 10];
    }

    /// <summary>
    /// 获取农历日期的地支
    /// </summary>
    public static string GetDizhi(int lunarYear)
    {
        return _dizhi[(lunarYear - 4) % 12];
    }

    /// <summary>
    /// 获取某年的闰月，0表示无闰月
    /// </summary>
    public static int GetLeapMonth(int lunarYear)
    {
        var leapMonth = _calendar.GetLeapMonth(lunarYear);
        return leapMonth > 0 ? leapMonth - 1 : 0;
    }

    /// <summary>
    /// 获取农历日期的年份数字
    /// </summary>
    public static int GetLunarYear(DateTime date)
    {
        if (!IsDateSupported(date)) return 0;
        try
        {
            return _calendar.GetYear(date);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取农历日期的月份
    /// </summary>
    public static int GetLunarMonth(DateTime date)
    {
        var lunarMonth = _calendar.GetMonth(date);
        var leapMonth = _calendar.GetLeapMonth(_calendar.GetYear(date));

        if (leapMonth > 0 && lunarMonth >= leapMonth)
        {
            return lunarMonth - 1;
        }
        return lunarMonth;
    }

    /// <summary>
    /// 获取农历日期的日期
    /// </summary>
    public static int GetLunarDay(DateTime date)
    {
        if (!IsDateSupported(date)) return 0;
        try
        {
            return _calendar.GetDayOfMonth(date);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 判断农历日期是否为闰月
    /// </summary>
    public static bool IsLeapMonth(DateTime date)
    {
        var lunarYear = _calendar.GetYear(date);
        var lunarMonth = _calendar.GetMonth(date);
        var leapMonth = _calendar.GetLeapMonth(lunarYear);
        return leapMonth > 0 && lunarMonth == leapMonth;
    }

    /// <summary>
    /// 获取农历日期的天数
    /// </summary>
    public static int GetDaysInLunarMonth(int lunarYear, int lunarMonth)
    {
        try
        {
            var leapMonth = _calendar.GetLeapMonth(lunarYear);
            var month = lunarMonth;

            if (leapMonth > 0 && lunarMonth >= leapMonth)
            {
                month = lunarMonth + 1;
            }

            return _calendar.GetDaysInMonth(lunarYear, month);
        }
        catch
        {
            return 30;
        }
    }

    private static string GetLunarDayString(int day)
    {
        if (day <= 0 || day > 30) return "";

        var prefix = new[] { "初", "十", "廿", "三" };
        var nums = new[] { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };

        if (day <= 10)
        {
            return $"{prefix[0]}{nums[day - 1]}";
        }
        else if (day < 20)
        {
            return $"{prefix[1]}{nums[day - 11]}";
        }
        else if (day == 20)
        {
            return "二十";
        }
        else if (day < 30)
        {
            return $"{prefix[2]}{nums[day - 21]}";
        }
        else if (day == 30)
        {
            return "三十";
        }

        return "";
    }
}
