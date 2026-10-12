using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class TimeZoneTimeViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly TimeZoneTimeSettings _settings;
    private IDisposable? _clockSubscription;
    private readonly Action<string> _updateFontColor;
    private readonly Action<double> _updateFontSize;
    private string _fullDisplay = string.Empty;
    private bool _isDisposed;

    // 时区与显示名只在 TimeZoneId 改变时才变化，缓存后刷新路径不再每 200ms 调两次 FindSystemTimeZoneById。
    private string? _cachedTimeZoneId;
    private TimeZoneInfo? _cachedTimeZone;
    private string _cachedTimeZoneDisplayName = "未知时区";

    public TimeZoneTimeViewModel(TimeBaseService timeBaseService, TimeZoneTimeSettings settings, Action<string> updateFontColor = null, Action<double> updateFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateFontColor = updateFontColor;
        _updateFontSize = updateFontSize;

        _settings.PropertyChanged += OnSettingsChanged;

        UpdateTime();

        _clockSubscription = SharedRenderClockService.Instance.Subscribe(OnClockTick);
        SharedRenderClockService.Instance.EnsureStarted();
    }

    private void OnClockTick(DateTime now)
    {
        // 共享时钟回调已在 UI 线程，直接同步刷新。
        UpdateTime();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimeZoneTimeSettings.FontColor) ||
            e.PropertyName == nameof(TimeZoneTimeSettings.EnableCustomFontColor) ||
            e.PropertyName == nameof(TimeZoneTimeSettings.FontFamily) ||
            e.PropertyName == nameof(TimeZoneTimeSettings.EnableCustomFontFamily))
        {
            _updateFontColor?.Invoke(_settings.FontColor);
        }
        if (e.PropertyName == nameof(TimeZoneTimeSettings.TimeZoneId))
        {
            UpdateTime();
        }
        if (e.PropertyName == nameof(TimeZoneTimeSettings.TextFontSize) ||
                 e.PropertyName == nameof(TimeZoneTimeSettings.EnableCustomFontSize) ||
                 e.PropertyName == nameof(TimeZoneTimeSettings.FontFamily) ||
                 e.PropertyName == nameof(TimeZoneTimeSettings.EnableCustomFontFamily) ||
                 e.PropertyName == nameof(TimeZoneTimeSettings.FontWeight) ||
                 e.PropertyName == nameof(TimeZoneTimeSettings.EnableCustomFontWeight))
        {
            _updateFontSize?.Invoke(_settings.EnableCustomFontSize ? _settings.TextFontSize : 0);
        }
    }

    public string FullDisplay
    {
        get => _fullDisplay;
        private set
        {
            if (_fullDisplay != value)
            {
                _fullDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    private void UpdateTime()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            var timeZoneTime = ConvertToTimeZone(now);

            FullDisplay = FormatFullDisplay(timeZoneTime);
        }
        catch (Exception)
        {
        }
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        if (_cachedTimeZoneId == _settings.TimeZoneId && _cachedTimeZone != null)
        {
            return _cachedTimeZone;
        }

        _cachedTimeZoneId = _settings.TimeZoneId;
        try
        {
            _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
            _cachedTimeZoneDisplayName = BuildTimeZoneDisplayName(_cachedTimeZone);
        }
        catch
        {
            _cachedTimeZone = null;
            _cachedTimeZoneDisplayName = "未知时区";
        }

        return _cachedTimeZone!;
    }

    private DateTime ConvertToTimeZone(DateTime utcTime)
    {
        var targetTimeZone = ResolveTimeZone();
        if (targetTimeZone == null)
        {
            return utcTime.ToLocalTime();
        }
        return TimeZoneInfo.ConvertTime(utcTime, targetTimeZone);
    }

    private string FormatFullDisplay(DateTime dateTime)
    {
        return $"区时 {dateTime:yyyy-MM-dd} {dateTime:HH:mm:ss} {_cachedTimeZoneDisplayName}";
    }

    private static string BuildTimeZoneDisplayName(TimeZoneInfo tz)
    {
        double offset = tz.BaseUtcOffset.TotalHours;
        string sign = offset >= 0 ? "东" : "西";
        int hours = Math.Abs((int)offset);
        int minutes = Math.Abs(tz.BaseUtcOffset.Minutes);

        string decimalPart = "";
        if (minutes == 15)
            decimalPart = ".25";
        else if (minutes == 30)
            decimalPart = ".5";
        else if (minutes == 45)
            decimalPart = ".75";

        return $"{sign}{hours}{decimalPart}区";
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
        _clockSubscription?.Dispose();
    }
}



