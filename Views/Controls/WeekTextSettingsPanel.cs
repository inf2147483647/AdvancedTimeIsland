using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// 主界面"周数/日期"类纯文本组件的通用设置面板：
/// 由标题、描述、可选的自定义分组、时间基准与字体样式表格组成。
/// 供农历日期、本周单双、下个节假日等组件复用，避免重复编写同一套字体设置界面。
/// </summary>
public class WeekTextSettingsPanel
{
    private readonly WeekSettings _settings;

    private readonly TextBlock _titleTextBlock;
    private readonly TextBlock _descTextBlock;
    private readonly List<TextBlock> _labels = new();
    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    // 这些控件在 CreateFontStyleTable 中通过 out 参数赋值（构造函数链内），故不能声明为 readonly。
    private CheckBox _enableCustomFontSizeToggle = null!;
    private CheckBox _enableCustomFontColorToggle = null!;
    private CheckBox _enableCustomFontFamilyToggle = null!;
    private CheckBox _enableCustomFontWeightToggle = null!;
    private ColorPicker _colorPicker = null!;
    private NumericUpDown _fontSizeNumericUpDown = null!;
    private ComboBox _fontFamilyComboBox = null!;
    private ComboBox _fontWeightComboBox = null!;
    private readonly ComboBox _timeBaseComboBox;

    private static readonly object[] TimeBaseItems =
    {
        "插件偏移后的服务器时间", "原始服务器时间", "ClassIsland时间"
    };

    /// <summary>
    /// 设置面板根控件。
    /// </summary>
    public Control Root { get; }

    /// <param name="settings">组件设置。</param>
    /// <param name="title">面板标题。</param>
    /// <param name="description">面板描述。</param>
    /// <param name="extraGroups">插在时间设置之前的分组控件（可为空）。</param>
    public WeekTextSettingsPanel(WeekSettings settings, string title, string description,
        params Control[] extraGroups)
    {
        _settings = settings;

        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock
        {
            Text = description,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        sp.Children.Add(_descTextBlock);

        foreach (var group in extraGroups)
        {
            sp.Children.Add(group);
        }

        // ==================== 时间设置 ====================
        var timePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var timeBaseRow = FontSettingsRowFactory.CreateComboBoxRow("时间基准", TimeBaseItems,
            out var timeBaseLabel, out _timeBaseComboBox, out _,
            toggleContent: null,
            selectionChangedHandler: OnTimeBaseChanged);
        _labels.Add(timeBaseLabel);
        timePanel.Children.Add(timeBaseRow);

        sp.Children.Add(SettingsGroupFactory.Create("时间设置", timePanel));

        // ==================== 文案设置 ====================
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var tableScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateFontStyleTable()
        };
        textPanel.Children.Add(tableScroll);
        textPanel.Children.Add(FontSettingsRowFactory.CreateFontWeightHintTextBlock());

        sp.Children.Add(SettingsGroupFactory.Create("文案设置", textPanel));

        Root = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        UpdateThemeColors();
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

    private WeekSettings Settings => _settings;

    /// <summary>
    /// 面板从可视树移除时调用，释放主题事件订阅。
    /// </summary>
    public void Detach()
    {
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    // ==================== 字体样式表格 ====================

    private Grid CreateFontStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        for (var i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (var i = 0; i < 2; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");
        AddTableHeader(grid, 0, 3, "自定义字体");
        AddTableHeader(grid, 0, 4, "自定义字重");

        AddTableRowLabel(grid, 1, "文本");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _fontSizeNumericUpDown, out _enableCustomFontSizeToggle, OnEnableCustomFontSizeChanged, OnFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _colorPicker, out _enableCustomFontColorToggle, OnEnableCustomFontColorChanged, OnColorChanged));
        AddTableCell(grid, 1, 3, CreateFamilyCell(out _fontFamilyComboBox, out _enableCustomFontFamilyToggle, OnEnableCustomFontFamilyChanged, OnFontFamilyChanged));
        AddTableCell(grid, 1, 4, CreateWeightCell(out _fontWeightComboBox, out _enableCustomFontWeightToggle, OnEnableCustomFontWeightChanged, OnFontWeightChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 2);
        Grid.SetColumnSpan(outerBorder, 5);
        _tableCellBorders.Add(outerBorder);
        grid.Children.Add(outerBorder);

        return grid;
    }

    private Border CreateCellBorder(Control child)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            Padding = new Thickness(6, 3, 6, 3),
            Child = child
        };
        _tableCellBorders.Add(border);
        return border;
    }

    private void AddTableHeader(Grid grid, int row, int col, string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        _dynamicTextBlocks.Add(tb);
        var border = CreateCellBorder(tb);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private void AddTableRowLabel(Grid grid, int row, string text)
    {
        var tb = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(tb);
        var border = CreateCellBorder(tb);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, 0);
        grid.Children.Add(border);
    }

    private void AddTableCell(Grid grid, int row, int col, Control control)
    {
        var border = CreateCellBorder(control);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private static StackPanel CreateSizeCell(out NumericUpDown numericUpDown, out CheckBox toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        numericUpDown = new NumericUpDown
        {
            Width = 120,
            Minimum = 1,
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            VerticalAlignment = VerticalAlignment.Center
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        panel.Children.Add(numericUpDown);
        return panel;
    }

    private static StackPanel CreateColorCell(out ColorPicker colorPicker, out CheckBox toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<ColorChangedEventArgs> colorChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        colorPicker = new ColorPicker { Width = 120, VerticalAlignment = VerticalAlignment.Center };
        colorPicker.ColorChanged += colorChangedHandler;
        panel.Children.Add(colorPicker);
        return panel;
    }

    private static StackPanel CreateFamilyCell(out ComboBox comboBox, out CheckBox toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        comboBox = new ComboBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            comboBox.Items.Add(font);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        panel.Children.Add(comboBox);
        return panel;
    }

    private static StackPanel CreateWeightCell(out ComboBox comboBox, out CheckBox toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        comboBox = new ComboBox { Width = 110, VerticalAlignment = VerticalAlignment.Center };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            comboBox.Items.Add(weight);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        panel.Children.Add(comboBox);
        return panel;
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

    private void UpdateControlsEnabled()
    {
        _colorPicker.IsEnabled = Settings.EnableCustomFontColor;
        _fontSizeNumericUpDown.IsEnabled = Settings.EnableCustomFontSize;
        _fontFamilyComboBox.IsEnabled = Settings.EnableCustomFontFamily;
        _fontWeightComboBox.IsEnabled = Settings.EnableCustomFontWeight;
    }

    private void UpdateThemeColors()
    {
        var textBrush = ThemeHelper.GetTextBrush();
        foreach (var label in _labels)
        {
            label.Foreground = textBrush;
        }
        _enableCustomFontSizeToggle.Foreground = textBrush;
        _enableCustomFontColorToggle.Foreground = textBrush;
        _enableCustomFontFamilyToggle.Foreground = textBrush;
        _enableCustomFontWeightToggle.Foreground = textBrush;
        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = textBrush;
        }
        var separatorBrush = ThemeHelper.GetSeparatorBrush();
        foreach (var border in _tableCellBorders)
        {
            border.BorderBrush = separatorBrush;
        }
        _titleTextBlock.Foreground = textBrush;
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return Color.Parse(colorString);
        }
        catch
        {
            return Color.Parse(ThemeHelper.GetTextColorHex());
        }
    }
}
