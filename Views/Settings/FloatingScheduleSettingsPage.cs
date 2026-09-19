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
/// 时间表悬浮窗设置页面（从主设置页拆出；与主设置页同属 AdvancedTimeIsland 导航分组）。
/// 设置项按三个折叠面板分组：基础外观 / 交互行为 / 窗口与高级。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandFloatingSchedule", "时间表悬浮窗")]
[Group("advancedtimeisland.main")]
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

        BuildEnableSwitchCard(mainPanel);   // 主开关单独展示在最前（不放进折叠栏）
        BuildBasicAppearanceGroup(mainPanel);
        BuildInteractionGroup(mainPanel);
        BuildWindowAdvancedGroup(mainPanel);
        BuildRandomTitleGroup(mainPanel);
        BuildIndependentProcessGroup(mainPanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    private TextBlock? _titleTextBlock;

    // ==================== 0. 启用悬浮时间表（单独展示，不放进折叠栏） ====================
    /// <summary>
    /// 悬浮窗总开关：按需求移出"基础外观"折叠栏，作为独立卡片展示在页面最前，
    /// 使"悬浮窗开没开"一眼可见（此前它藏在折叠栏内，被外部关掉后不易察觉）。
    /// 卡片样式对齐插件其它页面（ThemeHelper 卡片底色 + 深浅自适应文字）。
    /// </summary>
    private void BuildEnableSwitchCard(StackPanel mainPanel)
    {
        var enableToggle = CreateToggleSwitch(_settings?.EnableFloatingSchedule ?? false, isOn =>
        {
            if (_settings != null) _settings.EnableFloatingSchedule = isOn;
        });
        enableToggle.HorizontalAlignment = HorizontalAlignment.Right;
        enableToggle.VerticalAlignment = VerticalAlignment.Center;

        var textPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        textPanel.Children.Add(new TextBlock
        {
            Text = "启用悬浮时间表",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeHelper.GetTextBrush()
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = "打开后在桌面显示半透明悬浮课表窗口，关闭后窗口自动隐藏",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        });

        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("*, Auto"),
            ColumnSpacing = 12
        };
        Grid.SetColumn(textPanel, 0);
        grid.Children.Add(textPanel);
        Grid.SetColumn(enableToggle, 1);
        grid.Children.Add(enableToggle);

        mainPanel.Children.Add(new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Child = grid
        });

        // 【修复：托盘"退出"后设置页开关不跟随真实设置】子进程托盘"退出"、自动化行动等会在外部改这个开关；
        //  若页面不跟随，用户看到的仍是"已开启"，于是只在插件端去动别的项（如独立进程模式），
        //  主开关实际仍是关的 → 悬浮窗始终不显示（用户所见："托盘退出后重启，悬浮窗不可见"）。
        if (_settings != null)
        {
            _settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(PluginSettings.EnableFloatingSchedule)) return;
                var on = _settings.EnableFloatingSchedule;
                if (enableToggle.IsChecked != on) enableToggle.IsChecked = on;
            };
        }
    }

    // ==================== 1. 基础外观 ====================
    private void BuildBasicAppearanceGroup(StackPanel mainPanel)
    {
        var group = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Header", "基础外观");
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Description", "悬浮窗的外观显示设置");

        // 启用悬浮时间表（已移出本折叠栏，单独展示在页面最前，见 BuildEnableSwitchCard）
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
            Value = (decimal?)Math.Clamp(_settings?.FloatingScheduleOpacity ?? 0.5, 0.0, 1.0),
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
            "仅作用于卡片背景（不影响文字/进度条可读性）：1.0 完全不透明，0 完全透明；默认 0.5",
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

        // 显示明天课表（四档，参考 ClassIsland 课程表组件的 TomorrowScheduleShowMode）
        var tomorrowModeComboBox = new ComboBox
        {
            Width = 220,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        tomorrowModeComboBox.Items.Add(new TextBlock { Text = "不显示", VerticalAlignment = VerticalAlignment.Center });
        tomorrowModeComboBox.Items.Add(new TextBlock { Text = "放学后显示（默认）", VerticalAlignment = VerticalAlignment.Center });
        tomorrowModeComboBox.Items.Add(new TextBlock { Text = "总是显示", VerticalAlignment = VerticalAlignment.Center });
        tomorrowModeComboBox.Items.Add(new TextBlock { Text = "无展示课程时显示", VerticalAlignment = VerticalAlignment.Center });
        int InitTomorrowIndexFromMode(FloatingScheduleTomorrowShowMode m) => m switch
        {
            FloatingScheduleTomorrowShowMode.Never => 0,
            FloatingScheduleTomorrowShowMode.AfterSchool => 1,
            FloatingScheduleTomorrowShowMode.Always => 2,
            FloatingScheduleTomorrowShowMode.OnEmpty => 3,
            _ => 1
        };
        FloatingScheduleTomorrowShowMode TomorrowModeFromIndex(int idx) => idx switch
        {
            0 => FloatingScheduleTomorrowShowMode.Never,
            1 => FloatingScheduleTomorrowShowMode.AfterSchool,
            2 => FloatingScheduleTomorrowShowMode.Always,
            3 => FloatingScheduleTomorrowShowMode.OnEmpty,
            _ => FloatingScheduleTomorrowShowMode.AfterSchool
        };
        tomorrowModeComboBox.SelectedIndex = InitTomorrowIndexFromMode(
            _settings?.FloatingScheduleTomorrowShowMode ?? FloatingScheduleTomorrowShowMode.AfterSchool);
        tomorrowModeComboBox.SelectionChanged += (s, e) =>
        {
            if (_settings != null && s is ComboBox cb)
                _settings.FloatingScheduleTomorrowShowMode = TomorrowModeFromIndex(cb.SelectedIndex);
        };
        AddSettingsExpanderItem(group,
            "显示明天课表",
            "设置什么时候在悬浮窗显示明天课表。\n· 不显示：始终显示当天课表。\n· 放学后显示（默认）：当天放学后（或当天课表未加载时）自动切换为明天课表。\n· 总是显示：始终显示明天课表。\n· 无展示课程时显示：当天没有可展示课程时切换为明天课表。",
            tomorrowModeComboBox);

        // 今天无课程时的占位符（与"明天无课程时的占位符"对齐）
        var todayPlaceholderBox = new TextBox
        {
            Width = 220,
            Text = _settings?.FloatingScheduleTodayPlaceholderText ?? "今天没有课程",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        todayPlaceholderBox.TextChanged += (s, e) =>
        {
            if (_settings != null && s is TextBox tb)
                _settings.FloatingScheduleTodayPlaceholderText = tb.Text ?? string.Empty;
        };
        AddSettingsExpanderItem(group,
            "今天无课程时的占位符",
            "显示当天课表、但当天没有课程时展示的文字；默认为\"今天没有课程\"。留空时回退为默认文案。",
            todayPlaceholderBox);

        // 明天无课程时的占位符
        var tomorrowPlaceholderBox = new TextBox
        {
            Width = 220,
            Text = _settings?.FloatingScheduleTomorrowPlaceholderText ?? "明天没有课程",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        tomorrowPlaceholderBox.TextChanged += (s, e) =>
        {
            if (_settings != null && s is TextBox tb)
                _settings.FloatingScheduleTomorrowPlaceholderText = tb.Text ?? string.Empty;
        };
        AddSettingsExpanderItem(group,
            "明天无课程时的占位符",
            "显示明天课表、但明天没有课程时展示的文字；默认为\"明天没有课程\"。留空时回退为默认文案。",
            tomorrowPlaceholderBox);

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

        // 阻止截图（独立开关，不依赖随机窗口名）
        var preventCaptureToggle = CreateToggleSwitch(_settings?.FloatingSchedulePreventCapture ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingSchedulePreventCapture = isOn;
        });
        preventCaptureToggle.HorizontalAlignment = HorizontalAlignment.Right;
        preventCaptureToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "阻止截图",
            "开启后，其他应用无法截取悬浮窗窗口内容，录制时也不会录制到悬浮窗。Windows 10 2004 及以上有效，更低版本回退为黑色遮挡。",
            preventCaptureToggle);

        mainPanel.Children.Add(group);
    }

    // ==================== 5. 独立进程模式 ====================
    private Avalonia.Threading.DispatcherTimer? _independentStatusTimer;

    /// <summary>
    /// "重启"按钮是否可用：模式开启 且 不在重启过程中 且 子进程不在启动过程中。
    /// 统一出口，供按钮点击收尾 / 1s 轮询 / 模式开关联动三处复用。
    /// </summary>
    private bool ShouldEnableRestartButton()
    {
        var svc = Services.FloatScheduleHostProcessService.Instance;
        var on = _settings?.FloatingScheduleIndependentProcess ?? false;
        return on && svc != null && !svc.IsRestarting
            && svc.Status != Services.FloatScheduleHostProcessService.ChildStatus.Starting;
    }

    private void BuildIndependentProcessGroup(StackPanel mainPanel)
    {
        var group = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Header", "独立进程模式");
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(group, "Description",
            "由独立进程 AdvancedTimeIslandFloatSchedule.exe 渲染悬浮窗，防弹窗拦截能力更强（超级模式），且不受 ClassIsland 主程序卡顿影响。仅支持 Windows，非 Windows 平台此项禁用。");

        // 非 Windows（Linux/macOS/Android）整组禁用
        if (!OperatingSystem.IsWindows()) group.IsEnabled = false;

        // 1) 独立进程模式（主开关）
        var independentToggle = CreateToggleSwitch(_settings?.FloatingScheduleIndependentProcess ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleIndependentProcess = isOn;
        });
        independentToggle.HorizontalAlignment = HorizontalAlignment.Right;
        independentToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "独立进程模式",
            "开启后悬浮课表改由独立进程渲染（进程名 AdvancedTimeIslandFloatSchedule）；关闭后回到进程内渲染。切换即时生效。",
            independentToggle);

        // 2) 随机进程名（与主开关联动禁用）
        var randomNameToggle = CreateToggleSwitch(_settings?.FloatingScheduleRandomProcessName ?? false, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleRandomProcessName = isOn;
        });
        randomNameToggle.HorizontalAlignment = HorizontalAlignment.Right;
        randomNameToggle.VerticalAlignment = VerticalAlignment.Center;
        var randomNameItem = AddSettingsExpanderItem(group,
            "随机进程名",
            "以随机命名的临时副本启动子进程（任务管理器中进程名随机），进一步规避按进程名拦截。可能触发杀软误报，请知悉。",
            randomNameToggle);

        // 3) 强制重启进程（按钮）
        var restartButton = new Button
        {
            Content = "重启",
            Padding = new Thickness(14, 5),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        restartButton.Click += async (_, _) =>
        {
            var svc = Services.FloatScheduleHostProcessService.Instance;
            if (svc == null || svc.IsRestarting) return;
            // 重启过程立即禁用按钮：避免连点导致 Stop/Stop 交错（服务侧另有互斥兜底）
            restartButton.IsEnabled = false;
            try { await svc.RestartChildAsync(); }
            catch { }
            finally
            {
                restartButton.IsEnabled = ShouldEnableRestartButton();
            }
        };
        var restartItem = AddSettingsExpanderItem(group,
            "强制重启进程",
            "立即终止并以当前设置重新启动子进程悬浮窗（约 1~2 秒恢复，无需重启 ClassIsland）。",
            restartButton);

        // 4) 单实例保护
        var singleInstanceToggle = CreateToggleSwitch(_settings?.FloatingScheduleSingleInstanceProtection ?? true, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleSingleInstanceProtection = isOn;
        });
        singleInstanceToggle.HorizontalAlignment = HorizontalAlignment.Right;
        singleInstanceToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "单实例保护",
            "已有子进程实例时新进程直接退出并由插件接管连接。自下次子进程启动生效（无需重启）。",
            singleInstanceToggle);

        // 5) 跟随启停
        var followToggle = CreateToggleSwitch(_settings?.FloatingScheduleFollowHostLifetime ?? true, isOn =>
        {
            if (_settings != null) _settings.FloatingScheduleFollowHostLifetime = isOn;
        });
        followToggle.HorizontalAlignment = HorizontalAlignment.Right;
        followToggle.VerticalAlignment = VerticalAlignment.Center;
        AddSettingsExpanderItem(group,
            "跟随启停",
            "开启后，ClassIsland 关闭或异常停止后，此程序将关闭；关闭后悬浮窗冻结显示最后课表（进度本地续走），ClassIsland 重启后自动重连恢复推送。",
            followToggle);

        // 状态文本（随主题深浅自适应；1s 轮询 HostProcessService.StatusText）
        var statusText = new TextBlock
        {
            Text = "状态：未启用",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            Margin = new Thickness(0, 2, 0, 0)
        };
        AddSettingsExpanderItem(group, "子进程状态", "", statusText);

        // 联动：随机进程名/重启按钮仅在独立模式开启（且不在重启/启动过程中）时可用
        void SyncEnabled()
        {
            var on = _settings?.FloatingScheduleIndependentProcess ?? false;
            randomNameItem.IsEnabled = on;
            restartButton.IsEnabled = ShouldEnableRestartButton();
        }
        SyncEnabled();
        if (_settings != null)
        {
            _settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(PluginSettings.FloatingScheduleIndependentProcess)) return;
                SyncEnabled();
                // 【修复】子进程托盘"退出"会连带关闭本开关：页面必须同步为真实值，
                //  否则用户看到它仍是开的，重启悬浮窗的动作会落到"其实没开"的状态上（悬浮窗不可见）。
                var on = _settings.FloatingScheduleIndependentProcess;
                if (independentToggle.IsChecked != on) independentToggle.IsChecked = on;
            };
        }

        _independentStatusTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _independentStatusTimer.Tick += (_, _) =>
        {
            try
            {
                var svc = Services.FloatScheduleHostProcessService.Instance;
                statusText.Text = "状态：" + (svc?.StatusText ?? "未启用");
                // 【权威同步】重启进行中 / 子进程启动中 → 临时禁用"重启"按钮，避免过程中连点
                restartButton.IsEnabled = ShouldEnableRestartButton();
            }
            catch { }
        };
        _independentStatusTimer.Start();
        // 页面离开时停表防泄漏
        DetachedFromVisualTree += (_, _) =>
        {
            try { _independentStatusTimer?.Stop(); } catch { }
            _independentStatusTimer = null;
        };

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
