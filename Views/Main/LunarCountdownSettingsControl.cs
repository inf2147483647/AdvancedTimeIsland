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

public class LunarCountdownSettingsControl : ComponentBase<LunarCountdownSettings>
{
    private TextBox? _text1TextBox;
    private TextBox? _text3TextBox;
    private TextBox? _text4TextBox;
    private TextBox? _timeFormatTextBox;
    private TextBlock? _timeFormatHint;
    private ComboBox? _timeBaseComboBox;
    private ToggleSwitch? _text1EnableCustomFontSizeToggle;
    private ToggleSwitch? _text1EnableCustomFontColorToggle;
    private ToggleSwitch? _nameEnableCustomFontSizeToggle;
    private ToggleSwitch? _nameEnableCustomFontColorToggle;
    private ToggleSwitch? _text3EnableCustomFontSizeToggle;
    private ToggleSwitch? _text3EnableCustomFontColorToggle;
    private ToggleSwitch? _timeEnableCustomFontSizeToggle;
    private ToggleSwitch? _timeEnableCustomFontColorToggle;
    private ToggleSwitch? _text4EnableCustomFontSizeToggle;
    private ToggleSwitch? _text4EnableCustomFontColorToggle;
    private ListBox? _countdownListBox;
    private Button? _addButton;
    private Button? _removeButton;
    private Button? _editButton;

    private TextBlock? _selectionHintTextBlock;
    private System.Timers.Timer? _hintTimer;

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

    private TextBlock? _titleTextBlock;
    private TextBlock? _descTextBlock;
    private TextBlock? _orderHintTextBlock;
    private TextBlock? _textGroupHeader;
    private TextBlock? _formatGroupHeader;
    private TextBlock? _timeBaseGroupHeader;
    private TextBlock? _timeBaseLabel;
    private TextBlock? _fontGroupHeader;
    private TextBlock? _listGroupHeader;
    private TextBlock? _lunarHeader;
    private TextBlock? _solarHeader;
    private TextBlock? _notifyHeader;

    private ComboBox? _progressDisplayModeComboBox;
    private TextBlock? _progressDisplayModeLabel;
    private TextBlock? _progressDisplayModeGroupHeader;
    private ToggleSwitch? _enableCustomProgressColorToggle;
    private ColorPicker? _progressBarColorPicker;
    private ColorPicker? _progressRingColorPicker;

    private TextBlock? _startTimeGroupHeader;
    private ComboBox? _startYearRangeCombo;
    private ComboBox? _startTianganCombo;
    private ComboBox? _startDizhiCombo;
    private ComboBox? _startMonthCombo;
    private ToggleSwitch? _startLeapToggle;
    private ComboBox? _startDayCombo;
    private TimePicker? _startTimePicker;
    private TextBox? _startSolarYearTextBox;
    private ComboBox? _startSolarMonthComboBox;
    private ComboBox? _startSolarDayComboBox;
    private ComboBox? _startSolarHourComboBox;
    private ComboBox? _startSolarMinuteComboBox;
    private ComboBox? _startSolarSecondComboBox;
    private TextBlock? _startSolarDateLabel;
    private TextBlock? _startYearRangeLabel;
    private TextBlock? _startTianganLabel;
    private TextBlock? _startDizhiLabel;
    private TextBlock? _startMonthLabel;
    private TextBlock? _startLeapLabel;
    private TextBlock? _startDayLabel;
    private TextBlock? _startTimeLabel;
    private TextBlock? _startSolarHourSep1;
    private TextBlock? _startSolarHourSep2;

    private TextBlock? _text1StyleTextBlock;
    private TextBlock? _nameStyleTextBlock;
    private TextBlock? _text3StyleTextBlock;
    private TextBlock? _timeStyleTextBlock;
    private TextBlock? _text4StyleTextBlock;

    private List<TextBlock> _dynamicTextBlocks = new();

    private bool _isUpdatingStartTime;

    public LunarCountdownSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "农历多倒计时设置", FontSize = 14, FontWeight = FontWeight.Bold };
        mainPanel.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "配置农历倒计时显示选项和倒计时列表", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        mainPanel.Children.Add(_descTextBlock);

        _textGroupHeader = new TextBlock { Text = "文案设置" };
        var textGroup = new Expander { Header = _textGroupHeader, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _orderHintTextBlock = new TextBlock { Text = "以下内容在主界面上的顺序为：文案1->倒计时名称->文案3->剩余时间->文案4", FontSize = 11, FontWeight = FontWeight.Bold };
        textPanel.Children.Add(_orderHintTextBlock);
        textPanel.Children.Add(CreateTextRowFullWidth("文案1", "", out _text1TextBox));
        textPanel.Children.Add(CreateText2ButtonRow());
        textPanel.Children.Add(CreateTextRowFullWidth("文案3", "还有", out _text3TextBox));
        textPanel.Children.Add(CreateTextRowFullWidth("文案4", "", out _text4TextBox));

        textGroup.Content = textPanel;
        mainPanel.Children.Add(textGroup);

        _formatGroupHeader = new TextBlock { Text = "时间格式" };
        var formatGroup = new Expander { Header = _formatGroupHeader, IsExpanded = true };
        var formatPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _timeFormatTextBox = new TextBox { Watermark = "%d天%h小时%m分钟%s秒" };
        formatPanel.Children.Add(_timeFormatTextBox);

        _timeFormatHint = new TextBlock
        {
            Text = "格式化变量: %D总天数 %H总小时 %M总分钟 %S总秒 %X总毫秒\n%d天 %h小时 %m分钟 %s秒 %x毫秒\n%L剩余百分比 %P已过百分比 %p已过百分比(两位)\n%yy总年 %YY总年(两位) %mo总月 %MO总月(两位)",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        formatPanel.Children.Add(_timeFormatHint);

        formatGroup.Content = formatPanel;
        mainPanel.Children.Add(formatGroup);

        _timeBaseGroupHeader = new TextBlock { Text = "时间基准" };
        var timeBaseGroup = new Expander { Header = _timeBaseGroupHeader, IsExpanded = true };
        var timeBasePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var timeBaseRow = new Grid();
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _timeBaseLabel = new TextBlock { Text = "时间基准:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(_timeBaseLabel);
        Grid.SetColumn(_timeBaseLabel, 0);
        timeBaseRow.Children.Add(_timeBaseLabel);

        _timeBaseComboBox = new ComboBox();
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("ClassIsland时间");
        Grid.SetColumn(_timeBaseComboBox, 1);
        timeBaseRow.Children.Add(_timeBaseComboBox);

        timeBasePanel.Children.Add(timeBaseRow);
        timeBaseGroup.Content = timeBasePanel;
        mainPanel.Children.Add(timeBaseGroup);

        BuildStartTimeGroup(mainPanel);

        _progressDisplayModeGroupHeader = new TextBlock { Text = "进度显示" };
        var progressDisplayModeGroup = new Expander { Header = _progressDisplayModeGroupHeader, IsExpanded = true };
        var progressDisplayModePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var progressDisplayModeRow = new Grid();
        progressDisplayModeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        progressDisplayModeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _progressDisplayModeLabel = new TextBlock { Text = "显示进度条:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(_progressDisplayModeLabel);
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
        mainPanel.Children.Add(progressDisplayModeGroup);

        _fontGroupHeader = new TextBlock { Text = "字体样式" };
        var fontGroup = new Expander { Header = _fontGroupHeader, IsExpanded = false };
        var fontPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _text1StyleTextBlock = new TextBlock { Text = "文案1样式", FontSize = 12, FontWeight = FontWeight.Bold };
        var text1TitleRow = CreateTitleRow(_text1StyleTextBlock, out _text1EnableCustomFontSizeToggle, out _text1EnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnText1EnableCustomFontSizeChanged, OnText1EnableCustomFontColorChanged, null, null);
        fontPanel.Children.Add(text1TitleRow);
        fontPanel.Children.Add(CreateFontSizeRow("文本大小", out _text1FontSizeNumericUpDown, OnText1FontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("文本颜色", out _text1FontColorPicker, OnText1ColorChanged));

        _nameStyleTextBlock = new TextBlock { Text = "倒计时名称样式", FontSize = 12, FontWeight = FontWeight.Bold };
        var nameTitleRow = CreateTitleRow(_nameStyleTextBlock, out _nameEnableCustomFontSizeToggle, out _nameEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnNameEnableCustomFontSizeChanged, OnNameEnableCustomFontColorChanged, null, null);
        fontPanel.Children.Add(nameTitleRow);
        fontPanel.Children.Add(CreateFontSizeRow("文本大小", out _nameFontSizeNumericUpDown, OnNameFontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("文本颜色", out _nameFontColorPicker, OnNameColorChanged));

        _text3StyleTextBlock = new TextBlock { Text = "文案3样式", FontSize = 12, FontWeight = FontWeight.Bold };
        var text3TitleRow = CreateTitleRow(_text3StyleTextBlock, out _text3EnableCustomFontSizeToggle, out _text3EnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnText3EnableCustomFontSizeChanged, OnText3EnableCustomFontColorChanged, null, null);
        fontPanel.Children.Add(text3TitleRow);
        fontPanel.Children.Add(CreateFontSizeRow("文本大小", out _text3FontSizeNumericUpDown, OnText3FontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("文本颜色", out _text3FontColorPicker, OnText3ColorChanged));

        _timeStyleTextBlock = new TextBlock { Text = "时间样式", FontSize = 12, FontWeight = FontWeight.Bold };
        var timeTitleRow = CreateTitleRow(_timeStyleTextBlock, out _timeEnableCustomFontSizeToggle, out _timeEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnTimeEnableCustomFontSizeChanged, OnTimeEnableCustomFontColorChanged, null, null);
        fontPanel.Children.Add(timeTitleRow);
        fontPanel.Children.Add(CreateFontSizeRow("文本大小", out _timeFontSizeNumericUpDown, OnTimeFontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("文本颜色", out _timeFontColorPicker, OnTimeColorChanged));

        _text4StyleTextBlock = new TextBlock { Text = "文案4样式", FontSize = 12, FontWeight = FontWeight.Bold };
        var text4TitleRow = CreateTitleRow(_text4StyleTextBlock, out _text4EnableCustomFontSizeToggle, out _text4EnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnText4EnableCustomFontSizeChanged, OnText4EnableCustomFontColorChanged, null, null);
        fontPanel.Children.Add(text4TitleRow);
        fontPanel.Children.Add(CreateFontSizeRow("文本大小", out _text4FontSizeNumericUpDown, OnText4FontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("文本颜色", out _text4FontColorPicker, OnText4ColorChanged));

        fontGroup.Content = fontPanel;
        mainPanel.Children.Add(fontGroup);

        _listGroupHeader = new TextBlock { Text = "农历倒计时列表" };
        var listGroup = new Expander { Header = _listGroupHeader, IsExpanded = true };
        var listPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        _lunarHeader = new TextBlock
        {
            Text = "农历日期",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_lunarHeader, 0);
        headerGrid.Children.Add(_lunarHeader);

        _solarHeader = new TextBlock
        {
            Text = "对应公历日期",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_solarHeader, 1);
        headerGrid.Children.Add(_solarHeader);

        _notifyHeader = new TextBlock
        {
            Text = "启用通知？",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(_notifyHeader, 2);
        headerGrid.Children.Add(_notifyHeader);
        listPanel.Children.Add(headerGrid);

        _countdownListBox = new ListBox { Height = 150, SelectionMode = SelectionMode.Single };
        _countdownListBox.SelectionChanged += (s, e) =>
        {
            if (_countdownListBox != null && _countdownListBox.SelectedIndex >= 0)
            {
                HideHint();
            }
        };
        listPanel.Children.Add(_countdownListBox);

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

        listPanel.Children.Add(buttonPanel);

        _selectionHintTextBlock = new TextBlock
        {
            Text = "请选择一个农历倒计时",
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 12
        };
        listPanel.Children.Add(_selectionHintTextBlock);
        listGroup.Content = listPanel;
        mainPanel.Children.Add(listGroup);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = mainPanel
        };
        Content = scrollViewer;
    }

    private void BuildStartTimeGroup(StackPanel mainPanel)
    {
        _startTimeGroupHeader = new TextBlock { Text = "开始时间（农历）" };
        var startTimeGroup = new Expander { Header = _startTimeGroupHeader, IsExpanded = false };
        var startTimePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var lunarGroup = new Expander { Header = new TextBlock { Text = "农历日期", Foreground = ThemeHelper.GetTextBrush() }, IsExpanded = true };
        var lunarPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var yearRangePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _startYearRangeLabel = new TextBlock { Text = "年份范围:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(_startYearRangeLabel);
        yearRangePanel.Children.Add(_startYearRangeLabel);
        _startYearRangeCombo = new ComboBox { Width = 180 };
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            _startYearRangeCombo.Items.Add(range);
        }
        yearRangePanel.Children.Add(_startYearRangeCombo);
        lunarPanel.Children.Add(yearRangePanel);

        var yearRow = new Grid();
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _startTianganLabel = new TextBlock { Text = "天干地支年:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(_startTianganLabel);
        Grid.SetColumn(_startTianganLabel, 0);
        yearRow.Children.Add(_startTianganLabel);

        _startTianganCombo = new ComboBox { Width = 60 };
        var tiangan = new[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        foreach (var t in tiangan) _startTianganCombo.Items.Add(t);
        Grid.SetColumn(_startTianganCombo, 1);
        yearRow.Children.Add(_startTianganCombo);

        _startDizhiLabel = new TextBlock { Text = "地支:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        _dynamicTextBlocks.Add(_startDizhiLabel);
        Grid.SetColumn(_startDizhiLabel, 2);
        yearRow.Children.Add(_startDizhiLabel);

        _startDizhiCombo = new ComboBox { Width = 60 };
        var dizhi = new[] { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
        foreach (var d in dizhi) _startDizhiCombo.Items.Add(d);
        Grid.SetColumn(_startDizhiCombo, 3);
        yearRow.Children.Add(_startDizhiCombo);

        lunarPanel.Children.Add(yearRow);

        var monthRow = new Grid();
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        _startMonthLabel = new TextBlock { Text = "月:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(_startMonthLabel);
        Grid.SetColumn(_startMonthLabel, 0);
        monthRow.Children.Add(_startMonthLabel);

        _startMonthCombo = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) _startMonthCombo.Items.Add(i.ToString());
        Grid.SetColumn(_startMonthCombo, 1);
        monthRow.Children.Add(_startMonthCombo);

        _startLeapLabel = new TextBlock { Text = "闰月:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        _dynamicTextBlocks.Add(_startLeapLabel);
        Grid.SetColumn(_startLeapLabel, 2);
        monthRow.Children.Add(_startLeapLabel);

        _startLeapToggle = new ToggleSwitch { Margin = new Avalonia.Thickness(4, 0, 0, 0) };
        Grid.SetColumn(_startLeapToggle, 3);
        monthRow.Children.Add(_startLeapToggle);

        _startDayLabel = new TextBlock { Text = "日:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        _dynamicTextBlocks.Add(_startDayLabel);
        Grid.SetColumn(_startDayLabel, 4);
        monthRow.Children.Add(_startDayLabel);

        _startDayCombo = new ComboBox { Width = 80 };
        for (int i = 1; i <= 30; i++) _startDayCombo.Items.Add(i.ToString());
        Grid.SetColumn(_startDayCombo, 5);
        monthRow.Children.Add(_startDayCombo);

        lunarPanel.Children.Add(monthRow);

        var timeRow = new Grid();
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });

        _startTimeLabel = new TextBlock { Text = "时间:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(_startTimeLabel);
        Grid.SetColumn(_startTimeLabel, 0);
        timeRow.Children.Add(_startTimeLabel);

        _startTimePicker = new TimePicker
        {
            Width = 250,
            ClockIdentifier = "24HourClock",
            UseSeconds = true
        };
        Grid.SetColumn(_startTimePicker, 1);
        timeRow.Children.Add(_startTimePicker);

        lunarPanel.Children.Add(timeRow);

        lunarGroup.Content = lunarPanel;
        startTimePanel.Children.Add(lunarGroup);

        var solarGroup = new Expander { Header = new TextBlock { Text = "公历对照（可互转）", Foreground = ThemeHelper.GetTextBrush() }, IsExpanded = true };
        var solarPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _startSolarDateLabel = new TextBlock { Text = "公历日期:", Foreground = ThemeHelper.GetTextBrush() };
        _dynamicTextBlocks.Add(_startSolarDateLabel);
        solarPanel.Children.Add(_startSolarDateLabel);

        var solarDatePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        _startSolarYearTextBox = new TextBox { Width = 80, Watermark = "年" };
        _startSolarMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) _startSolarMonthComboBox.Items.Add($"{i}月");
        _startSolarDayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) _startSolarDayComboBox.Items.Add($"{i}日");

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startSolarYearTextBox, (s, e) => UpdateStartDayComboBox());
        _startSolarMonthComboBox.SelectionChanged += (s, e) => UpdateStartDayComboBox();

        solarDatePanel.Children.Add(_startSolarYearTextBox);
        solarDatePanel.Children.Add(_startSolarMonthComboBox);
        solarDatePanel.Children.Add(_startSolarDayComboBox);
        solarPanel.Children.Add(solarDatePanel);

        var solarTimePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        _startSolarHourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) _startSolarHourComboBox.Items.Add(i.ToString("D2"));
        _startSolarMinuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) _startSolarMinuteComboBox.Items.Add(i.ToString("D2"));
        _startSolarSecondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) _startSolarSecondComboBox.Items.Add(i.ToString("D2"));

        _startSolarHourSep1 = new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        _startSolarHourSep2 = new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };

        solarTimePanel.Children.Add(_startSolarHourComboBox);
        solarTimePanel.Children.Add(_startSolarHourSep1);
        solarTimePanel.Children.Add(_startSolarMinuteComboBox);
        solarTimePanel.Children.Add(_startSolarHourSep2);
        solarTimePanel.Children.Add(_startSolarSecondComboBox);
        solarPanel.Children.Add(solarTimePanel);

        var syncSolarToLunarButton = new Button { Content = "同步公历→农历", Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        syncSolarToLunarButton.Click += OnStartSyncSolarToLunarClick;
        solarPanel.Children.Add(syncSolarToLunarButton);

        var syncLunarToSolarButton = new Button { Content = "同步农历→公历", Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        syncLunarToSolarButton.Click += OnStartSyncLunarToSolarClick;
        solarPanel.Children.Add(syncLunarToSolarButton);

        solarGroup.Content = solarPanel;
        startTimePanel.Children.Add(solarGroup);

        startTimeGroup.Content = startTimePanel;
        mainPanel.Children.Add(startTimeGroup);
    }

    private void UpdateStartDayComboBox()
    {
        if (_startSolarYearTextBox == null || _startSolarMonthComboBox == null || _startSolarDayComboBox == null)
            return;
        if (!int.TryParse(_startSolarYearTextBox.Text?.Trim(), out var year))
            return;
        if (_startSolarMonthComboBox.SelectedItem == null)
            return;
        if (!int.TryParse(_startSolarMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return;

        var selectedDayText = _startSolarDayComboBox.SelectedItem?.ToString();
        int? selectedDay = null;
        if (selectedDayText != null && int.TryParse(selectedDayText.Replace("日", ""), out var d))
            selectedDay = d;

        _startSolarDayComboBox.Items.Clear();

        if (year == 1582 && month == 10)
        {
            for (int i = 1; i <= 4; i++)
            {
                _startSolarDayComboBox.Items.Add($"{i}日");
            }
            for (int i = 15; i <= 31; i++)
            {
                _startSolarDayComboBox.Items.Add($"{i}日");
            }
        }
        else
        {
            var daysInMonth = GetDaysInMonth(year, month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                _startSolarDayComboBox.Items.Add($"{i}日");
            }
        }

        if (selectedDay.HasValue)
        {
            var safeDay = Math.Min(selectedDay.Value, _startSolarDayComboBox.Items.Count);
            if (safeDay > 0)
            {
                _startSolarDayComboBox.SelectedItem = $"{safeDay}日";
            }
            else
            {
                _startSolarDayComboBox.SelectedIndex = -1;
            }
        }
        else
        {
            _startSolarDayComboBox.SelectedIndex = -1;
        }
    }

    private void OnStartSyncSolarToLunarClick(object? sender, EventArgs e)
    {
        if (_startSolarYearTextBox == null || _startSolarMonthComboBox == null || _startSolarDayComboBox == null ||
            _startSolarHourComboBox == null || _startSolarMinuteComboBox == null || _startSolarSecondComboBox == null)
            return;

        if (!int.TryParse(_startSolarYearTextBox.Text?.Trim(), out var year) ||
            _startSolarMonthComboBox.SelectedIndex < 0 ||
            _startSolarDayComboBox.SelectedItem == null ||
            !int.TryParse(_startSolarDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) ||
            _startSolarHourComboBox.SelectedIndex < 0 ||
            _startSolarMinuteComboBox.SelectedIndex < 0 ||
            _startSolarSecondComboBox.SelectedIndex < 0)
            return;

        var month = _startSolarMonthComboBox.SelectedIndex + 1;
        try
        {
            var solarDate = new DateTime(year, month, day,
                _startSolarHourComboBox.SelectedIndex, _startSolarMinuteComboBox.SelectedIndex, _startSolarSecondComboBox.SelectedIndex);

            if (!LunarCalendarHelper.IsDateSupported(solarDate))
                return;

            var lunarYear = LunarCalendarHelper.GetLunarYear(solarDate);
            var lunarMonth = LunarCalendarHelper.GetLunarMonth(solarDate);
            var isLeap = LunarCalendarHelper.IsLeapMonth(solarDate);
            var lunarDay = LunarCalendarHelper.GetLunarDay(solarDate);

            if (lunarYear == 0 || lunarMonth == 0 || lunarDay == 0)
                return;

            ApplyLunarToStartControls(lunarYear, lunarMonth, isLeap, lunarDay, solarDate.Hour, solarDate.Minute, solarDate.Second);
            CommitStartLunarToSettings();
        }
        catch { }
    }

    private void OnStartSyncLunarToSolarClick(object? sender, EventArgs e)
    {
        if (_startYearRangeCombo == null || _startTianganCombo == null || _startDizhiCombo == null ||
            _startMonthCombo == null || _startDayCombo == null || _startTimePicker == null)
            return;

        var lunarYearResult = TryResolveLunarYear();
        if (!lunarYearResult.HasValue) return;
        var lunarYearVal = lunarYearResult.Value;

        var lunarMonthVal = _startMonthCombo.SelectedIndex + 1;
        var isLeapVal = _startLeapToggle?.IsChecked == true;
        var lunarDayVal = _startDayCombo.SelectedIndex + 1;
        var hourVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Hours : 0;
        var minuteVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Minutes : 0;
        var secondVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Seconds : 0;

        var solarResult = LunarCalendarHelper.LunarToSolar(lunarYearVal, lunarMonthVal, isLeapVal, lunarDayVal, hourVal, minuteVal, secondVal);
        if (solarResult.HasValue)
        {
            _startSolarYearTextBox.Text = solarResult.Value.Year.ToString();
            _startSolarMonthComboBox.SelectedIndex = solarResult.Value.Month - 1;
            _startSolarDayComboBox.SelectedItem = $"{solarResult.Value.Day}日";
            _startSolarHourComboBox.SelectedIndex = solarResult.Value.Hour;
            _startSolarMinuteComboBox.SelectedIndex = solarResult.Value.Minute;
            _startSolarSecondComboBox.SelectedIndex = solarResult.Value.Second;
            CommitStartLunarToSettings();
        }
    }

    private void ApplyLunarToStartControls(int lunarYear, int lunarMonth, bool isLeap, int lunarDay, int hour, int minute, int second)
    {
        if (_startTianganCombo == null || _startDizhiCombo == null || _startMonthCombo == null ||
            _startLeapToggle == null || _startDayCombo == null || _startTimePicker == null ||
            _startYearRangeCombo == null)
            return;

        _isUpdatingStartTime = true;
        try
        {
            var tgIndex = (lunarYear - 4) % 10;
            if (tgIndex < 0) tgIndex += 10;
            var dzIndex = (lunarYear - 4) % 12;
            if (dzIndex < 0) dzIndex += 12;

            _startTianganCombo.SelectedIndex = tgIndex;
            _startDizhiCombo.SelectedIndex = dzIndex;
            _startMonthCombo.SelectedIndex = lunarMonth - 1;
            _startLeapToggle.IsChecked = isLeap;
            _startDayCombo.SelectedIndex = lunarDay - 1;
            _startTimePicker.SelectedTime = new TimeSpan(hour, minute, second);

            foreach (var range in LunarCalendarHelper.GetAllYearRanges())
            {
                if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
                {
                    if (lunarYear >= startYear && lunarYear <= endYear)
                    {
                        _startYearRangeCombo.SelectedItem = range;
                        break;
                    }
                }
            }
        }
        finally
        {
            _isUpdatingStartTime = false;
        }
    }

    private void CommitStartLunarToSettings()
    {
        if (_startYearRangeCombo == null || _startTianganCombo == null || _startDizhiCombo == null ||
            _startMonthCombo == null || _startDayCombo == null || _startTimePicker == null)
            return;

        var lunarYearResult = TryResolveLunarYear();
        if (!lunarYearResult.HasValue) return;
        var lunarYearVal = lunarYearResult.Value;

        var lunarMonthVal = _startMonthCombo.SelectedIndex + 1;
        var isLeapVal = _startLeapToggle?.IsChecked == true;
        var lunarDayVal = _startDayCombo.SelectedIndex + 1;
        var hourVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Hours : 0;
        var minuteVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Minutes : 0;
        var secondVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Seconds : 0;

        var solarResult = LunarCalendarHelper.LunarToSolar(lunarYearVal, lunarMonthVal, isLeapVal, lunarDayVal, hourVal, minuteVal, secondVal);
        if (solarResult.HasValue)
        {
            Settings.StartTime = UnixTimeHelper.ToUnixTimestamp(solarResult.Value);
            if (_startSolarYearTextBox != null) _startSolarYearTextBox.Text = solarResult.Value.Year.ToString();
            if (_startSolarMonthComboBox != null) _startSolarMonthComboBox.SelectedIndex = solarResult.Value.Month - 1;
            if (_startSolarDayComboBox != null) _startSolarDayComboBox.SelectedItem = $"{solarResult.Value.Day}日";
            if (_startSolarHourComboBox != null) _startSolarHourComboBox.SelectedIndex = solarResult.Value.Hour;
            if (_startSolarMinuteComboBox != null) _startSolarMinuteComboBox.SelectedIndex = solarResult.Value.Minute;
            if (_startSolarSecondComboBox != null) _startSolarSecondComboBox.SelectedIndex = solarResult.Value.Second;
        }
    }

    private int? TryResolveLunarYear()
    {
        if (_startYearRangeCombo == null || _startTianganCombo == null || _startDizhiCombo == null)
            return null;

        if (_startYearRangeCombo.SelectedItem == null ||
            _startTianganCombo.SelectedIndex < 0 ||
            _startDizhiCombo.SelectedIndex < 0)
            return null;

        var yearRange = _startYearRangeCombo.SelectedItem.ToString();
        if (string.IsNullOrEmpty(yearRange)) return null;

        var yearParts = yearRange.Split('-');
        if (yearParts.Length != 2) return null;
        if (!int.TryParse(yearParts[0], out var startYear)) return null;
        if (!int.TryParse(yearParts[1], out var endYear)) return null;

        var tg = _startTianganCombo.SelectedIndex;
        var dz = _startDizhiCombo.SelectedIndex;

        var baseYear = 4;
        var yearOffset = 0;
        while ((baseYear + yearOffset - 4) % 10 != tg ||
               (baseYear + yearOffset - 4) % 12 != dz)
        {
            yearOffset++;
            if (yearOffset > 60) return null;
        }

        var lunarYearVal = baseYear + yearOffset;
        while (lunarYearVal < startYear)
        {
            lunarYearVal += 60;
        }
        if (lunarYearVal > endYear)
        {
            lunarYearVal -= 60;
        }

        return lunarYearVal;
    }

    private Grid CreateTextRowFullWidth(string label, string watermark, out TextBox? textBox)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        textBox = new TextBox { Watermark = watermark };
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);

        return row;
    }

    private Grid CreateTitleRow(TextBlock title, out ToggleSwitch? toggle1, out ToggleSwitch? toggle2, out ToggleSwitch? toggle3, out ToggleSwitch? toggle4,
        string? content1, string? content2, string? content3, string? content4,
        EventHandler<RoutedEventArgs>? handler1, EventHandler<RoutedEventArgs>? handler2, EventHandler<RoutedEventArgs>? handler3, EventHandler<RoutedEventArgs>? handler4)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(title, 0);
        row.Children.Add(title);

        int col = 1;

        if (content1 != null)
        {
            toggle1 = new ToggleSwitch { Content = content1, VerticalAlignment = VerticalAlignment.Center };
            if (handler1 != null)
                toggle1.IsCheckedChanged += handler1;
            Grid.SetColumn(toggle1, col++);
            row.Children.Add(toggle1);
        }
        else
        {
            toggle1 = null;
        }

        if (content2 != null)
        {
            toggle2 = new ToggleSwitch { Content = content2, VerticalAlignment = VerticalAlignment.Center };
            if (handler2 != null)
                toggle2.IsCheckedChanged += handler2;
            Grid.SetColumn(toggle2, col++);
            row.Children.Add(toggle2);
        }
        else
        {
            toggle2 = null;
        }

        if (content3 != null)
        {
            toggle3 = new ToggleSwitch { Content = content3, VerticalAlignment = VerticalAlignment.Center };
            if (handler3 != null)
                toggle3.IsCheckedChanged += handler3;
            Grid.SetColumn(toggle3, col++);
            row.Children.Add(toggle3);
        }
        else
        {
            toggle3 = null;
        }

        if (content4 != null)
        {
            toggle4 = new ToggleSwitch { Content = content4, VerticalAlignment = VerticalAlignment.Center };
            if (handler4 != null)
                toggle4.IsCheckedChanged += handler4;
            Grid.SetColumn(toggle4, col);
            row.Children.Add(toggle4);
        }
        else
        {
            toggle4 = null;
        }

        return row;
    }

    private Grid CreateFontSizeRow(string labelText, out NumericUpDown? numericUpDown,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(label);
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        numericUpDown = new NumericUpDown
        {
            Width = 155,
            Minimum = 1,
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        Grid.SetColumn(numericUpDown, 1);
        row.Children.Add(numericUpDown);

        return row;
    }

    private Grid CreateColorRow(string label, out ColorPicker? colorPicker)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
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

    private Grid CreateColorRow(string labelText, out ColorPicker? colorPicker,
        EventHandler<ColorChangedEventArgs> colorChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(label);
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        colorPicker = new ColorPicker { Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        colorPicker.ColorChanged += colorChangedHandler;
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        return row;
    }

    private Grid CreateText2ButtonRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = "倒计时名称:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        var button = new Button
        {
            Content = "前往编辑农历倒计时名称",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        button.Click += OnText2ButtonClick;
        Grid.SetColumn(button, 1);
        row.Children.Add(button);

        return row;
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
        if (_timeFormatHint != null) _timeFormatHint.Foreground = ThemeHelper.GetGrayBrush();
        if (_timeBaseGroupHeader != null) _timeBaseGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_startTimeGroupHeader != null) _startTimeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_startSolarHourSep1 != null) _startSolarHourSep1.Foreground = ThemeHelper.GetTextBrush();
        if (_startSolarHourSep2 != null) _startSolarHourSep2.Foreground = ThemeHelper.GetTextBrush();
        if (_progressDisplayModeGroupHeader != null) _progressDisplayModeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_fontGroupHeader != null) _fontGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_listGroupHeader != null) _listGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_lunarHeader != null) _lunarHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_solarHeader != null) _solarHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_notifyHeader != null) _notifyHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_selectionHintTextBlock != null) _selectionHintTextBlock.Foreground = ThemeHelper.GetOrangeBrush();

        if (_text1StyleTextBlock != null) _text1StyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_nameStyleTextBlock != null) _nameStyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_text3StyleTextBlock != null) _text3StyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_timeStyleTextBlock != null) _timeStyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_text4StyleTextBlock != null) _text4StyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();

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

        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetTextBrush();
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

    private void OnEnableCustomProgressColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomProgressColor = _enableCustomProgressColorToggle?.IsChecked ?? false;
        UpdateProgressColorControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _nameFontSizeNumericUpDown.IsEnabled = Settings.NameEnableCustomFontSize;
        _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
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
        UpdateThemeColors();

        if (_text1TextBox != null) _text1TextBox.Text = Settings.Text1;
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

        LoadStartTimeControls();

        if (_progressDisplayModeComboBox != null) _progressDisplayModeComboBox.SelectedIndex = (int)Settings.ProgressDisplayMode;

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

        UpdateCountdownList();

        AttachEventHandlers();

        _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        _nameEnableCustomFontSizeToggle.IsChecked = Settings.NameEnableCustomFontSize;
        _nameEnableCustomFontColorToggle.IsChecked = Settings.NameEnableCustomFontColor;
        _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;
        _text4EnableCustomFontSizeToggle.IsChecked = Settings.Text4EnableCustomFontSize;
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
        _hintTimer?.Stop();
        _hintTimer?.Dispose();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void AttachEventHandlers()
    {
        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "");
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

        AttachColorPickerHandler(_progressBarColorPicker, v => Settings.ProgressBarColor = v);
        AttachColorPickerHandler(_progressRingColorPicker, v => Settings.ProgressRingColor = v);

        AttachStartTimeHandlers();
    }

    private void LoadStartTimeControls()
    {
        if (_startYearRangeCombo == null || _startTianganCombo == null || _startDizhiCombo == null ||
            _startMonthCombo == null || _startLeapToggle == null || _startDayCombo == null ||
            _startTimePicker == null || _startSolarYearTextBox == null || _startSolarMonthComboBox == null ||
            _startSolarDayComboBox == null || _startSolarHourComboBox == null || _startSolarMinuteComboBox == null ||
            _startSolarSecondComboBox == null)
            return;

        var startTime = UnixTimeHelper.FromUnixTimestamp(Settings.StartTime);

        _startSolarYearTextBox.Text = startTime.Year.ToString();
        _startSolarMonthComboBox.SelectedIndex = startTime.Month - 1;
        _startSolarDayComboBox.SelectedItem = $"{startTime.Day}日";
        _startSolarHourComboBox.SelectedIndex = startTime.Hour;
        _startSolarMinuteComboBox.SelectedIndex = startTime.Minute;
        _startSolarSecondComboBox.SelectedIndex = startTime.Second;

        var lunarYear = LunarCalendarHelper.GetLunarYear(startTime);
        var lunarMonth = LunarCalendarHelper.GetLunarMonth(startTime);
        var isLeap = LunarCalendarHelper.IsLeapMonth(startTime);
        var lunarDay = LunarCalendarHelper.GetLunarDay(startTime);

        if (lunarYear == 0 || lunarMonth == 0 || lunarDay == 0)
        {
            _startYearRangeCombo.SelectedItem = "1984-2043";
            _startTianganCombo.SelectedIndex = 0;
            _startDizhiCombo.SelectedIndex = 0;
            _startMonthCombo.SelectedIndex = 0;
            _startLeapToggle.IsChecked = false;
            _startDayCombo.SelectedIndex = 0;
            _startTimePicker.SelectedTime = new TimeSpan(startTime.Hour, startTime.Minute, startTime.Second);
            return;
        }

        ApplyLunarToStartControls(lunarYear, lunarMonth, isLeap, lunarDay, startTime.Hour, startTime.Minute, startTime.Second);
    }

    private void AttachStartTimeHandlers()
    {
        void UpdateStartTimeFromLunar()
        {
            if (_isUpdatingStartTime) return;
            CommitStartLunarToSettings();
        }

        if (_startYearRangeCombo != null)
        {
            _startYearRangeCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        }
        if (_startTianganCombo != null)
        {
            _startTianganCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        }
        if (_startDizhiCombo != null)
        {
            _startDizhiCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        }
        if (_startMonthCombo != null)
        {
            _startMonthCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        }
        if (_startLeapToggle != null)
        {
            _startLeapToggle.IsCheckedChanged += (s, e) => UpdateStartTimeFromLunar();
        }
        if (_startDayCombo != null)
        {
            _startDayCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        }
        if (_startTimePicker != null)
        {
            _startTimePicker.SelectedTimeChanged += (s, e) => UpdateStartTimeFromLunar();
        }
    }

    private void AttachTextHandler(TextBox? textBox, Action<string?> setter)
    {
        if (textBox == null) return;
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => setter(textBox.Text));
    }

    private void AttachFontSizeHandler(NumericUpDown? numericUpDown, Action<double> setter)
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

    private void OnText1FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text1FontSizeNumericUpDown?.Value.HasValue == true)
        {
            Settings.Text1FontSize = (double)_text1FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText1ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text1FontColor = _text1FontColorPicker?.Color.ToString() ?? "#FFFFFF";
    }

    private void OnNameFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_nameFontSizeNumericUpDown?.Value.HasValue == true)
        {
            Settings.NameFontSize = (double)_nameFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnNameColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.NameFontColor = _nameFontColorPicker?.Color.ToString() ?? "#FFFFFF";
    }

    private void OnText3FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text3FontSizeNumericUpDown?.Value.HasValue == true)
        {
            Settings.Text3FontSize = (double)_text3FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText3ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text3FontColor = _text3FontColorPicker?.Color.ToString() ?? "#FFFFFF";
    }

    private void OnTimeFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_timeFontSizeNumericUpDown?.Value.HasValue == true)
        {
            Settings.TimeFontSize = (double)_timeFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.TimeFontColor = _timeFontColorPicker?.Color.ToString() ?? "#FFFFFF";
    }

    private void OnText4FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text4FontSizeNumericUpDown?.Value.HasValue == true)
        {
            Settings.Text4FontSize = (double)_text4FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText4ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text4FontColor = _text4FontColorPicker?.Color.ToString() ?? "#FFFFFF";
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

    private void UpdateCountdownList()
    {
        if (_countdownListBox == null) return;
        _countdownListBox.Items.Clear();
        if (Settings.CountdownItems != null)
        {
            foreach (var item in Settings.CountdownItems)
            {
                var targetSolar = item.GetTargetTimestamp() > 0 ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp()) : Plugin.GetCurrentTime();
                var lunarDesc = GetLunarDateDescription(item);

                var container = new Grid();
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                container.Tag = item;

                var lunarTextBlock = new TextBlock
                {
                    Text = $"{item.Name} - {lunarDesc}",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetTextBrush(),
                    Padding = new Avalonia.Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(lunarTextBlock, 0);
                container.Children.Add(lunarTextBlock);

                var solarTextBlock = new TextBlock
                {
                    Text = targetSolar.ToString("yyyy-MM-dd HH:mm:ss"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    Padding = new Avalonia.Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(solarTextBlock, 1);
                container.Children.Add(solarTextBlock);

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
                Grid.SetColumn(notifyPanel, 2);
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

    private string GetLunarDateDescription(LunarCountdownItem item)
    {
        var yearName = LunarCalendarHelper.GetLunarYearName(item.LunarYear);
        var monthStr = item.IsLeapMonth ? $"闰{item.LunarMonth}月" : $"{item.LunarMonth}月";
        return $"{yearName}年 {monthStr} {GetLunarDayString(item.LunarDay)}";
    }

    private string GetLunarDayString(int day)
    {
        if (day <= 0 || day > 30) return "";
        var prefix = new[] { "初", "十", "廿", "三" };
        var nums = new[] { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
        if (day <= 10) return $"{prefix[0]}{nums[day - 1]}";
        else if (day < 20) return $"{prefix[1]}{nums[day - 11]}";
        else if (day == 20) return "二十";
        else if (day < 30) return $"{prefix[2]}{nums[day - 21]}";
        else if (day == 30) return "三十";
        return "";
    }

    private void OnAddClick(object? sender, EventArgs e)
    {
        HideHint();
        if (Settings.CountdownItems == null)
        {
            Settings.CountdownItems = new List<LunarCountdownItem>();
        }
        Settings.CountdownItems.Add(LunarCountdownItem.CreateDefault());
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

    private void ShowEditDialog(LunarCountdownItem item, int order = 0)
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", order > 0 ? $"正在编辑第{order}个农历倒计时" : "编辑农历倒计时");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "SecondaryButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "公历可用范围为1901-02-19 ~ 2101-01-28");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", false);
        contentPanel.Children.Add(infoBar);

        var nameLabel = new TextBlock { Text = "名称:", Foreground = ThemeHelper.GetTextBrush() };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var lunarGroup = new Expander { Header = new TextBlock { Text = "农历日期", Foreground = ThemeHelper.GetTextBrush() }, IsExpanded = true };
        var lunarPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var yearRangePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        yearRangePanel.Children.Add(new TextBlock { Text = "年份范围:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        var yearRangeCombo = new ComboBox { Width = 180 };
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            yearRangeCombo.Items.Add(range);
        }

        bool foundRange = false;
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
            {
                if (item.LunarYear >= startYear && item.LunarYear <= endYear)
                {
                    yearRangeCombo.SelectedItem = range;
                    foundRange = true;
                    break;
                }
            }
        }
        if (!foundRange)
            yearRangeCombo.SelectedItem = "1984-2043";

        yearRangePanel.Children.Add(yearRangeCombo);
        lunarPanel.Children.Add(yearRangePanel);

        var yearRow = new Grid();
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var yearLabel = new TextBlock { Text = "天干地支年:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(yearLabel, 0);
        yearRow.Children.Add(yearLabel);

        var tianganCombo = new ComboBox { Width = 60 };
        var tiangan = new[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        foreach (var t in tiangan) tianganCombo.Items.Add(t);
        Grid.SetColumn(tianganCombo, 1);
        yearRow.Children.Add(tianganCombo);

        var dizhiLabel = new TextBlock { Text = "地支:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        Grid.SetColumn(dizhiLabel, 2);
        yearRow.Children.Add(dizhiLabel);

        var dizhiCombo = new ComboBox { Width = 60 };
        var dizhi = new[] { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
        foreach (var d in dizhi) dizhiCombo.Items.Add(d);
        Grid.SetColumn(dizhiCombo, 3);
        yearRow.Children.Add(dizhiCombo);

        var tgIndex = (item.LunarYear - 4) % 10;
        if (tgIndex < 0) tgIndex += 10;
        var dzIndex = (item.LunarYear - 4) % 12;
        if (dzIndex < 0) dzIndex += 12;
        tianganCombo.SelectedIndex = tgIndex;
        dizhiCombo.SelectedIndex = dzIndex;

        lunarPanel.Children.Add(yearRow);

        var monthRow = new Grid();
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        var monthLabel = new TextBlock { Text = "月:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(monthLabel, 0);
        monthRow.Children.Add(monthLabel);

        var monthCombo = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthCombo.Items.Add(i.ToString());
        monthCombo.SelectedIndex = item.LunarMonth - 1;
        Grid.SetColumn(monthCombo, 1);
        monthRow.Children.Add(monthCombo);

        var leapLabel = new TextBlock { Text = "闰月:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        Grid.SetColumn(leapLabel, 2);
        monthRow.Children.Add(leapLabel);

        var leapToggle = new ToggleSwitch { IsChecked = item.IsLeapMonth, Margin = new Avalonia.Thickness(4, 0, 0, 0) };
        Grid.SetColumn(leapToggle, 3);
        monthRow.Children.Add(leapToggle);

        var dayLabel = new TextBlock { Text = "日:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        Grid.SetColumn(dayLabel, 4);
        monthRow.Children.Add(dayLabel);

        var dayCombo = new ComboBox { Width = 80 };
        for (int i = 1; i <= 30; i++) dayCombo.Items.Add(i.ToString());
        dayCombo.SelectedIndex = item.LunarDay - 1;
        Grid.SetColumn(dayCombo, 5);
        monthRow.Children.Add(dayCombo);

        lunarPanel.Children.Add(monthRow);

        var timeRow = new Grid();
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });

        var timeLabel = new TextBlock { Text = "时间:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(timeLabel, 0);
        timeRow.Children.Add(timeLabel);

        var lunarTimePicker = new TimePicker
        {
            Width = 250,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            SelectedTime = new TimeSpan(item.Hour, item.Minute, item.Second)
        };
        Grid.SetColumn(lunarTimePicker, 1);
        timeRow.Children.Add(lunarTimePicker);

        lunarPanel.Children.Add(timeRow);

        var solarGroup = new Expander { Header = new TextBlock { Text = "公历对照（可互转）", Foreground = ThemeHelper.GetTextBrush() }, IsExpanded = true };
        var solarPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var solarDateLabel = new TextBlock { Text = "公历日期:", Foreground = ThemeHelper.GetTextBrush() };
        solarPanel.Children.Add(solarDateLabel);

        var currentSolarDate = item.GetTargetTimestamp() > 0 ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp()) : Plugin.GetCurrentTime();

        var solarDatePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var solarYearTextBox = new TextBox { Width = 80, Watermark = "年", Text = currentSolarDate.Year.ToString() };
        var solarMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) solarMonthComboBox.Items.Add($"{i}月");
        solarMonthComboBox.SelectedIndex = currentSolarDate.Month - 1;
        var solarDayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) solarDayComboBox.Items.Add($"{i}日");
        solarDayComboBox.SelectedItem = $"{currentSolarDate.Day}日";

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(solarYearTextBox, (s, e) => UpdateDayComboBox(solarYearTextBox, solarMonthComboBox, solarDayComboBox));
        solarMonthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(solarYearTextBox, solarMonthComboBox, solarDayComboBox);

        solarDatePanel.Children.Add(solarYearTextBox);
        solarDatePanel.Children.Add(solarMonthComboBox);
        solarDatePanel.Children.Add(solarDayComboBox);
        solarPanel.Children.Add(solarDatePanel);

        var solarTimePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var solarHourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) solarHourComboBox.Items.Add(i.ToString("D2"));
        solarHourComboBox.SelectedIndex = currentSolarDate.Hour;
        var solarMinuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) solarMinuteComboBox.Items.Add(i.ToString("D2"));
        solarMinuteComboBox.SelectedIndex = currentSolarDate.Minute;
        var solarSecondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) solarSecondComboBox.Items.Add(i.ToString("D2"));
        solarSecondComboBox.SelectedIndex = currentSolarDate.Second;

        solarTimePanel.Children.Add(solarHourComboBox);
        solarTimePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        solarTimePanel.Children.Add(solarMinuteComboBox);
        solarTimePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        solarTimePanel.Children.Add(solarSecondComboBox);
        solarPanel.Children.Add(solarTimePanel);

        var syncButton = new Button { Content = "同步公历→农历", Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        syncButton.Click += (s, e) =>
        {
            if (int.TryParse(solarYearTextBox.Text?.Trim(), out var year) &&
                solarMonthComboBox.SelectedIndex >= 0 &&
                solarDayComboBox.SelectedItem != null &&
                int.TryParse(solarDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) &&
                solarHourComboBox.SelectedIndex >= 0 &&
                solarMinuteComboBox.SelectedIndex >= 0 &&
                solarSecondComboBox.SelectedIndex >= 0)
            {
                var month = solarMonthComboBox.SelectedIndex + 1;
                try
                {
                    var solarDate = new DateTime(year, month, day,
                        solarHourComboBox.SelectedIndex, solarMinuteComboBox.SelectedIndex, solarSecondComboBox.SelectedIndex);

                    if (!LunarCalendarHelper.IsDateSupported(solarDate))
                    {
                        return;
                    }

                    var lunarYear = LunarCalendarHelper.GetLunarYear(solarDate);
                    var lunarMonth = LunarCalendarHelper.GetLunarMonth(solarDate);
                    var isLeap = LunarCalendarHelper.IsLeapMonth(solarDate);
                    var lunarDay = LunarCalendarHelper.GetLunarDay(solarDate);

                    if (lunarYear == 0 || lunarMonth == 0 || lunarDay == 0)
                    {
                        return;
                    }

                    var tgIndexNew = (lunarYear - 4) % 10;
                    if (tgIndexNew < 0) tgIndexNew += 10;
                    var dzIndexNew = (lunarYear - 4) % 12;
                    if (dzIndexNew < 0) dzIndexNew += 12;

                    tianganCombo.SelectedIndex = tgIndexNew;
                    dizhiCombo.SelectedIndex = dzIndexNew;
                    monthCombo.SelectedIndex = lunarMonth - 1;
                    leapToggle.IsChecked = isLeap;
                    dayCombo.SelectedIndex = lunarDay - 1;
                    lunarTimePicker.SelectedTime = new TimeSpan(solarDate.Hour, solarDate.Minute, solarDate.Second);

                    foreach (var range in LunarCalendarHelper.GetAllYearRanges())
                    {
                        if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
                        {
                            if (lunarYear >= startYear && lunarYear <= endYear)
                            {
                                yearRangeCombo.SelectedItem = range;
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
        };
        solarPanel.Children.Add(syncButton);

        var syncButton2 = new Button { Content = "同步农历→公历", Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        syncButton2.Click += (s, e) =>
        {
            if (yearRangeCombo.SelectedItem == null ||
                tianganCombo.SelectedIndex < 0 ||
                dizhiCombo.SelectedIndex < 0)
                return;

            var yearRange = yearRangeCombo.SelectedItem.ToString();
            if (string.IsNullOrEmpty(yearRange)) return;

            var yearParts = yearRange.Split('-');
            if (yearParts.Length != 2) return;
            if (!int.TryParse(yearParts[0], out var startYear)) return;
            if (!int.TryParse(yearParts[1], out var endYear)) return;

            var tg = tianganCombo.SelectedIndex;
            var dz = dizhiCombo.SelectedIndex;

            var baseYear = 4;
            var yearOffset = 0;
            while ((baseYear + yearOffset - 4) % 10 != tg ||
                   (baseYear + yearOffset - 4) % 12 != dz)
            {
                yearOffset++;
                if (yearOffset > 60) return;
            }

            var baseLunarYearVal = baseYear + yearOffset;
            var lunarYearVal = baseLunarYearVal;

            while (lunarYearVal < startYear)
            {
                lunarYearVal += 60;
            }
            if (lunarYearVal > endYear)
            {
                lunarYearVal -= 60;
            }

            var lunarMonthVal = monthCombo.SelectedIndex + 1;
            var isLeapVal = leapToggle.IsChecked == true;
            var lunarDayVal = dayCombo.SelectedIndex + 1;
            var hourVal = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Hours : 0;
            var minuteVal = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Minutes : 0;
            var secondVal = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Seconds : 0;

            var solarResult = LunarCalendarHelper.LunarToSolar(lunarYearVal, lunarMonthVal, isLeapVal, lunarDayVal, hourVal, minuteVal, secondVal);
            if (solarResult.HasValue)
            {
                solarYearTextBox.Text = solarResult.Value.Year.ToString();
                solarMonthComboBox.SelectedIndex = solarResult.Value.Month - 1;
                solarDayComboBox.SelectedItem = $"{solarResult.Value.Day}日";
                solarHourComboBox.SelectedIndex = solarResult.Value.Hour;
                solarMinuteComboBox.SelectedIndex = solarResult.Value.Minute;
                solarSecondComboBox.SelectedIndex = solarResult.Value.Second;
            }
        };
        solarPanel.Children.Add(syncButton2);


        var notifyToggle = new ToggleSwitch { Content = "启用通知", IsChecked = item.EnableNotification };
        contentPanel.Children.Add(notifyToggle);

        lunarGroup.Content = lunarPanel;
        contentPanel.Children.Add(lunarGroup);
        solarGroup.Content = solarPanel;
        contentPanel.Children.Add(solarGroup);

        FluentAvaloniaCompatibilityHelper.AddContentDialogButtonClickHandler(dialog, "PrimaryButtonClick", (s, e) =>
        {
            item.Name = nameTextBox.Text ?? "新农历倒计时";

            if (yearRangeCombo.SelectedItem != null)
            {
                var yearRange = yearRangeCombo.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(yearRange))
                {
                    var yearParts = yearRange.Split('-');
                    if (yearParts.Length == 2 &&
                        int.TryParse(yearParts[0], out var startYear) &&
                        int.TryParse(yearParts[1], out var endYear))
                    {
                        var tg = tianganCombo.SelectedIndex >= 0 ? tianganCombo.SelectedIndex : 0;
                        var dz = dizhiCombo.SelectedIndex >= 0 ? dizhiCombo.SelectedIndex : 0;

                        var baseYear = 4;
                        var yearOffset = 0;
                        while ((baseYear + yearOffset - 4) % 10 != tg ||
                               (baseYear + yearOffset - 4) % 12 != dz)
                        {
                            yearOffset++;
                            if (yearOffset > 60) break;
                        }

                        var baseLunarYearVal = baseYear + yearOffset;
                        var lunarYearVal = baseLunarYearVal;

                        while (lunarYearVal < startYear)
                        {
                            lunarYearVal += 60;
                        }
                        if (lunarYearVal > endYear)
                        {
                            lunarYearVal -= 60;
                        }

                        item.LunarYear = lunarYearVal;
                    }
                }
            }
            item.LunarMonth = monthCombo.SelectedIndex + 1;
            item.IsLeapMonth = leapToggle.IsChecked == true;
            item.LunarDay = dayCombo.SelectedIndex + 1;
            item.Hour = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Hours : 0;
            item.Minute = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Minutes : 0;
            item.Second = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Seconds : 0;
            item.EnableNotification = notifyToggle.IsChecked == true;
            item.IsCompleted = false;

            UpdateCountdownList();
        });

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

        _ = FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, TopLevel.GetTopLevel(this));
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



