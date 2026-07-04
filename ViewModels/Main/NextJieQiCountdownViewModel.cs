using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;
using Lunar;

namespace AdvancedTimeIsland.ViewModels.Main;

public class NextJieQiCountdownViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly NextJieQiCountdownSettings _settings;
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

    public string Text1Display { get => _text1Display; private set { if (_text1Display != value) { _text1Display = value; OnPropertyChanged(); } } }
    public string NameDisplay { get => _nameDisplay; private set { if (_nameDisplay != value) { _nameDisplay = value; OnPropertyChanged(); } } }
    public string Text3Display { get => _text3Display; private set { if (_text3Display != value) { _text3Display = value; OnPropertyChanged(); } } }
    public string TimeDisplay { get => _timeDisplay; private set { if (_timeDisplay != value) { _timeDisplay = value; OnPropertyChanged(); } } }

    public NextJieQiCountdownViewModel(TimeBaseService timeBaseService, NextJieQiCountdownSettings settings,
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
        UpdateDisplay();
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NextJieQiCountdownSettings.Text1FontColor)) _updateText1FontColor?.Invoke(_settings.Text1FontColor);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.Text1FontSize)) _updateText1FontSize?.Invoke(_settings.Text1FontSize);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.NameFontColor)) _updateNameFontColor?.Invoke(_settings.NameFontColor);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.NameFontSize)) _updateNameFontSize?.Invoke(_settings.NameFontSize);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.Text3FontColor)) _updateText3FontColor?.Invoke(_settings.Text3FontColor);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.Text3FontSize)) _updateText3FontSize?.Invoke(_settings.Text3FontSize);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.TimeFontColor)) _updateTimeFontColor?.Invoke(_settings.TimeFontColor);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.TimeFontSize)) _updateTimeFontSize?.Invoke(_settings.TimeFontSize);
        else if (e.PropertyName == nameof(NextJieQiCountdownSettings.TimeFormat) || e.PropertyName == nameof(NextJieQiCountdownSettings.Text1) || e.PropertyName == nameof(NextJieQiCountdownSettings.Text3))
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
            var nextJieQi = GetNextJieQi(now);
            var targetTime = new DateTime(nextJieQi.Year, nextJieQi.Month, nextJieQi.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            Text1Display = _settings.Text1;
            NameDisplay = nextJieQi.Name;
            Text3Display = _settings.Text3;
            TimeDisplay = FormatTime(timeLeft);
        }
        catch { }
    }

    private async System.Threading.Tasks.Task UpdateDisplayAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            var nextJieQi = GetNextJieQi(now);
            var targetTime = new DateTime(nextJieQi.Year, nextJieQi.Month, nextJieQi.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Text1Display = _settings.Text1;
                NameDisplay = nextJieQi.Name;
                Text3Display = _settings.Text3;
                TimeDisplay = FormatTime(timeLeft);
            });
        }
        catch { }
    }

    private (string Name, int Year, int Month, int Day) GetNextJieQi(DateTime date)
    {
        for (int i = 0; i < 366; i++)
        {
            var checkDate = date.AddDays(i);
            var solar = Solar.FromDate(checkDate);
            var jieQi = solar.Lunar.JieQi;
            if (!string.IsNullOrEmpty(jieQi))
                return (jieQi, checkDate.Year, checkDate.Month, checkDate.Day);
        }
        return ("立春", date.Year + 1, 2, 4);
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
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
