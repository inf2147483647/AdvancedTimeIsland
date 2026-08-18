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
    "22334455-6677-8899-0011-223344556702",
    "今日宜忌（ATI）",
    PackIconKind.ClipboardTextOutline,
    "显示今日宜做和忌做的事情"
)]
public class DayYiJiControl : ComponentBase<DayYiJiSettings>
{
    private DayYiJiViewModel vm;
    private TextBlock yiLabelTb;
    private TextBlock yiValueTb;
    private TextBlock jiLabelTb;
    private TextBlock jiValueTb;
    private Border rootBorder;
    private StackPanel mainSp;
    private readonly TimeBaseService _timeBaseService;
    private bool _initCompleted;

    public DayYiJiControl(TimeBaseService tbs)
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
        
        mainSp = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
        
        var yiSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        yiLabelTb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        yiValueTb = new TextBlock { Foreground = ThemeHelper.GetYiBrush(), VerticalAlignment = VerticalAlignment.Center };
        yiSp.Children.Add(yiLabelTb);
        yiSp.Children.Add(yiValueTb);
        mainSp.Children.Add(yiSp);
        
        var jiSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        jiLabelTb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        jiValueTb = new TextBlock { Foreground = ThemeHelper.GetJiBrush(), VerticalAlignment = VerticalAlignment.Center };
        jiSp.Children.Add(jiLabelTb);
        jiSp.Children.Add(jiValueTb);
        mainSp.Children.Add(jiSp);
        
        rootBorder.Child = mainSp;
        Content = rootBorder;
    }

    private void UpdateYiLabelFontColor(string colorStr)
    {
        yiLabelTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.YiLabelEnableCustomFontColor);
    }

    private void UpdateJiLabelFontColor(string colorStr)
    {
        jiLabelTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.JiLabelEnableCustomFontColor);
    }

    private void UpdateYiLabelFontSize(double fontSize)
    {
        if (fontSize > 0)
            yiLabelTb.FontSize = fontSize;
        else
            yiLabelTb.FontSize = FontFamilyHelper.GetBodyFontSize(yiLabelTb);
    }

    private void UpdateYiValueFontSize(double fontSize)
    {
        if (fontSize > 0)
            yiValueTb.FontSize = fontSize;
        else
            yiValueTb.FontSize = FontFamilyHelper.GetBodyFontSize(yiValueTb);
    }

    private void UpdateJiLabelFontSize(double fontSize)
    {
        if (fontSize > 0)
            jiLabelTb.FontSize = fontSize;
        else
            jiLabelTb.FontSize = FontFamilyHelper.GetBodyFontSize(jiLabelTb);
    }

    private void UpdateJiValueFontSize(double fontSize)
    {
        if (fontSize > 0)
            jiValueTb.FontSize = fontSize;
        else
            jiValueTb.FontSize = FontFamilyHelper.GetBodyFontSize(jiValueTb);
    }

    private void UpdateYiLabelFontFamily()
    {
        if (Settings.YiLabelEnableCustomFontFamily)
            yiLabelTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.YiLabelFontFamily);
        else
            yiLabelTb.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateYiValueFontFamily()
    {
        if (Settings.YiValueEnableCustomFontFamily)
            yiValueTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.YiValueFontFamily);
        else
            yiValueTb.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateJiLabelFontFamily()
    {
        if (Settings.JiLabelEnableCustomFontFamily)
            jiLabelTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.JiLabelFontFamily);
        else
            jiLabelTb.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateJiValueFontFamily()
    {
        if (Settings.JiValueEnableCustomFontFamily)
            jiValueTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.JiValueFontFamily);
        else
            jiValueTb.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateYiLabelFontWeight(string fontWeight)
    {
        if (Settings.YiLabelEnableCustomFontWeight)
            yiLabelTb.FontWeight = FontFamilyHelper.GetFontWeightFromString(fontWeight);
        else
            yiLabelTb.ClearValue(TextBlock.FontWeightProperty);
    }

    private void UpdateYiValueFontWeight(string fontWeight)
    {
        if (Settings.YiValueEnableCustomFontWeight)
            yiValueTb.FontWeight = FontFamilyHelper.GetFontWeightFromString(fontWeight);
        else
            yiValueTb.ClearValue(TextBlock.FontWeightProperty);
    }

    private void UpdateJiLabelFontWeight(string fontWeight)
    {
        if (Settings.JiLabelEnableCustomFontWeight)
            jiLabelTb.FontWeight = FontFamilyHelper.GetFontWeightFromString(fontWeight);
        else
            jiLabelTb.ClearValue(TextBlock.FontWeightProperty);
    }

    private void UpdateJiValueFontWeight(string fontWeight)
    {
        if (Settings.JiValueEnableCustomFontWeight)
            jiValueTb.FontWeight = FontFamilyHelper.GetFontWeightFromString(fontWeight);
        else
            jiValueTb.ClearValue(TextBlock.FontWeightProperty);
    }

    private void UpdateDisplayMode()
    {
        mainSp.Orientation = Settings.DisplayMode == 0 ? Orientation.Horizontal : Orientation.Vertical;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateYiLabelFontColor(Settings.YiLabelFontColor);
        UpdateJiLabelFontColor(Settings.JiLabelFontColor);
        yiValueTb.Foreground = ThemeHelper.GetYiBrush();
        jiValueTb.Foreground = ThemeHelper.GetJiBrush();
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateYiLabelFontSize(Settings.YiLabelEnableCustomFontSize ? Settings.YiLabelFontSize : 0);
        UpdateYiValueFontSize(Settings.YiValueEnableCustomFontSize ? Settings.YiValueFontSize : 0);
        UpdateJiLabelFontSize(Settings.JiLabelEnableCustomFontSize ? Settings.JiLabelFontSize : 0);
        UpdateJiValueFontSize(Settings.JiValueEnableCustomFontSize ? Settings.JiValueFontSize : 0);
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
        vm = new DayYiJiViewModel(_timeBaseService, Settings, 
            UpdateYiLabelFontColor, UpdateJiLabelFontColor,
            UpdateYiLabelFontSize, UpdateYiValueFontSize, UpdateJiLabelFontSize, UpdateJiValueFontSize);
        DataContext = vm;
        yiLabelTb.Text = vm.YiLabelText;
        yiValueTb.Text = vm.YiValueText;
        jiLabelTb.Text = vm.JiLabelText;
        jiValueTb.Text = vm.JiValueText;
        vm.PropertyChanged += OnVmPropertyChanged;
        
        UpdateYiLabelFontColor(Settings.YiLabelFontColor);
        UpdateJiLabelFontColor(Settings.JiLabelFontColor);
        UpdateYiLabelFontSize(Settings.YiLabelEnableCustomFontSize ? Settings.YiLabelFontSize : 0);
        UpdateYiValueFontSize(Settings.YiValueEnableCustomFontSize ? Settings.YiValueFontSize : 0);
        UpdateJiLabelFontSize(Settings.JiLabelEnableCustomFontSize ? Settings.JiLabelFontSize : 0);
        UpdateJiValueFontSize(Settings.JiValueEnableCustomFontSize ? Settings.JiValueFontSize : 0);
        
        UpdateYiLabelFontFamily();
        UpdateYiValueFontFamily();
        UpdateJiLabelFontFamily();
        UpdateJiValueFontFamily();
        
        UpdateYiLabelFontWeight(Settings.YiLabelFontWeight);
        UpdateYiValueFontWeight(Settings.YiValueFontWeight);
        UpdateJiLabelFontWeight(Settings.JiLabelFontWeight);
        UpdateJiValueFontWeight(Settings.JiValueFontWeight);
        
        UpdateDisplayMode();
        Settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.YiLabelFontFamily) || e.PropertyName == nameof(Settings.YiLabelEnableCustomFontFamily))
            UpdateYiLabelFontFamily();
        if (e.PropertyName == nameof(Settings.YiValueFontFamily) || e.PropertyName == nameof(Settings.YiValueEnableCustomFontFamily))
            UpdateYiValueFontFamily();
        if (e.PropertyName == nameof(Settings.JiLabelFontFamily) || e.PropertyName == nameof(Settings.JiLabelEnableCustomFontFamily))
            UpdateJiLabelFontFamily();
        if (e.PropertyName == nameof(Settings.JiValueFontFamily) || e.PropertyName == nameof(Settings.JiValueEnableCustomFontFamily))
            UpdateJiValueFontFamily();
        
        if (e.PropertyName == nameof(Settings.YiLabelFontWeight) || e.PropertyName == nameof(Settings.YiLabelEnableCustomFontWeight))
            UpdateYiLabelFontWeight(Settings.YiLabelFontWeight);
        if (e.PropertyName == nameof(Settings.YiValueFontWeight) || e.PropertyName == nameof(Settings.YiValueEnableCustomFontWeight))
            UpdateYiValueFontWeight(Settings.YiValueFontWeight);
        if (e.PropertyName == nameof(Settings.JiLabelFontWeight) || e.PropertyName == nameof(Settings.JiLabelEnableCustomFontWeight))
            UpdateJiLabelFontWeight(Settings.JiLabelFontWeight);
        if (e.PropertyName == nameof(Settings.JiValueFontWeight) || e.PropertyName == nameof(Settings.JiValueEnableCustomFontWeight))
            UpdateJiValueFontWeight(Settings.JiValueFontWeight);
        
        if (e.PropertyName == nameof(Settings.DisplayMode))
            UpdateDisplayMode();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(vm.YiLabelText)) yiLabelTb.Text = vm.YiLabelText;
        if (e.PropertyName == nameof(vm.YiValueText)) yiValueTb.Text = vm.YiValueText;
        if (e.PropertyName == nameof(vm.JiLabelText)) jiLabelTb.Text = vm.JiLabelText;
        if (e.PropertyName == nameof(vm.JiValueText)) jiValueTb.Text = vm.JiValueText;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ThemeHelper.ThemeChanged -= OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        (vm as IDisposable)?.Dispose();
    }
}
