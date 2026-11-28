using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangxiDaxiJinzhi", "长褶 大褶 晋制", true, SettingsPageCategory.Debug)]
public class ChangxiDaxiJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长褶_大褶_晋制.md"));
    }
}