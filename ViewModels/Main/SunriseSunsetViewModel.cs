using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Helpers;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class SunriseSunsetViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly SunriseSunsetSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string, string> _updateSunriseLabelColor;
    private readonly Action<double, string> _updateSunriseLabelSize;
    private readonly Action<string, string> _updateSunriseTimeColor;
    private readonly Action<double, string> _updateSunriseTimeSize;
    private readonly Action<string, string> _updateSunsetLabelColor;
    private readonly Action<double, string> _updateSunsetLabelSize;
    private readonly Action<string, string> _updateSunsetTimeColor;
    private readonly Action<double, string> _updateSunsetTimeSize;
    private string _sunriseLabel = "日出：";
    private string _sunriseTime = "--:--";
    private string _sunsetLabel = "日落：";
    private string _sunsetTime = "--:--";
    private bool _isDisposed;

    public SunriseSunsetViewModel(TimeBaseService timeBaseService, SunriseSunsetSettings settings,
        Action<string, string> updateSunriseLabelColor = null,
        Action<double, string> updateSunriseLabelSize = null,
        Action<string, string> updateSunriseTimeColor = null,
        Action<double, string> updateSunriseTimeSize = null,
        Action<string, string> updateSunsetLabelColor = null,
        Action<double, string> updateSunsetLabelSize = null,
        Action<string, string> updateSunsetTimeColor = null,
        Action<double, string> updateSunsetTimeSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateSunriseLabelColor = updateSunriseLabelColor;
        _updateSunriseLabelSize = updateSunriseLabelSize;
        _updateSunriseTimeColor = updateSunriseTimeColor;
        _updateSunriseTimeSize = updateSunriseTimeSize;
        _updateSunsetLabelColor = updateSunsetLabelColor;
        _updateSunsetLabelSize = updateSunsetLabelSize;
        _updateSunsetTimeColor = updateSunsetTimeColor;
        _updateSunsetTimeSize = updateSunsetTimeSize;

        _settings.PropertyChanged += OnSettingsChanged;

        UpdateTimes();

        _updateTimer = new System.Timers.Timer(60000);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SunriseSunsetSettings.SunriseLabelFontColor):
                _updateSunriseLabelColor?.Invoke(_settings.SunriseLabelFontColor, "sunriseLabel");
                break;
            case nameof(SunriseSunsetSettings.SunriseLabelFontSize):
                _updateSunriseLabelSize?.Invoke(_settings.SunriseLabelFontSize, "sunriseLabel");
                break;
            case nameof(SunriseSunsetSettings.SunriseTimeFontColor):
                _updateSunriseTimeColor?.Invoke(_settings.SunriseTimeFontColor, "sunriseTime");
                break;
            case nameof(SunriseSunsetSettings.SunriseTimeFontSize):
                _updateSunriseTimeSize?.Invoke(_settings.SunriseTimeFontSize, "sunriseTime");
                break;
            case nameof(SunriseSunsetSettings.SunsetLabelFontColor):
                _updateSunsetLabelColor?.Invoke(_settings.SunsetLabelFontColor, "sunsetLabel");
                break;
            case nameof(SunriseSunsetSettings.SunsetLabelFontSize):
                _updateSunsetLabelSize?.Invoke(_settings.SunsetLabelFontSize, "sunsetLabel");
                break;
            case nameof(SunriseSunsetSettings.SunsetTimeFontColor):
                _updateSunsetTimeColor?.Invoke(_settings.SunsetTimeFontColor, "sunsetTime");
                break;
            case nameof(SunriseSunsetSettings.SunsetTimeFontSize):
                _updateSunsetTimeSize?.Invoke(_settings.SunsetTimeFontSize, "sunsetTime");
                break;
            case nameof(SunriseSunsetSettings.Longitude):
            case nameof(SunriseSunsetSettings.Latitude):
            case nameof(SunriseSunsetSettings.TimeZoneId):
                _ = UpdateTimesAsync();
                break;
        }
    }

    public string SunriseLabel
    {
        get => _sunriseLabel;
        private set
        {
            if (_sunriseLabel != value)
            {
                _sunriseLabel = value;
                OnPropertyChanged();
            }
        }
    }

    public string SunriseTime
    {
        get => _sunriseTime;
        private set
        {
            if (_sunriseTime != value)
            {
                _sunriseTime = value;
                OnPropertyChanged();
            }
        }
    }

    public string SunsetLabel
    {
        get => _sunsetLabel;
        private set
        {
            if (_sunsetLabel != value)
            {
                _sunsetLabel = value;
                OnPropertyChanged();
            }
        }
    }

    public string SunsetTime
    {
        get => _sunsetTime;
        private set
        {
            if (_sunsetTime != value)
            {
                _sunsetTime = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _ = UpdateTimesAsync();
    }

    private void UpdateTimes()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] UpdateTimes - current time: {now}, latitude: {_settings.Latitude}, longitude: {_settings.Longitude}, timeZoneId: {_settings.TimeZoneId}");
            var times = CalculateSunriseSunset(now);
            SunriseTime = times.Sunrise.ToString(@"hh\:mm");
            SunsetTime = times.Sunset.ToString(@"hh\:mm");
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] UpdateTimes - result: sunrise={SunriseTime}, sunset={SunsetTime}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] UpdateTimes error: {ex.Message}\n{ex.StackTrace}");
            SunriseTime = "--:--";
            SunsetTime = "--:--";
        }
    }

    private async System.Threading.Tasks.Task UpdateTimesAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] UpdateTimesAsync - current time: {now}, latitude: {_settings.Latitude}, longitude: {_settings.Longitude}, timeZoneId: {_settings.TimeZoneId}");
            var times = CalculateSunriseSunset(now);
            var sunriseStr = times.Sunrise.ToString(@"hh\:mm");
            var sunsetStr = times.Sunset.ToString(@"hh\:mm");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SunriseTime = sunriseStr;
                SunsetTime = sunsetStr;
            });
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] UpdateTimesAsync - result: sunrise={SunriseTime}, sunset={SunsetTime}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] UpdateTimesAsync error: {ex.Message}\n{ex.StackTrace}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SunriseTime = "--:--";
                SunsetTime = "--:--";
            });
        }
    }

    private (TimeSpan Sunrise, TimeSpan Sunset) CalculateSunriseSunset(DateTime dateTime)
    {
        var timeZoneId = !string.IsNullOrEmpty(_settings.TimeZoneId)
            ? _settings.TimeZoneId
            : Plugin.Instance?.Settings?.TimeZoneId ?? "China Standard Time";

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] TimeZoneInfo.FindSystemTimeZoneById failed: {ex.Message}, falling back to local time zone");
            timeZone = TimeZoneInfo.Local;
        }

        try
        {
            return SunriseSunsetCalculator.Calculate(dateTime, _settings.Latitude, _settings.Longitude, timeZone);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SunriseSunsetViewModel] SunriseSunsetCalculator.Calculate failed: {ex.Message}");
            throw;
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
