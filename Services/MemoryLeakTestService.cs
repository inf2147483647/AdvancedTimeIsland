using System;
using System.Collections.Generic;
using System.Threading;

namespace AdvancedTimeIsland.Services;

public class MemoryLeakTestService
{
    private static MemoryLeakTestService? _instance;
    private static readonly object _lock = new object();

    public static MemoryLeakTestService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new MemoryLeakTestService();
                }
            }
            return _instance;
        }
    }

    private MemoryLeakTestService()
    {
    }

    private Thread? _leakThread;
    private volatile bool _isRunning;
    private volatile bool _isPaused;
    private readonly List<byte[]> _buffers = new List<byte[]>();
    private long _leakedBytes;
    private double _leakRate = 1024;
    private string _leakUnit = "KiB";
    private readonly object _stateLock = new object();

    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public long LeakedBytes => Volatile.Read(ref _leakedBytes);
    public double LeakRate
    {
        get
        {
            lock (_stateLock)
            {
                return _leakRate;
            }
        }
    }
    public string LeakUnit
    {
        get
        {
            lock (_stateLock)
            {
                return _leakUnit;
            }
        }
    }

    public event EventHandler? LeakUpdated;

    public void Start()
    {
        if (_isRunning)
        {
            _isPaused = false;
            return;
        }

        _isRunning = true;
        _isPaused = false;
        _leakThread = new Thread(LeakLoop)
        {
            IsBackground = true,
            Name = "MemoryLeakTest"
        };
        _leakThread.Start();
    }

    public void Pause()
    {
        _isPaused = true;
    }

    public void Stop()
    {
        _isRunning = false;
        _isPaused = false;
        _leakThread?.Join(1000);
        _leakThread = null;
    }

    public void Clear()
    {
        Stop();
        lock (_buffers)
        {
            _buffers.Clear();
        }
        _leakedBytes = 0;
        LeakUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void SetLeakRate(double rate, string unit)
    {
        lock (_stateLock)
        {
            _leakRate = rate;
            _leakUnit = unit;
        }
    }

    private void LeakLoop()
    {
        while (_isRunning)
        {
            if (_isPaused)
            {
                Thread.Sleep(100);
                continue;
            }

            var bytesToLeak = CalculateBytesToLeak();
            if (bytesToLeak > 0)
            {
                var buffer = new byte[bytesToLeak];
                lock (_buffers)
                {
                    _buffers.Add(buffer);
                }
                Volatile.Write(ref _leakedBytes, Volatile.Read(ref _leakedBytes) + bytesToLeak);
                LeakUpdated?.Invoke(this, EventArgs.Empty);
            }

            Thread.Sleep(1000);
        }
    }

    private long CalculateBytesToLeak()
    {
        lock (_stateLock)
        {
            if (_leakRate <= 0)
                return 0;

            switch (_leakUnit)
            {
                case "Byte":
                    return (long)_leakRate;
                case "KiB":
                    return (long)(_leakRate * 1024);
                case "MiB":
                    return (long)(_leakRate * 1024 * 1024);
                default:
                    return 0;
            }
        }
    }

    public string FormatMemorySize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} Byte";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KiB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F2} MiB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GiB";
    }
}