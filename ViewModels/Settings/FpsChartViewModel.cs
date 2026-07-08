using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.ViewModels.Settings;

public static class FpsSampler
{
    private const int InitialCapacity = 10000;
    private const int MaxCapacity = 10000000;
    private static readonly RingBuffer<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)> s_records = new(InitialCapacity);
    private static bool s_isRunning;
    private static bool s_isPaused;

    public static event Action? DataUpdated;

    public static IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)> Records => s_records;

    public static int RecordCount => s_records.Count;

    public static bool IsPaused => s_isPaused;

    public static void Start()
    {
        if (s_isRunning) return;
        s_isRunning = true;

        FpsMonitorViewModel.FpsDataUpdated += OnFpsDataUpdated;
        FpsBackgroundCollectorService.FpsDataUpdated += OnFpsDataUpdated;
    }

    public static void Stop()
    {
        if (!s_isRunning) return;
        s_isRunning = false;

        FpsMonitorViewModel.FpsDataUpdated -= OnFpsDataUpdated;
        FpsBackgroundCollectorService.FpsDataUpdated -= OnFpsDataUpdated;
    }

    public static void Pause()
    {
        s_isPaused = true;
    }

    public static void Resume()
    {
        s_isPaused = false;
    }

    public static void Clear()
    {
        s_records.Clear();
        Dispatcher.UIThread.Post(() => DataUpdated?.Invoke());
    }

    private static void OnFpsDataUpdated(DateTime time, double fps, double max, double avg, double min, double low1)
    {
        if (s_isPaused) return;
        
        try
        {
            s_records.Enqueue((time, fps, max, avg, min, low1));

            while (s_records.Count > MaxCapacity)
            {
                s_records.Dequeue();
            }

            Dispatcher.UIThread.Post(() => DataUpdated?.Invoke());
        }
        catch
        {
        }
    }

    public static void WriteCsvData(Stream stream)
    {
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.WriteLine("Time,fps,max,avg,min,1%min");

        var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

        foreach (var r in s_records)
        {
            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss.fff},{1:F2},{2:F2},{3:F2},{4:F2},{5:F2}",
                r.Time, r.Fps, r.Max, r.Avg, r.Min, r.Low1);
        }
    }
}

public class RingBuffer<T> : IReadOnlyList<T>
{
    private T[] _buffer;
    private int _head;
    private int _tail;
    private int _count;
    private readonly int _maxCapacity;
    private readonly object _lock = new();

    public RingBuffer(int initialCapacity, int maxCapacity = int.MaxValue)
    {
        _buffer = new T[initialCapacity];
        _head = 0;
        _tail = 0;
        _count = 0;
        _maxCapacity = maxCapacity;
    }

    public int Count
    {
        get
        {
            lock (_lock) return _count;
        }
    }

    public T this[int index]
    {
        get
        {
            lock (_lock)
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();
                int bufferIndex = (_head + index) % _buffer.Length;
                return _buffer[bufferIndex];
            }
        }
    }

    public void Enqueue(T item)
    {
        lock (_lock)
        {
            if (_count == _buffer.Length)
            {
                int newCapacity = Math.Min(_buffer.Length * 2, _maxCapacity);
                Resize(newCapacity);
            }

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
        }
    }

    public T Dequeue()
    {
        lock (_lock)
        {
            if (_count == 0)
                throw new InvalidOperationException("Buffer is empty");

            T item = _buffer[_head];
            _buffer[_head] = default!;
            _head = (_head + 1) % _buffer.Length;
            _count--;

            return item;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }

    private void Resize(int newCapacity)
    {
        T[] newBuffer = new T[newCapacity];
        
        if (_count > 0)
        {
            if (_head < _tail)
            {
                Array.Copy(_buffer, _head, newBuffer, 0, _count);
            }
            else
            {
                Array.Copy(_buffer, _head, newBuffer, 0, _buffer.Length - _head);
                Array.Copy(_buffer, 0, newBuffer, _buffer.Length - _head, _tail);
            }
        }

        _buffer = newBuffer;
        _head = 0;
        _tail = _count;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var snapshot = new T[_count];
        lock (_lock)
        {
            for (int i = 0; i < _count; i++)
            {
                int bufferIndex = (_head + i) % _buffer.Length;
                snapshot[i] = _buffer[bufferIndex];
            }
        }
        foreach (var item in snapshot)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class FpsChartViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _isDisposed;

    public static IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)> Records => FpsSampler.Records;

    public int RecordCount => FpsSampler.RecordCount;

    public FpsChartViewModel()
    {
        FpsSampler.Start();
        FpsSampler.DataUpdated += OnDataUpdated;
    }

    private void OnDataUpdated()
    {
        OnPropertyChanged(nameof(RecordCount));
    }

    public void WriteCsvData(Stream stream)
    {
        FpsSampler.WriteCsvData(stream);
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
        FpsSampler.DataUpdated -= OnDataUpdated;
    }
}
