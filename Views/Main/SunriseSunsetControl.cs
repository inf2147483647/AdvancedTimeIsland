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
    "33445566-7788-9900-1122-334455667788",
    "日出日落（ATI）",
    "\uE121",
    "显示日出日落时间"
)]
public class SunriseSunsetControl : ComponentBase<SunriseSunsetSettings>
{
    private SunriseSunsetViewModel vm;
    private TextBlock _sunriseLabel;
    private TextBlock _sunriseTime;
    private TextBlock _sunsetLabel;
    private TextBlock _sunsetTime;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;
    private bool _isDisposed;

    public SunriseSunsetControl(TimeBaseService tbs)
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

        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        _sunriseLabel = new TextBlock { Text = "日出：", VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(_sunriseLabel);

        _sunriseTime = new TextBlock { Text = "--:--", VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(_sunriseTime);

        var spacer = new TextBlock { Text = "  ", VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(spacer);

        _sunsetLabel = new TextBlock { Text = "日落：", VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(_sunsetLabel);

        _sunsetTime = new TextBlock { Text = "--:--", VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(_sunsetTime);

        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateFontColor(string colorStr, string elementName)
    {
        var brush = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomColorAndFont);
        switch (elementName)
        {
            case "sunriseLabel":
                _sunriseLabel.Foreground = brush;
                break;
            case "sunriseTime":
                _sunriseTime.Foreground = brush;
                break;
            case "sunsetLabel":
                _sunsetLabel.Foreground = brush;
                break;
            case "sunsetTime":
                _sunsetTime.Foreground = brush;
                break;
        }
    }

    private void UpdateFontSize(double fontSize, string elementName)
    {
        switch (elementName)
        {
            case "sunriseLabel":
                _sunriseLabel.FontSize = fontSize;
                break;
            case "sunriseTime":
                _sunriseTime.FontSize = fontSize;
                break;
            case "sunsetLabel":
                _sunsetLabel.FontSize = fontSize;
                break;
            case "sunsetTime":
                _sunsetTime.FontSize = fontSize;
                break;
        }
    }

    private void UpdateAllFontColors()
    {
        UpdateFontColor(Settings.SunriseLabelFontColor, "sunriseLabel");
        UpdateFontColor(Settings.SunriseTimeFontColor, "sunriseTime");
        UpdateFontColor(Settings.SunsetLabelFontColor, "sunsetLabel");
        UpdateFontColor(Settings.SunsetTimeFontColor, "sunsetTime");
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!Settings.EnableCustomColorAndFont)
        {
            var newColor = ThemeHelper.GetThemeAwareTextColor();
            Settings.SunriseLabelFontColor = newColor;
            Settings.SunriseTimeFontColor = newColor;
            Settings.SunsetLabelFontColor = newColor;
            Settings.SunsetTimeFontColor = newColor;
        }
        UpdateAllFontColors();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        vm = new SunriseSunsetViewModel(
            _timeBaseService,
            Settings,
            UpdateFontColor,
            UpdateFontSize,
            UpdateFontColor,
            UpdateFontSize,
            UpdateFontColor,
            UpdateFontSize,
            UpdateFontColor,
            UpdateFontSize
        );

        DataContext = vm;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.SunriseTime)) _sunriseTime.Text = vm.SunriseTime;
            if (e.PropertyName == nameof(vm.SunsetTime)) _sunsetTime.Text = vm.SunsetTime;
        };

        _sunriseTime.Text = vm.SunriseTime;
        _sunsetTime.Text = vm.SunsetTime;

        UpdateAllFontColors();
        UpdateFontSize(Settings.SunriseLabelFontSize, "sunriseLabel");
        UpdateFontSize(Settings.SunriseTimeFontSize, "sunriseTime");
        UpdateFontSize(Settings.SunsetLabelFontSize, "sunsetLabel");
        UpdateFontSize(Settings.SunsetTimeFontSize, "sunsetTime");
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        (vm as IDisposable)?.Dispose();
        _isDisposed = true;
    }
}
