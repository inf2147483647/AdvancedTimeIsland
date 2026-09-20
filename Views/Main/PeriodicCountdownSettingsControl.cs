using System;
using System.Collections.Generic;
using System.Linq;
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

public class PeriodicCountdownSettingsControl : ComponentBase<PeriodicCountdownSettings>
{
    private TextBox? _text1TextBox;
    private TextBox? _text3TextBox;
    private TextBox? _text4TextBox;
    private TextBox? _timeFormatTextBox;
    private TextBlock? _timeFormatHint;
    private ToggleSwitch? _timeCorrectionToggle;
    private CheckBox? _text1EnableCustomFontSizeToggle;
    private CheckBox? _text1EnableCustomFontColorToggle;
    private CheckBox? _text2EnableCustomFontSizeToggle;
    private CheckBox? _text2EnableCustomFontColorToggle;
    private CheckBox? _text3EnableCustomFontSizeToggle;
    private CheckBox? _text3EnableCustomFontColorToggle;
    private CheckBox? _timeEnableCustomFontSizeToggle;
    private CheckBox? _timeEnableCustomFontColorToggle;
    private CheckBox? _text4EnableCustomFontSizeToggle;
    private CheckBox? _text4EnableCustomFontColorToggle;
    private ComboBox? _timeBaseComboBox;
    private CountdownListEditor<PeriodicCountdownItem>? _listEditor;

    private NumericUpDown? _text1FontSizeNumericUpDown;
    private ColorPicker? _text1FontColorPicker;
    private NumericUpDown? _text2FontSizeNumericUpDown;
    private ColorPicker? _text2FontColorPicker;
    private NumericUpDown? _text3FontSizeNumericUpDown;
    private ColorPicker? _text3FontColorPicker;
    private NumericUpDown? _timeFontSizeNumericUpDown;
    private ColorPicker? _timeFontColorPicker;
    private NumericUpDown? _text4FontSizeNumericUpDown;
    private ColorPicker? _text4FontColorPicker;

    private TextBlock? _titleTextBlock;
    private TextBlock? _descTextBlock;
    private TextBlock? _orderHintTextBlock;
    private TextBlock? _textGroupHeader;
    private TextBlock? _timeGroupHeader;
    private TextBlock? _formatGroupHeader;
    private TextBlock? _formatLabel;
    private TextBlock? _timeBaseGroupHeader;
    private TextBlock? _timeBaseLabel;
    private TextBlock? _listGroupHeader;

    private ComboBox? _progressDisplayModeComboBox;
    private TextBlock? _progressDisplayModeLabel;
    private TextBlock? _appearanceGroupHeader;
    private ToggleSwitch? _simpleModeToggle;
    private TextBlock? _simpleModeDesc;
    private ToggleSwitch? _enableCustomProgressColorToggle;
    private ColorPicker? _progressBarColorPicker;
    private ColorPicker? _progressRingColorPicker;

    private List<TextBlock> _dynamicTextBlocks = new();
    private List<Border> _tableCellBorders = new();
    private StackPanel? _mainPanel;

    public PeriodicCountdownSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "周期性倒计时设置", FontSize = 14, FontWeight = FontWeight.Bold };
        _mainPanel.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "配置周期性倒计时显示选项和倒计时列表", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        _mainPanel.Children.Add(_descTextBlock);

        // ==================== 文案设置（表格） ====================
        _textGroupHeader = new TextBlock { Text = "文案设置" };
        var textGroup = new Expander { Header = _textGroupHeader, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _orderHintTextBlock = new TextBlock { Text = "以下内容在主界面上显示的顺序为：文案1->倒计时名称->文案3->剩余时间->文案4", FontSize = 11, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap };
        textPanel.Children.Add(_orderHintTextBlock);

        textPanel.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateTextTable()
        });

        textGroup.Content = textPanel;
        _mainPanel.Children.Add(textGroup);

        // ==================== 时间设置（合并折叠栏） ====================
        _timeGroupHeader = new TextBlock { Text = "时间设置" };
        var timeGroup = new Expander { Header = _timeGroupHeader, IsExpanded = true };
        var timePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        // --- 时间格式 ---
        _formatGroupHeader = new TextBlock { Text = "时间格式", FontSize = 12, FontWeight = FontWeight.Bold };
        timePanel.Children.Add(_formatGroupHeader);

        var formatRow = new Grid();
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _formatLabel = new TextBlock { Text = "时间格式:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_formatLabel, 0);
        formatRow.Children.Add(_formatLabel);

        _timeFormatTextBox = new TextBox { Watermark = "%d天%h小时%m分钟%s秒" };
        Grid.SetColumn(_timeFormatTextBox, 1);
        formatRow.Children.Add(_timeFormatTextBox);

        timePanel.Children.Add(formatRow);

        _timeFormatHint = new TextBlock
        {
            Text = "格式化变量: %D总天数 %H总小时 %M总分钟 %S总秒 %X总毫秒\n%d天 %h小时 %m分钟 %s秒 %x毫秒\n%L剩余百分比 %P已过百分比 %p已过百分比(两位)\n%yy总年 %YY总年(两位) %mo总月 %MO总月(两位)",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        timePanel.Children.Add(_timeFormatHint);

        _timeCorrectionToggle = new ToggleSwitch
        {
            Content = "差一矫正（当精度不足时最小单位加一）",
            IsChecked = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _timeCorrectionToggle.IsCheckedChanged += (s, e) =>
        {
            Settings.EnableTimeCorrection = _timeCorrectionToggle.IsChecked == true;
        };
        timePanel.Children.Add(_timeCorrectionToggle);

        // --- 时间基准 ---
        _timeBaseGroupHeader = new TextBlock { Text = "时间基准", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 0) };
        timePanel.Children.Add(_timeBaseGroupHeader);

        var timeBaseRow = new Grid();
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _timeBaseLabel = new TextBlock { Text = "时间基准:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_timeBaseLabel, 0);
        timeBaseRow.Children.Add(_timeBaseLabel);

        _timeBaseComboBox = new ComboBox();
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("插件偏移后的系统时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("原始系统时间");
        Grid.SetColumn(_timeBaseComboBox, 1);
        timeBaseRow.Children.Add(_timeBaseComboBox);

        timePanel.Children.Add(timeBaseRow);

        timeGroup.Content = timePanel;
        _mainPanel.Children.Add(timeGroup);

        // ==================== 外观设置（含进度显示） ====================
        _appearanceGroupHeader = new TextBlock { Text = "外观设置" };
        var progressDisplayModeGroup = new Expander { Header = _appearanceGroupHeader, IsExpanded = true };
        var progressDisplayModePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _simpleModeToggle = new ToggleSwitch
        {
            Content = "简化模式",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _simpleModeToggle.IsCheckedChanged += OnSimpleModeToggleChanged;
        progressDisplayModePanel.Children.Add(_simpleModeToggle);

        _simpleModeDesc = new TextBlock
        {
            Text = "开启后，文案只显示名称与时间（文案1/文案3/文案4不再显示）。默认关闭。",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        progressDisplayModePanel.Children.Add(_simpleModeDesc);

        var progressDisplayModeRow = new Grid();
        progressDisplayModeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        progressDisplayModeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _progressDisplayModeLabel = new TextBlock { Text = "显示进度条:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_progressDisplayModeLabel, 0);
        progressDisplayModeRow.Children.Add(_progressDisplayModeLabel);

        _progressDisplayModeComboBox = new ComboBox();
        _progressDisplayModeComboBox.Items.Add("不显示");
        _progressDisplayModeComboBox.Items.Add("进度条");
        _progressDisplayModeComboBox.Items.Add("进度环");
        _progressDisplayModeComboBox.Items.Add("进度条和进度环");
        Grid.SetColumn(_progressDisplayModeComboBox, 1);
        progressDisplayModeRow.Children.Add(_progressDisplayModeComboBox);

        progressDisplayModePanel.Children.Add(progressDisplayModeRow);

        _enableCustomProgressColorToggle = new ToggleSwitch { Content = "启用自定义进度颜色", Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        _enableCustomProgressColorToggle.IsCheckedChanged += OnEnableCustomProgressColorChanged;
        progressDisplayModePanel.Children.Add(_enableCustomProgressColorToggle);

        var progressBarColorRow = CreateColorRow("进度条颜色:", out _progressBarColorPicker);
        progressDisplayModePanel.Children.Add(progressBarColorRow);

        var progressRingColorRow = CreateColorRow("进度环颜色:", out _progressRingColorPicker);
        progressDisplayModePanel.Children.Add(progressRingColorRow);

        progressDisplayModeGroup.Content = progressDisplayModePanel;
        _mainPanel.Children.Add(progressDisplayModeGroup);

        // ==================== 倒计时列表（主从式内联编辑，参考 ClassIsland 档案编辑） ====================
        _listGroupHeader = new TextBlock { Text = "倒计时列表" };
        var listGroup = new Expander { Header = _listGroupHeader, IsExpanded = true };
        _listEditor = new CountdownListEditor<PeriodicCountdownItem>(
            () => Settings.CountdownItems,
            items => Settings.CountdownItems = items,
            item => item.Name,
            item => GetPeriodTypeName(item.PeriodType) + DescribePeriodicTime(item),
            BuildPeriodicDetailPanel,
            PeriodicCountdownItem.CreateDefault,
            item => item.Clone());
        listGroup.Content = _listEditor;
        _mainPanel.Children.Add(listGroup);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _mainPanel
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
        for (int i = 0; i < 6; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "文案");
        AddTableHeader(grid, 0, 1, "内容");
        AddTableHeader(grid, 0, 2, "自定义大小");
        AddTableHeader(grid, 0, 3, "自定义颜色");

        // 文案1
        AddTableRowLabel(grid, 1, "文案1");
        _text1TextBox = new TextBox { Watermark = "距离", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 1, 1, _text1TextBox);
        AddTableCell(grid, 1, 2, CreateSizeCell(out _text1FontSizeNumericUpDown, out _text1EnableCustomFontSizeToggle, OnText1EnableCustomFontSizeChanged));
        AddTableCell(grid, 1, 3, CreateColorCell(out _text1FontColorPicker, out _text1EnableCustomFontColorToggle, OnText1EnableCustomFontColorChanged));

        // 倒计时名称
        AddTableRowLabel(grid, 2, "倒计时名称");
        var editNameButton = new Button { Content = "前往编辑倒计时名称", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        editNameButton.Click += OnText2ButtonClick;
        AddTableCell(grid, 2, 1, editNameButton);
        AddTableCell(grid, 2, 2, CreateSizeCell(out _text2FontSizeNumericUpDown, out _text2EnableCustomFontSizeToggle, OnText2EnableCustomFontSizeChanged));
        AddTableCell(grid, 2, 3, CreateColorCell(out _text2FontColorPicker, out _text2EnableCustomFontColorToggle, OnText2EnableCustomFontColorChanged));

        // 文案3
        AddTableRowLabel(grid, 3, "文案3");
        _text3TextBox = new TextBox { Watermark = "还有", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 3, 1, _text3TextBox);
        AddTableCell(grid, 3, 2, CreateSizeCell(out _text3FontSizeNumericUpDown, out _text3EnableCustomFontSizeToggle, OnText3EnableCustomFontSizeChanged));
        AddTableCell(grid, 3, 3, CreateColorCell(out _text3FontColorPicker, out _text3EnableCustomFontColorToggle, OnText3EnableCustomFontColorChanged));

        // 倒计时时间（内容无输入框，格式在时间设置中配置）
        AddTableRowLabel(grid, 4, "倒计时时间");
        var timeContentHint = new TextBlock { Text = "格式在时间设置中配置", FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(timeContentHint);
        AddTableCell(grid, 4, 1, timeContentHint);
        AddTableCell(grid, 4, 2, CreateSizeCell(out _timeFontSizeNumericUpDown, out _timeEnableCustomFontSizeToggle, OnTimeEnableCustomFontSizeChanged));
        AddTableCell(grid, 4, 3, CreateColorCell(out _timeFontColorPicker, out _timeEnableCustomFontColorToggle, OnTimeEnableCustomFontColorChanged));

        // 文案4
        AddTableRowLabel(grid, 5, "文案4");
        _text4TextBox = new TextBox { Watermark = "", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 5, 1, _text4TextBox);
        AddTableCell(grid, 5, 2, CreateSizeCell(out _text4FontSizeNumericUpDown, out _text4EnableCustomFontSizeToggle, OnText4EnableCustomFontSizeChanged));
        AddTableCell(grid, 5, 3, CreateColorCell(out _text4FontColorPicker, out _text4EnableCustomFontColorToggle, OnText4EnableCustomFontColorChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 6);
        Grid.SetColumnSpan(outerBorder, 4);
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

    private Grid CreateColorRow(string label, out ColorPicker? colorPicker)
    {
        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        colorPicker = new ColorPicker { Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        return row;
    }

    private void OnText2ButtonClick(object? sender, EventArgs e)
    {
        // 不再弹窗编辑：滚动到「倒计时列表」内联编辑面板，并确保有选中项。
        _listEditor?.FocusEditor();
    }

    private void UpdateThemeColors()
    {
        if (_titleTextBlock != null) _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        if (_descTextBlock != null) _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        if (_orderHintTextBlock != null) _orderHintTextBlock.Foreground = ThemeHelper.GetYellowBrush();
        if (_textGroupHeader != null) _textGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_timeGroupHeader != null) _timeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_formatGroupHeader != null) _formatGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_formatLabel != null) _formatLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_timeFormatHint != null) _timeFormatHint.Foreground = ThemeHelper.GetGrayBrush();
        if (_timeCorrectionToggle != null) _timeCorrectionToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeBaseGroupHeader != null) _timeBaseGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_timeBaseLabel != null) _timeBaseLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_appearanceGroupHeader != null) _appearanceGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeToggle != null) _simpleModeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeDesc != null) _simpleModeDesc.Foreground = ThemeHelper.GetGrayBrush();
        if (_progressDisplayModeLabel != null) _progressDisplayModeLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_enableCustomProgressColorToggle != null) _enableCustomProgressColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_listGroupHeader != null) _listGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        _listEditor?.UpdateThemeColors();

        if (_text1EnableCustomFontSizeToggle != null) _text1EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text1EnableCustomFontColorToggle != null) _text1EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text2EnableCustomFontSizeToggle != null) _text2EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text2EnableCustomFontColorToggle != null) _text2EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontSizeToggle != null) _text3EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontColorToggle != null) _text3EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontSizeToggle != null) _timeEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontColorToggle != null) _timeEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text4EnableCustomFontSizeToggle != null) _text4EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text4EnableCustomFontColorToggle != null) _text4EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();

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

    private void OnText2EnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.Text2EnableCustomFontSize = _text2EnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText2EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text2EnableCustomFontColor = _text2EnableCustomFontColorToggle?.IsChecked ?? false;
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

    private void OnEnableCustomProgressColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomProgressColor = _enableCustomProgressColorToggle?.IsChecked ?? false;
        UpdateProgressColorControlsEnabled();
    }

    private void OnSimpleModeToggleChanged(object? sender, EventArgs e)
    {
        Settings.EnableSimpleMode = _simpleModeToggle?.IsChecked ?? false;
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _text2FontSizeNumericUpDown.IsEnabled = Settings.Text2EnableCustomFontSize;
        _text2FontColorPicker.IsEnabled = Settings.Text2EnableCustomFontColor;
        _text3FontSizeNumericUpDown.IsEnabled = Settings.Text3EnableCustomFontSize;
        _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        _timeFontSizeNumericUpDown.IsEnabled = Settings.TimeEnableCustomFontSize;
        _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
        _text4FontSizeNumericUpDown.IsEnabled = Settings.Text4EnableCustomFontSize;
        _text4FontColorPicker.IsEnabled = Settings.Text4EnableCustomFontColor;
    }

    private void UpdateProgressColorControlsEnabled()
    {
        var isCustomEnabled = Settings.EnableCustomProgressColor;
        var showProgressBar = Settings.ProgressDisplayMode == ProgressDisplayMode.Bar || 
                              Settings.ProgressDisplayMode == ProgressDisplayMode.Both;
        
        _progressBarColorPicker.IsEnabled = isCustomEnabled && showProgressBar;
        _progressRingColorPicker.IsEnabled = isCustomEnabled;
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

        if (!Settings.WarningAccepted && _mainPanel != null)
        {
            var warningInfoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "Title", "作者提示");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "Message", "不推荐使用此功能实现放学倒计时或回家倒计时，否则使用者挨老师批，作者概不负责。关闭提示则代表同意此提示。");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "IsClosable", true);
            warningInfoBar.Margin = new Thickness(0, 0, 0, 8);
            FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(warningInfoBar, (s, e) =>
            {
                Settings.WarningAccepted = true;
            });
            _mainPanel.Children.Insert(0, warningInfoBar);
        }

        UpdateThemeColors();

        if (_text1TextBox != null) _text1TextBox.Text = Settings.Text1;
        if (_text3TextBox != null) _text3TextBox.Text = Settings.Text3;
        if (_text4TextBox != null) _text4TextBox.Text = Settings.Text4;
        if (_timeFormatTextBox != null) _timeFormatTextBox.Text = Settings.TimeFormat;
        if (_timeCorrectionToggle != null) _timeCorrectionToggle.IsChecked = Settings.EnableTimeCorrection;

        if (_timeBaseComboBox != null) _timeBaseComboBox.SelectedIndex = (int)Settings.TimeBaseType;

        if (_progressDisplayModeComboBox != null) _progressDisplayModeComboBox.SelectedIndex = (int)Settings.ProgressDisplayMode;
        if (_simpleModeToggle != null) _simpleModeToggle.IsChecked = Settings.EnableSimpleMode;

        if (_text1FontSizeNumericUpDown != null) _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        if (_text1FontColorPicker != null) _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        if (_text2FontSizeNumericUpDown != null) _text2FontSizeNumericUpDown.Value = (decimal)Settings.Text2FontSize;
        if (_text2FontColorPicker != null) _text2FontColorPicker.Color = ParseColor(Settings.Text2FontColor);
        if (_text3FontSizeNumericUpDown != null) _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        if (_text3FontColorPicker != null) _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        if (_timeFontSizeNumericUpDown != null) _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        if (_timeFontColorPicker != null) _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        if (_text4FontSizeNumericUpDown != null) _text4FontSizeNumericUpDown.Value = (decimal)Settings.Text4FontSize;
        if (_text4FontColorPicker != null) _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);

        AttachEventHandlers();

        // Settings 此时已注入，构建倒计时列表与详情面板
        _listEditor?.Refresh();

        if (_text1EnableCustomFontSizeToggle != null)
            _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        if (_text1EnableCustomFontColorToggle != null)
            _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        if (_text2EnableCustomFontSizeToggle != null)
            _text2EnableCustomFontSizeToggle.IsChecked = Settings.Text2EnableCustomFontSize;
        if (_text2EnableCustomFontColorToggle != null)
            _text2EnableCustomFontColorToggle.IsChecked = Settings.Text2EnableCustomFontColor;
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
        UpdateControlsEnabled();

        if (_enableCustomProgressColorToggle != null)
        {
            _enableCustomProgressColorToggle.IsChecked = Settings.EnableCustomProgressColor;
            UpdateProgressColorControlsEnabled();
        }
        if (_progressBarColorPicker != null) _progressBarColorPicker.Color = ParseColor(Settings.ProgressBarColor);
        if (_progressRingColorPicker != null) _progressRingColorPicker.Color = ParseColor(Settings.ProgressRingColor);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void AttachEventHandlers()
    {
        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "距离");
        AttachTextHandler(_text3TextBox, v => Settings.Text3 = v ?? "还有");
        AttachTextHandler(_text4TextBox, v => Settings.Text4 = v ?? "");
        AttachTextHandler(_timeFormatTextBox, v => Settings.TimeFormat = v ?? "%d天%h小时%m分钟%s秒");

        if (_timeBaseComboBox != null)
        {
            _timeBaseComboBox.SelectionChanged += (s, e) =>
            {
                if (_timeBaseComboBox != null && _timeBaseComboBox.SelectedIndex >= 0)
                {
                    Settings.TimeBaseType = (TimeBaseType)_timeBaseComboBox.SelectedIndex;
                }
            };
        }

        if (_progressDisplayModeComboBox != null)
        {
            _progressDisplayModeComboBox.SelectionChanged += (s, e) =>
            {
                if (_progressDisplayModeComboBox != null && _progressDisplayModeComboBox.SelectedIndex >= 0)
                {
                    Settings.ProgressDisplayMode = (ProgressDisplayMode)_progressDisplayModeComboBox.SelectedIndex;
                    UpdateProgressColorControlsEnabled();
                }
            };
        }

        AttachNumericUpDownHandler(_text1FontSizeNumericUpDown, v => Settings.Text1FontSize = v);
        AttachColorPickerHandler(_text1FontColorPicker, v => Settings.Text1FontColor = v);
        AttachNumericUpDownHandler(_text2FontSizeNumericUpDown, v => Settings.Text2FontSize = v);
        AttachColorPickerHandler(_text2FontColorPicker, v => Settings.Text2FontColor = v);
        AttachNumericUpDownHandler(_text3FontSizeNumericUpDown, v => Settings.Text3FontSize = v);
        AttachColorPickerHandler(_text3FontColorPicker, v => Settings.Text3FontColor = v);
        AttachNumericUpDownHandler(_timeFontSizeNumericUpDown, v => Settings.TimeFontSize = v);
        AttachColorPickerHandler(_timeFontColorPicker, v => Settings.TimeFontColor = v);
        AttachNumericUpDownHandler(_text4FontSizeNumericUpDown, v => Settings.Text4FontSize = v);
        AttachColorPickerHandler(_text4FontColorPicker, v => Settings.Text4FontColor = v);

        AttachColorPickerHandler(_progressBarColorPicker, v => Settings.ProgressBarColor = v);
        AttachColorPickerHandler(_progressRingColorPicker, v => Settings.ProgressRingColor = v);
    }

    private void AttachTextHandler(TextBox? textBox, Action<string?> setter)
    {
        if (textBox == null) return;
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => setter(textBox.Text));
    }

    private void AttachNumericUpDownHandler(NumericUpDown? numericUpDown, Action<double> setter)
    {
        if (numericUpDown == null) return;
        numericUpDown.ValueChanged += (s, e) =>
        {
            if (numericUpDown.Value.HasValue)
            {
                setter((double)numericUpDown.Value.Value);
            }
        };
    }

    private void AttachColorPickerHandler(ColorPicker? colorPicker, Action<string> setter)
    {
        if (colorPicker == null) return;
        colorPicker.ColorChanged += (s, e) => setter(colorPicker.Color.ToString());
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return Color.Parse(colorString);
        }
        catch
        {
            return Colors.White;
        }
    }

    private string GetPeriodTypeName(PeriodType periodType)
    {
        return periodType switch
        {
            PeriodType.Hourly => "每小时",
            PeriodType.Daily => "每天",
            PeriodType.Weekly => "每周",
            PeriodType.Monthly => "每月",
            PeriodType.Yearly => "每年",
            _ => "未知"
        };
    }

    /// <summary>
    /// 构建单个周期性倒计时的详情编辑面板（内联显示，不再使用弹窗）。
    /// 字段风格参考 ClassIsland「档案编辑」：官方 Field 标签在上、输入控件在下。
    /// </summary>
    private Control BuildPeriodicDetailPanel(PeriodicCountdownItem item)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(16)
        };

        panel.Children.Add(CountdownDetailFields.SectionHeader("\uec92", "周期性倒计时"));

        panel.Children.Add(CountdownDetailFields.Text("名称", item.Name, v =>
        {
            item.Name = string.IsNullOrWhiteSpace(v) ? "新周期性倒计时" : v;
            _listEditor?.RefreshRow(item);
        }, "新周期性倒计时"));

        // ---------- 周期 ----------
        var periodComboBox = new ComboBox();
        periodComboBox.Items.Add("每小时");
        periodComboBox.Items.Add("每天");
        periodComboBox.Items.Add("每周");
        periodComboBox.Items.Add("每月");
        periodComboBox.Items.Add("每年");
        periodComboBox.SelectedIndex = (int)item.PeriodType;

        var hourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) hourComboBox.Items.Add(i.ToString("D2"));
        hourComboBox.SelectedIndex = item.Hour;
        var minuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) minuteComboBox.Items.Add(i.ToString("D2"));
        minuteComboBox.SelectedIndex = item.Minute;
        var secondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) secondComboBox.Items.Add(i.ToString("D2"));
        secondComboBox.SelectedIndex = item.Second;

        var timePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        timePanel.Children.Add(hourComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(minuteComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(secondComboBox);

        // 星期 / 日期 / 月份（按周期类型显隐）
        var dayOfWeekComboBox = new ComboBox();
        foreach (var d in new[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" })
        {
            dayOfWeekComboBox.Items.Add(d);
        }
        dayOfWeekComboBox.SelectedIndex = item.DayOfWeek;
        var dayOfWeekField = CountdownDetailFields.Combo("星期", dayOfWeekComboBox, () =>
        {
            if (dayOfWeekComboBox.SelectedIndex >= 0)
            {
                item.DayOfWeek = dayOfWeekComboBox.SelectedIndex;
                _listEditor?.RefreshRow(item);
            }
        });

        var dayOfMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) dayOfMonthComboBox.Items.Add($"{i}日");
        dayOfMonthComboBox.SelectedItem = $"{item.DayOfMonth}日";
        var dayOfMonthField = CountdownDetailFields.Combo("日期", dayOfMonthComboBox, () =>
        {
            if (dayOfMonthComboBox.SelectedItem != null &&
                int.TryParse(dayOfMonthComboBox.SelectedItem.ToString()?.Replace("日", ""), out var dom))
            {
                item.DayOfMonth = dom;
                _listEditor?.RefreshRow(item);
            }
        });

        var monthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthComboBox.Items.Add($"{i}月");
        monthComboBox.SelectedIndex = item.Month - 1;
        var monthField = CountdownDetailFields.Combo("月份", monthComboBox, () =>
        {
            if (monthComboBox.SelectedIndex >= 0)
            {
                item.Month = monthComboBox.SelectedIndex + 1;
                _listEditor?.RefreshRow(item);
            }
        });

        void UpdateExtraVisibility()
        {
            var selected = (PeriodType)(periodComboBox.SelectedIndex >= 0 ? periodComboBox.SelectedIndex : (int)item.PeriodType);
            dayOfWeekField.IsVisible = selected == PeriodType.Weekly;
            dayOfMonthField.IsVisible = selected == PeriodType.Monthly || selected == PeriodType.Yearly;
            monthField.IsVisible = selected == PeriodType.Yearly;
        }

        void CommitTime()
        {
            if (hourComboBox.SelectedIndex >= 0) item.Hour = hourComboBox.SelectedIndex;
            if (minuteComboBox.SelectedIndex >= 0) item.Minute = minuteComboBox.SelectedIndex;
            if (secondComboBox.SelectedIndex >= 0) item.Second = secondComboBox.SelectedIndex;
            item.IsCompleted = false;
            _listEditor?.RefreshRow(item);
        }

        periodComboBox.SelectionChanged += (s, e) =>
        {
            if (periodComboBox.SelectedIndex >= 0)
            {
                item.PeriodType = (PeriodType)periodComboBox.SelectedIndex;
                item.IsCompleted = false;
                UpdateExtraVisibility();
                _listEditor?.RefreshRow(item);
            }
        };
        hourComboBox.SelectionChanged += (s, e) => CommitTime();
        minuteComboBox.SelectionChanged += (s, e) => CommitTime();
        secondComboBox.SelectionChanged += (s, e) => CommitTime();

        panel.Children.Add(CountdownDetailFields.Labeled("周期", periodComboBox));
        panel.Children.Add(CountdownDetailFields.Labeled("时间", timePanel));
        panel.Children.Add(dayOfWeekField);
        panel.Children.Add(dayOfMonthField);
        panel.Children.Add(monthField);
        UpdateExtraVisibility();

        panel.Children.Add(CountdownDetailFields.NotificationSection(
            item.EnableNotification,
            v => item.EnableNotification = v,
            () => item.NotificationTitle,
            v => item.NotificationTitle = string.IsNullOrWhiteSpace(v) ? "周期性倒计时到达" : v,
            () => item.NotificationContent,
            v => item.NotificationContent = string.IsNullOrWhiteSpace(v) ? "目标时间已到达！" : v,
            () => item.NotificationMaskDurationSeconds,
            v => item.NotificationMaskDurationSeconds = v,
            () => item.NotificationOverlayDurationSeconds,
            v => item.NotificationOverlayDurationSeconds = v));

        return panel;
    }

    /// <summary>列表副标题：周期发生的具体时刻描述。</summary>
    private static string DescribePeriodicTime(PeriodicCountdownItem item)
    {
        var time = $"{item.Hour:D2}:{item.Minute:D2}:{item.Second:D2}";
        return item.PeriodType switch
        {
            PeriodType.Hourly => $" 每小时第 {item.Minute} 分 {item.Second} 秒",
            PeriodType.Daily => $" {time}",
            PeriodType.Weekly => $" {WeekName(item.DayOfWeek)} {time}",
            PeriodType.Monthly => $" 每月 {item.DayOfMonth} 日 {time}",
            PeriodType.Yearly => $" 每年 {item.Month}月{item.DayOfMonth}日 {time}",
            _ => $" {time}"
        };
    }

    private static string WeekName(int dayOfWeek) => dayOfWeek switch
    {
        0 => "星期日",
        1 => "星期一",
        2 => "星期二",
        3 => "星期三",
        4 => "星期四",
        5 => "星期五",
        6 => "星期六",
        _ => "星期一"
    };

}
