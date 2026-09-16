using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandShanruQulingJinzhi", "衫襦 曲领 晋制", true, SettingsPageCategory.Debug)]
public class ShanruQulingJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("衫襦_曲领_晋制.md"));
    }
}