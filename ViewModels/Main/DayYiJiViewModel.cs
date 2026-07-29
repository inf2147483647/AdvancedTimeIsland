using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class DayYiJiViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly DayYiJiSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateYiLabelFontColor;
    private readonly Action<string> _updateJiLabelFontColor;
    private readonly Action<double> _updateYiLabelFontSize;
    private readonly Action<double> _updateYiValueFontSize;
    private readonly Action<double> _updateJiLabelFontSize;
    private readonly Action<double> _updateJiValueFontSize;
    private string _yiLabelText = string.Empty;
    private string _yiValueText = string.Empty;
    private string _jiLabelText = string.Empty;
    private string _jiValueText = string.Empty;
    private bool _isDisposed;

    public DayYiJiViewModel(TimeBaseService timeBaseService, DayYiJiSettings settings, 
        Action<string> updateYiLabelFontColor = null, Action<string> updateJiLabelFontColor = null,
        Action<double> updateYiLabelFontSize = null, Action<double> updateYiValueFontSize = null,
        Action<double> updateJiLabelFontSize = null, Action<double> updateJiValueFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateYiLabelFontColor = updateYiLabelFontColor;
        _updateJiLabelFontColor = updateJiLabelFontColor;
        _updateYiLabelFontSize = updateYiLabelFontSize;
        _updateYiValueFontSize = updateYiValueFontSize;
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
        if (e.PropertyName == nameof(DayYiJiSettings.YiLabelFontColor) ||
            e.PropertyName == nameof(DayYiJiSettings.YiLabelEnableCustomFontColor))
        {
            _updateYiLabelFontColor?.Invoke(_settings.YiLabelFontColor);
        }
        if (e.PropertyName == nameof(DayYiJiSettings.JiLabelFontColor) ||
            e.PropertyName == nameof(DayYiJiSettings.JiLabelEnableCustomFontColor))
        {
            _updateJiLabelFontColor?.Invoke(_settings.JiLabelFontColor);
        }
        if (e.PropertyName == nameof(DayYiJiSettings.YiLabelFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.YiLabelEnableCustomFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.YiLabelFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.YiLabelEnableCustomFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.YiLabelFontWeight) ||
            e.PropertyName == nameof(DayYiJiSettings.YiLabelEnableCustomFontWeight))
        {
            _updateYiLabelFontSize?.Invoke(_settings.YiLabelEnableCustomFontSize ? _settings.YiLabelFontSize : 0);
        }
        if (e.PropertyName == nameof(DayYiJiSettings.YiValueFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.YiValueEnableCustomFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.YiValueFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.YiValueEnableCustomFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.YiValueFontWeight) ||
            e.PropertyName == nameof(DayYiJiSettings.YiValueEnableCustomFontWeight))
        {
            _updateYiValueFontSize?.Invoke(_settings.YiValueEnableCustomFontSize ? _settings.YiValueFontSize : 0);
        }
        if (e.PropertyName == nameof(DayYiJiSettings.JiLabelFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.JiLabelEnableCustomFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.JiLabelFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.JiLabelEnableCustomFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.JiLabelFontWeight) ||
            e.PropertyName == nameof(DayYiJiSettings.JiLabelEnableCustomFontWeight))
        {
            _updateJiLabelFontSize?.Invoke(_settings.JiLabelEnableCustomFontSize ? _settings.JiLabelFontSize : 0);
        }
        if (e.PropertyName == nameof(DayYiJiSettings.JiValueFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.JiValueEnableCustomFontSize) ||
            e.PropertyName == nameof(DayYiJiSettings.JiValueFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.JiValueEnableCustomFontFamily) ||
            e.PropertyName == nameof(DayYiJiSettings.JiValueFontWeight) ||
            e.PropertyName == nameof(DayYiJiSettings.JiValueEnableCustomFontWeight))
        {
            _updateJiValueFontSize?.Invoke(_settings.JiValueEnableCustomFontSize ? _settings.JiValueFontSize : 0);
        }
    }

    public string YiLabelText
    {
        get => _yiLabelText;
        private set
        {
            if (_yiLabelText != value)
            {
                _yiLabelText = value;
                OnPropertyChanged();
            }
        }
    }

    public string YiValueText
    {
        get => _yiValueText;
        private set
        {
            if (_yiValueText != value)
            {
                _yiValueText = value;
                OnPropertyChanged();
            }
        }
    }

    public string JiLabelText
    {
        get => _jiLabelText;
        private set
        {
            if (_jiLabelText != value)
            {
                _jiLabelText = value;
                OnPropertyChanged();
            }
        }
    }

    public string JiValueText
    {
        get => _jiValueText;
        private set
        {
            if (_jiValueText != value)
            {
                _jiValueText = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _ = UpdateDisplayAsync();
    }

    private void UpdateDisplay()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            var yi = LunarHelper.GetDayYi(now);
            var ji = LunarHelper.GetDayJi(now);
            
            YiLabelText = "今日宜：";
            YiValueText = yi.Length == 0 ? "无" : string.Join("、", yi);
            JiLabelText = "今日忌：";
            JiValueText = ji.Length == 0 ? "无" : string.Join("、", ji);
        }
        catch (Exception)
        {
        }
    }

    private async System.Threading.Tasks.Task UpdateDisplayAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            var yi = LunarHelper.GetDayYi(now);
            var ji = LunarHelper.GetDayJi(now);
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                YiLabelText = "今日宜：";
                YiValueText = yi.Length == 0 ? "无" : string.Join("、", yi);
                JiLabelText = "今日忌：";
                JiValueText = ji.Length == 0 ? "无" : string.Join("、", ji);
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

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _settings.PropertyChanged -= OnSettingsChanged;
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
