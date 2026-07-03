using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
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

    public HanfuPage() : this(null)
    {
    }

    public HanfuPage(PluginSettings? pluginSettings)
    {
        _pluginSettings = pluginSettings;
        InitializeComponent();
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

        var title = new TextBlock
        {
            Text = ":(",
            FontSize = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.White
        };
        panel.Children.Add(title);

        var subtitle = new TextBlock
        {
            Text = "欧呦！你似乎进入了一个尚未动工的页面",
            FontSize = 25,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.LightGray
        };
        panel.Children.Add(subtitle);

        var description = new TextBlock
        {
            Text = "由于开发者精力有限，这个页面可能需要较久的时间来实现，敬请期待。",
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 600
        };
        panel.Children.Add(description);

        scrollViewer.Content = panel;
        return scrollViewer;
    }

    private Control CreateFemaleContent()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(16)
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        AddSection(panel, "女款汉服", 24, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "");

        AddSection(panel, "秦汉时期", 20, FontWeight.Bold, Brushes.LightBlue);
        AddParagraph(panel, "");

        AddSection(panel, "曲裾深衣", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "曲裾是秦汉时期女性最具代表性的礼服形制，属于深衣体系。其核心特征为续衽钩边，衣襟经过缠绕后延伸至身后或侧面，层层绕身，形成流畅的斜向线条。通身紧窄，长可曳地，下摆呈喇叭状，行不露足。领口、袖口多饰有华丽滚边，常出现\"三重衣\"效果，即每层衣领外露，最多可达三层。曲裾承载着先秦以来的礼制文化，造型庄重典雅，是贵族女性参与礼仪活动的正式着装，体现了秦汉时期\"被体深邃\"的服饰理念。");
        AddParagraph(panel, "");

        AddSection(panel, "直裾深衣", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "直裾又称襜褕，衣襟垂直而下，不经缠绕，相较于曲裾更为简洁便利。东汉以后，随着内衣形制的完善，曲裾绕襟的遮蔽功能逐渐失去必要性，直裾开始普及并成为主流。直裾深衣保留了交领右衽的基本结构，衣身平直，线条简洁，日常穿着更为舒适自在，逐渐取代曲裾成为女性常服的主要款式。");
        AddParagraph(panel, "");

        AddSection(panel, "襦裙", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "上襦下裙是秦汉女性的日常装束。上襦极短，仅至腰间，交领右衽，袖口有宽窄两种样式；下裙长垂至地，多为四幅拼接，上窄下宽。腰间以丝带系扎，腰带长垂。襦裙形制朴素实用，是劳动女性与平民阶层的主要服饰，也为后世历代女服奠定了\"上衣下裳\"的基本范式。");
        AddParagraph(panel, "");

        AddSection(panel, "魏晋南北朝", 20, FontWeight.Bold, Brushes.LightBlue);
        AddParagraph(panel, "");

        AddSection(panel, "杂裾垂髾服", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "魏晋时期女性服饰在承袭汉制基础上趋向飘逸洒脱。杂裾垂髾服在传统深衣基础上发展而来，衣摆处缀有三角形装饰\"髾\"，下缀飘带\"裥\"，行走时如燕飞舞，有\"华带飞髾\"的灵动效果。这种服饰深受玄学风气影响，追求轻盈如仙的视觉感受，体现了魏晋士人飘逸洒脱的审美取向。");
        AddParagraph(panel, "");

        AddSection(panel, "间色裙", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "魏晋南北朝女性下裙流行条纹间色裙，以两种或多种颜色的布条相间缝制而成，色彩对比鲜明。上襦多为对襟款式，衣袖宽大，领口偏低，可露出颈部肌肤。整体造型宽松随性，\"褒衣博带\"成为时代风尚，反映了当时思想解放、个性张扬的社会氛围，同时也融入了北方少数民族服饰的元素。");
        AddParagraph(panel, "");

        AddSection(panel, "宽袖衫襦", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "女性日常多着宽袖衫襦，衫身宽博，广袖拂尘。梁简文帝诗中\"广袖拂红尘\"即是对此的生动描绘。衫为单层薄衣，交领或对襟，衣摆垂坠，两侧常有三角形饰片。搭配长裙与蔽膝，整体造型雍容飘逸，是贵族女性的典型装束，体现了魏晋时期对风度与气韵的极致追求。");
        AddParagraph(panel, "");

        AddSection(panel, "隋唐时期", 20, FontWeight.Bold, Brushes.LightBlue);
        AddParagraph(panel, "");

        AddSection(panel, "齐胸襦裙", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "齐胸襦裙是唐代最具辨识度的女服形制。裙腰高束于胸部以上，上襦短小，对襟或交领，下裙宽大曳地。初唐多为交领，盛唐以后对襟款式盛行，领口渐低，可微露胸部。裙身常用六幅以上布料制成，裙摆宽大，行走时摇曳生姿。色彩明艳富丽，以红、绿、黄、紫等高饱和度色彩为主，著名的\"石榴裙\"即为此类。齐胸襦裙完美展现了大唐盛世的开放气度与雍容华贵，是盛唐气象在服饰上的集中体现。");
        AddParagraph(panel, "");

        AddSection(panel, "齐腰襦裙", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "齐腰襦裙裙腰系于正常腰线位置，是贯穿唐代始终的经典款式，上承汉魏，下启宋元。上着短襦或衫，下着长裙，腰系长带。相较于齐胸款式更为端庄内敛，初唐与晚唐更为流行。襦裙面料多样，有绫、罗、锦、纱等，常饰以联珠纹、宝相花、团花等图案，融合了中原传统与西域风格，体现了唐代兼容并蓄的文化特质。");
        AddParagraph(panel, "");

        AddSection(panel, "半臂", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "半臂又称半袖，是套在襦衫之外的短袖上衣，袖长及臂一半，故名。形制为对襟或交领，衣长及腰，领口宽大，穿着时衣襟敞开，不用纽扣。半臂源自隋代宫廷，初唐后在民间广泛流行，男女皆可穿着。它既可增加服饰层次感，又兼具一定保暖功能，是唐代女性日常穿搭中的重要单品，反映了唐代服饰实用与美观并重的特点。");
        AddParagraph(panel, "");

        AddSection(panel, "披帛", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "披帛是唐代女装的灵魂配饰，为长条状轻薄纱罗，搭于肩上，旋绕于手臂间。初唐披帛较厚较短，兼具装饰与御寒功能；盛唐以后逐渐变得细长轻薄，装饰性大于实用性。披帛材质多为素纱或印花纱，有的施以泥金绘画。行走时披帛随风飘动，增强了动态美感，使女性身姿更显婀娜飘逸，是唐代服饰浪漫气质的点睛之笔。");
        AddParagraph(panel, "");

        AddSection(panel, "圆领袍（女着男装）", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "唐代社会风气开放，女性身着男装成为时尚。圆领袍本为男子常服，盛唐时期贵族女性与宫女多有穿着。其形制为圆领、右衽、窄袖、腰间系带，搭配靴子与幞头，完整复刻男性装束。这种\"女扮男装\"的风尚从宫廷蔓延至民间，反映了唐代女性社会地位的提升与思想观念的解放，也体现了胡汉交融的文化特色。");
        AddParagraph(panel, "");

        AddSection(panel, "宋代", 20, FontWeight.Bold, Brushes.LightBlue);
        AddParagraph(panel, "");

        AddSection(panel, "褙子", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "褙子是宋代最具代表性的女性服饰，又称背子。形制为直领对襟，两腋开衩，衣长有过膝与及膝两种，袖型宽窄不一。衣襟、袖口和侧缝处常装饰印金缘饰，时称\"领抹\"。褙子穿着方式灵活，可敞开可系带，既可作为常服外穿，也可作为礼服内衬。其造型简约修长，线条流畅，不事张扬，完美契合宋代程朱理学影响下的素雅审美，体现了宋代女性含蓄内敛、温润端庄的气质。");
        AddParagraph(panel, "");

        AddSection(panel, "襦裙", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "宋代襦裙在唐代基础上趋于收敛简约。上襦多为交领或对襟，衣袖较唐代变窄；下裙腰身回落至正常位置，裙身修长。宋代女裙名目繁多，以\"石榴裙\"\"千褶裙\"\"百迭裙\"最为著名。百迭裙用料六幅至十二幅，周身密打细裥，如诗中所形容\"裙儿细褶如眉皱\"。配色清丽淡雅，多为浅青、淡粉、米白等柔和色调，反映了宋代崇尚质朴雅致的审美风尚。");
        AddParagraph(panel, "");

        AddSection(panel, "抹胸与背子组合", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "宋代女性内衣外穿形成独特搭配。抹胸为贴身内衣，上可覆胸，下可及腹，外罩褙子或背子，领口敞开处露出抹胸，形成层次丰富的视觉效果。这种搭配既符合礼制规范，又不失女性柔美，是宋代市井女性常见的日常装束，体现了宋代服饰在礼制约束下的生活智慧与审美表达。");
        AddParagraph(panel, "");

        AddSection(panel, "明代", 20, FontWeight.Bold, Brushes.LightBlue);
        AddParagraph(panel, "");

        AddSection(panel, "袄裙", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "袄裙是明代女性最主要的常服形制，与前代襦裙最大区别在于上衣不束入裙内。\"袄\"为夹层或加棉的厚上衣，有交领、立领、方领等多种领型，袖型以琵琶袖最为典型，袖身宽大，袖口收紧。衣长过腰，两侧开衩。下配长裙，整体造型端庄稳重。明代袄裙用料考究，贵族多用妆花缎、织金绸，饰以云肩通袖纹样。立领款式在明中后期流行，既适应小冰河期的寒冷气候，又形成了独特的明代审美标识。");
        AddParagraph(panel, "");

        AddSection(panel, "马面裙", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "马面裙是明代女裙的代表形制，因前后有光面裙门（俗称\"马面\"）而得名。裙身共四扇裙门，两两重合，两侧打褶，中间裙门重合形成光面。裙门两侧的褶子大而疏，为活褶。裙腰以白色织物为之，寓意\"白头偕老\"，两侧系带固定。马面裙结构科学，行走便利，兼具美观与实用。裙身常饰有底襕与膝襕，纹样丰富，有花卉、鸟兽、人物等。明代马面裙形制奠定了后世数百年裙装的基础范式。");
        AddParagraph(panel, "");

        AddSection(panel, "比甲", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "比甲是明代流行的无袖长马甲，形制为对襟、无袖、两侧开衩，长至膝下或及地。它源自元代服饰，明代融入汉服体系后成为年轻女性的日常外搭，多罩于袄裙之外。比甲既可增加保暖性，又能丰富穿搭层次，士庶妻女与奴婢阶层普遍穿着。故宫藏有月白色暗花纱比甲等实物，印证了其在明代的流行程度。");
        AddParagraph(panel, "");

        AddSection(panel, "霞帔", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "霞帔是明代命妇礼服的重要组成部分，为两条狭长的织锦带，从肩部斜向垂至身前，末端缀有金玉坠子。霞帔纹样依品级有严格规定：一品二品用蹙金绣云霞翟纹，三品四品用云霞孔雀纹，五品用云霞鸳鸯纹等。霞帔与大衫搭配穿着，是身份与等级的标志，只有受朝廷诰封的命妇方可使用。它不仅是服饰装饰，更是女性社会地位的象征，承载着明代森严的等级礼制。");
        AddParagraph(panel, "");

        AddSection(panel, "大衫", 16, FontWeight.Bold, Brushes.White);
        AddParagraph(panel, "大衫为明代皇后、命妇的礼服外袍，形制为直领对襟、大袖，衣长及地。皇后大衫为红色，织金龙凤纹；命妇大衫依品级用不同颜色与纹样。大衫内搭鞠衣、袄子等，下配长裙，前缀霞帔，头戴翟冠或凤冠，构成完整的命妇礼服体系。大衫霞帔组合是明代女性最高规格的礼服，用于重大礼仪场合，体现了明代冠服制度的完备与等级的森严。");

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
        panel.Children.Add(textBlock);
    }

    private void AddParagraph(StackPanel panel, string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 14,
            Foreground = Brushes.LightGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        panel.Children.Add(textBlock);
    }
}