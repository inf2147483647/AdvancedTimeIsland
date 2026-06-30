using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 综合触发器设置页面
/// 实现21条触发器功能
/// </summary>
public class TriggersSettingsControl : UserControl
{
    private ComboBox? _triggerTypeComboBox;
    private Panel? _triggerSettingsPanel;
    private TextBlock? _triggerDescriptionTextBlock;

    // 规则设置控件
    private YearlyTimeRangeRuleSettingsControl? _yearlyRuleControl;
    private MonthlyTimeRangeRuleSettingsControl? _monthlyRuleControl;
    private DailyTimeRangeRuleSettingsControl? _dailyRuleControl;
    private HourlyTimeRangeRuleSettingsControl? _hourlyRuleControl;
    private MinutelyTimeRangeRuleSettingsControl? _minutelyRuleControl;
    private ExactTimeRuleSettingsControl? _exactTimeRuleControl;
    private WeeklyTimeRangeRuleSettingsControl? _weeklyRuleControl;

    // 农历触发器控件
    private LunarExactTimeRuleSettingsControl? _lunarExactRuleControl;
    private LunarYearlyRuleSettingsControl? _lunarYearlyRuleControl;

    // 地方时触发器控件
    private LocalSolarExactTimeRuleSettingsControl? _localSolarExactRuleControl;
    private LocalSolarYearlyTimeRangeRuleSettingsControl? _localSolarYearlyRuleControl;
    private LocalSolarMonthlyTimeRangeRuleSettingsControl? _localSolarMonthlyRuleControl;
    private LocalSolarDailyTimeRangeRuleSettingsControl? _localSolarDailyRuleControl;
    private LocalSolarHourlyTimeRangeRuleSettingsControl? _localSolarHourlyRuleControl;
    private LocalSolarMinutelyTimeRangeRuleSettingsControl? _localSolarMinutelyRuleControl;

    // 区时触发器控件
    private TimeZoneExactTimeRuleSettingsControl? _timeZoneExactRuleControl;
    private TimeZoneYearlyTimeRangeRuleSettingsControl? _timeZoneYearlyRuleControl;
    private TimeZoneMonthlyTimeRangeRuleSettingsControl? _timeZoneMonthlyRuleControl;
    private TimeZoneDailyTimeRangeRuleSettingsControl? _timeZoneDailyRuleControl;
    private TimeZoneHourlyTimeRangeRuleSettingsControl? _timeZoneHourlyRuleControl;

    public TriggersSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 16,
            Margin = new Thickness(16)
        };

        mainPanel.Children.Add(new TextBlock
        {
            Text = "触发器设置",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        });

        var typePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };

        typePanel.Children.Add(new TextBlock
        {
            Text = "触发器类型:",
            FontSize = 14,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        _triggerTypeComboBox = new ComboBox
        {
            Width = 500,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 1. 每年时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "每年时间范围", Tag = "yearly" });
        // 2. 每周时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "每周时间范围", Tag = "weekly" });
        // 3. 每月时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "每月时间范围", Tag = "monthly" });
        // 4. 每天时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "每天时间范围", Tag = "daily" });
        // 5. 每小时时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "每小时时间范围", Tag = "hourly" });
        // 6. 每分钟时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "每分钟时间范围", Tag = "minutely" });
        // 7. 精确时间是
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "精确时间是", Tag = "exact" });
        // 8. 精确时间在范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "精确时间在范围", Tag = "exact_range" });
        // 9. 地方时精确时间在范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "地方时精确时间在范围", Tag = "local_exact" });
        // 10. 地方时每年时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "地方时每年时间范围", Tag = "local_yearly" });
        // 11. 地方时每月时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "地方时每月时间范围", Tag = "local_monthly" });
        // 12. 地方时每天时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "地方时每天时间范围", Tag = "local_daily" });
        // 13. 地方时每小时时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "地方时每小时时间范围", Tag = "local_hourly" });
        // 14. 地方时每分钟时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "地方时每分钟时间范围", Tag = "local_minutely" });
        // 15. 区时精确时间在范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "区时精确时间在范围", Tag = "zone_exact" });
        // 16. 区时每年时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "区时每年时间范围", Tag = "zone_yearly" });
        // 17. 区时每月时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "区时每月时间范围", Tag = "zone_monthly" });
        // 18. 区时每天时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "区时每天时间范围", Tag = "zone_daily" });
        // 19. 区时每小时时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "区时每小时时间范围", Tag = "zone_hourly" });
        // 20. 农历每年
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "农历每年", Tag = "lunar_yearly" });
        // 21. 农历精确时间
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "农历精确时间", Tag = "lunar_exact" });
        // 22. 农历每月
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "农历每月", Tag = "lunar_monthly" });
        // 23. 农历年倒数第n天
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "农历年倒数第n天", Tag = "lunar_lastday" });
        // 24. 地方时每周时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "地方时每周时间范围", Tag = "local_weekly" });
        // 25. 区时每周时间范围
        _triggerTypeComboBox.Items.Add(new ComboBoxItem { Content = "区时每周时间范围", Tag = "zone_weekly" });

        _triggerTypeComboBox.SelectedIndex = 0;
        _triggerTypeComboBox.SelectionChanged += OnTriggerTypeChanged;

        typePanel.Children.Add(_triggerTypeComboBox);
        mainPanel.Children.Add(typePanel);

        _triggerDescriptionTextBlock = new TextBlock
        {
            Text = "",
            FontSize = 12,
            Foreground = Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        mainPanel.Children.Add(_triggerDescriptionTextBlock);

        _triggerSettingsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12
        };
        mainPanel.Children.Add(_triggerSettingsPanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        UpdateTriggerSettings("yearly");
    }

    private void OnTriggerTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_triggerTypeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            UpdateTriggerSettings(tag);
        }
    }

    private void UpdateTriggerSettings(string triggerType)
    {
        if (_triggerSettingsPanel == null || _triggerDescriptionTextBlock == null)
            return;

        _triggerSettingsPanel.Children.Clear();

        var description = triggerType switch
        {
            "yearly" => "在每年的指定日期和时间触发。",
            "weekly" => "在每周的指定星期和时间触发。",
            "monthly" => "在每月的指定日期和时间触发。",
            "daily" => "在每天的指定时间触发。",
            "hourly" => "在每小时的指定分钟和秒触发。",
            "minutely" => "在每分钟的指定秒触发。",
            "exact" => "在指定的精确时间触发。",
            "exact_range" => "在指定的精确时间范围内触发。",
            "local_exact" => "在指定经度地方时的精确日期和时间范围内触发。",
            "local_yearly" => "在指定经度地方时每年的指定日期和时间范围内触发。",
            "local_monthly" => "在指定经度地方时每月的指定日期和时间范围内触发。",
            "local_daily" => "在指定经度地方时每天的指定时间范围内触发。",
            "local_hourly" => "在指定经度地方时每小时的指定分钟和秒范围内触发。",
            "local_minutely" => "在指定经度地方时每分钟的指定秒范围内触发。",
            "zone_exact" => "在指定时区的精确日期和时间范围内触发。",
            "zone_yearly" => "在指定时区每年的指定日期和时间范围内触发。",
            "zone_monthly" => "在指定时区每月的指定日期和时间范围内触发。",
            "zone_daily" => "在指定时区每天的指定时间范围内触发。",
            "zone_hourly" => "在指定时区每小时的指定分钟和秒范围内触发。",
            "lunar_yearly" => "在每年农历的指定月日和时间触发。",
            "lunar_exact" => "在精确的农历日期和时间触发。",
            "lunar_monthly" => "在农历每月的指定日期和时间触发。",
            "lunar_lastday" => "在农历每年指定月份的倒数第n天触发。",
            "local_weekly" => "在指定经度地方时每周的指定星期和时间范围内触发。",
            "zone_weekly" => "在指定时区每周的指定星期和时间范围内触发。",
            _ => ""
        };

        _triggerDescriptionTextBlock.Text = description;

        switch (triggerType)
        {
            case "yearly":
                _yearlyRuleControl ??= new YearlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_yearlyRuleControl);
                break;
            case "weekly":
                _weeklyRuleControl ??= new WeeklyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_weeklyRuleControl);
                break;
            case "monthly":
                _monthlyRuleControl ??= new MonthlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_monthlyRuleControl);
                break;
            case "daily":
                _dailyRuleControl ??= new DailyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_dailyRuleControl);
                break;
            case "hourly":
                _hourlyRuleControl ??= new HourlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_hourlyRuleControl);
                break;
            case "minutely":
                _minutelyRuleControl ??= new MinutelyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_minutelyRuleControl);
                break;
            case "exact":
                _exactTimeRuleControl ??= new ExactTimeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_exactTimeRuleControl);
                break;
            case "exact_range":
                var exactRangeControl = new ExactTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(exactRangeControl);
                break;
            case "lunar_exact":
                _lunarExactRuleControl ??= new LunarExactTimeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_lunarExactRuleControl);
                break;
            case "lunar_yearly":
                _lunarYearlyRuleControl ??= new LunarYearlyRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_lunarYearlyRuleControl);
                break;
            case "lunar_monthly":
                var lunarMonthlyControl = new LunarMonthlyRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(lunarMonthlyControl);
                break;
            case "lunar_lastday":
                var lunarLastDayControl = new LunarLastDayRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(lunarLastDayControl);
                break;
            case "local_exact":
                _localSolarExactRuleControl ??= new LocalSolarExactTimeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_localSolarExactRuleControl);
                break;
            case "local_yearly":
                _localSolarYearlyRuleControl ??= new LocalSolarYearlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_localSolarYearlyRuleControl);
                break;
            case "local_monthly":
                _localSolarMonthlyRuleControl ??= new LocalSolarMonthlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_localSolarMonthlyRuleControl);
                break;
            case "local_daily":
                _localSolarDailyRuleControl ??= new LocalSolarDailyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_localSolarDailyRuleControl);
                break;
            case "local_hourly":
                _localSolarHourlyRuleControl ??= new LocalSolarHourlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_localSolarHourlyRuleControl);
                break;
            case "local_minutely":
                _localSolarMinutelyRuleControl ??= new LocalSolarMinutelyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_localSolarMinutelyRuleControl);
                break;
            case "local_weekly":
                var localWeeklyControl = new LocalSolarWeeklyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(localWeeklyControl);
                break;
            case "zone_exact":
                _timeZoneExactRuleControl ??= new TimeZoneExactTimeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_timeZoneExactRuleControl);
                break;
            case "zone_yearly":
                _timeZoneYearlyRuleControl ??= new TimeZoneYearlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_timeZoneYearlyRuleControl);
                break;
            case "zone_monthly":
                _timeZoneMonthlyRuleControl ??= new TimeZoneMonthlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_timeZoneMonthlyRuleControl);
                break;
            case "zone_daily":
                _timeZoneDailyRuleControl ??= new TimeZoneDailyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_timeZoneDailyRuleControl);
                break;
            case "zone_hourly":
                _timeZoneHourlyRuleControl ??= new TimeZoneHourlyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(_timeZoneHourlyRuleControl);
                break;
            case "zone_weekly":
                var zoneWeeklyControl = new TimeZoneWeeklyTimeRangeRuleSettingsControl();
                _triggerSettingsPanel.Children.Add(zoneWeeklyControl);
                break;
            default:
                _triggerSettingsPanel.Children.Add(CreatePlaceholder("该触发器控件待实现"));
                break;
        }
    }

    private Border CreatePlaceholder(string text)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 14,
                Foreground = Brushes.Orange,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
    }
}
