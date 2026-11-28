using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDaofuSongzhi", "道服 宋制", true, SettingsPageCategory.Debug)]
public class DaofuSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("道服_宋制.md"));
    }
}