// WPF 版时间表悬浮窗渲染器：FloatScheduleRenderModel → WPF 控件树。
// 为什么另写一份：Avalonia 版渲染器（Shared\FloatingSchedule\FloatScheduleRenderer.cs）依赖 Avalonia
//   程序集，无法跨进程共用（WPF 子进程不引用 Avalonia）。这里把它的"模型 → 控件"映射关系逐项照搬，
//   控件样式（自绘两层 Border 进度条 / 教师列 pt×21.5 系数 / 字号相对差值 表头-2 时间-1 教师-3 /
//   当前课只高亮单元格 / 明日课表不画高亮与进度条）与
//   AdvancedTimeIslandWPF\Services\FloatingScheduleService.cs 的 RefreshSchedule 保持一致。
// 约束：颜色全部来自模型（ARGB），不依赖任何主题资源——子进程没有 ClassIsland 主题上下文。
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdvancedTimeIsland.Shared.FloatingSchedule;

namespace AdvancedTimeIsland.FloatScheduleWpfChild;

/// <summary>渲染结果引用：进度条宿主/前景与分隔线控件，供窗口后续按帧更新进度。</summary>
internal sealed class FloatScheduleWpfProgressRefs
{
    /// <summary>当前课进度条承载容器（Grid，两 Star 列）。</summary>
    public Grid? ClassProgressHost { get; set; }
    /// <summary>当前课进度条前景（占 Column0）。</summary>
    public Border? ClassProgressIndicator { get; set; }
    /// <summary>课间进度条承载容器。</summary>
    public Grid? BreakProgressHost { get; set; }
    /// <summary>课间进度条前景。</summary>
    public Border? BreakProgressIndicator { get; set; }
    /// <summary>本次构建产生的档案分隔线控件。</summary>
    public List<Border> SeparatorLines { get; } = new();
}

internal static class FloatScheduleWpfRenderer
{
    // ===== 卡片外框规格（与 Avalonia 版渲染器一致）=====
    public const double CardCornerRadius = 8;
    public static readonly Thickness CardPadding = new(12, 10, 12, 10);
    public static readonly Thickness CardBorderThickness = new(1);

    /// <summary>列间距（WPF 的 Grid 没有 ColumnSpacing，用固定宽度的空白列等价 Avalonia 的 14）。</summary>
    private const double ColumnGap = 14;
    private const int ColumnCourse = 0;
    private const int ColumnGapIndex = 1;
    private const int ColumnTime = 2;

    /// <summary>把模型中的卡片背景/边框色应用到容器 Border。</summary>
    public static void ApplyCardStyle(Border card, FloatScheduleRenderModel m)
    {
        card.Background = new SolidColorBrush(FromArgb(m.CardBackgroundArgb));
        card.BorderBrush = new SolidColorBrush(FromArgb(m.BorderArgb));
    }

    /// <summary>0xAARRGGBB → WPF Color。</summary>
    public static Color FromArgb(uint argb) =>
        Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    /// <summary>WPF Color → 0xAARRGGBB。</summary>
    public static uint ToArgb(Color c) => ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;

    /// <summary>
    /// 构建课表内容控件树（作为卡片 Border 的 Child）。
    /// 返回根控件与进度条引用；不创建卡片 Border 本身。
    /// </summary>
    public static (FrameworkElement root, FloatScheduleWpfProgressRefs refs) Build(FloatScheduleRenderModel m)
    {
        var refs = new FloatScheduleWpfProgressRefs();
        // 字体：宿主可解析的字体族名（非嵌入资源）显式下传，保证两种模式字形一致；
        //   null 时不设置（由窗口层兜底 "HarmonyOS Sans SC, Microsoft YaHei UI"）。
        FontFamily? family = null;
        if (!string.IsNullOrWhiteSpace(m.FontFamilySource))
        {
            try { family = new FontFamily(m.FontFamilySource + ", HarmonyOS Sans SC, Microsoft YaHei UI"); }
            catch { family = null; }
        }
        void SetFamily(TextBlock tb)
        {
            if (family != null) tb.FontFamily = family;
        }

        int fontSize = m.FontSize;
        Color accentColor = FromArgb(m.AccentArgb);
        var accentBrush = new SolidColorBrush(accentColor);
        var textFg = new SolidColorBrush(FromArgb(m.TextArgb));
        var subFg = new SolidColorBrush(FromArgb(m.SubTextArgb));
        var sepBrush = new SolidColorBrush(FromArgb(m.BorderArgb));

        // ---- 空表占位模式（与 Avalonia 版一致：明天=标题+占位两行；今天=仅占位）----
        if (!string.IsNullOrWhiteSpace(m.PlaceholderText))
        {
            var noClass = new TextBlock
            {
                Text = m.PlaceholderText,
                FontSize = fontSize,
                Foreground = subFg,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 6, 10, 6)
            };
            SetFamily(noClass);
            if (m.ShowTomorrow)
            {
                var emptyPanel = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                emptyPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                emptyPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var emptyTitle = CreateTomorrowTitle(fontSize, accentBrush);
                SetFamily(emptyTitle);
                Grid.SetRow(emptyTitle, 0);
                emptyPanel.Children.Add(emptyTitle);
                Grid.SetRow(noClass, 1);
                emptyPanel.Children.Add(noClass);
                return (emptyPanel, refs);
            }
            return (noClass, refs);
        }

        var highlightBg = new SolidColorBrush(FromArgb(m.HighlightArgb));

        // 【明日课表】不参与"当前课高亮 / 课间行 / 进度条"：这些语义只对当天成立。
        //   与 Avalonia 版渲染器同样在此统一落地：即使上层给出"当前课索引"，明日课表也永不画高亮与进度条。
        bool isTomorrowList = m.ShowTomorrow;
        int currentClassIndex = isTomorrowList ? -1 : m.CurrentClassIndex;
        FloatScheduleBreakModel? currentBreak = isTomorrowList ? null : m.Break;

        var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                        // 课程列
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColumnGap) });              // 列间距
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });    // 时间列

        int AddRow()
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            return grid.RowDefinitions.Count - 1;
        }

        // 【明日时间表】标题行：同一 Grid 的第 0 行（跨全部列），保证今天/明天卡片宽度计算路径一致
        if (m.ShowTomorrow)
        {
            int titleRow = AddRow();
            var tomorrowTitle = CreateTomorrowTitle(fontSize, accentBrush);
            SetFamily(tomorrowTitle);
            Grid.SetRow(tomorrowTitle, titleRow);
            Grid.SetColumnSpan(tomorrowTitle, 3);
            Grid.SetColumn(tomorrowTitle, ColumnCourse);
            grid.Children.Add(tomorrowTitle);
        }

        // 表头行
        int headerRow = AddRow();
        var header1 = new TextBlock
        {
            Text = "课程",
            FontSize = Math.Max(8, fontSize - 2),
            FontWeight = FontWeights.Bold,
            Foreground = textFg,
            Margin = new Thickness(0, 0, 0, 4)
        };
        SetFamily(header1);
        Grid.SetRow(header1, headerRow);
        Grid.SetColumn(header1, ColumnCourse);
        grid.Children.Add(header1);
        var header2 = new TextBlock
        {
            Text = "时间",
            FontSize = Math.Max(8, fontSize - 2),
            FontWeight = FontWeights.Bold,
            Foreground = textFg,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 4)
        };
        SetFamily(header2);
        Grid.SetRow(header2, headerRow);
        Grid.SetColumn(header2, ColumnTime);
        grid.Children.Add(header2);

        // 表头下分隔线
        int headerSepRow = AddRow();
        var sepLine = new Border
        {
            Height = 1,
            Background = sepBrush,
            Margin = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(0.5)
        };
        Grid.SetRow(sepLine, headerSepRow);
        Grid.SetColumn(sepLine, ColumnCourse);
        Grid.SetColumnSpan(sepLine, 3);
        grid.Children.Add(sepLine);

        var breakSeparatorBrush = new SolidColorBrush(FromArgb(m.SeparatorArgb));

        // 课程行
        for (int i = 0; i < m.Rows.Count; i++)
        {
            var row = m.Rows[i];
            bool isCurrent = i == currentClassIndex;

            int rIdx = AddRow();

            Brush rowBg = Brushes.Transparent;
            if (isCurrent) rowBg = highlightBg;

            // ---- 课程列（左课程名 * / 右教师名 Auto）----
            var coursePanel = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
                Background = rowBg,
                Margin = new Thickness(0, 2, 0, 2)
            };
            coursePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            coursePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            coursePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(coursePanel, rIdx);
            Grid.SetColumn(coursePanel, ColumnCourse);

            var courseTb = new TextBlock
            {
                Text = row.Course,
                FontSize = fontSize,
                FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal,
                Foreground = textFg,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                Margin = new Thickness(0, 0, 8, 0)
            };
            SetFamily(courseTb);
            Grid.SetRow(courseTb, 0);
            Grid.SetColumn(courseTb, 0);
            coursePanel.Children.Add(courseTb);

            if (!string.IsNullOrEmpty(row.Teacher))
            {
                int teacherFontSize = Math.Max(8, fontSize - 3);
                // 教师列宽单一事实来源：teacherFontSize(pt) × 21.5（完整展示 15 汉字的实测系数，
                //  与 Avalonia 版渲染器、WPF 进程内实现完全一致）
                var teacherTb = new TextBlock
                {
                    Text = row.Teacher,
                    FontSize = teacherFontSize,
                    Foreground = subFg,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    MaxWidth = teacherFontSize * 21.5,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                SetFamily(teacherTb);
                Grid.SetRow(teacherTb, 0);
                Grid.SetColumn(teacherTb, 1);
                coursePanel.Children.Add(teacherTb);
            }

            grid.Children.Add(coursePanel);

            // 当前课：进度条放在课程行的下边缘（跨全部列）
            if (isCurrent)
            {
                int pIdx = AddRow();
                var (progHost, progInd) = CreateSelfDrawnProgressBar(accentColor, 2.5, new Thickness(0, 2, 0, 0));
                refs.ClassProgressHost = progHost;
                refs.ClassProgressIndicator = progInd;
                Grid.SetRow(progHost, pIdx);
                Grid.SetColumn(progHost, ColumnCourse);
                Grid.SetColumnSpan(progHost, 3);
                grid.Children.Add(progHost);
            }

            // ---- 时间列 ----
            var timeTb = new TextBlock
            {
                Text = row.TimeText,
                FontSize = Math.Max(8, fontSize - 1),
                Foreground = textFg,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Background = rowBg,
                Margin = new Thickness(0, 2, 0, 2)
            };
            SetFamily(timeTb);
            Grid.SetRow(timeTb, rIdx);
            Grid.SetColumn(timeTb, ColumnTime);
            grid.Children.Add(timeTb);

            // ---- 课间休息插入行（仅当 i == Break.AfterClassIndex）----
            if (currentBreak != null && i == currentBreak.AfterClassIndex)
            {
                int brIdx = AddRow();
                var breakBg = new SolidColorBrush(FromArgb(m.BreakRowBackgroundArgb));
                var breakTb = new TextBlock
                {
                    Text = currentBreak.Name,
                    FontSize = Math.Max(8, fontSize - 1),
                    Foreground = subFg,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                SetFamily(breakTb);
                var breakCellL = new Border { Background = breakBg, Child = breakTb };
                Grid.SetRow(breakCellL, brIdx);
                Grid.SetColumn(breakCellL, ColumnCourse);
                grid.Children.Add(breakCellL);

                var breakTimeTb = new TextBlock
                {
                    Text = currentBreak.TimeText,
                    FontSize = Math.Max(8, fontSize - 1),
                    Foreground = subFg,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                SetFamily(breakTimeTb);
                var breakCellR = new Border { Background = breakBg, Child = breakTimeTb };
                Grid.SetRow(breakCellR, brIdx);
                Grid.SetColumn(breakCellR, ColumnTime);
                grid.Children.Add(breakCellR);

                // 课间进度条：插入行下方，横跨全部列
                int pbIdx = AddRow();
                var (breakHost, breakInd) = CreateSelfDrawnProgressBar(accentColor, 2.5, new Thickness(0, 2, 0, 0));
                refs.BreakProgressHost = breakHost;
                refs.BreakProgressIndicator = breakInd;
                Grid.SetRow(breakHost, pbIdx);
                Grid.SetColumn(breakHost, ColumnCourse);
                Grid.SetColumnSpan(breakHost, 3);
                grid.Children.Add(breakHost);
            }

            // ---- 档案分隔线（第 i 行之后）----
            if (m.SeparatorAfterClassIndex.Contains(i))
            {
                int sepRowIdx = AddRow();
                var sepBorder = new Border
                {
                    Height = 2,
                    Background = breakSeparatorBrush,
                    Opacity = 0.7,   // 【分隔线】70% 不透明
                    Margin = new Thickness(0, 1, 0, 1)
                };
                Grid.SetRow(sepBorder, sepRowIdx);
                Grid.SetColumn(sepBorder, ColumnCourse);
                Grid.SetColumnSpan(sepBorder, 3);
                grid.Children.Add(sepBorder);
                refs.SeparatorLines.Add(sepBorder);
            }
        }

        return (grid, refs);
    }

    /// <summary>"明日时间表"标题标识（强调色加粗）。</summary>
    public static TextBlock CreateTomorrowTitle(int fontSize, Brush accentBrush)
    {
        return new TextBlock
        {
            Text = "明日时间表",
            FontSize = fontSize,
            FontWeight = FontWeights.Bold,
            Foreground = accentBrush,
            Margin = new Thickness(0, 0, 0, 6)
        };
    }

    /// <summary>
    /// 自绘进度条：Grid 两 Star 列（Column0=进度比例 / Column1=剩余空白），
    /// 背景 Border 跨两列 + 前景 Border 占 Column0；比例由 Grid 布局自动分配，重建后布局瞬间即正确。
    /// （与 Avalonia 版渲染器、WPF 进程内实现同一套做法，不用 WPF ProgressBar 以避开其布局时序问题。）
    /// </summary>
    public static (Grid host, Border indicator) CreateSelfDrawnProgressBar(Color accentColor, double height, Thickness margin)
    {
        var accentBrush = new SolidColorBrush(accentColor);
        var bgBrush = new SolidColorBrush(Color.FromArgb(0x40, accentColor.R, accentColor.G, accentColor.B));

        var host = new Grid
        {
            Height = height,
            Margin = margin,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = true
        };
        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
        var bg = new Border
        {
            Background = bgBrush,
            CornerRadius = new CornerRadius(1.5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumnSpan(bg, 2);
        var indicator = new Border
        {
            Background = accentBrush,
            CornerRadius = new CornerRadius(1.5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumn(indicator, 0);
        host.Children.Add(bg);
        host.Children.Add(indicator);
        return (host, indicator);
    }

    /// <summary>更新进度比例：直接改两列 Star 宽度（Grid 布局自动分配前景宽度，无时序依赖）。</summary>
    public static void ApplyProgressRatio(Grid? host, Border? indicator, double ratio)
    {
        if (host == null || host.ColumnDefinitions.Count < 2) return;
        ratio = double.IsNaN(ratio) ? 0.0 : Math.Clamp(ratio, 0.0, 1.0);
        host.ColumnDefinitions[0].Width = new GridLength(ratio, GridUnitType.Star);
        host.ColumnDefinitions[1].Width = new GridLength(Math.Max(0.0, 1.0 - ratio), GridUnitType.Star);
    }
}
