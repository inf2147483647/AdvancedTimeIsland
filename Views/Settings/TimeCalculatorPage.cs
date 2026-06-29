using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Settings;

public class TimeCalculatorPage : UserControl
{
    private readonly PluginSettings? _pluginSettings;

    private DatePicker? _minuendDatePicker;
    private ComboBox? _minuendHourComboBox;
    private ComboBox? _minuendMinuteComboBox;
    private ComboBox? _minuendSecondComboBox;
    private Button? _minuendNowButton;

    private ComboBox? _subDirectionComboBox;
    private TextBox? _subtrahendTextBox;
    private TextBlock? _subtrahendHint;
    private Button? _calculateButton;

    private DatePicker? _resultDatePicker;
    private ComboBox? _resultHourComboBox;
    private ComboBox? _resultMinuteComboBox;
    private ComboBox? _resultSecondComboBox;
    private Button? _diffButton;

    private Button? _clearButton;

    public TimeCalculatorPage() : this(null)
    {
    }

    public TimeCalculatorPage(PluginSettings? pluginSettings)
    {
        _pluginSettings = pluginSettings;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        var titleText = new TextBlock
        {
            Text = "时间计算器",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };
        mainPanel.Children.Add(titleText);

        _clearButton = new Button
        {
            Content = "清空",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(16, 6)
        };
        _clearButton.Click += OnClearButtonClick;
        mainPanel.Children.Add(_clearButton);

        mainPanel.Children.Add(CreateMinuendSection());
        mainPanel.Children.Add(CreateSubtrahendSection());
        mainPanel.Children.Add(CreateResultSection());

        var scrollViewer = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        Content = scrollViewer;
    }

    private Control CreateMinuendSection()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        panel.Children.Add(new TextBlock
        {
            Text = "被减数",
            FontSize = 18,
            Foreground = Brushes.White
        });

        var dateTimeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        _minuendDatePicker = new DatePicker { HorizontalAlignment = HorizontalAlignment.Left };
        dateTimeRow.Children.Add(_minuendDatePicker);

        var timePanel = CreateTimeComboBoxes(
            out _minuendHourComboBox,
            out _minuendMinuteComboBox,
            out _minuendSecondComboBox);
        dateTimeRow.Children.Add(timePanel);

        panel.Children.Add(dateTimeRow);

        _minuendNowButton = new Button
        {
            Content = "选取当前时间",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 4)
        };
        _minuendNowButton.Click += OnMinuendNowButtonClick;
        panel.Children.Add(_minuendNowButton);

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };
    }

    private Control CreateSubtrahendSection()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        panel.Children.Add(new TextBlock
        {
            Text = "减数",
            FontSize = 18,
            Foreground = Brushes.White
        });

        var inputRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("180,*")
        };

        _subDirectionComboBox = new ComboBox { Width = 180, HorizontalAlignment = HorizontalAlignment.Left };
        _subDirectionComboBox.Items.Add("往后");
        _subDirectionComboBox.Items.Add("往前");
        _subDirectionComboBox.SelectedIndex = 0;
        Grid.SetColumn(_subDirectionComboBox, 0);
        inputRow.Children.Add(_subDirectionComboBox);

        _subtrahendTextBox = new TextBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Watermark = "例如：1d2h3m4s"
        };
        Grid.SetColumn(_subtrahendTextBox, 1);
        inputRow.Children.Add(_subtrahendTextBox);

        panel.Children.Add(inputRow);

        _subtrahendHint = new TextBlock
        {
            Text = "格式化规则：D总天数 H总小时 M总分钟 S总秒 X总毫秒 d天 h小时 m分钟 s秒 yy年 YY总年 mo月 MO总月\n大写小写不共存，大写规则互斥，不能出现3YY6mo的现象",
            FontSize = 11,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(_subtrahendHint);

        _calculateButton = new Button
        {
            Content = "是（计算）",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 4)
        };
        _calculateButton.Click += OnCalculateButtonClick;
        panel.Children.Add(_calculateButton);

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };
    }

    private Control CreateResultSection()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        panel.Children.Add(new TextBlock
        {
            Text = "结果",
            FontSize = 18,
            Foreground = Brushes.White
        });

        var dateTimeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        _resultDatePicker = new DatePicker { HorizontalAlignment = HorizontalAlignment.Left };
        dateTimeRow.Children.Add(_resultDatePicker);

        var timePanel = CreateTimeComboBoxes(
            out _resultHourComboBox,
            out _resultMinuteComboBox,
            out _resultSecondComboBox);
        dateTimeRow.Children.Add(timePanel);

        panel.Children.Add(dateTimeRow);

        _diffButton = new Button
        {
            Content = "相差（计算）",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 4)
        };
        _diffButton.Click += OnDiffButtonClick;
        panel.Children.Add(_diffButton);

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };
    }

    private Control CreateTimeComboBoxes(out ComboBox? hour, out ComboBox? minute, out ComboBox? second)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        hour = new ComboBox { Width = 60 };
        for (int i = 0; i < 24; i++) hour.Items.Add(i.ToString("D2"));
        hour.SelectedIndex = 0;
        panel.Children.Add(hour);

        panel.Children.Add(new TextBlock
        {
            Text = ":",
            FontSize = 16,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        minute = new ComboBox { Width = 60 };
        for (int i = 0; i < 60; i++) minute.Items.Add(i.ToString("D2"));
        minute.SelectedIndex = 0;
        panel.Children.Add(minute);

        panel.Children.Add(new TextBlock
        {
            Text = ":",
            FontSize = 16,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        second = new ComboBox { Width = 60 };
        for (int i = 0; i < 60; i++) second.Items.Add(i.ToString("D2"));
        second.SelectedIndex = 0;
        panel.Children.Add(second);

        return panel;
    }

    private void OnClearButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_minuendDatePicker != null) _minuendDatePicker.SelectedDate = null;
        if (_minuendHourComboBox != null) _minuendHourComboBox.SelectedIndex = 0;
        if (_minuendMinuteComboBox != null) _minuendMinuteComboBox.SelectedIndex = 0;
        if (_minuendSecondComboBox != null) _minuendSecondComboBox.SelectedIndex = 0;

        if (_subDirectionComboBox != null) _subDirectionComboBox.SelectedIndex = 0;
        if (_subtrahendTextBox != null) _subtrahendTextBox.Text = "";

        if (_resultDatePicker != null) _resultDatePicker.SelectedDate = null;
        if (_resultHourComboBox != null) _resultHourComboBox.SelectedIndex = 0;
        if (_resultMinuteComboBox != null) _resultMinuteComboBox.SelectedIndex = 0;
        if (_resultSecondComboBox != null) _resultSecondComboBox.SelectedIndex = 0;
    }

    private void OnMinuendNowButtonClick(object? sender, RoutedEventArgs e)
    {
        var now = TimeBaseService.Instance?.GetRawServerTime() ?? DateTime.Now;
        SetDateTimeToControls(now, _minuendDatePicker, _minuendHourComboBox, _minuendMinuteComboBox, _minuendSecondComboBox);
    }

    private static void SetDateTimeToControls(DateTime dt, DatePicker? datePicker, ComboBox? hourCb, ComboBox? minuteCb, ComboBox? secondCb)
    {
        dt = AdjustForGregorianReform(dt);

        if (datePicker != null) datePicker.SelectedDate = dt.Date;
        if (hourCb != null) hourCb.SelectedIndex = dt.Hour;
        if (minuteCb != null) minuteCb.SelectedIndex = dt.Minute;
        if (secondCb != null) secondCb.SelectedIndex = dt.Second;
    }

    private static DateTime GetDateTimeFromControls(DatePicker? datePicker, ComboBox? hourCb, ComboBox? minuteCb, ComboBox? secondCb)
    {
        var date = datePicker?.SelectedDate?.Date ?? DateTime.Today;
        var hour = hourCb?.SelectedIndex ?? 0;
        var minute = minuteCb?.SelectedIndex ?? 0;
        var second = secondCb?.SelectedIndex ?? 0;

        var dt = date.Add(new TimeSpan(hour, minute, second));
        return AdjustForGregorianReform(dt);
    }

    private void OnCalculateButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var minuend = GetDateTimeFromControls(
                _minuendDatePicker, _minuendHourComboBox, _minuendMinuteComboBox, _minuendSecondComboBox);

            var isForward = _subDirectionComboBox?.SelectedIndex == 0;
            var formatStr = _subtrahendTextBox?.Text ?? "";

            var parsed = ParseTimeFormat(formatStr);

            DateTime result;
            if (isForward)
            {
                result = AddTimeWithGregorianReform(minuend, parsed.Years, parsed.Months, parsed.TimeSpan);
            }
            else
            {
                result = AddTimeWithGregorianReform(minuend, -parsed.Years, -parsed.Months, -parsed.TimeSpan);
            }

            SetDateTimeToControls(result, _resultDatePicker, _resultHourComboBox, _resultMinuteComboBox, _resultSecondComboBox);
        }
        catch (Exception ex)
        {
            ShowErrorDialog($"计算失败: {ex.Message}");
        }
    }

    private void OnDiffButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var minuend = GetDateTimeFromControls(
                _minuendDatePicker, _minuendHourComboBox, _minuendMinuteComboBox, _minuendSecondComboBox);

            var result = GetDateTimeFromControls(
                _resultDatePicker, _resultHourComboBox, _resultMinuteComboBox, _resultSecondComboBox);

            var isForward = result >= minuend;
            var diff = CalculateDifferenceWithGregorianReform(minuend, result);

            var formatStr = FormatTimeDiffLowercase(minuend, result);

            if (_subDirectionComboBox != null)
                _subDirectionComboBox.SelectedIndex = isForward ? 0 : 1;

            if (_subtrahendTextBox != null)
                _subtrahendTextBox.Text = formatStr;
        }
        catch (Exception ex)
        {
            ShowErrorDialog($"计算失败: {ex.Message}");
        }
    }

    private struct ParsedTime
    {
        public int Years;
        public int Months;
        public TimeSpan TimeSpan;
    }

    private static ParsedTime ParseTimeFormat(string input)
    {
        var result = new ParsedTime { Years = 0, Months = 0, TimeSpan = TimeSpan.Zero };

        if (string.IsNullOrWhiteSpace(input))
            return result;

        input = input.Trim();

        var upperUnits = new List<string>();
        var lowerUnits = new List<string>();

        var allUnits = new[] { "YY", "MO", "D", "H", "M", "S", "X", "yy", "mo", "d", "h", "m", "s", "x" };

        foreach (var unit in allUnits)
        {
            string numPattern;
            if (char.IsUpper(unit[0]))
                numPattern = @"(\d+(?:\.\d+)?)";
            else
                numPattern = @"(\d+)";
            var pattern = $@"{numPattern}\s*{unit}(?![a-zA-Z])";
            if (Regex.IsMatch(input, pattern))
            {
                if (char.IsUpper(unit[0]))
                    upperUnits.Add(unit);
                else
                    lowerUnits.Add(unit);
            }
        }

        if (upperUnits.Count > 0 && lowerUnits.Count > 0)
        {
            throw new ArgumentException("大写和小写格式符不能同时使用");
        }

        if (upperUnits.Count > 1)
        {
            throw new ArgumentException("大写格式符互斥，只能使用一个大写单位");
        }

        double totalMilliseconds = 0;
        int years = 0;
        int months = 0;

        var patterns = new (string Pattern, Action<double> Setter)[]
        {
            (@"(\d+(?:\.\d+)?)\s*YY(?![a-zA-Z])", v => totalMilliseconds += v * 86400000 * 365),
            (@"(\d+(?:\.\d+)?)\s*MO(?![a-zA-Z])", v => totalMilliseconds += v * 86400000 * 30),
            (@"(\d+(?:\.\d+)?)\s*D(?![a-zA-Z])", v => totalMilliseconds += v * 86400000),
            (@"(\d+(?:\.\d+)?)\s*H(?![a-zA-Z])", v => totalMilliseconds += v * 3600000),
            (@"(\d+(?:\.\d+)?)\s*M(?![a-zA-Z])", v => totalMilliseconds += v * 60000),
            (@"(\d+(?:\.\d+)?)\s*S(?![a-zA-Z])", v => totalMilliseconds += v * 1000),
            (@"(\d+(?:\.\d+)?)\s*X(?![a-zA-Z])", v => totalMilliseconds += v),
            (@"(\d+)\s*yy(?![a-zA-Z])", v => years = (int)v),
            (@"(\d+)\s*mo(?![a-zA-Z])", v => months = (int)v),
            (@"(\d+)\s*d(?![a-zA-Z])", v => totalMilliseconds += v * 86400000),
            (@"(\d+)\s*h(?![a-zA-Z])", v => totalMilliseconds += v * 3600000),
            (@"(\d+)\s*m(?![a-zA-Z])", v => totalMilliseconds += v * 60000),
            (@"(\d+)\s*s(?![a-zA-Z])", v => totalMilliseconds += v * 1000),
            (@"(\d+)\s*x(?![a-zA-Z])", v => totalMilliseconds += v),
        };

        foreach (var (pattern, setter) in patterns)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    setter(value);
                }
            }
        }

        result.Years = years;
        result.Months = months;
        result.TimeSpan = TimeSpan.FromMilliseconds(totalMilliseconds);

        return result;
    }

    private static string FormatTimeDiffLowercase(DateTime start, DateTime end)
    {
        var earlier = start < end ? start : end;
        var later = start < end ? end : start;

        var years = later.Year - earlier.Year;
        var months = later.Month - earlier.Month;
        var days = later.Day - earlier.Day;

        if (days < 0)
        {
            months--;
            var prevMonth = later.AddMonths(-1);
            days += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        }

        if (months < 0)
        {
            years--;
            months += 12;
        }

        var timeDiff = later.TimeOfDay - earlier.TimeOfDay;
        if (timeDiff < TimeSpan.Zero)
        {
            timeDiff += TimeSpan.FromDays(1);
            days--;
            if (days < 0)
            {
                months--;
                var prevMonth = later.AddMonths(-1);
                days += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
            }
            if (months < 0)
            {
                years--;
                months += 12;
            }
        }

        var hours = timeDiff.Hours;
        var minutes = timeDiff.Minutes;
        var seconds = timeDiff.Seconds;

        var parts = new List<string>();

        if (years > 0) parts.Add($"{years}yy");
        if (months > 0) parts.Add($"{months}mo");
        if (days > 0) parts.Add($"{days}d");
        if (hours > 0) parts.Add($"{hours}h");
        if (minutes > 0) parts.Add($"{minutes}m");
        if (seconds > 0 || parts.Count == 0) parts.Add($"{seconds}s");

        return string.Join("", parts);
    }

    public static DateTime AdjustForGregorianReform(DateTime date)
    {
        if (date.Year == 1582 && date.Month == 10 && date.Day >= 5 && date.Day <= 14)
        {
            return new DateTime(1582, 10, 15, date.Hour, date.Minute, date.Second, date.Millisecond);
        }
        return date;
    }

    private static DateTime AddTimeWithGregorianReform(DateTime baseDate, int years, int months, TimeSpan timeSpan)
    {
        var result = baseDate;

        if (years != 0)
        {
            result = result.AddYears(years);
            result = AdjustForGregorianReform(result);
        }

        if (months != 0)
        {
            result = result.AddMonths(months);
            result = AdjustForGregorianReform(result);
        }

        if (timeSpan != TimeSpan.Zero)
        {
            var daysToAdd = (int)timeSpan.TotalDays;
            var remaining = timeSpan - TimeSpan.FromDays(daysToAdd);

            for (int i = 0; i < Math.Abs(daysToAdd); i++)
            {
                if (daysToAdd > 0)
                {
                    result = result.AddDays(1);
                    result = SkipGregorianGapForward(result);
                }
                else
                {
                    result = result.AddDays(-1);
                    result = SkipGregorianGapBackward(result);
                }
            }

            result = result.Add(remaining);
        }

        return result;
    }

    private static DateTime SkipGregorianGapForward(DateTime date)
    {
        if (date.Year == 1582 && date.Month == 10 && date.Day == 4)
        {
            return new DateTime(1582, 10, 15, date.Hour, date.Minute, date.Second, date.Millisecond);
        }
        return date;
    }

    private static DateTime SkipGregorianGapBackward(DateTime date)
    {
        if (date.Year == 1582 && date.Month == 10 && date.Day == 15)
        {
            return new DateTime(1582, 10, 4, date.Hour, date.Minute, date.Second, date.Millisecond);
        }
        return date;
    }

    private static TimeSpan CalculateDifferenceWithGregorianReform(DateTime start, DateTime end)
    {
        start = AdjustForGregorianReform(start);
        end = AdjustForGregorianReform(end);

        var earlier = start < end ? start : end;
        var later = start < end ? end : start;

        long totalDays = 0;
        var current = earlier.Date;
        var target = later.Date;

        while (current < target)
        {
            current = current.AddDays(1);
            if (!(current.Year == 1582 && current.Month == 10 && current.Day >= 5 && current.Day <= 14))
            {
                totalDays++;
            }
        }

        var timeDiff = later.TimeOfDay - earlier.TimeOfDay;
        return TimeSpan.FromDays(totalDays) + timeDiff;
    }

    private async void ShowErrorDialog(string message)
    {
        try
        {
            var dialog = new Window
            {
                Title = "错误",
                Width = 300,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var okButton = new Button
            {
                Content = "确定",
                Padding = new Thickness(16, 8),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(16),
                Spacing = 12
            };
            stack.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Red
            });
            stack.Children.Add(okButton);

            dialog.Content = stack;
            okButton.Click += (s, e) => dialog.Close();

            if (VisualRoot is Window owner)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                dialog.Show();
            }
        }
        catch
        {
        }
    }
}
