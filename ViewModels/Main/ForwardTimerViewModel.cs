using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class ForwardTimerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly ForwardTimerSettings _settings;
    private DispatcherTimer? _updateTimer;
    private DispatcherTimer? _highFrequencyTimer;
    private readonly Action<string, double> _updateText1Style;
    private readonly Action<string, double> _updateNameStyle;
    private readonly Action<string, double> _updateText3Style;
    private readonly Action<string, double> _updateTimeStyle;
    private readonly Action<string, double> _updateText4Style;
    private bool _isDisposed;
    private bool _requiresHighFrequencyRefresh;
    private readonly TimeSpan _normalInterval = TimeSpan.FromMilliseconds(200);
    private readonly TimeSpan _highFrequencyInterval = TimeSpan.FromMilliseconds(16.67);

    private string _text1Display = string.Empty;
    private string _nameDisplay = string.Empty;
    private string _text3Display = string.Empty;
    private string _timeDisplay = string.Empty;
    private string _text4Display = string.Empty;
    private bool _isNotStarted;

    public bool IsNotStarted
    {
        get => _isNotStarted;
        private set
        {
            if (_isNotStarted != value)
            {
                _isNotStarted = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text1Display
    {
        get => _text1Display;
        private set
        {
            if (_text1Display != value)
            {
                _text1Display = value;
                OnPropertyChanged();
            }
        }
    }

    public string NameDisplay
    {
        get => _nameDisplay;
        private set
        {
            if (_nameDisplay != value)
            {
                _nameDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text3Display
    {
        get => _text3Display;
        private set
        {
            if (_text3Display != value)
            {
                _text3Display = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeDisplay
    {
        get => _timeDisplay;
        private set
        {
            if (_timeDisplay != value)
            {
                _timeDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text4Display
    {
        get => _text4Display;
        private set
        {
            if (_text4Display != value)
            {
                _text4Display = value;
                OnPropertyChanged();
            }
        }
    }

    public ForwardTimerViewModel(TimeBaseService timeBaseService, ForwardTimerSettings settings,
        Action<string, double> updateText1Style = null,
        Action<string, double> updateNameStyle = null,
        Action<string, double> updateText3Style = null,
        Action<string, double> updateTimeStyle = null,
        Action<string, double> updateText4Style = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateText1Style = updateText1Style;
        _updateNameStyle = updateNameStyle;
        _updateText3Style = updateText3Style;
        _updateTimeStyle = updateTimeStyle;
        _updateText4Style = updateText4Style;

        _settings.PropertyChanged += OnSettingsChanged;

        UpdateTimer();
        _requiresHighFrequencyRefresh = RequiresHighFrequencyRefresh(_settings.TimeFormat);
        InitializeTimers();
    }

    private void InitializeTimers()
    {
        _updateTimer = new DispatcherTimer
        {
            Interval = _normalInterval
        };
        _updateTimer.Tick += OnTimerElapsed;
        _updateTimer.Start();

        _highFrequencyTimer = new DispatcherTimer
        {
            Interval = _highFrequencyInterval
        };
        _highFrequencyTimer.Tick += OnHighFrequencyTimerElapsed;

        if (_requiresHighFrequencyRefresh)
        {
            _updateTimer.Stop();
            _highFrequencyTimer.Start();
        }
    }

    private static bool RequiresHighFrequencyRefresh(string? timeFormat)
    {
        if (string.IsNullOrEmpty(timeFormat))
            return false;

        return timeFormat.Contains("%x") || 
               timeFormat.Contains("%X");
    }

    public void UpdateRefreshMode()
    {
        var newRequiresHighFrequency = RequiresHighFrequencyRefresh(_settings.TimeFormat);
        if (_requiresHighFrequencyRefresh != newRequiresHighFrequency)
        {
            _requiresHighFrequencyRefresh = newRequiresHighFrequency;

            if (_requiresHighFrequencyRefresh)
            {
                _updateTimer?.Stop();
                _highFrequencyTimer?.Start();
            }
            else
            {
                _highFrequencyTimer?.Stop();
                _updateTimer?.Start();
            }
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ForwardTimerSettings.Text1):
            case nameof(ForwardTimerSettings.Name):
            case nameof(ForwardTimerSettings.Text3):
            case nameof(ForwardTimerSettings.Text4):
            case nameof(ForwardTimerSettings.TimeBaseType):
            case nameof(ForwardTimerSettings.StartTime):
                UpdateTimer();
                break;
            case nameof(ForwardTimerSettings.TimeFormat):
                UpdateTimer();
                UpdateRefreshMode();
                break;
            case nameof(ForwardTimerSettings.Text1FontSize):
            case nameof(ForwardTimerSettings.Text1FontColor):
                _updateText1Style?.Invoke(_settings.Text1FontColor, _settings.Text1FontSize);
                break;
            case nameof(ForwardTimerSettings.NameFontSize):
            case nameof(ForwardTimerSettings.NameFontColor):
                _updateNameStyle?.Invoke(_settings.NameFontColor, _settings.NameFontSize);
                break;
            case nameof(ForwardTimerSettings.Text3FontSize):
            case nameof(ForwardTimerSettings.Text3FontColor):
                _updateText3Style?.Invoke(_settings.Text3FontColor, _settings.Text3FontSize);
                break;
            case nameof(ForwardTimerSettings.TimeFontSize):
            case nameof(ForwardTimerSettings.TimeFontColor):
                _updateTimeStyle?.Invoke(_settings.TimeFontColor, _settings.TimeFontSize);
                break;
            case nameof(ForwardTimerSettings.Text4FontSize):
            case nameof(ForwardTimerSettings.Text4FontColor):
                _updateText4Style?.Invoke(_settings.Text4FontColor, _settings.Text4FontSize);
                break;
        }
    }

    private void OnTimerElapsed(object? sender, EventArgs e)
    {
        _ = UpdateTimerAsync();
    }

    private void OnHighFrequencyTimerElapsed(object? sender, EventArgs e)
    {
        _ = UpdateTimerAsync();
    }

    private void UpdateTimer()
    {
        try
        {
            var now = GetCurrentTime();
            ProcessTimer(now);
        }
        catch
        {
        }
    }

    private async System.Threading.Tasks.Task UpdateTimerAsync()
    {
        try
        {
            var now = await GetCurrentTimeAsync().ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProcessTimer(now);
            });
        }
        catch
        {
        }
    }

    private DateTime GetCurrentTime()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => _timeBaseService.GetCurrentTime(),
            TimeBaseType.PluginOffsetSystemTime => _timeBaseService.GetPluginOffsetSystemTime(),
            TimeBaseType.RawServerTime => _timeBaseService.GetRawServerTime(),
            TimeBaseType.RawSystemTime => DateTime.Now,
            _ => _timeBaseService.GetCurrentTime()
        };
    }

    private async System.Threading.Tasks.Task<DateTime> GetCurrentTimeAsync()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false),
            TimeBaseType.PluginOffsetSystemTime => await _timeBaseService.GetPluginOffsetSystemTimeAsync().ConfigureAwait(false),
            TimeBaseType.RawServerTime => await _timeBaseService.GetRawServerTimeAsync().ConfigureAwait(false),
            TimeBaseType.RawSystemTime => await System.Threading.Tasks.Task.FromResult(DateTime.Now).ConfigureAwait(false),
            _ => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false)
        };
    }

    private void ProcessTimer(DateTime now)
    {
        var startTimeDate = LunarHelper.UnixTimestampToDateTime(_settings.StartTime);
        var startTimeJd = LunarHelper.UnixTimestampToJulianDay(_settings.StartTime);
        var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var nowJd = LunarHelper.UnixTimestampToJulianDay(nowTimestamp);
        var elapsedSeconds = (nowJd - startTimeJd) * 86400;
        
        if (elapsedSeconds < 0)
        {
            IsNotStarted = true;
            Text1Display = "";
            NameDisplay = "";
            Text3Display = "正向计时器未开始";
            TimeDisplay = "";
            Text4Display = "";
            return;
        }

        IsNotStarted = false;

        var elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _settings.StartTime * 1000;

        var timeFormat = string.IsNullOrEmpty(_settings.TimeFormat) ? "%d天%h小时%m分钟%s秒" : _settings.TimeFormat;
        var timeText = FormatTime(timeFormat, (long)Math.Floor(elapsedSeconds), elapsedMs, startTimeDate, now);

        Text1Display = _settings.Text1;
        NameDisplay = _settings.Name;
        Text3Display = _settings.Text3;
        TimeDisplay = timeText;
        Text4Display = _settings.Text4;
    }

    private string FormatTime(string format, long secondsElapsed, double millisecondsElapsed, DateTime startTimeDate, DateTime now)
    {
        var totalSeconds = secondsElapsed;
        var totalMilliseconds = millisecondsElapsed;
        var totalMinutes = Math.Ceiling(totalSeconds / 60.0);
        var totalHours = Math.Ceiling(totalSeconds / 3600.0);
        var totalDays = Math.Ceiling(totalSeconds / 86400.0);

        var days = (int)(totalSeconds / 86400);
        var remainingSeconds = totalSeconds % 86400;
        var hours = (int)(remainingSeconds / 3600);
        remainingSeconds %= 3600;
        var minutes = (int)(remainingSeconds / 60);
        var seconds = (int)(remainingSeconds % 60);
        var milliseconds = (int)(totalMilliseconds % 1000);

        var totalDuration = totalSeconds;

        string remainingPercent = "0";
        string elapsedPercent = "0";
        string elapsedPercentDecimal = "0.00";

        if (totalDuration > 0)
        {
            elapsedPercent = "100";
            elapsedPercentDecimal = "100.00";
            remainingPercent = "0";
        }

        bool hasMonth = format.Contains("%mo") || format.Contains("%MO");
        bool hasYear = format.Contains("%yy") || format.Contains("%YY");

        int displayYears = 0;
        int displayMonths = 0;
        int displayDays = days;

        if (hasYear || hasMonth)
        {
            var tempDate = startTimeDate;
            displayYears = 0;

            while (LunarHelper.SolarAddYears(tempDate, 1) <= now)
            {
                tempDate = LunarHelper.SolarAddYears(tempDate, 1);
                displayYears++;
            }

            if (hasMonth)
            {
                displayMonths = 0;
                while (LunarHelper.SolarAddMonths(tempDate, 1) <= now)
                {
                    tempDate = LunarHelper.SolarAddMonths(tempDate, 1);
                    displayMonths++;
                }

                var dayDiff = (int)Math.Floor(LunarHelper.DaysBetween(tempDate, now));
                displayDays = Math.Max(0, dayDiff);
            }
            else
            {
                var dayDiff = (int)Math.Floor(LunarHelper.DaysBetween(tempDate, now));
                displayDays = Math.Max(0, dayDiff);
            }
        }

        var yy = displayYears.ToString();
        var mo = ((int)(totalSeconds / (30.4375 * 86400.0))).ToString();
        var YY = (totalSeconds / (365.25 * 86400.0)).ToString("F2");
        var MO = (totalSeconds / (30.4375 * 86400.0)).ToString("F2");

        var result = format
            .Replace("%D", ((int)totalDays).ToString())
            .Replace("%H", ((int)totalHours).ToString())
            .Replace("%M", ((int)totalMinutes).ToString())
            .Replace("%S", totalSeconds.ToString())
            .Replace("%X", ((int)totalMilliseconds).ToString())
            .Replace("%L", remainingPercent)
            .Replace("%P", elapsedPercent)
            .Replace("%p", elapsedPercentDecimal)
            .Replace("%yy", yy)
            .Replace("%YY", YY)
            .Replace("%mo", displayMonths.ToString())
            .Replace("%MO", displayMonths.ToString())
            .Replace("%d", displayDays.ToString())
            .Replace("%h", hours.ToString())
            .Replace("%m", minutes.ToString("D2"))
            .Replace("%s", seconds.ToString("D2"))
            .Replace("%x", milliseconds.ToString("D3"));

        return result;
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
        _highFrequencyTimer?.Stop();
    }
}


