using System;
using System.ComponentModel;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Views.Controls;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "3f8a6d21-74c5-4b9e-8a13-0c62d5e97ab4",
    "七段数码管时钟（ATI）",
    "\uE121",
    "以七段数码管（三横四纵）样式显示当前时间"
)]
public class SevenSegmentClockControl : ComponentBase<SevenSegmentClockSettings>
{
    private readonly TimeBaseService _timeBaseService;
    private SevenSegmentDisplay _display;
    private SevenSegmentClockViewModel? _vm;
    private bool _initCompleted;

    public SevenSegmentClockControl(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _display = new SevenSegmentDisplay
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Content = _display;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted)
        {
            return;
        }

        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }

        _initCompleted = true;
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;

        _vm = new SevenSegmentClockViewModel(_timeBaseService, Settings);
        _vm.PropertyChanged += OnVmPropertyChanged;
        DataContext = _vm;

        Settings.PropertyChanged += OnSettingsChanged;

        ApplyAppearance();
        ApplyDisplay();
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    /// <summary>
    /// 应用强调色、倾斜模式、缩放与随正文字号自适应的数码管尺寸。
    /// </summary>
    private void ApplyAppearance()
    {
        _display.AccentColor = ParseAccentColor(Settings.AccentColor);
        _display.SkewEnabled = Settings.SkewMode;
        _display.Zoom = Settings.Zoom;
        _display.TransitionAnimationEnabled = Settings.EnableTransitionAnimation;
        _display.BaseFontSize = FontFamilyHelper.GetBodyFontSize(_display);
    }

    private void ApplyDisplay()
    {
        if (_vm == null)
        {
            return;
        }

        _display.Text = _vm.DisplayText;
        _display.SeparatorLit = _vm.SeparatorLit;
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        ApplyAppearance();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SevenSegmentClockSettings.AccentColor):
            case nameof(SevenSegmentClockSettings.SkewMode):
            case nameof(SevenSegmentClockSettings.Zoom):
            case nameof(SevenSegmentClockSettings.EnableTransitionAnimation):
                ApplyAppearance();
                break;
            case nameof(SevenSegmentClockSettings.ShowSeconds):
            case nameof(SevenSegmentClockSettings.SeparatorBlink):
                ApplyDisplay();
                break;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyDisplay();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;

        if (_vm != null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        (_vm as IDisposable)?.Dispose();
        _vm = null;
    }

    private static Color ParseAccentColor(string colorStr)
    {
        try
        {
            return Color.Parse(colorStr);
        }
        catch
        {
            return Color.Parse(SevenSegmentClockSettings.DefaultAccentColor);
        }
    }
}
