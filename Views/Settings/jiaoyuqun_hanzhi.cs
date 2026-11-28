using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunHanzhi", "交窬裙 汉制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunHanzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("交窬裙_汉制.md"));
    }
}