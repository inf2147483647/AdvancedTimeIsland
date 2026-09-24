using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

/// <summary>
/// 七段数码管时钟视图模型：按所选时间基准取时、格式化显示文本，并驱动分隔符（冒号）闪动相位。
/// </summary>
public class SevenSegmentClockViewModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>分隔符亮/灭各自的时长（毫秒）。</summary>
    private const int SeparatorBlinkHalfPeriodMs = 500;

    private readonly TimeBaseService _timeBaseService;
    private readonly SevenSegmentClockSettings _settings;
    private IDisposable? _subscription;
    private bool _isDisposed;

    private string _displayText = "--:--:--";
    private bool _separatorLit = true;

    public SevenSegmentClockViewModel(TimeBaseService timeBaseService, SevenSegmentClockSettings settings)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;

        // 迁移旧版时间基准值
        var migrated = TimeBaseTypeHelper.Migrate((int)_settings.TimeBaseType);
        if (migrated != _settings.TimeBaseType)
        {
            _settings.TimeBaseType = migrated;
        }

        _settings.PropertyChanged += OnSettingsChanged;

        UpdateDisplay(GetTimeByBase());

        _subscription = SharedRenderClockService.Instance.Subscribe(OnClockTick);
        SharedRenderClockService.Instance.EnsureStarted();
    }

    /// <summary>
    /// 当前应显示的文本，形如 11:45:14（关闭秒数时为 11:45）。
    /// </summary>
    public string DisplayText
    {
        get => _displayText;
        private set
        {
            if (_displayText != value)
            {
                _displayText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 分隔符（冒号）当前是否点亮。
    /// </summary>
    public bool SeparatorLit
    {
        get => _separatorLit;
        private set
        {
            if (_separatorLit != value)
            {
                _separatorLit = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SevenSegmentClockSettings.ShowSeconds):
            case nameof(SevenSegmentClockSettings.SeparatorBlink):
            case nameof(SevenSegmentClockSettings.TimeBaseType):
                _ = UpdateDisplayAsync();
                break;
        }
    }

    private void OnClockTick(DateTime now)
    {
        _ = UpdateDisplayAsync();
    }

    private async Task UpdateDisplayAsync()
    {
        try
        {
            var time = await GetTimeByBaseAsync().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => UpdateDisplay(time));
        }
        catch
        {
        }
    }

    private void UpdateDisplay(DateTime time)
    {
        DisplayText = _settings.ShowSeconds ? time.ToString("HH:mm:ss") : time.ToString("HH:mm");
        SeparatorLit = !_settings.SeparatorBlink || time.Millisecond < SeparatorBlinkHalfPeriodMs;
    }

    private DateTime GetTimeByBase()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.RawServerTime => _timeBaseService.GetRawServerTime(),
            TimeBaseType.ClassIslandTime => _timeBaseService.GetClassIslandTime(),
            _ => _timeBaseService.GetCurrentTime()
        };
    }

    private async Task<DateTime> GetTimeByBaseAsync()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.RawServerTime => await _timeBaseService.GetRawServerTimeAsync().ConfigureAwait(false),
            TimeBaseType.ClassIslandTime => await _timeBaseService.GetClassIslandTimeAsync().ConfigureAwait(false),
            _ => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false)
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _settings.PropertyChanged -= OnSettingsChanged;
        _subscription?.Dispose();
        _subscription = null;
    }
}
