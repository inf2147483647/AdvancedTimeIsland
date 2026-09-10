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

public class TomorrowYiJiSettingsControl : ComponentBase<TomorrowYiJiSettings>
{
    private NumericUpDown _yiLabelFontSizeNumericUpDown;
    private ColorPicker _yiLabelFontColorPicker;
    private NumericUpDown _yiValueFontSizeNumericUpDown;
    private NumericUpDown _jiLabelFontSizeNumericUpDown;
    private ColorPicker _jiLabelFontColorPicker;
    private NumericUpDown _jiValueFontSizeNumericUpDown;
    private CheckBox _yiLabelEnableCustomFontSizeToggle;
    private CheckBox _yiLabelEnableCustomFontColorToggle;
    private CheckBox _yiLabelEnableCustomFontFamilyToggle;
    private CheckBox _yiLabelEnableCustomFontWeightToggle;
    private CheckBox _yiValueEnableCustomFontSizeToggle;
    private CheckBox _yiValueEnableCustomFontFamilyToggle;
    private CheckBox _yiValueEnableCustomFontWeightToggle;
    private CheckBox _jiLabelEnableCustomFontSizeToggle;
    private CheckBox _jiLabelEnableCustomFontColorToggle;
    private CheckBox _jiLabelEnableCustomFontFamilyToggle;
    private CheckBox _jiLabelEnableCustomFontWeightToggle;
    private CheckBox _jiValueEnableCustomFontSizeToggle;
    private CheckBox _jiValueEnableCustomFontFamilyToggle;
    private CheckBox _jiValueEnableCustomFontWeightToggle;
    private ComboBox _yiLabelFontFamilyComboBox;
    private ComboBox _yiValueFontFamilyComboBox;
    private ComboBox _jiLabelFontFamilyComboBox;
    private ComboBox _jiValueFontFamilyComboBox;
    private ComboBox _yiLabelFontWeightComboBox;
    private ComboBox _yiValueFontWeightComboBox;
    private ComboBox _jiLabelFontWeightComboBox;
    private ComboBox _jiValueFontWeightComboBox;

    private ComboBox _displayModeComboBox;
    private TextBlock _displayModeLabel;
    private TextBox _yiLabelTextBox;
    private TextBox _jiLabelTextBox;
    private List<TextBlock> _dynamicTextBlocks = new();
    private List<TextBlock> _grayTextBlocks = new();
    private List<Border> _tableCellBorders = new();

    public TomorrowYiJiSettingsControl() { InitializeComponent(); }

    private void InitializeComponent()
    {
        var rootPanel = new Grid();
        var rootRowDefinitions = new RowDefinitions();
        rootRowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootRowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootRowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootPanel.RowDefinitions = rootRowDefinitions;

        var disclaimerBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityError());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Title", "郑重声明");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Message", "我们是坚定不移的唯物主义者，世界是物质的，不依赖于我们的意识。宜忌内容仅供参考，切勿当真。");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsClosable", false);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Margin", new Thickness(0, 0, 0, 8));
        Grid.SetRow(disclaimerBar, 0);
        rootPanel.Children.Add(disclaimerBar);

        var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "注意：此组件的内容可能非常长，以至于超出屏幕，建议包括在滚动容器中使用。");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", !Settings.InfoBarDismissed);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Margin", new Thickness(0, 0, 0, 8));
        FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(infoBar, (s, e) => Settings.InfoBarDismissed = true);
        Grid.SetRow(infoBar, 1);
        rootPanel.Children.Add(infoBar);

        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var displayPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
        displayPanel.Children.Add(CreateDisplayModeRow());
        sp.Children.Add(SettingsGroupFactory.Create("显示设置", displayPanel));

        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
        textPanel.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateStyleTable()
        });
        sp.Children.Add(SettingsGroupFactory.Create("文案设置", textPanel));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Grid.SetRow(scrollViewer, 2);
        rootPanel.Children.Add(scrollViewer);
        Content = rootPanel;
    }

    // ==================== 样式设置表格 ====================

    private Grid CreateStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        for (int i = 0; i < 5; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (int i = 0; i < 5; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "标签文本");
        AddTableHeader(grid, 0, 2, "自定义大小");
        AddTableHeader(grid, 0, 3, "自定义颜色");
        AddTableHeader(grid, 0, 4, "自定义字体");
        AddTableHeader(grid, 0, 5, "自定义字重");

        // 宜标签
        AddTableRowLabel(grid, 1, "宜标签");
        _yiLabelTextBox = new TextBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
        _yiLabelTextBox.TextChanged += (sender, e) => Settings.YiLabel = _yiLabelTextBox.Text ?? "";
        AddTableCell(grid, 1, 1, _yiLabelTextBox);
        AddTableCell(grid, 1, 2, CreateSizeCell(out _yiLabelFontSizeNumericUpDown, out _yiLabelEnableCustomFontSizeToggle, OnYiLabelFontSizeChanged, OnYiLabelEnableCustomFontSizeChanged));
        AddTableCell(grid, 1, 3, CreateColorCell(out _yiLabelFontColorPicker, out _yiLabelEnableCustomFontColorToggle, OnYiLabelColorChanged, OnYiLabelEnableCustomFontColorChanged));
        AddTableCell(grid, 1, 4, CreateFamilyCell(out _yiLabelFontFamilyComboBox, out _yiLabelEnableCustomFontFamilyToggle, OnYiLabelFontFamilyChanged, OnYiLabelEnableCustomFontFamilyChanged));
        AddTableCell(grid, 1, 5, CreateWeightCell(out _yiLabelFontWeightComboBox, out _yiLabelEnableCustomFontWeightToggle, OnYiLabelFontWeightChanged, OnYiLabelEnableCustomFontWeightChanged));

        // 宜值（颜色固定为绿色）
        AddTableRowLabel(grid, 2, "宜值");
        AddTableCell(grid, 2, 1, new StackPanel { VerticalAlignment = VerticalAlignment.Center });
        AddTableCell(grid, 2, 2, CreateSizeCell(out _yiValueFontSizeNumericUpDown, out _yiValueEnableCustomFontSizeToggle, OnYiValueFontSizeChanged, OnYiValueEnableCustomFontSizeChanged));
        AddTableCell(grid, 2, 3, CreateFixedColorHint("固定绿色"));
        AddTableCell(grid, 2, 4, CreateFamilyCell(out _yiValueFontFamilyComboBox, out _yiValueEnableCustomFontFamilyToggle, OnYiValueFontFamilyChanged, OnYiValueEnableCustomFontFamilyChanged));
        AddTableCell(grid, 2, 5, CreateWeightCell(out _yiValueFontWeightComboBox, out _yiValueEnableCustomFontWeightToggle, OnYiValueFontWeightChanged, OnYiValueEnableCustomFontWeightChanged));

        // 忌标签
        AddTableRowLabel(grid, 3, "忌标签");
        _jiLabelTextBox = new TextBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
        _jiLabelTextBox.TextChanged += (sender, e) => Settings.JiLabel = _jiLabelTextBox.Text ?? "";
        AddTableCell(grid, 3, 1, _jiLabelTextBox);
        AddTableCell(grid, 3, 2, CreateSizeCell(out _jiLabelFontSizeNumericUpDown, out _jiLabelEnableCustomFontSizeToggle, OnJiLabelFontSizeChanged, OnJiLabelEnableCustomFontSizeChanged));
        AddTableCell(grid, 3, 3, CreateColorCell(out _jiLabelFontColorPicker, out _jiLabelEnableCustomFontColorToggle, OnJiLabelColorChanged, OnJiLabelEnableCustomFontColorChanged));
        AddTableCell(grid, 3, 4, CreateFamilyCell(out _jiLabelFontFamilyComboBox, out _jiLabelEnableCustomFontFamilyToggle, OnJiLabelFontFamilyChanged, OnJiLabelEnableCustomFontFamilyChanged));
        AddTableCell(grid, 3, 5, CreateWeightCell(out _jiLabelFontWeightComboBox, out _jiLabelEnableCustomFontWeightToggle, OnJiLabelFontWeightChanged, OnJiLabelEnableCustomFontWeightChanged));

        // 忌值（颜色固定为红色）
        AddTableRowLabel(grid, 4, "忌值");
        AddTableCell(grid, 4, 1, new StackPanel { VerticalAlignment = VerticalAlignment.Center });
        AddTableCell(grid, 4, 2, CreateSizeCell(out _jiValueFontSizeNumericUpDown, out _jiValueEnableCustomFontSizeToggle, OnJiValueFontSizeChanged, OnJiValueEnableCustomFontSizeChanged));
        AddTableCell(grid, 4, 3, CreateFixedColorHint("固定红色"));
        AddTableCell(grid, 4, 4, CreateFamilyCell(out _jiValueFontFamilyComboBox, out _jiValueEnableCustomFontFamilyToggle, OnJiValueFontFamilyChanged, OnJiValueEnableCustomFontFamilyChanged));
        AddTableCell(grid, 4, 5, CreateWeightCell(out _jiValueFontWeightComboBox, out _jiValueEnableCustomFontWeightToggle, OnJiValueFontWeightChanged, OnJiValueEnableCustomFontWeightChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 5);
        Grid.SetColumnSpan(outerBorder, 6);
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

    private TextBlock CreateFixedColorHint(string text)
    {
        var tb = new TextBlock { Text = text, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        _grayTextBlocks.Add(tb);
        return tb;
    }

    private static StackPanel CreateSizeCell(out NumericUpDown numericUpDown, out CheckBox toggle,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
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
        EventHandler<ColorChangedEventArgs> colorChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
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
        EventHandler<SelectionChangedEventArgs> selectionChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
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
        EventHandler<SelectionChangedEventArgs> selectionChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
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

    private Grid CreateDisplayModeRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _displayModeLabel = new TextBlock { Text = "显示模式", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_displayModeLabel, 0);
        row.Children.Add(_displayModeLabel);

        _displayModeComboBox = new ComboBox { Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        _displayModeComboBox.Items.Add("单行");
        _displayModeComboBox.Items.Add("双行");
        _displayModeComboBox.SelectionChanged += OnDisplayModeChanged;
        Grid.SetColumn(_displayModeComboBox, 1);
        row.Children.Add(_displayModeComboBox);

        return row;
    }

    private void OnDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        Settings.DisplayMode = _displayModeComboBox.SelectedIndex;
    }

    private void UpdateThemeColors()
    {
        _displayModeLabel.Foreground = ThemeHelper.GetTextBrush();

        _yiLabelEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _yiValueEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _yiValueEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _yiValueEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiValueEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiValueEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiValueEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();

        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetTextBrush();
        }

        foreach (var tb in _grayTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetGrayBrush();
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

    private void OnYiLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontSize = _yiLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontColor = _yiLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.YiValueEnableCustomFontSize = _yiValueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontSize = _jiLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontColor = _jiLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.JiValueEnableCustomFontSize = _jiValueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontFamily = _yiLabelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontWeight = _yiLabelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiLabelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.YiLabelFontFamily = _yiLabelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiLabelFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiLabelFontWeightComboBox.SelectedItem != null)
        {
            Settings.YiLabelFontWeight = _yiLabelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiValueEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.YiValueEnableCustomFontFamily = _yiValueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.YiValueEnableCustomFontWeight = _yiValueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiValueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.YiValueFontFamily = _yiValueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiValueFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiValueFontWeightComboBox.SelectedItem != null)
        {
            Settings.YiValueFontWeight = _yiValueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiLabelEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontFamily = _jiLabelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontWeight = _jiLabelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiLabelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.JiLabelFontFamily = _jiLabelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiLabelFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiLabelFontWeightComboBox.SelectedItem != null)
        {
            Settings.JiLabelFontWeight = _jiLabelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiValueEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.JiValueEnableCustomFontFamily = _jiValueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.JiValueEnableCustomFontWeight = _jiValueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiValueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.JiValueFontFamily = _jiValueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiValueFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiValueFontWeightComboBox.SelectedItem != null)
        {
            Settings.JiValueFontWeight = _jiValueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        _yiLabelFontSizeNumericUpDown.IsEnabled = Settings.YiLabelEnableCustomFontSize;
        _yiLabelFontColorPicker.IsEnabled = Settings.YiLabelEnableCustomFontColor;
        _yiLabelFontFamilyComboBox.IsEnabled = Settings.YiLabelEnableCustomFontFamily;
        _yiLabelFontWeightComboBox.IsEnabled = Settings.YiLabelEnableCustomFontWeight;
        _yiValueFontSizeNumericUpDown.IsEnabled = Settings.YiValueEnableCustomFontSize;
        _yiValueFontFamilyComboBox.IsEnabled = Settings.YiValueEnableCustomFontFamily;
        _yiValueFontWeightComboBox.IsEnabled = Settings.YiValueEnableCustomFontWeight;
        _jiLabelFontSizeNumericUpDown.IsEnabled = Settings.JiLabelEnableCustomFontSize;
        _jiLabelFontColorPicker.IsEnabled = Settings.JiLabelEnableCustomFontColor;
        _jiLabelFontFamilyComboBox.IsEnabled = Settings.JiLabelEnableCustomFontFamily;
        _jiLabelFontWeightComboBox.IsEnabled = Settings.JiLabelEnableCustomFontWeight;
        _jiValueFontSizeNumericUpDown.IsEnabled = Settings.JiValueEnableCustomFontSize;
        _jiValueFontFamilyComboBox.IsEnabled = Settings.JiValueEnableCustomFontFamily;
        _jiValueFontWeightComboBox.IsEnabled = Settings.JiValueEnableCustomFontWeight;
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
        _displayModeComboBox.SelectedIndex = Settings.DisplayMode;

        _yiLabelTextBox.Text = Settings.YiLabel;
        _jiLabelTextBox.Text = Settings.JiLabel;

        _yiLabelFontSizeNumericUpDown.Value = (decimal)Settings.YiLabelFontSize;
        _yiLabelFontColorPicker.Color = ParseColor(Settings.YiLabelFontColor);
        _yiValueFontSizeNumericUpDown.Value = (decimal)Settings.YiValueFontSize;
        _jiLabelFontSizeNumericUpDown.Value = (decimal)Settings.JiLabelFontSize;
        _jiLabelFontColorPicker.Color = ParseColor(Settings.JiLabelFontColor);
        _jiValueFontSizeNumericUpDown.Value = (decimal)Settings.JiValueFontSize;

        _yiLabelFontFamilyComboBox.SelectedItem = Settings.YiLabelFontFamily;
        _yiValueFontFamilyComboBox.SelectedItem = Settings.YiValueFontFamily;
        _jiLabelFontFamilyComboBox.SelectedItem = Settings.JiLabelFontFamily;
        _jiValueFontFamilyComboBox.SelectedItem = Settings.JiValueFontFamily;
        _yiLabelFontWeightComboBox.SelectedItem = Settings.YiLabelFontWeight;
        _yiValueFontWeightComboBox.SelectedItem = Settings.YiValueFontWeight;
        _jiLabelFontWeightComboBox.SelectedItem = Settings.JiLabelFontWeight;
        _jiValueFontWeightComboBox.SelectedItem = Settings.JiValueFontWeight;

        _yiLabelEnableCustomFontSizeToggle.IsChecked = Settings.YiLabelEnableCustomFontSize;
        _yiLabelEnableCustomFontColorToggle.IsChecked = Settings.YiLabelEnableCustomFontColor;
        _yiLabelEnableCustomFontFamilyToggle.IsChecked = Settings.YiLabelEnableCustomFontFamily;
        _yiLabelEnableCustomFontWeightToggle.IsChecked = Settings.YiLabelEnableCustomFontWeight;
        _yiValueEnableCustomFontSizeToggle.IsChecked = Settings.YiValueEnableCustomFontSize;
        _yiValueEnableCustomFontFamilyToggle.IsChecked = Settings.YiValueEnableCustomFontFamily;
        _yiValueEnableCustomFontWeightToggle.IsChecked = Settings.YiValueEnableCustomFontWeight;
        _jiLabelEnableCustomFontSizeToggle.IsChecked = Settings.JiLabelEnableCustomFontSize;
        _jiLabelEnableCustomFontColorToggle.IsChecked = Settings.JiLabelEnableCustomFontColor;
        _jiLabelEnableCustomFontFamilyToggle.IsChecked = Settings.JiLabelEnableCustomFontFamily;
        _jiLabelEnableCustomFontWeightToggle.IsChecked = Settings.JiLabelEnableCustomFontWeight;
        _jiValueEnableCustomFontSizeToggle.IsChecked = Settings.JiValueEnableCustomFontSize;
        _jiValueEnableCustomFontFamilyToggle.IsChecked = Settings.JiValueEnableCustomFontFamily;
        _jiValueEnableCustomFontWeightToggle.IsChecked = Settings.JiValueEnableCustomFontWeight;

        UpdateControlsEnabled();
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
            return Color.Parse(ThemeHelper.GetTextColorHex());
        }
    }

    private void OnYiLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_yiLabelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.YiLabelFontSize = (double)_yiLabelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnYiValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_yiValueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.YiValueFontSize = (double)_yiValueFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnJiLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_jiLabelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.JiLabelFontSize = (double)_jiLabelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnJiValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_jiValueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.JiValueFontSize = (double)_jiValueFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnYiLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.YiLabelFontColor = _yiLabelFontColorPicker.Color.ToString();
    }

    private void OnJiLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.JiLabelFontColor = _jiLabelFontColorPicker.Color.ToString();
    }
}
