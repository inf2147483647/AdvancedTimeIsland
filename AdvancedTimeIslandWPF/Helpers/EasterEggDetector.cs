using System;
using System.Collections.Generic;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 彩蛋检测器
/// 实现5秒内点击11次的状态机
/// </summary>
public class EasterEggDetector
{
    private readonly List<DateTime> _clickTimes = new();
    private readonly int _requiredClicks;
    private readonly int _timeWindowSeconds;

    /// <summary>
    /// 是否已激活彩蛋
    /// </summary>
    public bool IsActivated { get; private set; }

    /// <summary>
    /// 彩蛋激活事件
    /// </summary>
    public event EventHandler? OnActivated;

    /// <summary>
    /// 创建彩蛋检测器
    /// </summary>
    /// <param name="requiredClicks">需要的点击次数（默认11）</param>
    /// <param name="timeWindowSeconds">时间窗口（秒，默认5）</param>
    public EasterEggDetector(int requiredClicks = 11, int timeWindowSeconds = 5)
    {
        _requiredClicks = requiredClicks;
        _timeWindowSeconds = timeWindowSeconds;
    }

    /// <summary>
    /// 记录一次点击
    /// </summary>
    public void RecordClick()
    {
        if (IsActivated) return;

        var now = DateTime.Now;

        // 清理超过时间窗口的记录
        _clickTimes.RemoveAll(t => (now - t).TotalSeconds > _timeWindowSeconds);

        // 添加当前点击
        _clickTimes.Add(now);

        // 检查是否达到激活条件
        if (_clickTimes.Count >= _requiredClicks)
        {
            IsActivated = true;
            OnActivated?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 重置彩蛋状态
    /// </summary>
    public void Reset()
    {
        _clickTimes.Clear();
        IsActivated = false;
    }

    /// <summary>
    /// 获取当前点击次数
    /// </summary>
    public int GetClickCount()
    {
        var now = DateTime.Now;
        _clickTimes.RemoveAll(t => (now - t).TotalSeconds > _timeWindowSeconds);
        return _clickTimes.Count;
    }
}



