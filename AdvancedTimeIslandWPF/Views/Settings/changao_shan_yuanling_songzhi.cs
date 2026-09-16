using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangaoShanYuanlingSongzhi", "长袄 衫 圆领 宋制", true, SettingsPageCategory.Debug)]
public class ChangaoShanYuanlingSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长袄_衫_圆领_宋制.md"));
    }
}