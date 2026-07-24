using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class XingZuoViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly XingZuoSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateLabelFontColor;
    private readonly Action<double> _updateLabelFontSize;
    private readonly Action<string> _updateValueFontColor;
    private readonly Action<double> _updateValueFontSize;
    private string _labelText = string.Empty;
    private string _valueText = string.Empty;
    private bool _isDisposed;

    public XingZuoViewModel(TimeBaseService timeBaseService, XingZuoSettings settings, 
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
        if (e.PropertyName == nameof(XingZuoSettings.LabelFontColor) ||
            e.PropertyName == nameof(XingZuoSettings.LabelEnableCustomFontColor))
        {
            _updateLabelFontColor?.Invoke(_settings.LabelFontColor);
        }
        if (e.PropertyName == nameof(XingZuoSettings.LabelFontSize) ||
                 e.PropertyName == nameof(XingZuoSettings.LabelEnableCustomFontSize))
        {
            _updateLabelFontSize?.Invoke(_settings.LabelEnableCustomFontSize ? _settings.LabelFontSize : 14);
        }
        if (e.PropertyName == nameof(XingZuoSettings.ValueFontColor) ||
                 e.PropertyName == nameof(XingZuoSettings.ValueEnableCustomFontColor))
        {
            _updateValueFontColor?.Invoke(_settings.ValueFontColor);
        }
        if (e.PropertyName == nameof(XingZuoSettings.ValueFontSize) ||
                 e.PropertyName == nameof(XingZuoSettings.ValueEnableCustomFontSize))
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
            var xingZuo = LunarHelper.GetXingZuo(now);
            var dateRange = GetXingZuoDateRange(now);
            LabelText = "当前星座：";
            ValueText = $"{xingZuo} ({dateRange})";
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
            var xingZuo = LunarHelper.GetXingZuo(now);
            var dateRange = GetXingZuoDateRange(now);
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                LabelText = "当前星座：";
                ValueText = $"{xingZuo} ({dateRange})";
            });
        }
        catch (Exception)
        {
        }
    }

    private string GetXingZuoDateRange(DateTime date)
    {
        var month = date.Month;
        var day = date.Day;

        if ((month == 3 && day >= 21) || (month == 4 && day <= 19)) return "3.21-4.19";
        if ((month == 4 && day >= 20) || (month == 5 && day <= 20)) return "4.20-5.20";
        if ((month == 5 && day >= 21) || (month == 6 && day <= 21)) return "5.21-6.21";
        if ((month == 6 && day >= 22) || (month == 7 && day <= 22)) return "6.22-7.22";
        if ((month == 7 && day >= 23) || (month == 8 && day <= 22)) return "7.23-8.22";
        if ((month == 8 && day >= 23) || (month == 9 && day <= 22)) return "8.23-9.22";
        if ((month == 9 && day >= 23) || (month == 10 && day <= 23)) return "9.23-10.23";
        if ((month == 10 && day >= 24) || (month == 11 && day <= 22)) return "10.24-11.22";
        if ((month == 11 && day >= 23) || (month == 12 && day <= 21)) return "11.23-12.21";
        if ((month == 12 && day >= 22) || (month == 1 && day <= 19)) return "12.22-1.19";
        if ((month == 1 && day >= 20) || (month == 2 && day <= 18)) return "1.20-2.18";
        return "2.19-3.20";
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
