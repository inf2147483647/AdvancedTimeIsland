using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDaxiushanXiapeiMingstyle", "大袖衫 霞帔 明制", true, SettingsPageCategory.Debug)]
public class DaxiushanXiapeiMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("大袖衫_霞帔_明制.md"));
    }
}