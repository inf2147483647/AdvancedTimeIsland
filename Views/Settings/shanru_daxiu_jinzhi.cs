using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanruDaxiuJinzhi", "衫襦 大袖 晋制", true, SettingsPageCategory.Debug)]
public class ShanruDaxiuJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("shanru_daxiu_jinzhi.md"));
    }
}