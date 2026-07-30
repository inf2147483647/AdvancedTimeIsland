using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public class NextJieQiCountdownSettings : INotifyPropertyChanged
{
    private string _text1 = "下个节气";
    private string _text3 = "还有";
    private string _timeFormat = "%d天";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private string _text1FontColor = "";
    private string _nameFontColor = "";
    private string _text3FontColor = "";
    private string _timeFontColor = "";

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

    private bool _text1EnableCustomFontSize = false;
    public bool Text1EnableCustomFontSize
    {
        get => _text1EnableCustomFontSize;
        set { if (_text1EnableCustomFontSize != value) { _text1EnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _text1EnableCustomFontColor = false;
    public bool Text1EnableCustomFontColor
    {
        get => _text1EnableCustomFontColor;
        set { if (_text1EnableCustomFontColor != value) { _text1EnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _nameEnableCustomFontSize = false;
    public bool NameEnableCustomFontSize
    {
        get => _nameEnableCustomFontSize;
        set { if (_nameEnableCustomFontSize != value) { _nameEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _nameEnableCustomFontColor = false;
    public bool NameEnableCustomFontColor
    {
        get => _nameEnableCustomFontColor;
        set { if (_nameEnableCustomFontColor != value) { _nameEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _text3EnableCustomFontSize = false;
    public bool Text3EnableCustomFontSize
    {
        get => _text3EnableCustomFontSize;
        set { if (_text3EnableCustomFontSize != value) { _text3EnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _text3EnableCustomFontColor = false;
    public bool Text3EnableCustomFontColor
    {
        get => _text3EnableCustomFontColor;
        set { if (_text3EnableCustomFontColor != value) { _text3EnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _timeEnableCustomFontSize = false;
    public bool TimeEnableCustomFontSize
    {
        get => _timeEnableCustomFontSize;
        set { if (_timeEnableCustomFontSize != value) { _timeEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _timeEnableCustomFontColor = false;
    public bool TimeEnableCustomFontColor
    {
        get => _timeEnableCustomFontColor;
        set { if (_timeEnableCustomFontColor != value) { _timeEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class NextXingZuoCountdownSettings : INotifyPropertyChanged
{
    private string _text1 = "下个星座";
    private string _text3 = "还有";
    private string _timeFormat = "%d天";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private string _text1FontColor = "";
    private string _nameFontColor = "";
    private string _text3FontColor = "";
    private string _timeFontColor = "";

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

    private bool _text1EnableCustomFontSize = false;
    public bool Text1EnableCustomFontSize
    {
        get => _text1EnableCustomFontSize;
        set { if (_text1EnableCustomFontSize != value) { _text1EnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _text1EnableCustomFontColor = false;
    public bool Text1EnableCustomFontColor
    {
        get => _text1EnableCustomFontColor;
        set { if (_text1EnableCustomFontColor != value) { _text1EnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _nameEnableCustomFontSize = false;
    public bool NameEnableCustomFontSize
    {
        get => _nameEnableCustomFontSize;
        set { if (_nameEnableCustomFontSize != value) { _nameEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _nameEnableCustomFontColor = false;
    public bool NameEnableCustomFontColor
    {
        get => _nameEnableCustomFontColor;
        set { if (_nameEnableCustomFontColor != value) { _nameEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _text3EnableCustomFontSize = false;
    public bool Text3EnableCustomFontSize
    {
        get => _text3EnableCustomFontSize;
        set { if (_text3EnableCustomFontSize != value) { _text3EnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _text3EnableCustomFontColor = false;
    public bool Text3EnableCustomFontColor
    {
        get => _text3EnableCustomFontColor;
        set { if (_text3EnableCustomFontColor != value) { _text3EnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _timeEnableCustomFontSize = false;
    public bool TimeEnableCustomFontSize
    {
        get => _timeEnableCustomFontSize;
        set { if (_timeEnableCustomFontSize != value) { _timeEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _timeEnableCustomFontColor = false;
    public bool TimeEnableCustomFontColor
    {
        get => _timeEnableCustomFontColor;
        set { if (_timeEnableCustomFontColor != value) { _timeEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class NextFestivalCountdownSettings : INotifyPropertyChanged
{
    private string _text1 = "下个节日";
    private string _text3 = "还有";
    private string _timeFormat = "%d天";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private string _text1FontColor = "";
    private string _nameFontColor = "";
    private string _text3FontColor = "";
    private string _timeFontColor = "";
    private bool _enableInternationalFestivals = true;
    private bool _enableChineseTraditionalFestivals = true;
    private bool _enableRedFestivals = true;

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

    private bool _enableExperimentalFeatures = false;

    public bool EnableExperimentalFeatures
    {
        get => _enableExperimentalFeatures;
        set
        {
            if (_enableExperimentalFeatures != value)
            {
                _enableExperimentalFeatures = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _text1EnableCustomFontSize = false;
    public bool Text1EnableCustomFontSize
    {
        get => _text1EnableCustomFontSize;
        set { if (_text1EnableCustomFontSize != value) { _text1EnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _text1EnableCustomFontColor = false;
    public bool Text1EnableCustomFontColor
    {
        get => _text1EnableCustomFontColor;
        set { if (_text1EnableCustomFontColor != value) { _text1EnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _nameEnableCustomFontSize = false;
    public bool NameEnableCustomFontSize
    {
        get => _nameEnableCustomFontSize;
        set { if (_nameEnableCustomFontSize != value) { _nameEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _nameEnableCustomFontColor = false;
    public bool NameEnableCustomFontColor
    {
        get => _nameEnableCustomFontColor;
        set { if (_nameEnableCustomFontColor != value) { _nameEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _text3EnableCustomFontSize = false;
    public bool Text3EnableCustomFontSize
    {
        get => _text3EnableCustomFontSize;
        set { if (_text3EnableCustomFontSize != value) { _text3EnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _text3EnableCustomFontColor = false;
    public bool Text3EnableCustomFontColor
    {
        get => _text3EnableCustomFontColor;
        set { if (_text3EnableCustomFontColor != value) { _text3EnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _timeEnableCustomFontSize = false;
    public bool TimeEnableCustomFontSize
    {
        get => _timeEnableCustomFontSize;
        set { if (_timeEnableCustomFontSize != value) { _timeEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _timeEnableCustomFontColor = false;
    public bool TimeEnableCustomFontColor
    {
        get => _timeEnableCustomFontColor;
        set { if (_timeEnableCustomFontColor != value) { _timeEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TomorrowYiJiSettings : INotifyPropertyChanged
{
    private string _yiLabel = "明日宜";
    private string _jiLabel = "明日忌";
    private double _yiLabelFontSize = 14;
    private double _yiValueFontSize = 14;
    private double _jiLabelFontSize = 14;
    private double _jiValueFontSize = 14;
    private string _yiLabelFontColor = "";
    private string _jiLabelFontColor = "";
    private int _displayMode = 1;

    public string YiLabel
    {
        get => _yiLabel;
        set
        {
            if (_yiLabel != value)
            {
                _yiLabel = value;
                OnPropertyChanged();
            }
        }
    }

    public string JiLabel
    {
        get => _jiLabel;
        set
        {
            if (_jiLabel != value)
            {
                _jiLabel = value;
                OnPropertyChanged();
            }
        }
    }

    public int DisplayMode
    {
        get => _displayMode;
        set { if (_displayMode != value) { _displayMode = value; OnPropertyChanged(); } }
    }

    public double YiLabelFontSize
    {
        get => _yiLabelFontSize;
        set
        {
            if (Math.Abs(_yiLabelFontSize - value) > 0.001)
            {
                _yiLabelFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double YiValueFontSize
    {
        get => _yiValueFontSize;
        set
        {
            if (Math.Abs(_yiValueFontSize - value) > 0.001)
            {
                _yiValueFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double JiLabelFontSize
    {
        get => _jiLabelFontSize;
        set
        {
            if (Math.Abs(_jiLabelFontSize - value) > 0.001)
            {
                _jiLabelFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public double JiValueFontSize
    {
        get => _jiValueFontSize;
        set
        {
            if (Math.Abs(_jiValueFontSize - value) > 0.001)
            {
                _jiValueFontSize = Math.Max(6, Math.Min(72, value));
                OnPropertyChanged();
            }
        }
    }

    public string YiLabelFontColor
    {
        get => _yiLabelFontColor;
        set
        {
            if (_yiLabelFontColor != value)
            {
                _yiLabelFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string JiLabelFontColor
    {
        get => _jiLabelFontColor;
        set
        {
            if (_jiLabelFontColor != value)
            {
                _jiLabelFontColor = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _yiLabelEnableCustomFontSize = false;
    public bool YiLabelEnableCustomFontSize
    {
        get => _yiLabelEnableCustomFontSize;
        set { if (_yiLabelEnableCustomFontSize != value) { _yiLabelEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _yiLabelEnableCustomFontColor = false;
    public bool YiLabelEnableCustomFontColor
    {
        get => _yiLabelEnableCustomFontColor;
        set { if (_yiLabelEnableCustomFontColor != value) { _yiLabelEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _yiValueEnableCustomFontSize = false;
    public bool YiValueEnableCustomFontSize
    {
        get => _yiValueEnableCustomFontSize;
        set { if (_yiValueEnableCustomFontSize != value) { _yiValueEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _jiLabelEnableCustomFontSize = false;
    public bool JiLabelEnableCustomFontSize
    {
        get => _jiLabelEnableCustomFontSize;
        set { if (_jiLabelEnableCustomFontSize != value) { _jiLabelEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _jiLabelEnableCustomFontColor = false;
    public bool JiLabelEnableCustomFontColor
    {
        get => _jiLabelEnableCustomFontColor;
        set { if (_jiLabelEnableCustomFontColor != value) { _jiLabelEnableCustomFontColor = value; OnPropertyChanged(); } }
    }

    private bool _jiValueEnableCustomFontSize = false;
    public bool JiValueEnableCustomFontSize
    {
        get => _jiValueEnableCustomFontSize;
        set { if (_jiValueEnableCustomFontSize != value) { _jiValueEnableCustomFontSize = value; OnPropertyChanged(); } }
    }

    private bool _yiLabelEnableCustomFontFamily = false;
    public bool YiLabelEnableCustomFontFamily
    {
        get => _yiLabelEnableCustomFontFamily;
        set { if (_yiLabelEnableCustomFontFamily != value) { _yiLabelEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    private string _yiLabelFontFamily = "";
    public string YiLabelFontFamily
    {
        get => _yiLabelFontFamily;
        set { if (_yiLabelFontFamily != value) { _yiLabelFontFamily = value; OnPropertyChanged(); } }
    }

    private bool _yiValueEnableCustomFontFamily = false;
    public bool YiValueEnableCustomFontFamily
    {
        get => _yiValueEnableCustomFontFamily;
        set { if (_yiValueEnableCustomFontFamily != value) { _yiValueEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    private string _yiValueFontFamily = "";
    public string YiValueFontFamily
    {
        get => _yiValueFontFamily;
        set { if (_yiValueFontFamily != value) { _yiValueFontFamily = value; OnPropertyChanged(); } }
    }

    private bool _jiLabelEnableCustomFontFamily = false;
    public bool JiLabelEnableCustomFontFamily
    {
        get => _jiLabelEnableCustomFontFamily;
        set { if (_jiLabelEnableCustomFontFamily != value) { _jiLabelEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    private string _jiLabelFontFamily = "";
    public string JiLabelFontFamily
    {
        get => _jiLabelFontFamily;
        set { if (_jiLabelFontFamily != value) { _jiLabelFontFamily = value; OnPropertyChanged(); } }
    }

    private bool _jiValueEnableCustomFontFamily = false;
    public bool JiValueEnableCustomFontFamily
    {
        get => _jiValueEnableCustomFontFamily;
        set { if (_jiValueEnableCustomFontFamily != value) { _jiValueEnableCustomFontFamily = value; OnPropertyChanged(); } }
    }

    private string _jiValueFontFamily = "";
    public string JiValueFontFamily
    {
        get => _jiValueFontFamily;
        set { if (_jiValueFontFamily != value) { _jiValueFontFamily = value; OnPropertyChanged(); } }
    }

    private string _yiLabelFontWeight = "Normal";
    public string YiLabelFontWeight
    {
        get => _yiLabelFontWeight;
        set { if (_yiLabelFontWeight != value) { _yiLabelFontWeight = value; OnPropertyChanged(); } }
    }

    private string _yiValueFontWeight = "Normal";
    public string YiValueFontWeight
    {
        get => _yiValueFontWeight;
        set { if (_yiValueFontWeight != value) { _yiValueFontWeight = value; OnPropertyChanged(); } }
    }

    private string _jiLabelFontWeight = "Normal";
    public string JiLabelFontWeight
    {
        get => _jiLabelFontWeight;
        set { if (_jiLabelFontWeight != value) { _jiLabelFontWeight = value; OnPropertyChanged(); } }
    }

    private string _jiValueFontWeight = "Normal";
    public string JiValueFontWeight
    {
        get => _jiValueFontWeight;
        set { if (_jiValueFontWeight != value) { _jiValueFontWeight = value; OnPropertyChanged(); } }
    }

    private bool _yiLabelEnableCustomFontWeight = false;
    public bool YiLabelEnableCustomFontWeight
    {
        get => _yiLabelEnableCustomFontWeight;
        set { if (_yiLabelEnableCustomFontWeight != value) { _yiLabelEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    private bool _yiValueEnableCustomFontWeight = false;
    public bool YiValueEnableCustomFontWeight
    {
        get => _yiValueEnableCustomFontWeight;
        set { if (_yiValueEnableCustomFontWeight != value) { _yiValueEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    private bool _jiLabelEnableCustomFontWeight = false;
    public bool JiLabelEnableCustomFontWeight
    {
        get => _jiLabelEnableCustomFontWeight;
        set { if (_jiLabelEnableCustomFontWeight != value) { _jiLabelEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    private bool _jiValueEnableCustomFontWeight = false;
    public bool JiValueEnableCustomFontWeight
    {
        get => _jiValueEnableCustomFontWeight;
        set { if (_jiValueEnableCustomFontWeight != value) { _jiValueEnableCustomFontWeight = value; OnPropertyChanged(); } }
    }

    private bool _infoBarDismissed = false;

    public bool InfoBarDismissed
    {
        get => _infoBarDismissed;
        set { if (_infoBarDismissed != value) { _infoBarDismissed = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
