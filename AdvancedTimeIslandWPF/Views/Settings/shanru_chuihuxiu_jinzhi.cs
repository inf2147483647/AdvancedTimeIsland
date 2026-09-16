using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanruChuihuxiuJinzhi", "衫襦 垂胡袖 晋制", true, SettingsPageCategory.Debug)]
public class ShanruChuihuxiuJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("shanru_chuihuxiu_jinzhi.md"));
    }
}