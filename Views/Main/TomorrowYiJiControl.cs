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
        yiLabelTb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        yiValueTb = new TextBlock { FontSize = 14, Foreground = Brushes.Green };
        yiSp.Children.Add(yiLabelTb);
        yiSp.Children.Add(yiValueTb);
        
        var jiSp = new StackPanel { Orientation = Orientation.Horizontal };
        jiLabelTb = new TextBlock { FontSize = 14, Foreground = Brushes.White };
        jiValueTb = new TextBlock { FontSize = 14, Foreground = Brushes.Red };
        jiSp.Children.Add(jiLabelTb);
        jiSp.Children.Add(jiValueTb);
        
        mainSp.Children.Add(yiSp);
        mainSp.Children.Add(jiSp);
        rootBorder.Child = mainSp;
        Content = rootBorder;
    }

    private void UpdateYiLabelFontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                yiLabelTb.Foreground = new SolidColorBrush(color);
            }
        }
        catch { yiLabelTb.Foreground = Brushes.White; }
    }

    private void UpdateYiLabelFontSize(double fontSize) { yiLabelTb.FontSize = fontSize; }
    private void UpdateYiValueFontSize(double fontSize) { yiValueTb.FontSize = fontSize; }
    private void UpdateJiLabelFontColor(string colorStr)
    {
        try
        {
            if (colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                jiLabelTb.Foreground = new SolidColorBrush(color);
            }
        }
        catch { jiLabelTb.Foreground = Brushes.White; }
    }

    private void UpdateJiLabelFontSize(double fontSize) { jiLabelTb.FontSize = fontSize; }
    private void UpdateJiValueFontSize(double fontSize) { jiValueTb.FontSize = fontSize; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
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
        UpdateYiLabelFontSize(Settings.YiLabelFontSize);
        UpdateYiValueFontSize(Settings.YiValueFontSize);
        UpdateJiLabelFontColor(Settings.JiLabelFontColor);
        UpdateJiLabelFontSize(Settings.JiLabelFontSize);
        UpdateJiValueFontSize(Settings.JiValueFontSize);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        (vm as IDisposable)?.Dispose();
    }
}