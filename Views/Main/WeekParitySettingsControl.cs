using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Views.Controls;
using Avalonia;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class WeekParitySettingsControl : ComponentBase<WeekParitySettings>
{
    private WeekTextSettingsPanel? _panel;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _panel = new WeekTextSettingsPanel(Settings, "本周单双设置",
            "以ClassIsland学期开始日为第一周，显示本周是单周还是双周。可自定义时间基准、字体颜色、大小、样式与字重。");
        Content = _panel.Root;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _panel?.Detach();
        base.OnDetachedFromVisualTree(e);
    }
}
