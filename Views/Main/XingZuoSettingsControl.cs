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
using AdvancedTimeIsland.Views.Controls;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class XingZuoSettingsControl : ComponentBase<XingZuoSettings>
{
    private ColorPicker _labelColorPicker;
    private NumericUpDown _labelFontSizeNumericUpDown;
    private ColorPicker _valueColorPicker;
    private NumericUpDown _valueFontSizeNumericUpDown;
    private CheckBox _labelEnableCustomFontSizeToggle;
    private CheckBox _labelEnableCustomFontColorToggle;
    private CheckBox _valueEnableCustomFontSizeToggle;
    private CheckBox _valueEnableCustomFontColorToggle;
    private CheckBox _labelEnableCustomFontFamilyToggle;
    private CheckBox _valueEnableCustomFontFamilyToggle;
    private CheckBox _labelEnableCustomFontWeightToggle;
    private CheckBox _valueEnableCustomFontWeightToggle;
    private ComboBox _labelFontFamilyComboBox;
    private ComboBox _valueFontFamilyComboBox;
    private ComboBox _labelFontWeightComboBox;
    private ComboBox _valueFontWeightComboBox;

    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    public XingZuoSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var tableScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateFontStyleTable()
        };

        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
        textPanel.Children.Add(tableScroll);
        textPanel.Children.Add(CreateFontWeightHintTextBlock());
        sp.Children.Add(SettingsGroupFactory.Create("文案设置", textPanel));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    // ==================== 字体样式表格 ====================

    private Grid CreateFontStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (int i = 0; i < 3; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");
        AddTableHeader(grid, 0, 3, "自定义字体");
        AddTableHeader(grid, 0, 4, "自定义字重");

        // 标签
        AddTableRowLabel(grid, 1, "标签");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _labelFontSizeNumericUpDown, out _labelEnableCustomFontSizeToggle, OnLabelEnableCustomFontSizeChanged, OnLabelFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _labelColorPicker, out _labelEnableCustomFontColorToggle, OnLabelEnableCustomFontColorChanged, OnLabelColorChanged));
        AddTableCell(grid, 1, 3, CreateFamilyCell(out _labelFontFamilyComboBox, out _labelEnableCustomFontFamilyToggle, OnLabelEnableCustomFontFamilyChanged, OnLabelFontFamilyChanged));
        AddTableCell(grid, 1, 4, CreateWeightCell(out _labelFontWeightComboBox, out _labelEnableCustomFontWeightToggle, OnLabelEnableCustomFontWeightChanged, OnLabelFontWeightChanged));

        // 值
        AddTableRowLabel(grid, 2, "值");
        AddTableCell(grid, 2, 1, CreateSizeCell(out _valueFontSizeNumericUpDown, out _valueEnableCustomFontSizeToggle, OnValueEnableCustomFontSizeChanged, OnValueFontSizeChanged));
        AddTableCell(grid, 2, 2, CreateColorCell(out _valueColorPicker, out _valueEnableCustomFontColorToggle, OnValueEnableCustomFontColorChanged, OnValueColorChanged));
        AddTableCell(grid, 2, 3, CreateFamilyCell(out _valueFontFamilyComboBox, out _valueEnableCustomFontFamilyToggle, OnValueEnableCustomFontFamilyChanged, OnValueFontFamilyChanged));
        AddTableCell(grid, 2, 4, CreateWeightCell(out _valueFontWeightComboBox, out _valueEnableCustomFontWeightToggle, OnValueEnableCustomFontWeightChanged, OnValueFontWeightChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 3);
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
        var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
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

    private TextBlock CreateFontWeightHintTextBlock()
    {
        return new TextBlock
        {
            Text = "需要对应字体支持所选字重",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.Orange,
            Margin = new Thickness(0, 2, 0, 0)
        };
    }

    private void UpdateThemeColors()
    {
        _labelEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();

        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetTextBrush();
        }

        var separatorBrush = ThemeHelper.GetSeparatorBrush();
        foreach (var border in _tableCellBorders)
        {
            border.BorderBrush = separatorBrush;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void OnLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontSize = _labelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontColor = _labelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontSize = _valueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontColor = _valueEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontFamily = _labelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontFamily = _valueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontWeight = _labelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontWeight = _valueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_labelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.LabelFontFamily = _labelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnLabelFontWeightChanged(object? sender, EventArgs e)
    {
        if (_labelFontWeightComboBox.SelectedItem != null)
        {
            Settings.LabelFontWeight = _labelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnValueFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_valueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.ValueFontFamily = _valueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnValueFontWeightChanged(object? sender, EventArgs e)
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

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
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
            return Color.Parse(colorString);
        }
        catch
        {
            return Color.Parse(ThemeHelper.GetTextColorHex());
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