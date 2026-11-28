using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunLongqunTangzhi", "交窬裙 笼裙 唐制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunLongqunTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("交窬裙_笼裙_唐制.md"));
    }
}