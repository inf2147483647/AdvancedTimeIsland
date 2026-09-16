using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDuanaoShanNansongshiSongzhi", "短袄 衫 南宋式 宋制", true, SettingsPageCategory.Debug)]
public class DuanaoShanNansongshiSongzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("duanao_shan_nansongshi_songzhi.md"));
    }
}