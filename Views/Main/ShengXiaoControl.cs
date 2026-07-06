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
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        labelTb = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush() };
        valueTb = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush() };
        sp.Children.Add(labelTb);
        sp.Children.Add(valueTb);
        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateLabelFontColor(string colorStr)
    {
        labelTb.Foreground = ThemeHelper.ParseColorOrThemeDefault(colorStr);
    }

    private void UpdateLabelFontSize(double fontSize)
    {
        labelTb.FontSize = fontSize;
    }

    private void UpdateValueFontColor(string colorStr)
    {
        valueTb.Foreground = ThemeHelper.ParseColorOrThemeDefault(colorStr);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        var newLabelColor = ThemeHelper.GetSmartContrastColor(Settings.LabelFontColor);
        Settings.LabelFontColor = newLabelColor;
        UpdateLabelFontColor(newLabelColor);

        var newValueColor = ThemeHelper.GetSmartContrastColor(Settings.ValueFontColor);
        Settings.ValueFontColor = newValueColor;
        UpdateValueFontColor(newValueColor);
    }

    private void UpdateValueFontSize(double fontSize)
    {
        valueTb.FontSize = fontSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        vm = new ShengXiaoViewModel(_timeBaseService, Settings, UpdateLabelFontColor, UpdateLabelFontSize, UpdateValueFontColor, UpdateValueFontSize);
        DataContext = vm;
        labelTb.Text = vm.LabelText;
        valueTb.Text = vm.ValueText;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.LabelText)) labelTb.Text = vm.LabelText;
            if (e.PropertyName == nameof(vm.ValueText)) valueTb.Text = vm.ValueText;
        };
        UpdateLabelFontColor(Settings.LabelFontColor);
        UpdateLabelFontSize(Settings.LabelFontSize);
        UpdateValueFontColor(Settings.ValueFontColor);
        UpdateValueFontSize(Settings.ValueFontSize);
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
