using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 精确时间范围规则设置控件
/// </summary>
public class ExactTimeRangeRuleSettingsControl : RuleSettingsControlBase<ExactTimeRangeRuleSettings>
{
    public ExactTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        // 开始时间输入
        var startPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        startPanel.Children.Add(new TextBlock
        {
            Text = "开始时间:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 70
        });
        var startTextBox = new TextBox
        {
            Watermark = "YYYY-MM-DD-hh-mm-ss",
            Width = 180,
            Text = Settings?.StartTime ?? ""
        };
        startTextBox.TextChanged += (s, e) =>
        {
            if (Settings != null)
                Settings.StartTime = startTextBox.Text ?? "";
        };
        startPanel.Children.Add(startTextBox);
        mainPanel.Children.Add(startPanel);

        // 结束时间输入
        var endPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        endPanel.Children.Add(new TextBlock
        {
            Text = "结束时间:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 70
        });
        var endTextBox = new TextBox
        {
            Watermark = "YYYY-MM-DD-hh-mm-ss",
            Width = 180,
            Text = Settings?.EndTime ?? ""
        };
        endTextBox.TextChanged += (s, e) =>
        {
            if (Settings != null)
                Settings.EndTime = endTextBox.Text ?? "";
        };
        endPanel.Children.Add(endTextBox);
        mainPanel.Children.Add(endPanel);

        Content = mainPanel;
    }
}
