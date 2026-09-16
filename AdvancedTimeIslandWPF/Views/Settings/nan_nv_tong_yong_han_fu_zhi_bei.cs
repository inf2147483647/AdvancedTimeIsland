using AdvancedTimeIsland.Helpers;
using System.Windows.Controls;
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
        RenderMarkdown(panel, LoadMarkdownFile("nan_nv_tong_yong_han_fu_zhi_bei.md"));
    }
}