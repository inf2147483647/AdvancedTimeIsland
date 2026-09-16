using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangaoShanJiaolingSongzhi", "长袄 衫 交领 宋制（男）", true, SettingsPageCategory.Debug)]
public class ChangaoShanJiaolingSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长袄_衫_交领_宋制.md"));
    }
}