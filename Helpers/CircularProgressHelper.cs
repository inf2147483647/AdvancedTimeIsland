using System;
using Avalonia;
using Avalonia.Media;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 圆形进度环的几何计算助手：把进度百分比转换为从 12 点方向顺时针绘制的圆弧路径。
/// </summary>
public static class CircularProgressHelper
{
    /// <summary>
    /// 创建进度圆弧几何。百分比 &lt;= 0 时返回 null（不绘制圆弧）。
    /// </summary>
    /// <param name="percentage">进度百分比（0–100）。</param>
    /// <param name="size">进度环外接正方形边长。</param>
    /// <param name="strokeThickness">圆弧线宽。</param>
    public static Geometry? CreateArcGeometry(double percentage, double size, double strokeThickness)
    {
        var radius = (size / 2) - (strokeThickness / 2);
        var center = new Point(size / 2, size / 2);

        if (percentage <= 0)
        {
            return null;
        }
        // 100% 时圆弧首尾重合会退化，收敛到接近整圆的角度。
        if (percentage >= 100)
        {
            percentage = 99.9999;
        }

        var angle = (percentage / 100) * 360;
        var startPoint = new Point(center.X, center.Y - radius);

        var angleRad = (Math.PI / 180.0) * (angle - 90);
        var endPoint = new Point(
            center.X + radius * Math.Cos(angleRad),
            center.Y + radius * Math.Sin(angleRad));

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            Segments = new PathSegments
            {
                new ArcSegment
                {
                    Point = endPoint,
                    Size = new Size(radius, radius),
                    IsLargeArc = angle > 180,
                    SweepDirection = SweepDirection.Clockwise,
                    IsStroked = true
                }
            },
            IsClosed = false
        };

        var geometry = new PathGeometry();
        geometry.Figures?.Add(figure);
        return geometry;
    }
}