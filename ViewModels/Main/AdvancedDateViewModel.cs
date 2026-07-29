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
    private readonly Action<string> _updateDateFontColor;
    private readonly Action<double> _updateDateFontSize;
    private readonly Action<string> _updateWeekDayFontColor;
    private readonly Action<double> _updateWeekDayFontSize;
    private string _datePart = string.Empty;
    private string _weekDayPart = string.Empty;
    private bool _isDisposed;

    public AdvancedDateViewModel(TimeBaseService timeBaseService, AdvancedDateSettings settings, 
        Action<string> updateDateFontColor = null, Action<double> updateDateFontSize = null,
        Action<string> updateWeekDayFontColor = null, Action<double> updateWeekDayFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateDateFontColor = updateDateFontColor;
        _updateDateFontSize = updateDateFontSize;
        _updateWeekDayFontColor = updateWeekDayFontColor;
        _updateWeekDayFontSize = updateWeekDayFontSize;
        
        _settings.PropertyChanged += OnSettingsChanged;
        
        UpdateTime();
        
        _updateTimer = new System.Timers.Timer(200);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdvancedDateSettings.FontColor) ||
            e.PropertyName == nameof(AdvancedDateSettings.EnableCustomFontColor))
        {
            _updateDateFontColor?.Invoke(_settings.FontColor);
        }
        if (e.PropertyName == nameof(AdvancedDateSettings.WeekDayFontColor) ||
            e.PropertyName == nameof(AdvancedDateSettings.WeekDayEnableCustomFontColor))
        {
            _updateWeekDayFontColor?.Invoke(_settings.WeekDayFontColor);
        }
        if (e.PropertyName == nameof(AdvancedDateSettings.ShowWeekDay) ||
            e.PropertyName == nameof(AdvancedDateSettings.DateContentOrder) ||
            e.PropertyName == nameof(AdvancedDateSettings.DateSeparator))
        {
            UpdateTime();
        }
        if (e.PropertyName == nameof(AdvancedDateSettings.DateFontSize) ||
             e.PropertyName == nameof(AdvancedDateSettings.EnableCustomFontSize) ||
             e.PropertyName == nameof(AdvancedDateSettings.FontFamily) ||
             e.PropertyName == nameof(AdvancedDateSettings.EnableCustomFontFamily) ||
             e.PropertyName == nameof(AdvancedDateSettings.FontWeight) ||
             e.PropertyName == nameof(AdvancedDateSettings.EnableCustomFontWeight))
        {
            _updateDateFontSize?.Invoke(_settings.EnableCustomFontSize ? _settings.DateFontSize : 0);
        }
        if (e.PropertyName == nameof(AdvancedDateSettings.WeekDayFontSize) ||
             e.PropertyName == nameof(AdvancedDateSettings.WeekDayEnableCustomFontSize) ||
             e.PropertyName == nameof(AdvancedDateSettings.WeekDayFontFamily) ||
             e.PropertyName == nameof(AdvancedDateSettings.WeekDayEnableCustomFontFamily) ||
             e.PropertyName == nameof(AdvancedDateSettings.WeekDayFontWeight) ||
             e.PropertyName == nameof(AdvancedDateSettings.WeekDayEnableCustomFontWeight))
        {
            _updateWeekDayFontSize?.Invoke(_settings.WeekDayEnableCustomFontSize ? _settings.WeekDayFontSize : 0);
        }
    }

    public string DatePart
    {
        get => _datePart;
        private set
        {
            if (_datePart != value)
            {
                _datePart = value;
                OnPropertyChanged();
            }
        }
    }

    public string WeekDayPart
    {
        get => _weekDayPart;
        private set
        {
            if (_weekDayPart != value)
            {
                _weekDayPart = value;
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
            var (datePart, weekDayPart) = BuildDisplayParts(now);
            DatePart = datePart;
            WeekDayPart = weekDayPart;
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
            var (datePart, weekDayPart) = BuildDisplayParts(now);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DatePart = datePart;
                WeekDayPart = weekDayPart;
            });
        }
        catch (Exception)
        {
        }
    }

    private (string datePart, string weekDayPart) BuildDisplayParts(DateTime now)
    {
        var dateStr = _settings.DateSeparator switch
        {
            0 => $"{now.Year}-{now.Month:D2}-{now.Day:D2}",
            1 => $"{now.Year}/{now.Month:D2}/{now.Day:D2}",
            2 => $"{now.Year}.{now.Month:D2}.{now.Day:D2}",
            3 => $"{now.Year} 年 {now.Month} 月 {now.Day} 日",
            _ => $"{now.Year}-{now.Month:D2}-{now.Day:D2}"
        };

        var weekDayStr = "";
        if (_settings.ShowWeekDay)
        {
            weekDayStr = now.DayOfWeek switch
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

        // DateContentOrder: 0 = 日期-星期, 1 = 星期-日期
        if (_settings.ShowWeekDay && _settings.DateContentOrder == 1)
        {
            return (weekDayStr, dateStr);
        }
        return (dateStr, weekDayStr);
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
