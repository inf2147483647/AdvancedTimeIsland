using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 「女装」页面已随女装彩蛋功能一并删除，此类型仅用于占回原页面 ID（<c>AdvancedTimeIslandWomenswear</c>），
/// 使旧的导航引用不再落到宿主的「出错啦！」页——导航到该页时直接显示 <see cref="GhostPage"/> 的内容。
/// </summary>
/// <remarks>
/// ClassIsland 注册设置页时按 <c>GetCustomAttributes(inherit: false)</c> 读取特性，故这里必须重新声明
/// <see cref="SettingsPageInfo"/> 与 <see cref="HidePageTitleAttribute"/>，不会与 <see cref="GhostPage"/> 的 ID 冲突。
/// </remarks>
[HidePageTitle]
[SettingsPageInfo("AdvancedTimeIslandWomenswear", "女装", true, SettingsPageCategory.Debug)]
public class WomenswearPage : GhostPage;
