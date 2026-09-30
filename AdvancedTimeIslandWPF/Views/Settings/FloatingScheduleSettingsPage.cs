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

        BuildEnableSwitchCard(mainPanel);    // 主开关单独展示在最前（不放进折叠栏）
        BuildBasicAppearanceGroup(mainPanel);
        BuildInteractionGroup(mainPanel);
        BuildWindowAdvancedGroup(mainPanel);
        BuildRandomTitleGroup(mainPanel);
        BuildIndependentProcessGroup(mainPanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    // ==================== 0. 启用悬浮时间表（单独展示，不放进折叠栏） ====================
    /// <summary>
    /// 悬浮窗总开关：按需求移出"基础外观"折叠栏，作为独立卡片展示在页面最前，
    /// 使"悬浮窗开没开"一眼可见（此前它藏在折叠栏内，被外部关掉后不易察觉）。
    /// </summary>
    private void BuildEnableSwitchCard(StackPanel mainPanel)
    {
        var toggle = new System.Windows.Controls.Primitives.ToggleButton
        {
            IsChecked = _settings?.EnableFloatingSchedule ?? false,
            Style = System.Windows.Application.Current?.TryFindResource("MaterialDesignSwitchToggleButton") as System.Windows.Style,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        void ApplyToggle()
        {
            if (_settings != null) _settings.EnableFloatingSchedule = toggle.IsChecked == true;
        }
        toggle.Checked += (_, _) => ApplyToggle();
        toggle.Unchecked += (_, _) => ApplyToggle();

        var textPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center
        };
        textPanel.Children.Add(new TextBlock
        {
            Text = "启用悬浮时间表",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = ThemeHelper.GetTextBrush()
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = "打开后在桌面显示半透明悬浮课表窗口，关闭后窗口自动隐藏",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(textPanel, 0);
        grid.Children.Add(textPanel);
        Grid.SetColumn(toggle, 1);
        grid.Children.Add(toggle);

        mainPanel.Children.Add(new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 16),
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
                if (toggle.IsChecked != on) toggle.IsChecked = on;
            };
        }
    }

    // ==================== 1. 基础外观 ====================
    private void BuildBasicAppearanceGroup(StackPanel mainPanel)
    {
        var group = CreateGroup("基础外观", "悬浮窗的外观显示设置");
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
        var opacityItem = CreateItem("背景不透明度", "仅作用于卡片背景（不影响文字/进度条可读性）：1.0 完全不透明，0 完全透明；默认 0.5", "Opacity");
        var opacityNumeric = new WpfNumericUpDown
        {
            Width = 155,
            Minimum = 0.0m,
            Maximum = 1.0m,
            Increment = 0.05m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left,
            Value = (decimal)Math.Clamp(_settings?.FloatingScheduleOpacity ?? 0.5, 0.0, 1.0)
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

        // 显示明天课表（四档，参考 ClassIsland 课程表组件的 TomorrowScheduleShowMode）
        var tomorrowModeItem = CreateItem("显示明天课表",
            "设置什么时候在悬浮窗显示明天课表。\n· 不显示：始终显示当天课表。\n· 放学后显示（默认）：当天放学后（或当天课表未加载时）自动切换为明天课表。\n· 总是显示：始终显示明天课表。\n· 无展示课程时显示：当天没有可展示课程时切换为明天课表。",
            "CalendarArrowRight");
        var tomorrowModeCombo = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
        tomorrowModeCombo.Items.Add("不显示");
        tomorrowModeCombo.Items.Add("放学后显示（默认）");
        tomorrowModeCombo.Items.Add("总是显示");
        tomorrowModeCombo.Items.Add("无展示课程时显示");
        static int TomorrowModeToIndex(FloatingScheduleTomorrowShowMode m) => m switch
        {
            FloatingScheduleTomorrowShowMode.Never => 0,
            FloatingScheduleTomorrowShowMode.AfterSchool => 1,
            FloatingScheduleTomorrowShowMode.Always => 2,
            FloatingScheduleTomorrowShowMode.OnEmpty => 3,
            _ => 1
        };
        static FloatingScheduleTomorrowShowMode TomorrowIndexToMode(int i) => i switch
        {
            0 => FloatingScheduleTomorrowShowMode.Never,
            1 => FloatingScheduleTomorrowShowMode.AfterSchool,
            2 => FloatingScheduleTomorrowShowMode.Always,
            3 => FloatingScheduleTomorrowShowMode.OnEmpty,
            _ => FloatingScheduleTomorrowShowMode.AfterSchool
        };
        tomorrowModeCombo.SelectedIndex = TomorrowModeToIndex(
            _settings?.FloatingScheduleTomorrowShowMode ?? FloatingScheduleTomorrowShowMode.AfterSchool);
        tomorrowModeCombo.SelectionChanged += (s, e) =>
        {
            if (_settings != null && s is ComboBox cb)
                _settings.FloatingScheduleTomorrowShowMode = TomorrowIndexToMode(cb.SelectedIndex);
        };
        tomorrowModeItem.Switcher = tomorrowModeCombo;
        group.Items.Add(tomorrowModeItem);

        // 今天无课程时的占位符
        var todayPlaceholderItem = CreateItem("今天无课程时的占位符",
            "显示当天课表、但当天没有课程时展示的文字；默认为\"今天没有课程\"。留空时回退为默认文案。",
            "TextFields");
        var todayPlaceholderBox = new TextBox
        {
            Width = 220,
            Text = _settings?.FloatingScheduleTodayPlaceholderText ?? "今天没有课程",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        todayPlaceholderBox.TextChanged += (s, e) =>
        {
            if (_settings != null && s is TextBox tb)
                _settings.FloatingScheduleTodayPlaceholderText = tb.Text ?? string.Empty;
        };
        todayPlaceholderItem.Switcher = todayPlaceholderBox;
        group.Items.Add(todayPlaceholderItem);

        // 明天无课程时的占位符
        var tomorrowPlaceholderItem = CreateItem("明天无课程时的占位符",
            "显示明天课表、但明天没有课程时展示的文字；默认为\"明天没有课程\"。留空时回退为默认文案。",
            "TextFields");
        var tomorrowPlaceholderBox = new TextBox
        {
            Width = 220,
            Text = _settings?.FloatingScheduleTomorrowPlaceholderText ?? "明天没有课程",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        tomorrowPlaceholderBox.TextChanged += (s, e) =>
        {
            if (_settings != null && s is TextBox tb)
                _settings.FloatingScheduleTomorrowPlaceholderText = tb.Text ?? string.Empty;
        };
        tomorrowPlaceholderItem.Switcher = tomorrowPlaceholderBox;
        group.Items.Add(tomorrowPlaceholderItem);

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

    // ==================== 5. 独立进程模式 ====================
    private System.Windows.Threading.DispatcherTimer? _independentStatusTimer;

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
        var group = CreateGroup("独立进程模式",
            "由独立进程 AdvancedTimeIslandWPFFloatSchedule.exe 渲染悬浮窗，防弹窗拦截能力更强（超级模式），且不受 ClassIsland 主程序卡顿影响。仅支持 Windows，非 Windows 平台此项禁用。",
            "ApplicationExport");

        // 非 Windows（Linux/macOS/Android）整组禁用
        if (!OperatingSystem.IsWindows()) group.IsEnabled = false;

        // 1) 独立进程模式（主开关）
        var independentItem = CreateItem("独立进程模式",
            "开启后悬浮课表改由独立进程渲染（进程名 AdvancedTimeIslandWPFFloatSchedule）；关闭后回到进程内渲染。切换即时生效。",
            "ApplicationExport");
        independentItem.IsOn = _settings?.FloatingScheduleIndependentProcess ?? false;
        WatchIsOn(independentItem, () =>
        {
            if (_settings != null) _settings.FloatingScheduleIndependentProcess = independentItem.IsOn;
        });
        group.Items.Add(independentItem);

        // 2) 随机进程名（与主开关联动禁用）
        var randomNameItem = CreateItem("随机进程名",
            "以随机命名的临时副本启动子进程（任务管理器中进程名随机），进一步规避按进程名拦截。可能触发杀软误报，请知悉。",
            "DiceMultipleOutline");
        randomNameItem.IsOn = _settings?.FloatingScheduleRandomProcessName ?? false;
        WatchIsOn(randomNameItem, () =>
        {
            if (_settings != null) _settings.FloatingScheduleRandomProcessName = randomNameItem.IsOn;
        });
        group.Items.Add(randomNameItem);

        // 3) 强制重启进程（按钮）
        var restartButton = new Button
        {
            Content = "重启",
            Padding = new Thickness(14, 5, 14, 5),
            HorizontalAlignment = HorizontalAlignment.Left
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
        var restartItem = CreateItem("强制重启进程",
            "立即终止并以当前设置重新启动子进程悬浮窗（约 1~2 秒恢复，无需重启 ClassIsland）。",
            "Restart");
        restartItem.Switcher = restartButton;
        group.Items.Add(restartItem);

        // 4) 单实例保护
        var singleInstanceItem = CreateItem("单实例保护",
            "已有子进程实例时新进程直接退出并由插件接管连接。自下次子进程启动生效（无需重启）。",
            "ShieldAccountOutline");
        singleInstanceItem.IsOn = _settings?.FloatingScheduleSingleInstanceProtection ?? true;
        WatchIsOn(singleInstanceItem, () =>
        {
            if (_settings != null) _settings.FloatingScheduleSingleInstanceProtection = singleInstanceItem.IsOn;
        });
        group.Items.Add(singleInstanceItem);

        // 5) 跟随启停
        var followItem = CreateItem("跟随启停",
            "开启后，ClassIsland 关闭或异常停止后，此程序将关闭；关闭后悬浮窗冻结显示最后课表（进度本地续走），ClassIsland 重启后自动重连恢复推送。",
            "PowerPlugOutline");
        followItem.IsOn = _settings?.FloatingScheduleFollowHostLifetime ?? true;
        WatchIsOn(followItem, () =>
        {
            if (_settings != null) _settings.FloatingScheduleFollowHostLifetime = followItem.IsOn;
        });
        group.Items.Add(followItem);

        // 6) 子进程状态（1s 轮询 HostProcessService.StatusText）
        var statusItem = CreateItem("子进程状态", "", "InformationOutline");
        var statusText = new TextBlock
        {
            Text = "状态：未启用",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusItem.Switcher = statusText;
        group.Items.Add(statusItem);

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
                if (independentItem.IsOn != on) independentItem.IsOn = on;
            };
        }

        _independentStatusTimer = new System.Windows.Threading.DispatcherTimer
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
        Unloaded += (_, _) =>
        {
            try { _independentStatusTimer?.Stop(); } catch { }
            _independentStatusTimer = null;
        };

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
