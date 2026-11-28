using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanAoYuanlingMingstyle", "衫 袄 圆领 明制", true, SettingsPageCategory.Debug)]
public class ShanAoYuanlingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("衫_袄_圆领_明制.md"));
    }
}