using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;
using Lunar;

namespace AdvancedTimeIsland.ViewModels.Main;

public class JieQiViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly JieQiSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateLabelFontColor;
    private readonly Action<double> _updateLabelFontSize;
    private readonly Action<string> _updateValueFontColor;
    private readonly Action<double> _updateValueFontSize;
    private string _labelText = string.Empty;
    private string _valueText = string.Empty;
    private bool _isDisposed;

    public JieQiViewModel(TimeBaseService timeBaseService, JieQiSettings settings, 
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
        if (e.PropertyName == nameof(JieQiSettings.LabelFontColor))
        {
            _updateLabelFontColor?.Invoke(_settings.LabelFontColor);
        }
        else if (e.PropertyName == nameof(JieQiSettings.LabelFontSize))
        {
            _updateLabelFontSize?.Invoke(_settings.LabelFontSize);
        }
        else if (e.PropertyName == nameof(JieQiSettings.ValueFontColor))
        {
            _updateValueFontColor?.Invoke(_settings.ValueFontColor);
        }
        else if (e.PropertyName == nameof(JieQiSettings.ValueFontSize))
        {
            _updateValueFontSize?.Invoke(_settings.ValueFontSize);
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
            var jieQiInfo = GetCurrentOrRecentJieQi(now);
            LabelText = "当前节气：";
            ValueText = $"{jieQiInfo.Name} ({jieQiInfo.DateRange})";
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
            var jieQiInfo = GetCurrentOrRecentJieQi(now);
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                LabelText = "当前节气：";
                ValueText = $"{jieQiInfo.Name} ({jieQiInfo.DateRange})";
            });
        }
        catch (Exception)
        {
        }
    }

    private (string Name, string DateRange) GetCurrentOrRecentJieQi(DateTime date)
    {
        var solar = Solar.FromDate(date);
        var jieQi = solar.Lunar.JieQi;
        
        if (!string.IsNullOrEmpty(jieQi))
        {
            return (jieQi, GetJieQiDateRange(jieQi));
        }

        for (int i = 1; i <= 60; i++)
        {
            var prevDate = date.AddDays(-i);
            var prevSolar = Solar.FromDate(prevDate);
            var prevJieQi = prevSolar.Lunar.JieQi;
            if (!string.IsNullOrEmpty(prevJieQi))
            {
                return (prevJieQi, GetJieQiDateRange(prevJieQi));
            }
        }

        return ("立春", "2.3-2.4");
    }

    private string GetJieQiDateRange(string jieQi)
    {
        return jieQi switch
        {
            "立春" => "2.3-2.4",
            "雨水" => "2.18-2.19",
            "惊蛰" => "3.5-3.6",
            "春分" => "3.20-3.21",
            "清明" => "4.4-4.6",
            "谷雨" => "4.19-4.20",
            "立夏" => "5.5-5.6",
            "小满" => "5.20-5.21",
            "芒种" => "6.5-6.6",
            "夏至" => "6.21-6.22",
            "小暑" => "7.6-7.7",
            "大暑" => "7.22-7.23",
            "立秋" => "8.7-8.8",
            "处暑" => "8.22-8.23",
            "白露" => "9.7-9.8",
            "秋分" => "9.22-9.23",
            "寒露" => "10.8-10.9",
            "霜降" => "10.23-10.24",
            "立冬" => "11.7-11.8",
            "小雪" => "11.22-11.23",
            "大雪" => "12.6-12.7",
            "冬至" => "12.21-12.22",
            "小寒" => "1.5-1.6",
            "大寒" => "1.19-1.20",
            _ => ""
        };
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
