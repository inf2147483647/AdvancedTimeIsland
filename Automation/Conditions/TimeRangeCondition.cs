using System;
using System.Threading.Tasks;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.Automation.Conditions;

/// <summary>
/// 时间范围条件：检查当前时间是否在指定的时间范围内
/// </summary>
public class TimeRangeCondition
{
    private readonly TimeBaseService _timeBaseService;

    /// <summary>
    /// 条件唯一ID
    /// </summary>
    public static Guid ConditionId => new("87654321-4321-4321-4321-210987654321");

    /// <summary>
    /// 条件显示名称
    /// </summary>
    public static string DisplayName => "精确时间在范围[YYYY-MM-DD-hh-mm-ss]₁ ~ [YYYY-MM-DD-hh-mm-ss]₂内";

    /// <summary>
    /// 范围开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 范围结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    public TimeRangeCondition(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;
    }

    /// <summary>
    /// 异步检查是否满足条件
    /// </summary>
    /// <returns>是否满足条件</returns>
    public async Task<bool> CheckConditionAsync()
    {
        // 异步操作，避免阻塞UI线程
        await Task.Yield();
        
        var currentTime = _timeBaseService.GetCurrentTime();
        
        // 检查当前时间是否在范围内
        return currentTime >= StartTime && currentTime <= EndTime;
    }

    /// <summary>
    /// 解析时间范围字符串
    /// </summary>
    /// <param name="startStr">开始时间字符串</param>
    /// <param name="endStr">结束时间字符串</param>
    /// <param name="startTime">解析后的开始时间</param>
    /// <param name="endTime">解析后的结束时间</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParseRange(string startStr, string endStr, out DateTime startTime, out DateTime endTime)
    {
        startTime = default;
        endTime = default;
        
        if (!DateTime.TryParseExact(startStr, "yyyy-MM-dd-HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out startTime))
            return false;
            
        if (!DateTime.TryParseExact(endStr, "yyyy-MM-dd-HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out endTime))
            return false;
            
        return true;
    }
}
