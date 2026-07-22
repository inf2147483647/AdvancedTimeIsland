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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

using Markdig;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHanfu", "汉服", SettingsPageCategory.Debug)]
public class HanfuPage : SettingsPageBase
{
    private readonly PluginSettings? _pluginSettings;

    private TextBlock? _maleTitleTextBlock;
    private TextBlock? _maleSubtitleTextBlock;
    private TextBlock? _maleDescriptionTextBlock;
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

    private TabStrip? _tabStrip;
    private ScrollViewer? _contentScrollViewer;
    private Control? _maleContent;
    private Control? _femaleContent;

    private void InitializeComponent()
    {
        _contentScrollViewer = new ScrollViewer
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
            var warningBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Message", "此页面为实验性功能，需要在插件设置中启用实验性功能才能查看完整内容。");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsClosable", false);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Margin", new Thickness(16, 16, 16, 0));
            mainPanel.Children.Add(warningBar);
            _contentScrollViewer.Content = mainPanel;
            Content = _contentScrollViewer;
            return;
        }

        _maleContent = CreateMaleContent();
        _femaleContent = CreateFemaleContent();

        _tabStrip = new TabStrip
        {
            Margin = new Thickness(0)
        };

        var maleTab = new TabStripItem
        {
            Content = "男装"
        };
        _tabStrip.Items.Add(maleTab);

        var femaleTab = new TabStripItem
        {
            Content = "女装"
        };
        _tabStrip.Items.Add(femaleTab);

        _tabStrip.SelectionChanged += OnTabSelectionChanged;
        _contentScrollViewer.Content = _maleContent;

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(_tabStrip, 0);
        rootGrid.Children.Add(_tabStrip);

        Grid.SetRow(_contentScrollViewer, 1);
        rootGrid.Children.Add(_contentScrollViewer);

        Content = rootGrid;
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tabStrip != null && _contentScrollViewer != null)
        {
            if (_tabStrip.SelectedIndex == 0)
            {
                _contentScrollViewer.Content = _maleContent;
            }
            else
            {
                _contentScrollViewer.Content = _femaleContent;
            }
        }
    }

    private Control CreateMaleContent()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _maleTitleTextBlock = new TextBlock
        {
            Text = ":(",
            FontSize = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = ThemeHelper.GetTextBrush()
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

        return panel;
    }

    private List<Button>? _xingZhiButtons;
    private List<TextBlock>? _dynastyTitleTextBlocks;

    private Control CreateFemaleContent()
    {
        _xingZhiButtons = new List<Button>();
        _dynastyTitleTextBlocks = new List<TextBlock>();

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
            Spacing = 16
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
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand)
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
            FontWeight = FontWeight.Bold,
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
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        UpdateXingZhiButtonStyle(button);

        button.Click += (s, e) =>
        {
            OnXingZhiButtonClick(text);
        };

        return button;
    }

    private void OnXingZhiButtonClick(string text)
    {
        if (text == "马面裙 侧褶 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandMamianQunCeZhe?ci_keepHistory=true"));
        }
        else if (text == "背子 褙子 宋制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandBeiZi?ci_keepHistory=true"));
        }
        else if (text == "交窬裙 唐制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandQiXiongJiaoYuQun?ci_keepHistory=true"));
        }
        else if (text == "袄 衫 直领 唐制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandQiXiongTop?ci_keepHistory=true"));
        }
        else if (text == "主腰 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandZhuYaoMingStyle?ci_keepHistory=true"));
        }
        else if (text == "贴里 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandTieliMingStyle?ci_keepHistory=true"));
        }
        else if (text == "百迭裙 宋制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandBaiDieQun?ci_keepHistory=true"));
        }
        else if (text == "短衫 袄 交领 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandDuanShanAoJiaoLing?ci_keepHistory=true"));
        }
        else if (text == "短衫 袄 竖领 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandDuanShanAoShuLing?ci_keepHistory=true"));
        }
        else if (text == "长衫 袄 竖领 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandChangShanAoShuLing?ci_keepHistory=true"));
        }
    }

    private readonly HashSet<string> _developedFeatures = new HashSet<string>
    {
        "马面裙 侧褶 明制",
        "背子 褙子 宋制",
        "交窬裙 唐制",
        "袄 衫 直领 唐制",
        "主腰 明制",
        "贴里 明制",
        "百迭裙 宋制",
        "短衫 袄 交领 明制",
        "短衫 袄 竖领 明制",
        "长衫 袄 竖领 明制"
    };

    private void UpdateXingZhiButtonStyle(Button button)
    {
        var isDark = ThemeHelper.IsDarkTheme();
        button.Background = isDark
            ? new SolidColorBrush(Color.Parse("#37373D"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
        var buttonText = button.Content as string;
        button.Foreground = _developedFeatures.Contains(buttonText)
            ? GetAccentBrush()
            : ThemeHelper.GetTextBrush();
        button.BorderBrush = isDark
            ? new SolidColorBrush(Color.Parse("#444444"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));
        button.BorderThickness = new Thickness(1);
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