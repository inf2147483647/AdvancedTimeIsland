using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class YearlyTimeRuleSettingsControl : RuleSettingsControlBase<YearlyTimeRangeRuleSettings>
{
    private DatePicker _datePicker = null!;
    private TimePicker _timePicker = null!;

    public YearlyTimeRuleSettingsControl()
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

        mainPanel.Children.Add(CreateDateTimePickerGroup("时间点:"));

        mainPanel.Children.Add(new TextBlock
        {
            Text = "每年的这个时间",
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

    private StackPanel CreateDateTimePickerGroup(string label)
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

        _datePicker = new DatePicker
        {
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Left,
            YearVisible = false
        };

        _timePicker = new TimePicker
        {
            Width = 380,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _datePicker.SelectedDateChanged += (s, e) => UpdateSettingsValue();
        _timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        var pickerRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        pickerRow.Children.Add(_datePicker);
        pickerRow.Children.Add(_timePicker);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = pickerRow
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        var initialValue = Settings.StartTime;
        ParseTimeString(initialValue, out int month, out int day, out int hour, out int minute, out int second);

        if (month > 0 && day > 0)
        {
            _datePicker.SelectedDate = new DateTimeOffset(new DateTime(2024, month, day));
        }
        _timePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        var date = _datePicker.SelectedDate?.DateTime ?? new DateTime(2024, 1, 1);
        var time = _timePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{date.Month:D2}-{date.Day:D2}-{time.Hours:D2}-{time.Minutes:D2}-{time.Seconds:D2}";
    }

    private void ParseTimeString(string value, out int month, out int day, out int hour, out int minute, out int second)
    {
        month = 0; day = 0; hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int m)) month = m;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int d)) day = d;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int h)) hour = h;
        if (parts.Length >= 4 && int.TryParse(parts[3], out int mi)) minute = mi;
        if (parts.Length >= 5 && int.TryParse(parts[4], out int s)) second = s;
    }
}
