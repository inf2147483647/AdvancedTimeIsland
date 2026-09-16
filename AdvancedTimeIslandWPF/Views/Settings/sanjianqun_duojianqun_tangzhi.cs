using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandSanjianqunDuojianqunTangzhi", "三裥裙 多裥裙 唐制", true, SettingsPageCategory.Debug)]
public class SanjianqunDuojianqunTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("三裥裙_多裥裙_唐制.md"));
    }
}