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
    private CountdownListEditor<LunarCountdownItem>? _listEditor;

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
    private TextBlock? _timeGroupHeader;
    private TextBlock? _formatGroupHeader;
    private TextBlock? _timeBaseGroupHeader;
    private TextBlock? _timeBaseLabel;
    private TextBlock? _listGroupHeader;

    private ComboBox? _progressDisplayModeComboBox;
    private TextBlock? _progressDisplayModeLabel;
    private TextBlock? _progressDisplayModeGroupHeader;
    private ToggleSwitch? _simpleModeToggle;
    private TextBlock? _simpleModeDesc;
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

    private List<TextBlock> _dynamicTextBlocks = new();
    private List<Border> _tableCellBorders = new();

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

        textPanel.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateTextTable()
        });

        textGroup.Content = textPanel;
        mainPanel.Children.Add(textGroup);

        // ==================== 时间设置（合并折叠栏） ====================
        _timeGroupHeader = new TextBlock { Text = "时间设置" };
        var timeGroup = new Expander { Header = _timeGroupHeader, IsExpanded = true };
        var timePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        // --- 时间格式 ---
        _formatGroupHeader = new TextBlock { Text = "时间格式", FontSize = 12, FontWeight = FontWeight.Bold };
        timePanel.Children.Add(_formatGroupHeader);

        _timeFormatTextBox = new TextBox { Watermark = "%d天%h小时%m分钟%s秒" };
        timePanel.Children.Add(_timeFormatTextBox);

        _timeFormatHint = new TextBlock
        {
            Text = "格式化变量: %D总天数 %H总小时 %M总分钟 %S总秒 %X总毫秒\n%d天 %h小时 %m分钟 %s秒 %x毫秒\n%L剩余百分比 %P已过百分比 %p已过百分比(两位)\n%yy总年 %YY总年(两位) %mo总月 %MO总月(两位)",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        timePanel.Children.Add(_timeFormatHint);

        // --- 时间基准 ---
        _timeBaseGroupHeader = new TextBlock { Text = "时间基准", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 6, 0, 0) };
        timePanel.Children.Add(_timeBaseGroupHeader);

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

        timePanel.Children.Add(timeBaseRow);

        // --- 开始时间（农历） ---
        BuildStartTimeGroup(timePanel);

        timeGroup.Content = timePanel;
        mainPanel.Children.Add(timeGroup);

        // ==================== 外观设置（含简化模式与进度显示） ====================
        _progressDisplayModeGroupHeader = new TextBlock { Text = "外观设置" };
        var progressDisplayModeGroup = new Expander { Header = _progressDisplayModeGroupHeader, IsExpanded = true };
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

        _listGroupHeader = new TextBlock { Text = "农历倒计时列表" };
        var listGroup = new Expander { Header = _listGroupHeader, IsExpanded = true };
        _listEditor = new CountdownListEditor<LunarCountdownItem>(
            () => Settings.CountdownItems,
            items => Settings.CountdownItems = items,
            item => item.Name,
            item => GetLunarDateDescription(item) + " · " +
                (item.GetTargetTimestamp() > 0
                    ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp()).ToString("yyyy-MM-dd HH:mm:ss")
                    : "无效日期"),
            BuildLunarDetailPanel,
            LunarCountdownItem.CreateDefault,
            item => item.Clone());
        listGroup.Content = _listEditor;
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
        _startTimeGroupHeader = new TextBlock { Text = "开始时间（农历）", FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 6, 0, 0) };
        var startTimePanel = mainPanel;
        mainPanel.Children.Add(_startTimeGroupHeader);

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
        _text1TextBox = new TextBox { Watermark = "", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 1, 1, _text1TextBox);
        AddTableCell(grid, 1, 2, CreateSizeCell(out _text1FontSizeNumericUpDown, OnText1FontSizeChanged, out _text1EnableCustomFontSizeToggle, OnText1EnableCustomFontSizeChanged));
        AddTableCell(grid, 1, 3, CreateColorCell(out _text1FontColorPicker, OnText1ColorChanged, out _text1EnableCustomFontColorToggle, OnText1EnableCustomFontColorChanged));

        // 倒计时名称
        AddTableRowLabel(grid, 2, "倒计时名称");
        var editNameButton = new Button { Content = "前往编辑农历倒计时名称", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        editNameButton.Click += OnText2ButtonClick;
        AddTableCell(grid, 2, 1, editNameButton);
        AddTableCell(grid, 2, 2, CreateSizeCell(out _nameFontSizeNumericUpDown, OnNameFontSizeChanged, out _nameEnableCustomFontSizeToggle, OnNameEnableCustomFontSizeChanged));
        AddTableCell(grid, 2, 3, CreateColorCell(out _nameFontColorPicker, OnNameColorChanged, out _nameEnableCustomFontColorToggle, OnNameEnableCustomFontColorChanged));

        // 文案3
        AddTableRowLabel(grid, 3, "文案3");
        _text3TextBox = new TextBox { Watermark = "还有", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 3, 1, _text3TextBox);
        AddTableCell(grid, 3, 2, CreateSizeCell(out _text3FontSizeNumericUpDown, OnText3FontSizeChanged, out _text3EnableCustomFontSizeToggle, OnText3EnableCustomFontSizeChanged));
        AddTableCell(grid, 3, 3, CreateColorCell(out _text3FontColorPicker, OnText3ColorChanged, out _text3EnableCustomFontColorToggle, OnText3EnableCustomFontColorChanged));

        // 倒计时时间（内容无输入框，格式在时间设置中配置）
        AddTableRowLabel(grid, 4, "倒计时时间");
        var timeContentHint = new TextBlock { Text = "格式在时间设置中配置", FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(timeContentHint);
        AddTableCell(grid, 4, 1, timeContentHint);
        AddTableCell(grid, 4, 2, CreateSizeCell(out _timeFontSizeNumericUpDown, OnTimeFontSizeChanged, out _timeEnableCustomFontSizeToggle, OnTimeEnableCustomFontSizeChanged));
        AddTableCell(grid, 4, 3, CreateColorCell(out _timeFontColorPicker, OnTimeColorChanged, out _timeEnableCustomFontColorToggle, OnTimeEnableCustomFontColorChanged));

        // 文案4
        AddTableRowLabel(grid, 5, "文案4");
        _text4TextBox = new TextBox { Watermark = "", VerticalAlignment = VerticalAlignment.Center };
        AddTableCell(grid, 5, 1, _text4TextBox);
        AddTableCell(grid, 5, 2, CreateSizeCell(out _text4FontSizeNumericUpDown, OnText4FontSizeChanged, out _text4EnableCustomFontSizeToggle, OnText4EnableCustomFontSizeChanged));
        AddTableCell(grid, 5, 3, CreateColorCell(out _text4FontColorPicker, OnText4ColorChanged, out _text4EnableCustomFontColorToggle, OnText4EnableCustomFontColorChanged));

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

    private StackPanel CreateSizeCell(out NumericUpDown? numericUpDown,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler,
        out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
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
        numericUpDown.ValueChanged += valueChangedHandler;
        panel.Children.Add(numericUpDown);
        return panel;
    }

    private StackPanel CreateColorCell(out ColorPicker? colorPicker,
        EventHandler<ColorChangedEventArgs> colorChangedHandler,
        out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
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
        // 不再弹窗编辑：滚动到「农历倒计时列表」内联编辑面板，并确保有选中项。
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
        if (_timeFormatHint != null) _timeFormatHint.Foreground = ThemeHelper.GetGrayBrush();
        if (_timeBaseGroupHeader != null) _timeBaseGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_startTimeGroupHeader != null) _startTimeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_startSolarHourSep1 != null) _startSolarHourSep1.Foreground = ThemeHelper.GetTextBrush();
        if (_startSolarHourSep2 != null) _startSolarHourSep2.Foreground = ThemeHelper.GetTextBrush();
        if (_progressDisplayModeGroupHeader != null) _progressDisplayModeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeToggle != null) _simpleModeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeDesc != null) _simpleModeDesc.Foreground = ThemeHelper.GetGrayBrush();
        if (_listGroupHeader != null) _listGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        _listEditor?.UpdateThemeColors();

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
        if (_simpleModeToggle != null) _simpleModeToggle.IsChecked = Settings.EnableSimpleMode;

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

        AttachEventHandlers();

        // Settings 此时已注入，构建农历倒计时列表与详情面板
        _listEditor?.Refresh();

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

    /// <summary>
    /// 构建单个农历倒计时的详情编辑面板（内联显示，不再使用弹窗）。
    /// 字段风格参考 ClassIsland「档案编辑」：官方 Field 标签在上、输入控件在下。
    /// </summary>
    private Control BuildLunarDetailPanel(LunarCountdownItem item)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(16)
        };

        panel.Children.Add(CountdownDetailFields.SectionHeader("\uec92", "农历倒计时"));

        var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "公历可用范围为1901-02-19 ~ 2101-01-28");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", false);
        panel.Children.Add(infoBar);

        panel.Children.Add(CountdownDetailFields.Text("名称", item.Name, v =>
        {
            item.Name = string.IsNullOrWhiteSpace(v) ? "新农历倒计时" : v;
            _listEditor?.RefreshRow(item);
        }, "新农历倒计时"));

        // ---------- 农历日期 ----------
        var yearRangeCombo = new ComboBox();
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            yearRangeCombo.Items.Add(range);
        }
        bool foundRange = false;
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            if (LunarCalendarHelper.ParseYearRange(range, out var sY, out var eY) &&
                item.LunarYear >= sY && item.LunarYear <= eY)
            {
                yearRangeCombo.SelectedItem = range;
                foundRange = true;
                break;
            }
        }
        if (!foundRange)
            yearRangeCombo.SelectedItem = "1984-2043";

        var tiangan = new[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        var dizhi = new[] { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
        var tianganCombo = new ComboBox();
        foreach (var t in tiangan) tianganCombo.Items.Add(t);
        var dizhiCombo = new ComboBox();
        foreach (var d in dizhi) dizhiCombo.Items.Add(d);
        var tgIndex = (item.LunarYear - 4) % 10;
        if (tgIndex < 0) tgIndex += 10;
        var dzIndex = (item.LunarYear - 4) % 12;
        if (dzIndex < 0) dzIndex += 12;
        tianganCombo.SelectedIndex = tgIndex;
        dizhiCombo.SelectedIndex = dzIndex;

        var monthCombo = new ComboBox();
        for (int i = 1; i <= 12; i++) monthCombo.Items.Add(i.ToString());
        monthCombo.SelectedIndex = Math.Clamp(item.LunarMonth, 1, 12) - 1;

        var dayCombo = new ComboBox();
        for (int i = 1; i <= 30; i++) dayCombo.Items.Add(i.ToString());
        dayCombo.SelectedIndex = Math.Clamp(item.LunarDay, 1, 30) - 1;

        var leapToggle = new ToggleSwitch
        {
            IsChecked = item.IsLeapMonth,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var lunarTimePicker = new TimePicker
        {
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            SelectedTime = new TimeSpan(item.Hour, item.Minute, item.Second)
        };

        // 程序化同步农历/公历控件时抑制提交，避免事件回环
        var suppressCommit = false;

        // 天干地支 + 年份范围 -> 农历年份
        int? ResolveLunarYear()
        {
            if (yearRangeCombo.SelectedItem == null || tianganCombo.SelectedIndex < 0 || dizhiCombo.SelectedIndex < 0)
                return null;
            var yearRange = yearRangeCombo.SelectedItem.ToString();
            if (string.IsNullOrEmpty(yearRange)) return null;
            var yearParts = yearRange.Split('-');
            if (yearParts.Length != 2) return null;
            if (!int.TryParse(yearParts[0], out var startYear) || !int.TryParse(yearParts[1], out var endYear))
                return null;

            var tg = tianganCombo.SelectedIndex;
            var dz = dizhiCombo.SelectedIndex;
            var baseYear = 4;
            var yearOffset = 0;
            while ((baseYear + yearOffset - 4) % 10 != tg || (baseYear + yearOffset - 4) % 12 != dz)
            {
                yearOffset++;
                if (yearOffset > 60) return null;
            }
            var lunarYearVal = baseYear + yearOffset;
            while (lunarYearVal < startYear) lunarYearVal += 60;
            if (lunarYearVal > endYear) lunarYearVal -= 60;
            return lunarYearVal;
        }

        void CommitLunarDate()
        {
            if (suppressCommit)
                return;
            var resolvedYear = ResolveLunarYear();
            if (resolvedYear.HasValue)
            {
                item.LunarYear = resolvedYear.Value;
            }
            if (monthCombo.SelectedIndex >= 0) item.LunarMonth = monthCombo.SelectedIndex + 1;
            if (dayCombo.SelectedIndex >= 0) item.LunarDay = dayCombo.SelectedIndex + 1;
            item.IsLeapMonth = leapToggle.IsChecked == true;
            if (lunarTimePicker.SelectedTime.HasValue)
            {
                item.Hour = lunarTimePicker.SelectedTime.Value.Hours;
                item.Minute = lunarTimePicker.SelectedTime.Value.Minutes;
                item.Second = lunarTimePicker.SelectedTime.Value.Seconds;
            }
            item.IsCompleted = false;
            _listEditor?.RefreshRow(item);
            SyncSolarFromLunar();
        }

        // ---------- 公历对照 ----------
        var currentSolarDate = item.GetTargetTimestamp() > 0
            ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp())
            : Plugin.GetCurrentTime();

        var solarYearTextBox = new TextBox { Width = 80, Watermark = "年", Text = currentSolarDate.Year.ToString() };
        var solarMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) solarMonthComboBox.Items.Add($"{i}月");
        solarMonthComboBox.SelectedIndex = currentSolarDate.Month - 1;
        var solarDayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) solarDayComboBox.Items.Add($"{i}日");
        solarDayComboBox.SelectedItem = $"{currentSolarDate.Day}日";

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(solarYearTextBox, (s, e) => UpdateDayComboBox(solarYearTextBox, solarMonthComboBox, solarDayComboBox));
        solarMonthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(solarYearTextBox, solarMonthComboBox, solarDayComboBox);

        var solarDatePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        solarDatePanel.Children.Add(solarYearTextBox);
        solarDatePanel.Children.Add(solarMonthComboBox);
        solarDatePanel.Children.Add(solarDayComboBox);

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
        solarTimePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, VerticalAlignment = VerticalAlignment.Center });
        solarTimePanel.Children.Add(solarMinuteComboBox);
        solarTimePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, VerticalAlignment = VerticalAlignment.Center });
        solarTimePanel.Children.Add(solarSecondComboBox);

        void SyncSolarFromLunar()
        {
            var solar = item.GetTargetTimestamp() > 0 ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp()) : (DateTime?)null;
            if (!solar.HasValue) return;
            solarYearTextBox.Text = solar.Value.Year.ToString();
            solarMonthComboBox.SelectedIndex = solar.Value.Month - 1;
            UpdateDayComboBox(solarYearTextBox, solarMonthComboBox, solarDayComboBox);
            solarDayComboBox.SelectedItem = $"{solar.Value.Day}日";
            solarHourComboBox.SelectedIndex = solar.Value.Hour;
            solarMinuteComboBox.SelectedIndex = solar.Value.Minute;
            solarSecondComboBox.SelectedIndex = solar.Value.Second;
        }

        var syncToLunarButton = new Button { Content = "同步公历 → 农历", HorizontalAlignment = HorizontalAlignment.Left };
        syncToLunarButton.Click += (s, e) =>
        {
            if (!int.TryParse(solarYearTextBox.Text?.Trim(), out var year) ||
                solarMonthComboBox.SelectedIndex < 0 ||
                solarDayComboBox.SelectedItem == null ||
                !int.TryParse(solarDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) ||
                solarHourComboBox.SelectedIndex < 0 ||
                solarMinuteComboBox.SelectedIndex < 0 ||
                solarSecondComboBox.SelectedIndex < 0)
            {
                return;
            }

            try
            {
                var solarDate = new DateTime(year, solarMonthComboBox.SelectedIndex + 1, day,
                    solarHourComboBox.SelectedIndex, solarMinuteComboBox.SelectedIndex, solarSecondComboBox.SelectedIndex);
                if (!LunarCalendarHelper.IsDateSupported(solarDate)) return;

                var lunarYear = LunarCalendarHelper.GetLunarYear(solarDate);
                var lunarMonth = LunarCalendarHelper.GetLunarMonth(solarDate);
                var isLeap = LunarCalendarHelper.IsLeapMonth(solarDate);
                var lunarDay = LunarCalendarHelper.GetLunarDay(solarDate);
                if (lunarYear == 0 || lunarMonth == 0 || lunarDay == 0) return;

                var tgNew = (lunarYear - 4) % 10;
                if (tgNew < 0) tgNew += 10;
                var dzNew = (lunarYear - 4) % 12;
                if (dzNew < 0) dzNew += 12;

                // 程序化写入农历控件会逐个触发 CommitLunarDate（部分更新的中间状态），
                // 此处抑制提交，最后统一按换算结果一次性落盘。
                suppressCommit = true;
                try
                {
                    tianganCombo.SelectedIndex = tgNew;
                    dizhiCombo.SelectedIndex = dzNew;
                    monthCombo.SelectedIndex = lunarMonth - 1;
                    dayCombo.SelectedIndex = lunarDay - 1;
                    leapToggle.IsChecked = isLeap;
                    lunarTimePicker.SelectedTime = new TimeSpan(solarDate.Hour, solarDate.Minute, solarDate.Second);

                    foreach (var range in LunarCalendarHelper.GetAllYearRanges())
                    {
                        if (LunarCalendarHelper.ParseYearRange(range, out var sY, out var eY) &&
                            lunarYear >= sY && lunarYear <= eY)
                        {
                            yearRangeCombo.SelectedItem = range;
                            break;
                        }
                    }
                }
                finally
                {
                    suppressCommit = false;
                }

                item.LunarYear = lunarYear;
                item.LunarMonth = lunarMonth;
                item.LunarDay = lunarDay;
                item.IsLeapMonth = isLeap;
                item.Hour = solarDate.Hour;
                item.Minute = solarDate.Minute;
                item.Second = solarDate.Second;
                item.IsCompleted = false;
                _listEditor?.RefreshRow(item);
            }
            catch
            {
                // 日期非法时不做处理
            }
        };

        var syncToSolarButton = new Button { Content = "同步农历 → 公历", HorizontalAlignment = HorizontalAlignment.Left };
        syncToSolarButton.Click += (s, e) => CommitLunarDate();

        panel.Children.Add(CountdownDetailFields.Labeled("年份范围", yearRangeCombo));

        var ganzhiPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        ganzhiPanel.Children.Add(tianganCombo);
        ganzhiPanel.Children.Add(dizhiCombo);
        panel.Children.Add(CountdownDetailFields.Labeled("天干地支年", ganzhiPanel));

        var monthRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        monthRow.Children.Add(new TextBlock { Text = "月", VerticalAlignment = VerticalAlignment.Center });
        monthRow.Children.Add(monthCombo);
        monthRow.Children.Add(new TextBlock { Text = "日", VerticalAlignment = VerticalAlignment.Center });
        monthRow.Children.Add(dayCombo);
        panel.Children.Add(CountdownDetailFields.Labeled("农历月日", monthRow));
        panel.Children.Add(CountdownDetailFields.Labeled("闰月", leapToggle));
        panel.Children.Add(CountdownDetailFields.Labeled("时间", lunarTimePicker));

        panel.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 4) });
        panel.Children.Add(CountdownDetailFields.SectionHeader("\ue917", "公历对照（可互转）"));
        panel.Children.Add(CountdownDetailFields.Labeled("公历日期", solarDatePanel));
        panel.Children.Add(CountdownDetailFields.Labeled("公历时间", solarTimePanel));

        var syncButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        syncButtons.Children.Add(syncToLunarButton);
        syncButtons.Children.Add(syncToSolarButton);
        panel.Children.Add(syncButtons);

        yearRangeCombo.SelectionChanged += (s, e) => CommitLunarDate();
        tianganCombo.SelectionChanged += (s, e) => CommitLunarDate();
        dizhiCombo.SelectionChanged += (s, e) => CommitLunarDate();
        monthCombo.SelectionChanged += (s, e) => CommitLunarDate();
        dayCombo.SelectionChanged += (s, e) => CommitLunarDate();
        leapToggle.IsCheckedChanged += (s, e) => CommitLunarDate();
        lunarTimePicker.SelectedTimeChanged += (s, e) => CommitLunarDate();

        panel.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 4) });
        panel.Children.Add(CountdownDetailFields.SectionHeader("\uef2b", "通知设置"));
        panel.Children.Add(CountdownDetailFields.Toggle("启用通知", item.EnableNotification, v => item.EnableNotification = v));

        return panel;
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



