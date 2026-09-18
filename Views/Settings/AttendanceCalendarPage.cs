 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Views.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 「在校时间统计」设置页：集中管理学期区间、统计口径、节假日/调休、寒暑假与自定义日期，
/// 并展示统计概览与按周/月的分段统计。数据由 <see cref="AttendanceCalendarService"/> 统一持久化。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandAttendance", "在校时间统计", "\ue126", "\ue126")]
#if NET10_0_OR_GREATER
[Group("advancedtimeisland.main")]
#endif
public class AttendanceCalendarPage : SettingsPageBase
{
    private readonly AttendanceCalendarService _calendar;

    private AttendanceStatisticsConfig Config => _calendar.Data.Config;

    private TextBlock? _summaryTextBlock;
    private TextBlock? _summaryDetailTextBlock;
    private ProgressBar? _summaryProgressBar;
    private TextBlock? _semesterRangeTextBlock;
    private TextBlock? _statusTextBlock;

    private ComboBox? _startSourceComboBox;
    private DatePicker? _manualStartDatePicker;
    private ComboBox? _endSourceComboBox;
    private NumericUpDown? _totalWeeksNumericUpDown;
    private DatePicker? _manualEndDatePicker;
    private NumericUpDown? _dailyHoursNumericUpDown;
    private ComboBox? _dailyHoursSourceComboBox;
    private TextBlock? _dailyHoursHintTextBlock;

    private StackPanel? _vacationListHost;
    private StackPanel? _customListHost;
    private StackPanel? _holidayListHost;
    private StackPanel? _periodTableHost;

    private TextBox? _newVacationNameTextBox;
    private DatePicker? _newVacationStartDatePicker;
    private DatePicker? _newVacationEndDatePicker;
    private DatePicker? _newCustomDatePicker;
    private TextBox? _newCustomNameTextBox;
    private ComboBox? _newCustomTypeComboBox;
    private DatePicker? _newHolidayDatePicker;
    private TextBox? _newHolidayNameTextBox;
    private ComboBox? _newHolidayTypeComboBox;
    private ComboBox? _periodModeComboBox;

    private CheckBox? _excludeSaturdayCheckBox;
    private CheckBox? _excludeSundayCheckBox;
    private CheckBox? _excludeHolidaysCheckBox;
    private CheckBox? _countMakeupDaysCheckBox;
    private CheckBox? _excludeVacationsCheckBox;
    private CheckBox? _respectCustomDatesCheckBox;

    /// <summary>程序化回填控件时置位，避免控件事件把配置改回控件中的旧值。</summary>
    private bool _isUpdatingControls;

    private static readonly object[] StartSourceItems = { "自动读取 ClassIsland 学期开始时间", "手动指定" };
    private static readonly object[] EndSourceItems = { "按学期周数推算", "手动指定" };
    private static readonly object[] DailyHoursSourceItems = { "自动获取", "手动指定" };
    private static readonly object[] HolidayTypeItems = { "放假日", "调休补班日" };
    private static readonly object[] CustomTypeItems = { "强制计入在校日", "强制排除" };
    private static readonly object[] PeriodModeItems = { "按周", "按月" };

    public AttendanceCalendarPage()
    {
        _calendar = IAppHost.TryGetService<AttendanceCalendarService>()
                    ?? AttendanceCalendarService.Instance
                    ?? new AttendanceCalendarService();

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Content = new TextBlock
            {
                Text = $"设置页面初始化失败: {ex.Message}",
                Foreground = Brushes.Red,
                Margin = new Thickness(16)
            };
        }
    }

    // ==================== 页面构建 ====================

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        var title = new TextBlock
        {
            Text = "在校时间统计（ATI）",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        title.Tag = "theme-text";
        mainPanel.Children.Add(title);

        BuildSummaryGroup(mainPanel);
        BuildSemesterGroup(mainPanel);
        BuildRuleGroup(mainPanel);
        BuildVacationGroup(mainPanel);
        BuildCustomDateGroup(mainPanel);
        BuildHolidayGroup(mainPanel);
        BuildPeriodGroup(mainPanel);

        Content = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = false
        };
    }

    private void BuildSummaryGroup(StackPanel mainPanel)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _summaryTextBlock = new TextBlock
        {
            Text = "正在统计…",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(_summaryTextBlock);

        _summaryProgressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 6,
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        panel.Children.Add(_summaryProgressBar);

        _summaryDetailTextBlock = new TextBlock
        {
            Text = string.Empty,
            TextWrapping = TextWrapping.Wrap,
            Tag = "sub"
        };
        panel.Children.Add(_summaryDetailTextBlock);

        _semesterRangeTextBlock = new TextBlock
        {
            Text = string.Empty,
            TextWrapping = TextWrapping.Wrap,
            Tag = "sub"
        };
        panel.Children.Add(_semesterRangeTextBlock);

        var refreshButton = new Button { Content = "刷新统计", Margin = new Thickness(0, 4, 0, 0) };
        refreshButton.Click += (_, _) => RefreshAll();
        panel.Children.Add(refreshButton);

        mainPanel.Children.Add(SettingsGroupFactory.Create("统计概览", panel));
    }

    private void BuildSemesterGroup(StackPanel mainPanel)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var startRow = FontSettingsRowFactory.CreateComboBoxRow("学期开始日", StartSourceItems,
            out _, out var startSourceComboBox, out _, selectionChangedHandler: OnSemesterOptionChanged);
        _startSourceComboBox = startSourceComboBox;
        panel.Children.Add(startRow);

        _manualStartDatePicker = new DatePicker { Width = 300 };
        _manualStartDatePicker.SelectedDateChanged += OnSemesterDateChanged;
        panel.Children.Add(CreateLabeledRow("手动开始日", _manualStartDatePicker));

        var endRow = FontSettingsRowFactory.CreateComboBoxRow("学期结束日", EndSourceItems,
            out _, out var endSourceComboBox, out _, selectionChangedHandler: OnSemesterOptionChanged);
        _endSourceComboBox = endSourceComboBox;
        panel.Children.Add(endRow);

        _totalWeeksNumericUpDown = new NumericUpDown
        {
            Width = 200,
            Minimum = 1,
            Maximum = 60,
            Increment = 1,
            FormatString = "0",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _totalWeeksNumericUpDown.ValueChanged += OnSemesterNumberChanged;
        panel.Children.Add(CreateLabeledRow("学期总周数", _totalWeeksNumericUpDown));

        _manualEndDatePicker = new DatePicker { Width = 300 };
        _manualEndDatePicker.SelectedDateChanged += OnSemesterDateChanged;
        panel.Children.Add(CreateLabeledRow("手动结束日", _manualEndDatePicker));

        _dailyHoursSourceComboBox = new ComboBox { Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var item in DailyHoursSourceItems)
        {
            _dailyHoursSourceComboBox.Items.Add(item);
        }
        _dailyHoursSourceComboBox.SelectionChanged += OnDailyHoursSourceChanged;
        panel.Children.Add(CreateLabeledRow("每日在校时长", _dailyHoursSourceComboBox));
        _dailyHoursHintTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeHelper.GetSubTextBrush(),
            Margin = new Thickness(0, -2, 0, 2)
        };
        panel.Children.Add(_dailyHoursHintTextBlock);

        _dailyHoursNumericUpDown = new NumericUpDown
        {
            Width = 200,
            Minimum = 0,
            Maximum = 24,
            Increment = 0.5m,
            FormatString = "0.#",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _dailyHoursNumericUpDown.ValueChanged += OnSemesterNumberChanged;
        panel.Children.Add(CreateLabeledRow("每日在校时长（小时）", _dailyHoursNumericUpDown));

        mainPanel.Children.Add(SettingsGroupFactory.Create("学期区间", panel));
    }

    private void BuildRuleGroup(StackPanel mainPanel)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };

        _excludeSaturdayCheckBox = AddCheckBox(panel, "排除周六", OnRuleChanged);
        _excludeSundayCheckBox = AddCheckBox(panel, "排除周日", OnRuleChanged);
        _excludeHolidaysCheckBox = AddCheckBox(panel, "排除法定节假日（放假日）", OnRuleChanged);
        _countMakeupDaysCheckBox = AddCheckBox(panel, "调休补班日计为在校日（优先于周末排除）", OnRuleChanged);
        _excludeVacationsCheckBox = AddCheckBox(panel, "排除寒暑假区间", OnRuleChanged);
        _respectCustomDatesCheckBox = AddCheckBox(panel, "应用自定义日期（强制计入 / 强制排除）", OnRuleChanged);

        mainPanel.Children.Add(SettingsGroupFactory.Create("排除规则", panel));
    }

    private void BuildVacationGroup(StackPanel mainPanel)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var addPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        _newVacationNameTextBox = new TextBox { Width = 150, Watermark = "如 2026 寒假" };
        _newVacationStartDatePicker = new DatePicker { Width = 300 };
        _newVacationEndDatePicker = new DatePicker { Width = 300 };
        var addButton = new Button { Content = "添加假期" };
        addButton.Click += OnAddVacationClick;
        addPanel.Children.Add(new TextBlock { Text = "名称", VerticalAlignment = VerticalAlignment.Center });
        addPanel.Children.Add(_newVacationNameTextBox);
        addPanel.Children.Add(new TextBlock { Text = "开始", VerticalAlignment = VerticalAlignment.Center });
        addPanel.Children.Add(_newVacationStartDatePicker);
        addPanel.Children.Add(new TextBlock { Text = "结束", VerticalAlignment = VerticalAlignment.Center });
        addPanel.Children.Add(_newVacationEndDatePicker);
        addPanel.Children.Add(addButton);
        panel.Children.Add(addPanel);

        _vacationListHost = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        panel.Children.Add(new ScrollViewer
        {
            Content = _vacationListHost,
            MaxHeight = 300,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });

        mainPanel.Children.Add(SettingsGroupFactory.Create("寒暑假区间", panel));
    }

    private void BuildCustomDateGroup(StackPanel mainPanel)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var addPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        _newCustomDatePicker = new DatePicker { Width = 300 };
        _newCustomNameTextBox = new TextBox { Width = 150, Watermark = "如 校运会" };
        _newCustomTypeComboBox = new ComboBox { Width = 150, ItemsSource = CustomTypeItems, SelectedIndex = 1 };
        var addButton = new Button { Content = "添加日期" };
        addButton.Click += OnAddCustomDateClick;
        addPanel.Children.Add(new TextBlock { Text = "日期", VerticalAlignment = VerticalAlignment.Center });
        addPanel.Children.Add(_newCustomDatePicker);
        addPanel.Children.Add(new TextBlock { Text = "名称", VerticalAlignment = VerticalAlignment.Center });
        addPanel.Children.Add(_newCustomNameTextBox);
        addPanel.Children.Add(_newCustomTypeComboBox);
        addPanel.Children.Add(addButton);
        panel.Children.Add(addPanel);

        _customListHost = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        panel.Children.Add(new ScrollViewer
        {
            Content = _customListHost,
            MaxHeight = 300,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });

        mainPanel.Children.Add(SettingsGroupFactory.Create("自定义日期", panel));
    }

    private void BuildHolidayGroup(StackPanel mainPanel)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var addPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        _newHolidayDatePicker = new DatePicker { Width = 300 };
        _newHolidayNameTextBox = new TextBox { Width = 150, Watermark = "如 春节" };
        _newHolidayTypeComboBox = new ComboBox { Width = 130, ItemsSource = HolidayTypeItems, SelectedIndex = 0 };
        var addButton = new Button { Content = "添加条目" };
        addButton.Click += OnAddHolidayClick;
        addPanel.Children.Add(new TextBlock { Text = "日期", VerticalAlignment = VerticalAlignment.Center });
        addPanel.Children.Add(_newHolidayDatePicker);
        addPanel.Children.Add(new TextBlock { Text = "名称", VerticalAlignment = VerticalAlignment.Center });
        addPanel.Children.Add(_newHolidayNameTextBox);
        addPanel.Children.Add(_newHolidayTypeComboBox);
        addPanel.Children.Add(addButton);
        panel.Children.Add(addPanel);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        var updateButton = new Button { Content = "从网络更新" };
        updateButton.Click += OnUpdateFromNetworkClick;
        var restoreButton = new Button { Content = "恢复内置数据" };
        restoreButton.Click += OnRestoreBuiltinClick;
        buttonPanel.Children.Add(updateButton);
        buttonPanel.Children.Add(restoreButton);

        _statusTextBlock = new TextBlock
        {
            Text = string.Empty,
            TextWrapping = TextWrapping.Wrap,
            Tag = "sub",
            VerticalAlignment = VerticalAlignment.Center
        };
        buttonPanel.Children.Add(_statusTextBlock);
        panel.Children.Add(buttonPanel);

        _holidayListHost = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        panel.Children.Add(new ScrollViewer
        {
            Content = _holidayListHost,
            MaxHeight = 360,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });

        mainPanel.Children.Add(SettingsGroupFactory.Create("法定节假日与调休", panel));
    }

    private void BuildPeriodGroup(StackPanel mainPanel)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var modeRow = FontSettingsRowFactory.CreateComboBoxRow("分段方式", PeriodModeItems,
            out _, out var periodModeComboBox, out _, selectionChangedHandler: (_, _) => RebuildPeriodTable());
        _periodModeComboBox = periodModeComboBox;
        _periodModeComboBox.SelectedIndex = 0;
        panel.Children.Add(modeRow);

        _periodTableHost = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        panel.Children.Add(new ScrollViewer
        {
            Content = _periodTableHost,
            MaxHeight = 400,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });

        mainPanel.Children.Add(SettingsGroupFactory.Create("分段统计", panel));
    }

    // ==================== 行与表格辅助 ====================

    private static Grid CreateLabeledRow(string labelText, Control control)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock
        {
            Text = labelText,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);

        return grid;
    }

    private static CheckBox AddCheckBox(StackPanel panel, string text, EventHandler<RoutedEventArgs> handler)
    {
        var checkBox = new CheckBox { Content = text, VerticalAlignment = VerticalAlignment.Center };
        checkBox.IsCheckedChanged += handler;
        panel.Children.Add(checkBox);
        return checkBox;
    }

    /// <summary>
    /// 构建表格：外层边框负责上/左边线，单元格负责右/下边线，拼合为完整网格。
    /// </summary>
    private static Border CreateTable(string[] headers, double[] widths, IEnumerable<Control[]> rows)
    {
        var grid = new Grid();
        for (var i = 0; i < headers.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = widths[i] > 0
                    ? new GridLength(widths[i])
                    : new GridLength(1, GridUnitType.Star)
            });
        }

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var c = 0; c < headers.Length; c++)
        {
            AddCell(grid, 0, c, new TextBlock
            {
                Text = headers[c],
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        var rowIndex = 1;
        foreach (var cells in rows)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (var c = 0; c < headers.Length && c < cells.Length; c++)
            {
                AddCell(grid, rowIndex, c, cells[c]);
            }
            rowIndex++;
        }

        return new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            Tag = "table-border",
            Child = grid
        };
    }

    private static void AddCell(Grid grid, int row, int column, Control child)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            Padding = new Thickness(6, 3, 6, 3),
            Child = child,
            Tag = "table-cell"
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    private static TextBlock CreateCellText(string text)
    {
        return new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
    }

    private static TextBlock CreateSourceText(HolidayEntrySource source)
    {
        return new TextBlock
        {
            Text = source switch
            {
                HolidayEntrySource.Builtin => "内置",
                HolidayEntrySource.Network => "联网",
                _ => "手动"
            },
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private Button CreateDeleteButton(Action onDelete)
    {
        var button = new Button { Content = "删除", Padding = new Thickness(8, 2, 8, 2) };
        button.Click += (_, _) => onDelete();
        return button;
    }

    // ==================== 数据刷新 ====================

    private void RefreshAll()
    {
        UpdateSemesterControls();
        UpdateRuleControls();
        RebuildVacationTable();
        RebuildCustomDateTable();
        RebuildHolidayTable();
        RebuildPeriodTable();
        _ = RefreshSummaryAsync();

        if (_statusTextBlock != null && string.IsNullOrEmpty(_statusTextBlock.Text))
        {
            var lastUpdated = _calendar.Data.LastUpdated;
            var count = _calendar.Data.Holidays.Count + _calendar.Data.MakeupDays.Count;
            _statusTextBlock.Text = lastUpdated == null
                ? $"当前共 {count} 条节假日/调休记录。"
                : $"当前共 {count} 条节假日/调休记录；最近联网更新：{lastUpdated:yyyy-MM-dd HH:mm}（{_calendar.Data.LastUpdateSource}）";
        }
    }

    private void UpdateSemesterControls()
    {
        _isUpdatingControls = true;
        try
        {
            _startSourceComboBox!.SelectedIndex = Config.StartSource == SemesterStartSource.Manual ? 1 : 0;
            _endSourceComboBox!.SelectedIndex = Config.EndSource == SemesterEndSource.ManualDate ? 1 : 0;
            _manualStartDatePicker!.SelectedDate = ToDateTimeOffset(Config.ManualStartDate);
            _manualEndDatePicker!.SelectedDate = ToDateTimeOffset(Config.ManualEndDate);
            _totalWeeksNumericUpDown!.Value = Config.TotalWeeks;
            _dailyHoursNumericUpDown!.Value = (decimal)Config.DailyHours;
            _dailyHoursSourceComboBox!.SelectedIndex = Config.DailyHoursSource == DailyHoursSource.Auto ? 0 : 1;
        }
        finally
        {
            _isUpdatingControls = false;
        }
        UpdateSemesterOptionEnabledState();
    }

    private void UpdateRuleControls()
    {
        _isUpdatingControls = true;
        try
        {
            _excludeSaturdayCheckBox!.IsChecked = Config.ExcludeSaturday;
            _excludeSundayCheckBox!.IsChecked = Config.ExcludeSunday;
            _excludeHolidaysCheckBox!.IsChecked = Config.ExcludeHolidays;
            _countMakeupDaysCheckBox!.IsChecked = Config.CountMakeupDays;
            _excludeVacationsCheckBox!.IsChecked = Config.ExcludeVacations;
            _respectCustomDatesCheckBox!.IsChecked = Config.RespectCustomDates;
        }
        finally
        {
            _isUpdatingControls = false;
        }
    }

    private void UpdateSemesterOptionEnabledState()
    {
        var manualStart = Config.StartSource == SemesterStartSource.Manual;
        _manualStartDatePicker!.IsEnabled = manualStart;

        var manualEnd = Config.EndSource == SemesterEndSource.ManualDate;
        _manualEndDatePicker!.IsEnabled = manualEnd;
        _totalWeeksNumericUpDown!.IsEnabled = !manualEnd;

        // 自动获取时手动值不参与计算，禁用输入但保留原值，切回手动模式即可继续使用。
        _dailyHoursNumericUpDown!.IsEnabled = Config.DailyHoursSource != DailyHoursSource.Auto;
        UpdateDailyHoursHint();
    }

    /// <summary>刷新「每日在校时长」的说明文案，自动模式下显示当天档案实际读到的时长。</summary>
    private void UpdateDailyHoursHint()
    {
        if (_dailyHoursHintTextBlock == null)
        {
            return;
        }

        _dailyHoursHintTextBlock.Foreground = ThemeHelper.GetSubTextBrush();

        if (Config.DailyHoursSource != DailyHoursSource.Auto)
        {
            _dailyHoursHintTextBlock.Text = "手动指定：所有在校日按下方固定时长换算。";
            return;
        }

        _dailyHoursHintTextBlock.Text = _calendar.TryGetScheduleDailyHours(DateTime.Now, out var hours)
            ? $"自动获取：当天档案第一节课开始到最后一节课下课共 {hours:0.#} 小时。"
            : $"自动获取失败（{_calendar.LastScheduleReadError ?? "未知原因"}），将回退到下方手动值。";
    }

    private void RebuildVacationTable()
    {
        if (_vacationListHost == null)
        {
            return;
        }

        var rows = _calendar.Data.Vacations
            .OrderBy(x => x.Start)
            .Select(vacation => new Control[]
            {
                CreateCellText(vacation.Name),
                CreateCellText($"{vacation.Start:yyyy-MM-dd}"),
                CreateCellText($"{vacation.End:yyyy-MM-dd}"),
                CreateCellText($"{Math.Max(0, (vacation.End.Date - vacation.Start.Date).Days + 1)} 天"),
                CreateDeleteButton(() =>
                {
                    _calendar.Data.Vacations.Remove(vacation);
                    _calendar.Save();
                    RebuildVacationTable();
                    _ = RefreshSummaryAsync();
                })
            })
            .ToList();

        _vacationListHost.Children.Clear();
        _vacationListHost.Children.Add(CreateTable(
            new[] { "名称", "开始日期", "结束日期", "天数", "操作" },
            new double[] { 160, 120, 120, 70, 80 },
            rows));
        ApplyTheme();
    }

    private void RebuildCustomDateTable()
    {
        if (_customListHost == null)
        {
            return;
        }

        _customListHost.Children.Clear();
        _customListHost.Children.Add(BuildCustomDateTable("强制计入在校日", _calendar.Data.CustomIncluded));
        _customListHost.Children.Add(BuildCustomDateTable("强制排除", _calendar.Data.CustomExcluded));
        ApplyTheme();
    }

    private Border BuildCustomDateTable(string title, List<HolidayDay> items)
    {
        var rows = items
            .OrderBy(x => x.Date)
            .Select(day => new Control[]
            {
                CreateCellText($"{day.Date:yyyy-MM-dd}（{GetDayOfWeekText(day.Date)}）"),
                CreateCellText(day.Name),
                CreateDeleteButton(() =>
                {
                    items.Remove(day);
                    _calendar.Save();
                    RebuildCustomDateTable();
                    _ = RefreshSummaryAsync();
                })
            })
            .ToList();

        return CreateTable(
            new[] { title, "名称", "操作" },
            new double[] { 200, 160, 80 },
            rows);
    }

    private void RebuildHolidayTable()
    {
        if (_holidayListHost == null)
        {
            return;
        }

        var rows = new List<Control[]>();

        foreach (var day in _calendar.Data.MakeupDays.OrderBy(x => x.Date))
        {
            rows.Add(new Control[]
            {
                CreateCellText($"{day.Date:yyyy-MM-dd}（{GetDayOfWeekText(day.Date)}）"),
                CreateCellText(day.Name),
                CreateCellText("调休补班"),
                CreateSourceText(day.Source),
                CreateDeleteButton(() =>
                {
                    _calendar.Data.MakeupDays.Remove(day);
                    _calendar.Save();
                    RebuildHolidayTable();
                    _ = RefreshSummaryAsync();
                })
            });
        }

        foreach (var day in _calendar.Data.Holidays.OrderBy(x => x.Date))
        {
            rows.Add(new Control[]
            {
                CreateCellText($"{day.Date:yyyy-MM-dd}（{GetDayOfWeekText(day.Date)}）"),
                CreateCellText(day.Name),
                CreateCellText("放假日"),
                CreateSourceText(day.Source),
                CreateDeleteButton(() =>
                {
                    _calendar.Data.Holidays.Remove(day);
                    _calendar.Save();
                    RebuildHolidayTable();
                    _ = RefreshSummaryAsync();
                })
            });
        }

        _holidayListHost.Children.Clear();
        _holidayListHost.Children.Add(CreateTable(
            new[] { "日期", "名称", "类型", "来源", "操作" },
            new double[] { 200, 160, 90, 70, 80 },
            rows));

        ApplyTheme();
    }

    private void RebuildPeriodTable() => _ = RebuildPeriodTableAsync();

    private async Task RebuildPeriodTableAsync()
    {
        if (_periodTableHost == null)
        {
            return;
        }

        var config = Config;
        var start = AttendanceStatisticsHelper.ResolveSemesterStart(config);
        if (start == null)
        {
            _periodTableHost.Children.Clear();
            _periodTableHost.Children.Add(new TextBlock
            {
                Text = "尚未获取学期开始日，无法进行分段统计。",
                Tag = "sub"
            });
            ApplyTheme();
            return;
        }

        var end = AttendanceStatisticsHelper.ResolveSemesterEnd(start.Value, config);
        var byMonth = _periodModeComboBox?.SelectedIndex == 1;

        AttendanceStatistics stats;
        try
        {
            stats = await Task.Run(() => _calendar.ComputeStatistics(start.Value, end, DateTime.Now));
        }
        catch (Exception ex)
        {
            _periodTableHost.Children.Clear();
            _periodTableHost.Children.Add(new TextBlock { Text = $"分段统计失败：{ex.Message}", Tag = "sub" });
            ApplyTheme();
            return;
        }

        var periods = byMonth ? stats.Monthly : stats.Weekly;

        var rows = periods.Select(period => new[]
        {
            CreateCellText(period.Label),
            CreateCellText(period.RangeText),
            CreateCellText($"{period.ElapsedInSchoolDays} / {period.InSchoolDays} 天"),
            CreateCellText($"{period.Hours:0.#} 小时")
        }).ToList();

        _periodTableHost.Children.Clear();
        if (rows.Count == 0)
        {
            _periodTableHost.Children.Add(new TextBlock { Text = "当前区间内没有在校日。", Tag = "sub" });
        }
        else
        {
            _periodTableHost.Children.Add(CreateTable(
                new[] { "分段", "日期范围", "已在校 / 总在校", "在校时长" },
                new double[] { 110, 140, 130, 100 },
                rows));
        }
        ApplyTheme();
    }

    private async Task RefreshSummaryAsync()
    {
        try
        {
            var config = Config;
            var start = AttendanceStatisticsHelper.ResolveSemesterStart(config);

            if (start == null)
            {
                SetSummary("尚未获取学期开始日", "请在下方将学期开始日设为「手动指定」，或先在 ClassIsland 中配置学期开始时间。", 0);
                return;
            }

            var end = AttendanceStatisticsHelper.ResolveSemesterEnd(start.Value, config);
            var stats = await Task.Run(() => _calendar.ComputeStatistics(start.Value, end, DateTime.Now));

            if (stats.NotStarted)
            {
                SetSummary($"距开学还有 {(stats.StartDate - stats.Today).Days} 天",
                    $"学期区间：{stats.StartDate:yyyy-MM-dd} ~ {stats.EndDate:yyyy-MM-dd}，预计在校 {stats.TotalInSchoolDays} 天。",
                    0);
            }
            else if (stats.Finished)
            {
                SetSummary($"本学期已结束：共在校 {stats.TotalInSchoolDays} 天，约 {stats.TotalHours:0.#} 小时",
                    $"学期区间：{stats.StartDate:yyyy-MM-dd} ~ {stats.EndDate:yyyy-MM-dd}。",
                    100);
            }
            else
            {
                SetSummary(
                    $"本学期已在校 {stats.ElapsedInSchoolDays} 天 / 共 {stats.TotalInSchoolDays} 天，约 {stats.ElapsedHours:0.#} 小时",
                    $"进度 {stats.ProgressPercent:0.#}%；剩余 {stats.RemainingInSchoolDays} 天 · 约 {stats.RemainingHours:0.#} 小时；" +
                    $"每日在校时长 {stats.DailyHours:0.#} 小时（{DailyHoursSourceText()}）。",
                    stats.ProgressPercent);
            }

            if (_semesterRangeTextBlock != null)
            {
                _semesterRangeTextBlock.Text =
                    $"当前生效学期区间：{stats.StartDate:yyyy-MM-dd} ~ {stats.EndDate:yyyy-MM-dd}（共 {stats.TotalCalendarDays} 个自然日）。";
            }
        }
        catch (Exception ex)
        {
            SetSummary("统计失败", ex.Message, 0);
        }
    }

    /// <summary>「每日在校时长」取值方式的展示文案。</summary>
    private string DailyHoursSourceText() =>
        Config.DailyHoursSource == DailyHoursSource.Auto ? "自动获取当天档案课表" : "手动指定";

    private void SetSummary(string text, string detail, double progress)
    {
        if (_summaryTextBlock != null)
        {
            _summaryTextBlock.Text = text;
        }
        if (_summaryDetailTextBlock != null)
        {
            _summaryDetailTextBlock.Text = detail;
        }
        if (_summaryProgressBar != null)
        {
            _summaryProgressBar.Value = progress;
        }
    }

    private void SetStatus(string text)
    {
        if (_statusTextBlock != null)
        {
            _statusTextBlock.Text = text;
        }
    }

    // ==================== 事件处理 ====================

    private void OnSemesterOptionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls || _startSourceComboBox == null || _endSourceComboBox == null)
        {
            return;
        }

        Config.StartSource = _startSourceComboBox.SelectedIndex == 1
            ? SemesterStartSource.Manual
            : SemesterStartSource.ClassIsland;
        Config.EndSource = _endSourceComboBox.SelectedIndex == 1
            ? SemesterEndSource.ManualDate
            : SemesterEndSource.TotalWeeks;

        UpdateSemesterOptionEnabledState();
        _ = RefreshSummaryAsync();
        RebuildPeriodTable();
    }

    private void OnDailyHoursSourceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls || _dailyHoursSourceComboBox == null)
        {
            return;
        }

        Config.DailyHoursSource = _dailyHoursSourceComboBox.SelectedIndex == 0
            ? DailyHoursSource.Auto
            : DailyHoursSource.Manual;

        UpdateSemesterOptionEnabledState();
        _ = RefreshSummaryAsync();
        RebuildPeriodTable();
    }

    private void OnSemesterDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (_manualStartDatePicker?.SelectedDate != null)
        {
            Config.ManualStartDate = _manualStartDatePicker.SelectedDate.Value.Date;
        }
        if (_manualEndDatePicker?.SelectedDate != null)
        {
            Config.ManualEndDate = _manualEndDatePicker.SelectedDate.Value.Date;
        }

        _ = RefreshSummaryAsync();
        RebuildPeriodTable();
    }

    private void OnSemesterNumberChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (_totalWeeksNumericUpDown?.Value != null)
        {
            Config.TotalWeeks = (int)_totalWeeksNumericUpDown.Value.Value;
        }
        if (_dailyHoursNumericUpDown?.Value != null)
        {
            Config.DailyHours = (double)_dailyHoursNumericUpDown.Value.Value;
        }

        _ = RefreshSummaryAsync();
        RebuildPeriodTable();
    }

    private void OnRuleChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Config.ExcludeSaturday = _excludeSaturdayCheckBox?.IsChecked ?? false;
        Config.ExcludeSunday = _excludeSundayCheckBox?.IsChecked ?? false;
        Config.ExcludeHolidays = _excludeHolidaysCheckBox?.IsChecked ?? false;
        Config.CountMakeupDays = _countMakeupDaysCheckBox?.IsChecked ?? false;
        Config.ExcludeVacations = _excludeVacationsCheckBox?.IsChecked ?? false;
        Config.RespectCustomDates = _respectCustomDatesCheckBox?.IsChecked ?? false;

        _ = RefreshSummaryAsync();
        RebuildPeriodTable();
    }

    private void OnAddVacationClick(object? sender, RoutedEventArgs e)
    {
        var start = _newVacationStartDatePicker?.SelectedDate?.Date;
        var end = _newVacationEndDatePicker?.SelectedDate?.Date;
        if (start == null || end == null)
        {
            SetStatus("请先选择假期的开始与结束日期。");
            return;
        }

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var name = string.IsNullOrWhiteSpace(_newVacationNameTextBox?.Text)
            ? "自定义假期"
            : _newVacationNameTextBox!.Text.Trim();

        _calendar.Data.Vacations.Add(new VacationRange { Name = name, Start = start.Value, End = end.Value });
        _calendar.Save();
        SetStatus($"已添加假期「{name}」（{start:yyyy-MM-dd} ~ {end:yyyy-MM-dd}）。");

        _newVacationNameTextBox!.Text = string.Empty;
        RebuildVacationTable();
        _ = RefreshSummaryAsync();
    }

    private void OnAddCustomDateClick(object? sender, RoutedEventArgs e)
    {
        var date = _newCustomDatePicker?.SelectedDate?.Date;
        if (date == null)
        {
            SetStatus("请先选择日期。");
            return;
        }

        var name = string.IsNullOrWhiteSpace(_newCustomNameTextBox?.Text)
            ? "自定义日期"
            : _newCustomNameTextBox!.Text.Trim();
        var included = _newCustomTypeComboBox?.SelectedIndex != 1;

        var entry = new HolidayDay
        {
            Date = date.Value,
            Name = name,
            IsOffDay = !included,
            Source = HolidayEntrySource.Manual
        };
        (included ? _calendar.Data.CustomIncluded : _calendar.Data.CustomExcluded).Add(entry);
        _calendar.Save();
        SetStatus($"已{(included ? "强制计入" : "强制排除")} {date:yyyy-MM-dd}（{name}）。");

        _newCustomNameTextBox!.Text = string.Empty;
        RebuildCustomDateTable();
        _ = RefreshSummaryAsync();
    }

    private void OnAddHolidayClick(object? sender, RoutedEventArgs e)
    {
        var date = _newHolidayDatePicker?.SelectedDate?.Date;
        if (date == null)
        {
            SetStatus("请先选择日期。");
            return;
        }

        var name = string.IsNullOrWhiteSpace(_newHolidayNameTextBox?.Text)
            ? "自定义条目"
            : _newHolidayNameTextBox!.Text.Trim();
        var isOffDay = _newHolidayTypeComboBox?.SelectedIndex != 1;

        var entry = new HolidayDay
        {
            Date = date.Value,
            Name = name,
            IsOffDay = isOffDay,
            Source = HolidayEntrySource.Manual
        };
        (isOffDay ? _calendar.Data.Holidays : _calendar.Data.MakeupDays).Add(entry);
        _calendar.Save();
        SetStatus($"已添加{(isOffDay ? "放假日" : "调休补班日")} {date:yyyy-MM-dd}（{name}）。");

        _newHolidayNameTextBox!.Text = string.Empty;
        RebuildHolidayTable();
        _ = RefreshSummaryAsync();
    }

    private async void OnUpdateFromNetworkClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        SetStatus("正在从网络更新节假日数据…");
        try
        {
            var config = Config;
            var start = AttendanceStatisticsHelper.ResolveSemesterStart(config) ?? DateTime.Today;
            var end = AttendanceStatisticsHelper.ResolveSemesterEnd(start, config);
            var years = AttendanceCalendarService.BuildUpdateYears(start, end);

            var result = await _calendar.UpdateFromNetworkAsync(years);
            SetStatus(result.ToDisplayText());

            RebuildHolidayTable();
            await RefreshSummaryAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"更新失败：{ex.Message}");
        }
        finally
        {
            if (sender is Button b)
            {
                b.IsEnabled = true;
            }
        }
    }

    private void OnRestoreBuiltinClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var count = _calendar.RestoreBuiltinData();
            SetStatus($"已恢复内置节假日数据，当前共 {count} 条记录（手动条目已保留）。");
            RebuildHolidayTable();
            _ = RefreshSummaryAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"恢复失败：{ex.Message}");
        }
    }

    // ==================== 主题与生命周期 ====================

    private void ApplyTheme()
    {
        if (Content is not Control root)
        {
            return;
        }

        var textBrush = ThemeHelper.GetTextBrush();
        var subTextBrush = ThemeHelper.GetSubTextBrush();
        var separatorBrush = ThemeHelper.GetSeparatorBrush();

        foreach (var descendant in root.GetVisualDescendants())
        {
            switch (descendant)
            {
                case TextBlock textBlock:
                    textBlock.Foreground = textBlock.Tag as string == "sub" ? subTextBrush : textBrush;
                    break;
                case CheckBox checkBox:
                    checkBox.Foreground = textBrush;
                    break;
                case Border border when border.Tag as string is "table-border" or "table-cell":
                    border.BorderBrush = separatorBrush;
                    break;
            }
        }

        if (_summaryProgressBar != null &&
            Application.Current?.Styles.TryGetResource("AccentFillColorDefaultBrush",
                Application.Current.ActualThemeVariant, out var accentBrush) == true &&
            accentBrush is IBrush brush)
        {
            _summaryProgressBar.Foreground = brush;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => ApplyTheme();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        RefreshAll();
        ApplyTheme();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime date) => new(date.Date);

    private static string GetDayOfWeekText(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "周一",
        DayOfWeek.Tuesday => "周二",
        DayOfWeek.Wednesday => "周三",
        DayOfWeek.Thursday => "周四",
        DayOfWeek.Friday => "周五",
        DayOfWeek.Saturday => "周六",
        _ => "周日"
    };
}