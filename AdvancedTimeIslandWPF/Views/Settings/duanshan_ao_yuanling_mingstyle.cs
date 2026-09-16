using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanshanAoYuanlingMingstyle", "短衫 袄 圆领 明制", true, SettingsPageCategory.Debug)]
public class DuanshanAoYuanlingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("duanshan_ao_yuanling_mingstyle.md"));
    }
}