using System;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class ExactTimeTriggerSettingsControl : TriggerSettingsControlBase<ExactTimeRuleSettings>
{
    private DatePicker _datePicker = null!;
    private TimePicker _timePicker = null!;

    public ExactTimeTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateDateTimePickerGroup("触发时间:"));

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
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        _datePicker = new DatePicker
        {
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _timePicker = new TimePicker
        {
            Width = 250,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _datePicker.SelectedDateChanged += (s, e) => UpdateSettingsValue();
        _timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        groupPanel.Children.Add(_datePicker);
        groupPanel.Children.Add(_timePicker);

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        var initialValue = Settings.TargetTime;
        ParseDateTimeString(initialValue, out int year, out int month, out int day, out int hour, out int minute, out int second);

        if (year > 0 && month > 0 && day > 0)
        {
            _datePicker.SelectedDate = new DateTimeOffset(new DateTime(year, month, day));
        }
        _timePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        var date = _datePicker.SelectedDate?.DateTime ?? DateTime.Today;
        var time = _timePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.TargetTime = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}-{time.Hours:D2}-{time.Minutes:D2}-{time.Seconds:D2}";
    }

    private void ParseDateTimeString(string value, out int year, out int month, out int day, out int hour, out int minute, out int second)
    {
        year = 0; month = 0; day = 0; hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int y)) year = y;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int m)) month = m;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int d)) day = d;
        if (parts.Length >= 4 && int.TryParse(parts[3], out int h)) hour = h;
        if (parts.Length >= 5 && int.TryParse(parts[4], out int mi)) minute = mi;
        if (parts.Length >= 6 && int.TryParse(parts[5], out int s)) second = s;
    }
}
