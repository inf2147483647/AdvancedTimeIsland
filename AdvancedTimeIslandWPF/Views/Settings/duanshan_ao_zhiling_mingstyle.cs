using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanshanAoZhilingMingstyle", "短衫 袄 直领 明制", true, SettingsPageCategory.Debug)]
public class DuanshanAoZhilingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("duanshan_ao_zhiling_mingstyle.md"));
    }
}