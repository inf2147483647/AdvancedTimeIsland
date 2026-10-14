// ATI 高级外观 · 独立编辑器窗口（非 CI 设置页）。
// 三栏：左导航（窗口/文本/表格/主题/调试）+ 中部设置项 + 右侧 1:1 实时预览（可拖动、滚轮缩放）。
// 读写与悬浮窗共用的 PluginSettings；任何改动即时写回（悬浮窗经 INPC 热应用）并刷新预览。
using System;
using System.Collections.Generic;
using System.Globalization;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Shared.FloatingSchedule;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace AdvancedTimeIsland.Views.Settings;

public sealed class FloatingScheduleAdvancedAppearanceWindow : Window
{
    static FloatingScheduleAdvancedAppearanceWindow? _instance;

    readonly PluginSettings? _settings;
    readonly ContentControl _centerHost = new();
    readonly Border _previewCard = new();
    readonly Canvas _previewCanvas = new();
    double _zoom = 1.0;
    bool _suppress;

    public static void ShowWindow(PluginSettings? settings, Window? owner)
    {
        if (_instance != null)
        {
            try { _instance.Activate(); } catch { }
            return;
        }
        var w = new FloatingScheduleAdvancedAppearanceWindow(settings) { Owner = owner };
        _instance = w;
        w.Closed += (_, _) => _instance = null;
        w.Show();
    }

    public FloatingScheduleAdvancedAppearanceWindow(PluginSettings? settings)
    {
        _settings = settings;
        Title = "ATI 高级外观";
        Width = 1180; Height = 760;
        MinWidth = 900; MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;

        var root = new Grid { Margin = new Thickness(12, 0, 12, 12) };
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(160)));
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(420)));

        // ---- 左导航 ----
        var nav = new ListBox
        {
            ItemsSource = new[] { "窗口", "文本", "表格", "主题", "调试" },
            SelectedIndex = 0,
            Margin = new Thickness(0, 0, 12, 0),
        };
        nav.SelectionChanged += (_, _) => SwitchPanel(nav.SelectedIndex);
        Grid.SetColumn(nav, 0);
        root.Children.Add(nav);

        // ---- 中部设置区 ----
        var centerScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 0, 12, 0) };
        centerScroll.Content = _centerHost;
        Grid.SetColumn(centerScroll, 1);
        root.Children.Add(centerScroll);

        // ---- 右预览 ----
        var right = new Grid { RowDefinitions = RowDefinitions.Parse("Auto,*") };
        right.Children.Add(new TextBlock { Text = "实时预览（1:1，滚轮缩放 / 拖动）", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var previewHost = new Border { Background = MakeCheckerBrush(), CornerRadius = new CornerRadius(8), ClipToBounds = true };
        _previewCanvas.Width = 396; _previewCanvas.Height = 640;
        _previewCard.Margin = new Thickness(0);
        var previewInner = new Panel();
        previewInner.Children.Add(_previewCanvas);
        previewHost.Child = previewInner;
        previewHost.PointerWheelChanged += PreviewHost_PointerWheelChanged;
        Grid.SetRow(previewHost, 1);
        right.Children.Add(previewHost);
        Grid.SetColumn(right, 2);
        root.Children.Add(right);

        var outer = new DockPanel();
        // 顶端警告 InfoBar（FluentAvalonia，经兼容层屏蔽 FA2/FA3 类型名差异；警告级、不可关闭、深浅自适应）
        var devWarn = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Title", "注意！");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Message", "正在开发中，部分功能有缺陷，欢迎提出 issue");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "IsClosable", false);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Margin", new Thickness(12, 12, 12, 10));
        DockPanel.SetDock(devWarn, Dock.Top);
        outer.Children.Add(devWarn);
        outer.Children.Add(root);
        Content = outer;

        if (_settings != null) _settings.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(RebuildPreview);
        SwitchPanel(0);
        RebuildPreview();
    }

    void SwitchPanel(int idx)
    {
        _centerHost.Content = idx switch
        {
            0 => BuildWindowPanel(),
            1 => BuildTextPanel(),
            2 => BuildTablePanel(),
            3 => BuildThemePanel(),
            4 => BuildDebugPanel(),
            _ => new StackPanel(),
        };
    }

    // ===================== 各分区 UI =====================
    Control BuildWindowPanel()
    {
        var p = NewPanel();
        p.Children.Add(RowCombo("窗口材质", new[] { "半透明", "磨砂", "亚克力", "液态玻璃 (Beta)", "古卷 (Alpha)", "自定义图片" },
            (int)_s.FloatingScheduleMaterial, i => _s.FloatingScheduleMaterial = (FloatingScheduleMaterialType)i,
            out var matCombo));
        p.Children.Add(RowToggle("果冻效果（仅液态玻璃）", _s.FloatingScheduleJellyEnabled, v => _s.FloatingScheduleJellyEnabled = v));
        p.Children.Add(RowSlider("圆角程度 (0~10px)", _s.FloatingScheduleCornerRadius, 0, 10, 0.5, v => _s.FloatingScheduleCornerRadius = v, out _));
        p.Children.Add(RowSlider("效果强度 (0~100%)", _s.FloatingScheduleMaterialIntensity * 100, 0, 100, 1, v => _s.FloatingScheduleMaterialIntensity = v / 100.0, out _));
        p.Children.Add(RowButton("选择自定义背景图片…", async (_, _) =>
        {
            try
            {
                var sp = StorageProvider;
                if (sp == null) return;
                var options = new FilePickerOpenOptions
                {
                    Title = "选择背景图片",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("图片") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" } } }
                };
                var files = await sp.OpenFilePickerAsync(options);
                if (files is { Count: > 0 })
                {
                    var path = files[0].TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path)) _s.FloatingScheduleCustomBackgroundPath = path;
                }
            }
            catch { }
        }));
        p.Children.Add(new TextBlock { Text = $"当前图片：{(string.IsNullOrWhiteSpace(_s.FloatingScheduleCustomBackgroundPath) ? "（未选择）" : _s.FloatingScheduleCustomBackgroundPath)}", FontSize = 12, Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap });
        p.Children.Add(RowToggle("窗口边框", _s.FloatingScheduleBorderEnabled, v => _s.FloatingScheduleBorderEnabled = v));
        p.Children.Add(RowCombo("边框样式", new[] { "画框", "黑板", "Windows 7", "Windows XP", "Windows 10", "Windows 11", "HarmonyOS" },
            (int)_s.FloatingScheduleBorderPreset, i => _s.FloatingScheduleBorderPreset = (FloatingScheduleBorderPreset)i, out _));
        p.Children.Add(RowToggle("边框自定义颜色", _s.FloatingScheduleBorderColorEnabled, v => _s.FloatingScheduleBorderColorEnabled = v));
        p.Children.Add(RowColor("边框颜色", _s.FloatingScheduleBorderColor, v => _s.FloatingScheduleBorderColor = v));
        return p;
    }

    Control BuildTextPanel()
    {
        var p = NewPanel();
        p.Children.Add(RowToggle("自定义字体", _s.FloatingScheduleFontEnabled, v => _s.FloatingScheduleFontEnabled = v));
        var fonts = new List<string> { "（跟随应用）" };
        try { fonts.AddRange(FontFamilyHelper.GetSystemFontFamilies()); } catch { }
        int fi = 0;
        if (_s.FloatingScheduleFontEnabled && !string.IsNullOrWhiteSpace(_s.FloatingScheduleTextFontFamily))
        { var k = fonts.IndexOf(_s.FloatingScheduleTextFontFamily!); if (k > 0) fi = k; }
        p.Children.Add(RowCombo("字体", fonts.ToArray(), fi, i =>
        {
            if (i <= 0) { _s.FloatingScheduleFontEnabled = false; _s.FloatingScheduleTextFontFamily = null; }
            else { _s.FloatingScheduleFontEnabled = true; _s.FloatingScheduleTextFontFamily = fonts[i]; }
        }, out _));
        p.Children.Add(new TextBlock { Text = "字号：请在「时间表悬浮窗 → 基础外观 → 课程名字号」设置（此处直接绑定）。", FontSize = 12, Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap });
        p.Children.Add(RowToggle("自定义字重", _s.FloatingScheduleFontWeightEnabled, v => _s.FloatingScheduleFontWeightEnabled = v));
        p.Children.Add(RowCombo("字重", new[] { "Thin(100)", "ExtraLight(200)", "Light(300)", "Normal(400)", "Medium(500)", "SemiBold(600)", "Bold(700)", "ExtraBold(800)", "Black(900)" },
            Math.Clamp(_s.FloatingScheduleTextFontWeight / 100 - 1, 0, 8), i => { _s.FloatingScheduleFontWeightEnabled = true; _s.FloatingScheduleTextFontWeight = (i + 1) * 100; }, out _));
        p.Children.Add(RowToggle("自定义字体颜色", _s.FloatingScheduleTextColorEnabled, v => _s.FloatingScheduleTextColorEnabled = v));
        p.Children.Add(RowColor("全局字体颜色", _s.FloatingScheduleTextColor, v => { _s.FloatingScheduleTextColorEnabled = true; _s.FloatingScheduleTextColor = v; }));
        p.Children.Add(RowToggle("按学科分色", _s.FloatingScheduleSubjectColorEnabled, v => _s.FloatingScheduleSubjectColorEnabled = v));
        p.Children.Add(RowButton("从课表刷新学科", (_, _) => RefreshSubjects()));
        p.Children.Add(RowButton("恢复学科默认色", (_, _) => { _s.FloatingScheduleSubjectColorMap = new Dictionary<string, uint>(); RebuildPreview(); SwitchPanel(1); }));
        foreach (var kv in _s.FloatingScheduleSubjectColorMap)
        {
            var name = kv.Key;
            p.Children.Add(RowColor("  " + name, kv.Value, v =>
            {
                var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap) { [name] = v };
                _s.FloatingScheduleSubjectColorMap = map; // 重新赋值触发 INPC → 悬浮窗热应用
            }));
        }
        return p;
    }

    Control BuildTablePanel()
    {
        var p = NewPanel();
        p.Children.Add(RowCombo("布局方向", new[] { "纵向排列（默认）", "横向排列（从左往右）", "横向排列（从右往左，推荐古卷）" },
            (int)_s.FloatingScheduleLayoutDirection, i => _s.FloatingScheduleLayoutDirection = (FloatingScheduleLayoutDir)i, out _));
        p.Children.Add(RowToggle("间色（斑马底）", _s.FloatingScheduleZebraStriped, v => _s.FloatingScheduleZebraStriped = v));
        p.Children.Add(RowSlider("行间距倍率 (0.5~2.0)", _s.FloatingScheduleRowSpacingFactor, 0.5, 2.0, 0.05, v => _s.FloatingScheduleRowSpacingFactor = v, out _));
        p.Children.Add(RowCombo("列间距模式", new[] { "自动适配", "自定义" }, (int)_s.FloatingScheduleColumnSpacingMode, i => _s.FloatingScheduleColumnSpacingMode = (FloatingScheduleColumnSpacingMode)i, out _));
        p.Children.Add(RowSlider("自定义列间距 (50~1000px)", _s.FloatingScheduleColumnSpacing, 50, 1000, 1, v => _s.FloatingScheduleColumnSpacing = v, out _));
        p.Children.Add(RowSlider("上边距 (0~50px)", _s.FloatingScheduleMarginTop, 0, 50, 1, v => _s.FloatingScheduleMarginTop = v, out _));
        p.Children.Add(RowSlider("下边距 (0~50px)", _s.FloatingScheduleMarginBottom, 0, 50, 1, v => _s.FloatingScheduleMarginBottom = v, out _));
        p.Children.Add(RowSlider("左边距 (0~50px)", _s.FloatingScheduleMarginLeft, 0, 50, 1, v => _s.FloatingScheduleMarginLeft = v, out _));
        p.Children.Add(RowSlider("右边距 (0~50px)", _s.FloatingScheduleMarginRight, 0, 50, 1, v => _s.FloatingScheduleMarginRight = v, out _));
        return p;
    }

    Control BuildThemePanel()
    {
        var p = NewPanel();
        p.Children.Add(RowCombo("主题色", new[] { "浅色", "深色", "跟随 ClassIsland", "跟随系统" },
            (int)_s.FloatingScheduleThemeMode, i => _s.FloatingScheduleThemeMode = (FloatingScheduleThemeMode)i, out _));
        p.Children.Add(RowToggle("自定义表格底色", _s.FloatingScheduleTableColorEnabled, v => _s.FloatingScheduleTableColorEnabled = v));
        p.Children.Add(RowColor("表格底色", _s.FloatingScheduleTableColor, v => { _s.FloatingScheduleTableColorEnabled = true; _s.FloatingScheduleTableColor = v; }));
        p.Children.Add(RowToggle("自定义强调色", _s.FloatingScheduleAccentColorEnabled, v => _s.FloatingScheduleAccentColorEnabled = v));
        p.Children.Add(RowColor("强调色", _s.FloatingScheduleAccentColor, v => { _s.FloatingScheduleAccentColorEnabled = true; _s.FloatingScheduleAccentColor = v; }));
        p.Children.Add(RowToggle("自定义分隔线颜色", _s.FloatingScheduleSeparatorColorEnabled, v => _s.FloatingScheduleSeparatorColorEnabled = v));
        p.Children.Add(RowColor("分隔线颜色", _s.FloatingScheduleSeparatorColor, v => { _s.FloatingScheduleSeparatorColorEnabled = true; _s.FloatingScheduleSeparatorColor = v; }));
        p.Children.Add(RowSlider("分隔线粗细 (0~10px)", _s.FloatingScheduleSeparatorThickness, 0, 10, 0.1, v => _s.FloatingScheduleSeparatorThickness = v, out _));
        return p;
    }

    Control BuildDebugPanel()
    {
        var p = NewPanel();
        p.Children.Add(RowToggle("显示 FPS 角标", _s.FloatingScheduleShowFps, v => _s.FloatingScheduleShowFps = v));
        p.Children.Add(new TextBlock { Text = "角标格式：渲染 FPS / 无等待 FPS", FontSize = 12, Margin = new Thickness(0, 0, 0, 8) });
        return p;
    }

    void RefreshSubjects()
    {
        // 从当前课表档案读取全部科目，补充默认色（语文橙/数学蓝/英语黄，其余取主题强调色变体）。
        try
        {
            var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap);
            foreach (var name in SampleSubjects)
            {
                if (map.ContainsKey(name)) continue;
                map[name] = name switch
                {
                    "语文" => 0xFFE08A2E,
                    "数学" => 0xFF2E7FE0,
                    "英语" => 0xFFE0C02E,
                    _ => 0xFF7A7A7A,
                };
            }
            _s.FloatingScheduleSubjectColorMap = map;
            SwitchPanel(1);
        }
        catch { }
    }

    // ===================== 预览 =====================
    static readonly string[] SampleSubjects = { "语文", "数学", "英语", "物理", "化学" };

    void RebuildPreview()
    {
        if (_settings == null) return;
        var model = BuildSampleModel();
        FloatScheduleRenderer.ApplyCardStyle(_previewCard, model);
        var (root, _) = FloatScheduleRenderer.Build(model);
        int mat = (int)_s.FloatingScheduleMaterial;
        if (!_s.EnableExperimentalFeatures && (mat == 3 || mat == 4 || mat == 5)) mat = 0;
        FloatScheduleMaterialApplier.Apply(_previewCard, mat, _s.FloatingScheduleMaterialIntensity, _s.FloatingScheduleCornerRadius, model.CardBackgroundArgb, _s.FloatingScheduleCustomBackgroundPath, root);
        FloatScheduleMaterialApplier.ApplyBorder(_previewCard, _s.FloatingScheduleBorderEnabled && mat != 3 && mat != 4, (int)_s.FloatingScheduleBorderPreset, _s.FloatingScheduleBorderColorEnabled, _s.FloatingScheduleBorderColor);
        _previewCard.Padding = new Thickness(_s.FloatingScheduleMarginLeft, _s.FloatingScheduleMarginTop, _s.FloatingScheduleMarginRight, _s.FloatingScheduleMarginBottom);
        _previewCard.MaxWidth = 360;
        _previewCard.HorizontalAlignment = HorizontalAlignment.Left;
        _previewCard.VerticalAlignment = VerticalAlignment.Top;
        _previewCard.RenderTransform = new ScaleTransform(_zoom, _zoom);
        if (_previewCanvas.Children.Count == 0) _previewCanvas.Children.Add(_previewCard);
        Canvas.SetLeft(_previewCard, 12); Canvas.SetTop(_previewCard, 12);
    }

    FloatScheduleRenderModel BuildSampleModel()
    {
        // 预览配色解析（镜像服务侧覆盖逻辑的简化版）
        Color cardBase, textBase, subBase, sepBase, accent;
        bool fl = _s.FloatingScheduleThemeMode == FloatingScheduleThemeMode.Light;
        bool fd = _s.FloatingScheduleThemeMode == FloatingScheduleThemeMode.Dark;
        if (fl) { cardBase = Color.FromRgb(0xFF, 0xFF, 0xFF); textBase = Color.FromRgb(0x1E, 0x1E, 0x1E); subBase = Color.FromRgb(0x5A, 0x5A, 0x5A); sepBase = Color.FromRgb(0xD8, 0xD8, 0xD8); }
        else if (fd) { cardBase = Color.FromRgb(0x2D, 0x2D, 0x30); textBase = Color.FromRgb(0xFF, 0xFF, 0xFF); subBase = Color.FromRgb(0xD3, 0xD3, 0xD3); sepBase = Color.FromRgb(0x55, 0x55, 0x55); }
        else { cardBase = (ThemeHelper.GetCardBackgroundBrush() as ISolidColorBrush)?.Color ?? Color.FromRgb(0x2D, 0x2D, 0x30); textBase = (ThemeHelper.GetTextBrush() as ISolidColorBrush)?.Color ?? Colors.White; subBase = (ThemeHelper.GetSubTextBrush() as ISolidColorBrush)?.Color ?? Color.FromRgb(0xD3, 0xD3, 0xD3); sepBase = (ThemeHelper.GetSeparatorBrush() as ISolidColorBrush)?.Color ?? Color.FromRgb(0x55, 0x55, 0x55); }
        if (_s.FloatingScheduleTableColorEnabled) cardBase = FloatScheduleRenderer.FromArgb(_s.FloatingScheduleTableColor);
        accent = _s.FloatingScheduleAccentColorEnabled ? FloatScheduleRenderer.FromArgb(_s.FloatingScheduleAccentColor) : Color.FromRgb(0x00, 0x78, 0xD4);
        var sepCol = _s.FloatingScheduleSeparatorColorEnabled ? FloatScheduleRenderer.FromArgb(_s.FloatingScheduleSeparatorColor) : sepBase;
        Color textCol, subCol;
        if (_s.FloatingScheduleTextColorEnabled) { var tc = FloatScheduleRenderer.FromArgb(_s.FloatingScheduleTextColor); textCol = tc; subCol = Color.FromArgb((byte)(tc.A * 0.8), tc.R, tc.G, tc.B); }
        else { textCol = textBase; subCol = subBase; }

        byte a = (byte)Math.Round(Math.Clamp(_s.FloatingScheduleOpacity, 0, 1) * 255);
        var m = new FloatScheduleRenderModel
        {
            FontSize = (int)Math.Round(Math.Clamp(_s.FloatingScheduleFontScale, 8, 32)),
            FontFamilySource = _s.FloatingScheduleFontEnabled ? _s.FloatingScheduleTextFontFamily : null,
            AccentArgb = FloatScheduleRenderer.ToArgb(accent),
            CardBackgroundArgb = ((uint)a << 24) | ((uint)cardBase.R << 16) | ((uint)cardBase.G << 8) | cardBase.B,
            BorderArgb = FloatScheduleRenderer.ToArgb(new Color(255, sepBase.R, sepBase.G, sepBase.B)),
            TextArgb = FloatScheduleRenderer.ToArgb(textCol),
            SubTextArgb = FloatScheduleRenderer.ToArgb(subCol),
            HighlightArgb = ((uint)0x66 << 24) | ((uint)accent.R << 16) | ((uint)accent.G << 8) | accent.B,
            BreakRowBackgroundArgb = (fl || (!fd && !ThemeHelper.IsDarkTheme())) ? 0x14000000u : 0x14FFFFFFu,
            SeparatorArgb = FloatScheduleRenderer.ToArgb(sepCol),
            CurrentClassIndex = 1,
            ClassProgressRatio = 0.45,
            LayoutDirection = (int)_s.FloatingScheduleLayoutDirection,
            RowSpacingFactor = _s.FloatingScheduleRowSpacingFactor,
            ColumnSpacingMode = (int)_s.FloatingScheduleColumnSpacingMode,
            ColumnSpacing = _s.FloatingScheduleColumnSpacingMode == FloatingScheduleColumnSpacingMode.Custom ? _s.FloatingScheduleColumnSpacing : 14,
            ZebraStriped = _s.FloatingScheduleZebraStriped,
            ZebraRowArgb = (fl || (!fd && !ThemeHelper.IsDarkTheme())) ? 0x14000000u : 0x1AFFFFFFu,
            FontWeightCustom = _s.FloatingScheduleFontWeightEnabled ? _s.FloatingScheduleTextFontWeight : 0,
            SeparatorThickness = _s.FloatingScheduleSeparatorThickness,
            ShowFps = _s.FloatingScheduleShowFps,
        };
        string[] times = { "08:00 - 08:40", "08:50 - 09:30", "09:40 - 10:20", "10:30 - 11:10", "11:20 - 12:00" };
        string[] teachers = { "张老师", "李老师", "王老师", "陈老师", "赵老师" };
        for (int i = 0; i < SampleSubjects.Length; i++)
        {
            uint sc = 0;
            if (_s.FloatingScheduleSubjectColorEnabled) _s.FloatingScheduleSubjectColorMap.TryGetValue(SampleSubjects[i], out sc);
            m.Rows.Add(new FloatScheduleRowModel { Course = SampleSubjects[i], Teacher = teachers[i], TimeText = times[i], SubjectColorArgb = sc });
        }
        m.Break = new FloatScheduleBreakModel { AfterClassIndex = 2, Name = "课间休息", TimeText = "10:20 - 10:30" };
        m.SeparatorAfterClassIndex.Add(3);
        return m;
    }

    void PreviewHost_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _zoom = Math.Clamp(_zoom + (e.Delta.Y > 0 ? 0.1 : -0.1), 0.4, 2.5);
        _previewCard.RenderTransform = new ScaleTransform(_zoom, _zoom);
        e.Handled = true;
    }

    // ===================== 控件工厂 =====================
    PluginSettings _s => _settings!;
    static StackPanel NewPanel() => new() { Spacing = 8, Margin = new Thickness(0, 0, 0, 0) };

    Control Row(string label, Control editor)
    {
        var g = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"), ColumnSpacing = 12, Margin = new Thickness(0, 2) };
        var lt = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(lt, 0); g.Children.Add(lt);
        editor.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(editor, 1); g.Children.Add(editor);
        return g;
    }

    Control RowToggle(string label, bool val, Action<bool> set)
    {
        var t = new ToggleSwitch { IsChecked = val };
        t.IsCheckedChanged += (_, _) => { if (_suppress) return; set(t.IsChecked == true); RebuildPreview(); };
        return Row(label, t);
    }

    Control RowCombo(string label, string[] items, int sel, Action<int> set, out ComboBox combo)
    {
        var c = new ComboBox { MinWidth = 180, SelectedIndex = Math.Clamp(sel, 0, items.Length - 1) };
        foreach (var it in items) c.Items.Add(it);
        c.SelectionChanged += (_, _) => { if (_suppress || c.SelectedIndex < 0) return; set(c.SelectedIndex); RebuildPreview(); };
        combo = c; return Row(label, c);
    }

    Control RowSlider(string label, double val, double min, double max, double step, Action<double> set, out Slider slider)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, MinWidth = 220 };
        var s = new Slider { Minimum = min, Maximum = max, Value = val, Width = 160, TickFrequency = step };
        var tb = new TextBlock { Text = val.ToString("0.##", CultureInfo.InvariantCulture), VerticalAlignment = VerticalAlignment.Center, Width = 48 };
        s.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) { if (_suppress) return; set(s.Value); tb.Text = s.Value.ToString("0.##", CultureInfo.InvariantCulture); RebuildPreview(); }
        };
        sp.Children.Add(s); sp.Children.Add(tb);
        slider = s; return Row(label, sp);
    }

    Control RowButton(string label, EventHandler<RoutedEventArgs> handler)
    {
        var b = new Button { Content = label, Padding = new Thickness(12, 5), HorizontalAlignment = HorizontalAlignment.Left };
        b.Click += handler;
        return b;
    }

    Control RowColor(string label, uint argb, Action<uint> set)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var swatch = new Border { Width = 28, Height = 22, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(FloatScheduleRenderer.FromArgb(argb)), BorderBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), BorderThickness = new Thickness(1) };
        var box = new TextBox { Text = "#" + argb.ToString("X8"), Width = 100 };
        box.LostFocus += (_, _) =>
        {
            try
            {
                var t = box.Text?.Trim().TrimStart('#') ?? "";
                if (t.Length == 8 && uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) { set(v); swatch.Background = new SolidColorBrush(FloatScheduleRenderer.FromArgb(v)); RebuildPreview(); }
            }
            catch { }
        };
        sp.Children.Add(swatch); sp.Children.Add(box);
        return Row(label, sp);
    }

    static IBrush MakeCheckerBrush()
    {
        var dg = new DrawingGroup();
        dg.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)), Geometry = new RectangleGeometry(new Rect(0, 0, 16, 16)) });
        dg.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)), Geometry = new GeometryGroup { Children = { new RectangleGeometry(new Rect(0, 0, 8, 8)), new RectangleGeometry(new Rect(8, 8, 8, 8)) } } });
        return new DrawingBrush(dg) { TileMode = TileMode.Tile, DestinationRect = new RelativeRect(0, 0, 16, 16, RelativeUnit.Absolute) };
    }
}
