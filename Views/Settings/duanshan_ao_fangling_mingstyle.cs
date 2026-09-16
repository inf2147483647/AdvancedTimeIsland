using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanshanAoFanglingMingstyle", "短衫 袄 方领 明制", true, SettingsPageCategory.Debug)]
public class DuanshanAoFanglingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("duanshan_ao_fangling_mingstyle.md"));
    }
}