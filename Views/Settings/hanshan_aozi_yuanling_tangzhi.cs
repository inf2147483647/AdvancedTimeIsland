using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHanshanAoziYuanlingTangzhi", "汗衫 袄子 圆领 唐制", true, SettingsPageCategory.Debug)]
public class HanshanAoziYuanlingTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("汗衫_袄子_圆领_唐制.md"));
    }
}