using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHexiuMoxiuSongzhiMale", "鹤袖 貉袖 宋制（男）", true, SettingsPageCategory.Debug)]
public class HexiuMoxiuSongzhiMalePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("鹤袖_貉袖_宋制_男.md"));
    }
}