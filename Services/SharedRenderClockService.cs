using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

public class SharedRenderClockService : IHostedService, IDisposable
{
    private const int NormalIntervalMs = 200;
    private const int HighFrequencyIntervalMs = 16;

    private static SharedRenderClockService? _instance;
    private static readonly object _instanceLock = new();

    public static SharedRenderClockService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        throw new InvalidOperationException("SharedRenderClockService must be created via DI container first.");
                    }
                }
            }
            return _instance;
        }
    }

    private readonly ILogger<SharedRenderClockService>? _logger;
    private DispatcherTimer? _timer;
    private readonly List<(Action<DateTime> Callback, bool HighFrequency)> _subscribers = new();
    private readonly object _lockObj = new();
    private bool _isRunning;
    private bool _isDisposed;
    private int _highFrequencySubscriberCount;

    public event Action<DateTime>? Tick;

    public SharedRenderClockService(ILogger<SharedRenderClockService>? logger)
    {
        _logger = logger;
        lock (_instanceLock)
        {
            _instance ??= this;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_logger != null)
            _logger.LogInformation("SharedRenderClockService is starting.");
        _isRunning = true;
        StartTimer(NormalIntervalMs);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_logger != null)
            _logger.LogInformation("SharedRenderClockService is stopping.");
        _isRunning = false;
        _timer?.Stop();
        return Task.CompletedTask;
    }

    public void EnsureStarted()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            StartTimer(NormalIntervalMs);
        }
    }

    public IDisposable Subscribe(Action<DateTime> callback, bool highFrequency = false)
    {
        lock (_lockObj)
        {
            _subscribers.Add((callback, highFrequency));
            
            if (highFrequency)
            {
                _highFrequencySubscriberCount++;
                if (_highFrequencySubscriberCount == 1)
                {
                    SwitchToHighFrequency();
                }
            }
        }

        return new SubscriptionToken(this, callback);
    }

    private void Unsubscribe(Action<DateTime> callback)
    {
        lock (_lockObj)
        {
            var toRemove = _subscribers.FindAll(s => ReferenceEquals(s.Callback, callback));
            foreach (var subscriber in toRemove)
            {
                if (subscriber.HighFrequency)
                {
                    _highFrequencySubscriberCount--;
                }
                _subscribers.Remove(subscriber);
            }

            if (_highFrequencySubscriberCount <= 0)
            {
                _highFrequencySubscriberCount = 0;
                SwitchToNormalFrequency();
            }
        }
    }

    private void StartTimer(int intervalMs)
    {
        _timer?.Stop();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(intervalMs), DispatcherPriority.Normal, OnTimerTick);
        _timer.Start();
    }

    private void SwitchToHighFrequency()
    {
        if (_timer != null && _isRunning)
        {
            StartTimer(HighFrequencyIntervalMs);
        }
    }

    private void SwitchToNormalFrequency()
    {
        if (_timer != null && _isRunning)
        {
            StartTimer(NormalIntervalMs);
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            var now = DateTime.Now;
            Tick?.Invoke(now);

            List<(Action<DateTime> Callback, bool HighFrequency)> subscribersCopy;
            lock (_lockObj)
            {
                subscribersCopy = new List<(Action<DateTime>, bool)>(_subscribers);
            }

            foreach (var subscriber in subscribersCopy)
            {
                try
                {
                    subscriber.Callback(now);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogError(ex, "Error in subscriber callback");
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger != null)
                _logger.LogError(ex, "Error in timer tick");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _timer?.Stop();
        _timer = null;
        _subscribers.Clear();
    }

    private class SubscriptionToken : IDisposable
    {
        private readonly SharedRenderClockService _service;
        private readonly Action<DateTime> _callback;
        private bool _isDisposed;

        public SubscriptionToken(SharedRenderClockService service, Action<DateTime> callback)
        {
            _service = service;
            _callback = callback;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _service.Unsubscribe(_callback);
        }
    }
}