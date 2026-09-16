using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanruChuihuxiuJinzhi", "衫襦 垂胡袖 晋制", true, SettingsPageCategory.Debug)]
public class ShanruChuihuxiuJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("衫襦_垂胡袖_晋制.md"));
    }
}