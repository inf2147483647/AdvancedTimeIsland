// 时间表悬浮窗"独立进程模式" WPF 子进程入口（ClassIsland 1.x 侧，.NET 8 桌面运行时）。
// 与 Avalonia 版子进程（AdvancedTimeIslandFloatSchedule\Program.cs）同构，差别只有三点：
//   ① 不借用宿主目录的 Avalonia/Skia DLL —— WPF 走框架依赖的 Microsoft.WindowsDesktop.App；
//   ② 管道名 / 单实例 Mutex 名加 ".wpf" 后缀（见 WpfNameSuffix），与 Avalonia 版子进程互不抢占，两者可并存；
//   ③ 不再有 Avalonia 代际校验（Avalonia 专属的 45/46 号退出码不适用）。
// 启动流程（严格顺序）：
//   ① 解析参数 ② 自身 exe 内容指纹 ③ Windows 桌面运行库预检 ④ 单实例 Mutex ⑤ 宿主存活看门狗 ⑥ WPF 启动
// 约定退出码（沿用 FloatScheduleIpc 常量）：42=已有实例；43=缺 Windows 桌面运行库/启动失败；44=参数非法；47=自检发现已被新版插件淘汰。
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AdvancedTimeIsland.Shared.FloatingSchedule;

namespace AdvancedTimeIsland.FloatScheduleWpfChild;

internal static class Program
{
    /// <summary>
    /// 管道名 / 单实例 Mutex 名的 WPF 后缀。
    /// 【为什么必须加】Avalonia 版与 WPF 版子进程协议完全相同、并以固定名连管道/抢 Mutex：
    ///   不加后缀时，两个子进程会互相抢管道连接（后到者拿到的是对方的服务器）、互相触发单实例退出。
    /// </summary>
    public const string WpfNameSuffix = ".wpf";

    // ===== 解析后的命令行参数（App/Window 读取）=====
    /// <summary>宿主下传的安装目录。WPF 版**不需要**从宿主目录借任何 DLL，此处仅作记录/诊断。</summary>
    public static string HostDir { get; private set; } = "";
    public static string PipeName { get; private set; } = FloatScheduleIpc.PipeName + WpfNameSuffix;
    public static int ParentPid { get; private set; } = -1;
    /// <summary>跟随启停（初始值来自 --follow-lifetime；运行中以 settings 消息热更新，看门狗触发时读最新值）。</summary>
    public static volatile bool FollowHostLifetime = true;
    public static bool SingleInstance { get; private set; } = true;

    /// <summary>
    /// 本进程 exe 的**内容指纹**（进程启动瞬间采集，= 本进程实际运行的代码身份）。
    /// 用于自检"我是否已被新版插件淘汰"：插件在 Init 里下发其包内 exe 的内容指纹，
    /// 与这里不一致 ⇒ 运行中的是旧构建 ⇒ 主动退出让插件用新版重启。
    /// 【为何在启动瞬间采集】插件重启子进程时会用新 exe 覆盖同一路径的运行副本；
    ///   若到 Init 时才读文件，读到的已是新文件 → 指纹"自洽" → 永远检测不出被淘汰。
    /// 【为何用内容哈希而非长度+修改时间】同一构建的不同副本长度相同但修改时间必然不同，
    ///   用时间戳会把"同版本"误判为"已更新"→ 每次宿主重启都无谓重启子进程。
    /// </summary>
    public static string SelfExeHash { get; private set; } = "";

    private static Mutex? _singleMutex;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);
    private const uint SYNCHRONIZE = 0x00100000;
    private const uint WAIT_OBJECT_0 = 0;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            // ① 参数解析
            if (!ParseArgs(args)) return FloatScheduleIpc.ExitCodeBadArgs;

            // ①b 采集自身 exe 内容指纹（必须最早：此后插件可能用新 exe 覆盖本路径的运行副本）
            SelfExeHash = ComputeSelfExeHash();

            // ② Windows 桌面运行库预检
            if (!HasWindowsDesktopRuntime()) return FloatScheduleIpc.ExitCodeMissingRuntime;

            // ③ 单实例保护（Mutex 仅启动时获取；"单实例保护"设置变化无需重启，自下次启动生效）
            if (SingleInstance && !TryAcquireSingleInstanceLock())
                return FloatScheduleIpc.ExitCodeAnotherInstance;

            // ④ 宿主存活看门狗
            StartHostWatchdog();

            // ⑤ 启动 WPF（单独方法：此处才触碰 WPF 类型）
            return RunWpf();
        }
        catch (Exception ex)
        {
            try { Console.Error.WriteLine("ATI.WPFFloatSchedule fatal: " + ex); } catch { }
            // 归为 43（"运行库/启动"类代码）：WPF 无 Avalonia 那样的专属启动失败码，宿主侧按 43 提示
            return FloatScheduleIpc.ExitCodeMissingRuntime;
        }
    }

    /// <summary>
    /// Windows 桌面运行库（Microsoft.WindowsDesktop.App）预检。
    /// WPF 是框架依赖：缺该共享框架时 hostfxr 阶段就会拒绝启动（宿主看到的只是运行时的退出码，
    /// 无法区分病因）。这里在托管 Main（说明 CoreCLR 已起来）里再确认 PresentationFramework.dll
    /// 就在当前运行时目录里，缺失则以 43（缺运行库）退出，与 Avalonia 版语义一致。
    /// </summary>
    private static bool HasWindowsDesktopRuntime()
    {
        try
        {
            var dir = RuntimeEnvironment.GetRuntimeDirectory();
            return !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "PresentationFramework.dll"));
        }
        catch { return false; }
    }

    private static string ComputeSelfExeHash()
    {
        try
        {
            var p = Environment.ProcessPath;
            if (string.IsNullOrEmpty(p) || !File.Exists(p)) return "";
            using var fs = File.OpenRead(p);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(fs));
        }
        catch { return ""; }
    }

    private static int RunWpf()
    {
        try
        {
            var app = new FloatScheduleWpfApp();
            return app.Run();
        }
        catch (Exception ex)
        {
            try { Console.Error.WriteLine("ATI.WPFFloatSchedule wpf start failed: " + ex); } catch { }
            return FloatScheduleIpc.ExitCodeMissingRuntime;
        }
    }

    // ===================== 参数解析 =====================
    static bool ParseArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                // --host-dir 仅为命令行兼容（宿主按 Avalonia 版约定下传）：WPF 不从宿主目录借 DLL，收到即忽略。
                case "--host-dir" when i + 1 < args.Length: HostDir = args[++i]; break;
                case "--pipe" when i + 1 < args.Length: PipeName = NormalizePipeName(args[++i]); break;
                case "--parent-pid" when i + 1 < args.Length && int.TryParse(args[i + 1], out var pid): ParentPid = pid; i++; break;
                case "--follow-lifetime" when i + 1 < args.Length: FollowHostLifetime = args[++i] == "1"; break;
                case "--single-instance" when i + 1 < args.Length: SingleInstance = args[++i] == "1"; break;
                // --avalonia-major 是 Avalonia 版专属参数，WPF 版无代际概念：容忍并忽略（宿主两版共用命令行拼装代码）
                case "--avalonia-major" when i + 1 < args.Length: i++; break;
            }
        }
        return !string.IsNullOrEmpty(PipeName);
    }

    /// <summary>
    /// 归一化管道名：宿主可能下传"基础名 + 后缀"或"基础名"两种形态，这里统一补齐 ".wpf" 后缀，
    /// 保证 WPF 版子进程永远连 WPF 版宿主（Avalonia 版宿主）的管道监听端。
    /// </summary>
    static string NormalizePipeName(string name)
        => name.EndsWith(WpfNameSuffix, StringComparison.OrdinalIgnoreCase) ? name : name + WpfNameSuffix;

    // ===================== 单实例 =====================
    static bool TryAcquireSingleInstanceLock()
    {
        try
        {
            _singleMutex = new Mutex(true, FloatScheduleIpc.SingleInstanceMutexName + WpfNameSuffix, out var createdNew);
            if (createdNew) return true;
            _singleMutex.Dispose(); _singleMutex = null;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Global\ 命名空间无权限（非提权会话）→ 回退同会话 Local 前缀
            try
            {
                _singleMutex = new Mutex(true, "AdvancedTimeIsland.FloatSchedule.v1" + WpfNameSuffix, out var createdNew2);
                if (createdNew2) return true;
                _singleMutex.Dispose(); _singleMutex = null;
                return false;
            }
            catch { return true; }   // 拿不到锁信息时放行（功能降级但不阻塞显示）
        }
        catch { return true; }
    }

    // ===================== 宿主存活看门狗 =====================
    static void StartHostWatchdog()
    {
        var th = new Thread(() =>
        {
            bool gone;
            if (ParentPid > 0)
            {
                // 首选：进程句柄式等待（正常退出/崩溃/被强杀都会 signaled；句柄在宿主死亡时不会因 PID 复用误判）
                var h = OpenProcess(SYNCHRONIZE, false, ParentPid);
                if (h != IntPtr.Zero)
                {
                    gone = WaitForSingleObject(h, unchecked((uint)Timeout.Infinite)) == WAIT_OBJECT_0;
                    CloseHandle(h);
                }
                else
                {
                    // 打不开宿主句柄（极端权限场景）→ 轮询进程存在性兜底（2s 间隔）
                    gone = false;
                    while (!gone)
                    {
                        try { System.Diagnostics.Process.GetProcessById(ParentPid).Dispose(); }
                        catch { gone = true; break; }
                        try { Thread.Sleep(2000); } catch { break; }
                    }
                }
            }
            else
            {
                // 无 --parent-pid（宿主未下传）→ 退化为"等宿主全局 Mutex 消失"。
                // 【为何不等待 Mutex abandoned】宿主未持有该锁时 WaitOne 会立即返回，会被误判为"宿主已退出"
                //   → 子进程反复自杀重启（Avalonia 版已因此去掉该判据）。这里只观察**命名对象本身是否还存在**：
                //   宿主退出后其句柄全部关闭 → 对象随之消失 → OpenExisting 抛异常，判据可靠且零误报。
                //   首次观察时对象就不存在（宿主未持有全局锁）→ 无法观察，直接放弃看门狗（保持存活，不误退）。
                bool observed = false;
                gone = false;
                while (true)
                {
                    try
                    {
                        using var hostMutex = Mutex.OpenExisting(FloatScheduleIpc.HostInstanceMutexName);
                        observed = true;   // 对象存在 = 宿主（或另一实例）仍在
                    }
                    catch
                    {
                        gone = observed;   // 之前观察到过、现在消失 → 宿主已退出
                        break;
                    }
                    try { Thread.Sleep(2000); } catch { break; }
                }
            }
            if (gone) OnHostGone();
        })
        { IsBackground = true };
        th.Start();
    }

    /// <summary>宿主进程终止（正常/崩溃/强杀）。读最近缓存的 FollowHostLifetime 决定退出或冻结。</summary>
    internal static void OnHostGone()
    {
        // 管道仍连接说明宿主进程必然存活（句柄等待误报）→ 忽略本次触发
        if (FloatScheduleWpfApp.IsPipeConnectedNow()) return;
        if (FollowHostLifetime)
        {
            try { FloatScheduleWpfApp.RequestExitGracefully(); } catch { }
            // 兜底：Shutdown 已切 UI 线程异步执行；若 UI 线程卡死无法完成 → 3 秒后强制退出
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Thread.Sleep(3000); } catch { }
                Environment.Exit(0);
            });
            return;
        }
        // 跟随启停=关：不退出，继续显示。窗口侧常驻的"本地推进器"会在推送停更后按本地时钟
        //   自主维护课表状态（换课 / 课间行 / 高亮 / 进度），并持续 1s 重连等待宿主重启后 adopt。
    }
}
