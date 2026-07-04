using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class NextXingZuoCountdownViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly NextXingZuoCountdownSettings _settings;
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

    public NextXingZuoCountdownViewModel(TimeBaseService timeBaseService, NextXingZuoCountdownSettings settings,
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
        if (e.PropertyName == nameof(NextXingZuoCountdownSettings.Text1FontColor)) _updateText1FontColor?.Invoke(_settings.Text1FontColor);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.Text1FontSize)) _updateText1FontSize?.Invoke(_settings.Text1FontSize);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.NameFontColor)) _updateNameFontColor?.Invoke(_settings.NameFontColor);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.NameFontSize)) _updateNameFontSize?.Invoke(_settings.NameFontSize);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.Text3FontColor)) _updateText3FontColor?.Invoke(_settings.Text3FontColor);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.Text3FontSize)) _updateText3FontSize?.Invoke(_settings.Text3FontSize);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.TimeFontColor)) _updateTimeFontColor?.Invoke(_settings.TimeFontColor);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.TimeFontSize)) _updateTimeFontSize?.Invoke(_settings.TimeFontSize);
        else if (e.PropertyName == nameof(NextXingZuoCountdownSettings.TimeFormat) || e.PropertyName == nameof(NextXingZuoCountdownSettings.Text1) || e.PropertyName == nameof(NextXingZuoCountdownSettings.Text3))
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
            var nextXingZuo = GetNextXingZuo(now);
            var targetTime = new DateTime(nextXingZuo.Year, nextXingZuo.Month, nextXingZuo.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            Text1Display = _settings.Text1;
            NameDisplay = $"{nextXingZuo.Name} ({nextXingZuo.DateRange})";
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
            var nextXingZuo = GetNextXingZuo(now);
            var targetTime = new DateTime(nextXingZuo.Year, nextXingZuo.Month, nextXingZuo.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Text1Display = _settings.Text1;
                NameDisplay = $"{nextXingZuo.Name} ({nextXingZuo.DateRange})";
                Text3Display = _settings.Text3;
                TimeDisplay = FormatTime(timeLeft);
            });
        }
        catch { }
    }

    private (string Name, string DateRange, int Year, int Month, int Day) GetNextXingZuo(DateTime date)
    {
        var xingZuoDates = new[]
        {
            ("白羊座", "3.21-4.19", 3, 21),
            ("金牛座", "4.20-5.20", 4, 20),
            ("双子座", "5.21-6.21", 5, 21),
            ("巨蟹座", "6.22-7.22", 6, 22),
            ("狮子座", "7.23-8.22", 7, 23),
            ("处女座", "8.23-9.22", 8, 23),
            ("天秤座", "9.23-10.23", 9, 23),
            ("天蝎座", "10.24-11.22", 10, 24),
            ("射手座", "11.23-12.21", 11, 23),
            ("摩羯座", "12.22-1.19", 12, 22),
            ("水瓶座", "1.20-2.18", 1, 20),
            ("双鱼座", "2.19-3.20", 2, 19)
        };

        for (int i = 0; i < xingZuoDates.Length; i++)
        {
            var (name, range, month, day) = xingZuoDates[i];
            int year = date.Year;
            if (month < date.Month || (month == date.Month && day <= date.Day))
                year = date.Year + 1;
            if (month == 1 && date.Month == 12)
                year = date.Year;

            var targetDate = new DateTime(year, month, day);
            if (targetDate > date)
                return (name, range, year, month, day);
        }
        return ("白羊座", "3.21-4.19", date.Year + 1, 3, 21);
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
