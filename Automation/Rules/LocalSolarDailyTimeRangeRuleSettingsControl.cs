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
/// 地方时每天时间范围规则设置控件（带经度输入）
/// </summary>
public class LocalSolarDailyTimeRangeRuleSettingsControl : RuleSettingsControlBase<LocalSolarDailyTimeRangeRuleSettings>
{
    private TextBox _longitudeBox = null!;
    private TextBox _dmsDegreesBox = null!;
    private TextBox _dmsMinutesBox = null!;
    private TextBox _dmsSecondsBox = null!;
    private ComboBox _dmsDirectionBox = null!;
    private Panel _dmsPanel = null!;
    private TimePicker _startTimePicker = null!;
    private TimePicker _endTimePicker = null!;
    private readonly PluginSettings? _pluginSettings;

    public LocalSolarDailyTimeRangeRuleSettingsControl() : this(null)
    {
    }

    public LocalSolarDailyTimeRangeRuleSettingsControl(PluginSettings? pluginSettings = null)
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
        ParseTimeString(startInitialValue, out int startHour, out int startMinute, out int startSecond);
        _startTimePicker.SelectedTime = new TimeSpan(startHour, startMinute, startSecond);

        var endInitialValue = Settings.EndTime;
        ParseTimeString(endInitialValue, out int endHour, out int endMinute, out int endSecond);
        _endTimePicker.SelectedTime = new TimeSpan(endHour, endMinute, endSecond);
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
        mainPanel.Children.Add(CreateTimePickerGroup("开始时间:", true));

        // 结束时间
        mainPanel.Children.Add(CreateTimePickerGroup("结束时间:", false));

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

    private StackPanel CreateTimePickerGroup(string label, bool isStart)
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

        // 时间选择器
        var timePicker = new TimePicker
        {
            Width = 260,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        if (isStart)
        {
            _startTimePicker = timePicker;
        }
        else
        {
            _endTimePicker = timePicker;
        }

        // 监听变化
        timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        // 用ScrollViewer包裹
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = timePicker
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

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        // 开始时间
        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        // 结束时间
        var endTime = _endTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.EndTime = $"{endTime.Hours:D2}-{endTime.Minutes:D2}-{endTime.Seconds:D2}";
    }

    private void ParseTimeString(string value, out int hour, out int minute, out int second)
    {
        hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int h)) hour = h;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int mi)) minute = mi;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int s)) second = s;
    }
}



