using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 每农历年规则设置控件
/// 格式：L_MM-L_DD-hh-mm-ss
/// </summary>
public class LunarYearlyRuleSettingsControl : RuleSettingsControlBase<LunarYearlyRuleSettings>
{
    private ComboBox _lunarMonthComboBox = null!;
    private ComboBox _lunarDayComboBox = null!;
    private TimePicker _timePicker = null!;

    public LunarYearlyRuleSettingsControl()
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

        var monthPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        monthPanel.Children.Add(new TextBlock
        {
            Text = "农历月:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _lunarMonthComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 12; i++)
        {
            _lunarMonthComboBox.Items.Add(i.ToString());
        }

        _lunarMonthComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        monthPanel.Children.Add(_lunarMonthComboBox);
        mainPanel.Children.Add(monthPanel);

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

        if (Settings.LunarMonth > 0 && Settings.LunarMonth <= 12)
        {
            _lunarMonthComboBox.SelectedIndex = Settings.LunarMonth - 1;
        }
        else
        {
            _lunarMonthComboBox.SelectedIndex = 0;
        }

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

        Settings.LunarMonth = _lunarMonthComboBox.SelectedIndex + 1;
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
