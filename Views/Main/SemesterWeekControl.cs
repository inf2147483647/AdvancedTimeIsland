using System;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.ViewModels.Main;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "99aabbcc-0001-2233-4455-66778899aa02",
    "今日周数（学期）（ATI）",
    PackIconKind.CalendarText,
    "显示当前周所处的学期周数（以ClassIsland学期开始日为第一周第一天）"
)]
public class SemesterWeekControl : ComponentBase<SemesterWeekSettings>
{
    private SemesterWeekViewModel? vm;
    private TextBlock _textBlock = null!;
    private readonly TimeBaseService _timeBaseService;
    private bool _initCompleted;

    public SemesterWeekControl(TimeBaseService tbs)
    {
        _timeBaseService = tbs;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var border = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _textBlock = new TextBlock { Text = "..." };
        border.Child = _textBlock;
        Content = border;
    }

    private void UpdateFontColor(string colorStr)
    {
        _textBlock.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);
    }

    private void UpdateFontSize(double fontSize)
    {
        if (fontSize > 0)
            _textBlock.FontSize = fontSize;
        else
            _textBlock.FontSize = FontFamilyHelper.GetBodyFontSize(_textBlock);
    }

    private void UpdateFontFamily()
    {
        if (Settings.EnableCustomFontFamily)
            _textBlock.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.FontFamily);
        else
            _textBlock.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateFontWeight()
    {
        if (Settings.EnableCustomFontWeight)
            _textBlock.FontWeight = FontFamilyHelper.GetFontWeightFromString(Settings.FontWeight);
        else
            _textBlock.ClearValue(TextBlock.FontWeightProperty);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateFontColor(Settings.FontColor);
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.FontSize : 0);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }
        _initCompleted = true;
        ThemeHelper.ThemeChanged += OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;
        Unloaded += OnUnloaded;
        vm = new SemesterWeekViewModel(_timeBaseService, Settings, UpdateFontColor, UpdateFontSize);
        DataContext = vm;
        _textBlock.Text = vm.DisplayText;
        vm.PropertyChanged += OnVmPropertyChanged;
        UpdateFontColor(Settings.FontColor);
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.FontSize : 0);
        UpdateFontFamily();
        UpdateFontWeight();
        Settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ThemeHelper.ThemeChanged -= OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        if (vm != null)
        {
            vm.PropertyChanged -= OnVmPropertyChanged;
            vm.Dispose();
        }
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.FontFamily) ||
            e.PropertyName == nameof(Settings.EnableCustomFontFamily))
        {
            UpdateFontFamily();
        }
        else if (e.PropertyName == nameof(Settings.FontWeight) ||
                 e.PropertyName == nameof(Settings.EnableCustomFontWeight))
        {
            UpdateFontWeight();
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(vm.DisplayText) && vm != null)
        {
            _textBlock.Text = vm.DisplayText;
        }
    }
}
