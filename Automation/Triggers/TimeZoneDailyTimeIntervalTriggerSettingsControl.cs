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

public class TimeZoneDailyTimeIntervalTriggerSettingsControl : TriggerSettingsControlBase<TimeZoneDailyTimeIntervalTriggerSettings>
{
    private ComboBox _timeZoneComboBox = null!;
    private TimePicker _startTimePicker = null!;
    private TimePicker _endTimePicker = null!;
    private NumericUpDown _intervalNumericUpDown = null!;
    private ComboBox _intervalUnitComboBox = null!;
    private bool _isLoading = false;

    public TimeZoneDailyTimeIntervalTriggerSettingsControl()
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
        mainPanel.Children.Add(CreateTimePickerGroup("开始时间:", true));
        mainPanel.Children.Add(CreateTimePickerGroup("结束时间:", false));
        mainPanel.Children.Add(CreateIntervalGroup());

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
            Foreground = ThemeHelper.GetTextBrush(),
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
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var timePicker = new TimePicker
        {
            Width = 250,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        if (isStart)
            _startTimePicker = timePicker;
        else
            _endTimePicker = timePicker;

        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();
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
            foreach (var item in _timeZoneComboBox.Items)
        {
            if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
            {
                _timeZoneComboBox.SelectedItem = item;
                break;
            }
        }

        ParseTimeString(Settings.StartTime, out int startHour, out int startMinute, out int startSecond);
        _startTimePicker.SelectedTime = new TimeSpan(startHour, startMinute, startSecond);

        ParseTimeString(Settings.EndTime, out int endHour, out int endMinute, out int endSecond);
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

    private void UpdateTimeZone()
    {
        if (_isLoading) return;
        if (Settings == null) return;
        if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz)
        {
            Settings.TimeZoneId = tz.Id;
        }
    }

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        var endTime = _endTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.EndTime = $"{endTime.Hours:D2}-{endTime.Minutes:D2}-{endTime.Seconds:D2}";

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

    private void ParseTimeString(string value, out int hour, out int minute, out int second)
    {
        hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int h)) hour = h;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int m)) minute = m;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int s)) second = s;
    }
}
