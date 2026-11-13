using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 「女装使用须知」页面已随女装彩蛋功能一并删除，此类型仅用于占回该页面 ID，
/// 使旧的导航引用不再落到宿主的「出错啦！」页——导航到该页时直接显示 <see cref="GhostPage"/> 的内容。
/// </summary>
/// <remarks>
/// 与 <see cref="WomenswearPage"/> 一致：ClassIsland 注册设置页时按 <c>GetCustomAttributes(inherit: false)</c>
/// 读取特性，故这里必须重新声明 <see cref="SettingsPageInfo"/> 与 <see cref="HidePageTitleAttribute"/>。
/// </remarks>
[HidePageTitle]
[SettingsPageInfo("AdvancedTimeIslandWomenswearNotice", "女装使用须知", true, SettingsPageCategory.Debug)]
public class WomenswearNoticePage : GhostPage;
