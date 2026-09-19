// 时间表悬浮窗独立进程入口。
// 启动流程（严格顺序；①-⑤ 不得触碰任何 Avalonia 类型，避免 JIT 早于程序集解析注册）：
//   ① 解析参数  ② 宿主目录运行库预检  ③ AssemblyLoadContext.Resolving（借用宿主 Avalonia/Skia DLL）
//   ④ 进程级 PATH 注入（libSkiaSharp 等原生库，任何 P/Invoke 前）  ⑤ 单实例 Mutex + 宿主存活看门狗
//   ⑥ RunAvalonia（此处才 JIT Avalonia 类型）
// 约定退出码：42=已有实例（单实例保护）；43=宿主目录缺运行库；44=参数非法。
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AdvancedTimeIsland.Shared.FloatingSchedule;
using Avalonia;

namespace AdvancedTimeIsland.FloatScheduleChild;

internal static class Program
{
    // ===== 解析后的命令行参数（App/Window 读取）=====
    public static string HostDir { get; private set; } = "";
    public static string PipeName { get; private set; } = FloatScheduleIpc.PipeName;
    public static int ParentPid { get; private set; } = -1;
    /// <summary>跟随启停（初始值来自 --follow-lifetime；运行中以 settings 消息热更新，看门狗触发时读最新值）。</summary>
    public static volatile bool FollowHostLifetime = true;
    public static bool SingleInstance { get; private set; } = true;

    private static Mutex? _singleMutex;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);
    private const uint SYNCHRONIZE = 0x00100000;
    private const uint WAIT_OBJECT_0 = 0;
    private const uint WAIT_ABANDONED = 1;
    private const uint WAIT_TIMEOUT = 0x102;

    private static int Main(string[] args)
    {
        try
        {
            // ① 参数解析
            if (!ParseArgs(args)) return FloatScheduleIpc.ExitCodeBadArgs;

            // ② 宿主目录运行库预检（托管 + 原生；目录式发布平铺根目录，保险起见同时探测 runtimes\win-x64\native）
            if (string.IsNullOrEmpty(HostDir) || !Directory.Exists(HostDir))
                return FloatScheduleIpc.ExitCodeMissingRuntime;
            if (!File.Exists(Path.Combine(HostDir, "Avalonia.Base.dll")))
                return FloatScheduleIpc.ExitCodeMissingRuntime;
            bool nativeOk =
                File.Exists(Path.Combine(HostDir, "libSkiaSharp.dll")) ||
                File.Exists(Path.Combine(HostDir, "runtimes", "win-x64", "native", "libSkiaSharp.dll"));
            if (!nativeOk) return FloatScheduleIpc.ExitCodeMissingRuntime;

            // ③ 程序集解析：仅 UI 栈前缀按"简单名"从宿主目录加载（忽略 Version 桥接补丁差，如 ref 11.3.10 vs 宿主 11.3.17）
            System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (ctx, name) =>
            {
                try
                {
                    var n = name.Name ?? "";
                    if (!(n.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase) ||
                          n.StartsWith("SkiaSharp", StringComparison.OrdinalIgnoreCase) ||
                          n.StartsWith("HarfBuzzSharp", StringComparison.OrdinalIgnoreCase) ||
                          n.StartsWith("Tmds", StringComparison.OrdinalIgnoreCase) ||
                          n.StartsWith("MicroCom", StringComparison.OrdinalIgnoreCase)))
                        return null;
                    var path = Path.Combine(HostDir, n + ".dll");
                    return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
                }
                catch { return null; }
            };

            // ④ 原生库搜索路径（进程级 PATH；LoadLibrary 动态读取，覆盖根目录与 runtimes 两种布局）
            try
            {
                var extra = HostDir + Path.PathSeparator + Path.Combine(HostDir, "runtimes", "win-x64", "native");
                var cur = Environment.GetEnvironmentVariable("PATH") ?? "";
                Environment.SetEnvironmentVariable("PATH", extra + Path.PathSeparator + cur);
            }
            catch { }

            // ⑤ 单实例保护（Mutex 仅启动时获取；"单实例保护"设置变化无需重启，自下次启动生效）
            if (SingleInstance)
            {
                if (!TryAcquireSingleInstanceLock())
                    return FloatScheduleIpc.ExitCodeAnotherInstance;
            }

            // ⑤b 宿主存活看门狗：OpenProcess(SYNCHRONIZE) 句柄式等待（正常退出/崩溃/被强杀都会 signaled；
            //     句柄在宿主死亡时不会因 PID 复用误判），双保险等待宿主全局 Mutex abandoned。
            StartHostWatchdog();

            // ⑥ 启动 Avalonia（单独方法：此处才 JIT Avalonia 类型）
            return RunAvalonia(args);
        }
        catch (Exception ex)
        {
            try { Console.Error.WriteLine("ATI.FloatSchedule fatal: " + ex); } catch { }
            return FloatScheduleIpc.ExitCodeMissingRuntime;
        }
    }

    private static int RunAvalonia(string[] args)
    {
        try
        {
            // 【启动速度】子进程仅运行于 Windows：显式指定 Win32 + Skia 后端，
            //   跳过 UsePlatformDetect 的跨平台多后端探测（逐个尝试 X11/macOS 等），缩短冷启动。
            return AppBuilder.Configure<FloatScheduleApp>()
                .UseWin32()
                .UseSkia()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try { Console.Error.WriteLine("ATI.FloatSchedule avalonia start failed: " + ex); } catch { }
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
                case "--host-dir" when i + 1 < args.Length: HostDir = args[++i]; break;
                case "--pipe" when i + 1 < args.Length: PipeName = args[++i]; break;
                case "--parent-pid" when i + 1 < args.Length && int.TryParse(args[i + 1], out var pid): ParentPid = pid; i++; break;
                case "--follow-lifetime" when i + 1 < args.Length: FollowHostLifetime = args[++i] == "1"; break;
                case "--single-instance" when i + 1 < args.Length: SingleInstance = args[++i] == "1"; break;
            }
        }
        return !string.IsNullOrEmpty(PipeName);
    }

    // ===================== 单实例 =====================
    static bool TryAcquireSingleInstanceLock()
    {
        try
        {
            _singleMutex = new Mutex(true, FloatScheduleIpc.SingleInstanceMutexName, out var createdNew);
            if (createdNew) return true;
            _singleMutex.Dispose(); _singleMutex = null;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Global\ 命名空间无权限（非提权会话）→ 回退同会话 Local 前缀
            try
            {
                _singleMutex = new Mutex(true, "AdvancedTimeIsland.FloatSchedule.v1", out var createdNew2);
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
        if (ParentPid <= 0) return;
        var th = new Thread(() =>
        {
            bool gone;
            var h = OpenProcess(SYNCHRONIZE, false, ParentPid);
            if (h != IntPtr.Zero)
            {
                gone = WaitForSingleObject(h, unchecked((uint)Timeout.Infinite)) == WAIT_OBJECT_0;
                CloseHandle(h);
            }
            else
            {
                // 打不开宿主句柄（极端权限场景）→ 轮询进程存在性兜底（2s 间隔）。
                // 注意：不再使用"等待宿主全局 Mutex abandoned"作判据——宿主未持有该锁时
                // WaitOne 会立即返回，被误判为"宿主已退出" → 子进程反复自杀重启。
                gone = false;
                while (!gone)
                {
                    try { System.Diagnostics.Process.GetProcessById(ParentPid).Dispose(); }
                    catch { gone = true; break; }
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
        if (FloatScheduleApp.IsPipeConnectedNow()) return;
        if (FollowHostLifetime)
        {
            try { FloatScheduleApp.RequestExitGracefully(); } catch { }
            // 兜底：Shutdown 已切 UI 线程异步执行；若 UI 线程卡死无法完成 → 3 秒后强制退出
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Thread.Sleep(3000); } catch { }
                Environment.Exit(0);
            });
            return;
        }
        // 跟随启停=关：不退出，继续显示。窗口侧常驻的"本地推进器"会在推送停更后按本地时钟
        //   自主维护课表状态（换课 / 课间行 / 高亮 / 进度），并持续 1s 重连等待宿主重启后 adopt，
        //   因此这里无需额外动作。
    }
}
