using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandYuanlingpaoShanMingstyleMale", "圆领袍 衫 明制（男）", true, SettingsPageCategory.Debug)]
public class YuanlingpaoShanMingstyleMalePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("圆领袍_衫_明制_男.md"));
    }
}