using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunShoushengkuanTangzhi", "交窬裙 收省款 唐制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunShoushengkuanTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("jiaoyuqun_shoushengkuan_tangzhi.md"));
    }
}