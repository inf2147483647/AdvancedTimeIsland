using System;
using System.ComponentModel;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

/// <summary>
/// 地方时每分钟时间触发器设置控件（触发器版，仅开始秒数）
/// </summary>
public class LocalSolarMinutelyTimeTriggerSettingsControl : TriggerSettingsControlBase<LocalSolarMinutelyTimeRangeRuleSettings>
{
    private TextBox _longitudeBox = null!;
    private TextBox _dmsDegreesBox = null!;
    private TextBox _dmsMinutesBox = null!;
    private TextBox _dmsSecondsBox = null!;
    private ComboBox _dmsDirectionBox = null!;
    private Panel _dmsPanel = null!;
    private TextBox _startSecondBox = null!;
    private readonly PluginSettings? _pluginSettings;

    public LocalSolarMinutelyTimeTriggerSettingsControl() : this(null)
    {
    }

    public LocalSolarMinutelyTimeTriggerSettingsControl(PluginSettings? pluginSettings = null)
    {
        _pluginSettings = pluginSettings;
        if (_pluginSettings != null)
        {
            _pluginSettings.PropertyChanged += OnPluginSettingsPropertyChanged;
        }
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
    }

    private void OnPluginSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.LongitudeDisplayMode))
        {
            UpdateLongitudeDisplay();
        }
    }

    private void UpdateLongitudeDisplay()
    {
        if (_pluginSettings == null) return;
        var isDms = _pluginSettings.LongitudeDisplayMode == LongitudeDisplayMode.Dms;
        _longitudeBox.IsVisible = !isDms;
        _dmsPanel.IsVisible = isDms;
        if (isDms)
        {
            UpdateDmsFromLongitude();
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateLongitudeDisplay();
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        _longitudeBox.Text = Settings.Longitude.ToString("F4");
        UpdateDmsFromLongitude();

        var initialValue = Settings.StartSecond;
        if (int.TryParse(initialValue, out int second))
        {
            _startSecondBox.Text = second.ToString("D2");
        }
        else
        {
            _startSecondBox.Text = "00";
        }
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        mainPanel.Children.Add(CreateLongitudeInputGroup());
        mainPanel.Children.Add(CreateInputGroup("触发秒数:"));
        mainPanel.Children.Add(CreateDescriptionText("设置触发发生的秒数"));

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private TextBlock CreateDescriptionText(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Brushes.Gray,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private StackPanel CreateLongitudeInputGroup()
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = "经度:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        var isDms = _pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms;

        _longitudeBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "经度 (-180~180)",
            IsVisible = !isDms
        };

        _longitudeBox.TextChanged += (s, e) => UpdateLongitude();

        _dmsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Left,
            IsVisible = isDms
        };

        _dmsDegreesBox = new TextBox { Width = 50, Watermark = "度" };
        _dmsDegreesBox.TextChanged += (s, e) => UpdateLongitudeFromDms();
        _dmsPanel.Children.Add(_dmsDegreesBox);
        _dmsPanel.Children.Add(new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center });

        _dmsMinutesBox = new TextBox { Width = 45, Watermark = "分" };
        _dmsMinutesBox.TextChanged += (s, e) => UpdateLongitudeFromDms();
        _dmsPanel.Children.Add(_dmsMinutesBox);
        _dmsPanel.Children.Add(new TextBlock { Text = "′", VerticalAlignment = VerticalAlignment.Center });

        _dmsSecondsBox = new TextBox { Width = 45, Watermark = "秒" };
        _dmsSecondsBox.TextChanged += (s, e) => UpdateLongitudeFromDms();
        _dmsPanel.Children.Add(_dmsSecondsBox);
        _dmsPanel.Children.Add(new TextBlock { Text = "″", VerticalAlignment = VerticalAlignment.Center });

        _dmsDirectionBox = new ComboBox { Width = 90 };
        _dmsDirectionBox.Items.Add("东经");
        _dmsDirectionBox.Items.Add("西经");
        _dmsDirectionBox.SelectedIndex = 0;
        _dmsDirectionBox.SelectionChanged += (s, e) => UpdateLongitudeFromDms();
        _dmsPanel.Children.Add(_dmsDirectionBox);

        var container = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };
        container.Children.Add(_longitudeBox);
        container.Children.Add(_dmsPanel);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = container
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private StackPanel CreateInputGroup(string label)
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _startSecondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "秒数 (0-59)"
        };

        _startSecondBox.TextChanged += (s, e) => UpdateSettingsValue();
        _startSecondBox.LostFocus += (s, e) => ValidateAndFormatTextBox(_startSecondBox);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _startSecondBox
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private void UpdateDmsFromLongitude()
    {
        if (Settings == null) return;
        LongitudeConverter.DecomposeDms(Settings.Longitude, out int d, out int m, out double s, out bool isEast);
        _dmsDegreesBox.Text = d.ToString();
        _dmsMinutesBox.Text = m.ToString();
        _dmsSecondsBox.Text = s.ToString("F2");
        _dmsDirectionBox.SelectedIndex = isEast ? 0 : 1;
    }

    private void UpdateLongitudeFromDms()
    {
        if (Settings == null) return;
        if (!int.TryParse(_dmsDegreesBox.Text, out int d)) d = 0;
        if (!int.TryParse(_dmsMinutesBox.Text, out int m)) m = 0;
        if (!double.TryParse(_dmsSecondsBox.Text, out double s)) s = 0;
        var isEast = _dmsDirectionBox.SelectedIndex == 0;
        if (LongitudeConverter.TryParseDms(d, m, s, isEast, out double lon))
        {
            Settings.Longitude = lon;
            _longitudeBox.Text = LongitudeConverter.ToDecimalString(lon);
        }
    }

    private void UpdateLongitude()
    {
        if (Settings == null) return;
        if (double.TryParse(_longitudeBox.Text, out double lon))
        {
            Settings.Longitude = Math.Clamp(lon, -180.0, 180.0);
            UpdateDmsFromLongitude();
        }
    }

    private void ValidateAndFormatTextBox(TextBox textBox)
    {
        var text = textBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            textBox.Text = "00";
            return;
        }

        if (double.TryParse(text, out double value))
        {
            int rounded = (int)Math.Round(value);
            int clamped = Math.Clamp(rounded, 0, 59);
            textBox.Text = clamped.ToString("D2");
        }
        else
        {
            textBox.Text = "00";
        }
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        int startSecond = ParseSecond(_startSecondBox.Text);
        Settings.StartSecond = $"{startSecond:D2}";
    }

    private int ParseSecond(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }
}
