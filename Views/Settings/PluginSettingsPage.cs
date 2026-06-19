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
    private readonly PluginSettings? _settings;
    private readonly LunarInstallerService? _lunarInstaller;
    private ToggleSwitch? _easterEggToggle;
    private Border? _easterEggItem;
    private Button? _lunarInstallButton;

    public PluginSettingsPage() : this(null, null)
    {
    }

    public PluginSettingsPage(PluginSettings? settings) : this(settings, null)
    {
    }

    public PluginSettingsPage(PluginSettings? settings, LunarInstallerService? lunarInstaller)
    {
        _settings = settings;
        _lunarInstaller = lunarInstaller;
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

        // 设置项列表
        mainPanel.Children.Add(CreateSettingItem(
            "启用农历功能",
            "开启后将显示农历日期",
            CreateToggleSwitch(_settings?.EnableLunarCalendar ?? true, OnLunarCalendarToggleChanged),
            null
        ));

        // 安装lunar-csharp
        var isLunarInstalled = _lunarInstaller?.IsInstalled ?? false;
        _lunarInstallButton = isLunarInstalled ? CreateInstalledButton() : CreateLinkButton("安装", OnLunarPackageClick);
        mainPanel.Children.Add(CreateSettingItem(
            "安装 lunar-csharp 的 NuGet 包",
            "农历转换功能依赖此包",
            _lunarInstallButton,
            null
        ));

        // 启用区时/地方时
        mainPanel.Children.Add(CreateSettingItem(
            "启用区时 / 地方时",
            "开启后将支持区时和地方时转换",
            CreateToggleSwitch(true, OnZoneLocalTimeToggleChanged),
            null
        ));

        // 时间基准
        mainPanel.Children.Add(CreateSettingItem(
            "时间基准",
            "选择使用ClassIsland时间还是系统时间",
            CreateTimeBaseComboBox(),
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
            Margin = new Thickness(0, 0, 0, 12)
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };

        // 标题行
        var titlePanel = new DockPanel
        {
            LastChildFill = true
        };

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };

        DockPanel.SetDock(titleText, Dock.Left);
        titlePanel.Children.Add(titleText);
        if (valueControl != null)
        {
            DockPanel.SetDock(valueControl, Dock.Right);
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
        var button = new Button
        {
            Content = $"➜ {text}",
            Padding = new Avalonia.Thickness(12, 4),
            Background = Brushes.DodgerBlue,
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(4)
        };
        button.Click += handler;
        return button;
    }

    /// <summary>
    /// 创建已安装按钮
    /// </summary>
    private Button CreateInstalledButton()
    {
        return new Button
        {
            Content = "✓ 已安装",
            Padding = new Avalonia.Thickness(12, 4),
            Background = Brushes.Green,
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(4),
            IsEnabled = false
        };
    }

    /// <summary>
    /// 创建时间基准下拉框
    /// </summary>
    private ComboBox CreateTimeBaseComboBox()
    {
        var comboBox = new ComboBox
        {
            Width = 150
        };

        comboBox.Items.Add("ClassIsland");
        comboBox.Items.Add("系统");
        comboBox.SelectedIndex = _settings?.TimeBaseMode ?? 0;
        comboBox.SelectionChanged += OnTimeBaseSelectionChanged;

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

    private async void OnLunarPackageClick(object? sender, RoutedEventArgs e)
    {
        if (_lunarInstallButton != null)
        {
            _lunarInstallButton.Content = "安装中...";
            _lunarInstallButton.IsEnabled = false;
        }

        try
        {
            bool success = false;
            if (_lunarInstaller != null)
            {
                success = await _lunarInstaller.TryInstallAsync();
            }
            if (success && _lunarInstallButton != null)
            {
                _lunarInstallButton.Content = "✓ 已安装";
                _lunarInstallButton.Background = Brushes.Green;
                if (_settings != null)
                {
                    _settings.IsLunarInstalled = true;
                }
            }
            else if (_lunarInstallButton != null)
            {
                _lunarInstallButton.Content = "➜ 安装";
                _lunarInstallButton.IsEnabled = true;
            }
        }
        catch
        {
            if (_lunarInstallButton != null)
            {
                _lunarInstallButton.Content = "➜ 安装";
                _lunarInstallButton.IsEnabled = true;
            }
        }
    }

    private void OnTimeBaseSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox comboBox)
        {
            _settings.TimeBaseMode = comboBox.SelectedIndex;
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
