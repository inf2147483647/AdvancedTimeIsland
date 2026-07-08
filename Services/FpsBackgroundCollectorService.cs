using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

public class FpsBackgroundCollectorService : IHostedService, IDisposable
{
    private const int WindowSeconds = 10;
    private const int MaxWindowSamples = 600;
    private const int SampleIntervalMs = 16;

    private readonly ILogger<FpsBackgroundCollectorService> _logger;
    private DispatcherTimer? _fpsTimer;
    private readonly Stopwatch _stopwatch = new();
    private double _lastFrameTimestamp;
    private double _currentFps;
    private readonly List<double> _fpsSamples = new();
    private double _currentMaxFps;
    private double _currentMinFps = double.MaxValue;
    private double _currentSumFps;
    private SortedList<double, int> _sortedFpsCounts = new();
    private bool _isDisposed;
    private bool _isRunning;
    private bool _isPaused;

    public static event Action<DateTime, double, double, double, double, double>? FpsDataUpdated;

    public bool IsPaused => _isPaused;

    public FpsBackgroundCollectorService(ILogger<FpsBackgroundCollectorService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FpsBackgroundCollectorService is starting.");

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _stopwatch.Restart();
                _lastFrameTimestamp = 0;
                _fpsTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(SampleIntervalMs), DispatcherPriority.Background, OnFpsTimerTick);
                _fpsTimer.Start();
                _isRunning = true;
            }, DispatcherPriority.Background);

            ViewModels.Settings.FpsSampler.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start FpsBackgroundCollectorService.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FpsBackgroundCollectorService is stopping.");

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _fpsTimer?.Stop();
                _fpsTimer = null;
                _stopwatch.Stop();
                _isRunning = false;
            }, DispatcherPriority.Background);

            ViewModels.Settings.FpsSampler.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop FpsBackgroundCollectorService.");
        }
    }

    public void Pause()
    {
        _isPaused = true;
    }

    public void Resume()
    {
        _isPaused = false;
        _lastFrameTimestamp = 0;
    }

    private void OnFpsTimerTick(object? sender, EventArgs e)
    {
        if (!_isRunning || _isPaused) return;

        try
        {
            var currentTimestamp = _stopwatch.Elapsed.TotalSeconds;

            if (_lastFrameTimestamp > 0)
            {
                var deltaSeconds = currentTimestamp - _lastFrameTimestamp;
                if (deltaSeconds > 0)
                {
                    _currentFps = 1.0 / deltaSeconds;

                    _fpsSamples.Add(_currentFps);
                    _currentSumFps += _currentFps;
                    _currentMaxFps = Math.Max(_currentMaxFps, _currentFps);
                    _currentMinFps = Math.Min(_currentMinFps, _currentFps);

                    if (_sortedFpsCounts.TryGetValue(_currentFps, out int count))
                        _sortedFpsCounts[_currentFps] = count + 1;
                    else
                        _sortedFpsCounts[_currentFps] = 1;

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

                    FpsDataUpdated?.Invoke(DateTime.Now, _currentFps, maxFps, avgFps, minFps, low1Fps);
                }
            }

            _lastFrameTimestamp = currentTimestamp;
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

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        try
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _fpsTimer?.Stop();
                _fpsTimer = null;
                _stopwatch.Stop();
            }, DispatcherPriority.Background);
        }
        catch
        {
        }
    }
}