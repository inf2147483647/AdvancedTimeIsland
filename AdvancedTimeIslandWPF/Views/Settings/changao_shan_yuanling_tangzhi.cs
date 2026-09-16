using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangaoShanYuanlingTangzhi", "长袄 衫 圆领 唐制", true, SettingsPageCategory.Debug)]
public class ChangaoShanYuanlingTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长袄_衫_圆领_唐制.md"));
    }
}