using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 地方时每年时间范围规则设置控件（带经度输入）
/// </summary>
public class LocalSolarYearlyTimeRangeRuleSettingsControl : RuleSettingsControlBase<LocalSolarYearlyTimeRangeRuleSettings>
{
    private TextBox _longitudeBox = null!;
    private DatePicker _startDatePicker = null!;
    private TimePicker _startTimePicker = null!;
    private DatePicker _endDatePicker = null!;
    private TimePicker _endTimePicker = null!;

    public LocalSolarYearlyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Settings != null)
        {
            _longitudeBox.Text = Settings.Longitude.ToString("F4");
        }
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

        // 经度输入框
        mainPanel.Children.Add(CreateLongitudeInputGroup());

        // 开始时间
        mainPanel.Children.Add(CreateDateTimePickerGroup("开始时间:", true));

        // 结束时间
        mainPanel.Children.Add(CreateDateTimePickerGroup("结束时间:", false));

        Content = mainPanel;
    }

    private StackPanel CreateLongitudeInputGroup()
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = "经度:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _longitudeBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "经度 (-180~180)"
        };

        _longitudeBox.TextChanged += (s, e) => UpdateLongitude();

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _longitudeBox
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
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
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            YearVisible = false
        };

        // 时间选择器
        var timePicker = new TimePicker
        {
            Width = 180,
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

        // 解析初始值
        var initialValue = isStart ? Settings?.StartTime ?? "" : Settings?.EndTime ?? "";
        ParseTimeString(initialValue, out int month, out int day, out int hour, out int minute, out int second);

        if (month > 0 && day > 0)
        {
            datePicker.SelectedDate = new DateTimeOffset(new DateTime(2024, month, day));
        }
        timePicker.SelectedTime = new TimeSpan(hour, minute, second);

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

    private void UpdateLongitude()
    {
        if (Settings == null) return;
        if (double.TryParse(_longitudeBox.Text, out double lon))
        {
            Settings.Longitude = Math.Clamp(lon, -180.0, 180.0);
        }
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        // 开始时间
        var startDate = _startDatePicker.SelectedDate?.DateTime ?? new DateTime(2024, 1, 1);
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startDate.Month:D2}-{startDate.Day:D2}-{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        // 结束时间
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
