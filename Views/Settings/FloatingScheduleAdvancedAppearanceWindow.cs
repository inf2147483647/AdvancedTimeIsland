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
    // 背景透卡（预览专用）：透出"卡片正下方"那块示例背景并按材质施加不同模糊（磨砂/亚克力/液态玻璃差异的核心）。
    Border? _previewBackdropLayer;
    int _previewBackdropMaterial;   // 3=液态玻璃 → 背景层走"真实 SDF 折射"，其余走模糊
    double _bdLeft = double.NaN, _bdTop = double.NaN, _bdW = double.NaN, _bdH = double.NaN, _bdZoom = double.NaN;
    double _zoom = 1.0;
    bool _suppress;
    // 预览拖动状态
    bool _previewDragging;
    double _dragStartX, _dragStartY, _cardStartX, _cardStartY;
    IPointer? _dragPointer;

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
        // 初始给一个适中的回退尺寸；Opened 后按实际屏幕工作区校准（构造期 Screens 可能未就绪）。
        Width = 1040; Height = 680;
        MinWidth = 720; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
        Opened += OnEditorOpened;

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
        right.Children.Add(new TextBlock { Text = "实时预览（1:1，滚轮缩放 / 拖动）· 玻璃为近似示意，真机使用系统亚克力", FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
        // 预览“桌面”示例背景（彩色 + 网格）：半透明玻璃卡片叠在其上，可看出“透出背后内容”的玻璃感。
        // 说明：编辑器是不透明窗口，系统亚克力（需采样真实桌面）在此不可用，故用示例背景做可视化近似。
        var previewHost = new Border { Background = BuildPreviewWallpaperBrush(), CornerRadius = new CornerRadius(8), ClipToBounds = true };
        _previewCanvas.Width = 396; _previewCanvas.Height = 640;
        _previewCard.Margin = new Thickness(0);
        var previewInner = new Panel();
        previewInner.Children.Add(_previewCanvas);
        previewHost.Child = previewInner;
        previewHost.PointerWheelChanged += PreviewHost_PointerWheelChanged;
        // 拖动预览卡片（左键按住卡片或预览区空白均可拖；滚轮缩放保持）
        AttachPreviewDrag(previewHost);
        // 背景透卡几何随卡片位置/尺寸/缩放变化重算（拖动、内容重排、缩放都会触发布局更新）
        _previewCard.LayoutUpdated += (_, _) => UpdatePreviewBackdrop();
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

    // 窗口显示后按所在屏幕工作区校准尺寸：高 = 工作区高 × 0.82（给标题栏/任务栏留安全余量），
    // 宽同理；夹到最小/最大区间。避免固定 760 在小屏或 125%/150% DPI 下标题栏溢出屏幕。
    void OnEditorOpened(object? sender, EventArgs e)
    {
        try
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen == null) return;
            var wa = screen.WorkingArea;
            double w = Math.Clamp(Math.Floor(wa.Width * 0.82), MinWidth, 1080);
            double h = Math.Clamp(Math.Floor(wa.Height * 0.82), MinHeight, 720);
            Width = w; Height = h;
        }
        catch { }
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
        // 液态玻璃真折射背景的采样率（直接绑定设置，改动即时生效；见设置项 XML 注释）
        p.Children.Add(RowNumeric("液态玻璃采样率 (1~60Hz)", _s.FloatingScheduleGlassSampleRateHz, 1, 60, 0.1, v => _s.FloatingScheduleGlassSampleRateHz = v));
        // 液态玻璃背景捕获方式（改动即时生效；DXGI 不可用时自动回退 GDI 并定期重试恢复）
        p.Children.Add(RowCombo("液态玻璃捕获方式", new[] { "DXGI Desktop Duplication (推荐)", "GDI BitBlt (回退)" },
            (int)_s.FloatingScheduleGlassCaptureMode, i => _s.FloatingScheduleGlassCaptureMode = (FloatingScheduleGlassCaptureMode)i, out _));
        // 拖动位移补偿（拖动期间背景按窗口位移偏移取材、重做折射，实现 60fps 跟手）
        p.Children.Add(RowToggle("拖动位移补偿", _s.FloatingScheduleGlassDragCompensation, v => _s.FloatingScheduleGlassDragCompensation = v));
        // 允许截图：暂停液态玻璃背景采样/重烘焙（背景冻结），并解除采样期强制的"排除捕获"，
        // 使窗口按用户自己的"防截图"设置出现在截图/录屏中（便于截图反馈问题）。默认关。
        p.Children.Add(RowToggle("允许截图（暂停液态玻璃背景采样）", _s.FloatingScheduleGlassAllowCapture, v => _s.FloatingScheduleGlassAllowCapture = v));
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
        // 添加学科：输入科目名 + 添加按钮
        var addName = new TextBox { Watermark = "科目名", Width = 140 };
        var addBtn = new Button { Content = "＋添加学科", Padding = new Thickness(10, 5) };
        addBtn.Click += (_, _) =>
        {
            var nm = addName.Text?.Trim() ?? "";
            if (nm.Length == 0 || _s.FloatingScheduleSubjectColorMap.ContainsKey(nm)) return;
            var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap) { [nm] = 0xFF7A7A7A };
            _s.FloatingScheduleSubjectColorMap = map;
            RebuildPreview(); SwitchPanel(1);
        };
        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 2) };
        addRow.Children.Add(addName); addRow.Children.Add(addBtn);
        p.Children.Add(addRow);
        p.Children.Add(RowButton("从课表刷新学科", (_, _) => RefreshSubjects()));
        p.Children.Add(RowButton("恢复学科默认色", (_, _) => { _s.FloatingScheduleSubjectColorMap = new Dictionary<string, uint>(); RebuildPreview(); SwitchPanel(1); }));
        foreach (var kv in _s.FloatingScheduleSubjectColorMap)
        {
            var name = kv.Key;
            p.Children.Add(SubjectRow(name, kv.Value,
                v => { var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap) { [name] = v }; _s.FloatingScheduleSubjectColorMap = map; },
                () => { var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap); map.Remove(name); _s.FloatingScheduleSubjectColorMap = map; RebuildPreview(); SwitchPanel(1); }));
        }
        return p;
    }

    // 学科行：科目名 + 颜色(色块+HEX) + 移除按钮。
    Control SubjectRow(string name, uint argb, Action<uint> onColor, Action onRemove)
    {
        var g = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 2) };
        var lbl = new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(lbl, 0); g.Children.Add(lbl);
        var cp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var swatch = new Border { Width = 26, Height = 20, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(FloatScheduleRenderer.FromArgb(argb)), BorderBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), BorderThickness = new Thickness(1) };
        var box = new TextBox { Text = "#" + argb.ToString("X8"), Width = 96 };
        box.LostFocus += (_, _) =>
        {
            try
            {
                var t = box.Text?.Trim().TrimStart('#') ?? "";
                if (t.Length == 8 && uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) { onColor(v); swatch.Background = new SolidColorBrush(FloatScheduleRenderer.FromArgb(v)); RebuildPreview(); }
            }
            catch { }
        };
        cp.Children.Add(swatch); cp.Children.Add(box);
        Grid.SetColumn(cp, 1); g.Children.Add(cp);
        var rm = new Button { Content = "移除", Padding = new Thickness(8, 3) };
        rm.Click += (_, _) => onRemove();
        Grid.SetColumn(rm, 2); g.Children.Add(rm);
        return g;
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
        // 背景透卡：磨砂/亚克力/液态玻璃需要"背后内容"才成立。编辑器是不透明窗口，采不到真实桌面，
        // 故在卡片内裁出"卡片正下方的示例背景"这块，由材质层按材质施加不同模糊（磨砂重 / 亚克力中 / 液态玻璃≈0）。
        _previewBackdropMaterial = mat;
        _previewBackdropLayer = (mat >= 1 && mat <= 3) ? BuildPreviewBackdropLayer() : null;
        // 内容内边距内移（材质层铺满整卡，避免非半透明材质出现空心边框）
        FloatScheduleMaterialApplier.Apply(_previewCard, mat, _s.FloatingScheduleMaterialIntensity, _s.FloatingScheduleCornerRadius, model.CardBackgroundArgb, _s.FloatingScheduleCustomBackgroundPath, root, osBackdrop: false,
            contentPadding: new Thickness(_s.FloatingScheduleMarginLeft, _s.FloatingScheduleMarginTop, _s.FloatingScheduleMarginRight, _s.FloatingScheduleMarginBottom),
            backdropLayer: _previewBackdropLayer, backdropIsRefracted: mat == 3);
        FloatScheduleMaterialApplier.ApplyBorder(_previewCard, _s.FloatingScheduleBorderEnabled && mat != 3 && mat != 4, (int)_s.FloatingScheduleBorderPreset, _s.FloatingScheduleBorderColorEnabled, _s.FloatingScheduleBorderColor);
        _previewCard.MaxWidth = 360;
        _previewCard.HorizontalAlignment = HorizontalAlignment.Left;
        _previewCard.VerticalAlignment = VerticalAlignment.Top;
        ApplyPreviewTransform();
        if (_previewCanvas.Children.Count == 0) _previewCanvas.Children.Add(_previewCard);
        Canvas.SetLeft(_previewCard, 12); Canvas.SetTop(_previewCard, 12);
        _bdLeft = _bdTop = _bdW = _bdH = _bdZoom = double.NaN;   // 强制下一次重算透卡几何
        UpdatePreviewBackdrop();
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
        ApplyPreviewTransform();
        e.Handled = true;
    }

    void ApplyPreviewTransform()
    {
        _previewCard.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
        _previewCard.RenderTransform = new ScaleTransform(_zoom, _zoom);
        UpdatePreviewBackdrop();
    }

    // 在预览宿主上按住左键拖动卡片（Canvas 坐标随缩放除回去）。
    void AttachPreviewDrag(Control host)
    {
        host.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(host).Properties.IsLeftButtonPressed) return;
            _previewDragging = true;
            var pos = e.GetPosition(host);
            _dragStartX = pos.X; _dragStartY = pos.Y;
            _cardStartX = Canvas.GetLeft(_previewCard);
            _cardStartY = Canvas.GetTop(_previewCard);
            if (double.IsNaN(_cardStartX)) _cardStartX = 0;
            if (double.IsNaN(_cardStartY)) _cardStartY = 0;
            _dragPointer = e.Pointer;
            try { e.Pointer.Capture(host); } catch { }
            e.Handled = true;
        };
        host.PointerMoved += (_, e) =>
        {
            if (!_previewDragging) return;
            var pos = e.GetPosition(host);
            double dx = (pos.X - _dragStartX) / _zoom;
            double dy = (pos.Y - _dragStartY) / _zoom;
            Canvas.SetLeft(_previewCard, _cardStartX + dx);
            Canvas.SetTop(_previewCard, _cardStartY + dy);
            UpdatePreviewBackdrop();
            e.Handled = true;
        };
        void Release(PointerReleasedEventArgs e)
        {
            if (!_previewDragging) return;
            _previewDragging = false;
            try { _dragPointer?.Capture(null); } catch { }
            _dragPointer = null;
            e.Handled = true;
        }
        host.PointerReleased += (_, e) => Release(e);
        host.PointerCaptureLost += (_, _) => { _previewDragging = false; _dragPointer = null; };
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

    // 数值输入行（NumericUpDown）：范围/步长由参数给定，改动即时写回设置（主进程与子进程都热跟随）
    Control RowNumeric(string label, double val, double min, double max, double step, Action<double> set)
    {
        var n = new NumericUpDown
        {
            Minimum = (decimal)min,
            Maximum = (decimal)max,
            Increment = (decimal)step,
            Value = (decimal)Math.Clamp(val, min, max),
            Width = 160,
            FormatString = step < 1 ? "0.0" : "0",
            ClipValueToMinMax = true,
        };
        n.PropertyChanged += (_, e) =>
        {
            if (e.Property != NumericUpDown.ValueProperty || _suppress) return;
            if (n.Value is not { } v) return;
            set((double)v);
        };
        return Row(label, n);
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

    // 预览“桌面”示例背景：彩色渐变 + 直线网格（参考 D:\liquid-glass 的背景层配色）。
    // 用途：编辑器为不透明窗口，系统亚克力无法采样真实桌面；用示例背景承载“背后内容”，
    // 使半透明玻璃卡片能透出色彩与网格，呈现玻璃质感（直线过玻璃最易观察通透度）。
    static DrawingGroup BuildPreviewWallpaperDrawing()
    {
        var full = new Rect(0, 0, 396, 640);
        var dg = new DrawingGroup();
        dg.Children.Add(new GeometryDrawing
        {
            Brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(0x1B, 0x23, 0x44), 0),
                    new GradientStop(Color.FromRgb(0x4D, 0x78, 0xFF), 0.28),
                    new GradientStop(Color.FromRgb(0x98, 0x6E, 0xFF), 0.5),
                    new GradientStop(Color.FromRgb(0xFF, 0x5C, 0x96), 0.72),
                    new GradientStop(Color.FromRgb(0x00, 0xD6, 0xBE), 1),
                }
            },
            Geometry = new RectangleGeometry(full)
        });
        var grid = new DrawingGroup();
        // 细节色块（落在卡片正下方区域）：让"模糊程度"可见——磨砂/亚克力把细节糊掉，液态玻璃保留清晰。
        dg.Children.Add(new GeometryDrawing
        {
            Brush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xE2, 0x9A)),
            Geometry = new EllipseGeometry(new Rect(28, 96, 96, 96)),
        });
        dg.Children.Add(new GeometryDrawing
        {
            Brush = new SolidColorBrush(Color.FromArgb(0x73, 0x3A, 0xE2, 0xFF)),
            Geometry = new EllipseGeometry(new Rect(140, 168, 74, 74)),
        });
        grid.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.FromArgb(0x58, 0xFF, 0xFF, 0xFF)), Geometry = new RectangleGeometry(new Rect(0, 0, 62, 1)) });
        grid.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.FromArgb(0x4A, 0xFF, 0xFF, 0xFF)), Geometry = new RectangleGeometry(new Rect(0, 0, 1, 62)) });
        var gridBrush = new DrawingBrush(grid)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            DestinationRect = new RelativeRect(0, 0, 62, 62, RelativeUnit.Absolute),
        };
        dg.Children.Add(new GeometryDrawing { Brush = gridBrush, Geometry = new RectangleGeometry(full) });
        return dg;
    }

    static IBrush BuildPreviewWallpaperBrush() => new DrawingBrush(BuildPreviewWallpaperDrawing())
    {
        Stretch = Stretch.Fill,
        DestinationRect = new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative),
    };

    // 背景透卡：一张透明层，Background 由 UpdatePreviewBackdrop 按卡片位置从示例背景取像；
    // 模糊半径由材质层（FloatScheduleMaterialApplier.Apply）按材质设置（磨砂重 / 亚克力中 / 液态玻璃≈0）。
    Border BuildPreviewBackdropLayer() => new()
    {
        IsHitTestVisible = false,
        CornerRadius = new CornerRadius(_s.FloatingScheduleCornerRadius),
    };

    // 透卡几何：按卡片在示例背景上的位置（Canvas 坐标）与缩放，取"正下方"那块背景并保持 1:1 观感。
    void UpdatePreviewBackdrop()
    {
        var bd = _previewBackdropLayer;
        if (bd == null || _settings == null) return;
        double left = Canvas.GetLeft(_previewCard); if (double.IsNaN(left)) left = 0;
        double top = Canvas.GetTop(_previewCard); if (double.IsNaN(top)) top = 0;
        var size = _previewCard.Bounds.Size;
        if (size.Width < 1 || size.Height < 1) return;
        double z = _zoom <= 0 ? 1 : _zoom;
        if (left == _bdLeft && top == _bdTop && size.Width == _bdW && size.Height == _bdH && z == _bdZoom) return;
        _bdLeft = left; _bdTop = top; _bdW = size.Width; _bdH = size.Height; _bdZoom = z;
        // 圆角裁切：BlurEffect 会外溢到卡片边界之外，用圆角矩形 Clip 收住，避免糊到卡片四角之外
        double r = Math.Max(0, _s.FloatingScheduleCornerRadius);
        bd.Clip = new RectangleGeometry(new Rect(0, 0, size.Width, size.Height), r, r);
        if (_previewBackdropMaterial == 3)
            bd.Background = BuildPreviewRefractedBrush(left, top, z, (int)Math.Round(size.Width), (int)Math.Round(size.Height), r);
        else
            bd.Background = BuildPreviewBackdropBrush(left, top, z);
    }

    // 液态玻璃"真实折射"：解析取样卡片正下方的示例背景 → 走共享折射核心（数学移植自 Kyant/backdrop 的
    // 圆角矩形 SDF 折射：边缘带圆弧剖面位移 + 梯度方向 + 7 抽样色散 + SDF 梯度镜面高光）→ 结果位图。
    static IBrush? BuildPreviewRefractedBrush(double left, double top, double z, int w, int h, double corner)
    {
        if (w < 2 || h < 2) return null;
        var src = SamplePreviewWallpaper(left, top, z, w, h);
        var dst = new byte[src.Length];
        // 参数取自联网核对的公开实现/规格（非拍脑袋）：
        //   折射带高度 ≤20px、位移幅度 ≈10% 卡片尺寸（kube.io 的物理模型给出"最大位移约 6% 物体尺寸"）、
        //   色散量 ≈0.7% 尺寸（kube.io: aberr ≈ 0.003·dome + 0.007·edge）、镜面高光取紧致 falloff。
        double band = Math.Clamp(Math.Min(w, h) * 0.14, 8.0, 20.0);   // 折射带高度（设计经验：UI 控件玻璃厚度 ≤20）
        double amount = Math.Min(w, h) * 0.10;                        // 最大位移量（≈10% 卡片尺寸）
        FloatScheduleRefraction.Refract(src, dst, w, h, corner, band, amount,
            // 与真机抓帧侧同源：depthEffect=false，位移仅取 SDF 梯度（边缘法线），避免"放射状"星芒
            depthEffect: false, chromaticAberration: 0.10, highlightStrength: 0.35, highlightAngleDeg: 45, highlightFalloff: 4.0);
        var bmp = FloatScheduleRefraction.ToBitmap(dst, w, h);
        return bmp == null ? null : new ImageBrush(bmp) { Stretch = Stretch.Fill };
    }

    // 示例背景的解析取样（与 BuildPreviewWallpaperDrawing 的绘制顺序/配色一致）：对角渐变 → 两个色块 → 62px 网格。
    // 编辑器是不透明窗口、采不到真实桌面，故用同一份示例背景作为折射需要的"背后内容"。
    static byte[] SamplePreviewWallpaper(double x0, double y0, double z, int w, int h)
    {
        var buf = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            double gy = y0 + y * z;
            int row = y * w * 4;
            for (int x = 0; x < w; x++)
            {
                var c = WallpaperPixelAt(x0 + x * z, gy);
                int o = row + x * 4;
                buf[o] = c.B; buf[o + 1] = c.G; buf[o + 2] = c.R; buf[o + 3] = 0xFF;
            }
        }
        return buf;
    }

    static Color WallpaperPixelAt(double x, double y)
    {
        var c = GradientAt(((x / 396.0) + (y / 640.0)) * 0.5);
        c = Blend(c, Color.FromArgb(0x80, 0xFF, 0xE2, 0x9A), EllipseCover(x, y, 76, 144, 48));
        c = Blend(c, Color.FromArgb(0x73, 0x3A, 0xE2, 0xFF), EllipseCover(x, y, 177, 205, 37));
        if (Fract(x, 62) < 1) c = Blend(c, Color.FromArgb(0x4A, 0xFF, 0xFF, 0xFF), 1);
        if (Fract(y, 62) < 1) c = Blend(c, Color.FromArgb(0x58, 0xFF, 0xFF, 0xFF), 1);
        return c;
    }

    static double Fract(double v, double m) { double f = v % m; return f < 0 ? f + m : f; }

    static double EllipseCover(double x, double y, double cx, double cy, double r)
    {
        double dx = x - cx, dy = y - cy;
        return Math.Clamp(r + 0.5 - Math.Sqrt(dx * dx + dy * dy), 0, 1);
    }

    static Color GradientAt(double t)
    {
        (double p, byte r, byte g, byte b)[] stops =
        {
            (0.0, 0x1B, 0x23, 0x44), (0.28, 0x4D, 0x78, 0xFF), (0.5, 0x98, 0x6E, 0xFF),
            (0.72, 0xFF, 0x5C, 0x96), (1.0, 0x00, 0xD6, 0xBE),
        };
        t = Math.Clamp(t, 0, 1);
        for (int i = 0; i < stops.Length - 1; i++)
        {
            if (t > stops[i + 1].p) continue;
            double k = (t - stops[i].p) / Math.Max(1e-6, stops[i + 1].p - stops[i].p);
            return Color.FromRgb(Lerp(stops[i].r, stops[i + 1].r, k), Lerp(stops[i].g, stops[i + 1].g, k), Lerp(stops[i].b, stops[i + 1].b, k));
        }
        var last = stops[stops.Length - 1];
        return Color.FromRgb(last.r, last.g, last.b);
    }

    static byte Lerp(byte a, byte b, double k) => (byte)Math.Clamp(a + (b - a) * k, 0, 255);

    static Color Blend(Color dst, Color src, double cover)
    {
        double sa = src.A / 255.0 * Math.Clamp(cover, 0, 1);
        return Color.FromRgb(
            (byte)Math.Clamp(dst.R * (1 - sa) + src.R * sa, 0, 255),
            (byte)Math.Clamp(dst.G * (1 - sa) + src.G * sa, 0, 255),
            (byte)Math.Clamp(dst.B * (1 - sa) + src.B * sa, 0, 255));
    }

    // 用与示例背景相同的绘制内容，以绝对 DestinationRect 作视口取"卡片正下方"那块：
    // 卡片内是未缩放的布局坐标，故把背景整体平移到 (-left/z, -top/z) 并缩放 1/z。
    static IBrush BuildPreviewBackdropBrush(double left, double top, double z)
    {
        double s = 1.0 / (z <= 0 ? 1 : z);
        return new DrawingBrush(BuildPreviewWallpaperDrawing())
        {
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            DestinationRect = new RelativeRect(-left * s, -top * s, 396 * s, 640 * s, RelativeUnit.Absolute),
        };
    }
}
