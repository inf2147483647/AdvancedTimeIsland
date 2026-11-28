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
    "11223344-5566-7788-9900-112233445566",
    "日期显示（ATI）",
    PackIconKind.Calendar,
    "显示精确日期"
)]
public class AdvancedDateControl : ComponentBase<AdvancedDateSettings>
{
    private AdvancedDateViewModel vm;
    private TextBlock _dateTextBlock;
    private TextBlock _weekDayTextBlock;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;
    private bool _isDisposed;
    private bool _initCompleted;

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
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        _dateTextBlock = new TextBlock { Text = "Loading..." };
        _weekDayTextBlock = new TextBlock { Text = "" };
        sp.Children.Add(_dateTextBlock);
        sp.Children.Add(_weekDayTextBlock);
        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateDateFontColor(string colorStr)
    {
        _dateTextBlock.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);
    }

    private void UpdateWeekDayFontColor(string colorStr)
    {
        _weekDayTextBlock.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.WeekDayEnableCustomFontColor);
    }

    private void UpdateDateFontSize(double fontSize)
    {
        if (fontSize > 0)
            _dateTextBlock.FontSize = fontSize;
        else
            _dateTextBlock.FontSize = FontFamilyHelper.GetBodyFontSize(_dateTextBlock);
    }

    private void UpdateWeekDayFontSize(double fontSize)
    {
        if (fontSize > 0)
            _weekDayTextBlock.FontSize = fontSize;
        else
            _weekDayTextBlock.FontSize = FontFamilyHelper.GetBodyFontSize(_weekDayTextBlock);
    }

    private void UpdateDateFontFamily()
    {
        if (Settings.EnableCustomFontFamily)
            _dateTextBlock.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.FontFamily);
        else
            _dateTextBlock.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateWeekDayFontFamily()
    {
        if (Settings.WeekDayEnableCustomFontFamily)
            _weekDayTextBlock.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.WeekDayFontFamily);
        else
            _weekDayTextBlock.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateDateFontWeight()
    {
        if (Settings.EnableCustomFontWeight)
            _dateTextBlock.FontWeight = FontFamilyHelper.GetFontWeightFromString(Settings.FontWeight);
        else
            _dateTextBlock.ClearValue(TextBlock.FontWeightProperty);
    }

    private void UpdateWeekDayFontWeight()
    {
        if (Settings.WeekDayEnableCustomFontWeight)
            _weekDayTextBlock.FontWeight = FontFamilyHelper.GetFontWeightFromString(Settings.WeekDayFontWeight);
        else
            _weekDayTextBlock.ClearValue(TextBlock.FontWeightProperty);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateDateFontColor(Settings.FontColor);
        UpdateWeekDayFontColor(Settings.WeekDayFontColor);
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateDateFontSize(Settings.EnableCustomFontSize ? Settings.DateFontSize : 0);
        UpdateWeekDayFontSize(Settings.WeekDayEnableCustomFontSize ? Settings.WeekDayFontSize : 0);
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
        vm = new AdvancedDateViewModel(_timeBaseService, Settings, 
            UpdateDateFontColor, UpdateDateFontSize,
            UpdateWeekDayFontColor, UpdateWeekDayFontSize);
        DataContext = vm;
        _dateTextBlock.Text = vm.DatePart;
        _weekDayTextBlock.Text = vm.WeekDayPart;
        _weekDayTextBlock.Visibility = Settings.ShowWeekDay ? Visibility.Visible : Visibility.Collapsed;
        vm.PropertyChanged += OnVmPropertyChanged;
        UpdateDateFontColor(Settings.FontColor);
        UpdateDateFontSize(Settings.EnableCustomFontSize ? Settings.DateFontSize : 0);
        UpdateDateFontFamily();
        UpdateDateFontWeight();
        UpdateWeekDayFontColor(Settings.WeekDayFontColor);
        UpdateWeekDayFontSize(Settings.WeekDayEnableCustomFontSize ? Settings.WeekDayFontSize : 0);
        UpdateWeekDayFontFamily();
        UpdateWeekDayFontWeight();
        Settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.FontFamily) ||
            e.PropertyName == nameof(Settings.EnableCustomFontFamily))
        {
            UpdateDateFontFamily();
        }
        else if (e.PropertyName == nameof(Settings.FontWeight) ||
                 e.PropertyName == nameof(Settings.EnableCustomFontWeight))
        {
            UpdateDateFontWeight();
        }
        else if (e.PropertyName == nameof(Settings.WeekDayFontFamily) ||
                 e.PropertyName == nameof(Settings.WeekDayEnableCustomFontFamily))
        {
            UpdateWeekDayFontFamily();
        }
        else if (e.PropertyName == nameof(Settings.WeekDayFontWeight) ||
                 e.PropertyName == nameof(Settings.WeekDayEnableCustomFontWeight))
        {
            UpdateWeekDayFontWeight();
        }
        else if (e.PropertyName == nameof(Settings.ShowWeekDay))
        {
            _weekDayTextBlock.Visibility = Settings.ShowWeekDay ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(vm.DatePart)) _dateTextBlock.Text = vm.DatePart;
        if (e.PropertyName == nameof(vm.WeekDayPart)) _weekDayTextBlock.Text = vm.WeekDayPart;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ThemeHelper.ThemeChanged -= OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        (vm as IDisposable)?.Dispose();
        _isDisposed = true;
    }
}
