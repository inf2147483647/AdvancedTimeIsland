using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class AdvancedDateViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly AdvancedDateSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateFontColor;
    private readonly Action<double> _updateFontSize;
    private string _dateDisplay = string.Empty;
    private bool _isDisposed;

    public AdvancedDateViewModel(TimeBaseService timeBaseService, AdvancedDateSettings settings, Action<string> updateFontColor = null, Action<double> updateFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateFontColor = updateFontColor;
        _updateFontSize = updateFontSize;
        
        _settings.PropertyChanged += OnSettingsChanged;
        
        UpdateTime();
        
        _updateTimer = new System.Timers.Timer(200);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdvancedDateSettings.FontColor))
        {
            _updateFontColor?.Invoke(_settings.FontColor);
        }
        else if (e.PropertyName == nameof(AdvancedDateSettings.ShowWeekDay))
        {
            UpdateTime();
        }
        else if (e.PropertyName == nameof(AdvancedDateSettings.DateFontSize))
        {
            _updateFontSize?.Invoke(_settings.DateFontSize);
        }
    }

    public string DateDisplay
    {
        get => _dateDisplay;
        private set
        {
            if (_dateDisplay != value)
            {
                _dateDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _ = UpdateTimeAsync();
    }

    private void UpdateTime()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            DateDisplay = FormatDateFull(now);
        }
        catch (Exception)
        {
        }
    }

    private async System.Threading.Tasks.Task UpdateTimeAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            var display = FormatDateFull(now);
            
            await Dispatcher.UIThread.InvokeAsync(() => { DateDisplay = display; });
        }
        catch (Exception)
        {
        }
    }

    private string FormatDateFull(DateTime dateTime)
    {
        string weekDay = "";
        if (_settings.ShowWeekDay)
        {
            weekDay = dateTime.DayOfWeek switch
            {
                DayOfWeek.Sunday => "周日",
                DayOfWeek.Monday => "周一",
                DayOfWeek.Tuesday => "周二",
                DayOfWeek.Wednesday => "周三",
                DayOfWeek.Thursday => "周四",
                DayOfWeek.Friday => "周五",
                DayOfWeek.Saturday => "周六",
                _ => ""
            };
        }
        
        return $"{dateTime.Year}-{dateTime.Month:D2}-{dateTime.Day:D2}" + (string.IsNullOrEmpty(weekDay) ? "" : $" {weekDay}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _settings.PropertyChanged -= OnSettingsChanged;
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}



