using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandHanfuGuide", "汉服指南", false, SettingsPageCategory.Debug)]
public class HanfuPointNorthPage : HanfuPageTemplate
{
    private readonly Dictionary<Button, bool> _guideButtons = new Dictionary<Button, bool>();

    protected override void BuildContent(StackPanel panel)
    {
        // 隐藏返回上一页按钮
        if (_backTextBlock != null)
        {
            _backTextBlock.IsVisible = false;
        }
        AddSection(panel, "汉服指南", 24, FontWeight.Bold, ThemeHelper.GetLightBlueBrush());
//        AddParagraph(panel, "给我指到南极点了");

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var guideItems = new[]
        {
            ("汉服百科", true),
            ("选购指南", false),
            ("汉服拍照姿势指南", false),
            ("汉服照片修图指南", false)
        };

        foreach (var (text, isDeveloped) in guideItems)
        {
            var button = new Button
            {
                Content = text,
                FontSize = 16,
                Padding = new Thickness(16, 12),
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            UpdateButtonStyle(button, isDeveloped);
            _guideButtons.Add(button, isDeveloped);

            button.Click += (s, e) =>
            {
                OnGuideButtonClick(text);
            };

            buttonPanel.Children.Add(button);
        }

        panel.Children.Add(buttonPanel);
    }

    private void OnGuideButtonClick(string text)
    {
        if (text == "汉服百科")
        {
            IAppHost.TryGetService<IUriNavigationService>()?
                .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandHanfu?ci_keepHistory=true"));
        }
    }

    private void UpdateButtonStyle(Button button, bool isDeveloped)
    {
        var isDark = ThemeHelper.IsDarkTheme();
        button.Background = isDark
            ? new SolidColorBrush(Color.Parse("#37373D"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
        button.Foreground = isDeveloped
            ? GetAccentBrush()
            : ThemeHelper.GetTextBrush();
        button.BorderBrush = isDark
            ? new SolidColorBrush(Color.Parse("#444444"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));
        button.BorderThickness = new Thickness(1);
    }

    protected override void UpdateThemeColors()
    {
        base.UpdateThemeColors();
        foreach (var (button, isDeveloped) in _guideButtons)
        {
            UpdateButtonStyle(button, isDeveloped);
        }
    }
}
