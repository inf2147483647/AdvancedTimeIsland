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
    private readonly Action<string> _updateLabelFontColor;
    private readonly Action<double> _updateLabelFontSize;
    private readonly Action<double> _updateValueFontSize;
    private string _yiLabelText = string.Empty;
    private string _yiValueText = string.Empty;
    private string _jiLabelText = string.Empty;
    private string _jiValueText = string.Empty;
    private bool _isDisposed;

    public DayYiJiViewModel(TimeBaseService timeBaseService, DayYiJiSettings settings, 
        Action<string> updateLabelFontColor = null, Action<double> updateLabelFontSize = null,
        Action<double> updateValueFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateLabelFontColor = updateLabelFontColor;
        _updateLabelFontSize = updateLabelFontSize;
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
        if (e.PropertyName == nameof(DayYiJiSettings.LabelFontColor))
        {
            _updateLabelFontColor?.Invoke(_settings.LabelFontColor);
        }
        else if (e.PropertyName == nameof(DayYiJiSettings.LabelFontSize))
        {
            _updateLabelFontSize?.Invoke(_settings.LabelFontSize);
        }
        else if (e.PropertyName == nameof(DayYiJiSettings.ValueFontSize))
        {
            _updateValueFontSize?.Invoke(_settings.ValueFontSize);
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
