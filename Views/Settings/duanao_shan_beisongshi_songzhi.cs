using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanaoShanBeisongshiSongzhi", "短袄 衫 北宋式 宋制", true, SettingsPageCategory.Debug)]
public class DuanaoShanBeisongshiSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("duanao_shan_beisongshi_songzhi.md"));
    }
}