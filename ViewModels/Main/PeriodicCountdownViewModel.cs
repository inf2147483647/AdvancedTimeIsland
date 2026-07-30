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

public class PeriodicCountdownViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly PeriodicCountdownSettings _settings;
    private IDisposable? _subscription;
    private readonly Action<string, double> _updateText1Style;
    private readonly Action<string, double> _updateText2Style;
    private readonly Action<string, double> _updateText3Style;
    private readonly Action<string, double> _updateTimeStyle;
    private readonly Action<string, double> _updateText4Style;
    private bool _isDisposed;
    private bool _isFirstUpdate = true;
    private bool _requiresHighFrequencyRefresh;

    private string _text1Display = string.Empty;
    private string _text2Display = string.Empty;
    private string _text3Display = string.Empty;
    private string _timeDisplay = string.Empty;
    private string _text4Display = string.Empty;
    private PeriodicCountdownItem? _currentItem;
    private bool _isAllCompleted;
    private bool _isEmpty;
    private double _percent;

    public double Percent
    {
        get => _percent;
        private set
        {
            if (_percent != value)
            {
                _percent = value;
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

    public PeriodicCountdownItem? CurrentItem
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

    public PeriodicCountdownViewModel(TimeBaseService timeBaseService, PeriodicCountdownSettings settings,
        Action<string, double> updateText1Style = null,
        Action<string, double> updateText2Style = null,
        Action<string, double> updateText3Style = null,
        Action<string, double> updateTimeStyle = null,
        Action<string, double> updateText4Style = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        // 迁移旧版时间基准值
        var migrated = TimeBaseTypeHelper.Migrate((int)_settings.TimeBaseType);
        if (migrated != _settings.TimeBaseType)
        {
            _settings.TimeBaseType = migrated;
        }
        _updateText1Style = updateText1Style;
        _updateText2Style = updateText2Style;
        _updateText3Style = updateText3Style;
        _updateTimeStyle = updateTimeStyle;
        _updateText4Style = updateText4Style;

        _settings.PropertyChanged += OnSettingsChanged;

        EnsureDefaultItems();
        UpdateCountdown();
        _isFirstUpdate = false;

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
               timeFormat.Contains("%X") ||
               timeFormat.Contains("%P") ||
               timeFormat.Contains("%p") ||
               timeFormat.Contains("%L");
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

    private void EnsureDefaultItems()
    {
        if (_settings.CountdownItems == null)
        {
            _settings.CountdownItems = new List<PeriodicCountdownItem>();
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PeriodicCountdownSettings.Text1) ||
            e.PropertyName == nameof(PeriodicCountdownSettings.Text2) ||
            e.PropertyName == nameof(PeriodicCountdownSettings.Text3) ||
            e.PropertyName == nameof(PeriodicCountdownSettings.Text4) ||
            e.PropertyName == nameof(PeriodicCountdownSettings.CountdownItems) ||
            e.PropertyName == nameof(PeriodicCountdownSettings.TimeBaseType))
        {
            UpdateCountdown();
        }
        if (e.PropertyName == nameof(PeriodicCountdownSettings.TimeFormat))
        {
            UpdateCountdown();
            UpdateRefreshMode();
        }
        if (e.PropertyName == nameof(PeriodicCountdownSettings.Text1FontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text1FontColor) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text1EnableCustomFontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text1EnableCustomFontColor))
        {
            _updateText1Style?.Invoke(_settings.Text1FontColor, _settings.Text1EnableCustomFontSize ? _settings.Text1FontSize : 0);
        }
        if (e.PropertyName == nameof(PeriodicCountdownSettings.Text2FontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text2FontColor) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text2EnableCustomFontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text2EnableCustomFontColor))
        {
            _updateText2Style?.Invoke(_settings.Text2FontColor, _settings.Text2EnableCustomFontSize ? _settings.Text2FontSize : 0);
        }
        if (e.PropertyName == nameof(PeriodicCountdownSettings.Text3FontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text3FontColor) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text3EnableCustomFontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text3EnableCustomFontColor))
        {
            _updateText3Style?.Invoke(_settings.Text3FontColor, _settings.Text3EnableCustomFontSize ? _settings.Text3FontSize : 0);
        }
        if (e.PropertyName == nameof(PeriodicCountdownSettings.TimeFontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.TimeFontColor) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.TimeEnableCustomFontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.TimeEnableCustomFontColor))
        {
            _updateTimeStyle?.Invoke(_settings.TimeFontColor, _settings.TimeEnableCustomFontSize ? _settings.TimeFontSize : 0);
        }
        if (e.PropertyName == nameof(PeriodicCountdownSettings.Text4FontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text4FontColor) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text4EnableCustomFontSize) ||
                 e.PropertyName == nameof(PeriodicCountdownSettings.Text4EnableCustomFontColor))
        {
            _updateText4Style?.Invoke(_settings.Text4FontColor, _settings.Text4EnableCustomFontSize ? _settings.Text4FontSize : 0);
        }
    }

    private void OnClockTick(DateTime now)
    {
        _ = UpdateCountdownAsync(now);
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

    private async System.Threading.Tasks.Task UpdateCountdownAsync(DateTime? clockTime = null)
    {
        try
        {
            var now = clockTime ?? await GetCurrentTimeAsync().ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var displayData = ProcessCountdownInternal(now);
                Text1Display = displayData.Text1;
                Text2Display = displayData.Text2;
                Text3Display = displayData.Text3;
                TimeDisplay = displayData.Time;
                Text4Display = displayData.Text4;
                IsAllCompleted = displayData.IsAllCompleted;
                IsEmpty = displayData.IsEmpty;
                CurrentItem = displayData.CurrentItem;
                Percent = displayData.Percent;
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
            TimeBaseType.RawServerTime => _timeBaseService.GetRawServerTime(),
            TimeBaseType.ClassIslandTime => _timeBaseService.GetClassIslandTime(),
            _ => _timeBaseService.GetCurrentTime()
        };
    }

    private async System.Threading.Tasks.Task<DateTime> GetCurrentTimeAsync()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false),
            TimeBaseType.RawServerTime => await _timeBaseService.GetRawServerTimeAsync().ConfigureAwait(false),
            TimeBaseType.ClassIslandTime => await _timeBaseService.GetClassIslandTimeAsync().ConfigureAwait(false),
            _ => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false)
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
        Percent = displayData.Percent;
    }

    private PeriodicCountdownDisplayData ProcessCountdownInternal(DateTime now)
    {
        if (_settings.CountdownItems == null || _settings.CountdownItems.Count == 0)
        {
            return new PeriodicCountdownDisplayData
            {
                Text1 = _settings.Text1,
                Text2 = string.Empty,
                Text3 = "当前无倒计时",
                Time = string.Empty,
                Text4 = string.Empty,
                IsAllCompleted = false,
                IsEmpty = true,
                CurrentItem = null
            };
        }

        var unixNow = UnixTimeHelper.ToUnixTimestampDouble(now);

        var expiredItems = _settings.CountdownItems
            .Where(item => !item.IsCompleted && item.GetNextTargetTimestamp(now) <= (long)unixNow)
            .ToList();

        foreach (var item in expiredItems)
        {
            HandleItemCompleted(item);
        }

        var activeItems = _settings.CountdownItems
            .Select(item => new { Item = item, TargetTimestamp = item.GetNextTargetTimestamp(now) })
            .Where(x => x.TargetTimestamp > (long)unixNow)
            .ToList();

        if (activeItems.Count == 0)
        {
            return new PeriodicCountdownDisplayData
            {
                Text1 = _settings.Text1,
                Text2 = string.Empty,
                Text3 = "倒计时已结束",
                Time = string.Empty,
                Text4 = string.Empty,
                IsAllCompleted = true,
                IsEmpty = false,
                CurrentItem = null
            };
        }

        var sortedItems = activeItems.OrderBy(x => x.TargetTimestamp).ToList();
        var currentItem = sortedItems.First();

        var currentTargetDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(currentItem.TargetTimestamp).ToLocalTime();
        var timeLeftSpan = currentTargetDate - now;
        var timeLeft = timeLeftSpan.TotalSeconds;
        var timeLeftMs = timeLeftSpan.TotalMilliseconds;

        var timeFormat = string.IsNullOrEmpty(_settings.TimeFormat) ? "%d天%h小时%m分钟%s秒" : _settings.TimeFormat;
        var timeText = FormatTime(timeFormat, (long)Math.Floor(timeLeft), timeLeftMs, now, currentTargetDate, currentItem.TargetTimestamp, _settings.EnableTimeCorrection);

        double percent = 0;
        var periodSeconds = currentItem.Item.GetPeriodSeconds(now);
        if (periodSeconds > 0)
        {
            var lastTargetTimestamp = currentItem.Item.GetPreviousTargetTimestamp(now);
            var totalDuration = currentItem.TargetTimestamp - lastTargetTimestamp;
            var elapsedSeconds = (long)unixNow - lastTargetTimestamp;
            percent = Math.Min(100, Math.Max(0, elapsedSeconds * 100.0 / totalDuration));
        }

        return new PeriodicCountdownDisplayData
        {
            Text1 = _settings.Text1,
            Text2 = currentItem.Item.Name,
            Text3 = _settings.Text3,
            Time = timeText,
            Text4 = _settings.Text4,
            IsAllCompleted = false,
            IsEmpty = false,
            CurrentItem = currentItem.Item,
            Percent = percent
        };
    }

    private void HandleItemCompleted(PeriodicCountdownItem item)
    {
        try
        {
            item.IsCompleted = true;

            if (item.EnableNotification && !_isFirstUpdate)
            {
                var maskText = string.IsNullOrEmpty(item.NotificationTitle) ? "周期性倒计时到达" : item.NotificationTitle;
                var maskDuration = item.NotificationMaskDurationSeconds > 0 ? item.NotificationMaskDurationSeconds : 3;
                var overlayText = item.NotificationContent ?? string.Empty;
                var overlayDuration = item.NotificationOverlayDurationSeconds > 0 ? item.NotificationOverlayDurationSeconds : 10;

                CountdownNotificationProvider.Notify(maskText, maskDuration, overlayText, overlayDuration);
            }

            item.IsCompleted = false;
        }
        catch (Exception)
        {
        }
    }

    private string FormatTime(string format, long secondsLeft, double millisecondsLeft, DateTime now, DateTime targetDate, long targetTime, bool enableTimeCorrection)
    {
        return OptimizedTimeFormatter.FormatPeriodicCountdownTime(format, secondsLeft, millisecondsLeft, now, targetDate, enableTimeCorrection);
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

    private class PeriodicCountdownDisplayData
    {
        public string Text1 { get; set; } = string.Empty;
        public string Text2 { get; set; } = string.Empty;
        public string Text3 { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Text4 { get; set; } = string.Empty;
        public bool IsAllCompleted { get; set; }
        public bool IsEmpty { get; set; }
        public PeriodicCountdownItem? CurrentItem { get; set; }
        public double Percent { get; set; }
    }
}