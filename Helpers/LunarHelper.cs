using System;
using Lunar;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// lunar-csharp 包装类，提供公历(Solar)和农历(Lunar)的统一访问入口。
/// 支持范围：0001-9999年，已处理1582年历法改革（消失的10天）。
/// </summary>
public static class LunarHelper
{
    /// <summary>
    /// 儒略日常量：UTC 1970-01-01 00:00:00 对应的儒略日
    /// </summary>
    public const double JulianDayUnixEpoch = 2440587.5;

    #region 公历(Solar)

    /// <summary>
    /// 从DateTime创建Solar对象
    /// </summary>
    public static Solar FromDateTime(DateTime dateTime)
    {
        return Solar.FromDate(dateTime);
    }

    /// <summary>
    /// 从年月日时分秒创建Solar对象
    /// </summary>
    public static Solar FromYmdHms(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
    {
        return Solar.FromYmdHms(year, month, day, hour, minute, second);
    }

    /// <summary>
    /// Solar转DateTime
    /// </summary>
    public static DateTime ToDateTime(Solar solar)
    {
        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    #endregion

    #region 农历(Lunar)

    /// <summary>
    /// 从公历DateTime获取农历对象
    /// </summary>
    public static LunarDate SolarToLunar(DateTime solarDate)
    {
        var solar = Solar.FromDate(solarDate);
        var lunar = solar.Lunar;
        return new LunarDate(lunar);
    }

    /// <summary>
    /// 从农历年月日时分秒获取Solar DateTime
    /// </summary>
    public static DateTime? LunarToSolar(int lunarYear, int lunarMonth, int lunarDay, bool isLeapMonth, int hour = 0, int minute = 0, int second = 0)
    {
        try
        {
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
    /// 获取农历年份的天干地支
    /// </summary>
    public static string GetYearGanZhi(int lunarYear)
    {
        try
        {
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, 1, 1);
            return lunar.YearGan + lunar.YearZhi;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 获取生肖
    /// </summary>
    public static string GetShengXiao(int lunarYear)
    {
        try
        {
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, 1, 1);
            return lunar.YearShengXiao;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 获取星座（从公历日期）
    /// </summary>
    public static string GetXingZuo(DateTime solarDate)
    {
        var solar = Solar.FromDate(solarDate);
        return solar.XingZuo;
    }

    /// <summary>
    /// 获取节气
    /// </summary>
    public static string? GetJieQi(DateTime solarDate)
    {
        var solar = Solar.FromDate(solarDate);
        var jq = solar.Lunar.JieQi;
        return string.IsNullOrEmpty(jq) ? null : jq;
    }

    /// <summary>
    /// 获取节日列表
    /// </summary>
    public static string[] GetFestivals(DateTime solarDate)
    {
        return GetFestivals(solarDate, true, true, true);
    }

    /// <summary>
    /// 获取节日列表（支持分类筛选）
    /// </summary>
    public static string[] GetFestivals(DateTime solarDate, bool includeInternational, bool includeTraditional, bool includeRed)
    {
        var festivalNames = new System.Collections.Generic.HashSet<string>();

        if (includeInternational || includeTraditional)
        {
            var solar = Solar.FromDate(solarDate);
            var festivals = solar.Festivals;
            if (festivals != null && festivals.Count > 0)
            {
                foreach (var f in festivals)
                    festivalNames.Add(f);
            }
            var lunarFestivals = solar.Lunar.Festivals;
            if (lunarFestivals != null && lunarFestivals.Count > 0)
            {
                foreach (var f in lunarFestivals)
                    festivalNames.Add(f);
            }
        }

        if (includeRed)
        {
            var redFestivals = GetRedFestivals(solarDate);
            foreach (var f in redFestivals)
                festivalNames.Add(f);
        }

        return festivalNames.ToArray();
    }

    /// <summary>
    /// 获取红色节日
    /// </summary>
    private static string[] GetRedFestivals(DateTime date)
    {
        var festivals = new System.Collections.Generic.List<string>();

        if (date.Month == 2 && date.Day == 7) festivals.Add("二七纪念日");
        if (date.Month == 3 && date.Day == 5) festivals.Add("学雷锋纪念日");
        if (date.Month == 5 && date.Day == 4) festivals.Add("五四青年节");
        if (date.Month == 7 && date.Day == 1) festivals.Add("七一建党节");
        if (date.Month == 8 && date.Day == 1) festivals.Add("八一建军节");
        if (date.Month == 9 && date.Day == 3) festivals.Add("中国人民抗日战争胜利纪念日");
        if (date.Month == 9 && date.Day == 18) festivals.Add("九一八事变纪念日");
        if (date.Month == 9 && date.Day == 30) festivals.Add("烈士纪念日");
        if (date.Month == 10 && date.Day == 1) festivals.Add("十一国庆节");
        if (date.Month == 10 && date.Day == 22) festivals.Add("中国工农红军长征胜利纪念日");
        if (date.Month == 12 && date.Day == 13) festivals.Add("南京大屠杀死难者国家公祭日");

        return festivals.ToArray();
    }

    /// <summary>
    /// 获取今日宜做事项
    /// </summary>
    public static string[] GetDayYi(DateTime solarDate)
    {
        var solar = Solar.FromDate(solarDate);
        var yi = solar.Lunar.DayYi;
        return yi == null || yi.Count == 0 ? Array.Empty<string>() : yi.ToArray();
    }

    /// <summary>
    /// 获取今日忌做事项
    /// </summary>
    public static string[] GetDayJi(DateTime solarDate)
    {
        var solar = Solar.FromDate(solarDate);
        var ji = solar.Lunar.DayJi;
        return ji == null || ji.Count == 0 ? Array.Empty<string>() : ji.ToArray();
    }

    /// <summary>
    /// 获取当前生肖（从公历日期）
    /// </summary>
    public static string GetCurrentShengXiao(DateTime solarDate)
    {
        var solar = Solar.FromDate(solarDate);
        return solar.Lunar.YearShengXiao;
    }

    #endregion

    #region 日期算术（完全基于lunar-csharp Solar，自动处理1582年消失的10天和儒略历）

    /// <summary>
    /// 添加小时数（完全基于Solar arithmetic）
    /// </summary>
    public static DateTime SolarAddHours(DateTime dateTime, double hours)
    {
        var solar = Solar.FromDate(dateTime);
        var julianDay = solar.JulianDay + hours / 24.0;
        solar = Solar.FromJulianDay(julianDay);

        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    /// <summary>
    /// 添加天数（完全基于Solar arithmetic）
    /// </summary>
    public static DateTime SolarAddDays(DateTime dateTime, double days)
    {
        var solar = Solar.FromDate(dateTime);
        var julianDay = solar.JulianDay + days;
        solar = Solar.FromJulianDay(julianDay);

        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    /// <summary>
    /// 添加时间跨度（完全基于Solar arithmetic）
    /// </summary>
    public static DateTime SolarAddTimeSpan(DateTime dateTime, TimeSpan timeSpan)
    {
        var solar = Solar.FromDate(dateTime);
        var julianDay = solar.JulianDay + timeSpan.TotalSeconds / 86400.0;
        solar = Solar.FromJulianDay(julianDay);

        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    /// <summary>
    /// 添加月份（完全基于Solar arithmetic）
    /// </summary>
    public static DateTime SolarAddMonths(DateTime dateTime, int months)
    {
        var solar = Solar.FromDate(dateTime);
        var targetMonth = solar.Month + months;
        var targetYear = solar.Year;

        while (targetMonth > 12)
        {
            targetMonth -= 12;
            targetYear++;
        }
        while (targetMonth < 1)
        {
            targetMonth += 12;
            targetYear--;
        }

        var daysInTargetMonth = Lunar.Util.SolarUtil.GetDaysOfMonth(targetYear, targetMonth);
        var targetDay = Math.Min(solar.Day, daysInTargetMonth);

        if (targetYear == 1582 && targetMonth == 10 && targetDay >= 5 && targetDay <= 14)
        {
            targetDay = 15;
        }

        solar = Solar.FromYmdHms(targetYear, targetMonth, targetDay, solar.Hour, solar.Minute, solar.Second);
        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    /// <summary>
    /// 添加年份（完全基于Solar arithmetic）
    /// </summary>
    public static DateTime SolarAddYears(DateTime dateTime, int years)
    {
        var solar = Solar.FromDate(dateTime);
        var targetYear = solar.Year + years;
        var targetMonth = solar.Month;
        var targetDay = solar.Day;

        var daysInTargetMonth = Lunar.Util.SolarUtil.GetDaysOfMonth(targetYear, targetMonth);
        targetDay = Math.Min(targetDay, daysInTargetMonth);

        if (targetYear == 1582 && targetMonth == 10 && targetDay >= 5 && targetDay <= 14)
        {
            targetDay = 15;
        }

        solar = Solar.FromYmdHms(targetYear, targetMonth, targetDay, solar.Hour, solar.Minute, solar.Second);
        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    /// <summary>
    /// 获取某月的天数（使用Solar算法，正确处理儒略历和公历）
    /// </summary>
    public static int GetDaysInMonth(int year, int month)
    {
        return Lunar.Util.SolarUtil.GetDaysOfMonth(year, month);
    }

    /// <summary>
    /// 判断是否为闰年（使用Solar算法，正确处理儒略历和公历）
    /// </summary>
    public static bool IsLeapYear(int year)
    {
        return Lunar.Util.SolarUtil.GetDaysOfMonth(year, 2) == 29;
    }

    /// <summary>
    /// 创建安全的DateTime（对于1582年之前的日期，使用Solar创建）
    /// </summary>
    public static DateTime CreateSafeDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
    {
        if (year >= 1583)
        {
            return new DateTime(year, month, day, hour, minute, second);
        }

        var solar = Solar.FromYmdHms(year, month, day, hour, minute, second);
        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    /// <summary>
    /// 比较两个日期的大小
    /// </summary>
    public static int Compare(DateTime date1, DateTime date2)
    {
        var solar1 = Solar.FromDate(date1);
        var solar2 = Solar.FromDate(date2);
        var jd1 = solar1.JulianDay;
        var jd2 = solar2.JulianDay;
        if (jd1 < jd2) return -1;
        if (jd1 > jd2) return 1;
        return 0;
    }

    /// <summary>
    /// 计算两个日期之间的天数差
    /// </summary>
    public static double DaysBetween(DateTime date1, DateTime date2)
    {
        var solar1 = Solar.FromDate(date1);
        var solar2 = Solar.FromDate(date2);
        return solar2.JulianDay - solar1.JulianDay;
    }

    #endregion

    #region 儒略日转换

    /// <summary>
    /// Unix时间戳转儒略日
    /// </summary>
    public static double UnixTimestampToJulianDay(double unixSeconds)
    {
        return unixSeconds / 86400.0 + JulianDayUnixEpoch;
    }

    /// <summary>
    /// 儒略日转Unix时间戳
    /// </summary>
    public static double JulianDayToUnixTimestamp(double julianDay)
    {
        return (julianDay - JulianDayUnixEpoch) * 86400;
    }

    /// <summary>
    /// 儒略日转DateTime（基于Solar，正确处理1582年之前的日期）
    /// </summary>
    public static DateTime JulianDayToDateTime(double julianDay)
    {
        var solar = Solar.FromJulianDay(julianDay);
        return new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
    }

    /// <summary>
    /// Unix时间戳转DateTime（基于Solar，正确处理1582年之前的日期）
    /// </summary>
    public static DateTime UnixTimestampToDateTime(double unixSeconds)
    {
        var julianDay = UnixTimestampToJulianDay(unixSeconds);
        return JulianDayToDateTime(julianDay);
    }

    #endregion

    // ===== TODO: 以下功能已预留接口，待后续实现 =====

    // TODO: 星座 - 已预留 GetXingZuo(DateTime solarDate) 方法，可进一步扩展为返回星座日期范围、星座符号等
    // TODO: 节气 - 已预留 GetJieQi(DateTime solarDate) 方法，可进一步扩展为返回节气日期、节气交节时间等
    // TODO: 生肖 - 已预留 GetShengXiao(int lunarYear) 方法，可进一步扩展为返回生肖吉祥物、生肖运势等
    // TODO: 节日 - 需实现 GetFestivals(DateTime solarDate) 方法，返回公历/农历节日列表（lunar-csharp 支持 lunar.Festivals 和 solar.Festivals）
    // TODO: 每日宜忌 - 需实现 GetDayYi(DateTime solarDate) 和 GetDayJi(DateTime solarDate) 方法（lunar-csharp 支持 lunar.DayYi 和 lunar.DayJi）
}

/// <summary>
/// 农历日期数据结构
/// </summary>
public class LunarDate
{
    private readonly Lunar.Lunar _lunar;

    internal LunarDate(Lunar.Lunar lunar)
    {
        _lunar = lunar;
    }

    public int Year => _lunar.Year;
    public int Month => Math.Abs(_lunar.Month);
    public int Day => _lunar.Day;
    public bool IsLeapMonth => _lunar.Month < 0;
    public string YearGan => _lunar.YearGan;
    public string YearZhi => _lunar.YearZhi;
    public string YearGanZhi => _lunar.YearGan + _lunar.YearZhi;
    public string MonthInChinese => _lunar.MonthInChinese;
    public string DayInChinese => _lunar.DayInChinese;
    public string YearShengXiao => _lunar.YearShengXiao;
    public string? JieQi => string.IsNullOrEmpty(_lunar.JieQi) ? null : _lunar.JieQi;
    public string XingZuo => _lunar.Solar.XingZuo;

    /// <summary>
    /// 格式化为农历字符串，如"甲子年 正月 初一"
    /// </summary>
    public override string ToString()
    {
        return $"{YearGanZhi}年 {MonthInChinese} {DayInChinese}";
    }
}
