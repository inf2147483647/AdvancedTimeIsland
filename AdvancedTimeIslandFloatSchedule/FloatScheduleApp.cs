// 子进程 Application：无主题资源（悬浮窗是纯代码控件树，颜色/字体全部由渲染模型显式携带），
// 因此不加载 FluentAvalonia / FluentTheme，最大化跨 Avalonia 代际兼容性。
using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AdvancedTimeIsland.FloatScheduleChild;

internal class FloatScheduleApp : Application
{
    public static FloatScheduleApp? Self { get; private set; }
    public static FloatScheduleChildWindow? MainWindow { get; private set; }

    public override void Initialize()
    {
        Self = this;
        // 子进程不加载任何控件主题：
        //   悬浮窗自身是纯代码控件树，颜色/字体全部由渲染模型显式携带，不依赖主题资源；
        //   托盘图标与右键菜单走原生 Win32 实现（FloatScheduleTrayIcon），同样不需要主题
        //   （宿主 ClassIsland 使用 FluentAvalonia，其依赖图里并不包含 Avalonia.Themes.Fluent，
        //    加载 Avalonia 主题在运行时会失败，故一律不引主题）。
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop)
        {
            var w = new FloatScheduleChildWindow();
            MainWindow = w;
            desktop.MainWindow = w;
            // 窗口初始不显示：收到插件 Init（设置+模型）后才 Show，避免空窗闪现
            w.StartWithPipe();
            // 托盘图标**不在此处创建**：Avalonia/Win32 的托盘菜单需要宿主顶层窗口已就绪才可靠弹出，
            //   因此推迟到窗口首次 Show 之后（见 EnsureTrayIcon，由窗口在显示时调用）。
        }
        base.OnFrameworkInitializationCompleted();
    }

    // ===== 托盘图标（与插件图标同源，取自本进程 exe 内嵌图标）=====
    //  实现见 FloatScheduleTrayIcon：原生 Win32（Shell_NotifyIcon + 原生右键菜单），
    //  不依赖 Avalonia 主题（Avalonia 的托盘菜单需要控件样式，而子进程刻意不加载主题）。
    private int _trayIconStarted;

    /// <summary>创建托盘图标（幂等；由窗口在首次显示后调用）。</summary>
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
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (Self?.ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                    return;
                }
            }
            catch { }
            Environment.Exit(0);   // 无生命周期对象/Shutdown 异常时兜底
        });
    }

}
