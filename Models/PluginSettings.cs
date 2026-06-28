﻿﻿﻿﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public enum LongitudeDisplayMode
{
    Decimal,
    Dms
}

/// <summary>
/// 插件全局设置
/// </summary>
public class PluginSettings : INotifyPropertyChanged
{
    private bool _isLunarInstalled = false;
    private double _timeOffsetSeconds = 0;
    private double _longitude = 114.2628;
    private string _timeZoneId = "China Standard Time";
    private bool _enableCountdownNotification = true;
    private int _countdownAlertSeconds = 60;
    private LongitudeDisplayMode _longitudeDisplayMode = LongitudeDisplayMode.Decimal;
    private bool _enableEasterEgg = false;
    private bool _disclaimerAccepted = false;
    private bool _easterEggDisclaimerAccepted = false;
    private bool _easterEggInfoAccepted = false;
    private string _ntpServer = "ntp.aliyun.com";
    private int _ntpSyncIntervalMinutes = 5;

    /// <summary>
    /// 是否启用农历功能（始终启用）
    /// </summary>
    public bool EnableLunarCalendar
    {
        get => true;
        set
        {
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

    /// <summary>
    /// 经度显示方式（小数/度分秒）
    /// </summary>
    public LongitudeDisplayMode LongitudeDisplayMode
    {
        get => _longitudeDisplayMode;
        set
        {
            if (_longitudeDisplayMode != value)
            {
                _longitudeDisplayMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用女装彩蛋
    /// </summary>
    public bool EnableEasterEgg
    {
        get => _enableEasterEgg;
        set
        {
            if (_enableEasterEgg != value)
            {
                _enableEasterEgg = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已接受免责声明
    /// </summary>
    public bool DisclaimerAccepted
    {
        get => _disclaimerAccepted;
        set
        {
            if (_disclaimerAccepted != value)
            {
                _disclaimerAccepted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已接受女装彩蛋免责声明
    /// </summary>
    public bool EasterEggDisclaimerAccepted
    {
        get => _easterEggDisclaimerAccepted;
        set
        {
            if (_easterEggDisclaimerAccepted != value)
            {
                _easterEggDisclaimerAccepted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已接受女装彩蛋关闭方式信息
    /// </summary>
    public bool EasterEggInfoAccepted
    {
        get => _easterEggInfoAccepted;
        set
        {
            if (_easterEggInfoAccepted != value)
            {
                _easterEggInfoAccepted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// NTP时间服务器地址
    /// </summary>
    public string NtpServer
    {
        get => _ntpServer;
        set
        {
            if (_ntpServer != value)
            {
                _ntpServer = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// NTP同步周期（分钟）
    /// </summary>
    public int NtpSyncIntervalMinutes
    {
        get => _ntpSyncIntervalMinutes;
        set
        {
            if (_ntpSyncIntervalMinutes != value)
            {
                _ntpSyncIntervalMinutes = value;
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
