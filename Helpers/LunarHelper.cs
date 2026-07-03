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
