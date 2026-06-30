using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每年时间范围规则设置控件
/// 格式：MM-DD-hh-mm-ss
/// 使用 DatePicker（隐藏年份）和 TimePicker
/// </summary>
public class YearlyTimeRangeRuleSettingsControl : RuleSettingsControlBase<YearlyTimeRangeRuleSettings>
{
    private DatePicker _startDatePicker = null!;
    private TimePicker _startTimePicker = null!;
    private DatePicker _endDatePicker = null!;
    private TimePicker _endTimePicker = null!;

    public YearlyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
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

        mainPanel.Children.Add(CreateDateTimePickerGroup("开始时间:", true));
        mainPanel.Children.Add(CreateDateTimePickerGroup("结束时间:", false));

                Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateDateTimePickerGroup(string label, bool isStart)
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

        // 日期选择器（隐藏年份）
        var datePicker = new DatePicker
        {
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Left,
            YearVisible = false
        };

        // 时间选择器
        var timePicker = new TimePicker
        {
            Width = 380,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        if (isStart)
        {
            _startDatePicker = datePicker;
            _startTimePicker = timePicker;
        }
        else
        {
            _endDatePicker = datePicker;
            _endTimePicker = timePicker;
        }

        // 监听变化
        datePicker.SelectedDateChanged += (s, e) => UpdateSettingsValue();
        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        // 水平排列的日期时间选择器
        var pickerRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        pickerRow.Children.Add(datePicker);
        pickerRow.Children.Add(timePicker);

        // 用ScrollViewer包裹，实现水平滚动
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = pickerRow
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        var startInitialValue = Settings.StartTime;
        ParseTimeString(startInitialValue, out int startMonth, out int startDay, out int startHour, out int startMinute, out int startSecond);
        if (startMonth > 0 && startDay > 0)
        {
            _startDatePicker.SelectedDate = new DateTimeOffset(new DateTime(2024, startMonth, startDay));
        }
        _startTimePicker.SelectedTime = new TimeSpan(startHour, startMinute, startSecond);

        var endInitialValue = Settings.EndTime;
        ParseTimeString(endInitialValue, out int endMonth, out int endDay, out int endHour, out int endMinute, out int endSecond);
        if (endMonth > 0 && endDay > 0)
        {
            _endDatePicker.SelectedDate = new DateTimeOffset(new DateTime(2024, endMonth, endDay));
        }
        _endTimePicker.SelectedTime = new TimeSpan(endHour, endMinute, endSecond);
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        var startDate = _startDatePicker.SelectedDate?.DateTime ?? new DateTime(2024, 1, 1);
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startDate.Month:D2}-{startDate.Day:D2}-{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        var endDate = _endDatePicker.SelectedDate?.DateTime ?? new DateTime(2024, 1, 1);
        var endTime = _endTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.EndTime = $"{endDate.Month:D2}-{endDate.Day:D2}-{endTime.Hours:D2}-{endTime.Minutes:D2}-{endTime.Seconds:D2}";
    }

    private void ParseTimeString(string value, out int month, out int day, out int hour, out int minute, out int second)
    {
        month = 0; day = 0; hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int m)) month = m;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int d)) day = d;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int h)) hour = h;
        if (parts.Length >= 4 && int.TryParse(parts[3], out int mi)) minute = mi;
        if (parts.Length >= 5 && int.TryParse(parts[4], out int s)) second = s;
    }
}



