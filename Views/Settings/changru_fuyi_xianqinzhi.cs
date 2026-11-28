using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangruFuyiXianqinzhi", "长襦 复衣 先秦制", true, SettingsPageCategory.Debug)]
public class ChangruFuyiXianqinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长襦_复衣_先秦制.md"));
    }
}