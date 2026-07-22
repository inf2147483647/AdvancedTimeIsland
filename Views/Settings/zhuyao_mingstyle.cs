using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandZhuYaoMingStyle", "主腰 明制", true, SettingsPageCategory.Debug)]
public class ZhuYaoMingStylePage : SettingsPageBase
{
    protected Border? _contentBorder;
    protected List<TextBlock>? _paragraphTextBlocks;
    protected List<TextBlock>? _sectionTextBlocks;
    protected TextBlock? _backTextBlock;

    public ZhuYaoMingStylePage()
    {
        InitializeComponent();
    }

    protected static IBrush GetAccentBrush()
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

    private void InitializeComponent()
    {
        _paragraphTextBlocks = new List<TextBlock>();
        _sectionTextBlocks = new List<TextBlock>();

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0)
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };

        _backTextBlock = new TextBlock
        {
            Text = "‹ 返回上一级",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            TextDecorations = TextDecorations.Underline,
            Margin = new Thickness(16, 12, 16, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        _backTextBlock.PointerPressed += OnBackClick;
        mainPanel.Children.Add(_backTextBlock);

        _contentBorder = new Border
        {
            Background = ThemeHelper.GetHanfuBackgroundBrush(),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(16, 12, 16, 16)
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        BuildContent(panel);

        _contentBorder.Child = panel;
        mainPanel.Children.Add(_contentBorder);
        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    protected virtual void BuildContent(StackPanel panel)
    {
        var markdown = @"# 明制汉服·主腰

主腰是明代最具代表性的贴身内衣形制，又称""主腰儿""""主腰子""，外形与背心相似，是明代男女皆可穿着的内层服饰。其名称源自""主于腰""的功能定位，核心作用为收束腰腹、规整身形，同时兼具保暖与遮护功能。

## 历史渊源

主腰由宋代抹胸发展演变而来，宋代内衣以护胸为主，明代主腰则向下延伸至腰腹，形成胸腰腹三段全覆盖的形制，更符合明代服饰端庄规整的审美追求。清代以后，主腰逐渐被肚兜取代。
（AI易编造虚假信息，所以历史材料暂时移除，后续会添加）

## 形制结构

### 基本形制分类
明代主腰主要分为两大类型：

**1. 有肩带款（背心式）**
- 整体形似现代背心，圆领、对襟、无袖
- 肩部与前襟不连，设有独立肩带
- 肩带与腰部均配有系带，系紧后形成明显收腰效果
- 背部有完整面料，可护住后心

**2. 无肩带款（抹胸式）**
- 为横幅长布形态，仅围于胸腰部位
- 前襟开口，通过纽扣或系带固定
- 结构相对简洁，类似宋代抹胸的加长版
- 前襟处设有较宽掩襟，防止走光

### 结构细节
- **开合方式**：前中对开，可使用布扣、金属扣或系带闭合，常见一排数枚纽扣
- **系带系统**：肩带处、腰部两侧均设有细布系带，可自由调节松紧
- **覆盖范围**：上起胸口，下至腰腹，防护面积大于宋代抹胸
- **剪裁特点**：平面剪裁，无硬衬无钢圈，贴合身体但不挤压胸廓
- **面料材质**：常用纱、绫、罗、绸、棉布等，贵族可用绫罗绸缎，平民多用素布

### 装饰特征
- 民间普通主腰多为纯色，简洁朴素
- 官宦贵族款常绣有折枝花果、缠枝卷草、八宝吉祥等纹样
- 刺绣细密规整，纹样排布克制，不铺满衣身
- 男性款多为素面，极少装饰

## 功能与应用

### 核心功能
1. **收束规整**：收拢内层衣衫，使外衣线条平整利落，避免松散走形，契合明代服饰端庄端正的审美
2. **分段保暖**：上护胸口、下护腰腹，全方位抵御风寒，防护范围比抹胸、肚兜更完整
3. **灵活适配**：系带可自由调节松紧，劳作时系紧、居家时放松，适应不同生活场景
4. **遮护礼制**：作为内衣层，配合外衣穿着，符合古代服饰礼制要求

### 适用人群
主腰并非女性专属服饰，明代男女皆可穿着：
- 女性：作为袄裙体系的标准内搭，各阶层普遍使用
- 男性：多为素面简化款，主要用于护住胸腹、抵御风寒
- 覆盖社会各阶层，无严格等级限制，贵族与平民均有对应款式（男装在做了……程序员正在充token）

### 适用场景
- **日常居家**：夏季可单穿主腰搭配薄衫，是内院常见清凉穿搭
- **正式外出**：作为内层搭配袄裙、比甲、披风等外衣
- **劳作活动**：系紧系带可固定衣衫，不影响肢体活动
- **季节适配**：夏季用纱罗薄料，冬季可做夹棉加厚款

## 穿着方法

### 传统穿着层次（由内而外）
1. **第一层**：主腰（贴身穿着）
2. **第二层**：中衣/衫袄（立领短衫、长袄等）
3. **第三层**：外搭（比甲、披风、披袄等）
4. **下装**：中裤→膝裤→马面裙

### 具体穿着步骤
1. 将主腰前襟敞开，贴于身前
2. 调整肩带至合适位置，系好肩部系带
3. 对齐前襟，依次扣好纽扣或系好胸前系带
4. 收紧腰部两侧系带，调整至舒适松紧度
5. 整理掩襟，确保前襟贴合无空隙
6. 外层依次穿着衫袄、比甲或披风等

### 夏季简化穿法
炎热天气可减少穿着层次：主腰外直接搭配薄纱长衫或比甲，下着马面裙，清凉透气又不失礼制。

## 注意事项

### 选购与尺码
- 主腰为贴身服饰，放量不宜过大，建议净胸围加3厘米左右即可
- 优先确认腋下高度、肩带长度、领口贴合度三个核心版型指标
- 尺码过小易勒出副乳、压迫胸廓，过大则无法起到收束作用
- 注意前襟掩襟宽度，过窄易走光，过宽影响贴合度

### 穿着礼仪
- 主腰属于内衣范畴，传统礼制中不可单独外穿于公共场合
- 正式场合必须外搭衫袄、比甲等外衣
- 居家、庭院等私密环境可单穿主腰搭配薄衫
- 现代汉服日常穿着可适当灵活，但正式活动建议遵循传统层次

### 搭配原则
- **色彩搭配**：主腰有刺绣提花时，外衣选纯色；外衣纹样复杂时，主腰选素色，全身仅保留一个视觉重点
- **形制适配**：主腰为明制专属内搭，主要搭配明制袄裙、比甲、圆领袍等，不建议搭配宋制、唐制服饰
- **季节适配**：夏季选纱、罗等轻薄面料，冬季可选夹棉主腰增强保暖性

### 养护要点
- 丝绸、绫罗材质建议手洗，避免用力揉搓
- 系带与纽扣处注意轻柔处理，防止脱落
- 收纳时平铺折叠，避免长期悬挂导致变形
- 刺绣款需注意保护绣线，避免勾挂磨损";

        RenderMarkdown(panel, markdown);
    }

    protected void RenderMarkdown(StackPanel panel, string markdown)
    {
        var pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .UsePipeTables()
            .Build();
        var document = Markdig.Markdown.Parse(markdown, pipeline);
        foreach (var block in document)
        {
            var element = ConvertMarkdownBlock(block);
            if (element != null)
            {
                panel.Children.Add(element);
            }
        }
    }

    private Control? ConvertMarkdownBlock(MarkdownObject block)
    {
        return block switch
        {
            HeadingBlock heading => ConvertHeading(heading),
            ParagraphBlock paragraph => ConvertParagraph(paragraph),
            CodeBlock code => ConvertCodeBlock(code),
            QuoteBlock quote => ConvertQuoteBlock(quote),
            ListBlock list => ConvertListBlock(list),
            Markdig.Extensions.Tables.Table table => ConvertTableBlock(table),
            _ => null
        };
    }

    private Control ConvertHeading(HeadingBlock heading)
    {
        var fontSize = heading.Level switch
        {
            1 => 28.0,
            2 => 21.0,
            3 => 16.0,
            4 => 14.0,
            5 => 12.0,
            6 => 9.0,
            _ => 14.0
        };

        var textBlock = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = FontWeight.Bold,
            Foreground = heading.Level <= 2 ? ThemeHelper.GetLightBlueBrush() : ThemeHelper.GetTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 4)
        };

        if (heading.Inline != null)
        {
            foreach (var inline in ConvertInline(heading.Inline))
            {
                textBlock.Inlines.Add(inline);
            }
        }

        if (heading.Level <= 2)
        {
            _sectionTextBlocks?.Add(textBlock);
        }

        return textBlock;
    }

    private Control ConvertParagraph(ParagraphBlock paragraph)
    {
        var hasImage = paragraph.Inline is ContainerInline container && container.Any(i => i is LinkInline link && link.IsImage);
        
        if (hasImage)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 4),
                Spacing = 4
            };

            var textBlock = new TextBlock
            {
                FontSize = 14,
                Foreground = ThemeHelper.GetSubTextBrush(),
                TextWrapping = TextWrapping.Wrap
            };

            var imagePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };

            ProcessParagraphInline(paragraph.Inline, textBlock, imagePanel);

            if (textBlock.Inlines.Count > 0 || !string.IsNullOrEmpty(textBlock.Text))
            {
                panel.Children.Add(textBlock);
                _paragraphTextBlocks?.Add(textBlock);
            }

            if (imagePanel.Children.Count > 0)
            {
                panel.Children.Add(imagePanel);
            }

            return panel;
        }

        var normalTextBlock = new TextBlock
        {
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };

        if (paragraph.Inline != null)
        {
            foreach (var inline in ConvertInline(paragraph.Inline, normalTextBlock))
            {
                normalTextBlock.Inlines.Add(inline);
            }
        }

        _paragraphTextBlocks?.Add(normalTextBlock);
        return normalTextBlock;
    }

    private void ProcessParagraphInline(Markdig.Syntax.Inlines.Inline? inline, TextBlock textBlock, StackPanel imagePanel)
    {
        ProcessParagraphInline(inline, textBlock.Inlines, imagePanel);
    }

    private void ProcessParagraphInline(Markdig.Syntax.Inlines.Inline? inline, InlineCollection inlines, StackPanel imagePanel)
    {
        if (inline == null) return;

        if (inline is LiteralInline literal)
        {
            inlines.Add(new Run { Text = literal.Content.ToString() });
        }
        else if (inline is EmphasisInline emphasis)
        {
            Span? styleSpan = null;
            if (emphasis.DelimiterChar == '*')
            {
                if (emphasis.DelimiterCount == 2)
                {
                    styleSpan = new Span { FontWeight = FontWeight.Bold };
                }
                else
                {
                    styleSpan = new Span { FontStyle = FontStyle.Italic };
                }
            }
            else if (emphasis.DelimiterChar == '~')
            {
                styleSpan = new Span { TextDecorations = TextDecorations.Strikethrough };
            }

            foreach (var child in emphasis)
            {
                if (styleSpan != null)
                {
                    ProcessParagraphInline(child, styleSpan.Inlines, imagePanel);
                }
                else
                {
                    ProcessParagraphInline(child, inlines, imagePanel);
                }
            }

            if (styleSpan != null)
            {
                inlines.Add(styleSpan);
            }
        }
        else if (inline is LinkInline link)
        {
            if (link.IsImage)
            {
                var imageControl = CreateImageControl(link.Url);
                if (imageControl != null)
                {
                    imagePanel.Children.Add(imageControl);
                }
            }
            else
            {
                var linkSpan = new Span { Foreground = GetAccentBrush(), TextDecorations = TextDecorations.Underline };
                foreach (var child in link)
                {
                    ProcessParagraphInline(child, linkSpan.Inlines, imagePanel);
                }
                inlines.Add(linkSpan);
            }
        }
        else if (inline is LineBreakInline)
        {
            inlines.Add(new LineBreak());
        }
        else if (inline is ContainerInline container)
        {
            foreach (var child in container)
            {
                ProcessParagraphInline(child, inlines, imagePanel);
            }
        }
        else
        {
            inlines.Add(new Run { Text = inline.ToString() });
        }
    }

    private Control? CreateImageControl(string url)
    {
        if (!url.StartsWith("http"))
            return null;

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

    private IEnumerable<Avalonia.Controls.Documents.Inline> ConvertInline(Markdig.Syntax.Inlines.Inline inline, TextBlock? parentTextBlock = null)
    {
        if (inline is LiteralInline literal)
        {
            yield return new Run { Text = literal.Content.ToString() };
        }
        else if (inline is EmphasisInline emphasis)
        {
            Span? styleSpan = null;
            if (emphasis.DelimiterChar == '*')
            {
                if (emphasis.DelimiterCount == 2)
                {
                    styleSpan = new Span { FontWeight = FontWeight.Bold };
                }
                else
                {
                    styleSpan = new Span { FontStyle = FontStyle.Italic };
                }
            }
            else if (emphasis.DelimiterChar == '~')
            {
                styleSpan = new Span { TextDecorations = TextDecorations.Strikethrough };
            }

            foreach (var child in emphasis)
            {
                foreach (var inlineChild in ConvertInline(child, parentTextBlock))
                {
                    if (styleSpan != null)
                    {
                        styleSpan.Inlines.Add(inlineChild);
                    }
                    else
                    {
                        yield return inlineChild;
                    }
                }
            }

            if (styleSpan != null)
            {
                yield return styleSpan;
            }
        }
        else if (inline is LinkInline link)
        {
            if (link.IsImage)
            {
                yield return new Run { Text = $"[图片: {link.Url}]" };
            }
            else
            {
                var linkSpan = new Span { Foreground = GetAccentBrush(), TextDecorations = TextDecorations.Underline };
                foreach (var child in link)
                {
                    foreach (var inlineChild in ConvertInline(child, parentTextBlock))
                    {
                        linkSpan.Inlines.Add(inlineChild);
                    }
                }

                if (parentTextBlock != null)
                {
                    parentTextBlock.Cursor = new Cursor(StandardCursorType.Hand);
                    parentTextBlock.PointerPressed += (sender, e) =>
                    {
                        OpenLink(link.Url);
                    };
                }

                yield return linkSpan;
            }
        }
        else if (inline is LineBreakInline)
        {
            yield return new LineBreak();
        }
        else if (inline is ContainerInline container)
        {
            foreach (var child in container)
            {
                foreach (var inlineChild in ConvertInline(child, parentTextBlock))
                {
                    yield return inlineChild;
                }
            }
        }
        else
        {
            yield return new Run { Text = inline.ToString() };
        }
    }

    private void OpenLink(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
        }
    }

    private Control ConvertCodeBlock(CodeBlock code)
    {
        var isDark = ThemeHelper.IsDarkTheme();
        var border = new Border
        {
            Background = isDark ? new SolidColorBrush(Color.Parse("#2d2d2d")) : new SolidColorBrush(Color.Parse("#f4f4f4")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 4)
        };

        var textBlock = new TextBlock
        {
            Text = code.Lines.ToString(),
            FontSize = 12,
            Foreground = ThemeHelper.GetTextBrush(),
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            TextWrapping = TextWrapping.NoWrap,
            Padding = new Thickness(0)
        };

        var scrollViewer = new ScrollViewer
        {
            Content = textBlock,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        border.Child = scrollViewer;
        return border;
    }

    private Control ConvertQuoteBlock(QuoteBlock quote)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 4, 0, 4)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftBorder = new Border
        {
            Background = GetAccentBrush()
        };
        Grid.SetColumn(leftBorder, 0);
        grid.Children.Add(leftBorder);

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };

        foreach (var block in quote)
        {
            var element = ConvertMarkdownBlock(block);
            if (element != null)
            {
                panel.Children.Add(element);
            }
        }

        Grid.SetColumn(panel, 1);
        grid.Children.Add(panel);

        return grid;
    }

    private Control ConvertListBlock(ListBlock list)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16, 4, 0, 4),
            Spacing = 4
        };

        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var marker = new TextBlock
                {
                    Text = list.IsOrdered ? $"{listItem.Order}." : "•",
                    FontSize = 16,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(marker, 0);

                var contentPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4,
                    Margin = new Thickness(8, 0, 0, 0)
                };

                foreach (var block in listItem)
                {
                    var element = ConvertMarkdownBlock(block);
                    if (element != null)
                    {
                        contentPanel.Children.Add(element);
                    }
                }
                Grid.SetColumn(contentPanel, 1);

                itemGrid.Children.Add(marker);
                itemGrid.Children.Add(contentPanel);
                panel.Children.Add(itemGrid);
            }
        }

        return panel;
    }

    private Control ConvertTableBlock(Markdig.Extensions.Tables.Table table)
    {
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 4, 0, 4)
        };

        var border = new Border
        {
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(1)
        };

        var tablePanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        var columnCount = 0;
        foreach (var row in table)
        {
            if (row is Markdig.Extensions.Tables.TableRow tableRow)
            {
                columnCount = Math.Max(columnCount, tableRow.Count);
            }
        }

        foreach (var row in table)
        {
            if (row is Markdig.Extensions.Tables.TableRow tableRow)
            {
                var rowGrid = new Grid();

                for (int i = 0; i < columnCount; i++)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                var cellIndex = 0;
                foreach (var cell in tableRow)
                {
                    if (cell is Markdig.Extensions.Tables.TableCell tableCell)
                    {
                        var cellBorder = new Border
                        {
                            BorderBrush = ThemeHelper.GetSeparatorBrush(),
                            BorderThickness = new Thickness(0, 0, 1, 1),
                            Padding = new Thickness(8, 6)
                        };

                        var cellContentPanel = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Spacing = 2
                        };

                        foreach (var block in tableCell)
                        {
                            var element = ConvertMarkdownBlock(block);
                            if (element != null)
                            {
                                if (tableRow.IsHeader && element is TextBlock tb)
                                {
                                    tb.Foreground = Brushes.White;
                                }
                                cellContentPanel.Children.Add(element);
                            }
                        }

                        cellBorder.Child = cellContentPanel;
                        Grid.SetColumn(cellBorder, cellIndex);
                        rowGrid.Children.Add(cellBorder);
                        cellIndex++;
                    }
                }

                if (tableRow.IsHeader)
                {
                    var headerBorder = new Border
                    {
                        Background = ThemeHelper.GetLightBlueBrush(),
                        CornerRadius = new CornerRadius(3, 3, 0, 0)
                    };
                    headerBorder.Child = rowGrid;
                    tablePanel.Children.Add(headerBorder);
                }
                else
                {
                    tablePanel.Children.Add(rowGrid);
                }
            }
        }

        border.Child = tablePanel;
        scrollViewer.Content = border;
        return scrollViewer;
    }

    protected virtual void OnBackClick(object? sender, PointerPressedEventArgs e)
    {
        FluentAvaloniaCompatibilityHelper.NavigateBack(this);
    }

    protected void AddSection(StackPanel panel, string text, double fontSize, FontWeight fontWeight, IBrush foreground)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap
        };
        _sectionTextBlocks?.Add(textBlock);
        panel.Children.Add(textBlock);
    }

    protected void AddParagraph(StackPanel panel, string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        _paragraphTextBlocks?.Add(textBlock);
        panel.Children.Add(textBlock);
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

    protected virtual void UpdateThemeColors()
    {
        if (_contentBorder != null)
            _contentBorder.Background = ThemeHelper.GetHanfuBackgroundBrush();

        if (_backTextBlock != null)
            _backTextBlock.Foreground = GetAccentBrush();

        if (_paragraphTextBlocks != null)
        {
            foreach (var tb in _paragraphTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetSubTextBrush();
            }
        }

        if (_sectionTextBlocks != null)
        {
            foreach (var tb in _sectionTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetLightBlueBrush();
            }
        }
    }
}