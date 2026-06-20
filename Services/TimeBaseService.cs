using System;
using System.Threading.Tasks;
using AdvancedTimeIsland.Models;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 时间基准管理服务
/// 基于系统时间并应用插件独立的时间偏移
/// </summary>
public class TimeBaseService
{
    private readonly PluginSettings _settings;

    public TimeBaseService(PluginSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// 获取当前时间（系统时间 + 插件时间偏移）
    /// </summary>
    /// <returns>当前时间</returns>
    public DateTime GetCurrentTime()
    {
        try
        {
            var baseTime = DateTime.Now;
            var offset = TimeSpan.FromSeconds(_settings.TimeOffsetSeconds);
            return baseTime.Add(offset);
        }
        catch
        {
            return DateTime.Now;
        }
    }

    /// <summary>
    /// 异步获取当前时间
    /// </summary>
    /// <returns>当前时间</returns>
    public async Task<DateTime> GetCurrentTimeAsync()
    {
        return await Task.Run(() => GetCurrentTime()).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取精确时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    /// <returns>精确时间字符串</returns>
    public string GetExactTimeString()
    {
        return GetCurrentTime().ToString("yyyy-MM-dd-HH-mm-ss");
    }

    /// <summary>
    /// 异步获取精确时间字符串
    /// </summary>
    /// <returns>精确时间字符串</returns>
    public async Task<string> GetExactTimeStringAsync()
    {
        var time = await GetCurrentTimeAsync().ConfigureAwait(false);
        return time.ToString("yyyy-MM-dd-HH-mm-ss");
    }

    /// <summary>
    /// 获取当前 Unix 时间戳（秒）
    /// </summary>
    /// <returns>Unix 时间戳</returns>
    public long GetCurrentUnixTimestamp()
    {
        return Helpers.UnixTimeHelper.ToUnixTimestamp(GetCurrentTime());
    }

    /// <summary>
    /// 异步获取当前 Unix 时间戳（秒）
    /// </summary>
    /// <returns>Unix 时间戳</returns>
    public async Task<long> GetCurrentUnixTimestampAsync()
    {
        var time = await GetCurrentTimeAsync().ConfigureAwait(false);
        return Helpers.UnixTimeHelper.ToUnixTimestamp(time);
    }
}