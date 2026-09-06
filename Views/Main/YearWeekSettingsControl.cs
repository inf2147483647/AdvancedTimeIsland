using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Views.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class YearWeekSettingsControl : ComponentBase<YearWeekSettings>
{
    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;
    private readonly List<TextBlock> _labels = new();
    private readonly List<ToggleSwitch> _toggles = new();

    private ToggleSwitch _enableCustomFontSizeToggle;
    private ToggleSwitch _enableCustomFontColorToggle;
    private ToggleSwitch _enableCustomFontFamilyToggle;
    private ToggleSwitch _enableCustomFontWeightToggle;
    private ColorPicker _colorPicker;
    private NumericUpDown _fontSizeNumericUpDown;
    private ComboBox _fontFamilyComboBox;
    private ComboBox _fontWeightComboBox;
    private ComboBox _firstDayOfWeekComboBox;
    private ComboBox _timeBaseComboBox;

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
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "今日周数（年）设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "显示当前周所处的年份周数，形如\"第 N 周\"。可自定义字体颜色、大小、样式与字重。", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_descTextBlock);

        var firstDayRow = FontSettingsRowFactory.CreateComboBoxRow("一周的第一天", FirstDayOfWeekItems,
            out var firstDayLabel, out _firstDayOfWeekComboBox, out var firstDayToggle,
            toggleContent: null,
            selectionChangedHandler: OnFirstDayOfWeekChanged);
        _labels.Add(firstDayLabel);
        sp.Children.Add(firstDayRow);

        var timeBaseRow = FontSettingsRowFactory.CreateComboBoxRow("时间基准", TimeBaseItems,
            out var timeBaseLabel, out _timeBaseComboBox, out var timeBaseToggle,
            toggleContent: null,
            selectionChangedHandler: OnTimeBaseChanged);
        _labels.Add(timeBaseLabel);
        sp.Children.Add(timeBaseRow);

        var fontSizeRow = FontSettingsRowFactory.CreateFontSizeRow("文本大小",
            out var fontSizeLabel, out _fontSizeNumericUpDown, out _enableCustomFontSizeToggle,
            OnFontSizeChanged, OnEnableCustomFontSizeChanged);
        _labels.Add(fontSizeLabel);
        _toggles.Add(_enableCustomFontSizeToggle);
        sp.Children.Add(fontSizeRow);

        var colorRow = FontSettingsRowFactory.CreateColorRow("文本颜色",
            out var colorLabel, out _colorPicker, out _enableCustomFontColorToggle,
            OnColorChanged, OnEnableCustomFontColorChanged);
        _labels.Add(colorLabel);
        _toggles.Add(_enableCustomFontColorToggle);
        sp.Children.Add(colorRow);

        var familyRow = FontSettingsRowFactory.CreateFontFamilyRow("字体样式",
            out var familyLabel, out _fontFamilyComboBox, out _enableCustomFontFamilyToggle,
            OnEnableCustomFontFamilyChanged, OnFontFamilyChanged);
        _labels.Add(familyLabel);
        _toggles.Add(_enableCustomFontFamilyToggle);
        sp.Children.Add(familyRow);

        var weightRow = FontSettingsRowFactory.CreateFontWeightRow("字重",
            out var weightLabel, out _fontWeightComboBox, out _enableCustomFontWeightToggle,
            OnEnableCustomFontWeightChanged, OnFontWeightChanged);
        _labels.Add(weightLabel);
        _toggles.Add(_enableCustomFontWeightToggle);
        sp.Children.Add(weightRow);

        sp.Children.Add(FontSettingsRowFactory.CreateFontWeightHintTextBlock());

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
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

    private void UpdateThemeColors()
    {
        FontSettingsRowFactory.ApplyTheme(_labels, _toggles);
        _titleTextBlock.Foreground = AdvancedTimeIsland.Helpers.ThemeHelper.GetTextBrush();
        _descTextBlock.Foreground = AdvancedTimeIsland.Helpers.ThemeHelper.GetSubTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
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

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return Color.Parse(colorString);
        }
        catch
        {
            return Color.Parse(AdvancedTimeIsland.Helpers.ThemeHelper.GetTextColorHex());
        }
    }
}