using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 「首次安装」欢迎提示：用宿主右下角的非模态 Toast 展示，标题 + 正文 + 「查看使用指南 / 不用了」操作按钮。
/// </summary>
public static class WelcomeGuideHelper
{
    /// <summary>「使用指南」设置页 Id（与 Using_pointer.cs 的 SettingsPageInfo.Id 保持一致）。</summary>
    public const string UsingGuidePageId = "AdvancedTimeIslandUsingPointer";

    /// <summary>
    /// 展示欢迎提示（非模态，可手动关闭；不会自动消失）。
    /// </summary>
    /// <param name="anchor">用于定位宿主窗口的控件，通常传当前设置页自身。</param>
    public static void ShowWelcomeToast(Control anchor)
    {
        ToastMessage? toast = null;

        var guideButton = new Button
        {
            Content = "查看使用指南",
            Padding = new Thickness(12, 6, 12, 6)
        };
        guideButton.Click += (_, _) =>
        {
            toast?.Close();
            FluentAvaloniaCompatibilityHelper.NavigateToSettingsPage(anchor, UsingGuidePageId);
        };

        var dismissButton = new Button
        {
            Content = "不用了",
            Padding = new Thickness(12, 6, 12, 6)
        };
        dismissButton.Click += (_, _) => toast?.Close();

        var actionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { guideButton, dismissButton }
        };

        toast = new ToastMessage("欢迎使用AdvancedTimeIsland！", "首次使用插件，是否查看使用指南？")
        {
            // 不自动关闭：让用户自行选择查看指南或点关闭，避免提示一闪而过
            AutoClose = false,
            ActionContent = actionPanel
        };

        anchor.ShowToast(toast);
    }
}
