using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;

using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
// Toast 的 InfoBar 严重级别枚举：FA2 (net8) 叫 InfoBarSeverity，FA3 (net10) 改名 FAInfoBarSeverity，
// 用别名统一，业务代码统一用 FAInfoBarSeverity（同 EasterEggPage）
#if NET10_0_OR_GREATER
using FAInfoBarSeverity = FluentAvalonia.UI.Controls.FAInfoBarSeverity;
#else
using FAInfoBarSeverity = FluentAvalonia.UI.Controls.InfoBarSeverity;
#endif


namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 关于页面
/// </summary>
// 与"时间表悬浮窗"等页同属 AdvancedTimeIsland 导航分组（仿 SystemTools 子页面结构）
[SettingsPageInfo("AdvancedTimeIsland", "主设置")]
[Group("advancedtimeisland.main")]
public class AboutPage : SettingsPageBase
{
    private static SolidColorBrush GetAccentBrush()
    {
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            return new SolidColorBrush(accentColor);
        }
        if (Application.Current?.TryFindResource("AccentColor", out var accentObj) == true && accentObj is Color accentColor2)
        {
            return new SolidColorBrush(accentColor2);
        }
        return new SolidColorBrush(Colors.DodgerBlue);
    }

    private static IBrush GetAccentTextBrush(SolidColorBrush accentBrush)
    {
        var color = accentBrush.Color;
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        return luminance > 0.6 ? Brushes.Black : Brushes.White;
    }

    private EasterEggDetector _easterEggDetector;
    private Border _iconBorder = null!;
    private TabControl? _tabControl;
    private DrawerHost? _drawerHost;
    private bool _easterEggActive;
    private readonly PluginSettings? _pluginSettings;

    /// <summary>集控验证进行中标记：防止验证窗口未关闭时重复进入激活流程。</summary>
    private bool _easterEggActivating;

    /// <summary>
    /// 「管理启用的功能」- 小工具：关闭后主设置导航栏不显示「时间格式转换 / 时间计算器 / 专业名词解释」。
    /// 该选项无需重启，变更后立即重建导航栏标签页。
    /// </summary>
    private bool UtilitiesEnabled => _pluginSettings?.EnableUtilities ?? true;

    private TextBlock? _nameTextBlock;
    private TextBlock? _authorTextBlock;
    private List<TextBlock>? _aboutContentTextBlocks;
    private List<TextBlock>? _infoRowLabelTextBlocks;
    private List<TextBlock>? _infoRowValueTextBlocks;
    private Button? _usingGuideButton;
    private Button? _changelogButton;
    private bool _isLoadingChangelog;

    public AboutPage() : this(null)
    {
    }

    public AboutPage(PluginSettings? pluginSettings = null)
    {
        _pluginSettings = pluginSettings;

        // 女装彩蛋状态不落盘、只存在于本页实例内：页面重建（关闭设置窗口 / 切到别的设置页再回来）
        // 即回到未触发状态，必须重新连点图标触发。宿主默认 SettingsPagesCachePolicy = 0，
        // 因此每次进入本设置页都会创建新实例，这里无需额外清理旧状态。
        _easterEggActive = false;

        _easterEggDetector = new EasterEggDetector(11, 5);
        _easterEggDetector.OnActivated += OnEasterEggActivated;

        InitializeComponent();
        IntroAnimationHelper.EnableSmoothScroll(this);
    }

    private void InitializeComponent()
    {
        // 【管理启用的功能】抽屉宿主必须在这里（创建标签栏之前）就建好：
        // TabControl 添加第一个标签页时会自动选中它并触发 SelectionChanged → LoadTabContent，
        // 其中会把本页的 FeatureDrawerHost 注入 PluginSettingsPage；
        // 若此时宿主还没创建，注入的就是 null（且标签内容不会重建），点击「管理启用的功能...」将毫无反应。
        _drawerHost = new DrawerHost
        {
            DrawerPlacement = DrawerHost.DrawerPlacementEnum.Right
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        if (_pluginSettings?.DisclaimerAccepted != true)
        {
            var disclaimerBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Title", "免责声明");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Message", "插件内包含大量文本输入框，插件作者不对使用者在其中输入的内容做任何担保，如果使用者因输入不当内容导致造成不良影响，使用者需自行承担相关责任，插件作者概不负责。");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "IsClosable", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(disclaimerBar, "Margin", new Thickness(0, 0, 0, 8));
            FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(disclaimerBar, (s, e) =>
            {
                _pluginSettings!.DisclaimerAccepted = true;
            });
            mainPanel.Children.Add(disclaimerBar);
        }

        var iconBorder = CreateIconBorder();
        _iconBorder = iconBorder;
        var headerPanel = CreateHeaderPanel(iconBorder);

        mainPanel.Children.Add(headerPanel);

        _tabControl = CreateNavigationTabs();
        mainPanel.Children.Add(_tabControl);

        var scrollViewer = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = false
        };

        // 抽屉宿主挂在根页面（本页）上，而不是「插件设置」标签页内部：
        // 嵌在标签页里时抽屉只能占据标签内容区的一部分，还会被标签栏与标签内容挤压。
        // 抽屉内容由 PluginSettingsPage 构建后在此挂载（见 LoadTabContent）。
        _drawerHost.Content = scrollViewer;

        Content = _drawerHost;
    }

    /// <summary>
    /// 创建图标边框（带彩蛋检测）
    /// </summary>
    private Border CreateIconBorder()
    {
        var iconBorder = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(8),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var image = new Avalonia.Controls.Image
        {
            Stretch = Stretch.UniformToFill,
            Width = 64,
            Height = 64
        };

        LoadIconImage(image);
        iconBorder.Child = image;

        iconBorder.PointerPressed += OnIconClicked;

        return iconBorder;
    }

    private void LoadIconImage(Avalonia.Controls.Image imageControl)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            // 直接复用 manifest 引用的根目录 icon.png，避免在包内保留重复副本以减小安装包体积
            var fullPath = System.IO.Path.Combine(baseDir, "icon.png");

            if (System.IO.File.Exists(fullPath))
            {
                var bitmap = new Avalonia.Media.Imaging.Bitmap(fullPath);
                imageControl.Source = bitmap;
            }
            else
            {
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var pluginDir = System.IO.Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    fullPath = System.IO.Path.Combine(pluginDir, "icon.png");
                    if (System.IO.File.Exists(fullPath))
                    {
                        var bitmap = new Avalonia.Media.Imaging.Bitmap(fullPath);
                        imageControl.Source = bitmap;
                    }
                }
            }
        }
        catch
        {
            imageControl.Source = null;
        }
    }

    private string GetPluginVersion()
    {
        try
        {
            var version = TryReadVersionFromManifest();
            if (!string.IsNullOrEmpty(version))
            {
                if (_pluginSettings != null && _pluginSettings.CachedVersion != version)
                {
                    _pluginSettings.CachedVersion = version;
                }
                return version;
            }
        }
        catch { }

        if (!string.IsNullOrEmpty(_pluginSettings?.CachedVersion))
        {
            return _pluginSettings!.CachedVersion!;
        }

        return "未知版本";
    }

    private static string? TryReadVersionFromManifest()
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

            if (!System.IO.File.Exists(manifestPath))
                return null;

            var content = System.IO.File.ReadAllText(manifestPath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"^\s*version\s*:\s*(.+?)\s*$", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim().Trim('"', '\'');
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// 图标点击事件
    /// </summary>
    private void OnIconClicked(object? sender, PointerPressedEventArgs e)
    {
        _easterEggDetector.RecordClick();
    }

    /// <summary>
    /// 显示彩蛋触发提示窗口
    /// </summary>
    private void ShowEasterEggDialog()
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "提示");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", new TextBlock
        {
            Text = "触发彩蛋成功~",
            FontSize = 16,
            Foreground = ThemeHelper.GetTextBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());
        FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, TopLevel.GetTopLevel(this));
    }

    /// <summary>
    /// 显示集控验证未通过的提示窗口。
    /// </summary>
    private void ShowEasterEggAuthFailedDialog()
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "提示");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", new TextBlock
        {
            Text = "集控验证未通过，彩蛋未触发。",
            FontSize = 16,
            Foreground = ThemeHelper.GetTextBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());
        FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, TopLevel.GetTopLevel(this));
    }

    /// <summary>女装答题验证的题目：题面 + 可接受答案组（判定前与用户输入一样先规范化）。</summary>
    private sealed record EasterEggQuizQuestion(string Prompt, string[] AcceptableAnswers);

    /// <summary>
    /// 答题验证失败（提交了答案但有错）后的冷却截止时刻（UTC）——冷却期内不能再次触发答题；
    /// 静态字段跨页面实例共享（设置页会被宿主重建）。「取消 / 关闭对话框」不算答错、不触发冷却。
    /// </summary>
    private static DateTime _quizCooldownUntilUtc = DateTime.MinValue;

    /// <summary>
    /// 女装答题验证题库（54 题：27 道数学填空 + 27 道英语单句语法填空）：
    /// 触发时随机不重复抽取 5 题，全部答对才通过。
    /// </summary>
    private static readonly EasterEggQuizQuestion[] EasterEggQuizBank =
    {
        new("已知 f(x)=2x+1，则 f(3)=？", new[] { "7" }),
        new("sin30°=？", new[] { "1/2", "0.5", "二分之一" }),
        new("等差数列首项为 2，公差为 3，则第 5 项为？", new[] { "14" }),
        new("等比数列首项为 1，公比为 2，则第 6 项为？", new[] { "32" }),
        new("不等式 |x|<3 的整数解共有几个？", new[] { "5" }),
        new("向量 a=(1,2)，b=(3,-1)，则 a 与 b 的数量积为？", new[] { "1" }),
        new("直线 y=2x+1 的斜率为？", new[] { "2" }),
        new("圆 x²+y²=9 的半径为？", new[] { "3" }),
        new("抛物线 y²=8x 的焦点到准线的距离为？", new[] { "4" }),
        new("cos60°=？", new[] { "1/2", "0.5", "二分之一" }),
        new("tan45°=？", new[] { "1" }),
        new("虚数单位 i 的平方等于？", new[] { "-1" }),
        new("(1+i)+(2-i)=？", new[] { "3" }),
        new("(x+1)³ 展开式中 x² 项的系数为？", new[] { "3" }),
        new("从 5 人中任选 2 人，选法共有几种？", new[] { "10" }),
        new("抛一枚均匀硬币两次，恰有一次正面的概率为？", new[] { "1/2", "0.5", "二分之一" }),
        new("函数 y=x²-4x+3 的对称轴为 x=？", new[] { "2" }),
        new("函数 f(x)=x³ 在 x=1 处的导数为？", new[] { "3" }),
        new("数列 1,3,5,7,… 的第 10 项为？", new[] { "19" }),
        new("等差数列首项为 1，公差为 2，则前 10 项和为？", new[] { "100" }),
        new("已知 cosθ=1/2，且 θ 为锐角，则 θ=？", new[] { "60°", "60度", "π/3" }),
        new("向量 (3,4) 的模为？", new[] { "5" }),
        new("两点 (0,0) 与 (3,4) 之间的距离为？", new[] { "5" }),
        new("直线 x+y-1=0 与 x 轴交点的横坐标为？", new[] { "1" }),
        new("函数 y=2^x 在 x=0 时的函数值为？", new[] { "1" }),
        new("以 2 为底 8 的对数为？", new[] { "3" }),
        new("袋中有 3 个红球、2 个白球，任取 1 球，取到红球的概率为？", new[] { "3/5", "0.6", "五分之三" }),

        // —— 英语单句语法填空（每空只填一个单词；大小写敏感：句首填空必须大写）——
        new("The book ______ (write) by a famous writer sells well.", new[] { "written" }),
        new("It was not until midnight ______ he came back.", new[] { "that" }),
        new("______ (see) from the top of the hill, the city looks beautiful.", new[] { "Seen" }),
        new("This is the house ______ windows face south.", new[] { "whose" }),
        new("______ he will come tomorrow or not is still unknown.", new[] { "Whether" }),
        new("The harder you work, ______ (good) results you will get.", new[] { "better" }),
        new("She is the only one of the students who ______ (be) often late for class.", new[] { "is" }),
        new("The film ______ (begin) at eight last night.", new[] { "began" }),
        new("______ (face) with the problem, he felt worried.", new[] { "Faced" }),
        new("There is no doubt ______ he will succeed.", new[] { "that" }),
        new("He devoted himself to ______ (help) the poor.", new[] { "helping" }),
        new("The teacher asked us to keep the classroom ______ (clean) and bright.", new[] { "clean" }),
        new("______ (compare) with his brother, he is more outgoing.", new[] { "Compared" }),
        new("The Great Wall ______ (attract) many tourists every year.", new[] { "attracts" }),
        new("He spoke loudly ______ make himself heard.", new[] { "to" }),
        new("Neither he nor I ______ (be) good at swimming now.", new[] { "am" }),
        new("The house ______ (build) last year is very beautiful.", new[] { "built" }),
        new("______ (walk) along the street, I met an old friend.", new[] { "Walking" }),
        new("He is so honest a boy ______ we all like him.", new[] { "that" }),
        new("The more careful you are, the ______ (few) mistakes you will make.", new[] { "fewer" }),
        new("It is important ______ we should learn English well.", new[] { "that" }),
        new("She had her hair ______ (cut) yesterday.", new[] { "cut" }),
        new("______ (give) more time, we could have done it better.", new[] { "Given" }),
        new("The book is worth ______ (read).", new[] { "reading" }),
        new("He was so tired that he fell asleep ______ (immediate).", new[] { "immediately" }),
        new("It was in the park ______ I met him.", new[] { "that" }),
        new("The population of China is larger than ______ of Japan.", new[] { "that" }),
    };

    /// <summary>
    /// 答案规范化：去除所有空白字符（半角 / 全角空格、制表符等）与零宽字符（U+200B/C/D、U+FEFF）。
    /// 大小写不转换——英语题答案**大小写敏感**：位于句首的填空必须大写（如 Seen / Whether / Faced /
    /// Compared / Walking / Given），小写判错；句中词、数学与中文答案不受影响。
    /// </summary>
    private static string NormalizeQuizAnswer(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }
            if (ch is '\u200B' or '\u200C' or '\u200D' or '\uFEFF')
            {
                continue;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static bool IsQuizAnswerCorrect(string? input, EasterEggQuizQuestion question)
    {
        var normalized = NormalizeQuizAnswer(input ?? string.Empty);
        if (normalized.Length == 0)
        {
            return false;
        }

        foreach (var acceptable in question.AcceptableAnswers)
        {
            if (NormalizeQuizAnswer(acceptable) == normalized)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 女装答题验证对话框：从 54 题库随机不重复抽 5 题，全部答对返回 true；有错题时锁定作答、
    /// 指出错题并揭晓正确答案，对话框在 10 × 答错数量 秒后自动关闭（或手动关闭）——两种关闭方式
    /// 均按验证失败处理（返回 false）；「取消」/ 关闭对话框同样按失败处理但不进入答题冷却。
    /// </summary>
    private Task<bool> ShowEasterEggQuizAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return Task.FromResult(false);
        }

        // 随机不重复抽 5 题（洗牌取前 5）
        var questions = EasterEggQuizBank.OrderBy(_ => Random.Shared.Next()).Take(5).ToArray();
        var inputs = new TextBox[questions.Length];
        var promptTexts = new TextBlock[questions.Length];

        var questionPanel = new StackPanel { Spacing = 14 };
        for (var i = 0; i < questions.Length; i++)
        {
            var index = i; // 避免闭包捕获循环变量
            inputs[index] = new TextBox { Watermark = "请输入答案" };
            promptTexts[index] = new TextBlock
            {
                Text = $"第 {index + 1} 题：{questions[index].Prompt}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 15,
                Foreground = ThemeHelper.GetTextBrush()
            };
            questionPanel.Children.Add(promptTexts[index]);
            questionPanel.Children.Add(inputs[index]);
        }

        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new ScrollViewer { MaxHeight = 420, Content = questionPanel });

        var succeeded = false;
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "验证");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", content);
        // 不使用原生底部按钮行：FA ContentDialog 的按钮顺序固定为 [Primary 左 | Close 右]，
        // 无法呈现「取消在左、确认在右」；改为内容区底部自定义按钮行（关闭 = 只设 CloseButtonText 为空，
        // 原生按钮全部隐藏）

        Action succeededHide = () => dialog.GetType().GetMethod("Hide", Type.EmptyTypes)?.Invoke(dialog, null);

        // 提交后的结果提示行（全对时不可见）
        var resultText = new TextBlock
        {
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeHelper.GetTextBrush()
        };

        // 底部按钮行：左侧「取消」（普通样式）、右侧「提交答案」（确认/accent 样式）
        var cancelButton = new Button { Content = "取消", MinWidth = 84 };
        var submitButton = new Button { Content = "提交答案", MinWidth = 84 };
        if (Application.Current?.Styles.TryGetResource("AccentFillColorDefaultBrush", Application.Current.ActualThemeVariant, out var accentBrush) == true && accentBrush is IBrush accentFill)
        {
            submitButton.Background = accentFill;
        }
        if (Application.Current?.Styles.TryGetResource("TextOnAccentFillColorPrimaryBrush", Application.Current.ActualThemeVariant, out var onAccentBrush) == true && onAccentBrush is IBrush onAccentFill)
        {
            submitButton.Foreground = onAccentFill;
        }

        var closed = false;
        Avalonia.Threading.DispatcherTimer? countdownTimer = null;

        // 提交判定：全部答对才通过；有错题则进入 30 秒答题冷却（外层重置连点计数）、锁定作答、
        // 指出错题——已作答的错题揭晓正确答案，未作答的题目只标错、不展示答案；对话框在
        // 10 × 答错数量 秒后自动关闭（或手动关闭）
        submitButton.Click += (_, _) =>
        {
            var wrong = new List<int>();  // 答错且已作答（揭晓答案）
            var blank = new List<int>();  // 未作答（只标错，不展示答案）
            for (var i = 0; i < questions.Length; i++)
            {
                if (IsQuizAnswerCorrect(inputs[i].Text, questions[i]))
                {
                    continue;
                }
                var answered = NormalizeQuizAnswer(inputs[i].Text ?? string.Empty).Length > 0;
                if (answered)
                {
                    wrong.Add(i);
                }
                else
                {
                    blank.Add(i);
                }
            }

            var wrongTotal = wrong.Count + blank.Count;
            if (wrongTotal == 0)
            {
                succeeded = true;
                succeededHide();
                return;
            }

            _quizCooldownUntilUtc = DateTime.UtcNow.AddSeconds(30);

            // 提交后不能修改：锁定全部输入框与提交按钮
            foreach (var input in inputs)
            {
                input.IsEnabled = false;
            }
            submitButton.IsEnabled = false;

            // 指出错题（题面标红 ✗）；仅已作答的错题在其输入框下方揭晓正确答案。
            // 从后往前处理避免子元素位移
            var allWrong = new List<int>(wrong);
            allWrong.AddRange(blank);
            allWrong.Sort((a, b) => b.CompareTo(a));
            foreach (var index in allWrong)
            {
                promptTexts[index].Text = "✗ " + promptTexts[index].Text;
                promptTexts[index].Foreground = Brushes.Red;
                if (!wrong.Contains(index))
                {
                    continue; // 未作答：不展示答案
                }
                var reveal = new TextBlock
                {
                    Text = "正确答案：" + string.Join(" / ", questions[index].AcceptableAnswers),
                    FontSize = 14,
                    Margin = new Thickness(0, -8, 0, 0),
                    Foreground = ThemeHelper.GetTextBrush()
                };
                questionPanel.Children.Insert(index * 2 + 2, reveal);
            }

            // 倒计时：10 × 答错数量 秒后自动关闭
            string BuildResultText(int remain) => blank.Count > 0
                ? $"答错 {wrongTotal} 题（含 {blank.Count} 题未作答，未作答的题目不展示答案），正确答案已标注在对应错题下方；作答已锁定，对话框将在 {remain} 秒后自动关闭（也可手动关闭）"
                : $"答错 {wrongTotal} 题，正确答案已标注在对应题目下方；作答已锁定，对话框将在 {remain} 秒后自动关闭（也可手动关闭）";
            var remaining = wrongTotal * 10;
            resultText.IsVisible = true;
            resultText.Text = BuildResultText(remaining);
            countdownTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            countdownTimer.Tick += (_, _) =>
            {
                if (closed)
                {
                    countdownTimer?.Stop();
                    return;
                }
                remaining--;
                if (remaining <= 0)
                {
                    countdownTimer?.Stop();
                    closed = true;
                    succeededHide();
                    return;
                }
                resultText.Text = BuildResultText(remaining);
            };
            countdownTimer.Start();
        };
        cancelButton.Click += (_, _) =>
        {
            // 取消不算答错：按失败处理（重置连点计数）但不进入答题冷却
            closed = true;
            countdownTimer?.Stop();
            succeededHide();
        };

        var buttonRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetColumn(cancelButton, 1);
        Grid.SetColumn(submitButton, 2);
        buttonRow.Children.Add(cancelButton);
        buttonRow.Children.Add(submitButton);
        content.Children.Add(resultText);
        content.Children.Add(buttonRow);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // 对话框关闭（取消 / 遮罩点击 / 提交后的 Hide）时 ShowAsync 完成——按最终标志收尾并停掉倒计时
        var showTask = FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, topLevel);
        showTask.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                System.Diagnostics.Debug.WriteLine($"[EasterEgg] 答题对话框异常关闭：{t.Exception}");
            }
            // 手动关闭（遮罩 / ESC 等）也要停掉倒计时计时器；Stop 需在 UI 线程调用
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                closed = true;
                countdownTimer?.Stop();
            });
            tcs.TrySetResult(succeeded);
        }, TaskScheduler.Default);

        return tcs.Task;
    }

    /// <summary>
    /// 集控验证：宿主已加入集控且集控下发了凭据（管理员 / 用户凭据）时，必须先通过宿主原生的
    /// 集控凭据校验才能触发彩蛋。
    /// <para>
    /// 这里不直接读取 <c>IManagementService.CredentialConfig</c> 判断"是否设了密码"，而是统一调用
    /// <c>AuthorizeByLevel</c>：宿主实现里"两种凭据都为空"会直接返回 true（不弹窗），有凭据时才弹出
    /// 凭据输入窗口，语义与"集控有密码才需要验证"完全一致，也避免插件依赖宿主的凭据内部结构。
    /// </para>
    /// </summary>
    private static async Task<bool> VerifyManagementCredentialAsync()
    {
        IManagementService? managementService;
        try
        {
            managementService = IAppHost.TryGetService<IManagementService>();
        }
        catch
        {
            managementService = null;
        }

        // 未加入集控（或宿主没有集控服务）时不需要额外验证
        if (managementService is not { IsManagementEnabled: true })
        {
            return true;
        }

        try
        {
            return await managementService.AuthorizeByLevel(AuthorizeLevel.User);
        }
        catch (Exception ex)
        {
            // 集控已启用却无法完成验证（服务异常 / 凭据数据损坏）时按失败处理：
            // 宁可让彩蛋触发不了，也不要让被集控管理的设备绕开管理员设置。
            System.Diagnostics.Debug.WriteLine($"[EasterEgg] 集控验证异常：{ex}");
            return false;
        }
    }

    /// <summary>
    /// 彩蛋激活处理：先做集控验证（加入集控且设置了凭据时必须通过），再点亮「女装」标签页。
    /// 状态只保存在本页实例内、不写回设置，页面重建即失效。
    /// </summary>
    private async void OnEasterEggActivated(object? sender, EventArgs e)
    {
        if (_easterEggActive || _easterEggActivating) return;

        // 触发门槛：仅当「实验性功能」开关在插件启动（应用重启）时已开启才可触发（快照判定，
        // 运行中改开关不影响本次会话）。未开启时静默重置检测器——彩蛋本就是隐藏性质，不做提示。
        if (!Plugin.EasterEggGateEnabled)
        {
            _easterEggDetector.Reset();
            return;
        }

        _easterEggActivating = true;
        try
        {
            // 答题冷却：上次答错后 30 秒内不能再次触发答题（静默重置检测器，
            // 连点计数从头累计；「取消 / 关闭对话框」不算答错、不进入冷却）
            if (DateTime.UtcNow < _quizCooldownUntilUtc)
            {
                _easterEggDetector.Reset();
                return;
            }

            // 答题验证（先于集控验证）：27 题库随机不重复抽 5 题、全部答对才算通过；
            // 取消 / 关闭按失败处理，重置检测器允许重新累计点击后再试
            if (!await ShowEasterEggQuizAsync())
            {
                _easterEggDetector.Reset();
                return;
            }

            if (!await VerifyManagementCredentialAsync())
            {
                // 验证取消 / 失败：重置检测器，允许用户重新累计点击后再试
                _easterEggDetector.Reset();
                ShowEasterEggAuthFailedDialog();
                return;
            }

            _easterEggActive = true;
            SyncNavigationTabs();
            ShowEasterEggDialog();
        }
        finally
        {
            _easterEggActivating = false;
        }
    }

    /// <summary>
    /// 女装开关状态变化处理（设置页里的开关只在彩蛋已触发时可见）。
    /// 该状态同样只作用于本页实例，不写回设置。
    /// </summary>
    private void OnEasterEggToggled(object? sender, bool isEnabled)
    {
        if (isEnabled)
        {
            if (_easterEggActive) return;
            _easterEggActive = true;
            SyncNavigationTabs();
            return;
        }

        // 关闭彩蛋时同步重置检测器，保证再次开启后可重新累计点击触发
        _easterEggDetector?.Reset();

        if (_easterEggActive)
        {
            _easterEggActive = false;
            SyncNavigationTabs();
        }
    }

    /// <summary>
    /// 「小工具」开关状态变化处理：立即增删工具栏标签页，无需重启。
    /// </summary>
    private void OnUtilitiesToggled(object? sender, bool isEnabled)
    {
        SyncNavigationTabs();
    }

    /// <summary>
    /// 只显示右上角"需要重启"按钮，不弹出原生对话框
    /// </summary>
    private void ShowRestartButton()
    {
        try
        {
            // FA2 中设置窗口（SettingsWindowNew）本身就是顶层窗口；
            // FA3/CI2 中 SettingsWindowNew 改成了内嵌的 ContentPage（不再是窗口），
            // TopLevel.GetTopLevel 返回的是主窗口，而主窗口的 ViewModel 没有 IsRequestedRestart，
            // 导致反射静默失败。统一方案：沿视觉树向上查找带 IsRequestedRestart 的 ViewModel。
            foreach (var ancestor in this.GetVisualAncestors())
            {
                var viewModel = ancestor.GetType().GetProperty("ViewModel", BindingFlags.Public | BindingFlags.Instance);
                if (viewModel == null)
                    continue;

                var vm = viewModel.GetValue(ancestor);
                if (vm == null)
                    continue;

                var isRequestedRestartProp = vm.GetType().GetProperty("IsRequestedRestart", BindingFlags.Public | BindingFlags.Instance);
                if (isRequestedRestartProp == null)
                    continue;

                isRequestedRestartProp.SetValue(vm, true);
                return;
            }
        }
        catch
        {
        }
    }

    /// <summary>「女装」标签页的 Tag（内容懒加载，与其它标签页一致）。</summary>
    private const string EasterEggTabTag = "EasterEgg";

    /// <summary>受「小工具」开关控制的三个标签页的 Tag。</summary>
    private static readonly string[] UtilityTabTags = { "TimeConverter", "TimeCalculator", "Glossary" };

    /// <summary>
    /// 按当前开关同步导航栏标签页：只增删受开关控制的标签页（「小工具」三个 + 「女装」一个）。
    /// 【FA3 修复】原实现把整条导航栏 <c>Items.Clear()</c> 后重新添加：触发彩蛋是在点击事件里调用它的，
    ///   此时清空并重建 TabControl 的 Items 会让内容演示器停留在失效状态，表现为
    ///   "首次切到女装标签页一片空白，再切一次才正常"。改为按需增删即不会破坏演示器状态；
    ///   同时新增的标签页一律懒加载（Content = null，选中时才由 LoadTabContent 创建内容），
    ///   与「时间格式转换」等既有标签页走完全相同的加载路径。
    /// </summary>
    private void SyncNavigationTabs()
    {
        if (_tabControl == null) return;

        var tabs = _tabControl.Items.OfType<TabItem>().ToList();

        // ① 「小工具」：控制三个工具标签页（插在「关于」之后，保持 插件设置 / 关于 / 工具… / 女装 的顺序）
        var hasUtilityTabs = tabs.Any(tab => tab.Tag is string tag && UtilityTabTags.Contains(tag));
        if (UtilitiesEnabled && !hasUtilityTabs)
        {
            var aboutIndex = tabs.FindIndex(tab => (tab.Tag as string) == "About");
            var insertIndex = aboutIndex >= 0 ? aboutIndex + 1 : _tabControl.Items.Count;
            foreach (var (header, tag) in new[] { ("时间格式转换", "TimeConverter"), ("时间计算器", "TimeCalculator"), ("专业名词解释", "Glossary") })
            {
                _tabControl.Items.Insert(insertIndex++, new TabItem { Header = header, Content = null, Tag = tag });
            }
        }
        else if (!UtilitiesEnabled && hasUtilityTabs)
        {
            foreach (var tab in tabs.Where(tab => tab.Tag is string tag && UtilityTabTags.Contains(tag)).ToList())
            {
                _tabControl.Items.Remove(tab);
            }
        }

        // ② 「女装」：始终排在最后
        var hasEasterEggTab = tabs.Any(tab => (tab.Tag as string) == EasterEggTabTag);
        if (_easterEggActive && !hasEasterEggTab)
        {
            _tabControl.Items.Add(new TabItem { Header = "女装", Content = null, Tag = EasterEggTabTag });
        }
        else if (!_easterEggActive && hasEasterEggTab)
        {
            foreach (var tab in tabs.Where(tab => (tab.Tag as string) == EasterEggTabTag).ToList())
            {
                _tabControl.Items.Remove(tab);
            }
        }
    }

    /// <summary>
    /// 创建关于内容
    /// </summary>
    private Control CreateAboutContent()
    {
        _aboutContentTextBlocks = new List<TextBlock>();

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 12
        };

        var authorText = new TextBlock
        {
            Text = "作者：inf2147483647",
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _aboutContentTextBlocks.Add(authorText);
        panel.Children.Add(authorText);

        var versionText = new TextBlock
        {
            Text = $"版本：{GetPluginVersion()}",
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _aboutContentTextBlocks.Add(versionText);
        panel.Children.Add(versionText);

        var descText = new TextBlock
        {
            Text = "AdvancedTimeIsland是一款为ClassIsland打造的高级时间管理插件，旨在弥补原生功能的不足（比如判断是否在某段时间范围内）。它提供丰富的时间相关组件，包括高级日期显示、多例倒计时、正计时、周期性倒计时等核心功能。同时支持农历日历、节气、生肖、星座、节日等展示。该插件具备强大的时间自动化能力，支持精确时间、周期性时间（年/月/周/日/时/分）、地方时、区时、农历时间等多种触发条件与规则，满足复杂的时间调度需求。此外还提供时间格式转换工具，支持北京时间、Unix时间戳、农历、区时、地方时之间的相互转换，并附带专业名词解释帮助用户理解时间相关概念。插件界面支持主题自适应，为用户提供更完善的时间管理体验。更多功能开发中，敬请期待。",
            FontSize = 14,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        };
        _aboutContentTextBlocks.Add(descText);
        panel.Children.Add(descText);

        _usingGuideButton = new Button
        {
            Content = "使用指南",
            FontSize = 16,
            Padding = new Thickness(16, 12),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 16, 0, 0)
        };

        UpdateUsingGuideButtonStyle();

        _usingGuideButton.Click += (s, e) =>
        {
            FluentAvaloniaCompatibilityHelper.NavigateToSettingsPage(this, "AdvancedTimeIslandUsingPointer");
        };

        panel.Children.Add(_usingGuideButton);

        // 更新日志：点击后从右侧滑出抽屉，展示 CHANGELOG.md 中当前版本的发布说明
        _changelogButton = new Button
        {
            Content = "更新日志",
            FontSize = 16,
            Padding = new Thickness(16, 12),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
        };
        UpdateChangelogButtonStyle();
        _changelogButton.Click += OnChangelogButtonClick;
        panel.Children.Add(_changelogButton);

        return panel;
    }

    /// <summary>
    /// 「更新日志」按钮点击：先打开抽屉显示加载占位（ExpressiveLoadingIndicator），
    /// 异步加载 CHANGELOG.md 完成后替换为正式内容。
    /// </summary>
    private async void OnChangelogButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_drawerHost == null || _isLoadingChangelog)
        {
            return;
        }

        _isLoadingChangelog = true;
        try
        {
            _drawerHost.DrawerContent = BuildChangelogDrawerLoading();
            _drawerHost.IsDrawerOpen = true;

            var entries = await ChangelogHelper.LoadEntriesAsync();
            if (_drawerHost.IsDrawerOpen)
            {
                _drawerHost.DrawerContent = BuildChangelogDrawerContent(entries);
            }
        }
        catch (Exception ex)
        {
            _lastChangelogError = new ChangelogErrorRecord(DateTime.Now, ex.GetType().Name, ex.Message);
            if (_drawerHost.IsDrawerOpen)
            {
                _drawerHost.DrawerContent = BuildChangelogDrawerMessage("更新日志加载失败", ex.Message);
            }
        }
        finally
        {
            _isLoadingChangelog = false;
        }
    }

    /// <summary>更新日志抽屉宽度（与「管理启用的功能」抽屉一致，DrawerHost 按抽屉内容的 Width + 水平 Margin 决定滑出宽度）。</summary>
    private const double ChangelogDrawerWidth = 500;

    /// <summary>构建抽屉标题行：标题靠左、关闭按钮靠右（加载占位与正式内容共用）。</summary>
    private Grid BuildChangelogDrawerHeader()
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var closeButton = new Button
        {
            Content = "✕",
            FontSize = 14,
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        closeButton.Click += (_, _) =>
        {
            if (_drawerHost != null)
            {
                _drawerHost.IsDrawerOpen = false;
            }
        };
        Grid.SetColumn(closeButton, 1);
        header.Children.Add(closeButton);
        header.Children.Add(new TextBlock
        {
            Text = "更新日志",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });
        return header;
    }

    /// <summary>构建加载占位视图：ExpressiveLoadingIndicator + 提示文本（宽度与正式内容一致，避免抽屉宽度跳动）。</summary>
    private Control BuildChangelogDrawerLoading()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        panel.Children.Add(BuildChangelogDrawerHeader());
        panel.Children.Add(new AdvancedTimeIsland.Views.Controls.ExpressiveLoadingIndicator
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = GetAccentBrush(),
            Margin = new Thickness(0, 48, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "正在加载更新日志…",
            FontSize = 13,
            Foreground = ThemeHelper.GetSubTextBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        });
        return new Border { Width = ChangelogDrawerWidth, Child = panel };
    }

    /// <summary>构建更新日志抽屉：固定宽度根容器 + 标题栏 + 滚动内容区（长日志可滚动浏览）。</summary>
    private Control BuildChangelogDrawerContent(IReadOnlyList<ChangelogHelper.ChangelogEntry> entries)
    {
        var root = new Border { Width = ChangelogDrawerWidth };

        var rootGrid = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        // 标题行（第 0 行）
        rootGrid.Children.Add(BuildChangelogDrawerHeader());

        // 版本卡片列表（第 1 行，剩余高度内滚动）
        var listPanel = new StackPanel { Orientation = Orientation.Vertical };
        if (entries.Count == 0)
        {
            listPanel.Children.Add(new TextBlock
            {
                Text = "暂无更新日志记录。",
                FontSize = 13,
                Foreground = ThemeHelper.GetSubTextBrush(),
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            foreach (var entry in entries)
            {
                listPanel.Children.Add(BuildChangelogEntryCard(entry));
            }
        }

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = listPanel
        };
        Grid.SetRow(scrollViewer, 1);
        rootGrid.Children.Add(scrollViewer);

        root.Child = rootGrid;
        return root;
    }

    /// <summary>最近一次更新日志加载失败的记录（用于错误占位的右键“另存为错误日志”）。</summary>
    private sealed record ChangelogErrorRecord(DateTime Time, string ExceptionType, string Message);

    private ChangelogErrorRecord? _lastChangelogError;

    /// <summary>
    /// 构建加载失败 / 异常占位视图（照搬 EasterEggPage 错误面板模式）：
    /// 标题 + 错误详情文本 + 右键“另存为…”（MenuFlyout 挂 ContextFlyout——FA3 无 ContextMenu 样式），
    /// 把加载失败记录导出为文本日志，成功/失败均通过宿主右下角 Toast 提示。
    /// </summary>
    private Control BuildChangelogDrawerMessage(string title, string message)
    {
        if (_lastChangelogError == null)
        {
            _lastChangelogError = new ChangelogErrorRecord(DateTime.Now, "Exception", message);
        }

        var root = new Border { Width = ChangelogDrawerWidth };
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        panel.Children.Add(BuildChangelogDrawerHeader());

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush(),
            Margin = new Thickness(0, 0, 0, 8)
        });

        var error = _lastChangelogError;
        panel.Children.Add(new TextBlock
        {
            Text = $"时间：{error.Time:yyyy-MM-dd HH:mm:ss}\n错误：{error.ExceptionType}\n{error.Message}",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "提示：在此区域右键可另存为错误日志",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        });

        // 右键“另存为…”错误日志（与 EasterEggPage 错误面板一致：MenuFlyout/ContextFlyout 在 FA2/FA3 行为一致）
        var saveLogMenuItem = new MenuItem { Header = "另存为…" };
        saveLogMenuItem.Click += async (_, _) => await SaveChangelogErrorLogAsync(root);
        var menuFlyout = new MenuFlyout();
        menuFlyout.Items.Add(saveLogMenuItem);
        menuFlyout.Opening += (_, _) => saveLogMenuItem.IsEnabled = _lastChangelogError != null;
        panel.ContextFlyout = menuFlyout;

        root.Child = panel;
        return root;
    }

    /// <summary>
    /// “另存为错误日志”：弹出系统保存文件对话框（默认 .txt），把更新日志加载失败记录
    /// （含环境信息）写入用户选择的位置。对话框 await 异步等待，写盘在后台线程执行；
    /// 成功/失败均通过宿主右下角 Toast 提示。交互照搬 EasterEggPage 的 SaveErrorLogAsAsync。
    /// </summary>
    private async Task SaveChangelogErrorLogAsync(Control anchor)
    {
        string targetPath = string.Empty;
        try
        {
            var topLevel = TopLevel.GetTopLevel(anchor);
            if (topLevel?.StorageProvider == null)
            {
                anchor.ShowToast(new ToastMessage("无法获取宿主窗口的存储服务，请重试。")
                {
                    Title = "错误日志保存失败",
                    Severity = FAInfoBarSeverity.Error,
                    AutoClose = false
                });
                return;
            }

            if (_lastChangelogError == null)
            {
                anchor.ShowToast(new ToastMessage("暂无错误记录。")
                {
                    Title = "错误日志保存失败",
                    Severity = FAInfoBarSeverity.Error,
                    AutoClose = false
                });
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "另存为错误日志",
                SuggestedFileName = $"AdvancedTimeIsland-changelog-error-{DateTime.Now:yyyyMMdd-HHmmss}",
                DefaultExtension = "txt",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("文本文件") { Patterns = new[] { "*.txt" } }
                }
            });
            if (file == null)
            {
                return;
            }

            targetPath = file.Path.LocalPath;
            var content = BuildChangelogErrorLogText();
            await Task.Run(() => File.WriteAllText(targetPath, content));
            anchor.ShowSuccessToast($"错误日志已保存到 {targetPath}");
        }
        catch (Exception ex)
        {
            anchor.ShowToast(new ToastMessage(DescribeSaveFailure(targetPath, ex))
            {
                Title = "错误日志保存失败",
                Severity = FAInfoBarSeverity.Error,
                AutoClose = false
            });
        }
    }

    /// <summary>组装更新日志错误日志文本：环境信息 + 失败记录（时间/异常类型/消息）。结构照搬 EasterEggPage。</summary>
    private string BuildChangelogErrorLogText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("AdvancedTimeIsland 更新日志加载错误日志");
        sb.AppendLine("========================================");
        sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"运行时：{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"操作系统：{System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine("========================================");
        var error = _lastChangelogError;
        sb.AppendLine($"时间：{error?.Time:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"异常类型：{error?.ExceptionType}");
        sb.AppendLine($"消息：{error?.Message}");
        sb.AppendLine("========================================");
        sb.AppendLine("本日志仅包含更新日志（CHANGELOG.md）加载时的错误信息，不含任何个人数据。");
        return sb.ToString();
    }

    /// <summary>
    /// 把保存失败异常翻译为用户可读的具体原因（照搬 EasterEggPage 的 DescribeSaveFailure）：
    /// 磁盘空间不足 / 无写入权限 / 路径无效 / 文件被占用等。
    /// </summary>
    private static string DescribeSaveFailure(string targetPath, Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return "没有写入该位置的权限，请尝试更换保存位置（如文档或桌面）。";
        }

        if (ex is ArgumentException or System.IO.PathTooLongException or NotSupportedException)
        {
            return $"保存路径无效：{ex.Message}";
        }

        if (ex is IOException)
        {
            // IOException 覆盖磁盘满、文件被占用、位置不可达等多种情况；优先探测目标盘剩余空间给出更具体的原因
            try
            {
                var root = Path.GetPathRoot(targetPath);
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    if (drive.IsReady && drive.AvailableFreeSpace < 64L * 1024 * 1024)
                    {
                        return $"目标磁盘 {root} 剩余空间不足（仅剩 {drive.AvailableFreeSpace / (1024 * 1024)} MB），请清理空间后重试。";
                    }
                }
            }
            catch
            {
                // 磁盘探测失败时退回通用 IO 描述
            }

            return $"写入文件时发生 I/O 错误，目标位置可能不可用或文件正被占用：{ex.Message}";
        }

        return ex.Message;
    }

    /// <summary>构建单个版本的卡片：版本号 + 发布日期 + 正文（轻量 Markdown 渲染）。</summary>
    private Border BuildChangelogEntryCard(ChangelogHelper.ChangelogEntry entry)
    {
        var cardPanel = new StackPanel { Orientation = Orientation.Vertical };

        var headGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(0, 0, 0, 4)
        };
        headGrid.Children.Add(new TextBlock
        {
            Text = entry.Version == ChangelogHelper.UnreleasedVersion ? "未发布" : $"v{entry.Version}",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = GetAccentBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });
        var dateText = new TextBlock
        {
            Text = entry.Date,
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(dateText, 1);
        headGrid.Children.Add(dateText);
        cardPanel.Children.Add(headGrid);

        // 正文统一交给汉服模板的 Markdig 渲染（支持表格 / 代码块 / 删除线 / alert 等全部语法），兼容将来任意 CHANGELOG 格式
        var bodyPanel = new StackPanel { Orientation = Orientation.Vertical };
        HanfuPageTemplate.RenderMarkdownToPanel(bodyPanel, entry.RawMarkdown);
        cardPanel.Children.Add(bodyPanel);

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Child = cardPanel
        };
    }

    /// <summary>
    /// 创建顶部通用区域
    /// </summary>
    private Panel CreateHeaderPanel(Border iconBorder)
    {
        var headerPanel = new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var infoPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4
        };

        _nameTextBlock = new TextBlock
        {
            Text = "AdvancedTimeIsland",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };

        var authorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        _authorTextBlock = new TextBlock
        {
            Text = "inf2147483647",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush()
        };

        var projectButton = new Button
        {
            Content = "项目主页",
            FontSize = 12,
            Padding = new Thickness(8, 2),
            Background = Brushes.Transparent,
            Foreground = GetAccentBrush(),
            BorderBrush = GetAccentBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        projectButton.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/inf2147483647/AdvancedTimeIsland",
                    UseShellExecute = true
                });
            }
            catch
            {
                // 忽略打开链接错误
            }
        };

        authorPanel.Children.Add(_authorTextBlock);
        authorPanel.Children.Add(projectButton);

        // 反馈问题按钮
        var feedbackButton = new Button
        {
            Content = "反馈问题",
            FontSize = 12,
            Padding = new Thickness(8, 2),
            Background = Brushes.Transparent,
            Foreground = GetAccentBrush(),
            BorderBrush = GetAccentBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(8, 0, 0, 0)
        };

        feedbackButton.Click += (s, e) =>
        {
            // 反馈问题 → 跳转到 issue_feedback 页面（内含 GitHub / 问卷星双提交渠道）
            FluentAvaloniaCompatibilityHelper.NavigateToSettingsPage(this, "AdvancedTimeIslandIssueFeedback");
        };

        authorPanel.Children.Add(feedbackButton);

        infoPanel.Children.Add(_nameTextBlock);
        infoPanel.Children.Add(authorPanel);

        DockPanel.SetDock(iconBorder, Dock.Left);
        headerPanel.Children.Add(iconBorder);
        headerPanel.Children.Add(infoPanel);

        return headerPanel;
    }

    /// <summary>
    /// 创建信息行
    /// </summary>
    private StackPanel CreateInfoRow(string label, string value)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var labelText = new TextBlock
        {
            Text = $"{label}：",
            FontSize = 13,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _infoRowLabelTextBlocks?.Add(labelText);
        row.Children.Add(labelText);

        var valueText = new TextBlock
        {
            Text = value,
            FontSize = 13,
            Foreground = ThemeHelper.GetSubTextBrush()
        };
        _infoRowValueTextBlocks?.Add(valueText);
        row.Children.Add(valueText);

        return row;
    }

    /// <summary>
    /// 创建标签导航栏
    /// </summary>
    private TabControl CreateNavigationTabs()
    {
        var tabControl = new TabControl
        {
            Background = Brushes.Transparent,
            Margin = new Thickness(0)
        };

        // 标签页切换的过场动画：参考 ClassIsland「档案编辑」导航栏切换所用的翻页效果
        // （旧页滑出淡出、新页滑入淡入），由工具集内的 PageFlipTransition 实现。
        // 之前为规避“动画播一半卡住、切过去整页空白”而直接置空禁用，该问题已由
        // PageFlipTransition 的复位 + 代次守卫解决（见其类注释）；动画级别为“禁用”时仍会置空。
        //   Avalonia 12 给 TabControl 新增了 PageTransition 属性，Avalonia 11 没有此属性（故 FA2 不受影响）。
        IntroAnimationHelper.EnablePageFlip(tabControl);

        tabControl.SelectionChanged += (s, e) =>
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem && tabItem.Content == null)
            {
                LoadTabContent(tabItem);
            }
        };

        tabControl.Items.Add(new TabItem { Header = "插件设置", Content = null, Tag = "PluginSettings" });
        tabControl.Items.Add(new TabItem { Header = "关于", Content = CreateAboutContent(), Tag = "About" });

        // 【管理启用的功能】关闭「小工具」后不显示工具类标签页
        if (UtilitiesEnabled)
        {
            tabControl.Items.Add(new TabItem { Header = "时间格式转换", Content = null, Tag = "TimeConverter" });
            tabControl.Items.Add(new TabItem { Header = "时间计算器", Content = null, Tag = "TimeCalculator" });
            tabControl.Items.Add(new TabItem { Header = "专业名词解释", Content = null, Tag = "Glossary" });
        }

        if (_easterEggActive)
        {
            tabControl.Items.Add(new TabItem { Header = "女装", Content = null, Tag = EasterEggTabTag });
        }

        return tabControl;
    }

    /// <summary>
    /// 加载标签页内容
    /// </summary>
    private void LoadTabContent(TabItem tabItem)
    {
        try
        {
            switch (tabItem.Tag?.ToString())
            {
                case "TimeConverter":
                    tabItem.Content = new TimeConverterPage(_pluginSettings);
                    break;
                case "TimeCalculator":
                    tabItem.Content = new TimeCalculatorPage(_pluginSettings);
                    break;
                case "PluginSettings":
                    var pluginSettings = new PluginSettingsPage(_pluginSettings);
                    pluginSettings.RequestRestartAction = ShowRestartButton;
                    // 抽屉宿主在根页面上：本页只提供取值委托与抽屉内容，宿主在点击时才被取用
                    // （避免本页创建时宿主尚未创建而注入 null）
                    pluginSettings.FeatureDrawerHostProvider = () => _drawerHost;
                    if (_easterEggActive)
                    {
                        pluginSettings.ShowEasterEggSetting();
                    }
                    pluginSettings.EasterEggToggled += OnEasterEggToggled;
                    // 【管理启用的功能】「小工具」变更后立即重建导航栏标签页（无需重启）
                    pluginSettings.UtilitiesToggled += OnUtilitiesToggled;
                    tabItem.Content = pluginSettings;
                    break;
                case "Glossary":
                    tabItem.Content = new GlossaryPage(_pluginSettings);
                    break;
                case "EasterEgg":
                    tabItem.Content = new EasterEggPage(_pluginSettings);
                    break;
                default:
                    tabItem.Content = new TextBlock { Text = "内容加载中...", Foreground = ThemeHelper.GetTextBrush() };
                    break;
            }
        }
        catch (Exception ex)
        {
            tabItem.Content = new TextBlock
            {
                Text = $"加载失败: {ex.Message}",
                Foreground = Brushes.Red,
                TextWrapping = TextWrapping.Wrap
            };
        }
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

        // 女装彩蛋状态不落盘、也不跨页面保留：离开设置页即清空并收起「女装」标签页。
        // 这样即使宿主开启了设置页实例缓存（SettingsPagesCachePolicy >= 1），重新进入设置页
        // 也一定是未触发状态，必须重新连点图标触发。
        ResetEasterEggSessionState();
    }

    /// <summary>
    /// 清空本页实例内的女装彩蛋状态（含点击计数），并在已触发时同步收起「女装」标签页。
    /// </summary>
    private void ResetEasterEggSessionState()
    {
        _easterEggActivating = false;
        _easterEggDetector?.Reset();

        if (!_easterEggActive) return;
        _easterEggActive = false;
        SyncNavigationTabs();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void UpdateThemeColors()
    {
        if (_nameTextBlock != null)
            _nameTextBlock.Foreground = ThemeHelper.GetTextBrush();
        if (_authorTextBlock != null)
            _authorTextBlock.Foreground = ThemeHelper.GetSubTextBrush();

        if (_aboutContentTextBlocks != null)
        {
            for (int i = 0; i < _aboutContentTextBlocks.Count; i++)
            {
                if (i < 2)
                    _aboutContentTextBlocks[i].Foreground = ThemeHelper.GetTextBrush();
                else
                    _aboutContentTextBlocks[i].Foreground = ThemeHelper.GetSubTextBrush();
            }
        }

        if (_infoRowLabelTextBlocks != null)
        {
            foreach (var tb in _infoRowLabelTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetTextBrush();
            }
        }
        if (_infoRowValueTextBlocks != null)
        {
            foreach (var tb in _infoRowValueTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetSubTextBrush();
            }
        }

        UpdateUsingGuideButtonStyle();
        UpdateChangelogButtonStyle();
    }

    private void UpdateChangelogButtonStyle()
    {
        if (_changelogButton == null) return;

        var isDark = ThemeHelper.IsDarkTheme();
        _changelogButton.Background = isDark
            ? new SolidColorBrush(Color.Parse("#37373D"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
        _changelogButton.Foreground = GetAccentBrush();
        _changelogButton.BorderBrush = isDark
            ? new SolidColorBrush(Color.Parse("#444444"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));
        _changelogButton.BorderThickness = new Thickness(1);
    }

    private void UpdateUsingGuideButtonStyle()
    {
        if (_usingGuideButton == null) return;

        var isDark = ThemeHelper.IsDarkTheme();
        _usingGuideButton.Background = isDark
            ? new SolidColorBrush(Color.Parse("#37373D"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
        _usingGuideButton.Foreground = GetAccentBrush();
        _usingGuideButton.BorderBrush = isDark
            ? new SolidColorBrush(Color.Parse("#444444"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));
        _usingGuideButton.BorderThickness = new Thickness(1);
    }
}
