using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHexiuMoxiuMingstyle", "鹤袖 貉袖 明制", true, SettingsPageCategory.Debug)]
public class HexiuMoxiuMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("hexiu_moxiu_mingstyle.md"));
    }
}