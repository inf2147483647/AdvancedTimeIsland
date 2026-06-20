﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Views.Main;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Automation.Triggers;
using AdvancedTimeIsland.Automation.Conditions;
using AdvancedTimeIsland.Automation.Rules;

namespace AdvancedTimeIsland;

[PluginEntrance]
public class Plugin : PluginBase
{
    public static Guid PluginId => new("11223344-5566-7788-9900-aabbccddeeff");

    public PluginSettings Settings { get; set; } = new();

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<PluginSettings>(context.Configuration.GetSection("AdvancedTimeIsland"));

        services.AddSingleton(Settings);

        services.AddSingleton<LunarInstallerService>();

        LunarInstallerService.AutoInstallAsync();

        services.AddSingleton<TimeBaseService>();

        services.AddComponent<AdvancedDateControl, AdvancedDateSettingsControl>();

        services.AddComponent<LocalSolarTimeControl, LocalSolarTimeSettingsControl>();

        services.AddComponent<TimeZoneTimeControl, TimeZoneTimeSettingsControl>();

        services.AddSingleton<ExactTimeTrigger>();

        services.AddSingleton<TimeRangeCondition>();

        // 注册规则：精确时间是
        services.AddRule<ExactTimeRuleSettings, ExactTimeRuleSettingsControl>(
            "advancedtimeisland.exact_time_is",
            "精确时间是",
            "\uecc1",
            settings =>
            {
                if (settings is not ExactTimeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.TargetTime))
                    return false;

                if (!Helpers.UnixTimeHelper.TryParseExactTime(s.TargetTime, out var targetTime))
                    return false;

                var currentTime = DateTime.Now;
                return currentTime.Year == targetTime.Year &&
                       currentTime.Month == targetTime.Month &&
                       currentTime.Day == targetTime.Day &&
                       currentTime.Hour == targetTime.Hour &&
                       currentTime.Minute == targetTime.Minute &&
                       currentTime.Second == targetTime.Second;
            }
        );

        // 注册规则：精确时间在范围
        services.AddRule<ExactTimeRangeRuleSettings, ExactTimeRangeRuleSettingsControl>(
            "advancedtimeisland.exact_time_range",
            "精确时间在范围",
            "\uecc2",
            settings =>
            {
                if (settings is not ExactTimeRangeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartTime) || string.IsNullOrWhiteSpace(s.EndTime))
                    return false;

                if (!Helpers.UnixTimeHelper.TryParseExactTime(s.StartTime, out var startTime) ||
                    !Helpers.UnixTimeHelper.TryParseExactTime(s.EndTime, out var endTime))
                    return false;

                var currentTime = DateTime.Now;
                return currentTime >= startTime && currentTime <= endTime;
            }
        );

        // 注册规则：每年时间范围 (MM-DD-hh-mm-ss)
        services.AddRule<YearlyTimeRangeRuleSettings, YearlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.yearly_time_range",
            "每年时间范围",
            "\uecc3",
            settings =>
            {
                if (settings is not YearlyTimeRangeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartTime) || string.IsNullOrWhiteSpace(s.EndTime))
                    return false;

                var startParts = s.StartTime.Split('-');
                var endParts = s.EndTime.Split('-');
                if (startParts.Length < 5 || endParts.Length < 5)
                    return false;

                if (!int.TryParse(startParts[0], out int startMonth) ||
                    !int.TryParse(startParts[1], out int startDay) ||
                    !int.TryParse(startParts[2], out int startHour) ||
                    !int.TryParse(startParts[3], out int startMinute) ||
                    !int.TryParse(startParts[4], out int startSecond))
                    return false;

                if (!int.TryParse(endParts[0], out int endMonth) ||
                    !int.TryParse(endParts[1], out int endDay) ||
                    !int.TryParse(endParts[2], out int endHour) ||
                    !int.TryParse(endParts[3], out int endMinute) ||
                    !int.TryParse(endParts[4], out int endSecond))
                    return false;

                var now = DateTime.Now;
                var startTimeThisYear = new DateTime(now.Year, startMonth, startDay, startHour, startMinute, startSecond);
                var endTimeThisYear = new DateTime(now.Year, endMonth, endDay, endHour, endMinute, endSecond);

                // 处理跨年情况
                if (startTimeThisYear > endTimeThisYear)
                {
                    // 跨年：如果当前时间大于开始时间，则结束时间是下一年
                    // 如果当前时间小于结束时间，则开始时间是上一年
                    if (now >= startTimeThisYear)
                        endTimeThisYear = endTimeThisYear.AddYears(1);
                    else
                        startTimeThisYear = startTimeThisYear.AddYears(-1);
                }

                return now >= startTimeThisYear && now <= endTimeThisYear;
            }
        );

        // 注册规则：每月时间范围 (DD-hh-mm-ss)
        services.AddRule<MonthlyTimeRangeRuleSettings, MonthlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.monthly_time_range",
            "每月时间范围",
            "\uecc4",
            settings =>
            {
                if (settings is not MonthlyTimeRangeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartTime) || string.IsNullOrWhiteSpace(s.EndTime))
                    return false;

                var startParts = s.StartTime.Split('-');
                var endParts = s.EndTime.Split('-');
                if (startParts.Length < 4 || endParts.Length < 4)
                    return false;

                if (!int.TryParse(startParts[0], out int startDay) ||
                    !int.TryParse(startParts[1], out int startHour) ||
                    !int.TryParse(startParts[2], out int startMinute) ||
                    !int.TryParse(startParts[3], out int startSecond))
                    return false;

                if (!int.TryParse(endParts[0], out int endDay) ||
                    !int.TryParse(endParts[1], out int endHour) ||
                    !int.TryParse(endParts[2], out int endMinute) ||
                    !int.TryParse(endParts[3], out int endSecond))
                    return false;

                var now = DateTime.Now;
                var startTimeThisMonth = new DateTime(now.Year, now.Month, startDay, startHour, startMinute, startSecond);
                var endTimeThisMonth = new DateTime(now.Year, now.Month, endDay, endHour, endMinute, endSecond);

                // 处理跨月情况
                if (startTimeThisMonth > endTimeThisMonth)
                {
                    if (now >= startTimeThisMonth)
                        endTimeThisMonth = endTimeThisMonth.AddMonths(1);
                    else
                        startTimeThisMonth = startTimeThisMonth.AddMonths(-1);
                }

                return now >= startTimeThisMonth && now <= endTimeThisMonth;
            }
        );

        // 注册规则：每天时间范围 (hh-mm-ss)
        services.AddRule<DailyTimeRangeRuleSettings, DailyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.daily_time_range",
            "每天时间范围",
            "\uecc5",
            settings =>
            {
                if (settings is not DailyTimeRangeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartTime) || string.IsNullOrWhiteSpace(s.EndTime))
                    return false;

                var startParts = s.StartTime.Split('-');
                var endParts = s.EndTime.Split('-');
                if (startParts.Length < 3 || endParts.Length < 3)
                    return false;

                if (!int.TryParse(startParts[0], out int startHour) ||
                    !int.TryParse(startParts[1], out int startMinute) ||
                    !int.TryParse(startParts[2], out int startSecond))
                    return false;

                if (!int.TryParse(endParts[0], out int endHour) ||
                    !int.TryParse(endParts[1], out int endMinute) ||
                    !int.TryParse(endParts[2], out int endSecond))
                    return false;

                var now = DateTime.Now;
                var startTimeToday = new DateTime(now.Year, now.Month, now.Day, startHour, startMinute, startSecond);
                var endTimeToday = new DateTime(now.Year, now.Month, now.Day, endHour, endMinute, endSecond);

                // 处理跨天情况
                if (startTimeToday > endTimeToday)
                {
                    if (now >= startTimeToday)
                        endTimeToday = endTimeToday.AddDays(1);
                    else
                        startTimeToday = startTimeToday.AddDays(-1);
                }

                return now >= startTimeToday && now <= endTimeToday;
            }
        );

        // 注册规则：每小时时间范围 (mm-ss)
        services.AddRule<HourlyTimeRangeRuleSettings, HourlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.hourly_time_range",
            "每小时时间范围",
            "\uecc6",
            settings =>
            {
                if (settings is not HourlyTimeRangeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartTime) || string.IsNullOrWhiteSpace(s.EndTime))
                    return false;

                var startParts = s.StartTime.Split('-');
                var endParts = s.EndTime.Split('-');
                if (startParts.Length < 2 || endParts.Length < 2)
                    return false;

                if (!int.TryParse(startParts[0], out int startMinute) ||
                    !int.TryParse(startParts[1], out int startSecond))
                    return false;

                if (!int.TryParse(endParts[0], out int endMinute) ||
                    !int.TryParse(endParts[1], out int endSecond))
                    return false;

                var now = DateTime.Now;
                var startTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, startMinute, startSecond);
                var endTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, endMinute, endSecond);

                // 处理跨小时情况
                if (startTimeThisHour > endTimeThisHour)
                {
                    if (now >= startTimeThisHour)
                        endTimeThisHour = endTimeThisHour.AddHours(1);
                    else
                        startTimeThisHour = startTimeThisHour.AddHours(-1);
                }

                return now >= startTimeThisHour && now <= endTimeThisHour;
            }
        );

        // 注册规则：每分钟时间范围 (ss)
        services.AddRule<MinutelyTimeRangeRuleSettings, MinutelyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.minutely_time_range",
            "每分钟时间范围",
            "\uecc7",
            settings =>
            {
                if (settings is not MinutelyTimeRangeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartSecond) || string.IsNullOrWhiteSpace(s.EndSecond))
                    return false;

                if (!int.TryParse(s.StartSecond, out int startSecond) ||
                    !int.TryParse(s.EndSecond, out int endSecond))
                    return false;

                var now = DateTime.Now;
                var startTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, startSecond);
                var endTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, endSecond);

                // 处理跨分钟情况
                if (startTimeThisMinute > endTimeThisMinute)
                {
                    if (now >= startTimeThisMinute)
                        endTimeThisMinute = endTimeThisMinute.AddMinutes(1);
                    else
                        startTimeThisMinute = startTimeThisMinute.AddMinutes(-1);
                }

                return now >= startTimeThisMinute && now <= endTimeThisMinute;
            }
        );

        services.AddSettingsPage<Views.Settings.AboutPage>();
    }
}
