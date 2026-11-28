using System;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using System.Windows.Shapes;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "77889900-1122-3344-5566-778899071122",
    "周期性倒计时（ATI）",
    PackIconKind.Refresh,
    "显示周期性重复的倒计时，支持每小时/每天/每周/每月/每年"
)]
public class PeriodicCountdownControl : ComponentBase<PeriodicCountdownSettings>
{
    private PeriodicCountdownViewModel vm;
    private bool _initCompleted;
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
    private System.Windows.Shapes.Path circleProgressPath;

    public PeriodicCountdownControl(TimeBaseService tbs)
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
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        tbText1 = new TextBlock { Text = "距离", VerticalAlignment = VerticalAlignment.Center };
        tbText2 = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };
        tbText3 = new TextBlock { Text = "还有", VerticalAlignment = VerticalAlignment.Center };
        tbTime = new TextBlock { Text = "Loading...", VerticalAlignment = VerticalAlignment.Center };
        tbText4 = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center };

        contentPanel.Children.Add(tbText1);
        contentPanel.Children.Add(tbText2);
        contentPanel.Children.Add(tbText3);
        contentPanel.Children.Add(tbTime);
        contentPanel.Children.Add(tbText4);

        outerGrid.Children.Add(contentPanel);

        circleProgressRoot = new Grid
        {
            Visibility = Visibility.Collapsed,
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

        circleProgressPath = new System.Windows.Shapes.Path
        {
            Width = 22,
            Height = 22,
            Fill = Brushes.Transparent,
            Stroke = ThemeHelper.GetLightBlueBrush(),

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
            Visibility = Visibility.Collapsed,
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
        UpdateTextBlockStyle(tbText1, colorStr, fontSize, Settings.Text1EnableCustomFontColor);
    }

    private void UpdateText2Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText2, colorStr, fontSize, Settings.Text2EnableCustomFontColor);
    }

    private void UpdateText3Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText3, colorStr, fontSize, Settings.Text3EnableCustomFontColor);
    }

    private void UpdateTimeStyle(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbTime, colorStr, fontSize, Settings.TimeEnableCustomFontColor);
    }

    private void UpdateText4Style(string colorStr, double fontSize)
    {
        UpdateTextBlockStyle(tbText4, colorStr, fontSize, Settings.Text4EnableCustomFontColor);
    }

    private void UpdateTextBlockStyle(TextBlock tb, string colorStr, double fontSize, bool enableCustomColor)
    {
        if (fontSize > 0)
            tb.FontSize = fontSize;
        else
            tb.FontSize = FontFamilyHelper.GetBodyFontSize(tb);
        tb.Foreground = ThemeHelper.GetColorBrush(colorStr, enableCustomColor);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateText1Style(Settings.Text1FontColor, Settings.Text1EnableCustomFontSize ? Settings.Text1FontSize : 0);
        UpdateText2Style(Settings.Text2FontColor, Settings.Text2EnableCustomFontSize ? Settings.Text2FontSize : 0);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3EnableCustomFontSize ? Settings.Text3FontSize : 0);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeEnableCustomFontSize ? Settings.TimeFontSize : 0);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4EnableCustomFontSize ? Settings.Text4FontSize : 0);
        UpdateProgressColors();
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateText1Style(Settings.Text1FontColor, Settings.Text1EnableCustomFontSize ? Settings.Text1FontSize : 0);
        UpdateText2Style(Settings.Text2FontColor, Settings.Text2EnableCustomFontSize ? Settings.Text2FontSize : 0);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3EnableCustomFontSize ? Settings.Text3FontSize : 0);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeEnableCustomFontSize ? Settings.TimeFontSize : 0);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4EnableCustomFontSize ? Settings.Text4FontSize : 0);
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
            var accentBrush = Application.Current?.TryFindResource("PrimaryHueMidBrush") as Brush
                ?? Application.Current?.TryFindResource("MaterialDesign.Brush.Primary") as Brush;
            if (accentBrush != null)
            {
                circleProgressPath.Stroke = accentBrush;
                progressBar.Foreground = accentBrush;
            }
            else
            {
                circleProgressPath.Stroke = ThemeHelper.GetLightBlueBrush();
                progressBar.Foreground = ThemeHelper.GetLightBlueBrush();
            }
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }
        _initCompleted = true;
        ThemeHelper.ThemeChanged += OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;
        Unloaded += OnUnloaded;
        vm = new PeriodicCountdownViewModel(_timeBaseService, Settings,
            UpdateText1Style, UpdateText2Style, UpdateText3Style, UpdateTimeStyle, UpdateText4Style);
        DataContext = vm;

        UpdateDisplays();

        vm.PropertyChanged += OnVmPropertyChanged;

        Settings.PropertyChanged += OnSettingsChanged;

        UpdateText1Style(Settings.Text1FontColor, Settings.Text1EnableCustomFontSize ? Settings.Text1FontSize : 0);
        UpdateText2Style(Settings.Text2FontColor, Settings.Text2EnableCustomFontSize ? Settings.Text2FontSize : 0);
        UpdateText3Style(Settings.Text3FontColor, Settings.Text3EnableCustomFontSize ? Settings.Text3FontSize : 0);
        UpdateTimeStyle(Settings.TimeFontColor, Settings.TimeEnableCustomFontSize ? Settings.TimeFontSize : 0);
        UpdateText4Style(Settings.Text4FontColor, Settings.Text4EnableCustomFontSize ? Settings.Text4FontSize : 0);
        UpdateProgressDisplayMode();
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
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
            // 结束状态由 ViewModel 计算（三段式或回退单行），此处直接呈现
            tbText1.Text = vm.Text1Display;
            tbText2.Text = vm.Text2Display;
            tbText3.Text = vm.Text3Display;
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
                circleProgressRoot.Visibility = Visibility.Collapsed;
                progressBar.Visibility = Visibility.Collapsed;
                break;
            case ProgressDisplayMode.Bar:
                circleProgressRoot.Visibility = Visibility.Collapsed;
                progressBar.Visibility = Visibility.Visible;
                break;
            case ProgressDisplayMode.Ring:
                circleProgressRoot.Visibility = Visibility.Visible;
                progressBar.Visibility = Visibility.Collapsed;
                break;
            case ProgressDisplayMode.Both:
                circleProgressRoot.Visibility = Visibility.Visible;
                progressBar.Visibility = Visibility.Visible;
                break;
        }
    }

    private void UpdateProgressDisplay()
    {
        progressBar.Value = vm.Percent;
        var geometry = PercentToPathGeometry(vm.Percent);
        circleProgressPath.Data = geometry;
        circleProgressBackground.Visibility = geometry != null ? Visibility.Visible : Visibility.Collapsed;
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

        var segments = new PathSegmentCollection
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

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ThemeHelper.ThemeChanged -= OnThemeVariantChanged;
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        (vm as IDisposable)?.Dispose();
    }
}
