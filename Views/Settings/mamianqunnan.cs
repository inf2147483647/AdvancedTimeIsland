using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
// using Hanfu;
// using Hanfu.Womenswear;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandMaMianQunMale", "马面裙男", true, SettingsPageCategory.Debug)]
public class MaMianQunMale : HanfuPageTemplate  // MaMianQunMale：
{
    protected override void BuildContent(StackPanel panel)
    {
        RenderMarkdown(panel, LoadMarkdownFile("mamianqunnan.md"));
    }
}