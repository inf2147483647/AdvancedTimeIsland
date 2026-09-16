using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShenyiDadaiFujinMingstyle", "深衣 大带 幅巾 明制", true, SettingsPageCategory.Debug)]
public class ShenyiDadaiFujinMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("shenyi_dadai_fujin_mingstyle.md"));
    }
}