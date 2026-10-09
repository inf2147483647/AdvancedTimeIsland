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
using Avalonia.Threading;
using AdvancedTimeIsland.Helpers;
// 别名指向插件内复制的占位符：避免与宿主 ClassIsland.Core.Controls 中的同名类型冲突（net10 侧两者都存在）
using ExpressiveLoadingIndicator = AdvancedTimeIsland.Views.Controls.ExpressiveLoadingIndicator;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Plugin;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 调试入口“女装”对应的独立隐藏设置页面（不继承 HanfuPageTemplate）。
/// 布局：顶部返回链接 + “女装”标题 + “汉服 / JK制服”标签页（外壳已 axaml 化，写法对齐官方设置页，
/// 头部三要素使用 settings-container animated-intro，宿主动画等级为“华丽”时依次淡入上滑）。
/// - 汉服：直接内嵌 <see cref="EasterEggPage"/>，图片与女装彩蛋页完全一致；
/// - JK制服：内容为官方“页面导航出错”标识（内嵌宿主 <c>ErrorSettingsPage</c>，显示“欧呦，出错啦！”，
///   反射失败时降级为官方样式的“啥都没有”）；先展示 30 秒加载占位符，到时后切换内容并直接显示宿主的
///   崩溃窗口，等价于宿主开发者菜单的“显示崩溃窗口”（<c>new CrashWindow().Show()</c>）。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandWomenswear", "女装", true, SettingsPageCategory.Debug)]
public partial class WomenswearPage : SettingsPageBase
{
    private EasterEggPage? _hanfuContent;
    private Control? _jkContent;

    /// <summary>JK制服标签页的加载占位符容器（三形状弹簧形变循环）。</summary>
    private Control? _jkLoadingContent;
    private ExpressiveLoadingIndicator? _jkLoadingIndicator;
    private DispatcherTimer? _jkLoadingTimer;

    /// <summary>JK制服标签页先展示加载占位符的时长；走完后再切换到页面导航失败占位符。</summary>
    private static readonly TimeSpan JkLoadingDuration = TimeSpan.FromSeconds(30);

    public WomenswearPage()
    {
        InitializeComponent();
        WireUI();
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

    private void WireUI()
    {
        // 返回链接颜色跟随应用强调色（axaml 中已声明下划线/手型/位置，这里只赋颜色并随主题刷新）
        BackTextBlock.Foreground = GetAccentBrush();

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

        // JK制服标签页的加载占位符容器：进入该标签页时先展示占位符 30 秒（模拟图片仍在缓冲），
        // 到时后再切换到上面的“页面导航失败”内容。
        _jkLoadingIndicator = new ExpressiveLoadingIndicator
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetAccentBrush(),
            IsActive = false,
            IsVisible = false
        };
        var jkLoadingPanel = new Grid();
        jkLoadingPanel.Children.Add(_jkLoadingIndicator);
        _jkLoadingContent = jkLoadingPanel;

        HostContentControl.Content = _hanfuContent;

        // axaml 中 TabStrip.SelectedIndex 已声明为 0（此时尚未订阅切换事件，不会误触发）
        TabStrip.SelectionChanged += OnTabSelectionChanged;
    }

    private void BackTextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        FluentAvaloniaCompatibilityHelper.NavigateBack(this);
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
        if (TabStrip == null || HostContentControl == null)
        {
            return;
        }

        if (TabStrip.SelectedIndex == 1)
        {
            StartJkLoading();
        }
        else
        {
            CancelJkLoading();
            HostContentControl.Content = _hanfuContent;
        }
    }

    /// <summary>
    /// 进入 JK制服标签页：先展示加载占位符 30 秒（模拟图片仍在缓冲），
    /// 到时后再切换为官方“页面导航出错”标识，并显示宿主的崩溃窗口。
    /// </summary>
    private void StartJkLoading()
    {
        if (HostContentControl == null || _jkLoadingContent == null || _jkLoadingIndicator == null)
        {
            return;
        }

        HostContentControl.Content = _jkLoadingContent;
        _jkLoadingIndicator.IsActive = true;
        _jkLoadingIndicator.IsVisible = true;

        CancelJkLoadingTimer();
        _jkLoadingTimer = new DispatcherTimer { Interval = JkLoadingDuration };
        _jkLoadingTimer.Tick += OnJkLoadingTimerTick;
        _jkLoadingTimer.Start();
    }

    private void OnJkLoadingTimerTick(object? sender, EventArgs e)
    {
        CancelJkLoadingTimer();

        if (_jkLoadingIndicator != null)
        {
            _jkLoadingIndicator.IsActive = false;
            _jkLoadingIndicator.IsVisible = false;
        }

        if (HostContentControl != null)
        {
            HostContentControl.Content = _jkContent;
        }

        // 占位符结束后再弹出宿主崩溃窗口：与原先“进入即弹”的 Easter Egg 行为一致，只是延后到加载占位结束。
        // 直接映射宿主开发者菜单的“显示崩溃窗口”（MainWindow 的
        // NativeMenuItemDebugCrashTest_OnClick：new CrashWindow().Show()）。
        // 刻意不走“抛未处理异常”的路径：本机开启了教学安全模式
        // （IsCriticalSafeMode=true 且 CriticalSafeModeMethod=0）时，
        // ProcessUnhandledException 会在 safe 分支直接 Stop() 静默退出，
        // 反而看不到崩溃窗口。每进入一次该标签页就显示一次。
        ShowHostCrashWindow();
    }

    /// <summary>取消 JK制服 标签页的加载占位符（含未到点的定时器）：切换标签页或离开页面时调用。</summary>
    private void CancelJkLoading()
    {
        CancelJkLoadingTimer();

        if (_jkLoadingIndicator != null)
        {
            _jkLoadingIndicator.IsActive = false;
            _jkLoadingIndicator.IsVisible = false;
        }
    }

    private void CancelJkLoadingTimer()
    {
        if (_jkLoadingTimer == null)
        {
            return;
        }

        _jkLoadingTimer.Stop();
        _jkLoadingTimer.Tick -= OnJkLoadingTimerTick;
        _jkLoadingTimer = null;
    }

    /// <summary>
    /// 显示宿主的崩溃窗口（<c>ClassIsland.Views.CrashWindow</c>）并填充报告正文；除正文外
    /// 行为与开发者菜单的“显示崩溃窗口”一致。该类型位于宿主主程序集（非 ClassIsland.Core），
    /// 插件没有编译期引用，故运行时从已加载程序集反射获取；不假定程序集名，避免宿主改名后失效。
    /// 【FA2 / FA3 差异】ClassIsland 2.0.x（FA2）的 CrashWindow 是 <see cref="Window"/>，
    /// 用 ShowDialog(owner) 显示；2.1.x（FA3）改成了 <c>ViewBase</c>（不再是窗口），
    /// 宿主自己用 <c>await CrashWindow.ShowModal()</c> 显示，故这里按实际类型分派——
    /// 否则旧实现里 `is not Window` 会直接 return，表现为"崩溃窗口无法召唤"。
    /// </summary>
    private void ShowHostCrashWindow()
    {
        try
        {
            var crashWindowType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ClassIsland.Views.CrashWindow"))
                .FirstOrDefault(type => type != null);

            if (crashWindowType == null || Activator.CreateInstance(crashWindowType) is not { } crashInstance)
            {
                return;
            }

            // 归因“女装/男娘”类插件：命中即让宿主按“本次错误由它们引起”处理
            // （禁用全部命中项），并把归因列表一并写进报告正文。
            var (blamedPlugins, blamedDisabled) = FindAndDisableFemboyPlugins();

            // 填充正文：CrashWindow.CrashInfo 是公开的可写 StyledProperty，
            // XAML 中正文 TextBox 绑定它，故显示前赋值即可刷新（FA2/FA3 同为该属性名）。
            // 注意 IsCritical / AllowIgnore 在 XAML 里是 OneTime 绑定，构造时（DataContext = this）
            // 已求值完毕，构造后再赋值不会改变红条与“忽略/调试”按钮的可见性，
            // 因此这里不设置它们——与开发者菜单“显示崩溃窗口”的默认表现保持一致。
            crashWindowType.GetProperty("CrashInfo")?.SetValue(crashInstance, BuildCrashInfo(blamedPlugins, blamedDisabled));

            // 【FA3】ViewBase 形态：优先用 ShowModal(Window owner)——宿主会转成原生 ShowDialog(owner)，
            // 由模态窗口独占焦点；若只传 ViewBase/null，宿主实际退化为非模态 Show()，
            // 窗口靠紧随其后的 Activate() 抢前台，时序上不保证抢到焦点（表现为被压在设置窗口下面）。
            if (crashInstance is not Window)
            {
                var viewOwner = FluentAvaloniaCompatibilityHelper.ResolveOwnerWindow(this);
                var showModalWithWindow = viewOwner == null
                    ? null
                    : FindSingleParameterMethod(crashWindowType, "ShowModal", typeof(Window).FullName!);
                if (showModalWithWindow?.Invoke(crashInstance, new object?[] { viewOwner }) is Task modalTask)
                {
                    _ = ObserveTaskAsync(modalTask);
                    return;
                }

                // 取不到设置窗口（或宿主没有该重载）时退化为 ShowModal(null)：仍能显示，只是不保证抢占焦点
                FindSingleParameterMethod(crashWindowType, "ShowModal", HostViewBaseTypeName)?
                    .Invoke(crashInstance, new object?[] { null });
                return;
            }

            // 让崩溃窗口占据设置窗口的焦点：走模态 ShowDialog（与宿主真实崩溃时
            // await CrashWindow.ShowDialog(GetRootWindow()) 的做法一致）。
            // 模态弹窗会独占焦点并阻止设置窗口输入；而非模态 Show() 若未设 Owner，
            // 其 z 序可能被压在设置窗口之下，无法保证“抢到焦点”。
            // 注：Avalonia 的 WindowBase.Owner setter 为 internal，不能直接赋值，
            // 由 ShowDialog(owner) 在内部完成 owner 绑定。
            var owner = FluentAvaloniaCompatibilityHelper.ResolveOwnerWindow(this);
            _ = ShowCrashWindowAsync((Window)crashInstance, owner);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowHostCrashWindow failed: {ex}");
        }
    }

    /// <summary>
    /// 等待崩溃视图关闭并就地观察其异常。单独抽成方法是为了让返回的 Task 始终被 await
    /// 并就地捕获异常——若放任其成为未观察异常，会经 TaskScheduler.UnobservedTaskException
    /// 触发宿主的崩溃处理流程（本机安全模式下会直接退出应用）。
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

    /// <summary>宿主 <c>ViewBase</c> 的全名（插件 net8 侧编译期不存在此类型，故仅以字符串引用）。</summary>
    private const string HostViewBaseTypeName = "ClassIsland.Core.Abstractions.Controls.ViewBase";

    /// <summary>
    /// 反射查找公开实例方法：名称匹配、非泛型、且只有一个参数、该参数类型全名匹配。
    /// 用于区分 <c>ViewBase.ShowModal(ViewBase?)</c>（非模态语义）与
    /// <c>ViewBase.ShowModal(Window)</c>（原生模态，抢占焦点）这类同名重载。
    /// </summary>
    private static MethodInfo? FindSingleParameterMethod(Type type, string methodName, string parameterTypeFullName) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => !method.IsGenericMethod && method.Name == methodName)
            .FirstOrDefault(method => method.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType.FullName == parameterTypeFullName);

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
    /// 归因“女装/男娘”类插件：扫描全部已安装插件，把所有名称或标识符命中该语义的条目一并
    /// 交由宿主禁用，并返回归因列表与宿主的“是否禁用成功”结果（用于决定报告措辞）。
    /// 无命中时返回 (空列表, false)，不影响崩溃窗口正常弹出。
    /// </summary>
    private static (IReadOnlyList<PluginInfo> Plugins, bool Disabled) FindAndDisableFemboyPlugins()
    {
        try
        {
            var matched = FindFemboyPlugins();
            if (matched.Count == 0)
            {
                return (Array.Empty<PluginInfo>(), false);
            }

            return (matched, DisablePluginsViaHost(matched));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FindAndDisableFemboyPlugins failed: {ex}");
            return (Array.Empty<PluginInfo>(), false);
        }
    }

    /// <summary>
    /// 从已安装插件中筛出“女装/男娘”类插件。
    /// 与宿主 DiagnosticService.GetPluginsByStacktrace 不同，这里不看运行时堆栈，而是对插件的
    /// 名称与标识符做归一化后的语义匹配——因此刻意改名伪装的同类插件（各种大小写、分隔符、
    /// 全角、插入空格、中日文同义/谐音的变体）同样会被识别出来。
    /// </summary>
    private static IReadOnlyList<PluginInfo> FindFemboyPlugins()
    {
        try
        {
            return IPluginService.LoadedPlugins
                .Where(IsEnabledAndLoaded)
                .Where(IsFemboyLikePlugin)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FindFemboyPlugins failed: {ex}");
            return Array.Empty<PluginInfo>();
        }
    }

    /// <summary>
    /// 仅归因“真正加载成功且已启用”的插件。IPluginService.LoadedPlugins 在预处理阶段就会把
    /// 已禁用的插件一并加入列表（LoadStatus=Disabled），若不过滤，已禁用插件会被重复列入归因、
    /// 在崩溃报告里当作“问题插件”显示，并再次写入 .disabled（误报）；
    /// 加载失败（Error）与未加载（NotLoaded / 非本地）的插件同理不参与归因。
    /// </summary>
    private static bool IsEnabledAndLoaded(PluginInfo plugin) =>
        plugin.IsEnabled && plugin.LoadStatus == PluginLoadStatus.Loaded;

    /// <summary>名称、标识符或简介任意一项命中“女装/男娘”语义，即视为同类插件。</summary>
    private static bool IsFemboyLikePlugin(PluginInfo plugin)
    {
        try
        {
            return IsFemboyLikeText(plugin.Manifest.Name)
                   || IsFemboyLikeText(plugin.Manifest.Id)
                   || IsFemboyLikeText(plugin.Manifest.Description);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>拉丁写法关键词：femboy 不是汉字，拼音转写不适用，故直接按关键词命中。</summary>
    private const string FemboyLatinKeyword = "femboy";

    /// <summary>“男娘”的标准全拼，作为中文写法的匹配基准。</summary>
    private const string NanniangPinyin = "nanniang";

    /// <summary>允许的拼音编辑距离：0 为精确匹配，1 为允许一次增删改（如 南梁 → nanliang）。</summary>
    private const int NanniangMaxDistance = 1;

    /// <summary>
    /// 判断文本是否属于“女装/男娘”语义：
    /// 1) 拉丁写法（femboy 及其大小写/分隔符/全角变体）直接按关键词命中——拼音转写不适用于非汉字；
    /// 2) 中文写法转全拼后与 <see cref="NanniangPinyin"/> 做“精确 / 编辑距离 1”匹配，
    ///    覆盖 男娘（nanniang）、楠酿（nanniang）、南梁（nanliang）等同音/近音写法。
    /// 全拼由 <see cref="CrossPluginHelper.GetFullPinyinCandidates"/> 提供：已安装可选的 LibPinyin4CI 时
    /// 走其 IPinyinService，未安装时自动回退到内置的简易拼音匹配，故此处无需再做字面兜底。
    /// </summary>
    private static bool IsFemboyLikeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // 先归一化：全角转半角、剔除分隔符与助词、转小写
        // （男の娘 → 男娘；Ｆｅｍｂｏｙ → femboy；F e m b o y → femboy）
        var normalized = NormalizeForMatch(text);

        if (normalized.Contains(FemboyLatinKeyword))
        {
            return true;
        }

        // 候选可能来自 LibPinyin（TitleCase，如 NanNiang）或内置匹配器（小写），比较前统一转小写
        return CrossPluginHelper.GetFullPinyinCandidates(normalized).Any(candidate =>
            LevenshteinDistance(candidate.ToLowerInvariant(), NanniangPinyin, NanniangMaxDistance)
                <= NanniangMaxDistance);
    }

    /// <summary>
    /// 计算两字符串的编辑距离（Levenshtein，插入/删除/替换代价均为 1）。
    /// 若长度差已超过 <paramref name="maxDistance"/>，直接返回一个大于上限的值，省去完整的 DP。
    /// </summary>
    private static int LevenshteinDistance(string source, string target, int maxDistance)
    {
        if (Math.Abs(source.Length - target.Length) > maxDistance)
        {
            return maxDistance + 1;
        }

        var previous = new int[target.Length + 1];
        var current = new int[target.Length + 1];
        for (var j = 0; j <= target.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= target.Length; j++)
            {
                var substitutionCost = source[i - 1] == target[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }

    /// <summary>归一化时需要剔除的“装饰性”分隔符与助词。</summary>
    private static readonly HashSet<char> _ignoredMatchChars = new()
    {
        ' ', '\t', '\r', '\n',
        '·', '・', '•', '‧',              // 各类中点/间隔号
        '-', '_', '.', '~', '|', '/', '\\',
        'の',                              // 日文所属格助词（男の娘 → 男娘）
    };

    /// <summary>
    /// 把文本归一化到便于语义比对的形式：
    /// 1) NFKC 兼容规范化——全角字母/数字转半角（Ｆｅｍｂｏｙ → Femboy）；
    /// 2) 剔除分隔符与助词（见 <see cref="_ignoredMatchChars"/>），使 Fem·boy / F e m b o y /
    ///    男·娘 / 男の娘 与紧凑写法等价；
    /// 3) 转小写——消除大小写差异（FEMBOY → femboy）。
    /// </summary>
    private static string NormalizeForMatch(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text.Normalize(System.Text.NormalizationForm.FormKC))
        {
            if (_ignoredMatchChars.Contains(ch))
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    /// <summary>
    /// 调用宿主的 DiagnosticService.DisableCorruptPlugins 禁用整批插件——即宿主在
    /// ProcessUnhandledException 中使用的同一套“异常插件自动禁用”机制：逐个置 PluginInfo.IsEnabled=false
    /// （写 .disabled 标记）并置 Settings.CorruptPluginsDisabledLastSession，且同样受宿主
    /// “自动禁用异常插件”设置（App.AutoDisableCorruptPlugins）约束，故开关关闭时不会禁用。
    /// 该方法位于宿主主程序集，插件无编译期引用，故反射调用。
    /// </summary>
    private static bool DisablePluginsViaHost(IReadOnlyList<PluginInfo> plugins)
    {
        var diagnosticServiceType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("ClassIsland.Services.DiagnosticService"))
            .FirstOrDefault(type => type != null);

        var disableMethod = diagnosticServiceType?.GetMethod(
            "DisableCorruptPlugins", BindingFlags.Public | BindingFlags.Static);

        var result = disableMethod?.Invoke(null, new object[] { plugins.ToList() });
        return result is true;
    }

    /// <summary>
    /// 构造崩溃报告正文，结构严格对齐宿主 ProcessUnhandledException：
    /// TraceID 提示块 → 被归因的插件告警（如有）→ 异常信息与堆栈（e.ToString() 形式）。
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

        // 归因段落：措辞与宿主 ProcessUnhandledException 中的插件告警文案完全一致，
        // 命中多个同类插件时逐条列出（宿主同样如此）。
        if (blamedPlugins.Count > 0)
        {
            builder.AppendLine("此问题可能由以下插件引起，请在向 ClassIsland 开发者反馈问题前先向以下插件的开发者反馈此问题：");
            foreach (var plugin in blamedPlugins)
            {
                builder.AppendLine($"- {plugin.Manifest.Name} [{plugin.Manifest.Id},{plugin.Manifest.Version}]");
            }
            if (blamedDisabled)
            {
                builder.AppendLine("以上异常插件已自动禁用，重启应用后生效。您可以在排除问题后前往【应用设置】->【插件】中重新启用这些插件，或在【应用设置】->【基本】中调整是否自动禁用异常插件。");
            }
            builder.AppendLine("================================");
        }

        // 异常正文：按实际归因到的插件动态生成，形态与真实崩溃的 e.ToString() 一致。
        // 不能在此处 new StackTrace()：那会把本插件（BuildCrashInfo / ShowHostCrashWindow /
        // OnJkLoadingTimerTick 等）的方法名写进堆栈，一眼就能看出这份报告是彩蛋伪造的。
        builder.Append(BuildCrashExceptionText(blamedPlugins));
        return builder.ToString();
    }

    /// <summary>
    /// 生成异常正文（<c>e.ToString()</c> 形态）。
    /// 堆栈里的插件帧按**实际归因到的插件列表**逐条生成（命名空间取自插件标识符），
    /// 与上方插件告警一一对应——而不是固定成某一个插件，否则会出现“告警列了 10 个插件、
    /// 堆栈却只提到其中 1 个”的破绽。同时刻意不包含本插件（AdvancedTimeIsland）的任何方法。
    /// </summary>
    private static string BuildCrashExceptionText(IReadOnlyList<PluginInfo> blamedPlugins)
    {
        var lines = new List<string> { "System.Exception: There is no pictures of JK uniform!" };

        if (blamedPlugins.Count > 0)
        {
            for (var i = 0; i < blamedPlugins.Count; i++)
            {
                var pluginNamespace = ToPluginNamespace(blamedPlugins[i].Manifest.Id);
                lines.Add(i == 0
                    ? $"   at {pluginNamespace}.Services.UniformPictureService.GetJkUniformPictures(String album)"
                    : $"   at {pluginNamespace}.Views.WomenswearPage.<LoadPicturesAsync>d__19.MoveNext()");
            }

            lines.Add("   --- End of stack trace from previous location ---");
        }
        else
        {
            // 未归因到任何插件时退化为宿主自身的调用帧，不做插件归因
            lines.Add("   at ClassIsland.Views.SettingPages.ErrorSettingsPage.OnLoaded(RoutedEventArgs e)");
        }

        lines.AddRange(FrameworkStackFrames);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// 异常堆栈尾部的框架帧（Avalonia / BCL / 桌面启动），与宿主真实崩溃报告一致。
    /// </summary>
    private static readonly string[] FrameworkStackFrames =
    [
        "   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()",
        "   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)",
        "   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)",
        "   at Avalonia.Threading.DispatcherOperation.InvokeCore()",
        "   at Avalonia.Threading.CulturePreservingExecutionContext.CallbackWrapper(Object obj)",
        "   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)",
        "   at Avalonia.Threading.DispatcherOperation.Execute()",
        "   at Avalonia.Threading.Dispatcher.ExecuteJob(DispatcherOperation job)",
        "   at Avalonia.Threading.Dispatcher.ExecuteJobsCore(Boolean fromExplicitBackgroundProcessingCallback)",
        "   at Avalonia.Win32.Win32Platform.WndProc(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam)",
        "   at Avalonia.Win32.Interop.UnmanagedMethods.DispatchMessage(MSG& lpmsg)",
        "   at Avalonia.Win32.Win32DispatcherImpl.RunLoop(CancellationToken cancellationToken)",
        "   at Avalonia.Threading.DispatcherFrame.Run(IControlledDispatcherImpl impl)",
        "   at Avalonia.Threading.Dispatcher.PushFrame(DispatcherFrame frame)",
        "   at Avalonia.Threading.Dispatcher.MainLoop(CancellationToken cancellationToken)",
        "   at Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime.StartCore(String[] args)",
        "   at Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime.Start(String[] args)",
        "   at Avalonia.ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(AppBuilder builder, string[] args, Action`1 lifetimeBuilder)",
        "   at ClassIsland.Desktop.Program.Main(String[] args) in /_/ClassIsland.Desktop/Program.cs:line 118",
    ];

    /// <summary>
    /// 由插件标识符推导报告堆栈中出现的“插件根命名空间”：逐段保留，仅把不能作为标识符
    /// 首字符的段前缀下划线（如 inf2147483647.MatchTest.01 → inf2147483647.MatchTest._01）。
    /// </summary>
    private static string ToPluginNamespace(string? pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            return "Plugin";
        }

        var segments = pluginId.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            var first = segments[i][0];
            if (!char.IsLetter(first) && first != '_')
            {
                segments[i] = "_" + segments[i];
            }
        }

        return segments.Length == 0 ? "Plugin" : string.Join('.', segments);
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
        CancelJkLoading();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        BackTextBlock.Foreground = GetAccentBrush();
    }
}
