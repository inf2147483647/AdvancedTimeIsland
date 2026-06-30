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

public class MonthlyTimeTriggerSettingsControl : TriggerSettingsControlBase<MonthlyTimeRangeRuleSettings>
{
    private ComboBox _dayComboBox = null!;
    private TimePicker _timePicker = null!;

    public MonthlyTimeTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateDayPicker("触发日期:"));
        mainPanel.Children.Add(CreateTimePickerGroup("触发时间:"));

        mainPanel.Children.Add(new TextBlock
        {
            Text = "在每月的指定日期和时间触发",
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

    private StackPanel CreateDayPicker(string label)
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

        _dayComboBox = new ComboBox
        {
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 31; i++)
        {
            _dayComboBox.Items.Add(i.ToString());
        }

        _dayComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        panel.Children.Add(_dayComboBox);

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
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _timePicker = new TimePicker
        {
            Width = 300,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        groupPanel.Children.Add(_timePicker);

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        var initialValue = Settings.StartTime;
        ParseTimeString(initialValue, out int day, out int hour, out int minute, out int second);

        if (day >= 1 && day <= 31)
        {
            _dayComboBox.SelectedIndex = day - 1;
        }
        else
        {
            _dayComboBox.SelectedIndex = 0;
        }

        _timePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        var day = _dayComboBox.SelectedIndex + 1;
        var time = _timePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{day:D2}-{time.Hours:D2}-{time.Minutes:D2}-{time.Seconds:D2}";
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
