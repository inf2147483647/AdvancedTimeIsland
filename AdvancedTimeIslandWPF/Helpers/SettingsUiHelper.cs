using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Core.Controls;
using MaterialDesignThemes.Wpf;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 设置页 UI 辅助：统一使用 ClassIsland 1.x 宿主提供的
/// MaterialDesign 主题资源与 SettingsCard/SettingsControl 标准控件，
/// 使插件设置页与 ClassIsland 原生设置页视觉一致并随主题自适应。
/// </summary>
public static class SettingsUiHelper
{
    /// <summary>
    /// 应用动态主题资源（等效 XAML 的 DynamicResource）。
    /// </summary>
    public static void SetDynamicResource(FrameworkElement element, DependencyProperty property, string resourceKey)
    {
        element.SetResourceReference(property, resourceKey);
    }

    /// <summary>
    /// 从宿主资源中获取主题画笔；找不到时回退到 ThemeHelper。
    /// </summary>
    public static Brush GetThemeBrush(string resourceKey)
    {
        if (Application.Current?.TryFindResource(resourceKey) is Brush brush)
        {
            return brush;
        }
        return ThemeHelper.GetTextBrush();
    }

    /// <summary>
    /// 创建 SettingsCard（带图标、标题、描述与右侧自定义开关内容）。
    /// </summary>
    public static SettingsCard CreateCard(
        string header,
        string description,
        PackIconKind icon,
        object? switcher = null)
    {
        var card = new SettingsCard
        {
            Header = header,
            Description = description,
            IconGlyph = icon
        };
        if (switcher != null)
        {
            card.Switcher = switcher;
        }
        return card;
    }

    /// <summary>
    /// 创建带开关的 SettingsCard，开关变化时触发回调。
    /// </summary>
    public static SettingsCard CreateToggleCard(
        string header,
        string description,
        PackIconKind icon,
        bool isOn,
        Action<bool>? onChanged = null)
    {
        var card = new SettingsCard
        {
            Header = header,
            Description = description,
            IconGlyph = icon,
            IsOn = isOn
        };
        if (onChanged != null)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(
                SettingsCard.IsOnProperty, typeof(SettingsCard));
            descriptor.AddValueChanged(card, (s, e) => onChanged(card.IsOn));
        }
        return card;
    }

    /// <summary>
    /// 创建带图标的小标题文本。
    /// </summary>
    public static TextBlock CreateTitle(string text, double fontSize = 20)
    {
        var title = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
        return title;
    }
}
