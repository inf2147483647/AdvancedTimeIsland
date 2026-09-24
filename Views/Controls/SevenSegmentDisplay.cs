using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// 七段数码管显示控件。
/// 七段数码管由「三横四纵」七个发光二极管（段 a~g，另有小数点 dp）封装而成，
/// 本控件即以真实的七段笔画绘制时间（如 11:45:14）：
/// 每段的边框使用强调色（默认红色），段内为径向渐变——四周为强调色，向中心依次过渡
/// 到橙、白（即“从四周到中心逐渐变白”），模拟发光管灯芯；
/// 未点亮的段以强调色的极低透明度保留微亮，模拟真实数码管的漏光效果。
/// 开启倾斜模式后，纵向数码管向右顺时针倾斜 6°（现代数码管多为斜体显示）。
/// </summary>
public class SevenSegmentDisplay : Control
{
    // 段位掩码：bit0=a(上) bit1=b(右上) bit2=c(右下) bit3=d(下) bit4=e(左下) bit5=f(左上) bit6=g(中)
    private static readonly int[] DigitMasks = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };

    private const int SegmentCount = 7;
    private const int MiddleSegment = 6;
    private const int BottomSegment = 3;
    private const int MiddleBarMask = 0x40;

    private const double DigitAspect = 0.45;         // 单个数码宽度 / 数码高度
    private const double ThicknessRatio = 0.13;      // 笔画厚度 / 数码高度
    private const double DigitSpacingRatio = 0.14;   // 相邻字符间距 / 数码高度
    private const double SeparatorWidthRatio = 0.28; // 冒号占宽 / 数码高度
    private const double JointGapRatio = 0.30;       // 段端头间隙 / 笔画厚度
    private const double HorizontalPaddingRatio = 0.12;
    private const double VerticalPaddingRatio = 0.08;
    private const double HeightToFontSizeRatio = 1.6; // 数码高度（缩放 1.0 时）/ 正文字号
    private const double DefaultZoom = 0.8;
    private const double SkewDegrees = 6.0;          // 倾斜模式角度（向右顺时针）
    private const double DefaultBaseFontSize = 16.0;

    private const int AnimationDurationMs = 250;     // 数码变化时的渐隐 + 渐显总时长
    private const int AnimationFrameIntervalMs = 16;

    private static readonly double SkewTangent = Math.Tan(SkewDegrees * Math.PI / 180.0);

    private static readonly Color CoreColor = Color.FromRgb(255, 165, 0);
    private static readonly Color CenterColor = Colors.White;

    private string _text = "--:--:--";
    private string _previousText = string.Empty;
    private bool _skewEnabled;
    private bool _separatorLit = true;
    private bool _transitionAnimationEnabled = true;
    private double _baseFontSize = DefaultBaseFontSize;
    private double _zoom = DefaultZoom;
    private Color _accentColor = Colors.Red;

    private double _digitHeight;
    private double _digitWidth;
    private double _thickness;
    private double _spacing;
    private double _separatorWidth;

    private IBrush? _litBrush;
    private IBrush? _offBrush;
    private IPen? _litPen;
    private IPen? _offPen;
    private IPen? _glowPen;

    private bool _brushCacheValid;
    private Color _cachedAccent = Colors.Transparent;
    private double _cachedThickness = -1;
    private bool _cachedDarkTheme;

    private bool _isTransitioning;
    private long _transitionStartTimestamp;
    private DispatcherTimer? _animationTimer;

    /// <summary>
    /// 显示文本，仅识别数字、'-' 与 ':'。
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            var newText = value ?? string.Empty;
            if (_text == newText)
            {
                return;
            }

            var previousText = _text;
            _text = newText;

            if (_transitionAnimationEnabled && previousText.Length == newText.Length && newText.Length > 0)
            {
                _previousText = previousText;
                _transitionStartTimestamp = Environment.TickCount64;
                _isTransitioning = true;
                EnsureAnimationTimer();
            }
            else
            {
                _isTransitioning = false;
            }

            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// 倾斜模式：开启后纵向数码管向右顺时针倾斜 6°。
    /// </summary>
    public bool SkewEnabled
    {
        get => _skewEnabled;
        set
        {
            if (_skewEnabled != value)
            {
                _skewEnabled = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// 分隔符（冒号）当前是否点亮。
    /// </summary>
    public bool SeparatorLit
    {
        get => _separatorLit;
        set
        {
            if (_separatorLit != value)
            {
                _separatorLit = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// 数码变化时是否使用 0.25s 的渐隐、渐显过渡动画。
    /// </summary>
    public bool TransitionAnimationEnabled
    {
        get => _transitionAnimationEnabled;
        set
        {
            if (_transitionAnimationEnabled != value)
            {
                _transitionAnimationEnabled = value;
                if (!value)
                {
                    _isTransitioning = false;
                    _animationTimer?.Stop();
                }
            }
        }
    }

    /// <summary>
    /// 缩放倍数（0.6~1.0，默认 0.8），用于整体放大或缩小数码管。
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (double.IsNaN(value) || value <= 0)
            {
                return;
            }

            if (Math.Abs(_zoom - value) > 0.0001)
            {
                _zoom = value;
                InvalidateMeasure();
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// 基准字号：数码高度按 <see cref="HeightToFontSizeRatio"/> 与 <see cref="Zoom"/> 换算，
    /// 从而跟随主题正文字号自适应。
    /// </summary>
    public double BaseFontSize
    {
        get => _baseFontSize;
        set
        {
            if (double.IsNaN(value) || value <= 0)
            {
                return;
            }

            if (Math.Abs(_baseFontSize - value) > 0.01)
            {
                _baseFontSize = value;
                InvalidateMeasure();
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// 强调色（默认为红色）：数码管边框与点亮段渐变的外圈色。
    /// </summary>
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor != value)
            {
                _accentColor = value;
                RebuildBrushes();
                InvalidateVisual();
            }
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        RebuildBrushes();
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }

        _isTransitioning = false;
        _animationTimer?.Stop();
        _animationTimer = null;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        RebuildBrushes();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var naturalHeight = _baseFontSize * HeightToFontSizeRatio * _zoom;

        ApplyMetrics(naturalHeight);
        var naturalWidth = MeasureTextWidth(CurrentText) + naturalHeight * HorizontalPaddingRatio * 2;
        var naturalTotalHeight = naturalHeight + naturalHeight * VerticalPaddingRatio * 2;

        var scale = 1.0;
        if (!double.IsInfinity(availableSize.Width) && availableSize.Width > 0 && naturalWidth > availableSize.Width)
        {
            scale = Math.Min(scale, availableSize.Width / naturalWidth);
        }
        if (!double.IsInfinity(availableSize.Height) && availableSize.Height > 0 && naturalTotalHeight > availableSize.Height)
        {
            scale = Math.Min(scale, availableSize.Height / naturalTotalHeight);
        }
        scale = Math.Max(0.1, Math.Min(1.0, scale));

        ApplyMetrics(naturalHeight * scale);
        EnsureBrushes();

        return new Size(naturalWidth * scale, naturalTotalHeight * scale);
    }

    public override void Render(DrawingContext context)
    {
        if (_digitHeight <= 0)
        {
            return;
        }

        var text = CurrentText;
        var contentWidth = MeasureTextWidth(text);
        var startX = Math.Max(0, (Bounds.Width - contentWidth) / 2);
        var top = Math.Max(0, (Bounds.Height - _digitHeight) / 2);

        EnsureBrushes();

        var isAnimating = _isTransitioning;
        var progress = 0.0;
        if (isAnimating)
        {
            progress = Math.Clamp(
                (Environment.TickCount64 - _transitionStartTimestamp) / (double)AnimationDurationMs, 0, 1);
            if (progress >= 1)
            {
                isAnimating = false;
                _isTransitioning = false;
            }
        }

        var x = startX;
        var isFirst = true;
        var index = 0;
        foreach (var ch in text)
        {
            var currentIndex = index++;
            var cellWidth = MeasureCellWidth(ch);
            if (cellWidth <= 0)
            {
                continue;
            }

            if (!isFirst)
            {
                x += _spacing;
            }
            isFirst = false;

            if (ch == ':')
            {
                // 分隔符由 SeparatorLit 单独驱动，不参与数码变化的过渡动画
                DrawSeparator(context, x, top);
            }
            else
            {
                var previousCh = isAnimating && currentIndex < _previousText.Length ? _previousText[currentIndex] : ch;
                var isChanged = isAnimating && previousCh != ch;

                if (!isChanged)
                {
                    DrawDigit(context, GetSegmentMask(ch), x, top);
                }
                else
                {
                    // 先后半程渐隐旧字形，后半程渐显新字形
                    var fadingOut = progress < 0.5;
                    var opacity = fadingOut ? 1 - progress * 2 : (progress - 0.5) * 2;
                    using (context.PushOpacity(opacity))
                    {
                        DrawDigit(context, GetSegmentMask(fadingOut ? previousCh : ch), x, top);
                    }
                }
            }

            x += cellWidth;
        }
    }

    private string CurrentText => string.IsNullOrEmpty(_text) ? "--:--:--" : _text;

    private void ApplyMetrics(double digitHeight)
    {
        _digitHeight = digitHeight;
        _digitWidth = digitHeight * DigitAspect;
        _thickness = digitHeight * ThicknessRatio;
        _spacing = digitHeight * DigitSpacingRatio;
        _separatorWidth = digitHeight * SeparatorWidthRatio;
    }

    private double MeasureCellWidth(char ch)
    {
        if (ch is >= '0' and <= '9' || ch == '-')
        {
            return _digitWidth;
        }

        return ch == ':' ? _separatorWidth : 0;
    }

    private double MeasureTextWidth(string text)
    {
        double total = 0;
        var count = 0;
        foreach (var ch in text)
        {
            var cellWidth = MeasureCellWidth(ch);
            if (cellWidth <= 0)
            {
                continue;
            }

            total += cellWidth;
            count++;
        }

        if (count > 1)
        {
            total += _spacing * (count - 1);
        }

        return total;
    }

    private static int GetSegmentMask(char ch)
    {
        if (ch is >= '0' and <= '9')
        {
            return DigitMasks[ch - '0'];
        }

        // '-' 只点亮中横，作为未取到时间时的占位
        return ch == '-' ? MiddleBarMask : 0;
    }

    private void EnsureAnimationTimer()
    {
        if (_animationTimer == null)
        {
            // 三参数重载在 Avalonia 11（FA2）与 12（FA3）下均可用，计时器基于当前 UI 线程的 Dispatcher
            _animationTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(AnimationFrameIntervalMs),
                DispatcherPriority.Normal,
                OnAnimationTick);
            _animationTimer.Stop();
        }

        if (!_animationTimer.IsEnabled)
        {
            _animationTimer.Start();
        }
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (!_isTransitioning || Environment.TickCount64 - _transitionStartTimestamp >= AnimationDurationMs)
        {
            _isTransitioning = false;
            _animationTimer?.Stop();
        }

        InvalidateVisual();
    }

    private void EnsureBrushes()
    {
        var isDark = ThemeHelper.IsDarkTheme();
        if (_brushCacheValid && _cachedAccent == _accentColor &&
            Math.Abs(_cachedThickness - _thickness) < 0.01 && _cachedDarkTheme == isDark)
        {
            return;
        }

        RebuildBrushes();
    }

    private void RebuildBrushes()
    {
        var accent = _accentColor;

        // 段内径向渐变：四周为强调色，向中心依次过渡到橙、白
        _litBrush = CreateRadialGradient(accent);
        _litPen = new Pen(new SolidColorBrush(accent), Math.Max(0.7, _thickness * 0.14));
        _glowPen = new Pen(new SolidColorBrush(accent, 0.28), Math.Max(1.0, _thickness * 0.75));

        var isDark = ThemeHelper.IsDarkTheme();
        _offBrush = new SolidColorBrush(accent, isDark ? 0.10 : 0.15);
        _offPen = new Pen(new SolidColorBrush(accent, isDark ? 0.16 : 0.24), Math.Max(0.5, _thickness * 0.10));

        _cachedAccent = accent;
        _cachedThickness = _thickness;
        _cachedDarkTheme = isDark;
        _brushCacheValid = true;
    }

    /// <summary>
    /// 生成“四周 → 中心”的径向渐变：渐变坐标相对各段自身边界框，
    /// 半径取默认值 0.5（相对单位），恰好内切于边界框，因此从四周到中心逐渐变白。
    /// </summary>
    private static IBrush CreateRadialGradient(Color accent)
    {
        var center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        var gradient = new RadialGradientBrush
        {
            Center = center,
            GradientOrigin = center,
            // 半径取边界框的一半（相对单位），使渐变恰好内切于各段自身边界框
            RadiusX = RelativeScalar.Middle,
            RadiusY = RelativeScalar.Middle
        };
        gradient.GradientStops.Add(new GradientStop(CenterColor, 0));
        gradient.GradientStops.Add(new GradientStop(CoreColor, 0.5));
        gradient.GradientStops.Add(new GradientStop(accent, 1));
        return gradient;
    }

    private void DrawDigit(DrawingContext context, int mask, double x, double y)
    {
        for (var segment = 0; segment < SegmentCount; segment++)
        {
            var points = BuildSegmentPoints(segment, x, y);
            var isLit = (mask & (1 << segment)) != 0;
            DrawSegment(context, points, isLit);
        }
    }

    private Point[] BuildSegmentPoints(int segment, double x, double y)
    {
        var half = _thickness * 0.5;
        var gap = _thickness * JointGapRatio;
        var middleY = y + _digitHeight * 0.5;
        var leftX = x + half;
        var rightX = x + _digitWidth - half;
        var topY = y + half;
        var bottomY = y + _digitHeight - half;

        switch (segment)
        {
            // a 上横
            case 0:
                return HorizontalPoints(leftX + gap, rightX - gap, topY, half);
            // g 中横
            case MiddleSegment:
                return HorizontalPoints(leftX + gap, rightX - gap, middleY, half);
            // d 下横
            case BottomSegment:
                return HorizontalPoints(leftX + gap, rightX - gap, bottomY, half);
            // f 左上竖
            case 5:
                return VerticalPoints(leftX, topY + gap, middleY - half - gap, half, y);
            // b 右上竖
            case 1:
                return VerticalPoints(rightX, topY + gap, middleY - half - gap, half, y);
            // e 左下竖
            case 4:
                return VerticalPoints(leftX, middleY + half + gap, bottomY - gap, half, y);
            // c 右下竖
            default:
                return VerticalPoints(rightX, middleY + half + gap, bottomY - gap, half, y);
        }
    }

    private static Point[] HorizontalPoints(double x0, double x1, double centerY, double half)
    {
        return new[]
        {
            new Point(x0, centerY),
            new Point(x0 + half, centerY - half),
            new Point(x1 - half, centerY - half),
            new Point(x1, centerY),
            new Point(x1 - half, centerY + half),
            new Point(x0 + half, centerY + half)
        };
    }

    private Point[] VerticalPoints(double centerX, double y0, double y1, double half, double digitTop)
    {
        var points = new[]
        {
            new Point(centerX, y0),
            new Point(centerX + half, y0 + half),
            new Point(centerX + half, y1 - half),
            new Point(centerX, y1),
            new Point(centerX - half, y1 - half),
            new Point(centerX - half, y0 + half)
        };

        if (!_skewEnabled)
        {
            return points;
        }

        // 以数码上下中心为基准做错切，使纵向数码管向右顺时针倾斜 6°
        var centerY = digitTop + _digitHeight * 0.5;
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new Point(points[i].X + (centerY - points[i].Y) * SkewTangent, points[i].Y);
        }

        return points;
    }

    private void DrawSeparator(DrawingContext context, double x, double y)
    {
        var size = _digitHeight * 0.16;
        var centerX = x + _separatorWidth * 0.5;
        DrawDot(context, centerX, y + _digitHeight * 0.32, size);
        DrawDot(context, centerX, y + _digitHeight * 0.68, size);
    }

    private void DrawDot(DrawingContext context, double centerX, double centerY, double size)
    {
        var rect = new Rect(centerX - size * 0.5, centerY - size * 0.5, size, size);
        var radius = size * 0.2;

        if (!_separatorLit)
        {
            context.DrawRectangle(_offBrush, _offPen, rect, radius, radius);
            return;
        }

        if (_glowPen != null)
        {
            context.DrawRectangle(null, _glowPen, rect, radius, radius);
        }

        context.DrawRectangle(_litBrush, _litPen, rect, radius, radius);
    }

    private void DrawSegment(DrawingContext context, Point[] points, bool isLit)
    {
        var geometry = CreateGeometry(points);

        if (!isLit)
        {
            context.DrawGeometry(_offBrush, _offPen, geometry);
            return;
        }

        if (_glowPen != null)
        {
            context.DrawGeometry(null, _glowPen, geometry);
        }

        context.DrawGeometry(_litBrush, _litPen, geometry);
    }

    private static Geometry CreateGeometry(Point[] points)
    {
        var figure = new PathFigure
        {
            StartPoint = points[0],
            IsClosed = true,
            IsFilled = true
        };

        var segments = new PathSegments();
        for (var i = 1; i < points.Length; i++)
        {
            segments.Add(new LineSegment { Point = points[i] });
        }

        figure.Segments = segments;

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
