﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class TimeZoneTimeViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly TimeZoneTimeSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateFontColor;
    private readonly Action<double> _updateFontSize;
    private string _fullDisplay = string.Empty;
    private bool _isDisposed;

    public TimeZoneTimeViewModel(TimeBaseService timeBaseService, TimeZoneTimeSettings settings, Action<string> updateFontColor = null, Action<double> updateFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateFontColor = updateFontColor;
        _updateFontSize = updateFontSize;
        
        _settings.PropertyChanged += OnSettingsChanged;
        
        UpdateTime();
        
        _updateTimer = new System.Timers.Timer(20);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimeZoneTimeSettings.FontColor))
        {
            _updateFontColor?.Invoke(_settings.FontColor);
        }
        else if (e.PropertyName == nameof(TimeZoneTimeSettings.TimeZoneId))
        {
            UpdateTime();
        }
        else if (e.PropertyName == nameof(TimeZoneTimeSettings.TextFontSize))
        {
            _updateFontSize?.Invoke(_settings.TextFontSize);
        }
    }

    public string FullDisplay
    {
        get => _fullDisplay;
        private set
        {
            if (_fullDisplay != value)
            {
                _fullDisplay = value;
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
            var timeZoneTime = ConvertToTimeZone(now);
            
            FullDisplay = FormatFullDisplay(timeZoneTime);
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
            var timeZoneTime = ConvertToTimeZone(now);
            var display = FormatFullDisplay(timeZoneTime);
            
            await Dispatcher.UIThread.InvokeAsync(() => { FullDisplay = display; });
        }
        catch (Exception)
        {
        }
    }

    private DateTime ConvertToTimeZone(DateTime utcTime)
    {
        try
        {
            var targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
            return TimeZoneInfo.ConvertTime(utcTime, targetTimeZone);
        }
        catch
        {
            return utcTime.ToLocalTime();
        }
    }

    private string FormatFullDisplay(DateTime dateTime)
    {
        string timeZoneName = GetTimeZoneDisplayName();
        return $"区时 {dateTime:yyyy-MM-dd} {dateTime:HH:mm:ss} {timeZoneName}";
    }

    private string GetTimeZoneDisplayName()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
            if (tz != null)
            {
                double offset = tz.BaseUtcOffset.TotalHours;
                string sign = offset >= 0 ? "东" : "西";
                int hours = Math.Abs((int)offset);
                int minutes = Math.Abs(tz.BaseUtcOffset.Minutes);
                
                string decimalPart = "";
                if (minutes == 15)
                    decimalPart = ".25";
                else if (minutes == 30)
                    decimalPart = ".5";
                else if (minutes == 45)
                    decimalPart = ".75";
                
                return $"{sign}{hours}{decimalPart}区";
            }
            return "未知时区";
        }
        catch
        {
            return "未知时区";
        }
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
