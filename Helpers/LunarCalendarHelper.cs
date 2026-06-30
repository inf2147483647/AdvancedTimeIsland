using System;
using System.Collections.Generic;
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
    /// 获取天干索引（0-9）
    /// </summary>
    public static int GetTianganIndex(string tiangan)
    {
        for (int i = 0; i < _tiangan.Length; i++)
        {
            if (_tiangan[i] == tiangan)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 获取地支索引（0-11）
    /// </summary>
    public static int GetDizhiIndex(string dizhi)
    {
        for (int i = 0; i < _dizhi.Length; i++)
        {
            if (_dizhi[i] == dizhi)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 由天干地支计算农历年份（返回第一个匹配的年份，范围1901-2100）
    /// </summary>
    public static int GetLunarYearFromTianganDizhi(string tiangan, string dizhi)
    {
        int tgIndex = GetTianganIndex(tiangan);
        int dzIndex = GetDizhiIndex(dizhi);
        
        if (tgIndex < 0 || dzIndex < 0)
            return 0;

        int baseYear = 4 + tgIndex;
        while ((baseYear - 4) % 12 != dzIndex)
        {
            baseYear += 10;
        }

        int result = baseYear;
        while (result < 1901)
            result += 60;
        while (result > 2100)
            result -= 60;

        return result;
    }

    /// <summary>
    /// 获取所有固定年份范围列表
    /// </summary>
    public static string[] GetAllYearRanges()
    {
        return new[] { "1901-1923", "1924-1983", "1984-2043", "2044-2101" };
    }

    /// <summary>
    /// 根据年份范围和天干地支计算农历年份
    /// </summary>
    public static int GetLunarYearFromRangeAndTianganDizhi(string yearRange, string tiangan, string dizhi)
    {
        int tgIndex = GetTianganIndex(tiangan);
        int dzIndex = GetDizhiIndex(dizhi);
        
        if (tgIndex < 0 || dzIndex < 0)
            return 0;

        if (!ParseYearRange(yearRange, out int startYear, out int endYear))
            return 0;

        var baseYear = 4;
        var yearOffset = 0;
        while ((baseYear + yearOffset - 4) % 10 != tgIndex ||
               (baseYear + yearOffset - 4) % 12 != dzIndex)
        {
            yearOffset++;
            if (yearOffset > 60) return 0;
        }

        var baseLunarYearVal = baseYear + yearOffset;
        var lunarYearVal = baseLunarYearVal;

        while (lunarYearVal < startYear)
        {
            lunarYearVal += 60;
        }
        if (lunarYearVal > endYear)
        {
            lunarYearVal -= 60;
        }

        return lunarYearVal;
    }

    /// <summary>
    /// 由天干地支获取所有匹配的农历年份（范围1901-2100）
    /// </summary>
    public static int[] GetAllLunarYearsFromTianganDizhi(string tiangan, string dizhi)
    {
        int tgIndex = GetTianganIndex(tiangan);
        int dzIndex = GetDizhiIndex(dizhi);
        
        if (tgIndex < 0 || dzIndex < 0)
            return Array.Empty<int>();

        var years = new List<int>();
        
        for (int year = 1901; year <= 2100; year++)
        {
            if ((year - 4) % 10 == tgIndex && (year - 4) % 12 == dzIndex)
            {
                years.Add(year);
            }
        }

        return years.ToArray();
    }

    /// <summary>
    /// 由天干地支获取年份范围列表（每个范围代表一个完整周期或部分周期）
    /// 返回格式如："1901-1923", "1924-1983", "1984-2043", "2044-2101"
    /// </summary>
    public static string[] GetYearRangesFromTianganDizhi(string tiangan, string dizhi)
    {
        var years = GetAllLunarYearsFromTianganDizhi(tiangan, dizhi);
        if (years.Length == 0)
            return Array.Empty<string>();

        var ranges = new List<string>();
        
        for (int i = 0; i < years.Length; i++)
        {
            int startYear = years[i];
            int endYear;
            
            if (i < years.Length - 1)
            {
                endYear = years[i + 1] - 1;
            }
            else
            {
                endYear = 2101;
            }
            
            ranges.Add($"{startYear}-{endYear}");
        }

        return ranges.ToArray();
    }

    /// <summary>
    /// 解析年份范围字符串，返回开始和结束年份
    /// </summary>
    public static bool ParseYearRange(string range, out int startYear, out int endYear)
    {
        startYear = 0;
        endYear = 0;
        
        if (string.IsNullOrWhiteSpace(range))
            return false;

        var parts = range.Split('-');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out startYear) && int.TryParse(parts[1], out endYear);
    }

    /// <summary>
    /// 获取所有天干列表
    /// </summary>
    public static string[] GetAllTiangan()
    {
        return _tiangan;
    }

    /// <summary>
    /// 获取所有地支列表
    /// </summary>
    public static string[] GetAllDizhi()
    {
        return _dizhi;
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
        if (!IsDateSupported(date)) return 0;
        try
        {
            var lunarMonth = _calendar.GetMonth(date);
            var leapMonth = _calendar.GetLeapMonth(_calendar.GetYear(date));

            if (leapMonth > 0 && lunarMonth >= leapMonth)
            {
                return lunarMonth - 1;
            }
            return lunarMonth;
        }
        catch
        {
            return 0;
        }
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
        if (!IsDateSupported(date)) return false;
        try
        {
            var lunarYear = _calendar.GetYear(date);
            var lunarMonth = _calendar.GetMonth(date);
            var leapMonth = _calendar.GetLeapMonth(lunarYear);
            return leapMonth > 0 && lunarMonth == leapMonth;
        }
        catch
        {
            return false;
        }
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



