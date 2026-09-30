using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class SevenSegmentClockSettingsControl : ComponentBase<SevenSegmentClockSettings>
{
    private CheckBox _showSecondsToggle = null!;
    private ComboBox _timeBaseComboBox = null!;
    private Slider _zoomSlider = null!;
    private TextBlock _zoomValueTextBlock = null!;
    private CheckBox _skewModeToggle = null!;
    private WpfColorPicker _accentColorPicker = null!;
    private CheckBox _separatorBlinkToggle = null!;
    private CheckBox _animationToggle = null!;

    private bool _initCompleted;

    public SevenSegmentClockSettingsControl()
    {
        InitializeComponent();
        InitializeControlReferences();
    }

    /// <summary>
    /// 从 XAML 的 SettingsControl.Switcher 中提取控件引用。
    /// （SettingsControl 模板内元素无法使用 x:Name，需通过 Switcher 属性访问）
    /// </summary>
    private void InitializeControlReferences()
    {
        _showSecondsToggle = (CheckBox)ShowSecondsItem.Switcher;
        _timeBaseComboBox = (ComboBox)TimeBaseItem.Switcher;

        var zoomRow = (StackPanel)ZoomItem.Switcher;
        _zoomSlider = (Slider)zoomRow.Children[0];
        _zoomValueTextBlock = (TextBlock)zoomRow.Children[1];

        _skewModeToggle = (CheckBox)SkewModeItem.Switcher;
        _accentColorPicker = (WpfColorPicker)AccentColorItem.Switcher;
        _separatorBlinkToggle = (CheckBox)SeparatorBlinkItem.Switcher;
        _animationToggle = (CheckBox)AnimationItem.Switcher;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted)
        {
            return;
        }

        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }

        _initCompleted = true;
        Loaded += OnLoaded;
    }

    private void OnLoadedAfterSettingsReady(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        // 迁移旧版时间基准值
        var migratedType = TimeBaseTypeHelper.Migrate((int)Settings.TimeBaseType);
        if (migratedType != Settings.TimeBaseType)
        {
            Settings.TimeBaseType = migratedType;
        }

        _showSecondsToggle.IsChecked = Settings.ShowSeconds;
        _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => 0,
            TimeBaseType.RawServerTime => 1,
            TimeBaseType.ClassIslandTime => 2,
            _ => 0
        };
        _zoomSlider.Value = Settings.Zoom;
        UpdateZoomValueText(Settings.Zoom);
        _skewModeToggle.IsChecked = Settings.SkewMode;
        _accentColorPicker.Color = ParseColor(Settings.AccentColor);
        _separatorBlinkToggle.IsChecked = Settings.SeparatorBlink;
        _animationToggle.IsChecked = Settings.EnableTransitionAnimation;
    }

    private void OnShowSecondsChanged(object sender, RoutedEventArgs e)
    {
        if (Settings == null)
        {
            return;
        }

        Settings.ShowSeconds = _showSecondsToggle.IsChecked ?? false;
    }

    private void OnTimeBaseSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Settings == null)
        {
            return;
        }

        Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
        {
            0 => TimeBaseType.PluginOffsetServerTime,
            1 => TimeBaseType.RawServerTime,
            2 => TimeBaseType.ClassIslandTime,
            _ => TimeBaseType.PluginOffsetServerTime
        };
    }

    private void OnZoomValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // 滑块按 0.01 步长取值，取整避免浮点误差累积
        var zoom = Math.Round(e.NewValue, 2);
        UpdateZoomValueText(zoom);

        if (Settings == null)
        {
            return;
        }

        Settings.Zoom = zoom;
    }

    private void OnSkewModeChanged(object sender, RoutedEventArgs e)
    {
        if (Settings == null)
        {
            return;
        }

        Settings.SkewMode = _skewModeToggle.IsChecked ?? false;
    }

    private void OnAccentColorChanged(object sender, ColorChangedEventArgs e)
    {
        if (Settings == null)
        {
            return;
        }

        Settings.AccentColor = _accentColorPicker.Color.ToString();
    }

    private void OnSeparatorBlinkChanged(object sender, RoutedEventArgs e)
    {
        if (Settings == null)
        {
            return;
        }

        Settings.SeparatorBlink = _separatorBlinkToggle.IsChecked ?? false;
    }

    private void OnAnimationChanged(object sender, RoutedEventArgs e)
    {
        if (Settings == null)
        {
            return;
        }

        Settings.EnableTransitionAnimation = _animationToggle.IsChecked ?? false;
    }

    private void UpdateZoomValueText(double zoom)
    {
        // XAML 解析期间控件引用可能尚未就绪
        if (_zoomValueTextBlock != null)
        {
            _zoomValueTextBlock.Text = zoom.ToString("0.00");
        }
    }

    private static Color ParseColor(string colorStr)
    {
        try
        {
            return ThemeHelper.ParseColor(colorStr);
        }
        catch
        {
            return ThemeHelper.ParseColor(SevenSegmentClockSettings.DefaultAccentColor);
        }
    }
}
