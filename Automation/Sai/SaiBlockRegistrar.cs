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
        Add("advancedtimeisland.specified_time_range", "在指定时间范围内", "\uecc3", typeof(SpecifiedTimeRangeRuleSettings));

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
        Args = BuildArgs(id, settingsType),
    };

    /// <summary>
    /// 依据设置类的公开属性生成积木参数元数据。
    /// 键即设置类属性名，也是 ClassIsland 设置 JSON 的字段名，SuperAutoIsland 会原样回传给行动/规则。
    /// </summary>
    private static Dictionary<string, MetaArgsBase> BuildArgs(string blockId, Type? settingsType)
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

            var name = ResolveFieldLabel(blockId, property.Name);

            // 枚举类字段：改成下拉选择，用户无需手打完整字符串。
            if (DropdownOptions.TryGetValue(property.Name, out var options))
            {
                args[property.Name] = new DropDownMetaArgs
                {
                    Name = name,
                    Type = MetaType.dropdown,
                    // 选项的显示文本与取值相同，因此与 SuperAutoIsland 的 (标签, 取值) 顺序无关。
                    Options = options.Select(value => (value, value)).ToList(),
                };
                continue;
            }

            var metaType = GetMetaType(property.PropertyType);
            if (metaType == null)
            {
                continue;
            }

            args[property.Name] = new CommonMetaArgs
            {
                Name = name,
                Type = metaType.Value,
            };
        }

        return args;
    }

    /// <summary>
    /// 解析字段在积木上显示的名称：优先使用该积木专属的格式模板，其次使用通用中文名，最后回退到属性名。
    /// </summary>
    private static string ResolveFieldLabel(string blockId, string propertyName)
    {
        // 时间类字段的字符串格式因积木而异（精确到年/月/日/时…），把完整格式与示例写进字段名。
        if (propertyName is "StartTime" or "EndTime" or "StartTargetTime" or "EndTargetTime" &&
            BlockTimeFormat.TryGetValue(blockId, out var format))
        {
            var prefix = propertyName.Contains("End") ? "结束时间" : "开始时间";
            return $"{prefix}({format})";
        }

        return FieldLabels.TryGetValue(propertyName, out var label) ? label : propertyName;
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
        // 每周期目前仅一个单位（恒为 0），对用户无意义，不暴露为积木参数。
        "Unit",
    };

    /// <summary>
    /// 枚举类字段的下拉选项（键为设置类属性名）。显示文本与取值相同，故与 (标签, 取值) 顺序无关。
    /// 取值与桌面端各设置控件中 ComboBox 的选项保持一致：星座 / 节气 / 生肖 / 节日。
    /// </summary>
    private static readonly Dictionary<string, string[]> DropdownOptions = new()
    {
        ["TargetXingZuo"] = new[]
        {
            "白羊座", "金牛座", "双子座", "巨蟹座", "狮子座", "处女座",
            "天秤座", "天蝎座", "射手座", "摩羯座", "水瓶座", "双鱼座",
        },
        ["TargetJieQi"] = new[]
        {
            "立春", "雨水", "惊蛰", "春分", "清明", "谷雨",
            "立夏", "小满", "芒种", "夏至", "小暑", "大暑",
            "立秋", "处暑", "白露", "秋分", "寒露", "霜降",
            "立冬", "小雪", "大雪", "冬至", "小寒", "大寒",
        },
        ["TargetShengXiao"] = new[] { "鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪" },
        ["TargetFestival"] = new[]
        {
            "元旦 (1月1日)", "妇女节 (3月8日)", "植树节 (3月12日)", "劳动节 (5月1日)", "儿童节 (6月1日)",
            "教师节 (9月10日)", "清明节 (公历4月4-6日)", "冬至 (公历12月21-23日)",
            "春节 (农历正月初一)", "元宵节 (农历正月十五)", "寒食节 (清明前一日)", "端午节 (农历五月初五)",
            "七夕节 (农历七月初七)", "中元节 (农历七月十五)", "中秋节 (农历八月十五)", "重阳节 (农历九月初九)",
            "腊八节 (农历十二月初八)", "小年 (农历十二月二十三)", "除夕 (农历十二月三十)",
            "二七纪念日 (2月7日)", "学雷锋纪念日 (3月5日)", "五四青年节 (5月4日)", "七一建党节 (7月1日)",
            "八一建军节 (8月1日)", "中国人民抗日战争胜利纪念日 (9月3日)", "九一八事变纪念日 (9月18日)",
            "烈士纪念日 (9月30日)", "十一国庆节 (10月1日)", "中国工农红军长征胜利纪念日 (10月22日)",
            "南京大屠杀死难者国家公祭日 (12月13日)",
        },
    };

    /// <summary>
    /// 各“时间范围”积木的时间字段格式模板（键为积木 Id）。
    /// 不同积木的 StartTime/EndTime 精度不同（精确到年 / 月 / 日 / 时…），需按积木给出对应格式与示例。
    /// </summary>
    private static readonly Dictionary<string, string> BlockTimeFormat = new()
    {
        ["advancedtimeisland.exact_time_range"] = "年-月-日-时-分-秒，例 2026-10-05-08-00-00",
        ["advancedtimeisland.local_solar_exact_time_range"] = "年-月-日-时-分-秒，例 2026-10-05-08-00-00",
        ["advancedtimeisland.time_zone_exact_time_range"] = "年-月-日-时-分-秒，例 2026-10-05-08-00-00",
        ["advancedtimeisland.yearly_time_range"] = "月-日-时-分-秒，例 10-05-08-00-00",
        ["advancedtimeisland.local_solar_yearly_time_range"] = "月-日-时-分-秒，例 10-05-08-00-00",
        ["advancedtimeisland.time_zone_yearly_time_range"] = "月-日-时-分-秒，例 10-05-08-00-00",
        ["advancedtimeisland.monthly_time_range"] = "日-时-分-秒，例 05-08-00-00",
        ["advancedtimeisland.local_solar_monthly_time_range"] = "日-时-分-秒，例 05-08-00-00",
        ["advancedtimeisland.time_zone_monthly_time_range"] = "日-时-分-秒，例 05-08-00-00",
        ["advancedtimeisland.daily_time_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.local_solar_daily_time_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.time_zone_daily_time_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.hourly_time_range"] = "分-秒，例 30-00",
        ["advancedtimeisland.local_solar_hourly_time_range"] = "分-秒，例 30-00",
        ["advancedtimeisland.time_zone_hourly_time_range"] = "分-秒，例 30-00",
        ["advancedtimeisland.weekly_time_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.local_solar_weekly_time_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.time_zone_weekly_time_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.lunar_exact_time_in_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.lunar_yearly_time_in_range"] = "时-分-秒，例 08-00-00",
        ["advancedtimeisland.lunar_monthly_time_in_range"] = "时-分-秒，例 08-00-00",
    };

    /// <summary>
    /// 积木参数字段的中文名（键为设置类属性名，缺失时回退为属性名本身）。
    /// </summary>
    private static readonly Dictionary<string, string> FieldLabels = new()
    {
        ["StartTime"] = "开始时间",
        ["EndTime"] = "结束时间",
        ["StartSecond"] = "开始秒(0-59)",
        ["EndSecond"] = "结束秒(0-59)",
        ["StartDayOfWeek"] = "开始星期(0-6，0=周一)",
        ["EndDayOfWeek"] = "结束星期(0-6，0=周一)",
        ["TimeZoneId"] = "时区 Id",
        ["TimeZone"] = "时区",
        ["Longitude"] = "经度",
        ["TargetXingZuo"] = "目标星座",
        ["TargetJieQi"] = "目标节气",
        ["TargetShengXiao"] = "目标生肖",
        ["TargetFestival"] = "目标节日",
        ["TargetTime"] = "目标时间",
        ["Period"] = "周期(0=每年 1=每月 2=每周 3=每天 4=每小时 5=每分钟)",
        ["Unit"] = "单位序号",
        ["Expression"] = "范围表达式(如 1,3-5,7~11)",
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
