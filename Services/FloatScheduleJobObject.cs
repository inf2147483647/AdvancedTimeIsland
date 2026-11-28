// Windows Job Object 封装：把子进程与宿主进程的生命周期绑定。
// 用途（仅"跟随启停=开"时）：宿主进程无论是正常退出、崩溃还是被任务管理器强杀，
//   只要宿主进程结束，内核就会关闭最后一个 job 句柄并终止 job 内全部进程 ——
//   这是比"子进程侧看门狗"更彻底的防僵尸机制（看门狗依赖子进程线程存活且能被调度）。
// 关键实现要点（踩坑记录，勿随意改动）：
//   1) SECURITY_ATTRIBUTES.bInheritHandle 必须为 false：否则子进程会继承 job 句柄，
//      宿主退出后句柄仍被引用，KILL_ON_JOB_CLOSE 不生效、僵尸照旧；
//   2) job 句柄必须活到插件进程结束，**不能**用 using/提前 CloseHandle，否则子进程会被立即杀掉；
//   3) 加入操作必须发生在子进程启动后尽早时机（本类在 Process.Start 后立即调用）；
//   4) Win8+ 支持嵌套 job，宿主本身已在 job 中时仍可加入；失败则静默降级（由子进程看门狗兜底）。
// 仅 Windows 生效；非 Windows / 初始化失败时 TryAssign 返回 false，调用方无副作用。
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AdvancedTimeIsland.Services;

internal static class FloatScheduleJobObject
{
    private const int JobObjectExtendedLimitInformation = 9;
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    private static readonly object Lock = new();
    private static IntPtr _jobHandle = IntPtr.Zero;
    private static bool _initFailed;

    /// <summary>把子进程加入"随宿主进程退出而终止"的 Job Object。失败返回 false（静默降级）。</summary>
    public static bool TryAssign(Process process)
    {
        if (!OperatingSystem.IsWindows() || process == null) return false;
        lock (Lock)
        {
            if (_initFailed) return false;
            if (_jobHandle == IntPtr.Zero && !EnsureJob()) return false;
            try
            {
                if (!AssignProcessToJobObject(_jobHandle, process.Handle)) return false;
                return true;
            }
            catch { return false; }
        }
    }

    private static bool EnsureJob()
    {
        IntPtr saPtr = IntPtr.Zero;
        try
        {
            var sa = new SECURITY_ATTRIBUTES
            {
                nLength = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = false,   // 见文件头要点 1
            };
            saPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SECURITY_ATTRIBUTES>());
            Marshal.StructureToPtr(sa, saPtr, false);

            var h = CreateJobObject(saPtr, null);
            if (h == IntPtr.Zero) { _initFailed = true; return false; }

            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
                },
            };
            int len = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            IntPtr infoPtr = Marshal.AllocHGlobal(len);
            try
            {
                Marshal.StructureToPtr(info, infoPtr, false);
                if (!SetInformationJobObject(h, JobObjectExtendedLimitInformation, infoPtr, (uint)len))
                {
                    CloseHandle(h);
                    _initFailed = true;
                    return false;
                }
            }
            finally { Marshal.FreeHGlobal(infoPtr); }

            _jobHandle = h;   // 故意不释放（见文件头要点 2）
            return true;
        }
        catch
        {
            _initFailed = true;
            return false;
        }
        finally
        {
            if (saPtr != IntPtr.Zero) Marshal.FreeHGlobal(saPtr);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)] public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
