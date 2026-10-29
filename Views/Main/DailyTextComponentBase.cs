using System;
using System.ComponentModel;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 按"日"刷新的纯文本主界面组件基类。
/// 统一处理字体样式、时间基准、共享渲染时钟订阅与设置变更，子类只需实现 <see cref="BuildText"/>。
/// </summary>
public abstract class DailyTextComponentBase<TSettings> : ComponentBase<TSettings>
    where TSettings : WeekSettings
{
    private readonly TimeBaseService _timeBaseService;
    private readonly TextBlock _textBlock;
    private readonly Border _rootBorder;
    private IDisposable? _clockSubscription;
    private DateTime _cachedDate = DateTime.MinValue;
    private bool _hasText;

    protected DailyTextComponentBase(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;

        _rootBorder = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _textBlock = new TextBlock { Text = "..." };
        _rootBorder.Child = _textBlock;
        Content = _rootBorder;
    }

    /// <summary>
    /// 是否按日期缓存文本。为 false 时每次时钟回调都会重算（适合只依赖日期的轻量计算）。
    /// </summary>
    protected virtual bool UseDateCache => true;

    /// <summary>
    /// 根据当前时间生成要显示的文本。
    /// </summary>
    protected abstract string BuildText(DateTime now);

    protected DateTime GetCurrentTime()
    {
        return Settings.TimeBaseType switch
        {
            TimeBaseType.RawServerTime => _timeBaseService.GetRawServerTime(),
            TimeBaseType.ClassIslandTime => _timeBaseService.GetClassIslandTime(),
            _ => _timeBaseService.GetCurrentTime()
        };
    }

    /// <summary>
    /// 重新计算并刷新显示。
    /// </summary>
    /// <param name="force">为 true 时忽略日期缓存立即重算。</param>
    protected void RefreshDisplay(bool force = false)
    {
        try
        {
            var now = GetCurrentTime();
            if (!force && UseDateCache && _hasText && now.Date == _cachedDate)
            {
                return;
            }

            var text = BuildText(now);
            _cachedDate = now.Date;
            _hasText = true;
            if (_textBlock.Text != text)
            {
                _textBlock.Text = text;
            }
        }
        catch
        {
            // 计算失败时保持上一次的文本，避免组件闪烁。
        }
    }

    private void UpdateFontColor(string colorStr)
    {
        _textBlock.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);
    }

    private void UpdateFontSize(double fontSize)
    {
        if (fontSize > 0)
        {
            _textBlock.FontSize = fontSize;
        }
        else
        {
            _textBlock.FontSize = FontFamilyHelper.GetBodyFontSize(_textBlock);
        }
    }

    private void UpdateFontFamily()
    {
        if (Settings.EnableCustomFontFamily)
        {
            _textBlock.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.FontFamily);
        }
        else
        {
            _textBlock.ClearValue(TextBlock.FontFamilyProperty);
        }
    }

    private void UpdateFontWeight()
    {
        if (Settings.EnableCustomFontWeight)
        {
            _textBlock.FontWeight = FontFamilyHelper.GetFontWeightFromString(Settings.FontWeight);
        }
        else
        {
            _textBlock.ClearValue(TextBlock.FontWeightProperty);
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateFontColor(Settings.FontColor);
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.FontSize : 0);
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WeekSettings.FontColor):
            case nameof(WeekSettings.EnableCustomFontColor):
                UpdateFontColor(Settings.FontColor);
                break;
            case nameof(WeekSettings.FontSize):
            case nameof(WeekSettings.EnableCustomFontSize):
                UpdateFontSize(Settings.EnableCustomFontSize ? Settings.FontSize : 0);
                break;
            case nameof(WeekSettings.FontFamily):
            case nameof(WeekSettings.EnableCustomFontFamily):
                UpdateFontFamily();
                break;
            case nameof(WeekSettings.FontWeight):
            case nameof(WeekSettings.EnableCustomFontWeight):
                UpdateFontWeight();
                break;
            default:
                // 时间基准、简洁模式等设置变化后立即重算。
                RefreshDisplay(true);
                break;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;
        Settings.PropertyChanged += OnSettingsChanged;

        UpdateFontColor(Settings.FontColor);
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.FontSize : 0);
        UpdateFontFamily();
        UpdateFontWeight();
        RefreshDisplay(true);

        _clockSubscription = SharedRenderClockService.Instance.Subscribe(_ => RefreshDisplay());
        SharedRenderClockService.Instance.EnsureStarted();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        _clockSubscription?.Dispose();
        _clockSubscription = null;
    }
}
