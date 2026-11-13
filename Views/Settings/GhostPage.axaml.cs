using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 幽灵页面：用于取代那些已被删除的页面。布局对齐 ClassIsland 内置的「出错啦！」页
/// （居中 Lucide 幽灵图标 + 标题 + 正文），不显示设置页顶部标题栏，也不出现在设置导航列表中。
/// </summary>
[HidePageTitle]
[SettingsPageInfo("AdvancedTimeIslandGhost", "Ghost Page", true)]
public partial class GhostPage : SettingsPageBase
{
    public GhostPage()
    {
        InitializeComponent();
    }
}
