using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandYuanlingpaoKuipaoSongzhi", "圆领袍 䙆袍 宋制（男）", true, SettingsPageCategory.Debug)]
public class YuanlingpaoKuipaoSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("yuanlingpao_kuipao_songzhi.md"));
    }
}