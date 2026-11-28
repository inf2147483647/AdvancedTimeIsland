using System;
using System.Collections.Generic;
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

public partial class CountdownSettingsControl : ComponentBase<CountdownSettings>
{
    private TextBox _text1TextBox = null!;
    private TextBox _text3TextBox = null!;
    private TextBox _text4TextBox = null!;
    private TextBox _timeFormatTextBox = null!;
    private CheckBox _text1EnableCustomFontSizeToggle = null!;
    private CheckBox _text1EnableCustomFontColorToggle = null!;
    private CheckBox _text2EnableCustomFontSizeToggle = null!;
    private CheckBox _text2EnableCustomFontColorToggle = null!;
    private CheckBox _text3EnableCustomFontSizeToggle = null!;
    private CheckBox _text3EnableCustomFontColorToggle = null!;
    private CheckBox _timeEnableCustomFontSizeToggle = null!;
    private CheckBox _timeEnableCustomFontColorToggle = null!;
    private CheckBox _text4EnableCustomFontSizeToggle = null!;
    private CheckBox _text4EnableCustomFontColorToggle = null!;
    private CheckBox _text1EnableCustomFontFamilyToggle = null!;
    private CheckBox _text2EnableCustomFontFamilyToggle = null!;
    private CheckBox _text3EnableCustomFontFamilyToggle = null!;
    private CheckBox _timeEnableCustomFontFamilyToggle = null!;
    private CheckBox _text4EnableCustomFontFamilyToggle = null!;
    private CheckBox _text1EnableCustomFontWeightToggle = null!;
    private CheckBox _text2EnableCustomFontWeightToggle = null!;
    private CheckBox _text3EnableCustomFontWeightToggle = null!;
    private CheckBox _timeEnableCustomFontWeightToggle = null!;
    private CheckBox _text4EnableCustomFontWeightToggle = null!;
    private ComboBox _timeBaseComboBox = null!;
    private ListBox _countdownListBox = null!;

    private TextBlock _selectionHintTextBlock = null!;
    private System.Timers.Timer? _hintTimer;

    private WpfNumericUpDown _text1FontSizeNumericUpDown = null!;
    private WpfColorPicker _text1FontColorPicker = null!;
    private WpfNumericUpDown _text2FontSizeNumericUpDown = null!;
    private WpfColorPicker _text2FontColorPicker = null!;
    private WpfNumericUpDown _text3FontSizeNumericUpDown = null!;
    private WpfColorPicker _text3FontColorPicker = null!;
    private WpfNumericUpDown _timeFontSizeNumericUpDown = null!;
    private WpfColorPicker _timeFontColorPicker = null!;
    private WpfNumericUpDown _text4FontSizeNumericUpDown = null!;
    private WpfColorPicker _text4FontColorPicker = null!;
    private ComboBox _text1FontFamilyComboBox = null!;
    private ComboBox _text2FontFamilyComboBox = null!;
    private ComboBox _text3FontFamilyComboBox = null!;
    private ComboBox _timeFontFamilyComboBox = null!;
    private ComboBox _text4FontFamilyComboBox = null!;
    private ComboBox _text1FontWeightComboBox = null!;
    private ComboBox _text2FontWeightComboBox = null!;
    private ComboBox _text3FontWeightComboBox = null!;
    private ComboBox _timeFontWeightComboBox = null!;
    private ComboBox _text4FontWeightComboBox = null!;

    private TextBox _startYearTextBox = null!;
    private ComboBox _startMonthComboBox = null!;
    private ComboBox _startDayComboBox = null!;
    private ComboBox _startHourComboBox = null!;
    private ComboBox _startMinuteComboBox = null!;
    private ComboBox _startSecondComboBox = null!;

    private ComboBox _progressDisplayModeComboBox = null!;
    private WpfColorPicker _progressBarColorPicker = null!;
    private WpfColorPicker _progressRingColorPicker = null!;
    private bool _initCompleted;

    public CountdownSettingsControl()
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
        _text1TextBox = (TextBox)((StackPanel)Text1Item.Switcher).Children[0];
        _text3TextBox = (TextBox)((StackPanel)Text3Item.Switcher).Children[0];
        _text4TextBox = (TextBox)((StackPanel)Text4Item.Switcher).Children[0];
        _timeFormatTextBox = (TextBox)((StackPanel)TimeFormatItem.Switcher).Children[0];
        _timeBaseComboBox = (ComboBox)((StackPanel)TimeBaseItem.Switcher).Children[0];

        _startYearTextBox = (TextBox)((StackPanel)StartDateItem.Switcher).Children[0];
        _startMonthComboBox = (ComboBox)((StackPanel)StartDateItem.Switcher).Children[1];
        _startDayComboBox = (ComboBox)((StackPanel)StartDateItem.Switcher).Children[2];
        _startHourComboBox = (ComboBox)((StackPanel)StartTimeItem.Switcher).Children[0];
        _startMinuteComboBox = (ComboBox)((StackPanel)StartTimeItem.Switcher).Children[2];
        _startSecondComboBox = (ComboBox)((StackPanel)StartTimeItem.Switcher).Children[4];

        _progressDisplayModeComboBox = (ComboBox)((StackPanel)ProgressDisplayModeItem.Switcher).Children[0];
        _progressBarColorPicker = (WpfColorPicker)((StackPanel)ProgressBarColorItem.Switcher).Children[0];
        _progressRingColorPicker = (WpfColorPicker)((StackPanel)ProgressRingColorItem.Switcher).Children[0];

        _text1FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text1FontSizeItem.Switcher).Children[0];
        _text1EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text1FontSizeItem.Switcher).Children[1];
        _text1FontColorPicker = (WpfColorPicker)((StackPanel)Text1ColorItem.Switcher).Children[0];
        _text1EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text1ColorItem.Switcher).Children[1];
        _text1FontFamilyComboBox = (ComboBox)((StackPanel)Text1FontFamilyItem.Switcher).Children[0];
        _text1EnableCustomFontFamilyToggle = (CheckBox)((StackPanel)Text1FontFamilyItem.Switcher).Children[1];
        _text1FontWeightComboBox = (ComboBox)((StackPanel)Text1FontWeightItem.Switcher).Children[0];
        _text1EnableCustomFontWeightToggle = (CheckBox)((StackPanel)Text1FontWeightItem.Switcher).Children[1];

        _text2FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text2FontSizeItem.Switcher).Children[0];
        _text2EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text2FontSizeItem.Switcher).Children[1];
        _text2FontColorPicker = (WpfColorPicker)((StackPanel)Text2ColorItem.Switcher).Children[0];
        _text2EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text2ColorItem.Switcher).Children[1];
        _text2FontFamilyComboBox = (ComboBox)((StackPanel)Text2FontFamilyItem.Switcher).Children[0];
        _text2EnableCustomFontFamilyToggle = (CheckBox)((StackPanel)Text2FontFamilyItem.Switcher).Children[1];
        _text2FontWeightComboBox = (ComboBox)((StackPanel)Text2FontWeightItem.Switcher).Children[0];
        _text2EnableCustomFontWeightToggle = (CheckBox)((StackPanel)Text2FontWeightItem.Switcher).Children[1];

        _text3FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text3FontSizeItem.Switcher).Children[0];
        _text3EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text3FontSizeItem.Switcher).Children[1];
        _text3FontColorPicker = (WpfColorPicker)((StackPanel)Text3ColorItem.Switcher).Children[0];
        _text3EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text3ColorItem.Switcher).Children[1];
        _text3FontFamilyComboBox = (ComboBox)((StackPanel)Text3FontFamilyItem.Switcher).Children[0];
        _text3EnableCustomFontFamilyToggle = (CheckBox)((StackPanel)Text3FontFamilyItem.Switcher).Children[1];
        _text3FontWeightComboBox = (ComboBox)((StackPanel)Text3FontWeightItem.Switcher).Children[0];
        _text3EnableCustomFontWeightToggle = (CheckBox)((StackPanel)Text3FontWeightItem.Switcher).Children[1];

        _timeFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)TimeFontSizeItem.Switcher).Children[0];
        _timeEnableCustomFontSizeToggle = (CheckBox)((StackPanel)TimeFontSizeItem.Switcher).Children[1];
        _timeFontColorPicker = (WpfColorPicker)((StackPanel)TimeColorItem.Switcher).Children[0];
        _timeEnableCustomFontColorToggle = (CheckBox)((StackPanel)TimeColorItem.Switcher).Children[1];
        _timeFontFamilyComboBox = (ComboBox)((StackPanel)TimeFontFamilyItem.Switcher).Children[0];
        _timeEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)TimeFontFamilyItem.Switcher).Children[1];
        _timeFontWeightComboBox = (ComboBox)((StackPanel)TimeFontWeightItem.Switcher).Children[0];
        _timeEnableCustomFontWeightToggle = (CheckBox)((StackPanel)TimeFontWeightItem.Switcher).Children[1];

        _text4FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text4FontSizeItem.Switcher).Children[0];
        _text4EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text4FontSizeItem.Switcher).Children[1];
        _text4FontColorPicker = (WpfColorPicker)((StackPanel)Text4ColorItem.Switcher).Children[0];
        _text4EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text4ColorItem.Switcher).Children[1];
        _text4FontFamilyComboBox = (ComboBox)((StackPanel)Text4FontFamilyItem.Switcher).Children[0];
        _text4EnableCustomFontFamilyToggle = (CheckBox)((StackPanel)Text4FontFamilyItem.Switcher).Children[1];
        _text4FontWeightComboBox = (ComboBox)((StackPanel)Text4FontWeightItem.Switcher).Children[0];
        _text4EnableCustomFontWeightToggle = (CheckBox)((StackPanel)Text4FontWeightItem.Switcher).Children[1];

        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _text1FontFamilyComboBox.Items.Add(font);
            _text2FontFamilyComboBox.Items.Add(font);
            _text3FontFamilyComboBox.Items.Add(font);
            _timeFontFamilyComboBox.Items.Add(font);
            _text4FontFamilyComboBox.Items.Add(font);
        }
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _text1FontWeightComboBox.Items.Add(weight);
            _text2FontWeightComboBox.Items.Add(weight);
            _text3FontWeightComboBox.Items.Add(weight);
            _timeFontWeightComboBox.Items.Add(weight);
            _text4FontWeightComboBox.Items.Add(weight);
        }

        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("ClassIsland时间");

        _progressDisplayModeComboBox.Items.Add("不显示");
        _progressDisplayModeComboBox.Items.Add("进度条");
        _progressDisplayModeComboBox.Items.Add("进度环");
        _progressDisplayModeComboBox.Items.Add("进度条和进度环");

        for (int i = 1; i <= 12; i++) _startMonthComboBox.Items.Add($"{i}月");
        for (int i = 1; i <= 31; i++) _startDayComboBox.Items.Add($"{i}日");
        for (int i = 0; i < 24; i++) _startHourComboBox.Items.Add(i.ToString("D2"));
        for (int i = 0; i < 60; i++) _startMinuteComboBox.Items.Add(i.ToString("D2"));
        for (int i = 0; i < 60; i++) _startSecondComboBox.Items.Add(i.ToString("D2"));

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startYearTextBox, (s, e) => UpdateDayComboBox(_startYearTextBox, _startMonthComboBox, _startDayComboBox));
        _startMonthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(_startYearTextBox, _startMonthComboBox, _startDayComboBox);

        // SettingsCard 无内置事件，通过 IsOn 依赖属性变化订阅开关切换
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(TimeCorrectionCard, (s, e) =>
            {
                Settings.EnableTimeCorrection = TimeCorrectionCard.IsOn;
            });
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(EnableCustomProgressColorCard, (s, e) =>
            {
                Settings.EnableCustomProgressColor = EnableCustomProgressColorCard.IsOn;
                UpdateProgressColorControlsEnabled();
            });
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(SimpleModeCard, (s, e) =>
            {
                Settings.EnableSimpleMode = SimpleModeCard.IsOn;
            });
    }

    private void OnText2ButtonClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
        }
        else
        {
            _countdownListBox.BringIntoView();
            ShowHint();
        }
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

    private void OnText2EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text2EnableCustomFontSize = _text2EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText2EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text2EnableCustomFontColor = _text2EnableCustomFontColorToggle.IsChecked ?? false;
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

    private void OnText4EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontSize = _text4EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontColor = _text4EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontFamily = _text1EnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText2EnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text2EnableCustomFontFamily = _text2EnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontFamily = _text3EnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontFamily = _timeEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontFamily = _text4EnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontWeight = _text1EnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText2EnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text2EnableCustomFontWeight = _text2EnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontWeight = _text3EnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontWeight = _timeEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontWeight = _text4EnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _text1FontFamilyComboBox.IsEnabled = Settings.Text1EnableCustomFontFamily;
        _text1FontWeightComboBox.IsEnabled = Settings.Text1EnableCustomFontWeight;
        _text2FontSizeNumericUpDown.IsEnabled = Settings.Text2EnableCustomFontSize;
        _text2FontColorPicker.IsEnabled = Settings.Text2EnableCustomFontColor;
        _text2FontFamilyComboBox.IsEnabled = Settings.Text2EnableCustomFontFamily;
        _text2FontWeightComboBox.IsEnabled = Settings.Text2EnableCustomFontWeight;
        _text3FontSizeNumericUpDown.IsEnabled = Settings.Text3EnableCustomFontSize;
        _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        _text3FontFamilyComboBox.IsEnabled = Settings.Text3EnableCustomFontFamily;
        _text3FontWeightComboBox.IsEnabled = Settings.Text3EnableCustomFontWeight;
        _timeFontSizeNumericUpDown.IsEnabled = Settings.TimeEnableCustomFontSize;
        _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
        _timeFontFamilyComboBox.IsEnabled = Settings.TimeEnableCustomFontFamily;
        _timeFontWeightComboBox.IsEnabled = Settings.TimeEnableCustomFontWeight;
        _text4FontSizeNumericUpDown.IsEnabled = Settings.Text4EnableCustomFontSize;
        _text4FontColorPicker.IsEnabled = Settings.Text4EnableCustomFontColor;
        _text4FontFamilyComboBox.IsEnabled = Settings.Text4EnableCustomFontFamily;
        _text4FontWeightComboBox.IsEnabled = Settings.Text4EnableCustomFontWeight;
    }

    private void UpdateProgressColorControlsEnabled()
    {
        var isCustomEnabled = Settings.EnableCustomProgressColor;
        var showProgressBar = Settings.ProgressDisplayMode == ProgressDisplayMode.Bar ||
                              Settings.ProgressDisplayMode == ProgressDisplayMode.Both;

        _progressBarColorPicker.IsEnabled = isCustomEnabled && showProgressBar;
        _progressRingColorPicker.IsEnabled = isCustomEnabled;
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

        _text1TextBox.Text = Settings.Text1;
        _text3TextBox.Text = Settings.Text3;
        _text4TextBox.Text = Settings.Text4;
        _timeFormatTextBox.Text = Settings.TimeFormat;
        TimeCorrectionCard.IsOn = Settings.EnableTimeCorrection;

        // 迁移旧版时间基准值
        var migratedType = TimeBaseTypeHelper.Migrate((int)Settings.TimeBaseType);
        if (migratedType != Settings.TimeBaseType)
        {
            Settings.TimeBaseType = migratedType;
        }

        _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => 0,
            TimeBaseType.RawServerTime => 1,
            TimeBaseType.ClassIslandTime => 2,
            _ => 0
        };

        _progressDisplayModeComboBox.SelectedIndex = (int)Settings.ProgressDisplayMode;
        SimpleModeCard.IsOn = Settings.EnableSimpleMode;

        _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        _text2FontSizeNumericUpDown.Value = (decimal)Settings.Text2FontSize;
        _text2FontColorPicker.Color = ParseColor(Settings.Text2FontColor);
        _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        _text4FontSizeNumericUpDown.Value = (decimal)Settings.Text4FontSize;
        _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);
        _text1FontFamilyComboBox.SelectedItem = Settings.Text1FontFamily;
        _text2FontFamilyComboBox.SelectedItem = Settings.Text2FontFamily;
        _text3FontFamilyComboBox.SelectedItem = Settings.Text3FontFamily;
        _timeFontFamilyComboBox.SelectedItem = Settings.TimeFontFamily;
        _text4FontFamilyComboBox.SelectedItem = Settings.Text4FontFamily;
        _text1FontWeightComboBox.SelectedItem = Settings.Text1FontWeight;
        _text2FontWeightComboBox.SelectedItem = Settings.Text2FontWeight;
        _text3FontWeightComboBox.SelectedItem = Settings.Text3FontWeight;
        _timeFontWeightComboBox.SelectedItem = Settings.TimeFontWeight;
        _text4FontWeightComboBox.SelectedItem = Settings.Text4FontWeight;

        var startTime = UnixTimeHelper.FromUnixTimestamp(Settings.StartTime);
        _startYearTextBox.Text = startTime.Year.ToString();
        _startMonthComboBox.SelectedIndex = startTime.Month - 1;
        _startDayComboBox.SelectedItem = $"{startTime.Day}日";
        _startHourComboBox.SelectedIndex = startTime.Hour;
        _startMinuteComboBox.SelectedIndex = startTime.Minute;
        _startSecondComboBox.SelectedIndex = startTime.Second;

        UpdateCountdownList();

        AttachEventHandlers();

        _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        _text2EnableCustomFontSizeToggle.IsChecked = Settings.Text2EnableCustomFontSize;
        _text2EnableCustomFontColorToggle.IsChecked = Settings.Text2EnableCustomFontColor;
        _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;
        _text4EnableCustomFontSizeToggle.IsChecked = Settings.Text4EnableCustomFontSize;
        _text4EnableCustomFontColorToggle.IsChecked = Settings.Text4EnableCustomFontColor;
        _text1EnableCustomFontFamilyToggle.IsChecked = Settings.Text1EnableCustomFontFamily;
        _text2EnableCustomFontFamilyToggle.IsChecked = Settings.Text2EnableCustomFontFamily;
        _text3EnableCustomFontFamilyToggle.IsChecked = Settings.Text3EnableCustomFontFamily;
        _timeEnableCustomFontFamilyToggle.IsChecked = Settings.TimeEnableCustomFontFamily;
        _text4EnableCustomFontFamilyToggle.IsChecked = Settings.Text4EnableCustomFontFamily;
        _text1EnableCustomFontWeightToggle.IsChecked = Settings.Text1EnableCustomFontWeight;
        _text2EnableCustomFontWeightToggle.IsChecked = Settings.Text2EnableCustomFontWeight;
        _text3EnableCustomFontWeightToggle.IsChecked = Settings.Text3EnableCustomFontWeight;
        _timeEnableCustomFontWeightToggle.IsChecked = Settings.TimeEnableCustomFontWeight;
        _text4EnableCustomFontWeightToggle.IsChecked = Settings.Text4EnableCustomFontWeight;
        UpdateControlsEnabled();

        EnableCustomProgressColorCard.IsOn = Settings.EnableCustomProgressColor;
        UpdateProgressColorControlsEnabled();
        _progressBarColorPicker.Color = ParseColor(Settings.ProgressBarColor);
        _progressRingColorPicker.Color = ParseColor(Settings.ProgressRingColor);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _hintTimer?.Stop();
        _hintTimer?.Dispose();
    }

    private void AttachEventHandlers()
    {
        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "距离");
        AttachTextHandler(_text3TextBox, v => Settings.Text3 = v ?? "还有");
        AttachTextHandler(_text4TextBox, v => Settings.Text4 = v ?? "");
        AttachTextHandler(_timeFormatTextBox, v => Settings.TimeFormat = v ?? "%d天%h小时%m分钟%s秒");
        AttachStartTimeHandlers();
    }

    private void AttachStartTimeHandlers()
    {
        void UpdateStartTime()
        {
            if (int.TryParse(_startYearTextBox.Text?.Trim(), out var year) &&
                _startMonthComboBox.SelectedIndex >= 0 &&
                _startDayComboBox.SelectedItem != null &&
                int.TryParse(_startDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) &&
                _startHourComboBox.SelectedIndex >= 0 &&
                _startMinuteComboBox.SelectedIndex >= 0 &&
                _startSecondComboBox.SelectedIndex >= 0)
            {
                var month = _startMonthComboBox.SelectedIndex + 1;
                try
                {
                    var startTime = new DateTime(year, month, day, _startHourComboBox.SelectedIndex,
                        _startMinuteComboBox.SelectedIndex, _startSecondComboBox.SelectedIndex);
                    Settings.StartTime = UnixTimeHelper.ToUnixTimestamp(startTime);
                }
                catch { }
            }
        }

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startYearTextBox, (s, e) => UpdateStartTime());
        _startMonthComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        _startDayComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        _startHourComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        _startMinuteComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        _startSecondComboBox.SelectionChanged += (s, e) => UpdateStartTime();
    }

    private void AttachTextHandler(TextBox textBox, Action<string?> setter)
    {
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => setter(textBox.Text));
    }

    private void OnTimeBaseChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_timeBaseComboBox.SelectedIndex >= 0)
        {
            Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
            {
                0 => TimeBaseType.PluginOffsetServerTime,
                1 => TimeBaseType.RawServerTime,
                2 => TimeBaseType.ClassIslandTime,
                _ => TimeBaseType.PluginOffsetServerTime
            };
        }
    }

    private void OnProgressDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_progressDisplayModeComboBox.SelectedIndex >= 0)
        {
            Settings.ProgressDisplayMode = (ProgressDisplayMode)_progressDisplayModeComboBox.SelectedIndex;
            UpdateProgressColorControlsEnabled();
        }
    }

    private void OnText1FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text1FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text1FontSize = (double)_text1FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText1ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text1FontColor = _text1FontColorPicker.Color.ToString();
    }

    private void OnText2FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text2FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text2FontSize = (double)_text2FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText2ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text2FontColor = _text2FontColorPicker.Color.ToString();
    }

    private void OnText3FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text3FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text3FontSize = (double)_text3FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText3ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text3FontColor = _text3FontColorPicker.Color.ToString();
    }

    private void OnTimeFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_timeFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.TimeFontSize = (double)_timeFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.TimeFontColor = _timeFontColorPicker.Color.ToString();
    }

    private void OnText4FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text4FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text4FontSize = (double)_text4FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText4ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text4FontColor = _text4FontColorPicker.Color.ToString();
    }

    private void OnText1FontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text1FontFamilyComboBox.SelectedItem != null)
        {
            Settings.Text1FontFamily = _text1FontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnText1FontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text1FontWeightComboBox.SelectedItem != null)
        {
            Settings.Text1FontWeight = _text1FontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnText2FontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text2FontFamilyComboBox.SelectedItem != null)
        {
            Settings.Text2FontFamily = _text2FontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnText2FontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text2FontWeightComboBox.SelectedItem != null)
        {
            Settings.Text2FontWeight = _text2FontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnText3FontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text3FontFamilyComboBox.SelectedItem != null)
        {
            Settings.Text3FontFamily = _text3FontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnText3FontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text3FontWeightComboBox.SelectedItem != null)
        {
            Settings.Text3FontWeight = _text3FontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnTimeFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_timeFontFamilyComboBox.SelectedItem != null)
        {
            Settings.TimeFontFamily = _timeFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnTimeFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_timeFontWeightComboBox.SelectedItem != null)
        {
            Settings.TimeFontWeight = _timeFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnText4FontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text4FontFamilyComboBox.SelectedItem != null)
        {
            Settings.Text4FontFamily = _text4FontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnText4FontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_text4FontWeightComboBox.SelectedItem != null)
        {
            Settings.Text4FontWeight = _text4FontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnProgressBarColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ProgressBarColor = _progressBarColorPicker.Color.ToString();
    }

    private void OnProgressRingColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ProgressRingColor = _progressRingColorPicker.Color.ToString();
    }

    private void OnCountdownListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_countdownListBox.SelectedIndex >= 0)
        {
            HideHint();
        }
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

    private void UpdateCountdownList()
    {
        _countdownListBox.Items.Clear();
        if (Settings.CountdownItems != null)
        {
            foreach (var item in Settings.CountdownItems)
            {
                var targetTime = UnixTimeHelper.FromUnixTimestamp(item.TargetTimestamp);

                var container = new Grid();
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                container.Tag = item;

                var textBlock = new TextBlock
                {
                    Text = $"{item.Name} - {targetTime:yyyy-MM-dd HH:mm:ss}",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetTextBrush(),
                    Padding = new Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(textBlock, 0);
                container.Children.Add(textBlock);

                var notifyPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                var notifySwitch = new CheckBox
                {
                    IsChecked = item.EnableNotification,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                var currentItem = item;
                notifySwitch.Checked += (s, e) =>
                {
                    currentItem.EnableNotification = notifySwitch.IsChecked == true;
                };
                notifySwitch.Unchecked += (s, e) =>
                {
                    currentItem.EnableNotification = notifySwitch.IsChecked == true;
                };
                notifyPanel.Children.Add(notifySwitch);
                Grid.SetColumn(notifyPanel, 1);
                container.Children.Add(notifyPanel);

                var listBoxItem = new ListBoxItem
                {
                    Content = container,
                    Tag = item
                };

                _countdownListBox.Items.Add(listBoxItem);
            }
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        HideHint();
        if (Settings.CountdownItems == null)
        {
            Settings.CountdownItems = new List<CountdownItem>();
        }
        Settings.CountdownItems.Add(CountdownItem.CreateDefault());
        UpdateCountdownList();
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            Settings.CountdownItems.RemoveAt(_countdownListBox.SelectedIndex);
            UpdateCountdownList();
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void ShowHint()
    {
        _selectionHintTextBlock.Visibility = Visibility.Visible;
        _hintTimer?.Stop();
        _hintTimer = new System.Timers.Timer(5000);
        _hintTimer.Elapsed += (s, e) =>
        {
            UIThread.Post(HideHint);
        };
        _hintTimer.AutoReset = false;
        _hintTimer.Start();
    }

    private void HideHint()
    {
        _selectionHintTextBlock.Visibility = Visibility.Collapsed;
        _hintTimer?.Stop();
    }

    private async void ShowEditDialog(CountdownItem item, int order = 0)
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", order > 0 ? $"正在编写第{order}个倒计时" : "编辑倒计时");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "SecondaryButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical };

        var nameLabel = new TextBlock { Text = "名称:", Foreground = ThemeHelper.GetTextBrush() };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var targetLabel = new TextBlock { Text = "目标时间:", Foreground = ThemeHelper.GetTextBrush() };
        contentPanel.Children.Add(targetLabel);

        var targetTime = UnixTimeHelper.FromUnixTimestamp(item.TargetTimestamp);

        var datePanel = new StackPanel { Orientation = Orientation.Horizontal };
        var yearTextBox = new TextBox { Width = 80, Text = targetTime.Year.ToString() };
        var monthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthComboBox.Items.Add($"{i}月");
        monthComboBox.SelectedIndex = targetTime.Month - 1;
        var dayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) dayComboBox.Items.Add($"{i}日");
        dayComboBox.SelectedItem = $"{targetTime.Day}日";

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(yearTextBox, (s, e) => UpdateDayComboBox(yearTextBox, monthComboBox, dayComboBox));
        monthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(yearTextBox, monthComboBox, dayComboBox);

        datePanel.Children.Add(yearTextBox);
        datePanel.Children.Add(monthComboBox);
        datePanel.Children.Add(dayComboBox);
        contentPanel.Children.Add(datePanel);

        var timePanel = new StackPanel { Orientation = Orientation.Horizontal };
        var hourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) hourComboBox.Items.Add(i.ToString("D2"));
        hourComboBox.SelectedIndex = targetTime.Hour;
        var minuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) minuteComboBox.Items.Add(i.ToString("D2"));
        minuteComboBox.SelectedIndex = targetTime.Minute;
        var secondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) secondComboBox.Items.Add(i.ToString("D2"));
        secondComboBox.SelectedIndex = targetTime.Second;

        timePanel.Children.Add(hourComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(minuteComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(secondComboBox);
        contentPanel.Children.Add(timePanel);

        var notifyToggle = new CheckBox { Content = "启用通知", IsChecked = item.EnableNotification };
        contentPanel.Children.Add(notifyToggle);

        var notifyTitleLabel = new TextBlock { Text = "通知标题:", Foreground = ThemeHelper.GetTextBrush() };
        var notifyTitleTextBox = new TextBox { Text = item.NotificationTitle };
        contentPanel.Children.Add(notifyTitleLabel);
        contentPanel.Children.Add(notifyTitleTextBox);

        var notifyContentLabel = new TextBlock { Text = "通知内容:", Foreground = ThemeHelper.GetTextBrush() };
        var notifyContentTextBox = new TextBox { Text = item.NotificationContent };
        contentPanel.Children.Add(notifyContentLabel);
        contentPanel.Children.Add(notifyContentTextBox);

        var maskDurationLabel = new TextBlock { Text = "通知标题时长(秒):", Foreground = ThemeHelper.GetTextBrush() };
        var maskDurationTextBox = new TextBox { Text = item.NotificationMaskDurationSeconds.ToString() };
        contentPanel.Children.Add(maskDurationLabel);
        contentPanel.Children.Add(maskDurationTextBox);

        var overlayDurationLabel = new TextBlock { Text = "通知内容时长(秒):", Foreground = ThemeHelper.GetTextBrush() };
        var overlayDurationTextBox = new TextBox { Text = item.NotificationOverlayDurationSeconds.ToString() };
        contentPanel.Children.Add(overlayDurationLabel);
        contentPanel.Children.Add(overlayDurationTextBox);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = contentPanel,
            Margin = new Thickness(12, 12, 12, 0)
        };

        var mainPanel = new Grid();
        mainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(scrollViewer, 0);
        mainPanel.Children.Add(scrollViewer);

        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", mainPanel);

        FluentAvaloniaCompatibilityHelper.AddContentDialogButtonClickHandler(dialog, "PrimaryButtonClick", (s, e) =>
        {
            item.Name = nameTextBox.Text ?? "新倒计时";

            if (int.TryParse(yearTextBox.Text?.Trim(), out var year) &&
                monthComboBox.SelectedIndex >= 0 &&
                dayComboBox.SelectedItem != null &&
                int.TryParse(dayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) &&
                hourComboBox.SelectedIndex >= 0 &&
                minuteComboBox.SelectedIndex >= 0 &&
                secondComboBox.SelectedIndex >= 0)
            {
                try
                {
                    var target = new DateTime(year, monthComboBox.SelectedIndex + 1, day,
                        hourComboBox.SelectedIndex, minuteComboBox.SelectedIndex, secondComboBox.SelectedIndex);
                    item.TargetTimestamp = UnixTimeHelper.ToUnixTimestamp(target);
                }
                catch { }
            }

            item.EnableNotification = notifyToggle.IsChecked == true;
            item.NotificationTitle = notifyTitleTextBox.Text ?? "倒计时到达";
            item.NotificationContent = notifyContentTextBox.Text ?? "目标时间已到达！";

            if (int.TryParse(maskDurationTextBox.Text, out int maskDuration))
            {
                item.NotificationMaskDurationSeconds = maskDuration;
            }

            if (int.TryParse(overlayDurationTextBox.Text, out int overlayDuration))
            {
                item.NotificationOverlayDurationSeconds = overlayDuration;
            }

            item.IsCompleted = false;
            UpdateCountdownList();
        });

        await FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, Window.GetWindow(this));
    }

    private static void UpdateDayComboBox(TextBox yearTextBox, ComboBox monthComboBox, ComboBox dayComboBox)
    {
        if (!int.TryParse(yearTextBox.Text?.Trim(), out var year))
            return;
        if (monthComboBox.SelectedItem == null)
            return;
        if (!int.TryParse(monthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return;

        var selectedDayText = dayComboBox.SelectedItem?.ToString();
        int? selectedDay = null;
        if (selectedDayText != null && int.TryParse(selectedDayText.Replace("日", ""), out var d))
            selectedDay = d;

        dayComboBox.Items.Clear();

        if (year == 1582 && month == 10)
        {
            for (int i = 1; i <= 4; i++)
            {
                dayComboBox.Items.Add($"{i}日");
            }
            for (int i = 15; i <= 31; i++)
            {
                dayComboBox.Items.Add($"{i}日");
            }
        }
        else
        {
            var daysInMonth = GetDaysInMonth(year, month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                dayComboBox.Items.Add($"{i}日");
            }
        }

        if (selectedDay.HasValue)
        {
            var safeDay = Math.Min(selectedDay.Value, dayComboBox.Items.Count);
            if (safeDay > 0)
            {
                dayComboBox.SelectedItem = $"{safeDay}日";
            }
            else
            {
                dayComboBox.SelectedIndex = -1;
            }
        }
        else
        {
            dayComboBox.SelectedIndex = -1;
        }
    }

    private static int GetDaysInMonth(int year, int month)
    {
        if (year > 1582)
        {
            return Lunar.Util.SolarUtil.GetDaysOfMonth(year, month);
        }

        if (year == 1582 && month == 10)
        {
            return 21;
        }

        if (month == 2)
        {
            if (IsJulianLeapYear(year))
                return 29;
            return 28;
        }

        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        return 31;
    }

    private static bool IsJulianLeapYear(int year)
    {
        return year % 4 == 0;
    }
}
