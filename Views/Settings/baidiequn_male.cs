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

[SettingsPageInfo("AdvancedTimeIslandBaiDieQunMale", "宋制百迭裙（男性款）", true, SettingsPageCategory.Debug)]
public class BaiDieQunMalePage : SettingsPageBase
{
    protected Border? _contentBorder;
    protected List<TextBlock>? _paragraphTextBlocks;
    protected List<TextBlock>? _sectionTextBlocks;
    protected TextBlock? _backTextBlock;

    public BaiDieQunMalePage()
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
        var markdown = @"# 宋制百迭裙（男性款）


## 一、历史沿革

百迭裙并非女性专属，宋代男性同样穿着百迭类裙装，是男性日常服饰体系的组成部分。浙江黄岩南宋赵伯澐墓出土的男性素纱百迭裙实物，直接佐证了南宋时期男性百迭裙的真实存在，墓主为南宋宗室子弟，其裙装形制代表了宋代士族男性的服饰标准。


宋代男性百迭裙多作为内层衬裙或日常便服使用，在士族与平民阶层均有普及，整体风格贴合宋代男性服饰简约内敛的审美特征。


## 二、形制结构

宋制男性百迭裙同为**一片式围合裙**，核心结构与女款同源，均采用""两侧光面、中间打褶""的基础设计，整体风格简约庄重。


### 基础构成

- **裙头**：双层加固腰头，两端缝制系带，用于围合系结固定。

- **光面与褶裥区**：裙身两侧保留平整光面，中间区域打制规整褶裥，褶型以顺褶为主。

- **裙身与裙摆**：裙身垂坠感强，形制规整，契合宋代男性服饰的内敛审美。


### 常见分类

1. **按褶型划分**

    - **顺褶百迭裙**：目前出土男性实物均为顺褶形制，褶子朝同一方向倒伏，规整简约，是男性百迭裙的主流款式。

2. **按围合方式划分**

    - **合围式百迭裙**：裙头长度适配腰围，围合后前后相接，多作为内层衬裙使用。

    - **交叠式百迭裙**：裙身两端重叠围合，包裹性更强，非正式场合可单独外穿。


### 文物参考数据

以赵伯澐墓出土素纱百迭裙为例：裙长93.5厘米，腰宽41厘米，裙身两端为光面，中间打有13道顺褶，单褶宽约4厘米，面料为素色纱质，无额外装饰。


## 三、应用场景

### 宋代历史场景

1. **内层衬裙**：多穿着于袍、衫之内，作为内衣层次，起到保暖、规整外衣形态的作用，是士族男性日常着装的标配内层。

2. **日常便服**：居家、非正式出行场景中，可单穿搭配短衫，作为闲适便服使用。

3. **礼仪辅助**：在部分礼仪场合中，作为礼服内层搭配，完善着装层次。


### 现代应用场景

1. **汉服礼仪穿搭**：搭配宋制圆领袍、交领衫，作为男性宋制汉服的内层下装，还原宋代男性着装层次。

2. **文化活动穿着**：在传统礼仪、汉服文化活动中，作为男性宋制造型的组成部分，体现服饰形制的完整性。


## 四、穿戴方法

宋制男性百迭裙以围合系结方式穿着，多作为内搭使用，标准穿戴步骤如下：

1. **定位围合**：将裙腰对齐腰部，一端贴紧腰身，另一端绕身一周覆盖重合。

2. **系带固定**：将两侧系带在腰侧或身后打活结固定，调整松紧使裙身平整垂顺。

3. **外层搭配**：整理裙身褶裥后，外穿袍、衫等上衣，裙身仅下摆部分外露即可。


## 五、注意事项

### 形制区分要点

男性百迭裙与女款核心结构一致，差异主要在装饰风格与整体比例：男款风格极简、多为素面无装饰，女款可承载更多纹样与工艺，二者风格定位不同。


### 洗护保养

1. 素面罗、纱材质优先选择轻柔手洗或专业干洗，避免强力揉搓损伤面料纤维。

2. 晾晒时自然垂挂，保持褶裥顺直，收纳以悬挂为佳，避免重压导致褶型散乱。


### 穿着注意

1. 传统男性百迭裙多为内搭，穿着时需注意与外层袍衫的长度适配，保持着装层次协调。

2. 围合松紧需适度，既保证活动便利，也避免裙身移位影响整体垂坠效果。


## 六、参考来源

1. `https://www.zjtz.gov.cn/col/col1229506201/art/2026/art_8a51f8f9ec692c6cbd3de34a67989a24.html` 

2. `https://qjwb.tidenews.com.cn/html/2026-03/29/content_4473673.htm?div=1` 

3. `https://www.click2macao.com/2022/05/04/hfxzjsss/`";

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
        else if (inline is Markdig.Syntax.Inlines.CodeInline code)
        {
            var codeText = code.Content.ToString();
            var isUrl = codeText.StartsWith("http://") || codeText.StartsWith("https://");
            
            if (isUrl)
            {
                var linkSpan = new Span { Foreground = GetAccentBrush(), TextDecorations = TextDecorations.Underline };
                linkSpan.Inlines.Add(new Run { Text = codeText });
                inlines.Add(linkSpan);
            }
            else
            {
                var isDark = ThemeHelper.IsDarkTheme();
                var codeSpan = new Span
                {
                    Background = isDark ? new SolidColorBrush(Color.Parse("#3c3c3c")) : new SolidColorBrush(Color.Parse("#f0f0f0")),
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 13
                };
                codeSpan.Inlines.Add(new Run { Text = " " + codeText + " " });
                inlines.Add(codeSpan);
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
        else if (inline is Markdig.Syntax.Inlines.CodeInline code)
        {
            var codeText = code.Content.ToString();
            var isUrl = codeText.StartsWith("http://") || codeText.StartsWith("https://");
            
            if (isUrl)
            {
                var linkSpan = new Span { Foreground = GetAccentBrush(), TextDecorations = TextDecorations.Underline };
                linkSpan.Inlines.Add(new Run { Text = codeText });
                
                if (parentTextBlock != null)
                {
                    parentTextBlock.Cursor = new Cursor(StandardCursorType.Hand);
                    parentTextBlock.PointerPressed += (sender, e) =>
                    {
                        OpenLink(codeText);
                    };
                }
                
                yield return linkSpan;
            }
            else
            {
                var isDark = ThemeHelper.IsDarkTheme();
                var codeSpan = new Span
                {
                    Background = isDark ? new SolidColorBrush(Color.Parse("#3c3c3c")) : new SolidColorBrush(Color.Parse("#f0f0f0")),
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 13
                };
                codeSpan.Inlines.Add(new Run { Text = " " + codeText + " " });
                yield return codeSpan;
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