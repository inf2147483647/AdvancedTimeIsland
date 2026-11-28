using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandAoShanYuanlingSongzhi", "袄 衫 圆领 宋制", true, SettingsPageCategory.Debug)]
public class AoShanYuanlingSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("袄_衫_圆领_宋制.md"));
    }
}