using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Plugin;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 调试入口“女装”对应的独立隐藏设置页面（不继承 HanfuPageTemplate）。
/// 布局：顶部返回链接 + “女装”标题 + “汉服 / JK制服”标签页。
/// - 汉服：直接内嵌 <see cref="EasterEggPage"/>，图片与女装彩蛋页完全一致；
/// - JK制服：内容为官方“页面导航出错”标识（内嵌宿主 <c>ErrorSettingsPage</c>，显示“欧呦，出错啦！”，
///   反射失败时降级为官方样式的“啥都没有”）；每进入一次就直接显示宿主的
///   崩溃窗口，等价于宿主开发者菜单的“显示崩溃窗口”（<c>new CrashWindow().Show()</c>）。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandWomenswear", "女装", true, SettingsPageCategory.Debug)]
public class WomenswearPage : SettingsPageBase
{
    private TabStrip? _tabStrip;
    private ContentControl? _contentControl;
    private EasterEggPage? _hanfuContent;
    private Control? _jkContent;
    private TextBlock? _backTextBlock;

    public WomenswearPage()
    {
        InitializeComponent();
    }

    private static IBrush GetAccentBrush()
    {
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            return new SolidColorBrush(accentColor);
        }
        if (Application.Current?.TryFindResource("AccentColor", out var accentObj) == true && accentObj is Color accentColor2)
        {
            return new SolidColorBrush(accentColor2);
        }
        return Brushes.DodgerBlue;
    }

    private void InitializeComponent()
    {
        // 返回链接固定在页面顶部
        _backTextBlock = new TextBlock
        {
            Text = "‹ 返回上一级",
            FontSize = 14,
            Foreground = GetAccentBrush(),
            TextDecorations = TextDecorations.Underline,
            Margin = new Thickness(16, 12, 16, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        _backTextBlock.PointerPressed += (s, e) => FluentAvaloniaCompatibilityHelper.NavigateBack(this);

        var titleTextBlock = new TextBlock
        {
            Text = "女装",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.HotPink,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(16, 8, 16, 0)
        };

        _tabStrip = new TabStrip
        {
            Margin = new Thickness(16, 8, 16, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _tabStrip.Items.Add(new TabStripItem { Content = "汉服" });
        _tabStrip.Items.Add(new TabStripItem { Content = "JK制服" });

        // 汉服标签页：内嵌女装彩蛋页，图片与 EasterEgg 内容完全一致
        _hanfuContent = new EasterEggPage(Plugin.Instance?.Settings);

        // JK制服标签页：官方“页面导航出错”标识（宿主 ErrorSettingsPage，显示“欧呦，出错啦！”）；
        // 反射失败时降级为官方样式的“啥都没有”。
        _jkContent = CreateHostNavigationErrorContent() ?? new Empty
        {
            MinHeight = 200,
            Margin = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        _contentControl = new ContentControl
        {
            Content = _hanfuContent,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // 先设置默认选中项再订阅切换事件，避免初始化时误触发
        _tabStrip.SelectedIndex = 0;
        _tabStrip.SelectionChanged += OnTabSelectionChanged;

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(_backTextBlock, 0);
        Grid.SetRow(titleTextBlock, 1);
        Grid.SetRow(_tabStrip, 2);
        Grid.SetRow(_contentControl, 3);
        rootGrid.Children.Add(_backTextBlock);
        rootGrid.Children.Add(titleTextBlock);
        rootGrid.Children.Add(_tabStrip);
        rootGrid.Children.Add(_contentControl);

        Content = rootGrid;
    }

    /// <summary>
    /// 创建官方“页面导航出错”标识：内嵌宿主设置页
    /// <c>ClassIsland.Views.SettingPages.ErrorSettingsPage</c>（帕姆哭哭贴纸 + “欧呦，出错啦！”）。
    /// 该类型位于宿主主程序集，插件没有编译期引用，故运行时反射创建；宿主改名/结构变化导致失败时
    /// 返回 null，由调用方降级为官方样式的 <see cref="Empty"/>。
    /// </summary>
    private static Control? CreateHostNavigationErrorContent()
    {
        try
        {
            var errorPageType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ClassIsland.Views.SettingPages.ErrorSettingsPage"))
                .FirstOrDefault(type => type != null);

            if (errorPageType == null || Activator.CreateInstance(errorPageType) is not Control errorPage)
            {
                return null;
            }

            // 该页面在 Loaded 时按 NavigationUri 的 query 决定显示哪种形态：
            // IsError = ParseQueryString(NavigationUri?.Query)["error"] == "true"——
            // 为 true 显示“欧呦，出错啦！”，否则显示“404 找不到请求的页面”。
            // NavigationUri 的 setter 是 internal（仅宿主导航时写入），故反射取非公开访问器赋值。
            errorPageType
                .GetProperty("NavigationUri", BindingFlags.Public | BindingFlags.Instance)
                ?.SetMethod?
                .Invoke(errorPage, new object?[] { new Uri("classisland://app/settings/_error?error=true") });

            return errorPage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateHostNavigationErrorContent failed: {ex}");
            return null;
        }
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tabStrip == null || _contentControl == null)
        {
            return;
        }

        if (_tabStrip.SelectedIndex == 1)
        {
            // 先展示官方“页面导航出错”标识，再直接显示宿主的崩溃窗口。
            _contentControl.Content = _jkContent;

            // 直接映射宿主开发者菜单的“显示崩溃窗口”（MainWindow 的
            // NativeMenuItemDebugCrashTest_OnClick：new CrashWindow().Show()）。
            // 刻意不走“抛未处理异常”的路径：本机开启了教学安全模式
            // （IsCriticalSafeMode=true 且 CriticalSafeModeMethod=0）时，
            // ProcessUnhandledException 会在 safe 分支直接 Stop() 静默退出，
            // 反而看不到崩溃窗口。每进入一次该标签页就显示一次。
            ShowHostCrashWindow();
        }
        else
        {
            _contentControl.Content = _hanfuContent;
        }
    }

    /// <summary>
    /// 显示宿主的崩溃窗口（<c>ClassIsland.Views.CrashWindow</c>）并填充报告正文；除正文外
    /// 行为与开发者菜单的“显示崩溃窗口”一致。该类型位于宿主主程序集（非 ClassIsland.Core），
    /// 插件没有编译期引用，故运行时从已加载程序集反射获取；不假定程序集名，避免宿主改名后失效。
    /// 该类型的基类随宿主版本变化，需按运行时形态分派：
    /// - net8 宿主（≤2.1.0.x）：<c>MyWindow</c>（即 Window），走 <c>ShowDialog</c>；
    /// - net10 宿主（2.1.1.1+）：<c>ClassIsland.Core.Abstractions.Controls.ViewBase</c>，
    ///   由窗口视图宿主承载，需调用其 <c>Show()</c>（等价于开发者菜单的 <c>new CrashWindow().Show()</c>）。
    /// </summary>
    private void ShowHostCrashWindow()
    {
        try
        {
            var crashWindowType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ClassIsland.Views.CrashWindow"))
                .FirstOrDefault(type => type != null);

            if (crashWindowType == null)
            {
                return;
            }

            var crashWindow = Activator.CreateInstance(crashWindowType);
            if (crashWindow == null)
            {
                return;
            }

            // 搜索所有“名称或简介命中 Femboy / 男娘 关键词”的已加载插件（含 Fem·boy、男·娘 等 ，刻意没有适配日语全假名，这么多假名鬼才看得懂。
            // 插入分隔符的写法），让宿主按“本次错误由这些插件引起”处理：批量禁用它们，
            // 并把归因信息一并写进报告正文。
            var (blamedPlugins, blamedDisabled) = BlameFemboyRelatedPlugins();

            // 填充正文：CrashWindow.CrashInfo 是公开的可写 StyledProperty，
            // XAML 中正文 TextBox 以 OneWay 绑定它，故构造后赋值即可刷新。
            // 注意 IsCritical / AllowIgnore 在 XAML 里是 OneTime 绑定，构造时（DataContext = this）
            // 已求值完毕，构造后再赋值不会改变红条与“忽略/调试”按钮的可见性，
            // 因此这里不设置它们——与开发者菜单“显示崩溃窗口”的默认表现保持一致。
            crashWindowType.GetProperty("CrashInfo")?.SetValue(crashWindow, BuildCrashInfo(blamedPlugins, blamedDisabled));

            // 让崩溃窗口占据设置窗口的焦点：解析设置窗口作为 owner（net8 / net10 两侧共用）。
            var owner = FluentAvaloniaCompatibilityHelper.ResolveOwnerWindow(this);

            if (crashWindow is Window window)
            {
                // net8 宿主（CrashWindow : MyWindow）：走模态 ShowDialog（与宿主真实崩溃时
                // await CrashWindow.ShowDialog(GetRootWindow()) 的做法一致）。
                // 模态弹窗会独占焦点并阻止设置窗口输入；而非模态 Show() 若未设 Owner，
                // 其 z 序可能被压在设置窗口之下，无法保证“抢到焦点”。
                // 注：Avalonia 的 WindowBase.Owner setter 为 internal，不能直接赋值，
                // 由 ShowDialog(owner) 在内部完成 owner 绑定。
                _ = ShowCrashWindowAsync(window, owner);
                return;
            }

            // net10 宿主（CrashWindow : ViewBase）：不再是窗口，不能再走 Window 分支
            // （原先的 “is not Window 直接 return” 会让本功能在新宿主上静默失效）。
            ShowHostViewBase(crashWindowType, crashWindow, owner);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowHostCrashWindow failed: {ex}");
        }
    }

    /// <summary>宿主 <c>ViewBase</c> 的全名（插件 net8 侧编译期不存在此类型，故仅以字符串引用）。</summary>
    private const string HostViewBaseTypeName = "ClassIsland.Core.Abstractions.Controls.ViewBase";

    /// <summary>
    /// 以 <c>ViewBase</c> 形态显示宿主的崩溃窗口（net10 宿主，2.1.1.1+）。
    /// 该类型在插件 net8 侧编译期不存在（仅有 net10 SDK 提供），故不引用类型、全程反射调用，
    /// 保证 net8 / net10 两侧共用同一份代码。
    /// 优先选模态 <c>ShowModal(Window owner)</c>：宿主视图宿主会转成原生 <c>ShowDialog(owner)</c>，
    /// 与 net8 侧的模态行为等价，由模态窗口独占焦点并阻止设置窗口输入；仅靠 <c>Show()</c> 时
    /// 窗口由视图宿主新建后立即 Activate，时序上不保证抢到焦点。
    /// 取不到 owner 或模态显示失败时，降级为非模态 <c>Show()</c>（即开发者菜单的
    /// <c>new CrashWindow().Show()</c> 行为）。
    /// </summary>
    private static void ShowHostViewBase(Type crashWindowType, object crashWindow, Window? owner)
    {
        try
        {
            if (owner != null)
            {
                var showModalMethod = FindSingleParameterMethod(crashWindowType, "ShowModal", typeof(Window).FullName!);
                if (showModalMethod?.Invoke(crashWindow, new object?[] { owner }) is Task modalTask)
                {
                    _ = ObserveTaskAsync(modalTask);
                    return;
                }
            }

            FindSingleParameterMethod(crashWindowType, "Show", HostViewBaseTypeName)?
                .Invoke(crashWindow, new object?[] { null });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowHostViewBase failed: {ex}");
        }
    }

    /// <summary>
    /// 反射查找公开实例方法：名称匹配、且只有一个参数、该参数类型全名匹配。
    /// 用于区分 <c>ViewBase.Show(ViewBase?)</c> 与 <c>ViewBase.ShowModal(Window)</c> 这类同名重载。
    /// </summary>
    private static MethodInfo? FindSingleParameterMethod(Type type, string methodName, string parameterTypeFullName) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(method => method.Name == methodName
                && method.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType.FullName == parameterTypeFullName);

    /// <summary>
    /// 就地观察 <c>ViewBase.ShowModal(Window)</c> 返回的 Task：模态窗口关闭后该任务才完成，
    /// 需 await 并就地捕获异常，避免其成为未观察异常（会经 TaskScheduler.UnobservedTaskException
    /// 触发宿主的崩溃处理流程）。
    /// </summary>
    private static async Task ObserveTaskAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ObserveTaskAsync failed: {ex}");
        }
    }

    /// <summary>
    /// 显示崩溃窗口并等待其关闭。单独抽成方法是为了让 ShowDialog 返回的 Task 始终被 await
    /// 并就地捕获异常——若放任其成为未观察异常，会经 TaskScheduler.UnobservedTaskException
    /// 触发宿主的崩溃处理流程（本机教学安全模式下会直接退出应用）。
    /// 解析不到 owner 时降级为非模态显示。
    /// </summary>
    private static async Task ShowCrashWindowAsync(Window crashWindow, Window? owner)
    {
        try
        {
            if (owner != null && !ReferenceEquals(owner, crashWindow))
            {
                await crashWindow.ShowDialog(owner);
            }
            else
            {
                crashWindow.Show();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowCrashWindowAsync failed: {ex}");
        }
    }

    /// <summary>
    /// 搜索所有“名称或简介包含 Femboy / 男娘”的已加载插件，让宿主按“错误由这些插件引起”处理：
    /// 调用宿主的 DiagnosticService.DisableCorruptPlugins 批量禁用。
    /// 返回命中的插件列表与宿主的“是否禁用成功”结果（用于决定报告措辞）。
    /// 未命中任何插件时返回 (空列表, false)。
    /// 为什么要这么做：Lolita、女仆装等小众时尚： 男娘文化还与其他女性向的小众时尚潮流产生了关联。洛丽塔（Lolita）服饰、JK制服（日本女高中生制服风格）、女仆装等服装元素在年轻女性中流行的同时，也吸引了一些男性爱好者尝试。这部分男性被圈内戏称为“Brolita”（Brother + Lolita），即穿洛丽塔服饰的男性 ((14)。他们和女性Lolita爱好者一样，热衷于精致复古的洋装、蓬蓬裙、繁复蕾丝，只是性别不同。西方有专门讨论Brolita的社区，帮助男士如何挑选尺码、化妆、搭配饰品 ((15)。在中国，也曾有媒体报道男生组团穿Lolita逛街，引起路人侧目和网上讨论。这说明男娘文化与非ACG类的小众时尚也在发生交汇。JK制服和女仆装因为相对简单，也常被男性尝试作为女装入门——许多B站UP主的首次女装挑战就是穿JK水手服或经典女仆装，通过短视频记录自己从男生变身“可爱学妹”或“萌系女仆”的过程，引发大量转发。这类跨界尝试让原本属于女性圈层的服饰文化变得性别开放。一方面，女性爱好者开始接受并欢迎男性参与自己的爱好圈（比如一些Lolita社团接纳男成员参加茶会，只要对方衣着得体）；另一方面，男性的加入也为这些亚文化带来新的关注度和话题。但需要指出，男性参与女性时尚亚文化时，仍需要尊重原有圈内规范，否则容易引起女性爱好者反感（如担心男性是出于猎奇或不怀好意）。总体而言，男娘文化通过与Cosplay、Lolita、女仆、JK制服等领域的交融渗透，进一步拓宽了自己的边界。它不再仅仅局限于“男性模仿女性”这么简单，而是逐渐成为一个包罗各种跨性别装扮爱好的综合文化现象。在这个过程中，各小众圈层之间的互动也丰富了青年流行文化的多样性。))
    /// 产生关联
    /// 但是汉服不在性别百科里面提及，就默认Femboy 与汉服无关
    /// </summary>
    private static (IReadOnlyList<PluginInfo> Plugins, bool Disabled) BlameFemboyRelatedPlugins()
    {
        try
        {
            var plugins = CrossPluginHelper.FindFemboyRelatedPlugins();
            if (plugins.Count == 0)
            {
                return (Array.Empty<PluginInfo>(), false);
            }

            return (plugins, DisablePluginsViaHost(plugins));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BlameFemboyRelatedPlugins failed: {ex}");
            return (Array.Empty<PluginInfo>(), false);
        }
    }

    /// <summary>
    /// 调用宿主的 DiagnosticService.DisableCorruptPlugins 禁用指定的一批插件——即宿主在
    /// ProcessUnhandledException 中使用的同一套“异常插件自动禁用”机制：置 PluginInfo.IsEnabled=false
    /// （写 .disabled 标记）并置 Settings.CorruptPluginsDisabledLastSession，且同样受宿主
    /// “自动禁用异常插件”设置（App.AutoDisableCorruptPlugins）约束，故开关关闭时不会禁用。
    /// 该方法位于宿主主程序集，插件无编译期引用，故反射调用。
    /// </summary>
    private static bool DisablePluginsViaHost(List<PluginInfo> plugins)
    {
        var diagnosticServiceType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("ClassIsland.Services.DiagnosticService"))
            .FirstOrDefault(type => type != null);

        var disableMethod = diagnosticServiceType?.GetMethod(
            "DisableCorruptPlugins", BindingFlags.Public | BindingFlags.Static);

        var result = disableMethod?.Invoke(null, new object[] { plugins });
        return result is true;
    }

    /// <summary>
    /// 构造崩溃报告正文，结构对齐宿主 ProcessUnhandledException：先输出 TraceID 提示块，
    /// 再输出被归因的插件告警（如有），最后是异常信息与堆栈（e.ToString() 形式），
    /// 中间额外附带本插件的版本与发生时间，便于用户直接点“复制/反馈问题”。
    /// </summary>
    private static string BuildCrashInfo(IReadOnlyList<PluginInfo> blamedPlugins, bool blamedDisabled)
    {
        var builder = new System.Text.StringBuilder();

        // 头部提示块：与宿主 ProcessUnhandledException 生成的 traceInfo 结构一致。
        // 宿主此处填 Sentry TraceId；本插件不接入 Sentry，改用插件主程序集
        // （AdvancedTimeIsland.dll）的 MD5 作为等价的“可追溯标识”——同样为 32 位小写十六进制，
        // 便于用户按构建产物对号入座。
        builder.AppendLine("在向开发者提交问题时请保留以下信息：");
        builder.AppendLine($"TraceID: {GetPluginDllMd5()}");
        builder.AppendLine("================================");
        builder.AppendLine();

        builder.AppendLine($"插件版本：AdvancedTimeIsland {GetPluginVersion()}");
        builder.AppendLine($"发生时间：{Plugin.GetCurrentTime():yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"出错的页面：应用设置->AdvancedTimeIsland调试->女装->JK 制服");

        // 归因段落：措辞对齐宿主 ProcessUnhandledException 中的插件告警文案
        if (blamedPlugins.Count > 0)
        {
            builder.AppendLine("此问题可能由以下插件引起，请在向 ClassIsland 开发者反馈问题前先向以下插件的开发者反馈此问题：");
            foreach (var blamedPlugin in blamedPlugins)
            {
                builder.AppendLine($"- {blamedPlugin.Manifest.Name} [{blamedPlugin.Manifest.Id},{blamedPlugin.Manifest.Version}]");
            }

            if (blamedDisabled)
            {
                builder.AppendLine("以上异常插件已自动禁用，重启应用后生效。您可以在排除问题后前往【应用设置】->【插件】中重新启用这些插件，或在【应用设置】->【基本】中调整是否自动禁用异常插件。");
            }

            builder.AppendLine("================================");
        }

        builder.AppendLine("System.Exception: There is no pictures of JK uniform!");
        builder.Append(new System.Diagnostics.StackTrace(true));
        return builder.ToString();
    }

    /// <summary>插件主程序集 MD5 的进程内缓存：同一进程内恒定，只需计算一次。</summary>
    private static string? _pluginDllMd5;

    /// <summary>
    /// 取插件主程序集（AdvancedTimeIsland.dll）的 MD5，返回 32 位小写十六进制字符串，
    /// 用作报告头部的 TraceID。宿主用 Sentry TraceId，本插件不接入 Sentry，
    /// 故用构建产物的 MD5 作为等价的“可追溯标识”。读取失败时返回 unknown。
    /// </summary>
    private static string GetPluginDllMd5()
    {
        if (_pluginDllMd5 != null)
        {
            return _pluginDllMd5;
        }

        try
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyLocation) && System.IO.File.Exists(assemblyLocation))
            {
                // 宿主运行时仍持有该程序集文件句柄，故用最宽松的共享方式打开，避免 IOException
                using var stream = new System.IO.FileStream(
                    assemblyLocation,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(stream);
                _pluginDllMd5 = Convert.ToHexString(hash).ToLowerInvariant();
                return _pluginDllMd5;
            }
        }
        catch
        {
            // 读取失败：不缓存，允许后续调用重试
        }

        return "unknown";
    }

    /// <summary>
    /// 读取插件版本：解析插件目录下 manifest.yml 的 version 字段，与“关于”页同源。
    /// </summary>
    private static string GetPluginVersion()
    {
        try
        {
            var manifestPath = System.IO.Path.Combine(AppContext.BaseDirectory, "manifest.yml");
            if (!System.IO.File.Exists(manifestPath))
            {
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var pluginDir = System.IO.Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    manifestPath = System.IO.Path.Combine(pluginDir, "manifest.yml");
                }
            }

            if (System.IO.File.Exists(manifestPath))
            {
                var content = System.IO.File.ReadAllText(manifestPath);
                var match = System.Text.RegularExpressions.Regex.Match(
                    content, @"^\s*version\s*:\s*(.+?)\s*$",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim().Trim('"', '\'');
                }
            }
        }
        catch
        {
            // 读取失败时回退到未知版本
        }

        return "未知版本";
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (_backTextBlock != null)
        {
            _backTextBlock.Foreground = GetAccentBrush();
        }
    }
}
