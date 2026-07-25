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
    "每日宜忌（ATI）",
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
        
        var mainSp = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
        
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

    private void UpdateLabelFontColor(string colorStr)
    {
        var brush = ThemeHelper.GetColorBrush(colorStr, Settings.LabelEnableCustomFontColor);
        yiLabelTb.Foreground = brush;
        jiLabelTb.Foreground = brush;
    }

    private void UpdateLabelFontSize(double fontSize)
    {
        yiLabelTb.FontSize = fontSize;
        jiLabelTb.FontSize = fontSize;
    }

    private void UpdateValueFontSize(double fontSize)
    {
        yiValueTb.FontSize = fontSize;
        jiValueTb.FontSize = fontSize;
    }

    private void UpdateLabelFontFamily()
    {
        if (Settings.LabelEnableCustomFontFamily)
        {
            var fontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.LabelFontFamily);
            yiLabelTb.FontFamily = fontFamily;
            jiLabelTb.FontFamily = fontFamily;
        }
        else
        {
            yiLabelTb.ClearValue(TextBlock.FontFamilyProperty);
            jiLabelTb.ClearValue(TextBlock.FontFamilyProperty);
        }
    }

    private void UpdateLabelFontWeight(string fontWeight)
    {
        if (Settings.LabelEnableCustomFontWeight)
        {
            var fw = FontFamilyHelper.GetFontWeightFromString(fontWeight);
            yiLabelTb.FontWeight = fw;
            jiLabelTb.FontWeight = fw;
        }
        else
        {
            yiLabelTb.ClearValue(TextBlock.FontWeightProperty);
            jiLabelTb.ClearValue(TextBlock.FontWeightProperty);
        }
    }

    private void UpdateValueFontFamily()
    {
        if (Settings.ValueEnableCustomFontFamily)
        {
            var fontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.ValueFontFamily);
            yiValueTb.FontFamily = fontFamily;
            jiValueTb.FontFamily = fontFamily;
        }
        else
        {
            yiValueTb.ClearValue(TextBlock.FontFamilyProperty);
            jiValueTb.ClearValue(TextBlock.FontFamilyProperty);
        }
    }

    private void UpdateValueFontWeight(string fontWeight)
    {
        if (Settings.ValueEnableCustomFontWeight)
        {
            var fw = FontFamilyHelper.GetFontWeightFromString(fontWeight);
            yiValueTb.FontWeight = fw;
            jiValueTb.FontWeight = fw;
        }
        else
        {
            yiValueTb.ClearValue(TextBlock.FontWeightProperty);
            jiValueTb.ClearValue(TextBlock.FontWeightProperty);
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateLabelFontColor(Settings.LabelFontColor);
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
        vm = new DayYiJiViewModel(_timeBaseService, Settings, UpdateLabelFontColor, UpdateLabelFontSize, UpdateValueFontSize);
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
        UpdateLabelFontColor(Settings.LabelFontColor);
        UpdateLabelFontSize(Settings.LabelEnableCustomFontSize ? Settings.LabelFontSize : 14);
        UpdateValueFontSize(Settings.ValueEnableCustomFontSize ? Settings.ValueFontSize : 14);
        UpdateLabelFontFamily();
        UpdateValueFontFamily();
        UpdateLabelFontWeight(Settings.LabelFontWeight);
        UpdateValueFontWeight(Settings.ValueFontWeight);
        Settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.LabelFontFamily) ||
            e.PropertyName == nameof(Settings.LabelEnableCustomFontFamily))
        {
            UpdateLabelFontFamily();
        }
        if (e.PropertyName == nameof(Settings.ValueFontFamily) ||
            e.PropertyName == nameof(Settings.ValueEnableCustomFontFamily))
        {
            UpdateValueFontFamily();
        }
        else if (e.PropertyName == nameof(Settings.LabelFontWeight) ||
                 e.PropertyName == nameof(Settings.LabelEnableCustomFontWeight))
        {
            UpdateLabelFontWeight(Settings.LabelFontWeight);
        }
        else if (e.PropertyName == nameof(Settings.ValueFontWeight) ||
                 e.PropertyName == nameof(Settings.ValueEnableCustomFontWeight))
        {
            UpdateValueFontWeight(Settings.ValueFontWeight);
        }
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
}
