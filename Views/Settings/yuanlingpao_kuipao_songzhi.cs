using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandYuanlingpaoKuipaoSongzhi", "圆领袍 䙆袍 宋制", true, SettingsPageCategory.Debug)]
public class YuanlingpaoKuipaoSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("圆领袍_䙆袍_宋制.md"));
    }
}