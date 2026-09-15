using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangShanAoJiaoLing", "长衫 袄 交领 明制", true, SettingsPageCategory.Debug)]
public class ChangShanAoJiaoLingPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("changshan_ao_jiaoling.md"));
    }
}
