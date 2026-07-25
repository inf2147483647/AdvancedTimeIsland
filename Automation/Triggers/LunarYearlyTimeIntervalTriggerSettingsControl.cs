using System;
using System.Collections.Generic;
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

public class LunarYearlyTimeIntervalTriggerSettingsControl : TriggerSettingsControlBase<LunarYearlyTimeIntervalTriggerSettings>
{
    private ComboBox _startLunarMonthComboBox = null!;
    private ComboBox _startLunarDayComboBox = null!;
    private CheckBox _startLeapMonthCheckBox = null!;
    private TimePicker _startTimePicker = null!;

    private ComboBox _endLunarMonthComboBox = null!;
    private ComboBox _endLunarDayComboBox = null!;
    private CheckBox _endLeapMonthCheckBox = null!;
    private TimePicker _endTimePicker = null!;

    private NumericUpDown _intervalNumericUpDown = null!;
    private ComboBox _intervalUnitComboBox = null!;
    private bool _isLoading = false;

    public LunarYearlyTimeIntervalTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateLunarDateGroup("开始时间:", true));
        mainPanel.Children.Add(CreateLunarDateGroup("结束时间:", false));
        mainPanel.Children.Add(CreateIntervalGroup());

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

        var lunarMonthComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 12; i++)
        {
            lunarMonthComboBox.Items.Add(i.ToString());
        }

        lunarMonthComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        monthPanel.Children.Add(lunarMonthComboBox);

        var leapMonthCheckBox = new CheckBox
        {
            Content = "闰月",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        FluentAvaloniaCompatibilityHelper.AddCheckedHandler(leapMonthCheckBox, (s, e) => UpdateSettingsValue());
        FluentAvaloniaCompatibilityHelper.AddUncheckedHandler(leapMonthCheckBox, (s, e) => UpdateSettingsValue());

        monthPanel.Children.Add(leapMonthCheckBox);
        groupPanel.Children.Add(monthPanel);

        if (isStart)
        {
            _startLunarMonthComboBox = lunarMonthComboBox;
            _startLeapMonthCheckBox = leapMonthCheckBox;
        }
        else
        {
            _endLunarMonthComboBox = lunarMonthComboBox;
            _endLeapMonthCheckBox = leapMonthCheckBox;
        }

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

        var lunarDayComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 30; i++)
        {
            lunarDayComboBox.Items.Add(i.ToString());
        }

        lunarDayComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        dayPanel.Children.Add(lunarDayComboBox);
        groupPanel.Children.Add(dayPanel);

        if (isStart)
        {
            _startLunarDayComboBox = lunarDayComboBox;
        }
        else
        {
            _endLunarDayComboBox = lunarDayComboBox;
        }

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

        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        timePanel.Children.Add(timePicker);
        groupPanel.Children.Add(timePanel);

        if (isStart)
        {
            _startTimePicker = timePicker;
        }
        else
        {
            _endTimePicker = timePicker;
        }

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
            if (Settings.StartMonth > 0 && Settings.StartMonth <= 12)
        {
            _startLunarMonthComboBox.SelectedIndex = Settings.StartMonth - 1;
        }
        else
        {
            _startLunarMonthComboBox.SelectedIndex = 0;
        }

        _startLeapMonthCheckBox.IsChecked = Settings.StartIsLeapMonth;

        if (Settings.StartDay > 0 && Settings.StartDay <= 30)
        {
            _startLunarDayComboBox.SelectedIndex = Settings.StartDay - 1;
        }
        else
        {
            _startLunarDayComboBox.SelectedIndex = 0;
        }

        var startInitialValue = Settings.StartTime ?? "";
        ParseTimeString(startInitialValue, out int startHour, out int startMinute, out int startSecond);
        _startTimePicker.SelectedTime = new TimeSpan(startHour, startMinute, startSecond);

        if (Settings.EndMonth > 0 && Settings.EndMonth <= 12)
        {
            _endLunarMonthComboBox.SelectedIndex = Settings.EndMonth - 1;
        }
        else
        {
            _endLunarMonthComboBox.SelectedIndex = 0;
        }

        _endLeapMonthCheckBox.IsChecked = Settings.EndIsLeapMonth;

        if (Settings.EndDay > 0 && Settings.EndDay <= 30)
        {
            _endLunarDayComboBox.SelectedIndex = Settings.EndDay - 1;
        }
        else
        {
            _endLunarDayComboBox.SelectedIndex = 0;
        }

        var endInitialValue = Settings.EndTime ?? "";
        ParseTimeString(endInitialValue, out int endHour, out int endMinute, out int endSecond);
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

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        Settings.StartMonth = _startLunarMonthComboBox.SelectedIndex + 1;
        Settings.StartIsLeapMonth = _startLeapMonthCheckBox.IsChecked ?? false;
        Settings.StartDay = _startLunarDayComboBox.SelectedIndex + 1;

        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        Settings.EndMonth = _endLunarMonthComboBox.SelectedIndex + 1;
        Settings.EndIsLeapMonth = _endLeapMonthCheckBox.IsChecked ?? false;
        Settings.EndDay = _endLunarDayComboBox.SelectedIndex + 1;

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
        if (parts.Length >= 2 && int.TryParse(parts[1], out int mi)) minute = mi;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int s)) second = s;
    }
}
