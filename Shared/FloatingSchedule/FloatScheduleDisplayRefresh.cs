// 悬浮窗拖动节流：按窗口所在显示器的刷新率决定位置提交间隔（Shared 层，主项目与独立子进程同源编译）。
//
// 【为什么需要】拖动是"按 PointerMoved 事件写窗口位置"的，为避免高回报率鼠标/触摸/笔的事件洪泛，
//   原先固定节流到 ~60fps（16ms）。在 120/144/165/240Hz 屏幕上这会把拖动压到 60fps，观感明显不如桌面其它窗口顺滑；
//   而在 60Hz 屏上提高频率只会白烧 CPU。故按"当前显示器刷新率"取间隔：60Hz 仍为 16.7ms，高刷屏按 1000/hz。
//
// 【取值来源】MonitorFromWindow（跟随悬浮窗所在显示器，多屏各自正确）+ MONITORINFOEXW.szDevice
//   → EnumDisplaySettingsW(ENUM_CURRENT_SETTINGS) 的 dmDisplayFrequency。
//   取不到（dmDisplayFrequency 为 0/1 表示"硬件默认"）时回退主显示器；仍取不到则按 60Hz。
//   结果限幅到 60–240Hz（间隔 16.7–4.2ms）：既覆盖常见高刷屏，也避免极端刷新率把 UI 线程压垮。
//
// 【代价】单次约两次 Win32 调用（微秒级）；调用方只在拖动开始与跨屏时取一次，PointerMoved 里不做检测。
using System;
using System.Runtime.InteropServices;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public static class FloatScheduleDisplayRefresh
{
    const int EnumCurrentSettings = -1;
    const uint MonitorDefaultToNearest = 2;
    const int MinRefreshHz = 60;
    const int MaxRefreshHz = 240;

    /// <summary>
    /// 拖动位置提交的最小间隔（ms）：跟随 <paramref name="hwnd"/> 所在显示器的刷新率。
    /// 60Hz / 取不到 → 16.7ms；高刷屏 → 1000/hz（限幅 60–240Hz）。
    /// </summary>
    public static double GetDragFrameIntervalMs(IntPtr hwnd)
    {
        int hz = GetRefreshRate(hwnd);
        if (hz < MinRefreshHz) hz = MinRefreshHz;
        if (hz > MaxRefreshHz) hz = MaxRefreshHz;
        return 1000.0 / hz;
    }

    /// <summary>
    /// 拖动位置提交的**节流阈值**（ms）= 显示帧间隔的一半。
    /// 【为什么不是一整帧】PointerMoved 的事件间隔在高刷屏 / 输入合并场景下正好等于一个显示帧，
    ///   Stopwatch 测出的实际间隔会因消息调度抖动略小于 1000/hz；若阈值取整帧，事件会被稳定地判为
    ///   "还不到时候"，于是每两帧才提交一次 —— 60Hz 屏上表现为 ~30fps（这是"帧率偶发腰斩"的根因）。
    ///   取半帧可保证每个显示帧至少提交一次，同时仍能合并同一帧内重复到达的指针事件。
    /// </summary>
    public static double GetDragThrottleMs(IntPtr hwnd) => GetDragFrameIntervalMs(hwnd) * 0.5;

    /// <summary>取窗口所在显示器刷新率（Hz）；取不到返回 0。</summary>
    public static int GetRefreshRate(IntPtr hwnd)
    {
        try
        {
            var hMonitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
            if (hMonitor != IntPtr.Zero)
            {
                var mi = new MonitorInfoEx { cbSize = Marshal.SizeOf<MonitorInfoEx>(), szDevice = string.Empty };
                if (GetMonitorInfo(hMonitor, ref mi) && !string.IsNullOrEmpty(mi.szDevice))
                {
                    int hz = QueryDeviceRefreshRate(mi.szDevice);
                    if (hz > 1) return hz;
                }
            }

            // 设备名取不到 / 该设备返回"硬件默认"（0/1）→ 回退主显示器（设备名为 null）
            int primary = QueryDeviceRefreshRate(null);
            if (primary > 1) return primary;
        }
        catch { /* 取刷新率失败不得影响拖动 */ }
        return 0;
    }

    static int QueryDeviceRefreshRate(string? deviceName)
    {
        try
        {
            var dm = new DevMode
            {
                dmSize = (short)Marshal.SizeOf<DevMode>(),
                dmDeviceName = string.Empty,
                dmFormName = string.Empty,
            };
            if (EnumDisplaySettings(deviceName, EnumCurrentSettings, ref dm))
                return dm.dmDisplayFrequency;
        }
        catch { }
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MonitorInfoEx
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DevMode lpDevMode);
}
