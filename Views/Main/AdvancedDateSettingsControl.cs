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

public class AdvancedDateSettingsControl : ComponentBase<AdvancedDateSettings>
{
    private ToggleSwitch _showWeekDayToggle;
    private ComboBox _contentOrderComboBox;
    private ComboBox _dateSeparatorComboBox;

    private CheckBox _dateEnableCustomFontSizeToggle;
    private CheckBox _dateEnableCustomFontColorToggle;
    private CheckBox _dateEnableCustomFontFamilyToggle;
    private CheckBox _dateEnableCustomFontWeightToggle;
    private ColorPicker _dateColorPicker;
    private NumericUpDown _dateFontSizeNumericUpDown;
    private ComboBox _dateFontFamilyComboBox;
    private ComboBox _dateFontWeightComboBox;

    private CheckBox _weekDayEnableCustomFontSizeToggle;
    private CheckBox _weekDayEnableCustomFontColorToggle;
    private CheckBox _weekDayEnableCustomFontFamilyToggle;
    private CheckBox _weekDayEnableCustomFontWeightToggle;
    private ColorPicker _weekDayColorPicker;
    private NumericUpDown _weekDayFontSizeNumericUpDown;
    private ComboBox _weekDayFontFamilyComboBox;
    private ComboBox _weekDayFontWeightComboBox;

    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;
    private TextBlock _labelTextBlock;
    private TextBlock _contentOrderLabelTextBlock;
    private TextBlock _dateSeparatorLabelTextBlock;

    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    public AdvancedDateSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "日期设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "配置日期显示选项", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_descTextBlock);

        // ==================== 显示设置 ====================
        var displayPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        _labelTextBlock = new TextBlock { Text = "显示星期", FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_labelTextBlock, 0);
        row.Children.Add(_labelTextBlock);

        _showWeekDayToggle = new ToggleSwitch();
        _showWeekDayToggle.IsCheckedChanged += OnShowWeekDayChanged;
        Grid.SetColumn(_showWeekDayToggle, 1);
        row.Children.Add(_showWeekDayToggle);

        displayPanel.Children.Add(row);

        var contentOrderRow = new Grid();
        contentOrderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentOrderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _contentOrderLabelTextBlock = new TextBlock { Text = "内容组合", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_contentOrderLabelTextBlock, 0);
        contentOrderRow.Children.Add(_contentOrderLabelTextBlock);

        _contentOrderComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 150 };
        _contentOrderComboBox.Items.Add("日期-星期");
        _contentOrderComboBox.Items.Add("星期-日期");
        _contentOrderComboBox.SelectedIndex = 0;
        _contentOrderComboBox.SelectionChanged += OnContentOrderChanged;
        Grid.SetColumn(_contentOrderComboBox, 1);
        contentOrderRow.Children.Add(_contentOrderComboBox);
        displayPanel.Children.Add(contentOrderRow);

        var dateSeparatorRow = new Grid();
        dateSeparatorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dateSeparatorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _dateSeparatorLabelTextBlock = new TextBlock { Text = "日期分隔符", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_dateSeparatorLabelTextBlock, 0);
        dateSeparatorRow.Children.Add(_dateSeparatorLabelTextBlock);

        _dateSeparatorComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 150 };
        _dateSeparatorComboBox.Items.Add("- (2026-07-29)");
        _dateSeparatorComboBox.Items.Add("/ (2026/07/29)");
        _dateSeparatorComboBox.Items.Add(". (2026.07.29)");
        _dateSeparatorComboBox.Items.Add("纯文本 (2026 年 7 月 29 日)");
        _dateSeparatorComboBox.SelectedIndex = 0;
        _dateSeparatorComboBox.SelectionChanged += OnDateSeparatorChanged;
        Grid.SetColumn(_dateSeparatorComboBox, 1);
        dateSeparatorRow.Children.Add(_dateSeparatorComboBox);
        displayPanel.Children.Add(dateSeparatorRow);

        sp.Children.Add(SettingsGroupFactory.Create("显示设置", displayPanel));

        // ==================== 文案设置 ====================
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var fontStyleTableScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateFontStyleTable()
        };
        fontStyleTableScroll.Margin = new Thickness(0, 10, 0, 0);
        textPanel.Children.Add(fontStyleTableScroll);
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

        AddTableRowLabel(grid, 1, "日期");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _dateFontSizeNumericUpDown, out _dateEnableCustomFontSizeToggle, OnDateEnableCustomFontSizeChanged, OnDateFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _dateColorPicker, out _dateEnableCustomFontColorToggle, OnDateEnableCustomFontColorChanged, OnDateColorChanged));
        AddTableCell(grid, 1, 3, CreateFamilyCell(out _dateFontFamilyComboBox, out _dateEnableCustomFontFamilyToggle, OnDateEnableCustomFontFamilyChanged, OnDateFontFamilyChanged));
        AddTableCell(grid, 1, 4, CreateWeightCell(out _dateFontWeightComboBox, out _dateEnableCustomFontWeightToggle, OnDateEnableCustomFontWeightChanged, OnDateFontWeightChanged));

        AddTableRowLabel(grid, 2, "星期");
        AddTableCell(grid, 2, 1, CreateSizeCell(out _weekDayFontSizeNumericUpDown, out _weekDayEnableCustomFontSizeToggle, OnWeekDayEnableCustomFontSizeChanged, OnWeekDayFontSizeChanged));
        AddTableCell(grid, 2, 2, CreateColorCell(out _weekDayColorPicker, out _weekDayEnableCustomFontColorToggle, OnWeekDayEnableCustomFontColorChanged, OnWeekDayColorChanged));
        AddTableCell(grid, 2, 3, CreateFamilyCell(out _weekDayFontFamilyComboBox, out _weekDayEnableCustomFontFamilyToggle, OnWeekDayEnableCustomFontFamilyChanged, OnWeekDayFontFamilyChanged));
        AddTableCell(grid, 2, 4, CreateWeightCell(out _weekDayFontWeightComboBox, out _weekDayEnableCustomFontWeightToggle, OnWeekDayEnableCustomFontWeightChanged, OnWeekDayFontWeightChanged));

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
        _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _labelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _contentOrderLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dateSeparatorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dateEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _dateEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _dateEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _dateEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _weekDayEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _weekDayEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _weekDayEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _weekDayEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();

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

    private void OnDateEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontSize = _dateEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontColor = _dateEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontFamily = _dateEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontWeight = _dateEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontSize = _weekDayEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontColor = _weekDayEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontFamily = _weekDayEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontWeight = _weekDayEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_dateFontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _dateFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnDateFontWeightChanged(object? sender, EventArgs e)
    {
        if (_dateFontWeightComboBox.SelectedItem != null)
        {
            Settings.FontWeight = _dateFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnWeekDayFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_weekDayFontFamilyComboBox.SelectedItem != null)
        {
            Settings.WeekDayFontFamily = _weekDayFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnWeekDayFontWeightChanged(object? sender, EventArgs e)
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
        _showWeekDayToggle.IsChecked = Settings.ShowWeekDay;
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

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnShowWeekDayChanged(object? sender, EventArgs e)
    {
        Settings.ShowWeekDay = _showWeekDayToggle.IsChecked == true;
        UpdateControlsEnabled();
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
            return Color.Parse(colorString);
        }
        catch
        {
            return Color.Parse(ThemeHelper.GetTextColorHex());
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
