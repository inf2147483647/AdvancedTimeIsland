﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.Automation.Triggers;

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

    public enum TriggerMode
    {
        ExactTime,
        UnixTimestamp,
        Periodic
    }

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

    private bool CheckExactTime(DateTime currentTime)
    {
        if (string.IsNullOrEmpty(TargetTimeString))
            return false;

        if (!UnixTimeHelper.TryParseExactTime(TargetTimeString, out var targetTime))
            return false;

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

    private bool CheckUnixTimestamp(long currentTimestamp)
    {
        if (TargetUnixTimestamp <= 0)
            return false;

        if (currentTimestamp >= TargetUnixTimestamp && !_hasTriggered)
        {
            _hasTriggered = true;
            return true;
        }

        return false;
    }

    private bool CheckPeriodic(long currentTimestamp)
    {
        if (PeriodicIntervalSeconds <= 0)
            return false;

        if (currentTimestamp % PeriodicIntervalSeconds == 0)
        {
            return true;
        }

        return false;
    }

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