using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHexiuMoxiuMingstyle", "鹤袖 貉袖 明制", true, SettingsPageCategory.Debug)]
public class HexiuMoxiuMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("鹤袖_貉袖_明制.md"));
    }
}