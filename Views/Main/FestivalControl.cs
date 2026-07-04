using System;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Models;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "22334455-6677-8899-0011-223344556701",
    "节日（ATI）",
    "\uE125",
    "显示当前节日"
)]
public class FestivalControl : ComponentBase<FestivalSettings>
{
    private FestivalViewModel vm;
    private TextBlock labelTb;
    private TextBlock valueTb;
    private Border rootBorder;
    private readonly TimeBaseService _timeBaseService;

    public FestivalControl(TimeBaseService tbs)
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
        labelTb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        valueTb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        sp.Children.Add(labelTb);
        sp.Children.Add(valueTb);
        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateLabelFontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && colorStr.Length == 7)
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                labelTb.Foreground = new SolidColorBrush(color);
            }
        }
        catch
        {
            labelTb.Foreground = Brushes.White;
        }
    }

    private void UpdateLabelFontSize(double fontSize)
    {
        labelTb.FontSize = fontSize;
    }

    private void UpdateValueFontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && colorStr.Length == 7)
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                valueTb.Foreground = new SolidColorBrush(color);
            }
        }
        catch
        {
            valueTb.Foreground = Brushes.White;
        }
    }

    private void UpdateValueFontSize(double fontSize)
    {
        valueTb.FontSize = fontSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        vm = new FestivalViewModel(_timeBaseService, Settings, UpdateLabelFontColor, UpdateLabelFontSize, UpdateValueFontColor, UpdateValueFontSize);
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
        (vm as IDisposable)?.Dispose();
    }
}
