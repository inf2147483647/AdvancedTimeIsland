using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandYaodaiTangzhi", "腰带 唐制", true, SettingsPageCategory.Debug)]
public class YaodaiTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("腰带_唐制.md"));
    }
}