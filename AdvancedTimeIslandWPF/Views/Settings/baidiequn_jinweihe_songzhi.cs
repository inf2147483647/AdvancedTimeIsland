using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandBaidiequnJinweiheSongzhi", "百迭裙 仅围合 宋制", true, SettingsPageCategory.Debug)]
public class BaidiequnJinweiheSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("百迭裙_仅围合_宋制.md"));
    }
}