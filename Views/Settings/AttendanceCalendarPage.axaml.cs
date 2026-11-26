using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 「在校时间统计」设置页：集中管理学期区间、统计口径、节假日/调休、寒暑假与自定义日期，
/// 并展示统计概览与按周/月的分段统计。数据由 <see cref="AttendanceCalendarService"/> 统一持久化。
/// <para>
/// 静态外壳（标题、分组折叠栏、输入控件）见 <c>AttendanceCalendarPage.axaml</c>；
/// 本文件只负责数据装配与动态表格生成。各分组使用 <c>CompatSettingsExpander</c>
/// （FA2/FA3 官方折叠栏，内部经 <see cref="IntroAnimationHelper.CreateExpandHost"/> 获得平滑高度过渡），
/// 页面可见时由 <see cref="IntroAnimationHelper.PlayIntro"/> 播放标题与分组的错峰入场动画。
/// </para>
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandAttendance", "在校时间统计", "\uE31C", "\uE31B")]
[Group("advancedtimeisland.main")]
public partial class AttendanceCalendarPage : SettingsPageBase
{
    private readonly AttendanceCalendarService _calendar;

    private AttendanceStatisticsConfig Config => _calendar.Data.Config;

    /// <summary>最近一次统计结果：切换分段方式（按周/按月）时直接复用，避免重复整学期计算。</summary>
    private AttendanceStatistics? _lastStats;

    /// <summary>入场动画只播放一次（避免页面重新挂载时重复错峰入场）。</summary>
    private bool _introPlayed;

    /// <summary>程序化回填控件时置位，避免控件事件把配置改回控件中的旧值。</summary>
    private bool _isUpdatingControls;

    /// <summary>
    /// 页面初始化期间置位：控件模板应用 / 首次回填会产生一批无关的变更事件，
    /// 这些事件不能写入配置，否则会误触发配置保存与统计重算。
    /// </summary>
    private bool _isInitializing = true;

    /// <summary>控件事件是否应忽略（程序化回填或页面初始化中）。</summary>
    private bool IsControlEventIgnored => _isUpdatingControls || _isInitializing;

    // 主题画刷缓存：表格单元格有数百个，逐次调用 ThemeHelper 会为每个控件新建画刷实例，
    // 且每次重建都全量遍历可视树重设属性，是页面卡顿的主要来源。
    private IBrush _textBrush = Brushes.White;
    private IBrush _subTextBrush = Brushes.LightGray;
    private IBrush _separatorBrush = new SolidColorBrush(Color.Parse("#555555"));

    // 已渲染过的统计结果：同一份结果（缓存对象未变）无需重复重建概览与分段表格。
    private AttendanceStatistics? _appliedSummaryStats;
    private AttendanceStatistics? _appliedPeriodStats;
    private int _appliedPeriodModeIndex = -1;

    // 「每日在校时长」提示缓存：避免每次切换选项都在 UI 线程反射读取课表。
    private DateTime _dailyHoursHintDate = DateTime.MinValue;
    private string? _dailyHoursHintAutoText;

    // 节假日表格分批构建的版本号：重新构建时作废上一轮未完成的批次。
    private int _holidayTableBuildVersion;

    public AttendanceCalendarPage()
    {
        _calendar = IAppHost.TryGetService<AttendanceCalendarService>()
                    ?? AttendanceCalendarService.Instance
                    ?? new AttendanceCalendarService();

        try
        {
            InitializeComponent();
            UpdateThemeBrushes();
            WireUI();
            IntroAnimationHelper.EnableSmoothScroll(this);
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

    // ==================== 页面装配 ====================

    /// <summary>
    /// axaml 外壳加载完成后的装配：各分组已是 <c>CompatSettingsExpander</c>
    /// （模板应用时自动创建平滑展开宿主），这里只做控件初值填充。
    /// </summary>
    private void WireUI()
    {
        // 分段方式初值：先置守卫再赋值，避免初始化误触发重建表格
        _isUpdatingControls = true;
        PeriodModeComboBox.SelectedIndex = 0;
        _isUpdatingControls = false;
    }

    // ==================== 表格辅助 ====================

    /// <summary>
    /// 构建表格：外层边框负责上/左边线，单元格负责右/下边线，拼合为完整网格。
    /// </summary>
    private Border CreateTable(string[] headers, double[] widths, IEnumerable<Control[]> rows)
    {
        var (table, grid) = CreateEmptyTable(headers, widths);
        foreach (var cells in rows)
        {
            AppendTableRow(grid, cells, headers.Length);
        }

        return table;
    }

    /// <summary>只创建表头与外框；数据行通过 <see cref="AppendTableRow"/> 分批追加。</summary>
    private (Border Table, Grid Grid) CreateEmptyTable(string[] headers, double[] widths)
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
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = _textBrush
            });
        }

        var table = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = _separatorBrush,
            Tag = "table-border",
            Child = grid
        };

        return (table, grid);
    }

    /// <summary>向表格追加一行（行号取当前行定义数）。</summary>
    private void AppendTableRow(Grid grid, Control[] cells, int columnCount)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var rowIndex = grid.RowDefinitions.Count - 1;
        for (var c = 0; c < columnCount && c < cells.Length; c++)
        {
            AddCell(grid, rowIndex, c, cells[c]);
        }
    }

    private void AddCell(Grid grid, int row, int column, Control child)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = _separatorBrush,
            Padding = new Thickness(6, 3, 6, 3),
            Child = child,
            Tag = "table-cell"
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    private TextBlock CreateCellText(string text)
    {
        return new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = _textBrush
        };
    }

    private TextBlock CreateSourceText(HolidayEntrySource source)
    {
        return new TextBlock
        {
            Text = source switch
            {
                HolidayEntrySource.Builtin => "内置",
                HolidayEntrySource.Network => "联网",
                _ => "手动"
            },
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = _textBrush
        };
    }

    private Button CreateDeleteButton(Action onDelete)
    {
        var button = new Button { Content = "删除", Padding = new Thickness(8, 2, 8, 2) };
        button.Click += (_, _) => onDelete();
        return button;
    }

    // ==================== 数据刷新 ====================

    /// <summary>刷新全部区域（异步分批执行，见 <see cref="RefreshAllAsync"/>）。</summary>
    private void RefreshAll(bool forceStatistics = false) => _ = RefreshAllAsync(forceStatistics);

    /// <summary>
    /// 刷新全部区域。
    /// 【不阻塞 UI】分多批执行，并在每批之间用 <c>await Task.Yield()</c> 把控制权交还 UI 线程：
    ///   各表格控件较多，一次性同步建完会让"打开设置页"出现明显卡顿；分批后界面在此期间可正常响应与重绘。
    /// </summary>
    private async Task RefreshAllAsync(bool forceStatistics = false)
    {
        try
        {
            UpdateSemesterControls();
            UpdateRuleControls();
            await Task.Yield();

            RebuildVacationTable();
            await Task.Yield();

            RebuildCustomDateTable();
            await Task.Yield();

            RebuildHolidayTable();
            await Task.Yield();

            // 统计由服务在后台线程计算并缓存，这里只取结果渲染
            RefreshStatistics(forceStatistics);

            if (string.IsNullOrEmpty(StatusTextBlock.Text))
            {
                var lastUpdated = _calendar.Data.LastUpdated;
                var count = _calendar.Data.Holidays.Count + _calendar.Data.MakeupDays.Count;
                StatusTextBlock.Text = lastUpdated == null
                    ? $"当前共 {count} 条节假日/调休记录。"
                    : $"当前共 {count} 条节假日/调休记录；最近联网更新：{lastUpdated:yyyy-MM-dd HH:mm}（{_calendar.Data.LastUpdateSource}）";
            }
        }
        catch (Exception ex)
        {
            SetStatus($"刷新失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 从后台统计缓存刷新「统计概览」与「分段统计」。
    /// 统计只由 <see cref="AttendanceStatisticsBackgroundService"/> 在后台计算并缓存，
    /// 进入页面时只读缓存、绝不触发重算；只有用户点击「刷新统计」才强制重算。
    /// 计算完成后由 <see cref="OnStatisticsUpdated"/> 回填。
    /// </summary>
    private void RefreshStatistics(bool forceRefresh = false)
    {
        if (AttendanceStatisticsHelper.ResolveSemesterStart(Config) == null)
        {
            _lastStats = null;
            SetSummary("尚未获取学期开始日", "请在下方将学期开始日设为「手动指定」，或先在 ClassIsland 中配置学期开始时间。", 0);
            ShowPeriodPlaceholder("尚未获取学期开始日，无法进行分段统计。");
            return;
        }

        var now = DateTime.Now;
        var cached = _calendar.CachedStatistics;
        if (cached != null)
        {
            _lastStats = cached;
            ApplySummary(cached);
            ApplyPeriodTable(cached);
        }
        else
        {
            _lastStats = null;
            SetSummary("正在后台统计…", "统计在后台线程完成，完成后会自动刷新。", 0);
            ShowPeriodPlaceholder("正在后台统计…");
        }

        // 仅「刷新统计」按钮强制重算；页面进入/其它刷新只读缓存，等待后台结果回填。
        if (forceRefresh)
        {
            _ = _calendar.GetStatisticsAsync(now, forceRefresh: true);
        }
    }

    /// <summary>后台统计完成：直接按最新缓存回填界面（已在 UI 线程）。</summary>
    private void OnStatisticsUpdated(object? sender, EventArgs e)
    {
        var cached = _calendar.CachedStatistics;
        if (cached == null)
        {
            return;
        }

        _lastStats = cached;
        ApplySummary(cached);
        ApplyPeriodTable(cached);
    }

    /// <summary>把统计结果写入「统计概览」区域（同一份结果只渲染一次）。</summary>
    private void ApplySummary(AttendanceStatistics stats)
    {
        if (ReferenceEquals(stats, _appliedSummaryStats))
        {
            return;
        }
        _appliedSummaryStats = stats;

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

    /// <summary>按当前分段方式把统计结果写入「分段统计」表格（同一份结果 + 同一分段方式只渲染一次）。</summary>
    private void ApplyPeriodTable(AttendanceStatistics stats)
    {
        var modeIndex = PeriodModeComboBox.SelectedIndex;
        if (ReferenceEquals(stats, _appliedPeriodStats) && modeIndex == _appliedPeriodModeIndex)
        {
            return;
        }
        _appliedPeriodStats = stats;
        _appliedPeriodModeIndex = modeIndex;

        var periods = modeIndex == 1 ? stats.Monthly : stats.Weekly;

        var rows = periods.Select(period => new[]
        {
            CreateCellText(period.Label),
            CreateCellText(period.RangeText),
            CreateCellText($"{period.ElapsedInSchoolDays} / {period.InSchoolDays} 天"),
            CreateCellText($"{period.Hours:0.#} 小时")
        }).ToList();

        PeriodTableHost.Children.Clear();
        if (rows.Count == 0)
        {
            PeriodTableHost.Children.Add(new TextBlock { Text = "当前区间内没有在校日。", Tag = "sub", Foreground = _subTextBrush });
        }
        else
        {
            PeriodTableHost.Children.Add(CreateTable(
                new[] { "分段", "日期范围", "已在校 / 总在校", "在校时长" },
                new double[] { 110, 140, 130, 100 },
                rows));
        }
        PeriodExpander.RefreshHost();
    }

    /// <summary>在「分段统计」区域显示一条提示（拿不到统计结果时）。</summary>
    private void ShowPeriodPlaceholder(string text)
    {
        _appliedPeriodStats = null;
        _appliedPeriodModeIndex = -1;
        _appliedSummaryStats = null;
        PeriodTableHost.Children.Clear();
        PeriodTableHost.Children.Add(new TextBlock { Text = text, Tag = "sub", Foreground = _subTextBrush });
        PeriodExpander.RefreshHost();
    }

    private void UpdateSemesterControls()
    {
        _isUpdatingControls = true;
        try
        {
            StartSourceComboBox.SelectedIndex = Config.StartSource == SemesterStartSource.Manual ? 1 : 0;
            EndSourceComboBox.SelectedIndex = Config.EndSource == SemesterEndSource.ManualDate ? 1 : 0;
            ManualStartDatePicker.SelectedDate = ToDateTimeOffset(Config.ManualStartDate);
            ManualEndDatePicker.SelectedDate = ToDateTimeOffset(Config.ManualEndDate);
            TotalWeeksNumericUpDown.Value = Config.TotalWeeks;
            DailyHoursNumericUpDown.Value = (decimal)Config.DailyHours;
            DailyHoursSourceComboBox.SelectedIndex = Config.DailyHoursSource == DailyHoursSource.Auto ? 0 : 1;
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
            ExcludeSaturdayCheckBox.IsChecked = Config.ExcludeSaturday;
            ExcludeSundayCheckBox.IsChecked = Config.ExcludeSunday;
            ExcludeHolidaysCheckBox.IsChecked = Config.ExcludeHolidays;
            CountMakeupDaysCheckBox.IsChecked = Config.CountMakeupDays;
            ExcludeVacationsCheckBox.IsChecked = Config.ExcludeVacations;
            RespectCustomDatesCheckBox.IsChecked = Config.RespectCustomDates;
        }
        finally
        {
            _isUpdatingControls = false;
        }
    }

    private void UpdateSemesterOptionEnabledState()
    {
        var manualStart = Config.StartSource == SemesterStartSource.Manual;
        ManualStartDatePicker.IsEnabled = manualStart;

        var manualEnd = Config.EndSource == SemesterEndSource.ManualDate;
        ManualEndDatePicker.IsEnabled = manualEnd;
        TotalWeeksNumericUpDown.IsEnabled = !manualEnd;

        // 自动获取时手动值不参与计算，禁用输入但保留原值，切回手动模式即可继续使用。
        DailyHoursNumericUpDown.IsEnabled = Config.DailyHoursSource != DailyHoursSource.Auto;
        UpdateDailyHoursHint();
    }

    /// <summary>
    /// 刷新「每日在校时长」的说明文案，自动模式下显示当天档案实际读到的时长。
    /// 课表读取需要宿主反射，放到后台线程执行；同一天的结果会缓存，切换选项时不再重复读取。
    /// </summary>
    private void UpdateDailyHoursHint()
    {
        DailyHoursHintTextBlock.Foreground = _subTextBrush;

        if (Config.DailyHoursSource != DailyHoursSource.Auto)
        {
            SetDailyHoursHintText("手动指定：所有在校日按下方固定时长换算。");
            return;
        }

        var today = DateTime.Now.Date;
        if (_dailyHoursHintDate == today && _dailyHoursHintAutoText != null)
        {
            SetDailyHoursHintText(_dailyHoursHintAutoText);
            return;
        }

        _ = UpdateDailyHoursHintAsync(today);
    }

    private async Task UpdateDailyHoursHintAsync(DateTime today)
    {
        var (ok, hours) = await Task.Run(() =>
        {
            var success = _calendar.TryGetScheduleDailyHours(today, out var value);
            return (success, value);
        });
        var text = ok
            ? $"自动获取：当天档案第一节课开始到最后一节课下课共 {hours:0.#} 小时。"
            : $"自动获取失败（{_calendar.LastScheduleReadError ?? "未知原因"}），将回退到下方手动值。";

        _dailyHoursHintDate = today;
        _dailyHoursHintAutoText = text;

        // 等待期间用户可能已切到手动模式，此时不再覆盖文案。
        if (Config.DailyHoursSource == DailyHoursSource.Auto)
        {
            SetDailyHoursHintText(text);
        }
    }

    private void SetDailyHoursHintText(string text)
    {
        if (DailyHoursHintTextBlock.Text == text)
        {
            return;
        }

        DailyHoursHintTextBlock.Text = text;
        SemesterExpander.RefreshHost();
    }

    private void RebuildVacationTable()
    {
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
                    RefreshStatistics();
                })
            })
            .ToList();

        VacationListHost.Children.Clear();
        VacationListHost.Children.Add(CreateTable(
            new[] { "名称", "开始日期", "结束日期", "天数", "操作" },
            new double[] { 160, 120, 120, 70, 80 },
            rows));
        VacationExpander.RefreshHost();
    }

    private void RebuildCustomDateTable()
    {
        CustomListHost.Children.Clear();
        CustomListHost.Children.Add(BuildCustomDateTable("强制计入在校日", _calendar.Data.CustomIncluded));
        CustomListHost.Children.Add(BuildCustomDateTable("强制排除", _calendar.Data.CustomExcluded));
        CustomExpander.RefreshHost();
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
                    RefreshStatistics();
                })
            })
            .ToList();

        return CreateTable(
            new[] { title, "名称", "操作" },
            new double[] { 200, 160, 80 },
            rows);
    }

    /// <summary>
    /// 重建「法定节假日与调休」表格。节假日条目通常上百条，
    /// 这里分批在 Background 优先级追加行，避免一次性创建上千控件造成掉帧；
    /// 新一次重建会让上一次未完成的分批任务自动作废。
    /// </summary>
    private void RebuildHolidayTable()
    {
        var version = ++_holidayTableBuildVersion;

        var entries = new List<(HolidayDay Day, bool IsMakeup)>();
        foreach (var day in _calendar.Data.MakeupDays.OrderBy(x => x.Date))
        {
            entries.Add((day, true));
        }
        foreach (var day in _calendar.Data.Holidays.OrderBy(x => x.Date))
        {
            entries.Add((day, false));
        }

        var (table, grid) = CreateEmptyTable(
            new[] { "日期", "名称", "类型", "来源", "操作" },
            new double[] { 200, 160, 90, 70, 80 });

        HolidayListHost.Children.Clear();
        HolidayListHost.Children.Add(table);

        var index = 0;
        const int batchSize = 40;

        void AppendBatch()
        {
            if (version != _holidayTableBuildVersion)
            {
                return;
            }

            var end = Math.Min(index + batchSize, entries.Count);
            for (; index < end; index++)
            {
                var (day, isMakeup) = entries[index];
                AppendTableRow(grid, CreateHolidayRow(day, isMakeup), 5);
            }

            if (index < entries.Count)
            {
                Dispatcher.UIThread.Post(AppendBatch, DispatcherPriority.Background);
                return;
            }

            HolidayExpander.RefreshHost();
        }

        AppendBatch();
    }

    /// <summary>创建节假日表格的一行（类型列按调休补班 / 放假日区分）。</summary>
    private Control[] CreateHolidayRow(HolidayDay day, bool isMakeup)
    {
        return new Control[]
        {
            CreateCellText($"{day.Date:yyyy-MM-dd}（{GetDayOfWeekText(day.Date)}）"),
            CreateCellText(day.Name),
            CreateCellText(isMakeup ? "调休补班" : "放假日"),
            CreateSourceText(day.Source),
            CreateDeleteButton(() =>
            {
                (isMakeup ? _calendar.Data.MakeupDays : _calendar.Data.Holidays).Remove(day);
                _calendar.Save();
                RebuildHolidayTable();
                RefreshStatistics();
            })
        };
    }

    /// <summary>
    /// 按当前分段方式重绘「分段统计」表格：数据取自最近一次统计结果（<see cref="_lastStats"/>），
    /// 故切换分段方式（按周/按月）无需重算整学期。
    /// </summary>
    private void RebuildPeriodTable()
    {
        if (_lastStats != null)
        {
            ApplyPeriodTable(_lastStats);
            return;
        }

        RefreshStatistics();
    }

    /// <summary>「每日在校时长」取值方式的展示文案。</summary>
    private string DailyHoursSourceText() =>
        Config.DailyHoursSource == DailyHoursSource.Auto ? "自动获取当天档案课表" : "手动指定";

    private void SetSummary(string text, string detail, double progress)
    {
        var changed = SummaryTextBlock.Text != text
                      || SummaryDetailTextBlock.Text != detail
                      || Math.Abs(SummaryProgressBar.Value - progress) > 0.01;
        SummaryTextBlock.Text = text;
        SummaryDetailTextBlock.Text = detail;
        SummaryProgressBar.Value = progress;
        if (changed)
        {
            SummaryExpander.RefreshHost();
        }
    }

    private void SetStatus(string text)
    {
        StatusTextBlock.Text = text;
    }

    // ==================== 事件处理 ====================

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => RefreshStatistics(forceRefresh: true);

    private void OnPeriodModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls || _isInitializing)
        {
            return;
        }

        RebuildPeriodTable();
    }

    private void OnSemesterOptionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls || _isInitializing)
        {
            return;
        }

        Config.StartSource = StartSourceComboBox.SelectedIndex == 1
            ? SemesterStartSource.Manual
            : SemesterStartSource.ClassIsland;
        Config.EndSource = EndSourceComboBox.SelectedIndex == 1
            ? SemesterEndSource.ManualDate
            : SemesterEndSource.TotalWeeks;

        UpdateSemesterOptionEnabledState();
        RefreshStatistics();
    }

    private void OnDailyHoursSourceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls || _isInitializing)
        {
            return;
        }

        Config.DailyHoursSource = DailyHoursSourceComboBox.SelectedIndex == 0
            ? DailyHoursSource.Auto
            : DailyHoursSource.Manual;

        UpdateSemesterOptionEnabledState();
        RefreshStatistics();
    }

    private void OnSemesterDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        if (_isUpdatingControls || _isInitializing)
        {
            return;
        }

        if (ManualStartDatePicker.SelectedDate != null)
        {
            Config.ManualStartDate = ManualStartDatePicker.SelectedDate.Value.Date;
        }
        if (ManualEndDatePicker.SelectedDate != null)
        {
            Config.ManualEndDate = ManualEndDatePicker.SelectedDate.Value.Date;
        }

        RefreshStatistics();
    }

    private void OnSemesterNumberChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdatingControls || _isInitializing)
        {
            return;
        }

        if (TotalWeeksNumericUpDown.Value != null)
        {
            Config.TotalWeeks = (int)TotalWeeksNumericUpDown.Value.Value;
        }
        if (DailyHoursNumericUpDown.Value != null)
        {
            Config.DailyHours = (double)DailyHoursNumericUpDown.Value.Value;
        }

        RefreshStatistics();
    }

    private void OnRuleChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls || _isInitializing)
        {
            return;
        }

        Config.ExcludeSaturday = ExcludeSaturdayCheckBox.IsChecked ?? false;
        Config.ExcludeSunday = ExcludeSundayCheckBox.IsChecked ?? false;
        Config.ExcludeHolidays = ExcludeHolidaysCheckBox.IsChecked ?? false;
        Config.CountMakeupDays = CountMakeupDaysCheckBox.IsChecked ?? false;
        Config.ExcludeVacations = ExcludeVacationsCheckBox.IsChecked ?? false;
        Config.RespectCustomDates = RespectCustomDatesCheckBox.IsChecked ?? false;

        RefreshStatistics();
    }

    private void OnAddVacationClick(object? sender, RoutedEventArgs e)
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
        RefreshStatistics();
    }

    private void OnAddCustomDateClick(object? sender, RoutedEventArgs e)
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
        RefreshStatistics();
    }

    private void OnAddHolidayClick(object? sender, RoutedEventArgs e)
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
        RefreshStatistics();
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
            RefreshStatistics();
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
            RefreshStatistics();
        }
        catch (Exception ex)
        {
            SetStatus($"恢复失败：{ex.Message}");
        }
    }

    // ==================== 主题与生命周期 ====================

    /// <summary>按当前主题刷新缓存的画刷（切换主题或首次加载时调用）。</summary>
    private void UpdateThemeBrushes()
    {
        _textBrush = ThemeHelper.GetTextBrush();
        _subTextBrush = ThemeHelper.GetSubTextBrush();
        _separatorBrush = ThemeHelper.GetSeparatorBrush();
    }

    /// <summary>
    /// 全量应用主题。只在页面加载与主题切换时调用：
    /// 表格重建时新控件已用缓存画刷着色，避免每次重建都遍历整棵可视树造成卡顿。
    /// </summary>
    private void ApplyTheme()
    {
        UpdateThemeBrushes();

        if (Content is not Control root)
        {
            return;
        }

        foreach (var descendant in root.GetVisualDescendants())
        {
            switch (descendant)
            {
                case TextBlock textBlock:
                    textBlock.Foreground = textBlock.Tag as string == "sub" ? _subTextBrush : _textBrush;
                    break;
                case CheckBox checkBox:
                    checkBox.Foreground = _textBrush;
                    break;
                case Border border when border.Tag as string is "table-border" or "table-cell":
                    border.BorderBrush = _separatorBrush;
                    break;
            }
        }

        if (Application.Current?.Styles.TryGetResource("AccentFillColorDefaultBrush",
                Application.Current.ActualThemeVariant, out var accentBrush) == true &&
            accentBrush is IBrush brush)
        {
            SummaryProgressBar.Foreground = brush;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => ApplyTheme();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        // 入场动画放在最前：避免后续初始化逻辑异常时中断动画播放
        if (!_introPlayed)
        {
            _introPlayed = true;
            IntroAnimationHelper.PlayIntro(RootPanel);
        }

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        // 后台统计完成时回填界面（服务已切回 UI 线程）
        AttendanceCalendarService.StatisticsUpdated -= OnStatisticsUpdated;
        AttendanceCalendarService.StatisticsUpdated += OnStatisticsUpdated;

        ApplyTheme();

        // 【不阻塞 UI】先让页面完成首帧渲染，再做数据相关的建表工作：
        //   否则"打开设置页"时会先同步建完所有表格才画出界面，用户看到的是整页卡住。
        await Task.Yield();
        RefreshAll();
        // 控件初值回填完成后解除初始化保护，此后只有用户真实操作才会写入配置。
        _isInitializing = false;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }

        AttendanceCalendarService.StatisticsUpdated -= OnStatisticsUpdated;

        // 页面离开时作废未完成的分批建表任务，避免无意义地继续创建控件。
        _holidayTableBuildVersion++;
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
