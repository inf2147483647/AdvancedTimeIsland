using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public enum TimeBaseType
{
    PluginOffsetServerTime,  // 插件偏移后的服务器时间（默认）
    PluginOffsetSystemTime,  // 插件偏移后的系统时间
    RawServerTime,           // 原始服务器时间
    RawSystemTime            // 原始系统时间
}

public class CountdownSettings : INotifyPropertyChanged
{
    private string _text1 = "距离";
    private string _text2 = "倒计时名称";
    private string _text3 = "还有";
    private string _text4 = string.Empty;
    private string _timeFormat = "%D天%h小时%m分钟%s秒";
    private double _text1FontSize = 14;
    private double _text2FontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private double _text4FontSize = 14;
    private string _text1FontColor = "#FFFFFF";
    private string _text2FontColor = "#FFFFFF";
    private string _text3FontColor = "#FFFFFF";
    private string _timeFontColor = "#FFFFFF";
    private string _text4FontColor = "#FFFFFF";
    private TimeBaseType _timeBaseType = TimeBaseType.PluginOffsetServerTime;
    private List<CountdownItem> _countdownItems = new();
    private long _startTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    private string _notificationMaskText = "倒计时到达";
    private int _notificationMaskDurationSeconds = 3;
    private string _notificationOverlayText = "目标时间已到达！";
    private int _notificationOverlayDurationSeconds = 5;

    public string Text1
    {
        get => _text1;
        set
        {
            if (_text1 != value)
            {
                _text1 = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text2
    {
        get => _text2;
        set
        {
            if (_text2 != value)
            {
                _text2 = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text3
    {
        get => _text3;
        set
        {
            if (_text3 != value)
            {
                _text3 = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text4
    {
        get => _text4;
        set
        {
            if (_text4 != value)
            {
                _text4 = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeFormat
    {
        get => _timeFormat;
        set
        {
            if (_timeFormat != value)
            {
                _timeFormat = value;
                OnPropertyChanged();
            }
        }
    }

    public double Text1FontSize
    {
        get => _text1FontSize;
        set
        {
            if (Math.Abs(_text1FontSize - value) > 0.001)
            {
                _text1FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double Text2FontSize
    {
        get => _text2FontSize;
        set
        {
            if (Math.Abs(_text2FontSize - value) > 0.001)
            {
                _text2FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double Text3FontSize
    {
        get => _text3FontSize;
        set
        {
            if (Math.Abs(_text3FontSize - value) > 0.001)
            {
                _text3FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double TimeFontSize
    {
        get => _timeFontSize;
        set
        {
            if (Math.Abs(_timeFontSize - value) > 0.001)
            {
                _timeFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double Text4FontSize
    {
        get => _text4FontSize;
        set
        {
            if (Math.Abs(_text4FontSize - value) > 0.001)
            {
                _text4FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string Text1FontColor
    {
        get => _text1FontColor;
        set
        {
            if (_text1FontColor != value)
            {
                _text1FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text2FontColor
    {
        get => _text2FontColor;
        set
        {
            if (_text2FontColor != value)
            {
                _text2FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text3FontColor
    {
        get => _text3FontColor;
        set
        {
            if (_text3FontColor != value)
            {
                _text3FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeFontColor
    {
        get => _timeFontColor;
        set
        {
            if (_timeFontColor != value)
            {
                _timeFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text4FontColor
    {
        get => _text4FontColor;
        set
        {
            if (_text4FontColor != value)
            {
                _text4FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public TimeBaseType TimeBaseType
    {
        get => _timeBaseType;
        set
        {
            if (_timeBaseType != value)
            {
                _timeBaseType = value;
                OnPropertyChanged();
            }
        }
    }

    public List<CountdownItem> CountdownItems
    {
        get => _countdownItems;
        set
        {
            if (_countdownItems != value)
            {
                _countdownItems = value;
                OnPropertyChanged();
            }
        }
    }

    public long StartTime
    {
        get => _startTime;
        set
        {
            if (_startTime != value)
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }
    }

    public string NotificationMaskText
    {
        get => _notificationMaskText;
        set
        {
            if (_notificationMaskText != value)
            {
                _notificationMaskText = value;
                OnPropertyChanged();
            }
        }
    }

    public int NotificationMaskDurationSeconds
    {
        get => _notificationMaskDurationSeconds;
        set
        {
            if (_notificationMaskDurationSeconds != value)
            {
                _notificationMaskDurationSeconds = Math.Max(1, Math.Min(60, value));
                OnPropertyChanged();
            }
        }
    }

    public string NotificationOverlayText
    {
        get => _notificationOverlayText;
        set
        {
            if (_notificationOverlayText != value)
            {
                _notificationOverlayText = value;
                OnPropertyChanged();
            }
        }
    }

    public int NotificationOverlayDurationSeconds
    {
        get => _notificationOverlayDurationSeconds;
        set
        {
            if (_notificationOverlayDurationSeconds != value)
            {
                _notificationOverlayDurationSeconds = Math.Max(1, Math.Min(60, value));
                OnPropertyChanged();
            }
        }
    }

    private bool _enableTimeCorrection = true;

    public bool EnableTimeCorrection
    {
        get => _enableTimeCorrection;
        set
        {
            if (_enableTimeCorrection != value)
            {
                _enableTimeCorrection = value;
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

public class ForwardTimerSettings : INotifyPropertyChanged
{
    private string _text1 = string.Empty;
    private string _name = "新正向计时器";
    private string _text3 = "已过";
    private string _text4 = string.Empty;
    private string _timeFormat = "%D天%h小时%m分钟%s秒";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private double _text4FontSize = 14;
    private string _text1FontColor = "#FFFFFF";
    private string _nameFontColor = "#FFFFFF";
    private string _text3FontColor = "#FFFFFF";
    private string _timeFontColor = "#FFFFFF";
    private string _text4FontColor = "#FFFFFF";
    private TimeBaseType _timeBaseType = TimeBaseType.PluginOffsetServerTime;
    private long _startTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

    public string Text1
    {
        get => _text1;
        set
        {
            if (_text1 != value)
            {
                _text1 = value;
                OnPropertyChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text3
    {
        get => _text3;
        set
        {
            if (_text3 != value)
            {
                _text3 = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text4
    {
        get => _text4;
        set
        {
            if (_text4 != value)
            {
                _text4 = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeFormat
    {
        get => _timeFormat;
        set
        {
            if (_timeFormat != value)
            {
                _timeFormat = value;
                OnPropertyChanged();
            }
        }
    }

    public double Text1FontSize
    {
        get => _text1FontSize;
        set
        {
            if (Math.Abs(_text1FontSize - value) > 0.001)
            {
                _text1FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double NameFontSize
    {
        get => _nameFontSize;
        set
        {
            if (Math.Abs(_nameFontSize - value) > 0.001)
            {
                _nameFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double Text3FontSize
    {
        get => _text3FontSize;
        set
        {
            if (Math.Abs(_text3FontSize - value) > 0.001)
            {
                _text3FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double TimeFontSize
    {
        get => _timeFontSize;
        set
        {
            if (Math.Abs(_timeFontSize - value) > 0.001)
            {
                _timeFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double Text4FontSize
    {
        get => _text4FontSize;
        set
        {
            if (Math.Abs(_text4FontSize - value) > 0.001)
            {
                _text4FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string Text1FontColor
    {
        get => _text1FontColor;
        set
        {
            if (_text1FontColor != value)
            {
                _text1FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string NameFontColor
    {
        get => _nameFontColor;
        set
        {
            if (_nameFontColor != value)
            {
                _nameFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text3FontColor
    {
        get => _text3FontColor;
        set
        {
            if (_text3FontColor != value)
            {
                _text3FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeFontColor
    {
        get => _timeFontColor;
        set
        {
            if (_timeFontColor != value)
            {
                _timeFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text4FontColor
    {
        get => _text4FontColor;
        set
        {
            if (_text4FontColor != value)
            {
                _text4FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public TimeBaseType TimeBaseType
    {
        get => _timeBaseType;
        set
        {
            if (_timeBaseType != value)
            {
                _timeBaseType = value;
                OnPropertyChanged();
            }
        }
    }

    public long StartTime
    {
        get => _startTime;
        set
        {
            if (_startTime != value)
            {
                _startTime = value;
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

public class LocalSolarTimeSettings : INotifyPropertyChanged
{
    private double _longitude = 116.4;
    private string _fontColor = "#FFFFFF";
    private double _textFontSize = 14;

    public double Longitude
    {
        get => _longitude;
        set
        {
            if (Math.Abs(_longitude - value) > 0.0001)
            {
                _longitude = Math.Max(-180, Math.Min(180, value));
                OnPropertyChanged();
            }
        }
    }

    public string FontColor
    {
        get => _fontColor;
        set
        {
            if (_fontColor != value)
            {
                _fontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double TextFontSize
    {
        get => _textFontSize;
        set
        {
            if (Math.Abs(_textFontSize - value) > 0.001)
            {
                _textFontSize = Math.Max(6, Math.Min(72, value));
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

public class TimeZoneTimeSettings : INotifyPropertyChanged
{
    private string _timeZoneId = "China Standard Time";
    private string _fontColor = "#FFFFFF";
    private double _textFontSize = 14;

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

    public string FontColor
    {
        get => _fontColor;
        set
        {
            if (_fontColor != value)
            {
                _fontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double TextFontSize
    {
        get => _textFontSize;
        set
        {
            if (Math.Abs(_textFontSize - value) > 0.001)
            {
                _textFontSize = Math.Max(6, Math.Min(72, value));
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

public class AdvancedDateSettings : INotifyPropertyChanged
{
    private bool _showWeekDay = true;
    private string _fontColor = "#FFFFFF";
    private double _dateFontSize = 14;

    public bool ShowWeekDay
    {
        get => _showWeekDay;
        set
        {
            if (_showWeekDay != value)
            {
                _showWeekDay = value;
                OnPropertyChanged();
            }
        }
    }

    public string FontColor
    {
        get => _fontColor;
        set
        {
            if (_fontColor != value)
            {
                _fontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double DateFontSize
    {
        get => _dateFontSize;
        set
        {
            if (Math.Abs(_dateFontSize - value) > 0.001)
            {
                _dateFontSize = Math.Max(6, Math.Min(72, value));
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

public class LunarCountdownSettings : INotifyPropertyChanged
{
    private string _text1 = string.Empty;
    private string _text3 = "还有";
    private string _text4 = string.Empty;
    private string _timeFormat = "%D天%h小时%m分钟%s秒";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private double _text4FontSize = 14;
    private string _text1FontColor = "#FFFFFF";
    private string _nameFontColor = "#FFFFFF";
    private string _text3FontColor = "#FFFFFF";
    private string _timeFontColor = "#FFFFFF";
    private string _text4FontColor = "#FFFFFF";
    private TimeBaseType _timeBaseType = TimeBaseType.PluginOffsetServerTime;
    private List<LunarCountdownItem> _countdownItems = new();

    public string Text1
    {
        get => _text1;
        set
        {
            if (_text1 != value)
            {
                _text1 = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text3
    {
        get => _text3;
        set
        {
            if (_text3 != value)
            {
                _text3 = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text4
    {
        get => _text4;
        set
        {
            if (_text4 != value)
            {
                _text4 = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeFormat
    {
        get => _timeFormat;
        set
        {
            if (_timeFormat != value)
            {
                _timeFormat = value;
                OnPropertyChanged();
            }
        }
    }

    public double Text1FontSize
    {
        get => _text1FontSize;
        set
        {
            if (Math.Abs(_text1FontSize - value) > 0.001)
            {
                _text1FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double NameFontSize
    {
        get => _nameFontSize;
        set
        {
            if (Math.Abs(_nameFontSize - value) > 0.001)
            {
                _nameFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double Text3FontSize
    {
        get => _text3FontSize;
        set
        {
            if (Math.Abs(_text3FontSize - value) > 0.001)
            {
                _text3FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double TimeFontSize
    {
        get => _timeFontSize;
        set
        {
            if (Math.Abs(_timeFontSize - value) > 0.001)
            {
                _timeFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double Text4FontSize
    {
        get => _text4FontSize;
        set
        {
            if (Math.Abs(_text4FontSize - value) > 0.001)
            {
                _text4FontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string Text1FontColor
    {
        get => _text1FontColor;
        set
        {
            if (_text1FontColor != value)
            {
                _text1FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string NameFontColor
    {
        get => _nameFontColor;
        set
        {
            if (_nameFontColor != value)
            {
                _nameFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text3FontColor
    {
        get => _text3FontColor;
        set
        {
            if (_text3FontColor != value)
            {
                _text3FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeFontColor
    {
        get => _timeFontColor;
        set
        {
            if (_timeFontColor != value)
            {
                _timeFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text4FontColor
    {
        get => _text4FontColor;
        set
        {
            if (_text4FontColor != value)
            {
                _text4FontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public TimeBaseType TimeBaseType
    {
        get => _timeBaseType;
        set
        {
            if (_timeBaseType != value)
            {
                _timeBaseType = value;
                OnPropertyChanged();
            }
        }
    }

    public List<LunarCountdownItem> CountdownItems
    {
        get => _countdownItems;
        set
        {
            if (_countdownItems != value)
            {
                _countdownItems = value;
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