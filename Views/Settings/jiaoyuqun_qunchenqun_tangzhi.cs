using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunQunchenqunTangzhi", "交窬裙 裙衬裙 唐制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunQunchenqunTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("jiaoyuqun_qunchenqun_tangzhi.md"));
    }
}