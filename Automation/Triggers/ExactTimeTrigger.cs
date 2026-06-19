﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.Automation.Triggers;

/// <summary>
/// 精确时间触发器
/// 支持精确时间(YYYY-MM-DD-hh-mm-ss)、绝对时间(Unix)、周期性触发
/// </summary>
public class ExactTimeTrigger : INotifyPropertyChanged
{
    private readonly TimeBaseService _timeBaseService;
    private string _targetTimeString = string.Empty;
    private long _targetUnixTimestamp;
    private int _periodicIntervalSeconds;
    private TriggerMode _mode = TriggerMode.ExactTime;
    private bool _isEnabled = true;
    private bool _hasTriggered;

    public ExactTimeTrigger(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;
    }

    /// <summary>
    /// 触发模式
    /// </summary>
    public enum TriggerMode
    {
        /// <summary>
        /// 精确时间模式 (YYYY-MM-DD-hh-mm-ss)
        /// </summary>
        ExactTime,
        
        /// <summary>
        /// Unix时间戳模式（秒）
        /// </summary>
        UnixTimestamp,
        
        /// <summary>
        /// 周期性触发模式
        /// </summary>
        Periodic
    }

    /// <summary>
    /// 触发模式
    /// </summary>
    public TriggerMode Mode
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
    /// 目标时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string TargetTimeString
    {
        get => _targetTimeString;
        set
        {
            if (_targetTimeString != value)
            {
                _targetTimeString = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 目标Unix时间戳（秒）
    /// </summary>
    public long TargetUnixTimestamp
    {
        get => _targetUnixTimestamp;
        set
        {
            if (_targetUnixTimestamp != value)
            {
                _targetUnixTimestamp = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 周期性触发间隔（秒）
    /// </summary>
    public int PeriodicIntervalSeconds
    {
        get => _periodicIntervalSeconds;
        set
        {
            if (_periodicIntervalSeconds != value)
            {
                _periodicIntervalSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 检查是否应该触发
    /// </summary>
    /// <returns>是否触发</returns>
    public async Task<bool> CheckTriggerAsync()
    {
        if (!IsEnabled)
            return false;

        var currentTime = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
        var currentTimestamp = UnixTimeHelper.ToUnixTimestamp(currentTime);

        return Mode switch
        {
            TriggerMode.ExactTime => CheckExactTime(currentTime),
            TriggerMode.UnixTimestamp => CheckUnixTimestamp(currentTimestamp),
            TriggerMode.Periodic => CheckPeriodic(currentTimestamp),
            _ => false
        };
    }

    /// <summary>
    /// 检查精确时间触发
    /// </summary>
    private bool CheckExactTime(DateTime currentTime)
    {
        if (string.IsNullOrEmpty(TargetTimeString))
            return false;

        if (!UnixTimeHelper.TryParseExactTime(TargetTimeString, out var targetTime))
            return false;

        // 检查是否到达目标时间（精确到秒）
        if (currentTime.Year == targetTime.Year &&
            currentTime.Month == targetTime.Month &&
            currentTime.Day == targetTime.Day &&
            currentTime.Hour == targetTime.Hour &&
            currentTime.Minute == targetTime.Minute &&
            currentTime.Second == targetTime.Second)
        {
            if (!_hasTriggered)
            {
                _hasTriggered = true;
                return true;
            }
        }
        else
        {
            _hasTriggered = false;
        }

        return false;
    }

    /// <summary>
    /// 检查Unix时间戳触发
    /// </summary>
    private bool CheckUnixTimestamp(long currentTimestamp)
    {
        if (TargetUnixTimestamp <= 0)
            return false;

        // 检查是否到达或超过目标时间戳
        if (currentTimestamp >= TargetUnixTimestamp && !_hasTriggered)
        {
            _hasTriggered = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查周期性触发
    /// </summary>
    private bool CheckPeriodic(long currentTimestamp)
    {
        if (PeriodicIntervalSeconds <= 0)
            return false;

        // 检查是否到达周期触发时间
        if (currentTimestamp % PeriodicIntervalSeconds == 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 重置触发状态
    /// </summary>
    public void Reset()
    {
        _hasTriggered = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
