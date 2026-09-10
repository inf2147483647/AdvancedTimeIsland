using System;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// 复用的设置分组折叠栏工厂，供各主界面组件设置面板使用。
/// 统一折叠栏的标题样式，并让标题颜色随主题深浅自适应。
/// </summary>
public static class SettingsGroupFactory
{
    /// <summary>
    /// 创建一个默认展开的分组折叠栏。
    /// </summary>
    /// <param name="title">折叠栏标题，如"文案设置"。</param>
    /// <param name="content">折叠栏内容控件。</param>
    public static Expander Create(string title, Control content)
    {
        var header = new TextBlock { Text = title, Foreground = ThemeHelper.GetTextBrush() };
        var expander = new Expander
        {
            Header = header,
            Content = content,
            IsExpanded = true
        };

        if (Application.Current != null)
        {
            void OnThemeVariantChanged(object? sender, EventArgs e)
            {
                header.Foreground = ThemeHelper.GetTextBrush();
            }

            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
            expander.DetachedFromVisualTree += (_, _) =>
                Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }

        return expander;
    }
}
