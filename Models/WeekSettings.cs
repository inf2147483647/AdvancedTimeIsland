using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

/// <summary>
/// 主界面组件共用的设置基类（周数、在校时间统计等）：
/// 提供字体大小、颜色、样式、字重的自定义开关与默认值，以及时间基准。
/// </summary>
public abstract class WeekSettings : INotifyPropertyChanged
{
    private double _fontSize = 14;
    private string _fontColor = "";
    private string _fontFamily = "";
    private string _fontWeight = "Normal";
    private bool _enableCustomFontSize = false;
    private bool _enableCustomFontColor = false;
    private bool _enableCustomFontFamily = false;
    private bool _enableCustomFontWeight = false;
    private TimeBaseType _timeBaseType = TimeBaseType.PluginOffsetServerTime;

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

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (Math.Abs(_fontSize - value) > 0.001)
            {
                _fontSize = Math.Max(6, Math.Min(72, value));
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

/// <summary>
/// 今日周数（年）组件设置。
/// </summary>
public class YearWeekSettings : WeekSettings
{
    /// <summary>
    /// 一周的第一天。取值对应 <see cref="DayOfWeek"/>：0=周日,1=周一,...,6=周六。
    /// </summary>
    private int _firstDayOfWeek = 0;

    public int FirstDayOfWeek
    {
        get => _firstDayOfWeek;
        set
        {
            var v = ((value % 7) + 7) % 7;
            if (_firstDayOfWeek != v)
            {
                _firstDayOfWeek = v;
                OnPropertyChanged();
            }
        }
    }
}

/// <summary>
/// 今日周数（学期）组件设置。
/// </summary>
public class SemesterWeekSettings : WeekSettings
{
}