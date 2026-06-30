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

public class UnixTimestampTriggerSettingsControl : TriggerSettingsControlBase<UnixTimestampTriggerSettings>
{
    private TextBox _timestampTextBox = null!;

    public UnixTimestampTriggerSettingsControl()
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

        var timestampPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        timestampPanel.Children.Add(new TextBlock
        {
            Text = "目标时间戳 (秒):",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _timestampTextBox = new TextBox
        {
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _timestampTextBox.TextChanged += (s, e) => UpdateSettingsValue();

        timestampPanel.Children.Add(_timestampTextBox);
        mainPanel.Children.Add(timestampPanel);

        mainPanel.Children.Add(new TextBlock
        {
            Text = "Unix时间戳，单位为秒，使用64位有符号整数",
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

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        _timestampTextBox.Text = Settings.TargetTimestamp.ToString();
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        if (long.TryParse(_timestampTextBox.Text, out long timestamp))
        {
            Settings.TargetTimestamp = timestamp;
        }
    }
}
