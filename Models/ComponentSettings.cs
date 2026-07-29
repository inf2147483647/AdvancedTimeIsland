using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public enum ProgressDisplayMode
{
    None = 0,
    Bar = 1,
    Ring = 2,
    Both = 3
}

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
    private string _timeFormat = "%d天%h小时%m分钟%s秒";
    private double _text1FontSize = 14;
    private double _text2FontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private double _text4FontSize = 14;
    private string _text1FontColor = "";
    private string _text2FontColor = "";
    private string _text3FontColor = "";
    private string _timeFontColor = "";
    private string _text4FontColor = "";
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

    private bool _text1EnableCustomFontSize = false;

    public bool Text1EnableCustomFontSize
    {
        get => _text1EnableCustomFontSize;
        set
        {
            if (_text1EnableCustomFontSize != value)
            {
                _text1EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text1EnableCustomFontColor = false;

    public bool Text1EnableCustomFontColor
    {
        get => _text1EnableCustomFontColor;
        set
        {
            if (_text1EnableCustomFontColor != value)
            {
                _text1EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text2EnableCustomFontSize = false;

    public bool Text2EnableCustomFontSize
    {
        get => _text2EnableCustomFontSize;
        set
        {
            if (_text2EnableCustomFontSize != value)
            {
                _text2EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text2EnableCustomFontColor = false;

    public bool Text2EnableCustomFontColor
    {
        get => _text2EnableCustomFontColor;
        set
        {
            if (_text2EnableCustomFontColor != value)
            {
                _text2EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontSize = false;

    public bool Text3EnableCustomFontSize
    {
        get => _text3EnableCustomFontSize;
        set
        {
            if (_text3EnableCustomFontSize != value)
            {
                _text3EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontColor = false;

    public bool Text3EnableCustomFontColor
    {
        get => _text3EnableCustomFontColor;
        set
        {
            if (_text3EnableCustomFontColor != value)
            {
                _text3EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontSize = false;

    public bool TimeEnableCustomFontSize
    {
        get => _timeEnableCustomFontSize;
        set
        {
            if (_timeEnableCustomFontSize != value)
            {
                _timeEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontColor = false;

    public bool TimeEnableCustomFontColor
    {
        get => _timeEnableCustomFontColor;
        set
        {
            if (_timeEnableCustomFontColor != value)
            {
                _timeEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontSize = false;

    public bool Text4EnableCustomFontSize
    {
        get => _text4EnableCustomFontSize;
        set
        {
            if (_text4EnableCustomFontSize != value)
            {
                _text4EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontColor = false;

        public bool Text4EnableCustomFontColor
        {
            get => _text4EnableCustomFontColor;
            set
            {
                if (_text4EnableCustomFontColor != value)
                {
                    _text4EnableCustomFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text1EnableCustomFontFamily = false;

        public bool Text1EnableCustomFontFamily
        {
            get => _text1EnableCustomFontFamily;
            set
            {
                if (_text1EnableCustomFontFamily != value)
                {
                    _text1EnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text1FontFamily = "";

        public string Text1FontFamily
        {
            get => _text1FontFamily;
            set
            {
                if (_text1FontFamily != value)
                {
                    _text1FontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text2EnableCustomFontFamily = false;

        public bool Text2EnableCustomFontFamily
        {
            get => _text2EnableCustomFontFamily;
            set
            {
                if (_text2EnableCustomFontFamily != value)
                {
                    _text2EnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text2FontFamily = "";

        public string Text2FontFamily
        {
            get => _text2FontFamily;
            set
            {
                if (_text2FontFamily != value)
                {
                    _text2FontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text3EnableCustomFontFamily = false;

        public bool Text3EnableCustomFontFamily
        {
            get => _text3EnableCustomFontFamily;
            set
            {
                if (_text3EnableCustomFontFamily != value)
                {
                    _text3EnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text3FontFamily = "";

        public string Text3FontFamily
        {
            get => _text3FontFamily;
            set
            {
                if (_text3FontFamily != value)
                {
                    _text3FontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _timeEnableCustomFontFamily = false;

        public bool TimeEnableCustomFontFamily
        {
            get => _timeEnableCustomFontFamily;
            set
            {
                if (_timeEnableCustomFontFamily != value)
                {
                    _timeEnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _timeFontFamily = "";

        public string TimeFontFamily
        {
            get => _timeFontFamily;
            set
            {
                if (_timeFontFamily != value)
                {
                    _timeFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text4EnableCustomFontFamily = false;

        public bool Text4EnableCustomFontFamily
        {
            get => _text4EnableCustomFontFamily;
            set
            {
                if (_text4EnableCustomFontFamily != value)
                {
                    _text4EnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text4FontFamily = "";

        public string Text4FontFamily
        {
            get => _text4FontFamily;
            set
            {
                if (_text4FontFamily != value)
                {
                    _text4FontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text1FontWeight = "Normal";

        public string Text1FontWeight
        {
            get => _text1FontWeight;
            set
            {
                if (_text1FontWeight != value)
                {
                    _text1FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text2FontWeight = "Normal";

        public string Text2FontWeight
        {
            get => _text2FontWeight;
            set
            {
                if (_text2FontWeight != value)
                {
                    _text2FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text3FontWeight = "Normal";

        public string Text3FontWeight
        {
            get => _text3FontWeight;
            set
            {
                if (_text3FontWeight != value)
                {
                    _text3FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _timeFontWeight = "Normal";

        public string TimeFontWeight
        {
            get => _timeFontWeight;
            set
            {
                if (_timeFontWeight != value)
                {
                    _timeFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text4FontWeight = "Normal";

        public string Text4FontWeight
        {
            get => _text4FontWeight;
            set
            {
                if (_text4FontWeight != value)
                {
                    _text4FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text1EnableCustomFontWeight = false;

        public bool Text1EnableCustomFontWeight
        {
            get => _text1EnableCustomFontWeight;
            set
            {
                if (_text1EnableCustomFontWeight != value)
                {
                    _text1EnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text2EnableCustomFontWeight = false;

        public bool Text2EnableCustomFontWeight
        {
            get => _text2EnableCustomFontWeight;
            set
            {
                if (_text2EnableCustomFontWeight != value)
                {
                    _text2EnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text3EnableCustomFontWeight = false;

        public bool Text3EnableCustomFontWeight
        {
            get => _text3EnableCustomFontWeight;
            set
            {
                if (_text3EnableCustomFontWeight != value)
                {
                    _text3EnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _timeEnableCustomFontWeight = false;

        public bool TimeEnableCustomFontWeight
        {
            get => _timeEnableCustomFontWeight;
            set
            {
                if (_timeEnableCustomFontWeight != value)
                {
                    _timeEnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text4EnableCustomFontWeight = false;

        public bool Text4EnableCustomFontWeight
        {
            get => _text4EnableCustomFontWeight;
            set
            {
                if (_text4EnableCustomFontWeight != value)
                {
                    _text4EnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private ProgressDisplayMode _progressDisplayMode = ProgressDisplayMode.None;

        public ProgressDisplayMode ProgressDisplayMode
        {
            get => _progressDisplayMode;
            set
            {
                if (_progressDisplayMode != value)
                {
                    _progressDisplayMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableCustomProgressColor = false;

        public bool EnableCustomProgressColor
        {
            get => _enableCustomProgressColor;
            set
            {
                if (_enableCustomProgressColor != value)
                {
                    _enableCustomProgressColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _progressBarColor = "#0078D4";

        public string ProgressBarColor
        {
            get => _progressBarColor;
            set
            {
                if (_progressBarColor != value)
                {
                    _progressBarColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _progressRingColor = "#0078D4";

        public string ProgressRingColor
        {
            get => _progressRingColor;
            set
            {
                if (_progressRingColor != value)
                {
                    _progressRingColor = value;
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
    private string _timeFormat = "%d天%h小时%m分钟%s秒";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private double _text4FontSize = 14;
    private string _text1FontColor = "";
    private string _nameFontColor = "";
    private string _text3FontColor = "";
    private string _timeFontColor = "";
    private string _text4FontColor = "";
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

    private bool _text1EnableCustomFontSize = false;

    public bool Text1EnableCustomFontSize
    {
        get => _text1EnableCustomFontSize;
        set
        {
            if (_text1EnableCustomFontSize != value)
            {
                _text1EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text1EnableCustomFontColor = false;

    public bool Text1EnableCustomFontColor
    {
        get => _text1EnableCustomFontColor;
        set
        {
            if (_text1EnableCustomFontColor != value)
            {
                _text1EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _nameEnableCustomFontSize = false;

    public bool NameEnableCustomFontSize
    {
        get => _nameEnableCustomFontSize;
        set
        {
            if (_nameEnableCustomFontSize != value)
            {
                _nameEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _nameEnableCustomFontColor = false;

    public bool NameEnableCustomFontColor
    {
        get => _nameEnableCustomFontColor;
        set
        {
            if (_nameEnableCustomFontColor != value)
            {
                _nameEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontSize = false;

    public bool Text3EnableCustomFontSize
    {
        get => _text3EnableCustomFontSize;
        set
        {
            if (_text3EnableCustomFontSize != value)
            {
                _text3EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontColor = false;

    public bool Text3EnableCustomFontColor
    {
        get => _text3EnableCustomFontColor;
        set
        {
            if (_text3EnableCustomFontColor != value)
            {
                _text3EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontSize = false;

    public bool TimeEnableCustomFontSize
    {
        get => _timeEnableCustomFontSize;
        set
        {
            if (_timeEnableCustomFontSize != value)
            {
                _timeEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontColor = false;

    public bool TimeEnableCustomFontColor
    {
        get => _timeEnableCustomFontColor;
        set
        {
            if (_timeEnableCustomFontColor != value)
            {
                _timeEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontSize = false;

    public bool Text4EnableCustomFontSize
    {
        get => _text4EnableCustomFontSize;
        set
        {
            if (_text4EnableCustomFontSize != value)
            {
                _text4EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontColor = false;

    public bool Text4EnableCustomFontColor
    {
        get => _text4EnableCustomFontColor;
        set
        {
            if (_text4EnableCustomFontColor != value)
            {
                _text4EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text1EnableCustomFontFamily = false;

    public bool Text1EnableCustomFontFamily
    {
        get => _text1EnableCustomFontFamily;
        set
        {
            if (_text1EnableCustomFontFamily != value)
            {
                _text1EnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _text1FontFamily = "";

    public string Text1FontFamily
    {
        get => _text1FontFamily;
        set
        {
            if (_text1FontFamily != value)
            {
                _text1FontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _nameEnableCustomFontFamily = false;

    public bool NameEnableCustomFontFamily
    {
        get => _nameEnableCustomFontFamily;
        set
        {
            if (_nameEnableCustomFontFamily != value)
            {
                _nameEnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _nameFontFamily = "";

    public string NameFontFamily
    {
        get => _nameFontFamily;
        set
        {
            if (_nameFontFamily != value)
            {
                _nameFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontFamily = false;

    public bool Text3EnableCustomFontFamily
    {
        get => _text3EnableCustomFontFamily;
        set
        {
            if (_text3EnableCustomFontFamily != value)
            {
                _text3EnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _text3FontFamily = "";

    public string Text3FontFamily
    {
        get => _text3FontFamily;
        set
        {
            if (_text3FontFamily != value)
            {
                _text3FontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontFamily = false;

    public bool TimeEnableCustomFontFamily
    {
        get => _timeEnableCustomFontFamily;
        set
        {
            if (_timeEnableCustomFontFamily != value)
            {
                _timeEnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _timeFontFamily = "";

    public string TimeFontFamily
    {
        get => _timeFontFamily;
        set
        {
            if (_timeFontFamily != value)
            {
                _timeFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontFamily = false;

    public bool Text4EnableCustomFontFamily
    {
        get => _text4EnableCustomFontFamily;
        set
        {
            if (_text4EnableCustomFontFamily != value)
            {
                _text4EnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _text4FontFamily = "";

    public string Text4FontFamily
        {
            get => _text4FontFamily;
            set
            {
                if (_text4FontFamily != value)
                {
                    _text4FontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text1FontWeight = "Normal";

        public string Text1FontWeight
        {
            get => _text1FontWeight;
            set
            {
                if (_text1FontWeight != value)
                {
                    _text1FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _nameFontWeight = "Normal";

        public string NameFontWeight
        {
            get => _nameFontWeight;
            set
            {
                if (_nameFontWeight != value)
                {
                    _nameFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text3FontWeight = "Normal";

        public string Text3FontWeight
        {
            get => _text3FontWeight;
            set
            {
                if (_text3FontWeight != value)
                {
                    _text3FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _timeFontWeight = "Normal";

        public string TimeFontWeight
        {
            get => _timeFontWeight;
            set
            {
                if (_timeFontWeight != value)
                {
                    _timeFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _text4FontWeight = "Normal";

        public string Text4FontWeight
        {
            get => _text4FontWeight;
            set
            {
                if (_text4FontWeight != value)
                {
                    _text4FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text1EnableCustomFontWeight = false;

        public bool Text1EnableCustomFontWeight
        {
            get => _text1EnableCustomFontWeight;
            set
            {
                if (_text1EnableCustomFontWeight != value)
                {
                    _text1EnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _nameEnableCustomFontWeight = false;

        public bool NameEnableCustomFontWeight
        {
            get => _nameEnableCustomFontWeight;
            set
            {
                if (_nameEnableCustomFontWeight != value)
                {
                    _nameEnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text3EnableCustomFontWeight = false;

        public bool Text3EnableCustomFontWeight
        {
            get => _text3EnableCustomFontWeight;
            set
            {
                if (_text3EnableCustomFontWeight != value)
                {
                    _text3EnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _timeEnableCustomFontWeight = false;

        public bool TimeEnableCustomFontWeight
        {
            get => _timeEnableCustomFontWeight;
            set
            {
                if (_timeEnableCustomFontWeight != value)
                {
                    _timeEnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _text4EnableCustomFontWeight = false;

        public bool Text4EnableCustomFontWeight
        {
            get => _text4EnableCustomFontWeight;
            set
            {
                if (_text4EnableCustomFontWeight != value)
                {
                    _text4EnableCustomFontWeight = value;
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
    private string _fontColor = "";
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

    private bool _enableCustomFontSize = false;

    public bool EnableCustomFontSize
    {
        get => _enableCustomFontSize;
        set
        {
            if (_enableCustomFontSize != value)
            {
                _enableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomFontColor = false;

    public bool EnableCustomFontColor
    {
        get => _enableCustomFontColor;
        set
        {
            if (_enableCustomFontColor != value)
            {
                _enableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomFontFamily = false;

    public bool EnableCustomFontFamily
    {
        get => _enableCustomFontFamily;
        set
        {
            if (_enableCustomFontFamily != value)
            {
                _enableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _fontFamily = "";

    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (_fontFamily != value)
            {
                _fontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _fontWeight = "Normal";

    public string FontWeight
    {
        get => _fontWeight;
        set
        {
            if (_fontWeight != value)
            {
                _fontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomFontWeight = false;

    public bool EnableCustomFontWeight
    {
        get => _enableCustomFontWeight;
        set
        {
            if (_enableCustomFontWeight != value)
            {
                _enableCustomFontWeight = value;
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
    private string _fontColor = "";
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

    private bool _enableCustomFontSize = false;

    public bool EnableCustomFontSize
    {
        get => _enableCustomFontSize;
        set
        {
            if (_enableCustomFontSize != value)
            {
                _enableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomFontColor = false;

        public bool EnableCustomFontColor
        {
            get => _enableCustomFontColor;
            set
            {
                if (_enableCustomFontColor != value)
                {
                    _enableCustomFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableCustomFontFamily = false;

        public bool EnableCustomFontFamily
        {
            get => _enableCustomFontFamily;
            set
            {
                if (_enableCustomFontFamily != value)
                {
                    _enableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _fontFamily = "";

        public string FontFamily
        {
            get => _fontFamily;
            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _fontWeight = "Normal";

        public string FontWeight
        {
            get => _fontWeight;
            set
            {
                if (_fontWeight != value)
                {
                    _fontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableCustomFontWeight = false;

        public bool EnableCustomFontWeight
        {
            get => _enableCustomFontWeight;
            set
            {
                if (_enableCustomFontWeight != value)
                {
                    _enableCustomFontWeight = value;
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
    private string _fontColor = "";
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

    private bool _enableCustomFontSize = false;

    public bool EnableCustomFontSize
    {
        get => _enableCustomFontSize;
        set
        {
            if (_enableCustomFontSize != value)
            {
                _enableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomFontColor = false;

    public bool EnableCustomFontColor
    {
        get => _enableCustomFontColor;
        set
        {
            if (_enableCustomFontColor != value)
            {
                _enableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomFontFamily = false;

    public bool EnableCustomFontFamily
    {
        get => _enableCustomFontFamily;
        set
        {
            if (_enableCustomFontFamily != value)
            {
                _enableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _fontFamily = "";

    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (_fontFamily != value)
            {
                _fontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _fontWeight = "Normal";

    public string FontWeight
    {
        get => _fontWeight;
        set
        {
            if (_fontWeight != value)
            {
                _fontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomFontWeight = false;

    public bool EnableCustomFontWeight
    {
        get => _enableCustomFontWeight;
        set
        {
            if (_enableCustomFontWeight != value)
            {
                _enableCustomFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private double _weekDayFontSize = 14;

    public double WeekDayFontSize
    {
        get => _weekDayFontSize;
        set
        {
            if (Math.Abs(_weekDayFontSize - value) > 0.001)
            {
                _weekDayFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    private string _weekDayFontColor = "";

    public string WeekDayFontColor
    {
        get => _weekDayFontColor;
        set
        {
            if (_weekDayFontColor != value)
            {
                _weekDayFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private string _weekDayFontFamily = "";

    public string WeekDayFontFamily
    {
        get => _weekDayFontFamily;
        set
        {
            if (_weekDayFontFamily != value)
            {
                _weekDayFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _weekDayFontWeight = "Normal";

    public string WeekDayFontWeight
    {
        get => _weekDayFontWeight;
        set
        {
            if (_weekDayFontWeight != value)
            {
                _weekDayFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _weekDayEnableCustomFontSize = false;

    public bool WeekDayEnableCustomFontSize
    {
        get => _weekDayEnableCustomFontSize;
        set
        {
            if (_weekDayEnableCustomFontSize != value)
            {
                _weekDayEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _weekDayEnableCustomFontColor = false;

    public bool WeekDayEnableCustomFontColor
    {
        get => _weekDayEnableCustomFontColor;
        set
        {
            if (_weekDayEnableCustomFontColor != value)
            {
                _weekDayEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _weekDayEnableCustomFontFamily = false;

    public bool WeekDayEnableCustomFontFamily
    {
        get => _weekDayEnableCustomFontFamily;
        set
        {
            if (_weekDayEnableCustomFontFamily != value)
            {
                _weekDayEnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _weekDayEnableCustomFontWeight = false;

    public bool WeekDayEnableCustomFontWeight
    {
        get => _weekDayEnableCustomFontWeight;
        set
        {
            if (_weekDayEnableCustomFontWeight != value)
            {
                _weekDayEnableCustomFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private int _dateContentOrder = 0;

    public int DateContentOrder
    {
        get => _dateContentOrder;
        set
        {
            if (_dateContentOrder != value)
            {
                _dateContentOrder = value;
                OnPropertyChanged();
            }
        }
    }

    private int _dateSeparator = 0;

    public int DateSeparator
    {
        get => _dateSeparator;
        set
        {
            if (_dateSeparator != value)
            {
                _dateSeparator = value;
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

public class XingZuoSettings : INotifyPropertyChanged
{
    private string _labelFontColor = "";
    private double _labelFontSize = 14;
    private string _valueFontColor = "";
    private double _valueFontSize = 14;

    public string LabelFontColor
    {
        get => _labelFontColor;
        set
        {
            if (_labelFontColor != value)
            {
                _labelFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double LabelFontSize
    {
        get => _labelFontSize;
        set
        {
            if (Math.Abs(_labelFontSize - value) > 0.001)
            {
                _labelFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string ValueFontColor
    {
        get => _valueFontColor;
        set
        {
            if (_valueFontColor != value)
            {
                _valueFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double ValueFontSize
    {
        get => _valueFontSize;
        set
        {
            if (Math.Abs(_valueFontSize - value) > 0.001)
            {
                _valueFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontSize = false;

    public bool LabelEnableCustomFontSize
    {
        get => _labelEnableCustomFontSize;
        set
        {
            if (_labelEnableCustomFontSize != value)
            {
                _labelEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontColor = false;

    public bool LabelEnableCustomFontColor
    {
        get => _labelEnableCustomFontColor;
        set
        {
            if (_labelEnableCustomFontColor != value)
            {
                _labelEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontSize = false;

    public bool ValueEnableCustomFontSize
    {
        get => _valueEnableCustomFontSize;
        set
        {
            if (_valueEnableCustomFontSize != value)
            {
                _valueEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontColor = false;

    public bool ValueEnableCustomFontColor
    {
        get => _valueEnableCustomFontColor;
        set
        {
            if (_valueEnableCustomFontColor != value)
            {
                _valueEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontFamily = false;

    public bool LabelEnableCustomFontFamily
    {
        get => _labelEnableCustomFontFamily;
        set
        {
            if (_labelEnableCustomFontFamily != value)
            {
                _labelEnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _labelFontFamily = "";

    public string LabelFontFamily
    {
        get => _labelFontFamily;
        set
        {
            if (_labelFontFamily != value)
            {
                _labelFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontFamily = false;

    public bool ValueEnableCustomFontFamily
    {
        get => _valueEnableCustomFontFamily;
        set
        {
            if (_valueEnableCustomFontFamily != value)
            {
                _valueEnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _valueFontFamily = "";

    public string ValueFontFamily
    {
        get => _valueFontFamily;
        set
        {
            if (_valueFontFamily != value)
            {
                _valueFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _labelFontWeight = "Normal";

    public string LabelFontWeight
    {
        get => _labelFontWeight;
        set
        {
            if (_labelFontWeight != value)
            {
                _labelFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private string _valueFontWeight = "Normal";

    public string ValueFontWeight
    {
        get => _valueFontWeight;
        set
        {
            if (_valueFontWeight != value)
            {
                _valueFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontWeight = false;

    public bool LabelEnableCustomFontWeight
    {
        get => _labelEnableCustomFontWeight;
        set
        {
            if (_labelEnableCustomFontWeight != value)
            {
                _labelEnableCustomFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontWeight = false;

    public bool ValueEnableCustomFontWeight
    {
        get => _valueEnableCustomFontWeight;
        set
        {
            if (_valueEnableCustomFontWeight != value)
            {
                _valueEnableCustomFontWeight = value;
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

public class JieQiSettings : INotifyPropertyChanged
{
    private string _labelFontColor = "";
    private double _labelFontSize = 14;
    private string _valueFontColor = "";
    private double _valueFontSize = 14;

    public string LabelFontColor
    {
        get => _labelFontColor;
        set
        {
            if (_labelFontColor != value)
            {
                _labelFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double LabelFontSize
    {
        get => _labelFontSize;
        set
        {
            if (Math.Abs(_labelFontSize - value) > 0.001)
            {
                _labelFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string ValueFontColor
    {
        get => _valueFontColor;
        set
        {
            if (_valueFontColor != value)
            {
                _valueFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double ValueFontSize
    {
        get => _valueFontSize;
        set
        {
            if (Math.Abs(_valueFontSize - value) > 0.001)
            {
                _valueFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontSize = false;

    public bool LabelEnableCustomFontSize
    {
        get => _labelEnableCustomFontSize;
        set
        {
            if (_labelEnableCustomFontSize != value)
            {
                _labelEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontColor = false;

    public bool LabelEnableCustomFontColor
    {
        get => _labelEnableCustomFontColor;
        set
        {
            if (_labelEnableCustomFontColor != value)
            {
                _labelEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontSize = false;

    public bool ValueEnableCustomFontSize
    {
        get => _valueEnableCustomFontSize;
        set
        {
            if (_valueEnableCustomFontSize != value)
            {
                _valueEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontColor = false;

        public bool ValueEnableCustomFontColor
        {
            get => _valueEnableCustomFontColor;
            set
            {
                if (_valueEnableCustomFontColor != value)
                {
                    _valueEnableCustomFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _labelEnableCustomFontFamily = false;

        public bool LabelEnableCustomFontFamily
        {
            get => _labelEnableCustomFontFamily;
            set
            {
                if (_labelEnableCustomFontFamily != value)
                {
                    _labelEnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _labelFontFamily = "";

        public string LabelFontFamily
        {
            get => _labelFontFamily;
            set
            {
                if (_labelFontFamily != value)
                {
                    _labelFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _valueEnableCustomFontFamily = false;

        public bool ValueEnableCustomFontFamily
        {
            get => _valueEnableCustomFontFamily;
            set
            {
                if (_valueEnableCustomFontFamily != value)
                {
                    _valueEnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _valueFontFamily = "";

        public string ValueFontFamily
        {
            get => _valueFontFamily;
            set
            {
                if (_valueFontFamily != value)
                {
                    _valueFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _labelFontWeight = "Normal";

        public string LabelFontWeight
        {
            get => _labelFontWeight;
            set
            {
                if (_labelFontWeight != value)
                {
                    _labelFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _valueFontWeight = "Normal";

        public string ValueFontWeight
        {
            get => _valueFontWeight;
            set
            {
                if (_valueFontWeight != value)
                {
                    _valueFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _labelEnableCustomFontWeight = false;

        public bool LabelEnableCustomFontWeight
        {
            get => _labelEnableCustomFontWeight;
            set
            {
                if (_labelEnableCustomFontWeight != value)
                {
                    _labelEnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _valueEnableCustomFontWeight = false;

        public bool ValueEnableCustomFontWeight
        {
            get => _valueEnableCustomFontWeight;
            set
            {
                if (_valueEnableCustomFontWeight != value)
                {
                    _valueEnableCustomFontWeight = value;
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

    public class ShengXiaoSettings : INotifyPropertyChanged
{
    private string _labelFontColor = "";
    private double _labelFontSize = 14;
    private string _valueFontColor = "";
    private double _valueFontSize = 14;

    public string LabelFontColor
    {
        get => _labelFontColor;
        set
        {
            if (_labelFontColor != value)
            {
                _labelFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double LabelFontSize
    {
        get => _labelFontSize;
        set
        {
            if (Math.Abs(_labelFontSize - value) > 0.001)
            {
                _labelFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string ValueFontColor
    {
        get => _valueFontColor;
        set
        {
            if (_valueFontColor != value)
            {
                _valueFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double ValueFontSize
    {
        get => _valueFontSize;
        set
        {
            if (Math.Abs(_valueFontSize - value) > 0.001)
            {
                _valueFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontSize = false;

    public bool LabelEnableCustomFontSize
    {
        get => _labelEnableCustomFontSize;
        set
        {
            if (_labelEnableCustomFontSize != value)
            {
                _labelEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontColor = false;

    public bool LabelEnableCustomFontColor
    {
        get => _labelEnableCustomFontColor;
        set
        {
            if (_labelEnableCustomFontColor != value)
            {
                _labelEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontSize = false;

    public bool ValueEnableCustomFontSize
    {
        get => _valueEnableCustomFontSize;
        set
        {
            if (_valueEnableCustomFontSize != value)
            {
                _valueEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontColor = false;

    public bool ValueEnableCustomFontColor
    {
        get => _valueEnableCustomFontColor;
        set
        {
            if (_valueEnableCustomFontColor != value)
            {
                _valueEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontFamily = false;

    public bool LabelEnableCustomFontFamily
    {
        get => _labelEnableCustomFontFamily;
        set
        {
            if (_labelEnableCustomFontFamily != value)
            {
                _labelEnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _labelFontFamily = "";

    public string LabelFontFamily
    {
        get => _labelFontFamily;
        set
        {
            if (_labelFontFamily != value)
            {
                _labelFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontFamily = false;

    public bool ValueEnableCustomFontFamily
    {
        get => _valueEnableCustomFontFamily;
        set
        {
            if (_valueEnableCustomFontFamily != value)
            {
                _valueEnableCustomFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _valueFontFamily = "";

    public string ValueFontFamily
    {
        get => _valueFontFamily;
        set
        {
            if (_valueFontFamily != value)
            {
                _valueFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private string _labelFontWeight = "Normal";

    public string LabelFontWeight
    {
        get => _labelFontWeight;
        set
        {
            if (_labelFontWeight != value)
            {
                _labelFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private string _valueFontWeight = "Normal";

    public string ValueFontWeight
    {
        get => _valueFontWeight;
        set
        {
            if (_valueFontWeight != value)
            {
                _valueFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontWeight = false;

    public bool LabelEnableCustomFontWeight
    {
        get => _labelEnableCustomFontWeight;
        set
        {
            if (_labelEnableCustomFontWeight != value)
            {
                _labelEnableCustomFontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontWeight = false;

    public bool ValueEnableCustomFontWeight
    {
        get => _valueEnableCustomFontWeight;
        set
        {
            if (_valueEnableCustomFontWeight != value)
            {
                _valueEnableCustomFontWeight = value;
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

public class FestivalSettings : INotifyPropertyChanged
{
    private string _labelFontColor = "";
    private double _labelFontSize = 14;
    private string _valueFontColor = "";
    private double _valueFontSize = 14;

    public string LabelFontColor
    {
        get => _labelFontColor;
        set
        {
            if (_labelFontColor != value)
            {
                _labelFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double LabelFontSize
    {
        get => _labelFontSize;
        set
        {
            if (Math.Abs(_labelFontSize - value) > 0.001)
            {
                _labelFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string ValueFontColor
    {
        get => _valueFontColor;
        set
        {
            if (_valueFontColor != value)
            {
                _valueFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public double ValueFontSize
    {
        get => _valueFontSize;
        set
        {
            if (Math.Abs(_valueFontSize - value) > 0.001)
            {
                _valueFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontSize = false;

    public bool LabelEnableCustomFontSize
    {
        get => _labelEnableCustomFontSize;
        set
        {
            if (_labelEnableCustomFontSize != value)
            {
                _labelEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _labelEnableCustomFontColor = false;

    public bool LabelEnableCustomFontColor
    {
        get => _labelEnableCustomFontColor;
        set
        {
            if (_labelEnableCustomFontColor != value)
            {
                _labelEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontSize = false;

    public bool ValueEnableCustomFontSize
    {
        get => _valueEnableCustomFontSize;
        set
        {
            if (_valueEnableCustomFontSize != value)
            {
                _valueEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _valueEnableCustomFontColor = false;

        public bool ValueEnableCustomFontColor
        {
            get => _valueEnableCustomFontColor;
            set
            {
                if (_valueEnableCustomFontColor != value)
                {
                    _valueEnableCustomFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _labelEnableCustomFontFamily = false;

        public bool LabelEnableCustomFontFamily
        {
            get => _labelEnableCustomFontFamily;
            set
            {
                if (_labelEnableCustomFontFamily != value)
                {
                    _labelEnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _labelFontFamily = "";

        public string LabelFontFamily
        {
            get => _labelFontFamily;
            set
            {
                if (_labelFontFamily != value)
                {
                    _labelFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _valueEnableCustomFontFamily = false;

        public bool ValueEnableCustomFontFamily
        {
            get => _valueEnableCustomFontFamily;
            set
            {
                if (_valueEnableCustomFontFamily != value)
                {
                    _valueEnableCustomFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _valueFontFamily = "";

        public string ValueFontFamily
        {
            get => _valueFontFamily;
            set
            {
                if (_valueFontFamily != value)
                {
                    _valueFontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _labelFontWeight = "Normal";

        public string LabelFontWeight
        {
            get => _labelFontWeight;
            set
            {
                if (_labelFontWeight != value)
                {
                    _labelFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _valueFontWeight = "Normal";

        public string ValueFontWeight
        {
            get => _valueFontWeight;
            set
            {
                if (_valueFontWeight != value)
                {
                    _valueFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _labelEnableCustomFontWeight = false;

        public bool LabelEnableCustomFontWeight
        {
            get => _labelEnableCustomFontWeight;
            set
            {
                if (_labelEnableCustomFontWeight != value)
                {
                    _labelEnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _valueEnableCustomFontWeight = false;

        public bool ValueEnableCustomFontWeight
        {
            get => _valueEnableCustomFontWeight;
            set
            {
                if (_valueEnableCustomFontWeight != value)
                {
                    _valueEnableCustomFontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableInternationalFestivals = true;
        private bool _enableChineseTraditionalFestivals = true;
        private bool _enableRedFestivals = true;

        public bool EnableInternationalFestivals
        {
            get => _enableInternationalFestivals;
            set
            {
                if (_enableInternationalFestivals != value)
                {
                    _enableInternationalFestivals = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableChineseTraditionalFestivals
        {
            get => _enableChineseTraditionalFestivals;
            set
            {
                if (_enableChineseTraditionalFestivals != value)
                {
                    _enableChineseTraditionalFestivals = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableRedFestivals
        {
            get => _enableRedFestivals;
            set
            {
                if (_enableRedFestivals != value)
                {
                    _enableRedFestivals = value;
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

    public class DayYiJiSettings : INotifyPropertyChanged
{
    private string _yiLabelFontColor = "";
    private double _yiLabelFontSize = 14;
    private double _yiValueFontSize = 14;
    private string _jiLabelFontColor = "";
    private double _jiLabelFontSize = 14;
    private double _jiValueFontSize = 14;
    private int _displayMode = 1;

    private bool _yiLabelEnableCustomFontSize = false;
    private bool _yiLabelEnableCustomFontColor = false;
    private bool _yiLabelEnableCustomFontFamily = false;
    private bool _yiLabelEnableCustomFontWeight = false;
    private bool _yiValueEnableCustomFontSize = false;
    private bool _yiValueEnableCustomFontFamily = false;
    private bool _yiValueEnableCustomFontWeight = false;
    private bool _jiLabelEnableCustomFontSize = false;
    private bool _jiLabelEnableCustomFontColor = false;
    private bool _jiLabelEnableCustomFontFamily = false;
    private bool _jiLabelEnableCustomFontWeight = false;
    private bool _jiValueEnableCustomFontSize = false;
    private bool _jiValueEnableCustomFontFamily = false;
    private bool _jiValueEnableCustomFontWeight = false;

    private string _yiLabelFontFamily = "";
    private string _yiLabelFontWeight = "Normal";
    private string _yiValueFontFamily = "";
    private string _yiValueFontWeight = "Normal";
    private string _jiLabelFontFamily = "";
    private string _jiLabelFontWeight = "Normal";
    private string _jiValueFontFamily = "";
    private string _jiValueFontWeight = "Normal";

    public string YiLabelFontColor
    {
        get => _yiLabelFontColor;
        set { if (_yiLabelFontColor != value) { _yiLabelFontColor = value; OnPropertyChanged(); } }
    }

    public double YiLabelFontSize
    {
        get => _yiLabelFontSize;
        set { if (Math.Abs(_yiLabelFontSize - value) > 0.001) { _yiLabelFontSize = Math.Max(6, Math.Min(72, value)); OnPropertyChanged(); } }
    }

    public double YiValueFontSize
    {
        get => _yiValueFontSize;
        set { if (Math.Abs(_yiValueFontSize - value) > 0.001) { _yiValueFontSize = Math.Max(6, Math.Min(72, value)); OnPropertyChanged(); } }
    }

    public string JiLabelFontColor
    {
        get => _jiLabelFontColor;
        set { if (_jiLabelFontColor != value) { _jiLabelFontColor = value; OnPropertyChanged(); } }
    }

    public double JiLabelFontSize
    {
        get => _jiLabelFontSize;
        set { if (Math.Abs(_jiLabelFontSize - value) > 0.001) { _jiLabelFontSize = Math.Max(6, Math.Min(72, value)); OnPropertyChanged(); } }
    }

    public double JiValueFontSize
    {
        get => _jiValueFontSize;
        set { if (Math.Abs(_jiValueFontSize - value) > 0.001) { _jiValueFontSize = Math.Max(6, Math.Min(72, value)); OnPropertyChanged(); } }
    }

    public int DisplayMode
    {
        get => _displayMode;
        set { if (_displayMode != value) { _displayMode = value; OnPropertyChanged(); } }
    }

    public bool YiLabelEnableCustomFontSize
    {
        get => _yiLabelEnableCustomFontSize;
        set { if (_yiLabelEnableCustomFontSize != value) { _yiLabelEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    public bool YiLabelEnableCustomFontColor
    {
        get => _yiLabelEnableCustomFontColor;
        set { if (_yiLabelEnableCustomFontColor != value) { _yiLabelEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    public bool YiLabelEnableCustomFontFamily
    {
        get => _yiLabelEnableCustomFontFamily;
        set { if (_yiLabelEnableCustomFontFamily != value) { _yiLabelEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    public bool YiLabelEnableCustomFontWeight
    {
        get => _yiLabelEnableCustomFontWeight;
        set { if (_yiLabelEnableCustomFontWeight != value) { _yiLabelEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    public bool YiValueEnableCustomFontSize
    {
        get => _yiValueEnableCustomFontSize;
        set { if (_yiValueEnableCustomFontSize != value) { _yiValueEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    public bool YiValueEnableCustomFontFamily
    {
        get => _yiValueEnableCustomFontFamily;
        set { if (_yiValueEnableCustomFontFamily != value) { _yiValueEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    public bool YiValueEnableCustomFontWeight
    {
        get => _yiValueEnableCustomFontWeight;
        set { if (_yiValueEnableCustomFontWeight != value) { _yiValueEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    public bool JiLabelEnableCustomFontSize
    {
        get => _jiLabelEnableCustomFontSize;
        set { if (_jiLabelEnableCustomFontSize != value) { _jiLabelEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    public bool JiLabelEnableCustomFontColor
    {
        get => _jiLabelEnableCustomFontColor;
        set { if (_jiLabelEnableCustomFontColor != value) { _jiLabelEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    public bool JiLabelEnableCustomFontFamily
    {
        get => _jiLabelEnableCustomFontFamily;
        set { if (_jiLabelEnableCustomFontFamily != value) { _jiLabelEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    public bool JiLabelEnableCustomFontWeight
    {
        get => _jiLabelEnableCustomFontWeight;
        set { if (_jiLabelEnableCustomFontWeight != value) { _jiLabelEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    public bool JiValueEnableCustomFontSize
    {
        get => _jiValueEnableCustomFontSize;
        set { if (_jiValueEnableCustomFontSize != value) { _jiValueEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    public bool JiValueEnableCustomFontFamily
    {
        get => _jiValueEnableCustomFontFamily;
        set { if (_jiValueEnableCustomFontFamily != value) { _jiValueEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    public bool JiValueEnableCustomFontWeight
    {
        get => _jiValueEnableCustomFontWeight;
        set { if (_jiValueEnableCustomFontWeight != value) { _jiValueEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    public string YiLabelFontFamily
    {
        get => _yiLabelFontFamily;
        set { if (_yiLabelFontFamily != value) { _yiLabelFontFamily = value; OnPropertyChanged(); } }
    }

    public string YiLabelFontWeight
    {
        get => _yiLabelFontWeight;
        set { if (_yiLabelFontWeight != value) { _yiLabelFontWeight = value; OnPropertyChanged(); } }
    }

    public string YiValueFontFamily
    {
        get => _yiValueFontFamily;
        set { if (_yiValueFontFamily != value) { _yiValueFontFamily = value; OnPropertyChanged(); } }
    }

    public string YiValueFontWeight
    {
        get => _yiValueFontWeight;
        set { if (_yiValueFontWeight != value) { _yiValueFontWeight = value; OnPropertyChanged(); } }
    }

    public string JiLabelFontFamily
    {
        get => _jiLabelFontFamily;
        set { if (_jiLabelFontFamily != value) { _jiLabelFontFamily = value; OnPropertyChanged(); } }
    }

    public string JiLabelFontWeight
    {
        get => _jiLabelFontWeight;
        set { if (_jiLabelFontWeight != value) { _jiLabelFontWeight = value; OnPropertyChanged(); } }
    }

    public string JiValueFontFamily
    {
        get => _jiValueFontFamily;
        set { if (_jiValueFontFamily != value) { _jiValueFontFamily = value; OnPropertyChanged(); } }
    }

    public string JiValueFontWeight
    {
        get => _jiValueFontWeight;
        set { if (_jiValueFontWeight != value) { _jiValueFontWeight = value; OnPropertyChanged(); } }
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
    private string _timeFormat = "%d天%h小时%m分钟%s秒";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private double _text4FontSize = 14;
    private string _text1FontColor = "";
    private string _nameFontColor = "";
    private string _text3FontColor = "";
    private string _timeFontColor = "";
    private string _text4FontColor = "";
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

    private bool _text1EnableCustomFontSize = false;

    public bool Text1EnableCustomFontSize
    {
        get => _text1EnableCustomFontSize;
        set
        {
            if (_text1EnableCustomFontSize != value)
            {
                _text1EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text1EnableCustomFontColor = false;

    public bool Text1EnableCustomFontColor
    {
        get => _text1EnableCustomFontColor;
        set
        {
            if (_text1EnableCustomFontColor != value)
            {
                _text1EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _nameEnableCustomFontSize = false;

    public bool NameEnableCustomFontSize
    {
        get => _nameEnableCustomFontSize;
        set
        {
            if (_nameEnableCustomFontSize != value)
            {
                _nameEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _nameEnableCustomFontColor = false;

    public bool NameEnableCustomFontColor
    {
        get => _nameEnableCustomFontColor;
        set
        {
            if (_nameEnableCustomFontColor != value)
            {
                _nameEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontSize = false;

    public bool Text3EnableCustomFontSize
    {
        get => _text3EnableCustomFontSize;
        set
        {
            if (_text3EnableCustomFontSize != value)
            {
                _text3EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontColor = false;

    public bool Text3EnableCustomFontColor
    {
        get => _text3EnableCustomFontColor;
        set
        {
            if (_text3EnableCustomFontColor != value)
            {
                _text3EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontSize = false;

    public bool TimeEnableCustomFontSize
    {
        get => _timeEnableCustomFontSize;
        set
        {
            if (_timeEnableCustomFontSize != value)
            {
                _timeEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontColor = false;

    public bool TimeEnableCustomFontColor
    {
        get => _timeEnableCustomFontColor;
        set
        {
            if (_timeEnableCustomFontColor != value)
            {
                _timeEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontSize = false;

    public bool Text4EnableCustomFontSize
    {
        get => _text4EnableCustomFontSize;
        set
        {
            if (_text4EnableCustomFontSize != value)
            {
                _text4EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontColor = false;

    public bool Text4EnableCustomFontColor
    {
        get => _text4EnableCustomFontColor;
        set
        {
            if (_text4EnableCustomFontColor != value)
            {
                _text4EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private ProgressDisplayMode _progressDisplayMode = ProgressDisplayMode.None;

    public ProgressDisplayMode ProgressDisplayMode
    {
        get => _progressDisplayMode;
        set
        {
            if (_progressDisplayMode != value)
            {
                _progressDisplayMode = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableCustomProgressColor = false;

    public bool EnableCustomProgressColor
    {
        get => _enableCustomProgressColor;
        set
        {
            if (_enableCustomProgressColor != value)
            {
                _enableCustomProgressColor = value;
                OnPropertyChanged();
            }
        }
    }

    private string _progressBarColor = "#0078D4";

    public string ProgressBarColor
    {
        get => _progressBarColor;
        set
        {
            if (_progressBarColor != value)
            {
                _progressBarColor = value;
                OnPropertyChanged();
            }
        }
    }

    private string _progressRingColor = "#0078D4";

    public string ProgressRingColor
    {
        get => _progressRingColor;
        set
        {
            if (_progressRingColor != value)
            {
                _progressRingColor = value;
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

public class PeriodicCountdownSettings : INotifyPropertyChanged
{
    private string _text1 = "距离";
    private string _text2 = "倒计时名称";
    private string _text3 = "还有";
    private string _text4 = string.Empty;
    private string _timeFormat = "%d天%h小时%m分钟%s秒";
    private double _text1FontSize = 14;
    private double _text2FontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private double _text4FontSize = 14;
    private string _text1FontColor = "";
    private string _text2FontColor = "";
    private string _text3FontColor = "";
    private string _timeFontColor = "";
    private string _text4FontColor = "";
    private TimeBaseType _timeBaseType = TimeBaseType.PluginOffsetServerTime;
    private List<PeriodicCountdownItem> _countdownItems = new();
    private bool _enableTimeCorrection = true;

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

    public List<PeriodicCountdownItem> CountdownItems
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

    private bool _text1EnableCustomFontSize = false;

    public bool Text1EnableCustomFontSize
    {
        get => _text1EnableCustomFontSize;
        set
        {
            if (_text1EnableCustomFontSize != value)
            {
                _text1EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text1EnableCustomFontColor = false;

    public bool Text1EnableCustomFontColor
    {
        get => _text1EnableCustomFontColor;
        set
        {
            if (_text1EnableCustomFontColor != value)
            {
                _text1EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text2EnableCustomFontSize = false;

    public bool Text2EnableCustomFontSize
    {
        get => _text2EnableCustomFontSize;
        set
        {
            if (_text2EnableCustomFontSize != value)
            {
                _text2EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text2EnableCustomFontColor = false;

    public bool Text2EnableCustomFontColor
    {
        get => _text2EnableCustomFontColor;
        set
        {
            if (_text2EnableCustomFontColor != value)
            {
                _text2EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontSize = false;

    public bool Text3EnableCustomFontSize
    {
        get => _text3EnableCustomFontSize;
        set
        {
            if (_text3EnableCustomFontSize != value)
            {
                _text3EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text3EnableCustomFontColor = false;

    public bool Text3EnableCustomFontColor
    {
        get => _text3EnableCustomFontColor;
        set
        {
            if (_text3EnableCustomFontColor != value)
            {
                _text3EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontSize = false;

    public bool TimeEnableCustomFontSize
    {
        get => _timeEnableCustomFontSize;
        set
        {
            if (_timeEnableCustomFontSize != value)
            {
                _timeEnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _timeEnableCustomFontColor = false;

    public bool TimeEnableCustomFontColor
    {
        get => _timeEnableCustomFontColor;
        set
        {
            if (_timeEnableCustomFontColor != value)
            {
                _timeEnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontSize = false;

    public bool Text4EnableCustomFontSize
    {
        get => _text4EnableCustomFontSize;
        set
        {
            if (_text4EnableCustomFontSize != value)
            {
                _text4EnableCustomFontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text4EnableCustomFontColor = false;

    public bool Text4EnableCustomFontColor
    {
        get => _text4EnableCustomFontColor;
        set
        {
            if (_text4EnableCustomFontColor != value)
            {
                _text4EnableCustomFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _warningAccepted = false;

        public bool WarningAccepted
        {
            get => _warningAccepted;
            set
            {
                if (_warningAccepted != value)
                {
                    _warningAccepted = value;
                    OnPropertyChanged();
                }
            }
        }

        private ProgressDisplayMode _progressDisplayMode = ProgressDisplayMode.None;

        public ProgressDisplayMode ProgressDisplayMode
        {
            get => _progressDisplayMode;
            set
            {
                if (_progressDisplayMode != value)
                {
                    _progressDisplayMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableCustomProgressColor = false;

        public bool EnableCustomProgressColor
        {
            get => _enableCustomProgressColor;
            set
            {
                if (_enableCustomProgressColor != value)
                {
                    _enableCustomProgressColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _progressBarColor = "#0078D4";

        public string ProgressBarColor
        {
            get => _progressBarColor;
            set
            {
                if (_progressBarColor != value)
                {
                    _progressBarColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _progressRingColor = "#0078D4";

        public string ProgressRingColor
        {
            get => _progressRingColor;
            set
            {
                if (_progressRingColor != value)
                {
                    _progressRingColor = value;
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

    public class SunriseSunsetSettings : INotifyPropertyChanged
    {
        private double _longitude = 114.207;
        private double _latitude = 34.8138;
        private string _timeZoneId = "China Standard Time";
        private string _sunriseLabelFontColor = "";
        private double _sunriseLabelFontSize = 14;
        private string _sunriseTimeFontColor = "";
        private double _sunriseTimeFontSize = 14;
        private string _sunsetLabelFontColor = "";
        private double _sunsetLabelFontSize = 14;
        private string _sunsetTimeFontColor = "";
        private double _sunsetTimeFontSize = 14;

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

        public double Latitude
        {
            get => _latitude;
            set
            {
                if (Math.Abs(_latitude - value) > 0.0001)
                {
                    _latitude = Math.Max(-90, Math.Min(90, value));
                    OnPropertyChanged();
                }
            }
        }

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

        public string SunriseLabelFontColor
        {
            get => _sunriseLabelFontColor;
            set
            {
                if (_sunriseLabelFontColor != value)
                {
                    _sunriseLabelFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public double SunriseLabelFontSize
        {
            get => _sunriseLabelFontSize;
            set
            {
                if (Math.Abs(_sunriseLabelFontSize - value) > 0.001)
                {
                    _sunriseLabelFontSize = Math.Max(6, Math.Min(72, value));
                    OnPropertyChanged();
                }
            }
        }

        public string SunriseTimeFontColor
        {
            get => _sunriseTimeFontColor;
            set
            {
                if (_sunriseTimeFontColor != value)
                {
                    _sunriseTimeFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public double SunriseTimeFontSize
        {
            get => _sunriseTimeFontSize;
            set
            {
                if (Math.Abs(_sunriseTimeFontSize - value) > 0.001)
                {
                    _sunriseTimeFontSize = Math.Max(6, Math.Min(72, value));
                    OnPropertyChanged();
                }
            }
        }

        public string SunsetLabelFontColor
        {
            get => _sunsetLabelFontColor;
            set
            {
                if (_sunsetLabelFontColor != value)
                {
                    _sunsetLabelFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public double SunsetLabelFontSize
        {
            get => _sunsetLabelFontSize;
            set
            {
                if (Math.Abs(_sunsetLabelFontSize - value) > 0.001)
                {
                    _sunsetLabelFontSize = Math.Max(6, Math.Min(72, value));
                    OnPropertyChanged();
                }
            }
        }

        public string SunsetTimeFontColor
        {
            get => _sunsetTimeFontColor;
            set
            {
                if (_sunsetTimeFontColor != value)
                {
                    _sunsetTimeFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public double SunsetTimeFontSize
        {
            get => _sunsetTimeFontSize;
            set
            {
                if (Math.Abs(_sunsetTimeFontSize - value) > 0.001)
                {
                    _sunsetTimeFontSize = Math.Max(6, Math.Min(72, value));
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunriseLabelEnableCustomFontSize = false;

        public bool SunriseLabelEnableCustomFontSize
        {
            get => _sunriseLabelEnableCustomFontSize;
            set
            {
                if (_sunriseLabelEnableCustomFontSize != value)
                {
                    _sunriseLabelEnableCustomFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunriseLabelEnableCustomFontColor = false;

        public bool SunriseLabelEnableCustomFontColor
        {
            get => _sunriseLabelEnableCustomFontColor;
            set
            {
                if (_sunriseLabelEnableCustomFontColor != value)
                {
                    _sunriseLabelEnableCustomFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunriseTimeEnableCustomFontSize = false;

        public bool SunriseTimeEnableCustomFontSize
        {
            get => _sunriseTimeEnableCustomFontSize;
            set
            {
                if (_sunriseTimeEnableCustomFontSize != value)
                {
                    _sunriseTimeEnableCustomFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunriseTimeEnableCustomFontColor = false;

        public bool SunriseTimeEnableCustomFontColor
        {
            get => _sunriseTimeEnableCustomFontColor;
            set
            {
                if (_sunriseTimeEnableCustomFontColor != value)
                {
                    _sunriseTimeEnableCustomFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunsetLabelEnableCustomFontSize = false;

        public bool SunsetLabelEnableCustomFontSize
        {
            get => _sunsetLabelEnableCustomFontSize;
            set
            {
                if (_sunsetLabelEnableCustomFontSize != value)
                {
                    _sunsetLabelEnableCustomFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunsetLabelEnableCustomFontColor = false;

        public bool SunsetLabelEnableCustomFontColor
        {
            get => _sunsetLabelEnableCustomFontColor;
            set
            {
                if (_sunsetLabelEnableCustomFontColor != value)
                {
                    _sunsetLabelEnableCustomFontColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunsetTimeEnableCustomFontSize = false;

        public bool SunsetTimeEnableCustomFontSize
        {
            get => _sunsetTimeEnableCustomFontSize;
            set
            {
                if (_sunsetTimeEnableCustomFontSize != value)
                {
                    _sunsetTimeEnableCustomFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _sunsetTimeEnableCustomFontColor = false;

        public bool SunsetTimeEnableCustomFontColor
        {
            get => _sunsetTimeEnableCustomFontColor;
            set
            {
                if (_sunsetTimeEnableCustomFontColor != value)
                {
                    _sunsetTimeEnableCustomFontColor = value;
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


