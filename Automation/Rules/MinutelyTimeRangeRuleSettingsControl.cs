using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每分钟时间范围规则设置控件
/// 格式：ss
/// 使用 TimePicker
/// </summary>
public class MinutelyTimeRangeRuleSettingsControl : RuleSettingsControlBase<MinutelyTimeRangeRuleSettings>
{
    private TimePicker _startTimePicker = null!;
    private TimePicker _endTimePicker = null!;

    public MinutelyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        mainPanel.Children.Add(CreateTimePickerGroup("开始秒数:", true));
        mainPanel.Children.Add(CreateTimePickerGroup("结束秒数:", false));

        Content = mainPanel;
    }

    private StackPanel CreateTimePickerGroup(string label, bool isStart)
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        // 时间选择器
        var timePicker = new TimePicker
        {
            Width = 300,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        if (isStart)
        {
            _startTimePicker = timePicker;
        }
        else
        {
            _endTimePicker = timePicker;
        }

        // 解析初始值
        var initialValue = isStart ? Settings?.StartSecond ?? "" : Settings?.EndSecond ?? "";
        if (int.TryParse(initialValue, out int second))
        {
            timePicker.SelectedTime = new TimeSpan(0, 0, second);
        }

        // 监听变化
        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        // 用ScrollViewer包裹
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = timePicker
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        // 开始秒数
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartSecond = $"{startTime.Seconds:D2}";

        // 结束秒数
        var endTime = _endTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.EndSecond = $"{endTime.Seconds:D2}";
    }
}
