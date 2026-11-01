using System;
using System.Threading.Tasks;
using AdvancedTimeIsland.Shared;
using ClassIsland.Core.Abstractions.Services;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 时间基准服务：插件内所有时间统一取 ClassIsland 的时间（含宿主调试/全局偏移）。
/// 插件不再维护独立的时间同步与时间偏移，因此本服务只是 ClassIsland 时间源的薄封装。
/// </summary>
public class TimeBaseService
{
    public static TimeBaseService? Instance { get; private set; }

    private static IExactTimeService? ExactTimeService => GlobalConstants.HostInterfaces.ExactTimeService;

    public TimeBaseService()
    {
        Instance = this;
    }

    /// <summary>
    /// 获取ClassIsland时间
    /// </summary>
    public DateTime GetClassIslandTime()
    {
        var exactTimeService = ExactTimeService;
        if (exactTimeService != null)
        {
            try
            {
                return exactTimeService.GetCurrentLocalDateTime();
            }
            catch
            {
                // ignore
            }
        }
        return DateTime.Now;
    }

    /// <summary>
    /// 异步获取ClassIsland时间
    /// </summary>
    /// <remarks>取值为纯内存运算，直接返回已完成任务，避免高频 tick await 时占用线程池。</remarks>
    public Task<DateTime> GetClassIslandTimeAsync()
    {
        return Task.FromResult(GetClassIslandTime());
    }

    /// <summary>
    /// 获取当前时间（等同ClassIsland时间）
    /// </summary>
    public DateTime GetCurrentTime()
    {
        return GetClassIslandTime();
    }

    /// <summary>
    /// 异步获取当前时间
    /// </summary>
    public Task<DateTime> GetCurrentTimeAsync()
    {
        return Task.FromResult(GetCurrentTime());
    }

    /// <summary>
    /// 获取精确时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string GetExactTimeString()
    {
        return GetCurrentTime().ToString("yyyy-MM-dd-HH-mm-ss");
    }

    /// <summary>
    /// 异步获取精确时间字符串
    /// </summary>
    public async Task<string> GetExactTimeStringAsync()
    {
        var time = await GetCurrentTimeAsync().ConfigureAwait(false);
        return time.ToString("yyyy-MM-dd-HH-mm-ss");
    }

    /// <summary>
    /// 获取当前 Unix 时间戳（秒）
    /// </summary>
    public long GetCurrentUnixTimestamp()
    {
        return Helpers.UnixTimeHelper.ToUnixTimestamp(GetCurrentTime());
    }

    /// <summary>
    /// 异步获取当前 Unix 时间戳（秒）
    /// </summary>
    public async Task<long> GetCurrentUnixTimestampAsync()
    {
        var time = await GetCurrentTimeAsync().ConfigureAwait(false);
        return Helpers.UnixTimeHelper.ToUnixTimestamp(time);
    }
}
