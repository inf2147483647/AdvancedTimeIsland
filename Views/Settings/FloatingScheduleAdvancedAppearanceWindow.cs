// ATI 高级外观 · 独立编辑器窗口（非 CI 设置页）。
// 三栏：左导航（窗口/文本/表格/主题/调试）+ 中部设置项 + 右侧 1:1 实时预览（可拖动、滚轮缩放）。
// 读写与悬浮窗共用的 PluginSettings；任何改动即时写回（悬浮窗经 INPC 热应用）并刷新预览。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Shared.FloatingSchedule;
using AdvancedTimeIsland.Views.Controls;
using ClassIsland.Shared;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AdvancedTimeIsland.Views.Settings;

public sealed class FloatingScheduleAdvancedAppearanceWindow : Window
{
    static FloatingScheduleAdvancedAppearanceWindow? _instance;

    readonly PluginSettings? _settings;
    readonly ContentControl _centerHost = new();
    readonly Border _previewCard = new();
    readonly Canvas _previewCanvas = new();
    // 背景透卡（预览专用）：透出"卡片正下方"那块示例背景并按材质施加不同模糊（云母/亚克力/液态玻璃差异的核心）。
    Border? _previewBackdropLayer;
    int _previewBackdropMaterial;   // 3=液态玻璃 → 背景层走"真实 SDF 折射"，其余走模糊
    double _bdLeft = double.NaN, _bdTop = double.NaN, _bdW = double.NaN, _bdH = double.NaN, _bdZoom = double.NaN;
    double _zoom = 1.0;
    bool _suppress;
    // 预览拖动状态
    bool _previewDragging;
    double _dragStartX, _dragStartY, _cardStartX, _cardStartY;
    IPointer? _dragPointer;

    // ===== 自制标题栏（宿主禁用「使用系统标题栏」时与宿主窗口完全一致）=====
    const double TitleBarHeight = 32;
    // 宿主是否处于自制标题栏模式：宿主该设置需重启生效，故构造期读取一次即可。
    readonly bool _customChrome = UseCustomTitleBar();
    Panel? _titleBarPanel;
    TextBlock? _titleBarTitle;
    TextBlock? _titleBarVersion;
    IDisposable? _titleBarBackgroundBinding;
    IDisposable? _titleBarForegroundBinding;

    public static void ShowWindow(PluginSettings? settings, Window? owner)
    {
        if (_instance != null)
        {
            try { _instance.Activate(); } catch { }
            return;
        }
        var w = new FloatingScheduleAdvancedAppearanceWindow(settings) { Owner = owner };
        // 同步宿主的云母材质（宿主设置窗口在 Win11 21H2+ 且启用 WinUI 合成时使用 Mica）。
        w.SyncHostWindowMaterial(owner);
        _instance = w;
        w.Closed += (_, _) => _instance = null;
        w.Show();
    }

    public FloatingScheduleAdvancedAppearanceWindow(PluginSettings? settings)
    {
        _settings = settings;
        Title = "ATI 高级外观";
        // 初始给一个适中的回退尺寸；Opened 后按实际屏幕工作区校准（构造期 Screens 可能未就绪）。
        Width = 1040; Height = 510;   // 高度较旧版 680 降低 25%
        MinWidth = 720; MinHeight = 200;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
        Opened += OnEditorOpened;
        // 宿主禁用系统标题栏时：与宿主 FAAppWindow 一致，把内容延伸到标题栏区域，
        // 系统仍绘制右上角最小化/最大化/关闭按钮，自绘标题栏（图标 + 标题）与宿主保持一致。
        if (_customChrome)
        {
            ExtendClientAreaToDecorationsHint = true;
#if !NET10_0_OR_GREATER
            // Avalonia 11 / FA2：显式要求保留系统标题栏按钮；Avalonia 12 已移除该属性。
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.PreferSystemChrome;
#endif
        }

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
        var centerScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 12, 0),
        };
        // 内容宿主同样置顶 / 横向拉伸：否则折叠栏折叠后会被 ScrollViewer 垂直居中，悬在设置区中部。
        _centerHost.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _centerHost.VerticalContentAlignment = VerticalAlignment.Top;
        centerScroll.Content = _centerHost;
        Grid.SetColumn(centerScroll, 1);
        root.Children.Add(centerScroll);

        // ---- 右预览 ----
        var right = new Grid { RowDefinitions = RowDefinitions.Parse("Auto,*") };
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
        // 顶端提示 InfoBar（FluentAvalonia，经兼容层屏蔽 FA2/FA3 类型名差异；警告级、可关闭、深浅自适应）
        var devWarn = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Title", "提示");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Message", "部分高级材质仍在完善中，遇到问题欢迎提出 issue");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "IsClosable", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(devWarn, "Margin", new Thickness(12, 12, 12, 10));
        DockPanel.SetDock(devWarn, Dock.Top);
        outer.Children.Add(devWarn);
        outer.Children.Add(root);

        if (_customChrome)
        {
            // 顶部 32px 标题栏（与宿主 FAAppWindow 的 DefaultTitleBar 同高同内容）+ 原有设置内容
            var shell = new Grid { RowDefinitions = RowDefinitions.Parse("Auto,*") };
            var titleBar = BuildCustomTitleBar();
            Grid.SetRow(titleBar, 0);
            shell.Children.Add(titleBar);
            Grid.SetRow(outer, 1);
            shell.Children.Add(outer);
            Content = shell;
        }
        else
        {
            Content = outer;
        }

        if (_settings != null) _settings.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(RebuildPreview);
        SwitchPanel(0);
        RebuildPreview();
    }

    // ===================== 自制标题栏（宿主「使用系统标题栏」关闭时） =====================
    /// <summary>
    /// 同步宿主窗口的云母（Mica）材质：宿主设置窗口经 <c>MyWindow.EnableMicaWindow</c> 只在
    /// Windows 11 21H2+ 且 Avalonia 使用 WinUI 合成时启用 Mica（Win10 及以下不启用）。
    /// 这里读取宿主窗口实际的透明级别 / 请求级别来判断，做到与宿主同开同关：
    /// 宿主窗口开启 Mica 时本窗口同样启用（背景透明 + Mica 背板），否则保持原不透明外观。
    /// 必须在 <see cref="Window.Show"/> 之前调用，避免窗口先以不透明背景显示后再切换造成闪烁。
    /// </summary>
    internal void SyncHostWindowMaterial(Window? owner)
    {
        if (owner == null) return;
        bool hostUsesMica = owner.ActualTransparencyLevel == WindowTransparencyLevel.Mica
            || owner.TransparencyLevelHint?.Contains(WindowTransparencyLevel.Mica) == true;
        if (!hostUsesMica) return;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica };
        Background = Brushes.Transparent;
    }

    /// <summary>宿主是否处于自制标题栏模式：读取 ClassIsland 的 <c>IThemeService.UseNativeTitlebar</c>（反射以兼容各宿主版本）。</summary>
    static bool UseCustomTitleBar()
    {
        var t = FloatingScheduleService.FindHostServiceType("ClassIsland.Core.Abstractions.Services.IThemeService");
        var pi = t?.GetProperty("UseNativeTitlebar", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        return pi?.GetValue(null) is bool useNative && !useNative;
    }

    // 标题栏版本号：与「关于」页同源（插件目录 manifest.yml 的 version），读取失败回退程序集版本。
    static string FormatVersionText()
    {
        var v = AboutPage.TryReadVersionFromManifest();
        if (string.IsNullOrWhiteSpace(v))
            v = typeof(FloatingScheduleAdvancedAppearanceWindow).Assembly.GetName().Version?.ToString();
        return string.IsNullOrWhiteSpace(v) ? "AdvancedTimeIsland" : $"AdvancedTimeIsland: {v}";
    }

    Control BuildCustomTitleBar()
    {
        // 与宿主 FAAppWindow 的 DefaultTitleBar（FluentAvalonia AppWindowStyles.axaml）完全一致：
        // 高 32、背景 TitleBarBackground、前景 TitleBarForeground（窗口失焦用 *Inactive 资源）、
        // 左侧 16×16 图标（左距 8）、标题字号 12（有图标时左距 32）；
        // 右上角最小化/最大化/关闭由系统标题栏按钮绘制（与宿主相同的扩展客户区方案）。
        _titleBarPanel = new Panel
        {
            Height = TitleBarHeight,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var icon = new Image
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(8, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Source = PluginIconHelper.LoadIconBitmap(),
            IsHitTestVisible = false,
        };
        _titleBarPanel.Children.Add(icon);
        _titleBarTitle = new TextBlock
        {
            Text = Title,
            FontSize = 12,
            Margin = new Thickness(32, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        _titleBarPanel.Children.Add(_titleBarTitle);
        // 版本号：放在宿主标题栏原先显示 AppVersion 的位置（宿主那里是「版本号 + 更多按钮」，
        // 本窗口不提供「更多」，故保留其空位）。右侧让出最小化/最大化/关闭三个按钮（FA 模板每个 46px）
        // 与宿主「更多」按钮的宽度，避免被窗口按钮遮挡。
        _titleBarVersion = new TextBlock
        {
            Text = FormatVersionText(),
            FontSize = 12,
            Margin = new Thickness(0, 0, 178, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        _titleBarVersion.Bind(TextBlock.ForegroundProperty,
            _titleBarVersion.GetResourceObservable("TextFillColorSecondaryBrush"));
        _titleBarPanel.Children.Add(_titleBarVersion);

        ApplyTitleBarColors();
        Activated += (_, _) => ApplyTitleBarColors();
        Deactivated += (_, _) => ApplyTitleBarColors();
#if NET10_0_OR_GREATER
        // Avalonia 12：与 FA3 模板使用同一标题栏元素角色，交系统处理拖动 / 双击最大化 / 右键系统菜单
        Avalonia.Controls.Chrome.WindowDecorationProperties.SetElementRole(
            _titleBarPanel, WindowDecorationsElementRole.TitleBar);
#else
        // Avalonia 11 无标题栏角色 API：自行处理拖动与双击最大化
        _titleBarPanel.PointerPressed += TitleBar_PointerPressed;
#endif
        return _titleBarPanel;
    }

    /// <summary>标题栏配色直接取宿主同名主题资源，窗口失焦时切到 Inactive 资源（与 FA 模板行为一致）。</summary>
    void ApplyTitleBarColors()
    {
        if (_titleBarPanel == null) return;
        var suffix = IsActive ? "" : "Inactive";
        _titleBarBackgroundBinding?.Dispose();
        _titleBarBackgroundBinding = _titleBarPanel.Bind(
            Panel.BackgroundProperty, _titleBarPanel.GetResourceObservable("TitleBarBackground" + suffix));
        _titleBarForegroundBinding?.Dispose();
        _titleBarForegroundBinding = _titleBarTitle?.Bind(
            TextBlock.ForegroundProperty, _titleBarTitle.GetResourceObservable("TitleBarForeground" + suffix));
    }

#if NET10_0_OR_GREATER
    // Avalonia 12 在「扩展客户区」模式下由系统自绘标题栏（FA 的 WindowDrawnDecorations 模板），默认含
    // 「全屏」按钮与模板标题文字。宿主 FAAppWindow 通过 :noFullScreen 伪类和专属样式把两者隐藏，
    // 由自己的 DefaultTitleBar 绘制图标 + 标题。插件窗口是普通 Window，命中不了这些类型限定样式，
    // 因此在这里按宿主相同的处理方式直接改控件：全屏按钮 Width=0 / Opacity=0 / 不可命中（保留占位，
    // 避免如 FA GH#740 所述的布局问题），模板标题文字隐藏，改由自制标题栏绘制。
    void HideDrawnDecorationsExtras()
    {
        var host = typeof(TopLevel)
            .GetProperty("TopLevelHost", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
            .GetValue(this);
        var decorations = host?.GetType()
            .GetProperty("Decorations", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
            .GetValue(host);
        if (decorations?.GetType().GetProperty("Content")?.GetValue(decorations) is not { } content) return;
        if (content.GetType().GetProperty("Overlay")?.GetValue(content) is not Control overlay) return;
        foreach (var visual in overlay.GetVisualDescendants())
        {
            if (visual is Button btn && btn.Name == "PART_FullScreenButton")
            {
                btn.Width = 0;
                btn.Opacity = 0;
                btn.IsHitTestVisible = false;
            }
            else if (visual is TextBlock tb && tb.FindAncestorOfType<Button>() == null)
            {
                // 模板标题文字（FA 模板名为 TitleTextBlock，Avalonia 自带模板无名）；不含按钮内的字形文本
                tb.IsVisible = false;
            }
        }
    }
#endif

#if !NET10_0_OR_GREATER
    void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2 && CanMaximize)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
            return;
        }
        // 触摸指针不走 BeginMoveDrag（其内部异步捕获鼠标在触摸下会抛异常），仅鼠标拖动标题栏
        if (e.Pointer.Type != PointerType.Mouse) return;
        BeginMoveDrag(e);
        e.Handled = true;
    }
#endif

    // 窗口显示后按所在屏幕工作区校准尺寸：高 = 工作区高 × 0.615（旧版 0.82 降低 25%；给标题栏/任务栏留安全余量），
    // 宽同理；夹到最小/最大区间。避免固定尺寸在小屏或 125%/150% DPI 下标题栏溢出屏幕。
    void OnEditorOpened(object? sender, EventArgs e)
    {
        try
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen == null) return;
            var wa = screen.WorkingArea;
            // 高度系数较旧版 0.82 降低 25% → 0.615；高度下限 200（MinHeight）
            double w = Math.Clamp(Math.Floor(wa.Width * 0.82), MinWidth, 1080);
            double h = Math.Clamp(Math.Floor(wa.Height * 0.615), MinHeight, 720);
            Width = w; Height = h;
        }
        catch { }
#if NET10_0_OR_GREATER
        // 自绘装饰层在窗口显示后才建立：延后到布局完成再隐藏「全屏」按钮与模板标题文字。
        if (_customChrome) Dispatcher.UIThread.Post(HideDrawnDecorationsExtras, DispatcherPriority.Loaded);
#endif
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

    // 分区根容器：直接使用官方 SettingsExpander 样式（CompatSettingsExpander 转发 FA2/FA3 真实控件），
    // 每个分区是一张带标题的官方折叠卡片，展开后条目按 SettingsExpanderItem 排行。
    static CompatSettingsExpander NewExpander(string header) => new()
    {
        Header = header,
        IsExpanded = true,
    };

    // ===================== 各分区 UI =====================
    Control BuildWindowPanel()
    {
        var p = NewExpander("窗口");
        p.Items.Add(RowCombo("窗口材质", new[] { "半透明", "云母", "亚克力", "液态玻璃", "古卷（实验性）", "自定义图片" },
            OperatingSystem.IsWindows() ? (int)_s.FloatingScheduleMaterial : 0, i =>
            {
                _s.FloatingScheduleMaterial = (FloatingScheduleMaterialType)i;
            },
            out var matCombo));
        if (!OperatingSystem.IsWindows())
        {
            // 云母/亚克力依赖 Windows DWM 合成：非 Windows 禁用材质选择并锁定半透明（与真实悬浮窗一致）
            matCombo.IsEnabled = false;
            bool prevSuppress = _suppress;
            _suppress = true;
            matCombo.SelectedIndex = 0;
            _suppress = prevSuppress;
            p.Items.Add(new TextBlock
            {
                Text = "当前系统不支持窗口材质（云母 / 亚克力依赖 Windows 桌面窗口管理器），已锁定为半透明。",
                FontSize = 12, Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap, Opacity = 0.8
            });
        }
        p.Items.Add(RowToggle("果冻效果", _s.FloatingScheduleJellyEnabled, v => _s.FloatingScheduleJellyEnabled = v));
        p.Items.Add(RowSlider("圆角程度 (0~10px)", _s.FloatingScheduleCornerRadius, 0, 10, 0.5, v => _s.FloatingScheduleCornerRadius = v, out _));
        p.Items.Add(RowSlider("效果强度 (0~100%)", _s.FloatingScheduleMaterialIntensity * 100, 0, 100, 1, v => _s.FloatingScheduleMaterialIntensity = v / 100.0, out _));
        // 液态玻璃真折射背景的采样率（直接绑定设置，改动即时生效；见设置项 XML 注释）
        p.Items.Add(RowNumeric("液态玻璃采样率 (1~240Hz)", _s.FloatingScheduleGlassSampleRateHz, 1, 240, 0.1, v => _s.FloatingScheduleGlassSampleRateHz = v));
        // 采集方案已归中为唯一的 GDI 整屏采集，界面上不再提供切换项。
        // 拖动位移与渲染帧同步：把"窗口位置 + 玻璃背景"放在同一个 Avalonia 渲染帧内提交，消除背景 ±1 帧抖动（代价：固定 1 帧延迟）
        p.Items.Add(RowToggle("拖动位移与渲染帧同步", _s.FloatingScheduleGlassVSync, v => _s.FloatingScheduleGlassVSync = v));
        p.Items.Add(RowButton("选择自定义背景图片…", async (_, _) =>
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
                    if (!string.IsNullOrEmpty(path)) { _s.FloatingScheduleCustomBackgroundPath = path; PersistNow(); }
                }
            }
            catch { }
        }));
        p.Items.Add(new TextBlock { Text = $"当前图片：{(string.IsNullOrWhiteSpace(_s.FloatingScheduleCustomBackgroundPath) ? "（未选择）" : _s.FloatingScheduleCustomBackgroundPath)}", FontSize = 12, Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap });
        p.Items.Add(RowToggle("窗口边框", _s.FloatingScheduleBorderEnabled, v => _s.FloatingScheduleBorderEnabled = v));
        p.Items.Add(RowCombo("边框样式", new[] { "画框", "黑板", "Windows 7", "Windows XP", "Windows 10", "Windows 11", "HarmonyOS" },
            (int)_s.FloatingScheduleBorderPreset, i => _s.FloatingScheduleBorderPreset = (FloatingScheduleBorderPreset)i, out _));
        p.Items.Add(RowToggle("边框自定义颜色", _s.FloatingScheduleBorderColorEnabled, v => _s.FloatingScheduleBorderColorEnabled = v));
        p.Items.Add(RowColor("边框颜色", _s.FloatingScheduleBorderColor, v => _s.FloatingScheduleBorderColor = v));
        return p;
    }

    Control BuildTextPanel()
    {
        var p = NewExpander("文本");
        p.Items.Add(RowToggle("自定义字体", _s.FloatingScheduleFontEnabled, v => _s.FloatingScheduleFontEnabled = v));
        var fonts = new List<string> { "（跟随应用）" };
        try { fonts.AddRange(FontFamilyHelper.GetSystemFontFamilies()); } catch { }
        int fi = 0;
        if (_s.FloatingScheduleFontEnabled && !string.IsNullOrWhiteSpace(_s.FloatingScheduleTextFontFamily))
        { var k = fonts.IndexOf(_s.FloatingScheduleTextFontFamily!); if (k > 0) fi = k; }
        p.Items.Add(RowCombo("字体", fonts.ToArray(), fi, i =>
        {
            if (i <= 0) { _s.FloatingScheduleFontEnabled = false; _s.FloatingScheduleTextFontFamily = null; }
            else { _s.FloatingScheduleFontEnabled = true; _s.FloatingScheduleTextFontFamily = fonts[i]; }
        }, out _));
        p.Items.Add(new TextBlock { Text = "字号：请在「时间表悬浮窗 → 基础外观 → 课程名字号」设置（此处直接绑定）。", FontSize = 12, Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap });
        p.Items.Add(RowToggle("自定义字重", _s.FloatingScheduleFontWeightEnabled, v => _s.FloatingScheduleFontWeightEnabled = v));
        p.Items.Add(RowCombo("字重", new[] { "Thin(100)", "ExtraLight(200)", "Light(300)", "Normal(400)", "Medium(500)", "SemiBold(600)", "Bold(700)", "ExtraBold(800)", "Black(900)" },
            Math.Clamp(_s.FloatingScheduleTextFontWeight / 100 - 1, 0, 8), i => { _s.FloatingScheduleFontWeightEnabled = true; _s.FloatingScheduleTextFontWeight = (i + 1) * 100; }, out _));
        p.Items.Add(RowToggle("自定义字体颜色", _s.FloatingScheduleTextColorEnabled, v => _s.FloatingScheduleTextColorEnabled = v));
        p.Items.Add(RowColor("全局字体颜色", _s.FloatingScheduleTextColor, v => { _s.FloatingScheduleTextColorEnabled = true; _s.FloatingScheduleTextColor = v; }));
        p.Items.Add(RowToggle("按学科分色", _s.FloatingScheduleSubjectColorEnabled, v => _s.FloatingScheduleSubjectColorEnabled = v));
#if NET10_0_OR_GREATER
        // FA3（ClassIsland 2.1.1.1+）：学科主题色直接复用宿主档案 Subject.ColorHex，插件自有色表体系取消，
        // 此处仅保留开关与跳转宿主「档案 → 学科」编辑页的入口。
        p.Items.Add(new TextBlock
        {
            Text = "学科颜色直接使用 ClassIsland 档案中的学科主题色，请在 ClassIsland 档案设置中编辑。",
            FontSize = 12, Margin = new Thickness(0, 2, 0, 8), TextWrapping = TextWrapping.Wrap, Opacity = 0.8
        });
        p.Items.Add(RowButton("前往 ClassIsland 编辑学科主题色", (_, _) => OpenClassIslandSubjectEditor()));
#else
        // FA2（旧版 ClassIsland 无原生学科主题色）：保留插件端编辑，色表预置/恢复为官方默认学科色。
        // 添加学科：输入科目名 + 添加按钮
        var addName = new TextBox { Watermark = "科目名", Width = 140 };
        var addBtn = new Button { Content = "＋添加学科", Padding = new Thickness(10, 5) };
        addBtn.Click += (_, _) =>
        {
            var nm = addName.Text?.Trim() ?? "";
            if (nm.Length == 0 || _s.FloatingScheduleSubjectColorMap.ContainsKey(nm)) return;
            var defColor = SubjectColorHelper.DefaultSubjectColors.TryGetValue(nm, out var d) ? d : 0xFF7A7A7A;
            var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap) { [nm] = defColor };
            _s.FloatingScheduleSubjectColorMap = map;
            RebuildPreview(); SwitchPanel(1); PersistNow();
        };
        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 2) };
        addRow.Children.Add(addName); addRow.Children.Add(addBtn);
        p.Items.Add(addRow);
        p.Items.Add(RowButton("从课表刷新学科", (_, _) => RefreshSubjects()));
        p.Items.Add(RowButton("恢复学科默认色", (_, _) => { _s.FloatingScheduleSubjectColorMap = SubjectColorHelper.BuildDefaultSubjectColorMap(); RebuildPreview(); SwitchPanel(1); PersistNow(); }));
        foreach (var kv in _s.FloatingScheduleSubjectColorMap)
        {
            var name = kv.Key;
            p.Items.Add(SubjectRow(name, kv.Value,
                v => { var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap) { [name] = v }; _s.FloatingScheduleSubjectColorMap = map; },
                () => { var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap); map.Remove(name); _s.FloatingScheduleSubjectColorMap = map; RebuildPreview(); SwitchPanel(1); PersistNow(); }));
        }
#endif
        return p;
    }

    // 学科行：科目名 + 颜色(色块+HEX) + 移除按钮。（仅 FA2 编译路径使用：FA3 已改为复用宿主档案学科色）
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
                if (t.Length == 8 && uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) { onColor(v); swatch.Background = new SolidColorBrush(FloatScheduleRenderer.FromArgb(v)); RebuildPreview(); PersistNow(); }
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
        var p = NewExpander("表格");
        p.Items.Add(RowCombo("布局方向", new[] { "纵向排列（默认）", "横向排列（从左往右）", "横向排列（从右往左，推荐古卷）" },
            (int)_s.FloatingScheduleLayoutDirection, i => _s.FloatingScheduleLayoutDirection = (FloatingScheduleLayoutDir)i, out _));
        p.Items.Add(RowToggle("间色（斑马底）", _s.FloatingScheduleZebraStriped, v => _s.FloatingScheduleZebraStriped = v));
        p.Items.Add(RowSlider("行间距倍率 (0.5~2.0)", _s.FloatingScheduleRowSpacingFactor, 0.5, 2.0, 0.05, v => _s.FloatingScheduleRowSpacingFactor = v, out _));
        p.Items.Add(RowCombo("列间距模式", new[] { "自动适配", "自定义" }, (int)_s.FloatingScheduleColumnSpacingMode, i => _s.FloatingScheduleColumnSpacingMode = (FloatingScheduleColumnSpacingMode)i, out _));
        p.Items.Add(RowSlider("自定义列间距 (50~1000px)", _s.FloatingScheduleColumnSpacing, 50, 1000, 1, v => _s.FloatingScheduleColumnSpacing = v, out _));
        p.Items.Add(RowSlider("上边距 (0~50px)", _s.FloatingScheduleMarginTop, 0, 50, 1, v => _s.FloatingScheduleMarginTop = v, out _));
        p.Items.Add(RowSlider("下边距 (0~50px)", _s.FloatingScheduleMarginBottom, 0, 50, 1, v => _s.FloatingScheduleMarginBottom = v, out _));
        p.Items.Add(RowSlider("左边距 (0~50px)", _s.FloatingScheduleMarginLeft, 0, 50, 1, v => _s.FloatingScheduleMarginLeft = v, out _));
        p.Items.Add(RowSlider("右边距 (0~50px)", _s.FloatingScheduleMarginRight, 0, 50, 1, v => _s.FloatingScheduleMarginRight = v, out _));
        return p;
    }

    Control BuildThemePanel()
    {
        var p = NewExpander("主题");
        p.Items.Add(RowCombo("主题色", new[] { "浅色", "深色", "跟随 ClassIsland", "跟随系统" },
            (int)_s.FloatingScheduleThemeMode, i => _s.FloatingScheduleThemeMode = (FloatingScheduleThemeMode)i, out _));
        p.Items.Add(RowToggle("自定义表格底色", _s.FloatingScheduleTableColorEnabled, v => _s.FloatingScheduleTableColorEnabled = v));
        p.Items.Add(RowColor("表格底色", _s.FloatingScheduleTableColor, v => { _s.FloatingScheduleTableColorEnabled = true; _s.FloatingScheduleTableColor = v; }));
        p.Items.Add(RowToggle("自定义强调色", _s.FloatingScheduleAccentColorEnabled, v => _s.FloatingScheduleAccentColorEnabled = v));
        p.Items.Add(RowColor("强调色", _s.FloatingScheduleAccentColor, v => { _s.FloatingScheduleAccentColorEnabled = true; _s.FloatingScheduleAccentColor = v; }));
        p.Items.Add(RowToggle("自定义分隔线颜色", _s.FloatingScheduleSeparatorColorEnabled, v => _s.FloatingScheduleSeparatorColorEnabled = v));
        p.Items.Add(RowColor("分隔线颜色", _s.FloatingScheduleSeparatorColor, v => { _s.FloatingScheduleSeparatorColorEnabled = true; _s.FloatingScheduleSeparatorColor = v; }));
        p.Items.Add(RowSlider("分隔线粗细 (0~10px)", _s.FloatingScheduleSeparatorThickness, 0, 10, 0.1, v => _s.FloatingScheduleSeparatorThickness = v, out _));
        return p;
    }

    Control BuildDebugPanel()
    {
        var p = NewExpander("调试");
        p.Items.Add(RowToggle("显示 FPS 角标", _s.FloatingScheduleShowFps, v => _s.FloatingScheduleShowFps = v));
        p.Items.Add(new TextBlock { Text = "角标格式：渲染 FPS / 无等待 FPS", FontSize = 12, Margin = new Thickness(0, 0, 0, 8) });
        return p;
    }

    void RefreshSubjects()
    {
        // FA2：用官方默认学科色表补充缺失科目（色表复制自 ClassIsland 默认档案 default-subjects.json）。
        try
        {
            var map = new Dictionary<string, uint>(_s.FloatingScheduleSubjectColorMap);
            foreach (var kv in SubjectColorHelper.DefaultSubjectColors)
            {
                if (!map.ContainsKey(kv.Key)) map[kv.Key] = kv.Value;
            }
            _s.FloatingScheduleSubjectColorMap = map;
            SwitchPanel(1); PersistNow();
        }
        catch { }
    }

#if NET10_0_OR_GREATER
    // FA3：前往「档案 → 学科」编辑页（classisland://app/profile/subjects）。
    // 两级尝试：
    //  1) 进程内 Uri 导航——反射宿主 IUriNavigationService / IPublicUriNavigationService（DI 按
    //     接口注册方式在不同版本或有差异，两级类型都找；方法亦按单参 NavigateWrapped/Navigate、
    //     两参 NavigateWrapped(Uri, out _) 依次尝试）；
    //  2) 兜底：用系统协议 URL 前往（宿主已把 classisland:// 注册到 HKCU\Software\Classes，
    //     shell 激活后经 IPC 由运行实例接管导航）。
    const string SubjectEditorUrl = "classisland://app/profile/subjects";
    void OpenClassIslandSubjectEditor()
    {
        // 1) 进程内导航
        try
        {
            if (IAppHost.Host != null)
            {
                var tNav = FloatingScheduleService.FindHostServiceType("ClassIsland.Core.Abstractions.Services.IUriNavigationService")
                        ?? FloatingScheduleService.FindHostServiceType("ClassIsland.Shared.IPC.Abstractions.Services.IPublicUriNavigationService");
                var nav = tNav == null ? null : IAppHost.Host.Services.GetService(tNav);
                if (tNav != null && nav != null)
                {
                    var uri = new Uri(SubjectEditorUrl);
                    var m = tNav.GetMethod("NavigateWrapped", new[] { typeof(Uri) })
                         ?? tNav.GetMethod("Navigate", new[] { typeof(Uri) });
                    if (m != null)
                    {
                        m.Invoke(nav, new object[] { uri });
                        return;
                    }
                    var m2 = tNav.GetMethod("NavigateWrapped", new[] { typeof(Uri), typeof(Exception).MakeByRefType() });
                    if (m2 != null)
                    {
                        m2.Invoke(nav, new[] { (object?)uri, (object?)null });
                        return;
                    }
                }
            }
        }
        catch { }
        // 2) 兜底：系统协议 URL 前往
        try
        {
            using var _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SubjectEditorUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }
#endif

    // ===================== 预览 =====================
    // 预览课表取自"当前实际加载的档案"（与悬浮窗同一数据源）；仅宿主服务未就绪时才回退到这份示例数据。
    static readonly string[] SampleSubjects = { "语文", "数学", "英语", "物理", "化学" };

    void RebuildPreview()
    {
        if (_settings == null) return;
        var model = BuildPreviewModel();
        FloatScheduleRenderer.ApplyCardStyle(_previewCard, model);
        var (root, _) = FloatScheduleRenderer.Build(model);
        // 非 Windows 无系统材质合成 → 预览锁定半透明（与真实悬浮窗 FloatingScheduleService.ResolveMaterialAv 一致）
        int mat = OperatingSystem.IsWindows() ? (int)_s.FloatingScheduleMaterial : 0;
        if (!_s.EnableExperimentalFeatures && (mat == 4 || mat == 5)) mat = 0;   // 液态玻璃(3) 已转正
        // 背景透卡：云母/亚克力/液态玻璃需要"背后内容"才成立。编辑器是不透明窗口，采不到真实桌面，
        // 故在卡片内裁出"卡片正下方的示例背景"这块，由材质层按材质施加不同模糊（云母轻 / 亚克力中 / 液态玻璃≈0）。
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

    FloatScheduleRenderModel BuildPreviewModel()
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
            LayoutDirection = (int)_s.FloatingScheduleLayoutDirection,
            RowSpacingFactor = _s.FloatingScheduleRowSpacingFactor,
            ColumnSpacingMode = (int)_s.FloatingScheduleColumnSpacingMode,
            ColumnSpacing = _s.FloatingScheduleColumnSpacingMode == FloatingScheduleColumnSpacingMode.Custom ? _s.FloatingScheduleColumnSpacing : 14,
            ZebraStriped = _s.FloatingScheduleZebraStriped,
            ZebraRowArgb = (fl || (!fd && !ThemeHelper.IsDarkTheme())) ? 0x14000000u : 0x1AFFFFFFu,
            FontWeightCustom = _s.FloatingScheduleFontWeightEnabled ? _s.FloatingScheduleTextFontWeight : 0,
            SeparatorThickness = _s.FloatingScheduleSeparatorThickness,
            ShowFps = _s.FloatingScheduleShowFps,
            CurrentClassIndex = -1,
        };
        // 预览内容 = 当前实际加载档案的时间表（课程/时间/教师/学科色/当前课高亮/课间行/分隔线，
        //   口径与悬浮窗 RefreshSchedule 完全一致）；宿主服务未就绪时返回 false → 回退下方示例数据。
        if (FloatingScheduleService.FillPreviewFromProfile(m, _s)) return m;

        string[] times = { "08:00 - 08:40", "08:50 - 09:30", "09:40 - 10:20", "10:30 - 11:10", "11:20 - 12:00" };
        string[] teachers = { "张老师", "李老师", "王老师", "陈老师", "赵老师" };
        for (int i = 0; i < SampleSubjects.Length; i++)
        {
            uint sc = 0;
            if (_s.FloatingScheduleSubjectColorEnabled)
            {
#if NET10_0_OR_GREATER
                // FA3：插件映射已取消，预览用官方默认学科色表示意宿主档案色效果
                SubjectColorHelper.DefaultSubjectColors.TryGetValue(SampleSubjects[i], out sc);
#else
                _s.FloatingScheduleSubjectColorMap.TryGetValue(SampleSubjects[i], out sc);
#endif
            }
            m.Rows.Add(new FloatScheduleRowModel { Course = SampleSubjects[i], Teacher = teachers[i], TimeText = times[i], SubjectColorArgb = sc });
        }
        m.Break = new FloatScheduleBreakModel { AfterClassIndex = 2, Name = "课间休息", TimeText = "10:20 - 10:30" };
        m.SeparatorAfterClassIndex.Add(3);
        m.CurrentClassIndex = 1;
        m.ClassProgressRatio = 0.45;
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

    // 单项设置：官方 SettingsExpanderItem（标题左、控件右、可选说明），由所属分区 Expander 承载。
    static CompatSettingsExpanderItem Row(string label, Control editor, string? description = null) => new()
    {
        Content = label,
        Description = description ?? string.Empty,
        Footer = editor,
    };

    CompatSettingsExpanderItem RowToggle(string label, bool val, Action<bool> set)
    {
        var t = new ToggleSwitch { IsChecked = val };
        t.IsCheckedChanged += (_, _) => { if (_suppress) return; set(t.IsChecked == true); RebuildPreview(); PersistNow(); };
        return Row(label, t);
    }

    CompatSettingsExpanderItem RowCombo(string label, string[] items, int sel, Action<int> set, out ComboBox combo)
    {
        // 先填充 Items 再设 SelectedIndex：空 Items 上设置 SelectedIndex 在新版 Avalonia（FA3/net10）
        // 中不会挂起待 Items 添加后补选，而是直接丢弃 → 重进窗口后下拉框文本丢失（配置本身未丢）。
        var c = new ComboBox { MinWidth = 200 };
        foreach (var it in items) c.Items.Add(it);
        c.SelectedIndex = Math.Clamp(sel, 0, items.Length - 1);
        c.SelectionChanged += (_, _) => { if (_suppress || c.SelectedIndex < 0) return; set(c.SelectedIndex); RebuildPreview(); PersistNow(); };
        combo = c; return Row(label, c);
    }

    // 数值输入行（NumericUpDown）：范围/步长由参数给定，改动即时写回设置（主进程与子进程都热跟随）
    CompatSettingsExpanderItem RowNumeric(string label, double val, double min, double max, double step, Action<double> set)
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
            PersistNow();
        };
        return Row(label, n);
    }

    CompatSettingsExpanderItem RowSlider(string label, double val, double min, double max, double step, Action<double> set, out Slider slider)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, MinWidth = 220 };
        var s = new Slider { Minimum = min, Maximum = max, Value = val, Width = 160, TickFrequency = step };
        var tb = new TextBlock { Text = val.ToString("0.##", CultureInfo.InvariantCulture), VerticalAlignment = VerticalAlignment.Center, Width = 48 };
        s.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) { if (_suppress) return; set(s.Value); tb.Text = s.Value.ToString("0.##", CultureInfo.InvariantCulture); RebuildPreview(); PersistNow(); }
        };
        sp.Children.Add(s); sp.Children.Add(tb);
        slider = s; return Row(label, sp);
    }

    CompatSettingsExpanderItem RowButton(string label, EventHandler<RoutedEventArgs> handler)
    {
        var b = new Button { Content = label, Padding = new Thickness(12, 5), HorizontalAlignment = HorizontalAlignment.Left };
        b.Click += handler;
        return Row(label, b);
    }

    // 颜色行：使用 Avalonia 官方 ColorPicker（与插件其他设置页一致），支持取色盘 / 色值输入；
    // 颜色变更即时写回设置、刷新预览并落盘。
    CompatSettingsExpanderItem RowColor(string label, uint argb, Action<uint> set)
    {
        var picker = new ColorPicker
        {
            Width = 160,
            Color = FloatScheduleRenderer.FromArgb(argb),
        };
        picker.ColorChanged += (_, _) =>
        {
            if (_suppress) return;
            set(FloatScheduleRenderer.ToArgb(picker.Color));
            RebuildPreview();
            PersistNow();
        };
        return Row(label, picker);
    }

    // 设置项写回后立即原子落盘（后台线程执行，不阻塞 UI）：
    // 常规路径 PropertyChanged → 500ms 防抖，改动停留在防抖窗口期内时若宿主退出/崩溃会丢失；
    // 高级外观窗口的所有设置项写回后绕过防抖立即保存，Slider 拖动等连续触发的保存请求在 SaveSettings 锁内串行。
    void PersistNow()
    {
        try { Plugin.Instance?.SaveSettingsNow(); } catch { }
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
        // 细节色块（落在卡片正下方区域）：让"模糊程度"可见——云母/亚克力把细节糊掉，液态玻璃保留清晰。
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
    // 模糊半径由材质层（FloatScheduleMaterialApplier.Apply）按材质设置（云母轻 / 亚克力中 / 液态玻璃≈0）。
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
            // 与真机抓帧侧同源：depthEffect=false，位移仅取 SDF 梯度（边缘法线），避免"放射状"星芒。
            // transparentCenter=true：与真机一致 —— 只有贴边一带折射，中部透明（露出示例背景）。
            depthEffect: false, chromaticAberration: 0.10, highlightStrength: 0.35, highlightAngleDeg: 45, highlightFalloff: 4.0,
            transparentCenter: true);
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
