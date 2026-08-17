using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class AdvancedDateSettingsControl : ComponentBase<AdvancedDateSettings>
{
    private CheckBox _weekDayEnableCustomFontSizeToggle = null!;
    private CheckBox _weekDayEnableCustomFontColorToggle = null!;
    private CheckBox _weekDayEnableCustomFontFamilyToggle = null!;
    private CheckBox _weekDayEnableCustomFontWeightToggle = null!;
    private WpfColorPicker _weekDayColorPicker = null!;
    private WpfNumericUpDown _weekDayFontSizeNumericUpDown = null!;
    private ComboBox _weekDayFontFamilyComboBox = null!;
    private ComboBox _weekDayFontWeightComboBox = null!;

    private CheckBox _dateEnableCustomFontSizeToggle = null!;
    private CheckBox _dateEnableCustomFontColorToggle = null!;
    private CheckBox _dateEnableCustomFontFamilyToggle = null!;
    private CheckBox _dateEnableCustomFontWeightToggle = null!;
    private WpfColorPicker _dateColorPicker = null!;
    private WpfNumericUpDown _dateFontSizeNumericUpDown = null!;
    private ComboBox _dateFontFamilyComboBox = null!;
    private ComboBox _dateFontWeightComboBox = null!;

    private ComboBox _contentOrderComboBox = null!;
    private ComboBox _dateSeparatorComboBox = null!;

    public AdvancedDateSettingsControl()
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
        _contentOrderComboBox = (ComboBox)((StackPanel)ContentOrderItem.Switcher).Children[0];
        _dateSeparatorComboBox = (ComboBox)((StackPanel)DateSeparatorItem.Switcher).Children[0];

        _dateFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)DateFontSizeItem.Switcher).Children[0];
        _dateEnableCustomFontSizeToggle = (CheckBox)((StackPanel)DateFontSizeItem.Switcher).Children[1];

        _dateColorPicker = (WpfColorPicker)((StackPanel)DateColorItem.Switcher).Children[0];
        _dateEnableCustomFontColorToggle = (CheckBox)((StackPanel)DateColorItem.Switcher).Children[1];

        _dateFontFamilyComboBox = (ComboBox)((StackPanel)DateFontFamilyItem.Switcher).Children[0];
        _dateEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)DateFontFamilyItem.Switcher).Children[1];

        _dateFontWeightComboBox = (ComboBox)((StackPanel)DateFontWeightItem.Switcher).Children[0];
        _dateEnableCustomFontWeightToggle = (CheckBox)((StackPanel)DateFontWeightItem.Switcher).Children[1];

        _weekDayFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)WeekDayFontSizeItem.Switcher).Children[0];
        _weekDayEnableCustomFontSizeToggle = (CheckBox)((StackPanel)WeekDayFontSizeItem.Switcher).Children[1];

        _weekDayColorPicker = (WpfColorPicker)((StackPanel)WeekDayColorItem.Switcher).Children[0];
        _weekDayEnableCustomFontColorToggle = (CheckBox)((StackPanel)WeekDayColorItem.Switcher).Children[1];

        _weekDayFontFamilyComboBox = (ComboBox)((StackPanel)WeekDayFontFamilyItem.Switcher).Children[0];
        _weekDayEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)WeekDayFontFamilyItem.Switcher).Children[1];

        _weekDayFontWeightComboBox = (ComboBox)((StackPanel)WeekDayFontWeightItem.Switcher).Children[0];
        _weekDayEnableCustomFontWeightToggle = (CheckBox)((StackPanel)WeekDayFontWeightItem.Switcher).Children[1];

        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _dateFontFamilyComboBox.Items.Add(font);
            _weekDayFontFamilyComboBox.Items.Add(font);
        }
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _dateFontWeightComboBox.Items.Add(weight);
            _weekDayFontWeightComboBox.Items.Add(weight);
        }

        _contentOrderComboBox.Items.Add("日期-星期");
        _contentOrderComboBox.Items.Add("星期-日期");
        _dateSeparatorComboBox.Items.Add("- (2026-07-29)");
        _dateSeparatorComboBox.Items.Add("/ (2026/07/29)");
        _dateSeparatorComboBox.Items.Add(". (2026.07.29)");
        _dateSeparatorComboBox.Items.Add("纯文本 (2026 年 7 月 29 日)");

        // SettingsCard 无内置事件，通过 IsOn 依赖属性变化订阅开关切换
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(ShowWeekDayCard, (s, e) =>
            {
                Settings.ShowWeekDay = ShowWeekDayCard.IsOn;
                UpdateControlsEnabled();
            });
    }

    private void OnDateEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontSize = _dateEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontColor = _dateEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontFamily = _dateEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontWeight = _dateEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.WeekDayEnableCustomFontSize = _weekDayEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.WeekDayEnableCustomFontColor = _weekDayEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.WeekDayEnableCustomFontFamily = _weekDayEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.WeekDayEnableCustomFontWeight = _weekDayEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_dateFontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _dateFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnDateFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_dateFontWeightComboBox.SelectedItem != null)
        {
            Settings.FontWeight = _dateFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnWeekDayFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_weekDayFontFamilyComboBox.SelectedItem != null)
        {
            Settings.WeekDayFontFamily = _weekDayFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnWeekDayFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_weekDayFontWeightComboBox.SelectedItem != null)
        {
            Settings.WeekDayFontWeight = _weekDayFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        var dateFontSizeEnabled = Settings.EnableCustomFontSize;
        var dateFontColorEnabled = Settings.EnableCustomFontColor;
        var dateFontFamilyEnabled = Settings.EnableCustomFontFamily;
        var dateFontWeightEnabled = Settings.EnableCustomFontWeight;
        _dateColorPicker.IsEnabled = dateFontColorEnabled;
        _dateFontSizeNumericUpDown.IsEnabled = dateFontSizeEnabled;
        _dateFontFamilyComboBox.IsEnabled = dateFontFamilyEnabled;
        _dateFontWeightComboBox.IsEnabled = dateFontWeightEnabled;

        var weekDayEnabled = Settings.ShowWeekDay;
        var weekDayFontSizeEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontSize;
        var weekDayFontColorEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontColor;
        var weekDayFontFamilyEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontFamily;
        var weekDayFontWeightEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontWeight;
        _weekDayColorPicker.IsEnabled = weekDayFontColorEnabled;
        _weekDayFontSizeNumericUpDown.IsEnabled = weekDayFontSizeEnabled;
        _weekDayFontFamilyComboBox.IsEnabled = weekDayFontFamilyEnabled;
        _weekDayFontWeightComboBox.IsEnabled = weekDayFontWeightEnabled;
        _weekDayEnableCustomFontSizeToggle.IsEnabled = weekDayEnabled;
        _weekDayEnableCustomFontColorToggle.IsEnabled = weekDayEnabled;
        _weekDayEnableCustomFontFamilyToggle.IsEnabled = weekDayEnabled;
        _weekDayEnableCustomFontWeightToggle.IsEnabled = weekDayEnabled;
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
        ShowWeekDayCard.IsOn = Settings.ShowWeekDay;
        _contentOrderComboBox.SelectedIndex = Settings.DateContentOrder;
        _dateSeparatorComboBox.SelectedIndex = Settings.DateSeparator;
        _dateEnableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _dateEnableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        _dateEnableCustomFontFamilyToggle.IsChecked = Settings.EnableCustomFontFamily;
        _dateEnableCustomFontWeightToggle.IsChecked = Settings.EnableCustomFontWeight;
        _weekDayEnableCustomFontSizeToggle.IsChecked = Settings.WeekDayEnableCustomFontSize;
        _weekDayEnableCustomFontColorToggle.IsChecked = Settings.WeekDayEnableCustomFontColor;
        _weekDayEnableCustomFontFamilyToggle.IsChecked = Settings.WeekDayEnableCustomFontFamily;
        _weekDayEnableCustomFontWeightToggle.IsChecked = Settings.WeekDayEnableCustomFontWeight;
        UpdateControlsEnabled();
        _dateColorPicker.Color = ParseColor(Settings.FontColor);
        _dateFontSizeNumericUpDown.Value = (decimal)Settings.DateFontSize;
        _dateFontFamilyComboBox.SelectedItem = Settings.FontFamily;
        _dateFontWeightComboBox.SelectedItem = Settings.FontWeight;
        _weekDayColorPicker.Color = ParseColor(Settings.WeekDayFontColor);
        _weekDayFontSizeNumericUpDown.Value = (decimal)Settings.WeekDayFontSize;
        _weekDayFontFamilyComboBox.SelectedItem = Settings.WeekDayFontFamily;
        _weekDayFontWeightComboBox.SelectedItem = Settings.WeekDayFontWeight;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
    }

    private void OnContentOrderChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_contentOrderComboBox.SelectedIndex >= 0)
        {
            Settings.DateContentOrder = _contentOrderComboBox.SelectedIndex;
        }
    }

    private void OnDateSeparatorChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_dateSeparatorComboBox.SelectedIndex >= 0)
        {
            Settings.DateSeparator = _dateSeparatorComboBox.SelectedIndex;
        }
    }

    private void OnDateColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.FontColor = _dateColorPicker.Color.ToString();
    }

    private void OnDateFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_dateFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.DateFontSize = (double)_dateFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnWeekDayColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.WeekDayFontColor = _weekDayColorPicker.Color.ToString();
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

    private void OnWeekDayFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_weekDayFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.WeekDayFontSize = (double)_weekDayFontSizeNumericUpDown.Value.Value;
        }
    }
}
