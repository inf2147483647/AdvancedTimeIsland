﻿using System;
using System.Threading.Tasks;
using AdvancedTimeIsland.Models;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 时间基准管理服务
/// 用于切换 ClassIsland 时间和系统时间
/// </summary>
public class TimeBaseService
{
    private readonly PluginSettings _settings;
    private DateTime? _classIslandTimeOffset;

    public TimeBaseService(PluginSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// 获取当前时间（根据设置的时间基准）
    /// </summary>
    /// <returns>当前时间</returns>
    public DateTime GetCurrentTime()
    {
        return _settings.TimeBaseMode switch
        {
            0 => GetClassIslandTime(),
            1 => DateTime.Now,
            _ => DateTime.Now
        };
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
    /// 获取 ClassIsland 时间
    /// 如果无法获取 ClassIsland 时间，则返回系统时间
    /// </summary>
    /// <returns>ClassIsland 时间或系统时间</returns>
    private DateTime GetClassIslandTime()
    {
        try
        {
            // 尝试从 ClassIsland 获取时间
            // TODO: 当 ClassIsland SDK 提供时间 API 时，在此处调用
            // 目前返回系统时间作为后备
            return DateTime.Now;
        }
        catch
        {
            // 如果获取失败，返回系统时间
            return DateTime.Now;
        }
    }

    /// <summary>
    /// 设置 ClassIsland 时间偏移
    /// </summary>
    /// <param name="offset">时间偏移</param>
    public void SetClassIslandTimeOffset(TimeSpan offset)
    {
        _classIslandTimeOffset = DateTime.Now.Add(offset);
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
