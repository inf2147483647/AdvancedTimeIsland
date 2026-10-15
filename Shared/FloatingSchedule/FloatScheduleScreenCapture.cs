// 真实悬浮窗"背景采样"：透明窗口拿不到 DWM 合成的桌面像素，唯一现实来源是屏幕捕获。
//
// 关键前提（已实测验证，探针工程见 D:\ati_capture_probe，验完即删）：
//   透明无框的 Avalonia 窗口（WS_EX_LAYERED）在启用 SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE) 后，
//   BitBlt(SRCCOPY|CAPTUREBLT) 抓该窗口所占区域，**拿到的就是窗口背后的桌面**（对比图：开 WDA 的结果与
//   "窗口出现前的基线"逐像素一致，关闭 WDA 则能看到窗口自身）——而不是 WPF 分层窗那种黑块。
//   该 API 只影响"捕获输出"、不影响屏幕上真正显示的内容，因此无论用户把"防截图"开关设成什么，
//   抓帧时都可以临时强制启用、抓完立刻恢复：这同时就是最干净的自采样回环防护
//   （否则抓屏会抓到自己的卡片 → 画面递归）。
//
// 只允许 System / Avalonia（DllImport 属于 System），与主项目、子进程同源编译。
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public static class FloatScheduleScreenCapture
{
    const int SRCCOPY = 0x00CC0020;
    const int CAPTUREBLT = 0x40000000;   // 必须叠加：否则 BitBlt 会跳过 WS_EX_LAYERED 窗口

    public const uint WDA_NONE = 0x00000000;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

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

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    [DllImport("user32.dll")] private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint dwAffinity);
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
    /// 确保窗口处于"排除捕获"态（WDA_EXCLUDEFROMCAPTURE）。
    /// 返回 false = 平台不支持（低于 Win10 2004）→ 此时**不能**抓帧（会把自己抓进画面形成递归），调用方应回退近似层。
    ///
    /// 【为什么不是每帧设一次】SetWindowDisplayAffinity 会让 DWM 重新合成该窗口；若每帧"强制→恢复"来回切，
    ///   60Hz 下就是每秒 120 次合成抖动 —— 表现为"CPU/GPU 都很低但拖动延迟很大"。
    ///   故改为**状态迁移式**：先查当前值，已是目标态就一次 Win32 都不调（查值极廉价），只在真正切换时才写。
    /// </summary>
    public static bool EnsureExcludedFromCapture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (GetWindowDisplayAffinity(hwnd, out uint cur) && cur == WDA_EXCLUDEFROMCAPTURE) return true;
        return SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
    }

    /// <summary>把亲和性恢复成用户"防截图"设置对应的取值（同样只在状态不同时才写）。</summary>
    public static bool RestoreAffinity(IntPtr hwnd, bool userPreventCapture)
    {
        if (hwnd == IntPtr.Zero) return false;
        uint want = userPreventCapture ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
        if (GetWindowDisplayAffinity(hwnd, out uint cur) && cur == want) return true;
        return SetWindowDisplayAffinity(hwnd, want);
    }

    /// <summary>
    /// 纯抓帧：抓"该窗口所占屏幕区域"到 dst（BGRA，Premul，top-down，长度需 ≥ w*h*4）。
    /// w/h 为**物理像素**尺寸；位置由 GetWindowRect 取。
    /// 前置条件：调用方已用 <see cref="EnsureExcludedFromCapture"/> 保证本窗口不会被抓进画面。
    /// 这里刻意不做任何亲和性切换 —— 见上面 EnsureExcludedFromCapture 的说明。
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
    /// 把"抓帧外扩量"限制在窗口所在显示器的矩形内（保证外扩后的区域仍在同一显示器/同一 DXGI output 里，
    /// 这样 DXGI 的区域拷贝与 GDI 的 BitBlt 都成立）。返回实际的 pad（≥0）。
    /// 取不到显示器信息时返回 0（退化为不外扩，功能降级但不影响抓帧）。
    /// </summary>
    public static int ClampPadToMonitor(IntPtr hwnd, int x, int y, int w, int h, int pad)
    {
        if (pad <= 0) return 0;
        try
        {
            var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hMon == IntPtr.Zero) return 0;
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hMon, ref mi)) return 0;
            int left = x - mi.rcMonitor.Left;
            int top = y - mi.rcMonitor.Top;
            int right = mi.rcMonitor.Right - (x + w);
            int bottom = mi.rcMonitor.Bottom - (y + h);
            int maxPad = Math.Min(Math.Min(left, top), Math.Min(right, bottom));
            return Math.Clamp(Math.Min(pad, maxPad), 0, pad);
        }
        catch { return 0; }
    }

    /// <summary>
    /// 抓屏幕上任意区域到 dst（BGRA，top-down，stride = w*4，长度需 ≥ w*h*4）。
    /// 这是 GDI 回退路径（BitBlt）—— 单次固定 24ms 左右，仅在 DXGI 不可用时使用。
    /// 前置条件同 <see cref="TryCaptureBehindWindow"/>：调用方已保证本窗口处于"排除捕获"态。
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

    /// <summary>
    /// 抓窗口背后区域 → 走共享折射核心 → 返回可直接作卡片背景的画笔（失败或尺寸非法返回 null）。
    /// cornerPx/band/amount 均为**物理像素**；参数与编辑器预览同源（折射带 ≤20px、位移 ≈10% 尺寸、
    /// 色散 ≈0.7% 尺寸、镜面高光 falloff 4）。
    /// </summary>
    public static IImageBrush? CaptureRefractedBackdrop(IntPtr hwnd, int w, int h, double cornerPx)
    {
        if (w < 4 || h < 4) return null;
        if (!EnsureExcludedFromCapture(hwnd)) return null;   // 拿不到"排除捕获"就不能抓（会抓到自己）
        var src = new byte[w * h * 4];
        if (!TryCaptureBehindWindow(hwnd, src, w, h)) return null;
        var dst = new byte[src.Length];
        RefractForCard(src, dst, w, h, cornerPx);
        var bmp = FloatScheduleRefraction.ToBitmap(dst, w, h);
        return bmp == null ? null : new ImageBrush(bmp) { Stretch = Stretch.Fill };
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
