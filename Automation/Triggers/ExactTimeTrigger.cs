using System;
using System.Threading.Tasks;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.Automation.Triggers;

/// <summary>
/// 精确时间触发器：当时间到达指定的精确时间时触发
/// </summary>
public class ExactTimeTrigger
{
    private readonly TimeBaseService _timeBaseService;
    private bool _hasTriggered;

    /// <summary>
    /// 触发器唯一ID
    /// </summary>
    public static Guid TriggerId => new("12345678-1234-1234-1234-123456789012");

    /// <summary>
    /// 触发器显示名称
    /// </summary>
    public static string DisplayName => "精确时间是(YYYY-MM-DD-hh-mm-ss)";

    /// <summary>
    /// 要触发的精确时间
    /// </summary>
    public DateTime ExactTime { get; set; }

    public ExactTimeTrigger(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;
    }

    /// <summary>
    /// 异步检查是否应该触发触发器
    /// </summary>
    /// <returns>是否触发</returns>
    public async Task<bool> CheckTriggerAsync()
    {
        // 异步操作，避免阻塞，这里虽然是简单计算，也保持异步接口
        await Task.Yield();
        
        var currentTime = _timeBaseService.GetCurrentTime();
        
        // 检查时间是否到达，并且还未触发过
        if (currentTime >= ExactTime && !_hasTriggered)
        {
            _hasTriggered = true;
            return true;
        }
        
        // 如果时间已经过了，保持已触发状态，避免重复触发
        if (currentTime < ExactTime)
        {
            _hasTriggered = false;
        }
        
        return false;
    }

    /// <summary>
    /// 重置触发器状态，用于自动化引擎重置
    /// </summary>
    public void Reset()
    {
        _hasTriggered = false;
    }

    /// <summary>
    /// 从字符串解析时间，格式：yyyy-MM-dd-HH-mm-ss
    /// </summary>
    /// <param name="timeStr">时间字符串</param>
    /// <returns>解析后的时间</returns>
    public static bool TryParseTime(string timeStr, out DateTime time)
    {
        return DateTime.TryParseExact(
            timeStr, 
            "yyyy-MM-dd-HH-mm-ss", 
            null, 
            System.Globalization.DateTimeStyles.None, 
            out time);
    }
}