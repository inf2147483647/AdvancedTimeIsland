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
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Views.Controls;

public class FpsChartAnalysisControl : TemplatedControl, IDisposable
{
    private const double SamplePixelWidth = 2.0;
    private const double LeftPadding = 50.0;
    private const double BottomPadding = 30.0;
    private const double TopPadding = 10.0;
    private const double RightPadding = 10.0;
    private const double LegendWidth = 100.0;
    private const double ChartHeight = 350.0;
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
    private List<Polyline>? _polylines;
    private IBrush[]? _seriesColors;
    private string[]? _seriesNames;
    private Border? _tooltipBorder;
    private TextBlock? _tooltipTextBlock;
    private bool _isDisposed;
    private bool _isInitialized;
    private bool _isDragging;
    private double _lastScreenX;
    private bool _isPinching;
    private int _lastDrawnRecordCount;
    private double _lastYMax;
    private List<TextBlock> _yAxisLabels = new();
    private List<Line> _gridLines = new();
    private bool[] _seriesVisible = { true, true, true, true, true };
    private double _initialPinchDistance;
    private double _initialPinchZoom;
    private (IPointer? Pointer, Point Position) _pointer1;
    private (IPointer? Pointer, Point Position) _pointer2;

    private static IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)>? _cachedData;

    private IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)>? _data;

    public IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)>? Data
    {
        get => _data;
        set
        {
            _data = value;
            _cachedData = value;
            _lastDrawnRecordCount = 0;
            Dispatcher.UIThread.Post(Redraw);
        }
    }

    public void SetData(IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)> data)
    {
        Data = data;
    }

    public FpsChartAnalysisControl()
    {
        Initialize();
        if (_cachedData != null)
        {
            _data = _cachedData;
            _lastDrawnRecordCount = 0;
            Dispatcher.UIThread.Post(Redraw);
        }
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _seriesNames = new[] { "fps", "max", "avg", "min", "1%low" };
        _seriesColors = new IBrush[]
        {
            new SolidColorBrush(Color.FromRgb(0, 255, 0)),
            new SolidColorBrush(Color.FromRgb(255, 60, 60)),
            new SolidColorBrush(Color.FromRgb(0, 220, 255)),
            new SolidColorBrush(Color.FromRgb(100, 180, 255)),
            new SolidColorBrush(Color.FromRgb(255, 180, 0))
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
        _canvas.PointerExited += OnCanvasPointerExited;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        _canvas.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _canvas.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        _canvas.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);

        _yAxisPanel = new Canvas();

        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            ClipToBounds = true
        };
        _scrollViewer.ScrollChanged += OnScrollChanged;

        _scrollViewer.Content = _canvas;

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

        var zoomButtonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var zoomInButton = new Button
        {
            Content = "放大",
            Padding = new Thickness(8, 4, 8, 4),
            FontSize = 11
        };
        zoomInButton.Click += (s, e) => ZoomIn();
        zoomButtonPanel.Children.Add(zoomInButton);

        var zoomOutButton = new Button
        {
            Content = "缩小",
            Padding = new Thickness(8, 4, 8, 4),
            FontSize = 11
        };
        zoomOutButton.Click += (s, e) => ZoomOut();
        zoomButtonPanel.Children.Add(zoomOutButton);

        var zoomResetButton = new Button
        {
            Content = "重置缩放",
            Padding = new Thickness(8, 4, 8, 4),
            FontSize = 11
        };
        zoomResetButton.Click += (s, e) => ZoomReset();
        zoomButtonPanel.Children.Add(zoomResetButton);

        Grid.SetRow(zoomButtonPanel, 0);
        Grid.SetColumn(zoomButtonPanel, 1);
        _rootGrid.Children.Add(zoomButtonPanel);

        Grid.SetRow(_yAxisPanel, 1);
        Grid.SetColumn(_yAxisPanel, 0);
        _rootGrid.Children.Add(_yAxisPanel);

        Grid.SetRow(_scrollViewer, 1);
        Grid.SetColumn(_scrollViewer, 1);
        _rootGrid.Children.Add(_scrollViewer);

        _legendPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(8, 8, 8, 8),
            VerticalAlignment = VerticalAlignment.Center
        };

        for (int i = 0; i < 5; i++)
        {
            var legendItem = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var legendColor = new Rectangle
            {
                Width = 16,
                Height = 4,
                Fill = _seriesColors[i]
            };
            var legendText = new TextBlock
            {
                Text = _seriesNames[i],
                FontSize = 11,
                Foreground = ThemeHelper.GetTextBrush()
            };
            legendItem.Children.Add(legendColor);
            legendItem.Children.Add(legendText);
            _legendPanel.Children.Add(legendItem);
        }

        Grid.SetRow(_legendPanel, 1);
        Grid.SetColumn(_legendPanel, 2);
        _rootGrid.Children.Add(_legendPanel);

        for (int i = 0; i < 5; i++)
        {
            var polyline = new Polyline
            {
                Stroke = _seriesColors[i],
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            _polylines.Add(polyline);
            _canvas.Children.Add(polyline);
        }

        _tooltipBorder = new Border
        {
            Background = ThemeHelper.IsDarkTheme() 
                ? new SolidColorBrush(Color.FromRgb(30, 30, 30)) 
                : new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            BorderBrush = ThemeHelper.IsDarkTheme() 
                ? new SolidColorBrush(Color.FromRgb(60, 60, 60)) 
                : new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(4),
            IsVisible = false
        };

        _tooltipTextBlock = new TextBlock
        {
            FontSize = 11,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _tooltipBorder.Child = _tooltipTextBlock;
        _canvas.Children.Add(_tooltipBorder);

        var checkboxPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Margin = new Thickness(10, 8, 10, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        for (int i = 0; i < 5; i++)
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
                Dispatcher.UIThread.Post(Redraw);
            };
            checkbox.Unchecked += (s, e) =>
            {
                _seriesVisible[idx] = false;
                _lastDrawnRecordCount = 0;
                Dispatcher.UIThread.Post(() =>
                {
                    if (_polylines != null && idx < _polylines.Count)
                    {
                        _polylines[idx].Points.Clear();
                    }
                    Redraw();
                });
            };
            checkboxPanel.Children.Add(checkbox);
        }

        Grid.SetRow(checkboxPanel, 2);
        Grid.SetColumn(checkboxPanel, 1);
        _rootGrid.Children.Add(checkboxPanel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_canvas != null)
        {
            _canvas.PointerPressed -= OnCanvasPointerPressed;
            _canvas.PointerReleased -= OnCanvasPointerReleased;
            _canvas.PointerMoved -= OnCanvasPointerMoved;
            _canvas.PointerExited -= OnCanvasPointerExited;
            _canvas.PointerWheelChanged -= OnCanvasPointerWheelChanged;
        }

        _polylines = null;
        _canvas = null;
        _scrollViewer = null;
        _rootGrid = null;
    }

    public Control GetContent()
    {
        return _rootGrid ?? new Control();
    }

    private void Redraw()
    {
        if (_isDisposed || !_isInitialized) return;
        if (_canvas == null || _scrollViewer == null || _polylines == null) return;

        var records = _data;
        
        if (records == null || records.Count == 0)
        {
            ClearCanvas();
            return;
        }

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

        UpdateTimeLabels(records, chartHeight, canvasWidth);

        if (needsFullRedraw)
        {
            for (int seriesIdx = 0; seriesIdx < 5; seriesIdx++)
            {
                if (!_seriesVisible[seriesIdx])
                {
                    _polylines[seriesIdx].Points.Clear();
                    continue;
                }
                
                var points = new List<Point>();
                for (int i = visibleStartIndex; i <= visibleEndIndex; i++)
                {
                    double value = seriesIdx switch
                    {
                        0 => records[i].Fps,
                        1 => records[i].Max,
                        2 => records[i].Avg,
                        3 => records[i].Min,
                        4 => records[i].Low1,
                        _ => 0
                    };
                    var x = i * CurrentSamplePixelWidth;
                    var y = TopPadding + chartHeight - (value / yMax * chartHeight);
                    points.Add(new Point(x, y));
                }
                _polylines[seriesIdx].Points = points;
            }
            _lastDrawnRecordCount = records.Count;
        }
        else
        {
            for (int seriesIdx = 0; seriesIdx < 5; seriesIdx++)
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
                    _ => 0
                };
                var x = i * CurrentSamplePixelWidth;
                var y = TopPadding + chartHeight - (value / yMax * chartHeight);
                _polylines[seriesIdx].Points.Add(new Point(x, y));
            }
            _lastDrawnRecordCount = records.Count;
        }
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

    private List<TextBlock> _timeLabels = new();
    private int _lastTimeLabelStartIndex = -1;
    private int _lastTimeLabelEndIndex = -1;
    private double _lastTimeLabelZoom = -1;

    private void UpdateTimeLabels(IReadOnlyList<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)> records, double chartHeight, double canvasWidth)
    {
        if (_canvas == null || _scrollViewer == null) return;

        var visibleStartIndex = 0;
        var visibleEndIndex = records.Count - 1;

        if (_scrollViewer.Bounds.Width > 0)
        {
            var visibleWidth = _scrollViewer.Bounds.Width;
            var offset = _scrollViewer.Offset.X;
            visibleStartIndex = Math.Max(0, (int)(offset / CurrentSamplePixelWidth));
            visibleEndIndex = Math.Min(records.Count - 1, visibleStartIndex + (int)(visibleWidth / CurrentSamplePixelWidth) + 100);
        }

        if (visibleStartIndex == _lastTimeLabelStartIndex && 
            visibleEndIndex == _lastTimeLabelEndIndex &&
            Math.Abs(_lastTimeLabelZoom - _zoomLevel) < 0.001)
        {
            return;
        }

        _lastTimeLabelStartIndex = visibleStartIndex;
        _lastTimeLabelEndIndex = visibleEndIndex;
        _lastTimeLabelZoom = _zoomLevel;

        foreach (var label in _timeLabels)
        {
            _canvas.Children.Remove(label);
        }
        _timeLabels.Clear();

        int labelInterval = (int)(150 / CurrentSamplePixelWidth);
        if (labelInterval < 1) labelInterval = 1;

        var startIndex = (visibleStartIndex / labelInterval) * labelInterval;
        for (int i = startIndex; i <= visibleEndIndex; i += labelInterval)
        {
            if (i < 0 || i >= records.Count) continue;
            var x = i * CurrentSamplePixelWidth;
            var timeLabel = new TextBlock
            {
                Text = records[i].Time.ToString("HH:mm:ss"),
                FontSize = 10,
                Foreground = ThemeHelper.GetSubTextBrush()
            };
            Canvas.SetLeft(timeLabel, x - 20);
            Canvas.SetTop(timeLabel, TopPadding + chartHeight + 5);
            _canvas.Children.Add(timeLabel);
            _timeLabels.Add(timeLabel);
        }
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

        var toRemove = new List<TextBlock>();
        for (int i = 0; i < _canvas.Children.Count; i++)
        {
            if (_canvas.Children[i] is TextBlock tb && tb != _tooltipTextBlock)
            {
                toRemove.Add(tb);
            }
        }
        foreach (var tb in toRemove)
        {
            _canvas.Children.Remove(tb);
        }

        _canvas.Width = _scrollViewer?.Bounds.Width > 0 ? _scrollViewer.Bounds.Width : 800;
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isPinching || _canvas == null) return;
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
        
        e.Pointer.Capture(_canvas);
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && _scrollViewer != null && _canvas != null)
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

        UpdateTooltip(e);
    }

    private void UpdateTooltip(PointerEventArgs e)
    {
        if (_tooltipBorder == null || _tooltipTextBlock == null || _canvas == null || _data == null) return;

        var position = e.GetPosition(_canvas);
        var chartHeight = ChartHeight - TopPadding - BottomPadding;

        if (position.X < 0 || position.Y < TopPadding || position.Y > TopPadding + chartHeight)
        {
            _tooltipBorder.IsVisible = false;
            return;
        }

        var index = (int)(position.X / CurrentSamplePixelWidth);
        if (index < 0 || index >= _data.Count)
        {
            _tooltipBorder.IsVisible = false;
            return;
        }

        var record = _data[index];

        _tooltipTextBlock.Text = $"时间: {record.Time:HH:mm:ss.fff}\n" +
                                $"fps: {record.Fps:F1}\n" +
                                $"max: {record.Max:F1}\n" +
                                $"avg: {record.Avg:F1}\n" +
                                $"min: {record.Min:F1}\n" +
                                $"1%low: {record.Low1:F1}";

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

    private void OnCanvasPointerExited(object? sender, PointerEventArgs e)
    {
        if (_tooltipBorder != null)
            _tooltipBorder.IsVisible = false;
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_scrollViewer == null) return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.Delta.Y > 0)
            {
                ZoomIn(e.GetPosition(_canvas).X);
            }
            else
            {
                ZoomOut(e.GetPosition(_canvas).X);
            }
            e.Handled = true;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_canvas == null) return;

        if (!_isPinching && e.Pointer.Type == PointerType.Touch)
        {
            if (_pointer1.Pointer == null)
            {
                _pointer1 = (e.Pointer, e.GetPosition(_canvas));
            }
            else if (_pointer2.Pointer == null)
            {
                _pointer2 = (e.Pointer, e.GetPosition(_canvas));
                _isPinching = true;
                _initialPinchDistance = CalculateDistance(_pointer1.Position, _pointer2.Position);
                _initialPinchZoom = _zoomLevel;
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
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
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPinching || _canvas == null || _scrollViewer == null) return;

        if (_pointer1.Pointer == e.Pointer)
        {
            _pointer1 = (e.Pointer, e.GetPosition(_canvas));
        }
        else if (_pointer2.Pointer == e.Pointer)
        {
            _pointer2 = (e.Pointer, e.GetPosition(_canvas));
        }

        var currentDistance = CalculateDistance(_pointer1.Position, _pointer2.Position);
        var scale = currentDistance / _initialPinchDistance;
        var newZoom = _initialPinchZoom * scale;

        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        ApplyZoom(newZoom, _scrollViewer.Viewport.Width / 2);
    }

    private double CalculateDistance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }

    public void ZoomIn()
    {
        if (_scrollViewer == null) return;
        ApplyZoom(_zoomLevel * ZoomStep, _scrollViewer.Viewport.Width / 2);
    }

    public void ZoomIn(double centerX)
    {
        if (_scrollViewer == null) return;
        ApplyZoom(_zoomLevel * ZoomStep, centerX);
    }

    public void ZoomOut()
    {
        if (_scrollViewer == null) return;
        ApplyZoom(_zoomLevel / ZoomStep, _scrollViewer.Viewport.Width / 2);
    }

    public void ZoomOut(double centerX)
    {
        if (_scrollViewer == null) return;
        ApplyZoom(_zoomLevel / ZoomStep, centerX);
    }

    public void ZoomReset()
    {
        if (_scrollViewer == null) return;
        ApplyZoom(DefaultZoom, _scrollViewer.Viewport.Width / 2);
    }

    private void ApplyZoom(double newZoom, double viewportCenterX)
    {
        if (_scrollViewer == null || _canvas == null) return;

        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        var viewportWidth = _scrollViewer.Viewport.Width;
        var currentOffset = _scrollViewer.Offset.X;
        var centerDataIndex = (int)((currentOffset + viewportCenterX) / CurrentSamplePixelWidth);

        _zoomLevel = newZoom;

        var newSampleWidth = CurrentSamplePixelWidth;
        var newCenterOffset = LeftPadding + centerDataIndex * newSampleWidth;
        var newOffset = newCenterOffset - viewportWidth / 2.0;

        _lastDrawnRecordCount = 0;
        Redraw();

        newOffset = Math.Max(0, newOffset);
        var maxOffset = _scrollViewer.Extent.Width - viewportWidth;
        newOffset = Math.Min(maxOffset, newOffset);

        _scrollViewer.Offset = new Vector(newOffset, 0);
    }

    private double _lastScrollOffset;
    private DateTime _lastRedrawTime = DateTime.MinValue;
    private const int RedrawIntervalMs = 50;

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer == null) return;
        
        var now = DateTime.Now;
        if ((now - _lastRedrawTime).TotalMilliseconds < RedrawIntervalMs) return;
        _lastRedrawTime = now;
        
        var currentOffset = _scrollViewer.Offset.X;
        if (Math.Abs(currentOffset - _lastScrollOffset) > 50)
        {
            _lastScrollOffset = currentOffset;
            _lastDrawnRecordCount = 0;
            Redraw();
        }
    }
}
