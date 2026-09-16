using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHexiuMoxiuSongzhi", "鹤袖 貉袖 宋制", true, SettingsPageCategory.Debug)]
public class HexiuMoxiuSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("鹤袖_貉袖_宋制.md"));
    }
}