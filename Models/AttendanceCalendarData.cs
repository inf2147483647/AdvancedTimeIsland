using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

/// <summary>
/// 节假日表条目的来源，用于在联网更新时保护用户手动编辑的内容。
/// </summary>
public enum HolidayEntrySource
{
    /// <summary>插件内置数据。</summary>
    Builtin = 0,

    /// <summary>联网更新获取。</summary>
    Network = 1,

    /// <summary>用户手动编辑（联网更新不会覆盖）。</summary>
    Manual = 2
}

/// <summary>
/// 学期开始日的取值来源。
/// </summary>
public enum SemesterStartSource
{
    /// <summary>自动读取 ClassIsland 的单周（学期）开始时间。</summary>
    ClassIsland = 0,

    /// <summary>手动指定日期。</summary>
    Manual = 1
}

/// <summary>
/// 学期结束日的推算方式。
/// </summary>
public enum SemesterEndSource
{
    /// <summary>由学期开始日 + 学期周数推算。</summary>
    TotalWeeks = 0,

    /// <summary>手动指定日期。</summary>
    ManualDate = 1
}

/// <summary>
/// 每日在校时长的取值方式。
/// </summary>
public enum DailyHoursSource
{
    /// <summary>手动指定固定时长。</summary>
    Manual = 0,

    /// <summary>自动获取当天档案中第一节课开始到最后一节课下课之间的小时数。</summary>
    Auto = 1
}

/// <summary>
/// 单个节假日（放假日）或调休补班日条目。
/// </summary>
public class HolidayDay
{
    /// <summary>日期（仅日期部分有效）。</summary>
    public DateTime Date { get; set; }

    /// <summary>名称，如"春节"、"端午调休"。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>是否为放假日；false 表示调休补班日。</summary>
    public bool IsOffDay { get; set; } = true;

    /// <summary>数据来源。</summary>
    public HolidayEntrySource Source { get; set; } = HolidayEntrySource.Manual;
}

/// <summary>
/// 寒暑假等自定义假期区间，区间内日期默认不计为在校日。
/// </summary>
public class VacationRange
{
    /// <summary>区间名称，如"2026 寒假"。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>起始日期（含）。</summary>
    public DateTime Start { get; set; }

    /// <summary>结束日期（含）。</summary>
    public DateTime End { get; set; }

    /// <summary>判断指定日期是否落在本区间内。</summary>
    public bool Contains(DateTime date)
    {
        var d = date.Date;
        return d >= Start.Date && d <= End.Date;
    }
}

/// <summary>
/// 在校日统计的口径配置：学期区间、每日在校时长与各类排除规则。
/// 由组件设置面板与「在校时间统计」设置页共用同一份实例。
/// </summary>
public class AttendanceStatisticsConfig : INotifyPropertyChanged
{
    private SemesterStartSource _startSource = SemesterStartSource.ClassIsland;
    private DateTime _manualStartDate = DateTime.Today;
    private SemesterEndSource _endSource = SemesterEndSource.TotalWeeks;
    private int _totalWeeks = 20;
    private DateTime _manualEndDate = DateTime.Today.AddDays(139);
    private double _dailyHours = 8;
    private DailyHoursSource _dailyHoursSource = DailyHoursSource.Manual;
    private bool _excludeSaturday = true;
    private bool _excludeSunday = true;
    private bool _excludeHolidays = true;
    private bool _countMakeupDays = true;
    private bool _excludeVacations = true;
    private bool _respectCustomDates = true;

    /// <summary>学期开始日来源。</summary>
    public SemesterStartSource StartSource
    {
        get => _startSource;
        set => Set(ref _startSource, value);
    }

    /// <summary>手动指定的学期开始日（仅当 <see cref="StartSource"/> 为 Manual 时生效）。</summary>
    public DateTime ManualStartDate
    {
        get => _manualStartDate;
        set => Set(ref _manualStartDate, value.Date);
    }

    /// <summary>学期结束日推算方式。</summary>
    public SemesterEndSource EndSource
    {
        get => _endSource;
        set => Set(ref _endSource, value);
    }

    /// <summary>学期总周数（仅当 <see cref="EndSource"/> 为 TotalWeeks 时生效）。</summary>
    public int TotalWeeks
    {
        get => _totalWeeks;
        set => Set(ref _totalWeeks, Math.Max(1, Math.Min(60, value)));
    }

    /// <summary>手动指定的学期结束日（仅当 <see cref="EndSource"/> 为 ManualDate 时生效）。</summary>
    public DateTime ManualEndDate
    {
        get => _manualEndDate;
        set => Set(ref _manualEndDate, value.Date);
    }

    /// <summary>每个在校日的标准在校时长（小时）。仅当 <see cref="DailyHoursSource"/> 为 Manual 时生效。</summary>
    public double DailyHours
    {
        get => _dailyHours;
        set => Set(ref _dailyHours, Math.Max(0, Math.Min(24, value)));
    }

    /// <summary>每日在校时长的取值方式。</summary>
    public DailyHoursSource DailyHoursSource
    {
        get => _dailyHoursSource;
        set => Set(ref _dailyHoursSource, value);
    }

    /// <summary>是否排除周六。</summary>
    public bool ExcludeSaturday
    {
        get => _excludeSaturday;
        set => Set(ref _excludeSaturday, value);
    }

    /// <summary>是否排除周日。</summary>
    public bool ExcludeSunday
    {
        get => _excludeSunday;
        set => Set(ref _excludeSunday, value);
    }

    /// <summary>是否排除法定节假日（放假日）。</summary>
    public bool ExcludeHolidays
    {
        get => _excludeHolidays;
        set => Set(ref _excludeHolidays, value);
    }

    /// <summary>调休补班日是否计为在校日（优先于周末排除规则）。</summary>
    public bool CountMakeupDays
    {
        get => _countMakeupDays;
        set => Set(ref _countMakeupDays, value);
    }

    /// <summary>是否排除寒暑假等自定义假期区间。</summary>
    public bool ExcludeVacations
    {
        get => _excludeVacations;
        set => Set(ref _excludeVacations, value);
    }

    /// <summary>是否应用自定义日期（强制计入 / 强制排除）。</summary>
    public bool RespectCustomDates
    {
        get => _respectCustomDates;
        set => Set(ref _respectCustomDates, value);
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
}

/// <summary>
/// 在校日统计数据：节假日、调休补班、寒暑假区间、自定义日期，
/// 以及统计口径配置。独立于 settings.json 存放在插件配置目录下，便于单独备份与联网覆盖。
/// </summary>
public class AttendanceCalendarData
{
    /// <summary>统计口径配置。</summary>
    public AttendanceStatisticsConfig Config { get; set; } = new();

    /// <summary>法定节假日（放假日）。</summary>
    public List<HolidayDay> Holidays { get; set; } = new();

    /// <summary>调休补班日。</summary>
    public List<HolidayDay> MakeupDays { get; set; } = new();

    /// <summary>寒暑假等假期区间。</summary>
    public List<VacationRange> Vacations { get; set; } = new();

    /// <summary>强制计入在校日期的自定义条目（优先级最高）。</summary>
    public List<HolidayDay> CustomIncluded { get; set; } = new();

    /// <summary>强制排除日期的自定义条目（如校运会、校庆）。</summary>
    public List<HolidayDay> CustomExcluded { get; set; } = new();

    /// <summary>最近一次联网更新时间。</summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>最近一次联网更新的数据来源描述。</summary>
    public string LastUpdateSource { get; set; } = string.Empty;
}