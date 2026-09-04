using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 时间表悬浮窗设置页面（从主设置页拆出）。
/// 设置项按三个折叠面板分组：基础外观 / 交互行为 / 窗口与高级。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandFloatingSchedule", "时间表悬浮窗")]
public class FloatingScheduleSettingsPage : SettingsPageBase
{
    private readonly PluginSettings? _settings;

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

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };

        mainPanel.Children.Add(new TextBlock
        {
            Text = "时间表悬浮窗",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = ThemeHelper.GetTextBrush(),
            Margin = new Thickness(0, 0, 0, 12)
        });

        BuildBasicAppearanceGroup(mainPanel);
        BuildInteractionGroup(mainPanel);
        BuildWindowAdvancedGroup(mainPanel);
        BuildRandomTitleGroup(mainPanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    // ==================== 1. 基础外观 ====================
    private void BuildBasicAppearanceGroup(StackPanel mainPanel)
    {
        var group = CreateGroup("基础外观", "悬浮窗的启用开关与外观显示设置");

        // 启用悬浮时间表
        var enableItem = CreateItem("启用悬浮时间表", "打开后在桌面显示半透明悬浮课表窗口，关闭后窗口自动隐藏", "CalendarClock");
        enableItem.IsOn = _settings?.EnableFloatingSchedule ?? false;
        WatchIsOn(enableItem, () =>
        {
            if (_settings != null) _settings.EnableFloatingSchedule = enableItem.IsOn;
        });
        group.Items.Add(enableItem);

        // 课程名字号
        var fontScaleItem = CreateItem("课程名字号", "课程名字体大小（单位 pt，范围 8 ~ 32，默认 18）；表头/时间/老师文字会按相对差值自动缩放", "FormatSize");
        var fontScaleNumeric = new WpfNumericUpDown
        {
            Width = 155,
            Minimum = 8m,
            Maximum = 32m,
            Increment = 1m,
            FormatString = "F0",
            HorizontalAlignment = HorizontalAlignment.Left,
            Value = (decimal)Math.Clamp(Math.Round(_settings?.FloatingScheduleFontScale ?? 18.0), 8.0, 32.0)
        };
        fontScaleNumeric.ValueChanged += (s, e) =>
        {
            if (_settings != null && fontScaleNumeric.Value.HasValue)
                _settings.FloatingScheduleFontScale = Math.Clamp(Math.Round((double)fontScaleNumeric.Value.Value), 8.0, 32.0);
        };
        fontScaleItem.Switcher = fontScaleNumeric;
        group.Items.Add(fontScaleItem);

        // 背景不透明度
        var opacityItem = CreateItem("背景不透明度", "仅作用于卡片背景（不影响文字/进度条可读性）：1.0 完全不透明，0 完全透明；默认 0.85", "Opacity");
        var opacityNumeric = new WpfNumericUpDown
        {
            Width = 155,
            Minimum = 0.0m,
            Maximum = 1.0m,
            Increment = 0.05m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left,
            Value = (decimal)Math.Clamp(_settings?.FloatingScheduleOpacity ?? 0.85, 0.0, 1.0)
        };
        opacityNumeric.ValueChanged += (s, e) =>
        {
            if (_settings != null && opacityNumeric.Value.HasValue)
                _settings.FloatingScheduleOpacity = Math.Clamp((double)opacityNumeric.Value.Value, 0.0, 1.0);
        };
        opacityItem.Switcher = opacityNumeric;
        group.Items.Add(opacityItem);

        // 显示教师
        var showTeacherItem = CreateItem("显示教师", "关闭后悬浮窗不显示任何教师信息（下一项同时失效，但保留其设置值）。", "AccountGroup");
        showTeacherItem.IsOn = _settings?.FloatingScheduleShowTeacher ?? true;
        WatchIsOn(showTeacherItem, () =>
        {
            if (_settings != null) _settings.FloatingScheduleShowTeacher = showTeacherItem.IsOn;
        });
        group.Items.Add(showTeacherItem);

        // 教师全名（联动：关闭"显示教师"时置灰，值保留）
        var fullTeacherItem = CreateItem("启用教师全名（不推荐）", "关闭时显示\"X老师\"（如：张老师）；开启后直接显示完整教师名（如：张三）。\n当科目未填写教师信息时不显示教师名。", "AccountEditOutline");
        fullTeacherItem.IsOn = _settings?.FloatingScheduleEnableFullTeacherName ?? false;
        fullTeacherItem.IsEnabled = _settings?.FloatingScheduleShowTeacher ?? true;
        WatchIsOn(fullTeacherItem, () =>
        {
            if (_settings != null) _settings.FloatingScheduleEnableFullTeacherName = fullTeacherItem.IsOn;
        });
        if (_settings != null)
        {
            _settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PluginSettings.FloatingScheduleShowTeacher))
                    fullTeacherItem.IsEnabled = _settings.FloatingScheduleShowTeacher;
            };
        }
        group.Items.Add(fullTeacherItem);

        mainPanel.Children.Add(group);
    }

    // ==================== 2. 交互行为 ====================
    private void BuildInteractionGroup(StackPanel mainPanel)
    {
        var group = CreateGroup("交互行为", "鼠标点击、指针淡化与贴边自动隐藏等交互设置", "CursorDefaultClickOutline");

        // 点击穿透
        var clickThroughItem = CreateItem("启用点击穿透", "开启后，悬浮窗对鼠标点击完全透明（点击会命中下方窗口），同时关闭悬浮窗拖拽（关闭穿透后恢复）。", "TapIn");
        clickThroughItem.IsOn = _settings?.FloatingScheduleClickThrough ?? false;
        WatchIsOn(clickThroughItem, () =>
        {
            if (_settings != null) _settings.FloatingScheduleClickThrough = clickThroughItem.IsOn;
        });
        group.Items.Add(clickThroughItem);

        // 指针移入淡化（反转项需先声明，供主开关联动闭包引用）
        var hoverFadeReverseItem = CreateItem("指针移入淡化（反转）", "仅在上一项启用时生效：反转淡化触发方向（指针在悬浮窗外时淡化，移入窗口内恢复不透明）。", "SwapVertical");

        // 指针移入淡化
        var hoverFadeItem = CreateItem("指针移入淡化", "参考 ClassIsland 主窗体淡化：当鼠标指针进入悬浮窗区域时，整体不透明度降到 5%；移出后恢复用户设置的背景不透明度。", "BrushVariant");
        hoverFadeItem.IsOn = _settings?.FloatingScheduleHoverFade ?? false;
        WatchIsOn(hoverFadeItem, () =>
        {
            if (_settings == null) return;
            _settings.FloatingScheduleHoverFade = hoverFadeItem.IsOn;
            // 主开关关闭时同步把反转开关置 false + 置灰；打开时恢复可用
            if (!hoverFadeItem.IsOn)
            {
                hoverFadeReverseItem.IsEnabled = false;
                hoverFadeReverseItem.IsOn = false;
                if (_settings.FloatingScheduleHoverFadeReverse)
                    _settings.FloatingScheduleHoverFadeReverse = false;
            }
            else
            {
                hoverFadeReverseItem.IsEnabled = true;
            }
        });
        group.Items.Add(hoverFadeItem);
        if (!(_settings?.FloatingScheduleHoverFade ?? false))
        {
            hoverFadeReverseItem.IsOn = false;
            hoverFadeReverseItem.IsEnabled = false;
            if (_settings != null && _settings.FloatingScheduleHoverFadeReverse)
                _settings.FloatingScheduleHoverFadeReverse = false;
        }
        else
        {
            hoverFadeReverseItem.IsEnabled = true;
            hoverFadeReverseItem.IsOn = _settings!.FloatingScheduleHoverFadeReverse;
        }
        WatchIsOn(hoverFadeReverseItem, () =>
        {
            if (_settings == null) return;
            // 主开关未开时反转开关任何操作都无效（先强制拉回 false 再忽略）
            if (!_settings.FloatingScheduleHoverFade)
            {
                hoverFadeReverseItem.IsOn = false;
                return;
            }
            _settings.FloatingScheduleHoverFadeReverse = hoverFadeReverseItem.IsOn;
        });
        group.Items.Add(hoverFadeReverseItem);

        // 贴边隐藏延迟时间（子项：仅开启贴边自动隐藏时激活；先声明供上方闭包引用）
        var edgeDelayItem = CreateItem("贴边隐藏延迟时间（秒）", "判定贴边（且指针离开）后等待本时长再滑出隐藏；范围 0 ~ 60，步长 1，精确到 0.1，默认 3 秒；0 表示立即隐藏。仅在开启贴边自动隐藏时生效。", "AvTimer");
        edgeDelayItem.IsEnabled = _settings?.FloatingScheduleEdgeHide ?? false;

        // 贴边自动隐藏
        var edgeHideItem = CreateItem("贴边自动隐藏", "开启后，悬浮时间表贴近屏幕边缘（<8px）时沿该边滑出、只保留约 6px 可见条；指针移入可见条滑回，离开后再次隐藏。", "ArrowCollapseHorizontal");
        edgeHideItem.IsOn = _settings?.FloatingScheduleEdgeHide ?? false;
        WatchIsOn(edgeHideItem, () =>
        {
            if (_settings == null) return;
            _settings.FloatingScheduleEdgeHide = edgeHideItem.IsOn;
            edgeDelayItem.IsEnabled = edgeHideItem.IsOn;
        });
        group.Items.Add(edgeHideItem);

        var edgeDelayNumeric = new WpfNumericUpDown
        {
            Width = 155,
            Minimum = 0.0m,
            Maximum = 60.0m,
            Increment = 1.0m,
            FormatString = "0.0",
            HorizontalAlignment = HorizontalAlignment.Left,
            Value = (decimal)Math.Clamp(_settings?.FloatingScheduleEdgeHideDelay ?? 3.0, 0.0, 60.0)
        };
        edgeDelayNumeric.ValueChanged += (s, e) =>
        {
            if (_settings != null && edgeDelayNumeric.Value.HasValue)
                _settings.FloatingScheduleEdgeHideDelay = Math.Clamp((double)edgeDelayNumeric.Value.Value, 0.0, 60.0);
        };
        edgeDelayItem.Switcher = edgeDelayNumeric;
        group.Items.Add(edgeDelayItem);

        mainPanel.Children.Add(group);
    }

    // ==================== 3. 窗口与高级 ====================
    private void BuildWindowAdvancedGroup(StackPanel mainPanel)
    {
        var group = CreateGroup("窗口与高级", "窗口层级、层级保护刷新频率与自动隐藏规则（高级设置）");

        // 悬浮窗层级
        var layerItem = CreateItem("悬浮窗层级", "默认为置底。置顶会遮挡其他窗口，不推荐", "LayersOutline");
        var layerComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        layerComboBox.Items.Add("置底");
        layerComboBox.Items.Add("置顶（不推荐）");
        layerComboBox.SelectedIndex = (_settings?.FloatingScheduleWindowLayer ?? FloatingScheduleWindowLayer.Bottom) ==
                                     FloatingScheduleWindowLayer.Topmost ? 1 : 0;
        layerComboBox.SelectionChanged += (s, e) =>
        {
            if (_settings == null) return;
            _settings.FloatingScheduleWindowLayer = layerComboBox.SelectedIndex == 1
                ? FloatingScheduleWindowLayer.Topmost
                : FloatingScheduleWindowLayer.Bottom;
        };
        layerItem.Switcher = layerComboBox;
        group.Items.Add(layerItem);

        // 悬浮窗层级设置频率（ComboBox 五档，50ms/1ms 带警告色）
        var recheckItem = CreateItem("悬浮窗层级设置频率", "ClassIsland 在什么时候重新设置悬浮窗层级（置底/置顶）。用于防止其他窗口挤占悬浮窗层级；高频设置会带来性能占用并可能导致界面轻微闪烁。", "AvTimer");
        var recheckCombo = new ComboBox { Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        recheckCombo.Items.Add("窗口层级变化时（默认）");
        recheckCombo.Items.Add("前台窗口变化时");
        var warn50 = new StackPanel { Orientation = Orientation.Horizontal };
        warn50.Children.Add(new TextBlock { Text = "每 50ms", VerticalAlignment = VerticalAlignment.Center });
        warn50.Children.Add(new TextBlock
        {
            Text = " ⚠",
            FontSize = 18,
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x5A)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -3, 0, -3),
            ToolTip = "较高频率可能带来性能占用与轻微闪烁，仅当置顶频繁丢失时使用。"
        });
        recheckCombo.Items.Add(warn50);
        var warn1 = new StackPanel { Orientation = Orientation.Horizontal };
        warn1.Children.Add(new TextBlock { Text = "每 1ms", VerticalAlignment = VerticalAlignment.Center });
        warn1.Children.Add(new TextBlock
        {
            Text = " ⚠",
            FontSize = 18,
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x4A, 0x34)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -3, 0, -3),
            ToolTip = "极高频率重设：明显占用 UI 线程、可能导致主窗口/悬浮窗闪烁，仅作极端调试用途。"
        });
        recheckCombo.Items.Add(warn1);
        recheckCombo.Items.Add("每 2s");
        static int ModeToIndex(FloatingTopmostRefreshMode m) => m switch
        {
            FloatingTopmostRefreshMode.OnWindowZOrderChanged => 0,
            FloatingTopmostRefreshMode.OnForegroundWindowChanged => 1,
            FloatingTopmostRefreshMode.Every50Ms => 2,
            FloatingTopmostRefreshMode.Every1Ms => 3,
            FloatingTopmostRefreshMode.Every2s => 4,
            _ => 0
        };
        static FloatingTopmostRefreshMode IndexToMode(int i) => i switch
        {
            0 => FloatingTopmostRefreshMode.OnWindowZOrderChanged,
            1 => FloatingTopmostRefreshMode.OnForegroundWindowChanged,
            2 => FloatingTopmostRefreshMode.Every50Ms,
            3 => FloatingTopmostRefreshMode.Every1Ms,
            4 => FloatingTopmostRefreshMode.Every2s,
            _ => FloatingTopmostRefreshMode.OnWindowZOrderChanged
        };
        recheckCombo.SelectedIndex = ModeToIndex(
            _settings?.FloatingScheduleTopmostRefreshMode ?? FloatingTopmostRefreshMode.Every50Ms);
        recheckCombo.SelectionChanged += (s, e) =>
        {
            if (_settings != null && s is ComboBox cb)
                _settings.FloatingScheduleTopmostRefreshMode = IndexToMode(cb.SelectedIndex);
        };
        recheckItem.Switcher = recheckCombo;
        group.Items.Add(recheckItem);

        // 隐藏悬浮窗（隐藏规则模式）
        var hideModeItem = CreateItem("隐藏悬浮窗", "在什么条件下自动隐藏悬浮窗。跟随主界面隐藏规则（默认）/ 基础模式 / 高级模式（规则集）/ 从不隐藏；基础与高级模式均复用 ClassIsland 主界面的隐藏开关与规则集。", "EyeOffOutline");
        var hideModeCombo = new ComboBox { Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        hideModeCombo.Items.Add("跟随主界面隐藏规则（默认）");
        hideModeCombo.Items.Add("基础模式");
        hideModeCombo.Items.Add("高级模式（规则集）");
        hideModeCombo.Items.Add("从不隐藏");
        static int HideModeToIndex(FloatingScheduleHideMode m) => m switch
        {
            FloatingScheduleHideMode.FollowHost => 0,
            FloatingScheduleHideMode.Basic => 1,
            FloatingScheduleHideMode.Advanced => 2,
            FloatingScheduleHideMode.Never => 3,
            _ => 0
        };
        static FloatingScheduleHideMode HideIndexToMode(int i) => i switch
        {
            0 => FloatingScheduleHideMode.FollowHost,
            1 => FloatingScheduleHideMode.Basic,
            2 => FloatingScheduleHideMode.Advanced,
            3 => FloatingScheduleHideMode.Never,
            _ => FloatingScheduleHideMode.FollowHost
        };
        hideModeCombo.SelectedIndex = HideModeToIndex(
            _settings?.FloatingScheduleHideMode ?? FloatingScheduleHideMode.FollowHost);
        hideModeCombo.SelectionChanged += (s, e) =>
        {
            if (_settings != null && s is ComboBox cb)
                _settings.FloatingScheduleHideMode = HideIndexToMode(cb.SelectedIndex);
        };
        hideModeItem.Switcher = hideModeCombo;
        group.Items.Add(hideModeItem);

        mainPanel.Children.Add(group);
    }

    // ==================== 4. 随机窗口名 ====================
    private void BuildRandomTitleGroup(StackPanel mainPanel)
    {
        var group = CreateGroup("随机窗口名", "为了防止学校把时间表悬浮窗拦截，建议开启此项", "DiceMultipleOutline");

        // 子项：增强随机模式（仅主开关开启时激活）
        var enhancedItem = CreateItem("增强随机模式", "仅在上一项启用时生效：开启后每 1 秒重新设置一次随机窗口标题，防止拦截工具按标题缓存识别。", "DiceMultipleOutline");
        enhancedItem.IsOn = (_settings?.FloatingScheduleRandomTitleEnhanced ?? false) &&
                            (_settings?.FloatingScheduleRandomTitle ?? false);
        enhancedItem.IsEnabled = _settings?.FloatingScheduleRandomTitle ?? false;
        WatchIsOn(enhancedItem, () =>
        {
            if (_settings == null) return;
            // 主开关未开时增强开关任何操作都无效（先强制拉回 false 再忽略）
            if (!_settings.FloatingScheduleRandomTitle)
            {
                enhancedItem.IsOn = false;
                return;
            }
            _settings.FloatingScheduleRandomTitleEnhanced = enhancedItem.IsOn;
        });
        group.Items.Add(enhancedItem);

        // 阻止截图（独立开关，不依赖随机窗口名）
        var preventCaptureItem = CreateItem("阻止截图", "开启后，其他应用无法截取悬浮窗窗口内容，录制时也不会录制到悬浮窗。系统限制：WPF 透明悬浮窗（AllowsTransparency）无法从截屏结果中完全隐藏，启用后显示为黑色矩形遮挡以保护内容。", "CameraOffOutline");
        preventCaptureItem.IsOn = _settings?.FloatingSchedulePreventCapture ?? false;
        WatchIsOn(preventCaptureItem, () =>
        {
            if (_settings != null) _settings.FloatingSchedulePreventCapture = preventCaptureItem.IsOn;
        });
        group.Items.Add(preventCaptureItem);

        // 主开关：随机窗口名（放在折叠栏 Footer，与主设置页"实验性功能"开关样式一致）
        var randomTitleToggle = new System.Windows.Controls.Primitives.ToggleButton
        {
            IsChecked = _settings?.FloatingScheduleRandomTitle ?? false,
            Style = System.Windows.Application.Current?.TryFindResource("MaterialDesignSwitchToggleButton") as System.Windows.Style,
            VerticalAlignment = VerticalAlignment.Center
        };
        randomTitleToggle.Checked += (_, _) => ApplyRandomTitleToggle();
        randomTitleToggle.Unchecked += (_, _) => ApplyRandomTitleToggle();
        void ApplyRandomTitleToggle()
        {
            if (_settings == null) return;
            var isOn = randomTitleToggle.IsChecked == true;
            _settings.FloatingScheduleRandomTitle = isOn;
            // 主开关关闭时同步把增强开关置 false + 置灰；打开时恢复可用
            if (!isOn)
            {
                enhancedItem.IsEnabled = false;
                enhancedItem.IsOn = false;
                if (_settings.FloatingScheduleRandomTitleEnhanced)
                    _settings.FloatingScheduleRandomTitleEnhanced = false;
            }
            else
            {
                enhancedItem.IsEnabled = true;
            }
        }
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Footer", randomTitleToggle);

        mainPanel.Children.Add(group);
    }

    // ==================== 辅助方法 ====================
    private static WpfSettingsExpander CreateGroup(string header, string description, string iconGlyph = "")
    {
        var group = (WpfSettingsExpander)FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Header", header);
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Description", description);
        if (!string.IsNullOrEmpty(iconGlyph)
            && Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(iconGlyph, true, out var kind))
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "IconSource", kind);
        }
        return group;
    }

    private static SettingsControl CreateItem(string header, string description, string iconGlyph)
    {
        if (!Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(iconGlyph, true, out var kind))
            kind = MaterialDesignThemes.Wpf.PackIconKind.Settings;
        return new SettingsControl
        {
            IconGlyph = kind,
            Header = header,
            Description = description,
            Margin = new Thickness(0, 8, 0, 0)
        };
    }

    /// <summary>监听 SettingsControl.IsOn 变化（与主设置页一致：DependencyPropertyDescriptor，避免 TwoWay 绑定歧义）。</summary>
    private static void WatchIsOn(SettingsControl control, Action onChanged)
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(SettingsControl.IsOnProperty, typeof(SettingsControl));
        descriptor.AddValueChanged(control, (s, e) => onChanged());
    }
}
