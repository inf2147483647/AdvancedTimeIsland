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
        bool enableCustom = elementName switch
        {
            "sunriseLabel" => Settings.SunriseLabelEnableCustomFontColor,
            "sunriseTime" => Settings.SunriseTimeEnableCustomFontColor,
            "sunsetLabel" => Settings.SunsetLabelEnableCustomFontColor,
            "sunsetTime" => Settings.SunsetTimeEnableCustomFontColor,
            _ => false
        };
        var brush = ThemeHelper.GetColorBrush(colorStr, enableCustom);
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
                if (fontSize > 0)
                    _sunriseLabel.FontSize = fontSize;
                else
                    _sunriseLabel.FontSize = FontFamilyHelper.GetBodyFontSize(_sunriseLabel);
                break;
            case "sunriseTime":
                if (fontSize > 0)
                    _sunriseTime.FontSize = fontSize;
                else
                    _sunriseTime.FontSize = FontFamilyHelper.GetBodyFontSize(_sunriseTime);
                break;
            case "sunsetLabel":
                if (fontSize > 0)
                    _sunsetLabel.FontSize = fontSize;
                else
                    _sunsetLabel.FontSize = FontFamilyHelper.GetBodyFontSize(_sunsetLabel);
                break;
            case "sunsetTime":
                if (fontSize > 0)
                    _sunsetTime.FontSize = fontSize;
                else
                    _sunsetTime.FontSize = FontFamilyHelper.GetBodyFontSize(_sunsetTime);
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
        UpdateAllFontColors();
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateFontSize(Settings.SunriseLabelEnableCustomFontSize ? Settings.SunriseLabelFontSize : 0, "sunriseLabel");
        UpdateFontSize(Settings.SunriseTimeEnableCustomFontSize ? Settings.SunriseTimeFontSize : 0, "sunriseTime");
        UpdateFontSize(Settings.SunsetLabelEnableCustomFontSize ? Settings.SunsetLabelFontSize : 0, "sunsetLabel");
        UpdateFontSize(Settings.SunsetTimeEnableCustomFontSize ? Settings.SunsetTimeFontSize : 0, "sunsetTime");
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;

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

        vm.PropertyChanged += OnVmPropertyChanged;

        _sunriseTime.Text = vm.SunriseTime;
        _sunsetTime.Text = vm.SunsetTime;

        UpdateAllFontColors();
        UpdateFontSize(Settings.SunriseLabelEnableCustomFontSize ? Settings.SunriseLabelFontSize : 0, "sunriseLabel");
        UpdateFontSize(Settings.SunriseTimeEnableCustomFontSize ? Settings.SunriseTimeFontSize : 0, "sunriseTime");
        UpdateFontSize(Settings.SunsetLabelEnableCustomFontSize ? Settings.SunsetLabelFontSize : 0, "sunsetLabel");
        UpdateFontSize(Settings.SunsetTimeEnableCustomFontSize ? Settings.SunsetTimeFontSize : 0, "sunsetTime");
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(vm.SunriseTime)) _sunriseTime.Text = vm.SunriseTime;
        if (e.PropertyName == nameof(vm.SunsetTime)) _sunsetTime.Text = vm.SunsetTime;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        (vm as IDisposable)?.Dispose();
        _isDisposed = true;
    }
}
