using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using FluentAvalonia.UI.Controls;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandMamianQun", "马面裙", true, SettingsPageCategory.Debug)]
public class MamianQunPage : SettingsPageBase
{
    private Border? _contentBorder;
    private List<TextBlock>? _paragraphTextBlocks;
    private TextBlock? _backTextBlock;

    public MamianQunPage()
    {
        InitializeComponent();
    }

    private static IBrush GetAccentBrush()
    {
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            return new SolidColorBrush(accentColor);
        }
        if (Application.Current?.TryFindResource("AccentColor", out var accentObj) == true && accentObj is Color accentColor2)
        {
            return new SolidColorBrush(accentColor2);
        }
        return Brushes.DodgerBlue;
    }

    private void InitializeComponent()
    {
        _paragraphTextBlocks = new List<TextBlock>();

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0)
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            Margin = new Thickness(16, 0, 16, 16)
        };

        _backTextBlock = new TextBlock
        {
            Text = "‹ 返回汉服",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            Cursor = new Cursor(StandardCursorType.Hand),
            TextDecorations = TextDecorations.Underline,
            Margin = new Thickness(0, 12, 0, 12)
        };
        _backTextBlock.PointerPressed += OnBackClick;
        mainPanel.Children.Add(_backTextBlock);

        var border = new Border
        {
            Background = ThemeHelper.GetHanfuBackgroundBrush(),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0)
        };
        _contentBorder = border;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        AddParagraph(panel, "马面裙，又称“马面褶裙”，是中国汉服中的一种主要裙式。它由前后四个裙门组成，两两重合，外裙门有装饰，内裙门装饰较少或无装饰，侧面呈现出打裥[jiǎn]的特点。裙腰多用白色布，取白头偕老之意，以绳或纽固结。在裙的中间部分，裙门重合形成一个梯形的光面，俗称为“马面”。");
        AddParagraph(panel, "");
        AddParagraph(panel, "马面裙经由宋代旋裙发展而来，历经辽、金、元等历史阶段，逐渐形成了其独特的样式，在明代达到了成熟，最典型的马面裙流行于清代前后。其风格由明代的清新淡雅到清代的华丽富贵，再到民国的秀丽质朴（民国初马面裙继承清代风格，后来逐渐简约），但它的“马面”结构一直根深蒂固地存在着。五四运动后，受“民主、自由”等思想的影响，女裙与西方接近，传统女裙的元素渐渐消失，马面裙已走近末梢。");
        AddParagraph(panel, "");
        AddParagraph(panel, "2022年7月，迪奥推出一条标价2.9万元的中长半身裙，前后片交叠剪裁，因与中国传统马面裙的版型相似被指抄袭，引发了公众对于马面裙的热议。");

        border.Child = panel;
        mainPanel.Children.Add(border);
        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    private void OnBackClick(object? sender, PointerPressedEventArgs e)
    {
        var frame = this.FindAncestorOfType<Frame>();
        if (frame != null && frame.CanGoBack)
        {
            frame.GoBack();
        }
    }

    private void AddParagraph(StackPanel panel, string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        _paragraphTextBlocks?.Add(textBlock);
        panel.Children.Add(textBlock);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
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
        if (_contentBorder != null)
            _contentBorder.Background = ThemeHelper.GetHanfuBackgroundBrush();

        if (_backTextBlock != null)
            _backTextBlock.Foreground = GetAccentBrush();

        if (_paragraphTextBlocks != null)
        {
            foreach (var tb in _paragraphTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetSubTextBrush();
            }
        }
    }
}
