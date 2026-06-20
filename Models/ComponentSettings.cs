using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

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