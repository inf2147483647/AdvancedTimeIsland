using System;
using System.Linq;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Shared;

/// <summary>
/// 对外公开的法定节假日查询接口，供其它 ClassIsland 插件跨插件调用。
/// 调用方无需引用本插件程序集，按类型名反射即可：
/// <code>
/// AdvancedTimeIsland.Shared.HolidayApi.IsStatutoryHoliday(DateTime)   → bool
/// AdvancedTimeIsland.Shared.HolidayApi.GetHolidayDates(int year)      → DateTime[]
/// AdvancedTimeIsland.Shared.HolidayApi.TryGetHolidayName(DateTime, out string) → bool
/// </code>
/// 本接口只读取插件已有的节假日数据（含联网更新与手动编辑的结果），不修改任何状态。
/// </summary>
public static class HolidayApi
{
    /// <summary>
    /// 接口契约版本。调用方可据此判断是否存在预期的方法，后续不兼容变更时递增。
    /// </summary>
    public const int Version = 1;

    /// <summary>
    /// 判断指定日期是否为法定节假日（放假日）。调休补班日不算节假日。
    /// </summary>
    public static bool IsStatutoryHoliday(DateTime date)
        => TryGetHolidayEntry(date, out _);

    /// <summary>
    /// 获取指定日期对应节假日的名称（如"春节"）；不是节假日时返回 false。
    /// </summary>
    public static bool TryGetHolidayName(DateTime date, out string name)
    {
        if (TryGetHolidayEntry(date, out var entry) && entry is not null)
        {
            name = entry.Name;
            return true;
        }

        name = string.Empty;
        return false;
    }

    /// <summary>
    /// 获取指定年份全部法定节假日（放假日）日期，按日期升序、去重。
    /// 数据不可用时返回空数组，调用方无需处理异常。
    /// </summary>
    public static DateTime[] GetHolidayDates(int year)
    {
        try
        {
            var service = GetService();
            if (service is null)
            {
                return Array.Empty<DateTime>();
            }

            return service.Data.Holidays
                .Where(entry => entry.IsOffDay && entry.Date.Year == year)
                .Select(entry => entry.Date.Date)
                .Distinct()
                .OrderBy(date => date)
                .ToArray();
        }
        catch
        {
            // 数据正在被编辑（集合枚举竞态）等异常一律按"无数据"处理。
            return Array.Empty<DateTime>();
        }
    }

    private static bool TryGetHolidayEntry(DateTime date, out HolidayDay? entry)
    {
        entry = null;

        try
        {
            var service = GetService();
            if (service is null)
            {
                return false;
            }

            var day = date.Date;
            entry = service.Data.Holidays
                .FirstOrDefault(x => x.IsOffDay && x.Date.Date == day);
            return entry is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 取日历服务：优先使用已构造的单例；尚未构造时从宿主 DI 解析（该动作会触发其构造），
    /// 保证即使「在校时间统计」组件从未被创建，节假日数据也可用。
    /// </summary>
    private static AttendanceCalendarService? GetService()
    {
        if (AttendanceCalendarService.Instance is not null)
        {
            return AttendanceCalendarService.Instance;
        }

        try
        {
            return IAppHost.Host?.Services.GetService(typeof(AttendanceCalendarService))
                as AttendanceCalendarService;
        }
        catch
        {
            return null;
        }
    }
}
