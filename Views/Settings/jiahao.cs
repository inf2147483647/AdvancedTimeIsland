using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaHao", "嘉豪", true, SettingsPageCategory.Debug)]
public class JiaHao : HanfuPageTemplate  
{
    protected override void BuildContent(StackPanel panel)
    {
        var markdown = @"
这个问题很复杂，无法解析。
这是一个相当可悲的故事，互联网恐怕就是这样的
        ";

        RenderMarkdown(panel, markdown);
    }
}