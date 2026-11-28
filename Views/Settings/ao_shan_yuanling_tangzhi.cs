using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandAoShanYuanlingTangzhi", "袄 衫 圆领 唐制", true, SettingsPageCategory.Debug)]
public class AoShanYuanlingTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("袄_衫_圆领_唐制.md"));
    }
}