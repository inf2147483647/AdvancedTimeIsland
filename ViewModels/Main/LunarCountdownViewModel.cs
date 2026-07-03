using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Services.NotificationProviders;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class LunarCountdownViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly LunarCountdownSettings _settings;
    private DispatcherTimer? _updateTimer;
    private DispatcherTimer? _highFrequencyTimer;
    private readonly Action<string, double> _updateText1Style;
    private readonly Action<string, double> _updateNameStyle;
    private readonly Action<string, double> _updateText3Style;
    private readonly Action<string, double> _updateTimeStyle;
    private readonly Action<string, double> _updateText4Style;
    private bool _isDisposed;
    private bool _isFirstUpdate = true;
    private bool _requiresHighFrequencyRefresh;
    private readonly TimeSpan _normalInterval = TimeSpan.FromMilliseconds(200);
    private readonly TimeSpan _highFrequencyInterval = TimeSpan.FromMilliseconds(16.67);

    private string _text1Display = string.Empty;
    private string _nameDisplay = string.Empty;
    private string _text3Display = string.Empty;
    private string _timeDisplay = string.Empty;
    private string _text4Display = string.Empty;
    private LunarCountdownItem? _currentItem;
    private bool _isAllCompleted;
    private bool _isEmpty;

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

    public LunarCountdownItem? CurrentItem
    {
        get => _currentItem;
        private set
        {
            if (_currentItem != value)
            {
                _currentItem = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsAllCompleted
    {
        get => _isAllCompleted;
        private set
        {
            if (_isAllCompleted != value)
            {
                _isAllCompleted = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set
        {
            if (_isEmpty != value)
            {
                _isEmpty = value;
                OnPropertyChanged();
            }
        }
    }

    public LunarCountdownViewModel(TimeBaseService timeBaseService, LunarCountdownSettings settings,
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

        EnsureDefaultItems();
        UpdateCountdown();
        _isFirstUpdate = false;

        _requiresHighFrequencyRefresh = RequiresHighFrequencyRefresh(_settings.TimeFormat);
        InitializeTimers();
    }

    private void InitializeTimers()
    {
        _updateTimer = new DispatcherTimer { Interval = _normalInterval };
        _updateTimer.Tick += OnTimerElapsed;
        _updateTimer.Start();

        _highFrequencyTimer = new DispatcherTimer { Interval = _highFrequencyInterval };
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
        return timeFormat.Contains("%x") || timeFormat.Contains("%X");
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

    private void EnsureDefaultItems()
    {
        if (_settings.CountdownItems == null)
        {
            _settings.CountdownItems = new List<LunarCountdownItem>();
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LunarCountdownSettings.Text1):
            case nameof(LunarCountdownSettings.Text3):
            case nameof(LunarCountdownSettings.Text4):
            case nameof(LunarCountdownSettings.CountdownItems):
            case nameof(LunarCountdownSettings.TimeBaseType):
                UpdateCountdown();
                break;
            case nameof(LunarCountdownSettings.TimeFormat):
                UpdateCountdown();
                UpdateRefreshMode();
                break;
            case nameof(LunarCountdownSettings.Text1FontSize):
            case nameof(LunarCountdownSettings.Text1FontColor):
                _updateText1Style?.Invoke(_settings.Text1FontColor, _settings.Text1FontSize);
                break;
            case nameof(LunarCountdownSettings.NameFontSize):
            case nameof(LunarCountdownSettings.NameFontColor):
                _updateNameStyle?.Invoke(_settings.NameFontColor, _settings.NameFontSize);
                break;
            case nameof(LunarCountdownSettings.Text3FontSize):
            case nameof(LunarCountdownSettings.Text3FontColor):
                _updateText3Style?.Invoke(_settings.Text3FontColor, _settings.Text3FontSize);
                break;
            case nameof(LunarCountdownSettings.TimeFontSize):
            case nameof(LunarCountdownSettings.TimeFontColor):
                _updateTimeStyle?.Invoke(_settings.TimeFontColor, _settings.TimeFontSize);
                break;
            case nameof(LunarCountdownSettings.Text4FontSize):
            case nameof(LunarCountdownSettings.Text4FontColor):
                _updateText4Style?.Invoke(_settings.Text4FontColor, _settings.Text4FontSize);
                break;
        }
    }

    private void OnTimerElapsed(object? sender, EventArgs e)
    {
        _ = UpdateCountdownAsync();
    }

    private void OnHighFrequencyTimerElapsed(object? sender, EventArgs e)
    {
        _ = UpdateCountdownAsync();
    }

    private void UpdateCountdown()
    {
        try
        {
            var now = GetCurrentTime();
            ProcessCountdown(now);
        }
        catch
        {
        }
    }

    private async System.Threading.Tasks.Task UpdateCountdownAsync()
    {
        try
        {
            var now = await GetCurrentTimeAsync().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var displayData = ProcessCountdownInternal(now);
                Text1Display = displayData.Text1;
                NameDisplay = displayData.Name;
                Text3Display = displayData.Text3;
                TimeDisplay = displayData.Time;
                Text4Display = displayData.Text4;
                IsAllCompleted = displayData.IsAllCompleted;
                IsEmpty = displayData.IsEmpty;
                CurrentItem = displayData.CurrentItem;
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

    private void ProcessCountdown(DateTime now)
    {
        var displayData = ProcessCountdownInternal(now);
        Text1Display = displayData.Text1;
        NameDisplay = displayData.Name;
        Text3Display = displayData.Text3;
        TimeDisplay = displayData.Time;
        Text4Display = displayData.Text4;
        IsAllCompleted = displayData.IsAllCompleted;
        CurrentItem = displayData.CurrentItem;
    }

    private LunarCountdownDisplayData ProcessCountdownInternal(DateTime now)
    {
        if (_settings.CountdownItems == null || _settings.CountdownItems.Count == 0)
        {
            return new LunarCountdownDisplayData
            {
                Text1 = _settings.Text1,
                Name = string.Empty,
                Text3 = "当前无农历倒计时",
                Time = string.Empty,
                Text4 = string.Empty,
                IsAllCompleted = false,
                IsEmpty = true,
                CurrentItem = null
            };
        }

        var unixNow = UnixTimeHelper.ToUnixTimestampDouble(now);

        var expiredItems = _settings.CountdownItems
            .Where(item => !item.IsCompleted && item.GetTargetTimestamp() <= (long)unixNow)
            .OrderBy(item => item.GetTargetTimestamp())
            .ToList();

        foreach (var item in expiredItems)
        {
            HandleItemCompleted(item);
        }

        var activeItems = _settings.CountdownItems
            .Where(item => !item.IsCompleted && item.GetTargetTimestamp() > (long)unixNow)
            .ToList();

        if (activeItems.Count == 0)
        {
            return new LunarCountdownDisplayData
            {
                Text1 = _settings.Text1,
                Name = string.Empty,
                Text3 = "农历倒计时已结束",
                Time = string.Empty,
                Text4 = string.Empty,
                IsAllCompleted = true,
                IsEmpty = false,
                CurrentItem = null
            };
        }

        var sortedItems = activeItems.OrderBy(item => item.GetTargetTimestamp()).ToList();
        var currentItem = sortedItems.First();

        var timeLeft = (double)currentItem.GetTargetTimestamp() - unixNow;
        var timeLeftMs = timeLeft * 1000;

        var timeFormat = string.IsNullOrEmpty(_settings.TimeFormat) ? "%D天%h小时%m分钟%s秒" : _settings.TimeFormat;
        var timeText = FormatTime(timeFormat, (long)Math.Floor(timeLeft), timeLeftMs);

        return new LunarCountdownDisplayData
        {
            Text1 = _settings.Text1,
            Name = currentItem.Name,
            Text3 = _settings.Text3,
            Time = timeText,
            Text4 = _settings.Text4,
            IsAllCompleted = false,
            IsEmpty = false,
            CurrentItem = currentItem
        };
    }

    private void HandleItemCompleted(LunarCountdownItem item)
    {
        try
        {
            item.IsCompleted = true;
            if (item.EnableNotification && !_isFirstUpdate)
            {
                var maskText = string.IsNullOrEmpty(item.NotificationTitle) ? "农历倒计时到达" : item.NotificationTitle;
                var maskDuration = item.NotificationMaskDurationSeconds > 0 ? item.NotificationMaskDurationSeconds : 3;
                var overlayText = item.NotificationContent ?? string.Empty;
                var overlayDuration = item.NotificationOverlayDurationSeconds > 0 ? item.NotificationOverlayDurationSeconds : 10;
                CountdownNotificationProvider.Notify(maskText, maskDuration, overlayText, overlayDuration);
            }
        }
        catch
        {
        }
    }

    private string FormatTime(string format, long secondsLeft, double millisecondsLeft)
    {
        var totalSeconds = secondsLeft;
        var totalMilliseconds = millisecondsLeft;
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

        var yy = ((int)(totalSeconds / (365.25 * 86400.0))).ToString();
        var mo = ((int)(totalSeconds / (30.4375 * 86400.0))).ToString();
        var YY = (totalSeconds / (365.25 * 86400.0)).ToString("F2");
        var MO = (totalSeconds / (30.4375 * 86400.0)).ToString("F2");

        bool hasMonth = format.Contains("%mo") || format.Contains("%MO");
        bool hasYear = format.Contains("%yy") || format.Contains("%YY");

        int displayMonths;
        if (hasYear)
        {
            var totalMonthsValue = (int)(totalSeconds / (30.4375 * 86400.0));
            var yearsFromTotal = (int)(totalSeconds / (365.25 * 86400.0));
            displayMonths = totalMonthsValue - yearsFromTotal * 12;
        }
        else
        {
            displayMonths = (int)(totalSeconds / (30.4375 * 86400.0));
        }

        int displayDays;
        if (hasMonth && hasYear)
        {
            var totalDaysValue = (int)Math.Floor(totalSeconds / 86400.0);
            var yearsFromTotal = (int)(totalSeconds / (365.25 * 86400.0));
            displayDays = totalDaysValue - yearsFromTotal * 365 - displayMonths * 30;
        }
        else if (hasMonth)
        {
            var totalDaysValue = (int)Math.Floor(totalSeconds / 86400.0);
            var monthsFromTotal = (int)(totalSeconds / (30.4375 * 86400.0));
            displayDays = totalDaysValue - monthsFromTotal * 30;
        }
        else if (hasYear)
        {
            var totalDaysValue = (int)Math.Floor(totalSeconds / 86400.0);
            var yearsFromTotal = (int)(totalSeconds / (365.25 * 86400.0));
            displayDays = totalDaysValue - yearsFromTotal * 365;
        }
        else
        {
            displayDays = days;
        }

        var result = format
            .Replace("%D", ((int)totalDays).ToString())
            .Replace("%H", ((int)totalHours).ToString())
            .Replace("%M", ((int)totalMinutes).ToString())
            .Replace("%S", totalSeconds.ToString())
            .Replace("%X", ((int)totalMilliseconds).ToString())
            .Replace("%L", "0")
            .Replace("%P", "0")
            .Replace("%p", "0.00")
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

    private class LunarCountdownDisplayData
    {
        public string Text1 { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Text3 { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Text4 { get; set; } = string.Empty;
        public bool IsAllCompleted { get; set; }
        public bool IsEmpty { get; set; }
        public LunarCountdownItem? CurrentItem { get; set; }
    }
}



