using System;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "66778899-0011-2233-4455-667788990011",
    "正向计时器（ATI）",
    "\uE122",
    "显示从开始时间到现在已过的时间"
)]
public class ForwardTimerControl : ComponentBase<ForwardTimerSettings>
{
    private ForwardTimerViewModel vm;
    private TextBlock tbText1;
    private TextBlock tbName;
    private TextBlock tbText3;
    private TextBlock tbTime;
    private TextBlock tbText4;
    private Border rootBorder;
    private StackPanel contentPanel;
    private readonly TimeBaseService _timeBaseService;

    public ForwardTimerControl(TimeBaseService tbs)
    {
        _timeBaseService = tbs;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        rootBorder = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        tbText1 = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };
        tbName = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };
        tbText3 = new TextBlock { Text = "已过", VerticalAlignment = VerticalAlignment.Center };
        tbTime = new TextBlock { Text = "Loading...", VerticalAlignment = VerticalAlignment.Center };
        tbText4 = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };

        contentPanel.Children.Add(tbText1);
        contentPanel.Children.Add(tbName);
        contentPanel.Children.Add(tbText3);
        contentPanel.Children.Add(tbTime);
        contentPanel.Children.Add(tbText4);

        rootBorder.Child = contentPanel;
        Content = rootBorder;
    }

    private void UpdateText1Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText1, colorStr, fontSize);
    }

    private void UpdateNameStyle(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbName, colorStr, fontSize);
    }

    private void UpdateText3Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText3, colorStr, fontSize);
    }

    private void UpdateTimeStyle(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbTime, colorStr, fontSize);
    }

    private void UpdateText4Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText4, colorStr, fontSize);
    }

    private void UpdateTextBlockStyle(TextBlock tb, string colorStr, double fontSize)
    {
        tb.FontSize = fontSize;
        tb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!Settings.EnableCustomFontColor)
        {
            var newColor = ThemeHelper.GetThemeAwareTextColor();
            Settings.Text1FontColor = newColor;
            Settings.NameFontColor = newColor;
            Settings.Text3FontColor = newColor;
            Settings.TimeFontColor = newColor;
            Settings.Text4FontColor = newColor;
        }
        UpdateText1Style(Settings.Text1FontColor, Settings.Text1FontSize);
        UpdateNameStyle(Settings.NameFontColor, Settings.NameFontSize);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3FontSize);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeFontSize);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4FontSize);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        vm = new ForwardTimerViewModel(_timeBaseService, Settings,
            UpdateText1Style, UpdateNameStyle, UpdateText3Style, UpdateTimeStyle, UpdateText4Style);
        DataContext = vm;

        vm.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(vm.Text1Display):
                    tbText1.Text = vm.Text1Display;
                    break;
                case nameof(vm.NameDisplay):
                    tbName.Text = vm.NameDisplay;
                    break;
                case nameof(vm.Text3Display):
                    tbText3.Text = vm.Text3Display;
                    break;
                case nameof(vm.TimeDisplay):
                    tbTime.Text = vm.TimeDisplay;
                    break;
                case nameof(vm.Text4Display):
                    tbText4.Text = vm.Text4Display;
                    break;
                case nameof(vm.IsNotStarted):
                    if (vm.IsNotStarted)
                    {
                        tbText1.Text = "";
                        tbName.Text = "";
                        tbText3.Text = "正向计时器未开始";
                        tbTime.Text = "";
                        tbText4.Text = "";
                    }
                    break;
            }
        };

        UpdateText1Style(Settings.Text1FontColor, Settings.Text1FontSize);
        UpdateNameStyle(Settings.NameFontColor, Settings.NameFontSize);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3FontSize);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeFontSize);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4FontSize);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        (vm as IDisposable)?.Dispose();
    }
}


