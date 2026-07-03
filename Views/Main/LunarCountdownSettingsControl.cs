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
using ClassIsland.Core.Abstractions.Controls;
using FluentAvalonia.UI.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class LunarCountdownSettingsControl : ComponentBase<LunarCountdownSettings>
{
    private TextBox? _text1TextBox;
    private TextBox? _text3TextBox;
    private TextBox? _text4TextBox;
    private TextBox? _timeFormatTextBox;
    private TextBlock? _timeFormatHint;
    private ComboBox? _timeBaseComboBox;
    private ListBox? _countdownListBox;
    private Button? _addButton;
    private Button? _removeButton;
    private Button? _editButton;

    private TextBlock? _selectionHintTextBlock;
    private System.Timers.Timer? _hintTimer;

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

    public LunarCountdownSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var title = new TextBlock { Text = "农历多倒计时设置", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
        mainPanel.Children.Add(title);

        var desc = new TextBlock { Text = "配置农历倒计时显示选项和倒计时列表", FontSize = 12, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap };
        mainPanel.Children.Add(desc);

        var textGroup = new Expander { Header = new TextBlock { Text = "文案设置", Foreground = Brushes.White }, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        textPanel.Children.Add(new TextBlock { Text = "以下内容在主界面上的顺序为：文案1->倒计时名称->文案3->剩余时间->文案4", Foreground = Brushes.Yellow, FontSize = 11, FontWeight = FontWeight.Bold });
        textPanel.Children.Add(CreateTextRowFullWidth("文案1", "", out _text1TextBox));
        textPanel.Children.Add(CreateText2ButtonRow());
        textPanel.Children.Add(CreateTextRowFullWidth("文案3", "还有", out _text3TextBox));
        textPanel.Children.Add(CreateTextRowFullWidth("文案4", "", out _text4TextBox));

        textGroup.Content = textPanel;
        mainPanel.Children.Add(textGroup);

        var formatGroup = new Expander { Header = new TextBlock { Text = "时间格式", Foreground = Brushes.White }, IsExpanded = true };
        var formatPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _timeFormatTextBox = new TextBox { Watermark = "%d天%h小时%m分钟%s秒" };
        formatPanel.Children.Add(_timeFormatTextBox);

        _timeFormatHint = new TextBlock
        {
            Text = "格式化变量: %D总天数 %H总小时 %M总分钟 %S总秒 %X总毫秒\n%d天 %h小时 %m分钟 %s秒 %x毫秒\n%L剩余百分比 %P已过百分比 %p已过百分比(两位)\n%yy总年 %YY总年(两位) %mo总月 %MO总月(两位)",
            FontSize = 11,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap
        };
        formatPanel.Children.Add(_timeFormatHint);

        formatGroup.Content = formatPanel;
        mainPanel.Children.Add(formatGroup);

        var timeBaseGroup = new Expander { Header = new TextBlock { Text = "时间基准", Foreground = Brushes.White }, IsExpanded = true };
        var timeBasePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var timeBaseRow = new Grid();
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeBaseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var timeBaseLabel = new TextBlock { Text = "时间基准:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(timeBaseLabel, 0);
        timeBaseRow.Children.Add(timeBaseLabel);

        _timeBaseComboBox = new ComboBox();
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("插件偏移后的系统时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("原始系统时间");
        Grid.SetColumn(_timeBaseComboBox, 1);
        timeBaseRow.Children.Add(_timeBaseComboBox);

        timeBasePanel.Children.Add(timeBaseRow);
        timeBaseGroup.Content = timeBasePanel;
        mainPanel.Children.Add(timeBaseGroup);

        var fontGroup = new Expander { Header = new TextBlock { Text = "字体样式", Foreground = Brushes.White }, IsExpanded = false };
        var fontPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        fontPanel.Children.Add(new TextBlock { Text = "文案1样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _text1FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text1FontColorPicker));

        fontPanel.Children.Add(new TextBlock { Text = "倒计时名称样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _nameFontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _nameFontColorPicker));

        fontPanel.Children.Add(new TextBlock { Text = "文案3样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _text3FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text3FontColorPicker));

        fontPanel.Children.Add(new TextBlock { Text = "时间样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _timeFontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _timeFontColorPicker));

        fontPanel.Children.Add(new TextBlock { Text = "文案4样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _text4FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text4FontColorPicker));

        fontGroup.Content = fontPanel;
        mainPanel.Children.Add(fontGroup);

        var listGroup = new Expander { Header = new TextBlock { Text = "农历倒计时列表", Foreground = Brushes.White }, IsExpanded = true };
        var listPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        var lunarHeader = new TextBlock
        {
            Text = "农历日期",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(lunarHeader, 0);
        headerGrid.Children.Add(lunarHeader);

        var solarHeader = new TextBlock
        {
            Text = "对应公历日期",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(solarHeader, 1);
        headerGrid.Children.Add(solarHeader);

        var notifyHeader = new TextBlock
        {
            Text = "启用通知？",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(notifyHeader, 2);
        headerGrid.Children.Add(notifyHeader);
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
            Foreground = Brushes.Orange,
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

    private Grid CreateTextRowFullWidth(string label, string watermark, out TextBox? textBox)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label + ":", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
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

        var lbl = new TextBlock { Text = label, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
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

        var lbl = new TextBlock { Text = label, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
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

        var lbl = new TextBlock { Text = "倒计时名称:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
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

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_text1TextBox != null) _text1TextBox.Text = Settings.Text1;
        if (_text3TextBox != null) _text3TextBox.Text = Settings.Text3;
        if (_text4TextBox != null) _text4TextBox.Text = Settings.Text4;
        if (_timeFormatTextBox != null) _timeFormatTextBox.Text = Settings.TimeFormat;
        if (_timeBaseComboBox != null) _timeBaseComboBox.SelectedIndex = (int)Settings.TimeBaseType;

        if (_text1FontSizeTextBox != null) _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_text1FontColorPicker != null) _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        if (_nameFontSizeTextBox != null) _nameFontSizeTextBox.Text = Settings.NameFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_nameFontColorPicker != null) _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        if (_text3FontSizeTextBox != null) _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_text3FontColorPicker != null) _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        if (_timeFontSizeTextBox != null) _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_timeFontColorPicker != null) _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        if (_text4FontSizeTextBox != null) _text4FontSizeTextBox.Text = Settings.Text4FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_text4FontColorPicker != null) _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);

        UpdateCountdownList();

        AttachEventHandlers();
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
                    Settings.TimeBaseType = (TimeBaseType)_timeBaseComboBox.SelectedIndex;
                }
            };
        }

        AttachFontSizeHandler(_text1FontSizeTextBox, v => Settings.Text1FontSize = v);
        AttachColorPickerHandler(_text1FontColorPicker, v => Settings.Text1FontColor = v);
        AttachFontSizeHandler(_nameFontSizeTextBox, v => Settings.NameFontSize = v);
        AttachColorPickerHandler(_nameFontColorPicker, v => Settings.NameFontColor = v);
        AttachFontSizeHandler(_text3FontSizeTextBox, v => Settings.Text3FontSize = v);
        AttachColorPickerHandler(_text3FontColorPicker, v => Settings.Text3FontColor = v);
        AttachFontSizeHandler(_timeFontSizeTextBox, v => Settings.TimeFontSize = v);
        AttachColorPickerHandler(_timeFontColorPicker, v => Settings.TimeFontColor = v);
        AttachFontSizeHandler(_text4FontSizeTextBox, v => Settings.Text4FontSize = v);
        AttachColorPickerHandler(_text4FontColorPicker, v => Settings.Text4FontColor = v);
    }

    private void AttachTextHandler(TextBox? textBox, Action<string?> setter)
    {
        if (textBox == null) return;
        textBox.LostFocus += (s, e) => setter(textBox.Text);
    }

    private void AttachFontSizeHandler(TextBox? textBox, Action<double> setter)
    {
        if (textBox == null) return;
        textBox.LostFocus += (s, e) => ParseAndSetFontSize(textBox, setter);
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
                    Foreground = Brushes.White,
                    Padding = new Avalonia.Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(lunarTextBlock, 0);
                container.Children.Add(lunarTextBlock);

                var solarTextBlock = new TextBlock
                {
                    Text = targetSolar.ToString("yyyy-MM-dd HH:mm:ss"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.LightGray,
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
        var dialog = new ContentDialog()
        {
            Title = order > 0 ? $"正在编辑第{order}个农历倒计时" : "编辑农历倒计时",
            PrimaryButtonText = "确定",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var infoBar = new InfoBar
        {
            Severity = InfoBarSeverity.Informational,
            Message = "公历可用范围为1901-02-19 ~ 2101-01-28",
            IsOpen = true,
            IsClosable = false
        };
        contentPanel.Children.Add(infoBar);

        var nameLabel = new TextBlock { Text = "名称:", Foreground = Brushes.White };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var lunarGroup = new Expander { Header = new TextBlock { Text = "农历日期", Foreground = Brushes.White }, IsExpanded = true };
        var lunarPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var yearRangePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        yearRangePanel.Children.Add(new TextBlock { Text = "年份范围:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
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

        var yearLabel = new TextBlock { Text = "天干地支年:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(yearLabel, 0);
        yearRow.Children.Add(yearLabel);

        var tianganCombo = new ComboBox { Width = 60 };
        var tiangan = new[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        foreach (var t in tiangan) tianganCombo.Items.Add(t);
        Grid.SetColumn(tianganCombo, 1);
        yearRow.Children.Add(tianganCombo);

        var dizhiLabel = new TextBlock { Text = "地支:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
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

        var monthLabel = new TextBlock { Text = "月:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(monthLabel, 0);
        monthRow.Children.Add(monthLabel);

        var monthCombo = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthCombo.Items.Add(i.ToString());
        monthCombo.SelectedIndex = item.LunarMonth - 1;
        Grid.SetColumn(monthCombo, 1);
        monthRow.Children.Add(monthCombo);

        var leapLabel = new TextBlock { Text = "闰月:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        Grid.SetColumn(leapLabel, 2);
        monthRow.Children.Add(leapLabel);

        var leapToggle = new ToggleSwitch { IsChecked = item.IsLeapMonth, Margin = new Avalonia.Thickness(4, 0, 0, 0) };
        Grid.SetColumn(leapToggle, 3);
        monthRow.Children.Add(leapToggle);

        var dayLabel = new TextBlock { Text = "日:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
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

        var timeLabel = new TextBlock { Text = "时间:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(timeLabel, 0);
        timeRow.Children.Add(timeLabel);

        var lunarTimePicker = new TimePicker
        {
            Width = 260,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            SelectedTime = new TimeSpan(item.Hour, item.Minute, item.Second)
        };
        Grid.SetColumn(lunarTimePicker, 1);
        timeRow.Children.Add(lunarTimePicker);

        lunarPanel.Children.Add(timeRow);

        var solarGroup = new Expander { Header = new TextBlock { Text = "公历对照（可互转）", Foreground = Brushes.White }, IsExpanded = true };
        var solarPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var solarDateLabel = new TextBlock { Text = "公历日期:", Foreground = Brushes.White };
        solarPanel.Children.Add(solarDateLabel);

        var currentSolarDate = item.GetTargetTimestamp() > 0 ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp()) : Plugin.GetCurrentTime();
        var solarDatePicker = new DatePicker { SelectedDate = currentSolarDate.Date };

        var solarTimeRow = new Grid();
        solarTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        solarTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });

        var solarTimeLabel = new TextBlock { Text = "时间:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(solarTimeLabel, 0);
        solarTimeRow.Children.Add(solarTimeLabel);

        var solarTimePicker = new TimePicker
        {
            Width = 260,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            SelectedTime = new TimeSpan(currentSolarDate.Hour, currentSolarDate.Minute, currentSolarDate.Second)
        };
        Grid.SetColumn(solarTimePicker, 1);
        solarTimeRow.Children.Add(solarTimePicker);

        solarPanel.Children.Add(solarDatePicker);
        solarPanel.Children.Add(solarTimeRow);

        var syncButton = new Button { Content = "同步公历→农历", Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        syncButton.Click += (s, e) =>
        {
            if (solarDatePicker.SelectedDate.HasValue)
            {
                var solarDate = solarDatePicker.SelectedDate.Value.Date;
                if (solarTimePicker.SelectedTime.HasValue)
                {
                    solarDate = solarDate.Add(solarTimePicker.SelectedTime.Value);
                }

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
                solarDatePicker.SelectedDate = solarResult.Value.Date;
                solarTimePicker.SelectedTime = new TimeSpan(solarResult.Value.Hour, solarResult.Value.Minute, solarResult.Value.Second);
            }
        };
        solarPanel.Children.Add(syncButton2);


        var notifyToggle = new ToggleSwitch { Content = new TextBlock { Text = "启用通知", Foreground = Brushes.White }, IsChecked = item.EnableNotification };
        contentPanel.Children.Add(notifyToggle);

        lunarGroup.Content = lunarPanel;
        contentPanel.Children.Add(lunarGroup);
        solarGroup.Content = solarPanel;
        contentPanel.Children.Add(solarGroup);

        dialog.PrimaryButtonClick += (s, e) =>
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
        };

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

        dialog.Content = mainPanel;

        dialog.ShowAsync(TopLevel.GetTopLevel(this));
    }
}



