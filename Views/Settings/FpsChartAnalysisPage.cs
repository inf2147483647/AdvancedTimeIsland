using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 「帧率折线图分析」功能已删除，此类型仅用于占回原页面 ID，
/// 使旧的导航引用与书签不再落到宿主的「出错啦！」页——导航到该页时直接显示 <see cref="GhostPage"/> 的内容。
/// </summary>
/// <remarks>
/// ClassIsland 注册设置页时按 <c>GetCustomAttributes(inherit: false)</c> 读取特性，
/// 故这里必须重新声明 <see cref="SettingsPageInfo"/> 与 <see cref="HidePageTitleAttribute"/>。
/// </remarks>
[HidePageTitle]
[SettingsPageInfo("AdvancedTimeIslandFpsChartAnalysis", "帧率折线图分析", SettingsPageCategory.Debug)]
public class FpsChartAnalysisPage : GhostPage;
