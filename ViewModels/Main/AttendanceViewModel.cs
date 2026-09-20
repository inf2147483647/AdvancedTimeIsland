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
/// 统计结果按天变化，因此跟随插件共用的渲染时钟在跨天时刷新，同时在配置或日历数据变更时立即重算。
/// </summary>
public class AttendanceViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly AttendanceSettings _settings;
    private readonly AttendanceCalendarService _calendar;
    private IDisposable? _clockSubscription;
    private DateTime _lastRefreshedDate = DateTime.MinValue;
    private bool _isDisposed;

    private readonly string[] _segmentTexts = new string[5];
    private readonly bool[] _segmentVisibility = new bool[5];
    private double _progress;
    private bool _isProgressAvailable;

    /// <summary>
    /// 段落文案或可见性发生变化时通知的属性名（一次刷新同时更新全部段落）。
    /// </summary>
    public const string SegmentsPropertyName = "Segments";

    /// <summary>取指定段落的文案；不可见时内容为空。</summary>
    public string GetSegmentText(AttendanceTextSegment segment) => _segmentTexts[(int)segment];

    /// <summary>指定段落的文案当前是否应当显示。</summary>
    public bool IsSegmentVisible(AttendanceTextSegment segment) => _segmentVisibility[(int)segment];

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

        // 统计结果按天变化，跟随插件共用的渲染时钟监听跨天，不额外占用定时器。
        _clockSubscription = SharedRenderClockService.Instance.Subscribe(OnClockTick);
        SharedRenderClockService.Instance.EnsureStarted();
    }

    /// <summary>渲染时钟回调（UI 线程）：仅当日期跨天时重算，避免高频无谓计算。</summary>
    private void OnClockTick(DateTime now)
    {
        if (GetCurrentTime().Date != _lastRefreshedDate)
        {
            Refresh();
        }
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

    private void OnExternalChanged(object? sender, EventArgs e) => RefreshOnUiThread();

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e) => RefreshOnUiThread();

    /// <summary>
    /// 外部事件（日历数据、学期开始日、组件设置）可能来自非 UI 线程，
    /// 此时属性变更会触发控件更新，必须切回 UI 线程再刷新。
    /// </summary>
    private void RefreshOnUiThread()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Refresh();
        }
        else
        {
            Dispatcher.UIThread.Post(Refresh);
        }
    }

    /// <summary>重新计算统计结果并刷新文案。</summary>
    public void Refresh()
    {
        var now = GetCurrentTime();
        try
        {
            var config = _calendar.Data.Config;
            var start = AttendanceStatisticsHelper.ResolveSemesterStart(config);
            if (start == null)
            {
                Statistics = null;
                _lastRefreshedDate = now.Date;
                IsProgressAvailable = false;
                Progress = 0;
                ApplySegments("尚未获取学期开始日（请在「在校时间统计」设置页中指定）", null, null, null, null);
                return;
            }

            var end = AttendanceStatisticsHelper.ResolveSemesterEnd(start.Value, config);
            // 每日在校时长与"启用时间表为空"的日期由服务结合宿主课表解析后传入统计算法。
            var stats = _calendar.ComputeStatistics(start.Value, end, now);
            Statistics = stats;
            _lastRefreshedDate = now.Date;

            IsProgressAvailable = stats.TotalInSchoolDays > 0;
            Progress = stats.ProgressPercent;
            ApplyStatistics(stats);
        }
        catch (Exception ex)
        {
            // 计算过程中的异常不影响组件渲染，保留上一次的结果，但记录原因便于排查。
            _lastRefreshedDate = now.Date;
            System.Diagnostics.Debug.WriteLine($"[AttendanceViewModel] 刷新在校统计失败：{ex}");

            // 首次刷新即失败时组件会完全没有内容，给一句可辨认的提示而不是空白。
            if (!_segmentVisibility[(int)AttendanceTextSegment.Summary])
            {
                ApplySegments("在校统计暂不可用", null, null, null, null);
            }
        }
    }

    /// <summary>
    /// 按学期状态与显示开关生成五段文案。
    /// 每段独立成块，交由组件按各自的字体样式渲染；不需要显示的段落传 null。
    /// </summary>
    private void ApplyStatistics(AttendanceStatistics stats)
    {
        // 未开学时只有状态提示有意义，其余段落一律隐藏。
        if (stats.NotStarted)
        {
            var daysToStart = (stats.StartDate - stats.Today).Days;
            ApplySegments($"距开学还有 {daysToStart} 天", null, null, null, null);
            return;
        }

        var summary = _settings.ShowSummaryText
            ? (stats.Finished
                ? $"本学期已结束：共在校 {stats.TotalInSchoolDays} 天"
                : $"本学期已在校 {stats.ElapsedInSchoolDays} 天 / 共 {stats.TotalInSchoolDays} 天")
            : (stats.Finished ? "本学期已结束" : $"已在校 {stats.ElapsedInSchoolDays} 天");

        // 学期结束后"已在校时长"与概要重复，不再单独显示。
        var hours = !stats.Finished && _settings.ShowHoursText
            ? FormatHours(stats.ElapsedHours)
            : null;

        var progress = _settings.ShowPercentText && stats.TotalInSchoolDays > 0
            ? $"进度 {stats.ProgressPercent:0.#}%"
            : null;

        var remainingDays = _settings.ShowRemainingText
            ? (stats.Finished ? "剩余 0 天" : $"剩余 {stats.RemainingInSchoolDays} 天")
            : null;

        var remainingHours = _settings.ShowRemainingText && !stats.Finished
            ? FormatHours(stats.RemainingHours)
            : null;

        ApplySegments(summary, hours, progress, remainingDays, remainingHours);
    }

    /// <summary>写入五段文案并通知组件重建文本行。</summary>
    private void ApplySegments(string? summary, string? hours, string? progress,
        string? remainingDays, string? remainingHours)
    {
        SetSegment(AttendanceTextSegment.Summary, summary);
        SetSegment(AttendanceTextSegment.Hours, hours);
        SetSegment(AttendanceTextSegment.Progress, progress);
        SetSegment(AttendanceTextSegment.RemainingDays, remainingDays);
        SetSegment(AttendanceTextSegment.RemainingHours, remainingHours);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(SegmentsPropertyName));
    }

    private void SetSegment(AttendanceTextSegment segment, string? text)
    {
        var index = (int)segment;
        _segmentTexts[index] = text ?? string.Empty;
        _segmentVisibility[index] = !string.IsNullOrEmpty(text);
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
        _clockSubscription?.Dispose();
        _clockSubscription = null;
        AttendanceCalendarService.DataChanged -= OnExternalChanged;
        SemesterStartService.SemesterStartDateChanged -= OnExternalChanged;
        _settings.PropertyChanged -= OnSettingsChanged;
    }
}