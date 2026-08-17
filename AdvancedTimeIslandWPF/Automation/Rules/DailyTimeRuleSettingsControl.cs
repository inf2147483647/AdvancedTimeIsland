using System;
using System.Windows;
using System.Windows.Controls;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class DailyTimeRuleSettingsControl : RuleSettingsControlBase<DailyTimeRangeRuleSettings>
{
    private WpfTimePicker _startTimePicker = null!;

    public DailyTimeRuleSettingsControl()
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
            HorizontalAlignment = HorizontalAlignment.Left
        };

        mainPanel.Children.Add(CreateTimePickerGroup("时间点:"));

        mainPanel.Children.Add(new TextBlock
        {
            Text = "每天的这个时间",
            FontSize = 12,
            Foreground = System.Windows.Media.Brushes.Gray
        });

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateTimePickerGroup(string label)
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

        _startTimePicker = new WpfTimePicker
        {
            Width = 250,
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

        var initialValue = Settings.StartTime;
        ParseTimeString(initialValue, out int hour, out int minute, out int second);
        _startTimePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

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
