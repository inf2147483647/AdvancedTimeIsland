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
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class ForwardTimerSettingsControl : ComponentBase<ForwardTimerSettings>
{
    private TextBox? _text1TextBox;
    private TextBox? _nameTextBox;
    private TextBox? _text3TextBox;
    private TextBox? _text4TextBox;
    private TextBox? _timeFormatTextBox;
    private TextBlock? _timeFormatHint;
    private ComboBox? _timeBaseComboBox;
    private CheckBox? _text1EnableCustomFontSizeToggle;
    private CheckBox? _text1EnableCustomFontColorToggle;
    private CheckBox? _nameEnableCustomFontSizeToggle;
    private CheckBox? _nameEnableCustomFontColorToggle;
    private CheckBox? _text3EnableCustomFontSizeToggle;
    private CheckBox? _text3EnableCustomFontColorToggle;
    private CheckBox? _timeEnableCustomFontSizeToggle;
    private CheckBox? _timeEnableCustomFontColorToggle;
    private CheckBox? _text4EnableCustomFontSizeToggle;
    private CheckBox? _text4EnableCustomFontColorToggle;
    private CheckBox? _text1EnableCustomFontFamilyToggle;
    private CheckBox? _nameEnableCustomFontFamilyToggle;
    private CheckBox? _text3EnableCustomFontFamilyToggle;
    private CheckBox? _timeEnableCustomFontFamilyToggle;
    private CheckBox? _text4EnableCustomFontFamilyToggle;
    private CheckBox? _text1EnableCustomFontWeightToggle;
    private CheckBox? _nameEnableCustomFontWeightToggle;
    private CheckBox? _text3EnableCustomFontWeightToggle;
    private CheckBox? _timeEnableCustomFontWeightToggle;
    private CheckBox? _text4EnableCustomFontWeightToggle;
    private TextBox? _startYearTextBox;
    private ComboBox? _startMonthComboBox;
    private ComboBox? _startDayComboBox;
    private ComboBox? _startHourComboBox;
    private ComboBox? _startMinuteComboBox;
    private ComboBox? _startSecondComboBox;
    private NumericUpDown? _text1FontSizeNumericUpDown;
    private ColorPicker? _text1FontColorPicker;
    private NumericUpDown? _nameFontSizeNumericUpDown;
    private ColorPicker? _nameFontColorPicker;
    private NumericUpDown? _text3FontSizeNumericUpDown;
    private ColorPicker? _text3FontColorPicker;
    private NumericUpDown? _timeFontSizeNumericUpDown;
    private ColorPicker? _timeFontColorPicker;
    private NumericUpDown? _text4FontSizeNumericUpDown;
    private ColorPicker? _text4FontColorPicker;
    private ComboBox? _text1FontFamilyComboBox;
    private ComboBox? _nameFontFamilyComboBox;
    private ComboBox? _text3FontFamilyComboBox;
    private ComboBox? _timeFontFamilyComboBox;
    private ComboBox? _text4FontFamilyComboBox;
    private ComboBox? _text1FontWeightComboBox;
    private ComboBox? _nameFontWeightComboBox;
    private ComboBox? _text3FontWeightComboBox;
    private ComboBox? _timeFontWeightComboBox;
    private ComboBox? _text4FontWeightComboBox;

    private TextBlock? _orderHintTextBlock;
    private TextBlock? _textGroupHeader;
    private TextBlock? _timeGroupHeader;
    private TextBlock? _formatGroupHeader;
    private TextBlock? _formatLabel;
    private TextBlock? _timeBaseGroupHeader;
    private TextBlock? _timeBaseLabel;
    private TextBlock? _startTimeGroupHeader;
    private TextBlock? _startDateLabel;
    private TextBlock? _startTimeLabel;
    private TextBlock? _hourSeparator;
    private TextBlock? _minuteSeparator;
    private TextBlock? _appearanceGroupHeader;
    private ToggleSwitch? _simpleModeToggle;
    private TextBlock? _simpleModeDesc;

    private List<TextBlock> _dynamicTextBlocks = new();
    private List<Border> _tableCellBorders = new();

    public ForwardTimerSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8, Margin = new Thickness(12) };

        // ==================== 文案设置（表格） ====================
        _textGroupHeader = new TextBlock { Text = "文案设置" };
        var textGroup = new Expander { Header = _textGroupHeader, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _orderHintTextBlock = new TextBlock
        {
            Text = "以下内容在主界面上显示的顺序为：文案1->正向计时器名称->文案3->已过时间->文案4",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        textPanel.Children.Add(_orderHintTextBlock);

        textPanel.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateTextTable()
        });
        textPanel.Children.Add(CreateFontWeightHintTextBlock());

        textGroup.Content = textPanel;
        mainPanel.Children.Add(textGroup);

        // ==================== 时间设置（合并折叠栏） ====================
        _timeGroupHeader = new TextBlock { Text = "时间设置" };
        var timeGroup = new Expander { Header = _timeGroupHeader, IsExpanded = true };
        var timePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        // --- 时间格式 ---
        _formatGroupHeader = new TextBlock { Text = "时间格式", FontSize = 12, FontWeight = FontWeight.Bold };
        timePanel.Children.Add(_formatGroupHeader);

        _formatLabel = new TextBlock { Text = "格式化文本:", FontSize = 12, FontWeight = FontWeight.Bold };
        timePanel.Children.Add(_formatLabel);
        _timeFormatTextBox = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, Text = "%d天%h小时%m分钟%s秒" };
        timePanel.Children.Add(_timeFormatTextBox);

        _timeFormatHint = new TextBlock
        {
            Text = "格式化变量: %D总天数 %H总小时 %M总分钟 %S总秒 %X总毫秒\n%d天 %h小时 %m分钟 %s秒 %x毫秒\n%L剩余百分比 %P已过百分比 %p已过百分比(两位)\n%yy总年 %YY总年(两位) %mo总月 %MO总月(两位)",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        timePanel.Children.Add(_timeFormatHint);

        // --- 时间基准 ---
        _timeBaseGroupHeader = new TextBlock { Text = "时间基准", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 0) };
        timePanel.Children.Add(_timeBaseGroupHeader);

        _timeBaseLabel = new TextBlock { Text = "时间来源:", FontSize = 12, FontWeight = FontWeight.Bold };
        timePanel.Children.Add(_timeBaseLabel);
        _timeBaseComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left };
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("ClassIsland时间");
        timePanel.Children.Add(_timeBaseComboBox);

        // --- 开始时间 ---
        _startTimeGroupHeader = new TextBlock { Text = "开始时间", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 0) };
        timePanel.Children.Add(_startTimeGroupHeader);

        var startDateRow = new Grid();
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        _startDateLabel = new TextBlock { Text = "日期:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_startDateLabel, 0);
        startDateRow.Children.Add(_startDateLabel);

        _startYearTextBox = new TextBox { Width = 80, Watermark = "年" };
        Grid.SetColumn(_startYearTextBox, 1);
        startDateRow.Children.Add(_startYearTextBox);

        _startMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) _startMonthComboBox.Items.Add($"{i}月");
        Grid.SetColumn(_startMonthComboBox, 2);
        startDateRow.Children.Add(_startMonthComboBox);

        _startDayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) _startDayComboBox.Items.Add($"{i}日");
        Grid.SetColumn(_startDayComboBox, 3);
        startDateRow.Children.Add(_startDayComboBox);

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startYearTextBox, (s, e) => UpdateDayComboBox(_startYearTextBox, _startMonthComboBox, _startDayComboBox));
        _startMonthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(_startYearTextBox, _startMonthComboBox, _startDayComboBox);

        timePanel.Children.Add(startDateRow);

        var startTimeRow = new Grid();
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        _startTimeLabel = new TextBlock { Text = "时间:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_startTimeLabel, 0);
        startTimeRow.Children.Add(_startTimeLabel);

        _startHourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) _startHourComboBox.Items.Add(i.ToString("D2"));
        Grid.SetColumn(_startHourComboBox, 1);
        startTimeRow.Children.Add(_startHourComboBox);

        _hourSeparator = new TextBlock { Text = ":", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_hourSeparator, 2);
        startTimeRow.Children.Add(_hourSeparator);

        _startMinuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) _startMinuteComboBox.Items.Add(i.ToString("D2"));
        Grid.SetColumn(_startMinuteComboBox, 3);
        startTimeRow.Children.Add(_startMinuteComboBox);

        _minuteSeparator = new TextBlock { Text = ":", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_minuteSeparator, 4);
        startTimeRow.Children.Add(_minuteSeparator);

        _startSecondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) _startSecondComboBox.Items.Add(i.ToString("D2"));
        Grid.SetColumn(_startSecondComboBox, 5);
        startTimeRow.Children.Add(_startSecondComboBox);

        timePanel.Children.Add(startTimeRow);

        timeGroup.Content = timePanel;
        mainPanel.Children.Add(timeGroup);

        // ==================== 外观设置 ====================
        _appearanceGroupHeader = new TextBlock { Text = "外观设置" };
        var appearanceGroup = new Expander { Header = _appearanceGroupHeader, IsExpanded = true };
        var appearancePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _simpleModeToggle = new ToggleSwitch
        {
            Content = "简化模式",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _simpleModeToggle.IsCheckedChanged += OnSimpleModeToggleChanged;
        appearancePanel.Children.Add(_simpleModeToggle);

        _simpleModeDesc = new TextBlock
        {
            Text = "开启后，文案只显示名称与时间（文案1/文案3/文案4不再显示）。默认关闭。",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        appearancePanel.Children.Add(_simpleModeDesc);

        appearanceGroup.Content = appearancePanel;
        mainPanel.Children.Add(appearanceGroup);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = mainPanel
        };
        Content = scrollViewer;
    }

    // ==================== 文案设置表格 ====================

    private Grid CreateTextTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 110 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (int i = 0; i < 6; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "文案");
        AddTableHeader(grid, 0, 1, "内容");
        AddTableHeader(grid, 0, 2, "自定义大小");
        AddTableHeader(grid, 0, 3, "自定义颜色");
        AddTableHeader(grid, 0, 4, "自定义字体");
        AddTableHeader(grid, 0, 5, "自定义字重");

        // 文案1
        AddTableRowLabel(grid, 1, "文案1");
        _text1TextBox = new TextBox { Watermark = "", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 1, 1, _text1TextBox);
        AddTableCell(grid, 1, 2, CreateSizeCell(out _text1FontSizeNumericUpDown, out _text1EnableCustomFontSizeToggle, OnText1EnableCustomFontSizeChanged));
        AddTableCell(grid, 1, 3, CreateColorCell(out _text1FontColorPicker, out _text1EnableCustomFontColorToggle, OnText1EnableCustomFontColorChanged));
        AddTableCell(grid, 1, 4, CreateFamilyCell(out _text1FontFamilyComboBox, out _text1EnableCustomFontFamilyToggle, OnText1EnableCustomFontFamilyChanged));
        AddTableCell(grid, 1, 5, CreateWeightCell(out _text1FontWeightComboBox, out _text1EnableCustomFontWeightToggle, OnText1EnableCustomFontWeightChanged));

        // 正向计时器名称
        AddTableRowLabel(grid, 2, "正向计时器名称");
        _nameTextBox = new TextBox { Watermark = "", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 2, 1, _nameTextBox);
        AddTableCell(grid, 2, 2, CreateSizeCell(out _nameFontSizeNumericUpDown, out _nameEnableCustomFontSizeToggle, OnNameEnableCustomFontSizeChanged));
        AddTableCell(grid, 2, 3, CreateColorCell(out _nameFontColorPicker, out _nameEnableCustomFontColorToggle, OnNameEnableCustomFontColorChanged));
        AddTableCell(grid, 2, 4, CreateFamilyCell(out _nameFontFamilyComboBox, out _nameEnableCustomFontFamilyToggle, OnNameEnableCustomFontFamilyChanged));
        AddTableCell(grid, 2, 5, CreateWeightCell(out _nameFontWeightComboBox, out _nameEnableCustomFontWeightToggle, OnNameEnableCustomFontWeightChanged));

        // 文案3
        AddTableRowLabel(grid, 3, "文案3");
        _text3TextBox = new TextBox { Watermark = "已过", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 3, 1, _text3TextBox);
        AddTableCell(grid, 3, 2, CreateSizeCell(out _text3FontSizeNumericUpDown, out _text3EnableCustomFontSizeToggle, OnText3EnableCustomFontSizeChanged));
        AddTableCell(grid, 3, 3, CreateColorCell(out _text3FontColorPicker, out _text3EnableCustomFontColorToggle, OnText3EnableCustomFontColorChanged));
        AddTableCell(grid, 3, 4, CreateFamilyCell(out _text3FontFamilyComboBox, out _text3EnableCustomFontFamilyToggle, OnText3EnableCustomFontFamilyChanged));
        AddTableCell(grid, 3, 5, CreateWeightCell(out _text3FontWeightComboBox, out _text3EnableCustomFontWeightToggle, OnText3EnableCustomFontWeightChanged));

        // 已过时间（内容无输入框，格式在时间设置中配置）
        AddTableRowLabel(grid, 4, "已过时间");
        var timeContentHint = new TextBlock { Text = "格式在时间设置中配置", FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(timeContentHint);
        AddTableCell(grid, 4, 1, timeContentHint);
        AddTableCell(grid, 4, 2, CreateSizeCell(out _timeFontSizeNumericUpDown, out _timeEnableCustomFontSizeToggle, OnTimeEnableCustomFontSizeChanged));
        AddTableCell(grid, 4, 3, CreateColorCell(out _timeFontColorPicker, out _timeEnableCustomFontColorToggle, OnTimeEnableCustomFontColorChanged));
        AddTableCell(grid, 4, 4, CreateFamilyCell(out _timeFontFamilyComboBox, out _timeEnableCustomFontFamilyToggle, OnTimeEnableCustomFontFamilyChanged));
        AddTableCell(grid, 4, 5, CreateWeightCell(out _timeFontWeightComboBox, out _timeEnableCustomFontWeightToggle, OnTimeEnableCustomFontWeightChanged));

        // 文案4
        AddTableRowLabel(grid, 5, "文案4");
        _text4TextBox = new TextBox { Watermark = "", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 5, 1, _text4TextBox);
        AddTableCell(grid, 5, 2, CreateSizeCell(out _text4FontSizeNumericUpDown, out _text4EnableCustomFontSizeToggle, OnText4EnableCustomFontSizeChanged));
        AddTableCell(grid, 5, 3, CreateColorCell(out _text4FontColorPicker, out _text4EnableCustomFontColorToggle, OnText4EnableCustomFontColorChanged));
        AddTableCell(grid, 5, 4, CreateFamilyCell(out _text4FontFamilyComboBox, out _text4EnableCustomFontFamilyToggle, OnText4EnableCustomFontFamilyChanged));
        AddTableCell(grid, 5, 5, CreateWeightCell(out _text4FontWeightComboBox, out _text4EnableCustomFontWeightToggle, OnText4EnableCustomFontWeightChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 6);
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
        var tb = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
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

    private static StackPanel CreateSizeCell(out NumericUpDown? numericUpDown, out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        numericUpDown = new NumericUpDown
        {
            Width = 125,
            Minimum = 1,
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(numericUpDown);
        return panel;
    }

    private static StackPanel CreateColorCell(out ColorPicker? colorPicker, out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        colorPicker = new ColorPicker { Width = 120, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(colorPicker);
        return panel;
    }

    private static StackPanel CreateFamilyCell(out ComboBox? comboBox, out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
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
        panel.Children.Add(comboBox);
        return panel;
    }

    private static StackPanel CreateWeightCell(out ComboBox? comboBox, out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
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
        if (_orderHintTextBlock != null) _orderHintTextBlock.Foreground = ThemeHelper.GetYellowBrush();
        if (_textGroupHeader != null) _textGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_timeGroupHeader != null) _timeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_formatGroupHeader != null) _formatGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_formatLabel != null) _formatLabel.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_timeFormatHint != null) _timeFormatHint.Foreground = ThemeHelper.GetGrayBrush();
        if (_timeBaseGroupHeader != null) _timeBaseGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_timeBaseLabel != null) _timeBaseLabel.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_startTimeGroupHeader != null) _startTimeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_startDateLabel != null) _startDateLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_startTimeLabel != null) _startTimeLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_hourSeparator != null) _hourSeparator.Foreground = ThemeHelper.GetTextBrush();
        if (_minuteSeparator != null) _minuteSeparator.Foreground = ThemeHelper.GetTextBrush();
        if (_appearanceGroupHeader != null) _appearanceGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeToggle != null) _simpleModeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeDesc != null) _simpleModeDesc.Foreground = ThemeHelper.GetGrayBrush();

        if (_text1EnableCustomFontSizeToggle != null) _text1EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text1EnableCustomFontColorToggle != null) _text1EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_nameEnableCustomFontSizeToggle != null) _nameEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_nameEnableCustomFontColorToggle != null) _nameEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontSizeToggle != null) _text3EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontColorToggle != null) _text3EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontSizeToggle != null) _timeEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontColorToggle != null) _timeEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text4EnableCustomFontSizeToggle != null) _text4EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text4EnableCustomFontColorToggle != null) _text4EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text1EnableCustomFontFamilyToggle != null) _text1EnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_nameEnableCustomFontFamilyToggle != null) _nameEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontFamilyToggle != null) _text3EnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontFamilyToggle != null) _timeEnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text4EnableCustomFontFamilyToggle != null) _text4EnableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text1EnableCustomFontWeightToggle != null) _text1EnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_nameEnableCustomFontWeightToggle != null) _nameEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontWeightToggle != null) _text3EnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontWeightToggle != null) _timeEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text4EnableCustomFontWeightToggle != null) _text4EnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();

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

    private void OnText1EnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontSize = _text1EnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontColor = _text1EnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontSize = _nameEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontColor = _nameEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontSize = _text3EnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontColor = _text3EnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontSize = _timeEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontColor = _timeEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.Text4EnableCustomFontSize = _text4EnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text4EnableCustomFontColor = _text4EnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontFamily = _text1EnableCustomFontFamilyToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontFamily = _nameEnableCustomFontFamilyToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontFamily = _text3EnableCustomFontFamilyToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontFamily = _timeEnableCustomFontFamilyToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.Text4EnableCustomFontFamily = _text4EnableCustomFontFamilyToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontWeight = _text1EnableCustomFontWeightToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontWeight = _nameEnableCustomFontWeightToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontWeight = _text3EnableCustomFontWeightToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontWeight = _timeEnableCustomFontWeightToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.Text4EnableCustomFontWeight = _text4EnableCustomFontWeightToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSimpleModeToggleChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableSimpleMode = _simpleModeToggle?.IsChecked ?? false;
    }

    private void UpdateControlsEnabled()
    {
        if (_text1FontSizeNumericUpDown != null) _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        if (_text1FontColorPicker != null) _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        if (_text1FontFamilyComboBox != null) _text1FontFamilyComboBox.IsEnabled = Settings.Text1EnableCustomFontFamily;
        if (_text1FontWeightComboBox != null) _text1FontWeightComboBox.IsEnabled = Settings.Text1EnableCustomFontWeight;
        if (_nameFontSizeNumericUpDown != null) _nameFontSizeNumericUpDown.IsEnabled = Settings.NameEnableCustomFontSize;
        if (_nameFontColorPicker != null) _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
        if (_nameFontFamilyComboBox != null) _nameFontFamilyComboBox.IsEnabled = Settings.NameEnableCustomFontFamily;
        if (_nameFontWeightComboBox != null) _nameFontWeightComboBox.IsEnabled = Settings.NameEnableCustomFontWeight;
        if (_text3FontSizeNumericUpDown != null) _text3FontSizeNumericUpDown.IsEnabled = Settings.Text3EnableCustomFontSize;
        if (_text3FontColorPicker != null) _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        if (_text3FontFamilyComboBox != null) _text3FontFamilyComboBox.IsEnabled = Settings.Text3EnableCustomFontFamily;
        if (_text3FontWeightComboBox != null) _text3FontWeightComboBox.IsEnabled = Settings.Text3EnableCustomFontWeight;
        if (_timeFontSizeNumericUpDown != null) _timeFontSizeNumericUpDown.IsEnabled = Settings.TimeEnableCustomFontSize;
        if (_timeFontColorPicker != null) _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
        if (_timeFontFamilyComboBox != null) _timeFontFamilyComboBox.IsEnabled = Settings.TimeEnableCustomFontFamily;
        if (_timeFontWeightComboBox != null) _timeFontWeightComboBox.IsEnabled = Settings.TimeEnableCustomFontWeight;
        if (_text4FontSizeNumericUpDown != null) _text4FontSizeNumericUpDown.IsEnabled = Settings.Text4EnableCustomFontSize;
        if (_text4FontColorPicker != null) _text4FontColorPicker.IsEnabled = Settings.Text4EnableCustomFontColor;
        if (_text4FontFamilyComboBox != null) _text4FontFamilyComboBox.IsEnabled = Settings.Text4EnableCustomFontFamily;
        if (_text4FontWeightComboBox != null) _text4FontWeightComboBox.IsEnabled = Settings.Text4EnableCustomFontWeight;
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

        if (_text1TextBox != null) _text1TextBox.Text = Settings.Text1;
        if (_nameTextBox != null) _nameTextBox.Text = Settings.Name;
        if (_text3TextBox != null) _text3TextBox.Text = Settings.Text3;
        if (_text4TextBox != null) _text4TextBox.Text = Settings.Text4;
        if (_timeFormatTextBox != null) _timeFormatTextBox.Text = Settings.TimeFormat;
        // 迁移旧版时间基准值
        var migratedType = TimeBaseTypeHelper.Migrate((int)Settings.TimeBaseType);
        if (migratedType != Settings.TimeBaseType)
        {
            Settings.TimeBaseType = migratedType;
        }

        if (_timeBaseComboBox != null) _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => 0,
            TimeBaseType.RawServerTime => 1,
            TimeBaseType.ClassIslandTime => 2,
            _ => 0
        };

        var startTime = DateTimeOffset.FromUnixTimeSeconds(Settings.StartTime).LocalDateTime;
        if (_startYearTextBox != null) _startYearTextBox.Text = startTime.Year.ToString();
        if (_startMonthComboBox != null) _startMonthComboBox.SelectedIndex = startTime.Month - 1;
        if (_startDayComboBox != null) _startDayComboBox.SelectedItem = $"{startTime.Day}日";
        if (_startHourComboBox != null) _startHourComboBox.SelectedIndex = startTime.Hour;
        if (_startMinuteComboBox != null) _startMinuteComboBox.SelectedIndex = startTime.Minute;
        if (_startSecondComboBox != null) _startSecondComboBox.SelectedIndex = startTime.Second;

        if (_text1FontSizeNumericUpDown != null) _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        if (_text1FontColorPicker != null) _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        if (_nameFontSizeNumericUpDown != null) _nameFontSizeNumericUpDown.Value = (decimal)Settings.NameFontSize;
        if (_nameFontColorPicker != null) _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        if (_text3FontSizeNumericUpDown != null) _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        if (_text3FontColorPicker != null) _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        if (_timeFontSizeNumericUpDown != null) _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        if (_timeFontColorPicker != null) _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        if (_text4FontSizeNumericUpDown != null) _text4FontSizeNumericUpDown.Value = (decimal)Settings.Text4FontSize;
        if (_text4FontColorPicker != null) _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);
        if (_text1FontFamilyComboBox != null) _text1FontFamilyComboBox.SelectedItem = Settings.Text1FontFamily;
        if (_nameFontFamilyComboBox != null) _nameFontFamilyComboBox.SelectedItem = Settings.NameFontFamily;
        if (_text3FontFamilyComboBox != null) _text3FontFamilyComboBox.SelectedItem = Settings.Text3FontFamily;
        if (_timeFontFamilyComboBox != null) _timeFontFamilyComboBox.SelectedItem = Settings.TimeFontFamily;
        if (_text4FontFamilyComboBox != null) _text4FontFamilyComboBox.SelectedItem = Settings.Text4FontFamily;
        if (_text1FontWeightComboBox != null) _text1FontWeightComboBox.SelectedItem = Settings.Text1FontWeight;
        if (_nameFontWeightComboBox != null) _nameFontWeightComboBox.SelectedItem = Settings.NameFontWeight;
        if (_text3FontWeightComboBox != null) _text3FontWeightComboBox.SelectedItem = Settings.Text3FontWeight;
        if (_timeFontWeightComboBox != null) _timeFontWeightComboBox.SelectedItem = Settings.TimeFontWeight;
        if (_text4FontWeightComboBox != null) _text4FontWeightComboBox.SelectedItem = Settings.Text4FontWeight;

        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "");
        AttachTextHandler(_nameTextBox, v => Settings.Name = v ?? "");
        AttachTextHandler(_text3TextBox, v => Settings.Text3 = v ?? "已过");
        AttachTextHandler(_text4TextBox, v => Settings.Text4 = v ?? "");
        AttachTextHandler(_timeFormatTextBox, v => Settings.TimeFormat = v ?? "%d天%h小时%m分钟%s秒");

        if (_timeBaseComboBox != null)
        {
            _timeBaseComboBox.SelectionChanged += (s, e) =>
            {
                Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
                {
                    0 => TimeBaseType.PluginOffsetServerTime,
                    1 => TimeBaseType.RawServerTime,
                    2 => TimeBaseType.ClassIslandTime,
                    _ => TimeBaseType.PluginOffsetServerTime
                };
            };
        }

        AttachDateTimeHandlers();

        AttachFontHandlers(_text1FontSizeNumericUpDown, _text1FontColorPicker, (fs, fc) => { Settings.Text1FontSize = fs; Settings.Text1FontColor = fc; });
        AttachFontHandlers(_nameFontSizeNumericUpDown, _nameFontColorPicker, (fs, fc) => { Settings.NameFontSize = fs; Settings.NameFontColor = fc; });
        AttachFontHandlers(_text3FontSizeNumericUpDown, _text3FontColorPicker, (fs, fc) => { Settings.Text3FontSize = fs; Settings.Text3FontColor = fc; });
        AttachFontHandlers(_timeFontSizeNumericUpDown, _timeFontColorPicker, (fs, fc) => { Settings.TimeFontSize = fs; Settings.TimeFontColor = fc; });
        AttachFontHandlers(_text4FontSizeNumericUpDown, _text4FontColorPicker, (fs, fc) => { Settings.Text4FontSize = fs; Settings.Text4FontColor = fc; });
        AttachFontFamilyHandler(_text1FontFamilyComboBox, v => Settings.Text1FontFamily = v);
        AttachFontFamilyHandler(_nameFontFamilyComboBox, v => Settings.NameFontFamily = v);
        AttachFontFamilyHandler(_text3FontFamilyComboBox, v => Settings.Text3FontFamily = v);
        AttachFontFamilyHandler(_timeFontFamilyComboBox, v => Settings.TimeFontFamily = v);
        AttachFontFamilyHandler(_text4FontFamilyComboBox, v => Settings.Text4FontFamily = v);
        AttachFontWeightHandler(_text1FontWeightComboBox, v => Settings.Text1FontWeight = v);
        AttachFontWeightHandler(_nameFontWeightComboBox, v => Settings.NameFontWeight = v);
        AttachFontWeightHandler(_text3FontWeightComboBox, v => Settings.Text3FontWeight = v);
        AttachFontWeightHandler(_timeFontWeightComboBox, v => Settings.TimeFontWeight = v);
        AttachFontWeightHandler(_text4FontWeightComboBox, v => Settings.Text4FontWeight = v);

        if (_text1EnableCustomFontSizeToggle != null)
            _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        if (_text1EnableCustomFontColorToggle != null)
            _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        if (_nameEnableCustomFontSizeToggle != null)
            _nameEnableCustomFontSizeToggle.IsChecked = Settings.NameEnableCustomFontSize;
        if (_nameEnableCustomFontColorToggle != null)
            _nameEnableCustomFontColorToggle.IsChecked = Settings.NameEnableCustomFontColor;
        if (_text3EnableCustomFontSizeToggle != null)
            _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        if (_text3EnableCustomFontColorToggle != null)
            _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        if (_timeEnableCustomFontSizeToggle != null)
            _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        if (_timeEnableCustomFontColorToggle != null)
            _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;
        if (_text4EnableCustomFontSizeToggle != null)
            _text4EnableCustomFontSizeToggle.IsChecked = Settings.Text4EnableCustomFontSize;
        if (_text4EnableCustomFontColorToggle != null)
            _text4EnableCustomFontColorToggle.IsChecked = Settings.Text4EnableCustomFontColor;
        if (_text1EnableCustomFontFamilyToggle != null)
            _text1EnableCustomFontFamilyToggle.IsChecked = Settings.Text1EnableCustomFontFamily;
        if (_nameEnableCustomFontFamilyToggle != null)
            _nameEnableCustomFontFamilyToggle.IsChecked = Settings.NameEnableCustomFontFamily;
        if (_text3EnableCustomFontFamilyToggle != null)
            _text3EnableCustomFontFamilyToggle.IsChecked = Settings.Text3EnableCustomFontFamily;
        if (_timeEnableCustomFontFamilyToggle != null)
            _timeEnableCustomFontFamilyToggle.IsChecked = Settings.TimeEnableCustomFontFamily;
        if (_text4EnableCustomFontFamilyToggle != null)
            _text4EnableCustomFontFamilyToggle.IsChecked = Settings.Text4EnableCustomFontFamily;
        if (_text1EnableCustomFontWeightToggle != null)
            _text1EnableCustomFontWeightToggle.IsChecked = Settings.Text1EnableCustomFontWeight;
        if (_nameEnableCustomFontWeightToggle != null)
            _nameEnableCustomFontWeightToggle.IsChecked = Settings.NameEnableCustomFontWeight;
        if (_text3EnableCustomFontWeightToggle != null)
            _text3EnableCustomFontWeightToggle.IsChecked = Settings.Text3EnableCustomFontWeight;
        if (_timeEnableCustomFontWeightToggle != null)
            _timeEnableCustomFontWeightToggle.IsChecked = Settings.TimeEnableCustomFontWeight;
        if (_text4EnableCustomFontWeightToggle != null)
            _text4EnableCustomFontWeightToggle.IsChecked = Settings.Text4EnableCustomFontWeight;
        if (_simpleModeToggle != null)
            _simpleModeToggle.IsChecked = Settings.EnableSimpleMode;
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

    private void AttachTextHandler(TextBox? textBox, Action<string?> handler)
    {
        if (textBox == null) return;

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) =>
        {
            handler(textBox.Text);
        });
    }

    private void AttachDateTimeHandlers()
    {
        void UpdateStartTime()
        {
            if (int.TryParse(_startYearTextBox?.Text?.Trim(), out var year) &&
                _startMonthComboBox?.SelectedIndex >= 0 &&
                _startDayComboBox?.SelectedItem != null &&
                int.TryParse(_startDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) &&
                _startHourComboBox?.SelectedIndex >= 0 &&
                _startMinuteComboBox?.SelectedIndex >= 0 &&
                _startSecondComboBox?.SelectedIndex >= 0)
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

        if (_startYearTextBox != null)
            FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startYearTextBox, (s, e) => UpdateStartTime());

        if (_startMonthComboBox != null)
            _startMonthComboBox.SelectionChanged += (s, e) => UpdateStartTime();

        if (_startDayComboBox != null)
            _startDayComboBox.SelectionChanged += (s, e) => UpdateStartTime();

        if (_startHourComboBox != null)
            _startHourComboBox.SelectionChanged += (s, e) => UpdateStartTime();

        if (_startMinuteComboBox != null)
            _startMinuteComboBox.SelectionChanged += (s, e) => UpdateStartTime();

        if (_startSecondComboBox != null)
            _startSecondComboBox.SelectionChanged += (s, e) => UpdateStartTime();
    }

    private void AttachFontHandlers(NumericUpDown? fontSizeNumericUpDown, ColorPicker? colorPicker, Action<double, string> handler)
    {
        if (fontSizeNumericUpDown != null)
        {
            fontSizeNumericUpDown.ValueChanged += (s, e) =>
            {
                if (fontSizeNumericUpDown.Value.HasValue)
                {
                    var fontSize = (double)fontSizeNumericUpDown.Value.Value;
                    var color = colorPicker?.Color.ToString() ?? "#FFFFFF";
                    handler(fontSize, color);
                }
            };
        }

        if (colorPicker != null)
        {
            colorPicker.ColorChanged += (s, e) =>
            {
                if (fontSizeNumericUpDown?.Value.HasValue == true)
                {
                    var fontSize = (double)fontSizeNumericUpDown.Value.Value;
                    handler(fontSize, colorPicker.Color.ToString());
                }
            };
        }
    }

    private Color ParseColor(string colorStr)
    {
        try
        {
            return Color.Parse(colorStr);
        }
        catch
        {
            return Color.Parse(ThemeHelper.GetTextColorHex());
        }
    }

    private void AttachFontFamilyHandler(ComboBox? comboBox, Action<string> setter)
    {
        if (comboBox == null) return;
        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem != null)
            {
                setter(comboBox.SelectedItem.ToString() ?? "");
            }
        };
    }

    private void AttachFontWeightHandler(ComboBox? comboBox, Action<string> setter)
    {
        if (comboBox == null) return;
        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem != null)
            {
                setter(comboBox.SelectedItem.ToString() ?? "");
            }
        };
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
