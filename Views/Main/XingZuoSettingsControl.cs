using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class XingZuoSettingsControl : ComponentBase<XingZuoSettings>
{
    private WpfColorPicker _labelColorPicker = null!;
    private bool _initCompleted;
    private WpfNumericUpDown _labelFontSizeNumericUpDown = null!;
    private WpfColorPicker _valueColorPicker = null!;
    private WpfNumericUpDown _valueFontSizeNumericUpDown = null!;
    private CheckBox _labelEnableCustomFontSizeToggle = null!;
    private CheckBox _labelEnableCustomFontColorToggle = null!;
    private CheckBox _valueEnableCustomFontSizeToggle = null!;
    private CheckBox _valueEnableCustomFontColorToggle = null!;
    private CheckBox _labelEnableCustomFontFamilyToggle = null!;
    private CheckBox _valueEnableCustomFontFamilyToggle = null!;
    private CheckBox _labelEnableCustomFontWeightToggle = null!;
    private CheckBox _valueEnableCustomFontWeightToggle = null!;
    private ComboBox _labelFontFamilyComboBox = null!;
    private ComboBox _valueFontFamilyComboBox = null!;
    private ComboBox _labelFontWeightComboBox = null!;
    private ComboBox _valueFontWeightComboBox = null!;

    public XingZuoSettingsControl()
    {
        InitializeComponent();
        InitializeControlReferences();
    }

    /// <summary>
    /// 从 XAML 的 SettingsControl.Switcher 中提取控件引用。
    /// （SettingsControl 模板内元素无法使用 x:Name，需通过 Switcher 属性访问）
    /// </summary>
    private void InitializeControlReferences()
    {
        _labelFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)LabelFontSizeItem.Switcher).Children[0];
        _labelEnableCustomFontSizeToggle = (CheckBox)((StackPanel)LabelFontSizeItem.Switcher).Children[1];

        _labelColorPicker = (WpfColorPicker)((StackPanel)LabelColorItem.Switcher).Children[0];
        _labelEnableCustomFontColorToggle = (CheckBox)((StackPanel)LabelColorItem.Switcher).Children[1];

        _labelFontFamilyComboBox = (ComboBox)((StackPanel)LabelFontFamilyItem.Switcher).Children[0];
        _labelEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)LabelFontFamilyItem.Switcher).Children[1];

        _labelFontWeightComboBox = (ComboBox)((StackPanel)LabelFontWeightItem.Switcher).Children[0];
        _labelEnableCustomFontWeightToggle = (CheckBox)((StackPanel)LabelFontWeightItem.Switcher).Children[1];

        _valueFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)ValueFontSizeItem.Switcher).Children[0];
        _valueEnableCustomFontSizeToggle = (CheckBox)((StackPanel)ValueFontSizeItem.Switcher).Children[1];

        _valueColorPicker = (WpfColorPicker)((StackPanel)ValueColorItem.Switcher).Children[0];
        _valueEnableCustomFontColorToggle = (CheckBox)((StackPanel)ValueColorItem.Switcher).Children[1];

        _valueFontFamilyComboBox = (ComboBox)((StackPanel)ValueFontFamilyItem.Switcher).Children[0];
        _valueEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)ValueFontFamilyItem.Switcher).Children[1];

        _valueFontWeightComboBox = (ComboBox)((StackPanel)ValueFontWeightItem.Switcher).Children[0];
        _valueEnableCustomFontWeightToggle = (CheckBox)((StackPanel)ValueFontWeightItem.Switcher).Children[1];

        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _labelFontFamilyComboBox.Items.Add(font);
            _valueFontFamilyComboBox.Items.Add(font);
        }
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _labelFontWeightComboBox.Items.Add(weight);
            _valueFontWeightComboBox.Items.Add(weight);
        }
    }

    private void OnLabelEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.LabelEnableCustomFontSize = _labelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.LabelEnableCustomFontColor = _labelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.ValueEnableCustomFontSize = _valueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.ValueEnableCustomFontColor = _valueEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.LabelEnableCustomFontFamily = _labelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.ValueEnableCustomFontFamily = _valueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.LabelEnableCustomFontWeight = _labelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.ValueEnableCustomFontWeight = _valueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_labelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.LabelFontFamily = _labelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnLabelFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_labelFontWeightComboBox.SelectedItem != null)
        {
            Settings.LabelFontWeight = _labelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnValueFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_valueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.ValueFontFamily = _valueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnValueFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_valueFontWeightComboBox.SelectedItem != null)
        {
            Settings.ValueFontWeight = _valueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        _labelColorPicker.IsEnabled = Settings.LabelEnableCustomFontColor;
        _labelFontSizeNumericUpDown.IsEnabled = Settings.LabelEnableCustomFontSize;
        _labelFontFamilyComboBox.IsEnabled = Settings.LabelEnableCustomFontFamily;
        _labelFontWeightComboBox.IsEnabled = Settings.LabelEnableCustomFontWeight;
        _valueColorPicker.IsEnabled = Settings.ValueEnableCustomFontColor;
        _valueFontSizeNumericUpDown.IsEnabled = Settings.ValueEnableCustomFontSize;
        _valueFontFamilyComboBox.IsEnabled = Settings.ValueEnableCustomFontFamily;
        _valueFontWeightComboBox.IsEnabled = Settings.ValueEnableCustomFontWeight;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }
        _initCompleted = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        _labelEnableCustomFontSizeToggle.IsChecked = Settings.LabelEnableCustomFontSize;
        _labelEnableCustomFontColorToggle.IsChecked = Settings.LabelEnableCustomFontColor;
        _valueEnableCustomFontSizeToggle.IsChecked = Settings.ValueEnableCustomFontSize;
        _valueEnableCustomFontColorToggle.IsChecked = Settings.ValueEnableCustomFontColor;
        _labelEnableCustomFontFamilyToggle.IsChecked = Settings.LabelEnableCustomFontFamily;
        _valueEnableCustomFontFamilyToggle.IsChecked = Settings.ValueEnableCustomFontFamily;
        _labelEnableCustomFontWeightToggle.IsChecked = Settings.LabelEnableCustomFontWeight;
        _valueEnableCustomFontWeightToggle.IsChecked = Settings.ValueEnableCustomFontWeight;
        UpdateControlsEnabled();
        _labelColorPicker.Color = ParseColor(Settings.LabelFontColor);
        _labelFontSizeNumericUpDown.Value = (decimal)Settings.LabelFontSize;
        _valueColorPicker.Color = ParseColor(Settings.ValueFontColor);
        _valueFontSizeNumericUpDown.Value = (decimal)Settings.ValueFontSize;
        _labelFontFamilyComboBox.SelectedItem = Settings.LabelFontFamily;
        _valueFontFamilyComboBox.SelectedItem = Settings.ValueFontFamily;
        _labelFontWeightComboBox.SelectedItem = Settings.LabelFontWeight;
        _valueFontWeightComboBox.SelectedItem = Settings.ValueFontWeight;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
    }

    private void OnLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.LabelFontColor = _labelColorPicker.Color.ToString();
    }

    private void OnLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_labelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.LabelFontSize = (double)_labelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnValueColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ValueFontColor = _valueColorPicker.Color.ToString();
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return ThemeHelper.ParseColor(colorString);
        }
        catch
        {
            return ThemeHelper.ParseColor(ThemeHelper.GetTextColorHex());
        }
    }

    private void OnValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_valueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.ValueFontSize = (double)_valueFontSizeNumericUpDown.Value.Value;
        }
    }
}
