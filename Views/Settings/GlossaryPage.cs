using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Views.Settings;

public class GlossaryPage : UserControl
{
    private List<TextBlock>? _paragraphTextBlocks;
    private List<TextBlock>? _heading2TextBlocks;
    private List<TextBlock>? _heading3TextBlocks;
    private List<TextBlock>? _listItemTextBlocks;
    private List<Border>? _quoteBorders;
    private List<TextBlock>? _quoteTextBlocks;
    private Border? _markdownSectionBorder;
    private List<Border>? _separatorBorders;

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

        mainPanel.Children.Add(new TextBlock
        {
            Text = "专业名词解释",
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Foreground = GetAccentBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });

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

> **注意**：时区与区时是两个相关但不相同的概念。

---

## 帧率（FPS）

每秒屏幕呈现的画面张数，数值越高，画面动作越连贯流畅。比如60帧就是1秒内播放60张画面，和快速翻漫画的原理一致，翻得越快动作越顺滑。

---

## 1% Low 帧率

统计一段时间内所有画面的渲染速度，取最慢的1%位置并计算其平均帧率，代表画面最卡顿时刻的流畅下限。

---

## 汉服

全称""汉民族传统服饰""，是汉族流传数千年的传统服饰体系，**并非单指汉朝的衣服**。

它以衣襟向右掩（交领右衽）、系带固定为典型特征，款式随朝代发展演变出多种样式，齐胸襦裙、明制袄裙、圆领袍等都属于经典形制，如今是传统文化复兴的重要符号，日常与节日场合都有人穿着。

---

## 交领右衽

汉服最核心的标志性形制，可拆开理解：

- **交领**：左右两片衣襟在胸前交叉，在第二人称下领口呈现""y""字形。
- **右衽**：穿衣时左侧衣襟压住右侧衣襟，衣襟开口朝向人体右侧，最终在右腋下系带固定。";

        mainPanel.Children.Add(CreateMarkdownSection(markdownContent));

        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    private Border CreateMarkdownSection(string markdownText)
    {
        _paragraphTextBlocks = new List<TextBlock>();
        _heading2TextBlocks = new List<TextBlock>();
        _heading3TextBlocks = new List<TextBlock>();
        _listItemTextBlocks = new List<TextBlock>();
        _quoteBorders = new List<Border>();
        _quoteTextBlocks = new List<TextBlock>();

        var section = new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(6)
        };
        _markdownSectionBorder = section;

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

            var tb = BuildInlineTextBlock(paragraphText, 13, ThemeHelper.GetSubTextBrush(), false);
            _paragraphTextBlocks.Add(tb);
            content.Children.Add(tb);
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
                var h2 = new TextBlock
                {
                    Text = line.Substring(3).Trim(),
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Foreground = GetAccentBrush(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 12, 0, 4)
                };
                _heading2TextBlocks.Add(h2);
                content.Children.Add(h2);
                continue;
            }

            if (line.StartsWith("### "))
            {
                FlushParagraph();
                var h3 = new TextBlock
                {
                    Text = line.Substring(4).Trim(),
                    FontSize = 15,
                    FontWeight = FontWeight.Bold,
                    Foreground = ThemeHelper.GetLightBlueBrush(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 2)
                };
                _heading3TextBlocks.Add(h3);
                content.Children.Add(h3);
                continue;
            }

            if (line.StartsWith("---"))
            {
                FlushParagraph();
                var sep = new Border
                {
                    Height = 1,
                    Background = ThemeHelper.GetSeparatorBrush(),
                    Margin = new Thickness(0, 8, 0, 8)
                };
                _separatorBorders?.Add(sep);
                content.Children.Add(sep);
                continue;
            }

            if (line.StartsWith("> "))
            {
                FlushParagraph();
                var accentBrush = GetAccentBrush();
                var quotePanel = new Border
                {
                    BorderBrush = accentBrush is SolidColorBrush sb ? sb : new SolidColorBrush(Color.Parse("#1E90FF")),
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(10, 6, 6, 6),
                    Margin = new Thickness(0, 4, 0, 4),
                    Background = ThemeHelper.GetQuoteBackgroundBrush(),
                    Child = BuildInlineTextBlock(line.Substring(2).Trim(), 13, ThemeHelper.GetYellowBrush(), false)
                };
                _quoteBorders.Add(quotePanel);
                _quoteTextBlocks.Add((TextBlock)quotePanel.Child);
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
                    Foreground = GetAccentBrush(),
                    FontWeight = FontWeight.Bold
                });
                var itemText = BuildInlineTextBlock(line.Substring(2).Trim(), 13, ThemeHelper.GetSubTextBrush(), false);
                _listItemTextBlocks.Add(itemText);
                itemPanel.Children.Add(itemText);
                content.Children.Add(itemPanel);
                continue;
            }

            paragraphBuffer.Add(line.Trim());
        }

        FlushParagraph();

        section.Child = content;
        return section;
    }

    private TextBlock BuildInlineTextBlock(string text, double fontSize, IBrush defaultBrush, bool isItalic)
    {
        var cleanText = text.Replace("**", "").Replace("`", "");

        return new TextBlock
        {
            Text = cleanText,
            FontSize = fontSize,
            Foreground = defaultBrush,
            TextWrapping = TextWrapping.Wrap,
            FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal
        };
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

    private void UpdateThemeColors()
    {
        if (_markdownSectionBorder != null)
            _markdownSectionBorder.Background = ThemeHelper.GetCardBackgroundBrush();

        if (_separatorBorders != null)
        {
            foreach (var border in _separatorBorders)
            {
                border.Background = ThemeHelper.GetSeparatorBrush();
            }
        }

        if (_quoteBorders != null)
        {
            foreach (var border in _quoteBorders)
            {
                border.Background = ThemeHelper.GetQuoteBackgroundBrush();
            }
        }

        if (_paragraphTextBlocks != null)
        {
            foreach (var tb in _paragraphTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetSubTextBrush();
            }
        }
        if (_heading3TextBlocks != null)
        {
            foreach (var tb in _heading3TextBlocks)
            {
                tb.Foreground = ThemeHelper.GetLightBlueBrush();
            }
        }
        if (_listItemTextBlocks != null)
        {
            foreach (var tb in _listItemTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetSubTextBrush();
            }
        }
        if (_quoteTextBlocks != null)
        {
            foreach (var tb in _quoteTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetYellowBrush();
            }
        }
    }
}