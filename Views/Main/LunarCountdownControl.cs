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
    "44556677-8899-0022-3344-556677889900",
    "农历多倒计时（ATI）",
    "\uE121",
    "显示农历倒计时，自动排序并切换"
)]
public class LunarCountdownControl : ComponentBase<LunarCountdownSettings>
{
    private LunarCountdownViewModel vm;
    private TextBlock tbText1;
    private TextBlock tbName;
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

    public LunarCountdownControl(TimeBaseService tbs)
    {
        _timeBaseService = tbs;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        rootBorder = new Border
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var outerGrid = new Grid();

        contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        tbText1 = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };
        tbName = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };
        tbText3 = new TextBlock { Text = "还有", VerticalAlignment = VerticalAlignment.Center };
        tbTime = new TextBlock { Text = "Loading...", VerticalAlignment = VerticalAlignment.Center };
        tbText4 = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };

        contentPanel.Children.Add(tbText1);
        contentPanel.Children.Add(tbName);
        contentPanel.Children.Add(tbText3);
        contentPanel.Children.Add(tbTime);
        contentPanel.Children.Add(tbText4);

        outerGrid.Children.Add(contentPanel);

        circleProgressRoot = new Panel
        {
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(15, 0, 4, 0)
        };

        circleProgressBackground = new Ellipse
        {
            Width = 21.4,
            Height = 21.4,
            Fill = Brushes.Transparent,
            Stroke = ThemeHelper.GetProgressRingBackgroundBrush(),
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

        progressBar = new ProgressBar
        {
            Maximum = 100,
            Minimum = 0,
            MinWidth = 0,
            IsVisible = false,
            Value = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 4
        };

        outerGrid.Children.Add(progressBar);

        rootBorder.Child = outerGrid;
        Content = rootBorder;
    }

    private void UpdateText1Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText1, colorStr, fontSize);
    }

    private void UpdateNameStyle(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbName, colorStr, fontSize);
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
            Settings.NameFontColor = newColor;
            Settings.Text3FontColor = newColor;
            Settings.TimeFontColor = newColor;
            Settings.Text4FontColor = newColor;
        }
        UpdateText1Style(Settings.Text1FontColor, Settings.Text1FontSize);
        UpdateNameStyle(Settings.NameFontColor, Settings.NameFontSize);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3FontSize);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeFontSize);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4FontSize);
        UpdateProgressColors();
    }

    private void UpdateProgressColors()
    {
        circleProgressBackground.Stroke = ThemeHelper.GetProgressRingBackgroundBrush();
        
        if (Settings.EnableCustomProgressColor)
        {
            circleProgressPath.Stroke = ThemeHelper.GetColorBrush(Settings.ProgressRingColor, true);
            progressBar.Foreground = ThemeHelper.GetColorBrush(Settings.ProgressBarColor, true);
        }
        else
        {
            if (Application.Current?.Styles.TryGetResource("AccentFillColorDefaultBrush", Application.Current.ActualThemeVariant, out var accentBrush) == true && accentBrush is IBrush brush)
            {
                circleProgressPath.Stroke = brush;
                progressBar.Foreground = brush;
            }
            else
            {
                circleProgressPath.Stroke = ThemeHelper.GetLightBlueBrush();
                progressBar.Foreground = ThemeHelper.GetLightBlueBrush();
            }
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        vm = new LunarCountdownViewModel(_timeBaseService, Settings,
            UpdateText1Style, UpdateNameStyle, UpdateText3Style, UpdateTimeStyle, UpdateText4Style);
        DataContext = vm;

        UpdateDisplays();

        vm.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(vm.Text1Display):
                    tbText1.Text = vm.Text1Display;
                    break;
                case nameof(vm.NameDisplay):
                    tbName.Text = vm.NameDisplay;
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

        Settings.PropertyChanged += OnSettingsChanged;

        UpdateText1Style(Settings.Text1FontColor, Settings.Text1FontSize);
        UpdateNameStyle(Settings.NameFontColor, Settings.NameFontSize);
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
            tbName.Text = "";
            tbText3.Text = "当前无农历倒计时";
            tbTime.Text = "";
            tbText4.Text = "";
        }
        else if (vm.IsAllCompleted)
        {
            tbText1.Text = "";
            tbName.Text = "";
            tbText3.Text = "农历倒计时已结束";
            tbTime.Text = "";
            tbText4.Text = "";
        }
        else
        {
            tbText1.Text = vm.Text1Display;
            tbName.Text = vm.NameDisplay;
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

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.ProgressDisplayMode))
        {
            UpdateProgressDisplayMode();
        }
        else if (e.PropertyName == nameof(Settings.EnableCustomProgressColor) ||
                 e.PropertyName == nameof(Settings.ProgressBarColor) ||
                 e.PropertyName == nameof(Settings.ProgressRingColor))
        {
            UpdateProgressColors();
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



