using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandLanpaoLanshanTangzhi", "襕袍 襕衫 唐制", true, SettingsPageCategory.Debug)]
public class LanpaoLanshanTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("襕袍_襕衫_唐制.md"));
    }
}