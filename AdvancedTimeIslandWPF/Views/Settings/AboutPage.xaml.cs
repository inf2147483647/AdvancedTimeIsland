using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;

using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using MaterialDesignThemes.Wpf;


namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 关于页面
/// </summary>
[SettingsPageInfo("AdvancedTimeIsland", "AdvancedTimeIsland 设置")]
public partial class AboutPage : SettingsPageBase
{
    private EasterEggDetector _easterEggDetector;
    private bool _easterEggActive;
    private readonly PluginSettings? _pluginSettings;

    private List<TextBlock>? _aboutContentTextBlocks;
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

        InitializePageContent();
    }

    private void InitializePageContent()
    {
        // 图标（含彩蛋点击检测）
        var image = new Image
        {
            Stretch = Stretch.UniformToFill,
            Width = 64,
            Height = 64
        };
        IconBorder.Child = image;
        LoadIconImage(image);

        // 免责声明 InfoBar（未接受过时显示）
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
            MainPanel.Children.Insert(0, disclaimerBar);
        }

        BuildNavigationTabs();
    }

    private void LoadIconImage(Image imageControl)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            // 直接复用 manifest 引用的根目录 icon.png，避免在包内保留重复副本以减小安装包体积
            var fullPath = System.IO.Path.Combine(baseDir, "icon.png");

            if (System.IO.File.Exists(fullPath))
            {
                imageControl.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(fullPath));
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
                        imageControl.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(fullPath));
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
    private void OnIconClicked(object? sender, MouseButtonEventArgs e)
    {
        // 跨插件联动：FemboyTest 与女装彩蛋互斥，FemboyTest 启用时不能触发彩蛋
        if (CrossPluginHelper.IsFemboyTestEnabled()) return;
        _easterEggDetector.RecordClick();
    }

    private void OnProjectButtonClick(object? sender, RoutedEventArgs e)
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
    }

    private void OnFeedbackButtonClick(object? sender, RoutedEventArgs e)
    {
        // 反馈问题 → 跳转到 issue_feedback 页面（内含 GitHub / 问卷星双提交渠道）
        try
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandIssueFeedback?ci_keepHistory=true"));
        }
        catch
        {
            // 忽略导航错误
        }
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
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());
        FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, Window.GetWindow(this));
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

        UpdateTabOrder(true);
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
            UpdateTabOrder(false);
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
            // 沿逻辑树向上查找带 IsRequestedRestart 的 ViewModel。
            DependencyObject? current = this;
            while (current != null)
            {
                var viewModel = current.GetType().GetProperty("ViewModel", BindingFlags.Public | BindingFlags.Instance);
                if (viewModel != null)
                {
                    var vm = viewModel.GetValue(current);
                    if (vm != null)
                    {
                        var isRequestedRestartProp = vm.GetType().GetProperty("IsRequestedRestart", BindingFlags.Public | BindingFlags.Instance);
                        if (isRequestedRestartProp != null)
                        {
                            isRequestedRestartProp.SetValue(vm, true);
                            return;
                        }
                    }
                }
                current = current is FrameworkElement fe ? fe.Parent : null;
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 初始化导航标签
    /// </summary>
    private void BuildNavigationTabs()
    {
        MainTabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
        MainTabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
        MainTabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
        MainTabControl.Items.Add(new TabItem { Header = "时间计算器", Content = null, Tag = "TimeCalculator" });
        MainTabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });

        if (_easterEggActive)
        {
            MainTabControl.Items.Add(new TabItem { Header = "女装", Content = null, Tag = "EasterEgg" });
        }
    }

    /// <summary>
    /// 更新标签顺序
    /// </summary>
    private void UpdateTabOrder(bool easterEggActive)
    {
        MainTabControl.Items.Clear();

        MainTabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
        MainTabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent() });
        MainTabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
        MainTabControl.Items.Add(new TabItem { Header = "时间计算器", Content = null, Tag = "TimeCalculator" });
        MainTabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });

        if (easterEggActive)
        {
            MainTabControl.Items.Add(new TabItem { Header = "女装", Content = new EasterEggPage(_pluginSettings) });
        }
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem && tabItem.Content == null)
        {
            LoadTabContent(tabItem);
        }
    }

    /// <summary>
    /// 创建关于内容
    /// </summary>
    private FrameworkElement CreateAboutContent()
    {
        _aboutContentTextBlocks = new List<TextBlock>();

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };

        var authorText = new TextBlock
        {
            Text = "作者：inf2147483647",
            FontSize = 14
        };
        authorText.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
        _aboutContentTextBlocks.Add(authorText);
        panel.Children.Add(authorText);

        var versionText = new TextBlock
        {
            Text = $"版本：{GetPluginVersion()}",
            FontSize = 14
        };
        versionText.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
        _aboutContentTextBlocks.Add(versionText);
        panel.Children.Add(versionText);

        var descText = new TextBlock
        {
            Text = "AdvancedTimeIsland是一款为ClassIsland打造的高级时间管理插件，旨在弥补原生功能的不足（比如判断是否在某段时间范围内）。它提供丰富的时间相关组件，包括高级日期显示、多例倒计时、正计时、周期性倒计时等核功能。同时支持农历日历、节气、生肖、星座、节日等展示。该插件具备强大的时间自动化能力，支持精确时间、周期性时间（年/月/周/日/时/分）、地方时、区时、农历时间等多种触发条件与规则，满足复杂的时间调度需求。此外还提供时间格式转换工具，支持北京时间、Unix时间戳、农历、区时、地方时之间的相互转换，并附带专业名词解释帮助用户理解时间相关概念。插件界面支持主题自适应，为用户提供更完善的时间管理体验。更多功能开发中，敬请期待。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };
        descText.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBodyLight");
        _aboutContentTextBlocks.Add(descText);
        panel.Children.Add(descText);

        _usingGuideButton = new Button
        {
            Content = "使用指南",
            FontSize = 16,
            Padding = new Thickness(16, 12, 16, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 16, 0, 0)
        };

        UpdateUsingGuideButtonStyle();

        _usingGuideButton.Click += (s, e) =>
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandUsingPointer?ci_keepHistory=true"));
        };

        panel.Children.Add(_usingGuideButton);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = panel
        };

        return scrollViewer;
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
                    if (_easterEggActive)
                    {
                        pluginSettings.ShowEasterEggSetting();
                    }
                    pluginSettings.EasterEggToggled += OnEasterEggToggled;
                    tabItem.Content = pluginSettings;
                    break;
                case "Glossary":
                    tabItem.Content = new GlossaryPage(_pluginSettings);
                    break;
                case "EasterEgg":
                    tabItem.Content = new EasterEggPage(_pluginSettings);
                    break;
                default:
                    tabItem.Content = new TextBlock { Text = "内容加载中...", Foreground = Brushes.Gray };
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

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        CrossPluginHelper.EasterEggForceReset -= OnEasterEggForceReset;
    }

    private void UpdateUsingGuideButtonStyle()
    {
        if (_usingGuideButton == null) return;

        _usingGuideButton.SetResourceReference(Control.BackgroundProperty, "MaterialDesignCardBackground");
        _usingGuideButton.SetResourceReference(Control.ForegroundProperty, "MaterialDesignBody");
        _usingGuideButton.BorderBrush = Brushes.Transparent;
        _usingGuideButton.BorderThickness = new Thickness(0);
    }
}
