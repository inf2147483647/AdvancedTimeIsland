using System;
using AdvancedTimeIsland.Models;
using Microsoft.Extensions.Options;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 时间基准服务，管理当前使用的时间源（ClassIsland时间/系统时间）
/// </summary>
public class TimeBaseService
{
    private readonly PluginSettings _settings;

    public TimeBaseService(IOptions<PluginSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>
    /// 获取当前的时间，根据设置的时间基准
    /// </summary>
    /// <returns>当前时间</returns>
    public DateTime GetCurrentTime()
    {
        if (_settings.TimeBase == "ClassIsland")
        {
            // TODO: 调用ClassIsland的时间API获取当前应用内的时间
            // 这里暂时先返回系统时间，后续接入ClassIsland的SDK接口
            return DateTime.Now;
        }
        
        // 系统时间
        return DateTime.Now;
    }
}