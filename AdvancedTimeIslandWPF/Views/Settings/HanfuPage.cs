using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
// using Hanfu;
// using Hanfu.Womenswear;

using Markdig;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHanfu", "汉服百科", true, SettingsPageCategory.Debug)]
public class HanfuPage : HanfuPageTemplate
{
    private readonly PluginSettings? _pluginSettings;

    private Border? _femaleContentBorder;
    private TextBlock? _femaleGuideLinkTextBlock;

    public HanfuPage() : this(null)
    {
    }

    public HanfuPage(PluginSettings? pluginSettings) : base(true)
    {
        _pluginSettings = pluginSettings;
        InitializeComponent();
    }

    private TabControl? _tabStrip;
    private ContentControl? _contentControl;
    private FrameworkElement? _maleContent;
    private FrameworkElement? _femaleContent;

    protected override void BuildContent(StackPanel panel)
    {
        if (_pluginSettings == null || !_pluginSettings.EnableExperimentalFeatures)
        {
            var warningBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Message", "此页面为实验性功能，需要在插件设置中启用实验性功能才能查看完整内容。");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsClosable", false);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Margin", new Thickness(0, 0, 0, 8));
            panel.Children.Add(warningBar);
            return;
        }

        _xingZhiButtons = new List<Button>();
        _dynastyTitleTextBlocks = new List<TextBlock>();

        _maleContent = CreateMaleContent();
        _femaleContent = CreateFemaleContent();

        _tabStrip = new TabControl
        {
            Margin = new Thickness(0)
        };

        var maleTab = new TabItem
        {
            Content = "男装"
        };
        _tabStrip.Items.Add(maleTab);

        var femaleTab = new TabItem
        {
            Content = "女装"
        };
        _tabStrip.Items.Add(femaleTab);

        _tabStrip.SelectionChanged += OnTabSelectionChanged;

        _contentControl = new ContentControl
        {
            Content = _maleContent,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(_tabStrip, 0);
        rootGrid.Children.Add(_tabStrip);

        Grid.SetRow(_contentControl, 1);
        rootGrid.Children.Add(_contentControl);

        panel.Children.Add(rootGrid);
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tabStrip != null && _contentControl != null)
        {
            if (_tabStrip.SelectedIndex == 0)
            {
                _contentControl.Content = _maleContent;
            }
            else
            {
                _contentControl.Content = _femaleContent;
            }
        }
    }

    private Border? _maleContentBorder;
    private bool _isMaleTab;

    private FrameworkElement CreateMaleContent()
    {
        _isMaleTab = true;
        var border = new Border
        {
            Background = ThemeHelper.GetHanfuBackgroundBrush(),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0)
        };
        _maleContentBorder = border;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        AddDynastySection(panel, "明制汉服款式", new[]
        {
            "衫 袄 交领 明制", "衫 袄 圆领 明制", "贴里 明制", "裤 明制", "满褶裙 明制",
            "道袍 明制", "直裰 明制", "比甲 背心 明制", "披风 明制", "氅衣 明制",
            "褡护 明制", "野服 明制", "罩甲 明制", "直身 明制", "圆领袍 衫 明制",
            "深衣 大带 幅巾 明制", "道服 明制", "曳撒 明制", "襕衫 蓝袍 明制",
            "公服 梁冠 明制", "朝服 幞头 明制", "冕服 明制"
        });

        AddDynastySection(panel, "宋制汉服款式", new[]
        {
            "抱腹 宋制", "袄 衫 直领 宋制", "袄 衫 交领 宋制", "袄 衫 圆领 宋制",
            "长袄 衫 直领 宋制", "长袄 衫 交领 宋制", "长袄 衫 圆领 宋制",
            "裈 合裆裤 宋制", "袴 开裆裤 宋制", "百迭裙 宋制", "背心 宋制",
            "长背子 宋制", "氅衣 宋制", "鹤袖 貉袖 宋制", "圆领袍 䙆袍 宋制",
            "深衣 大带 幅巾 宋制", "公服 宋制", "衬袍 衬褙子 宋制",
            "圆领袍 襕袍衫 宋制", "襕衫 宋制", "道服 宋制", "祭服 宋制", "衮冕 宋制"
        });

        AddDynastySection(panel, "唐制汉服款式", new[]
        {
            "裈 袴 唐制", "汗衫 袄子 圆领 唐制", "汗衫 袄子 交领 唐制",
            "半臂 唐制", "长袖 唐制", "圆领袍衫 缺胯袍衫 唐制",
            "披袍 披衫 唐制", "浴袍 唐制", "襕袍 襕衫 唐制",
            "公服 唐制", "朝服 唐制", "祭服 唐制", "通天冠服 唐制", "衮冕 唐制"
        });

        AddDynastySection(panel, "晋制汉服款式", new[]
        {
            "两当 晋制", "木屐 木屧 晋制", "裈 袴 晋制", "褶 衫 晋制",
            "长褶 大褶 晋制", "裙褶 袴褶 晋制", "衫襦 直领 晋制",
            "衫襦 曲领 晋制", "衫襦 垂胡袖 晋制", "衫襦 窄袖 直袖 晋制",
            "衫襦 大袖 晋制", "交窬裙 无缘裙 晋制", "交窬裙 有缘裙 晋制",
            "半袖 晋制", "襦 晋制", "半袖裙襦 东汉式 晋制", "帔子 晋制",
            "半袖裙襦 蔽膝 晋制", "单衣 蔽膝 晋制"
        });

        AddDynastySection(panel, "汉制汉服款式", new[]
        {
            "长襦 曲裾式 汉制", "长襦 直裾式 汉制", "交窬裙 汉制",
            "单衣 汉制", "夹衣 汉制", "复衣 汉制"
        });

        AddDynastySection(panel, "先秦制汉服款式", new[]
        {
            "裈 袴 先秦制", "交窬裙 先秦制", "长襦 单衣 先秦制",
            "长襦 夹衣 先秦制", "长襦 复衣 先秦制"
        });

        border.Child = panel;
        return border;
    }

    private List<Button>? _xingZhiButtons;
    private readonly Dictionary<Button, bool> _xingZhiDevelopedStates = new Dictionary<Button, bool>();
    private List<TextBlock>? _dynastyTitleTextBlocks;

    private FrameworkElement CreateFemaleContent()
    {
        _isMaleTab = false;
        var border = new Border
        {
            Background = ThemeHelper.GetHanfuBackgroundBrush(),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0)
        };
        _femaleContentBorder = border;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        var guidePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
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
            TextDecorations = TextDecorations.Underline,
            Cursor = Cursors.Hand
        };
        _femaleGuideLinkTextBlock.MouseLeftButtonUp += (s, e) =>
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

        AddDynastySection(panel, "明制汉服款式", new[]
        {
            "主腰 明制", "裤 明制", "贴里 明制", "短衫 袄 直领 明制", "长衫 袄 直领 明制",
            "短衫 袄 交领 明制", "长衫 袄 交领 明制", "短衫 袄 圆领 明制", "长衫 袄 圆领 明制",
            "短衫 袄 竖领 明制", "长衫 袄 竖领 明制", "短衫 袄 方领 明制", "长衫 袄 方领 明制",
            "马面裙 侧褶 明制", "马面裙 百褶 明制", "满褶裙 明制", "鹤袖 貉袖 明制", "比甲 背心 明制",
            "披袄 明制", "氅衣 明制", "披风 明制", "云肩 明制", "长背子 明制", "圆领鞠衣 明制",
            "圆领袍 衫 明制", "大袖衫 霞帔 明制", "补子 补服 明制", "翟衣 明制", "翟冠 明制"
        });

        AddDynastySection(panel, "宋制汉服款式", new[]
        {
            "抹胸 裹肚 宋制", "黄昇抹胸 宋制", "裈 合裆裤 宋制", "袴 开裆裤 宋制", "裆 宋裤 宋制",
            "三襜 三穿 宋制", "交窬裙 宋制", "百迭裙 宋制", "百迭裙 拖后款 宋制", "百迭裙 仅围合 宋制",
            "三裥裙 宋制", "两片裙 宋制", "短袄 衫 交领 宋制", "短袄 衫 北宋式 宋制", "短袄 衫 南宋式 宋制",
            "长袄 衫 北宋式 宋制", "长袄 衫 南宋式 宋制", "背心 北宋式 宋制", "背心 南宋式 宋制",
            "鹤袖 貉袖 宋制", "氅衣 宋制", "圆领袍 宋制", "背子 褙子 宋制", "大袖衣 横帔 霞帔 宋制",
            "袆衣 宋制", "褕翟 宋制"
        });

        AddDynastySection(panel, "唐制汉服款式", new[]
        {
            "抹乳 陌腹 唐制", "袄 衫 交领 唐制", "袄 衫 直领 唐制", "袄 衫 圆领 唐制",
            "长袄 衫 交领 唐制", "长袄 衫 直领 唐制", "长袄 衫 圆领 唐制", "裈 袴 唐制",
            "交窬裙 唐制", "交窬裙 收省款 唐制", "交窬裙 襻带式 唐制", "交窬裙 裙衬裙 唐制",
            "交窬裙 笼裙 唐制", "交窬裙 短裙 腰裙 唐制", "三裥裙 多裥裙 唐制", "腰带 唐制",
            "背子 唐制", "披袄 披衫 唐制", "帔子 夹帔子 披帛 唐制", "羽袖 唐制", "大袖裙襦 唐制",
            "大袖连裳 唐制", "袆衣 唐制"
        });

        AddDynastySection(panel, "晋制汉服款式", new[]
        {
            "两当 晋制", "木屐 木屧 晋制", "裈 袴 晋制", "褶 衫 晋制", "长褶 大褶 晋制",
            "裙褶 袴褶 晋制", "衫襦 直领 晋制", "衫襦 曲领 晋制", "衫襦 垂胡袖 晋制",
            "衫襦 窄袖 直袖 晋制", "衫襦 大袖 晋制", "交窬裙 无缘裙 晋制", "交窬裙 有缘裙 晋制",
            "半袖 晋制", "襦 晋制", "半袖裙襦 东汉式 晋制", "帔子 晋制", "半袖裙襦 蔽膝 晋制",
            "单衣 蔽膝 晋制"
        });

        AddDynastySection(panel, "汉制汉服款式", new[]
        {
            "长襦 曲裾式 汉制", "长襦 直裾式 汉制", "交窬裙 汉制", "单衣 汉制", "夹衣 汉制", "复衣 汉制"
        });

        AddDynastySection(panel, "先秦制汉服款式", new[]
        {
            "裈 袴 先秦制", "交窬裙 先秦制", "长襦 单衣 先秦制", "长襦 夹衣 先秦制", "长襦 复衣 先秦制"
        });

        border.Child = panel;
        return border;
    }

    private void AddDynastySection(StackPanel panel, string title, string[] xingZhis)
    {
        var titleTextBlock = new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = ThemeHelper.GetLightBlueBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 8)
        };
        _dynastyTitleTextBlocks?.Add(titleTextBlock);
        panel.Children.Add(titleTextBlock);

        var buttonPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var xingZhi in xingZhis)
        {
            var button = CreateXingZhiButton(xingZhi);
            button.Margin = new Thickness(0, 0, 8, 8);
            _xingZhiButtons?.Add(button);
            buttonPanel.Children.Add(button);
        }

        panel.Children.Add(buttonPanel);
    }

    private Button CreateXingZhiButton(string text)
    {
        var button = new Button
        {
            Content = text,
            FontSize = 14,
            Padding = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        _xingZhiDevelopedStates[button] = IsDeveloped(text, _isMaleTab);
        UpdateXingZhiButtonStyle(button);

        button.Click += (s, e) =>
        {
            OnXingZhiButtonClick(text);
        };

        return button;
    }

    private void OnXingZhiButtonClick(string text)
    {
        var isMaleTab = _tabStrip?.SelectedIndex == 0;

        // 未开发条目（对应 Markdown 内容不足 32 字节）不响应点击
        if (!IsDeveloped(text, isMaleTab))
            return;

        var markdownName = GetMarkdownName(text, isMaleTab);
        if (markdownName == null)
            return;

        var uri = $"classisland://app/settings/{GetPageId(markdownName)}?ci_keepHistory=true";
        IAppHost.TryGetService<IUriNavigationService>()?
            .NavigateWrapped(new Uri(uri));
    }

    /// <summary>条目 → Markdown 内容文件名（不含扩展名）。文件名由条目拼音生成，空格转下划线。</summary>
    private static readonly Dictionary<string, string> _xingZhiMarkdownNames = new Dictionary<string, string>
    {
        { "袄 衫 交领 宋制", "ao_shan_jiaoling_songzhi" },
        { "袄 衫 交领 唐制", "ao_shan_jiaoling_tangzhi" },
        { "袄 衫 圆领 宋制", "ao_shan_yuanling_songzhi" },
        { "袄 衫 圆领 唐制", "ao_shan_yuanling_tangzhi" },
        { "袄 衫 直领 宋制", "ao_shan_zhiling_songzhi" },
        { "袄 衫 直领 唐制", "qixiong_top" },
        { "百迭裙 仅围合 宋制", "baidiequn_jinweihe_songzhi" },
        { "百迭裙 宋制", "baidiequn" },
        { "百迭裙 拖后款 宋制", "baidiequn_tuohoukuan_songzhi" },
        { "半臂 唐制", "banbi_tangzhi" },
        { "半袖 晋制", "banxiu_jinzhi" },
        { "半袖裙襦 蔽膝 晋制", "banxiuqunru_bixi_jinzhi" },
        { "半袖裙襦 东汉式 晋制", "banxiuqunru_donghanshi_jinzhi" },
        { "抱腹 宋制", "baofu_songzhi" },
        { "背心 北宋式 宋制", "beixin_beisongshi_songzhi" },
        { "背心 南宋式 宋制", "beixin_nansongshi_songzhi" },
        { "背心 宋制", "beixin_songzhi" },
        { "背子 褙子 宋制", "beizi" },
        { "背子 唐制", "beizi_tangzhi" },
        { "比甲 背心 明制", "bijia_beixin_mingstyle" },
        { "补子 补服 明制", "buzi_bufu_mingstyle" },
        { "氅衣 明制", "changyi_mingstyle" },
        { "氅衣 宋制", "changyi_songzhi" },
        { "朝服 幞头 明制", "chaofu_futou_mingstyle" },
        { "朝服 唐制", "chaofu_tangzhi" },
        { "衬袍 衬褙子 宋制", "chenpao_chenbeizi_songzhi" },
        { "褡护 明制", "dahu_mingstyle" },
        { "大袖连裳 唐制", "daxiulianshang_tangzhi" },
        { "大袖裙襦 唐制", "daxiuqunru_tangzhi" },
        { "大袖衫 霞帔 明制", "daxiushan_xiapei_mingstyle" },
        { "大袖衣 横帔 霞帔 宋制", "daxiuyi_hengpei_xiapei_songzhi" },
        { "单衣 蔽膝 晋制", "danyi_bixi_jinzhi" },
        { "单衣 汉制", "danyi_hanzhi" },
        { "裆 宋裤 宋制", "dang_songku_songzhi" },
        { "道服 明制", "daofu_mingstyle" },
        { "道服 宋制", "daofu_songzhi" },
        { "道袍 明制", "daopao_mingstyle" },
        { "翟冠 明制", "diguan_mingstyle" },
        { "翟衣 明制", "diyi_mingstyle" },
        { "短袄 衫 北宋式 宋制", "duanao_shan_beisongshi_songzhi" },
        { "短袄 衫 交领 宋制", "duanao_shan_jiaoling_songzhi" },
        { "短袄 衫 南宋式 宋制", "duanao_shan_nansongshi_songzhi" },
        { "短衫 袄 方领 明制", "duanshan_ao_fangling_mingstyle" },
        { "短衫 袄 交领 明制", "duanshan_ao_jiaoling" },
        { "短衫 袄 竖领 明制", "duanshan_ao_shuling" },
        { "短衫 袄 圆领 明制", "duanshan_ao_yuanling_mingstyle" },
        { "短衫 袄 直领 明制", "duanshan_ao_zhiling_mingstyle" },
        { "复衣 汉制", "fuyi_hanzhi" },
        { "公服 梁冠 明制", "gongfu_liangguan_mingstyle" },
        { "公服 宋制", "gongfu_songzhi" },
        { "公服 唐制", "gongfu_tangzhi" },
        { "衮冕 宋制", "gunmian_songzhi" },
        { "衮冕 唐制", "gunmian_tangzhi" },
        { "汗衫 袄子 交领 唐制", "hanshan_aozi_jiaoling_tangzhi" },
        { "汗衫 袄子 圆领 唐制", "hanshan_aozi_yuanling_tangzhi" },
        { "鹤袖 貉袖 明制", "hexiu_moxiu_mingstyle" },
        { "鹤袖 貉袖 宋制", "hexiu_moxiu_songzhi" },
        { "黄昇抹胸 宋制", "huangshengmoxiong_songzhi" },
        { "袆衣 宋制", "huiyi_songzhi" },
        { "袆衣 唐制", "huiyi_tangzhi" },
        { "祭服 宋制", "jifu_songzhi" },
        { "祭服 唐制", "jifu_tangzhi" },
        { "夹衣 汉制", "jiayi_hanzhi" },
        { "交窬裙 短裙 腰裙 唐制", "jiaoyuqun_duanqun_yaoqun_tangzhi" },
        { "交窬裙 汉制", "jiaoyuqun_hanzhi" },
        { "交窬裙 笼裙 唐制", "jiaoyuqun_longqun_tangzhi" },
        { "交窬裙 襻带式 唐制", "jiaoyuqun_pandaishi_tangzhi" },
        { "交窬裙 裙衬裙 唐制", "jiaoyuqun_qunchenqun_tangzhi" },
        { "交窬裙 收省款 唐制", "jiaoyuqun_shoushengkuan_tangzhi" },
        { "交窬裙 宋制", "jiaoyuqun_songzhi" },
        { "交窬裙 唐制", "qixiong_jiaoyuqun" },
        { "交窬裙 无缘裙 晋制", "jiaoyuqun_wuyuanqun_jinzhi" },
        { "交窬裙 先秦制", "jiaoyuqun_xianqinzhi" },
        { "交窬裙 有缘裙 晋制", "jiaoyuqun_youyuanqun_jinzhi" },
        { "袴 开裆裤 宋制", "ku_kaidangku_songzhi" },
        { "裤 明制", "ku_mingstyle" },
        { "裈 合裆裤 宋制", "kun_hedangku_songzhi" },
        { "裈 袴 晋制", "kun_ku_jinzhi" },
        { "裈 袴 唐制", "kun_ku_tangzhi" },
        { "裈 袴 先秦制", "kun_ku_xianqinzhi" },
        { "襕袍 襕衫 唐制", "lanpao_lanshan_tangzhi" },
        { "襕衫 蓝袍 明制", "lanshan_lanpao_mingstyle" },
        { "襕衫 宋制", "lanshan_songzhi" },
        { "两当 晋制", "liangdang_jinzhi" },
        { "两片裙 宋制", "liangpianqun_songzhi" },
        { "马面裙 百褶 明制", "mamianqun_baizhe" },
        { "马面裙 侧褶 明制", "mamianqun_cezhe" },
        { "满褶裙 明制", "manzhequn" },
        { "冕服 明制", "mianfu_mingstyle" },
        { "抹乳 陌腹 唐制", "moru_mofu_tangzhi" },
        { "抹胸 裹肚 宋制", "songmo" },
        { "木屐 木屧 晋制", "muji_muxie_jinzhi" },
        { "帔子 夹帔子 披帛 唐制", "peizi_jiapeizi_pibo_tangzhi" },
        { "帔子 晋制", "peizi_jinzhi" },
        { "披袄 明制", "pi_ao_mingstyle" },
        { "披袄 披衫 唐制", "piao_pishan_tangzhi" },
        { "披风 明制", "pifeng_mingstyle" },
        { "披袍 披衫 唐制", "pipao_pishan_tangzhi" },
        { "裙褶 袴褶 晋制", "qunxi_kuxi_jinzhi" },
        { "襦 晋制", "ru_jinzhi" },
        { "三襜 三穿 宋制", "sanchan_sanchuan_songzhi" },
        { "三裥裙 多裥裙 唐制", "sanjianqun_duojianqun_tangzhi" },
        { "三裥裙 宋制", "sanjianqun_songzhi" },
        { "衫 袄 交领 明制", "shan_ao_jiaoling_mingstyle" },
        { "衫 袄 圆领 明制", "shan_ao_yuanling_mingstyle" },
        { "衫襦 垂胡袖 晋制", "shanru_chuihuxiu_jinzhi" },
        { "衫襦 大袖 晋制", "shanru_daxiu_jinzhi" },
        { "衫襦 曲领 晋制", "shanru_quling_jinzhi" },
        { "衫襦 窄袖 直袖 晋制", "shanru_zhaixiu_zhixiu_jinzhi" },
        { "衫襦 直领 晋制", "shanru_zhiling_jinzhi" },
        { "深衣 大带 幅巾 明制", "shenyi_dadai_fujin_mingstyle" },
        { "深衣 大带 幅巾 宋制", "shenyi_dadai_fujin_songzhi" },
        { "贴里 明制", "tieli_mingstyle" },
        { "通天冠服 唐制", "tongtianguanfu_tangzhi" },
        { "腰带 唐制", "yaodai_tangzhi" },
        { "野服 明制", "yefu_mingstyle" },
        { "曳撒 明制", "yisa_mingstyle" },
        { "褕翟 宋制", "yudi_songzhi" },
        { "羽袖 唐制", "yuxiu_tangzhi" },
        { "浴袍 唐制", "yupao_tangzhi" },
        { "圆领鞠衣 明制", "yuanlingjuyi_mingstyle" },
        { "圆领袍 襕袍衫 宋制", "yuanlingpao_lanpaoshan_songzhi" },
        { "圆领袍 衫 明制", "yuanlingpao_shan_mingstyle" },
        { "圆领袍 宋制", "yuanlingpao_songzhi" },
        { "圆领袍 䙆袍 宋制", "yuanlingpao_kuipao_songzhi" },
        { "圆领袍衫 缺胯袍衫 唐制", "yuanlingpaoshan_quekuapaoshan_tangzhi" },
        { "云肩 明制", "yunjian_mingstyle" },
        { "长袄 衫 北宋式 宋制", "changao_shan_beisongshi_songzhi" },
        { "长袄 衫 交领 宋制", "changao_shan_jiaoling_songzhi" },
        { "长袄 衫 交领 唐制", "changao_shan_jiaoling_tangzhi" },
        { "长袄 衫 南宋式 宋制", "changao_shan_nansongshi_songzhi" },
        { "长袄 衫 圆领 宋制", "changao_shan_yuanling_songzhi" },
        { "长袄 衫 圆领 唐制", "changao_shan_yuanling_tangzhi" },
        { "长袄 衫 直领 宋制", "changao_shan_zhiling_songzhi" },
        { "长袄 衫 直领 唐制", "changao_shan_zhiling_tangzhi" },
        { "长背子 明制", "changbeizi_mingstyle" },
        { "长背子 宋制", "changbeizi_songzhi" },
        { "长襦 单衣 先秦制", "changru_danyi_xianqinzhi" },
        { "长襦 复衣 先秦制", "changru_fuyi_xianqinzhi" },
        { "长襦 夹衣 先秦制", "changru_jiayi_xianqinzhi" },
        { "长襦 曲裾式 汉制", "changru_qujushi_hanzhi" },
        { "长襦 直裾式 汉制", "changru_zhijushi_hanzhi" },
        { "长衫 袄 方领 明制", "changshan_ao_fangling_mingstyle" },
        { "长衫 袄 交领 明制", "changshan_ao_jiaoling" },
        { "长衫 袄 竖领 明制", "changshan_ao_shuling" },
        { "长衫 袄 圆领 明制", "changshan_ao_yuanling_mingstyle" },
        { "长衫 袄 直领 明制", "changshan_ao_zhiling_mingstyle" },
        { "长袖 唐制", "changxiu_tangzhi" },
        { "长褶 大褶 晋制", "changxi_daxi_jinzhi" },
        { "罩甲 明制", "zhaojia_mingstyle" },
        { "褶 衫 晋制", "xi_shan_jinzhi" },
        { "直裰 明制", "zhiduo_mingstyle" },
        { "直身 明制", "zhishen_mingstyle" },
        { "主腰 明制", "zhuyao_mingstyle" },
    };

    /// <summary>男女同款条目（唐制及以后同时出现在男装、女装列表）：女款用基础名，男款加 _male 后缀。</summary>
    private static readonly HashSet<string> _unisexXingZhi = new HashSet<string>
    {
        "百迭裙 宋制",
        "比甲 背心 明制",
        "氅衣 明制",
        "氅衣 宋制",
        "鹤袖 貉袖 宋制",
        "袴 开裆裤 宋制",
        "裤 明制",
        "裈 合裆裤 宋制",
        "裈 袴 唐制",
        "满褶裙 明制",
        "披风 明制",
        "贴里 明制",
        "圆领袍 衫 明制",
    };

    /// <summary>历史遗留页面：文件名与页面 ID 不一致的既有页面。</summary>
    private static readonly Dictionary<string, string> _legacyPageIds = new Dictionary<string, string>
    {
        { "baidiequn", "AdvancedTimeIslandBaiDieQun" },
        { "baidiequn_male", "AdvancedTimeIslandBaiDieQunMale" },
        { "beizi", "AdvancedTimeIslandBeiZi" },
        { "changshan_ao_jiaoling", "AdvancedTimeIslandChangShanAoJiaoLing" },
        { "changshan_ao_shuling", "AdvancedTimeIslandChangShanAoShuLing" },
        { "duanshan_ao_jiaoling", "AdvancedTimeIslandDuanShanAoJiaoLing" },
        { "duanshan_ao_shuling", "AdvancedTimeIslandDuanShanAoShuLing" },
        { "mamianqun_baizhe", "AdvancedTimeIslandMamianQunBaiZhe" },
        { "mamianqun_cezhe", "AdvancedTimeIslandMamianQunCeZhe" },
        { "manzhequn_male", "AdvancedTimeIslandManZheQunMale" },
        { "qixiong_jiaoyuqun", "AdvancedTimeIslandQiXiongJiaoYuQun" },
        { "qixiong_top", "AdvancedTimeIslandQiXiongTop" },
        { "songmo", "AdvancedTimeIslandSongMo" },
        { "tieli_mingstyle", "AdvancedTimeIslandTieliMingStyle" },
        { "tieli_mingstyle_male", "AdvancedTimeIslandTieliMingStyleMale" },
        { "zhuyao_mingstyle", "AdvancedTimeIslandZhuYaoMingStyle" },
    };

    /// <summary>Markdown 内容达到该字节数视为已开发。</summary>
    private const long DevelopedMarkdownBytes = 32;

    private static string? GetMarkdownName(string text, bool isMaleTab)
    {
        if (!_xingZhiMarkdownNames.TryGetValue(text, out var baseName))
            return null;
        return _unisexXingZhi.Contains(text) && isMaleTab ? baseName + "_male" : baseName;
    }

    private static string GetPageId(string markdownName)
    {
        if (_legacyPageIds.TryGetValue(markdownName, out var legacyId))
            return legacyId;

        var builder = new System.Text.StringBuilder("AdvancedTimeIsland");
        foreach (var part in markdownName.Split('_', StringSplitOptions.RemoveEmptyEntries))
        {
            builder.Append(char.ToUpperInvariant(part[0]));
            builder.Append(part, 1, part.Length - 1);
        }
        return builder.ToString();
    }

    private static bool IsDeveloped(string text, bool isMaleTab)
    {
        var markdownName = GetMarkdownName(text, isMaleTab);
        if (markdownName == null)
            return false;
        return HanfuPageTemplate.GetMarkdownFileSize(markdownName + ".md") >= DevelopedMarkdownBytes;
    }
    private void UpdateXingZhiButtonStyle(Button button)
    {
        var isDark = ThemeHelper.IsDarkTheme();
        button.Background = isDark
            ? new SolidColorBrush(ThemeHelper.ParseColor("#37373D"))
            : new SolidColorBrush(ThemeHelper.ParseColor("#E8E8E8"));
        var isDeveloped = _xingZhiDevelopedStates.TryGetValue(button, out var dev) && dev;
        button.Foreground = isDeveloped
            ? GetAccentBrush()
            : ThemeHelper.GetTextBrush();
        button.BorderBrush = isDark
            ? new SolidColorBrush(ThemeHelper.ParseColor("#444444"))
            : new SolidColorBrush(ThemeHelper.ParseColor("#CCCCCC"));
        button.BorderThickness = new Thickness(1);
    }

    protected override void UpdateThemeColors()
    {
        base.UpdateThemeColors();

        if (_femaleContentBorder != null)
            _femaleContentBorder.Background = ThemeHelper.GetHanfuBackgroundBrush();

        if (_maleContentBorder != null)
            _maleContentBorder.Background = ThemeHelper.GetHanfuBackgroundBrush();

        if (_femaleGuideLinkTextBlock != null)
        {
            _femaleGuideLinkTextBlock.Foreground = GetAccentBrush();
        }

        if (_xingZhiButtons != null)
        {
            foreach (var button in _xingZhiButtons)
            {
                UpdateXingZhiButtonStyle(button);
            }
        }

        if (_dynastyTitleTextBlocks != null)
        {
            foreach (var tb in _dynastyTitleTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetLightBlueBrush();
            }
        }
    }
}
