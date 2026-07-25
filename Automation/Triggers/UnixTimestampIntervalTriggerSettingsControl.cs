using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class UnixTimestampIntervalTriggerSettingsControl : TriggerSettingsControlBase<UnixTimestampIntervalTriggerSettings>
{
    private TextBox _startTimestampTextBox = null!;
    private TextBox _endTimestampTextBox = null!;
    private TextBox _intervalTimestampTextBox = null!;
    private bool _isLoading = false;

    public UnixTimestampIntervalTriggerSettingsControl()
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

        mainPanel.Children.Add(CreateTimestampInputGroup("开始时间戳:", out _startTimestampTextBox));
        mainPanel.Children.Add(CreateTimestampInputGroup("结束时间戳:", out _endTimestampTextBox));
        mainPanel.Children.Add(CreateIntervalInputGroup());

        mainPanel.Children.Add(new TextBlock
        {
            Text = "单位为秒，支持小数，精确到3位小数",
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

    private StackPanel CreateTimestampInputGroup(string label, out TextBox textBox)
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
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        textBox = new TextBox
        {
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "输入时间戳"
        };
        textBox.TextChanged += (s, e) => UpdateSettingsValue();

        var button = new Button
        {
            Content = "选取当前时间戳",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        var textBoxRef = textBox;
        button.Click += (s, e) => SetCurrentTimestamp(textBoxRef);

        inputPanel.Children.Add(textBox);
        inputPanel.Children.Add(button);

        groupPanel.Children.Add(inputPanel);

        return groupPanel;
    }

    private StackPanel CreateIntervalInputGroup()
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = "触发间隔时间戳:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        _intervalTimestampTextBox = new TextBox
        {
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "输入间隔时间戳"
        };
        _intervalTimestampTextBox.TextChanged += (s, e) => UpdateSettingsValue();

        groupPanel.Children.Add(_intervalTimestampTextBox);

        return groupPanel;
    }

    private void SetCurrentTimestamp(TextBox textBox)
    {
        var currentTimestamp = UnixTimeHelper.GetCurrentUnixTimestampDouble();
        textBox.Text = currentTimestamp.ToString("F3", CultureInfo.InvariantCulture);
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _isLoading = true;
        try
        {
            _startTimestampTextBox.Text = Settings.StartTimestamp.ToString("F3", CultureInfo.InvariantCulture);
            _endTimestampTextBox.Text = Settings.EndTimestamp.ToString("F3", CultureInfo.InvariantCulture);
            _intervalTimestampTextBox.Text = Settings.IntervalTimestamp.ToString("F3", CultureInfo.InvariantCulture);
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

        if (double.TryParse(_startTimestampTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double startTimestamp))
        {
            Settings.StartTimestamp = startTimestamp;
        }

        if (double.TryParse(_endTimestampTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double endTimestamp))
        {
            Settings.EndTimestamp = endTimestamp;
        }

        if (double.TryParse(_intervalTimestampTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double intervalTimestamp))
        {
            Settings.IntervalTimestamp = intervalTimestamp;
        }
    }
}
