using System;
using System.Collections.Generic;
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

    private TabStrip? _tabStrip;
    private ContentControl? _contentControl;
    private Control? _maleContent;
    private Control? _femaleContent;

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

    private Control CreateMaleContent()
    {
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
            Orientation = Orientation.Vertical,
            Spacing = 16
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
    private List<TextBlock>? _dynastyTitleTextBlocks;

    private Control CreateFemaleContent()
    {
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
        var isMaleTab = _tabStrip?.SelectedIndex == 0;

        if (text == "贴里 明制")
        {
            var uri = isMaleTab
                ? "classisland://app/settings/AdvancedTimeIslandTieliMingStyleMale?ci_keepHistory=true"
                : "classisland://app/settings/AdvancedTimeIslandTieliMingStyle?ci_keepHistory=true";
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri(uri));
        }
        else if (text == "百迭裙 宋制")
        {
            var uri = isMaleTab
                ? "classisland://app/settings/AdvancedTimeIslandBaiDieQunMale?ci_keepHistory=true"
                : "classisland://app/settings/AdvancedTimeIslandBaiDieQun?ci_keepHistory=true";
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri(uri));
        }
        else if (text == "满褶裙 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandManZheQunMale?ci_keepHistory=true"));
        }
        else if (text == "马面裙 侧褶 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandMamianQunCeZhe?ci_keepHistory=true"));
        }
        else if (text == "马面裙 百褶 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandMamianQunBaiZhe?ci_keepHistory=true"));
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
        else if (text == "长衫 袄 交领 明制")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandChangShanAoJiaoLing?ci_keepHistory=true"));
        }
    }

    private readonly HashSet<string> _developedFeatures = new HashSet<string>
    {
        "马面裙 侧褶 明制",
        "马面裙 百褶 明制",
        "背子 褙子 宋制",
        "交窬裙 唐制",
        "袄 衫 直领 唐制",
        "主腰 明制",
        "贴里 明制",
        "百迭裙 宋制",
        "短衫 袄 交领 明制",
        "短衫 袄 竖领 明制",
        "长衫 袄 竖领 明制",
        "长衫 袄 交领 明制",
        "满褶裙 明制"
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
