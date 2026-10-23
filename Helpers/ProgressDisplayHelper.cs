using AdvancedTimeIsland.Models;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 进度显示方式的语义助手。
/// <para>
/// 设置界面统一用「进度条」「进度环」两个复选框替代原先的四选一下拉框：
/// 两个都不勾 = 不显示进度，勾一个 = 只显示该种，两个都勾 = 同时显示。
/// </para>
/// <para>
/// 持久化仍沿用 <see cref="ProgressDisplayMode"/> 枚举（None / Bar / Ring / Both）——
/// 两种复选框的 4 种组合与枚举取值一一对应，因此组件渲染逻辑与旧配置文件都无需迁移。
/// </para>
/// </summary>
public static class ProgressDisplayHelper
{
    /// <summary>当前设置是否需要显示进度条。</summary>
    public static bool ShowsBar(ProgressDisplayMode mode) =>
        mode is ProgressDisplayMode.Bar or ProgressDisplayMode.Both;

    /// <summary>当前设置是否需要显示进度环。</summary>
    public static bool ShowsRing(ProgressDisplayMode mode) =>
        mode is ProgressDisplayMode.Ring or ProgressDisplayMode.Both;

    /// <summary>把两个复选框的勾选状态合成为持久化用的枚举值，两者都未勾选即不显示进度。</summary>
    public static ProgressDisplayMode Compose(bool showBar, bool showRing) => (showBar, showRing) switch
    {
        (true, true) => ProgressDisplayMode.Both,
        (true, false) => ProgressDisplayMode.Bar,
        (false, true) => ProgressDisplayMode.Ring,
        _ => ProgressDisplayMode.None
    };
}
