using System;
using System.Collections.Generic;
using Lunar;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 农历日历辅助类，基于 lunar-csharp 库实现。
/// 支持范围：0001-9999年（由 lunar-csharp 提供）。
/// 原 ChineseLunisolarCalendar 仅支持 1901-2101，现已替换。
/// </summary>
public static class LunarCalendarHelper
{
    // ===== ChineseLunisolarCalendar 已弃用，改用 lunar-csharp =====
    // private static readonly ChineseLunisolarCalendar _calendar = new();

    // 农历支持范围 - 扩展到 0001-9999 年（lunar-csharp 支持）
    private static readonly DateTime _minLunarDate = new(1, 1, 1);
    private static readonly DateTime _maxLunarDate = new(9999, 12, 31, 23, 59, 59);

    /// <summary>
    /// 判断日期是否在农历支持范围内
    /// </summary>
    public static bool IsDateSupported(DateTime date)
    {
        // lunar-csharp 支持 0001-9999 年
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
                return "农历不支持此日期范围(0001-9999)";
            }

            var solar = Solar.FromDate(date);
            var lunar = solar.Lunar;

            return $"{lunar.YearGan}{lunar.YearZhi}年 {lunar.MonthInChinese} {lunar.DayInChinese}";
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
            // lunar-csharp 中闰月用负数表示
            var month = isLeapMonth ? -lunarMonth : lunarMonth;
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, month, lunarDay, hour, minute, second);
            var solar = lunar.Solar;
            return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
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
        try
        {
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, 1, 1);
            return lunar.YearGan + lunar.YearZhi;
        }
        catch
        {
            var tg = _tiangan[(lunarYear - 4) % 10];
            var dz = _dizhi[(lunarYear - 4) % 12];
            return $"{tg}{dz}";
        }
    }

    /// <summary>
    /// 获取农历日期的天干
    /// </summary>
    public static string GetTiangan(int lunarYear)
    {
        try
        {
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, 1, 1);
            return lunar.YearGan;
        }
        catch
        {
            return _tiangan[(lunarYear - 4) % 10];
        }
    }

    /// <summary>
    /// 获取农历日期的地支
    /// </summary>
    public static string GetDizhi(int lunarYear)
    {
        try
        {
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, 1, 1);
            return lunar.YearZhi;
        }
        catch
        {
            return _dizhi[(lunarYear - 4) % 12];
        }
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
    /// 由天干地支计算农历年份（返回第一个匹配的年份，范围1-9999）
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
        while (result < 1)
            result += 60;
        while (result > 9999)
            result -= 60;

        return result;
    }

    /// <summary>
    /// 获取所有固定年份范围列表（基于甲子年，扩展到0001-9999）
    /// 甲子年：4, 64, 124, 184, ..., 9964（每60年一个周期）
    /// 范围格式：1-3, 4-63, 64-123, ..., 9904-9963, 9964-9999
    /// </summary>
    public static string[] GetAllYearRanges()
    {
        var ranges = new List<string>();
        
        ranges.Add("0001-0003");
        
        for (int jiaziYear = 4; jiaziYear <= 9964; jiaziYear += 60)
        {
            int endYear = jiaziYear + 59;
            if (endYear > 9999)
                endYear = 9999;
            ranges.Add($"{jiaziYear:D4}-{endYear:D4}");
        }
        
        return ranges.ToArray();
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
    /// 由天干地支获取所有匹配的农历年份（范围0001-9999）
    /// </summary>
    public static int[] GetAllLunarYearsFromTianganDizhi(string tiangan, string dizhi)
    {
        int tgIndex = GetTianganIndex(tiangan);
        int dzIndex = GetDizhiIndex(dizhi);

        if (tgIndex < 0 || dzIndex < 0)
            return Array.Empty<int>();

        var years = new List<int>();

        for (int year = 1; year <= 9999; year++)
        {
            if ((year - 4) % 10 == tgIndex && (year - 4) % 12 == dzIndex)
            {
                years.Add(year);
            }
        }

        return years.ToArray();
    }

    /// <summary>
    /// 由天干地支获取年份范围列表
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
                endYear = 9999;
            }

            ranges.Add($"{startYear:D4}-{endYear:D4}");
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
        try
        {
            // 遍历1-12月，检查哪个月是闰月（负数）
            for (int m = 1; m <= 12; m++)
            {
                var lunar = Lunar.Lunar.FromYmdHms(lunarYear, m, 1);
                if (lunar.Month < 0)
                    return Math.Abs(lunar.Month);
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取农历日期的年份数字
    /// </summary>
    public static int GetLunarYear(DateTime date)
    {
        if (!IsDateSupported(date)) return 0;
        try
        {
            var solar = Solar.FromDate(date);
            var lunar = solar.Lunar;
            return lunar.Year;
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
            var solar = Solar.FromDate(date);
            var lunar = solar.Lunar;
            return Math.Abs(lunar.Month);
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
            var solar = Solar.FromDate(date);
            var lunar = solar.Lunar;
            return lunar.Day;
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
            var solar = Solar.FromDate(date);
            var lunar = solar.Lunar;
            return lunar.Month < 0;
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
            // 尝试创建30日，如果成功则该月有30天，否则29天
            try
            {
                Lunar.Lunar.FromYmdHms(lunarYear, lunarMonth, 30);
                return 30;
            }
            catch
            {
                return 29;
            }
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
