using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassIsland.Core.Attributes;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Views.Main;
using AdvancedTimeIsland.Automation.Triggers;
using AdvancedTimeIsland.Automation.Conditions;

namespace AdvancedTimeIsland;

public interface IPlugin
{
    void OnServiceConfiguring(HostBuilderContext context, IServiceCollection services);
    void Initialize();
}

[PluginEntrance]
public class Plugin : IPlugin
{
    public static Guid PluginId => new("11223344-5566-7788-9900-aabbccddeeff");
    
    public void OnServiceConfiguring(HostBuilderContext context, IServiceCollection services)
    {
        // 注册配置
        services.Configure<PluginSettings>(context.Configuration.GetSection("AdvancedTimeIsland"));
        
        // 注册核心服务
        services.AddSingleton<TimeBaseService>();
        
        // 注册主界面组件
        services.AddSingleton<AdvancedDateControl>();
        
        // 注册自动化触发器
        // TODO: 接入SuperAutoIsland的触发器注册API
        services.AddSingleton<ExactTimeTrigger>();
        
        // 注册自动化条件
        // TODO: 接入SuperAutoIsland的条件注册API
        services.AddSingleton<TimeRangeCondition>();
    }

    public void Initialize()
    {
        // 插件初始化逻辑
    }
}