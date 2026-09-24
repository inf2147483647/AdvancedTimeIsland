using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AdvancedTimeIsland.Models;
using Avalonia.Threading;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Plugin;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 跨插件联动辅助类
/// </summary>
public static class CrossPluginHelper
{
    private const string FemboyTestIdentifier = "FemboyTest";
    private const string FemboyTestIdentifierSignature = "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F";

    /// <summary>
    /// FemboyTest 各"独立进程"版本的跨进程"正在运行"命名事件名（内嵌真身签名，冒充程序无法预知）。
    /// 需与 FemboyTestWpf 插件 Plugin.RunningEventName、FemboyTest_publish 独立版 Program.RunningEventName、
    /// FemboyTest_SecRandom 插件 Plugin.RunningEventName，以及三个原生 exe
    /// （FemboyTestWin32 / FemboyTestWin64 / FemboyTestWinOld）的 CreateEventW 名称保持一致。
    /// </summary>
    private const string FemboyTestRunningEventName = "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F-Running";

    /// <summary>
    /// 彩蛋被强制重置（FemboyTest 与女装彩蛋互斥）时触发
    /// </summary>
    public static event Action? EasterEggForceReset;

    /// <summary>
    /// 互斥判定的**唯一**入口：FemboyTest（任意发行版本）是否处于需要与"女装"彩蛋互斥的状态。
    /// 彩蛋锁（拦截触发、重置已触发状态）与"女装"页内的警告栏必须都调用本方法，
    /// 不得在调用点各自拼表达式：历史上警告栏曾被单独收窄为"仅运行中"，
    /// 与彩蛋锁的判定不一致，形成"锁已释放但警告栏仍在"（或反之）的绕过路径。
    /// 判定为三条路径的**并集**：
    /// 1. 真身双因子反射扫描（见 <see cref="IsFemboyTestPresentByIdentifier"/>）——
    ///    覆盖与本插件同进程的 CI2 版（FemboyTest / FemboyTestNet10）；
    /// 2. 跨进程命名事件（见 <see cref="IsFemboyTestRunningProcess"/>）——
    ///    覆盖运行在其他进程中的版本：WPF 插件版、独立 Avalonia 版（FemboyTest_publish 及其安装包）、
    ///    SecRandom 插件版，以及三个原生 exe（Win32 / win64 / win_old）；事件名内嵌真身签名、冒充者无法伪造；
    /// 3. 标识符文件（见 <see cref="IsFemboyTestIdentifierPresent"/>）——
    ///    兜底"不在本进程、又未发布命名事件"的 CI2 插件版（反射看不到的那一类）：
    ///    只要该插件已安装且未被 .disabled 标记禁用，即视为生效。
    /// 路径 1/2 回答"是否正在运行"，路径 3 回答"是否已启用"；取并集才能保证
    /// 多个 FemboyTest 实例并存时，关闭其中任意一个都不会让互斥提前解除。
    /// 本检测与插件加载顺序无关，无需修改 ClassIsland 或声明 manifest 依赖：
    /// ClassIsland 在插件加载阶段之前会先扫描所有插件目录并把它们（含禁用的）加入
    /// IPluginService.LoadedPlugins，因此本插件 Initialize 时检测立即可用。
    /// 原"程序集名精确匹配"与"清单 ID 匹配"的名称判定可被同名冒充，已不再单独作为互斥依据；
    /// "禁用后重启前程序集仍驻留内存"的场景由签名扫描覆盖：该程序集仍在
    /// AssemblyLoadContext 中，反射扫描依然可见，互斥在重启前持续生效。
    /// </summary>
    public static bool IsFemboyTestEnabled()
    {
        // 检测路径一：双因子签名反射扫描（同进程 CI2 版）
        if (IsFemboyTestPresentByIdentifier())
            return true;

        // 检测路径二：跨进程命名事件检测（WPF 插件版 / 独立 Avalonia 版 / SecRandom 插件版 / 三个原生 exe）
        if (IsFemboyTestRunningProcess())
            return true;

        // 检测路径三：标识符文件检测（不在本进程的 CI2 插件版兜底）
        if (IsFemboyTestIdentifierPresent())
            return true;

        return false;
    }

    /// <summary>
    /// 跨进程检测运行在独立进程中的 FemboyTest 是否正在运行。
    /// 以下版本均不在 ClassIsland 进程内，无法被本进程内的反射扫描覆盖，因此统一由各自进程启动时
    /// 创建命名事件 <see cref="FemboyTestRunningEventName"/> 作为运行信号：
    /// WPF 插件版（ClassIsland 1.7 独立宿主进程）、独立 Avalonia 版（FemboyTest_publish / 安装包）、
    /// SecRandom 插件版、三个原生 exe（Win32 / win64 / win_old）。
    /// - 事件名内嵌"真身双因子"签名，冒充程序无法预知事件名，无法伪造；
    /// - 命名事件是内核对象，创建它的进程退出后自动销毁，OpenExisting 随即失败，检测随之失效。
    /// </summary>
    public static bool IsFemboyTestRunningProcess()
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(FemboyTestRunningEventName);
            return signal != null;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    // 反射扫描结果缓存：以已加载程序集的名称集合为指纹，程序集集合未变化时直接返回缓存。
    // 避免在点击热路径（OnIconClicked 每次点击都调用）上反复执行昂贵的 GetTypes 反射扫描，
    // 否则快速连点会因 UI 卡顿而丢失点击，导致触发彩蛋需要比 11 次更多的点击。
    private static string? _identifierScanFingerprint;
    private static bool _identifierScanResult;

    /// <summary>
    /// 通过反射扫描已加载程序集中带 [PluginEntrance] 特性的插件入口类型，执行"真身双因子"校验：
    /// 要求同一类型上同时存在硬编码常量 <c>UniqueIdentifier == "FemboyTest"</c> 且
    /// <c>IdentifierSignature == "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F"</c>。
    /// 双因子均匹配才判定为真正的 FemboyTest：
    /// - 真插件更换名称（程序集名/清单 ID 变化）后两个常量不变，仍可被识别；
    /// - 冒充插件即使起名为 "FemboyTest"，因缺少签名常量也会被拒绝。
    /// 结果按程序集名称集合缓存：插件加载/卸载会改变集合从而触发重新扫描。
    /// </summary>
    public static bool IsFemboyTestPresentByIdentifier()
    {
        try
        {
            // 计算当前已加载程序集的名称集合指纹（不含动态程序集）
            var names = new System.Collections.Generic.List<string>();
            foreach (var context in AssemblyLoadContext.All)
            {
                foreach (var asm in context.Assemblies)
                {
                    if (asm.IsDynamic) continue;
                    try { names.Add(asm.GetName().Name ?? ""); }
                    catch { }
                }
            }
            names.Sort(StringComparer.Ordinal);
            var fingerprint = string.Join("|", names);

            // 程序集集合未变化：直接返回上次扫描结果
            if (fingerprint == _identifierScanFingerprint)
                return _identifierScanResult;

            // 集合已变化：执行完整反射扫描并缓存结果
            var found = FindFemboyTestEntranceType() != null;

            _identifierScanFingerprint = fingerprint;
            _identifierScanResult = found;
            return found;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 在已加载程序集中扫描 FemboyTest 的插件入口类型（真身双因子校验：标识符与签名常量需同时匹配）。
    /// 找到返回该类型，否则返回 null。供 <see cref="IsFemboyTestPresentByIdentifier"/> 与
    /// <see cref="TryGetFemboyTestPluginInfo"/> 共用，保证两条路径判定完全一致。
    /// </summary>
    private static Type? FindFemboyTestEntranceType()
    {
        foreach (var context in AssemblyLoadContext.All)
        {
            foreach (var asm in context.Assemblies)
            {
                if (asm.IsDynamic)
                    continue;
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        // 只检查插件入口类型
                        if (type.GetCustomAttribute<PluginEntrance>() == null)
                            continue;
                        // 真身双因子：标识符与签名两个常量必须同时匹配
                        var idField = type.GetField("UniqueIdentifier",
                            BindingFlags.Public | BindingFlags.Static);
                        var sigField = type.GetField("IdentifierSignature",
                            BindingFlags.Public | BindingFlags.Static);
                        if (idField != null && idField.IsLiteral &&
                            idField.GetValue(null) is string id &&
                            string.Equals(id, FemboyTestIdentifier, StringComparison.Ordinal) &&
                            sigField != null && sigField.IsLiteral &&
                            sigField.GetValue(null) is string sig &&
                            string.Equals(sig, FemboyTestIdentifierSignature, StringComparison.Ordinal))
                        {
                            return type;
                        }
                    }
                }
                catch
                {
                    // GetTypes 可能因引用缺失等抛异常，跳过该程序集
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 解析 FemboyTest（同进程 CI2 插件版）对应的宿主 <see cref="PluginInfo"/>。
    /// 复用与 <see cref="IsFemboyTestEnabled"/> 路径一完全一致的真身双因子扫描，
    /// 再经 AssemblyLoadContext → PluginLoadContext.Info 映射——与宿主
    /// DiagnosticService.GetPluginsByStacktrace 的归因方式同源，因此可用于
    /// 让宿主按“错误由该插件引起”处理（即禁用该插件）。
    /// 仅当 FemboyTest 以插件形式加载在本进程内时可解析；独立进程版/原生 exe 返回 null。
    /// </summary>
    public static PluginInfo? TryGetFemboyTestPluginInfo()
    {
        try
        {
            var entranceType = FindFemboyTestEntranceType();
            if (entranceType == null)
                return null;

            var context = AssemblyLoadContext.GetLoadContext(entranceType.Assembly);
            if (context == null)
                return null;

            // PluginLoadContext.Info 为宿主类型成员，用反射读取以保持对宿主程序集零编译期依赖
            var infoProperty = context.GetType()
                .GetProperty("Info", BindingFlags.Public | BindingFlags.Instance);
            return infoProperty?.GetValue(context) as PluginInfo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 检测 FemboyTest 插件是否"已安装且已启用"（唯一标识符文件 + .disabled 标记）。
    /// FemboyTest 在初始化时会在其配置目录写入 unique_identifier.txt
    /// （两行：UniqueIdentifier + IdentifierSignature），
    /// 本方法通过读取该文件并做双因子校验识别 FemboyTest 的真实存在。
    /// 它是 <see cref="IsFemboyTestEnabled"/> 三条路径里唯一能覆盖"跨进程 CI2 插件版"的一条：
    /// 反射扫描只看本进程、命名事件只看发布了信号的变种，都认不出跑在另一个进程里的 CI2 插件。
    /// 遍历插件配置根目录下的全部插件目录查找：FemboyTest 各 CI2 版本共用同一标识符文件，
    /// 但配置目录名随发行版本而异（FemboyTest / FemboyTestNet10 / FemboyTest_SecRandom），不能写死目录名。
    /// 注意：ClassIsland 禁用插件只写入 .disabled 标记、不删除该文件，
    /// 因此必须同时校验 .disabled 标记，避免 FemboyTest 禁用后仍因文件残留而误判为启用。
    /// </summary>
    public static bool IsFemboyTestIdentifierPresent()
    {
        try
        {
            var pluginsRoot = Path.GetDirectoryName(Plugin.Instance.PluginConfigFolder);
            if (string.IsNullOrEmpty(pluginsRoot) || !Directory.Exists(pluginsRoot))
                return false;

            foreach (var pluginDir in Directory.GetDirectories(pluginsRoot))
            {
                try
                {
                    var identifierPath = Path.Combine(pluginDir, "unique_identifier.txt");
                    if (!File.Exists(identifierPath))
                        continue;

                    // 插件已被禁用（.disabled 标记存在）：文件残留不代表插件在运行，跳过该目录
                    if (File.Exists(Path.Combine(pluginDir, ".disabled")))
                        continue;

                    // 双因子校验：第一行标识符 + 第二行签名，两者都匹配才算真身
                    var lines = File.ReadAllLines(identifierPath);
                    if (lines.Length >= 2 &&
                        lines[0].Trim() == FemboyTestIdentifier &&
                        lines[1].Trim() == FemboyTestIdentifierSignature)
                        return true;
                }
                catch
                {
                    // 单个插件目录读取失败（权限/占用等）不影响其余目录的检测
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// FemboyTest 与女装彩蛋互斥。
    /// 当 FemboyTest 已启用时，将彩蛋状态重置为未触发。
    /// </summary>
    /// <returns>是否执行了重置</returns>
    public static bool ResetEasterEggIfFemboyTestEnabled(PluginSettings settings)
    {
        if (!IsFemboyTestEnabled())
            return false;
        if (!settings.EnableEasterEgg)
            return false;

        settings.EnableEasterEgg = false;
        EasterEggForceReset?.Invoke();
        return true;
    }

    /// <summary>
    /// 安排一次彩蛋互斥检测，在指定延迟后执行一次。
    /// 用于插件初始化后延迟检测：此时所有插件均已加载完成，
    /// 对 FemboyTest 是否在运行的判断准确可靠。
    /// </summary>
    public static void ScheduleEasterEggMutexCheck(PluginSettings settings, TimeSpan delay)
    {
        var timer = new DispatcherTimer
        {
            Interval = delay
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            // 彩蛋未触发时无需检查，快速跳过
            if (!settings.EnableEasterEgg)
                return;
            ResetEasterEggIfFemboyTestEnabled(settings);
        };
        timer.Start();
    }
}
