// 真实悬浮窗"背景采样"：透明窗口拿不到 DWM 合成的桌面像素，唯一现实来源是屏幕捕获。
//
// 【完整桌面采样时临时排除自身】要拍到"所有下方窗口 + 壁纸"，同时不能把悬浮窗自己拍进去：
//   采样前把悬浮窗临时设为 WDA_EXCLUDEFROMCAPTURE，抓完立刻恢复原亲和性（只持续一次抓帧）。
//   远程会话（SM_REMOTESESSION）不走这条路——排除自身会让远端看不到悬浮窗，
//   由调用方改用"下方窗口"单窗口采样。
//
// 只允许 System / Avalonia（DllImport 属于 System），与主项目、子进程同源编译。
using System;
using System.Runtime.InteropServices;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public static class FloatScheduleScreenCapture
{
    const int SRCCOPY = 0x00CC0020;
    const int CAPTUREBLT = 0x40000000;   // 必须叠加：否则 BitBlt 会跳过 WS_EX_LAYERED 窗口

    const uint WDA_NONE = 0x00000000;
    const uint WDA_MONITOR = 0x00000001;
    const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
    const int SM_REMOTESESSION = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public int biSize, biWidth, biHeight;
        public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public uint[] bmiColors;
    }

    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    [DllImport("user32.dll")] static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr h);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines, byte[] bits, ref BITMAPINFO bi, uint usage);


    /// <summary>
    /// 纯抓帧：抓"该窗口所占屏幕区域"到 dst（BGRA，Premul，top-down，长度需 ≥ w*h*4）。
    /// w/h 为**物理像素**尺寸；位置由 GetWindowRect 取。
    /// 不做任何捕获亲和性切换（见文件头说明）。
    /// </summary>
    public static bool TryCaptureBehindWindow(IntPtr hwnd, byte[] dst, int w, int h)
    {
        if (hwnd == IntPtr.Zero || w <= 0 || h <= 0 || dst.Length < w * h * 4) return false;
        if (!GetWindowRect(hwnd, out RECT r)) return false;
        int cw = r.Right - r.Left, ch = r.Bottom - r.Top;
        if (cw <= 0 || ch <= 0) return false;
        // 窗口物理尺寸与请求尺寸不一致时按请求尺寸取左上角（调用方通常已按 DPI 换算，正常应一致）
        return CaptureRegion(r.Left, r.Top, Math.Min(w, cw), Math.Min(h, ch), dst, w, h);
    }

    /// <summary>取窗口"客户区/窗口矩形"的屏幕物理像素原点（抓帧区域左上角）。</summary>
    public static bool TryGetWindowOrigin(IntPtr hwnd, out int x, out int y)
    {
        x = y = 0;
        if (hwnd == IntPtr.Zero) return false;
        if (!GetWindowRect(hwnd, out RECT r)) return false;
        x = r.Left;
        y = r.Top;
        return true;
    }

    /// <summary>
    /// 本次采样期间把窗口排除出捕获（WDA_EXCLUDEFROMCAPTURE），并返回原始亲和性供抓完后恢复。
    /// 若窗口本来就已排除（用户的"防截图"开启），直接返回 true（无需恢复，恢复也是同一个值）。
    /// </summary>
    public static bool TryExcludeFromCapture(IntPtr hwnd, out uint originalAffinity)
    {
        originalAffinity = WDA_NONE;
        if (hwnd == IntPtr.Zero) return false;
        try
        {
            if (!GetWindowDisplayAffinity(hwnd, out originalAffinity)) originalAffinity = WDA_NONE;
            if (originalAffinity == WDA_EXCLUDEFROMCAPTURE) return true;
            if (SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)) return true;
            if (SetWindowDisplayAffinity(hwnd, WDA_MONITOR)) return true;   // Win10 2004 之前的回退
        }
        catch { }
        return false;
    }

    /// <summary>窗口当前是否已被系统排除出捕获（WDA_MONITOR / WDA_EXCLUDEFROMCAPTURE）。</summary>
    public static bool IsProtectedFromCapture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        try { return GetWindowDisplayAffinity(hwnd, out uint a) && a != WDA_NONE; } catch { return false; }
    }

    /// <summary>恢复采样前的捕获亲和性（WDA_NONE / WDA_MONITOR / WDA_EXCLUDEFROMCAPTURE）。</summary>
    public static void RestoreCaptureAffinity(IntPtr hwnd, uint affinity)
    {
        if (hwnd == IntPtr.Zero) return;
        try { SetWindowDisplayAffinity(hwnd, affinity); } catch { }
    }

    /// <summary>当前是否为远程会话（RDP）。远程会话下不做"排除自身"，否则远端看不到悬浮窗。</summary>
    public static bool IsRemoteSession()
    {
        try { return GetSystemMetrics(SM_REMOTESESSION) != 0; } catch { return false; }
    }

    /// <summary>等待 DWM 完成一次合成：让刚设置的捕获亲和性真正反映到桌面合成帧上。</summary>
    public static void WaitForComposition()
    {
        try { DwmFlush(); } catch { }
    }

    /// <summary>
    /// 抓屏幕上任意区域到 dst（BGRA，top-down，stride = w*4，长度需 ≥ w*h*4）。
    /// 这是 GDI 回退路径（BitBlt）—— 单次固定 24ms 左右，仅在 DXGI 不可用时使用。
    /// 不做任何捕获亲和性切换（见文件头说明）。
    /// </summary>
    public static bool TryCaptureScreenRegion(int x, int y, int w, int h, byte[] dst)
    {
        if (w <= 0 || h <= 0 || dst.Length < w * h * 4) return false;
        return CaptureRegion(x, y, w, h, dst, w, h);
    }

    static bool CaptureRegion(int x, int y, int w, int h, byte[] dst, int strideW, int strideH)
    {
        IntPtr screen = GetDC(IntPtr.Zero);
        IntPtr mem = CreateCompatibleDC(screen);
        IntPtr bmp = CreateCompatibleBitmap(screen, strideW, strideH);
        IntPtr old = SelectObject(mem, bmp);
        bool ok = BitBlt(mem, 0, 0, w, h, screen, x, y, SRCCOPY | CAPTUREBLT);

        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = 40, biWidth = strideW, biHeight = -strideH, biPlanes = 1, biBitCount = 32,
                biCompression = 0, biSizeImage = strideW * strideH * 4,
            },
            bmiColors = new uint[4],
        };
        var tmp = new byte[strideW * strideH * 4];
        int got = GetDIBits(mem, bmp, 0, (uint)strideH, tmp, ref bmi, 0);

        SelectObject(mem, old);
        DeleteObject(bmp);
        DeleteDC(mem);
        ReleaseDC(IntPtr.Zero, screen);

        if (got <= 0) return false;
        // 抓到的整块贴回 dst（只取有效区，避免越界；未覆盖到的部分保持原值）
        for (int row = 0; row < Math.Min(strideH, got); row++)
            Buffer.BlockCopy(tmp, row * strideW * 4, dst, row * strideW * 4, strideW * 4);
        return true && ok;
    }

    /// <summary>在任意线程上做纯计算：把已抓到的背景缓冲折射成卡片背景（不碰 UI 对象）。</summary>
    public static void RefractForCard(byte[] src, byte[] dst, int w, int h, double cornerPx)
        => RefractForCard(src, dst, w, h, cornerPx, 0, 0, w, h, false);

    /// <summary>
    /// 带"卡片在缓冲内的偏移"的折射：缓冲 w×h 可比卡片 cardW×cardH 大（外扩区），
    /// 卡片位于 (cardOffsetX, cardOffsetY)。band/amount 按**卡片**尺寸计算，与卡片铺满缓冲时完全一致。
    /// keepOutsidePixels=true 时卡片圆角形状之外保持源图原样（供"外扩缓冲 + 取样矩形平移"：
    /// 平移露出的外扩区必须显示真实桌面，否则卡片边缘出现透明空洞；圆角由承载 Border 负责）。
    /// 默认重载即 offset=0、card=w×h、keepOutsidePixels=false（与旧行为逐字节一致）。
    /// </summary>
    public static void RefractForCard(byte[] src, byte[] dst, int w, int h, double cornerPx,
        int cardOffsetX, int cardOffsetY, int cardW, int cardH, bool keepOutsidePixels)
    {
        double band = Math.Clamp(Math.Min(cardW, cardH) * 0.14, 8.0, 20.0);
        double amount = Math.Min(cardW, cardH) * 0.10;
        FloatScheduleRefraction.Refract(src, dst, w, h, cornerPx, band, amount,
            // depthEffect 必须为 false：叠加"由中心指向该点"的径向分量会让位移朝中心收束，
            // 呈现星芒状放射（与预期不符）。只用 SDF 梯度（边缘外法线）→ 透镜效应局限在贴边一带，
            // 卡片中部不变形、四角沿圆角弧心呈扇形发散（上游 Kyant 通用 lens() 的默认值也是 false）。
            depthEffect: false, chromaticAberration: 0.10, highlightStrength: 0.35, highlightAngleDeg: 45, highlightFalloff: 4.0,
            cardOffsetX: cardOffsetX, cardOffsetY: cardOffsetY, cardW: cardW, cardH: cardH,
            keepOutsidePixels: keepOutsidePixels);
    }
}
