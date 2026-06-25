using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Services.NotificationProviders;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class CountdownViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly CountdownSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string, double> _updateText1Style;
    private readonly Action<string, double> _updateText2Style;
    private readonly Action<string, double> _updateText3Style;
    private readonly Action<string, double> _updateTimeStyle;
    private readonly Action<string, double> _updateText4Style;
    private bool _isDisposed;
    private bool _isFirstUpdate = true;

    private string _text1Display = string.Empty;
    private string _text2Display = string.Empty;
    private string _text3Display = string.Empty;
    private string _timeDisplay = string.Empty;
    private string _text4Display = string.Empty;
    private CountdownItem? _currentItem;
    private bool _isAllCompleted;

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

    public string Text2Display
    {
        get => _text2Display;
        private set
        {
            if (_text2Display != value)
            {
                _text2Display = value;
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

    public CountdownItem? CurrentItem
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

    public CountdownViewModel(TimeBaseService timeBaseService, CountdownSettings settings,
        Action<string, double> updateText1Style = null,
        Action<string, double> updateText2Style = null,
        Action<string, double> updateText3Style = null,
        Action<string, double> updateTimeStyle = null,
        Action<string, double> updateText4Style = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateText1Style = updateText1Style;
        _updateText2Style = updateText2Style;
        _updateText3Style = updateText3Style;
        _updateTimeStyle = updateTimeStyle;
        _updateText4Style = updateText4Style;

        _settings.PropertyChanged += OnSettingsChanged;

        EnsureDefaultItems();
        UpdateCountdown();
        _isFirstUpdate = false;

        _updateTimer = new System.Timers.Timer(200);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void EnsureDefaultItems()
    {
        if (_settings.CountdownItems == null || _settings.CountdownItems.Count == 0)
        {
            _settings.CountdownItems = new List<CountdownItem>
            {
                CountdownItem.CreateDefault()
            };
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CountdownSettings.Text1):
            case nameof(CountdownSettings.Text2):
            case nameof(CountdownSettings.Text3):
            case nameof(CountdownSettings.Text4):
            case nameof(CountdownSettings.TimeFormat):
            case nameof(CountdownSettings.CountdownItems):
            case nameof(CountdownSettings.TimeBaseType):
                UpdateCountdown();
                break;
            case nameof(CountdownSettings.Text1FontSize):
            case nameof(CountdownSettings.Text1FontColor):
                _updateText1Style?.Invoke(_settings.Text1FontColor, _settings.Text1FontSize);
                break;
            case nameof(CountdownSettings.Text2FontSize):
            case nameof(CountdownSettings.Text2FontColor):
                _updateText2Style?.Invoke(_settings.Text2FontColor, _settings.Text2FontSize);
                break;
            case nameof(CountdownSettings.Text3FontSize):
            case nameof(CountdownSettings.Text3FontColor):
                _updateText3Style?.Invoke(_settings.Text3FontColor, _settings.Text3FontSize);
                break;
            case nameof(CountdownSettings.TimeFontSize):
            case nameof(CountdownSettings.TimeFontColor):
                _updateTimeStyle?.Invoke(_settings.TimeFontColor, _settings.TimeFontSize);
                break;
            case nameof(CountdownSettings.Text4FontSize):
            case nameof(CountdownSettings.Text4FontColor):
                _updateText4Style?.Invoke(_settings.Text4FontColor, _settings.Text4FontSize);
                break;
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
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
                Text2Display = displayData.Text2;
                Text3Display = displayData.Text3;
                TimeDisplay = displayData.Time;
                Text4Display = displayData.Text4;
                IsAllCompleted = displayData.IsAllCompleted;
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
            TimeBaseType.SystemTime => DateTime.Now,
            TimeBaseType.PluginOffset => _timeBaseService.GetCurrentTime(),
            _ => Plugin.GetCurrentTime()
        };
    }

    private async System.Threading.Tasks.Task<DateTime> GetCurrentTimeAsync()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.SystemTime => await System.Threading.Tasks.Task.FromResult(DateTime.Now),
            TimeBaseType.PluginOffset => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false),
            _ => await System.Threading.Tasks.Task.FromResult(Plugin.GetCurrentTime())
        };
    }

    private void ProcessCountdown(DateTime now)
    {
        var displayData = ProcessCountdownInternal(now);
        Text1Display = displayData.Text1;
        Text2Display = displayData.Text2;
        Text3Display = displayData.Text3;
        TimeDisplay = displayData.Time;
        Text4Display = displayData.Text4;
        IsAllCompleted = displayData.IsAllCompleted;
        CurrentItem = displayData.CurrentItem;
    }

    private CountdownDisplayData ProcessCountdownInternal(DateTime now)
    {
        if (_settings.CountdownItems == null || _settings.CountdownItems.Count == 0)
        {
            return new CountdownDisplayData
            {
                Text1 = _settings.Text1,
                Text2 = string.Empty,
                Text3 = "倒计时已结束",
                Time = string.Empty,
                Text4 = string.Empty,
                IsAllCompleted = true,
                CurrentItem = null
            };
        }

        var unixNow = UnixTimeHelper.ToUnixTimestamp(now);

        var expiredItems = _settings.CountdownItems
            .Where(item => !item.IsCompleted && item.TargetTimestamp <= unixNow)
            .OrderBy(item => item.TargetTimestamp)
            .ToList();

        foreach (var item in expiredItems)
        {
            HandleItemCompleted(item);
        }

        var activeItems = _settings.CountdownItems.Where(item => !item.IsCompleted && item.TargetTimestamp > unixNow).ToList();

        if (activeItems.Count == 0)
        {
            return new CountdownDisplayData
            {
                Text1 = _settings.Text1,
                Text2 = string.Empty,
                Text3 = "倒计时已结束",
                Time = string.Empty,
                Text4 = string.Empty,
                IsAllCompleted = true,
                CurrentItem = null
            };
        }

        var sortedItems = activeItems.OrderBy(item => item.TargetTimestamp).ToList();
        var currentItem = sortedItems.First();

        var timeLeft = currentItem.TargetTimestamp - unixNow;
        var timeLeftMs = timeLeft * 1000;

        var timeFormat = string.IsNullOrEmpty(_settings.TimeFormat) ? "%d天%h小时%m分钟%s秒" : _settings.TimeFormat;
        var timeText = FormatTime(timeFormat, timeLeft, timeLeftMs);

        return new CountdownDisplayData
        {
            Text1 = _settings.Text1,
            Text2 = _settings.Text2,
            Text3 = _settings.Text3,
            Time = timeText,
            Text4 = _settings.Text4,
            IsAllCompleted = false,
            CurrentItem = currentItem
        };
    }

    private void HandleItemCompleted(CountdownItem item)
    {
        try
        {
            item.IsCompleted = true;

            if (item.EnableNotification && !_isFirstUpdate)
            {
                var maskText = string.IsNullOrEmpty(item.NotificationTitle) ? "倒计时到达" : item.NotificationTitle;
                var maskDuration = item.NotificationMaskDurationSeconds > 0 ? item.NotificationMaskDurationSeconds : 3;
                var overlayText = item.NotificationContent ?? string.Empty;
                var overlayDuration = item.NotificationOverlayDurationSeconds > 0 ? item.NotificationOverlayDurationSeconds : 10;
                
                CountdownNotificationProvider.Notify(maskText, maskDuration, overlayText, overlayDuration);
            }
        }
        catch (Exception)
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

        var result = format
            .Replace("%D", ((int)totalDays).ToString())
            .Replace("%H", ((int)totalHours).ToString())
            .Replace("%M", ((int)totalMinutes).ToString())
            .Replace("%S", totalSeconds.ToString())
            .Replace("%X", ((int)totalMilliseconds).ToString())
            .Replace("%L", "0")
            .Replace("%P", "100")
            .Replace("%p", "100.00")
            .Replace("%d", days.ToString())
            .Replace("%h", hours.ToString())
            .Replace("%m", minutes.ToString())
            .Replace("%s", seconds.ToString())
            .Replace("%x", milliseconds.ToString());

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
        _updateTimer?.Dispose();
    }

    private class CountdownDisplayData
    {
        public string Text1 { get; set; } = string.Empty;
        public string Text2 { get; set; } = string.Empty;
        public string Text3 { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Text4 { get; set; } = string.Empty;
        public bool IsAllCompleted { get; set; }
        public CountdownItem? CurrentItem { get; set; }
    }
}