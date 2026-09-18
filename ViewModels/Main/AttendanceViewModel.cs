using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

/// <summary>
/// 总在校时间统计（ATI）组件的视图模型：
/// 按时间基准解析学期区间，计算在校天数/时长、进度与剩余量，并生成展示文案。
/// 统计结果按天变化，因此使用低频定时器刷新，同时在配置或日历数据变更时立即重算。
/// </summary>
public class AttendanceViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly AttendanceSettings _settings;
    private readonly AttendanceCalendarService _calendar;
    private readonly DispatcherTimer _timer;
    private bool _isDisposed;

    private string _displayText = string.Empty;
    private string _subText = string.Empty;
    private double _progress;
    private bool _isProgressAvailable;

    /// <summary>主要统计文案，如"本学期已在校 82 天 / 共 100 天，约 656 小时"。</summary>
    public string DisplayText
    {
        get => _displayText;
        private set => Set(ref _displayText, value);
    }

    /// <summary>次要统计文案，如"进度 82.0% · 剩余 18 天 · 约 144 小时"。</summary>
    public string SubText
    {
        get => _subText;
        private set => Set(ref _subText, value);
    }

    /// <summary>进度百分比（0–100）。</summary>
    public double Progress
    {
        get => _progress;
        private set => Set(ref _progress, value);
    }

    /// <summary>进度是否可用（学期区间可解析且总在校日大于 0）。</summary>
    public bool IsProgressAvailable
    {
        get => _isProgressAvailable;
        private set => Set(ref _isProgressAvailable, value);
    }

    /// <summary>当前统计结果，供外部（如设置页预览）读取。</summary>
    public AttendanceStatistics? Statistics { get; private set; }

    public AttendanceViewModel(TimeBaseService timeBaseService, AttendanceSettings settings,
        AttendanceCalendarService calendar)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _calendar = calendar;

        AttendanceCalendarService.DataChanged += OnExternalChanged;
        SemesterStartService.SemesterStartDateChanged += OnExternalChanged;
        _settings.PropertyChanged += OnSettingsChanged;

        Refresh();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    private DateTime GetCurrentTime()
    {
        return _settings.TimeBaseType switch
        {
            TimeBaseType.RawServerTime => _timeBaseService.GetRawServerTime(),
            TimeBaseType.ClassIslandTime => _timeBaseService.GetClassIslandTime(),
            _ => _timeBaseService.GetCurrentTime()
        };
    }

    private void OnExternalChanged(object? sender, EventArgs e) => Refresh();

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    /// <summary>重新计算统计结果并刷新文案。</summary>
    public void Refresh()
    {
        try
        {
            var now = GetCurrentTime();
            var config = _calendar.Data.Config;
            var start = AttendanceStatisticsHelper.ResolveSemesterStart(config);
            if (start == null)
            {
                Statistics = null;
                IsProgressAvailable = false;
                Progress = 0;
                DisplayText = "尚未获取学期开始日";
                SubText = "请在「在校时间统计」设置页中指定学期开始日";
                return;
            }

            var end = AttendanceStatisticsHelper.ResolveSemesterEnd(start.Value, config);
            // 每日在校时长与"启用时间表为空"的日期由服务结合宿主课表解析后传入统计算法。
            var stats = _calendar.ComputeStatistics(start.Value, end, now);
            Statistics = stats;

            IsProgressAvailable = stats.TotalInSchoolDays > 0;
            Progress = stats.ProgressPercent;
            DisplayText = BuildDisplayText(stats);
            SubText = BuildSubText(stats);
        }
        catch (Exception)
        {
            // 计算过程中的异常不影响组件渲染，保留上一次的结果。
        }
    }

    private string BuildDisplayText(AttendanceStatistics stats)
    {
        if (stats.NotStarted)
        {
            var daysToStart = (stats.StartDate - stats.Today).Days;
            return $"距开学还有 {daysToStart} 天";
        }

        if (stats.Finished)
        {
            return _settings.ShowSummaryText
                ? $"本学期已结束：共在校 {stats.TotalInSchoolDays} 天"
                : "本学期已结束";
        }

        var text = _settings.ShowSummaryText
            ? $"本学期已在校 {stats.ElapsedInSchoolDays} 天 / 共 {stats.TotalInSchoolDays} 天"
            : $"已在校 {stats.ElapsedInSchoolDays} 天";

        if (_settings.ShowHoursText)
        {
            text += $"，{FormatHours(stats.ElapsedHours)}";
        }

        return text;
    }

    private string BuildSubText(AttendanceStatistics stats)
    {
        var parts = new List<string>();

        if (_settings.ShowPercentText && stats.TotalInSchoolDays > 0)
        {
            parts.Add($"进度 {stats.ProgressPercent:0.#}%");
        }

        if (_settings.ShowRemainingText)
        {
            parts.Add(stats.Finished
                ? "剩余 0 天"
                : $"剩余 {stats.RemainingInSchoolDays} 天 · {FormatHours(stats.RemainingHours)}");
        }

        return string.Join(" · ", parts);
    }

    /// <summary>小时数文案：按设置决定是否保留一位小数。</summary>
    private string FormatHours(double hours)
    {
        return _settings.DecimalizeHours
            ? $"约 {hours:0.#} 小时"
            : $"约 {Math.Round(hours):0} 小时";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        _timer.Stop();
        AttendanceCalendarService.DataChanged -= OnExternalChanged;
        SemesterStartService.SemesterStartDateChanged -= OnExternalChanged;
        _settings.PropertyChanged -= OnSettingsChanged;
    }
}