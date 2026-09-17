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
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 总在校时间统计（ATI）组件的设置面板：只包含本组件实例的显示选项；
/// 学期区间、节假日与寒暑假等统计口径由插件设置页中的「在校时间统计」统一管理。
/// </summary>
public class AttendanceSettingsControl : ComponentBase<AttendanceSettings>
{
    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;
    private TextBlock _scopeHintTextBlock;
    private readonly List<TextBlock> _labels = new();
    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();
    private readonly List<CheckBox> _checkBoxes = new();

    private CheckBox _enableCustomFontSizeToggle;
    private CheckBox _enableCustomFontColorToggle;
    private CheckBox _enableCustomFontFamilyToggle;
    private CheckBox _enableCustomFontWeightToggle;
    private ColorPicker _colorPicker;
    private NumericUpDown _fontSizeNumericUpDown;
    private ComboBox _fontFamilyComboBox;
    private ComboBox _fontWeightComboBox;

    private ComboBox _progressDisplayModeComboBox;
    private ComboBox _timeBaseComboBox;
    private CheckBox _showSummaryTextCheckBox;
    private CheckBox _showHoursTextCheckBox;
    private CheckBox _showRemainingTextCheckBox;
    private CheckBox _showPercentTextCheckBox;
    private CheckBox _decimalizeHoursCheckBox;

    /// <summary>程序化回填控件时置位，避免控件事件把设置改回控件中的旧值。</summary>
    private bool _isUpdatingControls;

    private static readonly object[] ProgressDisplayModeItems =
    {
        "不显示", "进度条", "进度环", "进度条 + 进度环"
    };

    private static readonly object[] TimeBaseItems =
    {
        "插件偏移后的服务器时间", "原始服务器时间", "ClassIsland时间"
    };

    public AttendanceSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "总在校时间统计（ATI）设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock
        {
            Text = "统计学期内的在校天数与时长，自动排除周末、法定节假日与寒暑假，并展示学期进度与剩余量。",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        sp.Children.Add(_descTextBlock);

        _scopeHintTextBlock = new TextBlock
        {
            Text = "学期起止、每日在校时长、节假日与寒暑假等统计口径，请前往插件设置页的「在校时间统计」中配置。",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        sp.Children.Add(_scopeHintTextBlock);

        // ==================== 显示设置 ====================
        var displayPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var progressModeRow = FontSettingsRowFactory.CreateComboBoxRow("进度显示方式", ProgressDisplayModeItems,
            out var progressModeLabel, out _progressDisplayModeComboBox, out _,
            selectionChangedHandler: OnProgressDisplayModeChanged);
        _labels.Add(progressModeLabel);
        displayPanel.Children.Add(progressModeRow);

        _showSummaryTextCheckBox = AddCheckBox(displayPanel, "显示概要文案（已在校 N 天 / 共 M 天）");
        _showHoursTextCheckBox = AddCheckBox(displayPanel, "在概要文案中显示累计在校时长");
        _showRemainingTextCheckBox = AddCheckBox(displayPanel, "显示剩余在校天数与时长");
        _showPercentTextCheckBox = AddCheckBox(displayPanel, "显示进度百分比");
        _decimalizeHoursCheckBox = AddCheckBox(displayPanel, "时长保留一位小数（关闭则取整）");

        sp.Children.Add(SettingsGroupFactory.Create("显示设置", displayPanel));

        // ==================== 时间设置 ====================
        var timePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var timeBaseRow = FontSettingsRowFactory.CreateComboBoxRow("时间基准", TimeBaseItems,
            out var timeBaseLabel, out _timeBaseComboBox, out _,
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

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    private CheckBox AddCheckBox(StackPanel panel, string text)
    {
        var checkBox = new CheckBox { Content = text, VerticalAlignment = VerticalAlignment.Center };
        checkBox.IsCheckedChanged += OnDisplayOptionChanged;
        _checkBoxes.Add(checkBox);
        panel.Children.Add(checkBox);
        return checkBox;
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

        AddTableRowLabel(grid, 1, "在校统计");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _fontSizeNumericUpDown, out _enableCustomFontSizeToggle, OnEnableCustomFontSizeChanged, OnFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _colorPicker, out _enableCustomFontColorToggle, OnEnableCustomFontColorChanged, OnColorChanged));
        AddTableCell(grid, 1, 3, CreateFamilyCell(out _fontFamilyComboBox, out _enableCustomFontFamilyToggle, OnEnableCustomFontFamilyChanged, OnFontFamilyChanged));
        AddTableCell(grid, 1, 4, CreateWeightCell(out _fontWeightComboBox, out _enableCustomFontWeightToggle, OnEnableCustomFontWeightChanged, OnFontWeightChanged));

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

    // ==================== 事件处理 ====================

    private void OnProgressDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.ProgressDisplayMode = _progressDisplayModeComboBox.SelectedIndex switch
        {
            0 => ProgressDisplayMode.None,
            1 => ProgressDisplayMode.Bar,
            2 => ProgressDisplayMode.Ring,
            3 => ProgressDisplayMode.Both,
            _ => ProgressDisplayMode.Ring
        };
    }

    private void OnTimeBaseChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
        {
            0 => TimeBaseType.PluginOffsetServerTime,
            1 => TimeBaseType.RawServerTime,
            2 => TimeBaseType.ClassIslandTime,
            _ => TimeBaseType.PluginOffsetServerTime
        };
    }

    private void OnDisplayOptionChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.ShowSummaryText = _showSummaryTextCheckBox.IsChecked ?? false;
        Settings.ShowHoursText = _showHoursTextCheckBox.IsChecked ?? false;
        Settings.ShowRemainingText = _showRemainingTextCheckBox.IsChecked ?? false;
        Settings.ShowPercentText = _showPercentTextCheckBox.IsChecked ?? false;
        Settings.DecimalizeHours = _decimalizeHoursCheckBox.IsChecked ?? false;
    }

    private void OnEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.EnableCustomFontColor = _enableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.EnableCustomFontFamily = _enableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.EnableCustomFontWeight = _enableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.FontColor = _colorPicker.Color.ToString();
    }

    private void OnFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (_fontSizeNumericUpDown.Value.HasValue)
        {
            Settings.FontSize = (double)_fontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (_fontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _fontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (_fontWeightComboBox.SelectedItem != null)
        {
            Settings.FontWeight = _fontWeightComboBox.SelectedItem.ToString() ?? "";
        }
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
        foreach (var checkBox in _checkBoxes)
        {
            checkBox.Foreground = textBrush;
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
        _scopeHintTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => UpdateThemeColors();

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

        _isUpdatingControls = true;
        try
        {
            _progressDisplayModeComboBox.SelectedIndex = Settings.ProgressDisplayMode switch
            {
                ProgressDisplayMode.None => 0,
                ProgressDisplayMode.Bar => 1,
                ProgressDisplayMode.Ring => 2,
                ProgressDisplayMode.Both => 3,
                _ => 2
            };
            _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
            {
                TimeBaseType.PluginOffsetServerTime => 0,
                TimeBaseType.RawServerTime => 1,
                TimeBaseType.ClassIslandTime => 2,
                _ => 0
            };
            _showSummaryTextCheckBox.IsChecked = Settings.ShowSummaryText;
            _showHoursTextCheckBox.IsChecked = Settings.ShowHoursText;
            _showRemainingTextCheckBox.IsChecked = Settings.ShowRemainingText;
            _showPercentTextCheckBox.IsChecked = Settings.ShowPercentText;
            _decimalizeHoursCheckBox.IsChecked = Settings.DecimalizeHours;

            _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
            _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
            _enableCustomFontFamilyToggle.IsChecked = Settings.EnableCustomFontFamily;
            _enableCustomFontWeightToggle.IsChecked = Settings.EnableCustomFontWeight;
            _colorPicker.Color = ParseColor(Settings.FontColor);
            _fontSizeNumericUpDown.Value = (decimal)Settings.FontSize;
            _fontFamilyComboBox.SelectedItem = Settings.FontFamily;
            _fontWeightComboBox.SelectedItem = Settings.FontWeight;
        }
        finally
        {
            _isUpdatingControls = false;
        }

        UpdateControlsEnabled();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private static Color ParseColor(string colorString)
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