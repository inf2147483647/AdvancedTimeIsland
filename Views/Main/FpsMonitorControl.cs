using System;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "11223344-5566-7788-9900-aabbccddeefe",
    "实时主界面刷新率（调试）（ATI）",
    "\uE9D1",
    "实时显示ClassIsland主界面帧率"
)]
public class FpsMonitorControl : ComponentBase<FpsMonitorSettings>
{
    private FpsMonitorViewModel? vm;
    private TextBlock labelFpsTb;
    private TextBlock valueFpsTb;
    private TextBlock labelMaxTb;
    private TextBlock valueMaxTb;
    private TextBlock labelAvgTb;
    private TextBlock valueAvgTb;
    private TextBlock labelMinTb;
    private TextBlock valueMinTb;
    private TextBlock labelLow1Tb;
    private TextBlock valueLow1Tb;
    private TextBlock labelOneSecondFrameCountTb;
    private TextBlock valueOneSecondFrameCountTb;
    private Border rootBorder;
    private double _lastFrameTimestamp;
    private double _currentFps;
    private bool _isEnabled;
    private TopLevel? _topLevel;
    private IDisposable? _renderTimerSubscription;
    private double _currentSecondStartTimestamp;
    private int _currentSecondFrameCount;
    private int _lastReportedFrameCount;

    public FpsMonitorControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        rootBorder = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };

        labelFpsTb = new TextBlock { Text = "fps:" };
        valueFpsTb = new TextBlock { Foreground = Brushes.Green, Text = "---" };
        labelMaxTb = new TextBlock { Text = " max:" };
        valueMaxTb = new TextBlock { Foreground = Brushes.Green, Text = "---" };
        labelAvgTb = new TextBlock { Text = " avg:" };
        valueAvgTb = new TextBlock { Foreground = Brushes.Green, Text = "---" };
        labelMinTb = new TextBlock { Text = " min:" };
        valueMinTb = new TextBlock { Foreground = Brushes.Green, Text = "---" };
        labelLow1Tb = new TextBlock { Text = " 1%low:" };
        valueLow1Tb = new TextBlock { Foreground = Brushes.Green, Text = "---" };
        labelOneSecondFrameCountTb = new TextBlock { Text = " 1s frm:" };
        valueOneSecondFrameCountTb = new TextBlock { Foreground = Brushes.Green, Text = "---" };

        sp.Children.Add(labelFpsTb);
        sp.Children.Add(valueFpsTb);
        sp.Children.Add(labelMaxTb);
        sp.Children.Add(valueMaxTb);
        sp.Children.Add(labelAvgTb);
        sp.Children.Add(valueAvgTb);
        sp.Children.Add(labelMinTb);
        sp.Children.Add(valueMinTb);
        sp.Children.Add(labelLow1Tb);
        sp.Children.Add(valueLow1Tb);
        sp.Children.Add(labelOneSecondFrameCountTb);
        sp.Children.Add(valueOneSecondFrameCountTb);

        rootBorder.Child = sp;
        Content = rootBorder;
    }

    private void UpdateLabelFontColor(string colorStr)
    {
        var brush = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);
        labelFpsTb.Foreground = brush;
        labelMaxTb.Foreground = brush;
        labelAvgTb.Foreground = brush;
        labelMinTb.Foreground = brush;
        labelLow1Tb.Foreground = brush;
        labelOneSecondFrameCountTb.Foreground = brush;
    }

    private void UpdateLabelFontSize(double fontSize)
    {
        labelFpsTb.FontSize = fontSize;
        labelMaxTb.FontSize = fontSize;
        labelAvgTb.FontSize = fontSize;
        labelMinTb.FontSize = fontSize;
        labelLow1Tb.FontSize = fontSize;
        labelOneSecondFrameCountTb.FontSize = fontSize;
    }

    private void UpdateValueFontSize(double fontSize)
    {
        valueFpsTb.FontSize = fontSize;
        valueMaxTb.FontSize = fontSize;
        valueAvgTb.FontSize = fontSize;
        valueMinTb.FontSize = fontSize;
        valueLow1Tb.FontSize = fontSize;
        valueOneSecondFrameCountTb.FontSize = fontSize;
    }

    private void UpdateFpsForeground(IBrush brush)
    {
        valueFpsTb.Foreground = brush;
    }

    private void UpdateMaxForeground(IBrush brush)
    {
        valueMaxTb.Foreground = brush;
    }

    private void UpdateAvgForeground(IBrush brush)
    {
        valueAvgTb.Foreground = brush;
    }

    private void UpdateMinForeground(IBrush brush)
    {
        valueMinTb.Foreground = brush;
    }

    private void UpdateLow1Foreground(IBrush brush)
    {
        valueLow1Tb.Foreground = brush;
    }

    private void UpdateOneSecondFrameCountForeground(IBrush brush)
    {
        valueOneSecondFrameCountTb.Foreground = brush;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!Settings.EnableCustomFontColor)
        {
            var newColor = ThemeHelper.GetThemeAwareTextColor();
            Settings.LabelFontColor = newColor;
            UpdateLabelFontColor(newColor);
        }
        else
        {
            UpdateLabelFontColor(Settings.LabelFontColor);
        }
    }

    private void OnRenderTimerTick(TimeSpan timestamp)
    {
        if (!_isEnabled) return;
        UpdateFpsValue(timestamp.TotalSeconds);
    }

    private void OnAnimationFrame(TimeSpan timestamp)
    {
        if (!_isEnabled) return;
        UpdateFpsValue(timestamp.TotalSeconds);
        _topLevel?.RequestAnimationFrame(OnAnimationFrame);
    }

    private void UpdateFpsValue(double timestamp)
    {
        try
        {
            _currentSecondFrameCount++;

            if (_lastFrameTimestamp > 0)
            {
                var deltaSeconds = timestamp - _lastFrameTimestamp;
                if (deltaSeconds > 0)
                {
                    _currentFps = 1.0 / deltaSeconds;

                    if (_currentSecondStartTimestamp == 0)
                    {
                        _currentSecondStartTimestamp = timestamp;
                    }
                    else if (timestamp - _currentSecondStartTimestamp >= 1.0)
                    {
                        _lastReportedFrameCount = _currentSecondFrameCount;
                        _currentSecondStartTimestamp = timestamp;
                        _currentSecondFrameCount = 0;
                    }

                    vm?.UpdateFps(_currentFps, _lastReportedFrameCount);
                }
            }
            else
            {
                _currentSecondStartTimestamp = timestamp;
                _currentSecondFrameCount = 0;
                _lastReportedFrameCount = 0;
            }

            _lastFrameTimestamp = timestamp;
        }
        catch (Exception)
        {
        }
    }

    private void EnableComponent()
    {
        _isEnabled = true;
        Settings.EnableComponent = true;

        vm = new FpsMonitorViewModel(Settings, UpdateLabelFontColor, UpdateLabelFontSize, UpdateFpsForeground, UpdateMaxForeground, UpdateAvgForeground, UpdateMinForeground, UpdateLow1Foreground, UpdateOneSecondFrameCountForeground, UpdateValueFontSize);
        DataContext = vm;

        vm.PropertyChanged += (s, e) =>
        {
            if (!_isEnabled) return;
            if (e.PropertyName == nameof(vm.FpsText)) valueFpsTb.Text = vm.FpsText;
            if (e.PropertyName == nameof(vm.MaxText)) valueMaxTb.Text = vm.MaxText;
            if (e.PropertyName == nameof(vm.AvgText)) valueAvgTb.Text = vm.AvgText;
            if (e.PropertyName == nameof(vm.MinText)) valueMinTb.Text = vm.MinText;
            if (e.PropertyName == nameof(vm.Low1Text)) valueLow1Tb.Text = vm.Low1Text;
            if (e.PropertyName == nameof(vm.OneSecondFrameCountText)) valueOneSecondFrameCountTb.Text = vm.OneSecondFrameCountText;
        };

        _topLevel = TopLevel.GetTopLevel(this);
        _lastFrameTimestamp = 0;
        _currentSecondStartTimestamp = 0;
        _currentSecondFrameCount = 0;
        _lastReportedFrameCount = 0;

        if (_topLevel != null)
        {
            // 优先使用 IRenderTimer.Tick（底层渲染定时器）
            _renderTimerSubscription = RenderTimerHelper.SubscribeTick(_topLevel, OnRenderTimerTick);
            if (_renderTimerSubscription != null)
            {
                return;
            }

            // 回退到 RequestAnimationFrame（Avalonia 公开 API）
            _topLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }

    private void DisableComponent()
    {
        _isEnabled = false;
        Settings.EnableComponent = false;

        _renderTimerSubscription?.Dispose();
        _renderTimerSubscription = null;
        _topLevel = null;

        (vm as IDisposable)?.Dispose();
        vm = null;

        valueFpsTb.Text = "---";
        valueMaxTb.Text = "---";
        valueAvgTb.Text = "---";
        valueMinTb.Text = "---";
        valueLow1Tb.Text = "---";
        valueOneSecondFrameCountTb.Text = "---";
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        UpdateLabelFontColor(Settings.LabelFontColor);
        UpdateLabelFontSize(Settings.LabelFontSize);
        UpdateValueFontSize(Settings.ValueFontSize);

        if (Settings.EnableComponent)
        {
            EnableComponent();
        }

        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FpsMonitorSettings.EnableComponent))
        {
            if (Settings.EnableComponent && !_isEnabled)
            {
                EnableComponent();
            }
            else if (!Settings.EnableComponent && _isEnabled)
            {
                DisableComponent();
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_isEnabled && _topLevel == null)
        {
            _topLevel = TopLevel.GetTopLevel(this);
            if (_topLevel != null)
            {
                _renderTimerSubscription = RenderTimerHelper.SubscribeTick(_topLevel, OnRenderTimerTick);
                if (_renderTimerSubscription == null)
                {
                    _topLevel.RequestAnimationFrame(OnAnimationFrame);
                }
            }
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        Settings.PropertyChanged -= OnSettingsPropertyChanged;

        _renderTimerSubscription?.Dispose();
        _renderTimerSubscription = null;
        _topLevel = null;
        _isEnabled = false;
        (vm as IDisposable)?.Dispose();
    }
}
