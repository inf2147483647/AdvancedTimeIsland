using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class JieQiRuleSettingsControl : RuleSettingsControlBase<JieQiRuleSettings>
{
    private ComboBox _jieQiComboBox;

    public JieQiRuleSettingsControl()
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

        var label = new TextBlock { Text = "选择节气：" };
        panel.Children.Add(label);

        _jieQiComboBox = new ComboBox
        {
            ItemsSource = new[] { "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满", "芒种", "夏至", "小暑", "大暑", "立秋", "处暑", "白露", "秋分", "寒露", "霜降", "立冬", "小雪", "大雪", "冬至", "小寒", "大寒" }
        };
        _jieQiComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        panel.Children.Add(_jieQiComboBox);

        Content = panel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _jieQiComboBox.SelectedItem = Settings.TargetJieQi;
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;
        Settings.TargetJieQi = _jieQiComboBox.SelectedItem as string ?? string.Empty;
    }
}
