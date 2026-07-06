using System;
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
    private static readonly List<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)> s_records = new();
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

    private static void OnFpsDataUpdated(DateTime time, double fps, double max, double avg, double min, double low1)
    {
        if (s_isPaused) return;
        
        try
        {
            s_records.Add((time, fps, max, avg, min, low1));

            DataUpdated?.Invoke();
        }
        catch
        {
        }
    }

    public static string GetCsvData()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Time,fps,max,avg,min,1%min");

        foreach (var r in s_records)
        {
            sb.AppendLine($"{r.Time:yyyy-MM-dd HH:mm:ss.fff},{r.Fps:F2},{r.Max:F2},{r.Avg:F2},{r.Min:F2},{r.Low1:F2}");
        }

        return sb.ToString();
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

    public string GetCsvData()
    {
        return FpsSampler.GetCsvData();
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
