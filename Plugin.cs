using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Services.NotificationProviders;
using AdvancedTimeIsland.Views.Main;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Automation.Triggers;
using AdvancedTimeIsland.Automation.Conditions;
using AdvancedTimeIsland.Automation.Rules;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace AdvancedTimeIsland;

[PluginEntrance]
public class Plugin : PluginBase
{
    public static Guid PluginId => new("11223344-5566-7788-9900-aabbccddeeff");

    public static Plugin Instance { get; private set; } = null!;

    public PluginSettings Settings { get; set; } = new();
    public DebugSettings DebugSettings { get; set; } = new();

    private const string SettingsFileName = "settings.json";
    private const string TempFileSuffix = ".tmp";

    private static readonly object _saveLock = new();

    public Plugin()
    {
        Instance = this;
    }

    private string GetSettingsFilePath()
    {
        return Path.Combine(PluginConfigFolder, SettingsFileName);
    }

    private void LoadSettings()
    {
        try
        {
            var filePath = GetSettingsFilePath();
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var loadedContainer = JsonSerializer.Deserialize<SettingsContainer>(json, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                if (loadedContainer != null)
                {
                    if (loadedContainer.Settings != null)
                    {
                        Settings = loadedContainer.Settings;
                        Settings.PropertyChanged -= OnSettingsPropertyChanged;
                        Settings.PropertyChanged += OnSettingsPropertyChanged;
                    }
                    if (loadedContainer.DebugSettings != null)
                    {
                        DebugSettings = loadedContainer.DebugSettings;
                        DebugSettings.PropertyChanged -= OnDebugSettingsPropertyChanged;
                        DebugSettings.PropertyChanged += OnDebugSettingsPropertyChanged;
                    }
                }
            }
            else
            {
                Settings.PropertyChanged -= OnSettingsPropertyChanged;
                Settings.PropertyChanged += OnSettingsPropertyChanged;
                DebugSettings.PropertyChanged -= OnDebugSettingsPropertyChanged;
                DebugSettings.PropertyChanged += OnDebugSettingsPropertyChanged;
            }
        }
        catch
        {
            Settings.PropertyChanged -= OnSettingsPropertyChanged;
            Settings.PropertyChanged += OnSettingsPropertyChanged;
            DebugSettings.PropertyChanged -= OnDebugSettingsPropertyChanged;
            DebugSettings.PropertyChanged += OnDebugSettingsPropertyChanged;
        }
    }

    private void SaveSettings()
    {
        lock (_saveLock)
        {
            try
            {
                var filePath = GetSettingsFilePath();
                var tempPath = filePath + TempFileSuffix;

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var container = new SettingsContainer
                {
                    Settings = Settings,
                    DebugSettings = DebugSettings
                };

                var json = JsonSerializer.Serialize(container, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fs))
                {
                    writer.Write(json);
                    writer.Flush();
                    fs.Flush(true);
                }

                if (File.Exists(filePath))
                {
                    File.Replace(tempPath, filePath, null);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
            }
            catch
            {
                // 如果保存失败，忽略
            }
        }
    }

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SaveSettings();
    }

    private void OnDebugSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SaveSettings();
    }

    /// <summary>
    /// 获取当前时间（应用插件全局偏移，基于NTP服务器时间）
    /// </summary>
    public static DateTime GetCurrentTime()
    {
        if (TimeBaseService.Instance != null)
        {
            return TimeBaseService.Instance.GetCurrentTime();
        }
        var offset = Instance?.Settings.TimeOffsetSeconds ?? 0;
        return DateTime.Now.AddSeconds(offset);
    }

    /// <summary>
    /// 计算地方时
    /// </summary>
    public static DateTime GetLocalSolarTime(DateTime localTime)
    {
        var longitude = Instance?.Settings.Longitude ?? 116.4;
        return GetLocalSolarTime(localTime, longitude);
    }

    /// <summary>
    /// 计算地方时（指定经度）
    /// </summary>
    public static DateTime GetLocalSolarTime(DateTime localTime, double longitude)
    {
        var standardMeridian = TimeZoneInfo.Local.BaseUtcOffset.TotalHours * 15;
        var timeDifferenceMinutes = (longitude - standardMeridian) * 4;
        return localTime.AddMinutes(timeDifferenceMinutes);
    }

    /// <summary>
    /// 计算区时
    /// </summary>
    public static DateTime GetTimeZoneTime(DateTime localTime)
    {
        var timeZoneId = Instance?.Settings.TimeZoneId ?? "China Standard Time";
        return GetTimeZoneTime(localTime, timeZoneId);
    }

    /// <summary>
    /// 计算区时（指定时区）
    /// </summary>
    public static DateTime GetTimeZoneTime(DateTime localTime, string timeZoneId)
    {
        try
        {
            var targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTime(localTime, targetTimeZone);
        }
        catch
        {
            return localTime.ToLocalTime();
        }
    }


    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 加载已保存的设置
        LoadSettings();

        // 订阅设置变更事件，自动保存（LoadSettings 中已订阅，此处为双重保险）
        if (Settings != null)
        {
            Settings.PropertyChanged -= OnSettingsPropertyChanged;
            Settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        if (DebugSettings != null)
        {
            DebugSettings.PropertyChanged -= OnDebugSettingsPropertyChanged;
            DebugSettings.PropertyChanged += OnDebugSettingsPropertyChanged;
        }

        services.Configure<PluginSettings>(context.Configuration.GetSection("AdvancedTimeIsland"));

        services.AddSingleton(Settings);

        services.AddSingleton<LunarInstallerService>();

        LunarInstallerService.AutoInstallAsync();

        services.AddSingleton<TimeBaseService>();
        services.AddNotificationProvider<CountdownNotificationProvider>();

        services.AddComponent<AdvancedDateControl, AdvancedDateSettingsControl>();
        services.AddComponent<CountdownControl, CountdownSettingsControl>();
        services.AddComponent<ForwardTimerControl, ForwardTimerSettingsControl>();
        services.AddComponent<LunarCountdownControl, LunarCountdownSettingsControl>();

        services.AddComponent<LocalSolarTimeControl, LocalSolarTimeSettingsControl>();

        services.AddComponent<TimeZoneTimeControl, TimeZoneTimeSettingsControl>();

        services.AddSingleton<ExactTimeTrigger>();

        services.AddSingleton<TimeRangeCondition>();

        // ========== 现有条件：时间基准改为插件全局偏移后的时间 ==========

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

                var currentTime = GetCurrentTime();
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

                var currentTime = GetCurrentTime();
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

                var now = GetCurrentTime();
                var startTimeThisYear = new DateTime(now.Year, startMonth, startDay, startHour, startMinute, startSecond);
                var endTimeThisYear = new DateTime(now.Year, endMonth, endDay, endHour, endMinute, endSecond);

                if (startTimeThisYear > endTimeThisYear)
                {
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

                var now = GetCurrentTime();
                var startTimeThisMonth = new DateTime(now.Year, now.Month, startDay, startHour, startMinute, startSecond);
                var endTimeThisMonth = new DateTime(now.Year, now.Month, endDay, endHour, endMinute, endSecond);

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

                var now = GetCurrentTime();
                var startTimeToday = new DateTime(now.Year, now.Month, now.Day, startHour, startMinute, startSecond);
                var endTimeToday = new DateTime(now.Year, now.Month, now.Day, endHour, endMinute, endSecond);

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

                var now = GetCurrentTime();
                var startTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, startMinute, startSecond);
                var endTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, endMinute, endSecond);

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

                var now = GetCurrentTime();
                var startTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, startSecond);
                var endTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, endSecond);

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

        // ========== 地方时条件（6个，带经度设置）==========

        // 1. 地方时精确时间在范围
        services.AddRule<LocalSolarExactTimeRuleSettings, LocalSolarExactTimeRuleSettingsControl>(
            "advancedtimeisland.local_solar_exact_time_range",
            "地方时精确时间在范围",
            "\uecc2",
            settings =>
            {
                if (settings is not LocalSolarExactTimeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartTime) || string.IsNullOrWhiteSpace(s.EndTime))
                    return false;

                if (!Helpers.UnixTimeHelper.TryParseExactTime(s.StartTime, out var startTime) ||
                    !Helpers.UnixTimeHelper.TryParseExactTime(s.EndTime, out var endTime))
                    return false;

                var currentTime = GetLocalSolarTime(GetCurrentTime(), s.Longitude);
                return currentTime >= startTime && currentTime <= endTime;
            }
        );

        // 2. 地方时每年时间范围
        services.AddRule<LocalSolarYearlyTimeRangeRuleSettings, LocalSolarYearlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.local_solar_yearly_time_range",
            "地方时每年时间范围",
            "\uecc3",
            settings =>
            {
                if (settings is not LocalSolarYearlyTimeRangeRuleSettings s)
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

                var now = GetLocalSolarTime(GetCurrentTime(), s.Longitude);
                var startTimeThisYear = new DateTime(now.Year, startMonth, startDay, startHour, startMinute, startSecond);
                var endTimeThisYear = new DateTime(now.Year, endMonth, endDay, endHour, endMinute, endSecond);

                if (startTimeThisYear > endTimeThisYear)
                {
                    if (now >= startTimeThisYear)
                        endTimeThisYear = endTimeThisYear.AddYears(1);
                    else
                        startTimeThisYear = startTimeThisYear.AddYears(-1);
                }

                return now >= startTimeThisYear && now <= endTimeThisYear;
            }
        );

        // 3. 地方时每月时间范围
        services.AddRule<LocalSolarMonthlyTimeRangeRuleSettings, LocalSolarMonthlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.local_solar_monthly_time_range",
            "地方时每月时间范围",
            "\uecc4",
            settings =>
            {
                if (settings is not LocalSolarMonthlyTimeRangeRuleSettings s)
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

                var now = GetLocalSolarTime(GetCurrentTime(), s.Longitude);
                var startTimeThisMonth = new DateTime(now.Year, now.Month, startDay, startHour, startMinute, startSecond);
                var endTimeThisMonth = new DateTime(now.Year, now.Month, endDay, endHour, endMinute, endSecond);

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

        // 4. 地方时每天时间范围
        services.AddRule<LocalSolarDailyTimeRangeRuleSettings, LocalSolarDailyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.local_solar_daily_time_range",
            "地方时每天时间范围",
            "\uecc5",
            settings =>
            {
                if (settings is not LocalSolarDailyTimeRangeRuleSettings s)
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

                var now = GetLocalSolarTime(GetCurrentTime(), s.Longitude);
                var startTimeToday = new DateTime(now.Year, now.Month, now.Day, startHour, startMinute, startSecond);
                var endTimeToday = new DateTime(now.Year, now.Month, now.Day, endHour, endMinute, endSecond);

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

        // 5. 地方时每小时时间范围
        services.AddRule<LocalSolarHourlyTimeRangeRuleSettings, LocalSolarHourlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.local_solar_hourly_time_range",
            "地方时每小时时间范围",
            "\uecc6",
            settings =>
            {
                if (settings is not LocalSolarHourlyTimeRangeRuleSettings s)
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

                var now = GetLocalSolarTime(GetCurrentTime(), s.Longitude);
                var startTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, startMinute, startSecond);
                var endTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, endMinute, endSecond);

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

        // 6. 地方时每分钟时间范围
        services.AddRule<LocalSolarMinutelyTimeRangeRuleSettings, LocalSolarMinutelyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.local_solar_minutely_time_range",
            "地方时每分钟时间范围",
            "\uecc7",
            settings =>
            {
                if (settings is not LocalSolarMinutelyTimeRangeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartSecond) || string.IsNullOrWhiteSpace(s.EndSecond))
                    return false;

                if (!int.TryParse(s.StartSecond, out int startSecond) ||
                    !int.TryParse(s.EndSecond, out int endSecond))
                    return false;

                var now = GetLocalSolarTime(GetCurrentTime(), s.Longitude);
                var startTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, startSecond);
                var endTimeThisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, endSecond);

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

        // ========== 区时条件（5个，带时区设置）==========

        // 7. 区时精确时间在范围
        services.AddRule<TimeZoneExactTimeRuleSettings, TimeZoneExactTimeRuleSettingsControl>(
            "advancedtimeisland.time_zone_exact_time_range",
            "区时精确时间在范围",
            "\uecc2",
            settings =>
            {
                if (settings is not TimeZoneExactTimeRuleSettings s)
                    return false;

                if (string.IsNullOrWhiteSpace(s.StartTime) || string.IsNullOrWhiteSpace(s.EndTime))
                    return false;

                if (!Helpers.UnixTimeHelper.TryParseExactTime(s.StartTime, out var startTime) ||
                    !Helpers.UnixTimeHelper.TryParseExactTime(s.EndTime, out var endTime))
                    return false;

                var currentTime = GetTimeZoneTime(GetCurrentTime(), s.TimeZoneId);
                return currentTime >= startTime && currentTime <= endTime;
            }
        );

        // 8. 区时每年时间范围
        services.AddRule<TimeZoneYearlyTimeRangeRuleSettings, TimeZoneYearlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.time_zone_yearly_time_range",
            "区时每年时间范围",
            "\uecc3",
            settings =>
            {
                if (settings is not TimeZoneYearlyTimeRangeRuleSettings s)
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

                var now = GetTimeZoneTime(GetCurrentTime(), s.TimeZoneId);
                var startTimeThisYear = new DateTime(now.Year, startMonth, startDay, startHour, startMinute, startSecond);
                var endTimeThisYear = new DateTime(now.Year, endMonth, endDay, endHour, endMinute, endSecond);

                if (startTimeThisYear > endTimeThisYear)
                {
                    if (now >= startTimeThisYear)
                        endTimeThisYear = endTimeThisYear.AddYears(1);
                    else
                        startTimeThisYear = startTimeThisYear.AddYears(-1);
                }

                return now >= startTimeThisYear && now <= endTimeThisYear;
            }
        );

        // 9. 区时每月时间范围
        services.AddRule<TimeZoneMonthlyTimeRangeRuleSettings, TimeZoneMonthlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.time_zone_monthly_time_range",
            "区时每月时间范围",
            "\uecc4",
            settings =>
            {
                if (settings is not TimeZoneMonthlyTimeRangeRuleSettings s)
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

                var now = GetTimeZoneTime(GetCurrentTime(), s.TimeZoneId);
                var startTimeThisMonth = new DateTime(now.Year, now.Month, startDay, startHour, startMinute, startSecond);
                var endTimeThisMonth = new DateTime(now.Year, now.Month, endDay, endHour, endMinute, endSecond);

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

        // 10. 区时每天时间范围
        services.AddRule<TimeZoneDailyTimeRangeRuleSettings, TimeZoneDailyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.time_zone_daily_time_range",
            "区时每天时间范围",
            "\uecc5",
            settings =>
            {
                if (settings is not TimeZoneDailyTimeRangeRuleSettings s)
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

                var now = GetTimeZoneTime(GetCurrentTime(), s.TimeZoneId);
                var startTimeToday = new DateTime(now.Year, now.Month, now.Day, startHour, startMinute, startSecond);
                var endTimeToday = new DateTime(now.Year, now.Month, now.Day, endHour, endMinute, endSecond);

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

        // 11. 区时每小时时间范围
        services.AddRule<TimeZoneHourlyTimeRangeRuleSettings, TimeZoneHourlyTimeRangeRuleSettingsControl>(
            "advancedtimeisland.time_zone_hourly_time_range",
            "区时每小时时间范围",
            "\uecc6",
            settings =>
            {
                if (settings is not TimeZoneHourlyTimeRangeRuleSettings s)
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

                var now = GetTimeZoneTime(GetCurrentTime(), s.TimeZoneId);
                var startTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, startMinute, startSecond);
                var endTimeThisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, endMinute, endSecond);

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

        services.AddSettingsPage<Views.Settings.AboutPage>();
        services.AddSettingsPage<Views.Settings.DebugPage>();
    }
}
