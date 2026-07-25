using System;
using System.Collections.Generic;
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

public class HourlyTimeIntervalTriggerSettingsControl : TriggerSettingsControlBase<HourlyTimeIntervalTriggerSettings>
{
    private TextBox _startMinuteBox = null!;
    private TextBox _startSecondBox = null!;
    private TextBox _endMinuteBox = null!;
    private TextBox _endSecondBox = null!;
    private NumericUpDown _intervalNumericUpDown = null!;
    private ComboBox _intervalUnitComboBox = null!;
    private bool _isLoading = false;

    public HourlyTimeIntervalTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateTimeInputGroup("开始时间（分:秒）:", true));
        mainPanel.Children.Add(CreateTimeInputGroup("结束时间（分:秒）:", false));
        mainPanel.Children.Add(CreateIntervalGroup());

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateTimeInputGroup(string label, bool isStart)
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

        var inputPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var minuteBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "分钟 (0-59)"
        };

        var secondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "秒数 (0-59)"
        };

        if (isStart)
        {
            _startMinuteBox = minuteBox;
            _startSecondBox = secondBox;
        }
        else
        {
            _endMinuteBox = minuteBox;
            _endSecondBox = secondBox;
        }

        minuteBox.TextChanged += (s, e) => UpdateSettingsValue();
        secondBox.TextChanged += (s, e) => UpdateSettingsValue();

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(minuteBox, (s, e) => ValidateAndFormatTextBox(minuteBox));
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(secondBox, (s, e) => ValidateAndFormatTextBox(secondBox));

        inputPanel.Children.Add(minuteBox);
        inputPanel.Children.Add(secondBox);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = inputPanel
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private StackPanel CreateIntervalGroup()
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = "触发间隔:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var intervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _intervalNumericUpDown = new NumericUpDown
        {
            Width = 225,
            Minimum = 0.001m,
            Maximum = 1000000m,
            Increment = 1m,
            FormatString = "0.###",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _intervalNumericUpDown.ValueChanged += (s, e) => UpdateSettingsValue();

        _intervalUnitComboBox = new ComboBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            ItemsSource = new List<string> { "秒", "分", "时", "天", "星期", "月", "年" }
        };
        _intervalUnitComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        intervalPanel.Children.Add(_intervalNumericUpDown);
        intervalPanel.Children.Add(_intervalUnitComboBox);
        groupPanel.Children.Add(intervalPanel);

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _isLoading = true;
        try
        {
            ParseTimeString(Settings.StartTime, out int startMinute, out int startSecond);
        _startMinuteBox.Text = startMinute.ToString("D2");
        _startSecondBox.Text = startSecond.ToString("D2");

        ParseTimeString(Settings.EndTime, out int endMinute, out int endSecond);
        _endMinuteBox.Text = endMinute.ToString("D2");
        _endSecondBox.Text = endSecond.ToString("D2");

        _intervalNumericUpDown.Value = Settings.Interval;
        _intervalUnitComboBox.SelectedIndex = Settings.IntervalUnit switch
            {
                "Second" => 0,
                "Minute" => 1,
                "Hour" => 2,
                "Day" => 3,
                "Week" => 4,
                "Month" => 5,
                "Year" => 6,
                _ => 1
            };
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ValidateAndFormatTextBox(TextBox textBox)
    {
        var text = textBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            textBox.Text = "00";
            return;
        }

        if (double.TryParse(text, out double value))
        {
            int rounded = (int)Math.Round(value);
            int clamped = Math.Clamp(rounded, 0, 59);
            textBox.Text = clamped.ToString("D2");
        }
        else
        {
            textBox.Text = "00";
        }
    }

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        int startMinute = ParseMinute(_startMinuteBox.Text);
        int startSecond = ParseSecond(_startSecondBox.Text);
        Settings.StartTime = $"{startMinute:D2}-{startSecond:D2}";

        int endMinute = ParseMinute(_endMinuteBox.Text);
        int endSecond = ParseSecond(_endSecondBox.Text);
        Settings.EndTime = $"{endMinute:D2}-{endSecond:D2}";

        Settings.Interval = _intervalNumericUpDown.Value ?? 1m;
        Settings.IntervalUnit = _intervalUnitComboBox.SelectedIndex switch
        {
            0 => "Second",
            1 => "Minute",
            2 => "Hour",
            3 => "Day",
            4 => "Week",
            5 => "Month",
            6 => "Year",
            _ => "Minute"
        };
    }

    private int ParseMinute(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }

    private int ParseSecond(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }

    private void ParseTimeString(string value, out int minute, out int second)
    {
        minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int m)) minute = m;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int s)) second = s;
    }
}
