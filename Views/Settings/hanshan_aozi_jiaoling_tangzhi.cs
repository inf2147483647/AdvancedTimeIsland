using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHanshanAoziJiaolingTangzhi", "汗衫 袄子 交领 唐制（男）", true, SettingsPageCategory.Debug)]
public class HanshanAoziJiaolingTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("hanshan_aozi_jiaoling_tangzhi.md"));
    }
}