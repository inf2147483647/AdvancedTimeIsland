using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 专业名词解释页面
/// 使用Markdown格式展示名词解释
/// </summary>
public class GlossaryPage : UserControl
{
    public GlossaryPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
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
            Text = "专业名词解释",
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.DodgerBlue,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Markdown 文档
        var markdownContent = @"## Unix时间戳

世界统一的时间，是指格林威治时间1970年01月01日00时00分00秒（北京时间1970年01月01日08时00分00秒）起至现在的总秒数（不考虑**闰秒**），它的核心作用是确保唯一性和顺序：在计算机系统中，它能精确记录事件发生的时刻，并且因为时间一直向前，每个时间戳都是独一无二的。

**例如**：北京时间2026-6-6 12:00:00对应的时间戳为 `1780718400`

---

## 闰秒

是指为保持协调世界时接近于世界时时刻，由国际计量局统一规定在年底或年中（也可能在季末）对协调世界时增加或减少1秒的调整。

由于地球自转的不均匀性和长期变慢性（主要由潮汐摩擦引起的），会使世界时（民用时）和原子时之间相差超过到±0.9秒时，就把协调世界时向前拨1秒（**负闰秒**，最后一分钟为59秒）或向后拨1秒（**正闰秒**，最后一分钟为61秒）；闰秒一般加在公历年末或公历六月末。

---

## 区时

**区时**是将地球表面按经度划分为24个时区后，每个时区中央经线的地方平太阳时，也就是该时区统一使用的标准时间。

为了解决各地时间不统一的问题，规定同一时区内大家都用同一个参考时间，相邻时区的区时相差一小时整，比如**北京时间**就是东八区的区时（东经120°的地方时）。

---

## 时区

**时区**是地球上按照经度划分的统一时间区域，旨在协调全球标准时间。地球自西向东自转，导致经度不同的两地之间的地方时也不同。

为解决全球各地时间差异造成的不便，将全世界按经线划分为24个使用同一时间的区域，称为时区。每个时区的经度跨度为**15°**，各时区以中央经线的地方平太阳时作为本时区的标准时间——区时，相邻时区的区时相差一小时整。

中国横跨五个时区，新中国成立后采取东八区区时（北京时间）作为国家标准时。

> **注意**：时区与区时是两个相关但不相同的概念。";

        mainPanel.Children.Add(CreateMarkdownSection(markdownContent));

        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    /// <summary>
    /// 解析 Markdown 文本为带样式的 StackPanel
    /// </summary>
    private Border CreateMarkdownSection(string markdownText)
    {
        var section = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(6)
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10
        };

        var lines = markdownText.Split('\n');
        var paragraphBuffer = new System.Collections.Generic.List<string>();

        void FlushParagraph()
        {
            if (paragraphBuffer.Count == 0) return;
            var paragraphText = string.Join(" ", paragraphBuffer).Trim();
            paragraphBuffer.Clear();
            if (string.IsNullOrWhiteSpace(paragraphText)) return;

            content.Children.Add(BuildInlineTextBlock(paragraphText, 13, Brushes.LightGray, false));
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                content.Children.Add(new Border { Height = 4 });
                continue;
            }

            if (line.StartsWith("## "))
            {
                FlushParagraph();
                content.Children.Add(new TextBlock
                {
                    Text = line.Substring(3).Trim(),
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.DodgerBlue,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 12, 0, 4)
                });
                continue;
            }

            if (line.StartsWith("### "))
            {
                FlushParagraph();
                content.Children.Add(new TextBlock
                {
                    Text = line.Substring(4).Trim(),
                    FontSize = 15,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.LightBlue,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 2)
                });
                continue;
            }

            if (line.StartsWith("---"))
            {
                FlushParagraph();
                content.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#555555")),
                    Margin = new Thickness(0, 8, 0, 8)
                });
                continue;
            }

            if (line.StartsWith("> "))
            {
                FlushParagraph();
                var quotePanel = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.Parse("#1E90FF")),
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(10, 6, 6, 6),
                    Margin = new Thickness(0, 4, 0, 4),
                    Background = new SolidColorBrush(Color.Parse("#252528")),
                    Child = BuildInlineTextBlock(line.Substring(2).Trim(), 13, Brushes.Khaki, false)
                };
                content.Children.Add(quotePanel);
                continue;
            }

            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                FlushParagraph();
                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                itemPanel.Children.Add(new TextBlock
                {
                    Text = "•",
                    FontSize = 13,
                    Foreground = Brushes.DodgerBlue,
                    FontWeight = FontWeight.Bold
                });
                itemPanel.Children.Add(BuildInlineTextBlock(line.Substring(2).Trim(), 13, Brushes.LightGray, false));
                content.Children.Add(itemPanel);
                continue;
            }

            paragraphBuffer.Add(line.Trim());
        }

        FlushParagraph();

        section.Child = content;
        return section;
    }

    /// <summary>
    /// 构建支持 Markdown 内联格式的 TextBlock（支持 **bold** 和 `code`）
    /// </summary>
    private TextBlock BuildInlineTextBlock(string text, double fontSize, IBrush defaultBrush, bool isItalic)
    {
        var textBlock = new TextBlock
        {
            FontSize = fontSize,
            Foreground = defaultBrush,
            TextWrapping = TextWrapping.Wrap,
            FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal
        };

        var runs = new System.Collections.Generic.List<Run>();
        int i = 0;
        while (i < text.Length)
        {
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2);
                if (end > i)
                {
                    var content = text.Substring(i + 2, end - i - 2);
                    runs.Add(new Run
                    {
                        Text = content,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.Gold
                    });
                    i = end + 2;
                    continue;
                }
            }

            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    var content = text.Substring(i + 1, end - i - 1);
                    runs.Add(new Run
                    {
                        Text = content,
                        FontFamily = new FontFamily("Consolas"),
                        Foreground = Brushes.LightGreen
                    });
                    i = end + 1;
                    continue;
                }
            }

            int next = text.Length;
            for (int j = i; j < text.Length - 1; j++)
            {
                if ((text[j] == '*' && text[j + 1] == '*') || text[j] == '`')
                {
                    next = j;
                    break;
                }
            }
            var plainText = text.Substring(i, next - i);
            runs.Add(new Run { Text = plainText, Foreground = defaultBrush });
            i = next;
        }

        foreach (var run in runs)
        {
            if (textBlock.Inlines != null)
            {
                textBlock.Inlines.Add(run);
            }
        }

        if (textBlock.Inlines == null || textBlock.Inlines.Count == 0)
        {
            var combined = "";
            foreach (var run in runs)
            {
                combined += run.Text;
            }
            textBlock.Text = combined;
        }

        return textBlock;
    }
}
