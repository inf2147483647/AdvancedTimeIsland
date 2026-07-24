using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace AdvancedTimeIsland.Views.Settings;

public class TimeCalculatorPage : UserControl
{
    private readonly PluginSettings? _pluginSettings;

    private TextBlock? _titleTextBlock;
    private TextBlock? _minuendTitleTextBlock;
    private TextBlock? _subtrahendTitleTextBlock;
    private TextBlock? _resultTitleTextBlock;

    private List<TextBlock>? _labelTextBlocks;
    private List<Border>? _sectionBorders;

    private TextBox? _minuendYearTextBox;
    private ComboBox? _minuendMonthComboBox;
    private ComboBox? _minuendDayComboBox;
    private TimePicker? _minuendTimePicker;
    private Button? _minuendNowButton;

    private ComboBox? _subDirectionComboBox;
    private TextBox? _subtrahendTextBox;
    private TextBlock? _subtrahendHint;
    private Button? _calculateButton;

    private TextBox? _resultYearTextBox;
    private ComboBox? _resultMonthComboBox;
    private ComboBox? _resultDayComboBox;
    private TimePicker? _resultTimePicker;
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
        _labelTextBlocks = new List<TextBlock>();
        _sectionBorders = new List<Border>();

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        _titleTextBlock = new TextBlock
        {
            Text = "时间计算器",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        mainPanel.Children.Add(_titleTextBlock);

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

        _minuendTitleTextBlock = new TextBlock
        {
            Text = "被减数",
            FontSize = 18,
            Foreground = ThemeHelper.GetTextBrush()
        };
        panel.Children.Add(_minuendTitleTextBlock);

        var datePanel = CreateDatePickerRow("日期输入:", out _minuendYearTextBox, out _minuendMonthComboBox, out _minuendDayComboBox);
        panel.Children.Add(datePanel);

        var timePanel = CreateTimePickerRow("时间:", out _minuendTimePicker);
        panel.Children.Add(timePanel);

        _minuendNowButton = new Button
        {
            Content = "选取当前时间",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 4)
        };
        _minuendNowButton.Click += OnMinuendNowButtonClick;
        panel.Children.Add(_minuendNowButton);

        var border = new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };
        _sectionBorders.Add(border);
        return border;
    }

    private Control CreateSubtrahendSection()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _subtrahendTitleTextBlock = new TextBlock
        {
            Text = "减数",
            FontSize = 18,
            Foreground = ThemeHelper.GetTextBrush()
        };
        panel.Children.Add(_subtrahendTitleTextBlock);

        var inputRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        _subDirectionComboBox = new ComboBox { Width = 100, HorizontalAlignment = HorizontalAlignment.Left };
        _subDirectionComboBox.Items.Add("往后");
        _subDirectionComboBox.Items.Add("往前");
        _subDirectionComboBox.SelectedIndex = 0;
        inputRow.Children.Add(_subDirectionComboBox);

        _subtrahendTextBox = new TextBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Watermark = "例如：1d2h3m4s",
            MinWidth = 200
        };
        inputRow.Children.Add(_subtrahendTextBox);

        panel.Children.Add(inputRow);

        _subtrahendHint = new TextBlock
        {
            Text = "格式化规则：D总天数 H总小时 M总分钟 S总秒 X总毫秒 d天 h小时 m分钟 s秒 yy年 YY总年 mo月 MO总月\n大写小写不共存，大写规则互斥，不能出现3YY6mo的现象",
            FontSize = 11,
            Foreground = ThemeHelper.GetGrayBrush(),
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

        var border = new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };
        _sectionBorders.Add(border);
        return border;
    }

    private Control CreateResultSection()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _resultTitleTextBlock = new TextBlock
        {
            Text = "结果",
            FontSize = 18,
            Foreground = ThemeHelper.GetTextBrush()
        };
        panel.Children.Add(_resultTitleTextBlock);

        var datePanel = CreateDatePickerRow("日期输出:", out _resultYearTextBox, out _resultMonthComboBox, out _resultDayComboBox);
        panel.Children.Add(datePanel);

        var timePanel = CreateTimePickerRow("时间:", out _resultTimePicker);
        panel.Children.Add(timePanel);

        _diffButton = new Button
        {
            Content = "相差（计算）",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 4)
        };
        _diffButton.Click += OnDiffButtonClick;
        panel.Children.Add(_diffButton);

        var border = new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };
        _sectionBorders.Add(border);
        return border;
    }

    private void OnClearButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_minuendYearTextBox != null) _minuendYearTextBox.Text = "";
        if (_minuendMonthComboBox != null) _minuendMonthComboBox.SelectedIndex = -1;
        if (_minuendDayComboBox != null) _minuendDayComboBox.SelectedIndex = -1;
        if (_minuendTimePicker != null) _minuendTimePicker.SelectedTime = null;

        if (_subDirectionComboBox != null) _subDirectionComboBox.SelectedIndex = 0;
        if (_subtrahendTextBox != null) _subtrahendTextBox.Text = "";

        if (_resultYearTextBox != null) _resultYearTextBox.Text = "";
        if (_resultMonthComboBox != null) _resultMonthComboBox.SelectedIndex = -1;
        if (_resultDayComboBox != null) _resultDayComboBox.SelectedIndex = -1;
        if (_resultTimePicker != null) _resultTimePicker.SelectedTime = null;
    }

    private void OnMinuendNowButtonClick(object? sender, RoutedEventArgs e)
    {
        var now = TimeBaseService.Instance?.GetRawServerTime() ?? DateTime.Now;
        SetDateTimeToControls(now, _minuendYearTextBox, _minuendMonthComboBox, _minuendDayComboBox, _minuendTimePicker);
    }

    private static void SetDateTimeToControls(DateTime dt, TextBox? yearTextBox, ComboBox? monthComboBox, ComboBox? dayComboBox, TimePicker? timePicker)
    {
        if (yearTextBox != null) yearTextBox.Text = dt.Year.ToString();
        if (monthComboBox != null) monthComboBox.SelectedItem = $"{dt.Month}月";
        if (dayComboBox != null) dayComboBox.SelectedItem = $"{dt.Day}日";
        if (timePicker != null) timePicker.SelectedTime = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
    }

    private static bool TryGetDateTimeFromControls(TextBox? yearTextBox, ComboBox? monthComboBox, ComboBox? dayComboBox, TimePicker? timePicker, out DateTime result)
    {
        result = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(yearTextBox?.Text) ||
            monthComboBox?.SelectedItem == null ||
            dayComboBox?.SelectedItem == null ||
            timePicker?.SelectedTime == null)
            return false;

        if (!int.TryParse(yearTextBox.Text, out var year))
            return false;
        if (!int.TryParse(monthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return false;
        if (!int.TryParse(dayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day))
            return false;

        var time = timePicker.SelectedTime.Value;

        try
        {
            result = LunarHelper.CreateSafeDateTime(year, month, day, time.Hours, time.Minutes, time.Seconds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnCalculateButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!TryGetDateTimeFromControls(_minuendYearTextBox, _minuendMonthComboBox, _minuendDayComboBox, _minuendTimePicker, out var minuend))
            {
                ShowErrorDialog("请输入有效的日期和时间");
                return;
            }

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

            SetDateTimeToControls(result, _resultYearTextBox, _resultMonthComboBox, _resultDayComboBox, _resultTimePicker);
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
            if (!TryGetDateTimeFromControls(_minuendYearTextBox, _minuendMonthComboBox, _minuendDayComboBox, _minuendTimePicker, out var minuend))
            {
                ShowErrorDialog("请输入有效的被减数日期和时间");
                return;
            }

            if (!TryGetDateTimeFromControls(_resultYearTextBox, _resultMonthComboBox, _resultDayComboBox, _resultTimePicker, out var result))
            {
                ShowErrorDialog("请输入有效的结果日期和时间");
                return;
            }

            var isForward = LunarHelper.Compare(result, minuend) >= 0;

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
        var isForward = LunarHelper.Compare(end, start) >= 0;
        var earlier = isForward ? start : end;
        var later = isForward ? end : start;

        var totalSeconds = LunarHelper.DaysBetween(earlier, later) * 86400;
        
        var earlierTime = earlier.Hour * 3600 + earlier.Minute * 60 + earlier.Second;
        var laterTime = later.Hour * 3600 + later.Minute * 60 + later.Second;
        var timeSeconds = laterTime - earlierTime;
        
        if (timeSeconds < 0)
        {
            timeSeconds += 86400;
            totalSeconds -= 86400;
        }
        
        var totalDays = (int)Math.Floor(totalSeconds / 86400);
        var remainingSeconds = (int)(totalSeconds % 86400);
        
        var hours = remainingSeconds / 3600;
        var minutes = (remainingSeconds % 3600) / 60;
        var seconds = remainingSeconds % 60;

        var years = 0;
        var months = 0;
        var days = totalDays;

        var tempDate = earlier;
        while (days >= 365)
        {
            var nextYear = LunarHelper.SolarAddYears(tempDate, 1);
            var daysInYear = (int)Math.Round(LunarHelper.DaysBetween(tempDate, nextYear));
            if (days >= daysInYear)
            {
                days -= daysInYear;
                years++;
                tempDate = nextYear;
            }
            else
            {
                break;
            }
        }

        while (days >= 28)
        {
            var nextMonth = LunarHelper.SolarAddMonths(tempDate, 1);
            var daysInMonth = (int)Math.Round(LunarHelper.DaysBetween(tempDate, nextMonth));
            if (days >= daysInMonth)
            {
                days -= daysInMonth;
                months++;
                tempDate = nextMonth;
            }
            else
            {
                break;
            }
        }

        var parts = new List<string>();

        if (years > 0) parts.Add($"{years}yy");
        if (months > 0) parts.Add($"{months}mo");
        if (days > 0) parts.Add($"{days}d");
        if (hours > 0) parts.Add($"{hours}h");
        if (minutes > 0) parts.Add($"{minutes}m");
        if (seconds > 0 || parts.Count == 0) parts.Add($"{seconds}s");

        return string.Join("", parts);
    }

    // ===== 1582年历法改革相关方法已注释掉 =====
    // lunar-csharp 的 Solar 类已处理1582年历法改革（消失的10天），无需手动处理
    public static DateTime AdjustForGregorianReform(DateTime date)
    {
        // if (date.Year == 1582 && date.Month == 10 && date.Day >= 5 && date.Day <= 14)
        // {
        //     return new DateTime(1582, 10, 15, date.Hour, date.Minute, date.Second, date.Millisecond);
        // }
        return date;
    }

    private static DateTime AddTimeWithGregorianReform(DateTime baseDate, int years, int months, TimeSpan timeSpan)
    {
        var result = baseDate;

        if (years != 0)
        {
            result = LunarHelper.SolarAddYears(result, years);
        }

        if (months != 0)
        {
            result = LunarHelper.SolarAddMonths(result, months);
        }

        if (timeSpan != TimeSpan.Zero)
        {
            result = LunarHelper.SolarAddTimeSpan(result, timeSpan);
        }

        return result;
    }

    // 1582年跳过方法已注释掉 - lunar-csharp 已处理
    // private static DateTime SkipGregorianGapForward(DateTime date) { ... }
    // private static DateTime SkipGregorianGapBackward(DateTime date) { ... }

    private static TimeSpan CalculateDifferenceWithGregorianReform(DateTime start, DateTime end)
    {
        // 1582年修正已注释掉 - lunar-csharp 已处理
        // start = AdjustForGregorianReform(start);
        // end = AdjustForGregorianReform(end);

        var earlier = start < end ? start : end;
        var later = start < end ? end : start;

        // 直接计算时间差，无需跳过1582年10月5-14日
        return later - earlier;
    }

    private async void ShowErrorDialog(string message)
    {
        try
        {
            var dialog = new Window
            {
                Title = "错误",
                Width = 380,
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

    private StackPanel CreateDatePickerRow(string label, out TextBox yearTextBox, out ComboBox monthComboBox, out ComboBox dayComboBox)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        var labelTextBlock = new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        _labelTextBlocks.Add(labelTextBlock);
        panel.Children.Add(labelTextBlock);

        var datePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        var ytb = new TextBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            Watermark = "年"
        };
        yearTextBox = ytb;
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(ytb, OnYearTextBoxLostFocus);

        var mcb = new ComboBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 1; i <= 12; i++)
        {
            mcb.Items.Add($"{i}月");
        }
        monthComboBox = mcb;

        var dcb = new ComboBox
        {
            Width = 80,
            CornerRadius = new CornerRadius(4),
            SelectedIndex = -1
        };
        for (int i = 1; i <= 31; i++)
        {
            dcb.Items.Add($"{i}日");
        }
        dayComboBox = dcb;

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(ytb, (s, e) => UpdateDayComboBox(ytb, mcb, dcb));
        mcb.SelectionChanged += (s, e) => UpdateDayComboBox(ytb, mcb, dcb);

        datePanel.Children.Add(ytb);
        datePanel.Children.Add(mcb);
        datePanel.Children.Add(dcb);

        panel.Children.Add(datePanel);

        return panel;
    }

    private StackPanel CreateTimePickerRow(string label, out TimePicker timePicker)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };

        var labelTextBlock = new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        _labelTextBlocks.Add(labelTextBlock);
        panel.Children.Add(labelTextBlock);

        timePicker = new TimePicker
        {
            Width = 250,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        panel.Children.Add(timePicker);

        return panel;
    }

    private void OnYearTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (int.TryParse(textBox.Text, out var year))
            {
                if (year < 1) year = 1;
                if (year > 9999) year = 9999;
                textBox.Text = year.ToString();
            }
        }
    }

    private void UpdateDayComboBox(TextBox yearTextBox, ComboBox monthComboBox, ComboBox dayComboBox)
    {
        if (!int.TryParse(yearTextBox.Text?.Trim(), out var year))
            return;
        if (monthComboBox.SelectedItem == null)
            return;
        if (!int.TryParse(monthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return;

        var selectedDayText = dayComboBox.SelectedItem?.ToString();
        int? selectedDay = null;
        if (selectedDayText != null && int.TryParse(selectedDayText.Replace("日", ""), out var d))
            selectedDay = d;

        dayComboBox.Items.Clear();

        if (year == 1582 && month == 10)
        {
            for (int i = 1; i <= 4; i++)
            {
                dayComboBox.Items.Add($"{i}日");
            }
            for (int i = 15; i <= 31; i++)
            {
                dayComboBox.Items.Add($"{i}日");
            }
        }
        else
        {
            var daysInMonth = Lunar.Util.SolarUtil.GetDaysOfMonth(year, month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                dayComboBox.Items.Add($"{i}日");
            }
        }

        if (selectedDay.HasValue)
        {
            var daysInMonth = Lunar.Util.SolarUtil.GetDaysOfMonth(year, month);
            var safeDay = Math.Min(selectedDay.Value, daysInMonth);
            if (year == 1582 && month == 10)
            {
                if (safeDay >= 5 && safeDay <= 14)
                    safeDay = 4;
            }
            dayComboBox.SelectedItem = $"{safeDay}日";
        }
        else
        {
            dayComboBox.SelectedIndex = -1;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
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
        if (_titleTextBlock != null)
            _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();

        if (_minuendTitleTextBlock != null)
            _minuendTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();

        if (_subtrahendTitleTextBlock != null)
            _subtrahendTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();

        if (_resultTitleTextBlock != null)
            _resultTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();

        if (_subtrahendHint != null)
            _subtrahendHint.Foreground = ThemeHelper.GetGrayBrush();

        if (_labelTextBlocks != null)
        {
            foreach (var tb in _labelTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetTextBrush();
            }
        }

        if (_sectionBorders != null)
        {
            foreach (var border in _sectionBorders)
            {
                border.Background = ThemeHelper.GetCardBackgroundBrush();
            }
        }
    }
}



