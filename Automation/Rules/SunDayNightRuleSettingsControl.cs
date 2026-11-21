using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 「当前处于白昼/黑夜」条件的设置界面，沿用宿主原「是否日出/日落」的切换按钮样式。
/// </summary>
public class SunDayNightRuleSettingsControl : RuleSettingsControlBase<SunDayNightRuleSettings>
{
    private readonly TabStrip _tabStrip;
    private readonly AnimatedIconButton _sunriseButton;
    private readonly AnimatedIconButton _sunsetButton;

    public SunDayNightRuleSettingsControl()
    {
        _sunriseButton = new AnimatedIconButton
        {
            Glyph = "\uF465",
            Text = "白昼"
        };
        _sunriseButton.Classes.Add("display-role");

        _sunsetButton = new AnimatedIconButton
        {
            Glyph = "\uF44B",
            Text = "黑夜"
        };
        _sunsetButton.Classes.Add("display-role");

        _tabStrip = new TabStrip
        {
            Margin = new Thickness(0, 0, 1, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        _tabStrip.Classes.Add("compact");
        _tabStrip.Classes.Add("toggle");
        _tabStrip.Items.Add(new TabStripItem { Content = _sunriseButton });
        _tabStrip.Items.Add(new TabStripItem { Content = _sunsetButton });
        _tabStrip.SelectionChanged += OnSelectionChanged;

        Content = _tabStrip;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (this.TryFindResource("TabStripStyle", out var theme) && theme is ControlTheme controlTheme)
        {
            _tabStrip.Theme = controlTheme;
        }

        _tabStrip.SelectedIndex = Settings.IsSunset ? 1 : 0;
        UpdateExpansion();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Settings.IsSunset = _tabStrip.SelectedIndex == 1;
        UpdateExpansion();
    }

    private void UpdateExpansion()
    {
        _sunriseButton.IsKeepingExpanded = _tabStrip.SelectedIndex == 0;
        _sunsetButton.IsKeepingExpanded = _tabStrip.SelectedIndex == 1;
    }
}
