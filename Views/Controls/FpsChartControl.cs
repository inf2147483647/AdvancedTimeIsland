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

    private Canvas? _canvas;
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
    private Border? _tooltipBorder;
    private TextBlock? _tooltipTextBlock;
    private bool _isDisposed;
    private bool _isInitialized;
    private Button? _pauseButton;
    private bool _isPaused;
    private bool _isDragging;
    private double _lastScreenX;

    public FpsChartControl()
    {
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
            Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255))
        };
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerExited += OnCanvasPointerLeave;

        for (int i = 0; i < 5; i++)
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

        _legendPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        for (int i = 0; i < 5; i++)
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
                Foreground = Brushes.White
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

        _pauseButton = new Button
        {
            Content = "暂停采集",
            Width = 80,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _pauseButton.Click += OnPauseButtonClick;
        metricsPanel.Children.Add(_pauseButton);

        _tooltipBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            IsVisible = false,
            ZIndex = 100
        };
        _tooltipTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        };
        _tooltipBorder.Child = _tooltipTextBlock;
        _canvas.Children.Add(_tooltipBorder);

        _rootGrid = new Grid
        {
            MinHeight = ChartHeight + 30,
            Height = ChartHeight + 30
        };
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LegendWidth) });

        Grid.SetRow(metricsPanel, 0);
        Grid.SetColumn(metricsPanel, 0);
        _rootGrid.Children.Add(metricsPanel);

        Grid.SetRow(_scrollViewer, 1);
        Grid.SetColumn(_scrollViewer, 0);
        Grid.SetRow(_legendPanel, 1);
        Grid.SetColumn(_legendPanel, 1);

        _rootGrid.Children.Add(_scrollViewer);
        _rootGrid.Children.Add(_legendPanel);

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
            Foreground = Brushes.Gray
        };
        valueTextBlock = new TextBlock
        {
            Text = "---",
            FontSize = 12,
            Foreground = Brushes.Green
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
            FpsSampler.DataUpdated += OnDataUpdated;
            _redrawTimer?.Start();
            DispatcherTimer.RunOnce(Redraw, TimeSpan.FromMilliseconds(50));
        }
        catch (Exception)
        {
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
            }
        }
        catch (Exception)
        {
        }
    }

    private void OnDataUpdated()
    {
    }

    private void OnRedrawTick(object? sender, EventArgs e)
    {
        Redraw();
    }

    private void Redraw()
    {
        if (_isDisposed || !_isInitialized) return;
        if (_canvas == null || _scrollViewer == null || _polylines == null) return;

        var records = FpsSampler.Records;
        if (records.Count == 0) return;

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
            visibleStartIndex = Math.Max(0, (int)((offset - LeftPadding) / SamplePixelWidth));
            visibleEndIndex = Math.Min(records.Count - 1, visibleStartIndex + (int)(visibleWidth / SamplePixelWidth) + 100);
        }

        var visibleRecords = records.Skip(visibleStartIndex).Take(visibleEndIndex - visibleStartIndex + 1).ToList();

        double yMax = 0;
        foreach (var r in visibleRecords)
        {
            if (r.Fps > yMax) yMax = r.Fps;
            if (r.Max > yMax) yMax = r.Max;
            if (r.Avg > yMax) yMax = r.Avg;
        }
        yMax = Math.Ceiling(yMax / 10.0) * 10;
        if (yMax < 10) yMax = 10;

        var canvasWidth = Math.Max(
            _scrollViewer.Bounds.Width > 0 ? _scrollViewer.Bounds.Width : 800,
            records.Count * SamplePixelWidth + LeftPadding + RightPadding);

        _canvas.Width = canvasWidth;
        _canvas.Height = controlHeight;

        var toRemove = _canvas.Children.OfType<TextBlock>().Where(tb => tb != _tooltipTextBlock).ToList();
        foreach (var tb in toRemove)
        {
            _canvas.Children.Remove(tb);
        }

        var lineToRemove = _canvas.Children.OfType<Line>().ToList();
        foreach (var line in lineToRemove)
        {
            _canvas.Children.Remove(line);
        }

        int yStep = (int)yMax / 5;
        if (yStep < 1) yStep = 1;
        for (int y = 0; y <= yMax; y += yStep)
        {
            var yLabel = new TextBlock
            {
                Text = y.ToString(),
                FontSize = 10,
                Foreground = Brushes.Gray
            };
            Canvas.SetLeft(yLabel, 5);
            Canvas.SetTop(yLabel, TopPadding + chartHeight - (y / yMax * chartHeight) - 6);
            _canvas.Children.Add(yLabel);

            var gridLine = new Line
            {
                StartPoint = new Point(LeftPadding, TopPadding + chartHeight - (y / yMax * chartHeight)),
                EndPoint = new Point(canvasWidth - RightPadding, TopPadding + chartHeight - (y / yMax * chartHeight)),
                Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                StrokeThickness = 0.5
            };
            _canvas.Children.Add(gridLine);
        }

        int labelInterval = 300;
        for (int i = 0; i < records.Count; i += labelInterval)
        {
            var x = LeftPadding + i * SamplePixelWidth;
            var timeLabel = new TextBlock
            {
                Text = records[i].Time.ToString("HH:mm:ss"),
                FontSize = 10,
                Foreground = Brushes.Gray
            };
            Canvas.SetLeft(timeLabel, x - 20);
            Canvas.SetTop(timeLabel, TopPadding + chartHeight + 5);
            _canvas.Children.Add(timeLabel);
        }

        for (int seriesIdx = 0; seriesIdx < 5; seriesIdx++)
        {
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

                var x = LeftPadding + i * SamplePixelWidth;
                var y = TopPadding + chartHeight - (value / yMax * chartHeight);
                points.Add(new Point(x, y));
            }
            _polylines[seriesIdx].Points = points;
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

    private void UpdateMetrics((DateTime Time, double Fps, double Max, double Avg, double Min, double Low1) record)
    {
        UpdateMetricColor(_fpsValueTextBlock, record.Fps);
        UpdateMetricColor(_maxValueTextBlock, record.Max);
        UpdateMetricColor(_avgValueTextBlock, record.Avg);
        UpdateMetricColor(_minValueTextBlock, record.Min);
        UpdateMetricColor(_low1ValueTextBlock, record.Low1);

        _fpsValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Fps:F1}");
        _maxValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Max:F1}");
        _avgValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Avg:F1}");
        _minValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Min:F1}");
        _low1ValueTextBlock?.SetValue(TextBlock.TextProperty, $"{record.Low1:F1}");
    }

    private void UpdateMetricColor(TextBlock? textBlock, double fps)
    {
        if (textBlock == null) return;

        if (fps >= 30)
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        else if (fps >= 20)
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 0));
        else
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isInitialized || _scrollViewer == null) return;
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

        if (position.X < LeftPadding || position.Y < TopPadding || position.Y > TopPadding + chartHeight)
        {
            _tooltipBorder.IsVisible = false;
            return;
        }

        var index = (int)((position.X - LeftPadding) / SamplePixelWidth);
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

    private void OnCanvasPointerLeave(object? sender, PointerEventArgs e)
    {
        if (_tooltipBorder != null)
        {
            _tooltipBorder.IsVisible = false;
        }
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
            }
        }
        catch
        {
        }
    }
}