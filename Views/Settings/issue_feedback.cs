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
/// 反馈问题页面：提供 GitHub Issue 与问卷星两条提交渠道。
/// 由各版本插件设置中"关于"页面的"反馈问题"按钮导航进入
/// （hideDefault=true：不显示在设置导航列表中，仅通过 classisland:// URI 进入）。
/// 样式与交互继承 <see cref="HanfuPageTemplate"/>，随主题深浅自适应。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandIssueFeedback", "反馈问题", true, SettingsPageCategory.Debug)]
public class IssueFeedbackPage : HanfuPageTemplate
{
    private const string GitHubIssueUrl =
        "https://github.com/inf2147483647/AdvancedTimeIsland/issues/new/choose";

    private const string WenJuanXingIssueUrl =
        "https://v.wjx.cn/vm/hrBHauZ.aspx#";

    /// <summary>已创建的按钮，供主题切换时统一刷新配色。</summary>
    private readonly List<Button> _feedbackButtons = new();

    protected override void BuildContent(StackPanel panel)
    {
        // 顶部说明（会被模板统一记录，主题切换时自动重设配色）
        AddParagraph(panel, "在使用过程中遇到问题或有改进建议？请通过以下任一渠道提交 issue，帮助我们做得更好：");

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        AddFeedbackButton(buttonPanel, "GitHub提交issue", GitHubIssueUrl);
        AddFeedbackButton(buttonPanel, "问卷星提交issue", WenJuanXingIssueUrl);

        panel.Children.Add(buttonPanel);
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
