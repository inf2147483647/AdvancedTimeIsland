using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;

using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Shared;


namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 关于页面
/// </summary>
// 与"时间表悬浮窗"等页同属 AdvancedTimeIsland 导航分组（仿 SystemTools 子页面结构）
[SettingsPageInfo("AdvancedTimeIsland", "主设置")]
[Group("advancedtimeisland.main")]
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
    private DrawerHost? _drawerHost;
    private bool _easterEggActive;
    private readonly PluginSettings? _pluginSettings;

    /// <summary>
    /// 「管理启用的功能」- 小工具：关闭后主设置导航栏不显示「时间格式转换 / 时间计算器 / 专业名词解释」。
    /// 该选项无需重启，变更后立即重建导航栏标签页。
    /// </summary>
    private bool UtilitiesEnabled => _pluginSettings?.EnableUtilities ?? true;

    private TextBlock? _nameTextBlock;
    private TextBlock? _authorTextBlock;
    private List<TextBlock>? _aboutContentTextBlocks;
    private List<TextBlock>? _infoRowLabelTextBlocks;
    private List<TextBlock>? _infoRowValueTextBlocks;
    private Button? _usingGuideButton;

    public AboutPage() : this(null)
    {
    }

    public AboutPage(PluginSettings? pluginSettings = null)
    {
        _pluginSettings = pluginSettings;
        _easterEggActive = pluginSettings?.EnableEasterEgg ?? false;

        // 跨插件联动：FemboyTest 与女装彩蛋互斥。
        // 若 FemboyTest 插件已启用，则原有彩蛋已触发状态变为未触发。
        if (_easterEggActive && CrossPluginHelper.IsFemboyTestEnabled())
        {
            _easterEggActive = false;
            if (pluginSettings != null)
            {
                pluginSettings.EnableEasterEgg = false;
            }
        }

        // 订阅互斥强制重置事件，保证与 FemboyTest 完全互斥
        CrossPluginHelper.EasterEggForceReset += OnEasterEggForceReset;

        _easterEggDetector = new EasterEggDetector(11, 5);
        _easterEggDetector.OnActivated += OnEasterEggActivated;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // 【管理启用的功能】抽屉宿主必须在这里（创建标签栏之前）就建好：
        // TabControl 添加第一个标签页时会自动选中它并触发 SelectionChanged → LoadTabContent，
        // 其中会把本页的 FeatureDrawerHost 注入 PluginSettingsPage；
        // 若此时宿主还没创建，注入的就是 null（且标签内容不会重建），点击「管理启用的功能...」将毫无反应。
        _drawerHost = new DrawerHost
        {
            DrawerPlacement = DrawerHost.DrawerPlacementEnum.Right
        };

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
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = false
        };

        // 抽屉宿主挂在根页面（本页）上，而不是「插件设置」标签页内部：
        // 嵌在标签页里时抽屉只能占据标签内容区的一部分，还会被标签栏与标签内容挤压。
        // 抽屉内容由 PluginSettingsPage 构建后在此挂载（见 LoadTabContent）。
        _drawerHost.Content = scrollViewer;

        Content = _drawerHost;
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

    private string GetPluginVersion()
    {
        try
        {
            var version = TryReadVersionFromManifest();
            if (!string.IsNullOrEmpty(version))
            {
                if (_pluginSettings != null && _pluginSettings.CachedVersion != version)
                {
                    _pluginSettings.CachedVersion = version;
                }
                return version;
            }
        }
        catch { }

        if (!string.IsNullOrEmpty(_pluginSettings?.CachedVersion))
        {
            return _pluginSettings!.CachedVersion!;
        }

        return "未知版本";
    }

    private static string? TryReadVersionFromManifest()
    {
        try
        {
            var manifestPath = System.IO.Path.Combine(AppContext.BaseDirectory, "manifest.yml");
            if (!System.IO.File.Exists(manifestPath))
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var pluginDir = System.IO.Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    manifestPath = System.IO.Path.Combine(pluginDir, "manifest.yml");
                }
            }

            if (!System.IO.File.Exists(manifestPath))
                return null;

            var content = System.IO.File.ReadAllText(manifestPath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"^\s*version\s*:\s*(.+?)\s*$", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim().Trim('"', '\'');
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// 图标点击事件
    /// </summary>
    private void OnIconClicked(object? sender, PointerPressedEventArgs e)
    {
        // 跨插件联动：FemboyTest 与女装彩蛋互斥，FemboyTest 启用时不能触发彩蛋
        if (CrossPluginHelper.IsFemboyTestEnabled()) return;
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
        // 跨插件联动：FemboyTest 与女装彩蛋互斥，FemboyTest 启用时不能触发彩蛋
        if (CrossPluginHelper.IsFemboyTestEnabled()) return;
        if (_easterEggActive) return;
        _easterEggActive = true;
        if (_pluginSettings != null)
        {
            _pluginSettings.EnableEasterEgg = true;
        }

        SyncNavigationTabs();
        ShowEasterEggDialog();
    }

    /// <summary>
    /// FemboyTest 与女装彩蛋互斥强制重置处理（由互斥监视器触发）
    /// </summary>
    private void OnEasterEggForceReset()
    {
        // 同步重置彩蛋检测器：清空 IsActivated 与点击缓冲，
        // 避免 FemboyTest 关闭后彩蛋因 IsActivated 永不复位而无法再次触发（漏洞 10），
        // 也避免已累计的点击在 FemboyTest 启停切换后残留、跨状态续点（漏洞 4）。
        _easterEggDetector?.Reset();

        if (_easterEggActive)
        {
            _easterEggActive = false;
            SyncNavigationTabs();
        }
    }

    /// <summary>
    /// 女装开关状态变化处理
    /// </summary>
    private void OnEasterEggToggled(object? sender, bool isEnabled)
    {
        // 关闭彩蛋时同步重置检测器，保证再次开启后可重新累计点击触发
        if (!isEnabled)
        {
            _easterEggDetector?.Reset();
        }

        if (!isEnabled && _easterEggActive)
        {
            _easterEggActive = false;
            SyncNavigationTabs();
        }
    }

    /// <summary>
    /// 「小工具」开关状态变化处理：立即增删工具栏标签页，无需重启。
    /// </summary>
    private void OnUtilitiesToggled(object? sender, bool isEnabled)
    {
        SyncNavigationTabs();
    }

    /// <summary>
    /// 只显示右上角"需要重启"按钮，不弹出原生对话框
    /// </summary>
    private void ShowRestartButton()
    {
        try
        {
            // FA2 中设置窗口（SettingsWindowNew）本身就是顶层窗口；
            // FA3/CI2 中 SettingsWindowNew 改成了内嵌的 ContentPage（不再是窗口），
            // TopLevel.GetTopLevel 返回的是主窗口，而主窗口的 ViewModel 没有 IsRequestedRestart，
            // 导致反射静默失败。统一方案：沿视觉树向上查找带 IsRequestedRestart 的 ViewModel。
            foreach (var ancestor in this.GetVisualAncestors())
            {
                var viewModel = ancestor.GetType().GetProperty("ViewModel", BindingFlags.Public | BindingFlags.Instance);
                if (viewModel == null)
                    continue;

                var vm = viewModel.GetValue(ancestor);
                if (vm == null)
                    continue;

                var isRequestedRestartProp = vm.GetType().GetProperty("IsRequestedRestart", BindingFlags.Public | BindingFlags.Instance);
                if (isRequestedRestartProp == null)
                    continue;

                isRequestedRestartProp.SetValue(vm, true);
                return;
            }
        }
        catch
        {
        }
    }

    /// <summary>「女装」标签页的 Tag（内容懒加载，与其它标签页一致）。</summary>
    private const string EasterEggTabTag = "EasterEgg";

    /// <summary>受「小工具」开关控制的三个标签页的 Tag。</summary>
    private static readonly string[] UtilityTabTags = { "TimeConverter", "TimeCalculator", "Glossary" };

    /// <summary>
    /// 按当前开关同步导航栏标签页：只增删受开关控制的标签页（「小工具」三个 + 「女装」一个）。
    /// 【FA3 修复】原实现把整条导航栏 <c>Items.Clear()</c> 后重新添加：触发彩蛋是在点击事件里调用它的，
    ///   此时清空并重建 TabControl 的 Items 会让内容演示器停留在失效状态，表现为
    ///   "首次切到女装标签页一片空白，再切一次才正常"。改为按需增删即不会破坏演示器状态；
    ///   同时新增的标签页一律懒加载（Content = null，选中时才由 LoadTabContent 创建内容），
    ///   与「时间格式转换」等既有标签页走完全相同的加载路径。
    /// </summary>
    private void SyncNavigationTabs()
    {
        if (_tabControl == null) return;

        var tabs = _tabControl.Items.OfType<TabItem>().ToList();

        // ① 「小工具」：控制三个工具标签页（插在「关于」之后，保持 插件设置 / 关于 / 工具… / 女装 的顺序）
        var hasUtilityTabs = tabs.Any(tab => tab.Tag is string tag && UtilityTabTags.Contains(tag));
        if (UtilitiesEnabled && !hasUtilityTabs)
        {
            var aboutIndex = tabs.FindIndex(tab => (tab.Tag as string) == "About");
            var insertIndex = aboutIndex >= 0 ? aboutIndex + 1 : _tabControl.Items.Count;
            foreach (var (header, tag) in new[] { ("时间格式转换", "TimeConverter"), ("时间计算器", "TimeCalculator"), ("专业名词解释", "Glossary") })
            {
                _tabControl.Items.Insert(insertIndex++, new TabItem { Header = header, Content = null, Tag = tag });
            }
        }
        else if (!UtilitiesEnabled && hasUtilityTabs)
        {
            foreach (var tab in tabs.Where(tab => tab.Tag is string tag && UtilityTabTags.Contains(tag)).ToList())
            {
                _tabControl.Items.Remove(tab);
            }
        }

        // ② 「女装」：始终排在最后
        var hasEasterEggTab = tabs.Any(tab => (tab.Tag as string) == EasterEggTabTag);
        if (_easterEggActive && !hasEasterEggTab)
        {
            _tabControl.Items.Add(new TabItem { Header = "女装", Content = null, Tag = EasterEggTabTag });
        }
        else if (!_easterEggActive && hasEasterEggTab)
        {
            foreach (var tab in tabs.Where(tab => (tab.Tag as string) == EasterEggTabTag).ToList())
            {
                _tabControl.Items.Remove(tab);
            }
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
            Text = $"版本：{GetPluginVersion()}",
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _aboutContentTextBlocks.Add(versionText);
        panel.Children.Add(versionText);

        var descText = new TextBlock
        {
            Text = "AdvancedTimeIsland是一款为ClassIsland打造的高级时间管理插件，旨在弥补原生功能的不足（比如判断是否在某段时间范围内）。它提供丰富的时间相关组件，包括高级日期显示、多例倒计时、正计时、周期性倒计时等核心功能。同时支持农历日历、节气、生肖、星座、节日等展示。该插件具备强大的时间自动化能力，支持精确时间、周期性时间（年/月/周/日/时/分）、地方时、区时、农历时间等多种触发条件与规则，满足复杂的时间调度需求。此外还提供时间格式转换工具，支持北京时间、Unix时间戳、农历、区时、地方时之间的相互转换，并附带专业名词解释帮助用户理解时间相关概念。插件界面支持主题自适应，为用户提供更完善的时间管理体验。更多功能开发中，敬请期待。",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        };
        _aboutContentTextBlocks.Add(descText);
        panel.Children.Add(descText);

        _usingGuideButton = new Button
        {
            Content = "使用指南",
            FontSize = 16,
            Padding = new Thickness(16, 12),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 16, 0, 0)
        };

        UpdateUsingGuideButtonStyle();

        _usingGuideButton.Click += (s, e) =>
        {
            FluentAvaloniaCompatibilityHelper.NavigateToSettingsPage(this, "AdvancedTimeIslandUsingPointer");
        };

        panel.Children.Add(_usingGuideButton);

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
            // 反馈问题 → 跳转到 issue_feedback 页面（内含 GitHub / 问卷星双提交渠道）
            FluentAvaloniaCompatibilityHelper.NavigateToSettingsPage(this, "AdvancedTimeIslandIssueFeedback");
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

        // 【FA3 / Avalonia 12】禁用标签页切换的过场动画。
        //   Avalonia 12 给 TabControl 新增了 PageTransition 属性（Avalonia 11 没有此属性，故 FA2 不受影响，
        //   这也是本问题只在 FA3 出现的原因），主题会把它设上，于是切换标签页时会对内容播放位移/淡入过渡。
        //   实测该动画会"卡住"：切到任一标签页后内容停在动画中间状态 —— 表现为整页空白，再切一次才恢复。
        //   本插件各标签页内容都很重（女装页含 20+ 张图片），该动画只会带来不稳定，故直接置空禁用。
        //   注：属性在 Avalonia 11 上不存在，故用反射设置，两个目标框架共用同一份代码。
        tabControl.GetType()
            .GetProperty("PageTransition", BindingFlags.Public | BindingFlags.Instance)
            ?.SetValue(tabControl, null);

        tabControl.SelectionChanged += (s, e) =>
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem && tabItem.Content == null)
            {
                LoadTabContent(tabItem);
            }
        };

        tabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
        tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent(), Tag = "About" });

        // 【管理启用的功能】关闭「小工具」后不显示工具类标签页
        if (UtilitiesEnabled)
        {
            tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
            tabControl.Items.Add(new TabItem { Header = "时间计算器", Content = null, Tag = "TimeCalculator" });
            tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });
        }

        if (_easterEggActive)
        {
            tabControl.Items.Add(new TabItem { Header = "女装", Content = null, Tag = EasterEggTabTag });
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
                    var pluginSettings = new PluginSettingsPage(_pluginSettings);
                    pluginSettings.RequestRestartAction = ShowRestartButton;
                    // 抽屉宿主在根页面上：本页只提供取值委托与抽屉内容，宿主在点击时才被取用
                    // （避免本页创建时宿主尚未创建而注入 null）
                    pluginSettings.FeatureDrawerHostProvider = () => _drawerHost;
                    if (_easterEggActive)
                    {
                        pluginSettings.ShowEasterEggSetting();
                    }
                    pluginSettings.EasterEggToggled += OnEasterEggToggled;
                    // 【管理启用的功能】「小工具」变更后立即重建导航栏标签页（无需重启）
                    pluginSettings.UtilitiesToggled += OnUtilitiesToggled;
                    tabItem.Content = pluginSettings;
                    break;
                case "Glossary":
                    tabItem.Content = new GlossaryPage(_pluginSettings);
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
        CrossPluginHelper.EasterEggForceReset -= OnEasterEggForceReset;
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

        UpdateUsingGuideButtonStyle();
    }

    private void UpdateUsingGuideButtonStyle()
    {
        if (_usingGuideButton == null) return;

        var isDark = ThemeHelper.IsDarkTheme();
        _usingGuideButton.Background = isDark
            ? new SolidColorBrush(Color.Parse("#37373D"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
        _usingGuideButton.Foreground = GetAccentBrush();
        _usingGuideButton.BorderBrush = isDark
            ? new SolidColorBrush(Color.Parse("#444444"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));
        _usingGuideButton.BorderThickness = new Thickness(1);
    }
}
