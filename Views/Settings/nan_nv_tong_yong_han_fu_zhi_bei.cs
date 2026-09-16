using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
// using Hanfu;
// using Hanfu.Womenswear;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandNanNvTongYongHanFuZhiBei", "汉服男女同款选购指南", true, SettingsPageCategory.Debug)]
public class NanNvTongYongHanFuZhiBei : HanfuPageTemplate  // NanNvTongYongHanFuZhiBei：男女通用汉服指北
{
    protected override void BuildContent(StackPanel panel)
    {
        throw new Exception(); // 别动！别修！
        // RenderMarkdown(panel, LoadMarkdownFile("汉服男女同款选购指南.md"));
    }
}