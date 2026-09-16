using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandKuKaidangkuSongzhiMale", "袴 开裆裤 宋制（男）", true, SettingsPageCategory.Debug)]
public class KuKaidangkuSongzhiMalePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("袴_开裆裤_宋制_男.md"));
    }
}