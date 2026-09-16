using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangshanAoFanglingMingstyle", "长衫 袄 方领 明制", true, SettingsPageCategory.Debug)]
public class ChangshanAoFanglingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("changshan_ao_fangling_mingstyle.md"));
    }
}