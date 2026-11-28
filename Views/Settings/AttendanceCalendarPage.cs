using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 「在校时间统计」设置页：集中管理学期区间、统计口径、节假日/调休、寒暑假与自定义日期，
/// 并展示统计概览与按周/月的分段统计。数据由 <see cref="AttendanceCalendarService"/> 统一持久化。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandAttendance", "在校时间统计",
    MaterialDesignThemes.Wpf.PackIconKind.CalendarCheck,
    MaterialDesignThemes.Wpf.PackIconKind.CalendarCheck,
    SettingsPageCategory.External)]
public partial class AttendanceCalendarPage : SettingsPageBase
{
    private readonly AttendanceCalendarService _calendar;

    private AttendanceStatisticsConfig Config => _calendar.Data.Config;

    private ComboBox _startSourceComboBox = null!;
    private DatePicker _manualStartDatePicker = null!;
    private ComboBox _endSourceComboBox = null!;
    private WpfNumericUpDown _totalWeeksNumericUpDown = null!;
    private DatePicker _manualEndDatePicker = null!;
    private WpfNumericUpDown _dailyHoursNumericUpDown = null!;
    private ComboBox _dailyHoursSourceComboBox = null!;
    private ComboBox _periodModeComboBox = null!;

    private CheckBox _excludeSaturdayCheckBox = null!;
    private CheckBox _excludeSundayCheckBox = null!;
    private CheckBox _excludeHolidaysCheckBox = null!;
    private CheckBox _countMakeupDaysCheckBox = null!;
    private CheckBox _excludeVacationsCheckBox = null!;
    private CheckBox _respectCustomDatesCheckBox = null!;

    /// <summary>程序化回填控件时置位，避免控件事件把配置改回控件中的旧值。</summary>
    private bool _isUpdatingControls;

    private static readonly object[] StartSourceItems = { "自动读取 ClassIsland 学期开始时间", "手动指定" };
    private static readonly object[] EndSourceItems = { "按学期周数推算", "手动指定" };
    private static readonly object[] DailyHoursSourceItems = { "自动获取", "手动指定" };
    private static readonly object[] HolidayTypeItems = { "放假日", "调休补班日" };
    private static readonly object[] CustomTypeItems = { "强制计入在校日", "强制排除" };
    private static readonly object[] PeriodModeItems = { "按周", "按月" };

    public AttendanceCalendarPage() : this(null)
    {
    }

    public AttendanceCalendarPage(AttendanceCalendarService? calendar = null)
    {
        // 宿主 DI 解析时注入 AttendanceCalendarService 单例；手动 new（无参）时回退到静态实例。
        _calendar = calendar
                    ?? IAppHost.TryGetService<AttendanceCalendarService>()
                    ?? AttendanceCalendarService.Instance
                    ?? new AttendanceCalendarService();

        try
        {
            InitializeComponent();
            InitializeControlReferences();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
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

    /// <summary>
    /// 从 XAML 的 SettingsControl.Switcher 中提取控件引用并填充下拉选项。
    /// （SettingsControl 模板内元素无法使用 x:Name，需通过 Switcher 属性访问）
    /// </summary>
    private void InitializeControlReferences()
    {
        _startSourceComboBox = (ComboBox)StartSourceItem.Switcher;
        _manualStartDatePicker = (DatePicker)ManualStartDateItem.Switcher;
        _endSourceComboBox = (ComboBox)EndSourceItem.Switcher;
        _totalWeeksNumericUpDown = (WpfNumericUpDown)TotalWeeksItem.Switcher;
        _manualEndDatePicker = (DatePicker)ManualEndDateItem.Switcher;
        _dailyHoursSourceComboBox = (ComboBox)DailyHoursSourceItem.Switcher;
        _dailyHoursNumericUpDown = (WpfNumericUpDown)DailyHoursItem.Switcher;
        _periodModeComboBox = (ComboBox)PeriodModeItem.Switcher;

        _excludeSaturdayCheckBox = (CheckBox)ExcludeSaturdayItem.Switcher;
        _excludeSundayCheckBox = (CheckBox)ExcludeSundayItem.Switcher;
        _excludeHolidaysCheckBox = (CheckBox)ExcludeHolidaysItem.Switcher;
        _countMakeupDaysCheckBox = (CheckBox)CountMakeupDaysItem.Switcher;
        _excludeVacationsCheckBox = (CheckBox)ExcludeVacationsItem.Switcher;
        _respectCustomDatesCheckBox = (CheckBox)RespectCustomDatesItem.Switcher;

        _isUpdatingControls = true;
        try
        {
            foreach (var item in StartSourceItems)
            {
                _startSourceComboBox.Items.Add(item);
            }
            foreach (var item in EndSourceItems)
            {
                _endSourceComboBox.Items.Add(item);
            }
            foreach (var item in DailyHoursSourceItems)
            {
                _dailyHoursSourceComboBox.Items.Add(item);
            }
            foreach (var item in PeriodModeItems)
            {
                _periodModeComboBox.Items.Add(item);
            }
            foreach (var item in HolidayTypeItems)
            {
                NewHolidayTypeComboBox.Items.Add(item);
            }
            foreach (var item in CustomTypeItems)
            {
                NewCustomTypeComboBox.Items.Add(item);
            }

            _periodModeComboBox.SelectedIndex = 0;
            NewHolidayTypeComboBox.SelectedIndex = 0;
            NewCustomTypeComboBox.SelectedIndex = 1;
        }
        finally
        {
            _isUpdatingControls = false;
        }
    }

    // ==================== 行与表格辅助 ====================

    /// <summary>
    /// 构建表格：外层边框负责上/左边线，单元格负责右/下边线，拼合为完整网格。
    /// 颜色统一走宿主主题资源，随主题深浅自动切换。
    /// </summary>
    private static Border CreateTable(string[] headers, double[] widths, IEnumerable<UIElement[]> rows)
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
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
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

        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            Child = grid
        };
        outerBorder.SetResourceReference(Border.BorderBrushProperty, "MaterialDesignDivider");
        return outerBorder;
    }

    private static void AddCell(Grid grid, int row, int column, UIElement child)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(6, 3, 6, 3),
            Child = child
        };
        border.SetResourceReference(Border.BorderBrushProperty, "MaterialDesignDivider");
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    private static TextBlock CreateCellText(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
        return textBlock;
    }

    private static TextBlock CreateSourceText(HolidayEntrySource source)
    {
        return CreateCellText(source switch
        {
            HolidayEntrySource.Builtin => "内置",
            HolidayEntrySource.Network => "联网",
            _ => "手动"
        });
    }

    private static Button CreateDeleteButton(Action onDelete)
    {
        var button = new Button
        {
            Content = "删除",
            Padding = new Thickness(8, 2, 8, 2)
        };
        button.SetResourceReference(FrameworkElement.StyleProperty, "MaterialDesignFlatButton");
        button.Click += (_, _) => onDelete();
        return button;
    }

    private static TextBlock CreateSubText(string text)
    {
        var textBlock = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBodyLight");
        return textBlock;
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

        if (string.IsNullOrEmpty(StatusTextBlock.Text))
        {
            var lastUpdated = _calendar.Data.LastUpdated;
            var count = _calendar.Data.Holidays.Count + _calendar.Data.MakeupDays.Count;
            StatusTextBlock.Text = lastUpdated == null
                ? $"当前共 {count} 条节假日/调休记录。"
                : $"当前共 {count} 条节假日/调休记录；最近联网更新：{lastUpdated:yyyy-MM-dd HH:mm}（{_calendar.Data.LastUpdateSource}）";
        }
    }

    private void UpdateSemesterControls()
    {
        _isUpdatingControls = true;
        try
        {
            _startSourceComboBox.SelectedIndex = Config.StartSource == SemesterStartSource.Manual ? 1 : 0;
            _endSourceComboBox.SelectedIndex = Config.EndSource == SemesterEndSource.ManualDate ? 1 : 0;
            _manualStartDatePicker.SelectedDate = Config.ManualStartDate.Date;
            _manualEndDatePicker.SelectedDate = Config.ManualEndDate.Date;
            _totalWeeksNumericUpDown.Value = Config.TotalWeeks;
            _dailyHoursNumericUpDown.Value = (decimal)Config.DailyHours;
            _dailyHoursSourceComboBox.SelectedIndex = Config.DailyHoursSource == DailyHoursSource.Auto ? 0 : 1;
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
            _excludeSaturdayCheckBox.IsChecked = Config.ExcludeSaturday;
            _excludeSundayCheckBox.IsChecked = Config.ExcludeSunday;
            _excludeHolidaysCheckBox.IsChecked = Config.ExcludeHolidays;
            _countMakeupDaysCheckBox.IsChecked = Config.CountMakeupDays;
            _excludeVacationsCheckBox.IsChecked = Config.ExcludeVacations;
            _respectCustomDatesCheckBox.IsChecked = Config.RespectCustomDates;
        }
        finally
        {
            _isUpdatingControls = false;
        }
    }

    private void UpdateSemesterOptionEnabledState()
    {
        var manualStart = Config.StartSource == SemesterStartSource.Manual;
        _manualStartDatePicker.IsEnabled = manualStart;

        var manualEnd = Config.EndSource == SemesterEndSource.ManualDate;
        _manualEndDatePicker.IsEnabled = manualEnd;
        _totalWeeksNumericUpDown.IsEnabled = !manualEnd;

        // 自动获取时手动值不参与计算，禁用输入但保留原值，切回手动模式即可继续使用。
        _dailyHoursNumericUpDown.IsEnabled = Config.DailyHoursSource != DailyHoursSource.Auto;
        UpdateDailyHoursHint();
    }

    /// <summary>刷新「每日在校时长」的说明文案，自动模式下显示当天档案实际读到的时长。</summary>
    private void UpdateDailyHoursHint()
    {
        if (Config.DailyHoursSource != DailyHoursSource.Auto)
        {
            DailyHoursHintTextBlock.Text = "手动指定：所有在校日按下方固定时长换算。";
            return;
        }

        DailyHoursHintTextBlock.Text = _calendar.TryGetScheduleDailyHours(DateTime.Now, out var hours)
            ? $"自动获取：当天档案第一节课开始到最后一节课下课共 {hours:0.#} 小时。"
            : $"自动获取失败（{_calendar.LastScheduleReadError ?? "未知原因"}），将回退到下方手动值。";
    }

    private void RebuildVacationTable()
    {
        var rows = _calendar.Data.Vacations
            .OrderBy(x => x.Start)
            .Select(vacation => new UIElement[]
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

        VacationListHost.Children.Clear();
        VacationListHost.Children.Add(CreateTable(
            new[] { "名称", "开始日期", "结束日期", "天数", "操作" },
            new double[] { 160, 120, 120, 70, 80 },
            rows));
    }

    private void RebuildCustomDateTable()
    {
        CustomListHost.Children.Clear();
        CustomListHost.Children.Add(BuildCustomDateTable("强制计入在校日", _calendar.Data.CustomIncluded));
        CustomListHost.Children.Add(BuildCustomDateTable("强制排除", _calendar.Data.CustomExcluded));
    }

    private Border BuildCustomDateTable(string title, List<HolidayDay> items)
    {
        var rows = items
            .OrderBy(x => x.Date)
            .Select(day => new UIElement[]
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
        var rows = new List<UIElement[]>();

        foreach (var day in _calendar.Data.MakeupDays.OrderBy(x => x.Date))
        {
            rows.Add(new UIElement[]
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
            rows.Add(new UIElement[]
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

        HolidayListHost.Children.Clear();
        HolidayListHost.Children.Add(CreateTable(
            new[] { "日期", "名称", "类型", "来源", "操作" },
            new double[] { 200, 160, 90, 70, 80 },
            rows));
    }

    private void RebuildPeriodTable() => _ = RebuildPeriodTableAsync();

    private async Task RebuildPeriodTableAsync()
    {
        var config = Config;
        var start = AttendanceStatisticsHelper.ResolveSemesterStart(config);
        if (start == null)
        {
            PeriodTableHost.Children.Clear();
            PeriodTableHost.Children.Add(CreateSubText("尚未获取学期开始日，无法进行分段统计。"));
            return;
        }

        var end = AttendanceStatisticsHelper.ResolveSemesterEnd(start.Value, config);
        var byMonth = _periodModeComboBox.SelectedIndex == 1;

        AttendanceStatistics stats;
        try
        {
            // 宿主课表反射查询与区间遍历耗时较长，放后台线程执行，避免阻塞 UI。
            stats = await Task.Run(() => _calendar.ComputeStatistics(start.Value, end, DateTime.Now));
        }
        catch (Exception ex)
        {
            PeriodTableHost.Children.Clear();
            PeriodTableHost.Children.Add(CreateSubText($"分段统计失败：{ex.Message}"));
            return;
        }

        var periods = byMonth ? stats.Monthly : stats.Weekly;

        var rows = periods.Select(period => new UIElement[]
        {
            CreateCellText(period.Label),
            CreateCellText(period.RangeText),
            CreateCellText($"{period.ElapsedInSchoolDays} / {period.InSchoolDays} 天"),
            CreateCellText($"{period.Hours:0.#} 小时")
        }).ToList();

        PeriodTableHost.Children.Clear();
        if (rows.Count == 0)
        {
            PeriodTableHost.Children.Add(CreateSubText("当前区间内没有在校日。"));
        }
        else
        {
            PeriodTableHost.Children.Add(CreateTable(
                new[] { "分段", "日期范围", "已在校 / 总在校", "在校时长" },
                new double[] { 110, 140, 130, 100 },
                rows));
        }
    }

    private async Task RefreshSummaryAsync()
    {
        try
        {
            var config = Config;
            var start = AttendanceStatisticsHelper.ResolveSemesterStart(config);

            if (start == null)
            {
                SetSummary("尚未获取学期开始日", "请将学期开始日设为「手动指定」，或先在 ClassIsland 中配置学期开始时间。", 0);
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

            SemesterRangeTextBlock.Text =
                $"当前生效学期区间：{stats.StartDate:yyyy-MM-dd} ~ {stats.EndDate:yyyy-MM-dd}（共 {stats.TotalCalendarDays} 个自然日）。";
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
        SummaryTextBlock.Text = text;
        SummaryDetailTextBlock.Text = detail;
        SummaryProgressBar.Value = progress;
    }

    // ==================== 事件处理 ====================

    private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

    private void OnSemesterOptionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
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

    private void OnDailyHoursSourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
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

    private void OnSemesterDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (_manualStartDatePicker.SelectedDate != null)
        {
            Config.ManualStartDate = _manualStartDatePicker.SelectedDate.Value.Date;
        }
        if (_manualEndDatePicker.SelectedDate != null)
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

        if (_totalWeeksNumericUpDown.Value != null)
        {
            Config.TotalWeeks = (int)_totalWeeksNumericUpDown.Value.Value;
        }
        if (_dailyHoursNumericUpDown.Value != null)
        {
            Config.DailyHours = (double)_dailyHoursNumericUpDown.Value.Value;
        }

        _ = RefreshSummaryAsync();
        RebuildPeriodTable();
    }

    private void OnRuleChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Config.ExcludeSaturday = _excludeSaturdayCheckBox.IsChecked ?? false;
        Config.ExcludeSunday = _excludeSundayCheckBox.IsChecked ?? false;
        Config.ExcludeHolidays = _excludeHolidaysCheckBox.IsChecked ?? false;
        Config.CountMakeupDays = _countMakeupDaysCheckBox.IsChecked ?? false;
        Config.ExcludeVacations = _excludeVacationsCheckBox.IsChecked ?? false;
        Config.RespectCustomDates = _respectCustomDatesCheckBox.IsChecked ?? false;

        _ = RefreshSummaryAsync();
        RebuildPeriodTable();
    }

    private void OnPeriodModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        RebuildPeriodTable();
    }

    private void OnAddVacationClick(object sender, RoutedEventArgs e)
    {
        var start = NewVacationStartDatePicker.SelectedDate?.Date;
        var end = NewVacationEndDatePicker.SelectedDate?.Date;
        if (start == null || end == null)
        {
            SetStatus("请先选择假期的开始与结束日期。");
            return;
        }

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var name = string.IsNullOrWhiteSpace(NewVacationNameTextBox.Text)
            ? "自定义假期"
            : NewVacationNameTextBox.Text.Trim();

        _calendar.Data.Vacations.Add(new VacationRange { Name = name, Start = start.Value, End = end.Value });
        _calendar.Save();
        SetStatus($"已添加假期「{name}」（{start:yyyy-MM-dd} ~ {end:yyyy-MM-dd}）。");

        NewVacationNameTextBox.Text = string.Empty;
        RebuildVacationTable();
        _ = RefreshSummaryAsync();
    }

    private void OnAddCustomDateClick(object sender, RoutedEventArgs e)
    {
        var date = NewCustomDatePicker.SelectedDate?.Date;
        if (date == null)
        {
            SetStatus("请先选择日期。");
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewCustomNameTextBox.Text)
            ? "自定义日期"
            : NewCustomNameTextBox.Text.Trim();
        var included = NewCustomTypeComboBox.SelectedIndex != 1;

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

        NewCustomNameTextBox.Text = string.Empty;
        RebuildCustomDateTable();
        _ = RefreshSummaryAsync();
    }

    private void OnAddHolidayClick(object sender, RoutedEventArgs e)
    {
        var date = NewHolidayDatePicker.SelectedDate?.Date;
        if (date == null)
        {
            SetStatus("请先选择日期。");
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewHolidayNameTextBox.Text)
            ? "自定义条目"
            : NewHolidayNameTextBox.Text.Trim();
        var isOffDay = NewHolidayTypeComboBox.SelectedIndex != 1;

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

        NewHolidayNameTextBox.Text = string.Empty;
        RebuildHolidayTable();
        _ = RefreshSummaryAsync();
    }

    private async void OnUpdateFromNetworkClick(object sender, RoutedEventArgs e)
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

    private void OnRestoreBuiltinClick(object sender, RoutedEventArgs e)
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

    private void SetStatus(string text)
    {
        StatusTextBlock.Text = text;
    }

    // ==================== 生命周期 ====================

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        RefreshAll();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
    }

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
