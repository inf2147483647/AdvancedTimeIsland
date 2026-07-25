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
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;


namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 女装彩蛋页面
/// 使用Markdown格式展示内容
/// </summary>
public class EasterEggPage : UserControl
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
    private List<(string url, Image image, StackPanel errorPanel, TextBlock errorDetailText, Button retryButton)>? _imageLoadInfos;
    private ScrollViewer? _scrollViewer;
    private Button? _backToTopButton;
    private OverlayLayer? _overlayLayer;
    private ScrollViewer? _outerScrollViewer;

    public EasterEggPage() : this(null)
    {
    }

    public EasterEggPage(PluginSettings? pluginSettings = null)
    {
        _pluginSettings = pluginSettings;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = false
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        // 标题
        mainPanel.Children.Add(new TextBlock
        {
            Text = "女装",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.HotPink,
            HorizontalAlignment = HorizontalAlignment.Center
        });

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
            mainPanel.Children.Add(disclaimerBar);
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
            mainPanel.Children.Add(infoBar);
        }

        // Markdown 内容
        var markdownContent = @"## 图片展示

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

![图片15](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC02563.jpg)";

        mainPanel.Children.Add(CreateMarkdownSection(markdownContent));

        // 在最后一个链接下面添加30px的空白
        mainPanel.Children.Add(new Border { Height = 30 });

        _scrollViewer.Content = mainPanel;
        Content = _scrollViewer;

        // 在Loaded事件中初始化返回顶部按钮
        Loaded += OnLoaded;
    }

    /// <summary>
    /// 创建Markdown格式的文本区域
    /// </summary>
    private Border CreateMarkdownSection(string markdownText)
    {
        _normalTextBlocks = new List<TextBlock>();
        _boldTextBlocks = new List<TextBlock>();
        _separatorBorders = new List<Border>();
        _imageLoadInfos = new List<(string url, Image image, StackPanel errorPanel, TextBlock errorDetailText, Button retryButton)>();

        var section = new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16),
            BorderBrush = new SolidColorBrush(Color.Parse("#BBBBBB")),
            BorderThickness = new Thickness(2)
        };
        _markdownSectionBorder = section;

        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        // 解析Markdown并创建文本块
        var lines = markdownText.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                // 空行添加间距
                content.Children.Add(new Border { Height = 8 });
                continue;
            }

            if (line.StartsWith("## "))
            {
                // 二级标题
                var title = line.Substring(3).Trim();
                content.Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.HotPink,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 4)
                });

                if (title == "图片展示")
                {
                    var refreshButton = new Button
                    {
                        Content = "刷新",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Padding = new Thickness(16, 8),
                        FontSize = 12,
                        Margin = new Thickness(0, 4, 0, 8)
                    };
                    refreshButton.Click += RefreshButton_Click;
                    content.Children.Add(refreshButton);
                }
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
                // Markdown 图片 ![alt](url)
                var imageControl = CreateMarkdownImage(line);
                if (imageControl != null)
                {
                    content.Children.Add(imageControl);
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
            Margin = new Thickness(0, 4, 0, 4)
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
            Text = "图片加载失败",
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
            Content = "重试",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(12, 6),
            FontSize = 12
        };
        errorPanel.Children.Add(retryButton);

        var grid = new Grid();
        grid.Children.Add(image);
        grid.Children.Add(errorPanel);

        var container = new Border
        {
            Child = grid,
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 0
        };

        container.Loaded += (s, e) =>
        {
            var parent = container.Parent as Control;
            if (parent != null)
            {
                container.Width = parent.Bounds.Width * 0.8;
            }
        };

        async void RetryHandler(object? sender, RoutedEventArgs args)
        {
            retryButton.IsEnabled = false;
            retryButton.Content = "加载中...";
            errorPanel.IsVisible = false;
            errorDetailText.Text = "";
            await LoadRemoteImageWithRetry(url, image, errorPanel, errorDetailText, retryButton);
        }

        retryButton.Click += RetryHandler;

        _imageLoadInfos?.Add((url, image, errorPanel, errorDetailText, retryButton));

        LoadRemoteImageWithRetry(url, image, errorPanel, errorDetailText, retryButton);

        return container;
    }

    private async Task LoadRemoteImageWithRetry(string url, Image imageControl, StackPanel errorPanel, TextBlock errorDetailText, Button retryButton)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
            var cacheDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Images");
            var cachePath = Path.Combine(cacheDir, fileName);

            if (File.Exists(cachePath))
            {
                var bitmap = new Avalonia.Media.Imaging.Bitmap(cachePath);
                imageControl.Source = bitmap;
                return;
            }

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();

            using var ms = new MemoryStream(bytes);
            var bitmap2 = new Avalonia.Media.Imaging.Bitmap(ms);
            imageControl.Source = bitmap2;

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
            errorDetailText.Text = $"URL: {url}\n错误: {ex.GetType().Name}: {ex.Message}";
            errorPanel.IsVisible = true;
            retryButton.IsEnabled = true;
            retryButton.Content = "重试";
        }
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

                tasks.Add(LoadRemoteImageWithRetry(info.url, info.image, info.errorPanel, info.errorDetailText, info.retryButton));
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
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 先清理旧的按钮（防止切换Tab回来时重复创建）
        CleanupBackToTopButton();

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



