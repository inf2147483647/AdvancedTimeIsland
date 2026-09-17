using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.ViewModels.Main;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 总在校时间统计（ATI）主界面组件：
/// 以文字 + 进度条 / 进度环展示学期内的在校天数、时长、进度与剩余量。
/// </summary>
[ComponentInfo(
    "99aabbcc-0001-2233-4455-66778899aa03",
    "总在校时间统计（ATI）",
    "\uE126",
    "统计学期内的在校天数与时长，自动排除周末、法定节假日与寒暑假"
)]
public class AttendanceControl : ComponentBase<AttendanceSettings>
{
    private const double RingSize = 22;
    private const double RingStrokeThickness = 3;

    private AttendanceViewModel vm;
    private readonly TimeBaseService _timeBaseService;
    private readonly AttendanceCalendarService _calendarService;

    private TextBlock _mainText;
    private TextBlock _subText;
    private Panel _ringRoot;
    private Ellipse _ringBackground;
    private Avalonia.Controls.Shapes.Path _ringPath;
    private ProgressBar _progressBar;

    public AttendanceControl(TimeBaseService tbs, AttendanceCalendarService calendar)
    {
        _timeBaseService = tbs;
        _calendarService = calendar;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var rootBorder = new Border
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var dock = new DockPanel();

        _progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 4,
            MinWidth = 0,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        DockPanel.SetDock(_progressBar, Dock.Bottom);
        dock.Children.Add(_progressBar);

        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _ringRoot = new Panel
        {
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _ringBackground = new Ellipse
        {
            Width = RingSize - 0.6,
            Height = RingSize - 0.6,
            Fill = Brushes.Transparent,
            Stroke = ThemeHelper.GetProgressRingBackgroundBrush(),
            StrokeThickness = 2.6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _ringPath = new Avalonia.Controls.Shapes.Path
        {
            Width = RingSize,
            Height = RingSize,
            Fill = Brushes.Transparent,
            Stroke = ThemeHelper.GetLightBlueBrush(),
            StrokeLineCap = PenLineCap.Round,
            StrokeThickness = 3.1,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _ringRoot.Children.Add(_ringBackground);
        _ringRoot.Children.Add(_ringPath);
        contentPanel.Children.Add(_ringRoot);

        var textStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };

        _mainText = new TextBlock
        {
            Text = "...",
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        };
        _subText = new TextBlock
        {
            Text = string.Empty,
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        };
        textStack.Children.Add(_mainText);
        textStack.Children.Add(_subText);
        contentPanel.Children.Add(textStack);

        dock.Children.Add(contentPanel);

        rootBorder.Child = dock;
        Content = rootBorder;
    }

    private void UpdateFontStyles()
    {
        UpdateTextBlockStyle(_mainText, Settings.FontColor);
        UpdateTextBlockStyle(_subText, Settings.FontColor);
    }

    private void UpdateTextBlockStyle(TextBlock tb, string colorStr)
    {
        tb.FontSize = Settings.EnableCustomFontSize && Settings.FontSize > 0
            ? Settings.FontSize
            : FontFamilyHelper.GetBodyFontSize(tb);

        tb.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);

        if (Settings.EnableCustomFontFamily)
        {
            tb.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.FontFamily);
        }
        else
        {
            tb.ClearValue(TextBlock.FontFamilyProperty);
        }

        if (Settings.EnableCustomFontWeight)
        {
            tb.FontWeight = FontFamilyHelper.GetFontWeightFromString(Settings.FontWeight);
        }
        else
        {
            tb.ClearValue(TextBlock.FontWeightProperty);
        }
    }

    private void UpdateProgressColors()
    {
        _ringBackground.Stroke = ThemeHelper.GetProgressRingBackgroundBrush();

        if (Application.Current?.Styles.TryGetResource("AccentFillColorDefaultBrush",
                Application.Current.ActualThemeVariant, out var accentBrush) == true && accentBrush is IBrush brush)
        {
            _ringPath.Stroke = brush;
            _progressBar.Foreground = brush;
        }
        else
        {
            _ringPath.Stroke = ThemeHelper.GetLightBlueBrush();
            _progressBar.Foreground = ThemeHelper.GetLightBlueBrush();
        }
    }

    private void UpdateProgressDisplayMode()
    {
        switch (Settings.ProgressDisplayMode)
        {
            case ProgressDisplayMode.None:
                _ringRoot.IsVisible = false;
                _progressBar.IsVisible = false;
                break;
            case ProgressDisplayMode.Bar:
                _ringRoot.IsVisible = false;
                _progressBar.IsVisible = true;
                break;
            case ProgressDisplayMode.Ring:
                _ringRoot.IsVisible = true;
                _progressBar.IsVisible = false;
                break;
            case ProgressDisplayMode.Both:
                _ringRoot.IsVisible = true;
                _progressBar.IsVisible = true;
                break;
        }
    }

    private void UpdateProgressDisplay()
    {
        var available = vm.IsProgressAvailable;
        _progressBar.Value = vm.Progress;

        var geometry = available
            ? CircularProgressHelper.CreateArcGeometry(vm.Progress, RingSize, RingStrokeThickness)
            : null;
        _ringPath.Data = geometry;
        _ringBackground.IsVisible = geometry != null;
    }

    private void UpdateTexts()
    {
        _mainText.Text = vm.DisplayText;
        _subText.Text = vm.SubText;
        _subText.IsVisible = !string.IsNullOrEmpty(vm.SubText);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateFontStyles();
        UpdateProgressColors();
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e) => UpdateFontStyles();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 防御重复初始化：先释放旧 VM 并解除其事件订阅，避免泄漏。
        if (vm != null)
        {
            vm.PropertyChanged -= OnVmPropertyChanged;
            vm.Dispose();
        }

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;

        vm = new AttendanceViewModel(_timeBaseService, Settings, _calendarService);
        DataContext = vm;

        UpdateTexts();
        UpdateProgressDisplay();
        UpdateFontStyles();
        UpdateProgressColors();
        UpdateProgressDisplayMode();

        vm.PropertyChanged += OnVmPropertyChanged;
        Settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(vm.DisplayText):
            case nameof(vm.SubText):
                UpdateTexts();
                break;
            case nameof(vm.Progress):
            case nameof(vm.IsProgressAvailable):
                UpdateProgressDisplay();
                break;
        }
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.ProgressDisplayMode):
                UpdateProgressDisplayMode();
                break;
            case nameof(Settings.FontSize):
            case nameof(Settings.FontColor):
            case nameof(Settings.FontFamily):
            case nameof(Settings.FontWeight):
            case nameof(Settings.EnableCustomFontSize):
            case nameof(Settings.EnableCustomFontColor):
            case nameof(Settings.EnableCustomFontFamily):
            case nameof(Settings.EnableCustomFontWeight):
                UpdateFontStyles();
                break;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        if (vm != null)
        {
            vm.PropertyChanged -= OnVmPropertyChanged;
            vm.Dispose();
            vm = null;
        }
    }
}