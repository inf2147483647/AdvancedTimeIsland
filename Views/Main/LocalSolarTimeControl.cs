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
    "22334455-6677-8899-0011-223344556677",
    "地方时（ATI）",
    "\uE121",
    "显示指定经度的地方时"
)]
public class LocalSolarTimeControl : ComponentBase<LocalSolarTimeSettings>
{
    private LocalSolarTimeViewModel vm;
    private TextBlock tb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public LocalSolarTimeControl(TimeBaseService tbs)
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
        tb = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush(), Text = "Loading..." };
        rootBorder.Child = tb;
        Content = rootBorder;
    }

    private void UpdateFontColor(string colorStr)
    {
        tb.Foreground = ThemeHelper.ParseColorOrThemeDefault(colorStr);
    }

    private void UpdateFontSize(double fontSize)
    {
        tb.FontSize = fontSize;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        var newColor = ThemeHelper.GetSmartContrastColor(Settings.FontColor);
        Settings.FontColor = newColor;
        UpdateFontColor(newColor);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        vm = new LocalSolarTimeViewModel(_timeBaseService, Settings, UpdateFontColor, UpdateFontSize);
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
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        (vm as IDisposable)?.Dispose();
    }
}
