using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 农历日期范围规则设置控件 - 使用天干地支选择
/// 格式：L_YYYY-L_MM-L_DD-hh-mm-ss~L_YYYY-L_MM-L_DD-hh-mm-ss（考虑闰月）
/// </summary>
public class LunarExactTimeRangeRuleSettingsControl : RuleSettingsControlBase<LunarExactTimeRangeRuleSettings>
{
    private ComboBox _startTianganComboBox = null!;
    private ComboBox _startDizhiComboBox = null!;
    private ComboBox _startYearRangeComboBox = null!;
    private ComboBox _startLunarMonthComboBox = null!;
    private CheckBox _startLeapMonthCheckBox = null!;
    private ComboBox _startLunarDayComboBox = null!;
    private TimePicker _startTimePicker = null!;

    private ComboBox _endTianganComboBox = null!;
    private ComboBox _endDizhiComboBox = null!;
    private ComboBox _endYearRangeComboBox = null!;
    private ComboBox _endLunarMonthComboBox = null!;
    private CheckBox _endLeapMonthCheckBox = null!;
    private ComboBox _endLunarDayComboBox = null!;
    private TimePicker _endTimePicker = null!;

    public LunarExactTimeRangeRuleSettingsControl()
    {
        InitializeComponent();
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

        // 开始时间部分
        var startSection = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        startSection.Children.Add(new TextBlock
        {
            Text = "开始时间",
            Foreground = Brushes.LightBlue,
            FontSize = 14,
            FontWeight = FontWeight.Bold
        });

        // 开始年份（年份范围+天干地支选择）
        var startYearPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 年份范围下拉框
        var startYearRangePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        startYearRangePanel.Children.Add(new TextBlock
        {
            Text = "年份范围:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 60
        });

        _startYearRangeComboBox = new ComboBox
        {
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            _startYearRangeComboBox.Items.Add(range);
        }

        startYearRangePanel.Children.Add(_startYearRangeComboBox);
        startYearPanel.Children.Add(startYearRangePanel);

        // 天干地支
        var startTgDzPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        startTgDzPanel.Children.Add(new TextBlock
        {
            Text = "天干:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
        });

        _startTianganComboBox = new ComboBox
        {
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var tg in LunarCalendarHelper.GetAllTiangan())
        {
            _startTianganComboBox.Items.Add(tg);
        }

        startTgDzPanel.Children.Add(_startTianganComboBox);

        startTgDzPanel.Children.Add(new TextBlock
        {
            Text = "地支:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
        });

        _startDizhiComboBox = new ComboBox
        {
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var dz in LunarCalendarHelper.GetAllDizhi())
        {
            _startDizhiComboBox.Items.Add(dz);
        }

        startTgDzPanel.Children.Add(_startDizhiComboBox);
        startYearPanel.Children.Add(startTgDzPanel);

        // 初始化开始时间选中状态
        if (Settings?.StartLunarYear > 0)
        {
            var tg = LunarCalendarHelper.GetTiangan(Settings.StartLunarYear);
            var dz = LunarCalendarHelper.GetDizhi(Settings.StartLunarYear);
            _startTianganComboBox.SelectedItem = tg;
            _startDizhiComboBox.SelectedItem = dz;
            
            foreach (var range in LunarCalendarHelper.GetAllYearRanges())
            {
                if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
                {
                    if (Settings.StartLunarYear >= startYear && Settings.StartLunarYear <= endYear)
                    {
                        _startYearRangeComboBox.SelectedItem = range;
                        break;
                    }
                }
            }
        }
        else
        {
            _startYearRangeComboBox.SelectedIndex = 2;
            _startTianganComboBox.SelectedIndex = 0;
            _startDizhiComboBox.SelectedIndex = 0;
        }

        _startTianganComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        _startDizhiComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        _startYearRangeComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        startSection.Children.Add(startYearPanel);

        // 开始月份和闰月
        var startMonthPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        startMonthPanel.Children.Add(new TextBlock
        {
            Text = "农历月:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _startLunarMonthComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 12; i++)
        {
            _startLunarMonthComboBox.Items.Add(i.ToString());
        }

        if (Settings?.StartLunarMonth > 0 && Settings.StartLunarMonth <= 12)
        {
            _startLunarMonthComboBox.SelectedIndex = Settings.StartLunarMonth - 1;
        }
        else
        {
            _startLunarMonthComboBox.SelectedIndex = 0;
        }

        _startLunarMonthComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        startMonthPanel.Children.Add(_startLunarMonthComboBox);

        _startLeapMonthCheckBox = new CheckBox
        {
            Content = "闰月",
            IsChecked = Settings?.StartIsLeapMonth ?? false,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _startLeapMonthCheckBox.Checked += (s, e) => UpdateSettingsValue();
        _startLeapMonthCheckBox.Unchecked += (s, e) => UpdateSettingsValue();

        startMonthPanel.Children.Add(_startLeapMonthCheckBox);
        startSection.Children.Add(startMonthPanel);

        // 开始日期
        var startDayPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        startDayPanel.Children.Add(new TextBlock
        {
            Text = "农历日:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _startLunarDayComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 30; i++)
        {
            _startLunarDayComboBox.Items.Add(i.ToString());
        }

        if (Settings?.StartLunarDay > 0 && Settings.StartLunarDay <= 30)
        {
            _startLunarDayComboBox.SelectedIndex = Settings.StartLunarDay - 1;
        }
        else
        {
            _startLunarDayComboBox.SelectedIndex = 0;
        }

        _startLunarDayComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        startDayPanel.Children.Add(_startLunarDayComboBox);
        startSection.Children.Add(startDayPanel);

        // 开始时间选择器
        var startTimePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        startTimePanel.Children.Add(new TextBlock
        {
            Text = "时间:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _startTimePicker = new TimePicker
        {
            Width = 300,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var startInitialValue = Settings?.StartTargetTime ?? "";
        ParseTimeString(startInitialValue, out int startHour, out int startMinute, out int startSecond);

        _startTimePicker.SelectedTime = new TimeSpan(startHour, startMinute, startSecond);

        _startTimePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        startTimePanel.Children.Add(_startTimePicker);
        startSection.Children.Add(startTimePanel);

        mainPanel.Children.Add(startSection);

        // 结束时间部分
        var endSection = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        endSection.Children.Add(new TextBlock
        {
            Text = "结束时间",
            Foreground = Brushes.LightPink,
            FontSize = 14,
            FontWeight = FontWeight.Bold
        });

        // 结束年份（年份范围+天干地支选择）
        var endYearPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 年份范围下拉框
        var endYearRangePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        endYearRangePanel.Children.Add(new TextBlock
        {
            Text = "年份范围:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 60
        });

        _endYearRangeComboBox = new ComboBox
        {
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            _endYearRangeComboBox.Items.Add(range);
        }

        endYearRangePanel.Children.Add(_endYearRangeComboBox);
        endYearPanel.Children.Add(endYearRangePanel);

        // 天干地支
        var endTgDzPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        endTgDzPanel.Children.Add(new TextBlock
        {
            Text = "天干:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
        });

        _endTianganComboBox = new ComboBox
        {
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var tg in LunarCalendarHelper.GetAllTiangan())
        {
            _endTianganComboBox.Items.Add(tg);
        }

        endTgDzPanel.Children.Add(_endTianganComboBox);

        endTgDzPanel.Children.Add(new TextBlock
        {
            Text = "地支:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
        });

        _endDizhiComboBox = new ComboBox
        {
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (var dz in LunarCalendarHelper.GetAllDizhi())
        {
            _endDizhiComboBox.Items.Add(dz);
        }

        endTgDzPanel.Children.Add(_endDizhiComboBox);
        endYearPanel.Children.Add(endTgDzPanel);

        // 初始化结束时间选中状态
        if (Settings?.EndLunarYear > 0)
        {
            var tg = LunarCalendarHelper.GetTiangan(Settings.EndLunarYear);
            var dz = LunarCalendarHelper.GetDizhi(Settings.EndLunarYear);
            _endTianganComboBox.SelectedItem = tg;
            _endDizhiComboBox.SelectedItem = dz;
            
            foreach (var range in LunarCalendarHelper.GetAllYearRanges())
            {
                if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
                {
                    if (Settings.EndLunarYear >= startYear && Settings.EndLunarYear <= endYear)
                    {
                        _endYearRangeComboBox.SelectedItem = range;
                        break;
                    }
                }
            }
        }
        else
        {
            _endYearRangeComboBox.SelectedIndex = 2;
            _endTianganComboBox.SelectedIndex = 0;
            _endDizhiComboBox.SelectedIndex = 0;
        }

        _endTianganComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        _endDizhiComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        _endYearRangeComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        endSection.Children.Add(endYearPanel);

        // 结束月份和闰月
        var endMonthPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        endMonthPanel.Children.Add(new TextBlock
        {
            Text = "农历月:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _endLunarMonthComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 12; i++)
        {
            _endLunarMonthComboBox.Items.Add(i.ToString());
        }

        if (Settings?.EndLunarMonth > 0 && Settings.EndLunarMonth <= 12)
        {
            _endLunarMonthComboBox.SelectedIndex = Settings.EndLunarMonth - 1;
        }
        else
        {
            _endLunarMonthComboBox.SelectedIndex = 0;
        }

        _endLunarMonthComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        endMonthPanel.Children.Add(_endLunarMonthComboBox);

        _endLeapMonthCheckBox = new CheckBox
        {
            Content = "闰月",
            IsChecked = Settings?.EndIsLeapMonth ?? false,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _endLeapMonthCheckBox.Checked += (s, e) => UpdateSettingsValue();
        _endLeapMonthCheckBox.Unchecked += (s, e) => UpdateSettingsValue();

        endMonthPanel.Children.Add(_endLeapMonthCheckBox);
        endSection.Children.Add(endMonthPanel);

        // 结束日期
        var endDayPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        endDayPanel.Children.Add(new TextBlock
        {
            Text = "农历日:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _endLunarDayComboBox = new ComboBox
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 1; i <= 30; i++)
        {
            _endLunarDayComboBox.Items.Add(i.ToString());
        }

        if (Settings?.EndLunarDay > 0 && Settings.EndLunarDay <= 30)
        {
            _endLunarDayComboBox.SelectedIndex = Settings.EndLunarDay - 1;
        }
        else
        {
            _endLunarDayComboBox.SelectedIndex = 0;
        }

        _endLunarDayComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();

        endDayPanel.Children.Add(_endLunarDayComboBox);
        endSection.Children.Add(endDayPanel);

        // 结束时间选择器
        var endTimePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        endTimePanel.Children.Add(new TextBlock
        {
            Text = "时间:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _endTimePicker = new TimePicker
        {
            Width = 300,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var endInitialValue = Settings?.EndTargetTime ?? "";
        ParseTimeString(endInitialValue, out int endHour, out int endMinute, out int endSecond);

        _endTimePicker.SelectedTime = new TimeSpan(endHour, endMinute, endSecond);

        _endTimePicker.SelectedTimeChanged += (s, e) => UpdateSettingsValue();

        endTimePanel.Children.Add(_endTimePicker);
        endSection.Children.Add(endTimePanel);

        mainPanel.Children.Add(endSection);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;

        // 更新开始时间
        var startYearRange = _startYearRangeComboBox.SelectedItem as string;
        var startTiangan = _startTianganComboBox.SelectedItem as string;
        var startDizhi = _startDizhiComboBox.SelectedItem as string;

        if (!string.IsNullOrEmpty(startYearRange) && !string.IsNullOrEmpty(startTiangan) && !string.IsNullOrEmpty(startDizhi))
        {
            var lunarYear = LunarCalendarHelper.GetLunarYearFromRangeAndTianganDizhi(startYearRange, startTiangan, startDizhi);
            Settings.StartLunarYear = lunarYear;
            if (LunarCalendarHelper.ParseYearRange(startYearRange, out int startYear, out int endYear))
            {
                Settings.StartLunarYearRangeEnd = endYear;
            }
        }

        Settings.StartLunarMonth = _startLunarMonthComboBox.SelectedIndex + 1;
        Settings.StartIsLeapMonth = _startLeapMonthCheckBox.IsChecked ?? false;
        Settings.StartLunarDay = _startLunarDayComboBox.SelectedIndex + 1;

        var startTime = _startTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.StartTargetTime = $"{startTime.Hours:D2}-{startTime.Minutes:D2}-{startTime.Seconds:D2}";

        // 更新结束时间
        var endYearRange = _endYearRangeComboBox.SelectedItem as string;
        var endTiangan = _endTianganComboBox.SelectedItem as string;
        var endDizhi = _endDizhiComboBox.SelectedItem as string;

        if (!string.IsNullOrEmpty(endYearRange) && !string.IsNullOrEmpty(endTiangan) && !string.IsNullOrEmpty(endDizhi))
        {
            var lunarYear = LunarCalendarHelper.GetLunarYearFromRangeAndTianganDizhi(endYearRange, endTiangan, endDizhi);
            Settings.EndLunarYear = lunarYear;
            if (LunarCalendarHelper.ParseYearRange(endYearRange, out int startYear, out int endYear))
            {
                Settings.EndLunarYearRangeEnd = endYear;
            }
        }

        Settings.EndLunarMonth = _endLunarMonthComboBox.SelectedIndex + 1;
        Settings.EndIsLeapMonth = _endLeapMonthCheckBox.IsChecked ?? false;
        Settings.EndLunarDay = _endLunarDayComboBox.SelectedIndex + 1;

        var endTime = _endTimePicker.SelectedTime ?? TimeSpan.Zero;
        Settings.EndTargetTime = $"{endTime.Hours:D2}-{endTime.Minutes:D2}-{endTime.Seconds:D2}";
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
