using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class DayYiJiSettingsControl : ComponentBase<DayYiJiSettings>
{
    private WpfNumericUpDown _yiLabelFontSizeNumericUpDown = null!;
    private bool _initCompleted;
    private WpfColorPicker _yiLabelFontColorPicker = null!;
    private WpfNumericUpDown _yiValueFontSizeNumericUpDown = null!;
    private WpfNumericUpDown _jiLabelFontSizeNumericUpDown = null!;
    private WpfColorPicker _jiLabelFontColorPicker = null!;
    private WpfNumericUpDown _jiValueFontSizeNumericUpDown = null!;
    private CheckBox _yiLabelEnableCustomFontSizeToggle = null!;
    private CheckBox _yiLabelEnableCustomFontColorToggle = null!;
    private CheckBox _yiLabelEnableCustomFontFamilyToggle = null!;
    private CheckBox _yiLabelEnableCustomFontWeightToggle = null!;
    private CheckBox _yiValueEnableCustomFontSizeToggle = null!;
    private CheckBox _yiValueEnableCustomFontFamilyToggle = null!;
    private CheckBox _yiValueEnableCustomFontWeightToggle = null!;
    private CheckBox _jiLabelEnableCustomFontSizeToggle = null!;
    private CheckBox _jiLabelEnableCustomFontColorToggle = null!;
    private CheckBox _jiLabelEnableCustomFontFamilyToggle = null!;
    private CheckBox _jiLabelEnableCustomFontWeightToggle = null!;
    private CheckBox _jiValueEnableCustomFontSizeToggle = null!;
    private CheckBox _jiValueEnableCustomFontFamilyToggle = null!;
    private CheckBox _jiValueEnableCustomFontWeightToggle = null!;
    private ComboBox _yiLabelFontFamilyComboBox = null!;
    private ComboBox _yiValueFontFamilyComboBox = null!;
    private ComboBox _jiLabelFontFamilyComboBox = null!;
    private ComboBox _jiValueFontFamilyComboBox = null!;
    private ComboBox _yiLabelFontWeightComboBox = null!;
    private ComboBox _yiValueFontWeightComboBox = null!;
    private ComboBox _jiLabelFontWeightComboBox = null!;
    private ComboBox _jiValueFontWeightComboBox = null!;
    private ComboBox _displayModeComboBox = null!;

    public DayYiJiSettingsControl()
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
        _displayModeComboBox = (ComboBox)((StackPanel)DisplayModeItem.Switcher).Children[0];

        _yiLabelFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)YiLabelFontSizeItem.Switcher).Children[0];
        _yiLabelEnableCustomFontSizeToggle = (CheckBox)((StackPanel)YiLabelFontSizeItem.Switcher).Children[1];

        _yiLabelFontColorPicker = (WpfColorPicker)((StackPanel)YiLabelColorItem.Switcher).Children[0];
        _yiLabelEnableCustomFontColorToggle = (CheckBox)((StackPanel)YiLabelColorItem.Switcher).Children[1];

        _yiLabelFontFamilyComboBox = (ComboBox)((StackPanel)YiLabelFontFamilyItem.Switcher).Children[0];
        _yiLabelEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)YiLabelFontFamilyItem.Switcher).Children[1];

        _yiLabelFontWeightComboBox = (ComboBox)((StackPanel)YiLabelFontWeightItem.Switcher).Children[0];
        _yiLabelEnableCustomFontWeightToggle = (CheckBox)((StackPanel)YiLabelFontWeightItem.Switcher).Children[1];

        _yiValueFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)YiValueFontSizeItem.Switcher).Children[0];
        _yiValueEnableCustomFontSizeToggle = (CheckBox)((StackPanel)YiValueFontSizeItem.Switcher).Children[1];

        _yiValueFontFamilyComboBox = (ComboBox)((StackPanel)YiValueFontFamilyItem.Switcher).Children[0];
        _yiValueEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)YiValueFontFamilyItem.Switcher).Children[1];

        _yiValueFontWeightComboBox = (ComboBox)((StackPanel)YiValueFontWeightItem.Switcher).Children[0];
        _yiValueEnableCustomFontWeightToggle = (CheckBox)((StackPanel)YiValueFontWeightItem.Switcher).Children[1];

        _jiLabelFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)JiLabelFontSizeItem.Switcher).Children[0];
        _jiLabelEnableCustomFontSizeToggle = (CheckBox)((StackPanel)JiLabelFontSizeItem.Switcher).Children[1];

        _jiLabelFontColorPicker = (WpfColorPicker)((StackPanel)JiLabelColorItem.Switcher).Children[0];
        _jiLabelEnableCustomFontColorToggle = (CheckBox)((StackPanel)JiLabelColorItem.Switcher).Children[1];

        _jiLabelFontFamilyComboBox = (ComboBox)((StackPanel)JiLabelFontFamilyItem.Switcher).Children[0];
        _jiLabelEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)JiLabelFontFamilyItem.Switcher).Children[1];

        _jiLabelFontWeightComboBox = (ComboBox)((StackPanel)JiLabelFontWeightItem.Switcher).Children[0];
        _jiLabelEnableCustomFontWeightToggle = (CheckBox)((StackPanel)JiLabelFontWeightItem.Switcher).Children[1];

        _jiValueFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)JiValueFontSizeItem.Switcher).Children[0];
        _jiValueEnableCustomFontSizeToggle = (CheckBox)((StackPanel)JiValueFontSizeItem.Switcher).Children[1];

        _jiValueFontFamilyComboBox = (ComboBox)((StackPanel)JiValueFontFamilyItem.Switcher).Children[0];
        _jiValueEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)JiValueFontFamilyItem.Switcher).Children[1];

        _jiValueFontWeightComboBox = (ComboBox)((StackPanel)JiValueFontWeightItem.Switcher).Children[0];
        _jiValueEnableCustomFontWeightToggle = (CheckBox)((StackPanel)JiValueFontWeightItem.Switcher).Children[1];

        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _yiLabelFontFamilyComboBox.Items.Add(font);
            _yiValueFontFamilyComboBox.Items.Add(font);
            _jiLabelFontFamilyComboBox.Items.Add(font);
            _jiValueFontFamilyComboBox.Items.Add(font);
        }
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _yiLabelFontWeightComboBox.Items.Add(weight);
            _yiValueFontWeightComboBox.Items.Add(weight);
            _jiLabelFontWeightComboBox.Items.Add(weight);
            _jiValueFontWeightComboBox.Items.Add(weight);
        }
    }

    private void OnDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        Settings.DisplayMode = _displayModeComboBox.SelectedIndex;
    }

    private void OnInfoBarClosed(object? sender, EventArgs e)
    {
        Settings.InfoBarDismissed = true;
    }

    private void OnYiLabelEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.YiLabelEnableCustomFontSize = _yiLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.YiLabelEnableCustomFontColor = _yiLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.YiValueEnableCustomFontSize = _yiValueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.JiLabelEnableCustomFontSize = _jiLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.JiLabelEnableCustomFontColor = _jiLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.JiValueEnableCustomFontSize = _jiValueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.YiLabelEnableCustomFontFamily = _yiLabelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.YiLabelEnableCustomFontWeight = _yiLabelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiLabelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.YiLabelFontFamily = _yiLabelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiLabelFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiLabelFontWeightComboBox.SelectedItem != null)
        {
            Settings.YiLabelFontWeight = _yiLabelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiValueEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.YiValueEnableCustomFontFamily = _yiValueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.YiValueEnableCustomFontWeight = _yiValueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiValueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.YiValueFontFamily = _yiValueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiValueFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiValueFontWeightComboBox.SelectedItem != null)
        {
            Settings.YiValueFontWeight = _yiValueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiLabelEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.JiLabelEnableCustomFontFamily = _jiLabelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.JiLabelEnableCustomFontWeight = _jiLabelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiLabelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.JiLabelFontFamily = _jiLabelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiLabelFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiLabelFontWeightComboBox.SelectedItem != null)
        {
            Settings.JiLabelFontWeight = _jiLabelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiValueEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.JiValueEnableCustomFontFamily = _jiValueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.JiValueEnableCustomFontWeight = _jiValueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiValueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.JiValueFontFamily = _jiValueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiValueFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiValueFontWeightComboBox.SelectedItem != null)
        {
            Settings.JiValueFontWeight = _jiValueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        _yiLabelFontSizeNumericUpDown.IsEnabled = Settings.YiLabelEnableCustomFontSize;
        _yiLabelFontColorPicker.IsEnabled = Settings.YiLabelEnableCustomFontColor;
        _yiLabelFontFamilyComboBox.IsEnabled = Settings.YiLabelEnableCustomFontFamily;
        _yiLabelFontWeightComboBox.IsEnabled = Settings.YiLabelEnableCustomFontWeight;
        _yiValueFontSizeNumericUpDown.IsEnabled = Settings.YiValueEnableCustomFontSize;
        _yiValueFontFamilyComboBox.IsEnabled = Settings.YiValueEnableCustomFontFamily;
        _yiValueFontWeightComboBox.IsEnabled = Settings.YiValueEnableCustomFontWeight;
        _jiLabelFontSizeNumericUpDown.IsEnabled = Settings.JiLabelEnableCustomFontSize;
        _jiLabelFontColorPicker.IsEnabled = Settings.JiLabelEnableCustomFontColor;
        _jiLabelFontFamilyComboBox.IsEnabled = Settings.JiLabelEnableCustomFontFamily;
        _jiLabelFontWeightComboBox.IsEnabled = Settings.JiLabelEnableCustomFontWeight;
        _jiValueFontSizeNumericUpDown.IsEnabled = Settings.JiValueEnableCustomFontSize;
        _jiValueFontFamilyComboBox.IsEnabled = Settings.JiValueEnableCustomFontFamily;
        _jiValueFontWeightComboBox.IsEnabled = Settings.JiValueEnableCustomFontWeight;
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
        InfoBar.IsOpen = !Settings.InfoBarDismissed;
        _displayModeComboBox.SelectedIndex = Settings.DisplayMode;

        _yiLabelFontSizeNumericUpDown.Value = (decimal)Settings.YiLabelFontSize;
        _yiLabelFontColorPicker.Color = ParseColor(Settings.YiLabelFontColor);
        _yiValueFontSizeNumericUpDown.Value = (decimal)Settings.YiValueFontSize;
        _jiLabelFontSizeNumericUpDown.Value = (decimal)Settings.JiLabelFontSize;
        _jiLabelFontColorPicker.Color = ParseColor(Settings.JiLabelFontColor);
        _jiValueFontSizeNumericUpDown.Value = (decimal)Settings.JiValueFontSize;

        _yiLabelFontFamilyComboBox.SelectedItem = Settings.YiLabelFontFamily;
        _yiValueFontFamilyComboBox.SelectedItem = Settings.YiValueFontFamily;
        _jiLabelFontFamilyComboBox.SelectedItem = Settings.JiLabelFontFamily;
        _jiValueFontFamilyComboBox.SelectedItem = Settings.JiValueFontFamily;
        _yiLabelFontWeightComboBox.SelectedItem = Settings.YiLabelFontWeight;
        _yiValueFontWeightComboBox.SelectedItem = Settings.YiValueFontWeight;
        _jiLabelFontWeightComboBox.SelectedItem = Settings.JiLabelFontWeight;
        _jiValueFontWeightComboBox.SelectedItem = Settings.JiValueFontWeight;

        _yiLabelEnableCustomFontSizeToggle.IsChecked = Settings.YiLabelEnableCustomFontSize;
        _yiLabelEnableCustomFontColorToggle.IsChecked = Settings.YiLabelEnableCustomFontColor;
        _yiLabelEnableCustomFontFamilyToggle.IsChecked = Settings.YiLabelEnableCustomFontFamily;
        _yiLabelEnableCustomFontWeightToggle.IsChecked = Settings.YiLabelEnableCustomFontWeight;
        _yiValueEnableCustomFontSizeToggle.IsChecked = Settings.YiValueEnableCustomFontSize;
        _yiValueEnableCustomFontFamilyToggle.IsChecked = Settings.YiValueEnableCustomFontFamily;
        _yiValueEnableCustomFontWeightToggle.IsChecked = Settings.YiValueEnableCustomFontWeight;
        _jiLabelEnableCustomFontSizeToggle.IsChecked = Settings.JiLabelEnableCustomFontSize;
        _jiLabelEnableCustomFontColorToggle.IsChecked = Settings.JiLabelEnableCustomFontColor;
        _jiLabelEnableCustomFontFamilyToggle.IsChecked = Settings.JiLabelEnableCustomFontFamily;
        _jiLabelEnableCustomFontWeightToggle.IsChecked = Settings.JiLabelEnableCustomFontWeight;
        _jiValueEnableCustomFontSizeToggle.IsChecked = Settings.JiValueEnableCustomFontSize;
        _jiValueEnableCustomFontFamilyToggle.IsChecked = Settings.JiValueEnableCustomFontFamily;
        _jiValueEnableCustomFontWeightToggle.IsChecked = Settings.JiValueEnableCustomFontWeight;

        UpdateControlsEnabled();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
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

    private void OnYiLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_yiLabelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.YiLabelFontSize = (double)_yiLabelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnYiValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_yiValueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.YiValueFontSize = (double)_yiValueFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnJiLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_jiLabelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.JiLabelFontSize = (double)_jiLabelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnJiValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_jiValueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.JiValueFontSize = (double)_jiValueFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnYiLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.YiLabelFontColor = _yiLabelFontColorPicker.Color.ToString();
    }

    private void OnJiLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.JiLabelFontColor = _jiLabelFontColorPicker.Color.ToString();
    }
}
