using System;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
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
    private ProgressBar progressBar;
    private Panel circleProgressRoot;
    private Ellipse circleProgressBackground;
    private Avalonia.Controls.Shapes.Path circleProgressPath;

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

        var outerPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        tbText1 = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush(), Text = "距离" };
        tbText2 = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush(), Text = "" };
        tbText3 = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush(), Text = "还有" };
        tbTime = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush(), Text = "Loading..." };
        tbText4 = new TextBlock { FontSize = 14, Foreground = ThemeHelper.GetTextBrush(), Text = "" };

        contentPanel.Children.Add(tbText1);
        contentPanel.Children.Add(tbText2);
        contentPanel.Children.Add(tbText3);
        contentPanel.Children.Add(tbTime);
        contentPanel.Children.Add(tbText4);

        outerPanel.Children.Add(contentPanel);

        circleProgressRoot = new Panel
        {
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        };

        circleProgressBackground = new Ellipse
        {
            Width = 21.4,
            Height = 21.4,
            Fill = Brushes.Transparent,
            Stroke = ThemeHelper.GetTextBrush(),
            StrokeThickness = 2.6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        circleProgressPath = new Avalonia.Controls.Shapes.Path
        {
            Width = 22,
            Height = 22,
            Fill = Brushes.Transparent,
            Stroke = ThemeHelper.GetLightBlueBrush(),
            StrokeLineCap = PenLineCap.Round,
            StrokeThickness = 3.1,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        circleProgressRoot.Children.Add(circleProgressBackground);
        circleProgressRoot.Children.Add(circleProgressPath);

        contentPanel.Children.Insert(0, circleProgressRoot);

        var progressContainer = new Grid
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 4
        };

        progressBar = new ProgressBar
        {
            Maximum = 100,
            Minimum = 0,
            MinWidth = 0,
            IsVisible = false,
            Value = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        progressContainer.Children.Add(progressBar);
        outerPanel.Children.Add(progressContainer);

        rootBorder.Child = outerPanel;
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
        tb.FontSize = fontSize;
        tb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomColorAndFont);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!Settings.EnableCustomColorAndFont)
        {
            var newColor = ThemeHelper.GetThemeAwareTextColor();
            Settings.Text1FontColor = newColor;
            Settings.Text2FontColor = newColor;
            Settings.Text3FontColor = newColor;
            Settings.TimeFontColor = newColor;
            Settings.Text4FontColor = newColor;
        }
        UpdateText1Style(Settings.Text1FontColor, Settings.Text1FontSize);
        UpdateText2Style(Settings.Text2FontColor, Settings.Text2FontSize);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3FontSize);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeFontSize);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4FontSize);
        UpdateProgressColors();
    }

    private void UpdateProgressColors()
    {
        circleProgressBackground.Stroke = ThemeHelper.GetTextBrush();
        circleProgressPath.Stroke = ThemeHelper.GetLightBlueBrush();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
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
                case nameof(vm.Percent):
                    UpdateProgressDisplay();
                    break;
                case nameof(vm.IsAllCompleted):
                case nameof(vm.IsEmpty):
                    UpdateDisplays();
                    break;
            }
        };

        Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Settings.ProgressDisplayMode))
            {
                UpdateProgressDisplayMode();
            }
        };

        UpdateText1Style(Settings.Text1FontColor, Settings.Text1FontSize);
        UpdateText2Style(Settings.Text2FontColor, Settings.Text2FontSize);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3FontSize);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeFontSize);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4FontSize);
        UpdateProgressDisplayMode();
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

    private void UpdateProgressDisplayMode()
    {
        switch (Settings.ProgressDisplayMode)
        {
            case ProgressDisplayMode.None:
                circleProgressRoot.IsVisible = false;
                progressBar.IsVisible = false;
                break;
            case ProgressDisplayMode.Bar:
                circleProgressRoot.IsVisible = false;
                progressBar.IsVisible = true;
                break;
            case ProgressDisplayMode.Ring:
                circleProgressRoot.IsVisible = true;
                progressBar.IsVisible = false;
                break;
            case ProgressDisplayMode.Both:
                circleProgressRoot.IsVisible = true;
                progressBar.IsVisible = true;
                break;
        }
    }

    private void UpdateProgressDisplay()
    {
        progressBar.Value = vm.Percent;
        var geometry = PercentToPathGeometry(vm.Percent);
        circleProgressPath.Data = geometry;
        circleProgressBackground.IsVisible = geometry != null;
    }

    private Geometry PercentToPathGeometry(double percentage)
    {
        const double width = 22;
        const double height = 22;
        const double strokeThickness = 3;

        var radius = (width / 2) - (strokeThickness / 2);
        var center = new Point(width / 2, height / 2);

        if (percentage >= 100) percentage = 99.9999;
        if (percentage <= 0) return null;

        var angle = (percentage / 100) * 360;

        var startPoint = new Point(center.X, center.Y - radius);

        var angleRad = (Math.PI / 180.0) * (angle - 90);
        var endPoint = new Point(
            center.X + radius * Math.Cos(angleRad),
            center.Y + radius * Math.Sin(angleRad)
        );

        var segments = new PathSegments
        {
            new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = angle > 180,
                SweepDirection = SweepDirection.Clockwise,
                IsStroked = true
            }
        };

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            Segments = segments,
            IsClosed = false
        };

        var geometry = new PathGeometry();
        geometry.Figures?.Add(figure);

        return geometry;
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
        if (e.PropertyName == nameof(Settings.ProgressDisplayMode))
        {
            UpdateProgressDisplayMode();
        }
    }
}


