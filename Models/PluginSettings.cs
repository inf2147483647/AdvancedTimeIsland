﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

/// <summary>
/// 插件全局设置
/// </summary>
public class PluginSettings : INotifyPropertyChanged
{
    private bool _enableLunarCalendar = true;
    private bool _isLunarInstalled = false;
    private double _timeOffsetSeconds = 0; // 时间偏移（秒），与ClassIsland时间独立
    private double _longitude = 116.4; // 默认北京经度
    private string _timeZoneId = "China Standard Time";
    private bool _enableCountdownNotification = true;
    private int _countdownAlertSeconds = 60;

    /// <summary>
    /// 是否启用农历功能
    /// </summary>
    public bool EnableLunarCalendar
    {
        get => _enableLunarCalendar;
        set
        {
            if (_enableLunarCalendar != value)
            {
                _enableLunarCalendar = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已安装农历组件
    /// </summary>
    public bool IsLunarInstalled
    {
        get => _isLunarInstalled;
        set
        {
            if (_isLunarInstalled != value)
            {
                _isLunarInstalled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 时间偏移（秒），与ClassIsland时间独立
    /// 增大偏移抵消铃声滞后，减小偏移抵消铃声提前
    /// </summary>
    public double TimeOffsetSeconds
    {
        get => _timeOffsetSeconds;
        set
        {
            if (Math.Abs(_timeOffsetSeconds - value) > 0.001)
            {
                _timeOffsetSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 经度（用于地方时计算）
    /// </summary>
    public double Longitude
    {
        get => _longitude;
        set
        {
            if (Math.Abs(_longitude - value) > 0.0001)
            {
                _longitude = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 时区ID
    /// </summary>
    public string TimeZoneId
    {
        get => _timeZoneId;
        set
        {
            if (_timeZoneId != value)
            {
                _timeZoneId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用倒计时通知
    /// </summary>
    public bool EnableCountdownNotification
    {
        get => _enableCountdownNotification;
        set
        {
            if (_enableCountdownNotification != value)
            {
                _enableCountdownNotification = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 倒计时提醒时间（秒）
    /// </summary>
    public int CountdownAlertSeconds
    {
        get => _countdownAlertSeconds;
        set
        {
            if (_countdownAlertSeconds != value)
            {
                _countdownAlertSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
