// 子进程专用 Win32 帮助类：语义对齐主项目 Services\FloatingScheduleService.cs 的
// 置顶/置底竞争防护、点击穿透三件套、防截图、DPI 兜底等（常量与实测结论同源，勿随意改动）。
// 不放入 Shared\：主项目侧 P/Invoke 与其状态机深度耦合，保持零改动；本文件仅子进程编译。
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AdvancedTimeIsland.FloatScheduleChild;

internal static class FloatScheduleNative
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    public const uint GW_HWNDFIRST = 0;
    public const uint GW_HWNDNEXT = 2;

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    public static readonly uint OwnProcessId = (uint)Environment.ProcessId;

    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_NOSENDCHANGING = 0x0400;
    public const uint SWP_NOOWNERZORDER = 0x0200;

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")]
    public static extern bool SetWindowDpiAwarenessContext(IntPtr hWnd, IntPtr value);
    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [DllImport("user32.dll")]
    public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    [DllImport("user32.dll")]
    public static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint dwAffinity);
    public const uint WDA_NONE = 0x00000000;
    public const uint WDA_MONITOR = 0x00000001;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    /// <summary>期望扩展样式位（唯一事实源，对齐主项目 ComputeDesiredExStyleAv；不设 WS_EX_COMPOSITED——
    /// 该位与 WS_EX_TRANSPARENT 同写会被系统静默丢弃，导致点击穿透失效）。</summary>
    public static int ComputeDesiredExStyle(int ex, bool clickThrough, bool preventCapture, int layer)
    {
        ex |= WS_EX_TOOLWINDOW;
        bool wantLayered = !preventCapture || clickThrough;
        if (wantLayered) ex |= WS_EX_LAYERED;
        else ex &= ~WS_EX_LAYERED;
        if (clickThrough) ex |= WS_EX_TRANSPARENT;
        else ex &= ~WS_EX_TRANSPARENT;
        // 穿透开启：永不激活；穿透关闭：置底窗口永不激活（激活会被系统强制提升 z-order），置顶允许激活
        if (clickThrough || layer != 1 /*Topmost*/) ex |= WS_EX_NOACTIVATE;
        else ex &= ~WS_EX_NOACTIVATE;
        return ex;
    }

    /// <summary>读取当前 exStyle 并收敛到期望（值相同不写，避免高频触发 DWM 重评估闪烁）。
    /// 【★ 不要调用 SetLayeredWindowAttributes】置上 WS_EX_LAYERED 后再声明分层属性（alpha=255, LWA_ALPHA）
    /// 会让 DWM 改用"分层窗口"合成路径，而 Avalonia 在 Win8.x 上走 RedirectionSurface（透明靠
    /// DwmEnableBlurBehindWindow），其呈现内容不进入该路径 → 悬浮窗完全不可见（Win10/11 走 DComp 视觉树，
    /// 故此前未暴露）。实测仅需 LAYERED|TRANSPARENT 即可穿透，无需分层属性；主项目 ApplyExStylesAv 与
    /// WPF 版同样从不调用该 API（对齐 2.0.5.1 #8 修复）。</summary>
    public static bool ApplyExStyles(IntPtr hwnd, int target)
    {
        int current = (int)(long)GetWindowLong(hwnd, GWL_EXSTYLE);
        if (target == current) return false;
        SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)target);
        return true;
    }

    /// <summary>真实 Z 序探测·置底：沿 GW_HWNDNEXT 向下扫描是否存在"其它进程的普通可见窗口"
    /// （排除桌面 Shell/本进程/置顶/NOACTIVATE 同类窗口——否则正确置底时永远误判"被抬起"→高频闪烁）。</summary>
    public static bool IsRaisedAboveForeignWindow(IntPtr hwnd)
    {
        try
        {
            var below = GetWindow(hwnd, GW_HWNDNEXT);
            while (below != IntPtr.Zero)
            {
                if (IsWindowVisible(below) && !IsShellOrOwnProcessWindow(below))
                {
                    var exStyle = (int)(long)GetWindowLong(below, GWL_EXSTYLE);
                    if ((exStyle & WS_EX_TOPMOST) == 0 && (exStyle & WS_EX_NOACTIVATE) == 0)
                        return true;
                }
                below = GetWindow(below, GW_HWNDNEXT);
            }
        }
        catch { }
        return false;
    }

    static bool IsShellOrOwnProcessWindow(IntPtr hwnd)
    {
        try
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == OwnProcessId) return true;
            var sb = new StringBuilder(64);
            if (GetClassName(hwnd, sb, sb.Capacity) <= 0) return false;
            return sb.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";
        }
        catch { return true; }
    }

    /// <summary>把前台焦点让给 z-order 下方第一个可见窗口（点击穿透开启且本窗恰为前台时）。</summary>
    public static void MoveFocusToWindowBehind(IntPtr hwnd)
    {
        try
        {
            var next = GetWindow(hwnd, GW_HWNDNEXT);
            while (next != IntPtr.Zero)
            {
                if (IsWindowVisible(next)) { SetForegroundWindow(next); return; }
                next = GetWindow(next, GW_HWNDNEXT);
            }
        }
        catch { }
    }
}
