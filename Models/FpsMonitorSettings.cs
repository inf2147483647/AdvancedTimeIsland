using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public class FpsMonitorSettings : INotifyPropertyChanged
{
    private string _labelFontColor = "";
    private double _labelFontSize = 14;
    private string _valueFontColor = "";
    private double _valueFontSize = 14;
    private bool _enableCustomFontSize = false;
    private bool _enableCustomFontColor = false;
    private bool _enableComponent = false;

    public bool EnableComponent
    {
        get => _enableComponent;
        set
        {
            if (_enableComponent != value)
            {
                _enableComponent = value;
                OnPropertyChanged();
            }
        }
    }

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
            if (_labelFontSize != value)
            {
                _labelFontSize = value;
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
            if (_valueFontSize != value)
            {
                _valueFontSize = value;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}