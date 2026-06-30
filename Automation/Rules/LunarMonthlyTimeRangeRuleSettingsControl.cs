using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历每月范围规则设置控件
/// 格式：[L_DD-hh-mm-ss]₁ ~ [L_DD-hh-mm-ss]₂
/// </summary>
public class LunarMonthlyTimeRangeRuleSettingsControl : RuleSettingsControlBase<LunarMonthlyTimeRangeRuleSettings>
{
    private ComboBox _startDayComboBox = null!;
    private TimePicker _startTimePicker = null!;

    private ComboBox _endDayComboBox = null!;
    private TimePicker _endTimePicker = null!;

    public LunarMonthlyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        mainPanel.Children.Add(CreateDayTimeGroup("开始:", true));
        mainPanel.Children.Add(CreateDayTimeGroup("结束:", false));

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateDayTimeGroup(string label, bool isStart)
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Bold
        });

        // 农历日期
        var dayPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        dayPanel.Children.Add(new TextBlock
        {
            Text = "农历日:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        var dayComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 30; i++)
        {
            dayComboBox.Items.Add(i.ToString());
        }

        var dayValue = isStart ? (Settings?.StartDay ?? 0) : (Settings?.EndDay ?? 0);
        if (dayValue > 0 && dayValue <= 30)
        {
            dayComboBox.SelectedIndex = dayValue - 1;
        }
        else
        {
            dayComboBox.SelectedIndex = 0;
        }

        dayComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        if (isStart)
            _startDayComboBox = dayComboBox;
        else
            _endDayComboBox = dayComboBox;

        dayPanel.Children.Add(dayComboBox);
        groupPanel.Children.Add(dayPanel);

        // 说明提示
        groupPanel.Children.Add(new TextBlock
        {
            Text = "选\"三十\"则每大月，选\"初一～廿九\"则每月",
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, -4, 0, 0)
        });

        // 时间选择器
        var timePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        timePanel.Children.Add(new TextBlock
        {
            Text = "时间:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        var timePicker = new TimePicker
        {
            Width = 300,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var initialTimeValue = isStart ? (Settings?.StartTime ?? "") : (Settings?.EndTime ?? "");
        ParseTimeString(initialTimeValue, out int hour, out int minute, out int second);

        timePicker.SelectedTime = new TimeSpan(hour, minute, second);

        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        if (isStart)
            _startTimePicker = timePicker;
        else
            _endTimePicker = timePicker;

        timePanel.Children.Add(timePicker);
        groupPanel.Children.Add(timePanel);

        return groupPanel;
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        Settings.StartDay = _startDayComboBox.SelectedIndex + 1;
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        Settings.EndDay = _endDayComboBox.SelectedIndex + 1;
        var endTime = _endTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.EndTime = $"{endTime.Hours:D2}-{endTime.Minutes:D2}-{endTime.Seconds:D2}";
    }

    private void ParseTimeString(string value, out int hour, out int minute, out int second)
    {
        hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int h)) hour = h;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int mi)) minute = mi;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int s)) second = s;
    }
}
