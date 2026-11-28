using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.ViewModels.Main;

/// <summary>
/// 周数组件视图模型基类，负责统一的时间刷新、时间基准切换与字体回调分发。
/// </summary>
public abstract class WeekViewModelBase<TSettings> : INotifyPropertyChanged, IDisposable
    where TSettings : WeekSettings
{
    private readonly TimeBaseService _timeBaseService;
    protected readonly TSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateFontColor;
    private readonly Action<double> _updateFontSize;
    private bool _isDisposed;
    private string _displayText = string.Empty;

    public string DisplayText
    {
        get => _displayText;
        protected set
        {
            if (_displayText != value)
            {
                _displayText = value;
                OnPropertyChanged();
            }
        }
    }

    protected WeekViewModelBase(TimeBaseService timeBaseService, TSettings settings,
        Action<string>? updateFontColor = null, Action<double>? updateFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateFontColor = updateFontColor;
        _updateFontSize = updateFontSize;

        // 迁移旧版时间基准值
        var migrated = TimeBaseTypeHelper.Migrate((int)_settings.TimeBaseType);
        if (migrated != _settings.TimeBaseType)
        {
            _settings.TimeBaseType = migrated;
        }

        Refresh();

        _updateTimer = new System.Timers.Timer(200);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    /// <summary>计算当前周数。</summary>
    protected abstract int ComputeWeekNumber(DateTime now);

    /// <summary>将周数转换为显示文本。</summary>
    protected abstract string BuildDisplayText(int weekNumber);

    protected void NotifyFontColor(string color) => _updateFontColor?.Invoke(color);

    protected void NotifyFontSize(double fontSize) => _updateFontSize?.Invoke(fontSize);

    protected void TriggerAsyncUpdate() => _ = UpdateTimeAsync();

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

    private async Task<DateTime> GetCurrentTimeAsync()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false),
            TimeBaseType.RawServerTime => await _timeBaseService.GetRawServerTimeAsync().ConfigureAwait(false),
            TimeBaseType.ClassIslandTime => await _timeBaseService.GetClassIslandTimeAsync().ConfigureAwait(false),
            _ => await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false)
        };
    }

    private void Refresh()
    {
        try
        {
            var now = GetCurrentTime();
            DisplayText = BuildDisplayText(ComputeWeekNumber(now));
        }
        catch (Exception)
        {
        }
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _ = UpdateTimeAsync();
    }

    private async Task UpdateTimeAsync()
    {
        try
        {
            var now = await GetCurrentTimeAsync().ConfigureAwait(false);
            var text = BuildDisplayText(ComputeWeekNumber(now));

            // WPF 版用 Dispatcher 取代 Avalonia 的 UIThread
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                DisplayText = text;
                return;
            }

            await dispatcher.InvokeAsync(() =>
            {
                DisplayText = text;
            });
        }
        catch (Exception)
        {
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public virtual void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
