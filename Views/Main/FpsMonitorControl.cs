using System;
using System.Threading.Tasks;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
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
    private bool _isInDialogFlow;
    private TopLevel? _topLevel;
    private IDisposable? _renderTimerSubscription;

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
        var brush = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomColorAndFont);
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
        if (!Settings.EnableCustomColorAndFont)
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
            if (_lastFrameTimestamp > 0)
            {
                var deltaSeconds = timestamp - _lastFrameTimestamp;
                if (deltaSeconds > 0)
                {
                    _currentFps = 1.0 / deltaSeconds;
                    vm?.UpdateFps(_currentFps);
                }
            }

            _lastFrameTimestamp = timestamp;
        }
        catch (Exception)
        {
        }
    }

    private async Task<bool> ShowEpilepsyWarningDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return false;

        var contentPanel = new StackPanel();

        var warningTextBlock = new TextBlock
        {
            Text = "有极少数的人在观看一些视觉影像时可能会突然癫痫发作，这些影像包括快速改变的数字或图形。在使用此组件时，这些人可能会出现癫痫症状。甚至连不具有癫痫史的人，也可能在查看此组件时出现类似癫痫症状。\n\n" +
                   "如果您或您的家人有癫痫史，请在添加此组件之前先与医生咨询。如果您在使用此组件时出现以下症状，包括眼睛疼痛、视觉异常、偏头痛、痉挛或意识障碍（诸如昏迷）等，请立即中止使用，并且请您于再次使用此组件之前咨询您的医生。\n\n" +
                   "除上述症状外，当您感到头痛、头晕眼花、恶心想吐或类似晕车症状时，以及当身体的某些部位感到不舒服或疼痛时，请立即中止使用。若在中止使用后，症状仍没有减退，请立即寻求医生的诊疗。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(12)
        };
        contentPanel.Children.Add(warningTextBlock);

        var countDownTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 8, 0, 0),
            Text = "请阅读以上内容，确定按钮将在15秒后可用..."
        };
        contentPanel.Children.Add(countDownTextBlock);

        var dialog = new ContentDialog
        {
            Title = "警告：使用前详阅",
            Content = contentPanel,
            PrimaryButtonText = "确定（15）",
            CloseButtonText = "取消",
            IsPrimaryButtonEnabled = false
        };

        _ = Task.Run(async () =>
        {
            for (int i = 15; i >= 0; i--)
            {
                await Task.Delay(1000);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (i > 0)
                    {
                        dialog.PrimaryButtonText = $"确定（{i}）";
                    }
                    else
                    {
                        dialog.PrimaryButtonText = "确定";
                        dialog.IsPrimaryButtonEnabled = true;
                    }
                });
            }
        });

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async Task<bool> ShowDebugWarningDialogAsync(int count)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return false;

        var dialog = new ContentDialog
        {
            Title = "警告",
            Content = new TextBlock
            {
                Text = "此组件仅供调试，严禁用于教学环境！！！",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(12)
            },
            PrimaryButtonText = "确定",
            CloseButtonText = "取消"
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async void StartEnableFlow()
    {
        if (_isInDialogFlow) return;
        _isInDialogFlow = true;

        try
        {
            bool epilepsyAccepted = await ShowEpilepsyWarningDialogAsync();
            if (!epilepsyAccepted)
            {
                Settings.EnableComponent = false;
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bool debugAccepted = await ShowDebugWarningDialogAsync(i + 1);
                if (!debugAccepted)
                {
                    Settings.EnableComponent = false;
                    return;
                }
            }

            EnableComponent();
        }
        finally
        {
            _isInDialogFlow = false;
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
            StartEnableFlow();
        }

        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FpsMonitorSettings.EnableComponent))
        {
            if (Settings.EnableComponent && !_isEnabled && !_isInDialogFlow)
            {
                StartEnableFlow();
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
