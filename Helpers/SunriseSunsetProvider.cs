using System;
using System.Linq;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 读取主界面「日出日落」组件的设置并计算日出 / 日落时间。
/// 触发器等非组件场景通过此入口复用主界面上的经纬度与时区配置。
/// </summary>
public static class SunriseSunsetProvider
{
    /// <summary>「日出日落」组件的 Guid（与 SunriseSunsetControl 的 ComponentInfo 保持一致）。</summary>
    public const string ComponentGuid = "33445566-7788-9900-1122-334455667788";

    /// <summary>主界面是否配置了「日出日落」组件。</summary>
    public static bool HasMainWindowComponent =>
        FindMainWindowSettings() != null;

    /// <summary>
    /// 获取主界面「日出日落」组件的设置；未找到组件时返回一份默认设置。
    /// </summary>
    public static SunriseSunsetSettings GetEffectiveSettings()
    {
        return FindMainWindowSettings() ?? new SunriseSunsetSettings();
    }

    /// <summary>
    /// 按主界面「日出日落」组件的经纬度与时区计算指定日期的日出 / 日落时间。
    /// </summary>
    public static (TimeSpan Sunrise, TimeSpan Sunset) Calculate(DateTime dateTime)
    {
        var settings = GetEffectiveSettings();
        var timeZoneId = string.IsNullOrEmpty(settings.TimeZoneId)
            ? Plugin.Instance?.Settings?.TimeZoneId ?? "China Standard Time"
            : settings.TimeZoneId;

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone) || timeZone == null)
        {
            timeZone = TimeZoneInfo.Local;
        }

        return SunriseSunsetCalculator.Calculate(dateTime, settings.Latitude, settings.Longitude, timeZone);
    }

    /// <summary>
    /// 判断某个时刻是否处于白昼：日出（含）到日落（不含）为白昼，其余为黑夜。
    /// 同时处理极昼 / 极夜：日出为 0 且日落为 24 小时表示极昼，两者均为 0 表示极夜。
    /// </summary>
    public static bool IsDaytime(TimeSpan timeOfDay, TimeSpan sunrise, TimeSpan sunset)
    {
        if (sunrise <= TimeSpan.Zero && sunset >= TimeSpan.FromDays(1))
        {
            return true;
        }

        if (sunrise <= TimeSpan.Zero && sunset <= TimeSpan.Zero)
        {
            return false;
        }

        return timeOfDay >= sunrise && timeOfDay < sunset;
    }

    private static SunriseSunsetSettings? FindMainWindowSettings()
    {
        var componentsService = IAppHost.TryGetService<IComponentsService>();
        var component = componentsService?.CurrentComponents?.Lines
            .SelectMany(line => line.Children)
            .FirstOrDefault(item => string.Equals(item.Id, ComponentGuid, StringComparison.OrdinalIgnoreCase));

        return component?.Settings as SunriseSunsetSettings;
    }
}
