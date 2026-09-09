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

public class CountdownSettingsControl : ComponentBase<CountdownSettings>
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
    private CheckBox? _text1EnableCustomFontFamilyToggle;
    private CheckBox? _text2EnableCustomFontFamilyToggle;
    private CheckBox? _text3EnableCustomFontFamilyToggle;
    private CheckBox? _timeEnableCustomFontFamilyToggle;
    private CheckBox? _text4EnableCustomFontFamilyToggle;
    private CheckBox? _text1EnableCustomFontWeightToggle;
    private CheckBox? _text2EnableCustomFontWeightToggle;
    private CheckBox? _text3EnableCustomFontWeightToggle;
    private CheckBox? _timeEnableCustomFontWeightToggle;
    private CheckBox? _text4EnableCustomFontWeightToggle;
    private ComboBox? _timeBaseComboBox;
    private ListBox? _countdownListBox;
    private Button? _addButton;
    private Button? _removeButton;
    private Button? _editButton;

    private TextBlock? _selectionHintTextBlock;
    private System.Timers.Timer? _hintTimer;

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
    private ComboBox? _text1FontFamilyComboBox;
    private ComboBox? _text2FontFamilyComboBox;
    private ComboBox? _text3FontFamilyComboBox;
    private ComboBox? _timeFontFamilyComboBox;
    private ComboBox? _text4FontFamilyComboBox;
    private ComboBox? _text1FontWeightComboBox;
    private ComboBox? _text2FontWeightComboBox;
    private ComboBox? _text3FontWeightComboBox;
    private ComboBox? _timeFontWeightComboBox;
    private ComboBox? _text4FontWeightComboBox;

    private TextBox? _startYearTextBox;
    private ComboBox? _startMonthComboBox;
    private ComboBox? _startDayComboBox;
    private ComboBox? _startHourComboBox;
    private ComboBox? _startMinuteComboBox;
    private ComboBox? _startSecondComboBox;

    private ComboBox? _progressDisplayModeComboBox;
    private TextBlock? _progressDisplayModeLabel;
    private ToggleSwitch? _enableCustomProgressColorToggle;
    private ColorPicker? _progressBarColorPicker;
    private ColorPicker? _progressRingColorPicker;

    private TextBlock? _titleTextBlock;
    private TextBlock? _descTextBlock;
    private TextBlock? _orderHintTextBlock;
    private TextBlock? _textGroupHeader;
    private TextBlock? _timeGroupHeader;
    private TextBlock? _appearanceGroupHeader;
    private ToggleSwitch? _simpleModeToggle;
    private TextBlock? _simpleModeDesc;
    private TextBlock? _formatGroupHeader;
    private TextBlock? _formatLabel;
    private TextBlock? _timeBaseGroupHeader;
    private TextBlock? _timeBaseLabel;
    private TextBlock? _startTimeGroupHeader;
    private TextBlock? _startDateLabel;
    private TextBlock? _startTimeLabel;
    private TextBlock? _hourSeparator;
    private TextBlock? _minuteSeparator;
    private TextBlock? _listGroupHeader;
    private TextBlock? _nameHeader;
    private TextBlock? _notifyHeader;

    private List<TextBlock> _dynamicTextBlocks = new();
    private List<Border> _tableCellBorders = new();

    public CountdownSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "多倒计时设置", FontSize = 14, FontWeight = FontWeight.Bold };
        mainPanel.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "配置倒计时显示选项和倒计时列表", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        mainPanel.Children.Add(_descTextBlock);

        // ==================== 文案设置（表格） ====================
        _textGroupHeader = new TextBlock { Text = "文案设置" };
        var textGroup = new Expander { Header = _textGroupHeader, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _orderHintTextBlock = new TextBlock
        {
            Text = "以下内容在主界面上显示的顺序为：文案1->倒计时名称->文案3->剩余时间->文案4",
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

        // --- 开始时间 ---
        _startTimeGroupHeader = new TextBlock { Text = "开始时间", FontSize = 12, FontWeight = FontWeight.Bold };
        timePanel.Children.Add(_startTimeGroupHeader);

        var startDateRow = new Grid { ColumnSpacing = 6 };
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

        var startTimeRow = new Grid { ColumnSpacing = 6 };
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

        // --- 时间基准 ---
        _timeBaseGroupHeader = new TextBlock { Text = "时间基准", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 0) };
        timePanel.Children.Add(_timeBaseGroupHeader);

        var timeBaseRow = new Grid();
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _timeBaseLabel = new TextBlock { Text = "时间基准:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_timeBaseLabel, 0);
        timeBaseRow.Children.Add(_timeBaseLabel);

        _timeBaseComboBox = new ComboBox();
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("ClassIsland时间");
        Grid.SetColumn(_timeBaseComboBox, 1);
        timeBaseRow.Children.Add(_timeBaseComboBox);

        timePanel.Children.Add(timeBaseRow);

        // --- 时间格式 ---
        _formatGroupHeader = new TextBlock { Text = "时间格式", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 0) };
        timePanel.Children.Add(_formatGroupHeader);

        var formatRow = new Grid();
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _formatLabel = new TextBlock { Text = "时间格式:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
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

        // --- 倒计时列表 ---
        _listGroupHeader = new TextBlock { Text = "倒计时列表", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 0) };
        timePanel.Children.Add(_listGroupHeader);

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        _nameHeader = new TextBlock
        {
            Text = "倒计时目标时间",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_nameHeader, 0);
        headerGrid.Children.Add(_nameHeader);

        _notifyHeader = new TextBlock
        {
            Text = "启用通知？",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(_notifyHeader, 1);
        headerGrid.Children.Add(_notifyHeader);
        timePanel.Children.Add(headerGrid);

        _countdownListBox = new ListBox { Height = 150, SelectionMode = SelectionMode.Single };
        _countdownListBox.SelectionChanged += (s, e) =>
        {
            if (_countdownListBox != null && _countdownListBox.SelectedIndex >= 0)
            {
                HideHint();
            }
        };
        timePanel.Children.Add(_countdownListBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

        _addButton = new Button { Content = "添加", Width = 60 };
        _addButton.Click += OnAddClick;
        buttonPanel.Children.Add(_addButton);

        _removeButton = new Button { Content = "删除", Width = 60 };
        _removeButton.Click += OnRemoveClick;
        buttonPanel.Children.Add(_removeButton);

        _editButton = new Button { Content = "编辑", Width = 60 };
        _editButton.Click += OnEditClick;
        buttonPanel.Children.Add(_editButton);

        timePanel.Children.Add(buttonPanel);

        _selectionHintTextBlock = new TextBlock
        {
            Text = "请选择一个倒计时",
            Foreground = Brushes.Orange,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 12
        };
        timePanel.Children.Add(_selectionHintTextBlock);

        timeGroup.Content = timePanel;
        mainPanel.Children.Add(timeGroup);

        // ==================== 外观设置（新增） ====================
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
            Text = "开启后，文案只显示倒计时名称与剩余时间（文案1/文案3/文案4不再显示）。默认关闭。",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        appearancePanel.Children.Add(_simpleModeDesc);

        _progressDisplayModeLabel = new TextBlock { Text = "显示进度条:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
        var progressDisplayModeRow = new Grid();
        progressDisplayModeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        progressDisplayModeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(_progressDisplayModeLabel, 0);
        progressDisplayModeRow.Children.Add(_progressDisplayModeLabel);

        _progressDisplayModeComboBox = new ComboBox();
        _progressDisplayModeComboBox.Items.Add("不显示");
        _progressDisplayModeComboBox.Items.Add("进度条");
        _progressDisplayModeComboBox.Items.Add("进度环");
        _progressDisplayModeComboBox.Items.Add("进度条和进度环");
        Grid.SetColumn(_progressDisplayModeComboBox, 1);
        progressDisplayModeRow.Children.Add(_progressDisplayModeComboBox);

        appearancePanel.Children.Add(progressDisplayModeRow);

        _enableCustomProgressColorToggle = new ToggleSwitch { Content = "启用自定义进度颜色", Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        _enableCustomProgressColorToggle.IsCheckedChanged += OnEnableCustomProgressColorChanged;
        appearancePanel.Children.Add(_enableCustomProgressColorToggle);

        var progressBarColorRow = CreateColorRow("进度条颜色:", out _progressBarColorPicker);
        appearancePanel.Children.Add(progressBarColorRow);

        var progressRingColorRow = CreateColorRow("进度环颜色:", out _progressRingColorPicker);
        appearancePanel.Children.Add(progressRingColorRow);

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
        _text1TextBox = new TextBox { Watermark = "距离", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 1, 1, _text1TextBox);
        AddTableCell(grid, 1, 2, CreateSizeCell(out _text1FontSizeNumericUpDown, out _text1EnableCustomFontSizeToggle, OnText1EnableCustomFontSizeChanged));
        AddTableCell(grid, 1, 3, CreateColorCell(out _text1FontColorPicker, out _text1EnableCustomFontColorToggle, OnText1EnableCustomFontColorChanged));
        AddTableCell(grid, 1, 4, CreateFamilyCell(out _text1FontFamilyComboBox, out _text1EnableCustomFontFamilyToggle, OnText1EnableCustomFontFamilyChanged));
        AddTableCell(grid, 1, 5, CreateWeightCell(out _text1FontWeightComboBox, out _text1EnableCustomFontWeightToggle, OnText1EnableCustomFontWeightChanged));

        // 倒计时名称
        AddTableRowLabel(grid, 2, "倒计时名称");
        var editNameButton = new Button { Content = "前往编辑倒计时名称", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        editNameButton.Click += OnText2ButtonClick;
        AddTableCell(grid, 2, 1, editNameButton);
        AddTableCell(grid, 2, 2, CreateSizeCell(out _text2FontSizeNumericUpDown, out _text2EnableCustomFontSizeToggle, OnText2EnableCustomFontSizeChanged));
        AddTableCell(grid, 2, 3, CreateColorCell(out _text2FontColorPicker, out _text2EnableCustomFontColorToggle, OnText2EnableCustomFontColorChanged));
        AddTableCell(grid, 2, 4, CreateFamilyCell(out _text2FontFamilyComboBox, out _text2EnableCustomFontFamilyToggle, OnText2EnableCustomFontFamilyChanged));
        AddTableCell(grid, 2, 5, CreateWeightCell(out _text2FontWeightComboBox, out _text2EnableCustomFontWeightToggle, OnText2EnableCustomFontWeightChanged));

        // 文案3
        AddTableRowLabel(grid, 3, "文案3");
        _text3TextBox = new TextBox { Watermark = "还有", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 3, 1, _text3TextBox);
        AddTableCell(grid, 3, 2, CreateSizeCell(out _text3FontSizeNumericUpDown, out _text3EnableCustomFontSizeToggle, OnText3EnableCustomFontSizeChanged));
        AddTableCell(grid, 3, 3, CreateColorCell(out _text3FontColorPicker, out _text3EnableCustomFontColorToggle, OnText3EnableCustomFontColorChanged));
        AddTableCell(grid, 3, 4, CreateFamilyCell(out _text3FontFamilyComboBox, out _text3EnableCustomFontFamilyToggle, OnText3EnableCustomFontFamilyChanged));
        AddTableCell(grid, 3, 5, CreateWeightCell(out _text3FontWeightComboBox, out _text3EnableCustomFontWeightToggle, OnText3EnableCustomFontWeightChanged));

        // 倒计时时间（内容无输入框，格式在时间设置中配置）
        AddTableRowLabel(grid, 4, "倒计时时间");
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

    private void OnText2ButtonClick(object? sender, EventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
        }
        else
        {
            if (_countdownListBox != null)
            {
                _countdownListBox.BringIntoView();
            }
            ShowHint();
        }
    }

    private void UpdateThemeColors()
    {
        if (_titleTextBlock != null) _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        if (_descTextBlock != null) _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        if (_orderHintTextBlock != null) _orderHintTextBlock.Foreground = ThemeHelper.GetYellowBrush();
        if (_textGroupHeader != null) _textGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_formatGroupHeader != null) _formatGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_formatLabel != null) _formatLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_timeFormatHint != null) _timeFormatHint.Foreground = ThemeHelper.GetGrayBrush();
        if (_timeBaseGroupHeader != null) _timeBaseGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_timeBaseLabel != null) _timeBaseLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_startTimeGroupHeader != null) _startTimeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_startDateLabel != null) _startDateLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_startTimeLabel != null) _startTimeLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_hourSeparator != null) _hourSeparator.Foreground = ThemeHelper.GetTextBrush();
        if (_minuteSeparator != null) _minuteSeparator.Foreground = ThemeHelper.GetTextBrush();
        if (_timeGroupHeader != null) _timeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_appearanceGroupHeader != null) _appearanceGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeToggle != null) _simpleModeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeDesc != null) _simpleModeDesc.Foreground = ThemeHelper.GetGrayBrush();
        if (_progressDisplayModeLabel != null) _progressDisplayModeLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_listGroupHeader != null) _listGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_nameHeader != null) _nameHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_notifyHeader != null) _notifyHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_selectionHintTextBlock != null) _selectionHintTextBlock.Foreground = ThemeHelper.GetOrangeBrush();

        if (_enableCustomProgressColorToggle != null) _enableCustomProgressColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeCorrectionToggle != null) _timeCorrectionToggle.Foreground = ThemeHelper.GetTextBrush();

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
        if (_text1EnableCustomFontWeightToggle != null) _text1EnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text2EnableCustomFontWeightToggle != null) _text2EnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
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

    private void OnText1EnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontFamily = _text1EnableCustomFontFamilyToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText2EnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.Text2EnableCustomFontFamily = _text2EnableCustomFontFamilyToggle?.IsChecked ?? false;
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

    private void OnText2EnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.Text2EnableCustomFontWeight = _text2EnableCustomFontWeightToggle?.IsChecked ?? false;
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

    private void OnEnableCustomProgressColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomProgressColor = _enableCustomProgressColorToggle?.IsChecked ?? false;
        UpdateProgressColorControlsEnabled();
    }

    private void OnSimpleModeToggleChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableSimpleMode = _simpleModeToggle?.IsChecked ?? false;
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _text1FontFamilyComboBox.IsEnabled = Settings.Text1EnableCustomFontFamily;
        _text1FontWeightComboBox.IsEnabled = Settings.Text1EnableCustomFontWeight;
        _text2FontSizeNumericUpDown.IsEnabled = Settings.Text2EnableCustomFontSize;
        _text2FontColorPicker.IsEnabled = Settings.Text2EnableCustomFontColor;
        _text2FontFamilyComboBox.IsEnabled = Settings.Text2EnableCustomFontFamily;
        _text2FontWeightComboBox.IsEnabled = Settings.Text2EnableCustomFontWeight;
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
        UpdateThemeColors();

        if (_text1TextBox != null) _text1TextBox.Text = Settings.Text1;
        if (_text3TextBox != null) _text3TextBox.Text = Settings.Text3;
        if (_text4TextBox != null) _text4TextBox.Text = Settings.Text4;
        if (_timeFormatTextBox != null) _timeFormatTextBox.Text = Settings.TimeFormat;
        if (_timeCorrectionToggle != null) _timeCorrectionToggle.IsChecked = Settings.EnableTimeCorrection;

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
        if (_text1FontFamilyComboBox != null) _text1FontFamilyComboBox.SelectedItem = Settings.Text1FontFamily;
        if (_text2FontFamilyComboBox != null) _text2FontFamilyComboBox.SelectedItem = Settings.Text2FontFamily;
        if (_text3FontFamilyComboBox != null) _text3FontFamilyComboBox.SelectedItem = Settings.Text3FontFamily;
        if (_timeFontFamilyComboBox != null) _timeFontFamilyComboBox.SelectedItem = Settings.TimeFontFamily;
        if (_text4FontFamilyComboBox != null) _text4FontFamilyComboBox.SelectedItem = Settings.Text4FontFamily;
        if (_text1FontWeightComboBox != null) _text1FontWeightComboBox.SelectedItem = Settings.Text1FontWeight;
        if (_text2FontWeightComboBox != null) _text2FontWeightComboBox.SelectedItem = Settings.Text2FontWeight;
        if (_text3FontWeightComboBox != null) _text3FontWeightComboBox.SelectedItem = Settings.Text3FontWeight;
        if (_timeFontWeightComboBox != null) _timeFontWeightComboBox.SelectedItem = Settings.TimeFontWeight;
        if (_text4FontWeightComboBox != null) _text4FontWeightComboBox.SelectedItem = Settings.Text4FontWeight;

        if (_startYearTextBox != null && _startMonthComboBox != null && _startDayComboBox != null && _startHourComboBox != null && _startMinuteComboBox != null && _startSecondComboBox != null)
        {
            var startTime = UnixTimeHelper.FromUnixTimestamp(Settings.StartTime);
            _startYearTextBox.Text = startTime.Year.ToString();
            _startMonthComboBox.SelectedIndex = startTime.Month - 1;
            _startDayComboBox.SelectedItem = $"{startTime.Day}日";
            _startHourComboBox.SelectedIndex = startTime.Hour;
            _startMinuteComboBox.SelectedIndex = startTime.Minute;
            _startSecondComboBox.SelectedIndex = startTime.Second;
        }

        UpdateCountdownList();

        AttachEventHandlers();

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
        if (_text1EnableCustomFontFamilyToggle != null)
            _text1EnableCustomFontFamilyToggle.IsChecked = Settings.Text1EnableCustomFontFamily;
        if (_text2EnableCustomFontFamilyToggle != null)
            _text2EnableCustomFontFamilyToggle.IsChecked = Settings.Text2EnableCustomFontFamily;
        if (_text3EnableCustomFontFamilyToggle != null)
            _text3EnableCustomFontFamilyToggle.IsChecked = Settings.Text3EnableCustomFontFamily;
        if (_timeEnableCustomFontFamilyToggle != null)
            _timeEnableCustomFontFamilyToggle.IsChecked = Settings.TimeEnableCustomFontFamily;
        if (_text4EnableCustomFontFamilyToggle != null)
            _text4EnableCustomFontFamilyToggle.IsChecked = Settings.Text4EnableCustomFontFamily;
        if (_text1EnableCustomFontWeightToggle != null)
            _text1EnableCustomFontWeightToggle.IsChecked = Settings.Text1EnableCustomFontWeight;
        if (_text2EnableCustomFontWeightToggle != null)
            _text2EnableCustomFontWeightToggle.IsChecked = Settings.Text2EnableCustomFontWeight;
        if (_text3EnableCustomFontWeightToggle != null)
            _text3EnableCustomFontWeightToggle.IsChecked = Settings.Text3EnableCustomFontWeight;
        if (_timeEnableCustomFontWeightToggle != null)
            _timeEnableCustomFontWeightToggle.IsChecked = Settings.TimeEnableCustomFontWeight;
        if (_text4EnableCustomFontWeightToggle != null)
            _text4EnableCustomFontWeightToggle.IsChecked = Settings.Text4EnableCustomFontWeight;
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
        _hintTimer?.Stop();
        _hintTimer?.Dispose();
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
                    Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
                    {
                        0 => TimeBaseType.PluginOffsetServerTime,
                        1 => TimeBaseType.RawServerTime,
                        2 => TimeBaseType.ClassIslandTime,
                        _ => TimeBaseType.PluginOffsetServerTime
                    };
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
        AttachFontFamilyHandler(_text1FontFamilyComboBox, v => Settings.Text1FontFamily = v);
        AttachFontFamilyHandler(_text2FontFamilyComboBox, v => Settings.Text2FontFamily = v);
        AttachFontFamilyHandler(_text3FontFamilyComboBox, v => Settings.Text3FontFamily = v);
        AttachFontFamilyHandler(_timeFontFamilyComboBox, v => Settings.TimeFontFamily = v);
        AttachFontFamilyHandler(_text4FontFamilyComboBox, v => Settings.Text4FontFamily = v);
        AttachFontWeightHandler(_text1FontWeightComboBox, v => Settings.Text1FontWeight = v);
        AttachFontWeightHandler(_text2FontWeightComboBox, v => Settings.Text2FontWeight = v);
        AttachFontWeightHandler(_text3FontWeightComboBox, v => Settings.Text3FontWeight = v);
        AttachFontWeightHandler(_timeFontWeightComboBox, v => Settings.TimeFontWeight = v);
        AttachFontWeightHandler(_text4FontWeightComboBox, v => Settings.Text4FontWeight = v);

        AttachColorPickerHandler(_progressBarColorPicker, v => Settings.ProgressBarColor = v);
        AttachColorPickerHandler(_progressRingColorPicker, v => Settings.ProgressRingColor = v);

        AttachStartTimeHandlers();
    }

    private void AttachStartTimeHandlers()
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
                    var startTime = new DateTime(year, month, day, _startHourComboBox.SelectedIndex,
                        _startMinuteComboBox.SelectedIndex, _startSecondComboBox.SelectedIndex);
                    Settings.StartTime = UnixTimeHelper.ToUnixTimestamp(startTime);
                }
                catch { }
            }
        }

        if (_startYearTextBox != null)
        {
            FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startYearTextBox, (s, e) => UpdateStartTime());
        }

        if (_startMonthComboBox != null)
        {
            _startMonthComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        }

        if (_startDayComboBox != null)
        {
            _startDayComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        }

        if (_startHourComboBox != null)
        {
            _startHourComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        }

        if (_startMinuteComboBox != null)
        {
            _startMinuteComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        }

        if (_startSecondComboBox != null)
        {
            _startSecondComboBox.SelectionChanged += (s, e) => UpdateStartTime();
        }
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

    private void ParseAndSetNumber(TextBox textBox, Action<int> setter)
    {
        if (int.TryParse(textBox.Text, out int value))
        {
            setter(value);
            textBox.Text = value.ToString();
        }
        else
        {
            textBox.Text = "5";
        }
    }

    private void UpdateCountdownList()
    {
        if (_countdownListBox == null) return;
        _countdownListBox.Items.Clear();
        if (Settings.CountdownItems != null)
        {
            foreach (var item in Settings.CountdownItems)
            {
                var targetTime = UnixTimeHelper.FromUnixTimestamp(item.TargetTimestamp);

                var container = new Grid();
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                container.Tag = item;

                var textBlock = new TextBlock
                {
                    Text = $"{item.Name} - {targetTime:yyyy-MM-dd HH:mm:ss}",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetTextBrush(),
                    Padding = new Avalonia.Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(textBlock, 0);
                container.Children.Add(textBlock);

                var notifyPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                var notifySwitch = new ToggleSwitch
                {
                    IsChecked = item.EnableNotification,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 4, 0, 4)
                };
                var currentItem = item;
                notifySwitch.IsCheckedChanged += (s, e) =>
                {
                    currentItem.EnableNotification = notifySwitch.IsChecked == true;
                };
                notifyPanel.Children.Add(notifySwitch);
                Grid.SetColumn(notifyPanel, 1);
                container.Children.Add(notifyPanel);

                var listBoxItem = new ListBoxItem
                {
                    Content = container,
                    Tag = item
                };

                _countdownListBox.Items.Add(listBoxItem);
            }
        }
    }

    private void OnAddClick(object? sender, EventArgs e)
    {
        HideHint();
        if (Settings.CountdownItems == null)
        {
            Settings.CountdownItems = new List<CountdownItem>();
        }
        Settings.CountdownItems.Add(CountdownItem.CreateDefault());
        UpdateCountdownList();
    }

    private void OnRemoveClick(object? sender, EventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox != null && _countdownListBox.SelectedIndex >= 0)
        {
            Settings.CountdownItems.RemoveAt(_countdownListBox.SelectedIndex);
            UpdateCountdownList();
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void OnEditClick(object? sender, EventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void ShowHint()
    {
        if (_selectionHintTextBlock != null)
        {
            _selectionHintTextBlock.IsVisible = true;
            _hintTimer?.Stop();
            _hintTimer = new System.Timers.Timer(5000);
            _hintTimer.Elapsed += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(HideHint);
            };
            _hintTimer.AutoReset = false;
            _hintTimer.Start();
        }
    }

    private void HideHint()
    {
        if (_selectionHintTextBlock != null)
        {
            _selectionHintTextBlock.IsVisible = false;
            _hintTimer?.Stop();
        }
    }

    private async void ShowEditDialog(CountdownItem item, int order = 0)
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", order > 0 ? $"正在编写第{order}个倒计时" : "编辑倒计时");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "SecondaryButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var nameLabel = new TextBlock { Text = "名称:", Foreground = ThemeHelper.GetTextBrush() };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var targetLabel = new TextBlock { Text = "目标时间:", Foreground = ThemeHelper.GetTextBrush() };
        contentPanel.Children.Add(targetLabel);

        var targetTime = UnixTimeHelper.FromUnixTimestamp(item.TargetTimestamp);

        var datePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var yearTextBox = new TextBox { Width = 80, Watermark = "年", Text = targetTime.Year.ToString() };
        var monthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthComboBox.Items.Add($"{i}月");
        monthComboBox.SelectedIndex = targetTime.Month - 1;
        var dayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) dayComboBox.Items.Add($"{i}日");
        dayComboBox.SelectedItem = $"{targetTime.Day}日";

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(yearTextBox, (s, e) => UpdateDayComboBox(yearTextBox, monthComboBox, dayComboBox));
        monthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(yearTextBox, monthComboBox, dayComboBox);

        datePanel.Children.Add(yearTextBox);
        datePanel.Children.Add(monthComboBox);
        datePanel.Children.Add(dayComboBox);
        contentPanel.Children.Add(datePanel);

        var timePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var hourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) hourComboBox.Items.Add(i.ToString("D2"));
        hourComboBox.SelectedIndex = targetTime.Hour;
        var minuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) minuteComboBox.Items.Add(i.ToString("D2"));
        minuteComboBox.SelectedIndex = targetTime.Minute;
        var secondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) secondComboBox.Items.Add(i.ToString("D2"));
        secondComboBox.SelectedIndex = targetTime.Second;

        timePanel.Children.Add(hourComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(minuteComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(secondComboBox);
        contentPanel.Children.Add(timePanel);

        var notifyToggle = new ToggleSwitch { Content = "启用通知", IsChecked = item.EnableNotification };
        contentPanel.Children.Add(notifyToggle);

        var notifyTitleLabel = new TextBlock { Text = "通知标题:", Foreground = ThemeHelper.GetTextBrush() };
        var notifyTitleTextBox = new TextBox { Text = item.NotificationTitle };
        contentPanel.Children.Add(notifyTitleLabel);
        contentPanel.Children.Add(notifyTitleTextBox);

        var notifyContentLabel = new TextBlock { Text = "通知内容:", Foreground = ThemeHelper.GetTextBrush() };
        var notifyContentTextBox = new TextBox { Text = item.NotificationContent };
        contentPanel.Children.Add(notifyContentLabel);
        contentPanel.Children.Add(notifyContentTextBox);

        var maskDurationLabel = new TextBlock { Text = "通知标题时长(秒):", Foreground = ThemeHelper.GetTextBrush() };
        var maskDurationTextBox = new TextBox { Text = item.NotificationMaskDurationSeconds.ToString() };
        contentPanel.Children.Add(maskDurationLabel);
        contentPanel.Children.Add(maskDurationTextBox);

        var overlayDurationLabel = new TextBlock { Text = "通知内容时长(秒):", Foreground = ThemeHelper.GetTextBrush() };
        var overlayDurationTextBox = new TextBox { Text = item.NotificationOverlayDurationSeconds.ToString() };
        contentPanel.Children.Add(overlayDurationLabel);
        contentPanel.Children.Add(overlayDurationTextBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = contentPanel,
            Margin = new Avalonia.Thickness(12, 12, 12, 0)
        };

        var mainPanel = new Grid();
        mainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(scrollViewer, 0);
        mainPanel.Children.Add(scrollViewer);

        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", mainPanel);

        FluentAvaloniaCompatibilityHelper.AddContentDialogButtonClickHandler(dialog, "PrimaryButtonClick", (s, e) =>
        {
            item.Name = nameTextBox.Text ?? "新倒计时";

            if (int.TryParse(yearTextBox.Text?.Trim(), out var year) &&
                monthComboBox.SelectedIndex >= 0 &&
                dayComboBox.SelectedItem != null &&
                int.TryParse(dayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) &&
                hourComboBox.SelectedIndex >= 0 &&
                minuteComboBox.SelectedIndex >= 0 &&
                secondComboBox.SelectedIndex >= 0)
            {
                try
                {
                    var target = new DateTime(year, monthComboBox.SelectedIndex + 1, day,
                        hourComboBox.SelectedIndex, minuteComboBox.SelectedIndex, secondComboBox.SelectedIndex);
                    item.TargetTimestamp = UnixTimeHelper.ToUnixTimestamp(target);
                }
                catch { }
            }

            item.EnableNotification = notifyToggle.IsChecked == true;
            item.NotificationTitle = notifyTitleTextBox.Text ?? "倒计时到达";
            item.NotificationContent = notifyContentTextBox.Text ?? "目标时间已到达！";

            if (int.TryParse(maskDurationTextBox.Text, out int maskDuration))
            {
                item.NotificationMaskDurationSeconds = maskDuration;
            }

            if (int.TryParse(overlayDurationTextBox.Text, out int overlayDuration))
            {
                item.NotificationOverlayDurationSeconds = overlayDuration;
            }

            item.IsCompleted = false;
            UpdateCountdownList();
        });

        await FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, TopLevel.GetTopLevel(this));
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


