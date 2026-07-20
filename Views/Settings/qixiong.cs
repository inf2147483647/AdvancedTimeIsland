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
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandQiXiong", "齐胸襦裙", true, SettingsPageCategory.Debug)]
public class QiXiongPage : SettingsPageBase
{
    private Border? _contentBorder;
    private List<TextBlock>? _paragraphTextBlocks;
    private List<TextBlock>? _sectionTextBlocks;
    private TextBlock? _backTextBlock;

    public QiXiongPage()
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

        AddParagraph(panel, "齐胸襦裙是对隋唐五代时期特有的一种女子裙装的称呼，属于汉民族传统服饰。其裙腰束于胸部，有别于早期束于腰部的形制，一些服装史上亦称之为高腰衫裙。一般穿法为一件对襟衫衣或交领上襦搭配一条下裙。");
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸襦裙一般分为对襟和交领两种形制。它最早见于南北朝，经历了隋、唐、五代的发展，在唐朝仕女中非常盛行，至宋朝理学兴起后被历史淘汰。唐代的齐胸襦裙常搭配半臂与披帛，成为当时流行的装束。");
        AddParagraph(panel, "");

        AddSection(panel, "简介", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸衫裙（旧称高腰襦裙）是对隋唐五代时期特有的一种女子襦裙装的称呼，是汉民族传统服饰的一种。它最早见于南北朝，盛行于隋、唐、五代，直至宋朝理学兴起后才逐渐式微，历史绵延约千年。根据裙头的位置而得名，与高腰衫裙相区分，高腰衫裙系于胸部以下腰部以上，齐腰衫裙系于腰间，齐胸衫裙系于腋下。齐胸衫裙已有文物出土，新疆阿斯塔纳唐墓出土两条唐裙。");
        AddParagraph(panel, "");
        AddParagraph(panel, "在古代，一般女子的襦裙装裙子束的都不是很高，而南北朝到隋唐五代时期出现的一种裙腰束的非常高的襦裙，一些服装史上多称之为高腰襦裙，根据现在人们对它的考证，一般改称之为齐胸襦裙。唐代国家统一，经济繁荣，社会风尚也比较开放，服饰款式也是群芳争艳，瑰丽多姿，大气飘逸又华丽绚烂，比如襦衫领口的类型就有：圆领、方领、直领和鸡心领等多种。");
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸襦裙一般分为对襟齐胸襦裙和交领齐胸襦裙两种，其中对襟齐胸襦裙的使用范围更为广泛。齐胸襦裙在唐朝仕女中非常盛行，现在保留的不少古画、出土文物都有它的踪迹。");
        AddParagraph(panel, "");

        AddSection(panel, "发展历程", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸襦裙最早见于南北朝时期。在隋代及初唐时期，妇女的短襦都用小袖，下着紧身长裙，裙腰高系，一般都在腰部以上，有的甚至系在腋下，并以丝带系扎。隋唐时期，形制上略有变化，出现了小袖短襦，下装穿紧身长裙，裙腰束至腋下，增加了披帛。");
        AddParagraph(panel, "");
        AddParagraph(panel, "唐代之前出土的陶俑或绘画多见有背带的款式，发展到唐代开元年间变为用系带在腋下固定。盛唐以后，女服的样式日趋宽大，到了中晚唐时期，一般妇女服装袖宽往往四尺以上。");
        AddParagraph(panel, "");
        AddParagraph(panel, "北宋时期，妇女的服饰已经从唐代的齐胸襦裙逐渐将裙子高度下移，以齐腰襦裙的形制居多。齐胸襦裙经历了隋、唐、五代直到宋朝理学兴起才被历史淘汰，大约有1000年的历史。");
        AddParagraph(panel, "");

        AddSection(panel, "基本特征", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸襦裙是对隋唐五代时期特有的一种女子襦裙装的称呼，裙腰束得非常高，多在胸上。形制分为对襟齐胸襦裙和交领齐胸襦裙，裙分为一片式和两片式，穿法不同。常见搭配有大袖衫和披帛，面料以丝织品为主，裙色鲜艳如石榴红。齐胸襦裙在唐朝仕女中非常盛行，现在保留的不少古画、出土文物都有它的踪迹。");
        AddParagraph(panel, "");

        AddSection(panel, "特色", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "在古代，一般女子的襦裙装裙子束的都不是很高，而隋唐五代时期出现的一种裙腰束的非常高的襦裙，一些服装史上多称之为高腰襦裙。");
        AddParagraph(panel, "");
        AddParagraph(panel, "实际上高腰襦裙和齐胸襦裙也存在着一些区别。高腰襦裙的束带位置在胸以下腰以上。齐胸襦裙束带位置则是在胸以上。相同点就是不论高腰还是齐胸均可对襟或交领。");
        AddParagraph(panel, "");

        AddSection(panel, "历史背景", 21, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "齐胸襦裙最早见于南北朝，经历了隋、唐、五代直到宋朝理学兴起才被历史淘汰，大约有1000年的历史。唐代之前出土的陶俑，或绘画，多见有背带的款式，发展到唐代开元年间变为用系带在腋下固定。");
        AddParagraph(panel, "");
        AddParagraph(panel, "隋唐五代时，上衣为短襦，半臂（属于罩衫。半臂形制如同今短袖衫，因其袖子长度在长袖与裲裆之间，故称为半臂）与披肩（属于配饰）构成当时襦裙的重要组成部分。隋代，上襦又时兴小袖。唐代长期穿用小袖短襦和曳地长裙，但盛唐以后，贵族衣著又转向阔大拖沓。裙的式样用四幅连接缝合而成，上窄下宽，下垂至地，不施边缘。裙腰用绢条，两端缝有系带。这时上襦的领口变化多样，其中袒胸大袖衫一度流行，展示了盛唐思想解放的精神风貌。披肩从狭而长的披子演变而来，后来逐渐成为披之于双臂，舞之于前后的飘带，这是中国古代仕女的典型服饰，在盛唐及五代最为盛行。下裙面料以丝织品为主，以多幅为佳，裙腰上提，此时裙色鲜艳，多为深红、绛紫、月青、草绿等，其中以石榴红裙流行的时间最长，色彩多样，多中求异，让人眼花缭乱，目不暇接。如唐中宗的女儿安乐公主的百鸟裙，堪称中国织绣史上之名作；武则天时的响铃裙，将裙四角缀十二铃，行之随步，叮当作响，可谓千姿百态，美不胜收，与短襦和披肩相配一体，尽显盛唐女子雍容华贵的丰腴风韵，表现出极富诗意的美与韵律。");
        AddParagraph(panel, "");

        AddSection(panel, "一片式齐胸襦裙穿法步骤", 28, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");
        AddSection(panel, "1. 上襦系带", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "2. 套入裙子，先将后片裙子的系带绕到胸前系住", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "3. 提起前片盖住后片的带子，将前片的带子绕到背后交叉", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "4. 带子在背后交叉后绕回前面", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "5. 将带子绕回到胸前系住", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
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