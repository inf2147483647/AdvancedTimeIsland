using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    private ToggleSwitch? _enableCustomFontSizeToggle;
    private ToggleSwitch? _enableCustomFontColorToggle;
    private ComboBox? _timeBaseComboBox;
    private ListBox? _countdownListBox;
    private Button? _addButton;
    private Button? _removeButton;
    private Button? _editButton;

    private TextBlock? _selectionHintTextBlock;
    private System.Timers.Timer? _hintTimer;

    private TextBox? _text1FontSizeTextBox;
    private ColorPicker? _text1FontColorPicker;
    private TextBox? _text2FontSizeTextBox;
    private ColorPicker? _text2FontColorPicker;
    private TextBox? _text3FontSizeTextBox;
    private ColorPicker? _text3FontColorPicker;
    private TextBox? _timeFontSizeTextBox;
    private ColorPicker? _timeFontColorPicker;
    private TextBox? _text4FontSizeTextBox;
    private ColorPicker? _text4FontColorPicker;

    private TextBlock? _titleTextBlock;
    private TextBlock? _descTextBlock;
    private TextBlock? _orderHintTextBlock;
    private TextBlock? _textGroupHeader;
    private TextBlock? _formatGroupHeader;
    private TextBlock? _formatLabel;
    private TextBlock? _timeBaseGroupHeader;
    private TextBlock? _timeBaseLabel;
    private TextBlock? _fontGroupHeader;
    private TextBlock? _listGroupHeader;
    private TextBlock? _nameHeader;
    private TextBlock? _periodHeader;
    private TextBlock? _notifyHeader;

    private ComboBox? _progressDisplayModeComboBox;
    private TextBlock? _progressDisplayModeLabel;
    private TextBlock? _progressDisplayModeGroupHeader;
    private ToggleSwitch? _enableCustomProgressColorToggle;
    private ColorPicker? _progressBarColorPicker;
    private ColorPicker? _progressRingColorPicker;

    private TextBlock? _text1StyleTextBlock;
    private TextBlock? _text2StyleTextBlock;
    private TextBlock? _text3StyleTextBlock;
    private TextBlock? _timeStyleTextBlock;
    private TextBlock? _text4StyleTextBlock;

    private List<TextBlock> _dynamicTextBlocks = new();
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

        _textGroupHeader = new TextBlock { Text = "文案设置" };
        var textGroup = new Expander { Header = _textGroupHeader, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _orderHintTextBlock = new TextBlock { Text = "以下内容在主界面上显示的顺序为：文案1->倒计时名称->文案3->剩余时间->文案4", FontSize = 11, FontWeight = FontWeight.Bold };
        textPanel.Children.Add(_orderHintTextBlock);
        textPanel.Children.Add(CreateTextRow("文案1", "距离", out _text1TextBox));
        textPanel.Children.Add(CreateText2ButtonRow());
        textPanel.Children.Add(CreateTextRow("文案3", "还有", out _text3TextBox));
        textPanel.Children.Add(CreateTextRow("文案4", "", out _text4TextBox));

        textGroup.Content = textPanel;
        _mainPanel.Children.Add(textGroup);

        _formatGroupHeader = new TextBlock { Text = "时间格式" };
        var formatGroup = new Expander { Header = _formatGroupHeader, IsExpanded = true };
        var formatPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var formatRow = new Grid();
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _formatLabel = new TextBlock { Text = "时间格式:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_formatLabel, 0);
        formatRow.Children.Add(_formatLabel);

        _timeFormatTextBox = new TextBox { Watermark = "%d天%h小时%m分钟%s秒" };
        Grid.SetColumn(_timeFormatTextBox, 1);
        formatRow.Children.Add(_timeFormatTextBox);

        formatPanel.Children.Add(formatRow);

        _timeFormatHint = new TextBlock
        {
            Text = "格式化变量: %D总天数 %H总小时 %M总分钟 %S总秒 %X总毫秒\n%d天 %h小时 %m分钟 %s秒 %x毫秒\n%L剩余百分比 %P已过百分比 %p已过百分比(两位)\n%yy总年 %YY总年(两位) %mo总月 %MO总月(两位)",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        formatPanel.Children.Add(_timeFormatHint);

        _timeCorrectionToggle = new ToggleSwitch
        {
            Content = "差一矫正（当精度不足时最小单位加一）",
            IsChecked = true
        };
        _timeCorrectionToggle.IsCheckedChanged += (s, e) =>
        {
            Settings.EnableTimeCorrection = _timeCorrectionToggle.IsChecked == true;
        };
        formatPanel.Children.Add(_timeCorrectionToggle);

        formatGroup.Content = formatPanel;
        _mainPanel.Children.Add(formatGroup);

        _timeBaseGroupHeader = new TextBlock { Text = "时间基准" };
        var timeBaseGroup = new Expander { Header = _timeBaseGroupHeader, IsExpanded = true };
        var timeBasePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

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

        timeBasePanel.Children.Add(timeBaseRow);
        timeBaseGroup.Content = timeBasePanel;
        _mainPanel.Children.Add(timeBaseGroup);

        _progressDisplayModeGroupHeader = new TextBlock { Text = "进度显示" };
        var progressDisplayModeGroup = new Expander { Header = _progressDisplayModeGroupHeader, IsExpanded = true };
        var progressDisplayModePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

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

        _enableCustomProgressColorToggle = new ToggleSwitch { Content = "启用自定义进度颜色", Margin = new Thickness(0, 6, 0, 0) };
        _enableCustomProgressColorToggle.IsCheckedChanged += OnEnableCustomProgressColorChanged;
        progressDisplayModePanel.Children.Add(_enableCustomProgressColorToggle);

        var progressBarColorRow = CreateColorRow("进度条颜色:", out _progressBarColorPicker);
        progressDisplayModePanel.Children.Add(progressBarColorRow);

        var progressRingColorRow = CreateColorRow("进度环颜色:", out _progressRingColorPicker);
        progressDisplayModePanel.Children.Add(progressRingColorRow);

        progressDisplayModeGroup.Content = progressDisplayModePanel;
        _mainPanel.Children.Add(progressDisplayModeGroup);

        _enableCustomFontSizeToggle = new ToggleSwitch { Content = "启用自定义字体大小", Margin = new Thickness(0, 10, 0, 0) };
        _enableCustomFontSizeToggle.IsCheckedChanged += OnEnableCustomFontSizeChanged;
        _mainPanel.Children.Add(_enableCustomFontSizeToggle);

        _enableCustomFontColorToggle = new ToggleSwitch { Content = "启用自定义字体颜色", Margin = new Thickness(0, 4, 0, 0) };
        _enableCustomFontColorToggle.IsCheckedChanged += OnEnableCustomFontColorChanged;
        _mainPanel.Children.Add(_enableCustomFontColorToggle);

        _fontGroupHeader = new TextBlock { Text = "字体样式" };
        var fontGroup = new Expander { Header = _fontGroupHeader, IsExpanded = false };
        var fontPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _text1StyleTextBlock = new TextBlock { Text = "文案1样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_text1StyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _text1FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text1FontColorPicker));

        _text2StyleTextBlock = new TextBlock { Text = "文案2样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_text2StyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _text2FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text2FontColorPicker));

        _text3StyleTextBlock = new TextBlock { Text = "文案3样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_text3StyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _text3FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text3FontColorPicker));

        _timeStyleTextBlock = new TextBlock { Text = "时间样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_timeStyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _timeFontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _timeFontColorPicker));

        _text4StyleTextBlock = new TextBlock { Text = "文案4样式", FontSize = 12, FontWeight = FontWeight.Bold };
        fontPanel.Children.Add(_text4StyleTextBlock);
        fontPanel.Children.Add(CreateFontRow("大小:", out _text4FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text4FontColorPicker));

        fontGroup.Content = fontPanel;
        _mainPanel.Children.Add(fontGroup);

        _listGroupHeader = new TextBlock { Text = "倒计时列表" };
        var listGroup = new Expander { Header = _listGroupHeader, IsExpanded = true };
        var listPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        _nameHeader = new TextBlock
        {
            Text = "倒计时名称",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_nameHeader, 0);
        headerGrid.Children.Add(_nameHeader);

        _periodHeader = new TextBlock
        {
            Text = "周期",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(_periodHeader, 1);
        headerGrid.Children.Add(_periodHeader);

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
            Text = "请选择一个倒计时",
            Foreground = Brushes.Orange,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 12
        };
        listPanel.Children.Add(_selectionHintTextBlock);
        listGroup.Content = listPanel;
        _mainPanel.Children.Add(listGroup);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _mainPanel
        };
        Content = scrollViewer;
    }

    private Grid CreateTextRow(string label, string watermark, out TextBox? textBox)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        textBox = new TextBox { Watermark = watermark };
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);

        return row;
    }

    private Grid CreateFontRow(string label, out TextBox? textBox)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        textBox = new TextBox { Width = 80, Watermark = "14", HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);

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

    private Grid CreateText2ButtonRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = "倒计时名称", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        _dynamicTextBlocks.Add(lbl);
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        var button = new Button
        {
            Content = "前往编辑倒计时名称",
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
        if (_formatLabel != null) _formatLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_timeFormatHint != null) _timeFormatHint.Foreground = ThemeHelper.GetGrayBrush();
        if (_timeBaseGroupHeader != null) _timeBaseGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_timeBaseLabel != null) _timeBaseLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_progressDisplayModeGroupHeader != null) _progressDisplayModeGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_progressDisplayModeLabel != null) _progressDisplayModeLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_fontGroupHeader != null) _fontGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_listGroupHeader != null) _listGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_nameHeader != null) _nameHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_periodHeader != null) _periodHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_notifyHeader != null) _notifyHeader.Foreground = ThemeHelper.GetSubTextBrush();
        if (_selectionHintTextBlock != null) _selectionHintTextBlock.Foreground = ThemeHelper.GetOrangeBrush();

        if (_text1StyleTextBlock != null) _text1StyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_text2StyleTextBlock != null) _text2StyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_text3StyleTextBlock != null) _text3StyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_timeStyleTextBlock != null) _timeStyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();
        if (_text4StyleTextBlock != null) _text4StyleTextBlock.Foreground = ThemeHelper.GetLightBlueBrush();

        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetTextBrush();
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
        if (!Settings.EnableCustomFontColor)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void UpdateFontColorsForTheme()
    {
        var newColor = ThemeHelper.GetThemeAwareTextColor();

        if (_text1FontColorPicker != null)
        {
            Settings.Text1FontColor = newColor;
            _text1FontColorPicker.Color = ParseColor(newColor);
        }
        if (_text2FontColorPicker != null)
        {
            Settings.Text2FontColor = newColor;
            _text2FontColorPicker.Color = ParseColor(newColor);
        }
        if (_text3FontColorPicker != null)
        {
            Settings.Text3FontColor = newColor;
            _text3FontColorPicker.Color = ParseColor(newColor);
        }
        if (_timeFontColorPicker != null)
        {
            Settings.TimeFontColor = newColor;
            _timeFontColorPicker.Color = ParseColor(newColor);
        }
        if (_text4FontColorPicker != null)
        {
            Settings.Text4FontColor = newColor;
            _text4FontColorPicker.Color = ParseColor(newColor);
        }
    }

    private void OnEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontColor = _enableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
        if (!Settings.EnableCustomFontColor)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void OnEnableCustomProgressColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomProgressColor = _enableCustomProgressColorToggle?.IsChecked ?? false;
        UpdateProgressColorControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        var fontSizeEnabled = Settings.EnableCustomFontSize;
        var fontColorEnabled = Settings.EnableCustomFontColor;
        _text1FontSizeTextBox?.SetValue(IsEnabledProperty, fontSizeEnabled);
        _text1FontColorPicker?.SetValue(IsEnabledProperty, fontColorEnabled);
        _text2FontSizeTextBox?.SetValue(IsEnabledProperty, fontSizeEnabled);
        _text2FontColorPicker?.SetValue(IsEnabledProperty, fontColorEnabled);
        _text3FontSizeTextBox?.SetValue(IsEnabledProperty, fontSizeEnabled);
        _text3FontColorPicker?.SetValue(IsEnabledProperty, fontColorEnabled);
        _timeFontSizeTextBox?.SetValue(IsEnabledProperty, fontSizeEnabled);
        _timeFontColorPicker?.SetValue(IsEnabledProperty, fontColorEnabled);
        _text4FontSizeTextBox?.SetValue(IsEnabledProperty, fontSizeEnabled);
        _text4FontColorPicker?.SetValue(IsEnabledProperty, fontColorEnabled);
    }

    private void UpdateProgressColorControlsEnabled()
    {
        var isCustomEnabled = Settings.EnableCustomProgressColor;
        var showProgressBar = Settings.ProgressDisplayMode == ProgressDisplayMode.Bar || 
                              Settings.ProgressDisplayMode == ProgressDisplayMode.Both;
        
        _progressBarColorPicker?.SetValue(IsEnabledProperty, isCustomEnabled && showProgressBar);
        _progressRingColorPicker?.SetValue(IsEnabledProperty, isCustomEnabled);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
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

        if (_text1FontSizeTextBox != null) _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_text1FontColorPicker != null) _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        if (_text2FontSizeTextBox != null) _text2FontSizeTextBox.Text = Settings.Text2FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_text2FontColorPicker != null) _text2FontColorPicker.Color = ParseColor(Settings.Text2FontColor);
        if (_text3FontSizeTextBox != null) _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_text3FontColorPicker != null) _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        if (_timeFontSizeTextBox != null) _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_timeFontColorPicker != null) _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        if (_text4FontSizeTextBox != null) _text4FontSizeTextBox.Text = Settings.Text4FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_text4FontColorPicker != null) _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);

        UpdateCountdownList();

        AttachEventHandlers();

        if (_enableCustomFontSizeToggle != null)
        {
            _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        }
        if (_enableCustomFontColorToggle != null)
        {
            _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        }
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

        AttachFontSizeHandler(_text1FontSizeTextBox, v => Settings.Text1FontSize = v);
        AttachColorPickerHandler(_text1FontColorPicker, v => Settings.Text1FontColor = v);
        AttachFontSizeHandler(_text2FontSizeTextBox, v => Settings.Text2FontSize = v);
        AttachColorPickerHandler(_text2FontColorPicker, v => Settings.Text2FontColor = v);
        AttachFontSizeHandler(_text3FontSizeTextBox, v => Settings.Text3FontSize = v);
        AttachColorPickerHandler(_text3FontColorPicker, v => Settings.Text3FontColor = v);
        AttachFontSizeHandler(_timeFontSizeTextBox, v => Settings.TimeFontSize = v);
        AttachColorPickerHandler(_timeFontColorPicker, v => Settings.TimeFontColor = v);
        AttachFontSizeHandler(_text4FontSizeTextBox, v => Settings.Text4FontSize = v);
        AttachColorPickerHandler(_text4FontColorPicker, v => Settings.Text4FontColor = v);

        AttachColorPickerHandler(_progressBarColorPicker, v => Settings.ProgressBarColor = v);
        AttachColorPickerHandler(_progressRingColorPicker, v => Settings.ProgressRingColor = v);
    }

    private void AttachTextHandler(TextBox? textBox, Action<string?> setter)
    {
        if (textBox == null) return;
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => setter(textBox.Text));
    }

    private void AttachFontSizeHandler(TextBox? textBox, Action<double> setter)
    {
        if (textBox == null) return;
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => ParseAndSetFontSize(textBox, setter));
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

    private void ParseAndSetFontSize(TextBox textBox, Action<double> setter)
    {
        if (double.TryParse(textBox.Text, out double size))
        {
            setter(size);
            textBox.Text = size.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            textBox.Text = "14";
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
                var container = new Grid();
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                container.Tag = item;

                var textBlock = new TextBlock
                {
                    Text = item.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetTextBrush(),
                    Padding = new Avalonia.Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(textBlock, 0);
                container.Children.Add(textBlock);

                var periodTextBlock = new TextBlock
                {
                    Text = GetPeriodTypeName(item.PeriodType),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(periodTextBlock, 1);
                container.Children.Add(periodTextBlock);

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

    private void OnAddClick(object? sender, EventArgs e)
    {
        HideHint();
        if (Settings.CountdownItems == null)
        {
            Settings.CountdownItems = new List<PeriodicCountdownItem>();
        }
        Settings.CountdownItems.Add(PeriodicCountdownItem.CreateDefault());
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

    private void ShowEditDialog(PeriodicCountdownItem item, int order = 0)
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", order > 0 ? $"正在编辑第{order}个倒计时" : "编辑倒计时");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "SecondaryButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var nameLabel = new TextBlock { Text = "名称:", Foreground = ThemeHelper.GetTextBrush() };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var periodLabel = new TextBlock { Text = "周期:", Foreground = ThemeHelper.GetTextBrush() };
        contentPanel.Children.Add(periodLabel);

        var periodComboBox = new ComboBox();
        periodComboBox.Items.Add("每小时");
        periodComboBox.Items.Add("每天");
        periodComboBox.Items.Add("每周");
        periodComboBox.Items.Add("每月");
        periodComboBox.Items.Add("每年");
        periodComboBox.SelectedIndex = (int)item.PeriodType;
        contentPanel.Children.Add(periodComboBox);

        var timeLabel = new TextBlock { Text = "时间:", Foreground = ThemeHelper.GetTextBrush() };
        contentPanel.Children.Add(timeLabel);

        var timePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var hourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) hourComboBox.Items.Add(i.ToString("D2"));
        hourComboBox.SelectedIndex = item.Hour;
        var minuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) minuteComboBox.Items.Add(i.ToString("D2"));
        minuteComboBox.SelectedIndex = item.Minute;
        var secondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) secondComboBox.Items.Add(i.ToString("D2"));
        secondComboBox.SelectedIndex = item.Second;

        timePanel.Children.Add(hourComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(minuteComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(secondComboBox);
        contentPanel.Children.Add(timePanel);

        var extraPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var dayOfWeekRow = new Grid();
        dayOfWeekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dayOfWeekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dayOfWeekLabel = new TextBlock { Text = "星期:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dayOfWeekLabel, 0);
        dayOfWeekRow.Children.Add(dayOfWeekLabel);

        var dayOfWeekComboBox = new ComboBox();
        dayOfWeekComboBox.Items.Add("星期日");
        dayOfWeekComboBox.Items.Add("星期一");
        dayOfWeekComboBox.Items.Add("星期二");
        dayOfWeekComboBox.Items.Add("星期三");
        dayOfWeekComboBox.Items.Add("星期四");
        dayOfWeekComboBox.Items.Add("星期五");
        dayOfWeekComboBox.Items.Add("星期六");
        dayOfWeekComboBox.SelectedIndex = item.DayOfWeek;
        Grid.SetColumn(dayOfWeekComboBox, 1);
        dayOfWeekRow.Children.Add(dayOfWeekComboBox);
        extraPanel.Children.Add(dayOfWeekRow);

        var dayOfMonthRow = new Grid();
        dayOfMonthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dayOfMonthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dayOfMonthLabel = new TextBlock { Text = "日期:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dayOfMonthLabel, 0);
        dayOfMonthRow.Children.Add(dayOfMonthLabel);

        var dayOfMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) dayOfMonthComboBox.Items.Add($"{i}日");
        dayOfMonthComboBox.SelectedItem = $"{item.DayOfMonth}日";
        Grid.SetColumn(dayOfMonthComboBox, 1);
        dayOfMonthRow.Children.Add(dayOfMonthComboBox);
        extraPanel.Children.Add(dayOfMonthRow);

        var monthRow = new Grid();
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var monthLabel = new TextBlock { Text = "月份:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(monthLabel, 0);
        monthRow.Children.Add(monthLabel);

        var monthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthComboBox.Items.Add($"{i}月");
        monthComboBox.SelectedIndex = item.Month - 1;
        Grid.SetColumn(monthComboBox, 1);
        monthRow.Children.Add(monthComboBox);
        extraPanel.Children.Add(monthRow);

        void UpdateExtraVisibility()
        {
            dayOfWeekRow.IsVisible = periodComboBox.SelectedIndex == (int)PeriodType.Weekly;
            dayOfMonthRow.IsVisible = periodComboBox.SelectedIndex == (int)PeriodType.Monthly || periodComboBox.SelectedIndex == (int)PeriodType.Yearly;
            monthRow.IsVisible = periodComboBox.SelectedIndex == (int)PeriodType.Yearly;
        }

        periodComboBox.SelectionChanged += (s, e) => UpdateExtraVisibility();
        UpdateExtraVisibility();

        contentPanel.Children.Add(extraPanel);

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

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = contentPanel,
            Margin = new Avalonia.Thickness(12, 12, 12, 0)
        };

        var _mainPanel = new Grid();
        _mainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(scrollViewer, 0);
        _mainPanel.Children.Add(scrollViewer);

        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", _mainPanel);

        FluentAvaloniaCompatibilityHelper.AddContentDialogButtonClickHandler(dialog, "PrimaryButtonClick", (s, e) =>
        {
            item.Name = nameTextBox.Text ?? "新周期性倒计时";
            item.PeriodType = (PeriodType)(periodComboBox.SelectedIndex >= 0 ? periodComboBox.SelectedIndex : 1);

            if (hourComboBox.SelectedIndex >= 0)
                item.Hour = hourComboBox.SelectedIndex;
            if (minuteComboBox.SelectedIndex >= 0)
                item.Minute = minuteComboBox.SelectedIndex;
            if (secondComboBox.SelectedIndex >= 0)
                item.Second = secondComboBox.SelectedIndex;

            if (dayOfWeekComboBox.SelectedIndex >= 0)
                item.DayOfWeek = dayOfWeekComboBox.SelectedIndex;

            if (dayOfMonthComboBox.SelectedItem != null && int.TryParse(dayOfMonthComboBox.SelectedItem.ToString()?.Replace("日", ""), out var dayOfMonth))
                item.DayOfMonth = dayOfMonth;

            if (monthComboBox.SelectedIndex >= 0)
                item.Month = monthComboBox.SelectedIndex + 1;

            item.EnableNotification = notifyToggle.IsChecked == true;
            item.NotificationTitle = notifyTitleTextBox.Text ?? "周期性倒计时到达";
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

        _ = FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, TopLevel.GetTopLevel(this));
    }
}