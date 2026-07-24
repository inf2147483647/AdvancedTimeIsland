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
    private void UpdateYiValueFontSize(double fontSize) { yiValueTb.FontSize = fontSize; }
    private void UpdateJiLabelFontColor(string colorStr)
    {
        jiLabelTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.JiLabelEnableCustomFontColor);
    }

    private void UpdateJiLabelFontSize(double fontSize) { jiLabelTb.FontSize = fontSize; }
    private void UpdateJiValueFontSize(double fontSize) { jiValueTb.FontSize = fontSize; }

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
        UpdateYiLabelFontColor(Settings.YiLabelFontColor);
        UpdateYiLabelFontSize(Settings.YiLabelEnableCustomFontSize ? Settings.YiLabelFontSize : 14);
        UpdateYiValueFontSize(Settings.YiValueEnableCustomFontSize ? Settings.YiValueFontSize : 14);
        UpdateJiLabelFontColor(Settings.JiLabelFontColor);
        UpdateJiLabelFontSize(Settings.JiLabelEnableCustomFontSize ? Settings.JiLabelFontSize : 14);
        UpdateJiValueFontSize(Settings.JiValueEnableCustomFontSize ? Settings.JiValueFontSize : 14);
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
