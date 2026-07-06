using System;
using System.Collections.Generic;
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
    private long _lastFrameTime;
    private double _currentFps;
    private readonly List<double> _fpsSamples = new();
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
                _fpsTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(SampleIntervalMs), DispatcherPriority.Background, OnFpsTimerTick);
                _fpsTimer.Start();
                _isRunning = true;
            }, DispatcherPriority.Background);
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
                _isRunning = false;
            }, DispatcherPriority.Background);
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
        _lastFrameTime = 0;
    }

    private void OnFpsTimerTick(object? sender, EventArgs e)
    {
        if (!_isRunning || _isPaused) return;

        try
        {
            var currentTime = DateTime.Now.Ticks;

            if (_lastFrameTime > 0)
            {
                var deltaTicks = currentTime - _lastFrameTime;
                if (deltaTicks > 0)
                {
                    var deltaSeconds = deltaTicks / (double)TimeSpan.TicksPerSecond;
                    _currentFps = 1.0 / deltaSeconds;

                    _fpsSamples.Add(_currentFps);
                    while (_fpsSamples.Count > MaxWindowSamples)
                    {
                        _fpsSamples.RemoveAt(0);
                    }

                    double maxFps = _fpsSamples.Max();
                    double minFps = _fpsSamples.Min();
                    double avgFps = _fpsSamples.Average();
                    double low1Fps = CalculateLow1Percent();

                    FpsDataUpdated?.Invoke(DateTime.Now, _currentFps, maxFps, avgFps, minFps, low1Fps);
                }
            }

            _lastFrameTime = currentTime;
        }
        catch (Exception)
        {
        }
    }

    private double CalculateLow1Percent()
    {
        if (_fpsSamples.Count == 0)
            return 0;

        var sorted = _fpsSamples.OrderBy(f => f).ToList();
        var count = (int)Math.Ceiling(sorted.Count * 0.01);

        if (count == 0)
            count = 1;

        return sorted.Take(count).Average();
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
            }, DispatcherPriority.Background);
        }
        catch
        {
        }
    }
}