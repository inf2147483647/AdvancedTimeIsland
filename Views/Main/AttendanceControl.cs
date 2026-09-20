using System;
using System.ComponentModel;
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
/// 以五段独立文案 + 进度条 / 进度环展示学期内的在校天数、时长、进度与剩余量。
/// 五段文案分别为概要、累计时长、进度百分比、剩余天数、剩余时长，每段可单独设置字体样式。
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
    private const int SegmentCount = 5;

    /// <summary>相邻两段文案之间的分隔符，套用前一段的字体样式渲染。</summary>
    private const string SegmentSeparator = " ";

    /// <summary>分割线的线长与线宽，对齐 ClassIsland「课程表」组件的课程分隔线。</summary>
    private const double DividerHeight = 25;
    private const double DividerStrokeThickness = 2;

    /// <summary>分割线两侧的间距，与线本身的间距叠加后使两段文案间约为 20px。</summary>
    private const double DividerMargin = 4;

    private AttendanceViewModel vm;
    private readonly TimeBaseService _timeBaseService;
    private readonly AttendanceCalendarService _calendarService;

    private readonly TextBlock[] _segmentTexts = new TextBlock[SegmentCount];
    private TextStyleSettings[]? _textStyles;

    private StackPanel _textHost;
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

        _textHost = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        for (var i = 0; i < SegmentCount; i++)
        {
            _segmentTexts[i] = new TextBlock
            {
                Text = string.Empty,
                IsVisible = false,
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
            textBlock.IsVisible = true;
            ApplyTextStyle(textBlock, _textStyles[i]);
            _textHost.Children.Add(textBlock);
            previousIndex = i;
        }
    }

    /// <summary>创建空格分隔符，套用其前一段的字体样式，避免与自定义字号不匹配。</summary>
    private TextBlock CreateSeparator(int previousIndex)
    {
        var separator = new TextBlock
        {
            Text = SegmentSeparator,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        };
        ApplyTextStyle(separator, _textStyles![previousIndex]);
        return separator;
    }

    /// <summary>
    /// 创建段落间的竖线分割线，样式对齐 ClassIsland「课程表」组件的课程分隔线：
    /// 2px 线宽、25px 线长、次要文字色、80% 不透明度。
    /// 与宿主原生分割线组件一样用固定宽度的容器包裹，避免 0 宽度几何导致描边被挤出布局槽。
    /// </summary>
    private static Control CreateDivider()
    {
        var line = new Line
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, DividerHeight),
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
    private static IBrush GetDividerBrush()
    {
        if (Application.Current?.Styles.TryGetResource("TextFillColorSecondaryBrush",
                Application.Current.ActualThemeVariant, out var resource) == true && resource is IBrush brush)
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

    // ==================== 事件处理 ====================

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        RebuildTextRow();
        UpdateProgressColors();
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e) => RebuildTextRow();

    private void OnTextStyleChanged(object? sender, PropertyChangedEventArgs e) => RebuildTextRow();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;

        EnsureViewModel();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // 主界面重建时组件可能先分离再挂载，此时视图模型已被释放，必须重新创建，否则统计不再刷新。
        EnsureViewModel();
    }

    /// <summary>创建视图模型并接上刷新链路；已存在时不做任何事。</summary>
    private void EnsureViewModel()
    {
        if (vm != null)
        {
            return;
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
            case nameof(vm.Progress):
            case nameof(vm.IsProgressAvailable):
                UpdateProgressDisplay();
                break;
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.ProgressDisplayMode):
                UpdateProgressDisplayMode();
                break;
            case nameof(Settings.ShowSegmentDivider):
                RebuildTextRow();
                break;
            case nameof(Settings.SummaryStyle):
            case nameof(Settings.HoursStyle):
            case nameof(Settings.ProgressStyle):
            case nameof(Settings.RemainingDaysStyle):
            case nameof(Settings.RemainingHoursStyle):
                RefreshTextStyles();
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