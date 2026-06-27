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

namespace AdvancedTimeIsland.Views.Main;

public class CountdownSettingsControl : ComponentBase<CountdownSettings>
{
    private TextBox? _text1TextBox;
    private TextBox? _text3TextBox;
    private TextBox? _text4TextBox;
    private TextBox? _timeFormatTextBox;
    private TextBlock? _timeFormatHint;
    private ToggleSwitch? _timeCorrectionToggle;
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

    private DatePicker? _startDatePicker;
    private ComboBox? _startHourComboBox;
    private ComboBox? _startMinuteComboBox;
    private ComboBox? _startSecondComboBox;

    public CountdownSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var title = new TextBlock { Text = "多倒计时设置", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
        mainPanel.Children.Add(title);

        var desc = new TextBlock { Text = "配置倒计时显示选项和倒计时列表", FontSize = 12, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap };
        mainPanel.Children.Add(desc);

        var textGroup = new Expander { Header = new TextBlock { Text = "文案设置", Foreground = Brushes.White }, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        textPanel.Children.Add(new TextBlock { Text = "以下内容在主界面上显示的顺序为：文案1->倒计时名称->文案3->剩余时间->文案4", Foreground = Brushes.Yellow, FontSize = 11, FontWeight = FontWeight.Bold });
        textPanel.Children.Add(CreateTextRow("文案1", "距离", out _text1TextBox));
        textPanel.Children.Add(CreateText2ButtonRow());
        textPanel.Children.Add(CreateTextRow("文案3", "还有", out _text3TextBox));
        textPanel.Children.Add(CreateTextRow("文案4", "", out _text4TextBox));

        textGroup.Content = textPanel;
        mainPanel.Children.Add(textGroup);

        var formatGroup = new Expander { Header = new TextBlock { Text = "时间格式", Foreground = Brushes.White }, IsExpanded = true };
        var formatPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var formatRow = new Grid();
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var formatLabel = new TextBlock { Text = "时间格式:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(formatLabel, 0);
        formatRow.Children.Add(formatLabel);

        _timeFormatTextBox = new TextBox { Watermark = "%d天%h小时%m分钟%s秒" };
        Grid.SetColumn(_timeFormatTextBox, 1);
        formatRow.Children.Add(_timeFormatTextBox);

        formatPanel.Children.Add(formatRow);

        _timeFormatHint = new TextBlock
        {
            Text = "格式化变量: %D总天数 %H总小时 %M总分钟 %S总秒 %X总毫秒\n%d天 %h小时 %m分钟 %s秒 %x毫秒\n%L剩余百分比 %P已过百分比 %p已过百分比(两位)\n%yy总年 %YY总年(两位) %mo总月 %MO总月(两位)",
            FontSize = 11,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap
        };
        formatPanel.Children.Add(_timeFormatHint);

        _timeCorrectionToggle = new ToggleSwitch
        {
            Content = new TextBlock { Text = "差一矫正（当精度不足时最小单位加一）", Foreground = Brushes.White },
            IsChecked = true
        };
        _timeCorrectionToggle.IsCheckedChanged += (s, e) =>
        {
            Settings.EnableTimeCorrection = _timeCorrectionToggle.IsChecked == true;
        };
        formatPanel.Children.Add(_timeCorrectionToggle);

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
        _timeBaseComboBox.Items.Add("跟随插件全局设置");
        _timeBaseComboBox.Items.Add("插件偏移后时间");
        _timeBaseComboBox.Items.Add("系统时间");
        Grid.SetColumn(_timeBaseComboBox, 1);
        timeBaseRow.Children.Add(_timeBaseComboBox);

        timeBasePanel.Children.Add(timeBaseRow);
        timeBaseGroup.Content = timeBasePanel;
        mainPanel.Children.Add(timeBaseGroup);

        var startTimeGroup = new Expander { Header = new TextBlock { Text = "开始时间", Foreground = Brushes.White }, IsExpanded = true };
        var startTimePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var startDateRow = new Grid();
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var startDateLabel = new TextBlock { Text = "日期:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(startDateLabel, 0);
        startDateRow.Children.Add(startDateLabel);

        _startDatePicker = new DatePicker();
        Grid.SetColumn(_startDatePicker, 1);
        startDateRow.Children.Add(_startDatePicker);

        startTimePanel.Children.Add(startDateRow);

        var startTimeRow = new Grid();
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        var startTimeLabel = new TextBlock { Text = "时间:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(startTimeLabel, 0);
        startTimeRow.Children.Add(startTimeLabel);

        _startHourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) _startHourComboBox.Items.Add(i.ToString("D2"));
        Grid.SetColumn(_startHourComboBox, 1);
        startTimeRow.Children.Add(_startHourComboBox);

        var hourSeparator = new TextBlock { Text = ":", FontSize = 16, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(hourSeparator, 2);
        startTimeRow.Children.Add(hourSeparator);

        _startMinuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) _startMinuteComboBox.Items.Add(i.ToString("D2"));
        Grid.SetColumn(_startMinuteComboBox, 3);
        startTimeRow.Children.Add(_startMinuteComboBox);

        var minuteSeparator = new TextBlock { Text = ":", FontSize = 16, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(minuteSeparator, 4);
        startTimeRow.Children.Add(minuteSeparator);

        _startSecondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) _startSecondComboBox.Items.Add(i.ToString("D2"));
        Grid.SetColumn(_startSecondComboBox, 5);
        startTimeRow.Children.Add(_startSecondComboBox);

        startTimePanel.Children.Add(startTimeRow);

        startTimeGroup.Content = startTimePanel;
        mainPanel.Children.Add(startTimeGroup);

        var fontGroup = new Expander { Header = new TextBlock { Text = "字体样式", Foreground = Brushes.White }, IsExpanded = false };
        var fontPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        fontPanel.Children.Add(new TextBlock { Text = "文案1样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _text1FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text1FontColorPicker));

        fontPanel.Children.Add(new TextBlock { Text = "文案2样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _text2FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text2FontColorPicker));

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

        var listGroup = new Expander { Header = new TextBlock { Text = "倒计时列表", Foreground = Brushes.White }, IsExpanded = true };
        var listPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        var nameHeader = new TextBlock
        {
            Text = "倒计时目标时间",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameHeader, 0);
        headerGrid.Children.Add(nameHeader);

        var notifyHeader = new TextBlock
        {
            Text = "启用通知？",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(notifyHeader, 1);
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
            Text = "请选择一个倒计时",
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

    private Grid CreateTextRow(string label, string watermark, out TextBox? textBox)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
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

    private Grid CreateNumberRow(string label, string watermark, out TextBox? textBox)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = label, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(lbl, 0);
        row.Children.Add(lbl);

        textBox = new TextBox { Width = 80, Watermark = watermark, HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);

        return row;
    }

    private Grid CreateText2ButtonRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock { Text = "倒计时名称", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
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

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_text1TextBox != null) _text1TextBox.Text = Settings.Text1;
        if (_text3TextBox != null) _text3TextBox.Text = Settings.Text3;
        if (_text4TextBox != null) _text4TextBox.Text = Settings.Text4;
        if (_timeFormatTextBox != null) _timeFormatTextBox.Text = Settings.TimeFormat;
        if (_timeCorrectionToggle != null) _timeCorrectionToggle.IsChecked = Settings.EnableTimeCorrection;

        if (_timeBaseComboBox != null) _timeBaseComboBox.SelectedIndex = (int)Settings.TimeBaseType;

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

        if (_startDatePicker != null && _startHourComboBox != null && _startMinuteComboBox != null && _startSecondComboBox != null)
        {
            var startTime = UnixTimeHelper.FromUnixTimestamp(Settings.StartTime);
            _startDatePicker.SelectedDate = startTime.Date;
            _startHourComboBox.SelectedIndex = startTime.Hour;
            _startMinuteComboBox.SelectedIndex = startTime.Minute;
            _startSecondComboBox.SelectedIndex = startTime.Second;
        }

        UpdateCountdownList();

        AttachEventHandlers();
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

        AttachStartTimeHandlers();
    }

    private void AttachStartTimeHandlers()
    {
        void UpdateStartTime()
        {
            if (_startDatePicker?.SelectedDate.HasValue == true && 
                _startHourComboBox?.SelectedIndex >= 0 && 
                _startMinuteComboBox?.SelectedIndex >= 0 && 
                _startSecondComboBox?.SelectedIndex >= 0)
            {
                var startTime = _startDatePicker.SelectedDate.Value.Date
                    .Add(new TimeSpan(_startHourComboBox.SelectedIndex, 
                        _startMinuteComboBox.SelectedIndex, 
                        _startSecondComboBox.SelectedIndex));
                Settings.StartTime = UnixTimeHelper.ToUnixTimestamp(startTime);
            }
        }

        if (_startDatePicker != null)
        {
            _startDatePicker.SelectedDateChanged += (s, e) => UpdateStartTime();
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

    private void AttachNumberHandler(TextBox? textBox, Action<int> setter)
    {
        if (textBox == null) return;
        textBox.LostFocus += (s, e) => ParseAndSetNumber(textBox, setter);
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
                    Foreground = Brushes.White,
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

    private void ShowEditDialog(CountdownItem item, int order = 0)
    {
        var dialog = new Window
        {
            Title = order > 0 ? $"正在编写第{order}个倒计时" : "编辑倒计时",
            Width = 400,
            Height = 450,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var nameLabel = new TextBlock { Text = "名称:", Foreground = Brushes.White };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var targetLabel = new TextBlock { Text = "目标时间:", Foreground = Brushes.White };
        var targetTime = UnixTimeHelper.FromUnixTimestamp(item.TargetTimestamp);
        var datePicker = new DatePicker { SelectedDate = targetTime.Date };

        var timePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Avalonia.Thickness(0, 4, 0, 0) };
        var hourComboBox = new ComboBox { Width = 80, CornerRadius = new CornerRadius(4) };
        for (int i = 0; i < 24; i++) hourComboBox.Items.Add(i.ToString("D2"));
        hourComboBox.SelectedIndex = targetTime.Hour;
        timePanel.Children.Add(hourComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
        var minuteComboBox = new ComboBox { Width = 80, CornerRadius = new CornerRadius(4) };
        for (int i = 0; i < 60; i++) minuteComboBox.Items.Add(i.ToString("D2"));
        minuteComboBox.SelectedIndex = targetTime.Minute;
        timePanel.Children.Add(minuteComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
        var secondComboBox = new ComboBox { Width = 80, CornerRadius = new CornerRadius(4) };
        for (int i = 0; i < 60; i++) secondComboBox.Items.Add(i.ToString("D2"));
        secondComboBox.SelectedIndex = targetTime.Second;
        timePanel.Children.Add(secondComboBox);

        contentPanel.Children.Add(targetLabel);
        contentPanel.Children.Add(datePicker);
        contentPanel.Children.Add(timePanel);

        var notifyToggle = new ToggleSwitch { Content = new TextBlock { Text = "启用通知", Foreground = Brushes.White }, IsChecked = item.EnableNotification };
        contentPanel.Children.Add(notifyToggle);

        var notifyTitleLabel = new TextBlock { Text = "通知标题:", Foreground = Brushes.White };
        var notifyTitleTextBox = new TextBox { Text = item.NotificationTitle };
        contentPanel.Children.Add(notifyTitleLabel);
        contentPanel.Children.Add(notifyTitleTextBox);

        var notifyContentLabel = new TextBlock { Text = "通知内容:", Foreground = Brushes.White };
        var notifyContentTextBox = new TextBox { Text = item.NotificationContent };
        contentPanel.Children.Add(notifyContentLabel);
        contentPanel.Children.Add(notifyContentTextBox);

        var maskDurationLabel = new TextBlock { Text = "通知标题时长(秒):", Foreground = Brushes.White };
        var maskDurationTextBox = new TextBox { Text = item.NotificationMaskDurationSeconds.ToString() };
        contentPanel.Children.Add(maskDurationLabel);
        contentPanel.Children.Add(maskDurationTextBox);

        var overlayDurationLabel = new TextBlock { Text = "通知内容时长(秒):", Foreground = Brushes.White };
        var overlayDurationTextBox = new TextBox { Text = item.NotificationOverlayDurationSeconds.ToString() };
        contentPanel.Children.Add(overlayDurationLabel);
        contentPanel.Children.Add(overlayDurationTextBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var okButton = new Button { Content = "确定", Width = 80 };
        var cancelButton = new Button { Content = "取消", Width = 80 };

        okButton.Click += (s, e) =>
        {
            item.Name = nameTextBox.Text ?? "新倒计时";

            if (datePicker.SelectedDate.HasValue)
            {
                var hour = hourComboBox.SelectedIndex >= 0 ? hourComboBox.SelectedIndex : 0;
                var minute = minuteComboBox.SelectedIndex >= 0 ? minuteComboBox.SelectedIndex : 0;
                var second = secondComboBox.SelectedIndex >= 0 ? secondComboBox.SelectedIndex : 0;
                var target = datePicker.SelectedDate.Value.Date
                    .Add(new TimeSpan(hour, minute, second));
                item.TargetTimestamp = UnixTimeHelper.ToUnixTimestamp(target);
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
            dialog.Close();
        };

        cancelButton.Click += (s, e) => dialog.Close();

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = contentPanel,
            Margin = new Avalonia.Thickness(12, 12, 12, 0)
        };

        var mainPanel = new Grid();
        mainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid.SetRow(scrollViewer, 0);
        mainPanel.Children.Add(scrollViewer);

        buttonPanel.Margin = new Avalonia.Thickness(12, 8, 12, 12);
        Grid.SetRow(buttonPanel, 1);
        mainPanel.Children.Add(buttonPanel);

        dialog.Content = mainPanel;

        var ownerWindow = TopLevel.GetTopLevel(this) as Window;
        if (ownerWindow != null)
        {
            dialog.ShowDialog(ownerWindow);
        }
        else
        {
            dialog.Show();
        }
    }
}