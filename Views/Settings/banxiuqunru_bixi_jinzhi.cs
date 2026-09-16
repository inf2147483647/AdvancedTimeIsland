using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandBanxiuqunruBixiJinzhi", "半袖裙襦 蔽膝 晋制", true, SettingsPageCategory.Debug)]
public class BanxiuqunruBixiJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("半袖裙襦_蔽膝_晋制.md"));
    }
}