using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDaxiuyiHengpeiXiapeiSongzhi", "大袖衣 横帔 霞帔 宋制", true, SettingsPageCategory.Debug)]
public class DaxiuyiHengpeiXiapeiSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("大袖衣_横帔_霞帔_宋制.md"));
    }
}