using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandMamianQunCeZhe", "马面裙 侧褶", true, SettingsPageCategory.Debug)]
public class MamianQunCeZhePage : SettingsPageBase
{
    private Border? _contentBorder;
    private List<TextBlock>? _paragraphTextBlocks;
    private List<TextBlock>? _sectionTextBlocks;
    private TextBlock? _backTextBlock;
    private TextBlock? _douyinLinkTextBlock;
    private TextBlock? _imageCaptionTextBlock;
    private Border? _imageContainer;
    private TextBlock? _douyinLinkTextBlock2;

    public MamianQunCeZhePage()
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

        var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", false);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Margin", new Thickness(16, 16, 16, 0));
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "马面裙既包含明代汉服，又包含清代、民国的非汉服，这里只展示明代汉服，清代、民国的非汉服请前往");

        var actionButton = new Button
        {
            Content = "马面裙 - 百度百科",
            FontSize = 12,
            Padding = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        actionButton.Click += OnBaiduLinkClick;
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "ActionButton", actionButton);

        mainPanel.Children.Add(infoBar);

        _backTextBlock = new TextBlock
        {
            Text = "‹ 返回上一级",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            Cursor = new Cursor(StandardCursorType.Hand),
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

        var imagePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var image = new Image
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 4, 0, 4)
        };

        _imageContainer = new Border
        {
            Child = image,
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 0
        };

        _imageContainer.Loaded += UpdateImageWidth;
        _imageContainer.SizeChanged += (s, e) =>
        {
            var parent = _imageContainer.Parent as Control;
            if (parent != null)
            {
                _imageContainer.Width = parent.Bounds.Width * 0.5;
            }
        };

        LoadLocalImage("Assets/hanfupage/mamianqun_structure.jpg", image);
        imagePanel.Children.Add(_imageContainer);

        _imageCaptionTextBlock = new TextBlock
        {
            Text = "马面裙 侧褶 结构示意",
            FontSize = 18,
            Foreground = ThemeHelper.GetTextBrush(),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        imagePanel.Children.Add(_imageCaptionTextBlock);

        panel.Children.Add(imagePanel);
        panel.Children.Add(new Border { Height = 8 });

        AddSection(panel, "我们一般说的马面裙指的绝大多数是侧褶马面裙，前后有2个平整的裙门。", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddParagraph(panel, "马面裙，又称“马面褶裙”，是中国汉服中的一种主要裙式。它由前后四个裙门组成，两两重合，外裙门有装饰，内裙门装饰较少或无装饰，侧面呈现出打裥[jiǎn]的特点。裙腰多用白色布，取白头偕老之意，以绳或纽固结。在裙的中间部分，裙门重合形成一个梯形的光面，俗称为“马面”。");
        AddParagraph(panel, "马面裙经由宋代旋裙发展而来，历经辽、金、元等历史阶段，逐渐形成了其独特的样式，在明代达到了成熟，最典型的马面裙流行于清代前后。其风格由明代的清新淡雅到清代的华丽富贵，再到民国的秀丽质朴（民国初马面裙继承清代风格，后来逐渐简约），但它的“马面”结构一直根深蒂固地存在着。五四运动后，受“民主、自由”等思想的影响，女裙与西方接近，传统女裙的元素渐渐消失，马面裙已走近末梢。");
        AddParagraph(panel, "2022年7月，迪奥推出一条标价2.9万元的中长半身裙，前后片交叠剪裁，因与中国传统马面裙的版型相似被指抄袭，引发了公众对于马面裙的热议。");
        AddParagraph(panel, "");
        AddSection(panel, "起源发展", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddSection(panel, "起源", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "“马面”一词，最早出现在《明宫史》中：“曳撒，其制后襟不断，而两傍有摆，前襟两截，而下有马面褶，往两旁起。”曳撒最早起源于元代的质孙服，形制为上衣下裳相连。下长过膝、下摆宽大、腰上打上细密横褶后缝以辫线的“腰线袄子”，便于骑射 [14]。但也有说马面裙的历史可以追溯到宋辽，因为宋辽的裙子已经具有马面裙的马面形制了。");
        AddParagraph(panel, "旋裙是宋代女子为方便骑驴而设计的一种功能性的“开胯之裙”。孟晖在《开衩之裙》中道：“此类宋裙乃是由两片面积相等，彼此独立的裙裾合成，做裙时，两扇裙片被部分地叠合在一起，再缝连到裙腰上。一些出土的文物也有马面裙蛛丝马迹，如山西晋祠彩陶中的一尊宋代侍女像上就有马面裙的影子。");
        AddParagraph(panel, "");
        AddSection(panel, "发展", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "宋代的旋裙发展到明代，逐渐形成了马面裙。明代成化年间，京城人士都喜欢着马面裙。上至一国之母下至黎民百姓，人人皆穿马面裙。只是不同的阶级，马面裙的质地、装饰和色彩都有着严格的区别。只是这时并无“马面裙”之名，裙式简单且未定型，色彩秀丽，整体给人清新淡雅的感觉。");
        AddParagraph(panel, "2024年春节，被称为“新春战袍”的汉服马面裙火了。在山东曹县，以马面裙为主的龙年拜年服，销售额超过3亿元，#曹县卖了3亿的马面裙依然供不应求#等话题冲上热搜。在电商平台，马面裙搜索量暴涨，各大春节晚会上也不乏马面裙的身影。");
        AddParagraph(panel, "截至2024年3月，曹县以马面裙为主的龙年拜年服销售额已超3亿元。在不少电商平台，马面裙颇为受欢迎，成交额也增长迅猛。");
        AddSection(panel, "制作工艺", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        var douyinPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        douyinPanel.Children.Add(new TextBlock
        {
            Text = "马面裙的制作工艺",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush()
        });
        _douyinLinkTextBlock = new TextBlock
        {
            Text = "https://www.douyin.com/video/7266267573635845432",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            Cursor = new Cursor(StandardCursorType.Hand),
            TextDecorations = TextDecorations.Underline
        };
        _douyinLinkTextBlock.PointerPressed += OnDouyinLinkClick;
        douyinPanel.Children.Add(_douyinLinkTextBlock);
        panel.Children.Add(douyinPanel);
        AddParagraph(panel, "");

        AddSection(panel, "款式", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");

        AddSection(panel, "明代马面裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "明制马面裙一般用7幅布幅，每3幅半拼成一片裙幅，两片裙幅围合成裙子；裙子的前后叠合的4个裙门保持平整，两侧打活褶，褶子大而疏，用异色的裙腰固定，裙腰两端缝缀系带；裙摆宽大，摆幅上用织或绣的形式缀饰底翩，或膝斓。裙襕的纹饰往往采用寓意丰富的吉祥图案，官宦之家的女性则用更加讲究的龙纹、云蟒纹等。马面和裙襕的组合，成为马面裙变化丰富、摇曳多姿的形制基础。");
        AddParagraph(panel, "");

        AddSection(panel, "月华裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "明朝末年，裙子增至有十幅布幅构成，腰间褶裥愈来愈密，每个褶裥的颜色不同，褶裥内花纹图案不同，色彩娴雅，轻描淡绘，风吹时如月华一般，故名“月华裙”。");
        AddParagraph(panel, "");

        AddSection(panel, "清代马面裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "在汉服页面内不考虑清代服装");
        AddParagraph(panel, "");

        AddSection(panel, "民初马面裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "在汉服页面内不考虑民初服装");
        AddParagraph(panel, "");

        AddSection(panel, "面料", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "马面裙的裙幅面料主要是由绸和缎构成，不同的款式面料的构成不尽相同。侧边有褶裥的裙子因为要打褶，要求面料不仅要好而且要有弹性，易于定型，像鱼鳞百褶裙这一类更需要隔缝之间用丝线连接，所以常常使用暗花绸面料，襕干式的裙子则喜欢用绸缎面料去产生大块面的效果。大部分马面裙都有衬里，衬里材质以绸居多，而夏天的马面裙一般没有衬里。");
        AddParagraph(panel, "");

        AddSection(panel, "缝制", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "传统的马面裙都是平面围合式结构，因当时布幅较窄，单幅面料宽度不能满足褶裥余量，所以常以多幅拼接。现代面料幅宽比较宽，单幅宽度也能满足需求。");
        AddParagraph(panel, "按照传统布幅计，可以裁6幅50~60cm宽、腰线到脚踝处长的布片，三三拼合成两大片作为两个裙幅；也可以按照现代的布幅，取150~180cm宽、裙长相同的两片布。将每一份大布片两端各留出35~38cm的裙门，中间剩余部分做成褶裥，褶数可4-11个不等，褶子可压成活褶（可用胶带固定或者预缝一下），也可以缉缝。");
        AddParagraph(panel, "打褶后，每片裙片的宽度变为（腰围+裙门宽×2）/2。将打好褶裥的两片裙幅裙门处叠合，仅在腰线处固定；然后上好裙腰，同时缝好系带。通常，裙子的褶子是靠裙腰的缉缝固定的，裙腰以下的褶是不固定的。熨烫整理。至此，一件马面裙便缝制完毕。");

        AddSection(panel, "文化特征", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "文化内涵", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "马面裙色彩较为鲜艳，以红色居多，蓝色也比较常见。红色是中国传统文化中最热衷的颜色，因为其代表着吉祥与喜庆。按照汉族的习俗，在女子婚嫁及节日等庆典中，一般都穿红色裙子。在清代，蓝色的马面裙也比较常见，作为当时的流行色，常与黑色和黄色搭配。");
        AddParagraph(panel, "马面裙的纹样可以分为单独的刺绣织物纹样和大面积的刺绣织物纹样图案，其中单独的刺绣织物纹样占了大多数。这其中每一种纹样所传递出来的寓意也会不同。");
        AddParagraph(panel, "如动物类纹样，在传统的历史发展中，龙凤纹样一直伴随着每一个中国人，龙凤纹样大多出现在每一个皇帝及后宫的嫔妃们的服装中，龙和凤还象征着男女之间爱情的美好姻缘。还有在现实生活中鱼的寓意，鱼谐音“余”，就有了年年有余的美好寓意。");
        AddParagraph(panel, "植物花草类纹样，通过花它们身上各自不同的品质给予图案美好的寓意，作为国花的牡丹，代表富贵；出淤泥而不染的莲花，代表玉洁冰清；菊花代表益寿延年；百合象征着神圣圣洁，纯洁与友谊，寓意百年好合，未来生活充满阳光。");
        AddParagraph(panel, "");

        AddSection(panel, "穿着搭配", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "在穿着方式上，明代女子的基本着装规则还是传统的上袄下裙，继续延续着传统着装形式。在这之中，将上半身的棉袄与下半身的马面裙或者间隔褶裙相搭配穿着便形成“袄裙”的穿着形式。");
        AddParagraph(panel, "");

        AddSection(panel, "历史价值", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "在汉服页面内不考虑清代及以后内容");
        AddParagraph(panel, "");


        AddSection(panel, "穿着方法", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "穿前准备", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "认识结构：马面裙前后共有四个“裙门”（平整的光面），两两重合，侧面打褶。裙腰两端通常缝有系带（或为松紧/拉链设计）。");
        AddParagraph(panel, "内搭准备：因马面裙侧面开衩且裙门交叠，遇风易被吹开，强烈建议内穿打底裤、安全裤或衬裙以防走光。");
        AddParagraph(panel, "");

        AddSection(panel, "传统一片式系带马面裙穿法（最经典款式）", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");
        AddSection(panel, "1. 找准定位）", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "分清正反：将裙子展开，确认正反面（绣花朝外，裙腰的白色布料部分通常朝上/朝外）。");
        AddParagraph(panel, "对准中线：将裙子置于身前，把正前方的“前裙门”（光面）严格对准身体正中线（肚脐下方）。");
        AddParagraph(panel, "确定高度：将裙腰提至肚脐或肚脐稍上方（高腰位置），确保裙摆自然垂落至脚踝或脚面。");
        AddParagraph(panel, "");
        AddSection(panel, "2. 包裹裙身）", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddSection(panel, "有孔款（带暗孔设计）：", 15, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "右手固定住右侧裙头，左手将左侧裙片从前往后绕过身体。");
        AddParagraph(panel, "将左侧的系带从腰侧的“暗孔/小孔”中由内向外穿出。");
        AddParagraph(panel, "再将右侧裙片从后往前绕回身前，覆盖在左侧裙片之上。");
        AddParagraph(panel, "");
        AddSection(panel, "无孔款：", 15, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "直接将左侧裙片从前往后绕至身后。");
        AddParagraph(panel, "再将右侧裙片覆盖在上面，从后往前绕回身前。");
        AddParagraph(panel, "");
        AddSection(panel, "3. 绕腰与固定", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "双手分别握住两根系带，在身前交叉，绕至身后，再交叉绕回身前（根据腰围和系带长度，通常绕腰2-3圈）。");
        AddParagraph(panel, "关键动作：绕带时务必用力拉紧，确保裙腰平整紧贴腰部，避免松垮导致裙子下滑。");
        AddParagraph(panel, "");
        AddSection(panel, "4. 整理裙门与褶子（防炸褶）", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "对齐裙门：检查前后裙门是否对准身体中轴线，避免左右歪斜露出内搭。");
        AddParagraph(panel, "理顺褶子：用手指轻捏两侧的“褶峰”（打褶的凸起处）向下提拉整理，确保褶子均匀垂顺，两侧褶量对称。若发现“炸褶”（褶子散乱），可将系带位置微调至裙头与裙腰结合处重新勒紧。");
        AddParagraph(panel, "");

        AddSection(panel, "现代改良版马面裙穿法（松紧腰/拉链款）", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "此类款式专为日常通勤改良，穿法最为简单：");
        AddParagraph(panel, "整理裙身：展开裙子，确认正反面，将前后裙门分别置于身体正前方与正后方。");
        AddParagraph(panel, "提拉穿入：双手撑开松紧腰头（或拉开侧边拉链），从脚部向上提拉至腰部（尽量避免从头部套入，以免弄乱发型或拉扯变形）。");
        AddParagraph(panel, "对齐固定：调整前后裙门对准身体中线，确保左右对称。");
        AddParagraph(panel, "整理褶子：同样需要轻捏褶峰向下提拉，确保两侧褶子均匀垂顺。");
        AddParagraph(panel, "");

        AddSection(panel, "系带的4种常见打结方法（针对系带款）", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "基础交叉绕圈法：两根系带在身前交叉，绕到身后，再绕回身前，直接在腰侧或正面系一个基础的蝴蝶结。");
        AddParagraph(panel, "双交叉法（更显精致）：在身前先交叉一次，再交叉一次，形成紧密的X型纹理，然后绕到身后或侧面系蝴蝶结，正面视觉效果更丰富。");
        AddParagraph(panel, "绕圈再交叉法（适合长系带）：系带从前面交叉绕到后面，再交叉一次回到腰侧，从腰内侧穿出，绕两圈后打结固定，能有效消耗过长的系带。");
        AddParagraph(panel, "隐藏式系法（防臃肿）：打好蝴蝶结后，将蝴蝶结的“耳朵”（线圈）塞进系带下方或裙腰内侧隐藏起来。这样可以保持腰部平整，穿短款上衣或袄子时不会显得腰部臃肿。");
        AddSection(panel, "作者提示：不建议把系带结放在正后方，不然很可能导致系带结压迫脊椎，导致一系列问题！",17, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());

        _douyinLinkTextBlock2 = new TextBlock
        {
            Text = "[抖音]马面裙9系法，主打抠细节，看一遍就会。",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            Cursor = new Cursor(StandardCursorType.Hand),
            TextDecorations = TextDecorations.Underline,
            TextWrapping = TextWrapping.Wrap
        };
        _douyinLinkTextBlock2.PointerPressed += OnDouyinLinkClick2;
        panel.Children.Add(_douyinLinkTextBlock2);
        AddParagraph(panel, "");

        AddSection(panel, "穿后检查与注意事项", 18, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "防走光加固：若遇大风天气，可在裙门内侧交叠处使用暗扣、别针或魔术贴进行临时固定。");
        AddParagraph(panel, "防掉筒技巧：如果腰围偏大导致裙子往下掉，可以在绕系带时多绕一圈，或者在裙腰内侧缝制/垫上一块防滑硅胶布。");
        AddParagraph(panel, "裙摆检查：穿好后原地转一圈，检查裙摆是否平行于地面，确保没有踩到裙边，且两侧褶子自然散开。");
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

    private void OnDouyinLinkClick(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.douyin.com/video/7266267573635845432",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
    private void OnDouyinLinkClick2(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.douyin.com/video/7347524297797143808",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
    private void OnBaiduLinkClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://baike.baidu.com/item/%E9%A9%AC%E9%9D%A2%E8%A3%99",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private void UpdateImageWidth(object? sender, RoutedEventArgs e)
    {
        var parent = _imageContainer?.Parent as Control;
        if (parent != null && _imageContainer != null)
        {
            _imageContainer.Width = parent.Bounds.Width * 0.5;
        }
    }

    private void LoadLocalImage(string assetPath, Image imageControl)
    {
        try
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var pluginDir = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(pluginDir))
            {
                imageControl.Source = null;
                return;
            }

            var fullPath = Path.Combine(pluginDir, assetPath);
            if (File.Exists(fullPath))
            {
                var bitmap = new Bitmap(fullPath);
                imageControl.Source = bitmap;
            }
            else
            {
                imageControl.Source = null;
            }
        }
        catch
        {
            imageControl.Source = null;
        }
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

        if (_douyinLinkTextBlock != null)
            _douyinLinkTextBlock.Foreground = GetAccentBrush();

        if (_douyinLinkTextBlock2 != null)
            _douyinLinkTextBlock2.Foreground = GetAccentBrush();

        if (_imageCaptionTextBlock != null)
            _imageCaptionTextBlock.Foreground = ThemeHelper.GetTextBrush();

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
