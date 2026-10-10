using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Plugin;
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

    /// <summary>
    /// 页面当前在视觉树期间的加载取消令牌：离树（OnDetached）时 Cancel，
    /// 取消所有在途下载/解码并阻止其回调写回控件——否则 async 状态机会把整页控件树
    /// （含 24 张位图）一直钉到下载结束；重新进入时换新令牌并重载被释放的图片。
    /// </summary>
    private CancellationTokenSource? _imageLoadCts;

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

    /// <summary>返回顶部平滑滚动计时器（60fps 插值）。</summary>
    private DispatcherTimer? _scrollToTopTimer;
    private Vector _scrollToTopStartOffset;
    private double _scrollToTopElapsedMs;
    private bool _isScrollToTopAnimating;

    /// <summary>回顶动画总时长 0.7s；只做缓出（起步即快速移动，结尾平稳减速）。</summary>
    private static readonly TimeSpan ScrollToTopDuration = TimeSpan.FromSeconds(0.7);
    private const double ScrollToTopFrameMs = 16.0;

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
    /// axaml 只承载静态外壳（滚动容器 / 主面板 / 标题 / 两个警告栏宿主 / Markdown 宿主 / 底部留白）；
    /// 条件警告栏（FA2/FA3 的 InfoBar 类型名不同，须经兼容 Helper 创建）与 Markdown 图片区在此动态装配。
    /// </summary>
    private void WireUI()
    {
        // 首批图片加载使用初始令牌（页面离树后失效，OnLoaded 重新进入时换新令牌）
        _imageLoadCts = new CancellationTokenSource();

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
    /// 进程级的女装图片洗牌顺序：首次访问（ClassIsland 初始化时由
    /// <see cref="PrimeWomenswearImageOrder"/> 预热）执行一次 Fisher-Yates 洗牌，
    /// 之后本次宿主运行期内所有女装页实例共用；宿主重启后进程重建、顺序重新随机。
    /// Lazy 保证多线程首次访问（启动线程/UI 线程）安全且只洗一次。
    /// </summary>
    private static readonly Lazy<List<string>> ShuffledWomenswearImageLines = new(() =>
    {
        var imageLines = MarkdownContent
            .Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l) && l.TrimStart().StartsWith("!["))
            .ToList();
        ShuffleInPlace(imageLines);
        return imageLines;
    });

    /// <summary>在 ClassIsland 初始化时触发一次洗牌，使图片顺序在宿主启动阶段即确定（而非首次打开页面时）。</summary>
    internal static void PrimeWomenswearImageOrder() => _ = ShuffledWomenswearImageLines.Value;

    /// <summary>Fisher-Yates 原地洗牌（线程安全随机源 Random.Shared，.NET 8/10 均可用）。</summary>
    private static void ShuffleInPlace<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

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

        // 女装图片顺序在 ClassIsland 初始化时（Plugin.Initialize 预热）已随机排列，
        // 整个宿主运行期内所有页面实例共享同一份顺序，重启宿主才重新随机；
        // “刷新”按钮只重载图片、不重排。
        var lines = markdownText.Split('\n');
        var shuffledImageLines = ShuffledWomenswearImageLines.Value;
        var shuffledImageIndex = 0;
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
                // Markdown 图片 ![alt](url)：标题出现后进入图片列表面板；行内容取初始化时洗牌后的顺序
                var imageLine = shuffledImageIndex < shuffledImageLines.Count
                    ? shuffledImageLines[shuffledImageIndex++]
                    : line;
                var imageControl = CreateMarkdownImage(imageLine);
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
        // 滚动图片列表用双线性插值：降采样图在卡片里仍有缩放，高质量重采样在滚动逐帧合成时开销大；
        // 双线性对照片观感几乎无差异，却显著降低每帧光栅化成本
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.LowQuality);

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

        // 容器默认【不挂】RenderTransform / Transitions：带变换的 Visual 会被 Skia 提升为独立合成层，
        // 24 张图常驻变换层会让滚动到图片间隔处时同时合成两层大纹理而掉帧。
        // 入场动画只在华丽档播放的那 0.5s 内临时挂载，播完立即移除（见 PlayImageEntranceAnimation）。
        var container = new Border
        {
            Child = grid,
            HorizontalAlignment = HorizontalAlignment.Center,
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Background = ThemeHelper.GetProgressRingBackgroundBrush()
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
            await LoadRemoteImageWithRetry(url, image, errorPanel, errorDetailText, retryButton, loadingIndicator, GetCurrentLoadToken());
        }

        retryButton.Click += RetryHandler;

        _imageLoadInfos?.Add((url, image, errorPanel, errorDetailText, retryButton, loadingIndicator));

        LoadRemoteImageWithRetry(url, image, errorPanel, errorDetailText, retryButton, loadingIndicator, GetCurrentLoadToken());

        return container;
    }

    /// <summary>共享 HttpClient：避免每张图各建一个（原实现 20+ 张图会创建 20+ 个 HttpClient/连接池）。</summary>
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// 图片解码后的目标宽度（CSS px）。显示区最多占内容区 80%，1280 已覆盖高 DPI 缩放；
    /// 相机原图常见 4000~6000px，按面积算可把单张解码内存降低一个数量级。
    /// </summary>
    private const int DecodedImageTargetWidth = 1280;

    /// <summary>
    /// 同时进行图片解码的数量上限。页面构造时 20+ 张图并发加载，而 Avalonia 的 DecodeToWidth
    /// 内部仍会短暂持有全尺寸位图，不限并发时瞬时内存峰值可超过 1GiB（女装页曾触发宿主内存看门狗）。
    /// </summary>
    private static readonly SemaphoreSlim ImageDecodeGate = new(2, 2);

    /// <summary>
    /// 加载单张图片（带页面级取消令牌）。页面离树时令牌取消：HTTP/IO 抛出 OCE 后静默返回、
    /// 已解码出的位图立即 Dispose，绝不写回控件（否则 async 闭包会把整页控件树钉在内存里）。
    /// </summary>
    private async Task LoadRemoteImageWithRetry(string url, Image imageControl, StackPanel errorPanel, TextBlock errorDetailText, Button retryButton, ExpressiveLoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        // 该图缓冲期间显示占位符；缓冲结束（成功/失败）后隐藏
        loadingIndicator.IsActive = true;
        loadingIndicator.IsVisible = true;

        try
        {
            // 跨插件冲突拦截：宿主中存在入口 DLL 实际加载在本进程的“女装/男娘”类插件时，
            // 所有女装图片一律不读取本地缓存、也不发起网络请求，直接落到与加载失败相同的错误面板
            //（“加载失败，轻触屏幕”），详情列出需禁用并重启的插件。首次加载 / 重试 / 刷新 / 离树重载
            // 全部经过本方法，故在此一处拦截即可覆盖所有路径。宿主禁用插件只写 .disabled 标记、
            // 程序集仍驻留内存，因此禁用后必须重启宿主才会解除拦截，提示用户“禁用并重启”。
            // 本分支在 try 内 return，finally 仍会关闭加载占位符。
            var blockingPlugins = CrossPluginHelper.GetRunningFemboyLikePlugins();
            if (blockingPlugins.Count > 0)
            {
                ReplaceImageSource(imageControl, null);
                ResetImageEntranceState(imageControl);
                errorDetailText.Text = BuildPluginsBlockedMessage(blockingPlugins);
                errorPanel.IsVisible = true;
                retryButton.IsEnabled = true;
                retryButton.Content = "重新刷新";
                return;
            }

            var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
            var cacheDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Images");
            var cachePath = Path.Combine(cacheDir, fileName);

            cancellationToken.ThrowIfCancellationRequested();

            // 【不阻塞 UI】缓存探测（磁盘 I/O）放到后台线程
            Bitmap? decoded = null;
            var cacheExists = await Task.Run(() => File.Exists(cachePath), cancellationToken);
            if (cacheExists)
            {
                // 闸门只限解码（DecodeToWidth 内部会短暂持有全尺寸位图）；缓存的文件读取也在闸门内，
                // 因为它与解码共用同一个流且耗时主要在解码，网络下载并不在此闸门内
                await ImageDecodeGate.WaitAsync(cancellationToken);
                try
                {
                    decoded = await Task.Run(() =>
                    {
                        try
                        {
                            using var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                            return DecodeDownscaled(fs);
                        }
                        catch
                        {
                            return null;
                        }
                    }, cancellationToken);
                    // 等待解码期间页面可能已离树：立刻释放这张图，不写回控件
                    if (cancellationToken.IsCancellationRequested)
                    {
                        decoded?.Dispose();
                        return;
                    }
                }
                finally
                {
                    ImageDecodeGate.Release();
                }
            }

            if (decoded == null)
            {
                // 网络下载不受解码闸门限制：20+ 张图可并行下载（JPG 缓冲仅数 MB），
                // 只有下面的解码步骤排队进入闸门，避免“两路下载”串行拖慢全部图片
                using var response = await SharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var memoryBuffer = new MemoryStream();
                await downloadStream.CopyToAsync(memoryBuffer, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                await ImageDecodeGate.WaitAsync(cancellationToken);
                try
                {
                    // 同一缓冲先后用于解码与写缓存，避免 ReadAsByteArray/ToArray 的额外整份拷贝
                    memoryBuffer.Position = 0;
                    decoded = await Task.Run(() => DecodeDownscaled(memoryBuffer), CancellationToken.None);
                }
                finally
                {
                    ImageDecodeGate.Release();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    decoded?.Dispose();
                    return;
                }

                if (decoded != null)
                {
                    memoryBuffer.Position = 0;
                    await Task.Run(async () =>
                    {
                        Directory.CreateDirectory(cacheDir);
                        var tempPath = Path.Combine(cacheDir, Guid.NewGuid() + ".tmp");
                        try
                        {
                            await using var tempFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                            await memoryBuffer.CopyToAsync(tempFile, CancellationToken.None);
                            if (File.Exists(cachePath))
                            {
                                File.Delete(cachePath);
                            }
                            File.Move(tempPath, cachePath);
                        }
                        catch
                        {
                            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                        }
                    }, CancellationToken.None);
                }
            }

            if (decoded == null)
            {
                throw new InvalidOperationException("图片解码失败（返回空位图）。");
            }

            ReplaceImageSource(imageControl, decoded);
            PlayImageEntranceAnimation(imageControl);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 页面已离树：静默退出，不更新任何 UI、不显示错误面板（避免把已释放的页面重新钉回内存）
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            ReplaceImageSource(imageControl, null);
            ResetImageEntranceState(imageControl);
            errorDetailText.Text = $"URL: {url}\n错误: {ex.GetType().Name}: {ex.Message}";
            errorPanel.IsVisible = true;
            retryButton.IsEnabled = true;
            retryButton.Content = "重新刷新";
        }
        finally
        {
            // 页面已离树时不要碰控件（旧加载的 finally 可能晚于新一轮加载执行，会误关新占位符）
            if (!cancellationToken.IsCancellationRequested)
            {
                loadingIndicator.IsActive = false;
                loadingIndicator.IsVisible = false;
            }
        }
    }

    /// <summary>
    /// 按 <see cref="DecodedImageTargetWidth"/> 等比降采样解码位图（Avalonia 11/12 均提供的
    /// Bitmap.DecodeToWidth，跨版本兼容）。解码后的位图由 Image.Source 持有，替换时需显式 Dispose。
    /// </summary>
    private static Bitmap DecodeDownscaled(Stream stream)
    {
        try
        {
            return Bitmap.DecodeToWidth(stream, DecodedImageTargetWidth, BitmapInterpolationMode.HighQuality);
        }
        catch
        {
            // 个别损坏/不支持的编码下降采样解码可能失败，回退普通解码（仍能显示，只是内存占用大）
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            return new Bitmap(stream);
        }
    }

    /// <summary>获取当前页面存活期的加载令牌（令牌理论上总在 WireUI/OnLoaded 中就绪，给 None 兜底）。</summary>
    private CancellationToken GetCurrentLoadToken() => _imageLoadCts?.Token ?? CancellationToken.None;

    /// <summary>
    /// 页面离树时释放全部已解码位图（每张降采样后约 6~9MB × 24 张 ≈ 200MB，是每次进入累积增长的主因）。
    /// 重新进入时由 <see cref="ReloadReleasedImagesIfNeeded"/> 从本地缓存按需重新解码（限并发，约 1 秒内完成）。
    /// </summary>
    private void ReleaseAllLoadedBitmaps()
    {
        if (_imageLoadInfos == null)
        {
            return;
        }

        foreach (var info in _imageLoadInfos)
        {
            if (info.image.Source is Bitmap bitmap)
            {
                info.image.Source = null;
                bitmap.Dispose();
            }
            info.loadingIndicator.IsActive = false;
            info.loadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// 页面重新进入视觉树时，重载被 <see cref="ReleaseAllLoadedBitmaps"/> 释放的图片：
    /// Source 为空、未显示错误面板、且当前没有在加载（占位符未激活）的条目才重载——
    /// 加载失败的条目保留错误面板等用户手动重试，在途中的条目不重复发起。
    /// </summary>
    private void ReloadReleasedImagesIfNeeded()
    {
        if (_imageLoadInfos == null)
        {
            return;
        }

        var token = GetCurrentLoadToken();
        foreach (var info in _imageLoadInfos)
        {
            if (info.image.Source == null
                && !info.errorPanel.IsVisible
                && !info.loadingIndicator.IsActive)
            {
                info.loadingIndicator.IsActive = true;
                info.loadingIndicator.IsVisible = true;
                _ = LoadRemoteImageWithRetry(info.url, info.image, info.errorPanel, info.errorDetailText, info.retryButton, info.loadingIndicator, token);
            }
        }
    }

    /// <summary>
    /// 替换 Image 的位图并立即释放旧位图的非托管内存（Avalonia Bitmap 实现 IDisposable，
    /// 不 Dispose 只能等 finalizer，刷新/重试 20+ 张时会造成非托管内存堆积）。
    /// </summary>
    private static void ReplaceImageSource(Image imageControl, Bitmap? newSource)
    {
        if (imageControl.Source is Bitmap old && !ReferenceEquals(old, newSource))
        {
            imageControl.Source = null;
            old.Dispose();
        }
        imageControl.Source = newSource;
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
    /// 图片加载成功后播放一次入场动画（仅华丽档）：淡入 + 轻微上滑（0.45/0.5s，CubicEaseOut）。
    /// Transitions/RenderTransform 只在这 0.5s 内临时挂载——动画一结束就移除，让卡片回到
    /// “无变换、无过渡”的普通 Visual，滚动时不产生独立合成层（修复图片间隔处逐帧合成掉帧）。
    /// 非华丽档、或回调时页面已离开视觉树时直接落终值（同样不保留任何变换资源）。
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

        // 临时挂上过渡（播完由清理定时器移除）
        container.Transitions = new Transitions
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
        };

        // 初始帧：透明 + 下移 12px；下一布局帧（渲染前）写终值，Transition 接管插值
        container.Opacity = 0;
        container.RenderTransform = TransformOperations.Parse("translateY(12px)");
        Dispatcher.UIThread.Post(() =>
        {
            if (container.IsAttachedToVisualTree())
            {
                container.Opacity = 1;
                container.RenderTransform = TransformOperations.Parse("translateY(0px)");
                ScheduleImageCardLayerCleanup(container);
            }
            else
            {
                SetImageCardFinalState(container);
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// 入场过渡结束后（留 100ms 余量，确保最后一帧插值完成）移除 Transitions 与 RenderTransform，
    /// 释放该卡片占用的合成层。identity 变换 → null、Transitions → null 均不会产生视觉跳变。
    /// 每次播放都新建一个只关闭自己的定时器；页面离树时定时器回调因 IsAttachedToVisualTree 检查而安全空转。
    /// </summary>
    private static void ScheduleImageCardLayerCleanup(Border container)
    {
        var cleanupTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        cleanupTimer.Tick += (s, e) =>
        {
            cleanupTimer.Stop();
            if (!container.IsAttachedToVisualTree())
            {
                return;
            }
            container.RenderTransform = null;
            container.Transitions = null;
        };
        cleanupTimer.Start();
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

    /// <summary>可见终值：不透明、无变换、无过渡——滚动渲染的最轻形态。</summary>
    private static void SetImageCardFinalState(Border container)
    {
        container.Opacity = 1;
        container.RenderTransform = null;
        container.Transitions = null;
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

            // 存在运行中的冲突插件时本轮刷新只会逐图落到拦截错误面板（解除需重启宿主），
            // 不能删除本地缓存：否则用户在冲突期间点一次刷新，重启后就得重新联网下载全部 24 张图片。
            var blockedByPlugins = CrossPluginHelper.GetRunningFemboyLikePlugins();

            foreach (var info in _imageLoadInfos)
            {
                var fileName = Path.GetFileName(new Uri(info.url).AbsolutePath);
                var cachePath = Path.Combine(cacheDir, fileName);

                if (blockedByPlugins.Count == 0 && File.Exists(cachePath))
                {
                    try
                    {
                        File.Delete(cachePath);
                    }
                    catch
                    {
                    }
                }

                // 立即释放旧位图的非托管内存，再重载（否则旧图要等 GC finalizer 才归还，刷新高峰内存翻倍）
                if (info.image.Source is Bitmap oldBitmap)
                {
                    info.image.Source = null;
                    oldBitmap.Dispose();
                }
                info.errorPanel.IsVisible = false;
                info.errorDetailText.Text = "";
                info.retryButton.IsEnabled = false;
                info.retryButton.Content = "加载中...";

                tasks.Add(LoadRemoteImageWithRetry(info.url, info.image, info.errorPanel, info.errorDetailText, info.retryButton, info.loadingIndicator, GetCurrentLoadToken()));
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            button.IsEnabled = true;
            button.Content = "刷新";
        }
    }

    /// <summary>
    /// 构造“被冲突插件拦截”时错误面板的详情文案：逐条列出需要禁用并重启的插件（显示名 + 标识符）。
    /// 宿主禁用插件后程序集仍驻留内存，必须重启宿主才会解除拦截。展示在“加载失败，轻触屏幕”标题下方。
    /// </summary>
    private static string BuildPluginsBlockedMessage(IReadOnlyList<PluginInfo> blockingPlugins)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("检测到与本页面内容冲突的插件，请禁用以下插件并重启：");
        foreach (var plugin in blockingPlugins)
        {
            builder.AppendLine($"- {plugin.Manifest.Name}（{plugin.Manifest.Id}）");
        }
        return builder.ToString().TrimEnd('\r', '\n');
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

        // 取消所有在途图片下载/解码，阻止其回调写回已离树的控件（否则 async 闭包会钉住整页）
        _imageLoadCts?.Cancel();

        // 立即归还 24 张位图的非托管内存（~200MB），不等待 GC/finalizer——
        // 这是“每次进入女装页内存增长约 200MB”的直接修复点
        ReleaseAllLoadedBitmaps();

        // 注意：不移除KeyDown和Loaded订阅，以便切换Tab回来时仍能正常工作
        // 清理返回顶部按钮（移除OverlayLayer上的按钮和事件订阅）
        CleanupBackToTopButton();
    }

    private void CleanupBackToTopButton()
    {
        // 移除LayoutUpdated监听
        if (_overlayLayer != null)
        {
            _overlayLayer.LayoutUpdated -= OnLayoutUpdated;
        }

        StopScrollToTopAnimation();

        if (_outerScrollViewer != null)
        {
            _outerScrollViewer.ScrollChanged -= OnScrollChanged;
            _outerScrollViewer.PointerWheelChanged -= OnScrollViewerPointerWheelChanged;
            _outerScrollViewer.PointerPressed -= OnScrollViewerPointerPressed;
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

        // 离树期间旧令牌已取消；重新进入时换新令牌，并把已释放的位图从本地缓存重新解码
        if (_imageLoadCts == null || _imageLoadCts.IsCancellationRequested)
        {
            _imageLoadCts?.Dispose();
            _imageLoadCts = new CancellationTokenSource();
            ReloadReleasedImagesIfNeeded();
        }

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
            // 回顶动画进行中用户一旦手动滚动/按下即打断动画，交回滚动控制权
            _outerScrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;
            _outerScrollViewer.PointerPressed += OnScrollViewerPointerPressed;
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

    /// <summary>
    /// 平滑滚动回顶部：0.7s、三次缓出（ease-out，起步即快速移动、结尾平稳减速，不做缓入）。
    /// 用 60fps DispatcherTimer 手动插值 ScrollViewer.Offset——对 Offset(Vector) 直接跑 Animation
    /// 在 Avalonia 11/12 间行为不一致，手动插值两版本完全可控。重复触发时以当前位置为起点重启，不跳变。
    /// </summary>
    private void ScrollToTopAnimated()
    {
        if (_outerScrollViewer == null)
        {
            return;
        }

        // 已在顶部无需动画；重复点击先停掉旧计时（保留当前偏移作为新起点，下面重新读取）
        StopScrollToTopAnimation();

        var startOffset = _outerScrollViewer.Offset;
        if (startOffset.Y <= 0.01)
        {
            _outerScrollViewer.Offset = new Vector(0, 0);
            return;
        }

        _scrollToTopStartOffset = startOffset;
        _scrollToTopElapsedMs = 0;
        _isScrollToTopAnimating = true;

        _scrollToTopTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(ScrollToTopFrameMs)
        };
        _scrollToTopTimer.Tick += OnScrollToTopTick;
        _scrollToTopTimer.Start();
    }

    private void OnScrollToTopTick(object? sender, EventArgs e)
    {
        if (!_isScrollToTopAnimating || _outerScrollViewer == null)
        {
            StopScrollToTopAnimation();
            return;
        }

        _scrollToTopElapsedMs += ScrollToTopFrameMs;
        var progress = Math.Clamp(_scrollToTopElapsedMs / ScrollToTopDuration.TotalMilliseconds, 0.0, 1.0);

        // 三次缓出 f(t)=1-(1-t)^3：无缓入，末段速度趋零，平稳结束
        var eased = 1.0 - Math.Pow(1.0 - progress, 3);
        var y = _scrollToTopStartOffset.Y * (1.0 - eased);
        _outerScrollViewer.Offset = new Vector(0, y);

        if (progress >= 1.0)
        {
            _outerScrollViewer.Offset = new Vector(0, 0);
            StopScrollToTopAnimation();
        }
    }

    /// <summary>停止回顶动画（保留当前滚动位置，不跳回起点/顶部）。</summary>
    private void StopScrollToTopAnimation()
    {
        if (_scrollToTopTimer != null)
        {
            _scrollToTopTimer.Stop();
            _scrollToTopTimer.Tick -= OnScrollToTopTick;
            _scrollToTopTimer = null;
        }
        _isScrollToTopAnimating = false;
    }

    /// <summary>回顶动画期间用户滚动滚轮：立即交回滚动控制权，停止自动动画。</summary>
    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_isScrollToTopAnimating)
        {
            StopScrollToTopAnimation();
        }
    }

    /// <summary>回顶动画期间用户按下（拖动滚动条 / 触摸滑动 / 点内容）：立即停止自动动画。</summary>
    private void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isScrollToTopAnimating)
        {
            StopScrollToTopAnimation();
        }
    }

    private void BackToTopButton_Click(object? sender, RoutedEventArgs e)
    {
        ScrollToTopAnimated();
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
                ScrollToTopAnimated();
                e.Handled = true;
            }
            return;
        }

        // 回顶动画进行中，用户按其它滚动导航键（方向键 / PageUp / PageDown / End / 空格）即视为手动干预，打断动画
        if (_isScrollToTopAnimating && e.Key is
            (Key.Up or Key.Down or Key.Left or Key.Right or Key.PageUp or Key.PageDown or Key.End or Key.Space))
        {
            StopScrollToTopAnimation();
        }
    }
}



