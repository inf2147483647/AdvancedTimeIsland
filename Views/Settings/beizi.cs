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

[SettingsPageInfo("AdvancedTimeIslandBeiZi", "背子 褙子", true, SettingsPageCategory.Debug)]
public class BeiZiPage : SettingsPageBase
{
    private Border? _contentBorder;
    private List<TextBlock>? _paragraphTextBlocks;
    private List<TextBlock>? _sectionTextBlocks;
    private TextBlock? _backTextBlock;
    private TextBlock? _titleTextBlock;

    public BeiZiPage()
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
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 12, 0, 0)
        };

        _paragraphTextBlocks = new List<TextBlock>();
        _sectionTextBlocks = new List<TextBlock>();

        _backTextBlock = new TextBlock
        {
            Text = "‹ 返回上一级",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            TextDecorations = TextDecorations.Underline,
            Margin = new Thickness(16, 12, 16, 0)
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

        AddSection(panel, "褙子", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "女子褙子为常礼服，需搭配裙子、衫子穿着，合称为“裙褙”，褙子不一定要全缘边，但长度需垂至脚面。");
        AddParagraph(panel, "");
        AddParagraph(panel, "有主张要全缘边才能称为褙子，平台延续沈从文、黄能馥先生之观点，认为衣长至足。《古今图书集成·礼仪典·衣服部》引用《实录》：秦二世诏衫子上朝服加背子，其制袖短於衫，身与衫齐而大袖。今又长与裙齐，而袖才宽于衫。");
        AddParagraph(panel, "");

        AddSection(panel, "褙子与袄衫的差异", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "1. 穿着层级与功能定位不同", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "- 褙子：属于外层常礼服，定位类似现代的大衣、外搭开衫，是宋代女性正式场合的外层衣物，具备礼仪属性。它是宋代最具代表性的外穿服饰。");
        AddParagraph(panel, "- 袄衫：属于贴身内搭/基础便服，定位类似现代的T恤、衬衫，是日常贴身穿着的基础上衣。二者的区别在于厚薄：“衫”为单层单衣，适配春夏；“袄”带有里衬或夹棉，适配秋冬。");
        AddParagraph(panel, "");

        AddSection(panel, "2. 形制结构差异", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");

        AddSection(panel, "衣长区别", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "褙子以长款为主流，衣长通常过膝，长可及足踝，衣身垂坠修长；也有短款褙子，但礼仪属性弱于长款。");
        AddParagraph(panel, "衫多为短款，衣长至腰或臀部；袄有短袄与长袄之分，但作为内搭使用时，整体衣长通常短于同风格的外穿褙子。");
        AddParagraph(panel, "");

        AddSection(panel, "侧衩设计", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "褙子两侧开高衩，衩口可延伸至腋下位置，行走时衣摆灵动飘逸，同时保证活动便利性。");
        AddParagraph(panel, "袄衫侧缝开衩很低，甚至腋下位置不开衩，版型更贴合身形，适配贴身穿着的保暖与舒适需求。");
        AddParagraph(panel, "");

        AddSection(panel, "装饰细节", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "褙子的领口、袖口、衣襟下摆通常带有精致的镶边、印绣缘饰，装饰性强，是整套造型的视觉重点。");
        AddParagraph(panel, "袄衫作为内搭，装饰相对简约，以素面或简单纹样为主，避免与外层服饰产生视觉冲突。");
        AddParagraph(panel, "");

        AddSection(panel, "领型与穿法", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "褙子以直领对襟为典型样式，前襟自然敞开、不施纽扣，或仅在腰间松束腰带，风格舒展雅致。");
        AddParagraph(panel, "袄衫领型更丰富，包含对襟、交领等多种样式，可通过系带系合闭合，更贴合身形、保暖性更强。");
        AddParagraph(panel, "");

        AddSection(panel, "3. 穿搭规则不同", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "- 褙子遵循“不单穿”的礼仪原则，正式穿搭时内部必须搭配袄、衫、抹胸等内搭，形成“内衫袄+外褙子”的层次，正如图中宋代古画里红褙子内搭白衫子的装束逻辑。");
        AddParagraph(panel, "- 袄衫适配场景更灵活：既可单独作为日常便服穿着，也可作为内搭，配合褙子、大袖等外层礼服使用。");
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