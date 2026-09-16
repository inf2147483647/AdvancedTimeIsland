using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangshanAoZhilingMingstyle", "长衫 袄 直领 明制", true, SettingsPageCategory.Debug)]
public class ChangshanAoZhilingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("changshan_ao_zhiling_mingstyle.md"));
    }
}