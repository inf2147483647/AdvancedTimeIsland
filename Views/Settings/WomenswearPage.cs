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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Plugin;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 调试入口“女装”对应的独立隐藏设置页面（不继承 HanfuPageTemplate）。
/// 布局：顶部返回链接 + “女装”标题 + “汉服 / JK制服”标签页。
/// - 汉服：直接内嵌 <see cref="EasterEggPage"/>，图片与女装彩蛋页完全一致；
/// - JK制服：内容为官方样式的“啥都没有”（<see cref="Empty"/>）；每进入一次就直接显示宿主的
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

        // JK制服标签页：官方样式的“啥都没有”
        _jkContent = new Empty
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

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tabStrip == null || _contentControl == null)
        {
            return;
        }

        if (_tabStrip.SelectedIndex == 1)
        {
            // 先展示官方样式的“啥都没有”，再直接显示宿主的崩溃窗口。
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
    /// </summary>
    private void ShowHostCrashWindow()
    {
        try
        {
            var crashWindowType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ClassIsland.Views.CrashWindow"))
                .FirstOrDefault(type => type != null);

            if (crashWindowType == null || Activator.CreateInstance(crashWindowType) is not Window crashWindow)
            {
                return;
            }

            // 归因“女装/男娘”类插件：命中即让宿主按“本次错误由它们引起”处理
            // （禁用全部命中项），并把归因列表一并写进报告正文。
            var (blamedPlugins, blamedDisabled) = FindAndDisableFemboyPlugins();

            // 填充正文：CrashWindow.CrashInfo 是公开的可写 StyledProperty，
            // XAML 中正文 TextBox 以 OneWay 绑定它，故构造后赋值即可刷新。
            // 注意 IsCritical / AllowIgnore 在 XAML 里是 OneTime 绑定，构造时（DataContext = this）
            // 已求值完毕，构造后再赋值不会改变红条与“忽略/调试”按钮的可见性，
            // 因此这里不设置它们——与开发者菜单“显示崩溃窗口”的默认表现保持一致。
            crashWindowType.GetProperty("CrashInfo")?.SetValue(crashWindow, BuildCrashInfo(blamedPlugins, blamedDisabled));

            // 让崩溃窗口占据设置窗口的焦点：走模态 ShowDialog（与宿主真实崩溃时
            // await CrashWindow.ShowDialog(GetRootWindow()) 的做法一致）。
            // 模态弹窗会独占焦点并阻止设置窗口输入；而非模态 Show() 若未设 Owner，
            // 其 z 序可能被压在设置窗口之下，无法保证“抢到焦点”。
            // 注：Avalonia 的 WindowBase.Owner setter 为 internal，不能直接赋值，
            // 由 ShowDialog(owner) 在内部完成 owner 绑定。
            var owner = FluentAvaloniaCompatibilityHelper.ResolveOwnerWindow(this);
            _ = ShowCrashWindowAsync(crashWindow, owner);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowHostCrashWindow failed: {ex}");
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
                .Where(IsFemboyLikePlugin)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FindFemboyPlugins failed: {ex}");
            return Array.Empty<PluginInfo>();
        }
    }

    /// <summary>名称或标识符任意一项命中“女装/男娘”语义，即视为同类插件。</summary>
    private static bool IsFemboyLikePlugin(PluginInfo plugin)
    {
        try
        {
            return IsFemboyLikeText(plugin.Manifest.Name) || IsFemboyLikeText(plugin.Manifest.Id);
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

        // 归因段落：措辞对齐宿主 ProcessUnhandledException 中的插件告警文案，
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
