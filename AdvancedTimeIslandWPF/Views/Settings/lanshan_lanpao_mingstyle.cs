using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandLanshanLanpaoMingstyle", "襕衫 蓝袍 明制", true, SettingsPageCategory.Debug)]
public class LanshanLanpaoMingstylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("襕衫_蓝袍_明制.md"));
    }
}