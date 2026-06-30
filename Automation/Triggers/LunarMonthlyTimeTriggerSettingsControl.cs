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

public class LunarMonthlyTimeTriggerSettingsControl : TriggerSettingsControlBase<LunarMonthlyRuleSettings>
{
    private ComboBox _lunarDayComboBox = null!;
    private TimePicker _timePicker = null!;

    public LunarMonthlyTimeTriggerSettingsControl()
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

        var dayPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        dayPanel.Children.Add(new TextBlock
        {
            Text = "农历日:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _lunarDayComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 30; i++)
        {
            _lunarDayComboBox.Items.Add(i.ToString());
        }

        _lunarDayComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        dayPanel.Children.Add(_lunarDayComboBox);
        mainPanel.Children.Add(dayPanel);

        mainPanel.Children.Add(new TextBlock
        {
            Text = "选\"三十\"则每大月触发，选\"初一～廿九\"则每月触发",
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, -8, 0, 0)
        });

        var timePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        timePanel.Children.Add(new TextBlock
        {
            Text = "时间:",
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

        timePanel.Children.Add(_timePicker);
        mainPanel.Children.Add(timePanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        if (Settings.LunarDay > 0 && Settings.LunarDay <= 30)
        {
            _lunarDayComboBox.SelectedIndex = Settings.LunarDay - 1;
        }
        else
        {
            _lunarDayComboBox.SelectedIndex = 0;
        }

        var initialValue = Settings.TargetTime;
        ParseTimeString(initialValue, out int hour, out int minute, out int second);
        _timePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        Settings.LunarDay = _lunarDayComboBox.SelectedIndex + 1;

        var time = _timePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.TargetTime = $"{time.Hours:D2}-{time.Minutes:D2}-{time.Seconds:D2}";
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
