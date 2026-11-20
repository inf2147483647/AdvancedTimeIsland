using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 原「实时主界面帧率」组件的设置页。帧率功能已删除，设置内容替换为 Ghost Page。
/// </summary>
public class FpsMonitorSettingsControl : ComponentBase<FpsMonitorSettings>
{
    private readonly TextBlock _title = new()
    {
        Text = "Ghost Page",
        FontSize = 20,
        FontWeight = Avalonia.Media.FontWeight.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    private readonly TextBlock _body = new()
    {
        Text = "This page takes the place of pages that have been deleted. :ghost:",
        FontSize = 13,
        HorizontalAlignment = HorizontalAlignment.Center,
        TextWrapping = Avalonia.Media.TextWrapping.Wrap
    };

    public FpsMonitorSettingsControl()
    {
        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        };

        var icon = new LucideIcon
        {
            Glyph = "\uE20E",
            FontSize = 80,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stackPanel.Children.Add(icon);
        stackPanel.Children.Add(_title);
        stackPanel.Children.Add(_body);

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = stackPanel
        };
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        UpdateThemeColors();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void UpdateThemeColors()
    {
        _title.Foreground = ThemeHelper.GetTextBrush();
        _body.Foreground = ThemeHelper.GetSubTextBrush();
    }
}
