using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;
using Lunar;

namespace AdvancedTimeIsland.ViewModels.Main;

public class ShengXiaoViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly ShengXiaoSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateLabelFontColor;
    private readonly Action<double> _updateLabelFontSize;
    private readonly Action<string> _updateValueFontColor;
    private readonly Action<double> _updateValueFontSize;
    private string _labelText = string.Empty;
    private string _valueText = string.Empty;
    private bool _isDisposed;

    public ShengXiaoViewModel(TimeBaseService timeBaseService, ShengXiaoSettings settings, 
        Action<string> updateLabelFontColor = null, Action<double> updateLabelFontSize = null,
        Action<string> updateValueFontColor = null, Action<double> updateValueFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateLabelFontColor = updateLabelFontColor;
        _updateLabelFontSize = updateLabelFontSize;
        _updateValueFontColor = updateValueFontColor;
        _updateValueFontSize = updateValueFontSize;
        
        _settings.PropertyChanged += OnSettingsChanged;
        
        UpdateDisplay();
        
        _updateTimer = new System.Timers.Timer(60000);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShengXiaoSettings.LabelFontColor) ||
            e.PropertyName == nameof(ShengXiaoSettings.LabelEnableCustomFontColor) ||
            e.PropertyName == nameof(ShengXiaoSettings.LabelFontFamily) ||
            e.PropertyName == nameof(ShengXiaoSettings.LabelEnableCustomFontFamily))
        {
            _updateLabelFontColor?.Invoke(_settings.LabelFontColor);
        }
        if (e.PropertyName == nameof(ShengXiaoSettings.LabelFontSize) ||
                 e.PropertyName == nameof(ShengXiaoSettings.LabelEnableCustomFontSize) ||
                 e.PropertyName == nameof(ShengXiaoSettings.LabelFontFamily) ||
                 e.PropertyName == nameof(ShengXiaoSettings.LabelEnableCustomFontFamily) ||
                 e.PropertyName == nameof(ShengXiaoSettings.LabelFontWeight) ||
                 e.PropertyName == nameof(ShengXiaoSettings.LabelEnableCustomFontWeight))
        {
            _updateLabelFontSize?.Invoke(_settings.LabelEnableCustomFontSize ? _settings.LabelFontSize : 0);
        }
        if (e.PropertyName == nameof(ShengXiaoSettings.ValueFontColor) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueEnableCustomFontColor) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueFontFamily) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueEnableCustomFontFamily))
        {
            _updateValueFontColor?.Invoke(_settings.ValueFontColor);
        }
        if (e.PropertyName == nameof(ShengXiaoSettings.ValueFontSize) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueEnableCustomFontSize) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueFontFamily) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueEnableCustomFontFamily) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueFontWeight) ||
                 e.PropertyName == nameof(ShengXiaoSettings.ValueEnableCustomFontWeight))
        {
            _updateValueFontSize?.Invoke(_settings.ValueEnableCustomFontSize ? _settings.ValueFontSize : 0);
        }
    }

    public string LabelText
    {
        get => _labelText;
        private set
        {
            if (_labelText != value)
            {
                _labelText = value;
                OnPropertyChanged();
            }
        }
    }

    public string ValueText
    {
        get => _valueText;
        private set
        {
            if (_valueText != value)
            {
                _valueText = value;
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
            var shengXiao = LunarHelper.GetCurrentShengXiao(now);
            var dateRange = GetShengXiaoDateRange(now, shengXiao);
            LabelText = "当前生肖：";
            ValueText = $"{shengXiao} ({dateRange})";
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
            var shengXiao = LunarHelper.GetCurrentShengXiao(now);
            var dateRange = GetShengXiaoDateRange(now, shengXiao);
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                LabelText = "当前生肖：";
                ValueText = $"{shengXiao} ({dateRange})";
            });
        }
        catch (Exception)
        {
        }
    }

    private string GetShengXiaoDateRange(DateTime now, string shengXiao)
    {
        var lunar = Solar.FromDate(now).Lunar;
        var year = lunar.Year;
        return $"{year}年";
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
