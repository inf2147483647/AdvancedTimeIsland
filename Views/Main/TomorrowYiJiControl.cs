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

[ComponentInfo("688e6e20-f992-4310-8b16-bdebea05c101", "明日宜忌（ATI）", "\uE124", "显示明日宜忌")]
public class TomorrowYiJiControl : ComponentBase<TomorrowYiJiSettings>
{
    private TomorrowYiJiViewModel vm;
    private TextBlock yiLabelTb;
    private TextBlock yiValueTb;
    private TextBlock jiLabelTb;
    private TextBlock jiValueTb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public TomorrowYiJiControl(TimeBaseService tbs) { _timeBaseService = tbs; InitializeComponent(); }

    private void InitializeComponent()
    {
        rootBorder = new Border { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var mainSp = new StackPanel { Orientation = Orientation.Vertical };
        
        var yiSp = new StackPanel { Orientation = Orientation.Horizontal };
        yiLabelTb = new TextBlock();
        yiValueTb = new TextBlock { Foreground = ThemeHelper.GetYiBrush() };
        yiSp.Children.Add(yiLabelTb);
        yiSp.Children.Add(yiValueTb);
        
        var jiSp = new StackPanel { Orientation = Orientation.Horizontal };
        jiLabelTb = new TextBlock();
        jiValueTb = new TextBlock { Foreground = ThemeHelper.GetJiBrush() };
        jiSp.Children.Add(jiLabelTb);
        jiSp.Children.Add(jiValueTb);
        
        mainSp.Children.Add(yiSp);
        mainSp.Children.Add(jiSp);
        rootBorder.Child = mainSp;
        Content = rootBorder;
    }

    private void UpdateYiLabelFontColor(string colorStr)
    {
        yiLabelTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.YiLabelEnableCustomFontColor);
    }

    private void UpdateYiLabelFontSize(double fontSize) { yiLabelTb.FontSize = fontSize; }
    private void UpdateYiLabelFontFamily(string fontFamily) { yiLabelTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(fontFamily); }
    private void UpdateYiLabelFontWeight(string fontWeight) { yiLabelTb.FontWeight = Settings.YiLabelEnableCustomFontWeight ? FontFamilyHelper.GetFontWeightFromString(fontWeight) : FontWeight.Normal; }
    private void UpdateYiValueFontSize(double fontSize) { yiValueTb.FontSize = fontSize; }
    private void UpdateYiValueFontFamily(string fontFamily) { yiValueTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(fontFamily); }
    private void UpdateYiValueFontWeight(string fontWeight) { yiValueTb.FontWeight = Settings.YiValueEnableCustomFontWeight ? FontFamilyHelper.GetFontWeightFromString(fontWeight) : FontWeight.Normal; }
    private void UpdateJiLabelFontColor(string colorStr)
    {
        jiLabelTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.JiLabelEnableCustomFontColor);
    }

    private void UpdateJiLabelFontSize(double fontSize) { jiLabelTb.FontSize = fontSize; }
    private void UpdateJiLabelFontFamily(string fontFamily) { jiLabelTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(fontFamily); }
    private void UpdateJiLabelFontWeight(string fontWeight) { jiLabelTb.FontWeight = Settings.JiLabelEnableCustomFontWeight ? FontFamilyHelper.GetFontWeightFromString(fontWeight) : FontWeight.Normal; }
    private void UpdateJiValueFontSize(double fontSize) { jiValueTb.FontSize = fontSize; }
    private void UpdateJiValueFontFamily(string fontFamily) { jiValueTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(fontFamily); }
    private void UpdateJiValueFontWeight(string fontWeight) { jiValueTb.FontWeight = Settings.JiValueEnableCustomFontWeight ? FontFamilyHelper.GetFontWeightFromString(fontWeight) : FontWeight.Normal; }

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
        vm = new TomorrowYiJiViewModel(_timeBaseService, Settings, UpdateYiLabelFontColor, UpdateYiLabelFontSize, UpdateYiValueFontSize, UpdateJiLabelFontColor, UpdateJiLabelFontSize, UpdateJiValueFontSize);
        DataContext = vm;
        yiLabelTb.Text = vm.YiLabelText;
        yiValueTb.Text = vm.YiValueText;
        jiLabelTb.Text = vm.JiLabelText;
        jiValueTb.Text = vm.JiValueText;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.YiLabelText)) yiLabelTb.Text = vm.YiLabelText;
            if (e.PropertyName == nameof(vm.YiValueText)) yiValueTb.Text = vm.YiValueText;
            if (e.PropertyName == nameof(vm.JiLabelText)) jiLabelTb.Text = vm.JiLabelText;
            if (e.PropertyName == nameof(vm.JiValueText)) jiValueTb.Text = vm.JiValueText;
        };
        Settings.PropertyChanged += OnSettingsChanged;
        UpdateYiLabelFontColor(Settings.YiLabelFontColor);
        UpdateYiLabelFontSize(Settings.YiLabelEnableCustomFontSize ? Settings.YiLabelFontSize : 14);
        UpdateYiLabelFontFamily(Settings.YiLabelEnableCustomFontFamily ? Settings.YiLabelFontFamily : "");
        UpdateYiLabelFontWeight(Settings.YiLabelFontWeight);
        UpdateYiValueFontSize(Settings.YiValueEnableCustomFontSize ? Settings.YiValueFontSize : 14);
        UpdateYiValueFontFamily(Settings.YiValueEnableCustomFontFamily ? Settings.YiValueFontFamily : "");
        UpdateYiValueFontWeight(Settings.YiValueFontWeight);
        UpdateJiLabelFontColor(Settings.JiLabelFontColor);
        UpdateJiLabelFontSize(Settings.JiLabelEnableCustomFontSize ? Settings.JiLabelFontSize : 14);
        UpdateJiLabelFontFamily(Settings.JiLabelEnableCustomFontFamily ? Settings.JiLabelFontFamily : "");
        UpdateJiLabelFontWeight(Settings.JiLabelFontWeight);
        UpdateJiValueFontSize(Settings.JiValueEnableCustomFontSize ? Settings.JiValueFontSize : 14);
        UpdateJiValueFontFamily(Settings.JiValueEnableCustomFontFamily ? Settings.JiValueFontFamily : "");
        UpdateJiValueFontWeight(Settings.JiValueFontWeight);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        Settings.PropertyChanged -= OnSettingsChanged;
        (vm as IDisposable)?.Dispose();
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.YiLabelFontFamily) || e.PropertyName == nameof(Settings.YiLabelEnableCustomFontFamily))
        {
            UpdateYiLabelFontFamily(Settings.YiLabelEnableCustomFontFamily ? Settings.YiLabelFontFamily : "");
        }
        else if (e.PropertyName == nameof(Settings.YiValueFontFamily) || e.PropertyName == nameof(Settings.YiValueEnableCustomFontFamily))
        {
            UpdateYiValueFontFamily(Settings.YiValueEnableCustomFontFamily ? Settings.YiValueFontFamily : "");
        }
        else if (e.PropertyName == nameof(Settings.JiLabelFontFamily) || e.PropertyName == nameof(Settings.JiLabelEnableCustomFontFamily))
        {
            UpdateJiLabelFontFamily(Settings.JiLabelEnableCustomFontFamily ? Settings.JiLabelFontFamily : "");
        }
        else if (e.PropertyName == nameof(Settings.JiValueFontFamily) || e.PropertyName == nameof(Settings.JiValueEnableCustomFontFamily))
        {
            UpdateJiValueFontFamily(Settings.JiValueEnableCustomFontFamily ? Settings.JiValueFontFamily : "");
        }
        else if (e.PropertyName == nameof(Settings.YiLabelFontWeight) || e.PropertyName == nameof(Settings.YiLabelEnableCustomFontWeight))
        {
            UpdateYiLabelFontWeight(Settings.YiLabelFontWeight);
        }
        else if (e.PropertyName == nameof(Settings.YiValueFontWeight) || e.PropertyName == nameof(Settings.YiValueEnableCustomFontWeight))
        {
            UpdateYiValueFontWeight(Settings.YiValueFontWeight);
        }
        else if (e.PropertyName == nameof(Settings.JiLabelFontWeight) || e.PropertyName == nameof(Settings.JiLabelEnableCustomFontWeight))
        {
            UpdateJiLabelFontWeight(Settings.JiLabelFontWeight);
        }
        else if (e.PropertyName == nameof(Settings.JiValueFontWeight) || e.PropertyName == nameof(Settings.JiValueEnableCustomFontWeight))
        {
            UpdateJiValueFontWeight(Settings.JiValueFontWeight);
        }
    }
}
