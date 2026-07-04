using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class ShengXiaoRuleSettingsControl : RuleSettingsControlBase<ShengXiaoRuleSettings>
{
    private ComboBox _shengXiaoComboBox;

    public ShengXiaoRuleSettingsControl()
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

        var label = new TextBlock { Text = "选择生肖：" };
        panel.Children.Add(label);

        _shengXiaoComboBox = new ComboBox
        {
            ItemsSource = new[] { "鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪" }
        };
        _shengXiaoComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        panel.Children.Add(_shengXiaoComboBox);

        Content = panel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _shengXiaoComboBox.SelectedItem = Settings.TargetShengXiao;
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;
        Settings.TargetShengXiao = _shengXiaoComboBox.SelectedItem as string ?? string.Empty;
    }
}
