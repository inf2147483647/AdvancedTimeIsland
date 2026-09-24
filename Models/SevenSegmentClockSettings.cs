using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

/// <summary>
/// 七段数码管时钟组件设置。
/// </summary>
public class SevenSegmentClockSettings : INotifyPropertyChanged
{
    /// <summary>强调色默认值（红色）。</summary>
    public const string DefaultAccentColor = "#FFFF0000";

    /// <summary>缩放倍数默认值。</summary>
    public const double DefaultZoom = 0.8;

    /// <summary>缩放倍数下限。</summary>
    public const double MinZoom = 0.6;

    /// <summary>缩放倍数上限。</summary>
    public const double MaxZoom = 1.0;

    private bool _showSeconds = true;
    private TimeBaseType _timeBaseType = TimeBaseType.PluginOffsetServerTime;
    private bool _skewMode = false;
    private string _accentColor = DefaultAccentColor;
    private bool _separatorBlink = true;
    private double _zoom = DefaultZoom;
    private bool _enableTransitionAnimation = true;

    /// <summary>
    /// 是否显示秒数（默认开启）。
    /// </summary>
    public bool ShowSeconds
    {
        get => _showSeconds;
        set
        {
            if (_showSeconds != value)
            {
                _showSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 时间基准（时间差值来源）。
    /// </summary>
    public TimeBaseType TimeBaseType
    {
        get => _timeBaseType;
        set
        {
            if (_timeBaseType != value)
            {
                _timeBaseType = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 倾斜模式：开启后，纵向数码管向右顺时针倾斜 6°。
    /// </summary>
    public bool SkewMode
    {
        get => _skewMode;
        set
        {
            if (_skewMode != value)
            {
                _skewMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 强调色（默认为红色），用于数码管边框与点亮段的渐变起点。
    /// </summary>
    public string AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor != value)
            {
                _accentColor = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 分离管脚闪动：开启后分隔符（冒号）亮灭交替闪动。
    /// </summary>
    public bool SeparatorBlink
    {
        get => _separatorBlink;
        set
        {
            if (_separatorBlink != value)
            {
                _separatorBlink = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 缩放倍数（0.6~1.0，默认 0.8，步长 0.01）。
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            var clamped = Math.Max(MinZoom, Math.Min(MaxZoom, value));
            if (Math.Abs(_zoom - clamped) > 0.0001)
            {
                _zoom = clamped;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 动画：开启后，发生变化的数码管使用 0.25s 的渐隐、渐显过渡动画。
    /// </summary>
    public bool EnableTransitionAnimation
    {
        get => _enableTransitionAnimation;
        set
        {
            if (_enableTransitionAnimation != value)
            {
                _enableTransitionAnimation = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
