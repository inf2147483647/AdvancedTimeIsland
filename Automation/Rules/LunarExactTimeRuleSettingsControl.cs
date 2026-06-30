using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class LunarExactTimeRuleSettingsControl : RuleSettingsControlBase<LunarExactTimeRuleSettings>
{
    private ComboBox _tianganComboBox = null!;
    private ComboBox _dizhiComboBox = null!;
    private ComboBox _yearRangeComboBox = null!;
    private ComboBox _lunarMonthComboBox = null!;
    private CheckBox _leapMonthCheckBox = null!;
    private ComboBox _lunarDayComboBox = null!;
    private TimePicker _timePicker = null!;

    public LunarExactTimeRuleSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
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
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var yearPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var yearRangePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        yearRangePanel.Children.Add(new TextBlock
        {
            Text = "年份范围:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 60
        });

        _yearRangeComboBox = new ComboBox
        {
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            _yearRangeComboBox.Items.Add(range);
        }

        yearRangePanel.Children.Add(_yearRangeComboBox);
        yearPanel.Children.Add(yearRangePanel);

        var tgDzPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        tgDzPanel.Children.Add(new TextBlock
        {
            Text = "天干:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
        });

        _tianganComboBox = new ComboBox
        {
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var tg in LunarCalendarHelper.GetAllTiangan())
        {
            _tianganComboBox.Items.Add(tg);
        }

        tgDzPanel.Children.Add(_tianganComboBox);

        tgDzPanel.Children.Add(new TextBlock
        {
            Text = "地支:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
        });

        _dizhiComboBox = new ComboBox
        {
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var dz in LunarCalendarHelper.GetAllDizhi())
        {
            _dizhiComboBox.Items.Add(dz);
        }

        tgDzPanel.Children.Add(_dizhiComboBox);
        yearPanel.Children.Add(tgDzPanel);

        _tianganComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        _dizhiComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        _yearRangeComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        mainPanel.Children.Add(yearPanel);

        var monthPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        monthPanel.Children.Add(new TextBlock
        {
            Text = "农历月:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _lunarMonthComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 12; i++)
        {
            _lunarMonthComboBox.Items.Add(i.ToString());
        }

        _lunarMonthComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        monthPanel.Children.Add(_lunarMonthComboBox);

        _leapMonthCheckBox = new CheckBox
        {
            Content = "闰月",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _leapMonthCheckBox.Checked += (s, e) => UpdateSettingsValue();
        _leapMonthCheckBox.Unchecked += (s, e) => UpdateSettingsValue();

        monthPanel.Children.Add(_leapMonthCheckBox);
        mainPanel.Children.Add(monthPanel);

        var dayPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        dayPanel.Children.Add(new TextBlock
        {
            Text = "农历日:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _lunarDayComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 30; i++)
        {
            _lunarDayComboBox.Items.Add(i.ToString());
        }

        _lunarDayComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        dayPanel.Children.Add(_lunarDayComboBox);
        mainPanel.Children.Add(dayPanel);

        var timePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        timePanel.Children.Add(new TextBlock
        {
            Text = "时间:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _timePicker = new TimePicker
        {
            Width = 300,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _timePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        timePanel.Children.Add(_timePicker);
        mainPanel.Children.Add(timePanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private void LoadSettingsToUi()
    {
        if (Settings != null && Settings.LunarYear > 0)
        {
            var tg = LunarCalendarHelper.GetTiangan(Settings.LunarYear);
            var dz = LunarCalendarHelper.GetDizhi(Settings.LunarYear);
            _tianganComboBox.SelectedItem = tg;
            _dizhiComboBox.SelectedItem = dz;

            if (Settings.LunarYear >= 1901 && Settings.LunarYear <= 1923)
                _yearRangeComboBox.SelectedItem = "1901-1923";
            else if (Settings.LunarYear >= 1924 && Settings.LunarYear <= 1983)
                _yearRangeComboBox.SelectedItem = "1924-1983";
            else if (Settings.LunarYear >= 1984 && Settings.LunarYear <= 2043)
                _yearRangeComboBox.SelectedItem = "1984-2043";
            else if (Settings.LunarYear >= 2044 && Settings.LunarYear <= 2101)
                _yearRangeComboBox.SelectedItem = "2044-2101";
        }
        else
        {
            _yearRangeComboBox.SelectedIndex = 2;
            _tianganComboBox.SelectedIndex = 0;
            _dizhiComboBox.SelectedIndex = 0;
        }

        if (Settings != null && Settings.LunarMonth > 0 && Settings.LunarMonth <= 12)
        {
            _lunarMonthComboBox.SelectedIndex = Settings.LunarMonth - 1;
        }
        else
        {
            _lunarMonthComboBox.SelectedIndex = 0;
        }

        if (Settings != null)
        {
            _leapMonthCheckBox.IsChecked = Settings.IsLeapMonth;
        }
        else
        {
            _leapMonthCheckBox.IsChecked = false;
        }

        if (Settings != null && Settings.LunarDay > 0 && Settings.LunarDay <= 30)
        {
            _lunarDayComboBox.SelectedIndex = Settings.LunarDay - 1;
        }
        else
        {
            _lunarDayComboBox.SelectedIndex = 0;
        }

        var initialValue = Settings?.TargetTime ?? "";
        ParseTimeString(initialValue, out int hour, out int minute, out int second);
        _timePicker.SelectedTime = new TimeSpan(hour, minute, second);
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        var yearRange = _yearRangeComboBox.SelectedItem as string;
        var tiangan = _tianganComboBox.SelectedItem as string;
        var dizhi = _dizhiComboBox.SelectedItem as string;

        if (!string.IsNullOrEmpty(yearRange) && !string.IsNullOrEmpty(tiangan) && !string.IsNullOrEmpty(dizhi))
        {
            var lunarYear = LunarCalendarHelper.GetLunarYearFromRangeAndTianganDizhi(yearRange, tiangan, dizhi);
            Settings.LunarYear = lunarYear;
            if (LunarCalendarHelper.ParseYearRange(yearRange, out int startYear, out int endYear))
            {
                Settings.LunarYearRangeEnd = endYear;
            }
        }

        Settings.LunarMonth = _lunarMonthComboBox.SelectedIndex + 1;
        Settings.IsLeapMonth = _leapMonthCheckBox.IsChecked ?? false;
        Settings.LunarDay = _lunarDayComboBox.SelectedIndex + 1;

        var time = _timePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.TargetTime = $"{time.Hours:D2}-{time.Minutes:D2}-{time.Seconds:D2}";
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
