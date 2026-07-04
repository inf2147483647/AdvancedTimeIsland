using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class XingZuoRuleSettingsControl : RuleSettingsControlBase<XingZuoRuleSettings>
{
    private ComboBox _xingZuoComboBox;

    public XingZuoRuleSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
    }

    private void InitializeComponent()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        var label = new TextBlock { Text = "选择星座：" };
        panel.Children.Add(label);

        _xingZuoComboBox = new ComboBox
        {
            ItemsSource = new[] { "白羊座", "金牛座", "双子座", "巨蟹座", "狮子座", "处女座", "天秤座", "天蝎座", "射手座", "摩羯座", "水瓶座", "双鱼座" }
        };
        _xingZuoComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        panel.Children.Add(_xingZuoComboBox);

        Content = panel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _xingZuoComboBox.SelectedItem = Settings.TargetXingZuo;
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;
        Settings.TargetXingZuo = _xingZuoComboBox.SelectedItem as string ?? string.Empty;
    }
}
