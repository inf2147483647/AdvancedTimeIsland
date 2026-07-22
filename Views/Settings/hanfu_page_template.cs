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

[SettingsPageInfo("AdvancedTimeIslandHanfuTemplate", "汉服页面模板", true, SettingsPageCategory.Debug)]
public class HanfuPageTemplate : SettingsPageBase
{
    protected Border? _contentBorder;
    protected List<TextBlock>? _paragraphTextBlocks;
    protected List<TextBlock>? _sectionTextBlocks;
    protected TextBlock? _backTextBlock;

    public HanfuPageTemplate()
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
        var markdown = @"**支持的 Markdown 语法：**

> 本示例魔改自 http://leanote.leanote.com/post/markdown-source-code。

# Welcome to ClassIsland! 欢迎来到ClassIsland!

## 1. 排版

**粗体** *斜体*

~~这是一段错误的文本。~~

引用:

> 123123123123

有序列表:
 1. 支持Vim
 2. 支持Emacs

无序列表:

 - 项目1
 - 项目2

## 2. 图片与链接

网络图片:
![banner](https://raw.gitcode.com/inf2147483647/PicBed/raw/main/DSC02575.jpg)

WPF 资源图片：

![1690356161339](pack://application:,,,/ClassIsland;component/Assets/AppLogo.png)

链接:[AdvancedTimeIsland项目主页](https://github.com/inf2147483647/AdvancedTimeIsland)

## 3. 标题

以下是各级标题, 最多支持6级标题

# h1：28px一级标题
## h2：21px二级标题
### h3：16px三级标题
#### h4：14px四级标题
##### h5：12px五级标题
###### h6：9px六级标题

## 4. 代码

示例:

    function get(key) {
        return m[key];
    }

代码高亮示例:

```javascript
/**
* nth element in the fibonacci series.
* @param n >= 0
* @return the nth element, >= 0.
*/
function fib(n) {
  var a = 1, b = 1;
  var tmp;
  while (--n >= 0) {
    tmp = a;
    a += b;
    b = tmp;
  }
  return a;
}

document.write(fib(10));
```

```python
class Employee:
   empCount = 0

    def __init__(self, name, salary):
         self.name = name
         self.salary = salary
         Employee.empCount += 1
```

# 5. Markdown 扩展

Markdown 扩展支持:

* 表格

## 5.1 表格

表格示例（简化显示）:

Hanfu | Photo Count
-------- | ---
1 | 5
2 | 4
3 | 6";

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
            yield return new Run { Text = $"[{inline.GetType().Name}]" };
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
                                if (element is TextBlock tb)
                                {
                                    tb.TextWrapping = TextWrapping.Wrap;
                                    if (tableRow.IsHeader)
                                    {
                                        tb.Foreground = Brushes.White;
                                    }
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