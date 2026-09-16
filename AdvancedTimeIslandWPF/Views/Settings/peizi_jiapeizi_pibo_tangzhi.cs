using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandPeiziJiapeiziPiboTangzhi", "帔子 夹帔子 披帛 唐制", true, SettingsPageCategory.Debug)]
public class PeiziJiapeiziPiboTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("peizi_jiapeizi_pibo_tangzhi.md"));
    }
}