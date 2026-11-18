using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Shared;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

/// <summary>
/// 「日出时」「日落时」共用的设置界面：展示今日日出 / 日落时间，并说明时间来源。
/// </summary>
public class SunEventTriggerSettingsControl : TriggerSettingsControlBase<SunEventTriggerSettings>
{
    private readonly TextBlock _sunriseText;
    private readonly TextBlock _sunsetText;
    private readonly TextBlock _sourceText;

    public SunEventTriggerSettingsControl()
    {
        _sunriseText = new TextBlock();
        _sunsetText = new TextBlock();
        _sourceText = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        panel.Children.Add(new TextBlock
        {
            Text = "在每天日出 / 日落时触发。",
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        });

        panel.Children.Add(new TextBlock
        {
            Text = "时间跟随主界面「日出日落」组件中的经纬度与时区设置。",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(_sunriseText);
        panel.Children.Add(_sunsetText);
        panel.Children.Add(_sourceText);

        Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        Loaded += (_, _) => RefreshTimes();
    }

    private void RefreshTimes()
    {
        var now = GlobalConstants.HostInterfaces.ExactTimeService?.GetCurrentLocalDateTime() ?? DateTime.Now;
        var times = SunriseSunsetProvider.Calculate(now);

        _sunriseText.Text = $"今日日出：{FormatTime(times.Sunrise)}";
        _sunsetText.Text = $"今日日落：{FormatTime(times.Sunset)}";
        _sourceText.Text = SunriseSunsetProvider.HasMainWindowComponent
            ? "已同步主界面「日出日落」组件的设置。"
            : "未找到主界面「日出日落」组件，当前使用默认经纬度。";
    }

    private static string FormatTime(TimeSpan time)
    {
        return time > TimeSpan.Zero && time < TimeSpan.FromDays(1)
            ? time.ToString(@"hh\:mm")
            : "--:--";
    }
}
