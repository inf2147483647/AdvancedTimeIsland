using System;
using System.Collections.Generic;
using System.Diagnostics;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 反馈问题页面：提供 GitHub Issue 与 GitCode 两条提交渠道。
/// 由各版本插件设置中"插件设置 → 通用设置"的"反馈问题"项导航进入
/// （hideDefault=true：不显示在设置导航列表中，仅通过 classisland:// URI 进入）。
/// 样式与交互继承 <see cref="HanfuPageTemplate"/>，随主题深浅自适应。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandIssueFeedback", "反馈问题", true, SettingsPageCategory.Debug)]
public class IssueFeedbackPage : HanfuPageTemplate
{
    private const string GitHubIssueUrl =
        "https://github.com/inf2147483647/AdvancedTimeIsland/issues/new/choose";

    private const string GitCodeIssueUrl =
        "https://gitcode.com/inf2147483647/AdvancedTimeIsland/issues/create?type=template&title=Bug+%E5%8F%8D%E9%A6%88&template=.github%252FISSUE_TEMPLATE%252FBugReport.yml";

    /// <summary>已创建的按钮，供主题切换时统一刷新配色。</summary>
    private readonly List<Button> _feedbackButtons = new();

    /// <summary>「开发节奏说明」InfoBar 是否已在本进程内被关闭（关闭后不再重复弹出）。</summary>
    private static bool _devPaceNoticeDismissed;

    protected override void BuildContent(StackPanel panel)
    {
        AddDevPaceNotice(panel);

        // 顶部说明（会被模板统一记录，主题切换时自动重设配色）
        AddParagraph(panel, "在使用过程中遇到问题或有改进建议？请通过以下任一渠道提交 issue，帮助我们做得更好：");

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        AddFeedbackButton(buttonPanel, "GitHub提交issue", GitHubIssueUrl);
        AddFeedbackButton(buttonPanel, "GitCode提交issue", GitCodeIssueUrl);

        panel.Children.Add(buttonPanel);
    }

    /// <summary>
    /// 顶部公告：以 FA InfoBar（Informational 级别、可关闭）呈现当前开发节奏，
    /// 配色与关闭按钮由 FluentAvalonia 官方控件提供，随主题深浅自适应。
    /// </summary>
    private static void AddDevPaceNotice(StackPanel panel)
    {
        if (_devPaceNoticeDismissed)
        {
            return;
        }

        var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity",
            FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message",
            "当前开发节奏以 UI/UX 优化为主，欢迎 issue 反馈。");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Margin", new Thickness(0, 0, 0, 12));
        FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(infoBar, (_, _) => _devPaceNoticeDismissed = true);

        panel.Children.Add(infoBar);
    }

    private void AddFeedbackButton(StackPanel panel, string text, string url)
    {
        var button = new Button
        {
            Content = text,
            FontSize = 16,
            Padding = new Thickness(16, 12),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        UpdateButtonStyle(button);
        _feedbackButtons.Add(button);

        button.Click += (_, _) => OpenUrl(url);

        panel.Children.Add(button);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // 忽略打开链接错误
        }
    }

    private void UpdateButtonStyle(Button button)
    {
        var isDark = ThemeHelper.IsDarkTheme();
        button.Background = isDark
            ? new SolidColorBrush(Color.Parse("#37373D"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
        // 已开发（可用）状态：文字用主题强调色
        button.Foreground = GetAccentBrush();
        button.BorderBrush = isDark
            ? new SolidColorBrush(Color.Parse("#444444"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));
        button.BorderThickness = new Thickness(1);
    }

    protected override void UpdateThemeColors()
    {
        base.UpdateThemeColors();
        foreach (var button in _feedbackButtons)
        {
            UpdateButtonStyle(button);
        }
    }
}
