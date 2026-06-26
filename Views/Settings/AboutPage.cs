using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 关于页面
/// </summary>
[SettingsPageInfo("AdvancedTimeIsland", "AdvancedTimeIsland 设置")]
public class AboutPage : SettingsPageBase
{
    private static SolidColorBrush GetAccentBrush()
    {
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            return new SolidColorBrush(accentColor);
        }
        if (Application.Current?.TryFindResource("AccentColor", out var accentObj) == true && accentObj is Color accentColor2)
        {
            return new SolidColorBrush(accentColor2);
        }
        return new SolidColorBrush(Colors.DodgerBlue);
    }

    private static IBrush GetAccentTextBrush(SolidColorBrush accentBrush)
    {
        var color = accentBrush.Color;
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        return luminance > 0.6 ? Brushes.Black : Brushes.White;
    }

    private EasterEggDetector _easterEggDetector;
    private Border _iconBorder = null!;
    private TabControl? _tabControl;
    private bool _easterEggActive;
    private readonly LunarInstallerService? _lunarInstaller;
    private readonly PluginSettings? _pluginSettings;

    public AboutPage() : this(null, null)
    {
    }

    public AboutPage(LunarInstallerService? lunarInstaller = null) : this(lunarInstaller, null)
    {
    }

    public AboutPage(LunarInstallerService? lunarInstaller = null, PluginSettings? pluginSettings = null)
    {
        _lunarInstaller = lunarInstaller;
        _pluginSettings = pluginSettings;
        _easterEggActive = pluginSettings?.EnableEasterEgg ?? false;
        _easterEggDetector = new EasterEggDetector(11, 5);
        _easterEggDetector.OnActivated += OnEasterEggActivated;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        var iconBorder = CreateIconBorder();
        _iconBorder = iconBorder;
        var headerPanel = CreateHeaderPanel(iconBorder);

        mainPanel.Children.Add(headerPanel);

        _tabControl = CreateNavigationTabs();
        mainPanel.Children.Add(_tabControl);

        var scrollViewer = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        Content = scrollViewer;
    }

    /// <summary>
    /// 创建图标边框（带彩蛋检测）
    /// </summary>
    private Border CreateIconBorder()
    {
        var accentBrush = GetAccentBrush();
        var iconBorder = new Border
        {
            Width = 64,
            Height = 64,
            Background = accentBrush,
            CornerRadius = new CornerRadius(8),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0),
            Child = new TextBlock
            {
                Text = "[图标]",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = GetAccentTextBrush(accentBrush)
            }
        };

        // 添加点击事件用于彩蛋检测
        iconBorder.PointerPressed += OnIconClicked;

        return iconBorder;
    }

    /// <summary>
    /// 图标点击事件
    /// </summary>
    private void OnIconClicked(object? sender, PointerPressedEventArgs e)
    {
        _easterEggDetector.RecordClick();
    }

    /// <summary>
    /// 显示彩蛋触发提示窗口
    /// </summary>
    private async void ShowEasterEggDialog()
    {
        var dialog = new Window
        {
            Title = "提示",
            Width = 280,
            Height = 140,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "触发彩蛋成功~",
                        FontSize = 16,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new Button
                    {
                        Content = "确定",
                        Width = 80,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Background = GetAccentBrush(),
                        Foreground = GetAccentTextBrush(GetAccentBrush()),
                        CornerRadius = new CornerRadius(4)
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children[1] is Button okButton)
        {
            okButton.Click += (s, e) => dialog.Close();
        }

        await dialog.ShowDialog((Window)VisualRoot!);
    }

    /// <summary>
    /// 彩蛋激活处理
    /// </summary>
    private void OnEasterEggActivated(object? sender, EventArgs e)
    {
        if (_easterEggActive) return;
        _easterEggActive = true;
        if (_pluginSettings != null)
        {
            _pluginSettings.EnableEasterEgg = true;
        }

        UpdateTabOrder(true);
        ShowEasterEggDialog();
    }

    /// <summary>
    /// 女装开关状态变化处理
    /// </summary>
    private void OnEasterEggToggled(object? sender, bool isEnabled)
    {
        if (!isEnabled && _easterEggActive)
        {
            _easterEggActive = false;
            UpdateTabOrder(false);
        }
    }

    /// <summary>
    /// 更新标签顺序
    /// </summary>
    private void UpdateTabOrder(bool easterEggActive)
    {
        if (_tabControl == null) return;

        _tabControl.Items.Clear();

        if (easterEggActive)
        {
            _tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
            _tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
            _tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });
            var pluginSettingsTab = new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" };
            _tabControl.Items.Add(pluginSettingsTab);
            _tabControl.Items.Add(new TabItem { Header = "女装", Content = new EasterEggPage() });
        }
        else
        {
            _tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
            _tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
            _tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });
            _tabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
        }
    }

    /// <summary>
    /// 创建关于内容
    /// </summary>
    private Control CreateAboutContent()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 12
        };

        panel.Children.Add(new TextBlock
        {
            Text = "作者：inf2147483647",
            FontSize = 14,
            Foreground = Brushes.White
        });

        panel.Children.Add(new TextBlock
        {
            Text = "版本：1.0.0.7",
            FontSize = 14,
            Foreground = Brushes.White
        });

        panel.Children.Add(new TextBlock
        {
            Text = "介绍：一个好的插件，先从它的介绍开始",
            FontSize = 14,
            Foreground = Brushes.LightGray,
            TextWrapping = TextWrapping.Wrap
        });

        return panel;
    }

    /// <summary>
    /// 创建顶部通用区域
    /// </summary>
    private Panel CreateHeaderPanel(Border iconBorder)
    {
        var headerPanel = new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var infoPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4
        };

        var nameText = new TextBlock
        {
            Text = "AdvancedTimeIsland",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };

        var authorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var authorText = new TextBlock
        {
            Text = "inf2147483647",
            FontSize = 12,
            Foreground = Brushes.LightGray
        };

        var projectButton = new Button
        {
            Content = "项目主页",
            FontSize = 12,
            Padding = new Thickness(8, 2),
            Background = Brushes.Transparent,
            Foreground = GetAccentBrush(),
            BorderBrush = GetAccentBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        projectButton.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/inf2147483647/AdvancedTimeIsland",
                    UseShellExecute = true
                });
            }
            catch
            {
                // 忽略打开链接错误
            }
        };

        authorPanel.Children.Add(authorText);
        authorPanel.Children.Add(projectButton);

        infoPanel.Children.Add(nameText);
        infoPanel.Children.Add(authorPanel);

        DockPanel.SetDock(iconBorder, Dock.Left);
        headerPanel.Children.Add(iconBorder);
        headerPanel.Children.Add(infoPanel);

        return headerPanel;
    }

    /// <summary>
    /// 创建信息行
    /// </summary>
    private StackPanel CreateInfoRow(string label, string value)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        row.Children.Add(new TextBlock
        {
            Text = $"{label}：",
            FontSize = 13,
            Foreground = Brushes.White
        });

        row.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 13,
            Foreground = Brushes.LightGray
        });

        return row;
    }

    /// <summary>
    /// 创建标签导航栏
    /// </summary>
    private TabControl CreateNavigationTabs()
    {
        var tabControl = new TabControl
        {
            Background = Brushes.Transparent,
            Margin = new Thickness(0)
        };

        tabControl.SelectionChanged += (s, e) =>
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem && tabItem.Content == null)
            {
                LoadTabContent(tabItem);
            }
        };

        tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
        tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
        tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });
        tabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });

        if (_easterEggActive)
        {
            tabControl.Items.Add(new TabItem { Header = "女装", Content = new EasterEggPage() });
        }

        if (tabControl.Items.Count > 0 && tabControl.Items[0] is TabItem firstTab && firstTab.Content == null && firstTab.Tag != null)
        {
            LoadTabContent(firstTab);
        }

        return tabControl;
    }

    /// <summary>
    /// 加载标签页内容
    /// </summary>
    private void LoadTabContent(TabItem tabItem)
    {
        try
        {
            switch (tabItem.Tag?.ToString())
            {
                case "TimeConverter":
                    tabItem.Content = new TimeConverterPage(_pluginSettings);
                    break;
                case "PluginSettings":
                    var pluginSettings = new PluginSettingsPage(_pluginSettings, _lunarInstaller);
                    if (_easterEggActive)
                    {
                        pluginSettings.ShowEasterEggSetting();
                    }
                    pluginSettings.EasterEggToggled += OnEasterEggToggled;
                    tabItem.Content = pluginSettings;
                    break;
                case "Glossary":
                    tabItem.Content = new GlossaryPage();
                    break;
                default:
                    tabItem.Content = new TextBlock { Text = "内容加载中...", Foreground = Brushes.White };
                    break;
            }
        }
        catch (Exception ex)
        {
            tabItem.Content = new TextBlock
            {
                Text = $"加载失败: {ex.Message}",
                Foreground = Brushes.Red,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }
}
