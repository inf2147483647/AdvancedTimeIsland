using System;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "55667788-9900-1122-3344-556677889900",
    "区时（ATI）",
    PackIconKind.ClockOutline,
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
        tb = new TextBlock { Text = "Loading..." };
        rootBorder.Child = tb;
        Content = rootBorder;
    }

    private void UpdateFontColor(string colorStr)
    {
        tb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);
    }

    private void UpdateFontSize(double fontSize)
    {
        if (fontSize > 0)
            tb.FontSize = fontSize;
        else
            tb.FontSize = FontFamilyHelper.GetBodyFontSize(tb);
    }

    private void UpdateFontFamily(string fontFamily)
    {
        if (string.IsNullOrEmpty(fontFamily))
            tb.ClearValue(TextBlock.FontFamilyProperty);
        else
            tb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(fontFamily);
    }

    private void UpdateFontWeight(string fontWeight)
    {
        if (Settings.EnableCustomFontWeight)
            tb.FontWeight = FontFamilyHelper.GetFontWeightFromString(fontWeight);
        else
            tb.ClearValue(TextBlock.FontWeightProperty);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateFontColor(Settings.FontColor);
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.TextFontSize : 0);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        ThemeHelper.ThemeChanged += OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;
        Unloaded += OnUnloaded;
        vm = new TimeZoneTimeViewModel(_timeBaseService, Settings, UpdateFontColor, UpdateFontSize);
        DataContext = vm;
        tb.Text = vm.FullDisplay;
        vm.PropertyChanged += OnVmPropertyChanged;
        Settings.PropertyChanged += OnSettingsChanged;
        UpdateFontColor(Settings.FontColor);
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.TextFontSize : 0);
        UpdateFontFamily(Settings.EnableCustomFontFamily ? Settings.FontFamily : "");
        UpdateFontWeight(Settings.FontWeight);
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(vm.FullDisplay)) tb.Text = vm.FullDisplay;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ThemeHelper.ThemeChanged -= OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        (vm as IDisposable)?.Dispose();
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.FontFamily) || e.PropertyName == nameof(Settings.EnableCustomFontFamily))
        {
            UpdateFontFamily(Settings.EnableCustomFontFamily ? Settings.FontFamily : "");
        }
        else if (e.PropertyName == nameof(Settings.FontWeight) || e.PropertyName == nameof(Settings.EnableCustomFontWeight))
        {
            UpdateFontWeight(Settings.FontWeight);
        }
    }
}
