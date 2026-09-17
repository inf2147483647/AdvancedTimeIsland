namespace AdvancedTimeIsland.Models;

/// <summary>
/// 总在校时间统计（ATI）组件的显示设置。
/// 学期区间与统计口径不在此处，而是由 <see cref="AttendanceStatisticsConfig"/> 统一管理，
/// 使所有组件实例与设置页共用同一份配置。
/// </summary>
public class AttendanceSettings : WeekSettings
{
    private ProgressDisplayMode _progressDisplayMode = ProgressDisplayMode.Ring;
    private bool _showSummaryText = true;
    private bool _showHoursText = true;
    private bool _showRemainingText = true;
    private bool _showPercentText = true;
    private bool _decimalizeHours = true;

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

    /// <summary>概要文案中是否附加累计在校小时数。</summary>
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
}