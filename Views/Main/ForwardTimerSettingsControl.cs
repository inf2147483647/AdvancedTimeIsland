using System;
using System.Collections.Generic;
using System.Globalization;
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
    private TextBox? _startYearTextBox;
    private ComboBox? _startMonthComboBox;
    private ComboBox? _startDayComboBox;
    private ComboBox? _startHourComboBox;
    private ComboBox? _startMinuteComboBox;
    private ComboBox? _startSecondComboBox;
    private TextBox? _text1FontSizeTextBox;
    private ColorPicker? _text1FontColorPicker;
    private TextBox? _nameFontSizeTextBox;
    private ColorPicker? _nameFontColorPicker;
    private TextBox? _text3FontSizeTextBox;
    private ColorPicker? _text3FontColorPicker;
    private TextBox? _timeFontSizeTextBox;
    private ColorPicker? _timeFontColorPicker;
    private TextBox? _text4FontSizeTextBox;
    private ColorPicker? _text4FontColorPicker;

    private TextBlock? _orderHintTextBlock;
    private TextBlock? _textGroupHeader;
    private TextBlock? _formatGroupHeader;
    private TextBlock? _formatLabel;
    private TextBlock? _timeBaseGroupHeader;
    private TextBlock? _timeBaseLabel;
    private TextBlock? _startTimeGroupHeader;
    private TextBlock? _startDateLabel;
    private TextBlock? _startTimeLabel;
    private TextBlock? _hourSeparator;
    private TextBlock? _minuteSeparator;
    private TextBlock? _fontGroupHeader;

    private TextBlock? _text1StyleTextBlock;
    private TextBlock? _nameStyleTextBlock;
    private TextBlock? _text3StyleTextBlock;
    private TextBlock? _timeStyleTextBlock;
    private TextBlock? _text4StyleTextBlock;

    private List<TextBlock> _dynamicTextBlocks = new();

    public ForwardTimerSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8, Margin = new Thickness(12) };

        _textGroupHeader = new TextBlock { Text = "文案设置" };
        var textGroup = new Expander { Header = _textGroupHeader, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _orderHintTextBlock = new TextBlock { Text = "以下内容在主界面上显示的顺序为：文案1->正向计时器名称->文案3->已过时间->文案4", FontSize = 11, FontWeight = FontWeight.Bold };
        textPanel.Children.Add(_orderHintTextBlock);
        textPanel.Children.Add(CreateTextRow("文案1", "", out _text1TextBox));
        textPanel.Children.Add(CreateTextRow("正向计时器名称", "", out _nameTextBox));
        textPanel.Children.Add(CreateTextRow("文案3", "已过", out _text3TextBox));
        textPanel.Children.Add(CreateTextRow("文案4", "", out _text4TextBox));

        textGroup.Content = textPanel;
        mainPanel.Children.Add(textGroup);

        _formatGroupHeader = new TextBlock { Text = "时间格式" };
        var formatGroup = new Expander { Header = _formatGroupHeader, IsExpanded = false };
        var formatPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _formatLabel = new TextBlock { Text = "格式化文本:", FontSize = 12, FontWeight = FontWeight.Bold };
        formatPanel.Children.Add(_formatLabel);
        _timeFormatTextBox = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, Text = "%d天%h小时%m分钟%s秒" };
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
        var timeBaseGroup = new Expander { Header = _timeBaseGroupHeader, IsExpanded = false };
        var timeBasePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _timeBaseLabel = new TextBlock { Text = "时间来源:", FontSize = 12, FontWeight = FontWeight.Bold };
        timeBasePanel.Children.Add(_timeBaseLabel);
        _timeBaseComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left };
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("插件偏移后的系统时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("原始系统时间");
        timeBasePanel.Children.Add(_timeBaseComboBox);

        timeBaseGroup.Content = timeBasePanel;
        mainPanel.Children.Add(timeBaseGroup);

        _startTimeGroupHeader = new TextBlock { Text = "开始时间" };
        var startTimeGroup = new Expander { Header = _startTimeGroupHeader, IsExpanded = false };
        var startTimePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

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

        startTimePanel.Children.Add(startDateRow);

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

        startTimePanel.Children.Add(startTimeRow);

        startTimeGroup.Content = startTimePanel;
        mainPanel.Children.Add(startTimeGroup);

        _fontGroupHeader = new TextBlock { Text = "字体样式" };
        var fontGroup = new Expander { Header = _fontGroupHeader, IsExpanded = false };
        var fontPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _text1StyleTextBlock = new TextBlock { Text = "文案1样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_text1StyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _text1FontSizeTextBox, out _text1EnableCustomFontSizeToggle, OnText1EnableCustomFontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text1FontColorPicker, out _text1EnableCustomFontColorToggle, OnText1EnableCustomFontColorChanged));

        _nameStyleTextBlock = new TextBlock { Text = "正向计时器名称样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_nameStyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _nameFontSizeTextBox, out _nameEnableCustomFontSizeToggle, OnNameEnableCustomFontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _nameFontColorPicker, out _nameEnableCustomFontColorToggle, OnNameEnableCustomFontColorChanged));

        _text3StyleTextBlock = new TextBlock { Text = "文案3样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_text3StyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _text3FontSizeTextBox, out _text3EnableCustomFontSizeToggle, OnText3EnableCustomFontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text3FontColorPicker, out _text3EnableCustomFontColorToggle, OnText3EnableCustomFontColorChanged));

        _timeStyleTextBlock = new TextBlock { Text = "时间样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_timeStyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _timeFontSizeTextBox, out _timeEnableCustomFontSizeToggle, OnTimeEnableCustomFontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _timeFontColorPicker, out _timeEnableCustomFontColorToggle, OnTimeEnableCustomFontColorChanged));

        _text4StyleTextBlock = new TextBlock { Text = "文案4样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_text4StyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _text4FontSizeTextBox, out _text4EnableCustomFontSizeToggle, OnText4EnableCustomFontSizeChanged));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text4FontColorPicker, out _text4EnableCustomFontColorToggle, OnText4EnableCustomFontColorChanged));

        fontGroup.Content = fontPanel;
        mainPanel.Children.Add(fontGroup);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = mainPanel
        };
        Content = scrollViewer;
    }

    private Grid CreateTextRow(string label, string watermark, out TextBox? textBox)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        textBox = new TextBox { Watermark = watermark };
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);

        return row;
    }

    private Grid CreateFontRow(string label, out TextBox? textBox, out ToggleSwitch? toggle, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        textBox = new TextBox { Width = 80, Watermark = "14", HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);

        toggle = new ToggleSwitch { Content = "使用自定义大小" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateColorRow(string label, out ColorPicker? colorPicker, out ToggleSwitch? toggle, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        colorPicker = new ColorPicker { HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        toggle = new ToggleSwitch { Content = "使用自定义颜色" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private void UpdateThemeColors()
    {
        if (_orderHintTextBlock != null) _orderHintTextBlock.Foreground = ThemeHelper.GetYellowBrush();
        if (_textGroupHeader != null) _textGroupHeader.Foreground = ThemeHelper.GetTextBrush();
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
        if (_fontGroupHeader != null) _fontGroupHeader.Foreground = ThemeHelper.GetTextBrush();

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

    private void UpdateControlsEnabled()
    {
        _text1FontSizeTextBox?.SetValue(IsEnabledProperty, Settings.Text1EnableCustomFontSize);
        _text1FontColorPicker?.SetValue(IsEnabledProperty, Settings.Text1EnableCustomFontColor);
        _nameFontSizeTextBox?.SetValue(IsEnabledProperty, Settings.NameEnableCustomFontSize);
        _nameFontColorPicker?.SetValue(IsEnabledProperty, Settings.NameEnableCustomFontColor);
        _text3FontSizeTextBox?.SetValue(IsEnabledProperty, Settings.Text3EnableCustomFontSize);
        _text3FontColorPicker?.SetValue(IsEnabledProperty, Settings.Text3EnableCustomFontColor);
        _timeFontSizeTextBox?.SetValue(IsEnabledProperty, Settings.TimeEnableCustomFontSize);
        _timeFontColorPicker?.SetValue(IsEnabledProperty, Settings.TimeEnableCustomFontColor);
        _text4FontSizeTextBox?.SetValue(IsEnabledProperty, Settings.Text4EnableCustomFontSize);
        _text4FontColorPicker?.SetValue(IsEnabledProperty, Settings.Text4EnableCustomFontColor);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
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
        if (_timeBaseComboBox != null) _timeBaseComboBox.SelectedIndex = (int)Settings.TimeBaseType;

        var startTime = DateTimeOffset.FromUnixTimeSeconds(Settings.StartTime).LocalDateTime;
        if (_startYearTextBox != null) _startYearTextBox.Text = startTime.Year.ToString();
        if (_startMonthComboBox != null) _startMonthComboBox.SelectedIndex = startTime.Month - 1;
        if (_startDayComboBox != null) _startDayComboBox.SelectedItem = $"{startTime.Day}日";
        if (_startHourComboBox != null) _startHourComboBox.SelectedIndex = startTime.Hour;
        if (_startMinuteComboBox != null) _startMinuteComboBox.SelectedIndex = startTime.Minute;
        if (_startSecondComboBox != null) _startSecondComboBox.SelectedIndex = startTime.Second;

        if (_text1FontSizeTextBox != null) _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(CultureInfo.InvariantCulture);
        if (_text1FontColorPicker != null) _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        if (_nameFontSizeTextBox != null) _nameFontSizeTextBox.Text = Settings.NameFontSize.ToString(CultureInfo.InvariantCulture);
        if (_nameFontColorPicker != null) _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        if (_text3FontSizeTextBox != null) _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(CultureInfo.InvariantCulture);
        if (_text3FontColorPicker != null) _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        if (_timeFontSizeTextBox != null) _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(CultureInfo.InvariantCulture);
        if (_timeFontColorPicker != null) _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        if (_text4FontSizeTextBox != null) _text4FontSizeTextBox.Text = Settings.Text4FontSize.ToString(CultureInfo.InvariantCulture);
        if (_text4FontColorPicker != null) _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);

        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "");
        AttachTextHandler(_nameTextBox, v => Settings.Name = v ?? "");
        AttachTextHandler(_text3TextBox, v => Settings.Text3 = v ?? "已过");
        AttachTextHandler(_text4TextBox, v => Settings.Text4 = v ?? "");
        AttachTextHandler(_timeFormatTextBox, v => Settings.TimeFormat = v ?? "%d天%h小时%m分钟%s秒");

        if (_timeBaseComboBox != null)
        {
            _timeBaseComboBox.SelectionChanged += (s, e) =>
            {
                Settings.TimeBaseType = (TimeBaseType)_timeBaseComboBox.SelectedIndex;
            };
        }

        AttachDateTimeHandlers();

        AttachFontHandlers(_text1FontSizeTextBox, _text1FontColorPicker, (fs, fc) => { Settings.Text1FontSize = fs; Settings.Text1FontColor = fc; });
        AttachFontHandlers(_nameFontSizeTextBox, _nameFontColorPicker, (fs, fc) => { Settings.NameFontSize = fs; Settings.NameFontColor = fc; });
        AttachFontHandlers(_text3FontSizeTextBox, _text3FontColorPicker, (fs, fc) => { Settings.Text3FontSize = fs; Settings.Text3FontColor = fc; });
        AttachFontHandlers(_timeFontSizeTextBox, _timeFontColorPicker, (fs, fc) => { Settings.TimeFontSize = fs; Settings.TimeFontColor = fc; });
        AttachFontHandlers(_text4FontSizeTextBox, _text4FontColorPicker, (fs, fc) => { Settings.Text4FontSize = fs; Settings.Text4FontColor = fc; });

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

    private void AttachFontHandlers(TextBox? fontSizeTextBox, ColorPicker? colorPicker, Action<double, string> handler)
    {
        if (fontSizeTextBox != null)
        {
            FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(fontSizeTextBox, (s, e) =>
            {
                if (double.TryParse(fontSizeTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var fontSize))
                {
                    var color = colorPicker?.Color.ToString() ?? "#FFFFFF";
                    handler(fontSize, color);
                }
            });
        }

        if (colorPicker != null)
        {
            colorPicker.ColorChanged += (s, e) =>
            {
                if (double.TryParse(fontSizeTextBox?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var fontSize))
                {
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
            return ((SolidColorBrush)ThemeHelper.GetTextBrush()).Color;
        }
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


