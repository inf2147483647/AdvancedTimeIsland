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
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 地方时每小时时间范围规则设置控件（带经度输入）
/// </summary>
public class LocalSolarHourlyTimeRangeRuleSettingsControl : RuleSettingsControlBase<LocalSolarHourlyTimeRangeRuleSettings>
{
    private TextBox _longitudeBox = null!;
    private TextBox _dmsDegreesBox = null!;
    private TextBox _dmsMinutesBox = null!;
    private TextBox _dmsSecondsBox = null!;
    private ComboBox _dmsDirectionBox = null!;
    private Panel _dmsPanel = null!;
    private TextBox _startMinuteBox = null!;
    private TextBox _startSecondBox = null!;
    private TextBox _endMinuteBox = null!;
    private TextBox _endSecondBox = null!;
    private readonly PluginSettings? _pluginSettings;

    public LocalSolarHourlyTimeRangeRuleSettingsControl() : this(null)
    {
    }

    public LocalSolarHourlyTimeRangeRuleSettingsControl(PluginSettings? pluginSettings = null)
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

        var startInitialValue = Settings.StartTime;
        ParseTimeString(startInitialValue, out int startMinute, out int startSecond);
        _startMinuteBox.Text = startMinute.ToString("D2");
        _startSecondBox.Text = startSecond.ToString("D2");

        var endInitialValue = Settings.EndTime;
        ParseTimeString(endInitialValue, out int endMinute, out int endSecond);
        _endMinuteBox.Text = endMinute.ToString("D2");
        _endSecondBox.Text = endSecond.ToString("D2");
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

        // 经度输入框
        mainPanel.Children.Add(CreateLongitudeInputGroup());

        // 开始时间
        mainPanel.Children.Add(CreateInputGroup("开始时间:", true));

        // 结束时间
        mainPanel.Children.Add(CreateInputGroup("结束时间:", false));

                Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
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

    private StackPanel CreateInputGroup(string label, bool isStart)
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

        var inputPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var minuteBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "分钟 (0-59)"
        };

        var secondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Watermark = "秒数 (0-59)"
        };

        if (isStart)
        {
            _startMinuteBox = minuteBox;
            _startSecondBox = secondBox;
        }
        else
        {
            _endMinuteBox = minuteBox;
            _endSecondBox = secondBox;
        }

        minuteBox.TextChanged += (s, e) => UpdateSettingsValue();
        secondBox.TextChanged += (s, e) => UpdateSettingsValue();

        // 失去焦点时验证并格式化
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(minuteBox, (s, e) => ValidateAndFormatTextBox(minuteBox));
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(secondBox, (s, e) => ValidateAndFormatTextBox(secondBox));

        inputPanel.Children.Add(minuteBox);
        inputPanel.Children.Add(secondBox);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = inputPanel
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

        int startMinute = ParseMinute(_startMinuteBox.Text);
        int startSecond = ParseSecond(_startSecondBox.Text);
        Settings.StartTime = $"{startMinute:D2}-{startSecond:D2}";

        int endMinute = ParseMinute(_endMinuteBox.Text);
        int endSecond = ParseSecond(_endSecondBox.Text);
        Settings.EndTime = $"{endMinute:D2}-{endSecond:D2}";
    }

    private int ParseMinute(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }

    private int ParseSecond(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }

    private void ParseTimeString(string value, out int minute, out int second)
    {
        minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int mi)) minute = mi;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int s)) second = s;
    }
}



