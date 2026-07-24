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

[ComponentInfo("01245b89-eed1-4e7c-9f55-b111f57c263f", "下个星座倒计时（ATI）", "\uE122", "显示下个星座倒计时")]
public class NextXingZuoCountdownControl : ComponentBase<NextXingZuoCountdownSettings>
{
    private NextXingZuoCountdownViewModel vm;
    private TextBlock text1Tb;
    private TextBlock nameTb;
    private TextBlock text3Tb;
    private TextBlock timeTb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public NextXingZuoCountdownControl(TimeBaseService tbs) { _timeBaseService = tbs; InitializeComponent(); }

    private void InitializeComponent()
    {
        rootBorder = new Border { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        text1Tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        nameTb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        text3Tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        timeTb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(text1Tb);
        sp.Children.Add(nameTb);
        sp.Children.Add(text3Tb);
        sp.Children.Add(timeTb);
        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateText1FontColor(string colorStr)
    {
        text1Tb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.Text1EnableCustomFontColor);
    }

    private void UpdateText1FontSize(double fontSize) { text1Tb.FontSize = fontSize; }
    private void UpdateNameFontColor(string colorStr)
    {
        nameTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.NameEnableCustomFontColor);
    }

    private void UpdateNameFontSize(double fontSize) { nameTb.FontSize = fontSize; }
    private void UpdateText3FontColor(string colorStr)
    {
        text3Tb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.Text3EnableCustomFontColor);
    }

    private void UpdateText3FontSize(double fontSize) { text3Tb.FontSize = fontSize; }
    private void UpdateTimeFontColor(string colorStr)
    {
        timeTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.TimeEnableCustomFontColor);
    }

    private void UpdateTimeFontSize(double fontSize) { timeTb.FontSize = fontSize; }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateText1FontColor(Settings.Text1FontColor);
        UpdateNameFontColor(Settings.NameFontColor);
        UpdateText3FontColor(Settings.Text3FontColor);
        UpdateTimeFontColor(Settings.TimeFontColor);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        vm = new NextXingZuoCountdownViewModel(_timeBaseService, Settings, UpdateText1FontColor, UpdateText1FontSize, UpdateNameFontColor, UpdateNameFontSize, UpdateText3FontColor, UpdateText3FontSize, UpdateTimeFontColor, UpdateTimeFontSize);
        DataContext = vm;
        text1Tb.Text = vm.Text1Display;
        nameTb.Text = vm.NameDisplay;
        text3Tb.Text = vm.Text3Display;
        timeTb.Text = vm.TimeDisplay;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.Text1Display)) text1Tb.Text = vm.Text1Display;
            if (e.PropertyName == nameof(vm.NameDisplay)) nameTb.Text = vm.NameDisplay;
            if (e.PropertyName == nameof(vm.Text3Display)) text3Tb.Text = vm.Text3Display;
            if (e.PropertyName == nameof(vm.TimeDisplay)) timeTb.Text = vm.TimeDisplay;
        };
        UpdateText1FontColor(Settings.Text1FontColor);
        UpdateText1FontSize(Settings.Text1EnableCustomFontSize ? Settings.Text1FontSize : 14);
        UpdateNameFontColor(Settings.NameFontColor);
        UpdateNameFontSize(Settings.NameEnableCustomFontSize ? Settings.NameFontSize : 14);
        UpdateText3FontColor(Settings.Text3FontColor);
        UpdateText3FontSize(Settings.Text3EnableCustomFontSize ? Settings.Text3FontSize : 14);
        UpdateTimeFontColor(Settings.TimeFontColor);
        UpdateTimeFontSize(Settings.TimeEnableCustomFontSize ? Settings.TimeFontSize : 14);
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