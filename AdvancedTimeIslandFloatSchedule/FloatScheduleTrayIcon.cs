// 子进程托盘图标 + 右键菜单（原生 Win32 实现，替代 Avalonia 的 TrayIcon）。
//
// 为什么不用 Avalonia 的 TrayIcon + NativeMenu：
//   Avalonia 在 Windows 上的托盘菜单**不是**原生 Win32 菜单，而是用 MenuFlyoutPresenter 套一个
//   临时 Window 由 Avalonia 自己渲染出来的（见 Avalonia.Win32.TrayIconImpl.OnRightClicked）。
//   该渲染依赖控件主题样式，而本子进程刻意不加载 FluentAvalonia/Avalonia 主题
//   （宿主 ClassIsland 用的是 FluentAvalonia，其依赖图里并不包含 Avalonia.Themes.Fluent，
//    运行时无从借用），结果就是：菜单渲染成一片空白，且因测量尺寸为 0 导致弹窗定位也异常
//   —— 与用户反馈的"托盘菜单什么都没有、位置可能不对"完全吻合。
//
// 因此改为原生实现：
//   * 独立 STA 线程 + 隐藏消息窗口接收 Shell_NotifyIcon 回调（自带消息循环，不干扰 Avalonia）；
//   * 图标取自本进程 exe 内嵌图标（与插件图标同源）；
//   * 右键用 CreatePopupMenu + TrackPopupMenu 弹出**系统原生菜单**：
//     零主题依赖、定位精确（GetCursorPos）、跨 Avalonia 代际稳定；
//   * 菜单命令用 TPM_RETURNCMD 直接由返回值判定，无需再处理 WM_COMMAND。
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AdvancedTimeIsland.FloatScheduleChild;

internal static class FloatScheduleTrayIcon
{
    private const int WM_APP = 0x8000;
    private const int WM_TRAYICON = WM_APP + 1;
    private const int WM_DESTROY = 0x0002;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_CONTEXTMENU = 0x007B;

    private const int NIM_ADD = 0x00000000;
    private const int NIM_DELETE = 0x00000002;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;

    private const int MF_STRING = 0x00000000;
    private const int TPM_RETURNCMD = 0x0100;
    private const int TPM_RIGHTBUTTON = 0x0002;

    private const int CmdExit = 1;
    private const uint NID_TIP_LENGTH = 128;
    private const string WindowClassName = "AdvancedTimeIslandFloatScheduleTrayWnd";
    private const string ToolTip = "AdvancedTimeIsland 悬浮课表";

    private static Action? _onExit;
    private static WndProcDelegate? _wndProcKeepAlive;   // 保持委托引用，防止 GC 回收导致回调失效
    private static int _started;

    /// <summary>启动托盘图标（幂等）。onExit 在用户选择菜单"退出"时于托盘线程上回调。</summary>
    public static void Show(Action onExit)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) return;
        _onExit = onExit;
        var t = new Thread(ThreadProc)
        {
            IsBackground = true,
            Name = "FloatScheduleTrayIcon",
        };
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
    }

    private static void ThreadProc()
    {
        NOTIFYICONDATA data = default;
        try
        {
            _wndProcKeepAlive = WndProc;
            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = _wndProcKeepAlive,
                hInstance = GetModuleHandle(null),
                lpszClassName = WindowClassName,
            };
            if (RegisterClassEx(ref wc) == 0) return;

            var hwnd = CreateWindowEx(0, WindowClassName, "", 0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
            if (hwnd == IntPtr.Zero) return;

            data = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = hwnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = LoadExeIcon(),
                szTip = ToolTip,
            };
            Shell_NotifyIcon(NIM_ADD, ref data);

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            Shell_NotifyIcon(NIM_DELETE, ref data);
        }
        catch
        {
            // 托盘不可用时静默降级：悬浮窗本身不受影响
        }
    }

    /// <summary>取本进程 exe 的图标（与插件图标同源）。优先大图标，失败退回系统默认图标。</summary>
    private static IntPtr LoadExeIcon()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                if (ExtractIconEx(exe, 0, out var large, out var small, 1) > 0)
                    return large != IntPtr.Zero ? large : small;
            }
        }
        catch { }
        return LoadIcon(IntPtr.Zero, (IntPtr)32512 /* IDI_APPLICATION */);
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (msg == WM_TRAYICON)
            {
                // lParam 低 16 位是鼠标事件；同时兼容 WM_RBUTTONUP 与 WM_CONTEXTMENU
                // （新老 shell 行为不同，两者都处理可保证任何 Windows 版本下右键都能出菜单）
                var evt = (int)(lParam.ToInt64() & 0xFFFF);
                if (evt == WM_RBUTTONUP || evt == WM_CONTEXTMENU)
                    ShowContextMenu(hWnd);
            }
            else if (msg == WM_DESTROY)
            {
                PostQuitMessage(0);
            }
        }
        catch { }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private static void ShowContextMenu(IntPtr hWnd)
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero) return;
        try
        {
            // 菜单文案说明"退出"的语义：退出的只是这个独立进程，悬浮课表会由插件回退渲染继续显示。
            AppendMenu(menu, MF_STRING, (UIntPtr)CmdExit, "退出");
            if (!GetCursorPos(out var pt)) return;
            // 经典 Win32 要求：TrackPopupMenu 之前必须把宿主窗口设为前台，
            // 否则菜单不会在点击别处时消失（表现为"菜单卡在屏幕上"）。
            SetForegroundWindow(hWnd);
            int cmd = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, hWnd, IntPtr.Zero);
            if (cmd == CmdExit)
            {
                var cb = _onExit;
                if (cb != null) { try { cb(); } catch { } }
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public uint style;
        public WndProcDelegate? lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)NID_TIP_LENGTH)] public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public int uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(IntPtr hMenu, int uFlags, UIntPtr uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y, int nReserved,
        IntPtr hWnd, IntPtr prcRect);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex,
        out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
}
