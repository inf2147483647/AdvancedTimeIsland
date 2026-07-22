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

[SettingsPageInfo("AdvancedTimeIslandTieliMingStyle", "贴里明制", true, SettingsPageCategory.Debug)]
public class TieliMingStylePage : SettingsPageBase
{
    protected Border? _contentBorder;
    protected List<TextBlock>? _paragraphTextBlocks;
    protected List<TextBlock>? _sectionTextBlocks;
    protected TextBlock? _backTextBlock;

    public TieliMingStylePage()
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
        var markdown = @"# 明制贴里·女装

## 一、核心定位
贴里**并非明代女性的主流常规服饰**，不属于明代女性冠服制度的标准形制；仅在特定场景下存在女性穿着案例，属于非主流的特例穿着，未形成独立的女性贴里服饰体系。

## 二、文献与形象佐证
1. **文献记载**：明代正史、官方冠服典制中，无针对女性的贴里服用制度与品级规定；《酌中志》《明宫史》等记载贴里的一手史料，均指向男性群体。
2. **形象与实物**：
   - 现存明代女性人物画像、壁画中，穿贴里的女性形象多为女扮男装角色、侍女杂役，而非女性正装形象。
   - 目前国内已发掘的明代女性墓葬中，极少出土贴里实物，无成体系的女性贴里墓葬实证。
3. **学术界定**：服饰史学术研究普遍将贴里归入明代男子服饰体系，仅在""女扮男装""""侍女服饰""的细分研究中提及女性穿着案例。

## 三、女性穿着的主要场景
1. **女扮男装**：是女性穿贴里最常见的场景，多见于文学作品记载、戏曲形象，以及部分特殊身份女性的便服穿搭，属于功能性、模仿性穿着。
2. **侍从劳作**：明代宫廷、贵族府邸中的下层侍女、杂役女性，因劳作便利需求，会穿着简化版的贴里类短款服饰，不属于正式女装范畴。

## 四、形制特点
女性穿着的贴里无专属定制形制，基本沿用男性贴里的结构逻辑：前后分裁、腰部打褶、大襟右衽；仅在衣长、袖型上根据穿着者身形适度调整，未发展出女性独有的版型、纹样制度与穿搭体系。

## 五、现代认知说明
1. 当前汉服市场中的""女款贴里""，多为现代商家基于审美需求开发的改良衍生款，并非对明代女性传统服饰的考据复原。
2. 若追求严格的形制考据，贴里的核心复原依据均为男性文物与制度，不建议将其作为明代女性的正统形制进行宣传与穿搭。
3. 明代女性的主流常服体系为交领袄/衫搭配马面裙，立领袄、比甲、披风等是明代女性的典型外搭服饰。";

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