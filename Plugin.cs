using Microsoft.Extensions.DependencyInjection;
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

        services.AddSettingsPage<Views.Settings.AboutPage>();
    }
}
