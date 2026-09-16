using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandJiaoyuqunWuyuanqunJinzhi", "交窬裙 无缘裙 晋制", true, SettingsPageCategory.Debug)]
public class JiaoyuqunWuyuanqunJinzhiPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("交窬裙_无缘裙_晋制.md"));
    }
}