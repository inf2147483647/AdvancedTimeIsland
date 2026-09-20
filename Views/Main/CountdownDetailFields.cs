using System;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 倒计时详情编辑区的通用字段构建器，统一采用 ClassIsland 官方 <see cref="Field"/> 控件
/// （标签在上、输入控件在下），与「档案编辑」的表单风格保持一致。
/// </summary>
internal static class CountdownDetailFields
{
    /// <summary>创建一个官方 Field（Label + 内容 + 可选后缀）。</summary>
    public static Field Labeled(string label, Control content, string? suffix = null)
    {
        var field = new Field
        {
            Label = label,
            Content = content
        };
        if (!string.IsNullOrEmpty(suffix))
        {
            field.Suffix = suffix;
        }
        return field;
    }

    /// <summary>单行文本字段。</summary>
    public static Control Text(string label, string? value, Action<string?> onChanged, string? watermark = null)
    {
        var textBox = new TextBox
        {
            Text = value,
            Watermark = watermark,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => onChanged(textBox.Text));
        return Labeled(label, textBox);
    }

    /// <summary>多行文本字段。</summary>
    public static Control MultilineText(string label, string? value, Action<string?> onChanged, int minHeight = 60)
    {
        var textBox = new TextBox
        {
            Text = value,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = minHeight,
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => onChanged(textBox.Text));
        return Labeled(label, textBox);
    }

    /// <summary>开关字段。</summary>
    public static Control Toggle(string label, bool isChecked, Action<bool> onChanged)
    {
        var toggle = new ToggleSwitch
        {
            IsChecked = isChecked,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        toggle.IsCheckedChanged += (s, e) => onChanged(toggle.IsChecked == true);
        return Labeled(label, toggle);
    }

    /// <summary>下拉选择字段。</summary>
    public static Control Combo(string label, ComboBox comboBox, Action onChanged)
    {
        comboBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        comboBox.SelectionChanged += (s, e) => onChanged();
        return Labeled(label, comboBox);
    }

    /// <summary>整数输入字段（失焦时提交，非法输入忽略）。</summary>
    public static Control Int(string label, int value, Action<int> onChanged, string? watermark = null)
    {
        var textBox = new TextBox
        {
            Text = value.ToString(),
            Watermark = watermark,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) =>
        {
            if (int.TryParse(textBox.Text?.Trim(), out var parsed))
            {
                onChanged(parsed);
                textBox.Text = parsed.ToString();
            }
            else
            {
                textBox.Text = value.ToString();
            }
        });
        return Labeled(label, textBox);
    }

    /// <summary>
    /// 通知设置分区：官方「更多选项」标题 + 开关 + 标题/内容/时长字段。
    /// 开关关闭时自动禁用下方输入区。
    /// </summary>
    public static Control NotificationSection(
        bool enabled, Action<bool> setEnabled,
        Func<string?> getTitle, Action<string?> setTitle,
        Func<string?> getContent, Action<string?> setContent,
        Func<int> getMask, Action<int> setMask,
        Func<int> getOverlay, Action<int> setOverlay)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        panel.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 4) });
        panel.Children.Add(new IconText { Glyph = "\uef2b", Text = "通知设置" });

        var fields = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        fields.Children.Add(Text("通知标题", getTitle(), setTitle, "倒计时到达"));
        fields.Children.Add(MultilineText("通知内容", getContent(), setContent));
        fields.Children.Add(Int("通知标题时长（秒）", getMask(), v => setMask(Math.Clamp(v, 1, 60))));
        fields.Children.Add(Int("通知内容时长（秒）", getOverlay(), v => setOverlay(Math.Clamp(v, 1, 60))));

        var toggle = new ToggleSwitch
        {
            IsChecked = enabled,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        toggle.IsCheckedChanged += (s, e) =>
        {
            var isOn = toggle.IsChecked == true;
            setEnabled(isOn);
            SetNotificationFieldsEnabled(fields, isOn);
        };

        panel.Children.Add(Labeled("启用通知", toggle));
        panel.Children.Add(fields);
        SetNotificationFieldsEnabled(fields, enabled);

        return panel;
    }

    /// <summary>根据开关状态启用/禁用通知详情字段（开关下方的输入区）。</summary>
    public static void SetNotificationFieldsEnabled(Panel fields, bool enabled)
    {
        foreach (var child in fields.Children)
        {
            child.IsEnabled = enabled;
        }
    }

    /// <summary>分区标题（官方 IconText 样式）。</summary>
    public static Control SectionHeader(string glyph, string text)
    {
        return new IconText
        {
            Glyph = glyph,
            Text = text,
            Margin = new Thickness(0, 4, 0, 0)
        };
    }
}
