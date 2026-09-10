using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
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

public class TimeZoneTimeSettingsControl : ComponentBase<TimeZoneTimeSettings>
{
    private ComboBox _timeZoneComboBox;
    private CheckBox _enableCustomFontSizeToggle;
    private CheckBox _enableCustomFontColorToggle;
    private CheckBox _enableCustomFontFamilyToggle;
    private CheckBox _enableCustomFontWeightToggle;
    private ColorPicker _colorPicker;
    private NumericUpDown _fontSizeNumericUpDown;
    private ComboBox _fontFamilyComboBox;
    private ComboBox _fontWeightComboBox;

    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;

    public TimeZoneTimeSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "时区设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "选择时区", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_descTextBlock);

        // ==================== 时间设置 ====================
        var timePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _timeZoneComboBox = new ComboBox { Width = 250, HorizontalAlignment = HorizontalAlignment.Left };
        var tzs = TimeZoneInfo.GetSystemTimeZones();
        foreach (var tz in tzs) _timeZoneComboBox.Items.Add(tz);
        _timeZoneComboBox.SelectionChanged += OnTimeZoneSelectionChanged;
        timePanel.Children.Add(_timeZoneComboBox);

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
        for (int i = 0; i < 2; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");
        AddTableHeader(grid, 0, 3, "自定义字体");
        AddTableHeader(grid, 0, 4, "自定义字重");

        AddTableRowLabel(grid, 1, "时区时间");
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
        _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _enableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _enableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _enableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _enableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();

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

    private void OnEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontColor = _enableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontFamily = _enableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontWeight = _enableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_fontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _fontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnFontWeightChanged(object? sender, EventArgs e)
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

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
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
            return Color.Parse(colorString);
        }
        catch
        {
            return Color.Parse(ThemeHelper.GetTextColorHex());
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