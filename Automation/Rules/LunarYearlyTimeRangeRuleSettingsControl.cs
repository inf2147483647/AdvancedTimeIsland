using System;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历每年范围规则设置控件
/// 格式：[L_MM-L_DD-hh-mm-ss]₁ ~ [L_MM-L_DD-hh-mm-ss]₂
/// </summary>
public class LunarYearlyTimeRangeRuleSettingsControl : RuleSettingsControlBase<LunarYearlyTimeRangeRuleSettings>
{
    private ComboBox _startMonthComboBox = null!;
    private ComboBox _startDayComboBox = null!;
    private CheckBox _startIsLeapMonthCheckBox = null!;
    private TimePicker _startTimePicker = null!;

    private ComboBox _endMonthComboBox = null!;
    private ComboBox _endDayComboBox = null!;
    private CheckBox _endIsLeapMonthCheckBox = null!;
    private TimePicker _endTimePicker = null!;

    public LunarYearlyTimeRangeRuleSettingsControl()
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

        mainPanel.Children.Add(CreateLunarDateGroup("开始日期:", true));
        mainPanel.Children.Add(CreateLunarDateGroup("结束日期:", false));

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateLunarDateGroup(string label, bool isStart)
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
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Bold
        });

        // 农历月份和闰月
        var monthPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        monthPanel.Children.Add(new TextBlock
        {
            Text = "农历月:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var monthComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 12; i++)
        {
            monthComboBox.Items.Add(i.ToString());
        }

        var monthValue = isStart ? (Settings?.StartMonth ?? 0) : (Settings?.EndMonth ?? 0);
        if (monthValue > 0 && monthValue <= 12)
        {
            monthComboBox.SelectedIndex = monthValue - 1;
        }
        else
        {
            monthComboBox.SelectedIndex = 0;
        }

        monthComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        if (isStart)
            _startMonthComboBox = monthComboBox;
        else
            _endMonthComboBox = monthComboBox;

        monthPanel.Children.Add(monthComboBox);

        var isLeapMonthCheckBox = new CheckBox
        {
            Content = "闰月",
            IsChecked = isStart ? (Settings?.StartIsLeapMonth ?? false) : (Settings?.EndIsLeapMonth ?? false),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        FluentAvaloniaCompatibilityHelper.AddCheckedHandler(isLeapMonthCheckBox, (s, e) => UpdateSettingsValue());
        FluentAvaloniaCompatibilityHelper.AddUncheckedHandler(isLeapMonthCheckBox, (s, e) => UpdateSettingsValue());

        if (isStart)
            _startIsLeapMonthCheckBox = isLeapMonthCheckBox;
        else
            _endIsLeapMonthCheckBox = isLeapMonthCheckBox;

        monthPanel.Children.Add(isLeapMonthCheckBox);
        groupPanel.Children.Add(monthPanel);

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
            Foreground = ThemeHelper.GetTextBrush(),
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
            Foreground = ThemeHelper.GetTextBrush(),
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

        Settings.StartMonth = _startMonthComboBox.SelectedIndex + 1;
        Settings.StartDay = _startDayComboBox.SelectedIndex + 1;
        Settings.StartIsLeapMonth = _startIsLeapMonthCheckBox.IsChecked ?? false;
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        Settings.EndMonth = _endMonthComboBox.SelectedIndex + 1;
        Settings.EndDay = _endDayComboBox.SelectedIndex + 1;
        Settings.EndIsLeapMonth = _endIsLeapMonthCheckBox.IsChecked ?? false;
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
