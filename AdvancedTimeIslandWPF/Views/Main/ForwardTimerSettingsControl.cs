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

public partial class ForwardTimerSettingsControl : ComponentBase<ForwardTimerSettings>
{
    private TextBox _text1TextBox = null!;
    private TextBox _nameTextBox = null!;
    private TextBox _text3TextBox = null!;
    private TextBox _text4TextBox = null!;
    private TextBox _timeFormatTextBox = null!;
    private ComboBox _timeBaseComboBox = null!;
    private CheckBox _text1EnableCustomFontSizeToggle = null!;
    private CheckBox _text1EnableCustomFontColorToggle = null!;
    private CheckBox _nameEnableCustomFontSizeToggle = null!;
    private CheckBox _nameEnableCustomFontColorToggle = null!;
    private CheckBox _text3EnableCustomFontSizeToggle = null!;
    private CheckBox _text3EnableCustomFontColorToggle = null!;
    private CheckBox _timeEnableCustomFontSizeToggle = null!;
    private CheckBox _timeEnableCustomFontColorToggle = null!;
    private CheckBox _text4EnableCustomFontSizeToggle = null!;
    private CheckBox _text4EnableCustomFontColorToggle = null!;
    private CheckBox _text1EnableCustomFontFamilyToggle = null!;
    private CheckBox _nameEnableCustomFontFamilyToggle = null!;
    private CheckBox _text3EnableCustomFontFamilyToggle = null!;
    private CheckBox _timeEnableCustomFontFamilyToggle = null!;
    private CheckBox _text4EnableCustomFontFamilyToggle = null!;
    private CheckBox _text1EnableCustomFontWeightToggle = null!;
    private CheckBox _nameEnableCustomFontWeightToggle = null!;
    private CheckBox _text3EnableCustomFontWeightToggle = null!;
    private CheckBox _timeEnableCustomFontWeightToggle = null!;
    private CheckBox _text4EnableCustomFontWeightToggle = null!;
    private TextBox _startYearTextBox = null!;
    private ComboBox _startMonthComboBox = null!;
    private ComboBox _startDayComboBox = null!;
    private ComboBox _startHourComboBox = null!;
    private ComboBox _startMinuteComboBox = null!;
    private ComboBox _startSecondComboBox = null!;
    private WpfNumericUpDown _text1FontSizeNumericUpDown = null!;
    private WpfColorPicker _text1FontColorPicker = null!;
    private WpfNumericUpDown _nameFontSizeNumericUpDown = null!;
    private WpfColorPicker _nameFontColorPicker = null!;
    private WpfNumericUpDown _text3FontSizeNumericUpDown = null!;
    private WpfColorPicker _text3FontColorPicker = null!;
    private WpfNumericUpDown _timeFontSizeNumericUpDown = null!;
    private WpfColorPicker _timeFontColorPicker = null!;
    private WpfNumericUpDown _text4FontSizeNumericUpDown = null!;
    private WpfColorPicker _text4FontColorPicker = null!;
    private ComboBox _text1FontFamilyComboBox = null!;
    private ComboBox _nameFontFamilyComboBox = null!;
    private ComboBox _text3FontFamilyComboBox = null!;
    private ComboBox _timeFontFamilyComboBox = null!;
    private ComboBox _text4FontFamilyComboBox = null!;
    private ComboBox _text1FontWeightComboBox = null!;
    private ComboBox _nameFontWeightComboBox = null!;
    private ComboBox _text3FontWeightComboBox = null!;
    private ComboBox _timeFontWeightComboBox = null!;
    private ComboBox _text4FontWeightComboBox = null!;

    public ForwardTimerSettingsControl()
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
        _nameTextBox = (TextBox)((StackPanel)NameItem.Switcher).Children[0];
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

        _text1FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text1FontSizeItem.Switcher).Children[0];
        _text1EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text1FontSizeItem.Switcher).Children[1];
        _text1FontColorPicker = (WpfColorPicker)((StackPanel)Text1ColorItem.Switcher).Children[0];
        _text1EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text1ColorItem.Switcher).Children[1];
        _text1FontFamilyComboBox = (ComboBox)((StackPanel)Text1FontFamilyItem.Switcher).Children[0];
        _text1EnableCustomFontFamilyToggle = (CheckBox)((StackPanel)Text1FontFamilyItem.Switcher).Children[1];
        _text1FontWeightComboBox = (ComboBox)((StackPanel)Text1FontWeightItem.Switcher).Children[0];
        _text1EnableCustomFontWeightToggle = (CheckBox)((StackPanel)Text1FontWeightItem.Switcher).Children[1];

        _nameFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)NameFontSizeItem.Switcher).Children[0];
        _nameEnableCustomFontSizeToggle = (CheckBox)((StackPanel)NameFontSizeItem.Switcher).Children[1];
        _nameFontColorPicker = (WpfColorPicker)((StackPanel)NameColorItem.Switcher).Children[0];
        _nameEnableCustomFontColorToggle = (CheckBox)((StackPanel)NameColorItem.Switcher).Children[1];
        _nameFontFamilyComboBox = (ComboBox)((StackPanel)NameFontFamilyItem.Switcher).Children[0];
        _nameEnableCustomFontFamilyToggle = (CheckBox)((StackPanel)NameFontFamilyItem.Switcher).Children[1];
        _nameFontWeightComboBox = (ComboBox)((StackPanel)NameFontWeightItem.Switcher).Children[0];
        _nameEnableCustomFontWeightToggle = (CheckBox)((StackPanel)NameFontWeightItem.Switcher).Children[1];

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
            _nameFontFamilyComboBox.Items.Add(font);
            _text3FontFamilyComboBox.Items.Add(font);
            _timeFontFamilyComboBox.Items.Add(font);
            _text4FontFamilyComboBox.Items.Add(font);
        }
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _text1FontWeightComboBox.Items.Add(weight);
            _nameFontWeightComboBox.Items.Add(weight);
            _text3FontWeightComboBox.Items.Add(weight);
            _timeFontWeightComboBox.Items.Add(weight);
            _text4FontWeightComboBox.Items.Add(weight);
        }

        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("ClassIsland时间");

        for (int i = 1; i <= 12; i++) _startMonthComboBox.Items.Add($"{i}月");
        for (int i = 1; i <= 31; i++) _startDayComboBox.Items.Add($"{i}日");
        for (int i = 0; i < 24; i++) _startHourComboBox.Items.Add(i.ToString("D2"));
        for (int i = 0; i < 60; i++) _startMinuteComboBox.Items.Add(i.ToString("D2"));
        for (int i = 0; i < 60; i++) _startSecondComboBox.Items.Add(i.ToString("D2"));

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startYearTextBox, (s, e) => UpdateDayComboBox(_startYearTextBox, _startMonthComboBox, _startDayComboBox));
        _startMonthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(_startYearTextBox, _startMonthComboBox, _startDayComboBox);
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

    private void OnNameEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.NameEnableCustomFontFamily = _nameEnableCustomFontFamilyToggle.IsChecked ?? false;
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

    private void OnNameEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.NameEnableCustomFontWeight = _nameEnableCustomFontWeightToggle.IsChecked ?? false;
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
        _nameFontSizeNumericUpDown.IsEnabled = Settings.NameEnableCustomFontSize;
        _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
        _nameFontFamilyComboBox.IsEnabled = Settings.NameEnableCustomFontFamily;
        _nameFontWeightComboBox.IsEnabled = Settings.NameEnableCustomFontWeight;
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

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        _text1TextBox.Text = Settings.Text1;
        _nameTextBox.Text = Settings.Name;
        _text3TextBox.Text = Settings.Text3;
        _text4TextBox.Text = Settings.Text4;
        _timeFormatTextBox.Text = Settings.TimeFormat;
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

        var startTime = DateTimeOffset.FromUnixTimeSeconds(Settings.StartTime).LocalDateTime;
        _startYearTextBox.Text = startTime.Year.ToString();
        _startMonthComboBox.SelectedIndex = startTime.Month - 1;
        _startDayComboBox.SelectedItem = $"{startTime.Day}日";
        _startHourComboBox.SelectedIndex = startTime.Hour;
        _startMinuteComboBox.SelectedIndex = startTime.Minute;
        _startSecondComboBox.SelectedIndex = startTime.Second;

        _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        _nameFontSizeNumericUpDown.Value = (decimal)Settings.NameFontSize;
        _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        _text4FontSizeNumericUpDown.Value = (decimal)Settings.Text4FontSize;
        _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);
        _text1FontFamilyComboBox.SelectedItem = Settings.Text1FontFamily;
        _nameFontFamilyComboBox.SelectedItem = Settings.NameFontFamily;
        _text3FontFamilyComboBox.SelectedItem = Settings.Text3FontFamily;
        _timeFontFamilyComboBox.SelectedItem = Settings.TimeFontFamily;
        _text4FontFamilyComboBox.SelectedItem = Settings.Text4FontFamily;
        _text1FontWeightComboBox.SelectedItem = Settings.Text1FontWeight;
        _nameFontWeightComboBox.SelectedItem = Settings.NameFontWeight;
        _text3FontWeightComboBox.SelectedItem = Settings.Text3FontWeight;
        _timeFontWeightComboBox.SelectedItem = Settings.TimeFontWeight;
        _text4FontWeightComboBox.SelectedItem = Settings.Text4FontWeight;

        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "");
        AttachTextHandler(_nameTextBox, v => Settings.Name = v ?? "");
        AttachTextHandler(_text3TextBox, v => Settings.Text3 = v ?? "已过");
        AttachTextHandler(_text4TextBox, v => Settings.Text4 = v ?? "");
        AttachTextHandler(_timeFormatTextBox, v => Settings.TimeFormat = v ?? "%d天%h小时%m分钟%s秒");

        AttachDateTimeHandlers();

        _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        _nameEnableCustomFontSizeToggle.IsChecked = Settings.NameEnableCustomFontSize;
        _nameEnableCustomFontColorToggle.IsChecked = Settings.NameEnableCustomFontColor;
        _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;
        _text4EnableCustomFontSizeToggle.IsChecked = Settings.Text4EnableCustomFontSize;
        _text4EnableCustomFontColorToggle.IsChecked = Settings.Text4EnableCustomFontColor;
        _text1EnableCustomFontFamilyToggle.IsChecked = Settings.Text1EnableCustomFontFamily;
        _nameEnableCustomFontFamilyToggle.IsChecked = Settings.NameEnableCustomFontFamily;
        _text3EnableCustomFontFamilyToggle.IsChecked = Settings.Text3EnableCustomFontFamily;
        _timeEnableCustomFontFamilyToggle.IsChecked = Settings.TimeEnableCustomFontFamily;
        _text4EnableCustomFontFamilyToggle.IsChecked = Settings.Text4EnableCustomFontFamily;
        _text1EnableCustomFontWeightToggle.IsChecked = Settings.Text1EnableCustomFontWeight;
        _nameEnableCustomFontWeightToggle.IsChecked = Settings.NameEnableCustomFontWeight;
        _text3EnableCustomFontWeightToggle.IsChecked = Settings.Text3EnableCustomFontWeight;
        _timeEnableCustomFontWeightToggle.IsChecked = Settings.TimeEnableCustomFontWeight;
        _text4EnableCustomFontWeightToggle.IsChecked = Settings.Text4EnableCustomFontWeight;
        UpdateControlsEnabled();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
    }

    private void AttachTextHandler(TextBox textBox, Action<string?> handler)
    {
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e2) =>
        {
            handler(textBox.Text);
        });
    }

    private void AttachDateTimeHandlers()
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
                    var startTime = DateValidationHelper.FixInvalidDate(year, month, day,
                        _startHourComboBox.SelectedIndex,
                        _startMinuteComboBox.SelectedIndex,
                        _startSecondComboBox.SelectedIndex);
                    Settings.StartTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
                }
                catch { }
            }
        }

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startYearTextBox, (s, e2) => UpdateStartTime());
        _startMonthComboBox.SelectionChanged += (s, e2) => UpdateStartTime();
        _startDayComboBox.SelectionChanged += (s, e2) => UpdateStartTime();
        _startHourComboBox.SelectionChanged += (s, e2) => UpdateStartTime();
        _startMinuteComboBox.SelectionChanged += (s, e2) => UpdateStartTime();
        _startSecondComboBox.SelectionChanged += (s, e2) => UpdateStartTime();
    }

    private void OnTimeBaseChanged(object? sender, SelectionChangedEventArgs e)
    {
        Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
        {
            0 => TimeBaseType.PluginOffsetServerTime,
            1 => TimeBaseType.RawServerTime,
            2 => TimeBaseType.ClassIslandTime,
            _ => TimeBaseType.PluginOffsetServerTime
        };
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

    private void OnNameFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_nameFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.NameFontSize = (double)_nameFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnNameColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.NameFontColor = _nameFontColorPicker.Color.ToString();
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

    private void OnNameFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_nameFontFamilyComboBox.SelectedItem != null)
        {
            Settings.NameFontFamily = _nameFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnNameFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_nameFontWeightComboBox.SelectedItem != null)
        {
            Settings.NameFontWeight = _nameFontWeightComboBox.SelectedItem.ToString() ?? "";
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

    private Color ParseColor(string colorStr)
    {
        try
        {
            return ThemeHelper.ParseColor(colorStr);
        }
        catch
        {
            return ThemeHelper.ParseColor(ThemeHelper.GetTextColorHex());
        }
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
