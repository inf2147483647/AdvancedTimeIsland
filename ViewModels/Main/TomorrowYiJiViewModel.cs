using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class TomorrowYiJiViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly TomorrowYiJiSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateYiLabelFontColor;
    private readonly Action<double> _updateYiLabelFontSize;
    private readonly Action<double> _updateYiValueFontSize;
    private readonly Action<string> _updateJiLabelFontColor;
    private readonly Action<double> _updateJiLabelFontSize;
    private readonly Action<double> _updateJiValueFontSize;
    private string _yiLabelText = string.Empty;
    private string _yiValueText = string.Empty;
    private string _jiLabelText = string.Empty;
    private string _jiValueText = string.Empty;
    private bool _isDisposed;

    public string YiLabelText { get => _yiLabelText; private set { if (_yiLabelText != value) { _yiLabelText = value; OnPropertyChanged(); } } }
    public string YiValueText { get => _yiValueText; private set { if (_yiValueText != value) { _yiValueText = value; OnPropertyChanged(); } } }
    public string JiLabelText { get => _jiLabelText; private set { if (_jiLabelText != value) { _jiLabelText = value; OnPropertyChanged(); } } }
    public string JiValueText { get => _jiValueText; private set { if (_jiValueText != value) { _jiValueText = value; OnPropertyChanged(); } } }

    public TomorrowYiJiViewModel(TimeBaseService timeBaseService, TomorrowYiJiSettings settings,
        Action<string> updateYiLabelFontColor = null, Action<double> updateYiLabelFontSize = null,
        Action<double> updateYiValueFontSize = null, Action<string> updateJiLabelFontColor = null,
        Action<double> updateJiLabelFontSize = null, Action<double> updateJiValueFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateYiLabelFontColor = updateYiLabelFontColor;
        _updateYiLabelFontSize = updateYiLabelFontSize;
        _updateYiValueFontSize = updateYiValueFontSize;
        _updateJiLabelFontColor = updateJiLabelFontColor;
        _updateJiLabelFontSize = updateJiLabelFontSize;
        _updateJiValueFontSize = updateJiValueFontSize;
        
        _settings.PropertyChanged += OnSettingsChanged;
        UpdateDisplay();
        _updateTimer = new System.Timers.Timer(60000);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TomorrowYiJiSettings.YiLabelFontColor) ||
            e.PropertyName == nameof(TomorrowYiJiSettings.YiLabelEnableCustomFontColor))
        {
            _updateYiLabelFontColor?.Invoke(_settings.YiLabelFontColor);
        }
        if (e.PropertyName == nameof(TomorrowYiJiSettings.YiLabelFontSize) ||
            e.PropertyName == nameof(TomorrowYiJiSettings.YiLabelEnableCustomFontSize))
        {
            _updateYiLabelFontSize?.Invoke(_settings.YiLabelEnableCustomFontSize ? _settings.YiLabelFontSize : 14);
        }
        if (e.PropertyName == nameof(TomorrowYiJiSettings.YiValueFontSize) ||
            e.PropertyName == nameof(TomorrowYiJiSettings.YiValueEnableCustomFontSize))
        {
            _updateYiValueFontSize?.Invoke(_settings.YiValueEnableCustomFontSize ? _settings.YiValueFontSize : 14);
        }
        if (e.PropertyName == nameof(TomorrowYiJiSettings.JiLabelFontColor) ||
            e.PropertyName == nameof(TomorrowYiJiSettings.JiLabelEnableCustomFontColor))
        {
            _updateJiLabelFontColor?.Invoke(_settings.JiLabelFontColor);
        }
        if (e.PropertyName == nameof(TomorrowYiJiSettings.JiLabelFontSize) ||
            e.PropertyName == nameof(TomorrowYiJiSettings.JiLabelEnableCustomFontSize))
        {
            _updateJiLabelFontSize?.Invoke(_settings.JiLabelEnableCustomFontSize ? _settings.JiLabelFontSize : 14);
        }
        if (e.PropertyName == nameof(TomorrowYiJiSettings.JiValueFontSize) ||
            e.PropertyName == nameof(TomorrowYiJiSettings.JiValueEnableCustomFontSize))
        {
            _updateJiValueFontSize?.Invoke(_settings.JiValueEnableCustomFontSize ? _settings.JiValueFontSize : 14);
        }
        if (e.PropertyName == nameof(TomorrowYiJiSettings.YiLabel) || e.PropertyName == nameof(TomorrowYiJiSettings.JiLabel))
        {
            UpdateDisplay();
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => _ = UpdateDisplayAsync();

    private void UpdateDisplay()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            var tomorrow = now.AddDays(1);
            var yi = LunarHelper.GetDayYi(tomorrow);
            var ji = LunarHelper.GetDayJi(tomorrow);
            YiLabelText = _settings.YiLabel;
            YiValueText = yi.Length == 0 ? "" : string.Join("、", yi);
            JiLabelText = _settings.JiLabel;
            JiValueText = ji.Length == 0 ? "" : string.Join("、", ji);
        }
        catch { }
    }

    private async System.Threading.Tasks.Task UpdateDisplayAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            var tomorrow = now.AddDays(1);
            var yi = LunarHelper.GetDayYi(tomorrow);
            var ji = LunarHelper.GetDayJi(tomorrow);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                YiLabelText = _settings.YiLabel;
                YiValueText = yi.Length == 0 ? "" : string.Join("、", yi);
                JiLabelText = _settings.JiLabel;
                JiValueText = ji.Length == 0 ? "" : string.Join("、", ji);
            });
        }
        catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _settings.PropertyChanged -= OnSettingsChanged;
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
