using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanAoYuanlingMingstyle", "衫 袄 圆领 明制（男）", true, SettingsPageCategory.Debug)]
public class ShanAoYuanlingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("shan_ao_yuanling_mingstyle.md"));
    }
}