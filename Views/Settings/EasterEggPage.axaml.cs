using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
// 别名指向插件内复制的占位符：避免与宿主 ClassIsland.Core.Controls 中的同名类型冲突（net10 侧两者都存在）
using ExpressiveLoadingIndicator = AdvancedTimeIsland.Views.Controls.ExpressiveLoadingIndicator;


namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 女装彩蛋页面
/// 使用Markdown格式展示内容
/// </summary>
public partial class EasterEggPage : UserControl
{
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

    private readonly PluginSettings? _pluginSettings;

    private List<TextBlock>? _normalTextBlocks;
    private List<TextBlock>? _boldTextBlocks;
    private List<Border>? _separatorBorders;
    private Border? _markdownSectionBorder;
    private List<(string url, Image image, StackPanel errorPanel, TextBlock errorDetailText, Button retryButton, ExpressiveLoadingIndicator loadingIndicator)>? _imageLoadInfos;
    private List<Border>? _imageCardBorders;
    private Border? _headerSeparatorBorder;
    private Button? _backToTopButton;
    private OverlayLayer? _overlayLayer;
    private ScrollViewer? _outerScrollViewer;
    private Control? _femboyTestWarningBar;
    private DispatcherTimer? _femboyTestWatchTimer;

    public EasterEggPage() : this(null)
    {
    }

    public EasterEggPage(PluginSettings? pluginSettings = null)
    {
        _pluginSettings = pluginSettings;
        InitializeComponent();
        WireUI();
    }

    /// <summary>
    /// axaml 只承载静态外壳（滚动容器 / 主面板 / 标题 / 三个警告栏宿主 / Markdown 宿主 / 底部留白）；
    /// 条件警告栏（FA2/FA3 的 InfoBar 类型名不同，须经兼容 Helper 创建）与 Markdown 图片区在此动态装配。
    /// </summary>
    private void WireUI()
    {
        if (_pluginSettings?.EasterEggDisclaimerAccepted != true)
        {
            var disclaimerBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Title", "免责声明");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Message", "仅供娱乐，无不良引导。");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsClosable", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Margin", new Thickness(0, 0, 0, 8));
            FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(disclaimerBar, (s, e) =>
            {
                _pluginSettings!.EasterEggDisclaimerAccepted = true;
            });
            DisclaimerHost.Content = disclaimerBar;
        }

        if (_pluginSettings?.EasterEggInfoAccepted != true)
        {
            var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "关闭女装彩蛋的方式：进入插件设置，划到最底部，关闭“女装”");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Margin", new Thickness(0, 0, 0, 8));
            FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(infoBar, (s, e) =>
            {
                _pluginSettings!.EasterEggInfoAccepted = true;
            });
            InfoHost.Content = infoBar;
        }

        // FemboyTest 启用时显示错误类型警告（不可关闭）；FemboyTest 关闭时取消显示。
        // 该警告与页面是否可见无关，必须始终跟随 FemboyTest 实际运行状态动态增删，
        // 否则可能留下绕过互斥检测的漏洞。
        _femboyTestWarningBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(_femboyTestWarningBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityError());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(_femboyTestWarningBar, "Message", "《祖上传下来的护身符炸了》《爷爷送的玉佩炸了》《奶奶给我的黄符纸自燃了》《保我的菩萨断臂求生了》《护我道上的狐仙被别的狐狸配了》《我拜的关公倒地了》《家里的金银器全发黑了》《僵尸在我旁边撒糯米》");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(_femboyTestWarningBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(_femboyTestWarningBar, "IsClosable", false);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(_femboyTestWarningBar, "Margin", new Thickness(0, 0, 0, 8));

        // 定时监视 FemboyTest 运行状态，动态增删该警告栏
        _femboyTestWatchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _femboyTestWatchTimer.Tick += (s, e) => UpdateFemboyTestWarningBar();
        _femboyTestWatchTimer.Start();
        UpdateFemboyTestWarningBar();

        // Markdown 图片区（逐图独立缓冲占位符 / 错误重试），注入 axaml 中的命名宿主
        MarkdownSectionHost.Content = CreateMarkdownSection(MarkdownContent);
    }

    private const string MarkdownContent = @"## 图片展示

![图片1](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/womenswear_IMG_6868.jpg)

![图片2](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/womenswear_IMG_6871.jpg)

![图片3](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/womenswear_IMG_6880.jpg)

![图片4](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/womenswear_IMG_6892.jpg)

![图片5](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/womenswear_IMG_6907.jpg)

![图片6](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/8X9A0022.jpg)

![图片7](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/8X9A0031.jpg)

![图片8](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/8X9A0484.jpg)

![图片9](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/8X9A0488.jpg)

![图片10](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC02568.jpg)

![图片11](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC02575.jpg)

![图片12](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC02578.jpg)

![图片13](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC07548.jpg)

![图片14](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC07560.jpg)

![图片15](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC02563.jpg)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A5701.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A5704.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A5723.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A5728.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A6161.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A6172.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A6176.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A6179.JPG)

![图片16](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/2E3A6168.JPG)


";

    /// <summary>
    /// 创建Markdown格式的文本区域
    /// </summary>
    /// <summary>
    /// 创建图片展示卡片：头部为“图片展示”标题与“刷新”按钮同一行，下方分隔线后是图片列表；
    /// 图片容器卡片化（圆角 / 衬底 / ClipToBounds），每张图加载成功后各自播放一次华丽入场动画。
    /// </summary>
    private Border CreateMarkdownSection(string markdownText)
    {
        _normalTextBlocks = new List<TextBlock>();
        _boldTextBlocks = new List<TextBlock>();
        _separatorBorders = new List<Border>();
        _imageCardBorders = new List<Border>();
        _headerSeparatorBorder = null;
        _imageLoadInfos = new List<(string url, Image image, StackPanel errorPanel, TextBlock errorDetailText, Button retryButton, ExpressiveLoadingIndicator loadingIndicator)>();

        // 外层卡片：去掉原先的 2px 粗边框，统一为圆角卡片（与插件其他卡片风格一致，深浅主题自适应）
        var section = new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };
        _markdownSectionBorder = section;

        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        // 图片列表独立成面板：图片之间统一 16px 间距，不与 Markdown 空行的 8px 占位混在一起
        StackPanel? imageListPanel = null;

        // 解析Markdown并创建文本块
        var lines = markdownText.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                // 图片区已由独立面板的 Spacing 控制间距，末尾空行不再插入占位条
                if (imageListPanel == null)
                {
                    content.Children.Add(new Border { Height = 8 });
                }
                continue;
            }

            if (line.StartsWith("## "))
            {
                // 二级标题：与“刷新”按钮排成同一行（标题左对齐、按钮右对齐）
                var title = line.Substring(3).Trim();

                var headerGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Margin = new Thickness(0, 8, 0, 4)
                };

                var titleTextBlock = new TextBlock
                {
                    Text = title,
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.HotPink,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(titleTextBlock, 0);
                headerGrid.Children.Add(titleTextBlock);

                if (title == "图片展示")
                {
                    var refreshButton = new Button
                    {
                        Content = "刷新",
                        Padding = new Thickness(14, 6, 14, 6),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0)
                    };
                    refreshButton.Click += RefreshButton_Click;
                    Grid.SetColumn(refreshButton, 1);
                    headerGrid.Children.Add(refreshButton);
                }

                content.Children.Add(headerGrid);

                // 标题下分隔线
                _headerSeparatorBorder = new Border
                {
                    Height = 1,
                    Background = ThemeHelper.GetSeparatorBrush(),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                content.Children.Add(_headerSeparatorBorder);

                // 图片列表面板（标题之后的所有 ![]( ) 都进这里）
                imageListPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 16,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                content.Children.Add(imageListPanel);
            }
            else if (line.StartsWith("---"))
            {
                // 分隔线
                var sep = new Border
                {
                    Height = 1,
                    Background = ThemeHelper.GetGrayBrush(),
                    Margin = new Thickness(0, 8, 0, 8)
                };
                _separatorBorders.Add(sep);
                content.Children.Add(sep);
            }
            else if (line.StartsWith("!["))
            {
                // Markdown 图片 ![alt](url)：标题出现后进入图片列表面板
                var imageControl = CreateMarkdownImage(line);
                if (imageControl != null)
                {
                    (imageListPanel ?? content).Children.Add(imageControl);
                }
            }
            else if (line.StartsWith("- ["))
            {
                // 列表项链接
                var linkText = line.Substring(2).Trim();
                var linkPanel = CreateMarkdownLink(linkText);
                content.Children.Add(linkPanel);
            }
            else if (line.StartsWith("["))
            {
                // 纯链接
                var linkPanel = CreateMarkdownLink(line);
                content.Children.Add(linkPanel);
            }
            else if (line.StartsWith("**") && line.EndsWith("**"))
            {
                // 粗体标题
                var boldText = new TextBlock
                {
                    Text = line.Trim('*'),
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = ThemeHelper.GetOrangeBrush(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 4)
                };
                _boldTextBlocks.Add(boldText);
                content.Children.Add(boldText);
            }
            else if (line.Contains("[") && line.Contains("]("))
            {
                // 包含链接的文本
                var textBlock = CreateTextWithLink(line);
                content.Children.Add(textBlock);
            }
            else
            {
                // 普通文本
                var normalText = new TextBlock
                {
                    Text = line.Trim(),
                    FontSize = 13,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    TextWrapping = TextWrapping.Wrap
                };
                _normalTextBlocks.Add(normalText);
                content.Children.Add(normalText);
            }
        }

        section.Child = content;
        return section;
    }

    /// <summary>
    /// 创建Markdown链接
    /// </summary>
    private Panel CreateMarkdownLink(string linkText)
    {
        // 解析 [text](url) 格式
        var startBracket = linkText.IndexOf('[');
        var endBracket = linkText.IndexOf(']');
        var startParen = linkText.IndexOf('(');
        var endParen = linkText.IndexOf(')');

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };

        if (startBracket >= 0 && endBracket > startBracket && startParen > endBracket && endParen > startParen)
        {
            var text = linkText.Substring(startBracket + 1, endBracket - startBracket - 1);
            var url = linkText.Substring(startParen + 1, endParen - startParen - 1);

            var link = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = GetAccentBrush(),
                TextDecorations = TextDecorations.Underline,
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            link.PointerPressed += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // 忽略错误
                }
            };

            panel.Children.Add(new TextBlock
            {
                Text = "• ",
                FontSize = 13,
                Foreground = ThemeHelper.GetSubTextBrush()
            });
            panel.Children.Add(link);
        }
        else
        {
            panel.Children.Add(new TextBlock
            {
                Text = linkText,
                FontSize = 13,
                Foreground = ThemeHelper.GetSubTextBrush()
            });
        }

        return panel;
    }

    /// <summary>
    /// 创建包含链接的文本
    /// </summary>
    private TextBlock CreateTextWithLink(string line)
    {
        // 简单处理：提取链接部分
        var textBlock = new TextBlock
        {
            FontSize = 13,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        };

        // 解析 "更多内容请访问：[Cute-Dress/Dress](url)" 格式
        var colonIndex = line.IndexOf('：');
        if (colonIndex >= 0)
        {
            var beforeColon = line.Substring(0, colonIndex + 1);
            var afterColon = line.Substring(colonIndex + 1);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };

            panel.Children.Add(new TextBlock
            {
                Text = beforeColon,
                FontSize = 13,
                Foreground = ThemeHelper.GetSubTextBrush()
            });

            // 解析链接
            var startBracket = afterColon.IndexOf('[');
            var endBracket = afterColon.IndexOf(']');
            var startParen = afterColon.IndexOf('(');
            var endParen = afterColon.IndexOf(')');

            if (startBracket >= 0 && endBracket > startBracket && startParen > endBracket && endParen > startParen)
            {
                var linkText = afterColon.Substring(startBracket + 1, endBracket - startBracket - 1);
                var url = afterColon.Substring(startParen + 1, endParen - startParen - 1);

                var link = new TextBlock
                {
                    Text = linkText,
                    FontSize = 13,
                    Foreground = GetAccentBrush(),
                    TextDecorations = TextDecorations.Underline,
                    Cursor = new Cursor(StandardCursorType.Hand)
                };

                link.PointerPressed += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // 忽略错误
                    }
                };

                panel.Children.Add(link);
            }

            return new TextBlock
            {
                Text = line.Replace("[", "").Replace("]", "").Split('(')[0].Trim(),
                FontSize = 13,
                Foreground = ThemeHelper.GetSubTextBrush(),
                TextWrapping = TextWrapping.Wrap
            };
        }

        textBlock.Text = line.Replace("[", "").Replace("]", "").Split('(')[0].Trim();
        return textBlock;
    }

    /// <summary>
    /// 创建 Markdown 图片控件
    /// </summary>
    /// <summary>
    /// 创建 Markdown 图片卡片：圆角衬底容器（ClipToBounds 裁切），三层叠加——
    /// 图片 / 加载占位符 / 失败重试面板。容器挂 Opacity 与 RenderTransform(TransformOperations) 两条 Transition，
    /// 供华丽入场动画插值（不能对 Transform 对象跑 Animation，运行时会强转 Visual 崩溃）。
    /// </summary>
    private Control CreateMarkdownImage(string line)
    {
        var startBracket = line.IndexOf('[');
        var endBracket = line.IndexOf(']');
        var startParen = line.IndexOf('(');
        var endParen = line.IndexOf(')');

        if (startBracket < 0 || endBracket <= startBracket || startParen <= endBracket || endParen <= startParen)
            return new TextBlock { Text = "[无效图片]", FontSize = 13, Foreground = ThemeHelper.GetGrayBrush() };

        var url = line.Substring(startParen + 1, endParen - startParen - 1);
        if (string.IsNullOrWhiteSpace(url))
            return new TextBlock { Text = "[无效图片]", FontSize = 13, Foreground = ThemeHelper.GetGrayBrush() };

        var image = new Image
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0)
        };

        var errorPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
            Margin = new Thickness(16)
        };

        var errorText = new TextBlock
        {
            Text = "加载失败，轻触屏幕",
            FontSize = 13,
            Foreground = Brushes.Red,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        errorPanel.Children.Add(errorText);

        var errorDetailText = new TextBlock
        {
            Text = "",
            FontSize = 11,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300
        };
        errorPanel.Children.Add(errorDetailText);

        var retryButton = new Button
        {
            Content = "重新刷新",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(12, 6),
            FontSize = 12
        };
        errorPanel.Children.Add(retryButton);

        // 每张图片独立的加载占位符：该图缓冲完成前悬浮其上居中显示
        var loadingIndicator = new ExpressiveLoadingIndicator
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetAccentBrush()
        };

        var grid = new Grid();
        grid.Children.Add(image);
        grid.Children.Add(loadingIndicator);
        grid.Children.Add(errorPanel);

        // 容器：挂 Opacity / RenderTransform 两条 Transition，供加载成功后的入场动画插值。
        // RenderTransform 用 TransformOperations（配合 TransformOperationsTransition），
        // 不能在 TranslateTransform 对象上跑 Animation（Avalonia 11 运行时强转 Visual 会崩）。
        var container = new Border
        {
            Child = grid,
            HorizontalAlignment = HorizontalAlignment.Center,
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Background = ThemeHelper.GetProgressRingBackgroundBrush(),
            RenderTransform = TransformOperations.Parse("translateY(0px)"),
            Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = TimeSpan.FromSeconds(0.45),
                    Easing = new CubicEaseOut()
                },
                new TransformOperationsTransition
                {
                    Property = Visual.RenderTransformProperty,
                    Duration = TimeSpan.FromSeconds(0.5),
                    Easing = new CubicEaseOut()
                }
            }
        };
        _imageCardBorders?.Add(container);

        // 图片宽度跟随父级（内容区）宽度动态缩放，窗口缩放时自动调整；
        // 使用 MaxWidth 而非强制 Width：图片最多占内容区 80%，不会超出展示框，
        // 也不会把小图强制拉伸到占满整个页面宽度。
        ResponsiveImageHelper.MakeWidthFollowAncestor(container, 0.8, applyAsMax: true);

        async void RetryHandler(object? sender, RoutedEventArgs args)
        {
            retryButton.IsEnabled = false;
            retryButton.Content = "加载中...";
            errorPanel.IsVisible = false;
            errorDetailText.Text = "";
            await LoadRemoteImageWithRetry(url, image, errorPanel, errorDetailText, retryButton, loadingIndicator);
        }

        retryButton.Click += RetryHandler;

        _imageLoadInfos?.Add((url, image, errorPanel, errorDetailText, retryButton, loadingIndicator));

        LoadRemoteImageWithRetry(url, image, errorPanel, errorDetailText, retryButton, loadingIndicator);

        return container;
    }

    /// <summary>共享 HttpClient：避免每张图各建一个（原实现 20+ 张图会创建 20+ 个 HttpClient/连接池）。</summary>
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private async Task LoadRemoteImageWithRetry(string url, Image imageControl, StackPanel errorPanel, TextBlock errorDetailText, Button retryButton, ExpressiveLoadingIndicator loadingIndicator)
    {
        // 该图缓冲期间显示占位符；缓冲结束（成功/失败）后隐藏
        loadingIndicator.IsActive = true;
        loadingIndicator.IsVisible = true;

        try
        {
            var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
            var cacheDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Images");
            var cachePath = Path.Combine(cacheDir, fileName);

            // 【不阻塞 UI】缓存探测（磁盘 I/O）与位图解码放到后台线程：
            //   本方法在构建页面时会被 20+ 张图各调用一次，命中缓存时原实现会在 UI 线程同步
            //   完成 File.Exists + Bitmap 解码，直接造成打开彩蛋页/女装页明显卡顿。
            var cachedBitmap = await Task.Run(() =>
            {
                if (!File.Exists(cachePath)) return null;
                try { return new Avalonia.Media.Imaging.Bitmap(cachePath); }
                catch { return null; }
            });
            if (cachedBitmap != null)
            {
                imageControl.Source = cachedBitmap;
                PlayImageEntranceAnimation(imageControl);
                return;
            }

            using var response = await SharedHttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();

            // 解码同样放后台（首次下载的图片体积不小，UI 线程解码会卡顿）
            var bitmap2 = await Task.Run(() =>
            {
                using var ms = new MemoryStream(bytes);
                return new Avalonia.Media.Imaging.Bitmap(ms);
            });
            imageControl.Source = bitmap2;
            PlayImageEntranceAnimation(imageControl);

            await Task.Run(() =>
            {
                Directory.CreateDirectory(cacheDir);
                var tempPath = Path.Combine(cacheDir, Guid.NewGuid().ToString() + ".tmp");
                File.WriteAllBytes(tempPath, bytes);
                if (File.Exists(cachePath))
                    File.Delete(cachePath);
                File.Move(tempPath, cachePath);
            });
        }
        catch (Exception ex)
        {
            imageControl.Source = null;
            ResetImageEntranceState(imageControl);
            errorDetailText.Text = $"URL: {url}\n错误: {ex.GetType().Name}: {ex.Message}";
            errorPanel.IsVisible = true;
            retryButton.IsEnabled = true;
            retryButton.Content = "重新刷新";
        }
        finally
        {
            loadingIndicator.IsActive = false;
            loadingIndicator.IsVisible = false;
        }
    }

    /// <summary>宿主动画等级是否为“华丽”（IThemeService.AnimationLevel &gt;= 2；0=关闭，1=标准）。</summary>
    private static bool IsGorgeousAnimationEnabled()
    {
        // IThemeService.AnimationLevel 在两个宿主版本中均为静态属性（0=关闭，1=标准，>=2=华丽）
        try
        {
            return IThemeService.AnimationLevel >= 2;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 图片加载成功后播放一次入场动画：整体卡片淡入 + 轻微上滑（约 0.5s，CubicEaseOut）。
    /// 实现走容器的 <see cref="Transitions"/>（DoubleTransition + TransformOperationsTransition）：
    /// 先写初始值（透明 / translateY(12px)），下一布局帧再写终值，由 Transition 插值。
    /// 不能用 <c>Animation.RunAsync(TranslateTransform)</c>——Avalonia 11 内部会把动画目标强转为
    /// Visual，对 Transform 对象运行时直接抛 InvalidCastException（编译期无法发现）。
    /// 非华丽档、或回调时页面已离开视觉树时直接落终值。
    /// </summary>
    private static void PlayImageEntranceAnimation(Image imageControl)
    {
        if (imageControl.Parent is not Grid grid || grid.Parent is not Border container)
        {
            return;
        }

        if (!IsGorgeousAnimationEnabled() || !container.IsAttachedToVisualTree())
        {
            SetImageCardFinalState(container);
            return;
        }

        // 初始帧：透明 + 下移 12px；下一布局帧（Background 优先级，渲染前）写终值，Transition 接管插值
        container.Opacity = 0;
        container.RenderTransform = TransformOperations.Parse("translateY(12px)");
        Dispatcher.UIThread.Post(() =>
        {
            if (container.IsAttachedToVisualTree())
            {
                container.Opacity = 1;
                container.RenderTransform = TransformOperations.Parse("translateY(0px)");
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>把图片卡片落到可见终值（加载失败 / 非华丽档 / 页面重新进入时使用，不播放动画）。</summary>
    private static void ResetImageEntranceState(Image imageControl)
    {
        if (imageControl.Parent is not Grid grid || grid.Parent is not Border container)
        {
            return;
        }
        SetImageCardFinalState(container);
    }

    private static void SetImageCardFinalState(Border container)
    {
        container.Opacity = 1;
        container.RenderTransform = TransformOperations.Parse("translateY(0px)");
    }

    private async void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        
        button.IsEnabled = false;
        button.Content = "刷新中...";

        try
        {
            if (_imageLoadInfos == null || _imageLoadInfos.Count == 0)
                return;

            var cacheDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Images");
            var tasks = new List<Task>();

            foreach (var info in _imageLoadInfos)
            {
                var fileName = Path.GetFileName(new Uri(info.url).AbsolutePath);
                var cachePath = Path.Combine(cacheDir, fileName);

                if (File.Exists(cachePath))
                {
                    try
                    {
                        File.Delete(cachePath);
                    }
                    catch
                    {
                    }
                }

                info.image.Source = null;
                info.errorPanel.IsVisible = false;
                info.errorDetailText.Text = "";
                info.retryButton.IsEnabled = false;
                info.retryButton.Content = "加载中...";

                tasks.Add(LoadRemoteImageWithRetry(info.url, info.image, info.errorPanel, info.errorDetailText, info.retryButton, info.loadingIndicator));
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            button.IsEnabled = true;
            button.Content = "刷新";
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        // 监听键盘事件（使用Tunnel策略确保能接收到事件）
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        // 注意：不移除KeyDown和Loaded订阅，以便切换Tab回来时仍能正常工作
        // 清理返回顶部按钮（移除OverlayLayer上的按钮和事件订阅）
        CleanupBackToTopButton();
        // 停止 FemboyTest 状态监视
        if (_femboyTestWatchTimer != null)
        {
            _femboyTestWatchTimer.Stop();
            _femboyTestWatchTimer = null;
        }
    }

    /// <summary>
    /// 动态显示/取消 FemboyTest 错误警告栏。
    /// 与彩蛋锁共用同一条互斥判定 <see cref="CrossPluginHelper.IsFemboyTestEnabled"/>——
    /// 该判定已包含"标识符文件（已安装且已启用）"这一路，因此这里不得再单独收窄成"仅运行中"：
    /// 两处判定一旦不一致，就会出现"锁已被绕过但警告栏仍显示"（或反之）的漏洞。
    /// </summary>
    private void UpdateFemboyTestWarningBar()
    {
        if (_femboyTestWarningBar == null)
            return;

        // 与彩蛋锁共用同一条互斥判定：本判定含"已安装且已启用"这一路，
        // 两处必须完全一致，否则会出现"锁已被绕过但警告栏仍显示"（或反之）的漏洞。
        // 外壳 axaml 中专设 FemboyWarningHost 宿主：启用时放入警告栏（位于 Markdown 内容之前），
        // 禁用时清空（ContentControl 内容为 null 时不占位）。
        var enabled = CrossPluginHelper.IsFemboyTestEnabled();
        FemboyWarningHost.Content = enabled ? _femboyTestWarningBar : null;
    }

    private void CleanupBackToTopButton()
    {
        // 移除LayoutUpdated监听
        if (_overlayLayer != null)
        {
            _overlayLayer.LayoutUpdated -= OnLayoutUpdated;
        }

        if (_outerScrollViewer != null)
        {
            _outerScrollViewer.ScrollChanged -= OnScrollChanged;
            _outerScrollViewer = null;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            topLevel.SizeChanged -= OnTopLevelSizeChanged;
        }

        if (_backToTopButton != null && _overlayLayer != null)
        {
            _overlayLayer.Children.Remove(_backToTopButton);
            _backToTopButton.Click -= BackToTopButton_Click;
            _backToTopButton = null;
        }

        _overlayLayer = null;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void UpdateThemeColors()
    {
        if (_markdownSectionBorder != null)
            _markdownSectionBorder.Background = ThemeHelper.GetCardBackgroundBrush();

        if (_normalTextBlocks != null)
        {
            foreach (var tb in _normalTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetSubTextBrush();
            }
        }
        if (_boldTextBlocks != null)
        {
            foreach (var tb in _boldTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetOrangeBrush();
            }
        }
        if (_separatorBorders != null)
        {
            foreach (var border in _separatorBorders)
            {
                border.Background = ThemeHelper.GetGrayBrush();
            }
        }
        if (_headerSeparatorBorder != null)
        {
            _headerSeparatorBorder.Background = ThemeHelper.GetSeparatorBrush();
        }
        if (_imageCardBorders != null)
        {
            // 图片卡片衬底随深浅主题刷新（与外层卡片背景保持层级差异）
            var imageCardBrush = ThemeHelper.GetProgressRingBackgroundBrush();
            foreach (var card in _imageCardBorders)
            {
                card.Background = imageCardBrush;
            }
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 先清理旧的按钮（防止切换Tab回来时重复创建）
        CleanupBackToTopButton();

        // 页面重新进入时，把已加载完成的图片卡片落回可见终值：
        // 防止图片成功回调刚把卡片置为透明起播、页面就被切走导致动画取消而停在透明态
        if (_imageLoadInfos != null)
        {
            foreach (var info in _imageLoadInfos)
            {
                if (info.image.Source != null && info.image.Parent is Grid g && g.Parent is Border card)
                {
                    SetImageCardFinalState(card);
                }
            }
        }

        // 重启 FemboyTest 状态监视（切换 Tab 后 timer 已在 Detached 中停止）
        if (_femboyTestWatchTimer == null)
        {
            _femboyTestWatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _femboyTestWatchTimer.Tick += (s, e2) => UpdateFemboyTestWarningBar();
        }
        _femboyTestWatchTimer.Start();
        UpdateFemboyTestWarningBar();

        // 延迟到布局完成后再初始化，确保OverlayLayer的AvailableSize已正确计算
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                InitializeBackToTopButton();
            }
            catch
            {
                // 忽略初始化错误
            }
        });
    }

    private void InitializeBackToTopButton()
    {
        // 获取OverlayLayer用于放置固定按钮
        _overlayLayer = OverlayLayer.GetOverlayLayer(this);
        if (_overlayLayer == null)
            return;

        // 查找外层的ScrollViewer（实际滚动的那个）
        _outerScrollViewer = this.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();

        // 创建"返回顶部"按钮
        _backToTopButton = new Button
        {
            Content = "返回顶部",
            Padding = new Thickness(24, 10),
            FontSize = 14,
            IsVisible = false,
            Background = GetAccentBrush(),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(20),
            MinWidth = 100
        };
        _backToTopButton.Click += BackToTopButton_Click;

        _overlayLayer.Children.Add(_backToTopButton);

        // 监听LayoutUpdated来更新按钮位置（布局完成时触发）
        _overlayLayer.LayoutUpdated += OnLayoutUpdated;

        // 初始定位按钮
        UpdateBackToTopButtonPosition();

        // 监听外层ScrollViewer的滚动事件
        if (_outerScrollViewer != null)
        {
            _outerScrollViewer.ScrollChanged += OnScrollChanged;
            // 初始检查滚动位置（可能已经滚动过了）
            UpdateBackToTopButtonVisibility();
        }

        // 监听窗口大小变化
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            topLevel.SizeChanged += OnTopLevelSizeChanged;
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        UpdateBackToTopButtonPosition();
    }

    private void OnTopLevelSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateBackToTopButtonPosition();
    }

    private void UpdateBackToTopButtonPosition()
    {
        if (_backToTopButton == null || _overlayLayer == null)
            return;

        var availableSize = _overlayLayer.AvailableSize;
        // 如果AvailableSize还没准备好（布局未完成），跳过本次定位
        if (availableSize.Width <= 0 || availableSize.Height <= 0)
            return;

        // 测量按钮所需大小
        _backToTopButton.Measure(availableSize);
        var buttonWidth = _backToTopButton.DesiredSize.Width;
        var buttonHeight = _backToTopButton.DesiredSize.Height;

        // 水平居中，距底部24px
        var x = (availableSize.Width - buttonWidth) / 2;
        var y = availableSize.Height - buttonHeight - 24;

        Canvas.SetLeft(_backToTopButton, x);
        Canvas.SetTop(_backToTopButton, y);

        // 安排按钮的最终位置和大小
        _backToTopButton.Arrange(new Rect(x, y, buttonWidth, buttonHeight));
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateBackToTopButtonVisibility();
    }

    private void UpdateBackToTopButtonVisibility()
    {
        // 滚动超过250px时显示按钮，否则隐藏
        if (_backToTopButton != null && _outerScrollViewer != null)
        {
            _backToTopButton.IsVisible = _outerScrollViewer.Offset.Y >= 250;
        }
    }

    private void BackToTopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_outerScrollViewer != null)
        {
            _outerScrollViewer.Offset = new Vector(0, 0);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // 按下 Home 键返回顶部，无论滚动到何处都生效
        if (e.Key == Key.Home)
        {
            // 如果外层ScrollViewer未初始化，尝试重新查找
            _outerScrollViewer ??= this.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();
            if (_outerScrollViewer != null)
            {
                _outerScrollViewer.Offset = new Vector(0, 0);
                e.Handled = true;
            }
        }
    }
}



