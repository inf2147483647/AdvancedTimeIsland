using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHanshanAoziJiaolingTangzhi", "汗衫 袄子 交领 唐制", true, SettingsPageCategory.Debug)]
public class HanshanAoziJiaolingTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("汗衫_袄子_交领_唐制.md"));
    }
}