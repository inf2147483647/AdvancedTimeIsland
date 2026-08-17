using System;
using System.Windows.Controls;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// WPF 时间选择控件。封装 <see cref="MaterialDesignThemes.Wpf.TimePicker"/>，
/// 对外暴露与原 Avalonia 版 TimePicker 兼容的 <c>SelectedTime / ClockIdentifier / UseSeconds</c>
/// 属性与 <c>SelectedTimeChanged</c> 事件，以便最小化从 Avalonia 迁移的改动。
/// </summary>
public class WpfTimePicker : UserControl
{
    private readonly MaterialDesignThemes.Wpf.TimePicker _timePicker;

    public WpfTimePicker()
    {
        _timePicker = new MaterialDesignThemes.Wpf.TimePicker
        {
            Is24Hours = true,
            WithSeconds = true
        };
        _timePicker.SelectedTimeChanged += (_, _) => SelectedTimeChanged?.Invoke(this, EventArgs.Empty);
        Content = _timePicker;
    }

    /// <summary>
    /// 当前选中的时间（时分秒）。
    /// </summary>
    public TimeSpan? SelectedTime
    {
        get => _timePicker.SelectedTime?.TimeOfDay;
        set => _timePicker.SelectedTime = value.HasValue
            ? new DateTime(2000, 1, 1).Add(value.Value)
            : (DateTime?)null;
    }

    /// <summary>
    /// 时钟标识。仅兼容旧代码使用；"24HourClock" 会切换为 24 小时制显示。
    /// </summary>
    public string ClockIdentifier
    {
        get => _timePicker.Is24Hours ? "24HourClock" : "12HourClock";
        set => _timePicker.Is24Hours = string.Equals(value, "24HourClock", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 是否显示秒。
    /// </summary>
    public bool UseSeconds
    {
        get => _timePicker.WithSeconds;
        set => _timePicker.WithSeconds = value;
    }

    /// <summary>
    /// 选中时间变更事件。
    /// </summary>
    public event EventHandler? SelectedTimeChanged;
}
