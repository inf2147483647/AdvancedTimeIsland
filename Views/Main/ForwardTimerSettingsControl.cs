using System;
using System.Globalization;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
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
    private DatePicker? _startDatePicker;
    private TimePicker? _startTimePicker;
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

    public ForwardTimerSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8, Margin = new Thickness(12) };

        var textGroup = new Expander { Header = new TextBlock { Text = "文案设置", Foreground = Brushes.White }, IsExpanded = true };
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        textPanel.Children.Add(new TextBlock { Text = "以下内容在主界面上显示的顺序为：文案1->正向计时器名称->文案3->已过时间->文案4", Foreground = Brushes.Yellow, FontSize = 11, FontWeight = FontWeight.Bold });
        textPanel.Children.Add(CreateTextRow("文案1", "", out _text1TextBox));
        textPanel.Children.Add(CreateTextRow("正向计时器名称", "", out _nameTextBox));
        textPanel.Children.Add(CreateTextRow("文案3", "已过", out _text3TextBox));
        textPanel.Children.Add(CreateTextRow("文案4", "", out _text4TextBox));

        textGroup.Content = textPanel;
        mainPanel.Children.Add(textGroup);

        var formatGroup = new Expander { Header = new TextBlock { Text = "时间格式", Foreground = Brushes.White }, IsExpanded = false };
        var formatPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        formatPanel.Children.Add(new TextBlock { Text = "格式化文本:", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        _timeFormatTextBox = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, Text = "%d天%h小时%m分钟%s秒" };
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

        var timeBaseGroup = new Expander { Header = new TextBlock { Text = "时间基准", Foreground = Brushes.White }, IsExpanded = false };
        var timeBasePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        timeBasePanel.Children.Add(new TextBlock { Text = "时间来源:", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        _timeBaseComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left };
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("插件偏移后的系统时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("原始系统时间");
        timeBasePanel.Children.Add(_timeBaseComboBox);

        timeBaseGroup.Content = timeBasePanel;
        mainPanel.Children.Add(timeBaseGroup);

        var startTimeGroup = new Expander { Header = new TextBlock { Text = "开始时间", Foreground = Brushes.White }, IsExpanded = false };
        var startTimePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var startDateRow = new Grid();
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startDateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var startDateLabel = new TextBlock { Text = "日期:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(startDateLabel, 0);
        startDateRow.Children.Add(startDateLabel);

        _startDatePicker = new DatePicker { HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(_startDatePicker, 1);
        startDateRow.Children.Add(_startDatePicker);

        startTimePanel.Children.Add(startDateRow);

        var startTimeRow = new Grid();
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });

        var startTimeLabel = new TextBlock { Text = "时间:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(startTimeLabel, 0);
        startTimeRow.Children.Add(startTimeLabel);

        _startTimePicker = new TimePicker
        {
            Width = 260,
            ClockIdentifier = "24HourClock",
            UseSeconds = true
        };
        Grid.SetColumn(_startTimePicker, 1);
        startTimeRow.Children.Add(_startTimePicker);

        startTimePanel.Children.Add(startTimeRow);

        startTimeGroup.Content = startTimePanel;
        mainPanel.Children.Add(startTimeGroup);

        var fontGroup = new Expander { Header = new TextBlock { Text = "字体样式", Foreground = Brushes.White }, IsExpanded = false };
        var fontPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        fontPanel.Children.Add(new TextBlock { Text = "文案1样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
        fontPanel.Children.Add(CreateFontRow("大小:", out _text1FontSizeTextBox));
        fontPanel.Children.Add(CreateColorRow("颜色:", out _text1FontColorPicker));

        fontPanel.Children.Add(new TextBlock { Text = "正向计时器名称样式", FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.LightBlue });
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

        var lbl = new TextBlock { Text = label, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
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

        colorPicker = new ColorPicker { HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        return row;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_text1TextBox != null) _text1TextBox.Text = Settings.Text1;
        if (_nameTextBox != null) _nameTextBox.Text = Settings.Name;
        if (_text3TextBox != null) _text3TextBox.Text = Settings.Text3;
        if (_text4TextBox != null) _text4TextBox.Text = Settings.Text4;
        if (_timeFormatTextBox != null) _timeFormatTextBox.Text = Settings.TimeFormat;
        if (_timeBaseComboBox != null) _timeBaseComboBox.SelectedIndex = (int)Settings.TimeBaseType;

        var startTime = DateTimeOffset.FromUnixTimeSeconds(Settings.StartTime).LocalDateTime;
        if (_startDatePicker != null) _startDatePicker.SelectedDate = startTime.Date;
        if (_startTimePicker != null) _startTimePicker.SelectedTime = new TimeSpan(startTime.Hour, startTime.Minute, startTime.Second);

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
    }

    private void AttachTextHandler(TextBox? textBox, Action<string?> handler)
    {
        if (textBox == null) return;

        textBox.LostFocus += (s, e) =>
        {
            handler(textBox.Text);
        };
    }

    private void AttachDateTimeHandlers()
    {
        void UpdateStartTime()
        {
            if (_startDatePicker?.SelectedDate.HasValue == true && 
                _startTimePicker?.SelectedTime.HasValue == true)
            {
                var startTime = _startDatePicker.SelectedDate.Value.Date
                    .Add(_startTimePicker.SelectedTime.Value);
                Settings.StartTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
            }
        }

        if (_startDatePicker != null)
            _startDatePicker.SelectedDateChanged += (s, e) => UpdateStartTime();

        if (_startTimePicker != null)
            _startTimePicker.SelectedTimeChanged += (s, e) => UpdateStartTime();
    }

    private void AttachFontHandlers(TextBox? fontSizeTextBox, ColorPicker? colorPicker, Action<double, string> handler)
    {
        if (fontSizeTextBox != null)
        {
            fontSizeTextBox.LostFocus += (s, e) =>
            {
                if (double.TryParse(fontSizeTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var fontSize))
                {
                    var color = colorPicker?.Color.ToString() ?? "#FFFFFF";
                    handler(fontSize, color);
                }
            };
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
            return Colors.White;
        }
    }
}


