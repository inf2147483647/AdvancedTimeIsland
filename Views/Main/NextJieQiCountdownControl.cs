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

[ComponentInfo("0a67a32c-b772-432a-8c80-8c91ba271893", "下个节气倒计时（ATI）", "\uE121", "显示下个节气倒计时")]
public class NextJieQiCountdownControl : ComponentBase<NextJieQiCountdownSettings>
{
    private NextJieQiCountdownViewModel vm;
    private TextBlock text1Tb;
    private TextBlock nameTb;
    private TextBlock text3Tb;
    private TextBlock timeTb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public NextJieQiCountdownControl(TimeBaseService tbs) { _timeBaseService = tbs; InitializeComponent(); }

    private void InitializeComponent()
    {
        rootBorder = new Border { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        text1Tb = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush() };
        nameTb = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush() };
        text3Tb = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush() };
        timeTb = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush() };
        sp.Children.Add(text1Tb);
        sp.Children.Add(nameTb);
        sp.Children.Add(text3Tb);
        sp.Children.Add(timeTb);
        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateText1FontColor(string colorStr)
    {
        text1Tb.Foreground = ThemeHelper.ParseColorOrThemeDefault(colorStr);
    }

    private void UpdateText1FontSize(double fontSize) { text1Tb.FontSize = fontSize; }
    private void UpdateNameFontColor(string colorStr)
    {
        nameTb.Foreground = ThemeHelper.ParseColorOrThemeDefault(colorStr);
    }

    private void UpdateNameFontSize(double fontSize) { nameTb.FontSize = fontSize; }
    private void UpdateText3FontColor(string colorStr)
    {
        text3Tb.Foreground = ThemeHelper.ParseColorOrThemeDefault(colorStr);
    }

    private void UpdateText3FontSize(double fontSize) { text3Tb.FontSize = fontSize; }
    private void UpdateTimeFontColor(string colorStr)
    {
        timeTb.Foreground = ThemeHelper.ParseColorOrThemeDefault(colorStr);
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
        vm = new NextJieQiCountdownViewModel(_timeBaseService, Settings, UpdateText1FontColor, UpdateText1FontSize, UpdateNameFontColor, UpdateNameFontSize, UpdateText3FontColor, UpdateText3FontSize, UpdateTimeFontColor, UpdateTimeFontSize);
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
        UpdateText1FontSize(Settings.Text1FontSize);
        UpdateNameFontColor(Settings.NameFontColor);
        UpdateNameFontSize(Settings.NameFontSize);
        UpdateText3FontColor(Settings.Text3FontColor);
        UpdateText3FontSize(Settings.Text3FontSize);
        UpdateTimeFontColor(Settings.TimeFontColor);
        UpdateTimeFontSize(Settings.TimeFontSize);
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