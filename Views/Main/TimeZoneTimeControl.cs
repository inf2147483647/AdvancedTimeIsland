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

[ComponentInfo(
    "55667788-9900-1122-3344-556677889900",
    "区时（ATI）",
    "\uE121",
    "显示指定时区的区时"
)]
public class TimeZoneTimeControl : ComponentBase<TimeZoneTimeSettings>
{
    private TimeZoneTimeViewModel vm;
    private TextBlock tb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public TimeZoneTimeControl(TimeBaseService tbs)
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
        tb = new TextBlock { FontSize = 14, Foreground = Brushes.White, Text = "Loading..." };
        rootBorder.Child = tb;
        Content = rootBorder;
    }

    private void UpdateFontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && colorStr.Length == 7)
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                tb.Foreground = new SolidColorBrush(color);
            }
        }
        catch
        {
            tb.Foreground = Brushes.White;
        }
    }

    private void UpdateFontSize(double fontSize)
    {
        tb.FontSize = fontSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        vm = new TimeZoneTimeViewModel(_timeBaseService, Settings, UpdateFontColor, UpdateFontSize);
        DataContext = vm;
        tb.Text = vm.FullDisplay;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.FullDisplay)) tb.Text = vm.FullDisplay;
        };
        UpdateFontColor(Settings.FontColor);
        UpdateFontSize(Settings.TextFontSize);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        (vm as IDisposable)?.Dispose();
    }
}



