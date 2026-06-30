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
    "44556677-8899-0011-2233-445566778899",
    "多倒计时（ATI）",
    "\uE121",
    "显示多个倒计时，自动排序并切换"
)]
public class CountdownControl : ComponentBase<CountdownSettings>
{
    private CountdownViewModel vm;
    private TextBlock tbText1;
    private TextBlock tbText2;
    private TextBlock tbText3;
    private TextBlock tbTime;
    private TextBlock tbText4;
    private Border rootBorder;
    private StackPanel contentPanel;
    private readonly TimeBaseService _timeBaseService;

    public CountdownControl(TimeBaseService tbs)
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

        contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        tbText1 = new TextBlock { FontSize = 14, Foreground = Brushes.White, Text = "距离" };
        tbText2 = new TextBlock { FontSize = 14, Foreground = Brushes.White, Text = "" };
        tbText3 = new TextBlock { FontSize = 14, Foreground = Brushes.White, Text = "还有" };
        tbTime = new TextBlock { FontSize = 14, Foreground = Brushes.White, Text = "Loading..." };
        tbText4 = new TextBlock { FontSize = 14, Foreground = Brushes.White, Text = "" };

        contentPanel.Children.Add(tbText1);
        contentPanel.Children.Add(tbText2);
        contentPanel.Children.Add(tbText3);
        contentPanel.Children.Add(tbTime);
        contentPanel.Children.Add(tbText4);

        rootBorder.Child = contentPanel;
        Content = rootBorder;
    }

    private void UpdateText1Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText1, colorStr, fontSize);
    }

    private void UpdateText2Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText2, colorStr, fontSize);
    }

    private void UpdateText3Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText3, colorStr, fontSize);
    }

    private void UpdateTimeStyle(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbTime, colorStr, fontSize);
    }

    private void UpdateText4Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText4, colorStr, fontSize);
    }

    private void UpdateTextBlockStyle(TextBlock tb, string colorStr, double fontSize)
    {
        try
        {
            tb.FontSize = fontSize;
            if (!string.IsNullOrEmpty(colorStr) && colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
            {
                var color = Avalonia.Media.Color.Parse(colorStr);
                tb.Foreground = new SolidColorBrush(color);
            }
        }
        catch
        {
            tb.Foreground = Brushes.White;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        vm = new CountdownViewModel(_timeBaseService, Settings,
            UpdateText1Style, UpdateText2Style, UpdateText3Style, UpdateTimeStyle, UpdateText4Style);
        DataContext = vm;

        UpdateDisplays();

        vm.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(vm.Text1Display):
                    tbText1.Text = vm.Text1Display;
                    break;
                case nameof(vm.Text2Display):
                    tbText2.Text = vm.Text2Display;
                    break;
                case nameof(vm.Text3Display):
                    tbText3.Text = vm.Text3Display;
                    break;
                case nameof(vm.TimeDisplay):
                    tbTime.Text = vm.TimeDisplay;
                    break;
                case nameof(vm.Text4Display):
                    tbText4.Text = vm.Text4Display;
                    break;
                case nameof(vm.IsAllCompleted):
                case nameof(vm.IsEmpty):
                    UpdateDisplays();
                    break;
            }
        };

        UpdateText1Style(Settings.Text1FontColor, Settings.Text1FontSize);
        UpdateText2Style(Settings.Text2FontColor, Settings.Text2FontSize);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3FontSize);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeFontSize);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4FontSize);
    }

    private void UpdateDisplays()
    {
        if (vm.IsEmpty)
        {
            tbText1.Text = "";
            tbText2.Text = "";
            tbText3.Text = "当前无倒计时";
            tbTime.Text = "";
            tbText4.Text = "";
        }
        else if (vm.IsAllCompleted)
        {
            tbText1.Text = "";
            tbText2.Text = "";
            tbText3.Text = "倒计时已结束";
            tbTime.Text = "";
            tbText4.Text = "";
        }
        else
        {
            tbText1.Text = vm.Text1Display;
            tbText2.Text = vm.Text2Display;
            tbText3.Text = vm.Text3Display;
            tbTime.Text = vm.TimeDisplay;
            tbText4.Text = vm.Text4Display;
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        (vm as IDisposable)?.Dispose();
    }
}


