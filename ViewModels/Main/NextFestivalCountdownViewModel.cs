using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class NextFestivalCountdownViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly NextFestivalCountdownSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateText1FontColor;
    private readonly Action<double> _updateText1FontSize;
    private readonly Action<string> _updateNameFontColor;
    private readonly Action<double> _updateNameFontSize;
    private readonly Action<string> _updateText3FontColor;
    private readonly Action<double> _updateText3FontSize;
    private readonly Action<string> _updateTimeFontColor;
    private readonly Action<double> _updateTimeFontSize;
    private string _text1Display = string.Empty;
    private string _nameDisplay = string.Empty;
    private string _text3Display = string.Empty;
    private string _timeDisplay = string.Empty;
    private bool _isDisposed;
    private bool _enableExperimentalFeatures;

    public string Text1Display { get => _text1Display; private set { if (_text1Display != value) { _text1Display = value; OnPropertyChanged(); } } }
    public string NameDisplay { get => _nameDisplay; private set { if (_nameDisplay != value) { _nameDisplay = value; OnPropertyChanged(); } } }
    public string Text3Display { get => _text3Display; private set { if (_text3Display != value) { _text3Display = value; OnPropertyChanged(); } } }
    public string TimeDisplay { get => _timeDisplay; private set { if (_timeDisplay != value) { _timeDisplay = value; OnPropertyChanged(); } } }

    public NextFestivalCountdownViewModel(TimeBaseService timeBaseService, NextFestivalCountdownSettings settings,
        Action<string> updateText1FontColor = null, Action<double> updateText1FontSize = null,
        Action<string> updateNameFontColor = null, Action<double> updateNameFontSize = null,
        Action<string> updateText3FontColor = null, Action<double> updateText3FontSize = null,
        Action<string> updateTimeFontColor = null, Action<double> updateTimeFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateText1FontColor = updateText1FontColor;
        _updateText1FontSize = updateText1FontSize;
        _updateNameFontColor = updateNameFontColor;
        _updateNameFontSize = updateNameFontSize;
        _updateText3FontColor = updateText3FontColor;
        _updateText3FontSize = updateText3FontSize;
        _updateTimeFontColor = updateTimeFontColor;
        _updateTimeFontSize = updateTimeFontSize;
        
        _settings.PropertyChanged += OnSettingsChanged;

        _enableExperimentalFeatures = Plugin.Instance?.Settings?.EnableExperimentalFeatures ?? false;
        if (Plugin.Instance?.Settings != null)
            Plugin.Instance.Settings.PropertyChanged += OnPluginSettingsPropertyChanged;

        UpdateDisplay();
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnPluginSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.EnableExperimentalFeatures))
        {
            _enableExperimentalFeatures = Plugin.Instance?.Settings?.EnableExperimentalFeatures ?? false;
            UpdateDisplay();
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text1FontColor) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.Text1EnableCustomFontColor))
        {
            _updateText1FontColor?.Invoke(_settings.Text1FontColor);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text1FontSize) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.Text1EnableCustomFontSize))
        {
            _updateText1FontSize?.Invoke(_settings.Text1EnableCustomFontSize ? _settings.Text1FontSize : 0);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.NameFontColor) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.NameEnableCustomFontColor))
        {
            _updateNameFontColor?.Invoke(_settings.NameFontColor);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.NameFontSize) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.NameEnableCustomFontSize))
        {
            _updateNameFontSize?.Invoke(_settings.NameEnableCustomFontSize ? _settings.NameFontSize : 0);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text3FontColor) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.Text3EnableCustomFontColor))
        {
            _updateText3FontColor?.Invoke(_settings.Text3FontColor);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text3FontSize) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.Text3EnableCustomFontSize))
        {
            _updateText3FontSize?.Invoke(_settings.Text3EnableCustomFontSize ? _settings.Text3FontSize : 0);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.TimeFontColor) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.TimeEnableCustomFontColor))
        {
            _updateTimeFontColor?.Invoke(_settings.TimeFontColor);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.TimeFontSize) ||
            e.PropertyName == nameof(NextFestivalCountdownSettings.TimeEnableCustomFontSize))
        {
            _updateTimeFontSize?.Invoke(_settings.TimeEnableCustomFontSize ? _settings.TimeFontSize : 0);
        }
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.TimeFormat) || e.PropertyName == nameof(NextFestivalCountdownSettings.Text1) || e.PropertyName == nameof(NextFestivalCountdownSettings.Text3) ||
                 e.PropertyName == nameof(NextFestivalCountdownSettings.EnableSimpleMode) ||
                 e.PropertyName == nameof(NextFestivalCountdownSettings.EnableTimeCorrection) ||
                 e.PropertyName == nameof(NextFestivalCountdownSettings.EnableInternationalFestivals) || e.PropertyName == nameof(NextFestivalCountdownSettings.EnableChineseTraditionalFestivals) || e.PropertyName == nameof(NextFestivalCountdownSettings.EnableRedFestivals))
        {
            UpdateDisplay();
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => _ = UpdateDisplayAsync();

    private void UpdateDisplay()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            var nextFestival = GetNextFestival(now);
            var targetTime = new DateTime(nextFestival.Year, nextFestival.Month, nextFestival.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            Text1Display = _settings.EnableSimpleMode ? string.Empty : _settings.Text1;
            NameDisplay = nextFestival.Name;
            Text3Display = _settings.EnableSimpleMode ? string.Empty : _settings.Text3;
            TimeDisplay = FormatTime(timeLeft);
        }
        catch { }
    }

    private async System.Threading.Tasks.Task UpdateDisplayAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            var nextFestival = GetNextFestival(now);
            var targetTime = new DateTime(nextFestival.Year, nextFestival.Month, nextFestival.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Text1Display = _settings.EnableSimpleMode ? string.Empty : _settings.Text1;
                NameDisplay = nextFestival.Name;
                Text3Display = _settings.EnableSimpleMode ? string.Empty : _settings.Text3;
                TimeDisplay = FormatTime(timeLeft);
            });
        }
        catch { }
    }

    private (string Name, int Year, int Month, int Day) GetNextFestival(DateTime date)
    {
        var festivals = new List<(string Name, DateTime Date)>();

        if (_settings.EnableInternationalFestivals)
        {
            FestivalCatalog.AddInternationalFestivals(festivals, date);
        }

        if (_settings.EnableChineseTraditionalFestivals)
        {
            FestivalCatalog.AddChineseTraditionalFestivals(festivals, date, _enableExperimentalFeatures);
        }

        if (_settings.EnableRedFestivals)
        {
            FestivalCatalog.AddRedFestivals(festivals, date);
        }

        var nextFestival = festivals.Where(f => f.Date > date).OrderBy(f => f.Date).FirstOrDefault();
        if (nextFestival.Date == default)
        {
            if (_settings.EnableInternationalFestivals)
            {
                FestivalCatalog.AddInternationalFestivals(festivals, date.AddYears(1));
            }

            if (_settings.EnableChineseTraditionalFestivals)
            {
                FestivalCatalog.AddChineseTraditionalFestivals(festivals, date.AddYears(1), _enableExperimentalFeatures);
            }

            if (_settings.EnableRedFestivals)
            {
                FestivalCatalog.AddRedFestivals(festivals, date.AddYears(1));
            }

            nextFestival = festivals.Where(f => f.Date > date).OrderBy(f => f.Date).FirstOrDefault();
        }

        return (nextFestival.Name, nextFestival.Date.Year, nextFestival.Date.Month, nextFestival.Date.Day);
    }

    private string FormatTime(TimeSpan timeLeft)
    {
        if (timeLeft.TotalSeconds < 0) return "0天";
        var totalSeconds = (long)timeLeft.TotalSeconds;
        var totalMilliseconds = timeLeft.TotalMilliseconds;
        var days = (int)(totalSeconds / 86400);
        var remainingSeconds = totalSeconds % 86400;
        var hours = (int)(remainingSeconds / 3600);
        remainingSeconds %= 3600;
        var minutes = (int)(remainingSeconds / 60);
        var seconds = (int)(remainingSeconds % 60);
        var milliseconds = (int)(totalMilliseconds % 1000);
        var format = string.IsNullOrEmpty(_settings.TimeFormat) ? "%d天" : _settings.TimeFormat;

        // 差一矫正：精度不足时将最小显示单位加一（与多倒计时组件一致）
        if (_settings.EnableTimeCorrection && totalSeconds > 0 && !format.Contains("%x") && !format.Contains("%X"))
        {
            if (format.Contains("%s") || format.Contains("%S"))
            {
                seconds++;
                if (seconds >= 60)
                {
                    seconds = 0;
                    minutes++;
                    if (minutes >= 60)
                    {
                        minutes = 0;
                        hours++;
                        if (hours >= 24)
                        {
                            hours = 0;
                            days++;
                        }
                    }
                }
            }
            else if (format.Contains("%m") || format.Contains("%M"))
            {
                minutes++;
                if (minutes >= 60)
                {
                    minutes = 0;
                    hours++;
                    if (hours >= 24)
                    {
                        hours = 0;
                        days++;
                    }
                }
            }
            else if (format.Contains("%h") || format.Contains("%H"))
            {
                hours++;
                if (hours >= 24)
                {
                    hours = 0;
                    days++;
                }
            }
            else if (format.Contains("%d"))
            {
                days++;
            }
        }

        return format
            .Replace("%d", days.ToString())
            .Replace("%h", hours.ToString())
            .Replace("%m", minutes.ToString("D2"))
            .Replace("%s", seconds.ToString("D2"))
            .Replace("%x", milliseconds.ToString("D3"))
            .Replace("%H", ((int)timeLeft.TotalHours).ToString())
            .Replace("%M", ((int)timeLeft.TotalMinutes).ToString())
            .Replace("%S", totalSeconds.ToString())
            .Replace("%X", ((int)totalMilliseconds).ToString());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _settings.PropertyChanged -= OnSettingsChanged;
        if (Plugin.Instance?.Settings != null)
            Plugin.Instance.Settings.PropertyChanged -= OnPluginSettingsPropertyChanged;
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
