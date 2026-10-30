using System;
using System.IO;
using System.Reflection;
using Avalonia.Media.Imaging;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 插件根图标（manifest 引用的 icon.png）加载，供「关于」页与「女装」注意事项覆盖层共用。
/// 包内不保留重复副本：优先读 <see cref="AppContext.BaseDirectory"/> 下的 icon.png，
/// 读不到再回退到插件程序集所在目录。
/// </summary>
public static class PluginIconHelper
{
    public static Bitmap? LoadIconBitmap()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var fullPath = Path.Combine(baseDir, "icon.png");

            if (File.Exists(fullPath))
            {
                return new Bitmap(fullPath);
            }

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var pluginDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(pluginDir))
            {
                fullPath = Path.Combine(pluginDir, "icon.png");
                if (File.Exists(fullPath))
                {
                    return new Bitmap(fullPath);
                }
            }
        }
        catch
        {
            // 图标缺失或读取失败时由调用方显示空白，不影响页面其余内容
        }

        return null;
    }
}
