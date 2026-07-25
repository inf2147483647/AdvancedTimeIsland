using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class LocalSolarYearlyTimeIntervalTriggerSettingsControl : TriggerSettingsControlBase<LocalSolarYearlyTimeIntervalTriggerSettings>
{
    private TextBox _longitudeBox = null!;
    private DatePicker _startDatePicker = null!;
    private TimePicker _startTimePicker = null!;
    private DatePicker _endDatePicker = null!;
    private TimePicker _endTimePicker = null!;
    private NumericUpDown _intervalNumericUpDown = null!;
    private ComboBox _intervalUnitComboBox = null!;
    private bool _isLoading = false;

    public LocalSolarYearlyTimeIntervalTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateLongitudeInputGroup());
        mainPanel.Children.Add(CreateDateTimePickerGroup("开始时间:", true));
        mainPanel.Children.Add(CreateDateTimePickerGroup("结束时间:", false));
        mainPanel.Children.Add(CreateIntervalGroup());

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
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
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        _longitudeBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "经度 (-180~180)"
        };
        _longitudeBox.TextChanged += (s, e) => UpdateLongitude();

        groupPanel.Children.Add(_longitudeBox);
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
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var datePicker = new DatePicker
        {
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            YearVisible = false
        };

        var timePicker = new TimePicker
        {
            Width = 250,
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

        datePicker.SelectedDateChanged += (s, e) => UpdateSettingsValue();
        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        groupPanel.Children.Add(datePicker);
        groupPanel.Children.Add(timePicker);

        return groupPanel;
    }

    private StackPanel CreateIntervalGroup()
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = "触发间隔:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var intervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _intervalNumericUpDown = new NumericUpDown
        {
            Width = 225,
            Minimum = 0.001m,
            Maximum = 1000000m,
            Increment = 1m,
            FormatString = "0.###",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _intervalNumericUpDown.ValueChanged += (s, e) => UpdateSettingsValue();

        _intervalUnitComboBox = new ComboBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            ItemsSource = new List<string> { "秒", "分", "时", "天", "星期", "月", "年" }
        };
        _intervalUnitComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        intervalPanel.Children.Add(_intervalNumericUpDown);
        intervalPanel.Children.Add(_intervalUnitComboBox);
        groupPanel.Children.Add(intervalPanel);

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _isLoading = true;
        try
        {
            _longitudeBox.Text = Settings.Longitude.ToString("F4");

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

        _intervalNumericUpDown.Value = Settings.Interval;
        _intervalUnitComboBox.SelectedIndex = Settings.IntervalUnit switch
            {
                "Second" => 0,
                "Minute" => 1,
                "Hour" => 2,
                "Day" => 3,
                "Week" => 4,
                "Month" => 5,
                "Year" => 6,
                _ => 1
            };
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateLongitude()
    {
        if (_isLoading) return;
        if (Settings == null) return;
        if (double.TryParse(_longitudeBox.Text, out double lon))
        {
            Settings.Longitude = Math.Clamp(lon, -180.0, 180.0);
        }
    }

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        var startDate = _startDatePicker.SelectedDate?.DateTime ?? new DateTime(2024, 1, 1);
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startDate.Month:D2}-{startDate.Day:D2}-{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        var endDate = _endDatePicker.SelectedDate?.DateTime ?? new DateTime(2024, 1, 1);
        var endTime = _endTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.EndTime = $"{endDate.Month:D2}-{endDate.Day:D2}-{endTime.Hours:D2}-{endTime.Minutes:D2}-{endTime.Seconds:D2}";

        Settings.Interval = _intervalNumericUpDown.Value ?? 1m;
        Settings.IntervalUnit = _intervalUnitComboBox.SelectedIndex switch
        {
            0 => "Second",
            1 => "Minute",
            2 => "Hour",
            3 => "Day",
            4 => "Week",
            5 => "Month",
            6 => "Year",
            _ => "Minute"
        };
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
