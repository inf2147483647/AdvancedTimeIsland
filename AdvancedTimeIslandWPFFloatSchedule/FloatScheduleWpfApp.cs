// WPF 子进程 Application。
// 生命周期完全由管道/看门狗驱动：故意用 ShutdownMode.OnExplicitShutdown —— 窗口的关闭/隐藏都不能
// 带走进程（"跟随启停=关"时窗口要在宿主退出后继续存活），所有退出路径都显式走 Shutdown() 或 Environment.Exit。
using System;
using System.Threading;
using System.Windows;

namespace AdvancedTimeIsland.FloatScheduleWpfChild;

internal sealed class FloatScheduleWpfApp : Application
{
    public static FloatScheduleWpfApp? Self { get; private set; }
    public static FloatScheduleWpfChildWindow? MainWindow { get; private set; }

    private int _trayIconStarted;

    public FloatScheduleWpfApp()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Self = this;
        var w = new FloatScheduleWpfChildWindow();
        MainWindow = w;
        // 窗口初始不显示：收到插件 Init（设置+模型）后才 Show，避免空窗闪现
        w.StartWithPipe();
        // 托盘图标**不在此处创建**：托盘菜单需要窗口/消息循环就绪才可靠弹出，推迟到窗口首次显示之后
        //   （见 EnsureTrayIcon，由窗口在首次收到数据时调用）。
    }

    // ===== 托盘图标（与插件图标同源，取自本进程 exe 内嵌图标）=====
    //  实现见 FloatScheduleTrayIcon：原生 Win32（Shell_NotifyIcon + 原生右键菜单），零额外程序集依赖。
    /// <summary>创建托盘图标（幂等；由窗口在首次收到数据后调用）。</summary>
    public void EnsureTrayIcon()
    {
        if (Interlocked.Exchange(ref _trayIconStarted, 1) != 0) return;
        FloatScheduleTrayIcon.Show(() => MainWindow?.RequestExitByUser());
    }

    /// <summary>管道当前是否连接（看门狗误报守卫：连接中 = 宿主必然存活）。</summary>
    public static bool IsPipeConnectedNow() => MainWindow?.PipeConnected == true;

    /// <summary>宿主退出（跟随启停=开）：优雅关窗退出。可能从后台看门狗线程调用 → 必须切 UI 线程。</summary>
    public static void RequestExitGracefully()
    {
        var app = Current;
        if (app == null) { Environment.Exit(0); return; }   // 应用尚未起来（极端时序）：直接退出，避免留下孤儿进程
        try
        {
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 先放行窗口关闭守卫：否则本窗口为防误关而 Cancel 的 Closing 会连带取消 Shutdown
                try { MainWindow?.AllowCloseForExit(); } catch { }
                try { app.Shutdown(); return; } catch { }
                Environment.Exit(0);   // Shutdown 异常时兜底
            }));
        }
        catch { Environment.Exit(0); }
    }
}
