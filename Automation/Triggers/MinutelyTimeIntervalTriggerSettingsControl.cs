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

public class MinutelyTimeIntervalTriggerSettingsControl : TriggerSettingsControlBase<MinutelyTimeIntervalTriggerSettings>
{
    private TextBox _startSecondBox = null!;
    private TextBox _endSecondBox = null!;
    private NumericUpDown _intervalNumericUpDown = null!;
    private ComboBox _intervalUnitComboBox = null!;
    private bool _isLoading = false;

    public MinutelyTimeIntervalTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateSecondInputGroup("开始秒数:", true));
        mainPanel.Children.Add(CreateSecondInputGroup("结束秒数:", false));
        mainPanel.Children.Add(CreateIntervalGroup());

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateSecondInputGroup(string label, bool isStart)
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

        var secondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "秒数 (0-59)"
        };

        if (isStart)
            _startSecondBox = secondBox;
        else
            _endSecondBox = secondBox;

        secondBox.TextChanged += (s, e) => UpdateSettingsValue();
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(secondBox, (s, e) => ValidateAndFormatTextBox(secondBox));

        groupPanel.Children.Add(secondBox);

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
            if (int.TryParse(Settings.StartSecond, out int startSec))
            _startSecondBox.Text = startSec.ToString("D2");

        if (int.TryParse(Settings.EndSecond, out int endSec))
            _endSecondBox.Text = endSec.ToString("D2");

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
                _ => 0
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

        int startSecond = ParseSecond(_startSecondBox.Text);
        Settings.StartSecond = $"{startSecond:D2}";

        int endSecond = ParseSecond(_endSecondBox.Text);
        Settings.EndSecond = $"{endSecond:D2}";

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
            _ => "Second"
        };
    }

    private int ParseSecond(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }
}
