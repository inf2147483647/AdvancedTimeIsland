using System;
using AdvancedTimeIsland.Helpers;
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

    private static IBrush GetSyncingTextBrush()
    {
        // 正在同步时使用白色（深色主题）或黑色（浅色主题）
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            var luminance = (0.299 * accentColor.R + 0.587 * accentColor.G + 0.114 * accentColor.B) / 255.0;
            return luminance > 0.6 ? Brushes.Black : Brushes.White;
        }
        return Brushes.White;
    }

    private readonly PluginSettings? _settings;
    private ToggleSwitch? _easterEggToggle;
    private Border? _easterEggItem;
    private TextBox? _longitudeTextBox;
    private TextBox? _dmsDegreesTextBox;
    private TextBox? _dmsMinutesTextBox;
    private TextBox? _dmsSecondsTextBox;
    private ComboBox? _dmsDirectionComboBox;
    private TextBlock? _ntpTimeDisplayText;
    private TextBlock? _syncStatusText;
    private Button? _syncNowButton;
    private System.Timers.Timer? _ntpTimeDisplayTimer;

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

        // 地方时经度设置
        mainPanel.Children.Add(CreateSettingItem(
            "地方时经度",
            "设置地方时计算使用的经度（范围：-180 到 180）",
            CreateLongitudePanel(),
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

        // 时间服务器设置
        mainPanel.Children.Add(CreateSettingItem(
            "时间服务器",
            "选择用于同步时间的NTP服务器",
            CreateNtpServerComboBox(),
            null
        ));

        // 同步时间周期设置
        mainPanel.Children.Add(CreateSettingItem(
            "同步时间周期",
            "NTP时间同步周期，单位为分钟",
            CreateNtpSyncIntervalTextBox(),
            null
        ));

        // 女装彩蛋（默认不可见）
        var isEasterEggEnabled = _settings?.EnableEasterEgg ?? false;
        _easterEggToggle = CreateToggleSwitch(isEasterEggEnabled, OnEasterEggToggleChanged);
        _easterEggToggle.IsVisible = isEasterEggEnabled; // 根据保存的状态决定是否可见
        _easterEggItem = CreateSettingItem(
            "女装",
            "开启后显示女装彩蛋页面",
            _easterEggToggle,
            null
        );
        _easterEggItem.IsVisible = isEasterEggEnabled; // 根据保存的状态决定是否可见
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
    /// 创建经度输入面板（小数/度分秒切换）
    /// </summary>
    private Control CreateLongitudePanel()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };

        _longitudeTextBox = new TextBox
        {
            Width = 100,
            Text = _settings?.Longitude.ToString("F4") ?? "116.4",
            HorizontalAlignment = HorizontalAlignment.Right,
            IsVisible = _settings?.LongitudeDisplayMode != LongitudeDisplayMode.Dms
        };
        _longitudeTextBox.LostFocus += OnLongitudeLostFocus;
        panel.Children.Add(_longitudeTextBox);

        var dmsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsVisible = _settings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms
        };

        _dmsDegreesTextBox = new TextBox { Width = 50, Watermark = "度" };
        _dmsDegreesTextBox.LostFocus += OnDmsValueChanged;
        dmsPanel.Children.Add(_dmsDegreesTextBox);
        dmsPanel.Children.Add(new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center });

        _dmsMinutesTextBox = new TextBox { Width = 45, Watermark = "分" };
        _dmsMinutesTextBox.LostFocus += OnDmsValueChanged;
        dmsPanel.Children.Add(_dmsMinutesTextBox);
        dmsPanel.Children.Add(new TextBlock { Text = "′", VerticalAlignment = VerticalAlignment.Center });

        _dmsSecondsTextBox = new TextBox { Width = 45, Watermark = "秒" };
        _dmsSecondsTextBox.LostFocus += OnDmsValueChanged;
        dmsPanel.Children.Add(_dmsSecondsTextBox);
        dmsPanel.Children.Add(new TextBlock { Text = "″", VerticalAlignment = VerticalAlignment.Center });

        _dmsDirectionComboBox = new ComboBox { Width = 90 };
        _dmsDirectionComboBox.Items.Add("东经");
        _dmsDirectionComboBox.Items.Add("西经");
        _dmsDirectionComboBox.SelectedIndex = 0;
        _dmsDirectionComboBox.SelectionChanged += OnDmsValueChanged;
        dmsPanel.Children.Add(_dmsDirectionComboBox);

        panel.Children.Add(dmsPanel);

        UpdateDmsFromLongitude();

        return panel;
    }

    private void UpdateDmsFromLongitude()
    {
        if (_settings == null || _dmsDegreesTextBox == null) return;
        LongitudeConverter.DecomposeDms(_settings.Longitude, out int d, out int m, out double s, out bool isEast);
        _dmsDegreesTextBox.Text = d.ToString();
        _dmsMinutesTextBox.Text = m.ToString();
        _dmsSecondsTextBox.Text = s.ToString("F2");
        _dmsDirectionComboBox!.SelectedIndex = isEast ? 0 : 1;
    }

    private void OnDmsValueChanged(object? sender, EventArgs e)
    {
        if (_settings == null || _dmsDegreesTextBox == null) return;
        if (!int.TryParse(_dmsDegreesTextBox.Text, out int d)) d = 0;
        if (!int.TryParse(_dmsMinutesTextBox?.Text, out int m)) m = 0;
        if (!double.TryParse(_dmsSecondsTextBox?.Text, out double s)) s = 0;
        var isEast = _dmsDirectionComboBox?.SelectedIndex == 0;
        if (LongitudeConverter.TryParseDms(d, m, s, isEast, out double lon))
        {
            _settings.Longitude = lon;
            _longitudeTextBox!.Text = LongitudeConverter.ToDecimalString(lon);
        }
        else
        {
            UpdateDmsFromLongitude();
        }
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
    /// 创建时间服务器下拉框（包含同步时间显示）
    /// </summary>
    private Control CreateNtpServerComboBox()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        // 同步时间显示文本（放在左侧）
        _ntpTimeDisplayText = new TextBlock
        {
            FontSize = 24, // 字号改为原来的2倍
            VerticalAlignment = VerticalAlignment.Center,
            Text = "正在获取时间..."
        };
        panel.Children.Add(_ntpTimeDisplayText);

        var comboBox = new ComboBox
        {
            Width = 280,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        comboBox.Items.Add("ntp.aliyun.com");
        comboBox.Items.Add("1ntp.aliyun.com");
        comboBox.Items.Add("cn.ntp.org.cn");
        comboBox.Items.Add("pool.ntp.org");
        comboBox.Items.Add("time.windows.com");

        if (_settings != null)
        {
            comboBox.SelectedItem = _settings.NtpServer;
        }
        else
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.SelectionChanged += OnNtpServerSelectionChanged;
        panel.Children.Add(comboBox);

        // 启动定时器，每0.1秒刷新时间显示
        StartNtpTimeDisplayTimer();

        return panel;
    }

    private void StartNtpTimeDisplayTimer()
    {
        _ntpTimeDisplayTimer?.Dispose();
        _ntpTimeDisplayTimer = new System.Timers.Timer(100); // 0.1秒
        _ntpTimeDisplayTimer.Elapsed += (s, e) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_ntpTimeDisplayText != null)
                {
                    // 使用原始服务器时间（不经过插件偏移）
                    var ntpTime = TimeBaseService.Instance?.GetRawServerTime() ?? DateTime.Now;
                    var weekDay = GetChineseWeekDay(ntpTime);
                    _ntpTimeDisplayText.Text = $"{ntpTime:yyyy-MM-dd-HH-mm-ss} {weekDay}";
                }
            });
        };
        _ntpTimeDisplayTimer.Start();
    }

    private static string GetChineseWeekDay(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Sunday => "周日",
            DayOfWeek.Monday => "周一",
            DayOfWeek.Tuesday => "周二",
            DayOfWeek.Wednesday => "周三",
            DayOfWeek.Thursday => "周四",
            DayOfWeek.Friday => "周五",
            DayOfWeek.Saturday => "周六",
            _ => ""
        };
    }

    /// <summary>
    /// 创建同步时间周期输入框（包含立即同步按钮和状态提示）
    /// </summary>
    private Control CreateNtpSyncIntervalTextBox()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };

        // 输入框和按钮的行
        var inputRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        var textBox = new TextBox
        {
            Width = 350,
            HorizontalAlignment = HorizontalAlignment.Right,
            Text = _settings?.NtpSyncIntervalMinutes.ToString() ?? "5"
        };
        textBox.LostFocus += OnNtpSyncIntervalLostFocus;
        inputRow.Children.Add(textBox);

        // 立即同步按钮
        _syncNowButton = new Button
        {
            Content = "立即同步时间",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _syncNowButton.Click += OnSyncNowButtonClick;
        inputRow.Children.Add(_syncNowButton);

        panel.Children.Add(inputRow);

        // 状态提示文本（在按钮正下方）
        _syncStatusText = new TextBlock
        {
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            IsVisible = true
        };
        panel.Children.Add(_syncStatusText);

        var hint = new TextBlock
        {
            Text = "请输入不小于1的整数，单位为分钟",
            FontSize = 11,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(hint);

        // 订阅TimeBaseService的同步事件
        if (TimeBaseService.Instance != null)
        {
            TimeBaseService.Instance.SyncStatusChanged += OnSyncStatusChanged;
        }

        // 显示上次同步状态
        ShowLastSyncStatus();

        return panel;
    }

    private async void OnSyncNowButtonClick(object? sender, RoutedEventArgs e)
    {
        if (TimeBaseService.Instance != null)
        {
            await TimeBaseService.Instance.SyncTimeNowAsync();
        }
    }

    private void OnSyncStatusChanged(object? sender, SyncStatusEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateSyncStatusText(e.Status, e.SyncTime);
        });
    }

    private void UpdateSyncStatusText(SyncStatus status, DateTime? syncTime)
    {
        if (_syncStatusText == null) return;

        _syncStatusText.IsVisible = true;

        switch (status)
        {
            case SyncStatus.Syncing:
                _syncStatusText.Text = "正在同步时间...";
                _syncStatusText.Foreground = GetSyncingTextBrush();
                break;

            case SyncStatus.Success:
                if (syncTime.HasValue)
                {
                    _syncStatusText.Text = $"成功在[{syncTime:yyyy-MM-dd-HH-mm-ss}]同步时间";
                }
                else
                {
                    _syncStatusText.Text = "同步时间成功";
                }
                _syncStatusText.Foreground = GetSuccessForegroundBrush();
                break;

            case SyncStatus.Failed:
                _syncStatusText.Text = "同步时间失败！";
                _syncStatusText.Foreground = Brushes.Red;
                break;
        }
    }

    private void ShowLastSyncStatus()
    {
        if (_syncStatusText == null || _settings == null) return;

        if (_settings.LastSyncTime.HasValue && !string.IsNullOrEmpty(_settings.LastSyncStatus))
        {
            if (_settings.LastSyncStatus == "Success")
            {
                _syncStatusText.Text = $"成功在[{_settings.LastSyncTime:yyyy-MM-dd-HH-mm-ss}]同步时间";
                _syncStatusText.Foreground = GetSuccessForegroundBrush();
            }
            else if (_settings.LastSyncStatus == "Failed")
            {
                _syncStatusText.Text = $"在[{_settings.LastSyncTime:yyyy-MM-dd-HH-mm-ss}]同步时间失败";
                _syncStatusText.Foreground = Brushes.Red;
            }
        }
        else
        {
            _syncStatusText.Text = "尚未同步时间";
            _syncStatusText.Foreground = Brushes.Gray;
        }
    }

    private SolidColorBrush GetSuccessForegroundBrush()
    {
        // 浅色主题用深绿色，深色主题用浅绿色
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            var luminance = (0.299 * accentColor.R + 0.587 * accentColor.G + 0.114 * accentColor.B) / 255.0;
            return luminance > 0.6 ? new SolidColorBrush(Color.FromRgb(0, 128, 0)) : new SolidColorBrush(Color.FromRgb(144, 238, 144));
        }
        // 默认使用浅绿色
        return new SolidColorBrush(Color.FromRgb(144, 238, 144));
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

    private void OnNtpServerSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox comboBox && comboBox.SelectedItem is string server)
        {
            _settings.NtpServer = server;
        }
    }

    private void OnNtpSyncIntervalLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_settings != null && sender is TextBox textBox)
        {
            int value;
            if (!int.TryParse(textBox.Text, out value))
            {
                value = 5;
            }
            else
            {
                value = (int)Math.Round((double)value);
                if (value < 1)
                {
                    value = 1;
                }
            }
            _settings.NtpSyncIntervalMinutes = value;
            textBox.Text = value.ToString();
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
                UpdateDmsFromLongitude();
            }
        }
    }

    private void OnLongitudeModeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox comboBox)
        {
            var isDms = comboBox.SelectedIndex == 1;
            _settings.LongitudeDisplayMode = isDms
                ? LongitudeDisplayMode.Dms
                : LongitudeDisplayMode.Decimal;
            if (_longitudeTextBox != null)
            {
                _longitudeTextBox.IsVisible = !isDms;
            }
            if (_dmsDegreesTextBox != null && _dmsDegreesTextBox.Parent is Control dmsPanel)
            {
                dmsPanel.IsVisible = isDms;
                if (isDms)
                {
                    UpdateDmsFromLongitude();
                }
            }
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
            _settings!.EnableEasterEgg = isEnabled;
            if (!isEnabled)
            {
                HideEasterEggSetting();
            }
            EasterEggToggled?.Invoke(this, isEnabled);
        }
    }

    #endregion
}



