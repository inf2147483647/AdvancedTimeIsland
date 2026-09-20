using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

/// <summary>
/// 主界面组件中可独立设置样式的文案段落，取值顺序与屏幕上从左到右的排列一致。
/// </summary>
public enum AttendanceTextSegment
{
    /// <summary>概要文案：本学期已在校 N 天 / 共 M 天。</summary>
    Summary = 0,

    /// <summary>累计在校时长：约 N 小时。</summary>
    Hours = 1,

    /// <summary>进度百分比：进度 N%。</summary>
    Progress = 2,

    /// <summary>剩余在校天数：剩余 N 天。</summary>
    RemainingDays = 3,

    /// <summary>剩余在校时长：约 N 小时。</summary>
    RemainingHours = 4
}

/// <summary>
/// 单段文案的字体样式设置：大小、颜色、字体族与字重，各自带独立的自定义开关。
/// 未开启开关的项沿用宿主（ClassIsland）的默认字体样式，以便随主题与全局字号自适应。
/// </summary>
public class TextStyleSettings : INotifyPropertyChanged
{
    private double _fontSize = 14;
    private string _fontColor = string.Empty;
    private string _fontFamily = string.Empty;
    private string _fontWeight = "Normal";
    private bool _enableCustomFontSize;
    private bool _enableCustomFontColor;
    private bool _enableCustomFontFamily;
    private bool _enableCustomFontWeight;

    /// <summary>字体大小，未开启自定义时不起作用。</summary>
    public double FontSize
    {
        get => _fontSize;
        set => Set(ref _fontSize, Math.Max(6, Math.Min(72, value)));
    }

    /// <summary>字体颜色（ARGB 或 #RRGGBB 字符串），留空或未开启自定义时随主题自适应。</summary>
    public string FontColor
    {
        get => _fontColor;
        set => Set(ref _fontColor, value ?? string.Empty);
    }

    /// <summary>字体族名称，留空或未开启自定义时沿用宿主字体。</summary>
    public string FontFamily
    {
        get => _fontFamily;
        set => Set(ref _fontFamily, value ?? string.Empty);
    }

    /// <summary>字重名称，留空或未开启自定义时沿用宿主字重。</summary>
    public string FontWeight
    {
        get => _fontWeight;
        set => Set(ref _fontWeight, value ?? string.Empty);
    }

    public bool EnableCustomFontSize
    {
        get => _enableCustomFontSize;
        set => Set(ref _enableCustomFontSize, value);
    }

    public bool EnableCustomFontColor
    {
        get => _enableCustomFontColor;
        set => Set(ref _enableCustomFontColor, value);
    }

    public bool EnableCustomFontFamily
    {
        get => _enableCustomFontFamily;
        set => Set(ref _enableCustomFontFamily, value);
    }

    public bool EnableCustomFontWeight
    {
        get => _enableCustomFontWeight;
        set => Set(ref _enableCustomFontWeight, value);
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
/// 总在校时间统计（ATI）组件的显示设置。
/// 学期区间与统计口径不在此处，而是由 <see cref="AttendanceStatisticsConfig"/> 统一管理，
/// 使所有组件实例与设置页共用同一份配置。
/// </summary>
public class AttendanceSettings : WeekSettings
{
    private ProgressDisplayMode _progressDisplayMode = ProgressDisplayMode.Ring;
    private bool _showSegmentDivider;
    private bool _showSummaryText = true;
    private bool _showHoursText = true;
    private bool _showRemainingText = true;
    private bool _showPercentText = true;
    private bool _decimalizeHours = true;

    private TextStyleSettings _summaryStyle = new();
    private TextStyleSettings _hoursStyle = new();
    private TextStyleSettings _progressStyle = new();
    private TextStyleSettings _remainingDaysStyle = new();
    private TextStyleSettings _remainingHoursStyle = new();

    /// <summary>进度显示方式（不显示 / 进度条 / 进度环 / 两者）。</summary>
    public ProgressDisplayMode ProgressDisplayMode
    {
        get => _progressDisplayMode;
        set
        {
            if (_progressDisplayMode != value)
            {
                _progressDisplayMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>是否在各段文案之间插入竖线分割线（样式对齐 ClassIsland「课程表」组件的课程分隔线）。</summary>
    public bool ShowSegmentDivider
    {
        get => _showSegmentDivider;
        set
        {
            if (_showSegmentDivider != value)
            {
                _showSegmentDivider = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>是否显示"本学期已在校 N 天 / 共 M 天"概要文案。</summary>
    public bool ShowSummaryText
    {
        get => _showSummaryText;
        set
        {
            if (_showSummaryText != value)
            {
                _showSummaryText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>是否显示累计在校时长。</summary>
    public bool ShowHoursText
    {
        get => _showHoursText;
        set
        {
            if (_showHoursText != value)
            {
                _showHoursText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>是否显示剩余在校天数与时长。</summary>
    public bool ShowRemainingText
    {
        get => _showRemainingText;
        set
        {
            if (_showRemainingText != value)
            {
                _showRemainingText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>是否显示统计进度百分比。</summary>
    public bool ShowPercentText
    {
        get => _showPercentText;
        set
        {
            if (_showPercentText != value)
            {
                _showPercentText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>小时数是否保留一位小数。</summary>
    public bool DecimalizeHours
    {
        get => _decimalizeHours;
        set
        {
            if (_decimalizeHours != value)
            {
                _decimalizeHours = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>概要文案的字体样式。</summary>
    public TextStyleSettings SummaryStyle
    {
        get => _summaryStyle;
        set => SetStyle(ref _summaryStyle, value);
    }

    /// <summary>累计在校时长的字体样式。</summary>
    public TextStyleSettings HoursStyle
    {
        get => _hoursStyle;
        set => SetStyle(ref _hoursStyle, value);
    }

    /// <summary>进度百分比的字体样式。</summary>
    public TextStyleSettings ProgressStyle
    {
        get => _progressStyle;
        set => SetStyle(ref _progressStyle, value);
    }

    /// <summary>剩余在校天数的字体样式。</summary>
    public TextStyleSettings RemainingDaysStyle
    {
        get => _remainingDaysStyle;
        set => SetStyle(ref _remainingDaysStyle, value);
    }

    /// <summary>剩余在校时长的字体样式。</summary>
    public TextStyleSettings RemainingHoursStyle
    {
        get => _remainingHoursStyle;
        set => SetStyle(ref _remainingHoursStyle, value);
    }

    /// <summary>按段落取对应的字体样式，供组件与设置面板统一遍历。</summary>
    public TextStyleSettings GetStyle(AttendanceTextSegment segment) => segment switch
    {
        AttendanceTextSegment.Hours => _hoursStyle,
        AttendanceTextSegment.Progress => _progressStyle,
        AttendanceTextSegment.RemainingDays => _remainingDaysStyle,
        AttendanceTextSegment.RemainingHours => _remainingHoursStyle,
        _ => _summaryStyle
    };

    private void SetStyle(ref TextStyleSettings field, TextStyleSettings? value, [CallerMemberName] string? propertyName = null)
    {
        var style = value ?? new TextStyleSettings();
        if (ReferenceEquals(field, style))
        {
            return;
        }
        field = style;
        OnPropertyChanged(propertyName);
    }
}