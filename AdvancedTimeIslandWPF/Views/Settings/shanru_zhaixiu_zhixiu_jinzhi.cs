using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanruZhaixiuZhixiuJinzhi", "衫襦 窄袖 直袖 晋制", true, SettingsPageCategory.Debug)]
public class ShanruZhaixiuZhixiuJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("shanru_zhaixiu_zhixiu_jinzhi.md"));
    }
}