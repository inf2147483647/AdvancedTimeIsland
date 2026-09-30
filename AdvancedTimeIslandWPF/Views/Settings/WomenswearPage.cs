using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Plugin;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 调试入口“女装”对应的独立隐藏设置页面（不继承 <see cref="HanfuPageTemplate"/>）。
/// 布局：顶部返回链接 + “女装”标题 + “汉服 / JK制服”标签页。
/// - 汉服：直接内嵌 <see cref="EasterEggPage"/>，图片与女装彩蛋页完全一致；
/// - JK制服：内容为“啥都没有”占位（WPF 宿主 1.x 无官方的页面导航出错页，
///   故使用本地占位控件）；每进入一次就直接显示宿主的崩溃窗口，
///   等价于宿主开发者菜单的“显示崩溃窗口”（<c>new CrashWindow().Show()</c>）。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandWomenswear", "女装", SettingsPageCategory.Debug)]
public class WomenswearPage : SettingsPageBase
{
    private TabControl? _tabStrip;
    private ContentControl? _contentControl;
    private EasterEggPage? _hanfuContent;
    private FrameworkElement? _jkContent;
    private TextBlock? _backTextBlock;

    public WomenswearPage()
    {
        InitializeComponent();
        Loaded += (_, _) => ThemeHelper.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => ThemeHelper.ThemeChanged -= OnThemeChanged;
    }

    /// <summary>取主题强调色作为返回链接前景色（与汉服页面模板保持一致）。</summary>
    private static Brush GetAccentBrush()
    {
        var accentColor = Application.Current?.TryFindResource("SystemAccentColor") as Color?;
        if (accentColor.HasValue)
        {
            return new SolidColorBrush(accentColor.Value);
        }

        var accentColor2 = Application.Current?.TryFindResource("AccentColor") as Color?;
        if (accentColor2.HasValue)
        {
            return new SolidColorBrush(accentColor2.Value);
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
            Cursor = Cursors.Hand
        };
        _backTextBlock.MouseLeftButtonDown += (_, _) => FluentAvaloniaCompatibilityHelper.NavigateBack(this);

        var titleTextBlock = new TextBlock
        {
            Text = "女装",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.HotPink,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(16, 8, 16, 0)
        };

        // 汉服标签页：内嵌女装彩蛋页，图片与 EasterEgg 内容完全一致
        _hanfuContent = new EasterEggPage(Plugin.Instance?.Settings);

        // JK制服标签页：WPF 宿主（ClassIsland 1.x）没有官方的“页面导航出错”设置页，
        // 故使用等价的本地占位内容。
        _jkContent = CreateEmptyPlaceholder();

        _tabStrip = new TabControl
        {
            Margin = new Thickness(16, 8, 16, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _tabStrip.Items.Add(new TabItem { Header = "汉服" });
        _tabStrip.Items.Add(new TabItem { Header = "JK制服" });

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
    /// 创建 JK 制服标签页的占位内容：对齐宿主“啥都没有”占位页的观感
    /// （居中一行说明文字，浅色次要文字色）。
    /// </summary>
    private static FrameworkElement CreateEmptyPlaceholder() => new Border
    {
        MinHeight = 200,
        Margin = new Thickness(24),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock
        {
            Text = "啥都没有",
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ThemeHelper.GetSubTextBrush()
        }
    };

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tabStrip == null || _contentControl == null)
        {
            return;
        }

        if (_tabStrip.SelectedIndex == 1)
        {
            // 先展示占位内容，再直接显示宿主的崩溃窗口。
            _contentControl.Content = _jkContent;
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
    /// WPF 宿主（ClassIsland 1.x）的 CrashWindow 派生自 <c>MyWindow</c>（即 <see cref="Window"/>），
    /// 故直接走模态 <c>ShowDialog(owner)</c>。
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

            if (Activator.CreateInstance(crashWindowType) is not Window crashWindow)
            {
                return;
            }

            // 搜索所有“名称或简介命中 Femboy / 男娘 关键词”的已加载插件（含 Fem·boy、男·娘 等
            // 插入分隔符的写法），让宿主按“本次错误由这些插件引起”处理：批量禁用它们，
            // 并把归因信息一并写进报告正文。
            var (blamedPlugins, blamedDisabled) = BlameFemboyRelatedPlugins();
            var crashInfo = BuildCrashInfo(blamedPlugins, blamedDisabled);

            // 填充正文。注意 WPF 宿主的 CrashWindow.CrashInfo 是普通自动属性（无变更通知），
            // XAML 中的正文 TextBox 以 OneWay 绑定它，构造时已求值完毕，构造后再赋值不会刷新，
            // 因此除设置属性（供“复制/反馈问题”按钮使用）外，还需把正文直接写进 TextBox。
            crashWindowType.GetProperty("CrashInfo")?.SetValue(crashWindow, crashInfo);
            SetCrashInfoTextBox(crashWindow, crashInfo);

            // 让崩溃窗口占据设置窗口的焦点：走模态 ShowDialog（与宿主真实崩溃时
            // await CrashWindow.ShowDialog(GetRootWindow()) 的做法一致）。
            // 模态弹窗会独占焦点并阻止设置窗口输入；而非模态 Show() 若未设 Owner，
            // 其 z 序可能被压在设置窗口之下，无法保证“抢到焦点”。
            // 用 BeginInvoke 推迟到本次标签切换事件处理完成之后再弹窗，
            // 避免在 SelectionChanged 处理过程中启动嵌套消息循环。
            var owner = Window.GetWindow(this);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (owner != null && !ReferenceEquals(owner, crashWindow))
                    {
                        // 先绑定 Owner 再走无参 ShowDialog：模态窗口独占焦点且始终位于设置窗口之上，
                        // 与 ShowDialog(owner) 效果一致。
                        crashWindow.Owner = owner;
                    }

                    crashWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ShowHostCrashWindow (deferred) failed: {ex}");
                }
            }), DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowHostCrashWindow failed: {ex}");
        }
    }

    /// <summary>
    /// 反射把正文写入崩溃窗口的 <c>TextBoxCrashInfo</c>（XAML <c>x:Name</c> 生成的实例字段）。
    /// 宿主字段的访问级别可能变化（internal / private），故同时按公有与非公有查找；
    /// 找不到字段时静默跳过（属性已设置，仅观感上少一次刷新）。
    /// </summary>
    private static void SetCrashInfoTextBox(Window crashWindow, string crashInfo)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var field = crashWindow.GetType().GetField("TextBoxCrashInfo", flags);
        if (field?.GetValue(crashWindow) is TextBox textBox)
        {
            textBox.Text = crashInfo;
        }
    }

    /// <summary>
    /// 搜索所有“名称或简介包含 Femboy / 男娘”的已加载插件，让宿主按“错误由这些插件引起”处理：
    /// 调用宿主的 DiagnosticService.DisableCorruptPlugins 批量禁用。
    /// 返回命中的插件列表与宿主的“是否禁用成功”结果（用于决定报告措辞）。
    /// 未命中任何插件时返回 (空列表, false)。
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
    /// （写 .disabled 标记），且同样受宿主“自动禁用异常插件”设置（App.AutoDisableCorruptPlugins）约束，
    /// 故开关关闭时不会禁用。该方法位于宿主主程序集，插件无编译期引用，故反射调用。
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
        var builder = new StringBuilder();

        // 头部提示块：与宿主 ProcessUnhandledException 生成的 traceInfo 结构一致。
        // 宿主此处填 Sentry TraceId；本插件不接入 Sentry，改用插件主程序集
        // （AdvancedTimeIsland.dll）的 MD5 作为等价的“可追溯标识”——同样为 32 位小写十六进制，
        // 便于用户按构建产物对号入座。
        builder.AppendLine("在向开发者提交问题时请保留以下信息：");
        builder.AppendLine($"TraceID: {GetPluginDllMd5()}");
        builder.AppendLine("================================");
        builder.AppendLine();

        builder.AppendLine($"插件版本：AdvancedTimeIsland {GetPluginVersion()}");
        builder.AppendLine($"发生时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine("出错的页面：应用设置->AdvancedTimeIsland调试->女装->JK 制服");

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
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
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

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_backTextBlock != null)
        {
            _backTextBlock.Foreground = GetAccentBrush();
        }
    }
}
