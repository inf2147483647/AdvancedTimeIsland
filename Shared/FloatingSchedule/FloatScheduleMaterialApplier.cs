// 时间表悬浮窗"高级外观"材质应用器（独立进程模式共享层）。
// 约束：只允许 using System / Avalonia；不引用 ClassIsland.*/PluginSettings/ThemeHelper——
// 以便同一份源码同时编入主项目（进程内渲染）与 AdvancedTimeIslandFloatSchedule 子进程。
// 输入全部为插件侧解析后的终态参数（材质枚举 int / 强度 / 圆角 / tint ARGB / 图片路径 / 内容控件）。
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

/// <summary>
/// 把卡片 Border 的背景层按所选材质装配：
/// 0 半透明(纯色) / 1 磨砂 / 2 亚克力(ExperimentalAcrylicBorder) / 3 液态玻璃(亚克力近似+高光) /
/// 4 古卷(竹简代码纹理) / 5 自定义图片。content 为渲染器产出的内容控件，本方法负责把它挂到卡片下。
/// </summary>
public static class FloatScheduleMaterialApplier
{
    /// <summary>把材质应用到卡片 Border（就地修改 Background/Child）。</summary>
    public static void Apply(Border card, int material, double intensity, double corner, uint tintArgb, string? imagePath, Control content)
    {
        var cr = new CornerRadius(Math.Max(0, corner));
        var tint = FloatScheduleRenderer.FromArgb(tintArgb);
        card.CornerRadius = cr;

        switch (material)
        {
            case 1: // 磨砂
            case 2: // 亚克力
            case 3: // 液态玻璃（亚克力近似 + 高光描边）
                card.Background = null;
                card.Child = BuildAcrylic(content, cr, tint, intensity, glass: material == 3);
                break;
            case 4: // 古卷（竹简）
                card.Background = BuildBambooBrush();
                card.Child = BuildBamboo(content, cr);
                break;
            case 5: // 自定义图片
                var img = TryImageBrush(imagePath);
                if (img != null) { card.Background = img; card.Child = content; }
                else { card.Background = new SolidColorBrush(tint); card.Child = content; } // 无效路径回退半透明
                break;
            default: // 0 半透明
                card.Background = new SolidColorBrush(tint);
                card.Child = content;
                break;
        }
    }

    /// <summary>
    /// 装饰边框：按预设设定卡片 BorderBrush/BorderThickness（自定义色优先，否则预设主色）。
    /// enabled=false 时清除边框（恢复 1px 主题描边由 ApplyCardStyle 负责，这里只置 0 覆盖由调用方决定）。
    /// </summary>
    public static void ApplyBorder(Border card, bool enabled, int preset, bool colorEnabled, uint color)
    {
        if (!enabled) { card.BorderThickness = new Thickness(0); return; }
        var (thickness, defaultColor) = preset switch
        {
            0 => (8.0, Color.FromRgb(0xB8, 0x86, 0x0B)),   // 画框 暖金
            1 => (10.0, Color.FromRgb(0x6B, 0x4A, 0x2B)),  // 黑板 木框
            2 => (6.0, Color.FromRgb(0xBF, 0xE0, 0xFF)),   // Win7 玻璃蓝白
            3 => (6.0, Color.FromRgb(0x0A, 0x5A, 0xC0)),   // XP 蓝
            4 => (1.0, Color.FromRgb(0x88, 0x88, 0x88)),   // Win10 冷灰
            5 => (1.0, Color.FromArgb(0xAA, 0x9A, 0x9A, 0x9A)), // Win11 半透明细
            6 => (1.5, Color.FromRgb(0xD0, 0xD0, 0xD0)),   // HarmonyOS 细
            _ => (4.0, Color.FromRgb(0xB8, 0x86, 0x0B)),
        };
        card.BorderThickness = new Thickness(thickness);
        card.BorderBrush = new SolidColorBrush(colorEnabled ? FloatScheduleRenderer.FromArgb(color) : defaultColor);
    }

    // 磨砂/亚克力/液态玻璃：ExperimentalAcrylicBorder 作背景层，内容叠于其上；液态玻璃另加高光描边近似折射。
    private static Control BuildAcrylic(Control content, CornerRadius cr, Color tint, double intensity, bool glass)
    {
        // intensity 0~1 → 越大越"实"（tint 更浓、材质更不透明）；默认 0.5 取中间值。
        var mat = new ExperimentalAcrylicMaterial
        {
            TintColor = tint,
            // 磨砂比亚克力更实、液态玻璃更透；用 intensity 调制
            TintOpacity = glass ? 0.25 + 0.35 * intensity : 0.45 + 0.4 * intensity,
            MaterialOpacity = glass ? 0.35 + 0.3 * intensity : 0.55 + 0.35 * intensity,
        };
        var acrylic = new ExperimentalAcrylicBorder
        {
            CornerRadius = cr,
            Material = mat,
            Child = content,
        };
        if (!glass) return acrylic;

        // 液态玻璃近似：在亚克力上叠一层顶部→底部高光渐变 + 细亮边，模拟玻璃反射。
        var grid = new Grid();
        grid.Children.Add(acrylic);
        grid.Children.Add(new Border
        {
            CornerRadius = cr,
            IsHitTestVisible = false,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF), 0),
                    new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.5),
                    new GradientStop(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF), 1),
                }
            },
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
        });
        return grid;
    }

    // 古卷竹简：竖向竹条用平铺 DrawingBrush 作卡片背景；此处叠"天头地脚"暗边 + 两道横向编绳（比例行定位）。
    private static Control BuildBamboo(Control content, CornerRadius cr)
    {
        // 5 行：天头(*) 编绳(Auto) 中段(*) 编绳(Auto) 地脚(*)，编绳按 28%/72% 近似分布。
        var ropeGrid = new Grid
        {
            IsHitTestVisible = false,
            RowDefinitions = RowDefinitions.Parse("28*,2,44*,2,26*"),
        };
        var ropeBrush = new SolidColorBrush(Color.FromArgb(0x99, 0x4A, 0x32, 0x14));
        var rope1 = new Border { Background = ropeBrush, Height = 2, VerticalAlignment = VerticalAlignment.Stretch };
        Grid.SetRow(rope1, 1);
        var rope2 = new Border { Background = ropeBrush, Height = 2, VerticalAlignment = VerticalAlignment.Stretch };
        Grid.SetRow(rope2, 3);
        ropeGrid.Children.Add(rope1);
        ropeGrid.Children.Add(rope2);

        var grid = new Grid();
        grid.Children.Add(ropeGrid);
        // 天头地脚暗边
        grid.Children.Add(new Border
        {
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Top,
            Height = 14,
            CornerRadius = new CornerRadius(cr.TopLeft, cr.TopRight, 0, 0),
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.FromArgb(0x55, 0x5A, 0x40, 0x18), 0), new GradientStop(Color.FromArgb(0x00, 0x5A, 0x40, 0x18), 1) }
            }
        });
        grid.Children.Add(new Border
        {
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 14,
            CornerRadius = new CornerRadius(0, 0, cr.BottomLeft, cr.BottomRight),
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.FromArgb(0x55, 0x5A, 0x40, 0x18), 0), new GradientStop(Color.FromArgb(0x00, 0x5A, 0x40, 0x18), 1) }
            }
        });
        grid.Children.Add(content);
        return grid;
    }

    // 竹条平铺画刷：单片宽 22，竖向竹纹（中间亮、两侧暗的横向渐变）+ 竹片间深色缝。
    private static IBrush BuildBambooBrush()
    {
        var slatW = 22.0;
        var baseGrad = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(slatW, 0, RelativeUnit.Absolute),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(0xC9, 0xB0, 0x74), 0.0),
                new GradientStop(Color.FromRgb(0xE6, 0xD3, 0xA6), 0.5),
                new GradientStop(Color.FromRgb(0xC9, 0xB0, 0x74), 1.0),
            }
        };
        var dg = new DrawingGroup();
        dg.Children.Add(new GeometryDrawing
        {
            Brush = baseGrad,
            Geometry = new RectangleGeometry(new Rect(0, 0, slatW - 2, 20))
        });
        // 竹片间深色缝（右侧 2px）
        dg.Children.Add(new GeometryDrawing
        {
            Brush = new SolidColorBrush(Color.FromRgb(0x6E, 0x52, 0x24)),
            Geometry = new RectangleGeometry(new Rect(slatW - 2, 0, 2, 20))
        });
        return new DrawingBrush(dg)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
        };
    }

    private static ImageBrush? TryImageBrush(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            if (!System.IO.File.Exists(path)) return null;
            var bmp = new Avalonia.Media.Imaging.Bitmap(path);
            return new ImageBrush(bmp)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center,
            };
        }
        catch { return null; }
    }
}
