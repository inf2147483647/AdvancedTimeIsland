using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class YearWeekSettingsControl : ComponentBase<YearWeekSettings>
{
    private WpfColorPicker _colorPicker = null!;
    private WpfNumericUpDown _fontSizeNumericUpDown = null!;
    private CheckBox _enableCustomFontSizeToggle = null!;
    private CheckBox _enableCustomFontColorToggle = null!;
    private CheckBox _enableCustomFontFamilyToggle = null!;
    private CheckBox _enableCustomFontWeightToggle = null!;
    private ComboBox _fontFamilyComboBox = null!;
    private ComboBox _fontWeightComboBox = null!;
    private ComboBox _firstDayOfWeekComboBox = null!;
    private ComboBox _timeBaseComboBox = null!;
    private bool _initCompleted;

    private static readonly object[] FirstDayOfWeekItems =
    {
        "周日", "周一", "周二", "周三", "周四", "周五", "周六"
    };

    private static readonly object[] TimeBaseItems =
    {
        "插件偏移后的服务器时间", "原始服务器时间", "ClassIsland时间"
    };

    public YearWeekSettingsControl()
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
        _firstDayOfWeekComboBox = (ComboBox)FirstDayOfWeekItem.Switcher;
        _timeBaseComboBox = (ComboBox)TimeBaseItem.Switcher;

        _fontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)FontSizeItem.Switcher).Children[0];
        _enableCustomFontSizeToggle = (CheckBox)((StackPanel)FontSizeItem.Switcher).Children[1];

        _colorPicker = (WpfColorPicker)((StackPanel)ColorItem.Switcher).Children[0];
        _enableCustomFontColorToggle = (CheckBox)((StackPanel)ColorItem.Switcher).Children[1];

        _fontFamilyComboBox = (ComboBox)((StackPanel)FontFamilyItem.Switcher).Children[0];
        _enableCustomFontFamilyToggle = (CheckBox)((StackPanel)FontFamilyItem.Switcher).Children[1];

        _fontWeightComboBox = (ComboBox)((StackPanel)FontWeightItem.Switcher).Children[0];
        _enableCustomFontWeightToggle = (CheckBox)((StackPanel)FontWeightItem.Switcher).Children[1];

        foreach (var day in FirstDayOfWeekItems)
        {
            _firstDayOfWeekComboBox.Items.Add(day);
        }
        foreach (var item in TimeBaseItems)
        {
            _timeBaseComboBox.Items.Add(item);
        }
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

    private void OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.FontColor = _colorPicker.Color.ToString();
    }

    private void OnFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_fontSizeNumericUpDown.Value.HasValue)
        {
            Settings.FontSize = (double)_fontSizeNumericUpDown.Value.Value;
        }
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

    private void OnFirstDayOfWeekChanged(object? sender, SelectionChangedEventArgs e)
    {
        var idx = _firstDayOfWeekComboBox.SelectedIndex;
        if (idx >= 0 && idx <= 6)
        {
            Settings.FirstDayOfWeek = idx;
        }
    }

    private void OnTimeBaseChanged(object? sender, SelectionChangedEventArgs e)
    {
        var idx = _timeBaseComboBox.SelectedIndex;
        Settings.TimeBaseType = idx switch
        {
            0 => TimeBaseType.PluginOffsetServerTime,
            1 => TimeBaseType.RawServerTime,
            2 => TimeBaseType.ClassIslandTime,
            _ => TimeBaseType.PluginOffsetServerTime
        };
    }

    private void UpdateControlsEnabled()
    {
        _colorPicker.IsEnabled = Settings.EnableCustomFontColor;
        _fontSizeNumericUpDown.IsEnabled = Settings.EnableCustomFontSize;
        _fontFamilyComboBox.IsEnabled = Settings.EnableCustomFontFamily;
        _fontWeightComboBox.IsEnabled = Settings.EnableCustomFontWeight;
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
        _firstDayOfWeekComboBox.SelectedIndex = Settings.FirstDayOfWeek;
        _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => 0,
            TimeBaseType.RawServerTime => 1,
            TimeBaseType.ClassIslandTime => 2,
            _ => 0
        };
        _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        _enableCustomFontFamilyToggle.IsChecked = Settings.EnableCustomFontFamily;
        _enableCustomFontWeightToggle.IsChecked = Settings.EnableCustomFontWeight;
        UpdateControlsEnabled();
        _colorPicker.Color = ParseColor(Settings.FontColor);
        _fontSizeNumericUpDown.Value = (decimal)Settings.FontSize;
        _fontFamilyComboBox.SelectedItem = Settings.FontFamily;
        _fontWeightComboBox.SelectedItem = Settings.FontWeight;
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
}
