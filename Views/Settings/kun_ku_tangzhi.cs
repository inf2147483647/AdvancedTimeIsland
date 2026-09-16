using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandKunKuTangzhi", "裈 袴 唐制", true, SettingsPageCategory.Debug)]
public class KunKuTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("裈_袴_唐制.md"));
    }
}