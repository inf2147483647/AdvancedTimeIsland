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

[SettingsPageInfo("AdvancedTimeIslandRuQun", "襦裙", true, SettingsPageCategory.Debug)]
public class RuQunPage : SettingsPageBase
{
    private Border? _contentBorder;
    private List<TextBlock>? _paragraphTextBlocks;
    private List<TextBlock>? _sectionTextBlocks;
    private TextBlock? _backTextBlock;
    private TextBlock? _qiXiongLinkTextBlock;

    public RuQunPage()
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

        AddParagraph(panel, "襦裙是中国古代女子典型的“上衣下裳”服饰，由短上衣（襦）与下裙组成，襦长不过膝，多搭配紧身长裙、披帛、半臂等配饰。其制式可追溯至西周时期晋国贵族已出现上身右衽短衣与下裙搭配的衣着，战国时期出现，于魏晋南北朝时期形成固定制式并进一步发展。");
        AddParagraph(panel, "");
        AddParagraph(panel, "按照形制差异，襦裙可分为齐腰、高腰、齐胸三类，按领型分为交领和直领，按工艺分为单层单襦和夹层复襦。");
        AddParagraph(panel, "");
        AddParagraph(panel, "汉代贵族礼服与普通女子日常穿搭中均有襦裙形式，可通过玉组佩、粉彩女俑等文物与服饰复原对比展现其具体形制。");
        AddParagraph(panel, "");
        AddParagraph(panel, "该服饰自战国至唐前期为民间女性主要着装，后逐渐被衫袄替代。");
        AddParagraph(panel, "");

        AddSection(panel, "形制", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "襦可分为单襦和复襦，区别在于是否夹里。");
        AddParagraph(panel, "");
        AddParagraph(panel, "襦的袖子一般较长。交领右衽汉服标准的领口式样，外观如字母y形。（左衽为异族或死者的样式，方向不可以相反）。");
        AddParagraph(panel, "");
        AddParagraph(panel, "腰带用丝或革制成，起固定作用。");
        AddParagraph(panel, "");
        AddParagraph(panel, "宫绦以丝带编成，一般在中间打几个环结，然后下垂至地，有的还在中间串上一块玉佩，借以压裙幅，使其不至散开影响美观。（非必需）");
        AddParagraph(panel, "");
        AddParagraph(panel, "裙从六幅到十二幅，有各种颜色及繁多的式样。");
        AddParagraph(panel, "");
        AddParagraph(panel, "与其它服装形制相比，襦裙有一个明显的特点：上衣短，下裙长，上下比例体现了黄金分割的要求，具有丰富的美学内涵。");
        AddParagraph(panel, "");
        AddParagraph(panel, "它们有一个共同的特点：平面裁剪，多缘边，绸带系结；上襦变化主要在领型及门襟上，下裙长至鞋面。");
        AddParagraph(panel, "");
        AddParagraph(panel, "大凡衣短则裙长，衣短至腰间，裙长至脚踝骨之下；衣长则裙阔，衣长时，长到臀至膝下，而裙露仅几寸，裙子不必显出特色，襦裙忌讳上下平分秋色，会显得呆板少变化。");
        AddParagraph(panel, "");

        AddSection(panel, "具体分类", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "按领子的式样不同，可分为交领襦裙和直领襦裙。");
        AddParagraph(panel, "");
        AddParagraph(panel, "按是否夹里的区别，将襦裙分为单襦和复襦，单襦近于衫，复襦则近于袄。");
        AddParagraph(panel, "");
        AddParagraph(panel, "按裙腰的高低，可分为中腰襦裙（同齐腰襦裙）、高腰襦裙和");

        var qiXiongPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        qiXiongPanel.Children.Add(new TextBlock
        {
            Text = "",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush()
        });
        _qiXiongLinkTextBlock = new TextBlock
        {
            Text = "齐胸襦裙",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            TextDecorations = TextDecorations.Underline
        };
        _qiXiongLinkTextBlock.PointerPressed += OnQiXiongLinkClick;
        qiXiongPanel.Children.Add(_qiXiongLinkTextBlock);
        qiXiongPanel.Children.Add(new TextBlock
        {
            Text = "",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush()
        });
        panel.Children.Add(qiXiongPanel);

        AddParagraph(panel, "");
        AddParagraph(panel, "【案：襦裙的确有齐胸襦裙，是唐代的，将唐代襦裙称为高腰襦裙有误；高腰襦裙束于胸下。】");
        AddParagraph(panel, "");

        AddSection(panel, "在宋代", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "一、整体造型与审美风格", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "1. 腰线回落，廓形修长", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "襦裙腰线从唐代的齐胸、高腰回落至腰部，以齐腰襦裙为绝对主流；整体廓形从唐代的宽博丰腴逐渐转向瘦长修身，裙长多拖地掩足，姿态更显端庄内敛。");
        AddParagraph(panel, "");
        AddSection(panel, "2. 气质素雅克制", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "受程朱理学“存天理、灭人欲”的观念影响，服饰摒弃大面积奢华装饰与浓艳配色，追求淡雅恬静的视觉效果，审美偏向内敛、温润。");
        AddParagraph(panel, "");

        AddSection(panel, "二、上襦的形制特征", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "1. 衣身短窄，束腰穿着", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "襦为短款上衣，衣长一般仅及腰间，穿着时多将下摆束入裙内，裙外再另系腰带，是典型的“上衣下裳”两截式结构。");
        AddParagraph(panel, "");
        AddSection(panel, "2. 领袖设计实用为主", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "领型以交领、直领对襟最为常见；袖型日常多为窄袖，兼顾劳作与行动便利，礼仪场合也有宽袖礼服款；腰身、袖口相对唐代更为宽松舒适。");
        AddParagraph(panel, "");
        AddSection(panel, "3. 品类随季节分化", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "按厚度可分为单襦（薄款，近于衫，春夏穿着）和复襦（夹棉衬里，称夹袄、棉袄，秋冬保暖）；衣身的领口、袖口边缘多做刺绣或拼接缘饰，作为低调点缀。");
        AddParagraph(panel, "");

        AddSection(panel, "三、下裙的形制特征", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "1. 褶裙为绝对主流", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "宋代裙装以多褶为标志性特色，时兴“千褶”“百迭”裙（即百迭裙），裙褶排列整齐紧密，是后世百褶裙的雏形；此外还有前后开衩的旋裙、两片式的资裙、鱼尾状的褶裥裙等特色款式。");
        AddParagraph(panel, "");
        AddSection(panel, "2. 廓形随时代演变", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "北宋初期仍沿袭晚唐五代遗风，裙身较肥阔；北宋中期至南宋，裙型逐渐收窄，变得瘦长窄俏，垂坠感更强。");
        AddParagraph(panel, "");
        AddSection(panel, "3. 色彩与阶层区分", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "裙装色彩通常比上襦更鲜亮，名贵品类有郁金香根染制的黄裙、经典的石榴红裙等；老年妇女与民间劳动女性多穿深色素裙，风格质朴实用。");
        AddParagraph(panel, "");
        AddSection(panel, "4. 专属配饰玉环绶", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "裙腰的飘带上常悬挂一枚玉制圆环，称为“玉环绶”，作用是压住裙幅，避免行走时裙摆随风散开失仪，兼具装饰性与礼仪功能，是宋代襦裙的标志性细节。");
        AddParagraph(panel, "");

        AddSection(panel, "男式襦裙", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "（没做男装...）");
        AddParagraph(panel, "");

        AddSection(panel, "注意事项", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "襦裙是中国古代服饰中一种上下裙装，但不可将所有上下裙装都称为襦裙。清朝的旗服褂裙也不是襦裙。");
        AddParagraph(panel, "");
        AddParagraph(panel, "襦裙和曲裾分属于汉服的两个不同类别。曲裾有左襟续任后绕，裙在衣内，衣襟至少过膝，不可作短襦看待（曲裾有相对的长短，不可等同于“深衣”这个概念，但是的确有“曲裾深衣”这个款式）。襦裙的下裙无类似曲裾的绕襟式样。");
        AddParagraph(panel, "");
        AddParagraph(panel, "衫裙约流行于魏晋时期，和襦的主要区别：魏晋南北朝时襦多指加絮短衣，而衫无絮，郭璞注曰：“今或呼衫为单襦”。");
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

    private void OnQiXiongLinkClick(object? sender, PointerPressedEventArgs e)
    {
        IAppHost.TryGetService<IUriNavigationService>()?
            .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandQiXiong?ci_keepHistory=true"));
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

        if (_qiXiongLinkTextBlock != null)
            _qiXiongLinkTextBlock.Foreground = GetAccentBrush();

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