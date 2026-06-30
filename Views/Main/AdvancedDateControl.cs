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
    "11223344-5566-7788-9900-112233445566",
    "高级日期（ATI）",
    "\uE121",
    "显示精确日期"
)]
public class AdvancedDateControl : ComponentBase<AdvancedDateSettings>
{
    private AdvancedDateViewModel vm;
    private TextBlock tb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public AdvancedDateControl(TimeBaseService tbs)
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
        vm = new AdvancedDateViewModel(_timeBaseService, Settings, UpdateFontColor, UpdateFontSize);
        DataContext = vm;
        tb.Text = vm.DateDisplay;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.DateDisplay)) tb.Text = vm.DateDisplay;
        };
        UpdateFontColor(Settings.FontColor);
        UpdateFontSize(Settings.DateFontSize);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        (vm as IDisposable)?.Dispose();
    }
}



