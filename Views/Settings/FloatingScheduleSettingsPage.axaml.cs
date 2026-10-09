using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 时间表悬浮窗设置页面（axaml 版，从纯代码构建迁移，写法对齐 ClassIsland 官方设置页）。
/// 主面板使用官方 <c>settings-container animated-intro</c> 样式类：宿主“动画效果”设为“华丽”时，
/// 各分组依次淡入上滑；分组控件为真实的 FA2/FA3 SettingsExpander（经 CompatSettingsExpander 包装），
/// 展开 / 折叠动画由官方控件提供。
/// 设置项按折叠面板分组：基础外观 / 交互行为 / 窗口与高级 / 随机窗口名 / 独立进程模式。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandFloatingSchedule", "时间表悬浮窗")]
[Group("advancedtimeisland.main")]
public partial class FloatingScheduleSettingsPage : SettingsPageBase
{
    private readonly PluginSettings? _settings;
    private DispatcherTimer? _independentStatusTimer;
    private bool _initialized;

    public FloatingScheduleSettingsPage() : this(null)
    {
    }

    public FloatingScheduleSettingsPage(PluginSettings? settings)
    {
        // 宿主 DI 解析时注入 PluginSettings 单例；手动 new（无参）时回退 Plugin.Instance
        _settings = settings ?? Plugin.Instance?.Settings;
        try
        {
            InitializeComponent();
            WireSettings();
            _initialized = true;
        }
        catch (Exception ex)
        {
            Content = new TextBlock
            {
                Text = $"设置页面初始化失败: {ex.Message}",
                Foreground = Brushes.Red,
                Margin = new Thickness(16)
            };
        }
    }

    // ==================== 初始化（初始值 + 外部设置变化同步 + 状态轮询） ====================
    private void WireSettings()
    {
        var s = _settings;

        // ---- 初始值（赋回相同值不会触发 INPC，无副作用） ----
        EnableToggle.IsChecked = s?.EnableFloatingSchedule ?? false;

        FontScaleBox.Value = (decimal)Math.Clamp(Math.Round(s?.FloatingScheduleFontScale ?? 18.0), 8.0, 32.0);
        OpacityBox.Value = (decimal)Math.Clamp(s?.FloatingScheduleOpacity ?? 0.5, 0.0, 1.0);
        ShowTeacherToggle.IsChecked = s?.FloatingScheduleShowTeacher ?? true;
        TeacherFullToggle.IsChecked = s?.FloatingScheduleEnableFullTeacherName ?? false;
        TeacherFullItem.IsEnabled = s?.FloatingScheduleShowTeacher ?? true;
        TomorrowModeCombo.SelectedIndex = TomorrowModeToIndex(s?.FloatingScheduleTomorrowShowMode ?? FloatingScheduleTomorrowShowMode.AfterSchool);
        TodayPlaceholderBox.Text = s?.FloatingScheduleTodayPlaceholderText ?? "今天没有课程";
        TomorrowPlaceholderBox.Text = s?.FloatingScheduleTomorrowPlaceholderText ?? "明天没有课程";

        ClickThroughToggle.IsChecked = s?.FloatingScheduleClickThrough ?? false;
        HoverFadeToggle.IsChecked = s?.FloatingScheduleHoverFade ?? false;
        SyncHoverFadeReverseEnabled();
        EdgeHideToggle.IsChecked = s?.FloatingScheduleEdgeHide ?? false;
        EdgeDelayItem.IsEnabled = s?.FloatingScheduleEdgeHide ?? false;
        EdgeDelayBox.Value = (decimal)Math.Clamp(s?.FloatingScheduleEdgeHideDelay ?? 3.0, 0.0, 60.0);

        LayerCombo.SelectedIndex = (s?.FloatingScheduleWindowLayer ?? FloatingScheduleWindowLayer.Bottom) == FloatingScheduleWindowLayer.Bottom ? 0 : 1;
        RecheckCombo.SelectedIndex = RecheckModeToIndex(s?.FloatingScheduleTopmostRefreshMode ?? FloatingTopmostRefreshMode.Every50Ms);
        HideModeCombo.SelectedIndex = HideModeToIndex(s?.FloatingScheduleHideMode ?? FloatingScheduleHideMode.FollowHost);

        RandomTitleToggle.IsChecked = s?.FloatingScheduleRandomTitle ?? false;
        EnhancedToggle.IsChecked = (s?.FloatingScheduleRandomTitleEnhanced ?? false) && (s?.FloatingScheduleRandomTitle ?? false);
        EnhancedItem.IsEnabled = s?.FloatingScheduleRandomTitle ?? false;
        PreventCaptureToggle.IsChecked = s?.FloatingSchedulePreventCapture ?? false;

        IndependentToggle.IsChecked = s?.FloatingScheduleIndependentProcess ?? false;
        RandomNameToggle.IsChecked = s?.FloatingScheduleRandomProcessName ?? false;
        SingleInstanceToggle.IsChecked = s?.FloatingScheduleSingleInstanceProtection ?? true;
        FollowToggle.IsChecked = s?.FloatingScheduleFollowHostLifetime ?? true;
        SyncIndependentEnabled();
        // 非 Windows（Linux/macOS/Android）整组禁用
        if (!OperatingSystem.IsWindows())
        {
            IndependentGroup.IsEnabled = false;
        }

        // ---- 外部修改设置后同步本页开关（托盘“退出”、自动化行动等场景） ----
        if (s != null)
        {
            s.PropertyChanged += OnSettingsPropertyChanged;
        }

        // ---- 子进程状态：1s 轮询 HostProcessService.StatusText ----
        _independentStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _independentStatusTimer.Tick += (_, _) =>
        {
            try
            {
                var svc = Services.FloatScheduleHostProcessService.Instance;
                IndependentStatusText.Text = "状态：" + (svc?.StatusText ?? "未启用");
                // 重启进行中 / 子进程启动中 → 临时禁用“重启”按钮，避免过程中连点
                RestartButton.IsEnabled = ShouldEnableRestartButton();
            }
            catch
            {
            }
        };
    }

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_settings == null) return;
        switch (e.PropertyName)
        {
            // 托盘“退出”/自动化等会在外部改总开关，页面必须跟随真实值
            case nameof(PluginSettings.EnableFloatingSchedule):
                if (EnableToggle.IsChecked != _settings.EnableFloatingSchedule)
                    EnableToggle.IsChecked = _settings.EnableFloatingSchedule;
                break;
            case nameof(PluginSettings.FloatingScheduleShowTeacher):
                TeacherFullItem.IsEnabled = _settings.FloatingScheduleShowTeacher;
                break;
            case nameof(PluginSettings.FloatingScheduleHoverFade):
            case nameof(PluginSettings.FloatingScheduleHoverFadeReverse):
                SyncHoverFadeReverseEnabled();
                break;
            case nameof(PluginSettings.FloatingScheduleEdgeHide):
                EdgeDelayItem.IsEnabled = _settings.FloatingScheduleEdgeHide;
                break;
            case nameof(PluginSettings.FloatingScheduleRandomTitle):
                EnhancedItem.IsEnabled = _settings.FloatingScheduleRandomTitle;
                break;
            case nameof(PluginSettings.FloatingScheduleIndependentProcess):
                SyncIndependentEnabled();
                // 子进程托盘“退出”会连带关闭本开关：页面同步为真实值
                if (IndependentToggle.IsChecked != _settings.FloatingScheduleIndependentProcess)
                    IndependentToggle.IsChecked = _settings.FloatingScheduleIndependentProcess;
                break;
        }
    }

    // ==================== 0. 主开关 ====================
    private void EnableToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.EnableFloatingSchedule = EnableToggle.IsChecked == true;
    }

    // ==================== 1. 基础外观 ====================
    private void FontScaleBox_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_settings != null && e.NewValue is { } v)
            _settings.FloatingScheduleFontScale = Math.Clamp(Math.Round((double)v), 8.0, 32.0);
    }

    private void OpacityBox_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_settings != null && e.NewValue is { } v)
            _settings.FloatingScheduleOpacity = Math.Clamp((double)v, 0.0, 1.0);
    }

    private void ShowTeacherToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleShowTeacher = ShowTeacherToggle.IsChecked == true;
    }

    private void TeacherFullToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleEnableFullTeacherName = TeacherFullToggle.IsChecked == true;
    }

    private void TomorrowModeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox cb)
            _settings.FloatingScheduleTomorrowShowMode = TomorrowModeFromIndex(cb.SelectedIndex);
    }

    private void TodayPlaceholderBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_settings != null && sender is TextBox tb)
            _settings.FloatingScheduleTodayPlaceholderText = tb.Text ?? string.Empty;
    }

    private void TomorrowPlaceholderBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_settings != null && sender is TextBox tb)
            _settings.FloatingScheduleTomorrowPlaceholderText = tb.Text ?? string.Empty;
    }

    private static int TomorrowModeToIndex(FloatingScheduleTomorrowShowMode m) => m switch
    {
        FloatingScheduleTomorrowShowMode.Never => 0,
        FloatingScheduleTomorrowShowMode.AfterSchool => 1,
        FloatingScheduleTomorrowShowMode.Always => 2,
        FloatingScheduleTomorrowShowMode.OnEmpty => 3,
        _ => 1
    };

    private static FloatingScheduleTomorrowShowMode TomorrowModeFromIndex(int i) => i switch
    {
        0 => FloatingScheduleTomorrowShowMode.Never,
        1 => FloatingScheduleTomorrowShowMode.AfterSchool,
        2 => FloatingScheduleTomorrowShowMode.Always,
        3 => FloatingScheduleTomorrowShowMode.OnEmpty,
        _ => FloatingScheduleTomorrowShowMode.AfterSchool
    };

    // ==================== 2. 交互行为 ====================
    private void ClickThroughToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleClickThrough = ClickThroughToggle.IsChecked == true;
    }

    private void HoverFadeToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleHoverFade = HoverFadeToggle.IsChecked == true;
    }

    private void HoverFadeReverseToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings == null) return;
        // 主开关未开时反转开关任何操作都无效（先强制拉回 false 再忽略）
        if (!_settings.FloatingScheduleHoverFade)
        {
            HoverFadeReverseToggle.IsChecked = false;
            return;
        }
        _settings.FloatingScheduleHoverFadeReverse = HoverFadeReverseToggle.IsChecked == true;
    }

    /// <summary>淡化主开关关闭时，反转开关置灰禁用并归位 false；主开关打开时恢复可用。</summary>
    private void SyncHoverFadeReverseEnabled()
    {
        if (_settings == null) return;
        if (!_settings.FloatingScheduleHoverFade)
        {
            HoverFadeReverseItem.IsEnabled = false;
            HoverFadeReverseToggle.IsChecked = false;
            if (_settings.FloatingScheduleHoverFadeReverse)
                _settings.FloatingScheduleHoverFadeReverse = false;
        }
        else
        {
            HoverFadeReverseItem.IsEnabled = true;
            HoverFadeReverseToggle.IsChecked = _settings.FloatingScheduleHoverFadeReverse;
        }
    }

    private void EdgeHideToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleEdgeHide = EdgeHideToggle.IsChecked == true;
    }

    private void EdgeDelayBox_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_settings != null && e.NewValue is { } v)
            _settings.FloatingScheduleEdgeHideDelay = Math.Clamp((double)v, 0.0, 60.0);
    }

    // ==================== 3. 窗口与高级 ====================
    private void LayerCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings == null) return;
        _settings.FloatingScheduleWindowLayer = LayerCombo.SelectedIndex == 1
            ? FloatingScheduleWindowLayer.Topmost
            : FloatingScheduleWindowLayer.Bottom;
    }

    private void RecheckCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox cb)
            _settings.FloatingScheduleTopmostRefreshMode = RecheckModeFromIndex(cb.SelectedIndex);
    }

    private static int RecheckModeToIndex(FloatingTopmostRefreshMode m) => m switch
    {
        FloatingTopmostRefreshMode.OnWindowZOrderChanged => 0,
        FloatingTopmostRefreshMode.OnForegroundWindowChanged => 1,
        FloatingTopmostRefreshMode.Every50Ms => 2,
        FloatingTopmostRefreshMode.Every1Ms => 3,
        FloatingTopmostRefreshMode.Every2s => 4,
        _ => 0
    };

    private static FloatingTopmostRefreshMode RecheckModeFromIndex(int i) => i switch
    {
        0 => FloatingTopmostRefreshMode.OnWindowZOrderChanged,
        1 => FloatingTopmostRefreshMode.OnForegroundWindowChanged,
        2 => FloatingTopmostRefreshMode.Every50Ms,
        3 => FloatingTopmostRefreshMode.Every1Ms,
        4 => FloatingTopmostRefreshMode.Every2s,
        _ => FloatingTopmostRefreshMode.OnWindowZOrderChanged
    };

    private void HideModeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_settings != null && sender is ComboBox cb)
            _settings.FloatingScheduleHideMode = HideModeFromIndex(cb.SelectedIndex);
    }

    private static int HideModeToIndex(FloatingScheduleHideMode m) => m switch
    {
        FloatingScheduleHideMode.FollowHost => 0,
        FloatingScheduleHideMode.Basic => 1,
        FloatingScheduleHideMode.Advanced => 2,
        FloatingScheduleHideMode.Never => 3,
        _ => 0
    };

    private static FloatingScheduleHideMode HideModeFromIndex(int i) => i switch
    {
        0 => FloatingScheduleHideMode.FollowHost,
        1 => FloatingScheduleHideMode.Basic,
        2 => FloatingScheduleHideMode.Advanced,
        3 => FloatingScheduleHideMode.Never,
        _ => FloatingScheduleHideMode.FollowHost
    };

    // ==================== 4. 随机窗口名 ====================
    private void RandomTitleToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings == null) return;
        var isOn = RandomTitleToggle.IsChecked == true;
        _settings.FloatingScheduleRandomTitle = isOn;
        // 主开关关闭时同步把增强开关置 false + 置灰；打开时恢复可用
        if (!isOn)
        {
            EnhancedItem.IsEnabled = false;
            EnhancedToggle.IsChecked = false;
            if (_settings.FloatingScheduleRandomTitleEnhanced)
                _settings.FloatingScheduleRandomTitleEnhanced = false;
        }
        else
        {
            EnhancedItem.IsEnabled = true;
        }
    }

    private void EnhancedToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings == null) return;
        // 主开关未开时增强开关任何操作都无效
        if (!_settings.FloatingScheduleRandomTitle)
        {
            EnhancedToggle.IsChecked = false;
            return;
        }
        _settings.FloatingScheduleRandomTitleEnhanced = EnhancedToggle.IsChecked == true;
    }

    private void PreventCaptureToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingSchedulePreventCapture = PreventCaptureToggle.IsChecked == true;
    }

    // ==================== 5. 独立进程模式 ====================
    private void IndependentToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleIndependentProcess = IndependentToggle.IsChecked == true;
    }

    private void RandomNameToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleRandomProcessName = RandomNameToggle.IsChecked == true;
    }

    private void SingleInstanceToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleSingleInstanceProtection = SingleInstanceToggle.IsChecked == true;
    }

    private void FollowToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.FloatingScheduleFollowHostLifetime = FollowToggle.IsChecked == true;
    }

    private async void RestartButton_Click(object? sender, RoutedEventArgs e)
    {
        var svc = Services.FloatScheduleHostProcessService.Instance;
        if (svc == null || svc.IsRestarting) return;
        // 重启过程立即禁用按钮：避免连点导致 Stop/Stop 交错（服务侧另有互斥兜底）
        RestartButton.IsEnabled = false;
        try
        {
            await svc.RestartChildAsync();
        }
        catch
        {
        }
        finally
        {
            RestartButton.IsEnabled = ShouldEnableRestartButton();
        }
    }

    /// <summary>
    /// “重启”按钮是否可用：模式开启 且 不在重启过程中 且 子进程不在启动过程中。
    /// </summary>
    private bool ShouldEnableRestartButton()
    {
        var svc = Services.FloatScheduleHostProcessService.Instance;
        var on = _settings?.FloatingScheduleIndependentProcess ?? false;
        return on && svc != null && !svc.IsRestarting
            && svc.Status != Services.FloatScheduleHostProcessService.ChildStatus.Starting;
    }

    /// <summary>随机进程名 / 重启按钮仅在独立模式开启（且不在重启/启动过程中）时可用。</summary>
    private void SyncIndependentEnabled()
    {
        var on = _settings?.FloatingScheduleIndependentProcess ?? false;
        RandomNameItem.IsEnabled = on;
        RestartButton.IsEnabled = ShouldEnableRestartButton();
    }

    // ==================== 主题自适应 / 生命周期 ====================
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        if (Application.Current != null)
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        ApplyThemeColors();
        _independentStatusTimer?.Start();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        if (Application.Current != null)
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        if (_settings != null)
            _settings.PropertyChanged -= OnSettingsPropertyChanged;
        try
        {
            _independentStatusTimer?.Stop();
        }
        catch
        {
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => ApplyThemeColors();

    /// <summary>
    /// 主开关卡片沿用插件既有配色（深浅自适应）；分组内控件全部使用 FA 官方主题资源，自动适配。
    /// </summary>
    private void ApplyThemeColors()
    {
        TitleText.Foreground = ThemeHelper.GetTextBrush();
        EnableTitleText.Foreground = ThemeHelper.GetTextBrush();
        EnableSubText.Foreground = ThemeHelper.GetSubTextBrush();
        EnableCard.Background = ThemeHelper.GetCardBackgroundBrush();
        IndependentStatusText.Foreground = ThemeHelper.GetSubTextBrush();
    }
}
