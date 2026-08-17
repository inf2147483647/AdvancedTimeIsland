using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows.Threading;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 跨插件联动辅助类
/// </summary>
public static class CrossPluginHelper
{
    private const string FemboyTestIdentifier = "FemboyTest";
    private const string FemboyTestIdentifierSignature = "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F";

    /// <summary>
    /// WPF 1.7 版 FemboyTest 的跨进程"正在运行"命名事件名（内嵌真身签名，冒充插件无法预知）。
    /// 需与 FemboyTestWpf 插件 Plugin.RunningEventName 保持一致。
    /// </summary>
    private const string FemboyTestWpfRunningEventName = "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F-Running";

    /// <summary>
    /// 彩蛋被强制重置（FemboyTest 与女装彩蛋互斥）时触发
    /// </summary>
    public static event Action? EasterEggForceReset;

    /// <summary>
    /// 检测 FemboyTest 插件是否实际在运行（覆盖不同版本的 FemboyTest）。
    /// 互斥的依据是"FemboyTest 是否在运行"，而非"是否被标记为禁用"：
    /// 插件市场禁用只写入 .disabled 文件，在重启生效之前其程序集仍驻留内存、功能仍在运行，此时必须保持互斥。
    /// 本检测与插件加载顺序无关，无需修改 ClassIsland 或声明 manifest 依赖：
    /// ClassIsland 在插件加载阶段之前会先扫描所有插件目录并把它们（含禁用的）加入
    /// IPluginService.LoadedPlugins，因此本插件 Initialize 时 IsEnabled 检测立即可用；
    /// 互斥监视器再对运行时状态做周期兜底。
    /// 检测路径：
    /// 1. 真身双因子反射扫描（见 <see cref="IsFemboyTestPresentByIdentifier"/>）——
    ///    覆盖与本插件同进程的 CI2 版 FemboyTest：真插件更换名称仍可识别，冒充同名插件被拒绝；
    /// 2. 跨进程命名事件检测（见 <see cref="IsFemboyTestWpfRunning"/>）——
    ///    覆盖运行在独立宿主进程的 WPF 1.7 版 FemboyTest，事件名内嵌真身签名、冒充者无法伪造。
    /// 原"程序集名精确匹配"与"清单 ID 匹配"的名称判定可被同名冒充，已不再单独作为互斥依据；
    /// "禁用后重启前程序集仍驻留内存"的场景由签名扫描覆盖：该程序集仍在
    /// AssemblyLoadContext 中，反射扫描依然可见，互斥在重启前持续生效。
    /// </summary>
    public static bool IsFemboyTestEnabled()
    {
        // 检测路径一：双因子签名反射扫描（同进程 CI2 版）
        if (IsFemboyTestPresentByIdentifier())
            return true;

        // 检测路径二：跨进程命名事件检测（WPF 1.7 版，独立进程）
        if (IsFemboyTestWpfRunning())
            return true;

        return false;
    }

    /// <summary>
    /// 跨进程检测 WPF 1.7 版 FemboyTest 插件是否在运行。
    /// WPF 1.7 版运行在独立的宿主进程（与 CI2 不同进程），无法被本进程内的反射扫描覆盖，
    /// 因此由 WPF 版在插件初始化时创建命名事件 <see cref="FemboyTestWpfRunningEventName"/> 作为运行信号：
    /// - 事件名内嵌"真身双因子"签名，冒充插件无法预知事件名，无法伪造；
    /// - 命名事件是内核对象，WPF 版宿主进程退出后自动销毁，OpenExisting 随即失败，检测随之失效。
    /// </summary>
    public static bool IsFemboyTestWpfRunning()
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(FemboyTestWpfRunningEventName);
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
            var found = false;
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
                                found = true;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // GetTypes 可能因引用缺失等抛异常，跳过该程序集
                    }
                    if (found) break;
                }
                if (found) break;
            }

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
    /// 检测 FemboyTest 插件是否通过唯一标识符文件被识别。
    /// FemboyTest 在初始化时会在其配置目录写入 unique_identifier.txt
    /// （两行：UniqueIdentifier + IdentifierSignature），
    /// 本方法通过读取该文件并做双因子校验识别 FemboyTest 的真实存在，与反射扫描互为补充。
    /// 注意：ClassIsland 禁用插件只写入 .disabled 标记、不删除该文件，
    /// 因此必须同时校验 .disabled 标记，避免 FemboyTest 禁用后仍因文件残留而误判为启用。
    /// </summary>
    public static bool IsFemboyTestIdentifierPresent()
    {
        try
        {
            var pluginsRoot = Path.GetDirectoryName(Plugin.Instance.PluginConfigFolder);
            if (string.IsNullOrEmpty(pluginsRoot))
                return false;

            var femboyTestDir = Path.Combine(pluginsRoot, "FemboyTest");
            var identifierPath = Path.Combine(femboyTestDir, "unique_identifier.txt");
            if (!File.Exists(identifierPath))
                return false;

            // 插件已被禁用（.disabled 标记存在）：文件残留不代表插件在运行，返回 false
            if (File.Exists(Path.Combine(femboyTestDir, ".disabled")))
                return false;

            // 双因子校验：第一行标识符 + 第二行签名，两者都匹配才算真身
            var lines = File.ReadAllLines(identifierPath);
            return lines.Length >= 2 &&
                   lines[0].Trim() == FemboyTestIdentifier &&
                   lines[1].Trim() == FemboyTestIdentifierSignature;
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
