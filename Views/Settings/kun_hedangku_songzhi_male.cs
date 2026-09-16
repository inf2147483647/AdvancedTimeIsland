using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandKunHedangkuSongzhiMale", "裈 合裆裤 宋制（男）", true, SettingsPageCategory.Debug)]
public class KunHedangkuSongzhiMalePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("裈_合裆裤_宋制_男.md"));
    }
}