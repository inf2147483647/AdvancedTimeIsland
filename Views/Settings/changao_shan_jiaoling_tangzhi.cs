using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangaoShanJiaolingTangzhi", "长袄 衫 交领 唐制", true, SettingsPageCategory.Debug)]
public class ChangaoShanJiaolingTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长袄_衫_交领_唐制.md"));
    }
}