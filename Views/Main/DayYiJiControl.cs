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
    "22334455-6677-8899-0011-223344556702",
    "今日宜忌（ATI）",
    "\uE126",
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
        yiLabelTb.FontSize = fontSize;
    }

    private void UpdateYiValueFontSize(double fontSize)
    {
        yiValueTb.FontSize = fontSize;
    }

    private void UpdateJiLabelFontSize(double fontSize)
    {
        jiLabelTb.FontSize = fontSize;
    }

    private void UpdateJiValueFontSize(double fontSize)
    {
        jiValueTb.FontSize = fontSize;
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
        mainSp.Spacing = Settings.DisplayMode == 0 ? 16 : 0;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateYiLabelFontColor(Settings.YiLabelFontColor);
        UpdateJiLabelFontColor(Settings.JiLabelFontColor);
        yiValueTb.Foreground = ThemeHelper.GetYiBrush();
        jiValueTb.Foreground = ThemeHelper.GetJiBrush();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
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
        UpdateYiLabelFontSize(Settings.YiLabelEnableCustomFontSize ? Settings.YiLabelFontSize : 14);
        UpdateYiValueFontSize(Settings.YiValueEnableCustomFontSize ? Settings.YiValueFontSize : 14);
        UpdateJiLabelFontSize(Settings.JiLabelEnableCustomFontSize ? Settings.JiLabelFontSize : 14);
        UpdateJiValueFontSize(Settings.JiValueEnableCustomFontSize ? Settings.JiValueFontSize : 14);
        
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

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        Settings.PropertyChanged -= OnSettingsChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        (vm as IDisposable)?.Dispose();
    }
}
