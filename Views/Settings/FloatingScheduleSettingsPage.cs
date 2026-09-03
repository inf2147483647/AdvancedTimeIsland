using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 时间表悬浮窗设置页面（从主设置页拆出；net10 宿主下与主设置页同属 AdvancedTimeIsland 导航分组）。
/// 设置项按三个折叠面板分组：基础外观 / 交互行为 / 窗口与高级。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandFloatingSchedule", "时间表悬浮窗")]
#if NET10_0_OR_GREATER
[Group("advancedtimeisland.main")]
#endif
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
            Margin = new Thickness(16),
            Spacing = 16
        };

        _titleTextBlock = new TextBlock
        {
            Text = "时间表悬浮窗",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        mainPanel.Children.Add(_titleTextBlock);

        BuildBasicAppearanceGroup(mainPanel);
        BuildInteractionGroup(mainPanel);
        BuildWindowAdvancedGroup(mainPanel);
        BuildRandomTitleGroup(mainPanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    private TextBlock? _titleTextBlock;

    // ==================== 1. 基础外观 ====================
    private void BuildBasicAppearanceGroup(StackPanel mainPanel)
    {
        var group = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Header", "基础外观");
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Description", "悬浮窗的启用开关与外观显示设置");

        // 启用悬浮时间表
        var enableToggle = CreateToggleSwitch(_settings?.EnableFloatingSchedule ?? false, isOn =>
        {
            if (_settings != null) _settings.EnableFloatingSchedule = isOn;
        });
        enableToggle.HorizontalAlignment = HorizontalAlignment.Right;
        enableToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "启用悬浮时间表",
            "打开后在桌面显示半透明悬浮课表窗口，关闭后窗口自动隐藏",
            enableToggle);

        // 课程名字号
        var fontScaleBox = new NumericUpDown
        {
            Width = 160,
            Minimum = 8m,
            Maximum = 32m,
            Increment = 1m,
            Value = (decimal?)Math.Clamp(Math.Round(_settings?.FloatingScheduleFontScale ?? 18.0), 8.0, 32.0),
            FormatString = "F0",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        fontScaleBox.ValueChanged += (s, e) =>
        {
            if (_settings != null && e.NewValue is { } v)
                _settings.FloatingScheduleFontScale = Math.Clamp(Math.Round((double)v), 8.0, 32.0);
        };
        AddSettingsExpanderItem(group,
            "课程名字号",
            "课程名字体大小（单位 pt，范围 8 ~ 32，默认 18）；表头/时间/老师文字会按相对差值自动缩放",
            fontScaleBox);

        // 背景不透明度
        var opacityBox = new NumericUpDown
        {
            Width = 160,
            Minimum = 0.0m,
            Maximum = 1.0m,
            Increment = 0.05m,
            Value = (decimal?)Math.Clamp(_settings?.FloatingScheduleOpacity ?? 0.85, 0.0, 1.0),
            FormatString = "F2",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        opacityBox.ValueChanged += (s, e) =>
        {
            if (_settings != null && e.NewValue is { } v)
                _settings.FloatingScheduleOpacity = Math.Clamp((double)v, 0.0, 1.0);
        };
        AddSettingsExpanderItem(group,
            "背景不透明度",
            "仅作用于卡片背景（不影响文字/进度条可读性）：1.0 完全不透明，0 完全透明；默认 0.85",
            opacityBox);

        // 显示教师
        var showTeacherToggle = CreateToggleSwitch(_settings?.FloatingScheduleShowTeacher ?? true, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleShowTeacher = isOn;
        });
        showTeacherToggle.HorizontalAlignment = HorizontalAlignment.Right;
        showTeacherToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "显示教师",
            "关闭后悬浮窗不显示任何教师信息（下一项同时失效，但保留其设置值）。",
            showTeacherToggle);

        // 教师全名
        var teacherFullToggle = CreateToggleSwitch(_settings?.FloatingScheduleEnableFullTeacherName ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleEnableFullTeacherName = isOn;
        });
        teacherFullToggle.HorizontalAlignment = HorizontalAlignment.Right;
        teacherFullToggle.VerticalAlignment = VerticalAlignment.Center;
        var teacherFullItem = AddSettingsExpanderItem(group,
            "启用教师全名（不推荐）",
            "关闭时显示\"X老师\"（如：张老师）；开启后直接显示完整教师名（如：张三）。\n当科目未填写教师信息时不显示教师名。",
            teacherFullToggle);
        // 联动：关闭"显示教师"时本项置灰（值保留）
        static void SyncTeacherFullEnabled(Control? item, PluginSettings? s)
        {
            if (item == null || s == null) return;
            item.IsEnabled = s.FloatingScheduleShowTeacher;
        }
        SyncTeacherFullEnabled(teacherFullItem, _settings);
        if (_settings != null)
        {
            _settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PluginSettings.FloatingScheduleShowTeacher))
                    SyncTeacherFullEnabled(teacherFullItem, _settings);
            };
        }

        mainPanel.Children.Add(group);
    }

    // ==================== 2. 交互行为 ====================
    private void BuildInteractionGroup(StackPanel mainPanel)
    {
        var group = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Header", "交互行为");
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Description", "鼠标点击、指针淡化与贴边自动隐藏等交互设置");

        // 点击穿透
        var clickThroughToggle = CreateToggleSwitch(_settings?.FloatingScheduleClickThrough ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleClickThrough = isOn;
        });
        clickThroughToggle.HorizontalAlignment = HorizontalAlignment.Right;
        clickThroughToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "启用点击穿透",
            "开启后，悬浮窗对鼠标点击事件完全透明，点击会命中下方窗口；同时拖拽悬浮窗功能会临时关闭（关闭穿透后恢复）。",
            clickThroughToggle);

        // 指针移入淡化
        var hoverFadeToggle = CreateToggleSwitch(_settings?.FloatingScheduleHoverFade ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleHoverFade = isOn;
        });
        hoverFadeToggle.HorizontalAlignment = HorizontalAlignment.Right;
        hoverFadeToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "指针移入淡化",
            "参考 ClassIsland 主窗体淡化功能：当鼠标指针移动到悬浮窗区域上时，整体不透明度降到 5%；移出后恢复用户设置的不透明度。",
            hoverFadeToggle);

        // 指针移入淡化（反转）
        var hoverFadeReverseToggle = CreateToggleSwitch(
            (_settings?.FloatingScheduleHoverFadeReverse ?? false) &&
            (_settings?.FloatingScheduleHoverFade ?? false), isOn =>
            {
                // 仅当淡化主开关启用时才写入：避免"反转开但主开关关"的无效组合
                if (_settings == null) return;
                if (_settings.FloatingScheduleHoverFade)
                    _settings.FloatingScheduleHoverFadeReverse = isOn;
            });
        hoverFadeReverseToggle.HorizontalAlignment = HorizontalAlignment.Right;
        hoverFadeReverseToggle.VerticalAlignment = VerticalAlignment.Center;
        // 淡化主开关关闭时，反转开关置灰禁用并自动切到 false；主开关打开时恢复可用
        static void SyncReverseEnabled(ToggleSwitch rev, PluginSettings? s)
        {
            if (rev == null || s == null) return;
            if (!s.FloatingScheduleHoverFade)
            {
                rev.IsEnabled = false;
                rev.IsChecked = false;
                s.FloatingScheduleHoverFadeReverse = false;
            }
            else
            {
                rev.IsEnabled = true;
            }
        }
        SyncReverseEnabled(hoverFadeReverseToggle, _settings);
        if (_settings != null)
        {
            _settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PluginSettings.FloatingScheduleHoverFade) ||
                    e.PropertyName == nameof(PluginSettings.FloatingScheduleHoverFadeReverse))
                {
                    SyncReverseEnabled(hoverFadeReverseToggle, _settings);
                }
            };
        }
        AddSettingsExpanderItem(group,
            "指针移入淡化（反转）",
            "当启用指针移入淡化后再开启本项：变为\"指针在悬浮窗外时淡化，移入窗口内恢复不透明\"。适用于\"常驻淡化，仅在操作时清晰\"的场景。",
            hoverFadeReverseToggle);

        // 贴边自动隐藏
        var edgeHideToggle = CreateToggleSwitch(_settings?.FloatingScheduleEdgeHide ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleEdgeHide = isOn;
        });
        edgeHideToggle.HorizontalAlignment = HorizontalAlignment.Right;
        edgeHideToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "贴边自动隐藏",
            "开启后，把悬浮时间表拖到贴近屏幕边缘（<8px）时会自动滑出屏幕、只保留约 6px 可见条；\n鼠标移入可见条区域时滑回原位，离开后再次隐藏。关闭时恢复贴边前位置。",
            edgeHideToggle);

        // 贴边隐藏延迟时间（子项：仅开启贴边自动隐藏时激活）
        var edgeDelayBox = new NumericUpDown
        {
            Width = 160,
            Minimum = 0.0m,
            Maximum = 60.0m,
            Increment = 1.0m,
            Value = (decimal?)Math.Clamp(_settings?.FloatingScheduleEdgeHideDelay ?? 3.0, 0.0, 60.0),
            FormatString = "F1",
            HorizontalAlignment = HorizontalAlignment.Right,
            IsEnabled = _settings?.FloatingScheduleEdgeHide ?? false
        };
        edgeDelayBox.ValueChanged += (s, e) =>
        {
            if (_settings != null && e.NewValue is { } v)
                _settings.FloatingScheduleEdgeHideDelay = Math.Clamp((double)v, 0.0, 60.0);
        };
        var edgeDelayItem = AddSettingsExpanderItem(group,
            "贴边隐藏延迟时间（秒）",
            "判定贴边后等待本时长再滑出隐藏（光标离开可见条后同样延迟再隐藏）；\n范围 0 ~ 60，步长 1，精确到 0.1，默认 3 秒；0 表示立即隐藏。仅在开启贴边自动隐藏时生效。",
            edgeDelayBox);
        if (_settings != null)
        {
            _settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PluginSettings.FloatingScheduleEdgeHide) && edgeDelayItem != null)
                    edgeDelayItem.IsEnabled = _settings.FloatingScheduleEdgeHide;
            };
        }

        mainPanel.Children.Add(group);
    }

    // ==================== 3. 窗口与高级 ====================
    private void BuildWindowAdvancedGroup(StackPanel mainPanel)
    {
        var group = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Header", "窗口与高级");
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Description", "窗口层级、层级保护刷新频率与自动隐藏规则（高级设置）");

        // 悬浮窗层级
        var layerComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Right };
        layerComboBox.Items.Add("置底");
        layerComboBox.Items.Add("置顶（不推荐）");
        int layerIdx = (_settings?.FloatingScheduleWindowLayer ?? FloatingScheduleWindowLayer.Bottom) == FloatingScheduleWindowLayer.Bottom ? 0 : 1;
        layerComboBox.SelectedIndex = layerIdx;
        layerComboBox.SelectionChanged += (s, e) =>
        {
            if (_settings != null && s is ComboBox cb)
                _settings.FloatingScheduleWindowLayer = cb.SelectedIndex == 1
                    ? FloatingScheduleWindowLayer.Topmost
                    : FloatingScheduleWindowLayer.Bottom;
        };
        AddSettingsExpanderItem(group,
            "悬浮窗层级",
            "默认置底不会遮挡其他窗口；不推荐置顶，可能遮挡应用",
            layerComboBox);

        // 悬浮窗层级设置频率（ComboBox 五档 + 50ms/1ms 警告色文本，深浅自适应）
        var recheckComboBox = new ComboBox
        {
            Width = 240,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var recheckWarningOrange = new SolidColorBrush(Color.Parse("#FF8C5A00"));
        var recheckWarningOrangeRed = new SolidColorBrush(Color.Parse("#FF4A3400"));
        recheckComboBox.Items.Add(new TextBlock { Text = "窗口层级变化时（默认）", TextWrapping = TextWrapping.NoWrap });
        recheckComboBox.Items.Add(new TextBlock { Text = "前台窗口变化时", TextWrapping = TextWrapping.NoWrap });
        var warn50Text = new TextBlock
        {
            Text = "⚠",
            FontSize = 18,
            Foreground = recheckWarningOrange,
            Margin = new Thickness(0, -3, 0, -3),
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(warn50Text, "较高频率可能带来性能占用与轻微闪烁，仅当置顶频繁丢失时使用。");
        recheckComboBox.Items.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = "每 50ms", VerticalAlignment = VerticalAlignment.Center },
                warn50Text
            }
        });
        var warn1Text = new TextBlock
        {
            Text = "⚠",
            FontSize = 18,
            Foreground = recheckWarningOrangeRed,
            Margin = new Thickness(0, -3, 0, -3),
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(warn1Text, "极高频率重设：明显占用 UI 线程、可能导致主窗口/悬浮窗闪烁，仅作极端调试用途。");
        recheckComboBox.Items.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = "每 1ms", VerticalAlignment = VerticalAlignment.Center },
                warn1Text
            }
        });
        recheckComboBox.Items.Add(new TextBlock { Text = "每 2s" });
        int InitRecheckIndexFromMode(FloatingTopmostRefreshMode m) => m switch
        {
            FloatingTopmostRefreshMode.OnWindowZOrderChanged => 0,
            FloatingTopmostRefreshMode.OnForegroundWindowChanged => 1,
            FloatingTopmostRefreshMode.Every50Ms => 2,
            FloatingTopmostRefreshMode.Every1Ms => 3,
            FloatingTopmostRefreshMode.Every2s => 4,
            _ => 0
        };
        FloatingTopmostRefreshMode ModeFromRecheckIndex(int idx) => idx switch
        {
            0 => FloatingTopmostRefreshMode.OnWindowZOrderChanged,
            1 => FloatingTopmostRefreshMode.OnForegroundWindowChanged,
            2 => FloatingTopmostRefreshMode.Every50Ms,
            3 => FloatingTopmostRefreshMode.Every1Ms,
            4 => FloatingTopmostRefreshMode.Every2s,
            _ => FloatingTopmostRefreshMode.OnWindowZOrderChanged
        };
        recheckComboBox.SelectedIndex = InitRecheckIndexFromMode(
            _settings?.FloatingScheduleTopmostRefreshMode ?? FloatingTopmostRefreshMode.Every50Ms);
        recheckComboBox.SelectionChanged += (s, e) =>
        {
            if (_settings != null && s is ComboBox cb)
                _settings.FloatingScheduleTopmostRefreshMode = ModeFromRecheckIndex(cb.SelectedIndex);
        };
        AddSettingsExpanderItem(group,
            "悬浮窗层级设置频率",
            "ClassIsland 在什么时候重新把悬浮窗设回用户选择的层级（置底/置顶）。用于防止其他窗口挤占悬浮窗层级。\n高频设置会带来性能占用，并可能导致界面轻微闪烁。",
            recheckComboBox);

        // 隐藏悬浮窗（隐藏规则模式）
        var hideModeComboBox = new ComboBox
        {
            Width = 220,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        hideModeComboBox.Items.Add(new TextBlock { Text = "跟随主界面隐藏规则（默认）", VerticalAlignment = VerticalAlignment.Center });
        hideModeComboBox.Items.Add(new TextBlock { Text = "基础模式", VerticalAlignment = VerticalAlignment.Center });
        hideModeComboBox.Items.Add(new TextBlock { Text = "高级模式（规则集）", VerticalAlignment = VerticalAlignment.Center });
        hideModeComboBox.Items.Add(new TextBlock { Text = "从不隐藏", VerticalAlignment = VerticalAlignment.Center });
        int InitHideIndexFromMode(FloatingScheduleHideMode m) => m switch
        {
            FloatingScheduleHideMode.FollowHost => 0,
            FloatingScheduleHideMode.Basic => 1,
            FloatingScheduleHideMode.Advanced => 2,
            FloatingScheduleHideMode.Never => 3,
            _ => 0
        };
        FloatingScheduleHideMode HideModeFromIndex(int idx) => idx switch
        {
            0 => FloatingScheduleHideMode.FollowHost,
            1 => FloatingScheduleHideMode.Basic,
            2 => FloatingScheduleHideMode.Advanced,
            3 => FloatingScheduleHideMode.Never,
            _ => FloatingScheduleHideMode.FollowHost
        };
        hideModeComboBox.SelectedIndex = InitHideIndexFromMode(
            _settings?.FloatingScheduleHideMode ?? FloatingScheduleHideMode.FollowHost);
        hideModeComboBox.SelectionChanged += (s, e) =>
        {
            if (_settings != null && s is ComboBox cb)
                _settings.FloatingScheduleHideMode = HideModeFromIndex(cb.SelectedIndex);
        };
        AddSettingsExpanderItem(group,
            "隐藏悬浮窗",
            "在什么条件下自动隐藏悬浮窗。\n· 跟随主界面隐藏规则：主窗口隐藏时悬浮窗同步隐藏。\n· 基础模式：复用 ClassIsland 的\"上课时隐藏 / 前台窗口最大化时隐藏 / 前台窗口全屏时隐藏\"三个开关独立判定。\n· 高级模式（规则集）：复用 ClassIsland 主界面的隐藏规则集（在 ClassIsland 设置中编辑规则）。",
            hideModeComboBox);

        mainPanel.Children.Add(group);
    }

    // ==================== 4. 随机窗口名 ====================
    private void BuildRandomTitleGroup(StackPanel mainPanel)
    {
        var group = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Header", "随机窗口名");
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Description", "为了防止弹窗拦截工具把时间表悬浮窗拦截，建议开启此项");

        // 子项：增强随机模式（先声明，供主开关联动闭包引用；仅主开关开启时激活）
        var enhancedToggle = CreateToggleSwitch(
            (_settings?.FloatingScheduleRandomTitleEnhanced ?? false) &&
            (_settings?.FloatingScheduleRandomTitle ?? false), isOn =>
            {
                if (_settings == null) return;
                if (_settings.FloatingScheduleRandomTitle)
                    _settings.FloatingScheduleRandomTitleEnhanced = isOn;
            });
        enhancedToggle.HorizontalAlignment = HorizontalAlignment.Right;
        enhancedToggle.VerticalAlignment = VerticalAlignment.Center;

        // 主开关：随机窗口名（放在折叠栏 Footer，与"实验性功能"样式一致）
        var randomTitleToggle = CreateToggleSwitch(_settings?.FloatingScheduleRandomTitle ?? false, isOn =>
        {
            if (_settings == null) return;
            _settings.FloatingScheduleRandomTitle = isOn;
            // 主开关关闭时同步把增强开关置 false + 置灰；打开时恢复可用
            if (!isOn)
            {
                enhancedToggle.IsEnabled = false;
                enhancedToggle.IsChecked = false;
                if (_settings.FloatingScheduleRandomTitleEnhanced)
                    _settings.FloatingScheduleRandomTitleEnhanced = false;
            }
            else
            {
                enhancedToggle.IsEnabled = true;
            }
        });
        randomTitleToggle.HorizontalAlignment = HorizontalAlignment.Right;
        randomTitleToggle.VerticalAlignment = VerticalAlignment.Center;
        var randomTitleFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        randomTitleFooter.Children.Add(randomTitleToggle);
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Footer", randomTitleFooter);

        var enhancedItem = AddSettingsExpanderItem(group,
            "增强随机模式",
            "仅在上一项启用时生效：开启后每 1 秒重新设置一次随机窗口标题，防止拦截工具按标题缓存识别。",
            enhancedToggle);
        // 联动：主开关关闭时增强项置灰（值保留）
        static void SyncEnhancedEnabled(Control? item, PluginSettings? s)
        {
            if (item == null || s == null) return;
            item.IsEnabled = s.FloatingScheduleRandomTitle;
        }
        SyncEnhancedEnabled(enhancedItem, _settings);
        if (_settings != null)
        {
            _settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PluginSettings.FloatingScheduleRandomTitle))
                    SyncEnhancedEnabled(enhancedItem, _settings);
            };
        }

        // 防止截图（独立开关，不依赖随机窗口名）
        var preventCaptureToggle = CreateToggleSwitch(_settings?.FloatingSchedulePreventCapture ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingSchedulePreventCapture = isOn;
        });
        preventCaptureToggle.HorizontalAlignment = HorizontalAlignment.Right;
        preventCaptureToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "防止截图",
            "开启后其他应用无法捕获悬浮窗内容：截屏/录屏结果中悬浮窗不显示（透出下方内容），防止学校截图检测。Windows 10 2004 及以上有效，更低版本回退为黑色遮挡。",
            preventCaptureToggle);

        mainPanel.Children.Add(group);
    }

    // ==================== 辅助方法（与 PluginSettingsPage 同构） ====================
    private ToggleSwitch CreateToggleSwitch(bool isOn, Action<bool> onToggleChanged)
    {
        var toggle = new ToggleSwitch
        {
            IsChecked = isOn
        };
        FluentAvaloniaCompatibilityHelper.AddCheckedHandler(toggle, (s, e) => onToggleChanged?.Invoke(true));
        FluentAvaloniaCompatibilityHelper.AddUncheckedHandler(toggle, (s, e) => onToggleChanged?.Invoke(false));
        return toggle;
    }

    private Control? AddSettingsExpanderItem(Control expander, string content, string description, Control? footerContent)
    {
        var item = FluentAvaloniaCompatibilityHelper.CreateSettingsExpanderItem();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(item, "Content", content);
        if (!string.IsNullOrEmpty(description))
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(item, "Description", description);
        }

        if (footerContent != null)
        {
            var footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            footerPanel.Children.Add(footerContent);
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(item, "Footer", footerPanel);
        }

        AddChildToSettingsExpander(expander, item);
        return item;
    }

    private void AddChildToSettingsExpander(Control expander, Control child)
    {
        var type = expander.GetType();
        var itemsProperty = type.GetProperty("Items");
        if (itemsProperty != null)
        {
            var items = itemsProperty.GetValue(expander) as System.Collections.IList;
            if (items != null)
            {
                items.Add(child);
                return;
            }
        }

        var panel = expander as Panel;
        if (panel != null)
        {
            panel.Children.Add(child);
            return;
        }

        var contentProperty = type.GetProperty("Content");
        if (contentProperty != null)
        {
            var currentContent = contentProperty.GetValue(expander);
            if (currentContent is StackPanel stackPanel)
            {
                stackPanel.Children.Add(child);
            }
            else if (currentContent == null)
            {
                var newPanel = new StackPanel();
                newPanel.Children.Add(child);
                contentProperty.SetValue(expander, newPanel);
            }
        }
    }
}
