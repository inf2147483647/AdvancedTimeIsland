using System;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 插件设置页面
/// </summary>
public class PluginSettingsPage : UserControl
{
    private static SolidColorBrush GetAccentBrush()
    {
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            return new SolidColorBrush(accentColor);
        }
        if (Application.Current?.TryFindResource("AccentColor", out var accentObj) == true && accentObj is Color accentColor2)
        {
            return new SolidColorBrush(accentColor2);
        }
        return new SolidColorBrush(Colors.DodgerBlue);
    }

    private static IBrush GetAccentTextBrush(SolidColorBrush accentBrush)
    {
        var color = accentBrush.Color;
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        return luminance > 0.6 ? Brushes.Black : Brushes.White;
    }

    private readonly PluginSettings? _settings;
    private ToggleSwitch? _easterEggToggle;
    private Border? _easterEggItem;

    public PluginSettingsPage() : this(null, null)
    {
    }

    public PluginSettingsPage(PluginSettings? settings) : this(settings, null)
    {
    }

    public PluginSettingsPage(PluginSettings? settings, LunarInstallerService? lunarInstaller)
    {
        _settings = settings;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Avalonia.Thickness(16),
            Spacing = 16
        };

        // 标题
        mainPanel.Children.Add(new TextBlock
        {
            Text = "插件设置",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        });

        // 通用设置
        mainPanel.Children.Add(new TextBlock
        {
            Text = "通用设置",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        });

        // 许可证声明
        mainPanel.Children.Add(new TextBlock
        {
            Text = "本项目基于 GNU Lesser General Public License v3.0 获得许可",
            FontSize = 12,
            Foreground = Brushes.LightGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        });

        // 启用农历功能
        mainPanel.Children.Add(CreateSettingItem(
            "启用农历功能",
            "开启后将显示农历日期",
            CreateToggleSwitch(_settings?.EnableLunarCalendar ?? true, OnLunarCalendarToggleChanged),
            null
        ));

        // 启用区时/地方时
        mainPanel.Children.Add(CreateSettingItem(
            "启用区时 / 地方时",
            "开启后将支持区时和地方时转换",
            CreateToggleSwitch(true, OnZoneLocalTimeToggleChanged),
            null
        ));

        // 地方时经度设置
        mainPanel.Children.Add(CreateSettingItem(
            "地方时经度",
            "设置地方时计算使用的经度（范围：-180 到 180）",
            CreateLongitudeTextBox(),
            null
        ));

        // 经纬度表示方式
        mainPanel.Children.Add(CreateSettingItem(
            "经纬度表示方式",
            "选择经度的显示格式",
            CreateLongitudeModeComboBox(),
            null
        ));

        // 区时时区设置
        mainPanel.Children.Add(CreateSettingItem(
            "区时时区",
            "选择区时显示使用的时区",
            CreateTimeZoneComboBox(),
            null
        ));

        // 插件时间偏移设置（与ClassIsland时间独立）
        mainPanel.Children.Add(CreateSettingItem(
            "插件时间偏移",
            "与ClassIsland时间独立，单位为秒，增大偏移抵消铃声滞后，减小偏移抵消铃声提前",
            CreateTimeOffsetTextBox(),
            null
        ));

        // 女装彩蛋（默认不可见）
        _easterEggToggle = CreateToggleSwitch(false, OnEasterEggToggleChanged);
        _easterEggToggle.IsVisible = false; // 默认不可见
        _easterEggItem = CreateSettingItem(
            "女装",
            "开启后显示女装彩蛋页面",
            _easterEggToggle,
            null
        );
        _easterEggItem.IsVisible = false; // 整个设置项默认不可见
        mainPanel.Children.Add(_easterEggItem);

        // 说明文字
        mainPanel.Children.Add(new TextBlock
        {
            Text = "注意: 修改设置后可能需要重启插件才能生效。",
            FontSize = 12,
            Foreground = Brushes.Orange,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 16, 0, 0)
        });

        var scrollViewer = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        Content = scrollViewer;
    }

    /// <summary>
    /// 创建设置项
    /// </summary>
    private Border CreateSettingItem(string title, string description, Control valueControl, Action? onValueChanged)
    {
        var itemPanel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };

        // 标题行：使用 Grid 让标题左对齐、按钮右对齐且在同一行
        var titlePanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 0);
        titlePanel.Children.Add(titleText);

        if (valueControl != null)
        {
            valueControl.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(valueControl, 1);
            titlePanel.Children.Add(valueControl);
        }

        content.Children.Add(titlePanel);

        // 描述
        if (!string.IsNullOrEmpty(description))
        {
            content.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 12,
                Foreground = Brushes.LightGray,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
        }

        itemPanel.Child = content;
        return itemPanel;
    }

    /// <summary>
    /// 创建开关控件
    /// </summary>
    private ToggleSwitch CreateToggleSwitch(bool isOn, EventHandler<RoutedEventArgs> handler)
    {
        var toggle = new ToggleSwitch
        {
            IsChecked = isOn
        };
        toggle.IsCheckedChanged += handler;
        return toggle;
    }

    /// <summary>
    /// 创建跳转按钮
    /// </summary>
    private Button CreateLinkButton(string text, EventHandler<RoutedEventArgs> handler)
    {
        var accentBrush = GetAccentBrush();
        var button = new Button
        {
            Content = $"➜ {text}",
            Padding = new Avalonia.Thickness(12, 4),
            Background = accentBrush,
            Foreground = GetAccentTextBrush(accentBrush),
            CornerRadius = new Avalonia.CornerRadius(4)
        };
        button.Click += handler;
        return button;
    }

    /// <summary>
    /// 创建时间偏移输入框（秒）
    /// </summary>
    private TextBox CreateTimeOffsetTextBox()
    {
        var textBox = new TextBox
        {
            Width = 100,
            Text = (_settings?.TimeOffsetSeconds ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            HorizontalAlignment = HorizontalAlignment.Right,
            Watermark = "0"
        };
        textBox.LostFocus += OnTimeOffsetLostFocus;
        return textBox;
    }

    /// <summary>
    /// 创建经度输入框
    /// </summary>
    private TextBox CreateLongitudeTextBox()
    {
        var textBox = new TextBox
        {
            Width = 100,
            Text = _settings?.Longitude.ToString("F4") ?? "116.4",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        textBox.LostFocus += OnLongitudeLostFocus;
        return textBox;
    }

    /// <summary>
    /// 创建经度表示方式下拉框
    /// </summary>
    private ComboBox CreateLongitudeModeComboBox()
    {
        var comboBox = new ComboBox
        {
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        comboBox.Items.Add("小数");
        comboBox.Items.Add("度分秒");

        if (_settings != null)
        {
            comboBox.SelectedIndex = _settings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal ? 0 : 1;
        }
        else
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.SelectionChanged += OnLongitudeModeSelectionChanged;
        return comboBox;
    }

    /// <summary>
    /// 创建时区下拉框
    /// </summary>
    private ComboBox CreateTimeZoneComboBox()
    {
        var comboBox = new ComboBox
        {
            Width = 200
        };

        var timeZones = TimeZoneInfo.GetSystemTimeZones();
        foreach (var tz in timeZones)
        {
            comboBox.Items.Add(tz);
        }

        if (_settings != null)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is TimeZoneInfo tz && tz.Id == _settings.TimeZoneId)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        comboBox.SelectionChanged += OnTimeZoneSelectionChanged;
        comboBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<TimeZoneInfo>((tz, ns) => new TextBlock { Text = tz?.DisplayName ?? "" });

        return comboBox;
    }

    /// <summary>
    /// 显示女装设置项
    /// </summary>
    public void ShowEasterEggSetting()
    {
        if (_easterEggItem != null)
        {
            _easterEggItem.IsVisible = true;
        }
        if (_easterEggToggle != null)
        {
            _easterEggToggle.IsVisible = true;
            _easterEggToggle.IsChecked = true;
        }
    }

    /// <summary>
    /// 隐藏女装设置项
    /// </summary>
    public void HideEasterEggSetting()
    {
        if (_easterEggItem != null)
        {
            _easterEggItem.IsVisible = false;
        }
        if (_easterEggToggle != null)
        {
            _easterEggToggle.IsVisible = false;
            _easterEggToggle.IsChecked = false;
        }
    }

    #region 事件处理

    private void OnLunarCalendarToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (_settings != null && sender is ToggleSwitch toggle)
        {
            _settings.EnableLunarCalendar = toggle.IsChecked == true;
        }
    }

    private void OnZoneLocalTimeToggleChanged(object? sender, RoutedEventArgs e)
    {
        // 区时/地方时功能开关
    }

    private void OnTimeOffsetLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_settings != null && sender is TextBox textBox)
        {
            if (double.TryParse(textBox.Text, out double offset))
            {
                // 限制偏移范围为 -86400 到 86400 秒（-24小时到+24小时）
                offset = Math.Max(-86400, Math.Min(86400, offset));
                _settings.TimeOffsetSeconds = offset;
                textBox.Text = offset.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                textBox.Text = _settings.TimeOffsetSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    private void OnLongitudeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_settings != null && sender is TextBox textBox)
        {
            if (double.TryParse(textBox.Text, out double longitude))
            {
                longitude = Math.Max(-180, Math.Min(180, longitude));
                _settings.Longitude = longitude;
                textBox.Text = longitude.ToString("F4");
            }
        }
    }

    private void OnLongitudeModeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox comboBox)
        {
            _settings.LongitudeDisplayMode = comboBox.SelectedIndex == 0 
                ? LongitudeDisplayMode.Decimal 
                : LongitudeDisplayMode.Dms;
        }
    }

    private void OnTimeZoneSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox comboBox && comboBox.SelectedItem is TimeZoneInfo tz)
        {
            _settings.TimeZoneId = tz.Id;
        }
    }

    public event EventHandler<bool>? EasterEggToggled;

    private void OnEasterEggToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle)
        {
            var isEnabled = toggle.IsChecked == true;
            if (!isEnabled)
            {
                HideEasterEggSetting();
            }
            EasterEggToggled?.Invoke(this, isEnabled);
        }
    }

    #endregion
}
