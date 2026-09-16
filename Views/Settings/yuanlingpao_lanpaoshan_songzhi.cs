using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandYuanlingpaoLanpaoshanSongzhi", "圆领袍 襕袍衫 宋制（男）", true, SettingsPageCategory.Debug)]
public class YuanlingpaoLanpaoshanSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("yuanlingpao_lanpaoshan_songzhi.md"));
    }
}