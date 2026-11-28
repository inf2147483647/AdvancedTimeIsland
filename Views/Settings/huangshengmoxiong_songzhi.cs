using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHuangshengmoxiongSongzhi", "黄昇抹胸 宋制", true, SettingsPageCategory.Debug)]
public class HuangshengmoxiongSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("黄昇抹胸_宋制.md"));
    }
}