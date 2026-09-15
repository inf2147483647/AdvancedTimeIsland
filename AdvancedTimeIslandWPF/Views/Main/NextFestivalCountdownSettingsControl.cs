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

public partial class NextFestivalCountdownSettingsControl : ComponentBase<NextFestivalCountdownSettings>
{
    private TextBox _formatTextBox;
    private bool _initCompleted;
    private WpfColorPicker _text1FontColorPicker;
    private WpfColorPicker _nameFontColorPicker;
    private WpfColorPicker _text3FontColorPicker;
    private WpfColorPicker _timeFontColorPicker;
    private WpfNumericUpDown _text1FontSizeNumericUpDown;
    private WpfNumericUpDown _nameFontSizeNumericUpDown;
    private WpfNumericUpDown _text3FontSizeNumericUpDown;
    private WpfNumericUpDown _timeFontSizeNumericUpDown;

    private CheckBox _text1EnableCustomFontSizeToggle;
    private CheckBox _text1EnableCustomFontColorToggle;
    private CheckBox _nameEnableCustomFontSizeToggle;
    private CheckBox _nameEnableCustomFontColorToggle;
    private CheckBox _text3EnableCustomFontSizeToggle;
    private CheckBox _text3EnableCustomFontColorToggle;
    private CheckBox _timeEnableCustomFontSizeToggle;
    private CheckBox _timeEnableCustomFontColorToggle;

    public NextFestivalCountdownSettingsControl()
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
        _formatTextBox = (TextBox)FormatItem.Switcher;

        _text1FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text1FontSizeItem.Switcher).Children[0];
        _text1EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text1FontSizeItem.Switcher).Children[1];
        _text1FontColorPicker = (WpfColorPicker)((StackPanel)Text1ColorItem.Switcher).Children[0];
        _text1EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text1ColorItem.Switcher).Children[1];

        _nameFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)NameFontSizeItem.Switcher).Children[0];
        _nameEnableCustomFontSizeToggle = (CheckBox)((StackPanel)NameFontSizeItem.Switcher).Children[1];
        _nameFontColorPicker = (WpfColorPicker)((StackPanel)NameColorItem.Switcher).Children[0];
        _nameEnableCustomFontColorToggle = (CheckBox)((StackPanel)NameColorItem.Switcher).Children[1];

        _text3FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text3FontSizeItem.Switcher).Children[0];
        _text3EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text3FontSizeItem.Switcher).Children[1];
        _text3FontColorPicker = (WpfColorPicker)((StackPanel)Text3ColorItem.Switcher).Children[0];
        _text3EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text3ColorItem.Switcher).Children[1];

        _timeFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)TimeFontSizeItem.Switcher).Children[0];
        _timeEnableCustomFontSizeToggle = (CheckBox)((StackPanel)TimeFontSizeItem.Switcher).Children[1];
        _timeFontColorPicker = (WpfColorPicker)((StackPanel)TimeColorItem.Switcher).Children[0];
        _timeEnableCustomFontColorToggle = (CheckBox)((StackPanel)TimeColorItem.Switcher).Children[1];

        // 纯开关（SettingsCard.IsOn）：通过依赖属性监听变化，IsOn 值在 OnLoaded 中从 Settings 加载
        var internationalDescriptor = DependencyPropertyDescriptor.FromProperty(
            SettingsCard.IsOnProperty, typeof(SettingsCard));
        internationalDescriptor.AddValueChanged(InternationalItem, (s, e) => Settings.EnableInternationalFestivals = InternationalItem.IsOn);

        var traditionalDescriptor = DependencyPropertyDescriptor.FromProperty(
            SettingsCard.IsOnProperty, typeof(SettingsCard));
        traditionalDescriptor.AddValueChanged(TraditionalItem, (s, e) => Settings.EnableChineseTraditionalFestivals = TraditionalItem.IsOn);

        var redDescriptor = DependencyPropertyDescriptor.FromProperty(
            SettingsCard.IsOnProperty, typeof(SettingsCard));
        redDescriptor.AddValueChanged(RedItem, (s, e) => Settings.EnableRedFestivals = RedItem.IsOn);

        var timeCorrectionDescriptor = DependencyPropertyDescriptor.FromProperty(
            SettingsCard.IsOnProperty, typeof(SettingsCard));
        timeCorrectionDescriptor.AddValueChanged(TimeCorrectionCard, (s, e) => Settings.EnableTimeCorrection = TimeCorrectionCard.IsOn);
    }

    private void OnText1EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontSize = _text1EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontColor = _text1EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.NameEnableCustomFontSize = _nameEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.NameEnableCustomFontColor = _nameEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontSize = _text3EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontColor = _text3EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontSize = _timeEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontColor = _timeEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _nameFontSizeNumericUpDown.IsEnabled = Settings.NameEnableCustomFontSize;
        _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
        _text3FontSizeNumericUpDown.IsEnabled = Settings.Text3EnableCustomFontSize;
        _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        _timeFontSizeNumericUpDown.IsEnabled = Settings.TimeEnableCustomFontSize;
        _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
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
        _formatTextBox.Text = Settings.TimeFormat;

        _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        _nameFontSizeNumericUpDown.Value = (decimal)Settings.NameFontSize;
        _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);

        InternationalItem.IsOn = Settings.EnableInternationalFestivals;
        TraditionalItem.IsOn = Settings.EnableChineseTraditionalFestivals;
        RedItem.IsOn = Settings.EnableRedFestivals;
        TimeCorrectionCard.IsOn = Settings.EnableTimeCorrection;

        _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        _nameEnableCustomFontSizeToggle.IsChecked = Settings.NameEnableCustomFontSize;
        _nameEnableCustomFontColorToggle.IsChecked = Settings.NameEnableCustomFontColor;
        _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;

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
            return Colors.White;
        }
    }

    private void OnFormatLostFocus(object? sender, RoutedEventArgs e) { Settings.TimeFormat = _formatTextBox.Text ?? "%d天"; }

    private void OnText1FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text1FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text1FontSize = (double)_text1FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnNameFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_nameFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.NameFontSize = (double)_nameFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText3FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text3FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text3FontSize = (double)_text3FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnTimeFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_timeFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.TimeFontSize = (double)_timeFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText1ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text1FontColor = _text1FontColorPicker.Color.ToString();
    }

    private void OnNameColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.NameFontColor = _nameFontColorPicker.Color.ToString();
    }

    private void OnText3ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text3FontColor = _text3FontColorPicker.Color.ToString();
    }

    private void OnTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.TimeFontColor = _timeFontColorPicker.Color.ToString();
    }
}
