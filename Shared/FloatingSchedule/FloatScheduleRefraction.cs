// 液态玻璃"真实折射"核心（共享层，纯 System/Avalonia）。
//
// 数学移植自 Kyant/backdrop 的 RoundedRectRefractionShaderString（AGSL，见 D:\temp\AndroidLiquidGlass-kmp）：
//   ① 圆角矩形 SDF：边缘带 (-sd < refractionHeight) 内才发生位移，带外直通；
//   ② 圆弧剖面：d = -circleMap(1 - (-sd)/refractionHeight) * refractionAmount，circleMap(x)=1-sqrt(1-x²)；
//   ③ 位移方向 = SDF 梯度（外法向）(+ depthEffect * 径向分量)；
//   ④ 色散：intensity = chromaticAberration * (cx*cy)/(halfW*halfH)，沿位移方向做 7 抽样错位（权重与参考一致）；
//   ⑤ 镜面高光：intensity = |dot(SDF 梯度, 光向)|^falloff，直接叠加进结果（等价参考里的 Plus 混合）。
// 另：圆角外的像素置为全透明 —— 这样折射位图自身就带圆角，不必再额外 Clip。
//
// 为什么不用 GPU shader：net8 宿主只带 SkiaSharp 2.88（无 SKRuntimeEffect），且双宿主 SkiaSharp 版本分叉，
//   共享层触碰原生 SK API 会让双 TFM 脆断。这里改用纯 CPU 逐像素重采样：
//   卡片量级约 200×300（6 万像素 × 7 抽样），单次毫秒级；只在几何/参数变化时重建，不逐帧重算。
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public static class FloatScheduleRefraction
{
    /// <summary>
    /// 把 BGRA(premul) 源缓冲按圆角矩形 SDF 折射写入目标缓冲（两者等长，尺寸同为 w×h）。
    /// corner 为圆角半径(px)；refractionHeight 为折射带高度(px)；refractionAmount 为最大位移量(px)；
    /// depthEffect 追加径向分量；chromaticAberration 为色散强度(0~1)；
    /// highlightStrength/角度/falloff 控制 SDF 梯度镜面高光（0 表示不加）。
    /// 【卡片 = 缓冲内的一个子矩形（可选）】圆角矩形 SDF 的定义域可由 cardOffsetX/cardOffsetY/cardW/cardH 指定：
    ///   此时缓冲可**比卡片大**（外扩区），卡片位于 (cardOffsetX, cardOffsetY)、尺寸 cardW×cardH；
    ///   卡片圆角外（含整个外扩区）一律置透明，只有卡片矩形内才做折射 —— 于是"已折射图"本身就带着卡片，
    ///   调用方只要把卡片子矩形取样出来显示即可（拖动时改取样矩形 = 纯 GPU 平移）。
    ///   keepOutsidePixels=true 时改为"卡片圆角形状之外的像素保持源图原样"：供"外扩缓冲 + 取样矩形平移"用 ——
    ///   平移后露出的外扩区/卡片外沿必须显示**真实桌面**而不是透明，否则卡片边缘会出现透明空洞
    ///   （圆角裁剪由承载 Border 的 CornerRadius 负责，不依赖位图自带的透明角）。
    ///   默认值（cardW/cardH ≤ 0 视为等于 w/h、偏移 0、keepOutsidePixels=false）下与"卡片正好填满缓冲"的老行为**逐字节一致**。
    /// </summary>
    public static void Refract(byte[] src, byte[] dst, int w, int h, double corner,
        double refractionHeight, double refractionAmount, bool depthEffect = false,
        double chromaticAberration = 0.0, double highlightStrength = 0.0,
        double highlightAngleDeg = 45.0, double highlightFalloff = 1.0,
        int cardOffsetX = 0, int cardOffsetY = 0, int cardW = 0, int cardH = 0,
        bool keepOutsidePixels = false)
    {
        if (w <= 0 || h <= 0) return;
        int len = Math.Min(src.Length, dst.Length);
        Buffer.BlockCopy(src, 0, dst, 0, len);   // 默认直通（带外与带内退化的像素保持原样）

        // 卡片矩形（圆角矩形 SDF 的定义域）。默认与缓冲等大、位于原点 → 与老行为完全一致。
        int cw = cardW > 0 ? cardW : w;
        int ch = cardH > 0 ? cardH : h;
        double halfW = cw * 0.5, halfH = ch * 0.5;
        // 卡片中心在**缓冲坐标系**里的位置（像素 (x,y) 的卡片局部坐标 = (x+0.5) - 中心）
        double centerX = cardOffsetX + halfW, centerY = cardOffsetY + halfH;
        double r = Math.Clamp(corner, 0, Math.Min(halfW, halfH));
        double gradRadius = Math.Min(r * 1.5, Math.Min(halfW, halfH));
        double rh = Math.Max(1e-4, refractionHeight);
        double ang = highlightAngleDeg * Math.PI / 180.0;
        double cA = Math.Cos(ang), sA = Math.Sin(ang);
        double invHalf = 1.0 / Math.Max(1e-6, halfW * halfH);

        for (int y = 0; y < h; y++)
        {
            double cy = y + 0.5 - centerY;
            for (int x = 0; x < w; x++)
            {
                double cx = x + 0.5 - centerX;
                int o = (y * w + x) * 4;

                double sd = SdRoundedRect(cx, cy, halfW, halfH, r);
                if (sd > 0)
                {
                    // 圆角形状之外：默认置透明（自带圆角）；keepOutsidePixels 时保持源图原样（外扩缓冲用）
                    if (!keepOutsidePixels) { dst[o] = 0; dst[o + 1] = 0; dst[o + 2] = 0; dst[o + 3] = 0; }
                    continue;
                }
                double depth = -sd;
                if (depth >= rh) continue;                                                                 // 带外 → 直通

                double d = -CircleMap(1.0 - depth / rh) * refractionAmount;
                GradSdRoundedRect(cx, cy, halfW, halfH, gradRadius, out double gx, out double gy);
                if (depthEffect)
                {
                    double rl = Math.Sqrt(cx * cx + cy * cy);
                    if (rl > 1e-6) { gx += cx / rl; gy += cy / rl; }
                }
                double gl = Math.Sqrt(gx * gx + gy * gy);
                if (gl > 1e-6) { gx /= gl; gy /= gl; }
                else { gx = 0; gy = 0; }

                double sx = x + d * gx, sy = y + d * gy;
                double dispI = chromaticAberration * (cx * cy) * invHalf;
                double ddx = d * gx * dispI, ddy = d * gy * dispI;

                // 7 抽样色散（权重与参考实现一致；chromaticAberration=0 时 7 抽样重合，权重归一 → 等价于单抽样）
                double rr = 0, gg = 0, bb = 0, aa = 0;
                Tap(src, w, h, sx + ddx, sy + ddy, 1.0 / 3.5, 0, 0, ref rr, ref gg, ref bb, ref aa);
                Tap(src, w, h, sx + ddx * (2.0 / 3.0), sy + ddy * (2.0 / 3.0), 1.0 / 3.5, 1.0 / 7.0, 0, ref rr, ref gg, ref bb, ref aa);
                Tap(src, w, h, sx + ddx * (1.0 / 3.0), sy + ddy * (1.0 / 3.0), 1.0 / 3.5, 1.0 / 3.5, 0, ref rr, ref gg, ref bb, ref aa);
                Tap(src, w, h, sx, sy, 0, 1.0 / 3.5, 0, ref rr, ref gg, ref bb, ref aa);
                Tap(src, w, h, sx - ddx * (1.0 / 3.0), sy - ddy * (1.0 / 3.0), 0, 1.0 / 3.5, 1.0 / 3.0, ref rr, ref gg, ref bb, ref aa);
                Tap(src, w, h, sx - ddx * (2.0 / 3.0), sy - ddy * (2.0 / 3.0), 0, 0, 1.0 / 3.0, ref rr, ref gg, ref bb, ref aa);
                Tap(src, w, h, sx - ddx, sy - ddy, 1.0 / 7.0, 0, 1.0 / 3.0, ref rr, ref gg, ref bb, ref aa);

                if (highlightStrength > 0)
                {
                    double dot = gx * cA + gy * sA;
                    double hi = Math.Pow(Math.Abs(dot), Math.Max(1e-3, highlightFalloff)) * highlightStrength;
                    rr += hi; gg += hi; bb += hi;
                }

                dst[o] = Clamp(rr); dst[o + 1] = Clamp(gg); dst[o + 2] = Clamp(bb); dst[o + 3] = Clamp(aa);
            }
        }
    }

    // 取一次样并按键权重累加（源坐标 clamp 到边界）。
    // 注意 alpha 不做边缘渐隐：源是"卡片正下方那块背景"的切片、本身不透明，
    // 若按 shuding 的做法给边缘像素乘渐隐系数，会在位图最外圈留下 2px 透明带（卡片边出现穿帮缝隙）。
    static void Tap(byte[] src, int w, int h, double sx, double sy,
        double wR, double wG, double wB, ref double r, ref double g, ref double b, ref double a)
    {
        if (wR == 0 && wG == 0 && wB == 0) return;
        int ix = (int)Math.Round(sx), iy = (int)Math.Round(sy);
        if (ix < 0) ix = 0; else if (ix >= w) ix = w - 1;
        if (iy < 0) iy = 0; else if (iy >= h) iy = h - 1;
        int o = (iy * w + ix) * 4;
        r += src[o] * wR;
        g += src[o + 1] * wG;
        b += src[o + 2] * wB;
        a += src[o + 3] / 7.0;
    }

    static byte Clamp(double v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : (int)Math.Round(v));

    static double CircleMap(double x) => x <= 0 ? 0 : x >= 1 ? 1 : 1.0 - Math.Sqrt(1.0 - x * x);

    static double SdRoundedRect(double cx, double cy, double halfW, double halfH, double r)
    {
        double qx = Math.Abs(cx) - (halfW - r);
        double qy = Math.Abs(cy) - (halfH - r);
        double ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
        double outside = Math.Sqrt(ox * ox + oy * oy) - r;
        double inside = Math.Min(Math.Max(qx, qy), 0);
        return outside + inside;
    }

    static void GradSdRoundedRect(double cx, double cy, double halfW, double halfH, double r, out double gx, out double gy)
    {
        double qx = Math.Abs(cx) - (halfW - r);
        double qy = Math.Abs(cy) - (halfH - r);
        double sx = Math.Sign(cx), sy = Math.Sign(cy);
        if (qx >= 0 || qy >= 0)
        {
            double mx = Math.Max(qx, 0), my = Math.Max(qy, 0);
            double l = Math.Sqrt(mx * mx + my * my);
            if (l < 1e-6) { gx = sx; gy = sy; }
            else { gx = sx * mx / l; gy = sy * my / l; }
        }
        else
        {
            double t = qx >= qy ? 1.0 : 0.0;
            gx = sx * t; gy = sy * (1.0 - t);
        }
    }

    /// <summary>把 BGRA(premul) 缓冲包成 Avalonia 位图（供卡片背景直接显示）。</summary>
    public static WriteableBitmap? ToBitmap(byte[] bgra, int w, int h)
    {
        if (w <= 0 || h <= 0 || bgra.Length < w * h * 4) return null;
        var bmp = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = bmp.Lock())
        {
            if (fb.RowBytes == w * 4)
            {
                Marshal.Copy(bgra, 0, fb.Address, w * h * 4);
            }
            else
            {
                for (int y = 0; y < h; y++)
                    Marshal.Copy(bgra, y * w * 4, fb.Address + y * fb.RowBytes, w * 4);
            }
        }
        return bmp;
    }
}
