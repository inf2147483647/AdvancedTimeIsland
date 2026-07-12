using System;
using AdvancedTimeIsland.Helpers;using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class MinutelyTimeRangeRuleSettingsControl : RuleSettingsControlBase<MinutelyTimeRangeRuleSettings>
{
    private TextBox _startSecondBox = null!;
    private TextBox _endSecondBox = null!;

    public MinutelyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
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

        mainPanel.Children.Add(CreateInputGroup("开始秒数:", true));
        mainPanel.Children.Add(CreateInputGroup("结束秒数:", false));

                Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateInputGroup(string label, bool isStart)
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

        var secondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "秒数 (0-59)"
        };

        if (isStart)
        {
            _startSecondBox = secondBox;
        }
        else
        {
            _endSecondBox = secondBox;
        }

        var initialValue = isStart ? Settings?.StartSecond ?? "" : Settings?.EndSecond ?? "";
        if (int.TryParse(initialValue, out int second))
        {
            secondBox.Text = second.ToString("D2");
        }

        // 输入时只更新值，不格式化
        secondBox.TextChanged += (s, e) => UpdateSettingsValue();

        // 失去焦点时验证并格式化
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(secondBox, (s, e) => ValidateAndFormatTextBox(secondBox));

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = secondBox
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
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
        if (Settings == null) return;

        int startSecond = ParseSecond(_startSecondBox.Text);
        Settings.StartSecond = $"{startSecond:D2}";

        int endSecond = ParseSecond(_endSecondBox.Text);
        Settings.EndSecond = $"{endSecond:D2}";
    }

    private int ParseSecond(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }
}



