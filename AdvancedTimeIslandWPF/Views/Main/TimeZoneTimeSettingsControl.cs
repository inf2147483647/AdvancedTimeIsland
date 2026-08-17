using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class TimeZoneTimeSettingsControl : ComponentBase<TimeZoneTimeSettings>
{
    private ComboBox _timeZoneComboBox;
    private CheckBox _enableCustomFontSizeToggle;
    private CheckBox _enableCustomFontColorToggle;
    private CheckBox _enableCustomFontFamilyToggle;
    private CheckBox _enableCustomFontWeightToggle;
    private WpfColorPicker _colorPicker;
    private WpfNumericUpDown _fontSizeNumericUpDown;
    private ComboBox _fontFamilyComboBox;
    private ComboBox _fontWeightComboBox;

    public TimeZoneTimeSettingsControl()
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
        _timeZoneComboBox = (ComboBox)TimeZoneItem.Switcher;
        var tzs = TimeZoneInfo.GetSystemTimeZones();
        foreach (var tz in tzs) _timeZoneComboBox.Items.Add(tz);

        _fontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)FontSizeItem.Switcher).Children[0];
        _enableCustomFontSizeToggle = (CheckBox)((StackPanel)FontSizeItem.Switcher).Children[1];

        _colorPicker = (WpfColorPicker)((StackPanel)ColorItem.Switcher).Children[0];
        _enableCustomFontColorToggle = (CheckBox)((StackPanel)ColorItem.Switcher).Children[1];

        _fontFamilyComboBox = (ComboBox)((StackPanel)FontFamilyItem.Switcher).Children[0];
        _enableCustomFontFamilyToggle = (CheckBox)((StackPanel)FontFamilyItem.Switcher).Children[1];

        _fontWeightComboBox = (ComboBox)((StackPanel)FontWeightItem.Switcher).Children[0];
        _enableCustomFontWeightToggle = (CheckBox)((StackPanel)FontWeightItem.Switcher).Children[1];

        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _fontFamilyComboBox.Items.Add(font);
        }
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _fontWeightComboBox.Items.Add(weight);
        }
    }

    private void OnEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontColor = _enableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontFamily = _enableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontWeight = _enableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_fontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _fontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_fontWeightComboBox.SelectedItem != null)
        {
            Settings.FontWeight = _fontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        var fontSizeEnabled = Settings.EnableCustomFontSize;
        var fontColorEnabled = Settings.EnableCustomFontColor;
        var fontFamilyEnabled = Settings.EnableCustomFontFamily;
        var fontWeightEnabled = Settings.EnableCustomFontWeight;
        _colorPicker.IsEnabled = fontColorEnabled;
        _fontSizeNumericUpDown.IsEnabled = fontSizeEnabled;
        _fontFamilyComboBox.IsEnabled = fontFamilyEnabled;
        _fontWeightComboBox.IsEnabled = fontWeightEnabled;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        foreach (var item in _timeZoneComboBox.Items)
        {
            if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
            {
                _timeZoneComboBox.SelectedItem = item;
                break;
            }
        }
        _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        _enableCustomFontFamilyToggle.IsChecked = Settings.EnableCustomFontFamily;
        _enableCustomFontWeightToggle.IsChecked = Settings.EnableCustomFontWeight;
        UpdateControlsEnabled();
        _colorPicker.Color = ParseColor(Settings.FontColor);
        _fontSizeNumericUpDown.Value = (decimal)Settings.TextFontSize;
        _fontFamilyComboBox.SelectedItem = Settings.FontFamily;
        _fontWeightComboBox.SelectedItem = Settings.FontWeight;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
    }

    private void OnTimeZoneSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz) Settings.TimeZoneId = tz.Id;
    }

    private void OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.FontColor = _colorPicker.Color.ToString();
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

    private void OnFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_fontSizeNumericUpDown.Value.HasValue)
        {
            Settings.TextFontSize = (double)_fontSizeNumericUpDown.Value.Value;
        }
    }
}
