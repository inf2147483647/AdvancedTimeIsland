using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class TimeZoneWeeklyTimeTriggerSettingsControl : TriggerSettingsControlBase<TimeZoneWeeklyTimeRangeRuleSettings>
{
    private ComboBox _timeZoneComboBox = null!;
    private ComboBox _startDayOfWeekComboBox = null!;
    private TimePicker _startTimePicker = null!;

    public TimeZoneWeeklyTimeTriggerSettingsControl()
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
        mainPanel.Children.Add(CreateDayOfWeekPicker("触发星期:"));
        mainPanel.Children.Add(CreateTimePickerGroup("触发时间:"));

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

    private StackPanel CreateDayOfWeekPicker(string label)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        panel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var comboBox = new ComboBox
        {
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        comboBox.Items.Add("周一");
        comboBox.Items.Add("周二");
        comboBox.Items.Add("周三");
        comboBox.Items.Add("周四");
        comboBox.Items.Add("周五");
        comboBox.Items.Add("周六");
        comboBox.Items.Add("周日");

        _startDayOfWeekComboBox = comboBox;

        comboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        panel.Children.Add(comboBox);

        panel.Children.Add(new TextBlock
        {
            Text = "仅设置触发星期，不限制结束星期",
            Foreground = Brushes.Gray,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        });

        return panel;
    }

    private StackPanel CreateTimePickerGroup(string label)
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

        _startTimePicker = timePicker;

        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = timePicker
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
            if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZone)
            {
                _timeZoneComboBox.SelectedItem = item;
                break;
            }
        }

        if (Settings.StartDayOfWeek >= 0 && Settings.StartDayOfWeek < 7)
        {
            _startDayOfWeekComboBox.SelectedIndex = Settings.StartDayOfWeek;
        }
        else
        {
            _startDayOfWeekComboBox.SelectedIndex = 0;
        }

        var initialValue = Settings.StartTime;
        ParseTimeString(initialValue, out int hour, out int minute, out int second);
        _startTimePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateTimeZone()
    {
        if (Settings == null) return;
        if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz)
        {
            Settings.TimeZone = tz.Id;
        }
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        Settings.StartDayOfWeek = _startDayOfWeekComboBox.SelectedIndex;

        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";
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
