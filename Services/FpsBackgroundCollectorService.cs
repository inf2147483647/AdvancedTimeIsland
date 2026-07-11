using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

public class FpsBackgroundCollectorService : IHostedService, IDisposable
{
    private const int WindowSeconds = 10;
    private const int RetryDelayMs = 100;
    private const int FpsReportIntervalMs = 10;

    private readonly ILogger<FpsBackgroundCollectorService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _waitForTopLevelTask;
    private Task? _reportTask;
    private double _currentFps;
    private readonly List<(DateTime Time, double Fps)> _fpsSamples = new();
    private readonly List<double> _rawSamplesBuffer = new(3);
    private bool _isDisposed;
    private bool _isRunning;
    private bool _isPaused;
    private IDisposable? _renderTimerSubscription;
    private TopLevel? _currentTopLevel;

    private bool _useInternalFps;
    private int _frameCount;
    private DateTime _lastReportTime;

    private int _oneSecondFrameCount;
    private DateTime _lastOneSecondReportTime;

    public static event Action<DateTime, double, double, double, double, double, double>? FpsDataUpdated;

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
            _isRunning = true;
            _cts = new CancellationTokenSource();
            _frameCount = 0;
            _lastReportTime = DateTime.Now;
            _oneSecondFrameCount = 0;
            _lastOneSecondReportTime = DateTime.Now;

            _waitForTopLevelTask = WaitForTopLevelAndStart(_cts.Token);

            ViewModels.Settings.FpsSampler.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start FpsBackgroundCollectorService.");
        }
    }

    private async Task WaitForTopLevelAndStart(CancellationToken ct)
    {
        try
        {
            TopLevel? topLevel = null;
            while (!ct.IsCancellationRequested && _isRunning)
            {
                topLevel = await GetTopLevelAsync(ct);
                if (topLevel != null) break;

                try
                {
                    await Task.Delay(RetryDelayMs, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            if (topLevel == null || ct.IsCancellationRequested || !_isRunning) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_isRunning || ct.IsCancellationRequested) return;
                StartCollecting(topLevel);
            }, DispatcherPriority.Background);

            _reportTask = RunReportLoop(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WaitForTopLevelAndStart.");
        }
    }

    private void StartCollecting(TopLevel topLevel)
    {
        _currentTopLevel = topLevel;

        var fpsValue = RenderTimerHelper.GetFps(topLevel);
        if (fpsValue.HasValue && fpsValue.Value > 0)
        {
            _useInternalFps = true;
            _logger.LogInformation("Using internal FPS property for FPS collection.");
            return;
        }

        _useInternalFps = false;

        _renderTimerSubscription = RenderTimerHelper.SubscribeTick(topLevel, OnRenderTimerTick);
        if (_renderTimerSubscription != null)
        {
            _logger.LogInformation("Using RenderTimer (counting method) for FPS collection.");
            return;
        }

        topLevel.RequestAnimationFrame(OnAnimationFrame);
        _logger.LogInformation("Using RequestAnimationFrame (counting method) for FPS collection.");
    }

    private void OnRenderTimerTick(TimeSpan timestamp)
    {
        if (!_isRunning || _isPaused) return;
        _frameCount++;
        _oneSecondFrameCount++;
    }

    private void OnAnimationFrame(TimeSpan timestamp)
    {
        if (!_isRunning || _isPaused) return;
        _frameCount++;
        _oneSecondFrameCount++;

        if (_currentTopLevel != null && _isRunning && !_isPaused)
        {
            _currentTopLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }

    private async Task RunReportLoop(CancellationToken ct)
    {
        try
        {
            double oneSecondFrameCount = 0;

            while (!ct.IsCancellationRequested && _isRunning)
            {
                await Task.Delay(FpsReportIntervalMs, ct);

                if (_isPaused) continue;

                double fps;

                if (_useInternalFps && _currentTopLevel != null)
                {
                    var fpsValue = RenderTimerHelper.GetFps(_currentTopLevel);
                    fps = fpsValue ?? 0;
                }
                else
                {
                    var now = DateTime.Now;
                    var deltaSeconds = (now - _lastReportTime).TotalSeconds;
                    if (deltaSeconds > 0)
                    {
                        fps = _frameCount / deltaSeconds;
                    }
                    else
                    {
                        fps = 0;
                    }
                    _frameCount = 0;
                    _lastReportTime = now;
                }

                var now2 = DateTime.Now;
                var oneSecondDelta = (now2 - _lastOneSecondReportTime).TotalSeconds;
                if (oneSecondDelta >= 1.0)
                {
                    if (_oneSecondFrameCount > 0)
                    {
                        oneSecondFrameCount = _oneSecondFrameCount;
                    }
                    else
                    {
                        if (oneSecondDelta > 0)
                        {
                            oneSecondFrameCount = 1.0 / oneSecondDelta;
                        }
                        else
                        {
                            oneSecondFrameCount = 0;
                        }
                    }
                    _oneSecondFrameCount = 0;
                    _lastOneSecondReportTime = now2;
                }

                ProcessFrameValue(fps, oneSecondFrameCount);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in report loop.");
        }
    }

    private void ProcessFrameValue(double fps, double oneSecondFrameCount)
    {
        if (fps <= 0 || double.IsNaN(fps) || double.IsInfinity(fps)) return;

        _rawSamplesBuffer.Add(fps);

        if (_rawSamplesBuffer.Count < 3)
            return;

        var avgFps = _rawSamplesBuffer.Average();
        _currentFps = avgFps;
        _rawSamplesBuffer.Clear();

        var now = DateTime.Now;

        _fpsSamples.Add((now, avgFps));

        var windowStart = now - TimeSpan.FromSeconds(WindowSeconds);
        while (_fpsSamples.Count > 0 && _fpsSamples[0].Time < windowStart)
        {
            _fpsSamples.RemoveAt(0);
        }

        var currentSamples = _fpsSamples.Select(s => s.Fps).ToList();
        double maxFps = currentSamples.Count > 0 ? currentSamples.Max() : 0;
        double minFps = currentSamples.Count > 0 ? currentSamples.Min() : 0;
        double overallAvgFps = currentSamples.Count > 0 ? currentSamples.Average() : 0;
        double low1Fps = CalculateLow1Percent(currentSamples);

        FpsDataUpdated?.Invoke(now, avgFps, maxFps, overallAvgFps, minFps, low1Fps, oneSecondFrameCount);
    }

    private double CalculateLow1Percent(List<double> samples)
    {
        if (samples.Count == 0)
            return 0;

        var sorted = samples.OrderBy(f => f).ToList();
        var targetCount = (int)Math.Ceiling(sorted.Count * 0.01);
        if (targetCount == 0)
            targetCount = 1;
        if (targetCount >= sorted.Count)
            targetCount = sorted.Count;

        double sum = 0;
        for (int i = 0; i < targetCount; i++)
        {
            sum += sorted[i];
        }
        return sum / targetCount;
    }

    private Task<TopLevel?> GetTopLevelAsync(CancellationToken ct)
    {
        return Dispatcher.UIThread.InvokeAsync<TopLevel?>(() =>
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow != null)
                        return desktop.MainWindow;

                    if (desktop.Windows?.Count > 0)
                        return TopLevel.GetTopLevel(desktop.Windows[0]);
                }

                if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                {
                    if (singleView.MainView != null)
                        return TopLevel.GetTopLevel(singleView.MainView);
                }

                var lifetime = Application.Current?.ApplicationLifetime;
                if (lifetime != null)
                {
                    var lifetimeType = lifetime.GetType();
                    var mainWindowProp = lifetimeType.GetProperty("MainWindow", BindingFlags.Public | BindingFlags.Instance);
                    if (mainWindowProp != null)
                    {
                        var window = mainWindowProp.GetValue(lifetime) as Window;
                        if (window != null) return window;
                    }

                    var windowsProp = lifetimeType.GetProperty("Windows", BindingFlags.Public | BindingFlags.Instance);
                    if (windowsProp != null)
                    {
                        var windows = windowsProp.GetValue(lifetime) as IEnumerable<Window>;
                        if (windows != null)
                        {
                            foreach (var w in windows)
                            {
                                if (w != null) return w;
                            }
                        }
                    }

                    var mainViewProp = lifetimeType.GetProperty("MainView", BindingFlags.Public | BindingFlags.Instance);
                    if (mainViewProp != null)
                    {
                        var view = mainViewProp.GetValue(lifetime) as Visual;
                        if (view != null) return TopLevel.GetTopLevel(view);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }).GetTask()!;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FpsBackgroundCollectorService is stopping.");

        try
        {
            _isRunning = false;

            if (_cts != null)
            {
                _cts.Cancel();
                try
                {
                    if (_waitForTopLevelTask != null)
                    {
                        await _waitForTopLevelTask;
                    }
                    if (_reportTask != null)
                    {
                        await _reportTask;
                    }
                }
                catch
                {
                }
                _cts.Dispose();
                _cts = null;
            }

            _renderTimerSubscription?.Dispose();
            _renderTimerSubscription = null;
            _currentTopLevel = null;

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
        _frameCount = 0;
        _lastReportTime = DateTime.Now;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        try
        {
            _isRunning = false;

            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _renderTimerSubscription?.Dispose();
            _renderTimerSubscription = null;
            _currentTopLevel = null;
        }
        catch
        {
        }
    }
}
