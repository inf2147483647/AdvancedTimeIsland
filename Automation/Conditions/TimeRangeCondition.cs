﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.Automation.Conditions;

/// <summary>
/// 时间范围条件
/// 支持公历/Unix时间范围判断
/// </summary>
public class TimeRangeCondition : INotifyPropertyChanged
{
    private readonly TimeBaseService _timeBaseService;
    private string _startTimeString = string.Empty;
    private string _endTimeString = string.Empty;
    private long _startUnixTimestamp;
    private long _endUnixTimestamp;
    private ConditionMode _mode = ConditionMode.ExactTime;
    private bool _isInclusive = true;

    public TimeRangeCondition(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;
    }

    /// <summary>
    /// 条件模式
    /// </summary>
    public enum ConditionMode
    {
        /// <summary>
        /// 精确时间模式 (YYYY-MM-DD-hh-mm-ss)
        /// </summary>
        ExactTime,
        
        /// <summary>
        /// Unix时间戳模式（秒）
        /// </summary>
        UnixTimestamp
    }

    /// <summary>
    /// 条件模式
    /// </summary>
    public ConditionMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 开始时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string StartTimeString
    {
        get => _startTimeString;
        set
        {
            if (_startTimeString != value)
            {
                _startTimeString = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 结束时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string EndTimeString
    {
        get => _endTimeString;
        set
        {
            if (_endTimeString != value)
            {
                _endTimeString = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 开始Unix时间戳（秒）
    /// </summary>
    public long StartUnixTimestamp
    {
        get => _startUnixTimestamp;
        set
        {
            if (_startUnixTimestamp != value)
            {
                _startUnixTimestamp = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 结束Unix时间戳（秒）
    /// </summary>
    public long EndUnixTimestamp
    {
        get => _endUnixTimestamp;
        set
        {
            if (_endUnixTimestamp != value)
            {
                _endUnixTimestamp = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否包含边界
    /// </summary>
    public bool IsInclusive
    {
        get => _isInclusive;
        set
        {
            if (_isInclusive != value)
            {
                _isInclusive = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 异步检查当前时间是否在范围内
    /// </summary>
    /// <returns>是否在范围内</returns>
    public async Task<bool> CheckConditionAsync()
    {
        var currentTime = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
        var currentTimestamp = UnixTimeHelper.ToUnixTimestamp(currentTime);

        return Mode switch
        {
            ConditionMode.ExactTime => CheckExactTimeRange(currentTime),
            ConditionMode.UnixTimestamp => CheckUnixTimestampRange(currentTimestamp),
            _ => false
        };
    }

    /// <summary>
    /// 检查精确时间范围
    /// </summary>
    private bool CheckExactTimeRange(DateTime currentTime)
    {
        if (string.IsNullOrEmpty(StartTimeString) || string.IsNullOrEmpty(EndTimeString))
            return false;

        if (!UnixTimeHelper.TryParseExactTime(StartTimeString, out var startTime) ||
            !UnixTimeHelper.TryParseExactTime(EndTimeString, out var endTime))
            return false;

        return IsInclusive
            ? currentTime >= startTime && currentTime <= endTime
            : currentTime > startTime && currentTime < endTime;
    }

    /// <summary>
    /// 检查Unix时间戳范围
    /// </summary>
    private bool CheckUnixTimestampRange(long currentTimestamp)
    {
        if (StartUnixTimestamp <= 0 || EndUnixTimestamp <= 0)
            return false;

        return IsInclusive
            ? currentTimestamp >= StartUnixTimestamp && currentTimestamp <= EndUnixTimestamp
            : currentTimestamp > StartUnixTimestamp && currentTimestamp < EndUnixTimestamp;
    }

    /// <summary>
    /// 获取条件描述
    /// </summary>
    /// <returns>条件描述字符串</returns>
    public string GetConditionDescription()
    {
        return Mode switch
        {
            ConditionMode.ExactTime => $"精确时间在范围 [{StartTimeString}] ~ [{EndTimeString}]",
            ConditionMode.UnixTimestamp => $"Unix时间戳在范围 [{StartUnixTimestamp}] ~ [{EndUnixTimestamp}]",
            _ => "未知条件"
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
