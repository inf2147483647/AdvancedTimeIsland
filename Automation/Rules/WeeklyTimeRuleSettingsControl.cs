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

namespace AdvancedTimeIsland.Automation.Rules;

public class WeeklyTimeRuleSettingsControl : RuleSettingsControlBase<WeeklyTimeRangeRuleSettings>
{
    private ComboBox _startDayOfWeekComboBox = null!;
    private TimePicker _startTimePicker = null!;

    public WeeklyTimeRuleSettingsControl()
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

        mainPanel.Children.Add(CreateDayOfWeekPicker("星期:"));
        mainPanel.Children.Add(CreateTimePickerGroup("时间点:"));

        mainPanel.Children.Add(new TextBlock
        {
            Text = "每周的这个时间",
            FontSize = 12,
            Foreground = Brushes.Gray
        });

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateDayOfWeekPicker(string label)
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
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center,
            Width = 80
        });

        _startDayOfWeekComboBox = new ComboBox
        {
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _startDayOfWeekComboBox.Items.Add("周一");
        _startDayOfWeekComboBox.Items.Add("周二");
        _startDayOfWeekComboBox.Items.Add("周三");
        _startDayOfWeekComboBox.Items.Add("周四");
        _startDayOfWeekComboBox.Items.Add("周五");
        _startDayOfWeekComboBox.Items.Add("周六");
        _startDayOfWeekComboBox.Items.Add("周日");

        _startDayOfWeekComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        panel.Children.Add(_startDayOfWeekComboBox);

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

        _startTimePicker = new TimePicker
        {
            Width = 300,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _startTimePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        groupPanel.Children.Add(_startTimePicker);

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

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
