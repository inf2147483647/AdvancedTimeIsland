using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandYuanlingpaoshanQuekuapaoshanTangzhi", "圆领袍衫 缺胯袍衫 唐制（男）", true, SettingsPageCategory.Debug)]
public class YuanlingpaoshanQuekuapaoshanTangzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("yuanlingpaoshan_quekuapaoshan_tangzhi.md"));
    }
}