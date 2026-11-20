using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 原「实时主界面帧率」组件。帧率功能已删除，组件内容固定为文本 “Ghost”。
/// 组件 Guid 保持不变，已有课表中的该组件不会失效。
/// </summary>
[ComponentInfo(
    "11223344-5566-7788-9900-aabbccddeefe",
    "实时主界面刷新率（调试）（ATI）",
    "\uE8E1",
    "实时显示ClassIsland主界面帧率"
)]
public class FpsMonitorControl : ComponentBase<FpsMonitorSettings>
{
    private readonly TextBlock _textBlock = new()
    {
        Text = "Ghost",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    public FpsMonitorControl()
    {
        Content = _textBlock;
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
        _textBlock.Foreground = ThemeHelper.GetTextBrush();
    }
}
