using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunDuanqunYaoqunTangzhi", "交窬裙 短裙 腰裙 唐制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunDuanqunYaoqunTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("交窬裙_短裙_腰裙_唐制.md"));
    }
}