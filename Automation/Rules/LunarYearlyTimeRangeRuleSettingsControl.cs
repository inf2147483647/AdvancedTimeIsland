using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
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
    private WpfTimePicker _startTimePicker = null!;

    private ComboBox _endMonthComboBox = null!;
    private ComboBox _endDayComboBox = null!;
    private CheckBox _endIsLeapMonthCheckBox = null!;
    private WpfTimePicker _endTimePicker = null!;
    private bool _isLoading;

    public LunarYearlyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _isLoading = true;
        try
        {
            if (Settings.StartMonth > 0 && Settings.StartMonth <= 12)
            {
                _startMonthComboBox.SelectedIndex = Settings.StartMonth - 1;
            }
            else
            {
                _startMonthComboBox.SelectedIndex = 0;
            }

            if (Settings.StartDay > 0 && Settings.StartDay <= 30)
            {
                _startDayComboBox.SelectedIndex = Settings.StartDay - 1;
            }
            else
            {
                _startDayComboBox.SelectedIndex = 0;
            }

            _startIsLeapMonthCheckBox.IsChecked = Settings.StartIsLeapMonth;

            ParseTimeString(Settings.StartTime, out int startHour, out int startMinute, out int startSecond);
            _startTimePicker.SelectedTime = new TimeSpan(startHour, startMinute, startSecond);

            if (Settings.EndMonth > 0 && Settings.EndMonth <= 12)
            {
                _endMonthComboBox.SelectedIndex = Settings.EndMonth - 1;
            }
            else
            {
                _endMonthComboBox.SelectedIndex = 0;
            }

            if (Settings.EndDay > 0 && Settings.EndDay <= 30)
            {
                _endDayComboBox.SelectedIndex = Settings.EndDay - 1;
            }
            else
            {
                _endDayComboBox.SelectedIndex = 0;
            }

            _endIsLeapMonthCheckBox.IsChecked = Settings.EndIsLeapMonth;

            ParseTimeString(Settings.EndTime, out int endHour, out int endMinute, out int endSecond);
            _endTimePicker.SelectedTime = new TimeSpan(endHour, endMinute, endSecond);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
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
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.Bold
        });

        // 农历月份和闰月
        var monthPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
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
        isLeapMonthCheckBox.Checked += (s, e) => UpdateSettingsValue();
        isLeapMonthCheckBox.Unchecked += (s, e) => UpdateSettingsValue();

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
            HorizontalAlignment = HorizontalAlignment.Left
        };

        timePanel.Children.Add(new TextBlock
        {
            Text = "时间:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var timePicker = new WpfTimePicker
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
        if (_isLoading) return;
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
