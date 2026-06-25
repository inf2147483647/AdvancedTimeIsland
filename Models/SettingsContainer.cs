namespace AdvancedTimeIsland.Models;

/// <summary>
/// 设置容器，用于序列化所有插件设置
/// </summary>
public class SettingsContainer
{
    public PluginSettings? Settings { get; set; }
    public DebugSettings? DebugSettings { get; set; }
}
