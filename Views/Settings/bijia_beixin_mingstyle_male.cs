using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandBijiaBeixinMingstyleMale", "比甲 背心 明制（男）", true, SettingsPageCategory.Debug)]
public class BijiaBeixinMingstyleMalePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("bijia_beixin_mingstyle_male.md"));
    }
}