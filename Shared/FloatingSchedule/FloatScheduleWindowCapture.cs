// 液态玻璃"下方窗口"采样（共享层）。
//
// 【为什么要它】屏幕捕获会把悬浮窗自己也拍进去（尤其在移除"抓帧期间排除捕获"之后），
//   背景出现自采样回环/拖影。这里改为只采集悬浮窗**下方那个窗口**的像素：
//   · Windows 11 22H2+：Windows.Graphics.Capture（WGC，DWM 合成帧，可关闭黄色捕获边框）；
//   · Windows 10：PrintWindow(PW_RENDERFULLCONTENT) —— 无边框、不会拍到自己，
//     但属于窗口自绘结果，个别 GPU 直出/UWP 窗口可能得到黑帧（此时调用方回退屏幕采集）。
//
//   WGC 用**纯 COM 互操作**（不引用 Microsoft.Windows.SDK.NET 投影），避免给插件包增加 ~26MB 依赖；
//   接口 IID / 方法顺序取自 Windows SDK 的 windows.graphics.capture.h。
//
// 【坐标】全部是屏幕物理像素：调用方给出卡片所在屏幕矩形，本类按目标窗口的屏幕矩形裁剪出对应子区域。
//   目标窗口没有覆盖卡片的部分用最近边缘像素填充，避免玻璃边缘出现透明空洞。
using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public sealed class FloatScheduleWindowCapture : IDisposable
{
    const int PW_RENDERFULLCONTENT = 0x00000002;
    const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    const int DWMWA_CLOAK = 14;
    const int GWL_EXSTYLE = -20;
    const long WS_EX_LAYERED = 0x00080000;
    const long WS_EX_TRANSPARENT = 0x00000020;
    const uint GW_HWNDNEXT = 2;
    const int DirectXPixelFormatB8G8R8A8UIntNormalized = 87;

    static readonly Guid IID_IGraphicsCaptureItem = new("79c3f95b-31f7-4ec2-a464-632ef5d30760");
    static readonly Guid IID_IGraphicsCaptureItemInterop = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
    static readonly Guid IID_IDirect3D11CaptureFramePoolStatics2 = new("589b103f-6bbc-5df5-a991-02e28b3b66d5");
    static readonly Guid IID_IDirect3D11CaptureFramePool = new("24eb6d22-1975-422e-82e7-780dbd8ddf24");
    static readonly Guid IID_IGraphicsCaptureSession = new("814e42a9-f70f-4ad7-939b-fddcc6eb880d");
    static readonly Guid IID_IGraphicsCaptureSession2 = new("2c39ae40-7d2e-5044-804e-8b6799d4cf9e");
    static readonly Guid IID_IGraphicsCaptureSession3 = new("f2cdd966-22ae-5ea1-9596-3a289344c3be");
    static readonly Guid IID_IDirect3D11CaptureFrame = new("fa50c623-38da-4b32-acf3-fa9734ad800e");
    static readonly Guid IID_IDirect3DDxgiInterfaceAccess = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");

    readonly object _lock = new();

    // ===== WGC =====
    ID3D11Device? _device;
    ID3D11DeviceContext? _context;
    IntPtr _d3dDevice;                 // IDirect3DDevice（WinRT IInspectable）
    IntPtr _itemPtr;
    IntPtr _poolPtr;
    IntPtr _sessionPtr;
    IntPtr _wgcHwnd;
    int _itemW, _itemH;
    ID3D11Texture2D? _frameTex;
    ID3D11Texture2D? _staging;
    int _stagingW, _stagingH;
    bool _frameValid;
    int _frameW, _frameH;

    // ===== PrintWindow =====
    IntPtr _memDc, _dib, _oldObj, _bits;
    int _dibW, _dibH;

    /// <summary>
    /// 采集 <paramref name="hwnd"/>（应为"悬浮窗下方的窗口"）在屏幕矩形 (x,y,w,h) 处的像素到 dst（BGRA）。
    /// 返回 false 表示本次不可用（调用方应回退屏幕采集）。
    /// </summary>
    public bool TryCapture(IntPtr hwnd, int x, int y, int w, int h, byte[] dst)
        => TryCapture(hwnd, x, y, w, h, dst, fillEdges: true, out _);

    /// <summary>
    /// 同上；fillEdges=false 时窗口未覆盖的区域保持全 0（供 Z 序合成使用，不能把边缘像素外扩）。
    /// alphaValid 表示返回的 alpha 通道是否可信（WGC 可信；PrintWindow/GDI 不可信，应按不透明处理）。
    /// </summary>
    public bool TryCapture(IntPtr hwnd, int x, int y, int w, int h, byte[] dst, bool fillEdges, out bool alphaValid)
    {
        alphaValid = false;
        if (hwnd == IntPtr.Zero || w < 4 || h < 4 || dst.Length < w * h * 4) return false;
        lock (_lock)
        {
            try
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
                {
                    // Win11 22H2+：DWM/WGC（可关闭黄色捕获边框）；失败时退回 PrintWindow。
                    if (TryCaptureWgc(hwnd, x, y, w, h, dst, fillEdges))
                    {
                        alphaValid = true;
                        return true;
                    }
                }
                return TryCapturePrintWindow(hwnd, x, y, w, h, dst, fillEdges);
            }
            catch
            {
                // 任何捕获失败都只影响本帧，调用方回退屏幕采集。
                return false;
            }
        }
    }

    // ===================== 目标窗口选择 =====================

    /// <summary>
    /// 找悬浮窗 Z 序下方、与卡片矩形重叠最多的可见顶层窗口（通常就是用户看到的"下方窗口"）。
    /// 覆盖卡片 ≥60% 时立即返回（该窗口就是主体）；否则返回重叠面积最大者（一般是桌面）。
    /// </summary>
    public static IntPtr FindWindowBelow(IntPtr self, int x, int y, int w, int h)
    {
        try
        {
            long cardArea = (long)w * h;
            IntPtr best = IntPtr.Zero;
            long bestArea = 0;
            IntPtr cur = GetWindow(self, GW_HWNDNEXT);
            while (cur != IntPtr.Zero)
            {
                // 跳过无响应窗口：PrintWindow 会同步给目标窗口发消息，目标卡死时会把采样线程一起卡死。
                if (IsWindowVisible(cur) && !IsIconic(cur) && !IsCloaked(cur) && !IsHungAppWindow(cur))
                {
                    long ex = GetWindowLongPtr(cur, GWL_EXSTYLE).ToInt64();
                    // 跳过分层/点击穿透窗口：它们多是覆盖层，采到的往往不是用户看到的内容。
                    if ((ex & (WS_EX_LAYERED | WS_EX_TRANSPARENT)) == 0
                        && GetWindowRect(cur, out RECT r))
                    {
                        int iw = Math.Min(x + w, r.Right) - Math.Max(x, r.Left);
                        int ih = Math.Min(y + h, r.Bottom) - Math.Max(y, r.Top);
                        if (iw > 0 && ih > 0)
                        {
                            long area = (long)iw * ih;
                            if (area * 100 >= cardArea * 60) return cur;   // 覆盖主体 → 即为下方窗口
                            if (area > bestArea) { bestArea = area; best = cur; }
                        }
                    }
                }
                cur = GetWindow(cur, GW_HWNDNEXT);
            }
            return best;
        }
        catch { return IntPtr.Zero; }
    }

    // ===================== WGC（Win11 22H2+） =====================

    bool TryCaptureWgc(IntPtr hwnd, int x, int y, int w, int h, byte[] dst, bool fillEdges)
    {
        if (!EnsureWgc(hwnd)) return false;

        // 目标窗口尺寸变化（缩放/最大化切换）→ 下一帧重建会话
        if (!TryGetItemSize(_itemPtr, out int iw, out int ih)) return false;
        if (iw != _itemW || ih != _itemH)
        {
            DisposeWgc();
            return false;
        }

        IntPtr framePtr = IntPtr.Zero;
        try
        {
            var pool = (IDirect3D11CaptureFramePool)Marshal.GetTypedObjectForIUnknown(_poolPtr, typeof(IDirect3D11CaptureFramePool));
            try { framePtr = pool.TryGetNextFrame(); }
            finally { Marshal.ReleaseComObject(pool); }
        }
        catch { framePtr = IntPtr.Zero; }

        if (framePtr != IntPtr.Zero)
        {
            try
            {
                var frame = (IDirect3D11CaptureFrame)Marshal.GetTypedObjectForIUnknown(framePtr, typeof(IDirect3D11CaptureFrame));
                try
                {
                    IntPtr surfacePtr = IntPtr.Zero;
                    try
                    {
                        surfacePtr = frame.get_Surface();
                        if (!TryGetDxgiTexture(surfacePtr, out var tex, out int tw, out int th))
                        {
                            tex?.Dispose();
                            return false;
                        }
                        using (tex)
                        {
                            if (_frameTex == null || _frameW != tw || _frameH != th)
                            {
                                _frameTex?.Dispose();
                                _frameTex = _device!.CreateTexture2D(new Texture2DDescription
                                {
                                    Width = (uint)tw,
                                    Height = (uint)th,
                                    MipLevels = 1,
                                    ArraySize = 1,
                                    Format = Format.B8G8R8A8_UNorm,
                                    SampleDescription = new SampleDescription(1, 0),
                                    Usage = ResourceUsage.Default,
                                    BindFlags = BindFlags.None,
                                    CPUAccessFlags = CpuAccessFlags.None,
                                    MiscFlags = ResourceOptionFlags.None,
                                });
                                _frameW = tw;
                                _frameH = th;
                            }
                            _context!.CopyResource(_frameTex, tex);
                            _frameValid = true;
                        }
                    }
                    finally
                    {
                        if (surfacePtr != IntPtr.Zero) Marshal.Release(surfacePtr);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(frame);
                    Marshal.Release(framePtr);
                }
            }
            catch
            {
                // 帧处理失败：丢弃本帧，仍可用上一帧内容
            }
        }
        if (!_frameValid || _frameTex == null) return false;

        // WGC 的帧可能是"窗口外框"或"客户区"，按尺寸自适应映射到屏幕坐标。
        if (!TryGetWgcOrigin(hwnd, _frameW, _frameH, out int originX, out int originY)) return false;
        int sx = x - originX, sy = y - originY;
        if (sx >= _frameW || sy >= _frameH || sx + w <= 0 || sy + h <= 0) return false;

        int dx = Math.Max(0, -sx), dy = Math.Max(0, -sy);
        int cx = Math.Max(0, sx), cy = Math.Max(0, sy);
        int cw = Math.Min(w - dx, _frameW - cx);
        int ch = Math.Min(h - dy, _frameH - cy);
        if (cw <= 0 || ch <= 0) return false;

        Array.Clear(dst, 0, w * h * 4);
        EnsureStaging(w, h);
        _context.CopySubresourceRegion(_staging!, 0, (uint)dx, (uint)dy, 0, _frameTex, 0,
            new Box(cx, cy, 0, cx + cw, cy + ch, 1));

        var map = _context.Map(_staging!, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            for (int row = 0; row < ch; row++)
            {
                IntPtr src = IntPtr.Add(map.DataPointer, (int)((long)(dy + row) * map.RowPitch + dx * 4));
                Marshal.Copy(src, dst, ((dy + row) * w + dx) * 4, cw * 4);
            }
        }
        finally
        {
            _context.Unmap(_staging!, 0);
        }
        FillEdges(dst, w, h, dx, dy, cw, ch, fillEdges);
        return true;
    }

    bool EnsureWgc(IntPtr hwnd)
    {
        if (_wgcHwnd == hwnd && _sessionPtr != IntPtr.Zero) return true;
        DisposeWgc();
        EnsureDevice();
        if (_d3dDevice == IntPtr.Zero) return false;

        IntPtr itemPtr = CreateItem(hwnd);
        if (itemPtr == IntPtr.Zero) return false;
        if (!TryGetItemSize(itemPtr, out int iw, out int ih) || iw < 4 || ih < 4)
        {
            Marshal.Release(itemPtr);
            return false;
        }

        IntPtr poolPtr = IntPtr.Zero, sessionPtr = IntPtr.Zero;
        try
        {
            var size = new SizeInt32 { Width = iw, Height = ih };
            IntPtr staticsPtr = GetActivationFactory("Windows.Graphics.Capture.Direct3D11CaptureFramePool",
                IID_IDirect3D11CaptureFramePoolStatics2);
            if (staticsPtr == IntPtr.Zero) return false;
            try
            {
                var statics = (IDirect3D11CaptureFramePoolStatics2)Marshal.GetTypedObjectForIUnknown(
                    staticsPtr, typeof(IDirect3D11CaptureFramePoolStatics2));
                try { poolPtr = statics.CreateFreeThreaded(_d3dDevice, DirectXPixelFormatB8G8R8A8UIntNormalized, 2, size); }
                finally { Marshal.ReleaseComObject(statics); }
            }
            finally { Marshal.Release(staticsPtr); }
            if (poolPtr == IntPtr.Zero) return false;

            var pool = (IDirect3D11CaptureFramePool)Marshal.GetTypedObjectForIUnknown(
                poolPtr, typeof(IDirect3D11CaptureFramePool));
            try { sessionPtr = pool.CreateCaptureSession(itemPtr); }
            finally { Marshal.ReleaseComObject(pool); }
            if (sessionPtr == IntPtr.Zero) return false;

            // 光标不采集
            Guid s2Iid = IID_IGraphicsCaptureSession2;
            if (Marshal.QueryInterface(sessionPtr, ref s2Iid, out IntPtr s2Ptr) == 0)
            {
                try
                {
                    var s2 = (IGraphicsCaptureSession2)Marshal.GetTypedObjectForIUnknown(s2Ptr, typeof(IGraphicsCaptureSession2));
                    try { s2.put_IsCursorCaptureEnabled(false); }
                    finally { Marshal.ReleaseComObject(s2); }
                }
                finally { Marshal.Release(s2Ptr); }
            }
            // Win11 22H2+：关闭黄色捕获边框（Win10 上没有该接口，QueryInterface 失败即跳过）
            Guid s3Iid = IID_IGraphicsCaptureSession3;
            if (Marshal.QueryInterface(sessionPtr, ref s3Iid, out IntPtr s3Ptr) == 0)
            {
                try
                {
                    var s3 = (IGraphicsCaptureSession3)Marshal.GetTypedObjectForIUnknown(s3Ptr, typeof(IGraphicsCaptureSession3));
                    try { s3.put_IsBorderRequired(false); }
                    finally { Marshal.ReleaseComObject(s3); }
                }
                finally { Marshal.Release(s3Ptr); }
            }

            var session = (IGraphicsCaptureSession)Marshal.GetTypedObjectForIUnknown(
                sessionPtr, typeof(IGraphicsCaptureSession));
            try { session.StartCapture(); }
            finally { Marshal.ReleaseComObject(session); }
        }
        catch
        {
            if (sessionPtr != IntPtr.Zero) Marshal.Release(sessionPtr);
            if (poolPtr != IntPtr.Zero) Marshal.Release(poolPtr);
            Marshal.Release(itemPtr);
            return false;
        }

        _itemPtr = itemPtr;
        _poolPtr = poolPtr;
        _sessionPtr = sessionPtr;
        _wgcHwnd = hwnd;
        _itemW = iw;
        _itemH = ih;
        _frameValid = false;
        return true;
    }

    void EnsureDevice()
    {
        if (_device != null) return;
        var levels = new[]
        {
            FeatureLevel.Level_11_1, FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1, FeatureLevel.Level_10_0,
        };
        D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport,
            levels, out _device, out _context).CheckError();
        using var dxgi = _device.QueryInterface<IDXGIDevice>();
        CreateDirect3D11DeviceFromDXGIDevice(dxgi.NativePointer, out _d3dDevice);
    }

    static IntPtr CreateItem(IntPtr hwnd)
    {
        IntPtr factoryPtr = GetActivationFactory("Windows.Graphics.Capture.GraphicsCaptureItem",
            IID_IGraphicsCaptureItemInterop);
        if (factoryPtr == IntPtr.Zero) return IntPtr.Zero;
        try
        {
            var interop = (IGraphicsCaptureItemInterop)Marshal.GetTypedObjectForIUnknown(
                factoryPtr, typeof(IGraphicsCaptureItemInterop));
            try
            {
                Guid itemIid = IID_IGraphicsCaptureItem;
                return interop.CreateForWindow(hwnd, ref itemIid);
            }
            finally { Marshal.ReleaseComObject(interop); }
        }
        finally { Marshal.Release(factoryPtr); }
    }

    static IntPtr GetActivationFactory(string className, Guid iid)
    {
        WindowsCreateString(className, className.Length, out IntPtr hs);
        try { RoGetActivationFactory(hs, ref iid, out IntPtr factory); return factory; }
        finally { WindowsDeleteString(hs); }
    }

    static bool TryGetItemSize(IntPtr itemPtr, out int w, out int h)
    {
        w = h = 0;
        if (itemPtr == IntPtr.Zero) return false;
        try
        {
            var item = (IGraphicsCaptureItem)Marshal.GetTypedObjectForIUnknown(itemPtr, typeof(IGraphicsCaptureItem));
            try
            {
                var size = item.get_Size();
                w = size.Width;
                h = size.Height;
                return w > 0 && h > 0;
            }
            finally { Marshal.ReleaseComObject(item); }
        }
        catch { return false; }
    }

    static bool TryGetDxgiTexture(IntPtr surfacePtr, out ID3D11Texture2D? texture, out int w, out int h)
    {
        texture = null;
        w = h = 0;
        if (surfacePtr == IntPtr.Zero) return false;
        Guid iid = IID_IDirect3DDxgiInterfaceAccess;
        if (Marshal.QueryInterface(surfacePtr, ref iid, out IntPtr accessPtr) != 0) return false;
        try
        {
            var access = (IDirect3DDxgiInterfaceAccess)Marshal.GetTypedObjectForIUnknown(
                accessPtr, typeof(IDirect3DDxgiInterfaceAccess));
            try
            {
                Guid texIid = typeof(ID3D11Texture2D).GUID;
                IntPtr texPtr = access.GetInterface(ref texIid);
                if (texPtr == IntPtr.Zero) return false;
                texture = new ID3D11Texture2D(texPtr);
                var d = texture.Description;
                w = (int)d.Width;
                h = (int)d.Height;
                return true;
            }
            finally { Marshal.ReleaseComObject(access); }
        }
        finally { Marshal.Release(accessPtr); }
    }

    void EnsureStaging(int w, int h)
    {
        if (_staging != null && _stagingW == w && _stagingH == h) return;
        _staging?.Dispose();
        _staging = _device!.CreateTexture2D(new Texture2DDescription
        {
            Width = (uint)w,
            Height = (uint)h,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None,
        });
        _stagingW = w;
        _stagingH = h;
    }

    /// <summary>WGC 帧尺寸对应"窗口外框"还是"客户区"，据此给出帧左上角在屏幕上的物理坐标。</summary>
    static bool TryGetWgcOrigin(IntPtr hwnd, int frameW, int frameH, out int x, out int y)
    {
        x = y = 0;
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT ext, Marshal.SizeOf<RECT>()) == 0)
        {
            int ew = ext.Right - ext.Left, eh = ext.Bottom - ext.Top;
            if (Math.Abs(frameW - ew) <= 1 && Math.Abs(frameH - eh) <= 1)
            {
                x = ext.Left; y = ext.Top;
                return true;
            }
        }
        if (GetClientRect(hwnd, out RECT cr))
        {
            int cw = cr.Right - cr.Left, ch = cr.Bottom - cr.Top;
            if (Math.Abs(frameW - cw) <= 1 && Math.Abs(frameH - ch) <= 1)
            {
                var p = new POINT { X = 0, Y = 0 };
                if (ClientToScreen(hwnd, ref p)) { x = p.X; y = p.Y; return true; }
            }
        }
        // 兜底：按窗口矩形
        if (GetWindowRect(hwnd, out RECT wr)
            && Math.Abs(frameW - (wr.Right - wr.Left)) <= 1
            && Math.Abs(frameH - (wr.Bottom - wr.Top)) <= 1)
        {
            x = wr.Left; y = wr.Top;
            return true;
        }
        return false;
    }

    // ===================== PrintWindow（Win10） =====================

    bool TryCapturePrintWindow(IntPtr hwnd, int x, int y, int w, int h, byte[] dst, bool fillEdges)
    {
        if (!GetWindowRect(hwnd, out RECT wr)) return false;
        int ww = wr.Right - wr.Left, wh = wr.Bottom - wr.Top;
        if (ww < 4 || wh < 4) return false;
        if (!EnsurePrintResources(ww, wh)) return false;
        if (!PrintWindow(hwnd, _memDc, PW_RENDERFULLCONTENT)) return false;
        GdiFlush();   // GDI 可能延迟落笔：读 DIB 像素前必须刷出

        int sx = x - wr.Left, sy = y - wr.Top;
        if (sx >= ww || sy >= wh || sx + w <= 0 || sy + h <= 0) return false;

        int dx = Math.Max(0, -sx), dy = Math.Max(0, -sy);
        int cx = Math.Max(0, sx), cy = Math.Max(0, sy);
        int cw = Math.Min(w - dx, ww - cx);
        int ch = Math.Min(h - dy, wh - cy);
        if (cw <= 0 || ch <= 0) return false;

        Array.Clear(dst, 0, w * h * 4);
        for (int row = 0; row < ch; row++)
        {
            IntPtr src = IntPtr.Add(_bits, ((cy + row) * ww + cx) * 4);
            Marshal.Copy(src, dst, ((dy + row) * w + dx) * 4, cw * 4);
        }
        FillEdges(dst, w, h, dx, dy, cw, ch, fillEdges);
        return true;
    }

    /// <summary>目标窗口没有覆盖卡片的部分：用最近的边缘像素填充，避免玻璃边缘出现透明空洞。</summary>
    static void FillEdges(byte[] dst, int w, int h, int dx, int dy, int cw, int ch, bool fill)
    {
        if (!fill || cw <= 0 || ch <= 0) return;
        // 左右：按行复制最近的像素
        for (int row = dy; row < dy + ch; row++)
        {
            int rowBase = row * w * 4;
            int leftSrc = rowBase + dx * 4;
            int rightSrc = rowBase + (dx + cw - 1) * 4;
            for (int x = 0; x < dx; x++) Buffer.BlockCopy(dst, leftSrc, dst, rowBase + x * 4, 4);
            for (int x = dx + cw; x < w; x++) Buffer.BlockCopy(dst, rightSrc, dst, rowBase + x * 4, 4);
        }
        // 上下：整行复制
        int firstRow = dy * w * 4, lastRow = (dy + ch - 1) * w * 4;
        for (int row = 0; row < dy; row++) Buffer.BlockCopy(dst, firstRow, dst, row * w * 4, w * 4);
        for (int row = dy + ch; row < h; row++) Buffer.BlockCopy(dst, lastRow, dst, row * w * 4, w * 4);
    }

    bool EnsurePrintResources(int w, int h)
    {
        if (_memDc != IntPtr.Zero && _dib != IntPtr.Zero && _dibW == w && _dibH == h) return true;
        ReleasePrintResources();

        IntPtr screen = GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero) return false;
        try
        {
            _memDc = CreateCompatibleDC(screen);
            if (_memDc == IntPtr.Zero) return false;
            var bmi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = 40,
                    biWidth = w,
                    biHeight = -h,   // 负数 → top-down
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0,
                },
            };
            _dib = CreateDIBSection(screen, ref bmi, 0, out _bits, IntPtr.Zero, 0);
            if (_dib == IntPtr.Zero || _bits == IntPtr.Zero)
            {
                ReleasePrintResources();
                return false;
            }
            _oldObj = SelectObject(_memDc, _dib);
            _dibW = w;
            _dibH = h;
            return true;
        }
        finally { ReleaseDC(IntPtr.Zero, screen); }
    }

    void ReleasePrintResources()
    {
        if (_memDc != IntPtr.Zero && _oldObj != IntPtr.Zero) SelectObject(_memDc, _oldObj);
        _oldObj = IntPtr.Zero;
        if (_dib != IntPtr.Zero) { DeleteObject(_dib); _dib = IntPtr.Zero; }
        if (_memDc != IntPtr.Zero) { DeleteDC(_memDc); _memDc = IntPtr.Zero; }
        _bits = IntPtr.Zero;
        _dibW = _dibH = 0;
    }

    void DisposeWgc()
    {
        if (_sessionPtr != IntPtr.Zero) { try { Marshal.Release(_sessionPtr); } catch { } _sessionPtr = IntPtr.Zero; }
        if (_poolPtr != IntPtr.Zero) { try { Marshal.Release(_poolPtr); } catch { } _poolPtr = IntPtr.Zero; }
        if (_itemPtr != IntPtr.Zero) { try { Marshal.Release(_itemPtr); } catch { } _itemPtr = IntPtr.Zero; }
        _wgcHwnd = IntPtr.Zero;
        _frameValid = false;
        try { _frameTex?.Dispose(); } catch { }
        _frameTex = null;
        _frameW = _frameH = 0;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            DisposeWgc();
            try { _staging?.Dispose(); } catch { }
            _staging = null;
            _stagingW = _stagingH = 0;
            ReleasePrintResources();
            if (_d3dDevice != IntPtr.Zero) { try { Marshal.Release(_d3dDevice); } catch { } _d3dDevice = IntPtr.Zero; }
            try { _context?.Dispose(); } catch { }
            try { _device?.Dispose(); } catch { }
            _context = null;
            _device = null;
        }
    }

    // ===================== COM 接口（IID / 顺序取自 Windows SDK） =====================

    [StructLayout(LayoutKind.Sequential)]
    struct SizeInt32 { public int Width, Height; }

    // 经典 COM
    [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(IntPtr window, ref Guid iid);
        IntPtr CreateForMonitor(IntPtr monitor, ref Guid iid);
    }

    [ComImport, Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface(ref Guid iid);
    }

    // WinRT 接口：显式写出 IInspectable 的三个基方法，保证 vtable 顺序正确。
    [ComImport, Guid("79c3f95b-31f7-4ec2-a464-632ef5d30760"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureItem
    {
        void GetIids(out int iidCount, out IntPtr iids);
        IntPtr GetRuntimeClassName();
        int GetTrustLevel();
        IntPtr get_DisplayName();
        SizeInt32 get_Size();
        long add_Closed(IntPtr handler);
        void remove_Closed(long token);
    }

    [ComImport, Guid("589b103f-6bbc-5df5-a991-02e28b3b66d5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirect3D11CaptureFramePoolStatics2
    {
        void GetIids(out int iidCount, out IntPtr iids);
        IntPtr GetRuntimeClassName();
        int GetTrustLevel();
        IntPtr CreateFreeThreaded(IntPtr device, int pixelFormat, int numberOfBuffers, SizeInt32 size);
    }

    [ComImport, Guid("24eb6d22-1975-422e-82e7-780dbd8ddf24"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirect3D11CaptureFramePool
    {
        void GetIids(out int iidCount, out IntPtr iids);
        IntPtr GetRuntimeClassName();
        int GetTrustLevel();
        void Recreate(IntPtr device, int pixelFormat, int numberOfBuffers, SizeInt32 size);
        IntPtr TryGetNextFrame();
        long add_FrameArrived(IntPtr handler);
        void remove_FrameArrived(long token);
        IntPtr CreateCaptureSession(IntPtr item);
        IntPtr get_DispatcherQueue();
    }

    [ComImport, Guid("814e42a9-f70f-4ad7-939b-fddcc6eb880d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureSession
    {
        void GetIids(out int iidCount, out IntPtr iids);
        IntPtr GetRuntimeClassName();
        int GetTrustLevel();
        void StartCapture();
    }

    [ComImport, Guid("2c39ae40-7d2e-5044-804e-8b6799d4cf9e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureSession2
    {
        void GetIids(out int iidCount, out IntPtr iids);
        IntPtr GetRuntimeClassName();
        int GetTrustLevel();
        [return: MarshalAs(UnmanagedType.U1)] bool get_IsCursorCaptureEnabled();
        void put_IsCursorCaptureEnabled([MarshalAs(UnmanagedType.U1)] bool value);
    }

    [ComImport, Guid("f2cdd966-22ae-5ea1-9596-3a289344c3be"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureSession3
    {
        void GetIids(out int iidCount, out IntPtr iids);
        IntPtr GetRuntimeClassName();
        int GetTrustLevel();
        [return: MarshalAs(UnmanagedType.U1)] bool get_IsBorderRequired();
        void put_IsBorderRequired([MarshalAs(UnmanagedType.U1)] bool value);
    }

    [ComImport, Guid("fa50c623-38da-4b32-acf3-fa9734ad800e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirect3D11CaptureFrame
    {
        void GetIids(out int iidCount, out IntPtr iids);
        IntPtr GetRuntimeClassName();
        int GetTrustLevel();
        IntPtr get_Surface();
        long get_SystemRelativeTime();
        SizeInt32 get_ContentSize();
    }

    // ===================== Win32 / WinRT 激活 =====================

    [DllImport("d3d11.dll", PreserveSig = false)]
    static extern void CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [DllImport("combase.dll", PreserveSig = false)]
    static extern void RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("combase.dll", PreserveSig = false)]
    static extern void WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, out IntPtr hstring);

    [DllImport("combase.dll")]
    static extern int WindowsDeleteString(IntPtr hstring);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

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

    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool IsHungAppWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hWnd, ref POINT p);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, int flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("dwmapi.dll")]
    static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int value, int size);
    [DllImport("dwmapi.dll")]
    static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT value, int size);

    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage, out IntPtr bits, IntPtr hSection, uint offset);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr h);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern bool GdiFlush();

    static bool IsCloaked(IntPtr hwnd)
        => DwmGetWindowAttribute(hwnd, DWMWA_CLOAK, out int v, 4) == 0 && v != 0;
}
