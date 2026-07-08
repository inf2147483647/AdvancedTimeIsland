using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia.Media;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class FpsMonitorViewModel : INotifyPropertyChanged, IDisposable
{
    private const int WindowSeconds = 10;
    private const int MaxWindowSamples = 600;

    public static event Action<DateTime, double, double, double, double, double>? FpsDataUpdated;

    private readonly FpsMonitorSettings _settings;
    private readonly Action<string>? _updateLabelFontColor;
    private readonly Action<double>? _updateLabelFontSize;
    private readonly Action<IBrush>? _updateFpsForeground;
    private readonly Action<IBrush>? _updateMaxForeground;
    private readonly Action<IBrush>? _updateAvgForeground;
    private readonly Action<IBrush>? _updateMinForeground;
    private readonly Action<IBrush>? _updateLow1Foreground;
    private readonly Action<double>? _updateValueFontSize;
    private bool _isDisposed;
    private string _fpsText = "0.0";
    private string _maxText = "0.0";
    private string _avgText = "0.0";
    private string _minText = "0.0";
    private string _low1Text = "0.0";
    private double _currentFps = 0;
    private readonly List<double> _fpsSamples = new();
    private double _currentMaxFps;
    private double _currentMinFps = double.MaxValue;
    private double _currentSumFps;
    private SortedList<double, int> _sortedFpsCounts = new();

    public FpsMonitorViewModel(FpsMonitorSettings settings,
        Action<string>? updateLabelFontColor = null,
        Action<double>? updateLabelFontSize = null,
        Action<IBrush>? updateFpsForeground = null,
        Action<IBrush>? updateMaxForeground = null,
        Action<IBrush>? updateAvgForeground = null,
        Action<IBrush>? updateMinForeground = null,
        Action<IBrush>? updateLow1Foreground = null,
        Action<double>? updateValueFontSize = null)
    {
        _settings = settings;
        _updateLabelFontColor = updateLabelFontColor;
        _updateLabelFontSize = updateLabelFontSize;
        _updateFpsForeground = updateFpsForeground;
        _updateMaxForeground = updateMaxForeground;
        _updateAvgForeground = updateAvgForeground;
        _updateMinForeground = updateMinForeground;
        _updateLow1Foreground = updateLow1Foreground;
        _updateValueFontSize = updateValueFontSize;

        _settings.PropertyChanged += OnSettingsChanged;
    }

    public void UpdateFps(double fps)
    {
        try
        {
            _currentFps = fps;
            _fpsSamples.Add(fps);
            _currentSumFps += fps;
            _currentMaxFps = Math.Max(_currentMaxFps, fps);
            _currentMinFps = Math.Min(_currentMinFps, fps);

            if (_sortedFpsCounts.TryGetValue(fps, out int count))
                _sortedFpsCounts[fps] = count + 1;
            else
                _sortedFpsCounts[fps] = 1;

            while (_fpsSamples.Count > MaxWindowSamples)
            {
                var removed = _fpsSamples[0];
                _fpsSamples.RemoveAt(0);
                _currentSumFps -= removed;

                _sortedFpsCounts[removed]--;
                if (_sortedFpsCounts[removed] == 0)
                    _sortedFpsCounts.Remove(removed);

                if (_fpsSamples.Count > 0)
                {
                    if (removed >= _currentMaxFps)
                        _currentMaxFps = _fpsSamples.Max();
                    if (removed <= _currentMinFps)
                        _currentMinFps = _fpsSamples.Min();
                }
            }

            double maxFps = _currentMaxFps;
            double minFps = _currentMinFps;
            double avgFps = _fpsSamples.Count > 0 ? _currentSumFps / _fpsSamples.Count : 0;
            double low1Fps = CalculateLow1PercentFast();

            var fpsStr = fps.ToString("F1");
            var maxStr = maxFps.ToString("F1");
            var avgStr = avgFps.ToString("F1");
            var minStr = minFps.ToString("F1");
            var low1Str = low1Fps.ToString("F1");

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                FpsText = fpsStr;
                MaxText = maxStr;
                AvgText = avgStr;
                MinText = minStr;
                Low1Text = low1Str;

                _updateFpsForeground?.Invoke(GetFpsBrush(fps));
                _updateMaxForeground?.Invoke(GetFpsBrush(maxFps));
                _updateAvgForeground?.Invoke(GetFpsBrush(avgFps));
                _updateMinForeground?.Invoke(GetFpsBrush(minFps));
                _updateLow1Foreground?.Invoke(GetFpsBrush(low1Fps));
            });

            FpsDataUpdated?.Invoke(DateTime.Now, fps, maxFps, avgFps, minFps, low1Fps);
        }
        catch (Exception)
        {
        }
    }

    private double CalculateLow1PercentFast()
    {
        if (_sortedFpsCounts.Count == 0)
            return 0;

        var totalCount = _fpsSamples.Count;
        var targetCount = (int)Math.Ceiling(totalCount * 0.01);
        if (targetCount == 0)
            targetCount = 1;

        double sum = 0;
        int count = 0;

        foreach (var kvp in _sortedFpsCounts)
        {
            var take = Math.Min(kvp.Value, targetCount - count);
            sum += kvp.Key * take;
            count += take;

            if (count >= targetCount)
                break;
        }

        return count > 0 ? sum / count : 0;
    }

    public static IBrush GetFpsBrush(double fps)
    {
        if (fps >= 30)
            return new SolidColorBrush(Color.FromRgb(0, 255, 0));
        if (fps >= 20)
            return new SolidColorBrush(Color.FromRgb(255, 255, 0));
        return new SolidColorBrush(Color.FromRgb(255, 0, 0));
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FpsMonitorSettings.LabelFontColor))
        {
            _updateLabelFontColor?.Invoke(_settings.LabelFontColor);
        }
        else if (e.PropertyName == nameof(FpsMonitorSettings.LabelFontSize))
        {
            _updateLabelFontSize?.Invoke(_settings.LabelFontSize);
        }
        else if (e.PropertyName == nameof(FpsMonitorSettings.ValueFontSize))
        {
            _updateValueFontSize?.Invoke(_settings.ValueFontSize);
        }
    }

    public string FpsText
    {
        get => _fpsText;
        private set
        {
            if (_fpsText != value)
            {
                _fpsText = value;
                OnPropertyChanged();
            }
        }
    }

    public string MaxText
    {
        get => _maxText;
        private set
        {
            if (_maxText != value)
            {
                _maxText = value;
                OnPropertyChanged();
            }
        }
    }

    public string AvgText
    {
        get => _avgText;
        private set
        {
            if (_avgText != value)
            {
                _avgText = value;
                OnPropertyChanged();
            }
        }
    }

    public string MinText
    {
        get => _minText;
        private set
        {
            if (_minText != value)
            {
                _minText = value;
                OnPropertyChanged();
            }
        }
    }

    public string Low1Text
    {
        get => _low1Text;
        private set
        {
            if (_low1Text != value)
            {
                _low1Text = value;
                OnPropertyChanged();
            }
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
    }
}
