using System;
using System.Collections.Generic;
using System.Globalization;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 时间格式转换页面
/// 支持北京时间/Unix/农历/区时/地方时互转
/// </summary>
public class TimeConverterPage : UserControl
{
    // 北京时间转换模块控件
    private TextBox? _beijingDateTextBox;
    private TextBox? _beijingTimeTextBox;
    private TextBlock? _beijingResultTextBlock;

    // Unix时间戳转换模块控件
    private TextBox? _unixInputTextBox;
    private TextBlock? _unixResultTextBlock;

    // 农历转换模块控件
    private ComboBox? _lunarYearRangeComboBox;
    private ComboBox? _lunarTianganComboBox;
    private ComboBox? _lunarDizhiComboBox;
    private ComboBox? _lunarMonthComboBox;
    private ComboBox? _lunarDayComboBox;
    private TextBox? _lunarTimeTextBox;
    private TextBlock? _lunarResultTextBlock;

    // 区时转换模块控件
    private TextBox? _zoneDateTextBox;
    private TextBox? _zoneTimeTextBox;
    private ComboBox? _zoneComboBox;
    private TextBlock? _zoneResultTextBlock;

    // 地方时转换模块控件
    private TextBox? _localDateTextBox;
    private TextBox? _localTimeTextBox;
    private TextBox? _localLongitudeTextBox;
    private TextBlock? _localResultTextBlock;

    // 天干列表
    private readonly string[] _tiangan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };

    // 地支列表
    private readonly string[] _dizhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    // 时区列表
    private readonly Dictionary<string, int> _timeZones = new()
    {
        { "西十二区", -12 },
        { "西十一区", -11 },
        { "西十区", -10 },
        { "西九区", -9 },
        { "西八区", -8 },
        { "西七区", -7 },
        { "西六区", -6 },
        { "西五区", -5 },
        { "西四区", -4 },
        { "西三区", -3 },
        { "西二区", -2 },
        { "西一区", -1 },
        { "中时区", 0 },
        { "东一区", 1 },
        { "东二区", 2 },
        { "东三区", 3 },
        { "东四区", 4 },
        { "东五区", 5 },
        { "东六区", 6 },
        { "东七区", 7 },
        { "东八区", 8 },
        { "东九区", 9 },
        { "东十区", 10 },
        { "东十一区", 11 },
        { "东十二区", 12 }
    };

    public TimeConverterPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Avalonia.Thickness(16),
            Spacing = 24
        };

        // 第一行：清空按钮
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var clearButton = new Button
        {
            Content = "清空",
            Padding = new Avalonia.Thickness(16, 8),
            Background = Brushes.Gray,
            Foreground = Brushes.White
        };
        clearButton.Click += OnClearAllClick;

        headerPanel.Children.Add(clearButton);
        mainPanel.Children.Add(headerPanel);

        // 1. 北京时间转换模块
        mainPanel.Children.Add(CreateBeijingModule());

        // 2. Unix时间戳转换模块
        mainPanel.Children.Add(CreateUnixModule());

        // 3. 农历转换模块
        mainPanel.Children.Add(CreateLunarModule());

        // 4. 区时转换模块
        mainPanel.Children.Add(CreateZoneModule());

        // 5. 地方时转换模块
        mainPanel.Children.Add(CreateLocalModule());

        // 注意事项
        mainPanel.Children.Add(CreateNotesPanel());

        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    /// <summary>
    /// 创建北京时间转换模块
    /// </summary>
    private Border CreateBeijingModule()
    {
        var module = CreateModulePanel("北京时间转换模块", "北京时间");
        var content = GetModuleContent(module);

        // 日期输入
        var datePanel = CreateInputRow("日期输入（格式：年/月/日/星期）:", 200);
        _beijingDateTextBox = (TextBox)datePanel.Children[1];
        _beijingDateTextBox.Watermark = "点击选择日期";
        content.Children.Add(datePanel);

        // 时间选择
        var timePanel = CreateInputRow("时间:", 100);
        _beijingTimeTextBox = (TextBox)timePanel.Children[1];
        _beijingTimeTextBox.Watermark = "HH:mm:ss";
        content.Children.Add(timePanel);

        // 选取当前时间按钮
        var currentButton = new Button
        {
            Content = "选取当前时间",
            Padding = new Avalonia.Thickness(12, 6),
            Background = Brushes.DarkGreen,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        currentButton.Click += OnBeijingCurrentTimeClick;
        content.Children.Add(currentButton);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为 Unix 时间戳", OnBeijingToUnix);
        AddConvertButton(buttonPanel, "转为农历", OnBeijingToLunar);
        AddConvertButton(buttonPanel, "转为区时", OnBeijingToZone);
        AddConvertButton(buttonPanel, "转为地方时", OnBeijingToLocal);

        content.Children.Add(buttonPanel);

        // 结果显示
        _beijingResultTextBlock = CreateResultTextBlock();
        content.Children.Add(_beijingResultTextBlock);

        return module;
    }

    /// <summary>
    /// 创建Unix时间戳转换模块
    /// </summary>
    private Border CreateUnixModule()
    {
        var module = CreateModulePanel("Unix 时间戳转换模块", "Unix 时间戳");
        var content = GetModuleContent(module);

        // 输入框（整数）
        var inputPanel = CreateInputRow("输入（整数）:", 200);
        _unixInputTextBox = (TextBox)inputPanel.Children[1];
        _unixInputTextBox.Watermark = "输入Unix时间戳";
        content.Children.Add(inputPanel);

        // 复制按钮
        var copyButton = new Button
        {
            Content = "复制",
            Padding = new Avalonia.Thickness(12, 6),
            Background = Brushes.Gray,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        copyButton.Click += OnUnixCopyClick;
        content.Children.Add(copyButton);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为北京时间", OnUnixToBeijing);
        AddConvertButton(buttonPanel, "转为农历", OnUnixToLunar);
        AddConvertButton(buttonPanel, "转为区时", OnUnixToZone);
        AddConvertButton(buttonPanel, "转为地方时", OnUnixToLocal);

        content.Children.Add(buttonPanel);

        // 结果显示
        _unixResultTextBlock = CreateResultTextBlock();
        content.Children.Add(_unixResultTextBlock);

        return module;
    }

    /// <summary>
    /// 创建农历转换模块
    /// </summary>
    private Border CreateLunarModule()
    {
        var module = CreateModulePanel("农历转换模块", "农历");
        var content = GetModuleContent(module);

        // 年份范围
        var yearRangePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        yearRangePanel.Children.Add(new TextBlock
        {
            Text = "年份范围:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _lunarYearRangeComboBox = new ComboBox { Width = 180 };
        _lunarYearRangeComboBox.Items.Add("1924-1983");
        _lunarYearRangeComboBox.Items.Add("1984-2043");
        _lunarYearRangeComboBox.Items.Add("2044-2103");
        _lunarYearRangeComboBox.SelectedIndex = 1; // 默认1984-2043
        yearRangePanel.Children.Add(_lunarYearRangeComboBox);

        content.Children.Add(yearRangePanel);

        // 天干地支
        var tgdzPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        _lunarTianganComboBox = new ComboBox { Width = 60 };
        foreach (var tg in _tiangan) _lunarTianganComboBox.Items.Add(tg);
        tgdzPanel.Children.Add(_lunarTianganComboBox);

        _lunarDizhiComboBox = new ComboBox { Width = 60 };
        foreach (var dz in _dizhi) _lunarDizhiComboBox.Items.Add(dz);
        tgdzPanel.Children.Add(_lunarDizhiComboBox);

        tgdzPanel.Children.Add(new TextBlock
        {
            Text = "年",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        content.Children.Add(tgdzPanel);

        // 月份
        var monthPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        monthPanel.Children.Add(new TextBlock
        {
            Text = "月份:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _lunarMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++)
        {
            _lunarMonthComboBox.Items.Add($"{(i < 10 ? "0" : "")}{i}");
            _lunarMonthComboBox.Items.Add($"闰{(i < 10 ? "0" : "")}{i}"); // 闰月
        }
        monthPanel.Children.Add(_lunarMonthComboBox);

        content.Children.Add(monthPanel);

        // 日期
        var dayPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        dayPanel.Children.Add(new TextBlock
        {
            Text = "日期:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _lunarDayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 30; i++)
        {
            _lunarDayComboBox.Items.Add($"{(i < 10 ? "0" : "")}{i}");
        }
        dayPanel.Children.Add(_lunarDayComboBox);

        content.Children.Add(dayPanel);

        // 时间
        var lunarTimePanel = CreateInputRow("时间:", 100);
        _lunarTimeTextBox = (TextBox)lunarTimePanel.Children[1];
        _lunarTimeTextBox.Watermark = "HH:mm:ss";
        content.Children.Add(lunarTimePanel);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为北京时间", OnLunarToBeijing);
        AddConvertButton(buttonPanel, "转为 Unix 时间戳", OnLunarToUnix);
        AddConvertButton(buttonPanel, "转为区时", OnLunarToZone);
        AddConvertButton(buttonPanel, "转为地方时", OnLunarToLocal);

        content.Children.Add(buttonPanel);

        // 结果显示
        _lunarResultTextBlock = CreateResultTextBlock();
        content.Children.Add(_lunarResultTextBlock);

        return module;
    }

    /// <summary>
    /// 创建区时转换模块
    /// </summary>
    private Border CreateZoneModule()
    {
        var module = CreateModulePanel("区时转换模块", "区时");
        var content = GetModuleContent(module);

        // 日期输入
        var datePanel = CreateInputRow("日期输入（格式：年/月/日/星期）:", 200);
        _zoneDateTextBox = (TextBox)datePanel.Children[1];
        _zoneDateTextBox.Watermark = "点击选择日期";
        content.Children.Add(datePanel);

        // 时间选择
        var timePanel = CreateInputRow("时间:", 100);
        _zoneTimeTextBox = (TextBox)timePanel.Children[1];
        _zoneTimeTextBox.Watermark = "HH:mm:ss";
        content.Children.Add(timePanel);

        // 时区选择
        var zonePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        zonePanel.Children.Add(new TextBlock
        {
            Text = "时区:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _zoneComboBox = new ComboBox { Width = 100 };
        _zoneComboBox.Items.Add("中时区");
        foreach (var zone in _timeZones.Keys)
        {
            if (zone != "中时区")
                _zoneComboBox.Items.Add(zone);
        }
        _zoneComboBox.SelectedIndex = 0;
        zonePanel.Children.Add(_zoneComboBox);

        content.Children.Add(zonePanel);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为北京时间", OnZoneToBeijing);
        AddConvertButton(buttonPanel, "转为 Unix 时间戳", OnZoneToUnix);
        AddConvertButton(buttonPanel, "转为农历", OnZoneToLunar);
        AddConvertButton(buttonPanel, "转为地方时", OnZoneToLocal);

        content.Children.Add(buttonPanel);

        // 结果显示
        _zoneResultTextBlock = CreateResultTextBlock();
        content.Children.Add(_zoneResultTextBlock);

        return module;
    }

    /// <summary>
    /// 创建地方时转换模块
    /// </summary>
    private Border CreateLocalModule()
    {
        var module = CreateModulePanel("地方时转换模块", "地方时");
        var content = GetModuleContent(module);

        // 日期输入
        var datePanel = CreateInputRow("日期输入（格式：年/月/日/星期）:", 200);
        _localDateTextBox = (TextBox)datePanel.Children[1];
        _localDateTextBox.Watermark = "点击选择日期";
        content.Children.Add(datePanel);

        // 时间选择
        var timePanel = CreateInputRow("时间:", 100);
        _localTimeTextBox = (TextBox)timePanel.Children[1];
        _localTimeTextBox.Watermark = "HH:mm:ss";
        content.Children.Add(timePanel);

        // 经度输入
        var lonPanel = CreateInputRow("经度（单位：度，正数东经，负数西经）:", 100);
        _localLongitudeTextBox = (TextBox)lonPanel.Children[1];
        _localLongitudeTextBox.Watermark = "如：116.4";
        content.Children.Add(lonPanel);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为北京时间", OnLocalToBeijing);
        AddConvertButton(buttonPanel, "转为 Unix 时间戳", OnLocalToUnix);
        AddConvertButton(buttonPanel, "转为农历", OnLocalToLunar);
        AddConvertButton(buttonPanel, "转为区时", OnLocalToZone);

        content.Children.Add(buttonPanel);

        // 结果显示
        _localResultTextBlock = CreateResultTextBlock();
        content.Children.Add(_localResultTextBlock);

        return module;
    }

    #region 创建UI组件的辅助方法

    private Border CreateModulePanel(string title, string tag)
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        // 模块标题
        var titlePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        titlePanel.Children.Add(new TextBlock
        {
            Text = tag,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.DodgerBlue
        });

        titlePanel.Children.Add(new TextBlock
        {
            Text = $"| {title}",
            FontSize = 14,
            Foreground = Brushes.LightGray
        });

        content.Children.Add(titlePanel);
        panel.Child = content;

        return panel;
    }

    private static StackPanel GetModuleContent(Border module)
    {
        return (StackPanel)module.Child!;
    }

    private StackPanel CreateInputRow(string label, double inputWidth)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        var textBox = new TextBox
        {
            Width = inputWidth,
            Watermark = ""
        };
        panel.Children.Add(textBox);

        return panel;
    }

    private Button AddConvertButton(StackPanel panel, string text, EventHandler<RoutedEventArgs> handler)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Avalonia.Thickness(12, 6),
            Background = Brushes.DodgerBlue,
            Foreground = Brushes.White
        };
        button.Click += handler;
        panel.Children.Add(button);
        return button;
    }

    private TextBlock CreateResultTextBlock()
    {
        return new TextBlock
        {
            Text = "",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.LightGreen,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };
    }

    private StackPanel CreateNotesPanel()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Margin = new Avalonia.Thickness(0, 16, 0, 0)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "注意事项：",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.Orange
        });

        var notes = new[]
        {
            "• 转为区时/地方时前，需先选好对应的时区/经度，然后再转换",
            "• 试试连续点顶部通用区域的图标",
            "• 经度处填正数为东经，负数为西经，取值范围为 (-180, 180]"
        };

        foreach (var note in notes)
        {
            panel.Children.Add(new TextBlock
            {
                Text = note,
                FontSize = 12,
                Foreground = Brushes.LightGray,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
        }

        return panel;
    }

    #endregion

    #region 事件处理

    private void OnClearAllClick(object? sender, RoutedEventArgs e)
    {
        // 清除所有输入框
        if (_beijingDateTextBox != null) _beijingDateTextBox.Text = "";
        if (_beijingTimeTextBox != null) _beijingTimeTextBox.Text = "";
        if (_beijingResultTextBlock != null) _beijingResultTextBlock.Text = "";

        if (_unixInputTextBox != null) _unixInputTextBox.Text = "";
        if (_unixResultTextBlock != null) _unixResultTextBlock.Text = "";

        if (_lunarTianganComboBox != null) _lunarTianganComboBox.SelectedIndex = -1;
        if (_lunarDizhiComboBox != null) _lunarDizhiComboBox.SelectedIndex = -1;
        if (_lunarMonthComboBox != null) _lunarMonthComboBox.SelectedIndex = -1;
        if (_lunarDayComboBox != null) _lunarDayComboBox.SelectedIndex = -1;
        if (_lunarTimeTextBox != null) _lunarTimeTextBox.Text = "";
        if (_lunarResultTextBlock != null) _lunarResultTextBlock.Text = "";

        if (_zoneDateTextBox != null) _zoneDateTextBox.Text = "";
        if (_zoneTimeTextBox != null) _zoneTimeTextBox.Text = "";
        if (_zoneComboBox != null) _zoneComboBox.SelectedIndex = -1;
        if (_zoneResultTextBlock != null) _zoneResultTextBlock.Text = "";

        if (_localDateTextBox != null) _localDateTextBox.Text = "";
        if (_localTimeTextBox != null) _localTimeTextBox.Text = "";
        if (_localLongitudeTextBox != null) _localLongitudeTextBox.Text = "";
        if (_localResultTextBlock != null) _localResultTextBlock.Text = "";
    }

    private void OnBeijingCurrentTimeClick(object? sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(now.DayOfWeek);
        _beijingDateTextBox!.Text = $"{now.Year}/{now.Month}/{now.Day}/{dayOfWeek}";
        _beijingTimeTextBox!.Text = now.ToString("HH:mm:ss");
    }

    private void OnBeijingToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            _beijingResultTextBlock!.Text = "请输入有效的日期和时间";
            return;
        }
        var timestamp = UnixTimeHelper.ToUnixTimestamp(dt);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnBeijingToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            _beijingResultTextBlock!.Text = "请输入有效的日期和时间";
            return;
        }
        // TODO: 实现农历转换（需要lunar-csharp库）
        _beijingResultTextBlock!.Text = $"农历转换: {dt:yyyy年MM月dd日} (需安装lunar-csharp)";
    }

    private void OnBeijingToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            _beijingResultTextBlock!.Text = "请输入有效的日期和时间";
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            _beijingResultTextBlock!.Text = "请先选择时区";
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "中时区", 0);
        var zoneTime = dt.AddHours(-offset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        _zoneDateTextBox!.Text = $"{zoneTime.Year}/{zoneTime.Month}/{zoneTime.Day}/{dayOfWeek}";
        _zoneTimeTextBox!.Text = zoneTime.ToString("HH:mm:ss");
    }

    private void OnBeijingToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            _beijingResultTextBlock!.Text = "请输入有效的日期和时间";
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            _beijingResultTextBlock!.Text = "请输入有效的经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var localTime = dt.AddMinutes(-offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        _localDateTextBox!.Text = $"{localTime.Year}/{localTime.Month}/{localTime.Day}/{dayOfWeek}";
        _localTimeTextBox!.Text = localTime.ToString("HH:mm:ss");
    }

    private void OnUnixCopyClick(object? sender, RoutedEventArgs e)
    {
        if (_unixInputTextBox?.Text != null)
        {
            try
            {
                // 复制到剪贴板 - 由于API差异，这里简化处理
                var text = _unixInputTextBox.Text;
                // 在Avalonia中，可以通过TopLevel获取剪贴板
                if (TopLevel.GetTopLevel(this) is { } topLevel)
                {
                    topLevel.Clipboard?.SetTextAsync(text);
                }
            }
            catch
            {
                // 忽略错误
            }
        }
    }

    private void OnUnixToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            _unixResultTextBlock!.Text = "请输入有效的Unix时间戳（整数）";
            return;
        }
        var dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(dt.DayOfWeek);
        _beijingDateTextBox!.Text = $"{dt.Year}/{dt.Month}/{dt.Day}/{dayOfWeek}";
        _beijingTimeTextBox!.Text = dt.ToString("HH:mm:ss");
    }

    private void OnUnixToLunar(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            _unixResultTextBlock!.Text = "请输入有效的Unix时间戳（整数）";
            return;
        }
        var dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        _unixResultTextBlock!.Text = $"农历: {dt:yyyy年MM月dd日} (需安装lunar-csharp)";
    }

    private void OnUnixToZone(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            _unixResultTextBlock!.Text = "请输入有效的Unix时间戳（整数）";
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            _unixResultTextBlock!.Text = "请先选择时区";
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "中时区", 0);
        var dt = UnixTimeHelper.FromUnixTimestamp(timestamp).AddHours(-offset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(dt.DayOfWeek);
        _zoneDateTextBox!.Text = $"{dt.Year}/{dt.Month}/{dt.Day}/{dayOfWeek}";
        _zoneTimeTextBox!.Text = dt.ToString("HH:mm:ss");
    }

    private void OnUnixToLocal(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            _unixResultTextBlock!.Text = "请输入有效的Unix时间戳（整数）";
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            _unixResultTextBlock!.Text = "请输入有效的经度";
            return;
        }
        var dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        var offsetMinutes = (longitude - 120) * 4;
        var localTime = dt.AddMinutes(-offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        _localDateTextBox!.Text = $"{localTime.Year}/{localTime.Month}/{localTime.Day}/{dayOfWeek}";
        _localTimeTextBox!.Text = localTime.ToString("HH:mm:ss");
    }

    private void OnLunarToBeijing(object? sender, RoutedEventArgs e)
    {
        // TODO: 实现农历到公历转换（需要lunar-csharp库）
        _lunarResultTextBlock!.Text = "农历转换功能需要安装 lunar-csharp NuGet 包";
    }

    private void OnLunarToUnix(object? sender, RoutedEventArgs e)
    {
        _lunarResultTextBlock!.Text = "农历转换功能需要安装 lunar-csharp NuGet 包";
    }

    private void OnLunarToZone(object? sender, RoutedEventArgs e)
    {
        _lunarResultTextBlock!.Text = "农历转换功能需要安装 lunar-csharp NuGet 包";
    }

    private void OnLunarToLocal(object? sender, RoutedEventArgs e)
    {
        _lunarResultTextBlock!.Text = "农历转换功能需要安装 lunar-csharp NuGet 包";
    }

    private void OnZoneToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            _zoneResultTextBlock!.Text = "请输入有效的日期、时间和时区";
            return;
        }
        var beijingTime = dt.AddHours(offset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(beijingTime.DayOfWeek);
        _beijingDateTextBox!.Text = $"{beijingTime.Year}/{beijingTime.Month}/{beijingTime.Day}/{dayOfWeek}";
        _beijingTimeTextBox!.Text = beijingTime.ToString("HH:mm:ss");
    }

    private void OnZoneToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            _zoneResultTextBlock!.Text = "请输入有效的日期、时间和时区";
            return;
        }
        var beijingTime = dt.AddHours(offset);
        var timestamp = UnixTimeHelper.ToUnixTimestamp(beijingTime);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnZoneToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            _zoneResultTextBlock!.Text = "请输入有效的日期、时间和时区";
            return;
        }
        _zoneResultTextBlock!.Text = $"农历: {dt.AddHours(offset):yyyy年MM月dd日} (需安装lunar-csharp)";
    }

    private void OnZoneToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            _zoneResultTextBlock!.Text = "请输入有效的日期、时间和时区";
            return;
        }
        if (!TryParseLongitude())
        {
            _zoneResultTextBlock!.Text = "请输入有效的经度";
            return;
        }
        var beijingTime = dt.AddHours(offset);
        var longitude = double.Parse(_localLongitudeTextBox!.Text ?? "0");
        var offsetMinutes = (longitude - 120) * 4;
        var localTime = beijingTime.AddMinutes(-offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        _localDateTextBox!.Text = $"{localTime.Year}/{localTime.Month}/{localTime.Day}/{dayOfWeek}";
        _localTimeTextBox!.Text = localTime.ToString("HH:mm:ss");
    }

    private void OnLocalToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            _localResultTextBlock!.Text = "请输入有效的日期、时间和经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var beijingTime = dt.AddMinutes(offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(beijingTime.DayOfWeek);
        _beijingDateTextBox!.Text = $"{beijingTime.Year}/{beijingTime.Month}/{beijingTime.Day}/{dayOfWeek}";
        _beijingTimeTextBox!.Text = beijingTime.ToString("HH:mm:ss");
    }

    private void OnLocalToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            _localResultTextBlock!.Text = "请输入有效的日期、时间和经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var beijingTime = dt.AddMinutes(offsetMinutes);
        var timestamp = UnixTimeHelper.ToUnixTimestamp(beijingTime);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnLocalToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            _localResultTextBlock!.Text = "请输入有效的日期、时间和经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var beijingTime = dt.AddMinutes(offsetMinutes);
        _localResultTextBlock!.Text = $"农历: {beijingTime:yyyy年MM月dd日} (需安装lunar-csharp)";
    }

    private void OnLocalToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            _localResultTextBlock!.Text = "请输入有效的日期、时间和经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var beijingTime = dt.AddMinutes(offsetMinutes);
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            _localResultTextBlock!.Text = "请先选择时区";
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var zoneOffset = _timeZones.GetValueOrDefault(zoneName ?? "中时区", 0);
        var zoneTime = beijingTime.AddHours(-zoneOffset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        _zoneDateTextBox!.Text = $"{zoneTime.Year}/{zoneTime.Month}/{zoneTime.Day}/{dayOfWeek}";
        _zoneTimeTextBox!.Text = zoneTime.ToString("HH:mm:ss");
    }

    #endregion

    #region 辅助方法

    private bool TryParseBeijingDateTime(out DateTime result)
    {
        result = DateTime.MinValue;
        var dateText = _beijingDateTextBox?.Text ?? "";
        var timeText = _beijingTimeTextBox?.Text ?? "";

        if (string.IsNullOrWhiteSpace(dateText) || string.IsNullOrWhiteSpace(timeText))
            return false;

        // 解析日期（格式：年/月/日/星期）
        var dateParts = dateText.Split('/');
        if (dateParts.Length < 3) return false;
        if (!int.TryParse(dateParts[0], out var year)) return false;
        if (!int.TryParse(dateParts[1], out var month)) return false;
        if (!int.TryParse(dateParts[2], out var day)) return false;

        // 解析时间
        var timeParts = timeText.Split(':');
        if (timeParts.Length < 3) return false;
        if (!int.TryParse(timeParts[0], out var hour)) return false;
        if (!int.TryParse(timeParts[1], out var minute)) return false;
        if (!int.TryParse(timeParts[2], out var second)) return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryParseZoneDateTime(out DateTime result, out int offset)
    {
        result = DateTime.MinValue;
        offset = 0;

        var dateText = _zoneDateTextBox?.Text ?? "";
        var timeText = _zoneTimeTextBox?.Text ?? "";
        var zoneName = _zoneComboBox?.SelectedItem?.ToString() ?? "中时区";

        if (string.IsNullOrWhiteSpace(dateText) || string.IsNullOrWhiteSpace(timeText))
            return false;

        // 解析日期
        var dateParts = dateText.Split('/');
        if (dateParts.Length < 3) return false;
        if (!int.TryParse(dateParts[0], out var year)) return false;
        if (!int.TryParse(dateParts[1], out var month)) return false;
        if (!int.TryParse(dateParts[2], out var day)) return false;

        // 解析时间
        var timeParts = timeText.Split(':');
        if (timeParts.Length < 3) return false;
        if (!int.TryParse(timeParts[0], out var hour)) return false;
        if (!int.TryParse(timeParts[1], out var minute)) return false;
        if (!int.TryParse(timeParts[2], out var second)) return false;

        // 获取时区偏移
        offset = _timeZones.GetValueOrDefault(zoneName, 0);

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryParseLocalDateTime(out DateTime result, out double longitude)
    {
        result = DateTime.MinValue;
        longitude = 0;

        var dateText = _localDateTextBox?.Text ?? "";
        var timeText = _localTimeTextBox?.Text ?? "";

        if (string.IsNullOrWhiteSpace(dateText) || string.IsNullOrWhiteSpace(timeText))
            return false;

        if (!TryParseLongitude(out longitude))
            return false;

        // 解析日期
        var dateParts = dateText.Split('/');
        if (dateParts.Length < 3) return false;
        if (!int.TryParse(dateParts[0], out var year)) return false;
        if (!int.TryParse(dateParts[1], out var month)) return false;
        if (!int.TryParse(dateParts[2], out var day)) return false;

        // 解析时间
        var timeParts = timeText.Split(':');
        if (timeParts.Length < 3) return false;
        if (!int.TryParse(timeParts[0], out var hour)) return false;
        if (!int.TryParse(timeParts[1], out var minute)) return false;
        if (!int.TryParse(timeParts[2], out var second)) return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryParseLongitude()
    {
        return TryParseLongitude(out _);
    }

    private bool TryParseLongitude(out double result)
    {
        result = 0;
        var text = _localLongitudeTextBox?.Text ?? "";
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (!double.TryParse(text, out result)) return false;
        if (result <= -180 || result > 180) return false;

        return true;
    }

    #endregion
}
