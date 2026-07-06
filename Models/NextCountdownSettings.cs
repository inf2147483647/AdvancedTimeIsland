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
    private string _text1FontColor = "#FFFFFF";
    private string _nameFontColor = "#FFFFFF";
    private string _text3FontColor = "#FFFFFF";
    private string _timeFontColor = "#FFFFFF";

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

    private bool _enableCustomColorAndFont = false;

    public bool EnableCustomColorAndFont
    {
        get => _enableCustomColorAndFont;
        set
        {
            if (_enableCustomColorAndFont != value)
            {
                _enableCustomColorAndFont = value;
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

public class NextXingZuoCountdownSettings : INotifyPropertyChanged
{
    private string _text1 = "下个星座";
    private string _text3 = "还有";
    private string _timeFormat = "%d天";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private string _text1FontColor = "#FFFFFF";
    private string _nameFontColor = "#FFFFFF";
    private string _text3FontColor = "#FFFFFF";
    private string _timeFontColor = "#FFFFFF";

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

    private bool _enableCustomColorAndFont = false;

    public bool EnableCustomColorAndFont
    {
        get => _enableCustomColorAndFont;
        set
        {
            if (_enableCustomColorAndFont != value)
            {
                _enableCustomColorAndFont = value;
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

public class NextFestivalCountdownSettings : INotifyPropertyChanged
{
    private string _text1 = "下个节日";
    private string _text3 = "还有";
    private string _timeFormat = "%d天";
    private double _text1FontSize = 14;
    private double _nameFontSize = 14;
    private double _text3FontSize = 14;
    private double _timeFontSize = 14;
    private string _text1FontColor = "#FFFFFF";
    private string _nameFontColor = "#FFFFFF";
    private string _text3FontColor = "#FFFFFF";
    private string _timeFontColor = "#FFFFFF";
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

    private bool _enableCustomColorAndFont = false;

    public bool EnableCustomColorAndFont
    {
        get => _enableCustomColorAndFont;
        set
        {
            if (_enableCustomColorAndFont != value)
            {
                _enableCustomColorAndFont = value;
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

public class TomorrowYiJiSettings : INotifyPropertyChanged
{
    private string _yiLabel = "明日宜";
    private string _jiLabel = "明日忌";
    private double _yiLabelFontSize = 14;
    private double _yiValueFontSize = 14;
    private double _jiLabelFontSize = 14;
    private double _jiValueFontSize = 14;
    private string _yiLabelFontColor = "#FFFFFF";
    private string _jiLabelFontColor = "#FFFFFF";

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

    private bool _enableCustomColorAndFont = false;

    public bool EnableCustomColorAndFont
    {
        get => _enableCustomColorAndFont;
        set
        {
            if (_enableCustomColorAndFont != value)
            {
                _enableCustomColorAndFont = value;
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
