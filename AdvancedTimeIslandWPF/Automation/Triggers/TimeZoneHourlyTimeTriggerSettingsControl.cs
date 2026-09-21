using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class TimeZoneHourlyTimeTriggerSettingsControl : TriggerSettingsControlBase<TimeZoneHourlyTimeRangeRuleSettings>
{
    private ComboBox _timeZoneComboBox = null!;
    private WpfNumericUpDown _startMinuteNumeric = null!;
    private WpfNumericUpDown _startSecondNumeric = null!;
    private bool _isLoading;
    private bool _hasLoaded;

    public TimeZoneHourlyTimeTriggerSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Dispatcher.BeginInvoke(new Action(() => LoadSettingsToUi()));
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        mainPanel.Children.Add(CreateTimeZoneInputGroup());
        mainPanel.Children.Add(CreateTimePickerGroup("触发时间:"));

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateTimeZoneInputGroup()
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = "时区:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        _timeZoneComboBox = new ComboBox
        {
            Width = 500
        };

        var timeZones = TimeZoneInfo.GetSystemTimeZones();
        foreach (var tz in timeZones)
        {
            _timeZoneComboBox.Items.Add(tz);
        }

        _timeZoneComboBox.SelectionChanged += (s, e) => UpdateTimeZone();

        groupPanel.Children.Add(_timeZoneComboBox);

        return groupPanel;
    }

    private StackPanel CreateTimePickerGroup(string label)
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var numericPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        var minuteNumeric = new WpfNumericUpDown
        {
            Width = 100,
            Minimum = 0,
            Maximum = 59,
            Increment = 1,
            FormatString = "00"
        };

        var secondNumeric = new WpfNumericUpDown
        {
            Width = 100,
            Minimum = 0,
            Maximum = 59,
            Increment = 1,
            FormatString = "00"
        };

        _startMinuteNumeric = minuteNumeric;
        _startSecondNumeric = secondNumeric;

        minuteNumeric.ValueChanged += (s, e) => UpdateSettingsValue();
        secondNumeric.ValueChanged += (s, e) => UpdateSettingsValue();

        numericPanel.Children.Add(new TextBlock
        {
            Text = "分:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });
        numericPanel.Children.Add(minuteNumeric);
        numericPanel.Children.Add(new TextBlock
        {
            Text = "秒:",
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });
        numericPanel.Children.Add(secondNumeric);

        groupPanel.Children.Add(numericPanel);

        groupPanel.Children.Add(new TextBlock
        {
            Text = "仅设置触发时间，不限制结束时间",
            Foreground = Brushes.Gray,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        });

        return groupPanel;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;

        // 加载期间屏蔽 ValueChanged，避免 setter 重入导致读取未初始化的另一个控件
        _isLoading = true;
        try
        {
            foreach (var item in _timeZoneComboBox.Items)
            {
                if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
                {
                    _timeZoneComboBox.SelectedItem = item;
                    break;
                }
            }

            var initialValue = Settings.StartTime;
            ParseTimeString(initialValue, out int minute, out int second);

            _startMinuteNumeric.Value = minute;
            _startSecondNumeric.Value = second;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateTimeZone()
    {
        if (Settings == null) return;
        if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz)
        {
            Settings.TimeZoneId = tz.Id;
        }
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null || _isLoading) return;

        Settings.StartTime = $"{(int)(_startMinuteNumeric.Value ?? 0):D2}-{(int)(_startSecondNumeric.Value ?? 0):D2}";
    }

    private void ParseTimeString(string value, out int minute, out int second)
    {
        minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int m)) minute = m;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int s)) second = s;
    }
}
