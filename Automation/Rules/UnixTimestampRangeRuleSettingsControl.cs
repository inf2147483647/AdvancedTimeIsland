using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 绝对时间戳范围规则设置控件
/// 单位为秒，支持小数，精确到3位小数
/// </summary>
public class UnixTimestampRangeRuleSettingsControl : RuleSettingsControlBase<UnixTimestampRangeRuleSettings>
{
    private TextBox _startTimestampTextBox = null!;
    private TextBox _endTimestampTextBox = null!;
    private bool _isLoading;

    public UnixTimestampRangeRuleSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _isLoading = true;
        try
        {
            _startTimestampTextBox.Text = Settings.StartTimestamp.ToString();
            _endTimestampTextBox.Text = Settings.EndTimestamp.ToString();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 开始时间戳
        var startPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        startPanel.Children.Add(new TextBlock
        {
            Text = "开始时间戳 (秒):",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        _startTimestampTextBox = new TextBox
        {
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            Text = Settings?.StartTimestamp.ToString() ?? "0"
        };
        _startTimestampTextBox.TextChanged += (s, e) => UpdateSettingsValue();

        startPanel.Children.Add(_startTimestampTextBox);
        mainPanel.Children.Add(startPanel);

        // 结束时间戳
        var endPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        endPanel.Children.Add(new TextBlock
        {
            Text = "结束时间戳 (秒):",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        _endTimestampTextBox = new TextBox
        {
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            Text = Settings?.EndTimestamp.ToString() ?? "0"
        };
        _endTimestampTextBox.TextChanged += (s, e) => UpdateSettingsValue();

        endPanel.Children.Add(_endTimestampTextBox);
        mainPanel.Children.Add(endPanel);

        // 说明提示
        mainPanel.Children.Add(new TextBlock
        {
            Text = "支持小数，精确到3位小数，使用64位有符号浮点数",
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

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        if (double.TryParse(_startTimestampTextBox.Text, out double startTimestamp))
        {
            Settings.StartTimestamp = startTimestamp;
        }

        if (double.TryParse(_endTimestampTextBox.Text, out double endTimestamp))
        {
            Settings.EndTimestamp = endTimestamp;
        }
    }
}
