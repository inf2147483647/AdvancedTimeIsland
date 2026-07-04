using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
    private DispatcherTimer? _updateTimer;
    private DispatcherTimer? _highFrequencyTimer;
    private readonly Action<string, double> _updateText1Style;
    private readonly Action<string, double> _updateText2Style;
    private readonly Action<string, double> _updateText3Style;
    private readonly Action<string, double> _updateTimeStyle;
    private readonly Action<string, double> _updateText4Style;
    private bool _isDisposed;
    private bool _isFirstUpdate = true;
    private bool _requiresHighFrequencyRefresh;
    private readonly TimeSpan _normalInterval = TimeSpan.FromMilliseconds(200);
    private TimeSpan _highFrequencyInterval = TimeSpan.FromMilliseconds(16.67);

    private string _text1Display = string.Empty;
    private string _text2Display = string.Empty;
    private string _text3Display = string.Empty;
    private string _timeDisplay = string.Empty;
    private string _text4Display = string.Empty;
    private CountdownItem? _currentItem;
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

        // 根据显示器刷新率动态计算高频率刷新间隔
        _highFrequencyInterval = DisplayHelper.CalculateHighFrequencyInterval();

        // 检测是否需要高频率刷新
        _requiresHighFrequencyRefresh = RequiresHighFrequencyRefresh(_settings.TimeFormat);

        // 初始化计时器
        InitializeTimers();
    }

    private void InitializeTimers()
    {
        // 低频率计时器（每秒5次）
        _updateTimer = new DispatcherTimer
        {
            Interval = _normalInterval
        };
        _updateTimer.Tick += OnTimerElapsed;
        _updateTimer.Start();

        // 高频率计时器（帧级刷新，用于毫秒显示）
        _highFrequencyTimer = new DispatcherTimer
        {
            Interval = _highFrequencyInterval
        };
        _highFrequencyTimer.Tick += OnHighFrequencyTimerElapsed;

        // 如果需要高频率刷新，启动高频率计时器
        if (_requiresHighFrequencyRefresh)
        {
            _updateTimer.Stop();
            _highFrequencyTimer.Start();
        }
    }

    /// <summary>
    /// 检测格式化文本是否包含毫秒相关内容，需要高频率刷新
    /// </summary>
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

    /// <summary>
    /// 手动触发刷新模式更新（在设置控件中调用）
    /// </summary>
    public void UpdateRefreshMode()
    {
        var newRequiresHighFrequency = RequiresHighFrequencyRefresh(_settings.TimeFormat);
        if (_requiresHighFrequencyRefresh != newRequiresHighFrequency)
        {
            _requiresHighFrequencyRefresh = newRequiresHighFrequency;

            if (_requiresHighFrequencyRefresh)
            {
                // 切换到高频率模式
                _updateTimer?.Stop();
                _highFrequencyTimer?.Start();
            }
            else
            {
                // 切换到低频率模式
                _highFrequencyTimer?.Stop();
                _updateTimer?.Start();
            }
        }
    }

    private void EnsureDefaultItems()
    {
        if (_settings.CountdownItems == null)
        {
            _settings.CountdownItems = new List<CountdownItem>();
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
            case nameof(CountdownSettings.CountdownItems):
            case nameof(CountdownSettings.TimeBaseType):
                UpdateCountdown();
                break;
            case nameof(CountdownSettings.TimeFormat):
                UpdateCountdown();
                UpdateRefreshMode();
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
                Text2Display = displayData.Text2;
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
            .Where(item => !item.IsCompleted && item.TargetTimestamp <= (long)unixNow)
            .OrderBy(item => item.TargetTimestamp)
            .ToList();

        foreach (var item in expiredItems)
        {
            HandleItemCompleted(item);
        }

        var activeItems = _settings.CountdownItems.Where(item => !item.IsCompleted && item.TargetTimestamp > (long)unixNow).ToList();

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
                IsEmpty = false,
                CurrentItem = null
            };
        }

        var sortedItems = activeItems.OrderBy(item => item.TargetTimestamp).ToList();
        var currentItem = sortedItems.First();

        var currentTargetDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(currentItem.TargetTimestamp).ToLocalTime();
        var timeLeftSpan = currentTargetDate - now;
        var timeLeft = timeLeftSpan.TotalSeconds;
        var timeLeftMs = timeLeftSpan.TotalMilliseconds;

        var timeFormat = string.IsNullOrEmpty(_settings.TimeFormat) ? "%D天%h小时%m分钟%s秒" : _settings.TimeFormat;
        var timeText = FormatTime(timeFormat, (long)Math.Floor(timeLeft), timeLeftMs, now, currentTargetDate, _settings.StartTime, currentItem.TargetTimestamp, _settings.EnableTimeCorrection);

        return new CountdownDisplayData
        {
            Text1 = _settings.Text1,
            Text2 = currentItem.Name,
            Text3 = _settings.Text3,
            Time = timeText,
            Text4 = _settings.Text4,
            IsAllCompleted = false,
            IsEmpty = false,
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

    private string FormatTime(string format, long secondsLeft, double millisecondsLeft, DateTime now, DateTime targetDate, long startTime, long targetTime, bool enableTimeCorrection)
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

        bool hasMillisecond = format.Contains("%x") || format.Contains("%X");
        if (enableTimeCorrection && !hasMillisecond && secondsLeft > 0)
        {
            bool hasSeconds = format.Contains("%s") || format.Contains("%S");
            bool hasMinutes = format.Contains("%m") || format.Contains("%M");
            bool hasHours = format.Contains("%h") || format.Contains("%H");
            bool hasDays = format.Contains("%d") || format.Contains("%D");

            if (hasSeconds)
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
            else if (hasMinutes)
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
            else if (hasHours)
            {
                hours++;
                if (hours >= 24)
                {
                    hours = 0;
                    days++;
                }
            }
            else if (hasDays)
            {
                days++;
            }
        }

        var totalDuration = targetTime - startTime;
        var elapsedSeconds = targetTime - startTime - secondsLeft;

        string remainingPercent = "0";
        string elapsedPercent = "0";
        string elapsedPercentDecimal = "0.00";

        if (totalDuration > 0)
        {
            remainingPercent = ((int)(secondsLeft * 100.0 / totalDuration)).ToString();
            elapsedPercent = ((int)(elapsedSeconds * 100.0 / totalDuration)).ToString();
            elapsedPercentDecimal = (elapsedSeconds * 100.0 / totalDuration).ToString("F2");
        }

        bool hasMonth = format.Contains("%mo") || format.Contains("%MO");
        bool hasYear = format.Contains("%yy") || format.Contains("%YY");

        int displayYears = 0;
        int displayMonths = 0;
        int displayDays = days;

        if (hasYear || hasMonth)
        {
            var tempDate = now;
            displayYears = 0;

            while (tempDate.AddYears(1) <= targetDate)
            {
                tempDate = tempDate.AddYears(1);
                displayYears++;
            }

            if (hasMonth)
            {
                displayMonths = 0;
                while (tempDate.AddMonths(1) <= targetDate)
                {
                    tempDate = tempDate.AddMonths(1);
                    displayMonths++;
                }

                var dayDiff = (targetDate - tempDate).Days;
                displayDays = Math.Max(0, dayDiff);
            }
            else
            {
                var dayDiff = (targetDate - tempDate).Days;
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

    private class CountdownDisplayData
    {
        public string Text1 { get; set; } = string.Empty;
        public string Text2 { get; set; } = string.Empty;
        public string Text3 { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Text4 { get; set; } = string.Empty;
        public bool IsAllCompleted { get; set; }
        public bool IsEmpty { get; set; }
        public CountdownItem? CurrentItem { get; set; }
    }
}


