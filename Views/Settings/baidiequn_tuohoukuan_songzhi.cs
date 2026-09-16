using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandBaidiequnTuohoukuanSongzhi", "百迭裙 拖后款 宋制", true, SettingsPageCategory.Debug)]
public class BaidiequnTuohoukuanSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("baidiequn_tuohoukuan_songzhi.md"));
    }
}