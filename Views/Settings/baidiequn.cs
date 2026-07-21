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

[SettingsPageInfo("AdvancedTimeIslandBaiDieQun", "宋制百迭裙", true, SettingsPageCategory.Debug)]
public class BaiDieQunPage : SettingsPageBase
{
    protected Border? _contentBorder;
    protected List<TextBlock>? _paragraphTextBlocks;
    protected List<TextBlock>? _sectionTextBlocks;
    protected TextBlock? _backTextBlock;

    public BaiDieQunPage()
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
            Margin = new Thickness(16, 12, 16, 0)
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
        var markdown = @"# 宋制百迭裙（女性款）


## 一、历史沿革

百迭裙是宋代女性最具代表性的下裙形制之一，后世常以苏轼《梦中赋裙带》中""百迭漪漪水皱""的诗句对应此类细密褶裥裙，百迭裙也因此得名。该形制在唐代多褶裙的基础上发展而来，继承晚唐五代遗风并形成宋代独有的审美特征。


北宋时期百迭裙已在士族女性阶层流行，至南宋发展至鼎盛，是宋代女性服饰体系中普及度极高的裙装款式。目前已发现的权威女性出土实物包括：

1. **福州南宋黄昇墓**：出土多件百迭裙实物，墓主为南宋宗族贵妇，其百迭裙多以罗为面料，辅以印金工艺，属贵族盛装形制。

2. **南京花山宋墓**：出土前短后长式女性百迭裙，为目前罕见的特殊形制文物。


## 二、形制结构

宋制女性百迭裙为**一片式围合裙**，核心结构特征为""两侧光面、中间打褶""，裙身整体呈上窄下宽的梯形，平铺形态类似折扇扇面。


### 基础构成

- **裙头**：单条完整腰头，多为双层布料加固，两端缝有系带，用于围合固定。

- **光面（裙门）**：裙身左右两端各保留一段不打褶的平整面料，是百迭裙的标志性识别特征。

- **褶裥区**：两个光面之间的裙身全部打褶，褶子方向统一、排列规整，是""百迭""名称的核心来源。

- **裙摆**：下摆自然散开，因褶裥撑开形成宽松余量，行走时灵动飘逸。


### 常见分类

1. **按褶型划分**

    - **顺褶百迭裙**：褶子朝同一方向倒伏，是最主流、出土文物最多的款式，日常穿着最为普遍。

    - **工字褶百迭裙**：褶子正反交替折叠，成型后呈""工""字形，立体感更强，黄昇墓出土实物即有此类褶型。

2. **按围合方式划分**

    - **合围式百迭裙**：裙头长度约等于腰围，围合后前后相接无重叠，搭配裆裤穿着，是宋代日常款。

    - **交叠式百迭裙**：裙身两端有重叠区域，包裹性更强，防走光效果更好。

3. **按特殊形制划分**

    - **前短后长百迭裙**：仅花山宋墓有女性实物出土，裙身分三片，中间裙片较长且打满细褶，两侧短片叠合为后片。


### 文物工艺特征

黄昇墓出土的女性百迭裙采用轻薄罗质面料，印有金色团花纹饰，褶裥细密规整，印金工艺精致，体现了南宋贵族女性服饰清雅秀丽的审美与工艺水平。


## 三、应用场景

### 宋代历史场景

1. **贵族盛装**：高端百迭裙以罗、纱等轻薄面料制作，辅以印金、刺绣等工艺，搭配大袖衫、霞帔，是宋代贵族女性出席礼仪场合、宴会的正式装束，黄昇墓出土的百迭裙即为下葬时的盛装礼服。

2. **日常起居**：普通材质的百迭裙是宋代各阶层女性的日常下装，可搭配衫、袄、褙子等上衣，适配居家、出行等多种场景。


### 现代应用场景

1. **汉服日常穿搭**：是宋制汉服的基础女款，搭配宋抹、对襟衫、飞机袖短衫，风格清雅简约，适配通勤、逛街、约会等日常场景。

2. **礼仪与文化活动**：可搭配长褙子、大袖衫，作为传统节日、汉服文化活动、中式礼仪场合的女性着装。

3. **新中式混搭**：可单独作为半身裙，搭配衬衫、吊带、针织开衫等现代服饰，融入日常穿搭体系。


## 四、穿戴方法

宋制女性百迭裙为一片式围合穿着，无固定正反，可根据审美选择光面或褶区朝前，标准穿戴步骤如下：

1. **定位裙身**：将裙子展开，选定正面位置，裙腰对齐腰线位置。

2. **初步固定**：将一侧系带从身后绕至对侧，根据自身腰围调整松紧。

3. **围合整理**：将另一侧裙身从身后环绕，覆盖住第一层裙身，整理裙腰使其平整，避免歪斜。

4. **系带打结**：将两侧系带在身前或身侧打活结，确保裙身不滑落。

5. **细节调整**：整理裙摆褶裥，使其垂顺整齐；搭配宋抹、衬裤等内搭后再外穿上衣即可。


## 五、注意事项

### 形制区分误区

需注意与明制马面裙的核心差异，避免形制混淆：

- 百迭裙仅2个光面，中间为连续顺褶，整体为一片式结构；

- 马面裙有4个光面（前后裙门），两侧为相向的合抱褶，裙身有重叠交叠结构，二者分属不同时代的独立形制。


### 洗护保养

1. **洗涤方式**：优先选择冷水手洗，使用中性洗涤剂，轻柔按压清洁，严禁搓揉褶裥部位；若机洗必须装入细密洗衣袋，选择轻柔模式。

2. **水温控制**：水温不超过30℃，禁止热水浸泡，高温会破坏褶裥定型、导致面料褪色与纤维损伤。

3. **晾晒收纳**：洗后切勿拧干，带水悬挂于阴凉通风处自然晾干，利用重力保持褶型；禁止暴晒。收纳时优先悬挂存放，避免折叠重压导致褶线变形。

4. **特殊面料**：印金、刺绣、真丝类高端百迭裙建议专业干洗，避免水洗破坏工艺与面料。


### 穿着注意

1. 传统宋制百迭裙面料多轻薄通透，建议搭配衬裤、衬裙穿着，避免走光同时保持裙型挺括。

2. 围合穿着时需调整好裙腰松紧，过松易滑落，过紧会导致褶裥挤压变形，影响美观。

3. 避免长时间久坐挤压褶裥，起身时可轻整理裙摆，保持褶型规整。


## 六、参考来源

1. `http://wwj.wlt.fj.gov.cn/xwzx/wbyw/202411/t20241106_6562830.htm` 

2. `http://kejiao.cntv.cn/history/guobaodangan/classpage/video/20110907/101089.shtml` 

3. `https://rw.zjnu.edu.cn/2025/0428/c2012a516565/page.htm` 

4. `https://culture.ifeng.com/c/8Xfczh0LXn8` 

5. `https://tidenews.com.cn/news.html?id=3439006` 

6. `https://www.click2macao.com/2022/05/04/hfxzjsss/`";

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
                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Top
                };

                var marker = new TextBlock
                {
                    Text = list.IsOrdered ? $"{listItem.Order}." : "•",
                    FontSize = 16,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 24
                };

                var contentPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4
                };

                foreach (var block in listItem)
                {
                    var element = ConvertMarkdownBlock(block);
                    if (element != null)
                    {
                        contentPanel.Children.Add(element);
                    }
                }

                itemPanel.Children.Add(marker);
                itemPanel.Children.Add(contentPanel);
                panel.Children.Add(itemPanel);
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