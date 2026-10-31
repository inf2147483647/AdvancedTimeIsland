using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// 女装注意事项页头部右侧的线性「半身裙」图标（取代原先的 Fluent 插件图标 &#xE4DA;）。
/// 图形取自设计稿 SVG（viewBox 24 × 24、描边 1.5、圆头圆角），按控件实际尺寸等比缩放居中绘制；
/// 描边颜色由 <see cref="Stroke"/> 提供，由覆盖层在构造与主题切换时写入，深浅主题自适应。
/// </summary>
public class WomenswearSkirtIcon : Control
{
    /// <summary>设计稿画布尺寸（SVG viewBox 的正方形边长）。</summary>
    private const double DesignSize = 24;

    /// <summary>设计稿中的描边宽度（viewBox 单位）。</summary>
    private const double DesignStrokeThickness = 1.5;

    /// <summary>
    /// 描边画刷。默认取主题文字色；覆盖层会在构造与主题切换时显式刷新，
    /// 故这里不依赖动态资源，避免 FA2 / FA3 资源键差异。
    /// </summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<WomenswearSkirtIcon, IBrush?>(nameof(Stroke));

    static WomenswearSkirtIcon()
    {
        // 描边变化（含深浅主题切换时覆盖层刷新画刷）需要重绘
        AffectsRender<WomenswearSkirtIcon>(StrokeProperty);
    }

    /// <summary>获取或设置图标描边画刷。</summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// 设计稿中的裙腰圆角矩形（对应 SVG 的 <c>rect x=5.35 y=4 width=13.3 height=3.5 rx=ry=1.66</c>）。
    /// 用 <see cref="DrawingContext.DrawRectangle"/> 的圆角重载绘制，避免依赖路径字符串的圆弧语法。
    /// </summary>
    private static readonly Rect WaistRect = new(5.35, 4, 13.3, 3.5);

    /// <summary>裙腰圆角半径（SVG 中的 rx / ry）。</summary>
    private const double WaistCornerRadius = 1.66;

    /// <summary>
    /// 设计稿路径：裙身轮廓 + 3 条裙摆褶皱线，与 SVG 的坐标完全一致，缩放后比例不变。
    /// （裙腰圆角矩形单独用 <see cref="WaistRect"/> 绘制，见上。）
    /// </summary>
    private static readonly Geometry SkirtGeometry = StreamGeometry.Parse(
        "M 6.78,7.5 L 3,19 Q 12,21 21,19 L 17.22,7.5 Z " +
        "M 9.39,7.5 L 7.5,19.75 " +
        "M 12,7.5 L 12,20 " +
        "M 14.61,7.5 L 16.5,19.75");

    public WomenswearSkirtIcon()
    {
        // 纯装饰图标：不参与命中测试，避免抢占覆盖层按钮 / 滚动的指针事件
        IsHitTestVisible = false;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (Stroke is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        // 等比缩放：取宽高较小值，保证非正方形布局时图标不被拉伸；多出的空间用于居中
        var scale = Math.Min(Bounds.Width, Bounds.Height) / DesignSize;
        if (scale <= 0)
        {
            return;
        }

        var offsetX = (Bounds.Width - DesignSize * scale) / 2;
        var offsetY = (Bounds.Height - DesignSize * scale) / 2;

        // 先缩放到控件尺寸再平移居中（Avalonia 的矩阵乘法为行向量语义：a * b 即先 a 后 b）；
        // 变换同样作用于画笔，描边宽度随图标一起等比缩放，视觉效果与 SVG 一致
        var pen = new Pen(Stroke, DesignStrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        using (context.PushTransform(
                   Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offsetX, offsetY)))
        {
            context.DrawRectangle(null, pen, WaistRect, WaistCornerRadius, WaistCornerRadius);
            context.DrawGeometry(null, pen, SkirtGeometry);
        }
    }
}
