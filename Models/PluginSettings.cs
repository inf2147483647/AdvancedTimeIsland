using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public class PluginSettings : INotifyPropertyChanged
{
    private bool _isLunarEnabled = true;
    private bool _isGeoTimeEnabled = true;
    private string _timeBase = "ClassIsland";
    private double _longitude;
    private bool _isWomenWearEnabled;
    private bool _isWomenWearVisible;

    public bool IsLunarEnabled
    {
        get => _isLunarEnabled;
        set
        {
            _isLunarEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsGeoTimeEnabled
    {
        get => _isGeoTimeEnabled;
        set
        {
            _isGeoTimeEnabled = value;
            OnPropertyChanged();
        }
    }

    public string TimeBase
    {
        get => _timeBase;
        set
        {
            _timeBase = value;
            OnPropertyChanged();
        }
    }

    public double Longitude
    {
        get => _longitude;
        set
        {
            _longitude = value;
            OnPropertyChanged();
        }
    }

    public bool IsWomenWearEnabled
    {
        get => _isWomenWearEnabled;
        set
        {
            _isWomenWearEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsWomenWearVisible
    {
        get => _isWomenWearVisible;
        set
        {
            _isWomenWearVisible = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}