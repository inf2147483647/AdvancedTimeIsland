using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class DailyTimeTriggerSettingsControl : TriggerSettingsControlBase<DailyTimeRangeRuleSettings>
{
    private WpfTimePicker _startTimePicker = null!;

    public DailyTimeTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateTimePickerGroup("触发时间:"));

        mainPanel.Children.Add(new TextBlock
        {
            Text = "在每天的指定时间触发",
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

    private bool _isLoading;

    private void LoadSettingsToUi()
    {
        _isLoading = true;
        try
        {
            if (Settings == null) return;

            var initialValue = Settings.StartTime;
            ParseTimeString(initialValue, out int hour, out int minute, out int second);
            _startTimePicker.SelectedTime = new TimeSpan(hour, minute, second);
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
