using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class TimeZoneMonthlyTimeTriggerSettingsControl : TriggerSettingsControlBase<TimeZoneMonthlyTimeRangeRuleSettings>
{
    private ComboBox _timeZoneComboBox = null!;
    private DatePicker _startDatePicker = null!;
    private TimePicker _startTimePicker = null!;

    public TimeZoneMonthlyTimeTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateTimeZoneInputGroup());
        mainPanel.Children.Add(CreateDateTimePickerGroup("触发时间:"));

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateTimeZoneInputGroup()
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = "时区:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _timeZoneComboBox = new ComboBox
        {
            Width = 500
        };

        var timeZones = TimeZoneInfo.GetSystemTimeZones();
        foreach (var tz in timeZones)
        {
            _timeZoneComboBox.Items.Add(tz);
        }

        _timeZoneComboBox.SelectionChanged += (s, e) => UpdateTimeZone();

        groupPanel.Children.Add(_timeZoneComboBox);

        return groupPanel;
    }

    private StackPanel CreateDateTimePickerGroup(string label)
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

        var datePicker = new DatePicker
        {
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left,
            YearVisible = false,
            MonthVisible = false
        };

        var timePicker = new TimePicker
        {
            Width = 260,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _startDatePicker = datePicker;
        _startTimePicker = timePicker;

        datePicker.SelectedDateChanged += (s, e) => UpdateSettingsValue();
        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        var pickerRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        pickerRow.Children.Add(datePicker);
        pickerRow.Children.Add(timePicker);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = pickerRow
        };

        groupPanel.Children.Add(scrollViewer);

        groupPanel.Children.Add(new TextBlock
        {
            Text = "仅设置触发时间，不限制结束时间",
            Foreground = Brushes.Gray,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        });

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        foreach (var item in _timeZoneComboBox.Items)
        {
            if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
            {
                _timeZoneComboBox.SelectedItem = item;
                break;
            }
        }

        var initialValue = Settings.StartTime;
        ParseTimeString(initialValue, out int day, out int hour, out int minute, out int second);

        if (day > 0)
        {
            _startDatePicker.SelectedDate = new DateTimeOffset(new DateTime(2024, 1, day));
        }
        _startTimePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateTimeZone()
    {
        if (Settings == null) return;
        if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz)
        {
            Settings.TimeZoneId = tz.Id;
        }
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        var startDate = _startDatePicker.SelectedDate?.DateTime ?? new DateTime(2024, 1, 1);
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startDate.Day:D2}-{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";
    }

    private void ParseTimeString(string value, out int day, out int hour, out int minute, out int second)
    {
        day = 0; hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int d)) day = d;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int h)) hour = h;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int mi)) minute = mi;
        if (parts.Length >= 4 && int.TryParse(parts[3], out int s)) second = s;
    }
}
