using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
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
    private TextBox? _beijingYearTextBox;
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
    private TextBox? _zoneYearTextBox;
    private ComboBox? _zoneMonthComboBox;
    private ComboBox? _zoneDayComboBox;
    private ComboBox? _zoneHourComboBox;
    private ComboBox? _zoneMinuteComboBox;
    private ComboBox? _zoneSecondComboBox;
    private ComboBox? _zoneComboBox;
    private TextBlock? _zoneResultTextBlock;

    // 地方时转换模块控件
    private TextBox? _localYearTextBox;
    private ComboBox? _localMonthComboBox;
    private ComboBox? _localDayComboBox;
    private ComboBox? _localHourComboBox;
    private ComboBox? _localMinuteComboBox;
    private ComboBox? _localSecondComboBox;
    private TextBox? _localLongitudeTextBox;
    private TextBox? _localLongitudeDmsDegreesTextBox;
    private TextBox? _localLongitudeDmsMinutesTextBox;
    private TextBox? _localLongitudeDmsSecondsTextBox;
    private ComboBox? _localLongitudeDmsDirectionComboBox;
    private Panel? _localLongitudeDmsPanel;
    private TextBlock? _localResultTextBlock;

    // 提示文本定时器（用于自动清除提示）
    private readonly Dictionary<TextBlock, System.Timers.Timer> _resultTimers = new();

    // 夏令时开关
    private CheckBox? _zoneDstCheckBox;
    private CheckBox? _localDstCheckBox;

    // 插件设置（用于获取经度显示方式）
    private readonly PluginSettings? _settings;

    // 天干列表
    private readonly string[] _tiangan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };

    // 地支列表
    private readonly string[] _dizhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    // 时区列表
    private readonly Dictionary<string, double> _timeZones = new()
    {
        { "(UTC-12:00) 贝克岛", -12.0 },
        { "(UTC-11:00) 美属萨摩亚", -11.0 },
        { "(UTC-10:00) 夏威夷", -10.0 },
        { "(UTC-09:30) 马克萨斯群岛", -9.5 },
        { "(UTC-09:00) 阿拉斯加", -9.0 },
        { "(UTC-08:00) 太平洋时间", -8.0 },
        { "(UTC-07:00) 山地时间", -7.0 },
        { "(UTC-06:00) 中部时间", -6.0 },
        { "(UTC-05:00) 东部时间", -5.0 },
        { "(UTC-04:00) 大西洋时间", -4.0 },
        { "(UTC-03:30) 纽芬兰", -3.5 },
        { "(UTC-03:00) 巴西利亚", -3.0 },
        { "(UTC-02:00) 南乔治亚岛", -2.0 },
        { "(UTC-01:00) 亚速尔群岛", -1.0 },
        { "(UTC±00:00) 伦敦", 0.0 },
        { "(UTC+01:00) 巴黎", 1.0 },
        { "(UTC+02:00) 开罗", 2.0 },
        { "(UTC+03:00) 莫斯科", 3.0 },
        { "(UTC+03:30) 德黑兰", 3.5 },
        { "(UTC+04:00) 迪拜", 4.0 },
        { "(UTC+04:30) 喀布尔", 4.5 },
        { "(UTC+05:00) 伊斯兰堡", 5.0 },
        { "(UTC+05:30) 孟买", 5.5 },
        { "(UTC+05:45) 加德满都", 5.75 },
        { "(UTC+06:00) 达卡", 6.0 },
        { "(UTC+06:30) 仰光", 6.5 },
        { "(UTC+07:00) 曼谷", 7.0 },
        { "(UTC+08:00) 北京", 8.0 },
        { "(UTC+08:30) 科科斯群岛", 8.5 },
        { "(UTC+08:45) 尤克拉", 8.75 },
        { "(UTC+09:00) 东京", 9.0 },
        { "(UTC+09:30) 达尔文", 9.5 },
        { "(UTC+10:00) 悉尼", 10.0 },
        { "(UTC+10:30) 豪勋爵岛", 10.5 },
        { "(UTC+11:00) 新喀里多尼亚", 11.0 },
        { "(UTC+11:30) 诺福克岛", 11.5 },
        { "(UTC+12:00) 奥克兰", 12.0 },
        { "(UTC+12:45) 查塔姆群岛", 12.75 },
        { "(UTC+13:00) 斐济", 13.0 },
        { "(UTC+14:00) 基里巴斯", 14.0 }
    };

    public TimeConverterPage() : this(null)
    {
    }

    public TimeConverterPage(PluginSettings? settings)
    {
        _settings = settings;
        if (_settings != null)
        {
            _settings.PropertyChanged += OnSettingsPropertyChanged;
        }
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

        // 注意事项（放在导航栏下方，转换模块上方）
        mainPanel.Children.Add(CreateNotesPanel());

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
        var datePanel = CreateDatePickerRow("日期输入:", out _beijingYearTextBox, out _beijingMonthComboBox, out _beijingDayComboBox);
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
        _lunarYearRangeComboBox.Items.Add("1901-1923");
        _lunarYearRangeComboBox.Items.Add("1924-1983");
        _lunarYearRangeComboBox.Items.Add("1984-2043");
        _lunarYearRangeComboBox.Items.Add("2044-2101");
        _lunarYearRangeComboBox.SelectedIndex = 2; // 默认1984-2043
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
        var datePanel = CreateDatePickerRow("日期输入:", out _zoneYearTextBox, out _zoneMonthComboBox, out _zoneDayComboBox);
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

        _zoneComboBox = new ComboBox { Width = 200 };
        foreach (var zone in _timeZones.Keys)
        {
            _zoneComboBox.Items.Add(zone);
        }
        _zoneComboBox.SelectedIndex = 15; // 默认选中中时区(UTC±00:00)
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
        var datePanel = CreateDatePickerRow("日期输入:", out _localYearTextBox, out _localMonthComboBox, out _localDayComboBox);
        content.Children.Add(datePanel);

        // 时间选择
        var timePanel = CreateTimePickerRow("时间:", out _localHourComboBox, out _localMinuteComboBox, out _localSecondComboBox);
        content.Children.Add(timePanel);

        // 经度输入
        var lonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        lonPanel.Children.Add(new TextBlock
        {
            Text = "经度:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        var isDms = _settings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms;
        
        _localLongitudeTextBox = new TextBox { Width = 100, Watermark = "如：116.4", IsVisible = !isDms };
        _localLongitudeTextBox.Text = "116.4";
        _localLongitudeTextBox.LostFocus += OnLongitudeTextBoxLostFocus;
        
        _localLongitudeDmsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, IsVisible = isDms };

        _localLongitudeDmsDegreesTextBox = new TextBox { Width = 50, Watermark = "度" };
        _localLongitudeDmsDegreesTextBox.LostFocus += OnLongitudeDmsValueChanged;
        _localLongitudeDmsPanel.Children.Add(_localLongitudeDmsDegreesTextBox);
        _localLongitudeDmsPanel.Children.Add(new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center });

        _localLongitudeDmsMinutesTextBox = new TextBox { Width = 45, Watermark = "分" };
        _localLongitudeDmsMinutesTextBox.LostFocus += OnLongitudeDmsValueChanged;
        _localLongitudeDmsPanel.Children.Add(_localLongitudeDmsMinutesTextBox);
        _localLongitudeDmsPanel.Children.Add(new TextBlock { Text = "′", VerticalAlignment = VerticalAlignment.Center });

        _localLongitudeDmsSecondsTextBox = new TextBox { Width = 45, Watermark = "秒" };
        _localLongitudeDmsSecondsTextBox.LostFocus += OnLongitudeDmsValueChanged;
        _localLongitudeDmsPanel.Children.Add(_localLongitudeDmsSecondsTextBox);
        _localLongitudeDmsPanel.Children.Add(new TextBlock { Text = "″", VerticalAlignment = VerticalAlignment.Center });

        _localLongitudeDmsDirectionComboBox = new ComboBox { Width = 90 };
        _localLongitudeDmsDirectionComboBox.Items.Add("东经");
        _localLongitudeDmsDirectionComboBox.Items.Add("西经");
        _localLongitudeDmsDirectionComboBox.SelectedIndex = 0;
        _localLongitudeDmsDirectionComboBox.SelectionChanged += OnLongitudeDmsValueChanged;
        _localLongitudeDmsPanel.Children.Add(_localLongitudeDmsDirectionComboBox);
        
        lonPanel.Children.Add(_localLongitudeTextBox);
        lonPanel.Children.Add(_localLongitudeDmsPanel);
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

    private StackPanel CreateDatePickerRow(string label, out TextBox yearTextBox, out ComboBox monthComboBox, out ComboBox dayComboBox)
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

        // 年份输入框
        yearTextBox = new TextBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            Watermark = "年"
        };
        yearTextBox.LostFocus += OnYearTextBoxLostFocus;
        datePanel.Children.Add(yearTextBox);

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
        var textBlock = new TextBlock
        {
            Text = "",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.LightGreen,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        // 为每个TextBlock创建定时器
        var timer = new System.Timers.Timer(5000); // 5秒后自动清除
        timer.Elapsed += (s, e) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                textBlock.Text = "";
            });
            timer.Stop();
        };
        timer.AutoReset = false;
        _resultTimers[textBlock] = timer;

        return textBlock;
    }

    private void SetResultText(TextBlock? textBlock, string message)
    {
        if (textBlock == null) return;

        // 先停止之前的定时器
        if (_resultTimers.TryGetValue(textBlock, out var existingTimer))
        {
            existingTimer.Stop();
        }

        textBlock.Text = message;

        // 启动新的定时器，5秒后清除提示
        if (_resultTimers.TryGetValue(textBlock, out var timer))
        {
            timer.Start();
        }
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
            "• 经度处填正数为东经，负数为西经，取值范围为 (-180, 180]",
            "• 在1582年之前使用儒略历，导致日期误差累积，为了纠正之前的误差，格里高利历把1582.10.4下一天变为1584.10.15，看起来中间“消失”了10天"
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
        if (_beijingYearTextBox != null) _beijingYearTextBox.Text = "";
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

        if (_zoneYearTextBox != null) _zoneYearTextBox.Text = "";
        if (_zoneMonthComboBox != null) _zoneMonthComboBox.SelectedIndex = -1;
        if (_zoneDayComboBox != null) _zoneDayComboBox.SelectedIndex = -1;
        if (_zoneHourComboBox != null) _zoneHourComboBox.SelectedIndex = -1;
        if (_zoneMinuteComboBox != null) _zoneMinuteComboBox.SelectedIndex = -1;
        if (_zoneSecondComboBox != null) _zoneSecondComboBox.SelectedIndex = -1;
        if (_zoneComboBox != null) _zoneComboBox.SelectedIndex = -1;
        if (_zoneResultTextBlock != null) _zoneResultTextBlock.Text = "";

        if (_localYearTextBox != null) _localYearTextBox.Text = "";
        if (_localMonthComboBox != null) _localMonthComboBox.SelectedIndex = -1;
        if (_localDayComboBox != null) _localDayComboBox.SelectedIndex = -1;
        if (_localHourComboBox != null) _localHourComboBox.SelectedIndex = -1;
        if (_localMinuteComboBox != null) _localMinuteComboBox.SelectedIndex = -1;
        if (_localSecondComboBox != null) _localSecondComboBox.SelectedIndex = -1;
        if (_localLongitudeTextBox != null) _localLongitudeTextBox.Text = "";
        if (_localResultTextBlock != null) _localResultTextBlock.Text = "";
    }

    /// <summary>
    /// 年份输入框失去焦点时验证并修正输入值
    /// </summary>
    private void OnYearTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var text = textBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text))
            {
                textBox.Text = "2026";
                return;
            }

            // 尝试解析数字
            if (double.TryParse(text, out var value))
            {
                // 四舍五入到整数
                var intValue = (int)Math.Round(value);
                // 限制范围 1-9999
                if (intValue < 1) intValue = 1;
                if (intValue > 9999) intValue = 9999;
                textBox.Text = intValue.ToString();
            }
            else
            {
                // 无效输入，恢复为2026
                textBox.Text = "2026";
            }
        }
    }

    private void OnLongitudeTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var text = textBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text))
            {
                textBox.Text = "120";
                return;
            }

            if (double.TryParse(text, out var value))
            {
                value = Math.Round(value, 4);
                if (value < -180) value = -180;
                if (value > 180) value = 180;
                textBox.Text = value.ToString("F4");
            }
            else
            {
                textBox.Text = "120";
            }
        }
    }

    private void OnBeijingCurrentTimeClick(object? sender, RoutedEventArgs e)
    {
        var now = Plugin.GetCurrentTime();
        _beijingYearTextBox!.Text = now.Year.ToString();
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
            SetResultText(_beijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }
        var timestamp = UnixTimeHelper.ToUnixTimestamp(dt);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnBeijingToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            SetResultText(_beijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }
        if (!LunarCalendarHelper.IsDateSupported(dt))
        {
            SetResultText(_beijingResultTextBlock, "农历不支持此日期范围(1901-02-19 ~ 2101-01-28)");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(dt);
        SetResultText(_beijingResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(dt);
    }

    private void FillLunarComboBoxes(DateTime dt)
    {
        var lunarYear = LunarCalendarHelper.GetLunarYear(dt);
        var lunarMonth = LunarCalendarHelper.GetLunarMonth(dt);
        var lunarDay = LunarCalendarHelper.GetLunarDay(dt);
        var isLeapMonth = LunarCalendarHelper.IsLeapMonth(dt);

        // 如果农历年份为0，说明超出支持范围
        if (lunarYear == 0)
        {
            SetResultText(_lunarResultTextBlock, "转换结果超出有效范围(1901-02-19 ~ 2101-01-28)");
            return;
        }

        var tiangan = LunarCalendarHelper.GetTiangan(lunarYear);
        var dizhi = LunarCalendarHelper.GetDizhi(lunarYear);

        // 设置年份范围
        if (_lunarYearRangeComboBox != null)
        {
            if (lunarYear >= 1901 && lunarYear <= 1923)
                _lunarYearRangeComboBox.SelectedItem = "1901-1923";
            else if (lunarYear >= 1924 && lunarYear <= 1983)
                _lunarYearRangeComboBox.SelectedItem = "1924-1983";
            else if (lunarYear >= 1984 && lunarYear <= 2043)
                _lunarYearRangeComboBox.SelectedItem = "1984-2043";
            else if (lunarYear >= 2044 && lunarYear <= 2101)
                _lunarYearRangeComboBox.SelectedItem = "2044-2101";
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

        // 设置日期（自动修正非法日期）
        if (_lunarDayComboBox != null)
        {
            var safeDay = ValidateAndFixDay(dt.Year, dt.Month, lunarDay);
            _lunarDayComboBox.SelectedItem = $"{safeDay}日";
        }

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
            SetResultText(_beijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            SetResultText(_beijingResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(offset - 8 + dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_beijingResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        _zoneYearTextBox!.Text = zoneTime.Year.ToString();
        _zoneMonthComboBox!.SelectedItem = $"{zoneTime.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{safeDay}日";
        _zoneHourComboBox!.SelectedItem = zoneTime.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = zoneTime.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = zoneTime.Second.ToString("D2");
    }

    private void OnBeijingToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            SetResultText(_beijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            SetResultText(_beijingResultTextBlock, "请输入有效的经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var dstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(offsetSeconds + dstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_beijingResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        _localYearTextBox!.Text = localTime.Year.ToString();
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{safeDay}日";
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
            SetResultText(_unixResultTextBlock, "请输入有效的时间戳（整数）");
            return;
        }
        DateTime dt;
        try
        {
            dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_unixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(dt.DayOfWeek);
        var safeDay = ValidateAndFixDay(dt.Year, dt.Month, dt.Day);
        _beijingYearTextBox!.Text = dt.Year.ToString();
        _beijingMonthComboBox!.SelectedItem = $"{dt.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{safeDay}日";
        _beijingHourComboBox!.SelectedItem = dt.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = dt.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = dt.Second.ToString("D2");
    }

    private void OnUnixToLunar(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            SetResultText(_unixResultTextBlock, "请输入有效的时间戳（整数）");
            return;
        }
        DateTime dt;
        try
        {
            dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_unixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(dt);
        SetResultText(_unixResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(dt);
    }

    private void OnUnixToZone(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            SetResultText(_unixResultTextBlock, "请输入有效的时间戳（整数）");
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            SetResultText(_unixResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        DateTime dt;
        try
        {
            dt = UnixTimeHelper.FromUnixTimestampUtc(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_unixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(offset + dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_unixResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        _zoneYearTextBox!.Text = zoneTime.Year.ToString();
        _zoneMonthComboBox!.SelectedItem = $"{zoneTime.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{safeDay}日";
        _zoneHourComboBox!.SelectedItem = zoneTime.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = zoneTime.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = zoneTime.Second.ToString("D2");
    }

    private void OnUnixToLocal(object? sender, RoutedEventArgs e)
    {
        if (!long.TryParse(_unixInputTextBox?.Text, out var timestamp))
        {
            SetResultText(_unixResultTextBlock, "请输入有效的时间戳（整数）");
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            SetResultText(_unixResultTextBlock, "请输入有效的经度");
            return;
        }
        DateTime dt;
        try
        {
            dt = UnixTimeHelper.FromUnixTimestamp(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_unixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(offsetSeconds + localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_unixResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        _localYearTextBox!.Text = localTime.Year.ToString();
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{safeDay}日";
        _localHourComboBox!.SelectedItem = localTime.Hour.ToString("D2");
        _localMinuteComboBox!.SelectedItem = localTime.Minute.ToString("D2");
        _localSecondComboBox!.SelectedItem = localTime.Second.ToString("D2");
    }

    private void OnLunarToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(_lunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(dt.DayOfWeek);
        var safeDay = ValidateAndFixDay(dt.Year, dt.Month, dt.Day);
        _beijingYearTextBox!.Text = dt.Year.ToString();
        _beijingMonthComboBox!.SelectedItem = $"{dt.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{safeDay}日";
        _beijingHourComboBox!.SelectedItem = dt.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = dt.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = dt.Second.ToString("D2");
    }

    private void OnLunarToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(_lunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }
        var timestamp = UnixTimeHelper.ToUnixTimestamp(dt);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnLunarToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(_lunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            SetResultText(_lunarResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(offset - 8 + dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_lunarResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        _zoneYearTextBox!.Text = zoneTime.Year.ToString();
        _zoneMonthComboBox!.SelectedItem = $"{zoneTime.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{safeDay}日";
        _zoneHourComboBox!.SelectedItem = zoneTime.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = zoneTime.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = zoneTime.Second.ToString("D2");
    }

    private void OnLunarToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(_lunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            SetResultText(_lunarResultTextBlock, "请输入有效的经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var dstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(offsetSeconds + dstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_lunarResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        _localYearTextBox!.Text = localTime.Year.ToString();
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{safeDay}日";
        _localHourComboBox!.SelectedItem = localTime.Hour.ToString("D2");
        _localMinuteComboBox!.SelectedItem = localTime.Minute.ToString("D2");
        _localSecondComboBox!.SelectedItem = localTime.Second.ToString("D2");
    }

    private void OnZoneToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(_zoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        // 验证结果年份在有效范围内
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(8 - offset - dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_zoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        if (beijingTime.Year < 1 || beijingTime.Year > 9999)
        {
            SetResultText(_zoneResultTextBlock, "转换结果超出有效范围(1-9999年)");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(beijingTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(beijingTime.Year, beijingTime.Month, beijingTime.Day);
        _beijingYearTextBox!.Text = beijingTime.Year.ToString();
        _beijingMonthComboBox!.SelectedItem = $"{beijingTime.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{safeDay}日";
        _beijingHourComboBox!.SelectedItem = beijingTime.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = beijingTime.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = beijingTime.Second.ToString("D2");
    }

    private void OnZoneToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(_zoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        DateTime utcTime;
        try
        {
            utcTime = DateTime.SpecifyKind(dt.AddHours(-offset - dstOffset), DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_zoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var timestamp = UnixTimeHelper.ToUnixTimestamp(utcTime);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnZoneToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(_zoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        var dstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(8 - offset - dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_zoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(beijingTime);
        SetResultText(_zoneResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(beijingTime);
    }

    private void OnZoneToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(_zoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        if (!TryParseLongitude())
        {
            SetResultText(_zoneResultTextBlock, "请输入有效的经度");
            return;
        }
        var zoneDstOffset = (_zoneDstCheckBox?.IsChecked == true) ? 1 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(8 - offset - zoneDstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_zoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var longitude = double.Parse(_localLongitudeTextBox!.Text ?? "0");
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(beijingTime, TimeSpan.FromSeconds(offsetSeconds + localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_zoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        // 验证结果年份在有效范围内
        if (localTime.Year < 1 || localTime.Year > 9999)
        {
            SetResultText(_zoneResultTextBlock, "转换结果超出有效范围(1-9999年)");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(localTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        _localYearTextBox!.Text = localTime.Year.ToString();
        _localMonthComboBox!.SelectedItem = $"{localTime.Month}月";
        _localDayComboBox!.SelectedItem = $"{safeDay}日";
        _localHourComboBox!.SelectedItem = localTime.Hour.ToString("D2");
        _localMinuteComboBox!.SelectedItem = localTime.Minute.ToString("D2");
        _localSecondComboBox!.SelectedItem = localTime.Second.ToString("D2");
    }

    private void OnLocalToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(_localResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(-offsetSeconds - localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_localResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(beijingTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(beijingTime.Year, beijingTime.Month, beijingTime.Day);
        _beijingYearTextBox!.Text = beijingTime.Year.ToString();
        _beijingMonthComboBox!.SelectedItem = $"{beijingTime.Month}月";
        _beijingDayComboBox!.SelectedItem = $"{safeDay}日";
        _beijingHourComboBox!.SelectedItem = beijingTime.Hour.ToString("D2");
        _beijingMinuteComboBox!.SelectedItem = beijingTime.Minute.ToString("D2");
        _beijingSecondComboBox!.SelectedItem = beijingTime.Second.ToString("D2");
    }

    private void OnLocalToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(_localResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(-offsetSeconds - localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_localResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var timestamp = UnixTimeHelper.ToUnixTimestamp(beijingTime);
        _unixInputTextBox!.Text = timestamp.ToString();
    }

    private void OnLocalToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(_localResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(-offsetSeconds - localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_localResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(beijingTime);
        SetResultText(_localResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(beijingTime);
    }

    private void OnLocalToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(_localResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        if (_zoneComboBox == null || _zoneComboBox.SelectedItem == null)
        {
            SetResultText(_localResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = _zoneComboBox.SelectedItem.ToString();
        var zoneOffset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var offsetSeconds = (longitude - 120) * 240;
        var dstOffsetSeconds = (_localDstCheckBox?.IsChecked == true) ? 3600 : 0;
        var totalOffsetSeconds = (zoneOffset - 8) * 3600 + dstOffsetSeconds - offsetSeconds;
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(totalOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(_localResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var dayOfWeek = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(zoneTime.DayOfWeek);
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        _zoneYearTextBox!.Text = zoneTime.Year.ToString();
        _zoneMonthComboBox!.SelectedItem = $"{zoneTime.Month}月";
        _zoneDayComboBox!.SelectedItem = $"{safeDay}日";
        _zoneHourComboBox!.SelectedItem = zoneTime.Hour.ToString("D2");
        _zoneMinuteComboBox!.SelectedItem = zoneTime.Minute.ToString("D2");
        _zoneSecondComboBox!.SelectedItem = zoneTime.Second.ToString("D2");
    }

    #endregion

    #region 辅助方法

    private bool TryParseBeijingDateTime(out DateTime result)
    {
        result = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(_beijingYearTextBox?.Text) ||
            _beijingMonthComboBox?.SelectedItem == null ||
            _beijingDayComboBox?.SelectedItem == null)
            return false;

        if (!int.TryParse(_beijingYearTextBox.Text, out var year))
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

        // 校验日期是否合法，并自动修正为合法日期
        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;
        if (day > DateTime.DaysInMonth(year, month))
            return false;
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(year, month, day))
            return false;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private bool TryParseZoneDateTime(out DateTime result, out double offset)
    {
        result = DateTime.MinValue;
        offset = 0;

        if (string.IsNullOrWhiteSpace(_zoneYearTextBox?.Text) ||
            _zoneMonthComboBox?.SelectedItem == null ||
            _zoneDayComboBox?.SelectedItem == null)
            return false;

        if (!int.TryParse(_zoneYearTextBox.Text, out var year))
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

        var zoneName = _zoneComboBox?.SelectedItem?.ToString() ?? "(UTC±00:00) 伦敦";

        offset = _timeZones.GetValueOrDefault(zoneName, 0);

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;
        if (day > DateTime.DaysInMonth(year, month))
            return false;
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(year, month, day))
            return false;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private bool TryParseLocalDateTime(out DateTime result, out double longitude)
    {
        result = DateTime.MinValue;
        longitude = 0;

        if (string.IsNullOrWhiteSpace(_localYearTextBox?.Text) ||
            _localMonthComboBox?.SelectedItem == null ||
            _localDayComboBox?.SelectedItem == null)
            return false;

        if (!int.TryParse(_localYearTextBox.Text, out var year))
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

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;
        if (day > DateTime.DaysInMonth(year, month))
            return false;
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(year, month, day))
            return false;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        try
        {
            result = new DateTime(year, month, day, hour, minute, second);
            return true;
        }
        catch (ArgumentOutOfRangeException)
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
        
        if (_settings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms)
        {
            if (!int.TryParse(_localLongitudeDmsDegreesTextBox?.Text, out int d)) d = 0;
            if (!int.TryParse(_localLongitudeDmsMinutesTextBox?.Text, out int m)) m = 0;
            if (!double.TryParse(_localLongitudeDmsSecondsTextBox?.Text, out double s)) s = 0;
            var isEast = _localLongitudeDmsDirectionComboBox?.SelectedIndex == 0;
            return LongitudeConverter.TryParseDms(d, m, s, isEast, out result);
        }
        else
        {
            var text = _localLongitudeTextBox?.Text ?? "";
            if (string.IsNullOrWhiteSpace(text)) return false;
            return LongitudeConverter.TryParseDecimal(text, out result);
        }
    }

    private void OnLongitudeDmsValueChanged(object? sender, EventArgs e)
    {
        if (!int.TryParse(_localLongitudeDmsDegreesTextBox?.Text, out int d)) d = 0;
        if (!int.TryParse(_localLongitudeDmsMinutesTextBox?.Text, out int m)) m = 0;
        if (!double.TryParse(_localLongitudeDmsSecondsTextBox?.Text, out double s)) s = 0;
        var isEast = _localLongitudeDmsDirectionComboBox?.SelectedIndex == 0;
        if (LongitudeConverter.TryParseDms(d, m, s, isEast, out var lon))
        {
            _localLongitudeTextBox!.Text = LongitudeConverter.ToDecimalString(lon);
        }
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

        var baseYear = 4;
        var yearOffset = 0;
        while ((baseYear + yearOffset - 4) % 10 != tianganIndex ||
               (baseYear + yearOffset - 4) % 12 != dizhiIndex)
        {
            yearOffset++;
            if (yearOffset > 60) return false;
        }

        var baseLunarYear = baseYear + yearOffset;
        var lunarYear = baseLunarYear;

        while (lunarYear < startYear)
        {
            lunarYear += 60;
        }
        if (lunarYear > endYear)
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

        // 验证转换结果年份在有效范围内(1-9999)
        if (solarDate.Value.Year < 1 || solarDate.Value.Year > 9999) return false;

        // 验证转换结果不在1582年10月5-14日（历史上不存在的日期）
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(solarDate.Value)) return false;

        result = solarDate.Value;
        return true;
    }

    /// <summary>
    /// 验证并调整日期，如果日期无效则自动调整为该月最后一天
    /// </summary>
    private int ValidateAndFixDay(int year, int month, int day)
    {
        // 获取该月的最后一天
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return Math.Min(day, daysInMonth);
    }

    /// <summary>
    /// 尝试使用安全的日期解析，非法日期自动调整为合法日期
    /// </summary>
    private bool TryParseSafeDateTime(int year, int month, int day, int hour, int minute, int second, out DateTime result)
    {
        result = DateTime.MinValue;

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;

        if (DateValidationHelper.IsInvalidGregorianTransitionDate(year, month, day))
            return false;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        if (day > daysInMonth)
            day = daysInMonth;

        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

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

    #endregion

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.LongitudeDisplayMode))
        {
            UpdateLongitudeDisplay();
        }
    }

    private void UpdateLongitudeDisplay()
    {
        if (_settings == null || _localLongitudeTextBox == null || _localLongitudeDmsPanel == null)
            return;

        var currentLongitude = 116.4;
        if (_settings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal)
        {
            if (LongitudeConverter.TryParseDecimal(_localLongitudeTextBox.Text ?? "", out var lon))
                currentLongitude = lon;
            else if (TryParseLongitude(out lon))
                currentLongitude = lon;
        }
        else
        {
            if (TryParseLongitude(out var lon))
                currentLongitude = lon;
            else if (LongitudeConverter.TryParseDecimal(_localLongitudeTextBox.Text ?? "", out lon))
                currentLongitude = lon;
        }

        _localLongitudeTextBox.IsVisible = _settings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal;
        _localLongitudeDmsPanel.IsVisible = _settings.LongitudeDisplayMode == LongitudeDisplayMode.Dms;

        if (_settings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal)
        {
            _localLongitudeTextBox.Text = LongitudeConverter.ToDecimalString(currentLongitude);
        }
        else
        {
            LongitudeConverter.DecomposeDms(currentLongitude, out int d, out int m, out double s, out bool isEast);
            _localLongitudeDmsDegreesTextBox!.Text = d.ToString();
            _localLongitudeDmsMinutesTextBox!.Text = m.ToString();
            _localLongitudeDmsSecondsTextBox!.Text = s.ToString("F2");
            _localLongitudeDmsDirectionComboBox!.SelectedIndex = isEast ? 0 : 1;
        }
    }
}
