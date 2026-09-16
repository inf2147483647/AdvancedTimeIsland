using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanshanAoZhilingMingstyle", "短衫 袄 直领 明制", true, SettingsPageCategory.Debug)]
public class DuanshanAoZhilingMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("短衫_袄_直领_明制.md"));
    }
}