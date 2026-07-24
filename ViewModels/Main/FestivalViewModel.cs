using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class FestivalViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly FestivalSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateLabelFontColor;
    private readonly Action<double> _updateLabelFontSize;
    private readonly Action<string> _updateValueFontColor;
    private readonly Action<double> _updateValueFontSize;
    private string _labelText = string.Empty;
    private string _valueText = string.Empty;
    private bool _isDisposed;

    public FestivalViewModel(TimeBaseService timeBaseService, FestivalSettings settings, 
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
        if (e.PropertyName == nameof(FestivalSettings.LabelFontColor) ||
            e.PropertyName == nameof(FestivalSettings.LabelEnableCustomFontColor))
        {
            _updateLabelFontColor?.Invoke(_settings.LabelFontColor);
        }
        if (e.PropertyName == nameof(FestivalSettings.LabelFontSize) ||
                 e.PropertyName == nameof(FestivalSettings.LabelEnableCustomFontSize))
        {
            _updateLabelFontSize?.Invoke(_settings.LabelEnableCustomFontSize ? _settings.LabelFontSize : 14);
        }
        if (e.PropertyName == nameof(FestivalSettings.ValueFontColor) ||
                 e.PropertyName == nameof(FestivalSettings.ValueEnableCustomFontColor))
        {
            _updateValueFontColor?.Invoke(_settings.ValueFontColor);
        }
        if (e.PropertyName == nameof(FestivalSettings.ValueFontSize) ||
                 e.PropertyName == nameof(FestivalSettings.ValueEnableCustomFontSize))
        {
            _updateValueFontSize?.Invoke(_settings.ValueEnableCustomFontSize ? _settings.ValueFontSize : 14);
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
            var festivals = LunarHelper.GetFestivals(now);
            var festivalsText = festivals.Length == 0 ? "无" : string.Join("、", festivals);
            LabelText = "当前节日：";
            ValueText = festivalsText;
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
            var festivals = LunarHelper.GetFestivals(now);
            var festivalsText = festivals.Length == 0 ? "无" : string.Join("、", festivals);
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                LabelText = "当前节日：";
                ValueText = festivalsText;
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
