using System;
using AdvancedTimeIsland.Models;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 跨插件联动辅助类
/// </summary>
public static class CrossPluginHelper
{
    private const string FemboyTestPluginId = "FemboyTest";
    private const int MutexMonitorIntervalMs = 1500;

    private static DispatcherTimer? _mutexMonitorTimer;

    /// <summary>
    /// 彩蛋被强制重置（FemboyTest 与女装彩蛋互斥）时触发
    /// </summary>
    public static event Action? EasterEggForceReset;

    /// <summary>
    /// 检测 FemboyTest 插件是否已启用。
    /// 通过 ClassIsland 插件服务校验启用状态（最权威）：
    /// 插件扫描阶段会把所有本地插件（含已禁用的）加入列表，IsEnabled 基于 .disabled 文件实时判断。
    /// 注意：
    /// - 不能使用"插件配置目录是否存在"来判断——目录在插件曾启用时创建，禁用后仍会残留，会误判。
    /// - 不能使用"程序集是否已加载"来判断——禁用插件后若不重启进程，其程序集仍驻留内存，会误判。
    /// </summary>
    public static bool IsFemboyTestEnabled()
    {
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
    /// 启动彩蛋互斥监视器。
    /// 周期性检查 FemboyTest 与彩蛋的互斥关系，确保即使两个插件的加载顺序不同，也能保持完全互斥。
    /// </summary>
    public static void StartEasterEggMutexMonitor(PluginSettings settings)
    {
        if (_mutexMonitorTimer != null)
            return;

        _mutexMonitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(MutexMonitorIntervalMs)
        };
        _mutexMonitorTimer.Tick += (_, _) =>
        {
            // 彩蛋未触发时无需检查，快速跳过
            if (!settings.EnableEasterEgg)
                return;
            ResetEasterEggIfFemboyTestEnabled(settings);
        };
        _mutexMonitorTimer.Start();
    }

    /// <summary>
    /// 停止彩蛋互斥监视器
    /// </summary>
    public static void StopEasterEggMutexMonitor()
    {
        _mutexMonitorTimer?.Stop();
        _mutexMonitorTimer = null;
    }
}
