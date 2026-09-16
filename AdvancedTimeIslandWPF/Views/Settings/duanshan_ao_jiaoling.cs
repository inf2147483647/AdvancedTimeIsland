using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanShanAoJiaoLing", "短衫 袄 交领 明制", true, SettingsPageCategory.Debug)]
public class DuanShanAoJiaoLingPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("短衫_袄_交领_明制.md"));
    }
}