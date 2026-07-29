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
    private IDisposable? _subscription;
    private readonly Action<string, double> _updateText1Style;
    private readonly Action<string, double> _updateNameStyle;
    private readonly Action<string, double> _updateText3Style;
    private readonly Action<string, double> _updateTimeStyle;
    private readonly Action<string, double> _updateText4Style;
    private bool _isDisposed;
    private bool _requiresHighFrequencyRefresh;

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
        SubscribeToClock();
    }

    private void SubscribeToClock()
    {
        _subscription?.Dispose();
        _subscription = SharedRenderClockService.Instance.Subscribe(OnClockTick, _requiresHighFrequencyRefresh);
        SharedRenderClockService.Instance.EnsureStarted();
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
            SubscribeToClock();
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForwardTimerSettings.Text1) ||
            e.PropertyName == nameof(ForwardTimerSettings.Name) ||
            e.PropertyName == nameof(ForwardTimerSettings.Text3) ||
            e.PropertyName == nameof(ForwardTimerSettings.Text4) ||
            e.PropertyName == nameof(ForwardTimerSettings.TimeBaseType) ||
            e.PropertyName == nameof(ForwardTimerSettings.StartTime))
        {
            UpdateTimer();
        }
        if (e.PropertyName == nameof(ForwardTimerSettings.TimeFormat))
        {
            UpdateTimer();
            UpdateRefreshMode();
        }
        if (e.PropertyName == nameof(ForwardTimerSettings.Text1FontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text1FontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text1EnableCustomFontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text1EnableCustomFontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text1FontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text1EnableCustomFontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text1FontWeight) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text1EnableCustomFontWeight))
        {
            _updateText1Style?.Invoke(_settings.Text1FontColor, _settings.Text1EnableCustomFontSize ? _settings.Text1FontSize : 0);
        }
        if (e.PropertyName == nameof(ForwardTimerSettings.NameFontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.NameFontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.NameEnableCustomFontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.NameEnableCustomFontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.NameFontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.NameEnableCustomFontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.NameFontWeight) ||
                 e.PropertyName == nameof(ForwardTimerSettings.NameEnableCustomFontWeight))
        {
            _updateNameStyle?.Invoke(_settings.NameFontColor, _settings.NameEnableCustomFontSize ? _settings.NameFontSize : 0);
        }
        if (e.PropertyName == nameof(ForwardTimerSettings.Text3FontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text3FontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text3EnableCustomFontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text3EnableCustomFontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text3FontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text3EnableCustomFontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text3FontWeight) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text3EnableCustomFontWeight))
        {
            _updateText3Style?.Invoke(_settings.Text3FontColor, _settings.Text3EnableCustomFontSize ? _settings.Text3FontSize : 0);
        }
        if (e.PropertyName == nameof(ForwardTimerSettings.TimeFontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.TimeFontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.TimeEnableCustomFontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.TimeEnableCustomFontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.TimeFontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.TimeEnableCustomFontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.TimeFontWeight) ||
                 e.PropertyName == nameof(ForwardTimerSettings.TimeEnableCustomFontWeight))
        {
            _updateTimeStyle?.Invoke(_settings.TimeFontColor, _settings.TimeEnableCustomFontSize ? _settings.TimeFontSize : 0);
        }
        if (e.PropertyName == nameof(ForwardTimerSettings.Text4FontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text4FontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text4EnableCustomFontSize) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text4EnableCustomFontColor) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text4FontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text4EnableCustomFontFamily) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text4FontWeight) ||
                 e.PropertyName == nameof(ForwardTimerSettings.Text4EnableCustomFontWeight))
        {
            _updateText4Style?.Invoke(_settings.Text4FontColor, _settings.Text4EnableCustomFontSize ? _settings.Text4FontSize : 0);
        }
    }

    private void OnClockTick(DateTime now)
    {
        _ = UpdateTimerAsync(now);
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

    private async System.Threading.Tasks.Task UpdateTimerAsync(DateTime? clockTime = null)
    {
        try
        {
            var now = clockTime ?? await GetCurrentTimeAsync().ConfigureAwait(false);

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
        return OptimizedTimeFormatter.FormatForwardTime(format, secondsElapsed, millisecondsElapsed, startTimeDate, now);
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
        _subscription?.Dispose();
    }
}


