using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandBaidiequnJinweiheSongzhi", "百迭裙 仅围合 宋制", true, SettingsPageCategory.Debug)]
public class BaidiequnJinweiheSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("baidiequn_jinweihe_songzhi.md"));
    }
}