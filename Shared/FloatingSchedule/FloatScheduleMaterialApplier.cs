// 时间表悬浮窗"高级外观"材质应用器（独立进程模式共享层）。
// 约束：只允许 using System / Avalonia；不引用 ClassIsland.*/PluginSettings/ThemeHelper——
// 以便同一份源码同时编入主项目（进程内渲染）与 AdvancedTimeIslandFloatSchedule 子进程。
// 输入全部为插件侧解析后的终态参数（材质枚举 int / 强度 / 圆角 / tint ARGB / 图片路径 / 内容控件）。
//
// 【三套独立配方】依据微软 / 苹果一手规格，把磨砂、亚克力、液态玻璃拆成三条独立管线，
// 不再是"同一图层栈只换强度系数"（那样必然表现为"同效果的不同强度"）：
//   1 磨砂    ：重模糊 + 高不透明乳白 tint + 明显程序化颗粒 + 极弱 rim，无镜面高光（背景几乎不可辨）
//   2 亚克力  ：Win11 官方五层配方 背景→模糊→亮度/对比层→tint 覆盖→噪点(~2%) + 1px 柔和内描边（背景模糊但可辨）
//   3 液态玻璃：Apple 取向"透镜折射而非模糊"——背景保持清晰 + 极低 tint + 强 1px 镜面 rim
//               + 顶部镜面高光带 + 边缘透镜内发光 + 暖冷错位色散（真实窗口刻意不做系统模糊）
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

/// <summary>
/// 把卡片 Border 的背景层按所选材质装配：
/// 0 半透明(纯色) / 1 磨砂 / 2 亚克力 / 3 液态玻璃 / 4 古卷(竹简代码纹理) / 5 自定义图片。
/// content 为渲染器产出的内容控件，本方法负责把它挂到卡片下。
/// </summary>
public static class FloatScheduleMaterialApplier
{
    /// <summary>
    /// 背景透卡模糊半径（px）——三材质的核心物理差异之一，供编辑器预览按其模糊自绘背景：
    /// 磨砂最重（背景不可辨）、亚克力中等（背景可辨）、液态玻璃≈0（背景保持清晰，Apple 取向为折射而非模糊）。
    /// </summary>
    public static double BackdropBlurFor(int material, double intensity)
    {
        double it = Math.Clamp(intensity, 0, 1);
        return material switch
        {
            1 => 24.0 + 26.0 * it,   // 磨砂 24~50
            2 => 8.0 + 14.0 * it,    // 亚克力 8~22
            3 => 0.0 + 2.5 * it,     // 液态玻璃 0~2.5（近似"无模糊、纯折射"）
            _ => 0.0,
        };
    }

    /// <summary>
    /// 把材质应用到卡片 Border（就地修改 Background/Child）。
    /// osBackdrop=true 表示当前宿主是"透明窗口 + 可拿到 OS 合成背景"（真实悬浮窗进程内/子进程），
    ///   磨砂 / 亚克力 走系统合成材质（ExperimentalAcrylicBorder，真实桌面背景模糊）；
    /// osBackdrop=false 表示不透明宿主（编辑器预览）——系统材质采不到背后画面，改走自绘玻璃图层。
    /// 液态玻璃无论何种宿主都走自绘"折射"管线（真实窗口刻意不启用系统模糊，Apple 取向）。
    /// backdropLayer 为调用方（编辑器）提供的"背景透卡"控件（承载被模糊的示例背景），仅 osBackdrop=false 时使用。
    /// </summary>
    public static void Apply(Border card, int material, double intensity, double corner, uint tintArgb, string? imagePath, Control content, bool osBackdrop = false, Thickness contentPadding = default, Control? backdropLayer = null, bool backdropIsRefracted = false)
    {
        var cr = new CornerRadius(Math.Max(0, corner));
        var tint = FloatScheduleRenderer.FromArgb(tintArgb);
        card.CornerRadius = cr;
        // 材质层必须铺满整卡（含原 Padding 区域），否则 card.Background=null 时会出现「1px 描边 + 透明空隙 + 内容」的空心框。
        // 做法：card.Padding 归零；内容内边距改由内层容器承载。
        card.Padding = new Thickness(0);
        Control inner = content;
        if (contentPadding.Left > 0 || contentPadding.Top > 0 || contentPadding.Right > 0 || contentPadding.Bottom > 0)
            inner = new Border { Padding = contentPadding, Child = content };

        switch (material)
        {
            case 1: // 磨砂
                card.Background = null;
                card.Child = osBackdrop
                    ? BuildOsFrosted(inner, cr, tint, intensity)
                    : BuildFrosted(inner, cr, tint, intensity, PrepareBackdrop(backdropLayer, 1, intensity));
                break;
            case 2: // 亚克力（Win11 五层配方）
                card.Background = null;
                card.Child = osBackdrop
                    ? BuildOsAcrylic(inner, cr, tint, intensity)
                    : BuildAcrylic(inner, cr, tint, intensity, PrepareBackdrop(backdropLayer, 2, intensity));
                break;
            case 3: // 液态玻璃（Apple 折射取向，始终自绘）
                card.Background = null;
                card.Child = BuildLiquidGlass(inner, cr, tint, intensity, PrepareBackdrop(backdropLayer, 3, intensity), backdropIsRefracted);
                break;
            case 4: // 古卷（竹简）
                card.Background = BuildBambooBrush();
                card.Child = BuildBamboo(inner, cr);
                break;
            case 5: // 自定义图片
                var img = TryImageBrush(imagePath);
                if (img != null) { card.Background = img; card.Child = inner; }
                else { card.Background = new SolidColorBrush(tint); card.Child = inner; } // 无效路径回退半透明
                break;
            default: // 0 半透明
                card.Background = new SolidColorBrush(tint);
                card.Child = inner;
                break;
        }
    }

    // 给调用方提供的背景透卡按材质设置模糊半径（三材质差异的核心之一）；半径为 0 时不加 Effect（省一次 Skia 模糊）。
    static Control? PrepareBackdrop(Control? backdrop, int material, double intensity)
    {
        if (backdrop == null) return null;
        double r = BackdropBlurFor(material, intensity);
        if (r > 0.4) backdrop.Effect = new BlurEffect { Radius = r };
        return backdrop;
    }

    // ===== 真实悬浮窗：系统合成材质路径（透明窗口 + OS 桌面背景模糊）=====
    // ExperimentalAcrylicBorder 由 Avalonia 提交给系统合成器，对窗口背后（桌面）做实时模糊；
    // 需要窗口 TransparencyLevelHint 含 AcrylicBlur/Blur（由窗口侧按材质联动，见 FloatingScheduleService / 子进程）。
    // 注意：OS 侧模糊半径不可调（只有 AcrylicBlur/Blur 两档），因此磨砂与亚克力的差异靠
    //   tint 不透明度 + 亮度层 + 颗粒来体现（磨砂＝高遮盖乳白 + 明显颗粒；亚克力＝可辨识背景 + ~2% 噪点）。
    private static Control BuildOsAcrylic(Control content, CornerRadius cr, Color tint, double intensity)
    {
        double it = Math.Clamp(intensity, 0, 1);
        var mat = new ExperimentalAcrylicMaterial
        {
            TintColor = tint,
            TintOpacity = 0.24 + 0.16 * it,      // Win11 亚克力 tint 覆盖（比磨砂明显更透，保留"模糊但可辨"）
            MaterialOpacity = 0.38 + 0.20 * it,
        };
        var acrylic = new ExperimentalAcrylicBorder { CornerRadius = cr, Material = mat };
        // Win11 官方五层：系统模糊之上补 亮度层（白膜，近似 exclusion 混合的提亮；α≈10/255 对齐 Qt AcrylicEffect 的
        // luminosityColor rgba(255,255,255,10)）+ 噪声层（官方约 2%，用 128px 非周期纹理而非周期点阵）+ 柔和 1px 内描边
        var grid = new Grid();
        grid.Children.Add(acrylic);
        grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Background = Solid(0x0A, 0xFF, 0xFF, 0xFF) });
        grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Background = BuildNoiseBrush() });
        grid.Children.Add(new Border
        {
            CornerRadius = cr, IsHitTestVisible = false,
            BorderBrush = Solid(0x33, 0xFF, 0xFF, 0xFF), BorderThickness = new Thickness(1),
        });
        grid.Children.Add(content);
        return grid;
    }

    private static Control BuildOsFrosted(Control content, CornerRadius cr, Color tint, double intensity)
    {
        double it = Math.Clamp(intensity, 0, 1);
        var mat = new ExperimentalAcrylicMaterial
        {
            TintColor = tint,
            TintOpacity = 0.72 + 0.20 * it,      // 磨砂：高遮盖乳白，"背景不可辨"
            MaterialOpacity = 0.86 + 0.12 * it,
        };
        var acrylic = new ExperimentalAcrylicBorder { CornerRadius = cr, Material = mat };
        var grid = new Grid();
        grid.Children.Add(acrylic);
        // 乳白膜加厚遮盖（在系统模糊之上再压一层，进一步拉开与亚克力的差异）
        grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Background = Solid(WithAlpha(tint, 0.18 + 0.18 * it)) });
        // 极弱边缘发光（磨砂无镜面反射）
        grid.Children.Add(Ring(cr, 2, (byte)(0.10 * 255 * (0.7 + 0.6 * it)), 200, 220, 255));
        grid.Children.Add(content);
        return grid;
    }

    // ===== 编辑器预览：自绘路径（背景透卡由调用方提供，承载被模糊的示例背景）=====

    // 1 磨砂：重模糊 + 高不透明乳白 tint + 明显颗粒 + 极弱 rim，无镜面高光。
    private static Control BuildFrosted(Control content, CornerRadius cr, Color tint, double intensity, Control? backdrop)
    {
        double it = Math.Clamp(intensity, 0, 1);
        var grid = new Grid();
        if (backdrop != null) grid.Children.Add(backdrop);                       // ① 被重模糊的背景
        grid.Children.Add(new Border { CornerRadius = cr, Background = Solid(WithAlpha(tint, 0.64 + 0.20 * it)) });   // ② 底色遮盖（背景几乎不可辨）
        // ②b 乳白膜：磨砂的"乳白"质感（与亚克力的"着色透明"拉开差异）
        grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Background = Solid((byte)(0x1C + 0x18 * it), 0xFF, 0xFF, 0xFF) });
        grid.Children.Add(Ring(cr, 2, (byte)(0.10 * 255 * (0.7 + 0.6 * it)), 200, 220, 255));  // ③ 极弱边缘发光（描边环）
        grid.Children.Add(content);                                              // ⑤ 内容置顶
        return grid;
    }

    // 2 亚克力：对齐 Win11 官方五层配方——背景 → 模糊 → 亮度/对比层 → tint 覆盖，外加 1px 柔和内描边。
    // （官方配方里的"噪点"层已按用户要求移除：此前用的是 4px 周期点阵，缩放后呈明显规则点阵，观感差。）
    private static Control BuildAcrylic(Control content, CornerRadius cr, Color tint, double intensity, Control? backdrop)
    {
        double it = Math.Clamp(intensity, 0, 1);
        var grid = new Grid();
        if (backdrop != null) grid.Children.Add(backdrop);                       // ① 背景 + 模糊（中等，背景色彩可辨）
        grid.Children.Add(new Border { CornerRadius = cr, Background = Solid(WithAlpha(tint, 0.34 + 0.24 * it)) });   // ② tint 覆盖层
        grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Background = Solid(0x0A, 0xFF, 0xFF, 0xFF) });   // ③ 亮度层（α≈10/255）
        grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Background = BuildNoiseBrush() });   // ④ 噪声层（官方 ~2%，非周期纹理）
        grid.Children.Add(Ring(cr, 1, 0x2E, 0xFF, 0xFF, 0xFF));   // ⑤ 柔和内描边
        grid.Children.Add(content);                                              // ⑥ 内容置顶
        return grid;
    }

    // 3 液态玻璃：Apple 取向"透镜折射而非模糊"——背景清晰 + 极低 tint + 强镜面 rim + 顶部高光 + 边缘透镜 + 暖冷色散。
    // refracted=true 表示 backdrop 已是"真实 SDF 折射 + 色散 + 镜面高光"烘焙好的位图（编辑器预览路径），
    //   此时不再叠近似的透镜环/色散描边，只保留 tint 与顶部镜面高光（真机无背景纹理可采样，仍走近似层）。
    private static Control BuildLiquidGlass(Control content, CornerRadius cr, Color tint, double intensity, Control? backdrop, bool refracted = false)
    {
        double it = Math.Clamp(intensity, 0, 1);
        var grid = new Grid();
        if (backdrop != null) grid.Children.Add(backdrop);                       // ① 背景（真实折射位图 / 清晰背景）
        if (!refracted)
        {
            // 近似路径（尚未拿到折射位图）：保留低透明度的卡片 tint 作为底色。
            grid.Children.Add(new Border { CornerRadius = cr, Background = Solid(WithAlpha(tint, 0.10 + 0.14 * it)) });
        }
        // 真实折射路径不再叠任何 tint/白膜：直接显示折射位图本身（仅保留镜面 rim 与顶部高光）。
        if (!refracted)
        {
            // 无真实折射时（真机悬浮窗）用描边环近似"边缘聚光"；编辑器预览已由折射核心烘焙高光，跳过
            grid.Children.Add(Ring(cr, 3, (byte)(0.16 * 255 * (0.7 + 0.3 * it)), 214, 232, 255));
            grid.Children.Add(Ring(cr, 1.5, (byte)(0.34 * 255 * (0.7 + 0.3 * it)), 226, 240, 255));
            // 暖冷错位色散：1px 内框左上偏暖、右下偏冷（近似玻璃色散）
            grid.Children.Add(new Border
            {
                CornerRadius = cr, IsHitTestVisible = false, Margin = new Thickness(1), BorderThickness = new Thickness(1),
                BorderBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(0x20, 0xFF, 0xD2, 0xA8), 0),
                        new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.5),
                        new GradientStop(Color.FromArgb(0x20, 0xB4, 0xDE, 0xFF), 1),
                    }
                }
            });
        }
        grid.Children.Add(Ring(cr, 1, refracted ? (byte)0x2E : (byte)(0x66 + 0x33 * it), 0xFF, 0xFF, 0xFF));   // ⑤ 1px 镜面 rim（折射路径 α=0x2E 对齐 Apple CSS 的 border rgba(255,255,255,0.18)）
        grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Height = 2.5, VerticalAlignment = VerticalAlignment.Top, Background = MakeTopHighlightBrush(refracted ? 0.30 : 0.6 + 0.4 * it) });   // ⑥ 顶部镜面高光带（折射路径 α≈0.25 对齐 inset 0 1px 0 rgba(255,255,255,0.25)）
        if (!refracted) // 真折射路径已由折射核心烘焙对角高光，不再叠 sheen
            grid.Children.Add(new Border { CornerRadius = cr, IsHitTestVisible = false, Background = MakeSheenBrush(0.75 + 0.25 * it) });
        grid.Children.Add(content);                                              // ⑧ 内容置顶
        return grid;
    }

    /// <summary>
    /// 装饰边框：按预设设定卡片 BorderBrush/BorderThickness（自定义色优先，否则预设主色）。
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

    // ===== 图层工具 =====

    static Color WithAlpha(Color c, double a) => Color.FromArgb((byte)Math.Round(Math.Clamp(a, 0, 1) * 255), c.R, c.G, c.B);
    static SolidColorBrush Solid(byte a, byte r, byte g, byte b) => new(Color.FromArgb(a, r, g, b));
    static SolidColorBrush Solid(Color c) => new(c);

    // Win11 亚克力官方配方里的"噪声层"（官方约 2%）：128×128 程序化随机纹理，固定种子 → 图案稳定不闪。
    // 关键：必须是**非周期**噪声（官方用的是 noise.png 位图）。之前那版是 4px 周期点阵，
    // 在 125%~150% 显示缩放下被放大成肉眼可见的规则网格（用户报的"点阵"就是它），已弃用。
    static IImageBrush? _noiseBrush;
    private static IBrush? BuildNoiseBrush()
    {
        if (_noiseBrush != null) return _noiseBrush;
        const int N = 128;
        var buf = new byte[N * N * 4];       // BGRA(Premul)
        uint s = 0x12345678u;                // 固定种子
        for (int i = 0; i < N * N; i++)
        {
            s = s * 1664525u + 1013904223u;
            uint r = s;
            s = s * 1664525u + 1013904223u;
            byte a = (byte)((s >> 24) & 0x0F);              // α 0~15（峰值 ~6%、平均 ~3%，与官方 ~2% 同量级）
            bool light = (r & 0x10000) != 0;               // 明/暗噪声各半
            int o = i * 4;
            // Premul 约束：RGB ≤ α；亮噪声 RGB=α，暗噪声 RGB=0
            if (light) { buf[o] = a; buf[o + 1] = a; buf[o + 2] = a; }
            buf[o + 3] = a;
        }
        var bmp = new WriteableBitmap(new PixelSize(N, N), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = bmp.Lock()) Marshal.Copy(buf, 0, fb.Address, buf.Length);
        _noiseBrush = new ImageBrush(bmp)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            DestinationRect = new RelativeRect(0, 0, N, N, RelativeUnit.Absolute),
        };
        return _noiseBrush;
    }

    // 边缘光用「描边环」而非径向填充：径向渐变默认半径 0.5（内切椭圆），其末段颜色会在
    //   椭圆之外的四角三角区被钳制而把整块填白（此前卡片发灰发白的根因），故一律改用描边环。
    static Border Ring(CornerRadius cr, double thickness, byte a, byte r, byte g, byte b)
        => new()
        {
            CornerRadius = cr,
            IsHitTestVisible = false,
            BorderThickness = new Thickness(thickness),
            BorderBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b)),
        };

    // 顶部镜面高光带：顶部 2.5px 亮 → 透明（液态玻璃"最关键的一行"）。
    private static LinearGradientBrush MakeTopHighlightBrush(double strength)
    {
        byte a0 = (byte)Math.Clamp((int)(255 * 0.82 * Math.Clamp(strength, 0, 1)), 0, 255);
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(a0, 0xFF, 0xFF, 0xFF), 0),
                new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1),
            }
        };
    }

    // sheen 对角高光：左上亮白 → 中部透明 → 右下微蓝。
    private static LinearGradientBrush MakeSheenBrush(double strength)
    {
        byte wh = (byte)Math.Clamp((int)(255 * 0.20 * strength), 0, 255);
        byte bl = (byte)Math.Clamp((int)(255 * 0.12 * strength), 0, 255);
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(wh, 0xFF, 0xFF, 0xFF), 0),
                new GradientStop(Color.FromArgb(0, 0xFF, 0xFF, 0xFF), 0.5),
                new GradientStop(Color.FromArgb(bl, 150, 195, 255), 1),
            }
        };
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
        var ropeBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x4A, 0x32, 0x14));   // 编绳：降低不透明度避免压字
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
            DestinationRect = new RelativeRect(0, 0, slatW, 20, RelativeUnit.Absolute),
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
