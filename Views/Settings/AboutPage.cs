using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;

using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared;
// Toast 的 InfoBar 严重级别枚举：FA2 (net8) 叫 InfoBarSeverity，FA3 (net10) 改名 FAInfoBarSeverity，
// 用别名统一，业务代码统一用 FAInfoBarSeverity（同 EasterEggPage）
#if NET10_0_OR_GREATER
using FAInfoBarSeverity = FluentAvalonia.UI.Controls.FAInfoBarSeverity;
#else
using FAInfoBarSeverity = FluentAvalonia.UI.Controls.InfoBarSeverity;
#endif


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
    private Button? _changelogButton;
    private bool _isLoadingChangelog;

    public AboutPage() : this(null)
    {
    }

    public AboutPage(PluginSettings? pluginSettings = null)
    {
        _pluginSettings = pluginSettings;
        _easterEggActive = pluginSettings?.EnableEasterEgg ?? false;

        _easterEggDetector = new EasterEggDetector(11, 5);
        _easterEggDetector.OnActivated += OnEasterEggActivated;

        InitializeComponent();
        IntroAnimationHelper.EnableSmoothScroll(this);
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

        SyncNavigationTabs();
        ShowEasterEggDialog();
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

        // 更新日志：点击后从右侧滑出抽屉，展示 CHANGELOG.md 中当前版本的发布说明
        _changelogButton = new Button
        {
            Content = "更新日志",
            FontSize = 16,
            Padding = new Thickness(16, 12),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
        };
        UpdateChangelogButtonStyle();
        _changelogButton.Click += OnChangelogButtonClick;
        panel.Children.Add(_changelogButton);

        return panel;
    }

    /// <summary>
    /// 「更新日志」按钮点击：先打开抽屉显示加载占位（ExpressiveLoadingIndicator），
    /// 异步加载 CHANGELOG.md 完成后替换为正式内容。
    /// </summary>
    private async void OnChangelogButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_drawerHost == null || _isLoadingChangelog)
        {
            return;
        }

        _isLoadingChangelog = true;
        try
        {
            _drawerHost.DrawerContent = BuildChangelogDrawerLoading();
            _drawerHost.IsDrawerOpen = true;

            var entries = await ChangelogHelper.LoadEntriesAsync();
            if (_drawerHost.IsDrawerOpen)
            {
                _drawerHost.DrawerContent = BuildChangelogDrawerContent(entries);
            }
        }
        catch (Exception ex)
        {
            _lastChangelogError = new ChangelogErrorRecord(DateTime.Now, ex.GetType().Name, ex.Message);
            if (_drawerHost.IsDrawerOpen)
            {
                _drawerHost.DrawerContent = BuildChangelogDrawerMessage("更新日志加载失败", ex.Message);
            }
        }
        finally
        {
            _isLoadingChangelog = false;
        }
    }

    /// <summary>更新日志抽屉宽度（与「管理启用的功能」抽屉一致，DrawerHost 按抽屉内容的 Width + 水平 Margin 决定滑出宽度）。</summary>
    private const double ChangelogDrawerWidth = 500;

    /// <summary>构建抽屉标题行：标题靠左、关闭按钮靠右（加载占位与正式内容共用）。</summary>
    private Grid BuildChangelogDrawerHeader()
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var closeButton = new Button
        {
            Content = "✕",
            FontSize = 14,
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        closeButton.Click += (_, _) =>
        {
            if (_drawerHost != null)
            {
                _drawerHost.IsDrawerOpen = false;
            }
        };
        Grid.SetColumn(closeButton, 1);
        header.Children.Add(closeButton);
        header.Children.Add(new TextBlock
        {
            Text = "更新日志",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });
        return header;
    }

    /// <summary>构建加载占位视图：ExpressiveLoadingIndicator + 提示文本（宽度与正式内容一致，避免抽屉宽度跳动）。</summary>
    private Control BuildChangelogDrawerLoading()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        panel.Children.Add(BuildChangelogDrawerHeader());
        panel.Children.Add(new AdvancedTimeIsland.Views.Controls.ExpressiveLoadingIndicator
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = GetAccentBrush(),
            Margin = new Thickness(0, 48, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "正在加载更新日志…",
            FontSize = 13,
            Foreground = ThemeHelper.GetSubTextBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        });
        return new Border { Width = ChangelogDrawerWidth, Child = panel };
    }

    /// <summary>构建更新日志抽屉：固定宽度根容器 + 标题栏 + 滚动内容区（长日志可滚动浏览）。</summary>
    private Control BuildChangelogDrawerContent(IReadOnlyList<ChangelogHelper.ChangelogEntry> entries)
    {
        var root = new Border { Width = ChangelogDrawerWidth };

        var rootGrid = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        // 标题行（第 0 行）
        rootGrid.Children.Add(BuildChangelogDrawerHeader());

        // 版本卡片列表（第 1 行，剩余高度内滚动）
        var listPanel = new StackPanel { Orientation = Orientation.Vertical };
        if (entries.Count == 0)
        {
            listPanel.Children.Add(new TextBlock
            {
                Text = "暂无更新日志记录。",
                FontSize = 13,
                Foreground = ThemeHelper.GetSubTextBrush(),
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            foreach (var entry in entries)
            {
                listPanel.Children.Add(BuildChangelogEntryCard(entry));
            }
        }

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = listPanel
        };
        Grid.SetRow(scrollViewer, 1);
        rootGrid.Children.Add(scrollViewer);

        root.Child = rootGrid;
        return root;
    }

    /// <summary>最近一次更新日志加载失败的记录（用于错误占位的右键“另存为错误日志”）。</summary>
    private sealed record ChangelogErrorRecord(DateTime Time, string ExceptionType, string Message);

    private ChangelogErrorRecord? _lastChangelogError;

    /// <summary>
    /// 构建加载失败 / 异常占位视图（照搬 EasterEggPage 错误面板模式）：
    /// 标题 + 错误详情文本 + 右键“另存为…”（MenuFlyout 挂 ContextFlyout——FA3 无 ContextMenu 样式），
    /// 把加载失败记录导出为文本日志，成功/失败均通过宿主右下角 Toast 提示。
    /// </summary>
    private Control BuildChangelogDrawerMessage(string title, string message)
    {
        if (_lastChangelogError == null)
        {
            _lastChangelogError = new ChangelogErrorRecord(DateTime.Now, "Exception", message);
        }

        var root = new Border { Width = ChangelogDrawerWidth };
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        panel.Children.Add(BuildChangelogDrawerHeader());

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush(),
            Margin = new Thickness(0, 0, 0, 8)
        });

        var error = _lastChangelogError;
        panel.Children.Add(new TextBlock
        {
            Text = $"时间：{error.Time:yyyy-MM-dd HH:mm:ss}\n错误：{error.ExceptionType}\n{error.Message}",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "提示：在此区域右键可另存为错误日志",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        });

        // 右键“另存为…”错误日志（与 EasterEggPage 错误面板一致：MenuFlyout/ContextFlyout 在 FA2/FA3 行为一致）
        var saveLogMenuItem = new MenuItem { Header = "另存为…" };
        saveLogMenuItem.Click += async (_, _) => await SaveChangelogErrorLogAsync(root);
        var menuFlyout = new MenuFlyout();
        menuFlyout.Items.Add(saveLogMenuItem);
        menuFlyout.Opening += (_, _) => saveLogMenuItem.IsEnabled = _lastChangelogError != null;
        panel.ContextFlyout = menuFlyout;

        root.Child = panel;
        return root;
    }

    /// <summary>
    /// “另存为错误日志”：弹出系统保存文件对话框（默认 .txt），把更新日志加载失败记录
    /// （含环境信息）写入用户选择的位置。对话框 await 异步等待，写盘在后台线程执行；
    /// 成功/失败均通过宿主右下角 Toast 提示。交互照搬 EasterEggPage 的 SaveErrorLogAsAsync。
    /// </summary>
    private async Task SaveChangelogErrorLogAsync(Control anchor)
    {
        string targetPath = string.Empty;
        try
        {
            var topLevel = TopLevel.GetTopLevel(anchor);
            if (topLevel?.StorageProvider == null)
            {
                anchor.ShowToast(new ToastMessage("无法获取宿主窗口的存储服务，请重试。")
                {
                    Title = "错误日志保存失败",
                    Severity = FAInfoBarSeverity.Error,
                    AutoClose = false
                });
                return;
            }

            if (_lastChangelogError == null)
            {
                anchor.ShowToast(new ToastMessage("暂无错误记录。")
                {
                    Title = "错误日志保存失败",
                    Severity = FAInfoBarSeverity.Error,
                    AutoClose = false
                });
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "另存为错误日志",
                SuggestedFileName = $"AdvancedTimeIsland-changelog-error-{DateTime.Now:yyyyMMdd-HHmmss}",
                DefaultExtension = "txt",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("文本文件") { Patterns = new[] { "*.txt" } }
                }
            });
            if (file == null)
            {
                return;
            }

            targetPath = file.Path.LocalPath;
            var content = BuildChangelogErrorLogText();
            await Task.Run(() => File.WriteAllText(targetPath, content));
            anchor.ShowSuccessToast($"错误日志已保存到 {targetPath}");
        }
        catch (Exception ex)
        {
            anchor.ShowToast(new ToastMessage(DescribeSaveFailure(targetPath, ex))
            {
                Title = "错误日志保存失败",
                Severity = FAInfoBarSeverity.Error,
                AutoClose = false
            });
        }
    }

    /// <summary>组装更新日志错误日志文本：环境信息 + 失败记录（时间/异常类型/消息）。结构照搬 EasterEggPage。</summary>
    private string BuildChangelogErrorLogText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("AdvancedTimeIsland 更新日志加载错误日志");
        sb.AppendLine("========================================");
        sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"运行时：{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"操作系统：{System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine("========================================");
        var error = _lastChangelogError;
        sb.AppendLine($"时间：{error?.Time:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"异常类型：{error?.ExceptionType}");
        sb.AppendLine($"消息：{error?.Message}");
        sb.AppendLine("========================================");
        sb.AppendLine("本日志仅包含更新日志（CHANGELOG.md）加载时的错误信息，不含任何个人数据。");
        return sb.ToString();
    }

    /// <summary>
    /// 把保存失败异常翻译为用户可读的具体原因（照搬 EasterEggPage 的 DescribeSaveFailure）：
    /// 磁盘空间不足 / 无写入权限 / 路径无效 / 文件被占用等。
    /// </summary>
    private static string DescribeSaveFailure(string targetPath, Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return "没有写入该位置的权限，请尝试更换保存位置（如文档或桌面）。";
        }

        if (ex is ArgumentException or System.IO.PathTooLongException or NotSupportedException)
        {
            return $"保存路径无效：{ex.Message}";
        }

        if (ex is IOException)
        {
            // IOException 覆盖磁盘满、文件被占用、位置不可达等多种情况；优先探测目标盘剩余空间给出更具体的原因
            try
            {
                var root = Path.GetPathRoot(targetPath);
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    if (drive.IsReady && drive.AvailableFreeSpace < 64L * 1024 * 1024)
                    {
                        return $"目标磁盘 {root} 剩余空间不足（仅剩 {drive.AvailableFreeSpace / (1024 * 1024)} MB），请清理空间后重试。";
                    }
                }
            }
            catch
            {
                // 磁盘探测失败时退回通用 IO 描述
            }

            return $"写入文件时发生 I/O 错误，目标位置可能不可用或文件正被占用：{ex.Message}";
        }

        return ex.Message;
    }

    /// <summary>构建单个版本的卡片：版本号 + 发布日期 + 正文（轻量 Markdown 渲染）。</summary>
    private Border BuildChangelogEntryCard(ChangelogHelper.ChangelogEntry entry)
    {
        var cardPanel = new StackPanel { Orientation = Orientation.Vertical };

        var headGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(0, 0, 0, 4)
        };
        headGrid.Children.Add(new TextBlock
        {
            Text = entry.Version == ChangelogHelper.UnreleasedVersion ? "未发布" : $"v{entry.Version}",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = GetAccentBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });
        var dateText = new TextBlock
        {
            Text = entry.Date,
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(dateText, 1);
        headGrid.Children.Add(dateText);
        cardPanel.Children.Add(headGrid);

        // 正文统一交给汉服模板的 Markdig 渲染（支持表格 / 代码块 / 删除线 / alert 等全部语法），兼容将来任意 CHANGELOG 格式
        var bodyPanel = new StackPanel { Orientation = Orientation.Vertical };
        HanfuPageTemplate.RenderMarkdownToPanel(bodyPanel, entry.RawMarkdown);
        cardPanel.Children.Add(bodyPanel);

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Child = cardPanel
        };
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

        // 标签页切换的过场动画：参考 ClassIsland「档案编辑」导航栏切换所用的翻页效果
        // （旧页滑出淡出、新页滑入淡入），由工具集内的 PageFlipTransition 实现。
        // 之前为规避“动画播一半卡住、切过去整页空白”而直接置空禁用，该问题已由
        // PageFlipTransition 的复位 + 代次守卫解决（见其类注释）；动画级别为“禁用”时仍会置空。
        //   Avalonia 12 给 TabControl 新增了 PageTransition 属性，Avalonia 11 没有此属性（故 FA2 不受影响）。
        IntroAnimationHelper.EnablePageFlip(tabControl);

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
        UpdateChangelogButtonStyle();
    }

    private void UpdateChangelogButtonStyle()
    {
        if (_changelogButton == null) return;

        var isDark = ThemeHelper.IsDarkTheme();
        _changelogButton.Background = isDark
            ? new SolidColorBrush(Color.Parse("#37373D"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
        _changelogButton.Foreground = GetAccentBrush();
        _changelogButton.BorderBrush = isDark
            ? new SolidColorBrush(Color.Parse("#444444"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));
        _changelogButton.BorderThickness = new Thickness(1);
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
