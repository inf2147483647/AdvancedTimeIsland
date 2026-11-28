using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunQunchenqunTangzhi", "交窬裙 裙衬裙 唐制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunQunchenqunTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("交窬裙_裙衬裙_唐制.md"));
    }
}