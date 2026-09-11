using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AdvancedTimeIsland.Automation.Actions;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Shared;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using SuperAutoIsland.Interface;
using SuperAutoIsland.Interface.MetaData;
using SuperAutoIsland.Interface.MetaData.ArgsType;
using SuperAutoIsland.Interface.Services;

namespace AdvancedTimeIsland.Automation.Sai;

/// <summary>
/// 把 AdvancedTimeIsland 的行动与规则注册为 SuperAutoIsland 的 Blockly 积木。
/// 积木 Id 与 ClassIsland 的行动/规则 Id 完全一致，SuperAutoIsland 运行时会直接调用
/// ClassIsland 原有的行动处理与规则判断，因此此处只需注册元数据。
/// </summary>
internal static class SaiBlockRegistrar
{
    /// <summary>
    /// SuperAutoIsland 的插件 Id（见其 manifest.yml）。
    /// </summary>
    private const string SaiPluginId = "lrs2187.sai";

    /// <summary>
    /// 积木在 Blockly 工具箱中的分类名。
    /// </summary>
    private const string CategoryName = "AdvancedTimeIsland";

    /// <summary>
    /// 订阅应用启动事件，待所有插件加载完毕后再注册积木。
    /// 未安装或不启用 SuperAutoIsland 时静默跳过，不影响插件其余功能。
    /// </summary>
    public static void Register()
    {
        AppBase.Current.AppStarted += OnAppStarted;
    }

    private static void OnAppStarted(object? sender, EventArgs e)
    {
        var logger = GlobalConstants.HostInterfaces.PluginLogger;

        if (!IsSaiInstalled())
        {
            return;
        }

        // ISaiServer 由 SuperAutoIsland 在其插件初始化时注册进宿主容器。
        var server = IAppHost.TryGetService<ISaiServer>();
        if (server == null)
        {
            return;
        }

        try
        {
            server.RegisterBlocks(CategoryName, BuildRegisterData());
            logger?.LogInformation("已向 SuperAutoIsland 注册 Blockly 积木");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "向 SuperAutoIsland 注册 Blockly 积木失败");
        }
    }

    private static bool IsSaiInstalled()
    {
        try
        {
            return IPluginService.LoadedPlugins.Any(info =>
                info.Manifest.Id == SaiPluginId && info.IsEnabled);
        }
        catch
        {
            return false;
        }
    }

    private static RegisterData BuildRegisterData()
    {
        var settings = Plugin.Instance.Settings;

        var actions = new List<BlockMetadata>
        {
            NewBlock("advancedtimeisland.sync_classisland_time", "同步ClassIsland时间", "\uecc8", null),
            NewBlock("advancedtimeisland.sync_plugin_time", "同步AdvancedTimeIsland插件时间", "\uecc9", null),
            NewBlock("advancedtimeisland.set_floating_schedule", "设置悬浮时间表开关", "\uef27",
                typeof(SetFloatingScheduleActionSettings)),
        };

        var rules = new List<BlockMetadata>();

        void Add(string id, string name, string icon, Type settingsType) =>
            rules.Add(NewBlock(id, name, icon, settingsType));

        // ========== 基础时间范围 ==========
        Add("advancedtimeisland.exact_time_range", "精确时间在范围", "\uecc2", typeof(ExactTimeRangeRuleSettings));
        Add("advancedtimeisland.yearly_time_range", "每年时间范围", "\uecc3", typeof(YearlyTimeRangeRuleSettings));
        Add("advancedtimeisland.monthly_time_range", "每月时间范围", "\uecc4", typeof(MonthlyTimeRangeRuleSettings));
        Add("advancedtimeisland.daily_time_range", "每天时间范围", "\uecc5", typeof(DailyTimeRangeRuleSettings));
        Add("advancedtimeisland.hourly_time_range", "每小时时间范围", "\uecc6", typeof(HourlyTimeRangeRuleSettings));
        Add("advancedtimeisland.minutely_time_range", "每分钟时间范围", "\uecc7", typeof(MinutelyTimeRangeRuleSettings));
        Add("advancedtimeisland.weekly_time_range", "每周时间范围", "\uecd0", typeof(WeeklyTimeRangeRuleSettings));
        Add("advancedtimeisland.unix_timestamp_range", "绝对时间范围", "\ueceb", typeof(UnixTimestampRangeRuleSettings));

        // ========== 地方时（与主程序一致，仅在启用"地方时"时注册）==========
        if (settings.EnableLocalSolarTime)
        {
            Add("advancedtimeisland.local_solar_exact_time_range", "地方时精确时间在范围", "\uecc2", typeof(LocalSolarExactTimeRuleSettings));
            Add("advancedtimeisland.local_solar_yearly_time_range", "地方时每年时间范围", "\uecc3", typeof(LocalSolarYearlyTimeRangeRuleSettings));
            Add("advancedtimeisland.local_solar_monthly_time_range", "地方时每月时间范围", "\uecc4", typeof(LocalSolarMonthlyTimeRangeRuleSettings));
            Add("advancedtimeisland.local_solar_daily_time_range", "地方时每天时间范围", "\uecc5", typeof(LocalSolarDailyTimeRangeRuleSettings));
            Add("advancedtimeisland.local_solar_hourly_time_range", "地方时每小时时间范围", "\uecc6", typeof(LocalSolarHourlyTimeRangeRuleSettings));
            Add("advancedtimeisland.local_solar_minutely_time_range", "地方时每分钟时间范围", "\uecc7", typeof(LocalSolarMinutelyTimeRangeRuleSettings));
            Add("advancedtimeisland.local_solar_weekly_time_range", "地方时每周时间范围", "\uecd5", typeof(LocalSolarWeeklyTimeRangeRuleSettings));
        }

        // ========== 区时（与主程序一致，仅在启用"区时"时注册）==========
        if (settings.EnableTimeZoneTime)
        {
            Add("advancedtimeisland.time_zone_exact_time_range", "区时精确时间在范围", "\uecc2", typeof(TimeZoneExactTimeRuleSettings));
            Add("advancedtimeisland.time_zone_yearly_time_range", "区时每年时间范围", "\uecc3", typeof(TimeZoneYearlyTimeRangeRuleSettings));
            Add("advancedtimeisland.time_zone_monthly_time_range", "区时每月时间范围", "\uecc4", typeof(TimeZoneMonthlyTimeRangeRuleSettings));
            Add("advancedtimeisland.time_zone_daily_time_range", "区时每天时间范围", "\uecc5", typeof(TimeZoneDailyTimeRangeRuleSettings));
            Add("advancedtimeisland.time_zone_hourly_time_range", "区时每小时时间范围", "\uecc6", typeof(TimeZoneHourlyTimeRangeRuleSettings));
            Add("advancedtimeisland.time_zone_weekly_time_range", "区时每周时间范围", "\uecd6", typeof(TimeZoneWeeklyTimeRangeRuleSettings));
        }

        // ========== 农历（与主程序一致，仅在启用"农历"时注册）==========
        if (settings.EnableLunarCalendar)
        {
            Add("advancedtimeisland.lunar_exact_time_in_range", "农历精确时间范围", "\uece8", typeof(LunarExactTimeRangeRuleSettings));
            Add("advancedtimeisland.lunar_yearly_time_in_range", "农历每年时间范围", "\uece9", typeof(LunarYearlyTimeRangeRuleSettings));
            Add("advancedtimeisland.lunar_monthly_time_in_range", "农历每月时间范围", "\uecea", typeof(LunarMonthlyTimeRangeRuleSettings));
        }

        // ========== 星座 / 节气 / 生肖 / 节日 ==========
        if (settings.EnableXingZuo)
        {
            Add("advancedtimeisland.xingzuo", "当前星座是", "\uE120", typeof(XingZuoRuleSettings));
        }

        if (settings.EnableJieQi)
        {
            Add("advancedtimeisland.jieqi", "当前节气是", "\uE123", typeof(JieQiRuleSettings));
        }

        if (settings.EnableShengXiao)
        {
            Add("advancedtimeisland.shengxiao", "当前生肖是", "\uE124", typeof(ShengXiaoRuleSettings));
        }

        if (settings.EnableFestival)
        {
            Add("advancedtimeisland.festival", "当前节日是", "\uE125", typeof(FestivalRuleSettings));
        }

        return new RegisterData
        {
            Actions = actions,
            Rules = rules,
            Data = [],
        };
    }

    private static BlockMetadata NewBlock(string id, string name, string glyph, Type? settingsType) => new()
    {
        Id = id,
        Name = name,
        Icon = (name, glyph),
        Args = BuildArgs(settingsType),
    };

    /// <summary>
    /// 依据设置类的公开属性生成积木参数元数据。
    /// 键即设置类属性名，也是 ClassIsland 设置 JSON 的字段名，SuperAutoIsland 会原样回传给行动/规则。
    /// </summary>
    private static Dictionary<string, MetaArgsBase> BuildArgs(Type? settingsType)
    {
        var args = new Dictionary<string, MetaArgsBase>();
        if (settingsType == null)
        {
            return args;
        }

        foreach (var property in settingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || ExcludedFields.Contains(property.Name))
            {
                continue;
            }

            var metaType = GetMetaType(property.PropertyType);
            if (metaType == null)
            {
                continue;
            }

            args[property.Name] = new CommonMetaArgs
            {
                Name = FieldLabels.TryGetValue(property.Name, out var label) ? label : property.Name,
                Type = metaType.Value,
            };
        }

        return args;
    }

    private static MetaType? GetMetaType(Type type)
    {
        if (type == typeof(string))
        {
            return MetaType.text;
        }

        if (type == typeof(bool))
        {
            return MetaType.boolean;
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
            || type == typeof(double) || type == typeof(float) || type == typeof(decimal))
        {
            return MetaType.number;
        }

        return null;
    }

    /// <summary>
    /// 仅供界面派生状态使用、不参与规则判断的属性，不暴露为积木参数。
    /// </summary>
    private static readonly HashSet<string> ExcludedFields = new()
    {
        "StartLunarYearRangeEnd",
        "EndLunarYearRangeEnd",
        "LunarYearRangeEnd",
    };

    /// <summary>
    /// 积木参数字段的中文名（键为设置类属性名，缺失时回退为属性名本身）。
    /// </summary>
    private static readonly Dictionary<string, string> FieldLabels = new()
    {
        ["StartTime"] = "开始时间",
        ["EndTime"] = "结束时间",
        ["StartSecond"] = "开始秒",
        ["EndSecond"] = "结束秒",
        ["StartDayOfWeek"] = "开始星期(0-6，0=周日)",
        ["EndDayOfWeek"] = "结束星期(0-6，0=周日)",
        ["TimeZoneId"] = "时区 Id",
        ["TimeZone"] = "时区",
        ["Longitude"] = "经度",
        ["TargetXingZuo"] = "目标星座",
        ["TargetJieQi"] = "目标节气",
        ["TargetShengXiao"] = "目标生肖",
        ["TargetFestival"] = "目标节日",
        ["TargetTime"] = "目标时间",
        ["StartTimestamp"] = "开始时间戳",
        ["EndTimestamp"] = "结束时间戳",
        ["LunarYear"] = "农历年",
        ["LunarMonth"] = "农历月",
        ["LunarDay"] = "农历日",
        ["IsLeapMonth"] = "闰月",
        ["StartMonth"] = "开始月",
        ["StartDay"] = "开始日",
        ["StartIsLeapMonth"] = "开始闰月",
        ["EndMonth"] = "结束月",
        ["EndDay"] = "结束日",
        ["EndIsLeapMonth"] = "结束闰月",
        ["StartLunarYear"] = "开始农历年",
        ["StartLunarMonth"] = "开始农历月",
        ["StartLunarDay"] = "开始农历日",
        ["StartTargetTime"] = "开始时间",
        ["EndLunarYear"] = "结束农历年",
        ["EndLunarMonth"] = "结束农历月",
        ["EndLunarDay"] = "结束农历日",
        ["EndTargetTime"] = "结束时间",
        ["DaysFromEnd"] = "距月末天数",
        ["Enabled"] = "开启",
    };
}
