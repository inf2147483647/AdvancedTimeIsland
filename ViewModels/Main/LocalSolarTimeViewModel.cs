﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class LocalSolarTimeViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly LocalSolarTimeSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateFontColor;
    private readonly Action<double> _updateFontSize;
    private string _fullDisplay = string.Empty;
    private bool _isDisposed;

    public LocalSolarTimeViewModel(TimeBaseService timeBaseService, LocalSolarTimeSettings settings, Action<string> updateFontColor = null, Action<double> updateFontSize = null)
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
        if (e.PropertyName == nameof(LocalSolarTimeSettings.FontColor))
        {
            _updateFontColor?.Invoke(_settings.FontColor);
        }
        else if (e.PropertyName == nameof(LocalSolarTimeSettings.Longitude))
        {
            UpdateTime();
        }
        else if (e.PropertyName == nameof(LocalSolarTimeSettings.TextFontSize))
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
            var localSolarTime = CalculateLocalSolarTime(now);
            
            FullDisplay = FormatFullDisplay(localSolarTime);
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
            var localSolarTime = CalculateLocalSolarTime(now);
            var display = FormatFullDisplay(localSolarTime);
            
            await Dispatcher.UIThread.InvokeAsync(() => { FullDisplay = display; });
        }
        catch (Exception)
        {
        }
    }

    private DateTime CalculateLocalSolarTime(DateTime utcTime)
    {
        var localTime = utcTime.ToLocalTime();
        var standardMeridian = TimeZoneInfo.Local.BaseUtcOffset.TotalHours * 15;
        var timeDifferenceMinutes = (_settings.Longitude - standardMeridian) * 4;
        return localTime.AddMinutes(timeDifferenceMinutes);
    }

    private string FormatFullDisplay(DateTime dateTime)
    {
        double lon = _settings.Longitude;
        string direction = lon >= 0 ? "E" : "W";
        return $"地方时 {dateTime:yyyy-MM-dd} {dateTime:HH:mm:ss} {Math.Abs(lon)}°{direction}";
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
