using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanaoShanJiaolingSongzhi", "短袄 衫 交领 宋制", true, SettingsPageCategory.Debug)]
public class DuanaoShanJiaolingSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("duanao_shan_jiaoling_songzhi.md"));
    }
}