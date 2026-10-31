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
using Avalonia.Platform.Storage;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using SkiaSharp;
using System.Text;
// Toast 的 InfoBar 严重级别枚举：FA2 (net8) 叫 InfoBarSeverity，FA3 (net10) 改名 FAInfoBarSeverity，
// 用别名统一，业务代码统一用 FAInfoBarSeverity
#if NET10_0_OR_GREATER
using FAInfoBarSeverity = FluentAvalonia.UI.Controls.FAInfoBarSeverity;
#else
using FAInfoBarSeverity = FluentAvalonia.UI.Controls.InfoBarSeverity;
#endif
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
    private List<ImageSlot>? _imageSlots;
    private List<Border>? _imageCardBorders;
    private Border? _headerSeparatorBorder;
    private Button? _backToTopButton;
    private OverlayLayer? _overlayLayer;
    private ScrollViewer? _outerScrollViewer;

    /// <summary>
    /// 视口虚拟化加载的滚动根：实际发生滚动的 ScrollViewer（页面自带 MainScrollViewer，
    /// 或页面被外层 ScrollViewer 承载滚动时的外层）。ScrollChanged / SizeChanged 时重算
    /// 各图片卡片是否落在视口 ±<see cref="ViewportLoadMarginPx"/> 范围内，进入即加载、离开即卸载。
    /// </summary>
    private ScrollViewer? _viewportScrollViewer;
    private ScrollViewer? _viewportOuterScrollViewer;
    private bool _viewportUpdateScheduled;

    /// <summary>视口上下扩展的预加载余量（px）。</summary>
    private const double ViewportLoadMarginPx = 500;

    /// <summary>尺寸未知时的默认占位宽高比（高/宽，竖拍人像常见 3:4）。</summary>
    private const double DefaultImageAspect = 4.0 / 3.0;

    /// <summary>
    /// 进程级图片尺寸缓存：url → 高/宽比。图片真实尺寸从本地缓存文件头或 HTTP Range 头部
    /// 字节解析（JPEG SOF marker），用于在位图加载前铺设等比占位、避免加载完成后布局跳动；
    /// 跨页面实例共享，同一宿主运行期内每张图只解析一次。
    /// 【线程安全】后台线程（尺寸预扫描写入）与 UI 线程（占位/视口判定读取）并发访问，
    /// 必须用 ConcurrentDictionary——普通 Dictionary 并发读写会损坏内部状态直接崩溃宿主。
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, double> ImageAspectCache = new();

    /// <summary>尺寸头部解析的并发上限（文件读取 / 网络头部下载）。</summary>
    private static readonly SemaphoreSlim ImageSizeScanGate = new(4, 4);

    /// <summary>图片尺寸探测需要读取的头部字节数：相机 JPEG 的 EXIF 段（含缩略图）可达数十 KB，SOF 在其后。</summary>
    private const int ImageHeaderProbeBytes = 512 * 1024;

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
        Log($"===== session start, baseDir={AppContext.BaseDirectory}, avalonia={typeof(Image).Assembly.GetName().Version} =====");
        InitializeComponent();
        WireUI();
        IntroAnimationHelper.EnableSmoothScroll(this);
    }

    /// <summary>
    /// axaml 只承载静态外壳（滚动容器 / 主面板 / 标题 / 两个警告栏宿主 / Markdown 宿主 / 底部留白）；
    /// 条件警告栏（FA2/FA3 的 InfoBar 类型名不同，须经兼容 Helper 创建）与 Markdown 图片区在此动态装配。
    /// 图片版权注意事项覆盖层与「调试 → 女装」页共用同一控件与同一“已同意”标记——未同意时不装配图片区。
    /// </summary>
    private void WireUI()
    {
        // 首批图片加载使用初始令牌（页面离树后失效，OnLoaded 重新进入时换新令牌）
        _imageLoadCts = new CancellationTokenSource();

        NoticeOverlay.Bind(_pluginSettings);
        NoticeOverlay.Agreed += OnNoticeAgreed;

        if (NoticeOverlay.IsVisible)
        {
            // 未同意：不装配警告栏与图片区，也不触发布局预扫描 / 图片下载
            return;
        }

        BuildContent();
    }

    /// <summary>本页内容（警告栏 + 图片区）是否已装配：同意路径与加载路径可能先后触发，只装配一次。</summary>
    private bool _contentBuilt;

    /// <summary>用户同意图片版权注意事项后，再装配警告栏与图片区（与首次进入的装配路径一致）。</summary>
    private void OnNoticeAgreed(object? sender, EventArgs e)
    {
        BuildContent();
        InitializeLoadedContent();
    }

    private void BuildContent()
    {
        if (_contentBuilt)
        {
            return;
        }

        _contentBuilt = true;

        if (_pluginSettings?.EasterEggInfoAccepted != true)
        {
            var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "关闭女装彩蛋的方式：进入插件设置，划到最底部，关闭“女装”。彩蛋状态不会保存，重新进入设置页后需要重新触发。");
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

        // 图片尺寸预扫描：后台逐张解析宽高比（缓存文件头 / HTTP Range 头部字节），铺设等比占位。
        // 仅读头部字节（≤512KB），不下载/解码整图，与视口虚拟化加载互不阻塞
        _ = PrimeMissingImageSizesAsync();
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
        _imageSlots = new List<ImageSlot>();

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

        // 每张图片独立的加载占位符：该图缓冲完成前悬浮其上居中显示。
        // 覆盖控件默认的 168 固定宽度并改为拉伸：占位宽度在首次进入时先按兜底值铺设、布局就绪后再收敛，
        // 固定 168 宽在宽度尚未更新的窄卡片里会被 ClipToBounds 裁得只剩一个形状。
        var loadingIndicator = new ExpressiveLoadingIndicator
        {
            Width = double.NaN,
            HorizontalAlignment = HorizontalAlignment.Stretch,
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
            if (_imageSlots?.FirstOrDefault(s => ReferenceEquals(s.Image, image)) is { } retrySlot)
            {
                EnsureSlotLoaded(retrySlot);
            }
        }

        retryButton.Click += RetryHandler;

        var slot = new ImageSlot(url, image, container, errorPanel, errorDetailText, retryButton, loadingIndicator);
        _imageSlots?.Add(slot);

        // 视口虚拟化：构造时不立即加载，等滚动根就绪后由视口判定按需加载（进入视口 ±500px）。
        // 先铺设等比占位（尺寸来自进程级缓存 / 本地缓存文件头 / HTTP Range 头部解析），
        // 避免加载完成后布局跳动；占位期间 Container.Width/Height 由占位逻辑接管。
        ApplyPlaceholderSize(slot);
        container.SizeChanged += (_, _) =>
        {
            // 占位状态下高度必须跟随实际宽度维持等比：Width 属性可能被 MaxWidth 约束收窄，
            // 实际渲染宽度以 Bounds 为准（窗口缩放/MaxWidth 动态更新时由此收敛）。
            // 错误态不维持等比：高度交给错误面板内容自适应（否则错误信息多少都会落在占位高度里）。
            if (image.Source == null && !errorPanel.IsVisible && container.Bounds.Width > 1)
            {
                var aspect = GetImageAspect(url);
                var target = container.Bounds.Width * aspect;
                if (Math.Abs(container.Bounds.Height - target) > 1)
                {
                    container.Height = target;
                }
            }
        };

        // 右键菜单：按卡片状态分流——有缓存/位图 → 另存照片 JPG；无缓存无位图且错误态 → 另存错误日志；
        // 无缓存无位图且加载中 → 另存 ExpressiveLoadingIndicator.cs 源代码（见 AttachImageContextMenu）
        AttachImageContextMenu(container, url, image, errorPanel, loadingIndicator);

        return container;
    }

    /// <summary>
    /// 取女装图片的本地缓存路径（与 <see cref="LoadImageCoreAsync"/> 的缓存位置一致）。URL 非法时返回 null。
    /// </summary>
    private static string? GetImageCachePath(string url)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
            return string.IsNullOrWhiteSpace(fileName)
                ? null
                : Path.Combine(AppContext.BaseDirectory, "Assets", "Images", fileName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 右键“另存为”问题诊断日志（写插件目录 ContextMenuDebug.log）：跟踪右键事件是否到达
    /// 图片卡片、是否被上游标记 Handled、ContextMenu 是否尝试打开、保存流程各步骤。定位完成后可移除。
    /// </summary>
    private static readonly object DebugLogLock = new();

    private static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff} T{Environment.CurrentManagedThreadId}] {message}\r\n";
            lock (DebugLogLock)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "ContextMenuDebug.log"), line);
            }
        }
        catch
        {
            // 诊断日志失败不影响功能
        }
    }

    /// <summary>右键事件跟踪时的人类可读按键描述。</summary>
    private static string DescribePointerButton(PointerPoint point) =>
        point.Properties.PointerUpdateKind.ToString();

    /// <summary>
    /// 右键“另存为”的可执行动作：保存照片 / 保存加载占位符源码 / 保存错误日志。
    /// </summary>
    private enum ImageSaveAction { Image, LoadingSource, ErrorLog }

    /// <summary>
    /// 判定右键“另存为”应执行的动作。判据以「卡片当前显示的是什么」为准，而不是「本地有没有缓存」：
    /// 跨插件冲突拦截时本地可能仍残留旧缓存，此前「先看缓存再看状态」会把这份旧缓存当成可保存内容、
    /// 导出照片而不是错误信息。现在只要卡片没有显示位图且不在加载中，就按错误态导出错误日志
    ///（错误面板可见性与历史失败记录任一成立即可，避免面板可见性在重排间隙为 false 时误判）。
    /// 菜单标题与实际动作共用本判定。
    /// </summary>
    private static ImageSaveAction ResolveSaveAction(string url, Image imageControl, StackPanel errorPanel, ExpressiveLoadingIndicator loadingIndicator)
    {
        if (imageControl.Source is Bitmap)
        {
            return ImageSaveAction.Image;
        }

        if (errorPanel.IsVisible)
        {
            return ImageSaveAction.ErrorLog;
        }

        var cachePath = GetImageCachePath(url);
        var cacheExists = cachePath != null && File.Exists(cachePath);

        if (loadingIndicator.IsVisible)
        {
            // 正在加载：有本地缓存可直接复制原始 JPG，否则导出加载占位符源码
            return cacheExists ? ImageSaveAction.Image : ImageSaveAction.LoadingSource;
        }

        // 无位图且不在加载中：错误态，或视口外等待加载的纯占位卡片（此时缓存即其唯一可保存内容）
        if (GetImageLoadErrorCount(url) > 0)
        {
            return ImageSaveAction.ErrorLog;
        }

        return cacheExists ? ImageSaveAction.Image : ImageSaveAction.ErrorLog;
    }

    /// <summary>
    /// 为图片卡片挂右键菜单（“另存为…”），按卡片当前状态分三路：
    /// ① 有本地缓存或已显示位图 → 另存为照片 JPG（优先无损复制本地缓存的原始 JPG；缓存缺失时把
    ///    当前显示位图重编码保存）；
    /// ② 无缓存无位图且处于错误状态（错误面板可见）→ 另存为加载错误日志 TXT；
    /// ③ 无缓存无位图且正在加载中（加载占位符可见）→ 另存为 ExpressiveLoadingIndicator.cs 源代码。
    /// 菜单打开时按是否有任一可保存目标动态启用/禁用菜单项，避免对加载中的卡片右键“另存为”毫无反应。
    /// 注意：这里用 MenuFlyout（挂 ContextFlyout）而不是 ContextMenu——Avalonia 12 的 ContextMenu
    /// 改为 MenuBase、以自身作为 Popup 内容渲染，需要自己的 ControlTheme，而 FA3 (3.0.0) 未提供
    /// 该样式（反编译确认），导致菜单打开却完全不可见；MenuFlyout 走 MenuFlyoutPresenter，
    /// FA2/FA3 均有现成样式，ContextFlyout 机制在 Avalonia 11.3/12.1 行为一致（右键/长按自动弹出并定位指针）。
    /// </summary>
    private static void AttachImageContextMenu(Border container, string url, Image imageControl, StackPanel errorPanel, ExpressiveLoadingIndicator loadingIndicator)
    {
        var saveMenuItem = new MenuItem { Header = "另存为…" };
        saveMenuItem.Click += async (_, _) =>
        {
            switch (ResolveSaveAction(url, imageControl, errorPanel, loadingIndicator))
            {
                case ImageSaveAction.Image:
                    Log($"[click] 另存为 clicked (save image), url={url}");
                    await SaveImageAsAsync(url, imageControl);
                    break;
                case ImageSaveAction.LoadingSource:
                    Log($"[click] 另存为 clicked (loading state, save source code), url={url}");
                    await SaveSourceCodeAsAsync(imageControl);
                    break;
                default:
                    Log($"[click] 另存为 clicked (error state, save log), url={url}");
                    await SaveErrorLogAsAsync(url, imageControl);
                    break;
            }
        };

        var menuFlyout = new MenuFlyout();
        menuFlyout.Items.Add(saveMenuItem);
        menuFlyout.Opening += (_, _) =>
        {
            var cachePath = GetImageCachePath(url);
            var cacheExists = cachePath != null && File.Exists(cachePath);
            var hasBitmap = imageControl.Source is Bitmap;
            var isLoading = loadingIndicator.IsVisible;
            // 错误态判据：错误面板当前可见，或该图留有历史加载失败记录
            //（面板可见性是瞬态 UI 状态，失败记录是持久化数据，日志里同时输出两者便于定位）
            var isError = errorPanel.IsVisible || GetImageLoadErrorCount(url) > 0;

            // 菜单标题固定为“另存为…”，不随卡片状态变化（不同状态下具体保存什么由 ResolveSaveAction 决定）
            var action = ResolveSaveAction(url, imageControl, errorPanel, loadingIndicator);
            // 卡片状态只可能落在三种分支上：已有图片（缓存/位图） / 错误态 / 加载中（含尚未产生结果的
            // 瞬态），三者覆盖全部情形，菜单项因此恒可用——避免失败项或加载项在“部分图片已加载成功”的
            // 混合状态下被误判为“无任何可保存内容”而变灰（用户实测反馈：右键失败项菜单变灰）。
            saveMenuItem.IsEnabled = true;
            Log($"[opening] flyout Opening, url={url}, cacheExists={cacheExists}, hasBitmap={hasBitmap}, isLoading={isLoading}, isError={isError}, action={action}");
        };
        menuFlyout.Closed += (_, _) => Log($"[closed] flyout Closed, url={url}");
        container.ContextFlyout = menuFlyout;

        // 右键事件到达情况跟踪（隧道+冒泡各一道）：判断右键事件是否到达卡片、是否被上游标记 Handled
        container.AddHandler(InputElement.PointerPressedEvent, (object? s, PointerPressedEventArgs e) =>
        {
            var p = e.GetCurrentPoint(container);
            Log($"[ptr] Pressed tunnel btn={DescribePointerButton(p)}, handled={e.Handled}, source={e.Source?.GetType().Name ?? "null"}");
        }, RoutingStrategies.Tunnel);

        container.AddHandler(InputElement.PointerReleasedEvent, (object? s, PointerReleasedEventArgs e) =>
        {
            var p = e.GetCurrentPoint(container);
            Log($"[ptr] Released tunnel btn={DescribePointerButton(p)}, handled={e.Handled}, source={e.Source?.GetType().Name ?? "null"}");
        }, RoutingStrategies.Tunnel);

        container.AddHandler(InputElement.PointerReleasedEvent, (object? s, PointerReleasedEventArgs e) =>
        {
            var p = e.GetCurrentPoint(container);
            Log($"[ptr] Released bubble btn={DescribePointerButton(p)}, handled={e.Handled}, source={e.Source?.GetType().Name ?? "null"}");
        }, RoutingStrategies.Bubble);

        Log($"[attach] MenuFlyout attached via ContextFlyout, url={url}");
    }

    /// <summary>单次图片加载失败记录。</summary>
    private sealed record ImageLoadErrorEntry(DateTime Time, string ExceptionType, string Message);

    private static readonly object ImageLoadErrorsLock = new();
    private static readonly Dictionary<string, List<ImageLoadErrorEntry>> ImageLoadErrors = new();

    /// <summary>
    /// 记录一次图片加载失败（每次重试失败都会追加）。每个 URL 最多保留 20 条，防止长期驻留内存。
    /// </summary>
    private static void RecordImageLoadError(string url, Exception ex) =>
        RecordImageLoadError(url, ex.GetType().Name, ex.Message);

    /// <summary>
    /// 记录一次非异常型加载失败（如跨插件冲突拦截的主动阻止加载），使“另存为错误日志”有内容可导出。
    /// </summary>
    private static void RecordImageLoadError(string url, string exceptionType, string message)
    {
        lock (ImageLoadErrorsLock)
        {
            if (!ImageLoadErrors.TryGetValue(url, out var list))
            {
                list = new List<ImageLoadErrorEntry>();
                ImageLoadErrors[url] = list;
            }

            list.Add(new ImageLoadErrorEntry(DateTime.Now, exceptionType, message));
            if (list.Count > 20)
            {
                list.RemoveAt(0);
            }
        }
    }

    private static int GetImageLoadErrorCount(string url)
    {
        lock (ImageLoadErrorsLock)
        {
            return ImageLoadErrors.TryGetValue(url, out var list) ? list.Count : 0;
        }
    }

    private static List<ImageLoadErrorEntry> GetImageLoadErrors(string url)
    {
        lock (ImageLoadErrorsLock)
        {
            return ImageLoadErrors.TryGetValue(url, out var list)
                ? new List<ImageLoadErrorEntry>(list)
                : new List<ImageLoadErrorEntry>();
        }
    }

    /// <summary>
    /// 为错误面板挂右键菜单（“另存为错误日志...”）：把该图片的加载失败记录导出为文本日志，
    /// 便于用户反馈网络/服务端问题（如 429 限流、超时等）。菜单打开时按是否有失败记录动态启用。
    /// 与照片右键菜单同理使用 MenuFlyout/ContextFlyout（FA3 无 ContextMenu 样式，见 AttachImageContextMenu 注释）。
    /// </summary>
    private static void AttachErrorPanelContextMenu(StackPanel errorPanel, string url, Image imageControl)
    {
        var saveLogMenuItem = new MenuItem { Header = "另存为…" };
        saveLogMenuItem.Click += async (_, _) => await SaveErrorLogAsAsync(url, imageControl);

        var menuFlyout = new MenuFlyout();
        menuFlyout.Items.Add(saveLogMenuItem);
        menuFlyout.Opening += (_, _) =>
        {
            saveLogMenuItem.IsEnabled = GetImageLoadErrorCount(url) > 0;
            Log($"[opening] error panel flyout Opening, url={url}, records={GetImageLoadErrorCount(url)}");
        };
        errorPanel.ContextFlyout = menuFlyout;
    }

    /// <summary>
    /// 加载中状态的“另存为…”：把 <c>Views\Controls\ExpressiveLoadingIndicator.cs</c> 的源代码
    /// 另存到用户选择的位置。该源文件以 EmbeddedResource 随程序集分发（插件安装态下插件目录里
    /// 没有源码文件，只能从程序集资源读取），默认文件名 ExpressiveLoadingIndicator.cs。
    /// 对话框 await 异步等待，写盘在后台线程执行；成功/失败均通过宿主右下角 Toast 提示。
    /// </summary>
    private static async Task SaveSourceCodeAsAsync(Image imageControl)
    {
        string targetPath = string.Empty;
        try
        {
            var topLevel = TopLevel.GetTopLevel(imageControl);
            if (topLevel?.StorageProvider == null)
            {
                ShowErrorToast(imageControl, "另存为失败", "无法获取宿主窗口的存储服务，请重试。");
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "另存为",
                SuggestedFileName = "ExpressiveLoadingIndicator",
                DefaultExtension = "cs",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("C# 源文件") { Patterns = new[] { "*.cs" } }
                }
            });
            if (file == null)
            {
                Log("[save source] dialog returned null (user cancelled)");
                return;
            }

            targetPath = file.Path.LocalPath;
            var content = ReadEmbeddedSourceCode(EmbeddedLoadingIndicatorSourceName);
            await Task.Run(() => File.WriteAllText(targetPath, content));
            imageControl.ShowSuccessToast($"已保存到 {targetPath}");
        }
        catch (Exception ex)
        {
            Log($"[save source] exception: {ex.GetType().Name}: {ex.Message}");
            ShowErrorToast(imageControl, "另存为失败", DescribeSaveFailure(targetPath, ex));
        }
    }

    /// <summary>内嵌源代码资源的逻辑名（与 csproj 的 EmbeddedResource LogicalName 一致）。</summary>
    private const string EmbeddedLoadingIndicatorSourceName = "ExpressiveLoadingIndicator.cs";

    /// <summary>
    /// 从插件程序集读取内嵌的源代码文本（LogicalName 见 csproj 的 EmbeddedResource 配置）。
    /// </summary>
    private static string ReadEmbeddedSourceCode(string logicalName)
    {
        using var stream = typeof(EasterEggPage).Assembly.GetManifestResourceStream(logicalName);
        if (stream == null)
        {
            return $"// 未找到内嵌源文件资源“{logicalName}”。{Environment.NewLine}";
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// “另存为错误日志”：弹出系统保存文件对话框（默认 .txt），把该图片的加载失败记录
    /// （含环境信息）写入用户选择的位置。对话框 await 异步等待，写盘在后台线程执行；
    /// 成功/失败均通过宿主右下角 Toast 提示。
    /// </summary>
    private static async Task SaveErrorLogAsAsync(string url, Image imageControl)
    {
        string targetPath = string.Empty;
        try
        {
            var topLevel = TopLevel.GetTopLevel(imageControl);
            if (topLevel?.StorageProvider == null)
            {
                ShowErrorToast(imageControl, "错误日志保存失败", "无法获取宿主窗口的存储服务，请重试。");
                return;
            }

            var entries = GetImageLoadErrors(url);
            if (entries.Count == 0)
            {
                ShowErrorToast(imageControl, "错误日志保存失败", "该图片暂无加载失败记录。");
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "另存为错误日志",
                SuggestedFileName = $"AdvancedTimeIsland-image-load-error-{DateTime.Now:yyyyMMdd-HHmmss}",
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
            var content = BuildImageLoadErrorLogText(url, entries);
            await Task.Run(() => File.WriteAllText(targetPath, content));
            imageControl.ShowSuccessToast($"错误日志已保存到 {targetPath}");
        }
        catch (Exception ex)
        {
            Log($"[save error log] exception: {ex.GetType().Name}: {ex.Message}");
            ShowErrorToast(imageControl, "错误日志保存失败", DescribeSaveFailure(targetPath, ex));
        }
    }

    /// <summary>
    /// 组装错误日志文本：环境信息 + 该 URL 的全部失败记录（时间/异常类型/消息）。
    /// </summary>
    private static string BuildImageLoadErrorLogText(string url, List<ImageLoadErrorEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AdvancedTimeIsland 女装图片加载错误日志");
        sb.AppendLine("========================================");
        sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"运行时：{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"操作系统：{System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine("========================================");
        sb.AppendLine($"失败 URL：{url}");
        sb.AppendLine($"失败记录（共 {entries.Count} 次，按时间顺序）：");
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            sb.AppendLine($"[{i + 1}] {entry.Time:yyyy-MM-dd HH:mm:ss}  {entry.ExceptionType}");
            sb.AppendLine($"    {entry.Message}");
        }
        sb.AppendLine("========================================");
        sb.AppendLine("本日志仅包含图片加载的网络/解码错误信息，不含任何个人数据。");
        return sb.ToString();
    }

    /// <summary>
    /// “另存为”：弹出系统保存文件对话框（默认 jpg），把照片写入用户选择的位置。
    /// 保存对话框 await 异步等待，文件读写全部在后台线程执行，不阻塞 UI；
    /// 用户取消时静默返回，成功/失败均通过宿主右下角 Toast 提示（失败时给出具体原因）。
    /// </summary>
    private static async Task SaveImageAsAsync(string url, Image imageControl)
    {
        Log($"[save] begin, url={url}");
        string targetPath = string.Empty;
        try
        {
            var topLevel = TopLevel.GetTopLevel(imageControl);
            if (topLevel?.StorageProvider == null)
            {
                Log("[save] topLevel or StorageProvider is null, abort");
                ShowErrorToast(imageControl, "另存为失败", "无法获取宿主窗口的存储服务，请重试。");
                return;
            }

            var suggestedName = Path.GetFileName(new Uri(url).AbsolutePath);
            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = "image.jpg";
            }

            Log($"[save] opening save dialog, suggested={suggestedName}");
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "另存为",
                SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedName),
                DefaultExtension = "jpg",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("JPEG 图片") { Patterns = new[] { "*.jpg", "*.jpeg" } }
                }
            });
            if (file == null)
            {
                Log("[save] dialog returned null (user cancelled)");
                return;
            }

            targetPath = file.Path.LocalPath;
            Log($"[save] dialog returned: {targetPath}");
            var cachePath = GetImageCachePath(url);

            // 优先无损复制本地缓存的原始 JPG（与页面上显示的原图同源）；
            // 仅当缓存缺失时才把当前显示位图重编码为 JPG：Avalonia 11/12 公开 API 都没有
            // JpegEncoder（连 IImageEncoder 类型也未公开），故先在 UI 线程用 Save(Stream)
            // （默认 PNG 输出）拿到无损像素流（Bitmap 属于 UI 线程对象，不在后台线程访问），
            // 再交后台线程用 SkiaSharp 转码为 JPEG 并写盘。
            byte[]? pngBytes = null;
            if ((cachePath == null || !File.Exists(cachePath)) && imageControl.Source is Bitmap bitmap)
            {
                using var memory = new MemoryStream();
                bitmap.Save(memory);
                pngBytes = memory.ToArray();
                Log($"[save] cache missing, re-encoded display bitmap to PNG stream ({pngBytes.Length} bytes)");
            }

            var saved = false;
            await Task.Run(() =>
            {
                if (cachePath != null && File.Exists(cachePath))
                {
                    File.Copy(cachePath, targetPath, overwrite: true);
                    Log($"[save] copied cache file to target (thread T{Environment.CurrentManagedThreadId})");
                    saved = true;
                }
                else if (pngBytes != null)
                {
                    File.WriteAllBytes(targetPath, EncodeJpegViaSkia(pngBytes));
                    Log($"[save] wrote re-encoded JPEG to target (thread T{Environment.CurrentManagedThreadId})");
                    saved = true;
                }
            });

            if (saved)
            {
                Log("[save] done");
                // 与宿主“从本地安装插件”提示同款：右下角 InfoBar 风格成功 Toast
                imageControl.ShowSuccessToast($"已保存到 {targetPath}");
            }
            else
            {
                ShowErrorToast(imageControl, "另存为失败", "照片尚未加载完成，暂无可保存的图像。");
            }
        }
        catch (Exception ex)
        {
            var reason = DescribeSaveFailure(targetPath, ex);
            Log($"[save] exception: {ex.GetType().Name}: {ex.Message}\r\n{ex.StackTrace}");
            ShowErrorToast(imageControl, "另存为失败", reason);
        }
    }

    /// <summary>
    /// 在宿主窗口右下角弹出错误提示（InfoBar 错误样式，不自动关闭，需用户手动关闭），
    /// 标题 + 具体原因正文。
    /// </summary>
    private static void ShowErrorToast(Image imageControl, string title, string reason)
    {
        try
        {
            imageControl.ShowToast(new ToastMessage(reason)
            {
                Title = title,
                Severity = FAInfoBarSeverity.Error,
                AutoClose = false
            });
        }
        catch (Exception toastEx)
        {
            // Toast 展示失败不影响主流程
            System.Diagnostics.Debug.WriteLine($"ShowErrorToast failed: {toastEx}");
        }
    }

    /// <summary>
    /// 把保存失败异常翻译为用户可读的具体原因：磁盘空间不足 / 无写入权限 / 路径无效 / 文件被占用等。
    /// </summary>
    private static string DescribeSaveFailure(string targetPath, Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return "没有写入该位置的权限，请尝试更换保存位置（如文档或桌面）。";
        }

        if (ex is ArgumentException or PathTooLongException or NotSupportedException)
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

    /// <summary>
    /// 用 SkiaSharp 把 PNG 像素流转码为 JPEG 字节（质量 95）。SkiaSharp 是 Avalonia
    /// 渲染后端（Avalonia.Skia）自带的传递依赖，net8/net10 宿主进程内必然存在；
    /// 选用的 API（Decode/FromBitmap/Encode）在 SkiaSharp 2.x/3.x 间一致，两个宿主版本通用。
    /// </summary>
    private static byte[] EncodeJpegViaSkia(byte[] pngBytes)
    {
        using var bitmap = SKBitmap.Decode(pngBytes);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        return data.ToArray();
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
    /// 加载单个卡片（带卡片级取消令牌）。取消来源有两种：页面离树（linked 页面级令牌级联）或
    /// 卡片滚出视口 ±500px（<see cref="UpdateViewportLoading"/> 主动取消）——两种情况都静默返回、
    /// 已解码出的位图立即 Dispose，绝不写回控件（否则 async 闭包会把整页控件树钉在内存里）。
    /// </summary>
    private async Task LoadImageCoreAsync(ImageSlot slot, CancellationToken cancellationToken)
    {
        try
        {
            var url = slot.Url;
            var imageControl = slot.Image;
            var errorPanel = slot.ErrorPanel;
            var errorDetailText = slot.ErrorDetailText;
            var retryButton = slot.RetryButton;
            var loadingIndicator = slot.LoadingIndicator;

            // 跨插件冲突拦截：宿主中存在入口 DLL 实际加载在本进程的“女装/男娘”类插件时，
            // 所有女装图片一律不读取本地缓存、也不发起网络请求，直接落到与加载失败相同的错误面板
            //（“加载失败，轻触屏幕”），详情列出需禁用并重启的插件。首次加载 / 重试 / 刷新 / 视口重载
            // 全部经过本方法，故在此一处拦截即可覆盖所有路径。宿主禁用插件只写 .disabled 标记、
            // 程序集仍驻留内存，因此禁用后必须重启宿主才会解除拦截，提示用户“禁用并重启”。
            // 本分支在 try 内 return，finally 仍会复位卡片状态。
            var blockingPlugins = CrossPluginHelper.GetRunningFemboyLikePlugins();
            if (blockingPlugins.Count > 0)
            {
                ReplaceImageSource(imageControl, null);
                ResetImageEntranceState(imageControl);
                errorDetailText.Text = BuildPluginsBlockedMessage(blockingPlugins);
                errorPanel.IsVisible = true;
                // 错误态卡片尺寸自适应：高度由错误面板内容决定（少量信息不留大空隙，较多信息不溢出）
                ClearPlaceholderSize(slot);
                // 记一条失败记录：拦截并非异常，但“另存为错误日志”需要内容，且此处是用户排查问题的唯一线索
                RecordImageLoadError(url, "PluginConflict", BuildPluginsBlockedMessage(blockingPlugins));
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
                    // 等待解码期间页面可能已离树 / 卡片已滚出视口：立刻释放这张图，不写回控件
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
                // 【缓存落盘不响应取消】视口虚拟化下频繁滑动时，卡片刚进入视口开始下载就被滚出
                // ±500px 取消——若下载/写缓存挂在取消令牌上，任务会在中途抛 OCE 静默退出，
                // 缓存永远写不进、每次滑回都重新请求网络（触发服务端限流）。
                // 因此下载与落盘统一使用 CancellationToken.None，一次进入视口的下载必然完整落盘；
                // 取消令牌只控制“是否解码显示”（下方各检查点）。
                // 网络下载不受解码闸门限制：视口内的几张图可并行下载（JPG 缓冲仅数 MB），
                // 只有后面的解码步骤排队进入闸门，避免“两路下载”串行拖慢全部图片
                using var response = await SharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);
                response.EnsureSuccessStatusCode();
                await using var downloadStream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
                using var memoryBuffer = new MemoryStream();
                await downloadStream.CopyToAsync(memoryBuffer, CancellationToken.None);

                // 原子落盘（临时文件 + Move），先于解码——即使此刻已滚出视口，缓存也已就位，
                // 下次滑回直接走本地加载，不再发网络请求
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

                // 缓存已落盘：已滚出视口 / 页面已离树则到此为止，不解码不显示
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await ImageDecodeGate.WaitAsync(cancellationToken);
                try
                {
                    // 同一缓冲直接用于解码（写缓存已用独立的临时文件流拷贝，互不影响）
                    memoryBuffer.Position = 0;
                    decoded = await Task.Run(() => DecodeDownscaled(memoryBuffer), CancellationToken.None);
                }
                finally
                {
                    ImageDecodeGate.Release();
                }

                // 【防泄漏】解码期间页面可能已离树 / 卡片已滚出视口（OnDetached/卸载逻辑已把
                // Source 清空）：若此时仍把新解码位图写回控件，该位图会钉在已卸载页面上，只能等
                // GC/finalizer 才归还非托管内存。与其余取消检查保持一致，取消后立即释放、不写回。
                if (cancellationToken.IsCancellationRequested)
                {
                    decoded?.Dispose();
                    return;
                }
            }

            if (decoded == null)
            {
                throw new InvalidOperationException("图片解码失败（返回空位图）。");
            }

            ReplaceImageSource(imageControl, decoded);
            // 位图已接管卡片尺寸：清除等比占位，交还给位图 + MaxWidth 机制
            ClearPlaceholderSize(slot);
            PlayImageEntranceAnimation(imageControl);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 页面已离树 / 卡片已滚出视口：静默退出，不更新任何 UI、不显示错误面板
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            ReplaceImageSource(slot.Image, null);
            ResetImageEntranceState(slot.Image);
            slot.ErrorDetailText.Text = $"URL: {slot.Url}\n错误: {ex.GetType().Name}: {ex.Message}";
            slot.ErrorPanel.IsVisible = true;
            // 失败后卡片尺寸改为自适应：清除等比占位，高度交给错误面板内容（信息少不留大空隙、多不溢出）
            ClearPlaceholderSize(slot);
            slot.RetryButton.IsEnabled = true;
            slot.RetryButton.Content = "重新刷新";
            RecordImageLoadError(slot.Url, ex);
        }
        finally
        {
            slot.IsLoading = false;
            // 取消时不碰控件（卸载路径已负责关闭指示器，避免旧加载的 finally 误开新一轮的占位符）
            if (!cancellationToken.IsCancellationRequested)
            {
                slot.LoadingIndicator.IsActive = false;
                slot.LoadingIndicator.IsVisible = false;
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
    /// 页面离树时：取消全部在途加载（linked CTS 级联）并释放全部已解码位图。
    /// 视口虚拟化下每张图常驻内存仅占位尺寸，重进页面时由 <see cref="UpdateViewportLoading"/> 按视口重新加载。
    /// </summary>
    private void ReleaseAllLoadedBitmaps()
    {
        if (_imageSlots == null)
        {
            return;
        }

        foreach (var slot in _imageSlots)
        {
            ReleaseSlotBitmap(slot, keepErrorPanel: true);
        }
    }

    /// <summary>释放单个卡片的位图与加载指示器；仍在视口内且需要保留错误面板时可选择性恢复占位尺寸。</summary>
    private void ReleaseSlotBitmap(ImageSlot slot, bool keepErrorPanel)
    {
        slot.CancelLoad();
        if (slot.Image.Source is Bitmap bitmap)
        {
            slot.Image.Source = null;
            bitmap.Dispose();
        }
        slot.LoadingIndicator.IsActive = false;
        slot.LoadingIndicator.IsVisible = false;
        // 错误态：尺寸自适应（错误面板决定高度），不恢复等比占位；
        // 其余状态恢复等比占位（含错误面板已关闭的待重试卡片）
        if (slot.ErrorPanel.IsVisible)
        {
            ClearPlaceholderSize(slot);
        }
        else if (!keepErrorPanel || slot.Image.Source == null)
        {
            ApplyPlaceholderSize(slot);
        }
    }

    /// <summary>
    /// 视口虚拟化引擎。滚动根就绪后挂 ScrollChanged / SizeChanged（合并到 Background 优先级单帧执行）：
    /// 遍历全部卡片，卡片与视口上下扩展 <see cref="ViewportLoadMarginPx"/> 的范围相交 → 加载；
    /// 不相交 → 立即释放位图与在途任务。1000 张图常驻的也只有视口附近的几张位图。
    /// </summary>
    private void EnsureViewportTracking()
    {
        // 页面自带 MainScrollViewer 与宿主外层 ScrollViewer 二者都可能实际发生滚动
        //（取决于宿主如何限制页面高度），两个都挂监听；实际用于计算视口范围的根在
        // <see cref="UpdateViewportLoading"/> 中按“谁有可滚动溢出”动态选定
        var main = this.FindControl<ScrollViewer>("MainScrollViewer");
        ScrollViewer? outer = null;
        foreach (var ancestor in this.GetVisualAncestors())
        {
            if (ancestor is ScrollViewer sv && !ReferenceEquals(sv, main))
            {
                outer = sv;
                break;
            }
        }

        if (ReferenceEquals(main, _viewportScrollViewer) && ReferenceEquals(outer, _viewportOuterScrollViewer))
        {
            return;
        }

        DetachViewportTracking();
        _viewportScrollViewer = main;
        _viewportOuterScrollViewer = outer;

        if (main != null)
        {
            main.ScrollChanged += OnViewportScrollChanged;
            main.SizeChanged += OnViewportSizeChanged;
        }
        if (outer != null)
        {
            outer.ScrollChanged += OnViewportScrollChanged;
            outer.SizeChanged += OnViewportSizeChanged;
        }
    }

    private void DetachViewportTracking()
    {
        if (_viewportScrollViewer != null)
        {
            _viewportScrollViewer.ScrollChanged -= OnViewportScrollChanged;
            _viewportScrollViewer.SizeChanged -= OnViewportSizeChanged;
            _viewportScrollViewer = null;
        }
        if (_viewportOuterScrollViewer != null)
        {
            _viewportOuterScrollViewer.ScrollChanged -= OnViewportScrollChanged;
            _viewportOuterScrollViewer.SizeChanged -= OnViewportSizeChanged;
            _viewportOuterScrollViewer = null;
        }
    }

    /// <summary>
    /// 选定本次视口计算的实际滚动根：页面自带 MainScrollViewer 自身有溢出（内部滚动）时用它，
    /// 否则用外层 ScrollViewer（外层承载滚动），都没有回退 Main。
    /// </summary>
    private ScrollViewer? ResolveViewportRoot()
    {
        var main = _viewportScrollViewer;
        var outer = _viewportOuterScrollViewer;
        if (main != null && main.Extent.Height > main.Viewport.Height + 1)
        {
            return main;
        }
        return outer ?? main;
    }

    private void OnViewportScrollChanged(object? sender, ScrollChangedEventArgs e) => ScheduleViewportUpdate();

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e) => ScheduleViewportUpdate();

    /// <summary>滚动/缩放高频触发的重算合并到单帧执行（Background 优先级），避免逐事件全量遍历。</summary>
    private void ScheduleViewportUpdate()
    {
        if (_viewportUpdateScheduled || !this.IsAttachedToVisualTree())
        {
            return;
        }

        _viewportUpdateScheduled = true;
        Dispatcher.UIThread.Post(() =>
        {
            _viewportUpdateScheduled = false;
            UpdateViewportLoading();
        }, DispatcherPriority.Background);
    }

    /// <summary>按当前视口位置加载/卸载全部图片卡片。</summary>
    private void UpdateViewportLoading()
    {
        if (_imageSlots == null || !this.IsAttachedToVisualTree())
        {
            return;
        }

        var root = ResolveViewportRoot();
        if (root == null)
        {
            return;
        }

        // 视口宽度就绪/变化时先刷新占位尺寸：首次进入的兜底占位宽度在此收敛到真实宽度，
        // 保证加载占位符的三个形状完整落在卡片内
        RefreshPlaceholderSizes();

        // 【关键】TranslatePoint(卡片 → root) 得到的已经是“视口坐标”——随滚动实时变化，
        // 可见卡片的 Y ∈ [0, Viewport.Height]。绝不能再叠加 root.Offset.Y（否则偏移被计算两次，
        // 滚过约三张图后判定窗口整体漂出所有卡片的坐标范围，之后所有卡片被永久误判为离视不加载）。
        var top = -ViewportLoadMarginPx;
        var bottom = root.Viewport.Height + ViewportLoadMarginPx;

        foreach (var slot in _imageSlots)
        {
            var container = slot.Container;
            if (container.Bounds.Height <= 0 && slot.Image.Source == null)
            {
                // 占位尚未布局（0 高度）：先补占位再判定
                ApplyPlaceholderSize(slot);
            }

            var origin = container.TranslatePoint(new Point(0, 0), root);
            if (origin == null)
            {
                continue;
            }

            var cardTop = origin.Value.Y;
            var cardBottom = cardTop + Math.Max(container.Bounds.Height, 1);
            var inRange = cardBottom > top && cardTop < bottom;

            if (inRange)
            {
                if (slot.Image.Source == null && !slot.ErrorPanel.IsVisible)
                {
                    EnsureSlotLoaded(slot);
                }
            }
            else
            {
                // 离开视口 ±500px：立即卸载位图与在途任务（错误面板保留，等用户回到视口手动重试）
                if (slot.Image.Source != null || slot.LoadingIndicator.IsActive)
                {
                    ReleaseSlotBitmap(slot, keepErrorPanel: true);
                }
                else if (slot.ErrorPanel.IsVisible)
                {
                    CancelSlotLoad(slot);
                }
            }
        }
    }

    /// <summary>启动单个卡片的按需加载（幂等：已在加载/已有位图/错误待重试时跳过）。</summary>
    private void EnsureSlotLoaded(ImageSlot slot)
    {
        if (slot.IsLoading || slot.Image.Source != null || slot.ErrorPanel.IsVisible)
        {
            return;
        }

        slot.CancelLoad();
        slot.Cts = CancellationTokenSource.CreateLinkedTokenSource(GetCurrentLoadToken());
        slot.IsLoading = true;
        // 错误/空卡片重新开始加载前先恢复等比占位：错误态曾把尺寸改成自适应，
        // 加载期间需要回到占位高度，等位图到达后再交给位图尺寸
        if (slot.Image.Source == null)
        {
            ApplyPlaceholderSize(slot);
        }
        slot.LoadingIndicator.IsActive = true;
        slot.LoadingIndicator.IsVisible = true;
        _ = LoadImageCoreAsync(slot, slot.Cts.Token);
    }

    /// <summary>取消单个卡片的在途加载任务并复位其加载状态（不改控件）。</summary>
    private static void CancelSlotLoad(ImageSlot slot)
    {
        slot.CancelLoad();
        slot.IsLoading = false;
    }

    /// <summary>
    /// 获取图片宽高比（高/宽）。进程级缓存命中直接返回；未命中返回默认占位比，
    /// 真实尺寸由 <see cref="PrimeMissingImageSizesAsync"/> 异步补齐后刷新占位。
    /// </summary>
    private static double GetImageAspect(string url) =>
        ImageAspectCache.TryGetValue(url, out var aspect) ? aspect : DefaultImageAspect;

    /// <summary>
    /// 计算等比占位宽度：与 <see cref="ResponsiveImageHelper.MakeWidthFollowAncestor"/> 的
    /// MaxWidth 逻辑一致（内容区宽度的 80%）。基准宽度取视口根宽度（缺页边距 32）；
    /// 首次进入时视口尚未布局（宽度为 0），先用 640 兜底，布局就绪后由
    /// <see cref="RefreshPlaceholderSizes"/> 收敛到真实宽度。
    /// </summary>
    private double ResolvePlaceholderWidth()
    {
        var scrollViewer = _viewportScrollViewer ?? this.FindControl<ScrollViewer>("MainScrollViewer");
        var baseWidth = scrollViewer?.Bounds.Width ?? 0;
        if (baseWidth <= 1)
        {
            baseWidth = 640;
        }
        baseWidth -= 32; // 内容区左右边距
        return Math.Max(baseWidth * 0.8, 64);
    }

    /// <summary>
    /// 位图加载前为卡片铺设等比占位（宽度 + 宽度×宽高比的高度），加载完成后清除。
    /// 错误态卡片不铺设：卡片尺寸由错误面板内容自适应（少量信息不留大空隙、较多信息不溢出）。
    /// </summary>
    private void ApplyPlaceholderSize(ImageSlot slot, double? widthOverride = null)
    {
        if (slot.Image.Source != null || slot.ErrorPanel.IsVisible)
        {
            return;
        }

        var width = widthOverride ?? ResolvePlaceholderWidth();
        // 高度按「实际渲染宽度」等比：设定的 Width 可能被 MaxWidth 收窄，Bounds.Width 才是真实宽度；
        // 两者混用会让占位高度在「设定宽度」和「收窄宽度」之间每帧反复弹跳
        var effectiveWidth = slot.Container.Bounds.Width > 1
            ? Math.Min(width, slot.Container.Bounds.Width)
            : width;
        var height = effectiveWidth * GetImageAspect(slot.Url);
        if (double.IsNaN(slot.Container.Width) || Math.Abs(slot.Container.Width - width) > 0.5)
        {
            slot.Container.Width = width;
        }
        // 高度按占位宽度等比：Width 还可能被 MaxWidth 收窄，SizeChanged 会继续收敛
        if (double.IsNaN(slot.Container.Height) || Math.Abs(slot.Container.Height - height) > 0.5)
        {
            slot.Container.Height = height;
        }
    }

    /// <summary>
    /// 视口宽度变化后重新铺设全部卡片的占位尺寸。首次进入时视口尚未布局，占位只能先按兜底宽度铺设，
    /// 布局完成 / 窗口缩放后由本方法收敛到真实宽度（否则占位符的三个形状会被窄卡片裁成一个）。
    /// 错误态卡片清掉占位尺寸，保持内容自适应。
    /// </summary>
    private void RefreshPlaceholderSizes()
    {
        if (_imageSlots == null || _imageSlots.Count == 0)
        {
            return;
        }

        var width = ResolvePlaceholderWidth();
        foreach (var slot in _imageSlots)
        {
            if (slot.Image.Source != null)
            {
                continue;
            }

            if (slot.ErrorPanel.IsVisible)
            {
                ClearPlaceholderSize(slot);
                continue;
            }

            ApplyPlaceholderSize(slot, width);
        }
    }

    /// <summary>清除占位尺寸，交还给位图 + MaxWidth 机制决定卡片尺寸。</summary>
    private static void ClearPlaceholderSize(ImageSlot slot)
    {
        slot.Container.Width = double.NaN;
        slot.Container.Height = double.NaN;
    }

    /// <summary>
    /// 图片尺寸预扫描：为宽高比未知的图片补齐 <see cref="ImageAspectCache"/>。
    /// 有本地缓存 → 读文件头解析；无缓存 → HTTP Range 请求头部字节解析（失败回退完整下载）。
    /// 解析完成后在 UI 线程刷新对应卡片占位并触发一次视口重算。进程级缓存跨页面共享，
    /// 每张图整个运行期只解析一次；并发由 <see cref="ImageSizeScanGate"/> 限制。
    /// </summary>
    private async Task PrimeMissingImageSizesAsync()
    {
        if (_imageSlots == null)
        {
            return;
        }

        var pending = _imageSlots.Where(s => !ImageAspectCache.ContainsKey(s.Url)).Select(s => s.Url).Distinct().ToList();
        // 并行探测（网络探测由 ImageSizeScanGate 限并发，文件头读取本身即轻量 IO）
        var probeTasks = pending.Select(async url =>
        {
            try
            {
                var size = await Task.Run(() => ProbeImageSizeAsync(url)).ConfigureAwait(false);
                if (size == null)
                {
                    return;
                }

                ImageAspectCache[url] = (double)size.Value.Height / size.Value.Width;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_imageSlots != null)
                    {
                        foreach (var slot in _imageSlots)
                        {
                            if (slot.Url == url && slot.Image.Source == null && !slot.ErrorPanel.IsVisible)
                            {
                                // 已加载位图 / 错误态的无需占位；占位态刷新等比尺寸
                                ApplyPlaceholderSize(slot);
                            }
                        }
                    }
                    ScheduleViewportUpdate();
                });
            }
            catch
            {
                // 尺寸探测失败不影响主流程：保持默认占位比，加载完成后位图自然撑开
            }
        }).ToList();
        await Task.WhenAll(probeTasks).ConfigureAwait(false);
    }

    /// <summary>探测单张图片的像素尺寸：优先本地缓存文件头，否则 HTTP Range 头部。</summary>
    private async Task<PixelSize?> ProbeImageSizeAsync(string url)
    {
        var cachePath = GetImageCachePath(url);
        if (cachePath != null && File.Exists(cachePath))
        {
            try
            {
                using var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                var buffer = new byte[Math.Min(ImageHeaderProbeBytes, fs.Length)];
                await fs.ReadExactlyAsync(buffer).ConfigureAwait(false);
                if (TryParseJpegSize(buffer, out var w, out var h))
                {
                    return new PixelSize(w, h);
                }
            }
            catch
            {
                // 文件读取/解析失败 → 走网络探测
            }
        }

        return await ProbeImageSizeFromNetworkAsync(url).ConfigureAwait(false);
    }

    /// <summary>
    /// 通过 HTTP Range 请求只拉取图片头部字节（≤<see cref="ImageHeaderProbeBytes"/>）解析尺寸，
    /// 服务器不支持 Range（非 206）时回退完整下载并把响应体写入本地缓存（与加载管道共享缓存文件）。
    /// </summary>
    private async Task<PixelSize?> ProbeImageSizeFromNetworkAsync(string url)
    {
        await ImageSizeScanGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var cachePath = GetImageCachePath(url);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, ImageHeaderProbeBytes - 1);
                using var response = await SharedHttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    if (TryParseJpegSize(data, out var w, out var h))
                    {
                        // 已拿到完整响应（文件较小或服务器忽略 Range 返回 200 全量）且尚无缓存 → 顺手落盘
                        if (cachePath != null && !File.Exists(cachePath) && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                                var tempPath = Path.Combine(Path.GetDirectoryName(cachePath)!, Guid.NewGuid() + ".tmp");
                                await File.WriteAllBytesAsync(tempPath, data).ConfigureAwait(false);
                                File.Move(tempPath, cachePath);
                            }
                            catch
                            {
                                // 缓存写失败不影响尺寸结果
                            }
                        }
                        return new PixelSize(w, h);
                    }
                }
            }
            catch
            {
                // 头部探测失败 → 回退完整下载
            }

            // 回退：完整下载解析（不写缓存——留给加载管道统一写，避免双写竞态）
            try
            {
                var data = await SharedHttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                if (TryParseJpegSize(data, out var w, out var h))
                {
                    return new PixelSize(w, h);
                }
            }
            catch
            {
                // 网络不可用：保持默认占位比
            }

            return null;
        }
        finally
        {
            ImageSizeScanGate.Release();
        }
    }

    /// <summary>
    /// 解析 JPEG 头部获取像素尺寸：顺序扫描 marker，命中 SOF0~SOF15（剔除 DHT/JPG/DAC 扩展）后
    /// 读取宽高。只需头部 ≤512KB（相机 EXIF 含缩略图可达数十 KB，SOF 在其后），无需解码整图。
    /// </summary>
    private static bool TryParseJpegSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
        {
            return false;
        }

        var i = 2;
        while (i + 4 < data.Length)
        {
            if (data[i] != 0xFF)
            {
                i++;
                continue;
            }

            var marker = data[i + 1];
            if (marker == 0xFF || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7) || marker == 0xD8)
            {
                i += 2;
                continue;
            }
            if (marker == 0xD9)
            {
                return false; // EOI 前未见 SOF
            }

            var length = (data[i + 2] << 8) | data[i + 3];
            if (length < 2)
            {
                return false;
            }

            // SOF0~SOF15（0xC4 DHT / 0xC8 JPG / 0xCC DAC 除外）
            if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
            {
                if (i + 9 > data.Length)
                {
                    return false;
                }
                height = (data[i + 5] << 8) | data[i + 6];
                width = (data[i + 7] << 8) | data[i + 8];
                return width > 0 && height > 0;
            }

            i += 2 + length;
        }

        return false;
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
            if (_imageSlots == null || _imageSlots.Count == 0)
                return;

            var cacheDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Images");

            // 存在运行中的冲突插件时本轮刷新只会逐图落到拦截错误面板（解除需重启宿主），
            // 不能删除本地缓存：否则用户在冲突期间点一次刷新，重启后就得重新联网下载全部图片
            var blockedByPlugins = CrossPluginHelper.GetRunningFemboyLikePlugins();

            foreach (var slot in _imageSlots)
            {
                var fileName = Path.GetFileName(new Uri(slot.Url).AbsolutePath);
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

                // 立即释放旧位图的非托管内存与在途任务，再由视口引擎按需重载
                //（否则旧图要等 GC finalizer 才归还，刷新高峰内存翻倍）
                ReleaseSlotBitmap(slot, keepErrorPanel: false);
                slot.ErrorPanel.IsVisible = false;
                slot.ErrorDetailText.Text = "";
                slot.RetryButton.IsEnabled = false;
                slot.RetryButton.Content = "加载中...";
            }

            // 视口引擎立即重载当前视口内的图片；视口外的卡片等滚动到附近再加载
            ScheduleViewportUpdate();

            // 预扫描期间缓存文件已删除：重新补齐被删除图片的尺寸
            _ = PrimeMissingImageSizesAsync();
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // 若已在其它入口（如「调试 → 女装」页）同意过注意事项，则本页覆盖层随之解除并补装配内容；
        // 紧随其后的 OnLoaded 会完成视口虚拟化与「返回顶部」按钮的初始化。
        if (NoticeOverlay.IsVisible && _pluginSettings?.WomenswearNoticeAccepted == true)
        {
            NoticeOverlay.IsVisible = false;
            BuildContent();
        }
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

        // 视口虚拟化：取消视口监听、归还全部已加载位图的非托管内存——离树后常驻内存只剩
        // 轻量卡片控件与占位尺寸，不含任何位图
        DetachViewportTracking();
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
            _outerScrollViewer.RemoveHandler(InputElement.PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged);
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
        // 未同意图片版权注意事项时不装配内容（图片区为空），也不创建视口跟踪与“返回顶部”悬浮按钮——
        // 后者挂在宿主 OverlayLayer 上，会浮在注意事项覆盖层之上。
        if (NoticeOverlay.IsVisible)
        {
            return;
        }

        InitializeLoadedContent();
    }

    /// <summary>
    /// 页面进入视觉树 / 用户同意注意事项后的内容激活：重建加载令牌、挂载视口虚拟化与返回顶部按钮。
    /// </summary>
    private void InitializeLoadedContent()
    {
        // 先清理旧的按钮（防止切换Tab回来时重复创建）
        CleanupBackToTopButton();

        // 离树期间旧令牌已取消；重新进入时换新令牌，视口虚拟化引擎随后按视口位置按需加载
        if (_imageLoadCts == null || _imageLoadCts.IsCancellationRequested)
        {
            _imageLoadCts?.Dispose();
            _imageLoadCts = new CancellationTokenSource();
        }

        // 视口虚拟化：挂载滚动根监听并立即按当前视口加载可见范围内的图片（±500px），
        // 视口外的卡片保持轻量等比占位——不再全量重载
        EnsureViewportTracking();
        ScheduleViewportUpdate();

        // 页面重新进入时，把已加载完成的图片卡片落回可见终值：
        // 防止图片成功回调刚把卡片置为透明起播、页面就被切走导致动画取消而停在透明态
        if (_imageSlots != null)
        {
            foreach (var slot in _imageSlots)
            {
                if (slot.Image.Source != null && slot.Image.Parent is Grid g && g.Parent is Border card)
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
            // 回顶动画进行中用户一旦手动滚动/按下即打断动画，交回滚动控制权。
            // 用隧道 + handledEventsToo：平滑滚动（IntroAnimationHelper）会在隧道阶段把滚轮事件标记为已处理，
            // 必须仍能收到，否则回顶动画不会被滚轮打断。
            _outerScrollViewer.AddHandler(InputElement.PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged, RoutingStrategies.Tunnel, handledEventsToo: true);
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

        // 回顶动画直接改写 Offset，先停掉可能正在进行的滚轮平滑滚动，避免两者互相打架
        IntroAnimationHelper.StopSmoothScroll(_outerScrollViewer);

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

    /// <summary>
    /// 视口虚拟化的单张图片卡片槽位：控件引用 + 独立取消令牌 + 加载状态。
    /// 每张卡片的加载用独立 linked CTS（linked 页面级 CTS）：滚出视口时只取消这一张，
    /// 页面离树时由页面级令牌级联取消全部。
    /// </summary>
    private sealed class ImageSlot
    {
        public ImageSlot(string url, Image image, Border container, StackPanel errorPanel,
            TextBlock errorDetailText, Button retryButton, ExpressiveLoadingIndicator loadingIndicator)
        {
            Url = url;
            Image = image;
            Container = container;
            ErrorPanel = errorPanel;
            ErrorDetailText = errorDetailText;
            RetryButton = retryButton;
            LoadingIndicator = loadingIndicator;
        }

        public string Url { get; }
        public Image Image { get; }
        public Border Container { get; }
        public StackPanel ErrorPanel { get; }
        public TextBlock ErrorDetailText { get; }
        public Button RetryButton { get; }
        public ExpressiveLoadingIndicator LoadingIndicator { get; }

        /// <summary>本卡片的在途加载任务；null = 无在途任务。</summary>
        public CancellationTokenSource? Cts { get; set; }

        /// <summary>是否处于加载中（含等下载/等解码）。</summary>
        public bool IsLoading { get; set; }

        /// <summary>取消本卡片的在途加载任务并释放 CTS（幂等）。</summary>
        public void CancelLoad()
        {
            if (Cts != null)
            {
                Cts.Cancel();
                Cts.Dispose();
                Cts = null;
            }
        }
    }
}



