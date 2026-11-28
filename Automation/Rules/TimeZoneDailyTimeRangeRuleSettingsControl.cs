using System;
using System.Windows;
using System.Windows.Controls;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 区时每天时间范围规则设置控件（带时区选择）
/// </summary>
public class TimeZoneDailyTimeRangeRuleSettingsControl : RuleSettingsControlBase<TimeZoneDailyTimeRangeRuleSettings>
{
    private ComboBox _timeZoneComboBox = null!;
    private WpfTimePicker _startTimePicker = null!;
    private WpfTimePicker _endTimePicker = null!;
    private bool _isLoading;

    public TimeZoneDailyTimeRangeRuleSettingsControl()
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
        }
        finally
        {
            _isLoading = false;
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        if (Settings != null)
        {
            foreach (var item in _timeZoneComboBox.Items)
            {
                if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
                {
                    _timeZoneComboBox.SelectedItem = item;
                    break;
                }
            }
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

        // 时区选择下拉框
        mainPanel.Children.Add(CreateTimeZoneInputGroup());

        // 开始时间
        mainPanel.Children.Add(CreateTimePickerGroup("开始时间:", true));

        // 结束时间
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
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        // 时间选择器
        var timePicker = new WpfTimePicker
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

        // 解析初始值
        var initialValue = isStart ? Settings?.StartTime ?? "" : Settings?.EndTime ?? "";
        ParseTimeString(initialValue, out int hour, out int minute, out int second);

        timePicker.SelectedTime = new TimeSpan(hour, minute, second);

        // 监听变化
        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        // 用ScrollViewer包裹
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
            Settings.TimeZoneId = tz.Id;
        }
    }

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        // 开始时间
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        // 结束时间
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
