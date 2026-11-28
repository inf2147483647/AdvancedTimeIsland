using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangruDanyiXianqinzhi", "长襦 单衣 先秦制", true, SettingsPageCategory.Debug)]
public class ChangruDanyiXianqinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长襦_单衣_先秦制.md"));
    }
}