using System;
using System.Windows;
using System.Windows.Controls;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 区时每小时时间范围规则设置控件（带时区选择）
/// </summary>
public class TimeZoneHourlyTimeRangeRuleSettingsControl : RuleSettingsControlBase<TimeZoneHourlyTimeRangeRuleSettings>
{
    private ComboBox _timeZoneComboBox = null!;
    private TextBox _startMinuteBox = null!;
    private TextBox _startSecondBox = null!;
    private TextBox _endMinuteBox = null!;
    private TextBox _endSecondBox = null!;
    private bool _isLoading;
    private bool _hasLoaded;

    public TimeZoneHourlyTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Dispatcher.BeginInvoke(new Action(() => LoadSettingsToUi()));
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
    }

    private void LoadSettingsToUi()
    {
        if (_hasLoaded) return;
if (Settings == null) return;

        _hasLoaded = true;
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

            ParseTimeString(Settings.StartTime, out int startMinute, out int startSecond);
            _startMinuteBox.Text = startMinute.ToString("D2");
            _startSecondBox.Text = startSecond.ToString("D2");

            ParseTimeString(Settings.EndTime, out int endMinute, out int endSecond);
            _endMinuteBox.Text = endMinute.ToString("D2");
            _endSecondBox.Text = endSecond.ToString("D2");
        }
        finally
        {
            _isLoading = false;
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        if (Settings != null)
        {
            foreach (var item in _timeZoneComboBox.Items)
            {
                if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
                {
                    _timeZoneComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 时区选择下拉框
        mainPanel.Children.Add(CreateTimeZoneInputGroup());

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

    private StackPanel CreateInputGroup(string label, bool isStart)
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

        var inputPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        var minuteBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var secondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
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

        var initialValue = isStart ? Settings?.StartTime ?? "" : Settings?.EndTime ?? "";
        ParseTimeString(initialValue, out int minute, out int second);

        minuteBox.Text = minute.ToString("D2");
        secondBox.Text = second.ToString("D2");

        minuteBox.TextChanged += (s, e) => UpdateSettingsValue();
        secondBox.TextChanged += (s, e) => UpdateSettingsValue();

        // 失去焦点时验证并格式化
        minuteBox.LostFocus += (s, e) => ValidateAndFormatTextBox(minuteBox);
        secondBox.LostFocus += (s, e) => ValidateAndFormatTextBox(secondBox);

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

    private void UpdateTimeZone()
    {
        if (Settings == null) return;
        if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz)
        {
            Settings.TimeZoneId = tz.Id;
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
        if (_isLoading) return;
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
