using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanAoJiaolingMingstyle", "衫 袄 交领 明制（男）", true, SettingsPageCategory.Debug)]
public class ShanAoJiaolingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("shan_ao_jiaoling_mingstyle.md"));
    }
}