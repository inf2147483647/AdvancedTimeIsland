using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandChangshanAoYuanlingMingstyle", "长衫 袄 圆领 明制", true, SettingsPageCategory.Debug)]
public class ChangshanAoYuanlingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("长衫_袄_圆领_明制.md"));
    }
}