using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Threading.Tasks;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

/// <summary>
/// 周数组件视图模型基类，负责统一的时间刷新与字体回调分发。
/// </summary>
public abstract class WeekViewModelBase<TSettings> : INotifyPropertyChanged, IDisposable
    where TSettings : WeekSettings
{
    private readonly TimeBaseService _timeBaseService;
    protected readonly TSettings _settings;
    private IDisposable? _clockSubscription;
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

        Refresh();

        // 复用全局共享渲染时钟（普通档 200ms），不再各自维护 System.Timers.Timer，
        // 消除独立定时器与跨线程 Dispatcher 二次派发。
        _clockSubscription = SharedRenderClockService.Instance.Subscribe(OnClockTick);
        SharedRenderClockService.Instance.EnsureStarted();
    }

    /// <summary>计算当前周数。</summary>
    protected abstract int ComputeWeekNumber(DateTime now);

    /// <summary>将周数转换为显示文本。</summary>
    protected abstract string BuildDisplayText(int weekNumber);

    protected void NotifyFontColor(string color) => _updateFontColor?.Invoke(color);

    protected void NotifyFontSize(double fontSize) => _updateFontSize?.Invoke(fontSize);

    protected void TriggerAsyncUpdate() => Refresh();

    private DateTime GetCurrentTime()
    {
        return _timeBaseService.GetClassIslandTime();
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

    // 共享时钟回调已在 UI 线程，直接同步重算；DisplayText 的 setter 会在文本未变时短路 INPC。
    private void OnClockTick(DateTime now)
    {
        try
        {
            DisplayText = BuildDisplayText(ComputeWeekNumber(GetCurrentTime()));
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
        _clockSubscription?.Dispose();
    }
}