using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class TimeZoneWeeklyTimeRangeRuleSettingsControl : RuleSettingsControlBase<TimeZoneWeeklyTimeRangeRuleSettings>
{
    private ComboBox _timeZoneComboBox = null!;
    private ComboBox _startDayOfWeekComboBox = null!;
    private ComboBox _endDayOfWeekComboBox = null!;
    private TimePicker _startTimePicker = null!;
    private TimePicker _endTimePicker = null!;

    public TimeZoneWeeklyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Settings != null)
        {
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
            if (Settings.EndDayOfWeek >= 0 && Settings.EndDayOfWeek < 7)
            {
                _endDayOfWeekComboBox.SelectedIndex = Settings.EndDayOfWeek;
            }
            else
            {
                _endDayOfWeekComboBox.SelectedIndex = 0;
            }
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

        mainPanel.Children.Add(CreateTimeZoneInputGroup());
        mainPanel.Children.Add(CreateDayOfWeekPicker("开始星期:", true));
        mainPanel.Children.Add(CreateDayOfWeekPicker("结束星期:", false));
        mainPanel.Children.Add(CreateTimePickerGroup("开始时间:", true));
        mainPanel.Children.Add(CreateTimePickerGroup("结束时间:", false));

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

    private StackPanel CreateDayOfWeekPicker(string label, bool isStart)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        panel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 80
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

        if (isStart)
        {
            _startDayOfWeekComboBox = comboBox;
        }
        else
        {
            _endDayOfWeekComboBox = comboBox;
        }

        comboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        panel.Children.Add(comboBox);

        return panel;
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

        var timePicker = new TimePicker
        {
            Width = 260,
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

        var initialValue = isStart ? Settings?.StartTime ?? "" : Settings?.EndTime ?? "";
        ParseTimeString(initialValue, out int hour, out int minute, out int second);

        timePicker.SelectedTime = new TimeSpan(hour, minute, second);

        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = timePicker
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
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
        Settings.EndDayOfWeek = _endDayOfWeekComboBox.SelectedIndex;

        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

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