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
    "22334455-6677-8899-0011-223344556700",
    "生肖（ATI）",
    "\uE124",
    "显示当前生肖及年份"
)]
public class ShengXiaoControl : ComponentBase<ShengXiaoSettings>
{
    private ShengXiaoViewModel vm;
    private TextBlock labelTb;
    private TextBlock valueTb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public ShengXiaoControl(TimeBaseService tbs)
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
        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        labelTb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        valueTb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(labelTb);
        sp.Children.Add(valueTb);
        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateLabelFontColor(string colorStr)
    {
        labelTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.LabelEnableCustomFontColor);
    }

    private void UpdateLabelFontSize(double fontSize)
    {
        if (fontSize > 0)
            labelTb.FontSize = fontSize;
        else
            labelTb.FontSize = FontFamilyHelper.GetBodyFontSize(labelTb);
    }

    private void UpdateLabelFontFamily(string fontFamily)
    {
        if (string.IsNullOrEmpty(fontFamily))
            labelTb.ClearValue(TextBlock.FontFamilyProperty);
        else
            labelTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(fontFamily);
    }

    private void UpdateLabelFontWeight(string fontWeight)
    {
        if (Settings.LabelEnableCustomFontWeight)
            labelTb.FontWeight = FontFamilyHelper.GetFontWeightFromString(fontWeight);
        else
            labelTb.ClearValue(TextBlock.FontWeightProperty);
    }

    private void UpdateValueFontColor(string colorStr)
    {
        valueTb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.ValueEnableCustomFontColor);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateLabelFontColor(Settings.LabelFontColor);
        UpdateValueFontColor(Settings.ValueFontColor);
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateLabelFontSize(Settings.LabelEnableCustomFontSize ? Settings.LabelFontSize : 0);
        UpdateValueFontSize(Settings.ValueEnableCustomFontSize ? Settings.ValueFontSize : 0);
    }

    private void UpdateValueFontSize(double fontSize)
    {
        if (fontSize > 0)
            valueTb.FontSize = fontSize;
        else
            valueTb.FontSize = FontFamilyHelper.GetBodyFontSize(valueTb);
    }

    private void UpdateValueFontFamily(string fontFamily)
    {
        if (string.IsNullOrEmpty(fontFamily))
            valueTb.ClearValue(TextBlock.FontFamilyProperty);
        else
            valueTb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(fontFamily);
    }

    private void UpdateValueFontWeight(string fontWeight)
    {
        if (Settings.ValueEnableCustomFontWeight)
            valueTb.FontWeight = FontFamilyHelper.GetFontWeightFromString(fontWeight);
        else
            valueTb.ClearValue(TextBlock.FontWeightProperty);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;
        vm = new ShengXiaoViewModel(_timeBaseService, Settings, UpdateLabelFontColor, UpdateLabelFontSize, UpdateValueFontColor, UpdateValueFontSize);
        DataContext = vm;
        labelTb.Text = vm.LabelText;
        valueTb.Text = vm.ValueText;
        vm.PropertyChanged += OnVmPropertyChanged;
        Settings.PropertyChanged += OnSettingsChanged;
        UpdateLabelFontColor(Settings.LabelFontColor);
        UpdateLabelFontSize(Settings.LabelEnableCustomFontSize ? Settings.LabelFontSize : 0);
        UpdateLabelFontFamily(Settings.LabelEnableCustomFontFamily ? Settings.LabelFontFamily : "");
        UpdateLabelFontWeight(Settings.LabelFontWeight);
        UpdateValueFontColor(Settings.ValueFontColor);
        UpdateValueFontSize(Settings.ValueEnableCustomFontSize ? Settings.ValueFontSize : 0);
        UpdateValueFontFamily(Settings.ValueEnableCustomFontFamily ? Settings.ValueFontFamily : "");
        UpdateValueFontWeight(Settings.ValueFontWeight);
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(vm.LabelText)) labelTb.Text = vm.LabelText;
        if (e.PropertyName == nameof(vm.ValueText)) valueTb.Text = vm.ValueText;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        (vm as IDisposable)?.Dispose();
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.LabelFontFamily) || e.PropertyName == nameof(Settings.LabelEnableCustomFontFamily))
        {
            UpdateLabelFontFamily(Settings.LabelEnableCustomFontFamily ? Settings.LabelFontFamily : "");
        }
        else if (e.PropertyName == nameof(Settings.ValueFontFamily) || e.PropertyName == nameof(Settings.ValueEnableCustomFontFamily))
        {
            UpdateValueFontFamily(Settings.ValueEnableCustomFontFamily ? Settings.ValueFontFamily : "");
        }
        else if (e.PropertyName == nameof(Settings.LabelFontWeight) || e.PropertyName == nameof(Settings.LabelEnableCustomFontWeight))
        {
            UpdateLabelFontWeight(Settings.LabelFontWeight);
        }
        else if (e.PropertyName == nameof(Settings.ValueFontWeight) || e.PropertyName == nameof(Settings.ValueEnableCustomFontWeight))
        {
            UpdateValueFontWeight(Settings.ValueFontWeight);
        }
    }
}
