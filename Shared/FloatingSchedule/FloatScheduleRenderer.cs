// 时间表悬浮窗共享渲染器：FloatScheduleRenderModel → Avalonia 控件树。
// 与主项目 Services\FloatingScheduleService.cs 的课表 UI 构建段（原 RefreshSchedule 内联代码）
// 行为等价迁移：进程内模式与独立进程模式共用本渲染器，杜绝双份 UI 代码漂移。
// 约束：只允许 using System / Avalonia；颜色全部来自模型（不依赖主题资源）。
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

/// <summary>渲染结果引用：进度条宿主/前景与分隔线控件，供上层（进程内 service 或子进程窗口）后续按帧更新。</summary>
public sealed class FloatScheduleProgressRefs
{
    /// <summary>当前课进度条承载容器（Grid，两 Star 列）。</summary>
    public Layoutable? ClassProgressHost { get; set; }
    /// <summary>当前课进度条前景（占 Column0）。</summary>
    public Border? ClassProgressIndicator { get; set; }
    /// <summary>课间进度条承载容器。</summary>
    public Layoutable? BreakProgressHost { get; set; }
    /// <summary>课间进度条前景。</summary>
    public Border? BreakProgressIndicator { get; set; }
    /// <summary>本次构建产生的档案分隔线控件（进程内用于主题切换动态更新颜色）。</summary>
    public List<Border> SeparatorLines { get; } = new();
    /// <summary>FPS 角标文本（模型 ShowFps=true 时非空；宿主每帧按"渲染/无等待"更新 Text）。</summary>
    public TextBlock? FpsText { get; set; }
}

public static class FloatScheduleRenderer
{
    // ===== 卡片外框规格（与 EnsureWindow 中 _containerBorder 一致）=====
    public const double CardCornerRadius = 8;
    public static readonly Thickness CardPadding = new(12, 10);
    public static readonly Thickness CardBorderThickness = new(1);

    /// <summary>把模型中的卡片背景/边框色应用到容器 Border（进程内与子进程共用）。</summary>
    public static void ApplyCardStyle(Border card, FloatScheduleRenderModel m)
    {
        card.Background = new SolidColorBrush(FromArgb(m.CardBackgroundArgb));
        card.BorderBrush = new SolidColorBrush(FromArgb(m.BorderArgb));
    }

    /// <summary>0xAARRGGBB → Avalonia Color。</summary>
    public static Color FromArgb(uint argb) =>
        Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    /// <summary>Avalonia Color → 0xAARRGGBB。</summary>
    public static uint ToArgb(Color c) => ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;

    /// <summary>
    /// 构建课表内容控件树（作为卡片 Border 的 Child）。
    /// 返回根控件与进度条引用；不创建卡片 Border 本身。
    /// </summary>
    public static (Control root, FloatScheduleProgressRefs refs) Build(FloatScheduleRenderModel m)
    {
        var refs = new FloatScheduleProgressRefs();
        // 字体：宿主可解析的字体族名（非嵌入资源）显式下传（Avalonia FontFamily 沿树继承），
        // 保证子进程（无宿主主题资源）与进程内渲染字形一致；null 时不设置（进程内走宿主主题默认，
        // 子进程由窗口层兜底 "HarmonyOS Sans SC, Microsoft YaHei UI"）。
        FontFamily? familyAv = null;
        if (!string.IsNullOrWhiteSpace(m.FontFamilySource))
        {
            try { familyAv = new FontFamily(m.FontFamilySource + ", HarmonyOS Sans SC, Microsoft YaHei UI"); }
            catch { familyAv = null; }
        }
        var fontSize = m.FontSize;
        var accentColor = FromArgb(m.AccentArgb);
        // 字体：宿主可解析的字体族名（非嵌入资源）显式下传到每个 TextBlock（Avalonia 各代际 Grid 无 FontFamily），
        // 保证子进程（无宿主主题资源）与进程内渲染字形一致；null 时不设置（进程内走宿主主题默认，
        // 子进程由窗口层兜底 "HarmonyOS Sans SC, Microsoft YaHei UI"）。
        void SetFamily(TextBlock tb)
        {
            if (familyAv != null) tb.FontFamily = familyAv;
        }
        var accentBrush = new SolidColorBrush(accentColor);
        var textFg = new SolidColorBrush(FromArgb(m.TextArgb));
        var subFg = new SolidColorBrush(FromArgb(m.SubTextArgb));
        var sep = new SolidColorBrush(FromArgb(m.BorderArgb));

        // FPS 角标收尾：ShowFps=true 时把内容包进 Grid 并在右下角叠加一个小字 TextBlock（不撑宽、不占布局）。
        (Control, FloatScheduleProgressRefs) Finish(Control content)
        {
            if (!m.ShowFps) return (content, refs);
            var wrap = new Grid();
            wrap.Children.Add(content);
            var fps = new TextBlock
            {
                Text = "0.0/0.0",
                FontSize = Math.Max(7, fontSize - 6),
                Foreground = subFg,
                Opacity = 0.75,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 2, 0),
                IsHitTestVisible = false
            };
            SetFamily(fps);
            wrap.Children.Add(fps);
            refs.FpsText = fps;
            return (wrap, refs);
        }

        // 横向布局（每列一节课，列内竖排课程/时间/教师）：占位模式仍走下方早退。
        if (m.LayoutDirection != 0 && string.IsNullOrWhiteSpace(m.PlaceholderText))
            return Finish(BuildHorizontal(m, refs, fontSize, familyAv));

        // ---- 空表占位模式（与进程内原占位分支一致：明天=标题+占位两行 Grid；今天=仅占位）----
        if (!string.IsNullOrWhiteSpace(m.PlaceholderText))
        {
            var noClass = new TextBlock
            {
                Text = m.PlaceholderText,
                FontSize = fontSize,
                Foreground = subFg,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 6)
            };
            SetFamily(noClass);
            if (m.ShowTomorrow)
            {
                var tomorrowEmptyPanel = new Grid
                {
                    RowDefinitions = RowDefinitions.Parse("Auto, Auto"),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                var emptyTitle = CreateTomorrowTitle(fontSize, accentBrush);
                SetFamily(emptyTitle);
                Grid.SetRow(emptyTitle, 0);
                tomorrowEmptyPanel.Children.Add(emptyTitle);
                Grid.SetRow(noClass, 1);
                tomorrowEmptyPanel.Children.Add(noClass);
                return Finish(tomorrowEmptyPanel);
            }
            return Finish(noClass);
        }

        var highlightBg = new SolidColorBrush(FromArgb(m.HighlightArgb));
        var zebraBrush = new SolidColorBrush(FromArgb(m.ZebraRowArgb));
        double rowSpacingFactor = m.RowSpacingFactor <= 0 ? 1.0 : m.RowSpacingFactor;

        // 【明日课表】不参与"当前课高亮 / 课间行 / 进度条"：这些语义只对当天成立（明天的课还没上，不存在当前课）。
        //  渲染器是两种模式（进程内 / 独立进程）唯一的 UI 出口，在此统一落地可保证：
        //  即使上层模型/子进程本地推进给出了"当前课索引"，明日课表也永远不画高亮与进度条。
        bool isTomorrowList = m.ShowTomorrow;
        int currentClassIndex = isTomorrowList ? -1 : m.CurrentClassIndex;
        var currentBreak = isTomorrowList ? null : m.Break;

        // ---- 构建 Grid（列宽/间距与进程内一致）----
        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto, *"),
            RowDefinitions = new RowDefinitions(),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnSpacing = m.ColumnSpacing,
            RowSpacing = 0
        };

        // 【明日时间表】标题行：同一 Grid 的第 0 行（跨 2 列），保证今天/明天卡片宽度计算路径一致
        int headerRowIdx = 0;
        if (m.ShowTomorrow)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var tomorrowTitle = CreateTomorrowTitle(fontSize, accentBrush);
            SetFamily(tomorrowTitle);
            Grid.SetColumnSpan(tomorrowTitle, 2);
            Grid.SetRow(tomorrowTitle, 0);
            grid.Children.Add(tomorrowTitle);
            headerRowIdx = 1;
        }

        // 表头行
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var header1 = new TextBlock
        {
            Text = "课程",
            FontSize = Math.Max(8, fontSize - 2),
            FontWeight = FontWeight.Bold,
            Foreground = textFg,
            Margin = new Thickness(0, 0, 0, 4)
        };
        SetFamily(header1);
        Grid.SetColumn(header1, 0); Grid.SetRow(header1, headerRowIdx); grid.Children.Add(header1);
        var header2 = new TextBlock
        {
            Text = "时间",
            FontSize = Math.Max(8, fontSize - 2),
            FontWeight = FontWeight.Bold,
            Foreground = textFg,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 4)
        };
        SetFamily(header2);
        Grid.SetColumn(header2, 1); Grid.SetRow(header2, headerRowIdx); grid.Children.Add(header2);

        // 表头下分隔线
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var sepLine = new Border
        {
            Height = 1,
            Background = sep,
            Margin = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(0.5)
        };
        Grid.SetColumnSpan(sepLine, 2);
        Grid.SetRow(sepLine, headerRowIdx + 1); grid.Children.Add(sepLine);

        var breakSeparatorBrush = new SolidColorBrush(FromArgb(m.SeparatorArgb));

        // 课程行
        for (int i = 0; i < m.Rows.Count; i++)
        {
            var row = m.Rows[i];
            var isCurrent = i == currentClassIndex;

            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var rIdx = grid.RowDefinitions.Count - 1;

            IBrush rowBg = Brushes.Transparent;
            if (m.ZebraStriped && !isCurrent && i % 2 == 1) rowBg = zebraBrush;
            if (isCurrent) rowBg = highlightBg;
            double rowMarginV = 2 * rowSpacingFactor;

            // ---- 课程列（左课程名 * / 右教师名 Auto）----
            var coursePanel = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*, Auto"),
                RowDefinitions = RowDefinitions.Parse("Auto"),
                VerticalAlignment = VerticalAlignment.Center,
                Background = rowBg,
                Margin = new Thickness(0, rowMarginV, 0, rowMarginV)
            };
            Grid.SetColumn(coursePanel, 0);
            Grid.SetRow(coursePanel, rIdx);

            var courseTb = new TextBlock
            {
                Text = row.Course,
                FontSize = fontSize,
                FontWeight = m.FontWeightCustom != 0 ? MapWeight(m.FontWeightCustom) : (isCurrent ? FontWeight.Bold : FontWeight.Normal),
                Foreground = row.SubjectColorArgb != 0 ? new SolidColorBrush(FromArgb(row.SubjectColorArgb)) : textFg,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 8, 0)
            };
            SetFamily(courseTb);
            Grid.SetColumn(courseTb, 0); Grid.SetRow(courseTb, 0);
            coursePanel.Children.Add(courseTb);

            if (!string.IsNullOrEmpty(row.Teacher))
            {
                int teacherFontSize = Math.Max(8, fontSize - 3);
                // 教师列宽单一事实来源：teacherFontSize(pt) × 21.5（完整展示 15 汉字的实测系数，与历史行为一致）
                var teacherMaxW = teacherFontSize * 21.5;
                var teacherTb = new TextBlock
                {
                    Text = row.Teacher,
                    FontSize = teacherFontSize,
                    Foreground = subFg,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    MaxWidth = teacherMaxW,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                SetFamily(teacherTb);
                Grid.SetColumn(teacherTb, 1);
                Grid.SetRow(teacherTb, 0);
                coursePanel.Children.Add(teacherTb);
            }

            // 当前课：进度条放在课程行的下边缘
            if (isCurrent)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var pIdx = grid.RowDefinitions.Count - 1;
                var (progHost, progInd) = CreateSelfDrawnProgressBar(accentColor, 2.5, new Thickness(0, 2, 0, 0));
                refs.ClassProgressHost = progHost;
                refs.ClassProgressIndicator = progInd;
                Grid.SetColumn(progHost, 0);
                Grid.SetColumnSpan(progHost, 2);
                Grid.SetRow(progHost, pIdx);
                grid.Children.Add(progHost);
            }

            grid.Children.Add(coursePanel);

            // ---- 时间列 ----
            var timeTb = new TextBlock
            {
                Text = row.TimeText,
                FontSize = Math.Max(8, fontSize - 1),
                Foreground = textFg,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Background = rowBg,
                Margin = new Thickness(0, rowMarginV, 0, rowMarginV)
            };
            SetFamily(timeTb);
            Grid.SetColumn(timeTb, 1);
            Grid.SetRow(timeTb, rIdx);
            grid.Children.Add(timeTb);

            // ---- 课间休息插入行（仅当 i == Break.AfterClassIndex）----
            if (currentBreak != null && i == currentBreak.AfterClassIndex)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var brIdx = grid.RowDefinitions.Count - 1;
                IBrush breakBg = new SolidColorBrush(FromArgb(m.BreakRowBackgroundArgb));
                var breakTb = new TextBlock
                {
                    Text = currentBreak.Name,
                    FontSize = Math.Max(8, fontSize - 1),
                    Foreground = subFg,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 3 * rowSpacingFactor, 0, 3 * rowSpacingFactor)
                };
                SetFamily(breakTb);
                var breakCellL = new Border { Background = breakBg, Child = breakTb };
                Grid.SetColumn(breakCellL, 0);
                Grid.SetRow(breakCellL, brIdx);
                grid.Children.Add(breakCellL);

                var breakTimeTb = new TextBlock
                {
                    Text = currentBreak.TimeText,
                    FontSize = Math.Max(8, fontSize - 1),
                    Foreground = subFg,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 3 * rowSpacingFactor, 0, 3 * rowSpacingFactor)
                };
                SetFamily(breakTimeTb);
                var breakCellR = new Border { Background = breakBg, Child = breakTimeTb };
                Grid.SetColumn(breakCellR, 1);
                Grid.SetRow(breakCellR, brIdx);
                grid.Children.Add(breakCellR);

                // 课间进度条：插入行下方，横跨两列
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var pbIdx = grid.RowDefinitions.Count - 1;
                var (breakHost, breakInd) = CreateSelfDrawnProgressBar(accentColor, 2.5, new Thickness(0, 2, 0, 0));
                refs.BreakProgressHost = breakHost;
                refs.BreakProgressIndicator = breakInd;
                Grid.SetColumn(breakHost, 0);
                Grid.SetColumnSpan(breakHost, 2);
                Grid.SetRow(breakHost, pbIdx);
                grid.Children.Add(breakHost);
            }

            // ---- 档案分隔线（第 i 行之后；粗细=SeparatorThickness，0=禁用）----
            if (m.SeparatorThickness > 0 && m.SeparatorAfterClassIndex.Contains(i))
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var sepRowIdx = grid.RowDefinitions.Count - 1;
                var sepBorder = new Border
                {
                    Height = m.SeparatorThickness,
                    Background = breakSeparatorBrush,
                    Opacity = 0.7,   // 【分隔线】70% 不透明
                    Margin = new Thickness(0, 1, 0, 1)
                };
                Grid.SetColumnSpan(sepBorder, 2);
                Grid.SetRow(sepBorder, sepRowIdx);
                grid.Children.Add(sepBorder);
                refs.SeparatorLines.Add(sepBorder);
            }
        }

        return Finish(grid);
    }

    /// <summary>字重数值（100~900）映射到 Avalonia FontWeight 命名档（避免跨版本构造差异）。</summary>
    private static FontWeight MapWeight(int w) => w switch
    {
        <= 100 => FontWeight.Thin,
        <= 200 => FontWeight.ExtraLight,
        <= 300 => FontWeight.Light,
        <= 400 => FontWeight.Normal,
        <= 500 => FontWeight.Medium,
        <= 600 => FontWeight.SemiBold,
        <= 700 => FontWeight.Bold,
        <= 800 => FontWeight.ExtraBold,
        _ => FontWeight.Black,
    };

    /// <summary>
    /// 横向布局：每列一节课，列内自上而下竖排 课程名/时间/教师；dir=1 第1节最左，dir=2 第1节最右。
    /// 当前课整列高亮 + 列底进度条；课间/分隔线作为独立列插入。外层横向滚动以适配 MaxWidth。
    /// </summary>
    private static Control BuildHorizontal(FloatScheduleRenderModel m, FloatScheduleProgressRefs refs, int fontSize, FontFamily? familyAv)
    {
        var accentColor = FromArgb(m.AccentArgb);
        var accentBrush = new SolidColorBrush(accentColor);
        var textFg = new SolidColorBrush(FromArgb(m.TextArgb));
        var subFg = new SolidColorBrush(FromArgb(m.SubTextArgb));
        var highlightBg = new SolidColorBrush(FromArgb(m.HighlightArgb));
        var zebraBrush = new SolidColorBrush(FromArgb(m.ZebraRowArgb));
        var sepBrush = new SolidColorBrush(FromArgb(m.SeparatorArgb));
        double colSpacing = m.ColumnSpacing <= 0 ? 14 : m.ColumnSpacing;
        bool rtl = m.LayoutDirection == 2;
        bool isTomorrowList = m.ShowTomorrow;
        int currentClassIndex = isTomorrowList ? -1 : m.CurrentClassIndex;
        var currentBreak = isTomorrowList ? null : m.Break;

        void SetF(TextBlock tb) { if (familyAv != null) tb.FontFamily = familyAv; }

        var outer = new StackPanel { Orientation = Orientation.Vertical };
        if (m.ShowTomorrow)
        {
            var title = CreateTomorrowTitle(fontSize, accentBrush);
            SetF(title);
            outer.Children.Add(title);
        }

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(),
            RowDefinitions = RowDefinitions.Parse("Auto"),
            HorizontalAlignment = HorizontalAlignment.Left,
            ColumnSpacing = colSpacing,
        };

        // 构建"列单元"序列（课程列 / 课间列 / 分隔线列），再按 rtl 决定是否反转顺序。
        var cells = new List<Control>();
        for (int i = 0; i < m.Rows.Count; i++)
        {
            var row = m.Rows[i];
            var isCurrent = i == currentClassIndex;
            IBrush colBg = Brushes.Transparent;
            if (m.ZebraStriped && i % 2 == 1) colBg = zebraBrush;
            if (isCurrent) colBg = highlightBg;
            var col = new Border
            {
                Background = colBg,
                Padding = new Thickness(2, 2, 2, 2),
                Child = BuildCourseColumn(row, isCurrent, m, fontSize, familyAv, textFg, subFg, accentColor, refs)
            };
            cells.Add(col);
            if (currentBreak != null && i == currentBreak.AfterClassIndex)
                cells.Add(BuildBreakColumn(currentBreak, fontSize, subFg, m, refs, familyAv));
            if (m.SeparatorThickness > 0 && m.SeparatorAfterClassIndex.Contains(i))
                cells.Add(new Rectangle { Width = Math.Max(1, m.SeparatorThickness), Fill = sepBrush, Opacity = 0.7, Margin = new Thickness(0, 2, 0, 2) });
        }
        if (rtl) cells.Reverse();
        for (int c = 0; c < cells.Count; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.SetColumn(cells[c], c);
            Grid.SetRow(cells[c], 0);
            grid.Children.Add(cells[c]);
        }
        outer.Children.Add(grid);
        return new ScrollViewer
        {
            Content = outer,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
    }

    private static Control BuildCourseColumn(FloatScheduleRowModel row, bool isCurrent, FloatScheduleRenderModel m, int fontSize,
        FontFamily? familyAv, IBrush textFg, IBrush subFg, Color accentColor, FloatScheduleProgressRefs refs)
    {
        void SetF(TextBlock tb) { if (familyAv != null) tb.FontFamily = familyAv; }
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
        var courseTb = new TextBlock
        {
            Text = row.Course,
            FontSize = fontSize,
            FontWeight = m.FontWeightCustom != 0 ? MapWeight(m.FontWeightCustom) : (isCurrent ? FontWeight.Bold : FontWeight.Normal),
            Foreground = row.SubjectColorArgb != 0 ? new SolidColorBrush(FloatScheduleRenderer.FromArgb(row.SubjectColorArgb)) : textFg,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        };
        SetF(courseTb);
        panel.Children.Add(courseTb);
        var timeTb = new TextBlock { Text = row.TimeText, FontSize = Math.Max(8, fontSize - 1), Foreground = textFg, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
        SetF(timeTb);
        panel.Children.Add(timeTb);
        if (!string.IsNullOrEmpty(row.Teacher))
        {
            var teacherTb = new TextBlock { Text = row.Teacher, FontSize = Math.Max(8, fontSize - 3), Foreground = subFg, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
            SetF(teacherTb);
            panel.Children.Add(teacherTb);
        }
        if (isCurrent)
        {
            var (host, ind) = CreateSelfDrawnProgressBar(accentColor, 2.5, new Thickness(0, 3, 0, 0));
            refs.ClassProgressHost = host; refs.ClassProgressIndicator = ind;
            panel.Children.Add(host);
        }
        return panel;
    }

    private static Control BuildBreakColumn(FloatScheduleBreakModel br, int fontSize, IBrush subFg, FloatScheduleRenderModel m, FloatScheduleProgressRefs refs, FontFamily? familyAv)
    {
        var bg = new SolidColorBrush(FromArgb(m.BreakRowBackgroundArgb));
        var panel = new Border
        {
            Background = bg,
            Padding = new Thickness(2, 2, 2, 2),
            Child = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 2,
                Children =
                {
                    SetF2(new TextBlock { Text = br.Name, FontSize = Math.Max(8, fontSize - 1), Foreground = subFg, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center }, familyAv),
                    SetF2(new TextBlock { Text = br.TimeText, FontSize = Math.Max(8, fontSize - 1), Foreground = subFg, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center }, familyAv),
                }
            }
        };
        var (host, ind) = CreateSelfDrawnProgressBar(FromArgb(m.AccentArgb), 2.5, new Thickness(0, 3, 0, 0));
        refs.BreakProgressHost = host; refs.BreakProgressIndicator = ind;
        ((StackPanel)panel.Child!).Children.Add(host);
        return panel;
    }

    private static TextBlock SetF2(TextBlock tb, FontFamily? familyAv) { if (familyAv != null) tb.FontFamily = familyAv; return tb; }

    /// <summary>"明日时间表"标题标识（强调色加粗）。</summary>
    public static TextBlock CreateTomorrowTitle(int fontSize, IBrush accentBrush)
    {
        return new TextBlock
        {
            Text = "明日时间表",
            FontSize = fontSize,
            FontWeight = FontWeight.Bold,
            Foreground = accentBrush,
            Margin = new Thickness(0, 0, 0, 6)
        };
    }

    /// <summary>
    /// 自绘进度条（对齐原 CreateSelfDrawnProgressBar）：Grid 两 Star 列（Column0=进度比例 / Column1=剩余），
    /// 背景 Border 跨两列 + 前景 Border 占 Column0；比例由 Grid 布局自动分配，重建后布局瞬间即正确。
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

    /// <summary>更新进度比例（对齐原 ApplyProgressRatioAv）：直接改两列 Star 宽度。</summary>
    public static void ApplyProgressRatio(Layoutable? host, Border? indicator, double ratio)
    {
        if (host is not Grid hostGrid || hostGrid.ColumnDefinitions.Count < 2) return;
        ratio = double.IsNaN(ratio) ? 0.0 : Math.Clamp(ratio, 0.0, 1.0);
        hostGrid.ColumnDefinitions[0].Width = new GridLength(ratio, GridUnitType.Star);
        hostGrid.ColumnDefinitions[1].Width = new GridLength(Math.Max(0.0, 1.0 - ratio), GridUnitType.Star);
    }
}
