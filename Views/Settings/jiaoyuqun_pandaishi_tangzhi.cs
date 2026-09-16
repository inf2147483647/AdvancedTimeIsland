using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunPandaishiTangzhi", "交窬裙 襻带式 唐制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunPandaishiTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("jiaoyuqun_pandaishi_tangzhi.md"));
    }
}