using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 图片响应式布局辅助类：让图片宽度跟随视口/父级宽度动态缩放，窗口缩放时自动调整。
/// </summary>
public static class ResponsiveImageHelper
{
    /// <summary>
    /// 让目标控件宽度按比例跟随其"宽度来源"祖先动态缩放，窗口缩放时自动更新。
    /// 宽度来源：向上遍历视觉树，取最近的具有实际宽度（Bounds.Width &gt; 0）的祖先，优先 TextBlock。
    /// </summary>
    /// <param name="target">需要调整宽度的控件（如容器 Border 或 Image）</param>
    /// <param name="fraction">相对宽度的比例（0.8 表示 80%）</param>
    /// <param name="applyAsMax">true 设置 MaxWidth（不超过该宽度），false 设置 Width（强制宽度）</param>
    public static void MakeWidthFollowAncestor(Control target, double fraction, bool applyAsMax = true)
    {
        double lastWidth = double.NaN;

        void Apply(Control source)
        {
            if (source.Bounds.Width <= 0) return;
            var w = source.Bounds.Width * fraction;
            if (Math.Abs(w - lastWidth) < 0.5) return;
            lastWidth = w;
            if (applyAsMax)
                target.MaxWidth = w;
            else
                target.Width = w;
        }

        target.Loaded += (_, _) =>
        {
            // 向上遍历视觉树：优先取具有实际宽度（Bounds.Width > 0）的祖先作为宽度来源
            Control? source = null;
            Control? fallback = null;
            var p = target.GetVisualParent();
            while (p != null)
            {
                if (p is Control c)
                {
                    if (c.Bounds.Width > 0)
                    {
                        source = c;
                        if (c is TextBlock) break;
                    }
                    else if (fallback == null)
                    {
                        fallback = c;
                    }
                }
                p = p.GetVisualParent();
            }
            if (source == null) source = fallback;
            if (source == null) return;

            Apply(source);
            source.SizeChanged += (_, _) => Apply(source);

            // 若 Loaded 时父级宽度尚未就绪（Bounds.Width 仍为 0），延迟重试一次
            if (source.Bounds.Width <= 0)
            {
                Dispatcher.UIThread.Post(() => Apply(source));
            }
        };
    }

    /// <summary>
    /// 让内联图片（InlineUIContainer 内）宽度跟随所在 TextBlock 宽度动态缩放。
    /// 不依赖 Loaded 事件（避免 InlineUIContainer 内布局循环），改用 AttachedToVisualTree + SizeChanged。
    /// </summary>
    /// <param name="reference">内联图片的容器控件</param>
    /// <param name="image">目标图片</param>
    /// <param name="widthSpec">宽度规格（"auto"/空/百分比会随窗口缩放，像素宽度保持固定）</param>
    /// <param name="defaultFraction">未指定宽度时的默认比例（如 0.9）</param>
    public static void MakeInlineWidthFollowTextBlock(Control reference, Image image, string widthSpec, double defaultFraction)
    {
        TextBlock? textBlock = null;
        double lastWidth = double.NaN;

        void Apply()
        {
            if (textBlock == null || textBlock.Bounds.Width <= 0) return;
            double viewportWidth = textBlock.Bounds.Width;
            double w;
            if (string.IsNullOrEmpty(widthSpec) || widthSpec.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                w = viewportWidth * defaultFraction;
            }
            else if (widthSpec.EndsWith("%", StringComparison.Ordinal) &&
                     double.TryParse(widthSpec.Substring(0, widthSpec.Length - 1), out var pct))
            {
                w = viewportWidth * pct / 100.0;
            }
            else
            {
                // 固定像素宽度，不随窗口缩放
                return;
            }
            if (Math.Abs(w - lastWidth) < 0.5) return;
            lastWidth = w;
            image.MaxWidth = w;
        }

        reference.AttachedToVisualTree += (_, _) =>
        {
            var p = reference.GetVisualParent();
            while (p != null)
            {
                if (p is TextBlock tb)
                {
                    textBlock = tb;
                    tb.SizeChanged += (_, _) => Apply();
                    break;
                }
                p = p.GetVisualParent();
            }
            Apply();

            // 若附加时 TextBlock 宽度尚未就绪，延迟重试一次
            if (textBlock == null || textBlock.Bounds.Width <= 0)
            {
                Dispatcher.UIThread.Post(() => Apply());
            }
        };
    }
}
