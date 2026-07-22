using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
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

    private TextBlock? _nameTextBlock;
    private TextBlock? _authorTextBlock;
    private List<TextBlock>? _aboutContentTextBlocks;
    private List<TextBlock>? _infoRowLabelTextBlocks;
    private List<TextBlock>? _infoRowValueTextBlocks;

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

        if (_pluginSettings?.DisclaimerAccepted != true)
        {
            var disclaimerBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Title", "免责声明");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Message", "插件内包含大量文本输入框，插件作者不对使用者在其中输入的内容做任何担保，如果使用者因输入不当内容导致造成不良影响，使用者需自行承担相关责任，插件作者概不负责。");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsClosable", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Margin", new Thickness(0, 0, 0, 8));
            FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(disclaimerBar, (s, e) =>
            {
                _pluginSettings!.DisclaimerAccepted = true;
            });
            mainPanel.Children.Add(disclaimerBar);
        }

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
        var iconBorder = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(8),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var image = new Avalonia.Controls.Image
        {
            Stretch = Stretch.UniformToFill,
            Width = 64,
            Height = 64
        };

        LoadIconImage(image);
        iconBorder.Child = image;

        iconBorder.PointerPressed += OnIconClicked;

        return iconBorder;
    }

    private void LoadIconImage(Avalonia.Controls.Image imageControl)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            // 直接复用 manifest 引用的根目录 icon.png，避免在包内保留重复副本以减小安装包体积
            var fullPath = System.IO.Path.Combine(baseDir, "icon.png");

            if (System.IO.File.Exists(fullPath))
            {
                var bitmap = new Avalonia.Media.Imaging.Bitmap(fullPath);
                imageControl.Source = bitmap;
            }
            else
            {
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var pluginDir = System.IO.Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    fullPath = System.IO.Path.Combine(pluginDir, "icon.png");
                    if (System.IO.File.Exists(fullPath))
                    {
                        var bitmap = new Avalonia.Media.Imaging.Bitmap(fullPath);
                        imageControl.Source = bitmap;
                    }
                }
            }
        }
        catch
        {
            imageControl.Source = null;
        }
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
    private void ShowEasterEggDialog()
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "提示");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", new TextBlock
        {
            Text = "触发彩蛋成功~",
            FontSize = 16,
            Foreground = ThemeHelper.GetTextBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());
        FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, TopLevel.GetTopLevel(this));
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
    /// 只显示右上角"需要重启"按钮，不弹出原生对话框
    /// </summary>
    private void ShowRestartButton()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var viewModel = topLevel.GetType().GetProperty("ViewModel", BindingFlags.Public | BindingFlags.Instance);
            if (viewModel == null) return;

            var vm = viewModel.GetValue(topLevel);
            if (vm == null) return;

            var isRequestedRestartProp = vm.GetType().GetProperty("IsRequestedRestart", BindingFlags.Public | BindingFlags.Instance);
            if (isRequestedRestartProp == null) return;

            isRequestedRestartProp.SetValue(vm, true);
        }
        catch
        {
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
            _tabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
            _tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
            _tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
            _tabControl.Items.Add(new TabItem { Header = "时间计算器", Content = null, Tag = "TimeCalculator" });
            _tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });
            _tabControl.Items.Add(new TabItem { Header = "女装", Content = new EasterEggPage(_pluginSettings) });
        }
        else
        {
            _tabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
            _tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
            _tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
            _tabControl.Items.Add(new TabItem { Header = "时间计算器", Content = null, Tag = "TimeCalculator" });
            _tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });
        }
    }

    /// <summary>
    /// 创建关于内容
    /// </summary>
    private Control CreateAboutContent()
    {
        _aboutContentTextBlocks = new List<TextBlock>();

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 12
        };

        var authorText = new TextBlock
        {
            Text = "作者：inf2147483647",
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _aboutContentTextBlocks.Add(authorText);
        panel.Children.Add(authorText);

        var versionText = new TextBlock
        {
            Text = "版本：1.0.2.2",
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _aboutContentTextBlocks.Add(versionText);
        panel.Children.Add(versionText);

        var descText = new TextBlock
        {
            Text = "AdvancedTimeIsland是一款为ClassIsland打造的高级时间管理插件，旨在弥补原生功能的不足（比如判断是否在某段时间范围内）。它提供丰富的时间相关组件，包括高级日期显示、多例倒计时、正计时、周期性倒计时等核功能。同时支持农历日历、节气、生肖、星座、节日等展示。该插件具备强大的时间自动化能力，支持精确时间、周期性时间（年/月/周/日/时/分）、地方时、区时、农历时间等多种触发条件与规则，满足复杂的时间调度需求。此外还提供时间格式转换工具，支持北京时间、Unix时间戳、农历、区时、地方时之间的相互转换，并附带专业名词解释帮助用户理解时间相关概念。插件界面支持主题自适应，为用户提供更完善的时间管理体验。更多功能开发中，敬请期待。",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        };
        _aboutContentTextBlocks.Add(descText);
        panel.Children.Add(descText);

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

        _nameTextBlock = new TextBlock
        {
            Text = "AdvancedTimeIsland",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };

        var authorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        _authorTextBlock = new TextBlock
        {
            Text = "inf2147483647",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush()
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

        authorPanel.Children.Add(_authorTextBlock);
        authorPanel.Children.Add(projectButton);

        // 反馈问题按钮
        var feedbackButton = new Button
        {
            Content = "反馈问题",
            FontSize = 12,
            Padding = new Thickness(8, 2),
            Background = Brushes.Transparent,
            Foreground = GetAccentBrush(),
            BorderBrush = GetAccentBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(8, 0, 0, 0)
        };

        feedbackButton.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/inf2147483647/AdvancedTimeIsland/issues/new",
                    UseShellExecute = true
                });
            }
            catch
            {
                // 忽略打开链接错误
            }
        };

        authorPanel.Children.Add(feedbackButton);

        infoPanel.Children.Add(_nameTextBlock);
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

        var labelText = new TextBlock
        {
            Text = $"{label}：",
            FontSize = 13,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _infoRowLabelTextBlocks?.Add(labelText);
        row.Children.Add(labelText);

        var valueText = new TextBlock
        {
            Text = value,
            FontSize = 13,
            Foreground = ThemeHelper.GetSubTextBrush()
        };
        _infoRowValueTextBlocks?.Add(valueText);
        row.Children.Add(valueText);

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

        tabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
        tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
        tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
        tabControl.Items.Add(new TabItem { Header = "时间计算器", Content = null, Tag = "TimeCalculator" });
        tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });

        if (_easterEggActive)
        {
            tabControl.Items.Add(new TabItem { Header = "女装", Content = null, Tag = "EasterEgg" });
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
                case "TimeCalculator":
                    tabItem.Content = new TimeCalculatorPage(_pluginSettings);
                    break;
                case "PluginSettings":
                    var pluginSettings = new PluginSettingsPage(_pluginSettings, _lunarInstaller);
                    pluginSettings.RequestRestartAction = ShowRestartButton;
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
                case "EasterEgg":
                    tabItem.Content = new EasterEggPage(_pluginSettings);
                    break;
                default:
                    tabItem.Content = new TextBlock { Text = "内容加载中...", Foreground = ThemeHelper.GetTextBrush() };
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
        if (_nameTextBlock != null)
            _nameTextBlock.Foreground = ThemeHelper.GetTextBrush();
        if (_authorTextBlock != null)
            _authorTextBlock.Foreground = ThemeHelper.GetSubTextBrush();

        if (_aboutContentTextBlocks != null)
        {
            for (int i = 0; i < _aboutContentTextBlocks.Count; i++)
            {
                if (i < 2)
                    _aboutContentTextBlocks[i].Foreground = ThemeHelper.GetTextBrush();
                else
                    _aboutContentTextBlocks[i].Foreground = ThemeHelper.GetSubTextBrush();
            }
        }

        if (_infoRowLabelTextBlocks != null)
        {
            foreach (var tb in _infoRowLabelTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetTextBrush();
            }
        }
        if (_infoRowValueTextBlocks != null)
        {
            foreach (var tb in _infoRowValueTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetSubTextBrush();
            }
        }
    }
}
