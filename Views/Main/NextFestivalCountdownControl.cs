using System;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Models;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo("d913f8dd-4bb3-415f-9a5c-4f5a976c167f", "下个节日倒计时（ATI）", "\uE123", "显示下个节日倒计时")]
public class NextFestivalCountdownControl : ComponentBase<NextFestivalCountdownSettings>
{
    private NextFestivalCountdownViewModel vm;
    private TextBlock text1Tb;
    private TextBlock nameTb;
    private TextBlock text3Tb;
    private TextBlock timeTb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public NextFestivalCountdownControl(TimeBaseService tbs) { _timeBaseService = tbs; InitializeComponent(); }

    private void InitializeComponent()
    {
        rootBorder = new Border { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        text1Tb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        nameTb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        text3Tb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        timeTb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        sp.Children.Add(text1Tb);
        sp.Children.Add(nameTb);
        sp.Children.Add(text3Tb);
        sp.Children.Add(timeTb);
        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateText1FontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                text1Tb.Foreground = new SolidColorBrush(color);
            }
        }
        catch { text1Tb.Foreground = Brushes.White; }
    }

    private void UpdateText1FontSize(double fontSize) { text1Tb.FontSize = fontSize; }
    private void UpdateNameFontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                nameTb.Foreground = new SolidColorBrush(color);
            }
        }
        catch { nameTb.Foreground = Brushes.White; }
    }

    private void UpdateNameFontSize(double fontSize) { nameTb.FontSize = fontSize; }
    private void UpdateText3FontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                text3Tb.Foreground = new SolidColorBrush(color);
            }
        }
        catch { text3Tb.Foreground = Brushes.White; }
    }

    private void UpdateText3FontSize(double fontSize) { text3Tb.FontSize = fontSize; }
    private void UpdateTimeFontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                timeTb.Foreground = new SolidColorBrush(color);
            }
        }
        catch { timeTb.Foreground = Brushes.White; }
    }

    private void UpdateTimeFontSize(double fontSize) { timeTb.FontSize = fontSize; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        vm = new NextFestivalCountdownViewModel(_timeBaseService, Settings, UpdateText1FontColor, UpdateText1FontSize, UpdateNameFontColor, UpdateNameFontSize, UpdateText3FontColor, UpdateText3FontSize, UpdateTimeFontColor, UpdateTimeFontSize);
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
        (vm as IDisposable)?.Dispose();
    }
}