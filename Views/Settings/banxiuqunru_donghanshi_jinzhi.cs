using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandBanxiuqunruDonghanshiJinzhi", "半袖裙襦 东汉式 晋制", true, SettingsPageCategory.Debug)]
public class BanxiuqunruDonghanshiJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("半袖裙襦_东汉式_晋制.md"));
    }
}