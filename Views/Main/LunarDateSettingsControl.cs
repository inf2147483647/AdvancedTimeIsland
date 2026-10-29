using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Views.Controls;
using Avalonia;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class LunarDateSettingsControl : ComponentBase<LunarDateSettings>
{
    private WeekTextSettingsPanel? _panel;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _panel = new WeekTextSettingsPanel(Settings, "农历日期设置",
            "在主界面显示今日的农历日期，形如“农历 丙午年正月十五”。可自定义时间基准、字体颜色、大小、样式与字重。");
        Content = _panel.Root;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _panel?.Detach();
        base.OnDetachedFromVisualTree(e);
    }
}
