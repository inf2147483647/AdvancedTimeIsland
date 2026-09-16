using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHexiuMoxiuSongzhi", "鹤袖 貉袖 宋制", true, SettingsPageCategory.Debug)]
public class HexiuMoxiuSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("hexiu_moxiu_songzhi.md"));
    }
}