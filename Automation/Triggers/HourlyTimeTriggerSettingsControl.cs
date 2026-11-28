using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class HourlyTimeTriggerSettingsControl : TriggerSettingsControlBase<HourlyTimeRangeRuleSettings>
{
    private TextBox _startMinuteBox = null!;
    private TextBox _startSecondBox = null!;

    public HourlyTimeTriggerSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Dispatcher.BeginInvoke(new Action(() => LoadSettingsToUi()));
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

        mainPanel.Children.Add(CreateInputGroup("触发时间:"));

        mainPanel.Children.Add(new TextBlock
        {
            Text = "在每小时的指定分钟和秒触发",
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

    private StackPanel CreateInputGroup(string label)
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

        var inputPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        _startMinuteBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _startSecondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _startMinuteBox.TextChanged += (s, e) => UpdateSettingsValue();
        _startSecondBox.TextChanged += (s, e) => UpdateSettingsValue();

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startMinuteBox, (s, e) => ValidateAndFormatTextBox(_startMinuteBox));
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startSecondBox, (s, e) => ValidateAndFormatTextBox(_startSecondBox));

        inputPanel.Children.Add(_startMinuteBox);
        inputPanel.Children.Add(_startSecondBox);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = inputPanel
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private bool _isLoading;
    private bool _hasLoaded;

    private void LoadSettingsToUi()
    {
        if (_hasLoaded) return;
        if (Settings == null) return;
        _hasLoaded = true;
        _isLoading = true;
        try
        {

            var initialValue = Settings.StartTime;
            ParseTimeString(initialValue, out int minute, out int second);

            _startMinuteBox.Text = minute.ToString("D2");
            _startSecondBox.Text = second.ToString("D2");
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
        if (parts.Length >= 1 && int.TryParse(parts[0], out int mi)) minute = mi;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int s)) second = s;
    }
}
