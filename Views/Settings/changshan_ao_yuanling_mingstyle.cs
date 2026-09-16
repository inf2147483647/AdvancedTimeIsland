using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangshanAoYuanlingMingstyle", "长衫 袄 圆领 明制", true, SettingsPageCategory.Debug)]
public class ChangshanAoYuanlingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("changshan_ao_yuanling_mingstyle.md"));
    }
}