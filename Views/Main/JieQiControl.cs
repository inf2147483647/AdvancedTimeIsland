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
    "22334455-6677-8899-0011-223344556699",
    "节气（ATI）",
    "\uE123",
    "显示当前或最近节气及时间范围"
)]
public class JieQiControl : ComponentBase<JieQiSettings>
{
    private JieQiViewModel vm;
    private TextBlock labelTb;
    private TextBlock valueTb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public JieQiControl(TimeBaseService tbs)
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
        labelTb.FontSize = fontSize;
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

    private void UpdateValueFontSize(double fontSize)
    {
        valueTb.FontSize = fontSize;
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
        vm = new JieQiViewModel(_timeBaseService, Settings, UpdateLabelFontColor, UpdateLabelFontSize, UpdateValueFontColor, UpdateValueFontSize);
        DataContext = vm;
        labelTb.Text = vm.LabelText;
        valueTb.Text = vm.ValueText;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.LabelText)) labelTb.Text = vm.LabelText;
            if (e.PropertyName == nameof(vm.ValueText)) valueTb.Text = vm.ValueText;
        };
        Settings.PropertyChanged += OnSettingsChanged;
        UpdateLabelFontColor(Settings.LabelFontColor);
        UpdateLabelFontSize(Settings.LabelEnableCustomFontSize ? Settings.LabelFontSize : 14);
        UpdateLabelFontFamily(Settings.LabelEnableCustomFontFamily ? Settings.LabelFontFamily : "");
        UpdateLabelFontWeight(Settings.LabelFontWeight);
        UpdateValueFontColor(Settings.ValueFontColor);
        UpdateValueFontSize(Settings.ValueEnableCustomFontSize ? Settings.ValueFontSize : 14);
        UpdateValueFontFamily(Settings.ValueEnableCustomFontFamily ? Settings.ValueFontFamily : "");
        UpdateValueFontWeight(Settings.ValueFontWeight);
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
