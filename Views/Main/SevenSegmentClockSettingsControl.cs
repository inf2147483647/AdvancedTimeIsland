using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Views.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class SevenSegmentClockSettingsControl : ComponentBase<SevenSegmentClockSettings>
{
    private ToggleSwitch? _showSecondsToggle;
    private ComboBox? _timeBaseComboBox;
    private ToggleSwitch? _skewModeToggle;
    private ColorPicker? _accentColorPicker;
    private ToggleSwitch? _separatorBlinkToggle;
    private Slider? _zoomSlider;
    private TextBlock? _zoomValueTextBlock;
    private ToggleSwitch? _animationToggle;

    private readonly List<TextBlock> _labelTextBlocks = new();
    private readonly List<TextBlock> _descTextBlocks = new();

    public SevenSegmentClockSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8, Margin = new Thickness(12) };

        // ==================== 显示设置 ====================
        var displayPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _showSecondsToggle = new ToggleSwitch
        {
            Content = "显示秒数",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        displayPanel.Children.Add(_showSecondsToggle);

        displayPanel.Children.Add(CreateDescription("关闭后仅显示“时:分”（HH:mm）。默认为开启。"));

        displayPanel.Children.Add(CreateLabel("时间基准（时间差值）:", 6));

        _timeBaseComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left };
        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("ClassIsland时间");
        displayPanel.Children.Add(_timeBaseComboBox);

        displayPanel.Children.Add(CreateDescription("选择数码管时钟取时所依据的时间差值来源。"));

        mainPanel.Children.Add(SettingsGroupFactory.Create("显示设置", displayPanel));

        // ==================== 外观设置 ====================
        var appearancePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        appearancePanel.Children.Add(CreateLabel("缩放:", 0));

        var zoomRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        _zoomSlider = new Slider
        {
            Minimum = SevenSegmentClockSettings.MinZoom,
            Maximum = SevenSegmentClockSettings.MaxZoom,
            SmallChange = 0.01,
            LargeChange = 0.05,
            TickFrequency = 0.01,
            IsSnapToTickEnabled = true,
            Width = 240
        };
        zoomRow.Children.Add(_zoomSlider);

        _zoomValueTextBlock = new TextBlock
        {
            Text = SevenSegmentClockSettings.DefaultZoom.ToString("0.00"),
            FontSize = 12,
            MinWidth = 32,
            VerticalAlignment = VerticalAlignment.Center
        };
        _labelTextBlocks.Add(_zoomValueTextBlock);
        zoomRow.Children.Add(_zoomValueTextBlock);

        appearancePanel.Children.Add(zoomRow);
        appearancePanel.Children.Add(CreateDescription("整体缩放数码管尺寸，范围 0.6~1.0，默认 0.8，步长 0.01。"));

        _skewModeToggle = new ToggleSwitch
        {
            Content = "倾斜模式",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 0)
        };
        appearancePanel.Children.Add(_skewModeToggle);

        appearancePanel.Children.Add(CreateDescription("开启后，纵向数码管向右顺时针倾斜 6°，即数码管的斜体显示效果。"));

        appearancePanel.Children.Add(CreateLabel("强调色:", 6));

        _accentColorPicker = new ColorPicker
        {
            HorizontalAlignment = HorizontalAlignment.Left
        };
        appearancePanel.Children.Add(_accentColorPicker);

        appearancePanel.Children.Add(CreateDescription("默认为红色，用于数码管边框与段内径向渐变的外圈（四周）色。"));

        _separatorBlinkToggle = new ToggleSwitch
        {
            Content = "分离管脚闪动",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 0)
        };
        appearancePanel.Children.Add(_separatorBlinkToggle);

        appearancePanel.Children.Add(CreateDescription("开启后，分隔符（冒号）亮灭交替闪动。默认为开启。"));

        _animationToggle = new ToggleSwitch
        {
            Content = "动画",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 0)
        };
        appearancePanel.Children.Add(_animationToggle);

        appearancePanel.Children.Add(CreateDescription("开启后，发生变化的数码管使用 0.25s 的渐隐、渐显过渡动画。默认为开启。"));

        mainPanel.Children.Add(SettingsGroupFactory.Create("外观设置", appearancePanel));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = mainPanel
        };
        Content = scrollViewer;
    }

    private TextBlock CreateLabel(string text, double topMargin)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, topMargin, 0, 0)
        };
        _labelTextBlocks.Add(textBlock);
        return textBlock;
    }

    private TextBlock CreateDescription(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        _descTextBlocks.Add(textBlock);
        return textBlock;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        UpdateThemeColors();

        // 迁移旧版时间基准值
        var migratedType = TimeBaseTypeHelper.Migrate((int)Settings.TimeBaseType);
        if (migratedType != Settings.TimeBaseType)
        {
            Settings.TimeBaseType = migratedType;
        }

        if (_showSecondsToggle != null)
        {
            _showSecondsToggle.IsChecked = Settings.ShowSeconds;
            _showSecondsToggle.IsCheckedChanged += (_, _) =>
                Settings.ShowSeconds = _showSecondsToggle.IsChecked ?? false;
        }

        if (_timeBaseComboBox != null)
        {
            _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
            {
                TimeBaseType.PluginOffsetServerTime => 0,
                TimeBaseType.RawServerTime => 1,
                TimeBaseType.ClassIslandTime => 2,
                _ => 0
            };
            _timeBaseComboBox.SelectionChanged += (_, _) =>
            {
                Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
                {
                    0 => TimeBaseType.PluginOffsetServerTime,
                    1 => TimeBaseType.RawServerTime,
                    2 => TimeBaseType.ClassIslandTime,
                    _ => TimeBaseType.PluginOffsetServerTime
                };
            };
        }

        if (_skewModeToggle != null)
        {
            _skewModeToggle.IsChecked = Settings.SkewMode;
            _skewModeToggle.IsCheckedChanged += (_, _) =>
                Settings.SkewMode = _skewModeToggle.IsChecked ?? false;
        }

        if (_accentColorPicker != null)
        {
            _accentColorPicker.Color = ParseColor(Settings.AccentColor);
            _accentColorPicker.ColorChanged += (_, _) =>
                Settings.AccentColor = _accentColorPicker.Color.ToString();
        }

        if (_separatorBlinkToggle != null)
        {
            _separatorBlinkToggle.IsChecked = Settings.SeparatorBlink;
            _separatorBlinkToggle.IsCheckedChanged += (_, _) =>
                Settings.SeparatorBlink = _separatorBlinkToggle.IsChecked ?? false;
        }

        if (_zoomSlider != null)
        {
            _zoomSlider.Value = Settings.Zoom;
            UpdateZoomValueText(Settings.Zoom);
            _zoomSlider.PropertyChanged += (_, args) =>
            {
                if (args.Property != Slider.ValueProperty)
                {
                    return;
                }

                // 滑块按 0.01 步长取值，取整避免浮点误差累积
                Settings.Zoom = Math.Round(args.GetNewValue<double>(), 2);
                UpdateZoomValueText(Settings.Zoom);
            };
        }

        if (_animationToggle != null)
        {
            _animationToggle.IsChecked = Settings.EnableTransitionAnimation;
            _animationToggle.IsCheckedChanged += (_, _) =>
                Settings.EnableTransitionAnimation = _animationToggle.IsChecked ?? false;
        }
    }

    private void UpdateZoomValueText(double zoom)
    {
        if (_zoomValueTextBlock != null)
        {
            _zoomValueTextBlock.Text = zoom.ToString("0.00");
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void UpdateThemeColors()
    {
        var textBrush = ThemeHelper.GetTextBrush();
        foreach (var textBlock in _labelTextBlocks)
        {
            textBlock.Foreground = textBrush;
        }

        var descBrush = ThemeHelper.GetGrayBrush();
        foreach (var textBlock in _descTextBlocks)
        {
            textBlock.Foreground = descBrush;
        }

        if (_showSecondsToggle != null) _showSecondsToggle.Foreground = textBrush;
        if (_skewModeToggle != null) _skewModeToggle.Foreground = textBrush;
        if (_separatorBlinkToggle != null) _separatorBlinkToggle.Foreground = textBrush;
        if (_animationToggle != null) _animationToggle.Foreground = textBrush;
    }

    private static Color ParseColor(string colorStr)
    {
        try
        {
            return Color.Parse(colorStr);
        }
        catch
        {
            return Color.Parse(SevenSegmentClockSettings.DefaultAccentColor);
        }
    }
}
