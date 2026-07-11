using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AdvancedTimeIsland.ViewModels.Settings;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Views.Controls;

public class FpsChartControl : TemplatedControl, IDisposable
{
    private const double SamplePixelWidth = 2.0;
    private const double LeftPadding = 50.0;
    private const double BottomPadding = 30.0;
    private const double TopPadding = 10.0;
    private const double RightPadding = 10.0;
    private const int RedrawIntervalMs = 200;
    private const double LegendWidth = 100.0;
    private const double ChartHeight = 350.0;
    private const int MaxVisiblePoints = 10000;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 10.0;
    private const double DefaultZoom = 1.0;
    private const double ZoomStep = 1.25;

    private double _zoomLevel = DefaultZoom;

    private double CurrentSamplePixelWidth => SamplePixelWidth * _zoomLevel;

    private Canvas? _canvas;
    private Canvas? _yAxisPanel;
    private ScrollViewer? _scrollViewer;
    private Grid? _rootGrid;
    private StackPanel? _legendPanel;
    private DispatcherTimer? _redrawTimer;
    private List<Polyline>? _polylines;
    private IBrush[]? _seriesColors;
    private string[]? _seriesNames;
    private TextBlock? _fpsValueTextBlock;
    private TextBlock? _maxValueTextBlock;
    private TextBlock? _avgValueTextBlock;
    private TextBlock? _minValueTextBlock;
    private TextBlock? _low1ValueTextBlock;
    private TextBlock? _oneSecondFrameCountValueTextBlock;
    private Border? _tooltipBorder;
    private TextBlock? _tooltipTextBlock;
    private bool _isDisposed;
    private bool _isInitialized;
    private Button? _pauseButton;
    private bool _isPaused;
    private bool _isEffectivelyVisible = true;
    private bool _isDragging;
    private double _lastScreenX;
    private bool _isPinching;
    private int _lastDrawnRecordCount;
    private double _lastYMax;
    private List<TextBlock> _yAxisLabels = new();
    private List<Line> _gridLines = new();
    private List<TextBlock> _timeLabels = new();
    private bool[] _seriesVisible = { true, true, true, true, true, true };
    private TextBox? _refreshRateTextBox;
    private double _initialPinchDistance;
    private double _initialPinchZoom;
    private (IPointer? Pointer, Point Position) _pointer1;
    private (IPointer? Pointer, Point Position) _pointer2;

    public FpsChartControl()
    {
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _seriesNames = new[] { "fps", "max", "avg", "min", "1%low", "1s frm" };
        _seriesColors = new IBrush[]
        {
            new SolidColorBrush(Color.FromRgb(0, 255, 0)),
            new SolidColorBrush(Color.FromRgb(255, 60, 60)),
            new SolidColorBrush(Color.FromRgb(0, 220, 255)),
            new SolidColorBrush(Color.FromRgb(100, 180, 255)),
            new SolidColorBrush(Color.FromRgb(255, 180, 0)),
            new SolidColorBrush(Color.FromRgb(255, 100, 255))
        };

        _polylines = new List<Polyline>();

        _canvas = new Canvas
        {
            Background = ThemeHelper.IsDarkTheme() 
                ? new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)) 
                : new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)),
            Focusable = false
        };
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerExited += OnCanvasPointerLeave;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        _canvas.KeyDown += OnCanvasKeyDown;

        for (int i = 0; i < 6; i++)
        {
            var line = new Polyline
            {
                Stroke = _seriesColors[i],
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            _polylines.Add(line);
            _canvas.Children.Add(line);
        }

        _scrollViewer = new ScrollViewer
        {
            Content = _canvas,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MinHeight = ChartHeight,
            Height = ChartHeight
        };
        _scrollViewer.ScrollChanged += OnScrollChanged;

        _legendPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        for (int i = 0; i < 6; i++)
        {
            var legendItem = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

            var colorBox = new Rectangle
            {
                Width = 12,
                Height = 12,
                Fill = _seriesColors[i]
            };
            legendItem.Children.Add(colorBox);

            var label = new TextBlock
            {
                Text = _seriesNames[i],
                FontSize = 11,
                Foreground = ThemeHelper.GetTextBrush()
            };
            legendItem.Children.Add(label);

            _legendPanel.Children.Add(legendItem);
        }

        var metricsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 8)
        };

        metricsPanel.Children.Add(CreateMetricItem("fps:", out _fpsValueTextBlock));
        metricsPanel.Children.Add(CreateMetricItem("max:", out _maxValueTextBlock));
        metricsPanel.Children.Add(CreateMetricItem("avg:", out _avgValueTextBlock));
        metricsPanel.Children.Add(CreateMetricItem("min:", out _minValueTextBlock));
        metricsPanel.Children.Add(CreateMetricItem("1%low:", out _low1ValueTextBlock));
        metricsPanel.Children.Add(CreateMetricItem("1s frm:", out _oneSecondFrameCountValueTextBlock));

        _pauseButton = new Button
        {
            Content = "暂停采集",
            Width = 80,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _pauseButton.Click += OnPauseButtonClick;
        metricsPanel.Children.Add(_pauseButton);

        var zoomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var zoomInButton = new Button
        {
            Content = "放大",
            Width = 60,
            Height = 24,
            FontSize = 11
        };
        zoomInButton.Click += (s, e) => ZoomIn();
        zoomPanel.Children.Add(zoomInButton);

        var zoomOutButton = new Button
        {
            Content = "缩小",
            Width = 60,
            Height = 24,
            FontSize = 11
        };
        zoomOutButton.Click += (s, e) => ZoomOut();
        zoomPanel.Children.Add(zoomOutButton);

        var zoomResetButton = new Button
        {
            Content = "重置缩放",
            Width = 70,
            Height = 24,
            FontSize = 11
        };
        zoomResetButton.Click += (s, e) => ZoomReset();
        zoomPanel.Children.Add(zoomResetButton);

        metricsPanel.Children.Add(zoomPanel);

        _tooltipBorder = new Border
        {
            Background = ThemeHelper.IsDarkTheme() 
                ? new SolidColorBrush(Color.FromRgb(30, 30, 30)) 
                : new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            BorderBrush = ThemeHelper.GetGrayBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            IsVisible = false,
            ZIndex = 100
        };
        _tooltipTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeHelper.GetTextBrush(),
            TextWrapping = TextWrapping.Wrap
        };
        _tooltipBorder.Child = _tooltipTextBlock;
        _canvas.Children.Add(_tooltipBorder);

        _yAxisPanel = new Canvas
        {
            Width = LeftPadding,
            Height = ChartHeight
        };

        _rootGrid = new Grid
        {
            MinHeight = ChartHeight + 80
        };
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LeftPadding) });
        _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LegendWidth) });

        Grid.SetRow(metricsPanel, 0);
        Grid.SetColumn(metricsPanel, 1);
        _rootGrid.Children.Add(metricsPanel);

        Grid.SetRow(_yAxisPanel, 1);
        Grid.SetColumn(_yAxisPanel, 0);
        _rootGrid.Children.Add(_yAxisPanel);

        Grid.SetRow(_scrollViewer, 1);
        Grid.SetColumn(_scrollViewer, 1);
        Grid.SetRow(_legendPanel, 1);
        Grid.SetColumn(_legendPanel, 2);

        _rootGrid.Children.Add(_scrollViewer);
        _rootGrid.Children.Add(_legendPanel);

        var checkboxPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Margin = new Thickness(10, 8, 10, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        for (int i = 0; i < 6; i++)
        {
            var checkbox = new CheckBox
            {
                Content = _seriesNames[i],
                IsChecked = true,
                FontSize = 11,
                Foreground = ThemeHelper.GetTextBrush()
            };
            var idx = i;
            checkbox.Checked += (s, e) =>
            {
                _seriesVisible[idx] = true;
                _lastDrawnRecordCount = 0;
                Redraw();
            };
            checkbox.Unchecked += (s, e) =>
            {
                _seriesVisible[idx] = false;
                _lastDrawnRecordCount = 0;
                Redraw();
            };
            checkboxPanel.Children.Add(checkbox);
        }

        Grid.SetRow(checkboxPanel, 2);
        Grid.SetColumn(checkboxPanel, 1);
        _rootGrid.Children.Add(checkboxPanel);

        var refreshRatePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(10, 4, 10, 8),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var refreshRateLabel = new TextBlock
        {
            Text = "刷新频率:",
            FontSize = 11,
            Foreground = ThemeHelper.GetSubTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        refreshRatePanel.Children.Add(refreshRateLabel);

        _refreshRateTextBox = new TextBox
        {
            Text = "5",
            Width = 60,
            Height = 24,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _refreshRateTextBox.LostFocus += OnRefreshRateLostFocus;
        refreshRatePanel.Children.Add(_refreshRateTextBox);

        var refreshRateUnit = new TextBlock
        {
            Text = "次/秒",
            FontSize = 11,
            Foreground = ThemeHelper.GetSubTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        refreshRatePanel.Children.Add(refreshRateUnit);

        Grid.SetRow(refreshRatePanel, 3);
        Grid.SetColumn(refreshRatePanel, 1);
        _rootGrid.Children.Add(refreshRatePanel);

        _redrawTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(RedrawIntervalMs),
            DispatcherPriority.Background,
            OnRedrawTick);
    }

    private StackPanel CreateMetricItem(string labelText, out TextBlock valueTextBlock)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var label = new TextBlock
        {
            Text = labelText,
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush()
        };
        valueTextBlock = new TextBlock
        {
            Text = "---",
            FontSize = 12,
            Foreground = ThemeHelper.GetYiBrush()
        };
        panel.Children.Add(label);
        panel.Children.Add(valueTextBlock);
        return panel;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        try
        {
            Initialize();
            _isEffectivelyVisible = IsEffectivelyVisible;
            if (_isEffectivelyVisible)
            {
                FpsSampler.DataUpdated += OnDataUpdated;
                _redrawTimer?.Start();
                DispatcherTimer.RunOnce(Redraw, TimeSpan.FromMilliseconds(50));
            }
        }
        catch (Exception)
        {
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property.Name == "IsEffectivelyVisible")
        {
            _isEffectivelyVisible = (bool)e.NewValue!;
            if (_isEffectivelyVisible)
            {
                FpsSampler.DataUpdated += OnDataUpdated;
                _redrawTimer?.Start();
                DispatcherTimer.RunOnce(Redraw, TimeSpan.FromMilliseconds(50));
            }
            else
            {
                FpsSampler.DataUpdated -= OnDataUpdated;
                _redrawTimer?.Stop();
            }
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        try
        {
            FpsSampler.DataUpdated -= OnDataUpdated;
            _redrawTimer?.Stop();
            if (_canvas != null)
            {
                _canvas.PointerMoved -= OnCanvasPointerMoved;
                _canvas.PointerExited -= OnCanvasPointerLeave;
                _canvas.PointerWheelChanged -= OnCanvasPointerWheelChanged;
                _canvas.KeyDown -= OnCanvasKeyDown;
            }
        }
        catch (Exception)
        {
        }
    }

    private double _lastScrollOffset;
    private DateTime _lastScrollRedrawTime = DateTime.MinValue;
    private const int ScrollRedrawIntervalMs = 50;

    private void OnDataUpdated()
    {
        _lastDrawnRecordCount = 0;
        Redraw();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer == null) return;

        var now = DateTime.Now;
        if ((now - _lastScrollRedrawTime).TotalMilliseconds < ScrollRedrawIntervalMs) return;
        _lastScrollRedrawTime = now;

        var currentOffset = _scrollViewer.Offset.X;
        if (Math.Abs(currentOffset - _lastScrollOffset) > 50)
        {
            _lastScrollOffset = currentOffset;
            _lastDrawnRecordCount = 0;
            Redraw();
        }
    }

    private void OnRedrawTick(object? sender, EventArgs e)
    {
        if (_isPaused) return;
        Redraw();
    }

    public void ForceRedraw()
    {
        Redraw();
    }

    private void Redraw()
    {
        if (_isDisposed || !_isInitialized) return;
        if (!_isEffectivelyVisible) return;
        if (_canvas == null || _scrollViewer == null || _polylines == null) return;

        var records = FpsSampler.Records;
        
        if (records.Count == 0)
        {
            ClearCanvas();
            return;
        }

        UpdateMetrics(records[records.Count - 1]);

        var controlHeight = ChartHeight;
        var chartHeight = controlHeight - TopPadding - BottomPadding;
        if (chartHeight <= 0) chartHeight = 300;

        var visibleStartIndex = 0;
        var visibleEndIndex = records.Count - 1;

        if (_scrollViewer.Bounds.Width > 0)
        {
            var visibleWidth = _scrollViewer.Bounds.Width;
            var offset = _scrollViewer.Offset.X;
            visibleStartIndex = Math.Max(0, (int)(offset / CurrentSamplePixelWidth));
            visibleEndIndex = Math.Min(records.Count - 1, visibleStartIndex + (int)(visibleWidth / CurrentSamplePixelWidth) + 100);
        }

        double yMax = 0;
        for (int i = visibleStartIndex; i <= visibleEndIndex; i++)
        {
            var r = records[i];
            if (r.Fps > yMax) yMax = r.Fps;
            if (r.Max > yMax) yMax = r.Max;
            if (r.Avg > yMax) yMax = r.Avg;
            if (r.OneSecondFrameCount > yMax) yMax = r.OneSecondFrameCount;
        }
        yMax = Math.Ceiling(yMax / 10.0) * 10;
        if (yMax < 10) yMax = 10;

        var canvasWidth = Math.Max(
            _scrollViewer.Bounds.Width > 0 ? _scrollViewer.Bounds.Width : 800,
            records.Count * CurrentSamplePixelWidth + RightPadding);

        _canvas.Width = canvasWidth;
        _canvas.Height = controlHeight;

        bool needsFullRedraw = _lastDrawnRecordCount == 0 || 
                               records.Count - _lastDrawnRecordCount > 1 ||
                               yMax != _lastYMax;

        if (yMax != _lastYMax)
        {
            UpdateYAxisAndGrid(yMax, chartHeight, controlHeight, canvasWidth);
            _lastYMax = yMax;
            needsFullRedraw = true;
        }

        UpdateTimeLabels(records, chartHeight, canvasWidth, visibleStartIndex, visibleEndIndex);

        if (needsFullRedraw)
        {
            for (int seriesIdx = 0; seriesIdx < 6; seriesIdx++)
            {
                if (!_seriesVisible[seriesIdx])
                {
                    _polylines[seriesIdx].Points.Clear();
                    continue;
                }
                
                var points = new List<Point>();
                int visibleCount = visibleEndIndex - visibleStartIndex + 1;
                int step = visibleCount <= MaxVisiblePoints ? 1 : (int)Math.Ceiling((double)visibleCount / MaxVisiblePoints);

                for (int i = visibleStartIndex; i <= visibleEndIndex; i += step)
                {
                    double value = seriesIdx switch
                    {
                        0 => records[i].Fps,
                        1 => records[i].Max,
                        2 => records[i].Avg,
                        3 => records[i].Min,
                        4 => records[i].Low1,
                        5 => records[i].OneSecondFrameCount,
                        _ => 0
                    };
                    var x = i * CurrentSamplePixelWidth;
                    var y = TopPadding + chartHeight - (value / yMax * chartHeight);
                    points.Add(new Point(x, y));
                }
                
                if (visibleCount > MaxVisiblePoints && visibleEndIndex - visibleStartIndex >= step)
                {
                    double lastValue = seriesIdx switch
                    {
                        0 => records[visibleEndIndex].Fps,
                        1 => records[visibleEndIndex].Max,
                        2 => records[visibleEndIndex].Avg,
                        3 => records[visibleEndIndex].Min,
                        4 => records[visibleEndIndex].Low1,
                        5 => records[visibleEndIndex].OneSecondFrameCount,
                        _ => 0
                    };
                    var lastX = visibleEndIndex * CurrentSamplePixelWidth;
                    var lastY = TopPadding + chartHeight - (lastValue / yMax * chartHeight);
                    points.Add(new Point(lastX, lastY));
                }
                
                _polylines[seriesIdx].Points = points;
            }
            _lastDrawnRecordCount = records.Count;
        }
        else
        {
            for (int seriesIdx = 0; seriesIdx < 6; seriesIdx++)
            {
                if (!_seriesVisible[seriesIdx])
                {
                    _polylines[seriesIdx].Points.Clear();
                    continue;
                }
                
                var i = records.Count - 1;
                if (i < visibleStartIndex || i > visibleEndIndex) continue;
                
                double value = seriesIdx switch
                {
                    0 => records[i].Fps,
                    1 => records[i].Max,
                    2 => records[i].Avg,
                    3 => records[i].Min,
                    4 => records[i].Low1,
                    5 => records[i].OneSecondFrameCount,
                    _ => 0
                };
                var x = i * CurrentSamplePixelWidth;
                var y = TopPadding + chartHeight - (value / yMax * chartHeight);
                _polylines[seriesIdx].Points.Add(new Point(x, y));
            }
            _lastDrawnRecordCount = records.Count;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_scrollViewer != null)
            {
                var maxOffset = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;
                var threshold = 200;
                if (_scrollViewer.Offset.X > maxOffset - threshold)
                {
                    _scrollViewer.Offset = new Vector(maxOffset, 0);
                }
            }
        }, DispatcherPriority.Background);
    }

    private void UpdateYAxisAndGrid(double yMax, double chartHeight, double controlHeight, double canvasWidth)
    {
        if (_yAxisPanel != null)
        {
            foreach (var label in _yAxisLabels)
            {
                _yAxisPanel.Children.Remove(label);
            }
            _yAxisLabels.Clear();

            int yStep = (int)yMax / 5;
            if (yStep < 1) yStep = 1;
            for (int y = 0; y <= yMax; y += yStep)
            {
                var yLabel = new TextBlock
                {
                    Text = y.ToString(),
                    FontSize = 10,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    TextAlignment = TextAlignment.Right
                };
                Canvas.SetLeft(yLabel, LeftPadding - 25);
                Canvas.SetTop(yLabel, TopPadding + chartHeight - (y / yMax * chartHeight) - 6);
                _yAxisPanel.Children.Add(yLabel);
                _yAxisLabels.Add(yLabel);
            }
        }

        foreach (var line in _gridLines)
        {
            _canvas?.Children.Remove(line);
        }
        _gridLines.Clear();

        int yStep2 = (int)yMax / 5;
        if (yStep2 < 1) yStep2 = 1;
        for (int y = 0; y <= yMax; y += yStep2)
        {
            var gridLine = new Line
            {
                StartPoint = new Point(0, TopPadding + chartHeight - (y / yMax * chartHeight)),
                EndPoint = new Point(canvasWidth - RightPadding, TopPadding + chartHeight - (y / yMax * chartHeight)),
                Stroke = ThemeHelper.IsDarkTheme() 
                    ? new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) 
                    : new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                StrokeThickness = 0.5
            };
            _canvas?.Children.Add(gridLine);
            _gridLines.Add(gridLine);
        }
    }

    private void UpdateTimeLabels(IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1, double OneSecondFrameCount)> records, double chartHeight, double canvasWidth, int visibleStartIndex, int visibleEndIndex)
    {
        if (_canvas == null) return;

        int labelInterval = (int)(150 / CurrentSamplePixelWidth);
        if (labelInterval < 1) labelInterval = 1;

        int startLabelIndex = visibleStartIndex - (visibleStartIndex % labelInterval);
        if (startLabelIndex < 0) startLabelIndex = 0;
        int requiredCount = (visibleEndIndex - startLabelIndex) / labelInterval + 1;

        while (_timeLabels.Count < requiredCount)
        {
            var timeLabel = new TextBlock
            {
                FontSize = 10,
                Foreground = ThemeHelper.GetSubTextBrush()
            };
            _timeLabels.Add(timeLabel);
            _canvas.Children.Add(timeLabel);
        }

        while (_timeLabels.Count > requiredCount)
        {
            var tb = _timeLabels[^1];
            _canvas.Children.Remove(tb);
            _timeLabels.RemoveAt(_timeLabels.Count - 1);
        }

        int labelIndex = 0;
        for (int i = startLabelIndex; i <= visibleEndIndex; i += labelInterval)
        {
            var x = i * CurrentSamplePixelWidth;
            var timeLabel = _timeLabels[labelIndex];
            timeLabel.Text = records[i].Time.ToString("HH:mm:ss");
            Canvas.SetLeft(timeLabel, x - 20);
            Canvas.SetTop(timeLabel, TopPadding + chartHeight + 5);
            labelIndex++;
        }
    }

    private void OnRefreshRateLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_refreshRateTextBox == null || _redrawTimer == null) return;

        if (double.TryParse(_refreshRateTextBox.Text, out var rate) && rate > 0)
        {
            const double maxRate = 30.0;
            if (rate > maxRate)
            {
                rate = maxRate;
                _refreshRateTextBox.Text = maxRate.ToString();
            }
            var intervalMs = TimeSpan.FromMilliseconds(1000.0 / rate);
            _redrawTimer.Interval = intervalMs;
        }
        else
        {
            _refreshRateTextBox.Text = "5";
            _redrawTimer.Interval = TimeSpan.FromMilliseconds(200);
        }
    }

    private void OnPauseButtonClick(object? sender, RoutedEventArgs e)
    {
        _isPaused = !_isPaused;
        
        if (_isPaused)
        {
            _pauseButton?.SetValue(Button.ContentProperty, "继续采集");
            FpsSampler.Pause();
        }
        else
        {
            _pauseButton?.SetValue(Button.ContentProperty, "暂停采集");
            FpsSampler.Resume();
        }
    }

    private void UpdateMetrics((DateTime Time, double Fps, double Max, double Avg, double Min, double Low1, double OneSecondFrameCount) record)
    {
        UpdateMetricColor(_fpsValueTextBlock, record.Fps);
        UpdateMetricColor(_maxValueTextBlock, record.Max);
        UpdateMetricColor(_avgValueTextBlock, record.Avg);
        UpdateMetricColor(_minValueTextBlock, record.Min);
        UpdateMetricColor(_low1ValueTextBlock, record.Low1);
        UpdateMetricColor(_oneSecondFrameCountValueTextBlock, record.OneSecondFrameCount);

        _fpsValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Fps:F1}");
        _maxValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Max:F1}");
        _avgValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Avg:F1}");
        _minValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Min:F1}");
        _low1ValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Low1:F1}");
        _oneSecondFrameCountValueTextBlock?.SetValue(TextBlock.TextProperty, $"{Math.Round(record.OneSecondFrameCount)}");
    }

    private void UpdateMetricColor(TextBlock? textBlock, double fps)
    {
        if (textBlock == null) return;

        if (fps >= 30)
            textBlock.Foreground = ThemeHelper.GetYiBrush();
        else if (fps >= 20)
            textBlock.Foreground = ThemeHelper.GetOrangeBrush();
        else
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isInitialized || _scrollViewer == null || _canvas == null) return;

        var position = e.GetPosition(_canvas);

        if (_pointer1.Pointer == null)
        {
            _pointer1 = (e.Pointer, position);
        }
        else if (_pointer2.Pointer == null)
        {
            _pointer2 = (e.Pointer, position);
            _isPinching = true;
            _initialPinchDistance = GetDistance(_pointer1.Position, _pointer2.Position);
            _initialPinchZoom = _zoomLevel;
        }

        if (_pointer1.Pointer != null && _pointer2.Pointer != null)
        {
            _isDragging = false;
            return;
        }

        if (e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            var topLevel = TopLevel.GetTopLevel(_canvas);
            if (topLevel != null)
            {
                _lastScreenX = e.GetPosition(topLevel).X;
            }
            else
            {
                _lastScreenX = e.GetCurrentPoint(_canvas).Position.X;
            }
            _canvas.Cursor = new Cursor(StandardCursorType.Hand);
            e.Pointer.Capture(_canvas);
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer1.Pointer == e.Pointer)
        {
            _pointer1 = (null, default);
        }
        else if (_pointer2.Pointer == e.Pointer)
        {
            _pointer2 = (null, default);
        }

        if (_pointer1.Pointer == null && _pointer2.Pointer == null)
        {
            _isPinching = false;
        }

        if (_isDragging)
        {
            _isDragging = false;
            _canvas.Cursor = new Cursor(StandardCursorType.Arrow);
            e.Pointer.Capture(null);
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isInitialized || _canvas == null) return;

        if (_isPinching)
        {
            UpdatePointerPositions(e);
            return;
        }

        if (_isDragging && _scrollViewer != null)
        {
            var topLevel = TopLevel.GetTopLevel(_canvas);
            double currentScreenX;
            if (topLevel != null)
            {
                currentScreenX = e.GetPosition(topLevel).X;
            }
            else
            {
                currentScreenX = e.GetCurrentPoint(_canvas).Position.X;
            }
            var deltaX = _lastScreenX - currentScreenX;
            _lastScreenX = currentScreenX;

            var newOffsetX = _scrollViewer.Offset.X + deltaX;
            var maxOffsetX = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;
            newOffsetX = Math.Max(0, Math.Min(maxOffsetX, newOffsetX));

            _scrollViewer.Offset = new Vector(newOffsetX, 0);
            return;
        }

        if (_tooltipBorder == null || _tooltipTextBlock == null) return;

        var records = FpsSampler.Records;
        if (records.Count == 0) return;

        var position = e.GetPosition(_canvas);
        var chartHeight = ChartHeight - TopPadding - BottomPadding;

        if (position.X < 0 || position.Y < TopPadding || position.Y > TopPadding + chartHeight)
        {
            _tooltipBorder.IsVisible = false;
            return;
        }

        var index = (int)(position.X / CurrentSamplePixelWidth);
        if (index < 0 || index >= records.Count)
        {
            _tooltipBorder.IsVisible = false;
            return;
        }

        var record = records[index];

        _tooltipTextBlock.Text = $"时间: {record.Time:HH:mm:ss.fff}\n" +
                                $"fps: {record.Fps:F1}\n" +
                                $"max: {record.Max:F1}\n" +
                                $"avg: {record.Avg:F1}\n" +
                                $"min: {record.Min:F1}\n" +
                                $"1%low: {record.Low1:F1}\n" +
                                $"1s frm: {Math.Round(record.OneSecondFrameCount)}";

        var tooltipX = position.X + 10;
        var tooltipY = position.Y - 50;

        if (tooltipX + 150 > _canvas.Width)
            tooltipX = _canvas.Width - 160;
        if (tooltipY < 5)
            tooltipY = 5;

        Canvas.SetLeft(_tooltipBorder, tooltipX);
        Canvas.SetTop(_tooltipBorder, tooltipY);
        _tooltipBorder.IsVisible = true;
    }

    private void OnCanvasPointerLeave(object? sender, PointerEventArgs e)
    {
        if (_tooltipBorder != null)
        {
            _tooltipBorder.IsVisible = false;
        }
    }

    private void UpdatePointerPositions(PointerEventArgs e)
    {
        if (_canvas == null) return;

        var currentPoint = e.GetPosition(_canvas);
        
        if (_pointer1.Pointer == e.Pointer)
        {
            _pointer1 = (_pointer1.Pointer, currentPoint);
        }
        else if (_pointer2.Pointer == e.Pointer)
        {
            _pointer2 = (_pointer2.Pointer, currentPoint);
        }

        if (_pointer1.Pointer != null && _pointer2.Pointer != null)
        {
            double currentDistance = GetDistance(_pointer1.Position, _pointer2.Position);
            double scale = currentDistance / _initialPinchDistance;
            double newZoom = _initialPinchZoom * scale;
            newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));
            
            if (Math.Abs(newZoom - _zoomLevel) >= 0.001)
            {
                ApplyZoom(newZoom);
            }
        }
    }

    private double GetDistance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void ClearCanvas()
    {
        if (_canvas == null || _polylines == null) return;

        foreach (var polyline in _polylines)
        {
            polyline.Points.Clear();
        }

        _lastDrawnRecordCount = 0;
        _lastYMax = 0;

        foreach (var label in _yAxisLabels)
        {
            _yAxisPanel?.Children.Remove(label);
        }
        _yAxisLabels.Clear();

        foreach (var line in _gridLines)
        {
            _canvas.Children.Remove(line);
        }
        _gridLines.Clear();

        foreach (var tb in _timeLabels)
        {
            _canvas.Children.Remove(tb);
        }
        _timeLabels.Clear();

        _canvas.Width = _scrollViewer?.Bounds.Width > 0 ? _scrollViewer.Bounds.Width : 800;
        
        if (_fpsValueTextBlock != null)
            _fpsValueTextBlock.Text = "---";
        if (_maxValueTextBlock != null)
            _maxValueTextBlock.Text = "---";
        if (_avgValueTextBlock != null)
            _avgValueTextBlock.Text = "---";
        if (_minValueTextBlock != null)
            _minValueTextBlock.Text = "---";
        if (_low1ValueTextBlock != null)
            _low1ValueTextBlock.Text = "---";
        if (_oneSecondFrameCountValueTextBlock != null)
            _oneSecondFrameCountValueTextBlock.Text = "---";
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!_isInitialized || _scrollViewer == null) return;
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        e.Handled = true;

        // 向上滚动放大，向下滚动缩小
        if (e.Delta.Y > 0)
            ZoomIn();
        else if (e.Delta.Y < 0)
            ZoomOut();
    }

    private void OnCanvasKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isInitialized) return;
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        switch (e.Key)
        {
            case Key.OemPlus or Key.Add:
                e.Handled = true;
                ZoomIn();
                break;
            case Key.OemMinus or Key.Subtract:
                e.Handled = true;
                ZoomOut();
                break;
            case Key.D0 or Key.NumPad0:
                e.Handled = true;
                ZoomReset();
                break;
        }
    }

    private void ZoomIn()
    {
        var newZoom = _zoomLevel * ZoomStep;
        if (newZoom > MaxZoom) newZoom = MaxZoom;
        if (Math.Abs(newZoom - _zoomLevel) < 0.001) return;
        ApplyZoom(newZoom);
    }

    private void ZoomOut()
    {
        var newZoom = _zoomLevel / ZoomStep;
        if (newZoom < MinZoom) newZoom = MinZoom;
        if (Math.Abs(newZoom - _zoomLevel) < 0.001) return;
        ApplyZoom(newZoom);
    }

    private void ZoomReset()
    {
        if (Math.Abs(_zoomLevel - DefaultZoom) < 0.001) return;
        ApplyZoom(DefaultZoom);
    }

    private void ApplyZoom(double newZoom)
    {
        if (_scrollViewer == null || _canvas == null) return;

        // 记录当前视口中心对应的数据索引
        var oldSampleWidth = CurrentSamplePixelWidth;
        var oldOffset = _scrollViewer.Offset.X;
        var viewportWidth = _scrollViewer.Viewport.Width;
        var centerOffset = oldOffset + viewportWidth / 2.0;
        var centerDataIndex = (centerOffset - LeftPadding) / oldSampleWidth;

        _zoomLevel = newZoom;

        // 计算新的偏移量，保持视口中心的数据点不变
        var newSampleWidth = CurrentSamplePixelWidth;
        var newCenterOffset = LeftPadding + centerDataIndex * newSampleWidth;
        var newOffset = newCenterOffset - viewportWidth / 2.0;

        // 强制完全重绘以更新画布尺寸
        _lastDrawnRecordCount = 0;
        Redraw();

        // 更新暂停红线位置
        // 设置新的偏移量
        Dispatcher.UIThread.Post(() =>
        {
            if (_scrollViewer == null) return;
            var maxOffset = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;
            newOffset = Math.Max(0, Math.Min(maxOffset, newOffset));
            _scrollViewer.Offset = new Vector(newOffset, 0);
        }, DispatcherPriority.Background);
    }

    public Control GetContent()
    {
        Initialize();
        return _rootGrid ?? new Border() as Control;
    }

    public void Dispose()
    {
        _isDisposed = true;
        try
        {
            _redrawTimer?.Stop();
            FpsSampler.DataUpdated -= OnDataUpdated;
            if (_canvas != null)
            {
                _canvas.PointerPressed -= OnCanvasPointerPressed;
                _canvas.PointerReleased -= OnCanvasPointerReleased;
                _canvas.PointerMoved -= OnCanvasPointerMoved;
                _canvas.PointerExited -= OnCanvasPointerLeave;
                _canvas.PointerWheelChanged -= OnCanvasPointerWheelChanged;
                _canvas.KeyDown -= OnCanvasKeyDown;
            }
        }
        catch
        {
        }
    }
}