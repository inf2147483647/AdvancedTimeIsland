using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandLanpaoLanshanTangzhi", "襕袍 襕衫 唐制（男）", true, SettingsPageCategory.Debug)]
public class LanpaoLanshanTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("lanpao_lanshan_tangzhi.md"));
    }
}