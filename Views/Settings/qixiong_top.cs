using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandQiXiongTop", "袄 衫 直领", true, SettingsPageCategory.Debug)]
public class QiXiongTopPage : SettingsPageBase
{
    private Border? _contentBorder;
    private List<TextBlock>? _paragraphTextBlocks;
    private List<TextBlock>? _sectionTextBlocks;
    private TextBlock? _backTextBlock;

    public QiXiongTopPage()
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
        _sectionTextBlocks = new List<TextBlock>();

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0)
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };

        _backTextBlock = new TextBlock
        {
            Text = "‹ 返回上一级",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            TextDecorations = TextDecorations.Underline,
            Margin = new Thickness(16, 12, 16, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        _backTextBlock.PointerPressed += OnBackClick;
        mainPanel.Children.Add(_backTextBlock);

        var border = new Border
        {
            Background = ThemeHelper.GetHanfuBackgroundBrush(),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(16, 12, 16, 16)
        };
        _contentBorder = border;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        var greenText = new TextBlock
        {
            Text = "是否在寻找\"齐胸襦裙\"？",
            FontSize = 20,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 128, 0)),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(greenText);

        var linkText = new TextBlock
        {
            Text = "立刻前往",
            FontSize = 20,
            Foreground = GetAccentBrush(),
            TextDecorations = TextDecorations.Underline,
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        linkText.PointerPressed += (s, e) =>
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandQiXiong?ci_keepHistory=true"));
        };
        panel.Children.Add(linkText);

        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸襦裙的上襦是搭配齐胸下裙的短款上衣，下摆整体束于胸上裙腰之内，常见对襟与交领两种核心形制，袖型涵盖窄袖、直袖、广袖等多种样式。唐代上襦领型丰富多元，除基础的直领、交领外，还衍生出圆领、方领、鸡心领、袒领等特色款式。");
        AddParagraph(panel, "");
        
        AddSection(panel, "简介", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸衫裙是隋唐五代时期的女性主流常服，属于汉民族传统服饰襦裙体系的重要分支。上襦是这套服饰的上衣核心单元，因下裙腰头高束于胸部以上，衣身下摆完全收束于裙腰内部，视觉呈现的重心集中于领部与袖型。");
        AddParagraph(panel, "");
        AddParagraph(panel, "传统服饰语境中，“襦”特指带有腰襕结构的复层短衣；而唐代齐胸搭配的上衣多为单层无腰襕的“衫”，质地轻薄透气。因此服饰史领域也将其严谨称为“上衫”，“齐胸襦裙”是后世流传更广的通用泛称。");
        AddParagraph(panel, "");
        
        AddSection(panel, "发展历程", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸上襦的形制雏形出现于南北朝时期，此时女子裙腰逐步升高，上衣承袭汉魏以来交领窄袖的传统样式，风格质朴，尚未形成独立的时代特征。隋代正式确立小袖短襦配高腰长裙的搭配范式，为唐代形制奠定了基础。");
        AddParagraph(panel, "");
        AddParagraph(panel, "初唐上襦延续隋代紧窄风格，以交领窄袖为主，早期部分款式带有背带结构；至盛唐开元年间，固定方式逐渐转为腋下系带，对襟直领成为主流，袖型由窄转宽，领口开放度大幅提升。中晚唐以后，上襦样式日趋宽大，贵族阶层中广袖大衫盛行，袖宽可达四尺以上，这一风格延续至五代，至宋代后随裙腰回落逐渐退出主流。");
        AddParagraph(panel, "");
        
        AddSection(panel, "基本特征", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸上襦的核心特征为短身收束，衣长通常及腰或胸下，下摆全部束入裙腰内侧，衣身两侧一般不开衩，整体依靠裙腰的束紧力固定版型。形制主要分为对襟与交领两大类，面料以丝织品为主，常与半臂、大袖衫、披帛搭配组成完整穿搭。");
        AddParagraph(panel, "");
        
        AddSection(panel, "特色", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸上襦与普通高腰襦裙的上衣存在明显适配差异：高腰襦裙的上衣下摆束于胸下腰上位置，衣身版型偏常规；齐胸上襦因需全部收于胸上裙腰内，衣长更短、版型更紧凑，无需考虑下摆外穿的垂坠效果。二者形制逻辑一致，均可制作成对襟或交领款式，遵循传统平面剪裁工艺。");
        AddParagraph(panel, "");
        
        AddSection(panel, "历史背景", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "隋唐五代的齐胸穿搭体系中，除核心的短襦与下裙外，半臂与披帛是重要的配套组成。半臂是套穿在上襦之外的短袖罩衫，袖长及肘，形制介于长袖与无袖裲裆之间，兼具装饰性与轻度保暖作用，在初唐至盛唐时期极为流行。");
        AddParagraph(panel, "");
        AddParagraph(panel, "隋代上襦普遍流行紧窄小袖，适配简约务实的社会风气；唐代早中期延续小袖形制，同时随着社会开放度提升，领型设计愈发多元，袒胸款式一度在贵族阶层盛行，体现出盛唐包容开放的时代风貌。盛唐之后，贵族服饰审美转向宽博华丽，上襦袖型与衣身持续加宽，装饰工艺也日趋繁复。");
        AddParagraph(panel, "");
        AddSection(panel, "上襦穿法", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddSection(panel, "1. 左右衣襟自然对齐垂放", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "2. 在胸前或腰侧用细带系合，或敞襟不系结", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "3. 将衣摆纳入裙腰之下，由裙腰压牢固定", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");

        border.Child = panel;
        mainPanel.Children.Add(border);
        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    private void OnBackClick(object? sender, PointerPressedEventArgs e)
    {
        FluentAvaloniaCompatibilityHelper.NavigateBack(this);
    }

    private void AddSection(StackPanel panel, string text, double fontSize, FontWeight fontWeight, IBrush foreground)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap
        };
        _sectionTextBlocks?.Add(textBlock);
        panel.Children.Add(textBlock);
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

        if (_sectionTextBlocks != null)
        {
            foreach (var tb in _sectionTextBlocks)
            {
                var currentColor = tb.Foreground?.ToString() ?? "";
                if (currentColor.Contains("FFADD8E6") || currentColor.Contains("FF4169E1"))
                {
                    tb.Foreground = ThemeHelper.GetLightBlueBrush();
                }
                else
                {
                    tb.Foreground = ThemeHelper.GetTextBrush();
                }
            }
        }
    }
}