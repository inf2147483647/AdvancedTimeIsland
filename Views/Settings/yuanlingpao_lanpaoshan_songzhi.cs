using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandYuanlingpaoLanpaoshanSongzhi", "圆领袍 襕袍衫 宋制", true, SettingsPageCategory.Debug)]
public class YuanlingpaoLanpaoshanSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("圆领袍_襕袍衫_宋制.md"));
    }
}