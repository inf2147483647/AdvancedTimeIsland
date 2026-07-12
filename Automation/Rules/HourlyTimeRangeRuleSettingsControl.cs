using System;
using AdvancedTimeIsland.Helpers;using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class HourlyTimeRangeRuleSettingsControl : RuleSettingsControlBase<HourlyTimeRangeRuleSettings>
{
    private TextBox _startMinuteBox = null!;
    private TextBox _startSecondBox = null!;
    private TextBox _endMinuteBox = null!;
    private TextBox _endSecondBox = null!;

    public HourlyTimeRangeRuleSettingsControl()
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

        mainPanel.Children.Add(CreateInputGroup("开始时间:", true));
        mainPanel.Children.Add(CreateInputGroup("结束时间:", false));

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

        var initialValue = isStart ? Settings?.StartTime ?? "" : Settings?.EndTime ?? "";
        ParseTimeString(initialValue, out int minute, out int second);

        minuteBox.Text = minute.ToString("D2");
        secondBox.Text = second.ToString("D2");

        // 输入时只更新值，不格式化
        minuteBox.TextChanged += (s, e) => UpdateSettingsValue();
        secondBox.TextChanged += (s, e) => UpdateSettingsValue();

        // 失去焦点时验证并格式化
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

        int startMinute = ParseMinute(_startMinuteBox.Text);
        int startSecond = ParseSecond(_startSecondBox.Text);
        Settings.StartTime = $"{startMinute:D2}-{startSecond:D2}";

        int endMinute = ParseMinute(_endMinuteBox.Text);
        int endSecond = ParseSecond(_endSecondBox.Text);
        Settings.EndTime = $"{endMinute:D2}-{endSecond:D2}";
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



