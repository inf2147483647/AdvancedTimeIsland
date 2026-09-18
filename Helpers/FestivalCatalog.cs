using System;
using System.Collections.Generic;
using Lunar;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 插件内置节日目录。集中存放“下个节日倒计时”组件使用的节日定义，
/// 供该组件与调试页面的“显示节日列表”复用，避免两处数据各自维护而漂移。
/// 本类不做任何启用开关过滤，是否启用由调用方决定。
/// </summary>
public static class FestivalCatalog
{
    /// <summary>
    /// 添加国际节日（下个节日倒计时组件口径）。
    /// </summary>
    public static void AddInternationalFestivals(List<(string Name, DateTime Date)> festivals, DateTime date)
    {
        festivals.Add(("元旦", new DateTime(date.Year, 1, 1)));
        festivals.Add(("妇女节", new DateTime(date.Year, 3, 8)));
        festivals.Add(("植树节", new DateTime(date.Year, 3, 12)));
        festivals.Add(("劳动节", new DateTime(date.Year, 5, 1)));
        festivals.Add(("儿童节", new DateTime(date.Year, 6, 1)));
        festivals.Add(("教师节", new DateTime(date.Year, 9, 10)));
        festivals.Add(("清明节", GetQingMingDate(date.Year)));
        festivals.Add(("冬至", GetDongZhiDate(date.Year)));
    }

    /// <summary>
    /// 添加中国传统节日（下个节日倒计时组件口径）。
    /// </summary>
    public static void AddChineseTraditionalFestivals(List<(string Name, DateTime Date)> festivals, DateTime date, bool enableExperimental)
    {
        var solar = Solar.FromDate(date);
        var lunarYear = solar.Lunar.Year;

        festivals.Add(("春节", LunarToSolar(lunarYear, 1, 1)));
        festivals.Add(("元宵节", LunarToSolar(lunarYear, 1, 15)));
        festivals.Add(("寒食节", GetQingMingDate(date.Year).AddDays(-1)));
        festivals.Add(("清明节", GetQingMingDate(date.Year)));
        festivals.Add(("端午节", LunarToSolar(lunarYear, 5, 5)));
        festivals.Add(("上巳节", LunarToSolar(lunarYear, 3, 3)));
        festivals.Add(("七夕节", LunarToSolar(lunarYear, 7, 7)));
        festivals.Add(("中元节", LunarToSolar(lunarYear, 7, 15)));
        festivals.Add(("中秋节", LunarToSolar(lunarYear, 8, 15)));
        festivals.Add(("重阳节", LunarToSolar(lunarYear, 9, 9)));
        festivals.Add(("冬至", GetDongZhiDate(date.Year)));
        festivals.Add(("腊八节", LunarToSolar(lunarYear, 12, 8)));
        festivals.Add(("小年", LunarToSolar(lunarYear, 12, 23)));
        festivals.Add(("除夕", GetChuXiDate(lunarYear)));

        if (enableExperimental)
        {
            festivals.Add(("花朝节", LunarToSolar(lunarYear, 2, 15)));
        }
    }

    /// <summary>
    /// 添加红色节日。
    /// </summary>
    public static void AddRedFestivals(List<(string Name, DateTime Date)> festivals, DateTime date)
    {
        festivals.Add(("二七纪念日", new DateTime(date.Year, 2, 7)));
        festivals.Add(("学雷锋纪念日", new DateTime(date.Year, 3, 5)));
        festivals.Add(("五四青年节", new DateTime(date.Year, 5, 4)));
        festivals.Add(("七一建党节", new DateTime(date.Year, 7, 1)));
        festivals.Add(("八一建军节", new DateTime(date.Year, 8, 1)));
        festivals.Add(("中国人民抗日战争胜利纪念日", new DateTime(date.Year, 9, 3)));
        festivals.Add(("九一八事变纪念日", new DateTime(date.Year, 9, 18)));
        festivals.Add(("烈士纪念日", new DateTime(date.Year, 9, 30)));
        festivals.Add(("十一国庆节", new DateTime(date.Year, 10, 1)));
        festivals.Add(("中国工农红军长征胜利纪念日", new DateTime(date.Year, 10, 22)));
        festivals.Add(("台湾光复纪念日", new DateTime(date.Year, 10, 25)));
        festivals.Add(("南京大屠杀死难者国家公祭日", new DateTime(date.Year, 12, 13)));
    }

    /// <summary>
    /// 同义节日名合并表：键为“主界面-节日”所用的库内置名，值为插件统一使用的规范名。
    /// 仅用于调试页的节日列表展示，不影响两个组件各自的显示。
    /// </summary>
    private static readonly Dictionary<string, string> FestivalNameAliases = new()
    {
        ["元旦节"] = "元旦",
        ["青年节"] = "五四青年节",
        ["建党节"] = "七一建党节",
        ["建军节"] = "八一建军节",
        ["国庆节"] = "十一国庆节"
    };

    /// <summary>
    /// 各节日的起源年份（公历，公元前为负数）。节日在起源年份之前并不存在，因此不参与展示。
    /// 传统节日多为渐进演化，这里取学界主流的成形／官方确立年份。
    /// 取值已对照百度百科、快懂百科等中文百科词条复核；无明确记载的条目保守取值，不会误滤。
    /// </summary>
    private static readonly Dictionary<string, int> FestivalOriginYears = new()
    {
        // 中国传统节日 / 节气
        ["春节"] = -104,
        ["元宵节"] = -202,
        ["除夕"] = -221,
        ["寒食节"] = -202,
        ["清明节"] = 732,
        ["端午节"] = -278,
        ["七夕节"] = -202,
        ["中元节"] = 420,
        ["中秋节"] = 960,
        ["重阳节"] = 618,
        ["冬至"] = -202,
        ["腊八节"] = 550,
        ["小年"] = 960,
        ["龙头节"] = 1271,
        ["上巳节"] = -500,
        ["花朝节"] = 618,

        // 红色节日 / 现代纪念日
        ["二七纪念日"] = 1923,
        ["学雷锋纪念日"] = 1963,
        ["五四青年节"] = 1939,
        ["七一建党节"] = 1938,
        ["八一建军节"] = 1933,
        ["九一八事变纪念日"] = 1995,
        ["中国人民抗日战争胜利纪念日"] = 2014,
        ["烈士纪念日"] = 2014,
        ["十一国庆节"] = 1949,
        ["中国工农红军长征胜利纪念日"] = 1996,
        ["台湾光复纪念日"] = 2025,
        ["南京大屠杀死难者国家公祭日"] = 2014,

        // 国际节日 / 其他
        ["元旦"] = 1912,
        ["妇女节"] = 1910,
        ["植树节"] = 1979,
        ["劳动节"] = 1889,
        ["儿童节"] = 1949,
        ["教师节"] = 1985,
        ["情人节"] = 496,
        ["愚人节"] = 1564,
        ["母亲节"] = 1914,
        ["父亲节"] = 1910,
        ["万圣节前夜"] = 609,
        ["万圣节"] = 844,
        ["感恩节"] = 1863,
        ["消费者权益日"] = 1983,
        ["世界住房日"] = 1985,
        ["全国中小学生安全教育日"] = 1996,
        ["全国助残日"] = 1990,
        ["全民国防教育日"] = 2001
    };

    /// <summary>
    /// 判断节日在指定年份是否已存在（起源当年及之后才存在）。
    /// </summary>
    private static bool ExistsInYear(string name, int year)
    {
        return !FestivalOriginYears.TryGetValue(name, out var originYear) || year >= originYear;
    }

    /// <summary>
    /// 枚举指定年份“主界面-节日”组件支持的全部节日（国际、中国传统、红色、实验性 4 类），
    /// 不受“管理启用的功能”等任何启用开关影响，按日期排序并按节日名去重，同义节日名合并为规范名，
    /// 且不展示早于其起源年份的节日。
    /// </summary>
    public static List<(string Name, DateTime Date)> GetAllFestivalsOfYear(int year)
    {
        var result = new List<(string Name, DateTime Date)>();
        var seen = new HashSet<string>();

        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year, 12, 31);
        var date = start;
        while (true)
        {
            // 一律按全部类别（国际/传统/红色/实验性）取节日
            foreach (var name in LunarHelper.GetFestivals(date, true, true, true, true))
            {
                var canonical = NormalizeFestivalName(name);
                if (!ExistsInYear(canonical, year))
                    continue;
                if (seen.Add(canonical))
                    result.Add((canonical, date));
            }

            // 避免在 9999-12-31 上 AddDays 溢出
            if (date >= end)
                break;
            date = date.AddDays(1);
        }

        result.Sort((a, b) => a.Date != b.Date ? a.Date.CompareTo(b.Date) : string.CompareOrdinal(a.Name, b.Name));
        return result;
    }

    private static string NormalizeFestivalName(string name)
    {
        return FestivalNameAliases.TryGetValue(name, out var canonical) ? canonical : name;
    }

    public static DateTime LunarToSolar(int lunarYear, int lunarMonth, int lunarDay)
    {
        try
        {
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, lunarMonth, lunarDay);
            var solar = lunar.Solar;
            return new DateTime(solar.Year, solar.Month, solar.Day);
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }

    public static DateTime GetQingMingDate(int year)
    {
        var solar = Solar.FromYmdHms(year, 4, 4);
        var jieQi = solar.Lunar.JieQi;
        if (jieQi == "清明") return new DateTime(year, 4, 4);
        return new DateTime(year, 4, 5);
    }

    public static DateTime GetDongZhiDate(int year)
    {
        var solar = Solar.FromYmdHms(year, 12, 21);
        var jieQi = solar.Lunar.JieQi;
        if (jieQi == "冬至") return new DateTime(year, 12, 21);
        solar = Solar.FromYmdHms(year, 12, 22);
        jieQi = solar.Lunar.JieQi;
        if (jieQi == "冬至") return new DateTime(year, 12, 22);
        return new DateTime(year, 12, 23);
    }

    public static DateTime GetChuXiDate(int lunarYear)
    {
        try
        {
            var nextYearLunar = Lunar.Lunar.FromYmdHms(lunarYear + 1, 1, 1);
            var nextYearSolar = nextYearLunar.Solar;
            var nextYearDate = new DateTime(nextYearSolar.Year, nextYearSolar.Month, nextYearSolar.Day);
            return nextYearDate.AddDays(-1);
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }
}