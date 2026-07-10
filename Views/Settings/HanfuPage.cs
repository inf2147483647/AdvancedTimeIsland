using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using FluentAvalonia.UI.Controls;
using Markdig;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHanfu", "汉服", SettingsPageCategory.Debug)]
public class HanfuPage : SettingsPageBase
{
    private readonly PluginSettings? _pluginSettings;

    private TextBlock? _maleTitleTextBlock;
    private TextBlock? _maleSubtitleTextBlock;
    private TextBlock? _maleDescriptionTextBlock;
    private List<TextBlock>? _sectionTextBlocks;
    private List<TextBlock>? _paragraphTextBlocks;
    private Border? _femaleContentBorder;
    private Border? _maleTitleTooltipBorder;
    private TextBlock? _maleTitleTooltipTextBlock;
    private TextBlock? _femaleGuideLinkTextBlock;

    public HanfuPage() : this(null)
    {
    }

    public HanfuPage(PluginSettings? pluginSettings)
    {
        _pluginSettings = pluginSettings;
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
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0)
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0),
            Spacing = 0
        };

        if (_pluginSettings == null || !_pluginSettings.EnableExperimentalFeatures)
        {
            var warningBar = new InfoBar
            {
                Severity = InfoBarSeverity.Warning,
                Message = "此页面为实验性功能，需要在插件设置中启用实验性功能才能查看完整内容。",
                IsOpen = true,
                IsClosable = false,
                Margin = new Thickness(16, 16, 16, 0)
            };
            mainPanel.Children.Add(warningBar);
            scrollViewer.Content = mainPanel;
            Content = scrollViewer;
            return;
        }

        var tabControl = new TabControl
        {
            Margin = new Thickness(0),
            Height = double.NaN
        };

        var maleTab = new TabItem
        {
            Header = "男装",
            Content = CreateMaleContent()
        };
        tabControl.Items.Add(maleTab);

        var femaleTab = new TabItem
        {
            Header = "女装",
            Content = CreateFemaleContent()
        };
        tabControl.Items.Add(femaleTab);

        mainPanel.Children.Add(tabControl);
        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    private Control CreateMaleContent()
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(16)
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0),
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _maleTitleTextBlock = new TextBlock
        {
            Text = ":(",
            FontSize = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = ThemeHelper.GetTextBrush(),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        _maleTitleTooltipTextBlock = new TextBlock
        {
            Text = "目前这个页面开发意义不大，因为在汉服运动中，女生占比约80%（在开发者所在地——开封，可能超过95%），所以需要先满足大多数用户的使用需求，等到插件主要功能完善后在开发这个页面。你也可以反馈插件问题，帮助开发者进行完善功能。<delete_line>尽管作者也属于那5%的群体[大雾]</delete_line>",
            FontSize = 12,
            Foreground = ThemeHelper.GetTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300
        };

        _maleTitleTooltipBorder = new Border
        {
            Background = ThemeHelper.IsDarkTheme()
                ? new SolidColorBrush(Color.FromRgb(30, 30, 30))
                : new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            BorderBrush = ThemeHelper.GetGrayBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Child = _maleTitleTooltipTextBlock
        };

        ToolTip.SetTip(_maleTitleTextBlock, _maleTitleTooltipBorder);
        ToolTip.SetPlacement(_maleTitleTextBlock, PlacementMode.Pointer);

        panel.Children.Add(_maleTitleTextBlock);

        _maleSubtitleTextBlock = new TextBlock
        {
            Text = "欧呦！你似乎进入了一个尚未动工的页面",
            FontSize = 25,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = ThemeHelper.GetSubTextBrush()
        };
        panel.Children.Add(_maleSubtitleTextBlock);

        _maleDescriptionTextBlock = new TextBlock
        {
            Text = "由于开发者精力有限，这个页面可能需要较久的时间来实现，敬请期待。",
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = ThemeHelper.GetGrayBrush(),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 600
        };
        panel.Children.Add(_maleDescriptionTextBlock);

        scrollViewer.Content = panel;
        return scrollViewer;
    }

    private Control CreateFemaleContent()
    {
        _sectionTextBlocks = new List<TextBlock>();
        _paragraphTextBlocks = new List<TextBlock>();

        var border = new Border
        {
            Background = ThemeHelper.GetHanfuBackgroundBrush(),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(16)
        };
        _femaleContentBorder = border;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        var guidePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };

        guidePanel.Children.Add(new TextBlock
        {
            Text = "想要实战汉服？试试学习",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush()
        });

        _femaleGuideLinkTextBlock = new TextBlock
        {
            Text = "汉服怎么穿",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            Cursor = new Cursor(StandardCursorType.Hand),
            TextDecorations = TextDecorations.Underline
        };
        _femaleGuideLinkTextBlock.PointerPressed += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://jingyan.baidu.com/article/fdffd1f87b056bf3e98ca107.html",
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        };
        guidePanel.Children.Add(_femaleGuideLinkTextBlock);
        guidePanel.Children.Add(new TextBlock
        {
            Text = "。",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush()
        });

        panel.Children.Add(guidePanel);
        panel.Children.Add(new Border { Height = 8 });

        AddSection(panel, "女款汉服", 24, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "");

        AddSection(panel, "秦汉时期", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "曲裾深衣", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "曲裾是秦汉时期女性最具代表性的礼服形制，属于深衣体系。其核心特征为续衽钩边，衣襟经过缠绕后延伸至身后或侧面，层层绕身，形成流畅的斜向线条。通身紧窄，长可曳地，下摆呈喇叭状，行不露足。领口、袖口多饰有华丽滚边，常出现\"三重衣\"效果，即每层衣领外露，最多可达三层。曲裾承载着先秦以来的礼制文化，造型庄重典雅，是贵族女性参与礼仪活动的正式着装，体现了秦汉时期\"被体深邃\"的服饰理念。");
        AddParagraph(panel, "");

        AddSection(panel, "直裾深衣", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "直裾又称襜褕，衣襟垂直而下，不经缠绕，相较于曲裾更为简洁便利。东汉以后，随着内衣形制的完善，曲裾绕襟的遮蔽功能逐渐失去必要性，直裾开始普及并成为主流。直裾深衣保留了交领右衽的基本结构，衣身平直，线条简洁，日常穿着更为舒适自在，逐渐取代曲裾成为女性常服的主要款式。");
        AddParagraph(panel, "");

        AddSection(panel, "襦裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "上襦下裙是秦汉女性的日常装束。上襦极短，仅至腰间，交领右衽，袖口有宽窄两种样式；下裙长垂至地，多为四幅拼接，上窄下宽。腰间以丝带系扎，腰带长垂。襦裙形制朴素实用，是劳动女性与平民阶层的主要服饰，也为后世历代女服奠定了\"上衣下裳\"的基本范式。");
        AddParagraph(panel, "");

        AddSection(panel, "留仙裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "留仙裙是汉代宫廷盛行的经典裙装款式，相传因赵飞燕身着此裙起舞宛若仙子而得名。裙身多用素纱、轻罗等轻薄面料制成，裙幅打有细密顺褶，行走时裙摆随步摇曳如水波流转，轻盈灵动。留仙裙开创了古代褶裙的审美先河，是汉代女性追求飘逸柔美气质的服饰体现，也成为后世诸多裙装款式的灵感源头。");
        AddParagraph(panel, "");

        AddSection(panel, "袿衣", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "袿衣是秦汉贵族女性的高级礼服，承袭先秦深衣体系发展而来，因衣摆两侧裁出上宽下窄的刀圭状尖角、形如圭玉而得名。相较于普通深衣，袿衣衣身更为宽大，多织绣云纹、龙凤等祥瑞图案，边缘镶以厚重锦缘，工艺繁复华丽。它是后妃、命妇参与祭祀、朝会等重要礼仪场合的盛装，集中体现了秦汉服饰的等级礼制与织造工艺水准。");
        AddParagraph(panel, "");

        AddSection(panel, "魏晋南北朝", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "杂裾垂髾服", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "魏晋时期女性服饰在承袭汉制基础上趋向飘逸洒脱。杂裾垂髾服在传统深衣基础上发展而来，衣摆处缀有三角形装饰\"髾\"，下缀飘带\"裥\"，行走时如燕飞舞，有\"华带飞髾\"的灵动效果。这种服饰深受玄学风气影响，追求轻盈如仙的视觉感受，体现了魏晋士人飘逸洒脱的审美取向。");
        AddParagraph(panel, "");

        AddSection(panel, "间色裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "魏晋南北朝女性下裙流行条纹间色裙，以两种或多种颜色的布条相间缝制而成，色彩对比鲜明。上襦多为对襟款式，衣袖宽大，领口偏低，可露出颈部肌肤。整体造型宽松随性，\"褒衣博带\"成为时代风尚，反映了当时思想解放、个性张扬的社会氛围，同时也融入了北方少数民族服饰的元素。");
        AddParagraph(panel, "");

        AddSection(panel, "宽袖衫襦", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "女性日常多着宽袖衫襦，衫身宽博，广袖拂尘。梁简文帝诗中\"广袖拂红尘\"即是对此的生动描绘。衫为单层薄衣，交领或对襟，衣摆垂坠，两侧常有三角形饰片。搭配长裙与蔽膝，整体造型雍容飘逸，是贵族女性的典型装束，体现了魏晋时期对风度与气韵的极致追求。");
        AddParagraph(panel, "");

        AddSection(panel, "裲裆衫", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "裲裆衫最初源自北方少数民族戎装，魏晋时期传入中原并融入汉服体系，成为男女通用的流行服饰。其形制为前后两片织物，一片挡胸、一片挡背，肩部以皮革或织带相连，无袖无领，衣长及腰，类似后世坎肩。女性多将裲裆衫穿于衫襦之外，既可挡风御寒，又能丰富穿搭层次，兼具实用与装饰性，是当时胡汉服饰交融的典型代表。");
        AddParagraph(panel, "");

        AddSection(panel, "隋唐时期", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "齐胸襦裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "齐胸襦裙是唐代最具辨识度的女服形制。裙腰高束于胸部以上，上襦短小，对襟或交领，下裙宽大曳地。初唐多为交领，盛唐以后对襟款式盛行，领口渐低，可微露胸部。裙身常用六幅以上布料制成，裙摆宽大，行走时摇曳生姿。色彩明艳富丽，以红、绿、黄、紫等高饱和度色彩为主，著名的\"石榴裙\"即为此类。齐胸襦裙完美展现了大唐盛世的开放气度与雍容华贵，是盛唐气象在服饰上的集中体现。");
        AddParagraph(panel, "");

        AddSection(panel, "齐腰襦裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "齐腰襦裙裙腰系于正常腰线位置，是贯穿唐代始终的经典款式，上承汉魏，下启宋元。上着短襦或衫，下着长裙，腰系长带。相较于齐胸款式更为端庄内敛，初唐与晚唐更为流行。襦裙面料多样，有绫、罗、锦、纱等，常饰以联珠纹、宝相花、团花等图案，融合了中原传统与西域风格，体现了唐代兼容并蓄的文化特质。");
        AddParagraph(panel, "");

        AddSection(panel, "半臂", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "半臂又称半袖，是套在襦衫之外的短袖上衣，袖长及臂一半，故名。形制为对襟或交领，衣长及腰，领口宽大，穿着时衣襟敞开，不用纽扣。半臂源自隋代宫廷，初唐后在民间广泛流行，男女皆可穿着。它既可增加服饰层次感，又兼具一定保暖功能，是唐代女性日常穿搭中的重要单品，反映了唐代服饰实用与美观并重的特点。");
        AddParagraph(panel, "");

        AddSection(panel, "披帛", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "披帛是唐代女装的灵魂配饰，为长条状轻薄纱罗，搭于肩上，旋绕于手臂间。初唐披帛较厚较短，兼具装饰与御寒功能；盛唐以后逐渐变得细长轻薄，装饰性大于实用性。披帛材质多为素纱或印花纱，有的施以泥金绘画。行走时披帛随风飘动，增强了动态美感，使女性身姿更显婀娜飘逸，是唐代服饰浪漫气质的点睛之笔。");
        AddParagraph(panel, "");

        AddSection(panel, "圆领袍（女着男装）", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "唐代社会风气开放，女性身着男装成为时尚。圆领袍本为男子常服，盛唐时期贵族女性与宫女多有穿着。其形制为圆领、右衽、窄袖、腰间系带，搭配靴子与幞头，完整复刻男性装束。这种\"女扮男装\"的风尚从宫廷蔓延至民间，反映了唐代女性社会地位的提升与思想观念的解放，也体现了胡汉交融的文化特色。");
        AddParagraph(panel, "");

        AddSection(panel, "诃子裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "诃子裙是唐代极具特色的无肩带裙装，是盛唐开放风气下的代表性服饰。其形制为下裙直接束于胸部，无需搭配上襦，仅以胸带固定，领口平齐，可完整展现肩颈线条。裙身多选用厚重华丽的锦缎面料，饰以繁复纹样，常外搭轻薄大袖衫或披帛，雍容妩媚。诃子裙的流行反映了唐代女性对身体美学的大胆表达，是盛唐服饰文化自信的缩影。");
        AddParagraph(panel, "");

        AddSection(panel, "大袖纱罗衫", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "大袖纱罗衫是唐代女性的礼服外搭，多用于宫廷宴会、正式礼仪场合。衫身以轻薄透明的纱罗为料，对襟宽袖，袖型宽大舒展，衣长及膝或及地，边缘多饰以织金锦缘。穿着时多罩于齐胸襦裙之外，若隐若现间透出内层衣裙的色彩与纹样，飘逸华贵，仙气十足。这种搭配既符合礼制规范，又尽显唐代女性的柔美风姿，是贵族女性盛装的经典组合。");
        AddParagraph(panel, "");

        AddSection(panel, "回鹘装", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "回鹘装是唐代受西域回鹘部族服饰影响而流行的胡服款式，在盛唐至中晚唐的贵族女性中风靡一时。其形制为翻领、窄袖、长身及地，衣身偏紧身，腰间束带，领口与袖口多镶有宽阔锦缘，色彩以深红、暗紫等浓郁色调为主。身着回鹘装常搭配回鹘髻与金饰，英气华美，是唐代胡汉文化深度交融、社会风气包容开放的直观体现。");
        AddParagraph(panel, "");

        AddSection(panel, "宋代", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "褙子", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "褙子是宋代最具代表性的女性服饰，又称背子。形制为直领对襟，两腋开衩，衣长有过膝与及膝两种，袖型宽窄不一。衣襟、袖口和侧缝处常装饰印金缘饰，时称\"领抹\"。褙子穿着方式灵活，可敞开可系带，既可作为常服外穿，也可作为礼服内衬。其造型简约修长，线条流畅，不事张扬，完美契合宋代程朱理学影响下的素雅审美，体现了宋代女性含蓄内敛、温润端庄的气质。");
        AddParagraph(panel, "");

        AddSection(panel, "襦裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "宋代襦裙在唐代基础上趋于收敛简约。上襦多为交领或对襟，衣袖较唐代变窄；下裙腰身回落至正常位置，裙身修长。宋代女裙名目繁多，以\"石榴裙\"\"千褶裙\"\"百迭裙\"最为著名。百迭裙用料六幅至十二幅，周身密打细裥，如诗中所形容\"裙儿细褶如眉皱\"。配色清丽淡雅，多为浅青、淡粉、米白等柔和色调，反映了宋代崇尚质朴雅致的审美风尚。");
        AddParagraph(panel, "");

        AddSection(panel, "抹胸与背子组合", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "宋代女性内衣外穿形成独特搭配。抹胸为贴身内衣，上可覆胸，下可及腹，外罩褙子或背子，领口敞开处露出抹胸，形成层次丰富的视觉效果。这种搭配既符合礼制规范，又不失女性柔美，是宋代市井女性常见的日常装束，体现了宋代服饰在礼制约束下的生活智慧与审美表达。");
        AddParagraph(panel, "");

        AddSection(panel, "礼服大袖", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "礼服大袖是宋代贵族女性与命妇的正式礼服，因袖型宽大异常而得名，形制为直领对襟，衣身宽博，衣长及膝，袖宽可达二尺以上。大袖用料极为考究，多以织金锦、妆花缎等高级面料制成，衣身饰以龙凤、花卉等吉祥纹样，领口、袖口镶有精致缘边。它通常与长裙、霞帔搭配，用于婚嫁、祭祀等重大礼仪场合，是宋代女性服饰中规格较高的盛装形制。");
        AddParagraph(panel, "");

        AddSection(panel, "旋裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "旋裙是宋代市井女性日常穿着的实用裙装，因便于行走活动而得名。其形制多为单片围合式，裙身打有稀疏褶裥，长度及踝，腰间系带，穿脱便捷。相较于百迭裙的精致繁复，旋裙款式简约，面料多为普通棉布、素绢，配色朴素，是平民女性、劳动女性的日常首选。部分旋裙还会在两侧开衩，进一步提升行动便利性，体现了宋代服饰务实的一面。");
        AddParagraph(panel, "");

        AddSection(panel, "膝裤", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "膝裤是宋代女性常用的腿部服饰，属于裤装的变体，仅覆盖膝盖至脚踝的部位，形似后世长筒袜。膝裤多以锦缎或布帛制成，有夹里可保暖，表面常绣有花卉纹样，穿着时套于裤外、藏于裙内，既可御寒，又能起到装饰作用。它是宋代女性日常穿搭的重要补充，尤其盛行于秋冬时节，从宫廷到民间均有广泛穿着。");
        AddParagraph(panel, "");

        AddSection(panel, "明代", 20, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
        AddParagraph(panel, "");

        AddSection(panel, "袄裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "袄裙是明代女性最主要的常服形制，与前代襦裙最大区别在于上衣不束入裙内。\"袄\"为夹层或加棉的厚上衣，有交领、立领、方领等多种领型，袖型以琵琶袖最为典型，袖身宽大，袖口收紧。衣长过腰，两侧开衩。下配长裙，整体造型端庄稳重。明代袄裙用料考究，贵族多用妆花缎、织金绸，饰以云肩通袖纹样。立领款式在明中后期流行，既适应小冰河期的寒冷气候，又形成了独特的明代审美标识。");
        AddParagraph(panel, "");

        AddSection(panel, "马面裙", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "马面裙是明代女裙的代表形制，因前后有光面裙门（俗称\"马面\"）而得名。裙身共四扇裙门，两两重合，两侧打褶，中间裙门重合形成光面。裙门两侧的褶子大而疏，为活褶。裙腰以白色织物为之，寓意\"白头偕老\"，两侧系带固定。马面裙结构科学，行走便利，兼具美观与实用。裙身常饰有底襕与膝襕，纹样丰富，有花卉、鸟兽、人物等。明代马面裙形制奠定了后世数百年裙装的基础范式。");
        AddParagraph(panel, "");

        AddSection(panel, "比甲", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "比甲是明代流行的无袖长马甲，形制为对襟、无袖、两侧开衩，长至膝下或及地。它源自元代服饰，明代融入汉服体系后成为年轻女性的日常外搭，多罩于袄裙之外。比甲既可增加保暖性，又能丰富穿搭层次，士庶妻女与奴婢阶层普遍穿着。故宫藏有月白色暗花纱比甲等实物，印证了其在明代的流行程度。");
        AddParagraph(panel, "");

        AddSection(panel, "霞帔", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "霞帔是明代命妇礼服的重要组成部分，为两条狭长的织锦带，从肩部斜向垂至身前，末端缀有金玉坠子。霞帔纹样依品级有严格规定：一品二品用蹙金绣云霞翟纹，三品四品用云霞孔雀纹，五品用云霞鸳鸯纹等。霞帔与大衫搭配穿着，是身份与等级的标志，只有受朝廷诰封的命妇方可使用。它不仅是服饰装饰，更是女性社会地位的象征，承载着明代森严的等级礼制。");
        AddParagraph(panel, "");

        AddSection(panel, "大衫", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "大衫为明代皇后、命妇的礼服外袍，形制为直领对襟、大袖，衣长及地。皇后大衫为红色，织金龙凤纹；命妇大衫依品级用不同颜色与纹样。大衫内搭鞠衣、袄子等，下配长裙，前缀霞帔，头戴翟冠或凤冠，构成完整的命妇礼服体系。大衫霞帔组合是明代女性最高规格的礼服，用于重大礼仪场合，体现了明代冠服制度的完备与等级的森严。");
        AddParagraph(panel, "");

        AddSection(panel, "披风", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "披风是明代女性流行的外披服饰，形制为直领对襟、宽袖、长至膝下，两侧开衩，衣襟处用系带或纽扣固定，整体造型端庄舒展。披风面料随季节变化，夏用纱罗、冬用绸缎皮毛，素色与纹样皆有，既可作为日常常服外搭，也可作为礼仪场合的次礼服。它与比甲相比更为正式，袖型宽大，是明代中后期士庶女性皆喜爱的百搭外衫。");
        AddParagraph(panel, "");

        AddSection(panel, "水田衣", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "水田衣是明代兴起的特色服饰，又名\"百衲衣\"，以各色零碎织锦布料拼接缝制而成，因一块块面料交错形如水田而得名。其形制多为对襟长衣，袖型宽窄不一，拼接的布料色彩、纹样各异，错落有致，兼具别致的视觉效果与节俭的文化内涵。水田衣最初流行于民间，后传入宫廷，成为明代女性追求新奇审美、化零为整的服饰创意体现。");
        AddParagraph(panel, "");

        AddSection(panel, "主腰", 16, FontWeight.Bold, ThemeHelper.GetTextBrush());
        AddParagraph(panel, "主腰是明代女性的贴身内衣，功能类似后世束腰与抹胸的结合体。其形制为梯形或长方形织物，上可覆胸、下可及腰，两侧有系带，穿着时围于腰间系紧，既可束裹身形，又能起到保暖与支撑作用。主腰面料多为棉、绸，贵族女性所用常饰以刺绣、钉珠，外搭袄裙或比甲，是明代女性日常穿搭中不可或缺的内搭单品，体现了明代服饰体系的完整性。");
        AddParagraph(panel, "");
        border.Child = panel;
        return border;
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
        if (_maleTitleTextBlock != null)
            _maleTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        if (_maleSubtitleTextBlock != null)
            _maleSubtitleTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        if (_maleDescriptionTextBlock != null)
            _maleDescriptionTextBlock.Foreground = ThemeHelper.GetGrayBrush();

        if (_femaleContentBorder != null)
            _femaleContentBorder.Background = ThemeHelper.GetHanfuBackgroundBrush();

        if (_maleTitleTooltipBorder != null)
        {
            _maleTitleTooltipBorder.Background = ThemeHelper.IsDarkTheme()
                ? new SolidColorBrush(Color.FromRgb(30, 30, 30))
                : new SolidColorBrush(Color.FromRgb(230, 230, 230));
            _maleTitleTooltipBorder.BorderBrush = ThemeHelper.GetGrayBrush();
        }

        if (_maleTitleTooltipTextBlock != null)
        {
            _maleTitleTooltipTextBlock.Foreground = ThemeHelper.GetTextBrush();
        }

        if (_femaleGuideLinkTextBlock != null)
        {
            _femaleGuideLinkTextBlock.Foreground = GetAccentBrush();
        }

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