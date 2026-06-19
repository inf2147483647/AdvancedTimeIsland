﻿﻿﻿﻿﻿using Microsoft.Extensions.DependencyInjection;
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

        services.AddSingleton<AdvancedDateViewModel>();
        services.AddSingleton<AdvancedDateControl>();

        services.AddSingleton<ExactTimeTrigger>();

        services.AddSingleton<TimeRangeCondition>();

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

        services.AddSettingsPage<Views.Settings.AboutPage>();
    }
}
