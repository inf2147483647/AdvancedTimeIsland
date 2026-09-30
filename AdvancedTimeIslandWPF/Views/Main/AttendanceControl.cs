using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.ViewModels.Main;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 总在校时间统计（ATI）主界面组件：
/// 以五段独立文案 + 进度条 / 进度环展示学期内的在校天数、时长、进度与剩余量。
/// 五段文案分别为概要、累计时长、进度百分比、剩余天数、剩余时长，每段可单独设置字体样式。
/// </summary>
[ComponentInfo(
    "99aabbcc-0001-2233-4455-66778899aa03",
    "总在校时间统计（ATI）",
    PackIconKind.CalendarCheck,
    "统计学期内的在校天数与时长，自动排除周末、法定节假日与寒暑假"
)]
public class AttendanceControl : ComponentBase<AttendanceSettings>
{
    private const double RingSize = 22;
    private const double RingStrokeThickness = 3;
    private const int SegmentCount = 5;

    /// <summary>相邻两段文案之间的分隔符，套用前一段的字体样式渲染。</summary>
    private const string SegmentSeparator = " ";

    /// <summary>
    /// 相邻子元素之间的等效间距。WPF 的 StackPanel 没有 Spacing 属性，
    /// 因此改由每个子元素各留一半的左右外边距来复现 Avalonia 版 Spacing = 6 的效果。
    /// </summary>
    private const double SegmentSpacing = 6;

    /// <summary>分割线的线长与线宽，对齐 ClassIsland「课程表」组件的课程分隔线。</summary>
    private const double DividerHeight = 25;
    private const double DividerStrokeThickness = 2;

    /// <summary>
    /// 分割线两侧的间距。与子元素自身的左右外边距（<see cref="SegmentSpacing"/> 的一半）叠加后，
    /// 两段文案之间约为 20px，与原 Avalonia 版一致。
    /// </summary>
    private const double DividerMargin = 7;

    private AttendanceViewModel? vm;
    private readonly TimeBaseService _timeBaseService;
    private readonly AttendanceCalendarService _calendarService;

    private readonly TextBlock[] _segmentTexts = new TextBlock[SegmentCount];
    private TextStyleSettings[]? _textStyles;

    private readonly StackPanel _textHost;
    private readonly Panel _ringRoot;
    private readonly Ellipse _ringBackground;
    private readonly Path _ringPath;
    private readonly ProgressBar _progressBar;
    private bool _initCompleted;
    private bool _themeHooked;

    public AttendanceControl(TimeBaseService tbs, AttendanceCalendarService calendar)
    {
        _timeBaseService = tbs;
        _calendarService = calendar;

        var rootBorder = new Border
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var dock = new DockPanel { LastChildFill = true };

        _progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 4,
            MinWidth = 0,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        DockPanel.SetDock(_progressBar, Dock.Bottom);
        dock.Children.Add(_progressBar);

        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _ringRoot = new Grid
        {
            Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
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

        _ringPath = new Path
        {
            Width = RingSize,
            Height = RingSize,
            Fill = Brushes.Transparent,
            Stroke = ThemeHelper.GetLightBlueBrush(),
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeThickness = 3.1,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _ringRoot.Children.Add(_ringBackground);
        _ringRoot.Children.Add(_ringPath);
        contentPanel.Children.Add(_ringRoot);

        _textHost = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        for (var i = 0; i < SegmentCount; i++)
        {
            _segmentTexts[i] = new TextBlock
            {
                Text = string.Empty,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap
            };
        }
        contentPanel.Children.Add(_textHost);

        dock.Children.Add(contentPanel);

        rootBorder.Child = dock;
        Content = rootBorder;
    }

    // ==================== 文案渲染 ====================

    /// <summary>
    /// 重建文本行：按可见性依次排列各段文案，并在相邻两段之间插入分隔符。
    /// 分隔符套用其前一段的字体样式，避免与自定义字号不匹配。
    /// </summary>
    private void RebuildTextRow()
    {
        _textHost.Children.Clear();
        if (vm == null || _textStyles == null)
        {
            return;
        }

        var previousIndex = -1;
        for (var i = 0; i < SegmentCount; i++)
        {
            var segment = (AttendanceTextSegment)i;
            if (!vm.IsSegmentVisible(segment))
            {
                continue;
            }

            if (previousIndex >= 0)
            {
                _textHost.Children.Add(Settings.ShowSegmentDivider
                    ? CreateDivider()
                    : CreateSeparator(previousIndex));
            }

            var textBlock = _segmentTexts[i];
            textBlock.Text = vm.GetSegmentText(segment);
            textBlock.Visibility = Visibility.Visible;
            textBlock.Margin = CreateSegmentMargin();
            ApplyTextStyle(textBlock, _textStyles[i]);
            _textHost.Children.Add(textBlock);
            previousIndex = i;
        }
    }

    /// <summary>创建段落子元素的左右外边距，模拟 Avalonia 版 StackPanel 的等距排列。</summary>
    private static Thickness CreateSegmentMargin() =>
        new(SegmentSpacing / 2, 0, SegmentSpacing / 2, 0);

    /// <summary>创建空格分隔符，套用其前一段的字体样式，避免与自定义字号不匹配。</summary>
    private TextBlock CreateSeparator(int previousIndex)
    {
        var separator = new TextBlock
        {
            Text = SegmentSeparator,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            Margin = CreateSegmentMargin()
        };
        ApplyTextStyle(separator, _textStyles![previousIndex]);
        return separator;
    }

    /// <summary>
    /// 创建段落间的竖线分割线，样式对齐 ClassIsland「课程表」组件的课程分隔线：
    /// 2px 线宽、25px 线长、次要文字色、80% 不透明度。
    /// 与宿主原生分割线组件一样用固定宽度的容器包裹，避免 0 宽度几何导致描边被挤出布局槽。
    /// </summary>
    private static FrameworkElement CreateDivider()
    {
        var line = new Line
        {
            X1 = 0,
            Y1 = 0,
            X2 = 0,
            Y2 = DividerHeight,
            Stroke = GetDividerBrush(),
            StrokeThickness = DividerStrokeThickness,
            Opacity = 0.8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var root = new Grid
        {
            Width = DividerStrokeThickness,
            Margin = new Thickness(DividerMargin, 0, DividerMargin, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(line);
        return root;
    }

    /// <summary>分割线画刷：优先取宿主主题的次要文字色，取不到时回退到本插件的分隔线颜色。</summary>
    private static Brush GetDividerBrush()
    {
        if (Application.Current?.TryFindResource("MaterialDesignBodyLight") is Brush brush)
        {
            return brush;
        }
        return ThemeHelper.GetSeparatorBrush();
    }

    /// <summary>按段落的字体样式渲染文本块；未开启自定义的项沿用宿主默认样式。</summary>
    private static void ApplyTextStyle(TextBlock textBlock, TextStyleSettings style)
    {
        textBlock.FontSize = style.EnableCustomFontSize && style.FontSize > 0
            ? style.FontSize
            : FontFamilyHelper.GetBodyFontSize(textBlock);

        textBlock.Foreground = ThemeHelper.GetColorBrush(style.FontColor, style.EnableCustomFontColor);

        if (style.EnableCustomFontFamily)
        {
            textBlock.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(style.FontFamily);
        }
        else
        {
            textBlock.ClearValue(TextBlock.FontFamilyProperty);
        }

        if (style.EnableCustomFontWeight)
        {
            textBlock.FontWeight = FontFamilyHelper.GetFontWeightFromString(style.FontWeight);
        }
        else
        {
            textBlock.ClearValue(TextBlock.FontWeightProperty);
        }
    }

    /// <summary>重新绑定各段字体样式对象（设置对象被整体替换时调用）。</summary>
    private void RefreshTextStyles()
    {
        if (_textStyles != null)
        {
            foreach (var style in _textStyles)
            {
                style.PropertyChanged -= OnTextStyleChanged;
            }
        }

        var styles = new TextStyleSettings[SegmentCount];
        for (var i = 0; i < SegmentCount; i++)
        {
            styles[i] = Settings.GetStyle((AttendanceTextSegment)i);
            styles[i].PropertyChanged += OnTextStyleChanged;
        }
        _textStyles = styles;

        RebuildTextRow();
    }

    // ==================== 进度渲染 ====================

    private void UpdateProgressColors()
    {
        _ringBackground.Stroke = ThemeHelper.GetProgressRingBackgroundBrush();

        var accentBrush = Application.Current?.TryFindResource("PrimaryHueMidBrush") as Brush
            ?? Application.Current?.TryFindResource("MaterialDesign.Brush.Primary") as Brush;

        if (accentBrush != null)
        {
            _ringPath.Stroke = accentBrush;
            _progressBar.Foreground = accentBrush;
        }
        else
        {
            _ringPath.Stroke = ThemeHelper.GetLightBlueBrush();
            _progressBar.Foreground = ThemeHelper.GetLightBlueBrush();
        }
    }

    private void UpdateProgressDisplayMode()
    {
        if (Settings == null)
        {
            return;
        }

        switch (Settings.ProgressDisplayMode)
        {
            case ProgressDisplayMode.None:
                _ringRoot.Visibility = Visibility.Collapsed;
                _progressBar.Visibility = Visibility.Collapsed;
                break;
            case ProgressDisplayMode.Bar:
                _ringRoot.Visibility = Visibility.Collapsed;
                _progressBar.Visibility = Visibility.Visible;
                break;
            case ProgressDisplayMode.Ring:
                _ringRoot.Visibility = Visibility.Visible;
                _progressBar.Visibility = Visibility.Collapsed;
                break;
            case ProgressDisplayMode.Both:
                _ringRoot.Visibility = Visibility.Visible;
                _progressBar.Visibility = Visibility.Visible;
                break;
        }
    }

    private void UpdateProgressDisplay()
    {
        if (vm == null)
        {
            return;
        }

        var available = vm.IsProgressAvailable;
        _progressBar.Value = vm.Progress;

        var geometry = available
            ? CreateArcGeometry(vm.Progress, RingSize, RingStrokeThickness)
            : null;
        _ringPath.Data = geometry;
        _ringBackground.Visibility = geometry != null ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 把进度百分比转换为圆弧几何（WPF 版在原 Avalonia 版 CircularProgressHelper 的位置
    /// 于组件内部直接实现弧线绘制，避免新增公共 Helper 类）。
    /// </summary>
    private static Geometry? CreateArcGeometry(double percentage, double size, double strokeThickness)
    {
        if (percentage <= 0)
        {
            return null;
        }

        // 100% 时退化为接近整圆，否则起止点重合会导致弧线不可见。
        if (percentage >= 100)
        {
            percentage = 99.9999;
        }

        var radius = (size / 2) - (strokeThickness / 2);
        var center = new Point(size / 2, size / 2);
        var angle = percentage / 100 * 360;
        var startPoint = new Point(center.X, center.Y - radius);
        var angleRad = Math.PI / 180.0 * (angle - 90);
        var endPoint = new Point(
            center.X + radius * Math.Cos(angleRad),
            center.Y + radius * Math.Sin(angleRad));

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

    // ==================== 事件处理 ====================

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        RebuildTextRow();
        UpdateProgressColors();
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e) => RebuildTextRow();

    private void OnTextStyleChanged(object? sender, PropertyChangedEventArgs e) => RebuildTextRow();

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    /// <summary>
    /// 初始化刷新链路。OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，
    /// 需延迟到 Loaded 后再初始化（WPF 版用 Loaded/Unloaded 取代 Avalonia 的 OnAttachedToVisualTree）。
    /// </summary>
    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }

        _initCompleted = true;
        Unloaded += OnUnloaded;

        EnsureViewModel();

        // 主界面重建时组件实例可能被复用，挂载后需要重新创建已释放的视图模型，否则统计不再刷新。
        Loaded += OnLoaded;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => EnsureViewModel();

    /// <summary>创建视图模型并接上刷新链路；已存在时不做任何事。</summary>
    private void EnsureViewModel()
    {
        if (vm != null)
        {
            return;
        }

        // 主题与全局字号订阅跟随视图模型的生命周期，保证组件卸载后再挂载仍能响应主题切换。
        if (!_themeHooked)
        {
            ThemeHelper.ThemeChanged += OnThemeVariantChanged;
            FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;
            _themeHooked = true;
        }

        RefreshTextStyles();

        vm = new AttendanceViewModel(_timeBaseService, Settings, _calendarService);
        DataContext = vm;

        // 先接上属性变更，再执行初始渲染：即使后续某个渲染步骤抛异常，
        // 组件也仍能响应视图模型的刷新，不会永久停留在初始画面。
        vm.PropertyChanged += OnVmPropertyChanged;
        Settings.PropertyChanged += OnSettingsChanged;

        RebuildTextRow();
        UpdateProgressDisplay();
        UpdateProgressColors();
        UpdateProgressDisplayMode();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case AttendanceViewModel.SegmentsPropertyName:
                RebuildTextRow();
                break;
            case nameof(AttendanceViewModel.Progress):
            case nameof(AttendanceViewModel.IsProgressAvailable):
                UpdateProgressDisplay();
                break;
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AttendanceSettings.ProgressDisplayMode):
                UpdateProgressDisplayMode();
                break;
            case nameof(AttendanceSettings.ShowSegmentDivider):
                RebuildTextRow();
                break;
            case nameof(AttendanceSettings.SummaryStyle):
            case nameof(AttendanceSettings.HoursStyle):
            case nameof(AttendanceSettings.ProgressStyle):
            case nameof(AttendanceSettings.RemainingDaysStyle):
            case nameof(AttendanceSettings.RemainingHoursStyle):
                RefreshTextStyles();
                break;
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_themeHooked)
        {
            ThemeHelper.ThemeChanged -= OnThemeVariantChanged;
            FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
            _themeHooked = false;
        }
        Settings.PropertyChanged -= OnSettingsChanged;

        if (_textStyles != null)
        {
            foreach (var style in _textStyles)
            {
                style.PropertyChanged -= OnTextStyleChanged;
            }
            _textStyles = null;
        }

        if (vm != null)
        {
            vm.PropertyChanged -= OnVmPropertyChanged;
            vm.Dispose();
            vm = null;
        }
    }
}
