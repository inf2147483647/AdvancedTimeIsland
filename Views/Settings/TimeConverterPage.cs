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
/// 支持北京时间/时间戳/农历/区时/地方时互转
/// </summary>
public class TimeConverterPage : UserControl
{
    /// <summary>
    /// 获取 ClassIsland 强调色画刷
    /// </summary>
    private static SolidColorBrush GetAccentBrush()
    {
        // 尝试从 Avalonia 资源中获取系统强调色
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            return new SolidColorBrush(accentColor);
        }
        // 尝试获取 AccentColor
        if (Application.Current?.TryFindResource("AccentColor", out var accentObj) == true && accentObj is Color accentColor2)
        {
            return new SolidColorBrush(accentColor2);
        }
        // 回退到默认的 DodgerBlue
        return new SolidColorBrush(Colors.DodgerBlue);
    }

    /// <summary>
    /// 根据强调色亮度获取对比文本颜色（黑/白）
    /// </summary>
    private static IBrush GetAccentTextBrush(SolidColorBrush accentBrush)
    {
        var color = accentBrush.Color;
        // 使用相对亮度公式计算亮度（0-1）
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        // 亮度 > 0.6 时使用黑色文本，否则使用白色文本
        return luminance > 0.6 ? Brushes.Black : Brushes.White;
    }

    // 北京时间转换模块控件
    private ComboBox? _beijingYearComboBox;
    private ComboBox? _beijingMonthComboBox;
    private ComboBox? _beijingDayComboBox;
    private ComboBox? _beijingHourComboBox;
    private ComboBox? _beijingMinuteComboBox;
    private ComboBox? _beijingSecondComboBox;
    private TextBlock? _beijingResultTextBlock;

    // 时间戳转换模块控件
    private TextBox? _unixInputTextBox;
    private TextBlock? _unixResultTextBlock;

    // 农历转换模块控件
    private ComboBox? _lunarYearRangeComboBox;
    private ComboBox? _lunarTianganComboBox;
    private ComboBox? _lunarDizhiComboBox;
    private ComboBox? _lunarMonthComboBox;
    private ComboBox? _lunarDayComboBox;
    private ComboBox? _lunarHourComboBox;
    private ComboBox? _lunarMinuteComboBox;
    private ComboBox? _lunarSecondComboBox;
    private TextBlock? _lunarResultTextBlock;

    // 区时转换模块控件
    private ComboBox? _zoneYearComboBox;
    private ComboBox? _zoneMonthComboBox;
    private ComboBox? _zoneDayComboBox;
    private ComboBox? _zoneHourComboBox;
    private ComboBox? _zoneMinuteComboBox;
    private ComboBox? _zoneSecondComboBox;
    private ComboBox? _zoneComboBox;
    private TextBlock? _zoneResultTextBlock;

    // 地方时转换模块控件
    private ComboBox? _localYearComboBox;
    private ComboBox? _localMonthComboBox;
    private ComboBox? _localDayComboBox;
    private ComboBox? _localHourComboBox;
    private ComboBox? _localMinuteComboBox;
    private ComboBox? _localSecondComboBox;
    private TextBox? _localLongitudeTextBox;
    private TextBlock? _localResultTextBlock;

    // 夏令时开关
    private CheckBox? _zoneDstCheckBox;
    private CheckBox? _localDstCheckBox;

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
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(6)
        };
        clearButton.Click += OnClearAllClick;

        headerPanel.Children.Add(clearButton);
        mainPanel.Children.Add(headerPanel);

        // 1. 北京时间转换模块
        mainPanel.Children.Add(CreateBeijingModule());

        // 2. 时间戳转换模块
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
        var datePanel = CreateDatePickerRow("日期输入:", out _beijingYearComboBox, out _beijingMonthComboBox, out _beijingDayComboBox);
        content.Children.Add(datePanel);

        // 时间选择
        var timePanel = CreateTimePickerRow("时间:", out _beijingHourComboBox, out _beijingMinuteComboBox, out _beijingSecondComboBox);
        content.Children.Add(timePanel);

        // 选取当前时间按钮
        var currentButton = new Button
        {
            Content = "选取当前时间",
            Padding = new Avalonia.Thickness(12, 6),
            Background = Brushes.DarkGreen,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(6)
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

        AddConvertButton(buttonPanel, "转为时间戳", OnBeijingToUnix);
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
    /// 创建时间戳转换模块
    /// </summary>
    private Border CreateUnixModule()
    {
        var module = CreateModulePanel("时间戳转换模块", "时间戳");
        var content = GetModuleContent(module);

        // 输入框（整数）
        var inputPanel = CreateInputRow("输入（整数）:", 200);
        _unixInputTextBox = (TextBox)inputPanel.Children[1];
        _unixInputTextBox.Watermark = "输入时间戳";
        content.Children.Add(inputPanel);

        // 复制按钮
        var copyButton = new Button
        {
            Content = "复制",
            Padding = new Avalonia.Thickness(12, 6),
            Background = Brushes.Gray,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(6)
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

        _lunarTianganComboBox = new ComboBox { Width = 70 };
        foreach (var tg in _tiangan) _lunarTianganComboBox.Items.Add(tg);
        tgdzPanel.Children.Add(_lunarTianganComboBox);

        _lunarDizhiComboBox = new ComboBox { Width = 70 };
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
            _lunarMonthComboBox.Items.Add($"{i}月");
            _lunarMonthComboBox.Items.Add($"闰{i}月");
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

        _lunarDayComboBox = new ComboBox { Width = 70 };
        for (int i = 1; i <= 30; i++)
        {
            _lunarDayComboBox.Items.Add($"{i}日");
        }
        dayPanel.Children.Add(_lunarDayComboBox);

        content.Children.Add(dayPanel);

        // 时间
        var lunarTimePanel = CreateTimePickerRow("时间:", out _lunarHourComboBox, out _lunarMinuteComboBox, out _lunarSecondComboBox);
        content.Children.Add(lunarTimePanel);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为北京时间", OnLunarToBeijing);
        AddConvertButton(buttonPanel, "转为时间戳", OnLunarToUnix);
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
        var datePanel = CreateDatePickerRow("日期输入:", out _zoneYearComboBox, out _zoneMonthComboBox, out _zoneDayComboBox);
        content.Children.Add(datePanel);

        // 时间选择
        var timePanel = CreateTimePickerRow("时间:", out _zoneHourComboBox, out _zoneMinuteComboBox, out _zoneSecondComboBox);
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

        zonePanel.Children.Add(new TextBlock
        {
            Text = "夏令时:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(16, 0, 0, 0)
        });

        _zoneDstCheckBox = new CheckBox
        {
            Content = "开启",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        zonePanel.Children.Add(_zoneDstCheckBox);

        content.Children.Add(zonePanel);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为北京时间", OnZoneToBeijing);
        AddConvertButton(buttonPanel, "转为时间戳", OnZoneToUnix);
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
        var datePanel = CreateDatePickerRow("日期输入:", out _localYearComboBox, out _localMonthComboBox, out _localDayComboBox);
        content.Children.Add(datePanel);

        // 时间选择
        var timePanel = CreateTimePickerRow("时间:", out _localHourComboBox, out _localMinuteComboBox, out _localSecondComboBox);
        content.Children.Add(timePanel);

        // 经度输入
        var lonPanel = CreateInputRow("经度（单位：度，正数东经，负数西经）:", 100);
        _localLongitudeTextBox = (TextBox)lonPanel.Children[1];
        _localLongitudeTextBox.Watermark = "如：116.4";
        content.Children.Add(lonPanel);

        // 夏令时选择
        var dstPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        dstPanel.Children.Add(new TextBlock
        {
            Text = "夏令时:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _localDstCheckBox = new CheckBox
        {
            Content = "开启",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        dstPanel.Children.Add(_localDstCheckBox);

        content.Children.Add(dstPanel);

        // 转换按钮组
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        AddConvertButton(buttonPanel, "转为北京时间", OnLocalToBeijing);
        AddConvertButton(buttonPanel, "转为时间戳", OnLocalToUnix);
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
            Margin = new Thickness(0, 0, 0, 16),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true
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
            Foreground = GetAccentBrush()
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
            Watermark = "",
            CornerRadius = new CornerRadius(4)
        };
        panel.Children.Add(textBox);

        return panel;
    }

    private StackPanel CreateDatePickerRow(string label, out ComboBox yearComboBox, out ComboBox monthComboBox, out ComboBox dayComboBox)
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

        var datePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        yearComboBox = new ComboBox
        {
            Width = 100,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 1900; i <= 2100; i++)
        {
            yearComboBox.Items.Add($"{i}年");
        }
        datePanel.Children.Add(yearComboBox);

        monthComboBox = new ComboBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 1; i <= 12; i++)
        {
            monthComboBox.Items.Add($"{i}月");
        }
        datePanel.Children.Add(monthComboBox);

        dayComboBox = new ComboBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 1; i <= 31; i++)
        {
            dayComboBox.Items.Add($"{i}日");
        }
        datePanel.Children.Add(dayComboBox);

        panel.Children.Add(datePanel);

        return panel;
    }

    private StackPanel CreateTimePickerRow(string label, out ComboBox hourComboBox, out ComboBox minuteComboBox, out ComboBox secondComboBox)
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

        var timePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        hourComboBox = new ComboBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 0; i < 24; i++)
        {
            hourComboBox.Items.Add(i.ToString("D2"));
        }
        timePanel.Children.Add(hourComboBox);

        timePanel.Children.Add(new TextBlock
        {
            Text = ":",
            FontSize = 16,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        minuteComboBox = new ComboBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 0; i < 60; i++)
        {
            minuteComboBox.Items.Add(i.ToString("D2"));
        }
        timePanel.Children.Add(minuteComboBox);

        timePanel.Children.Add(new TextBlock
        {
            Text = ":",
            FontSize = 16,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        secondComboBox = new ComboBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 0; i < 60; i++)
        {
            secondComboBox.Items.Add(i.ToString("D2"));
        }
        timePanel.Children.Add(secondComboBox);

        panel.Children.Add(timePanel);

        return panel;
    }

    private Button AddConvertButton(StackPanel panel, string text, EventHandler<RoutedEventArgs> handler)
    {
        var accentBrush = GetAccentBrush();
        var button = new Button
        {
            Content = text,
            Padding = new Avalonia.Thickness(12, 6),
            Background = accentBrush,
            Foreground = GetAccentTextBrush(accentBrush),
            CornerRadius = new CornerRadius(6)
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
        if (_beijingYearComboBox != null) _beijingYearComboBox.SelectedIndex = -1;
        if (_beijingMonthComboBox != null) _beijingMonthComboBox.SelectedIndex = -1;
        if (_beijingDayComboBox != null) _beijingDayComboBox.SelectedIndex = -1;
        if (_beijingHourComboBox != null) _beijingHourComboBox.SelectedIndex = -1;
        if (_beijingMinuteComboBox != null) _beijingMinuteComboBox.SelectedIndex = -1;
        if (_beijingSecondComboBox != null) _beijingSecondComboBox.SelectedIndex = -1;
        if (_beijingResultTextBlock != null) _beijingResultTextBlock.Text = "";

        if (_unixInputTextBox != null) _unixInputTextBox.Text = "";
        if (_unixResultTextBlock != null) _unixResultTextBlock.Text = "";

        if (_lunarTianganComboBox != null) _lunarTianganComboBox.SelectedIndex = -1;
        if (_lunarDizhiComboBox != null) _lunarDizhiComboBox.SelectedIndex = -1;
        if (_lunarMonthComboBox != null) _lunarMonthComboBox.SelectedIndex = -1;
        if (_lunarDayComboBox != null) _lunarDayComboBox.SelectedIndex = -1;
        if (_lunarHourComboBox != null) _lunarHourComboBox.SelectedIndex = -1;
        if (_lunarMinuteComboBox != null) _lunarMinuteComboBox.SelectedIndex = -1;
        if (_lunarSecondComboBox != null) _lunarSecondComboBox.SelectedIndex = -1;
        if (_lunarResultTextBlock != null) _lunarResultTextBlock.Text = "";

        if (_zoneYearComboBox != null) _zoneYearComboBox.SelectedIndex = -1;
        if (_zoneMonthComboBox != null) _zoneMonthComboBox.SelectedIndex = -1;
        if (_zoneDayComboBox != null) _zoneDayComboBox.SelectedIndex = -1;
        if (_zoneHourComboBox != null) _zoneHourComboBox.SelectedIndex = -1;
        if (_zoneMinuteComboBox != null) _zoneMinuteComboBox.SelectedIndex = -1;
        if (_zoneSecondComboBox != null) _zoneSecondComboBox.SelectedIndex = -1;
        if (_zoneComboBox != null) _zoneComboBox.SelectedIndex = -1;
        if (_zoneResultTextBlock != null) _zoneResultTextBlock.Text = "";

        if (_localYearComboBox != null) _localYearComboBox.SelectedIndex = -1;
        if (_localMonthComboBox != null) _localMonthComboBox.SelectedIndex = -1;
        if (_localDayComboBox != null) _localDayComboBox.SelectedIndex = -1;
        if (_localHourComboBox != null) _localHourComboBox.SelectedIndex = -1;
        if (_localMinuteComboBox != null) _localMinuteComboBox.SelectedIndex = -1;
        if (_localSecondComboBox != null) _localSecondComboBox.SelectedIndex = -1;
        if (_localLongitudeTextBox != null) _localLongitudeTextBox.Text = "";
        if (_localResultTextBlock != null) _localResultTextBlock.Text = "";
    }

    private void OnBeijingCurrentTimeClick(object? sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        _beijingYearComboBox!.SelectedItem = $"{now.Year}年";
        _beijingMonthComboBox!.SelectedItem = $"{now.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{now.Day}日";
        _beijingHourComboBox!.SelectedItem = now.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = now.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = now.Second.ToString("D2");
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
        var lunar = LunarCalendarHelper.SolarToLunar(dt);
        _beijingResultTextBlock!.Text = $"农历: {lunar}";
        FillLunarComboBoxes(dt);
    }

    private void FillLunarComboBoxes(DateTime dt)
    {
        var lunarYear = LunarCalendarHelper.GetLunarYear(dt);
        var lunarMonth = LunarCalendarHelper.GetLunarMonth(dt);
        var lunarDay = LunarCalendarHelper.GetLunarDay(dt);
        var isLeapMonth = LunarCalendarHelper.IsLeapMonth(dt);
        var tiangan = LunarCalendarHelper.GetTiangan(lunarYear);
        var dizhi = LunarCalendarHelper.GetDizhi(lunarYear);

        // 设置年份范围
        if (_lunarYearRangeComboBox != null)
        {
            if (lunarYear >= 1924 && lunarYear <= 1983)
                _lunarYearRangeComboBox.SelectedItem = "1924-1983";
            else if (lunarYear >= 1984 && lunarYear <= 2043)
                _lunarYearRangeComboBox.SelectedItem = "1984-2043";
            else if (lunarYear >= 2044 && lunarYear <= 2103)
                _lunarYearRangeComboBox.SelectedItem = "2044-2103";
        }

        // 设置天干地支
        if (_lunarTianganComboBox != null)
            _lunarTianganComboBox.SelectedItem = tiangan;
        if (_lunarDizhiComboBox != null)
            _lunarDizhiComboBox.SelectedItem = dizhi;

        // 设置月份
        if (_lunarMonthComboBox != null)
        {
            var monthText = isLeapMonth ? $"闰{lunarMonth}月" : $"{lunarMonth}月";
            _lunarMonthComboBox.SelectedItem = monthText;
        }

        // 设置日期
        if (_lunarDayComboBox != null)
            _lunarDayComboBox.SelectedItem = $"{lunarDay}日";

        // 设置时间
        if (_lunarHourComboBox != null)
            _lunarHourComboBox.SelectedItem = dt.Hour.ToString("D2");
        if (_lunarMinuteComboBox != null)
            _lunarMinuteComboBox.SelectedItem = dt.Minute.ToString("D2");
        if (_lunarSecondComboBox != null)
            _lunarSecondComboBox.SelectedItem = dt.Second.ToString("D2");
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
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        var zoneTime = dt.AddHours(offset - 8 + dstOffset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        _zoneYearComboBox!.SelectedItem = $"{zoneTime.Year}年";
        _zoneMonthComboBox!.SelectedItem = $"{zoneTime.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{zoneTime.Day}日";
        _zoneHourComboBox!.SelectedItem = zoneTime.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = zoneTime.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = zoneTime.Second.ToString("D2");
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
        var dstOffset = (_localDstCheckBox?.IsChecked == true) ? 60 : 0;
        var localTime = dt.AddMinutes(offsetMinutes + dstOffset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        _localYearComboBox!.SelectedItem = $"{localTime.Year}年";
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{localTime.Day}日";
        _localHourComboBox!.SelectedItem = localTime.Hour.ToString("D2");
        _localMinuteComboBox!.SelectedItem = localTime.Minute.ToString("D2");
        _localSecondComboBox!.SelectedItem = localTime.Second.ToString("D2");
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
            _unixResultTextBlock!.Text = "请输入有效的时间戳（整数）";
            return;
        }
        var dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(dt.DayOfWeek);
        _beijingYearComboBox!.SelectedItem = $"{dt.Year}年";
        _beijingMonthComboBox!.SelectedItem = $"{dt.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{dt.Day}日";
        _beijingHourComboBox!.SelectedItem = dt.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = dt.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = dt.Second.ToString("D2");
    }

    private void OnUnixToLunar(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            _unixResultTextBlock!.Text = "请输入有效的时间戳（整数）";
            return;
        }
        var dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        var lunar = LunarCalendarHelper.SolarToLunar(dt);
        _unixResultTextBlock!.Text = $"农历: {lunar}";
        FillLunarComboBoxes(dt);
    }

    private void OnUnixToZone(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            _unixResultTextBlock!.Text = "请输入有效的时间戳（整数）";
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            _unixResultTextBlock!.Text = "请先选择时区";
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "中时区", 0);
        var dt = UnixTimeHelper.FromUnixTimestampUtc(timestamp).AddHours(offset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(dt.DayOfWeek);
        _zoneYearComboBox!.SelectedItem = $"{dt.Year}年";
        _zoneMonthComboBox!.SelectedItem = $"{dt.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{dt.Day}日";
        _zoneHourComboBox!.SelectedItem = dt.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = dt.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = dt.Second.ToString("D2");
    }

    private void OnUnixToLocal(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            _unixResultTextBlock!.Text = "请输入有效的时间戳（整数）";
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            _unixResultTextBlock!.Text = "请输入有效的经度";
            return;
        }
        var dt = UnixTimeHelper.FromUnixTimestampUtc(timestamp);
        var offsetMinutes = (longitude - 120) * 4;
        var localTime = dt.AddMinutes(offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        _localYearComboBox!.SelectedItem = $"{localTime.Year}年";
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{localTime.Day}日";
        _localHourComboBox!.SelectedItem = localTime.Hour.ToString("D2");
        _localMinuteComboBox!.SelectedItem = localTime.Minute.ToString("D2");
        _localSecondComboBox!.SelectedItem = localTime.Second.ToString("D2");
    }

    private void OnLunarToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            _lunarResultTextBlock!.Text = "请输入有效的农历日期和时间";
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(dt.DayOfWeek);
        _beijingYearComboBox!.SelectedItem = $"{dt.Year}年";
        _beijingMonthComboBox!.SelectedItem = $"{dt.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{dt.Day}日";
        _beijingHourComboBox!.SelectedItem = dt.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = dt.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = dt.Second.ToString("D2");
    }

    private void OnLunarToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            _lunarResultTextBlock!.Text = "请输入有效的农历日期和时间";
            return;
        }
        var timestamp = UnixTimeHelper.ToUnixTimestamp(dt);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnLunarToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            _lunarResultTextBlock!.Text = "请输入有效的农历日期和时间";
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            _lunarResultTextBlock!.Text = "请先选择时区";
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "中时区", 0);
        var zoneTime = dt.AddHours(-offset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        _zoneYearComboBox!.SelectedItem = $"{zoneTime.Year}年";
        _zoneMonthComboBox!.SelectedItem = $"{zoneTime.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{zoneTime.Day}日";
        _zoneHourComboBox!.SelectedItem = zoneTime.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = zoneTime.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = zoneTime.Second.ToString("D2");
    }

    private void OnLunarToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            _lunarResultTextBlock!.Text = "请输入有效的农历日期和时间";
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            _lunarResultTextBlock!.Text = "请输入有效的经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var localTime = dt.AddMinutes(-offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        _localYearComboBox!.SelectedItem = $"{localTime.Year}年";
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{localTime.Day}日";
        _localHourComboBox!.SelectedItem = localTime.Hour.ToString("D2");
        _localMinuteComboBox!.SelectedItem = localTime.Minute.ToString("D2");
        _localSecondComboBox!.SelectedItem = localTime.Second.ToString("D2");
    }

    private void OnZoneToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            _zoneResultTextBlock!.Text = "请输入有效的日期、时间和时区";
            return;
        }
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        var beijingTime = dt.AddHours(8 - offset - dstOffset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(beijingTime.DayOfWeek);
        _beijingYearComboBox!.SelectedItem = $"{beijingTime.Year}年";
        _beijingMonthComboBox!.SelectedItem = $"{beijingTime.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{beijingTime.Day}日";
        _beijingHourComboBox!.SelectedItem = beijingTime.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = beijingTime.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = beijingTime.Second.ToString("D2");
    }

    private void OnZoneToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            _zoneResultTextBlock!.Text = "请输入有效的日期、时间和时区";
            return;
        }
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        var utcTime = DateTime.SpecifyKind(dt.AddHours(-offset - dstOffset), DateTimeKind.Utc);
        var timestamp = UnixTimeHelper.ToUnixTimestamp(utcTime);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnZoneToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            _zoneResultTextBlock!.Text = "请输入有效的日期、时间和时区";
            return;
        }
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        var beijingTime = dt.AddHours(8 - offset - dstOffset);
        var lunar = LunarCalendarHelper.SolarToLunar(beijingTime);
        _zoneResultTextBlock!.Text = $"农历: {lunar}";
        FillLunarComboBoxes(beijingTime);
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
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        var beijingTime = dt.AddHours(8 - offset - dstOffset);
        var longitude = double.Parse(_localLongitudeTextBox!.Text ?? "0");
        var offsetMinutes = (longitude - 120) * 4;
        var localTime = beijingTime.AddMinutes(offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        _localYearComboBox!.SelectedItem = $"{localTime.Year}年";
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{localTime.Day}日";
        _localHourComboBox!.SelectedItem = localTime.Hour.ToString("D2");
        _localMinuteComboBox!.SelectedItem = localTime.Minute.ToString("D2");
        _localSecondComboBox!.SelectedItem = localTime.Second.ToString("D2");
    }

    private void OnLocalToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            _localResultTextBlock!.Text = "请输入有效的日期、时间和经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var beijingTime = dt.AddMinutes(-offsetMinutes);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(beijingTime.DayOfWeek);
        _beijingYearComboBox!.SelectedItem = $"{beijingTime.Year}年";
        _beijingMonthComboBox!.SelectedItem = $"{beijingTime.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{beijingTime.Day}日";
        _beijingHourComboBox!.SelectedItem = beijingTime.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = beijingTime.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = beijingTime.Second.ToString("D2");
    }

    private void OnLocalToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            _localResultTextBlock!.Text = "请输入有效的日期、时间和经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var beijingTime = dt.AddMinutes(-offsetMinutes);
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
        var beijingTime = dt.AddMinutes(-offsetMinutes);
        var lunar = LunarCalendarHelper.SolarToLunar(beijingTime);
        _localResultTextBlock!.Text = $"农历: {lunar}";
        FillLunarComboBoxes(beijingTime);
    }

    private void OnLocalToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            _localResultTextBlock!.Text = "请输入有效的日期、时间和经度";
            return;
        }
        var offsetMinutes = (longitude - 120) * 4;
        var beijingTime = dt.AddMinutes(-offsetMinutes);
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            _localResultTextBlock!.Text = "请先选择时区";
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "中时区", 0);
        var dstOffset = (_localDstCheckBox?.IsChecked == true) ? 1 : 0;
        var zoneTime = beijingTime.AddHours(offset + dstOffset);
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        _zoneYearComboBox!.SelectedItem = $"{zoneTime.Year}年";
        _zoneMonthComboBox!.SelectedItem = $"{zoneTime.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{zoneTime.Day}日";
        _zoneHourComboBox!.SelectedItem = zoneTime.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = zoneTime.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = zoneTime.Second.ToString("D2");
    }

    #endregion

    #region 辅助方法

    private bool TryParseBeijingDateTime(out DateTime result)
    {
        result = DateTime.MinValue;

        if (_beijingYearComboBox?.SelectedItem == null ||
            _beijingMonthComboBox?.SelectedItem == null ||
            _beijingDayComboBox?.SelectedItem == null)
            return false;

        if (!int.TryParse(_beijingYearComboBox.SelectedItem.ToString()?.Replace("年", ""), out var year))
            return false;
        if (!int.TryParse(_beijingMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return false;
        if (!int.TryParse(_beijingDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day))
            return false;

        if (!int.TryParse(_beijingHourComboBox?.SelectedItem?.ToString(), out var hour))
            return false;
        if (!int.TryParse(_beijingMinuteComboBox?.SelectedItem?.ToString(), out var minute))
            return false;
        if (!int.TryParse(_beijingSecondComboBox?.SelectedItem?.ToString(), out var second))
            return false;

        result = new DateTime(year, month, day, hour, minute, second);
        return true;
    }

    private bool TryParseZoneDateTime(out DateTime result, out int offset)
    {
        result = DateTime.MinValue;
        offset = 0;

        if (_zoneYearComboBox?.SelectedItem == null ||
            _zoneMonthComboBox?.SelectedItem == null ||
            _zoneDayComboBox?.SelectedItem == null)
            return false;

        if (!int.TryParse(_zoneYearComboBox.SelectedItem.ToString()?.Replace("年", ""), out var year))
            return false;
        if (!int.TryParse(_zoneMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return false;
        if (!int.TryParse(_zoneDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day))
            return false;

        if (!int.TryParse(_zoneHourComboBox?.SelectedItem?.ToString(), out var hour))
            return false;
        if (!int.TryParse(_zoneMinuteComboBox?.SelectedItem?.ToString(), out var minute))
            return false;
        if (!int.TryParse(_zoneSecondComboBox?.SelectedItem?.ToString(), out var second))
            return false;

        var zoneName = _zoneComboBox?.SelectedItem?.ToString() ?? "中时区";

        offset = _timeZones.GetValueOrDefault(zoneName, 0);

        result = new DateTime(year, month, day, hour, minute, second);
        return true;
    }

    private bool TryParseLocalDateTime(out DateTime result, out double longitude)
    {
        result = DateTime.MinValue;
        longitude = 0;

        if (_localYearComboBox?.SelectedItem == null ||
            _localMonthComboBox?.SelectedItem == null ||
            _localDayComboBox?.SelectedItem == null)
            return false;

        if (!int.TryParse(_localYearComboBox.SelectedItem.ToString()?.Replace("年", ""), out var year))
            return false;
        if (!int.TryParse(_localMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return false;
        if (!int.TryParse(_localDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day))
            return false;

        if (!int.TryParse(_localHourComboBox?.SelectedItem?.ToString(), out var hour))
            return false;
        if (!int.TryParse(_localMinuteComboBox?.SelectedItem?.ToString(), out var minute))
            return false;
        if (!int.TryParse(_localSecondComboBox?.SelectedItem?.ToString(), out var second))
            return false;

        if (!TryParseLongitude(out longitude))
            return false;

        result = new DateTime(year, month, day, hour, minute, second);
        return true;
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

    private bool TryParseLunarDateTime(out DateTime result)
    {
        result = DateTime.MinValue;

        if (_lunarYearRangeComboBox?.SelectedItem == null ||
            _lunarTianganComboBox?.SelectedItem == null ||
            _lunarDizhiComboBox?.SelectedItem == null ||
            _lunarMonthComboBox?.SelectedItem == null ||
            _lunarDayComboBox?.SelectedItem == null)
            return false;

        // 解析年份范围
        var yearRange = _lunarYearRangeComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(yearRange)) return false;

        var yearParts = yearRange.Split('-');
        if (yearParts.Length != 2) return false;
        if (!int.TryParse(yearParts[0], out var startYear)) return false;
        if (!int.TryParse(yearParts[1], out var endYear)) return false;

        // 获取天干地支对应年份
        var tiangan = _lunarTianganComboBox.SelectedItem.ToString();
        var dizhi = _lunarDizhiComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(tiangan) || string.IsNullOrEmpty(dizhi)) return false;

        // 计算天干地支对应的年份
        var tianganIndex = Array.IndexOf(_tiangan, tiangan);
        var dizhiIndex = Array.IndexOf(_dizhi, dizhi);
        if (tianganIndex < 0 || dizhiIndex < 0) return false;

        var baseYear = 4; // 甲子年 = 4 AD
        var yearOffset = 0;
        while ((baseYear + yearOffset - 4) % 10 != tianganIndex ||
               (baseYear + yearOffset - 4) % 12 != dizhiIndex)
        {
            yearOffset++;
            if (yearOffset > 60) return false; // 60年一甲子
        }

        var lunarYear = startYear + yearOffset;
        while (lunarYear < startYear)
        {
            lunarYear += 60;
        }
        while (lunarYear > endYear)
        {
            lunarYear -= 60;
        }

        // 解析月份
        var monthText = _lunarMonthComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(monthText)) return false;

        var isLeapMonth = monthText.StartsWith("闰");
        var monthStr = isLeapMonth ? monthText[1..].Replace("月", "") : monthText.Replace("月", "");
        if (!int.TryParse(monthStr, out var lunarMonth)) return false;

        // 解析日期
        var dayText = _lunarDayComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(dayText)) return false;
        if (!int.TryParse(dayText.Replace("日", ""), out var lunarDay)) return false;

        // 解析时间
        if (!int.TryParse(_lunarHourComboBox?.SelectedItem?.ToString(), out var hour))
            return false;
        if (!int.TryParse(_lunarMinuteComboBox?.SelectedItem?.ToString(), out var minute))
            return false;
        if (!int.TryParse(_lunarSecondComboBox?.SelectedItem?.ToString(), out var second))
            return false;

        var solarDate = LunarCalendarHelper.LunarToSolar(lunarYear, lunarMonth, isLeapMonth, lunarDay, hour, minute, second);
        if (solarDate == null) return false;

        result = solarDate.Value;
        return true;
    }

    #endregion
}
