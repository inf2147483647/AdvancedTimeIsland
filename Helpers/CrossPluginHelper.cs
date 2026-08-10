using System;
using System.IO;
using System.Runtime.Loader;
using AdvancedTimeIsland.Models;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 跨插件联动辅助类
/// </summary>
public static class CrossPluginHelper
{
    private const string FemboyTestAssemblyName = "FemboyTest";
    private const string FemboyTestPluginId = "FemboyTest";

    /// <summary>
    /// 彩蛋被强制重置（FemboyTest 与女装彩蛋互斥）时触发
    /// </summary>
    public static event Action? EasterEggForceReset;

    /// <summary>
    /// 检测 FemboyTest 插件是否实际在运行。
    /// 互斥的依据是"FemboyTest 是否在运行"，而非"是否被标记为禁用"：
    /// 插件市场禁用只写入 .disabled 文件，在重启生效之前其程序集仍驻留内存、功能仍在运行，此时必须保持互斥。
    /// 本检测与插件加载顺序无关，无需修改 ClassIsland 或声明 manifest 依赖：
    /// ClassIsland 在插件加载阶段之前会先扫描所有插件目录并把它们（含禁用的）加入
    /// IPluginService.LoadedPlugins，因此本插件 Initialize 时 IsEnabled 检测立即可用；
    /// 互斥监视器再对运行时状态做周期兜底。
    /// 判断顺序：
    /// 1. 程序集是否已加载到进程内（在运行的黄金标准，覆盖禁用后重启前仍驻留内存的情况）；
    /// 2. 插件服务中 IsEnabled 是否为 true（覆盖 FemboyTest 已启用但程序集尚未加载/加载失败的边缘情况）。
    /// </summary>
    public static bool IsFemboyTestEnabled()
    {
        // 方式一：程序集是否已加载到进程内（在运行的黄金标准）
        try
        {
            foreach (var context in AssemblyLoadContext.All)
            {
                foreach (var asm in context.Assemblies)
                {
                    if (asm.GetName().Name == FemboyTestAssemblyName)
                        return true;
                }
            }
        }
        catch
        {
        }

        // 方式二：插件服务中 IsEnabled（覆盖加载顺序与程序集加载失败的边缘情况）
        try
        {
            foreach (var plugin in IPluginService.LoadedPlugins)
            {
                if (plugin.Manifest.Id == FemboyTestPluginId && plugin.IsEnabled)
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    /// <summary>
    /// 检测 FemboyTest 插件是否通过唯一标识符文件被识别。
    /// FemboyTest 在初始化时会在其配置目录写入 unique_identifier.txt（内容为 "FemboyTest"），
    /// 本方法通过读取该文件识别 FemboyTest 的真实存在，与程序集名/清单 ID 匹配互为补充。
    /// </summary>
    public static bool IsFemboyTestIdentifierPresent()
    {
        try
        {
            var pluginsRoot = Path.GetDirectoryName(Plugin.Instance.PluginConfigFolder);
            if (string.IsNullOrEmpty(pluginsRoot))
                return false;

            var identifierPath = Path.Combine(pluginsRoot, "FemboyTest", "unique_identifier.txt");
            if (!File.Exists(identifierPath))
                return false;

            var content = File.ReadAllText(identifierPath).Trim();
            return content == "FemboyTest";
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
