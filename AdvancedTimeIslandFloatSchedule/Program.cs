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

    /// <summary>
    /// 本 exe 编译期对应的 Avalonia 大版本（与 csproj 的 AvaloniaVersion 一一对应）。
    /// 本 exe 是"双 TFM 各出一个"的产物，Avalonia 跨代二进制不兼容，必须与宿主精确配对：
    /// 把宿主安装目录里的 Avalonia 大版本与它比对，即可在**调用任何 Avalonia 类型之前**判定包是否选错。
    /// </summary>
#if NET10_0_OR_GREATER
    public const int ExpectedAvaloniaMajor = 12;   // net10.0-windows → Avalonia 12.x（ClassIsland 2.1.x / FA3）
#else
    public const int ExpectedAvaloniaMajor = 11;   // net8.0-windows  → Avalonia 11.x（ClassIsland 2.0.x / FA2）
#endif

    /// <summary>宿主下传的"期望 Avalonia 大版本"（0 = 宿主未下传）。</summary>
    public static int HostExpectedAvaloniaMajor { get; private set; }

    /// <summary>
    /// 本进程 exe 的**内容指纹**（进程启动瞬间采集，= 本进程实际运行的代码身份）。
    /// 用于自检"我是否已被新版插件淘汰"：插件在 Init 里下发其包内 exe 的内容指纹，
    /// 与这里不一致 ⇒ 运行中的是旧构建 ⇒ 主动退出让插件用新版重启。
    /// 【为何在启动瞬间采集、而不是收到 Init 时再读文件】插件重启子进程时会用新 exe 覆盖同一路径的
    ///   运行副本；若到 Init 时才读文件，读到的已是新文件 → 指纹"自洽" → 永远检测不出被淘汰。
    /// 【为何用内容哈希而非长度+修改时间】同一构建的不同副本（插件目录原文件 vs 临时副本）
    ///   长度相同但修改时间必然不同，用时间戳会把"同版本"误判为"已更新"→ 每次宿主重启都无谓重启子进程
    ///   （而 adopt 的设计目的正是让冻结窗口跨宿主重启存活）。
    /// </summary>
    public static string SelfExeHash { get; private set; } = "";

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

            // ①b 采集自身 exe 内容指纹（必须最早：此后插件可能用新 exe 覆盖本路径的运行副本）
            SelfExeHash = ComputeSelfExeHash();

            // ② 宿主目录运行库预检（托管 + 原生；目录式发布平铺根目录，保险起见同时探测 runtimes\win-x64\native）
            if (string.IsNullOrEmpty(HostDir) || !Directory.Exists(HostDir))
                return FloatScheduleIpc.ExitCodeMissingRuntime;
            if (!File.Exists(Path.Combine(HostDir, "Avalonia.Base.dll")))
                return FloatScheduleIpc.ExitCodeMissingRuntime;
            bool nativeOk =
                File.Exists(Path.Combine(HostDir, "libSkiaSharp.dll")) ||
                File.Exists(Path.Combine(HostDir, "runtimes", "win-x64", "native", "libSkiaSharp.dll"));
            if (!nativeOk) return FloatScheduleIpc.ExitCodeMissingRuntime;

            // ②b Avalonia 代际预检：宿主安装目录里的 Avalonia 大版本必须与本 exe 的编译目标一致。
            //   本 exe 与宿主 Avalonia 是"同一代才能加载"的关系（跨代 TypeLoad/MissingMethod 级别不兼容），
            //   而两个 TFM 的 exe 文件名完全相同 → 用户很容易把 net8 包装进 Avalonia 12 的宿主里。
            //   此处在**未触碰任何 Avalonia 类型之前**用文件版本号判定，给出专属退出码与明确提示，
            //   不再让这种"包选错"退化成泛化启动异常、被宿主端误报为"缺少 Avalonia/Skia 运行库"。
            //
            //   两道判据（任一命中即判定包选错）：
            //     a) 宿主下传的期望代际 ≠ 本 exe 编译目标 → exe 与插件 DLL 不同源（混装了不同 TFM 的产物）；
            //     b) 宿主目录 Avalonia 文件版本 ≠ 本 exe 编译目标 → 包装错代际。
            bool exePackageMismatch = HostExpectedAvaloniaMajor > 0 && HostExpectedAvaloniaMajor != ExpectedAvaloniaMajor;
            var hostAvaloniaMajor = ReadHostAvaloniaMajor();
            bool hostEngineMismatch = hostAvaloniaMajor > 0 && hostAvaloniaMajor != ExpectedAvaloniaMajor;
            if (exePackageMismatch || hostEngineMismatch)
            {
                try
                {
                    Console.Error.WriteLine(
                        $"ATI.FloatSchedule avalonia generation mismatch: host={hostAvaloniaMajor}.x, " +
                        $"pluginExpects={HostExpectedAvaloniaMajor}, thisExe={ExpectedAvaloniaMajor}.x " +
                        "(wrong plugin package for this ClassIsland build)");
                }
                catch { }
                return FloatScheduleIpc.ExitCodeAvaloniaMismatch;
            }

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

    /// <summary>
    /// 读取宿主安装目录里 Avalonia.Base.dll 的大版本号（0 = 读不到，调用方应跳过校验）。
    /// 用文件版本而不是"尝试加载程序集"：此处必须早于任何 Avalonia 类型使用（JIT 期就可能抛 TypeLoad）。
    /// </summary>
    private static int ReadHostAvaloniaMajor()
    {
        try
        {
            var path = Path.Combine(HostDir, "Avalonia.Base.dll");
            if (!File.Exists(path)) return 0;
            var v = System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
            if (string.IsNullOrEmpty(v)) return 0;
            var dot = v.IndexOf('.');
            return int.TryParse(dot > 0 ? v[..dot] : v, out var major) ? major : 0;
        }
        catch { return 0; }
    }

    private static int RunAvalonia(string[] args)
    {
        try
        {
            // 【启动速度】子进程仅运行于 Windows：显式指定 Win32 + Skia 后端，
            //   跳过 UsePlatformDetect 的跨平台多后端探测（逐个尝试 X11/macOS 等），缩短冷启动。
            var builder = AppBuilder.Configure<FloatScheduleApp>()
                .UseWin32()
                .UseSkia();
#if NET10_0_OR_GREATER
            // 【修复：FA3（Avalonia 12）上独立进程必然启动失败】Avalonia 12 把"文本整形"拆成独立包
            //   Avalonia.HarfBuzz，必须显式注册；否则 AppBuilder.Setup() 直接抛
            //   InvalidOperationException: "No text shaping system configured. Consider calling UseHarfBuzz()"
            //   → 被下面 catch 归一成"缺少运行库"退出码，用户看到的是完全错误的提示。
            //   Avalonia 11 没有这个 API（整形内建于 UseSkia），故只在 net10 分支调用。
            builder = builder.UseHarfBuzz();
#endif
            return builder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try { Console.Error.WriteLine("ATI.FloatSchedule avalonia start failed: " + ex); } catch { }
            // 【修复：包选错被误报为"缺少运行库"】预检（②b）已按文件版本拦下绝大多数代际不匹配；
            //   这里再按"实际加载到的 Avalonia 大版本"兜一道 —— 若确实不匹配，返回专属退出码，
            //   让宿主端给出"插件包选错、该装哪个包"的准确提示，而不是泛化的运行库缺失。
            try
            {
                var loadedMajor = typeof(Avalonia.Application).Assembly.GetName().Version?.Major ?? 0;
                if (loadedMajor > 0 && loadedMajor != ExpectedAvaloniaMajor)
                    return FloatScheduleIpc.ExitCodeAvaloniaMismatch;
            }
            catch { }
            // 运行库确实就位、代际也匹配，却仍在启动阶段抛异常 → 归为"启动失败"（专属退出码），
            //   宿主端据此提示"启动失败"并附上子进程 stderr，不再误报"缺少 Avalonia/Skia 运行库"。
            return FloatScheduleIpc.ExitCodeAvaloniaStartFailed;
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
                // 宿主下传的"期望 Avalonia 大版本"（宿主插件的编译目标）。仅在自校验**读不到**宿主
                // Avalonia 文件版本、或与自身编译目标冲突时用于加严判定，正常时以自身编译目标为准。
                case "--avalonia-major" when i + 1 < args.Length && int.TryParse(args[i + 1], out var avm): HostExpectedAvaloniaMajor = avm; i++; break;
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
