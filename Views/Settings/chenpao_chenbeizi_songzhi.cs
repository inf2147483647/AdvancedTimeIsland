using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChenpaoChenbeiziSongzhi", "衬袍 衬褙子 宋制", true, SettingsPageCategory.Debug)]
public class ChenpaoChenbeiziSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("衬袍_衬褙子_宋制.md"));
    }
}