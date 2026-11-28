using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 时间格式转换页面
/// 支持北京时间/时间戳/农历/区时/地方时互转
/// </summary>
public partial class TimeConverterPage : UserControl
{
    // 提示文本定时器（用于自动清除提示）
    private readonly Dictionary<TextBlock, System.Timers.Timer> _resultTimers = new();

    // 插件设置（用于获取经度显示方式）
    private readonly PluginSettings? _settings;

    // 天干列表
    private readonly string[] _tiangan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };

    // 地支列表
    private readonly string[] _dizhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    // 时区列表
    private readonly Dictionary<string, double> _timeZones = new()
    {
        { "(UTC-12:00) 贝克岛", -12.0 },
        { "(UTC-11:00) 美属萨摩亚", -11.0 },
        { "(UTC-10:00) 夏威夷", -10.0 },
        { "(UTC-09:30) 马克萨斯群岛", -9.5 },
        { "(UTC-09:00) 阿拉斯加", -9.0 },
        { "(UTC-08:00) 太平洋时间", -8.0 },
        { "(UTC-07:00) 山地时间", -7.0 },
        { "(UTC-06:00) 中部时间", -6.0 },
        { "(UTC-05:00) 东部时间", -5.0 },
        { "(UTC-04:00) 大西洋时间", -4.0 },
        { "(UTC-03:30) 纽芬兰", -3.5 },
        { "(UTC-03:00) 巴西利亚", -3.0 },
        { "(UTC-02:00) 南乔治亚岛", -2.0 },
        { "(UTC-01:00) 亚速尔群岛", -1.0 },
        { "(UTC±00:00) 伦敦", 0.0 },
        { "(UTC+01:00) 巴黎", 1.0 },
        { "(UTC+02:00) 开罗", 2.0 },
        { "(UTC+03:00) 莫斯科", 3.0 },
        { "(UTC+03:30) 德黑兰", 3.5 },
        { "(UTC+04:00) 迪拜", 4.0 },
        { "(UTC+04:30) 喀布尔", 4.5 },
        { "(UTC+05:00) 伊斯兰堡", 5.0 },
        { "(UTC+05:30) 孟买", 5.5 },
        { "(UTC+05:45) 加德满都", 5.75 },
        { "(UTC+06:00) 达卡", 6.0 },
        { "(UTC+06:30) 仰光", 6.5 },
        { "(UTC+07:00) 曼谷", 7.0 },
        { "(UTC+08:00) 北京", 8.0 },
        { "(UTC+08:30) 科科斯群岛", 8.5 },
        { "(UTC+08:45) 尤克拉", 8.75 },
        { "(UTC+09:00) 东京", 9.0 },
        { "(UTC+09:30) 达尔文", 9.5 },
        { "(UTC+10:00) 悉尼", 10.0 },
        { "(UTC+10:30) 豪勋爵岛", 10.5 },
        { "(UTC+11:00) 新喀里多尼亚", 11.0 },
        { "(UTC+11:30) 诺福克岛", 11.5 },
        { "(UTC+12:00) 奥克兰", 12.0 },
        { "(UTC+12:45) 查塔姆群岛", 12.75 },
        { "(UTC+13:00) 斐济", 13.0 },
        { "(UTC+14:00) 基里巴斯", 14.0 }
    };

    public TimeConverterPage() : this(null)
    {
    }

    public TimeConverterPage(PluginSettings? settings)
    {
        _settings = settings ?? Plugin.Instance?.Settings;
        if (_settings != null)
        {
            _settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        InitializeComponent();
        InitializeComboBoxes();
        InitializeResultTimers();
        UpdateLongitudeDisplay();
    }

    /// <summary>
    /// 初始化所有 ComboBox 的可选项
    /// </summary>
    private void InitializeComboBoxes()
    {
        // 北京时间模块的月份和日期
        for (int i = 1; i <= 12; i++) BeijingMonthComboBox.Items.Add($"{i}月");
        for (int i = 1; i <= 31; i++) BeijingDayComboBox.Items.Add($"{i}日");
        BeijingMonthComboBox.SelectedIndex = -1;
        BeijingDayComboBox.SelectedIndex = -1;
        BeijingYearTextBox.Value = DateTime.Now.Year;

        // 农历模块
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            LunarYearRangeComboBox.Items.Add(range);
        }
        LunarYearRangeComboBox.SelectedItem = "1984-2043"; // 默认1984-2043

        foreach (var tg in _tiangan) LunarTianganComboBox.Items.Add(tg);
        foreach (var dz in _dizhi) LunarDizhiComboBox.Items.Add(dz);

        for (int i = 1; i <= 12; i++)
        {
            LunarMonthComboBox.Items.Add($"{i}月");
            LunarMonthComboBox.Items.Add($"闰{i}月");
        }

        for (int i = 1; i <= 30; i++)
        {
            LunarDayComboBox.Items.Add($"{i}日");
        }

        // 区时模块
        for (int i = 1; i <= 12; i++) ZoneMonthComboBox.Items.Add($"{i}月");
        for (int i = 1; i <= 31; i++) ZoneDayComboBox.Items.Add($"{i}日");
        ZoneMonthComboBox.SelectedIndex = -1;
        ZoneDayComboBox.SelectedIndex = -1;
        ZoneYearTextBox.Value = DateTime.Now.Year;

        foreach (var zone in _timeZones.Keys)
        {
            ZoneComboBox.Items.Add(zone);
        }
        ZoneComboBox.SelectedIndex = 15; // 默认选中中时区(UTC±00:00)

        // 地方时模块
        for (int i = 1; i <= 12; i++) LocalMonthComboBox.Items.Add($"{i}月");
        for (int i = 1; i <= 31; i++) LocalDayComboBox.Items.Add($"{i}日");
        LocalMonthComboBox.SelectedIndex = -1;
        LocalDayComboBox.SelectedIndex = -1;
        LocalYearTextBox.Value = DateTime.Now.Year;
    }

    /// <summary>
    /// 为所有结果 TextBlock 创建自动清除定时器
    /// </summary>
    private void InitializeResultTimers()
    {
        RegisterResultTimer(BeijingResultTextBlock);
        RegisterResultTimer(UnixResultTextBlock);
        RegisterResultTimer(LunarResultTextBlock);
        RegisterResultTimer(ZoneResultTextBlock);
        RegisterResultTimer(LocalResultTextBlock);
    }

    private void RegisterResultTimer(TextBlock textBlock)
    {
        var timer = new System.Timers.Timer(5000); // 5秒后自动清除
        timer.Elapsed += (s, e) =>
        {
            UIThread.Post(() =>
            {
                textBlock.Text = "";
            });
            timer.Stop();
        };
        timer.AutoReset = false;
        _resultTimers[textBlock] = timer;
    }

    private void SetResultText(TextBlock? textBlock, string message)
    {
        if (textBlock == null) return;

        // 先停止之前的定时器
        if (_resultTimers.TryGetValue(textBlock, out var existingTimer))
        {
            existingTimer.Stop();
        }

        textBlock.Text = message;

        // 启动新的定时器，5秒后清除提示
        if (_resultTimers.TryGetValue(textBlock, out var timer))
        {
            timer.Start();
        }
    }

    #region 事件处理

    private void OnClearAllClick(object? sender, RoutedEventArgs e)
    {
        // 清除所有输入框
        BeijingYearTextBox.Value = null;
        BeijingMonthComboBox.SelectedIndex = -1;
        BeijingDayComboBox.SelectedIndex = -1;
        BeijingTimePicker.SelectedTime = null;
        BeijingResultTextBlock.Text = "";

        UnixInputTextBox.Value = null;
        UnixResultTextBlock.Text = "";

        LunarYearRangeComboBox.SelectedIndex = -1;
        LunarTianganComboBox.SelectedIndex = -1;
        LunarDizhiComboBox.SelectedIndex = -1;
        LunarMonthComboBox.SelectedIndex = -1;
        LunarDayComboBox.SelectedIndex = -1;
        LunarTimePicker.SelectedTime = null;
        LunarResultTextBlock.Text = "";

        ZoneYearTextBox.Value = null;
        ZoneMonthComboBox.SelectedIndex = -1;
        ZoneDayComboBox.SelectedIndex = -1;
        ZoneTimePicker.SelectedTime = null;
        ZoneComboBox.SelectedIndex = -1;
        ZoneResultTextBlock.Text = "";

        LocalYearTextBox.Value = null;
        LocalMonthComboBox.SelectedIndex = -1;
        LocalDayComboBox.SelectedIndex = -1;
        LocalTimePicker.SelectedTime = null;
        LocalLongitudeTextBox.Value = null;
        LocalResultTextBlock.Text = "";
    }

    /// <summary>
    /// 北京时间日期变更：联动更新日期 ComboBox 的可选项
    /// </summary>
    private void OnBeijingValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        UpdateDayComboBox(BeijingYearTextBox, BeijingMonthComboBox, BeijingDayComboBox);
    }

    private void OnBeijingSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateDayComboBox(BeijingYearTextBox, BeijingMonthComboBox, BeijingDayComboBox);
    }

    /// <summary>
    /// 区时日期变更：联动更新日期 ComboBox 的可选项
    /// </summary>
    private void OnZoneValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        UpdateDayComboBox(ZoneYearTextBox, ZoneMonthComboBox, ZoneDayComboBox);
    }

    private void OnZoneSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateDayComboBox(ZoneYearTextBox, ZoneMonthComboBox, ZoneDayComboBox);
    }

    /// <summary>
    /// 地方时日期变更：联动更新日期 ComboBox 的可选项
    /// </summary>
    private void OnLocalValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        UpdateDayComboBox(LocalYearTextBox, LocalMonthComboBox, LocalDayComboBox);
    }

    private void OnLocalSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateDayComboBox(LocalYearTextBox, LocalMonthComboBox, LocalDayComboBox);
    }

    /// <summary>
    /// 经度十进制输入变更：同步 DMS 控件
    /// </summary>
    private void OnLongitudeTextBoxChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is WpfNumericUpDown numericUpDown)
        {
            if (numericUpDown.Value == null)
            {
                numericUpDown.Value = 120;
                return;
            }

            var value = (double)numericUpDown.Value.Value;
            value = Math.Round(value, 4);
            if (value < -180) value = -180;
            if (value > 180) value = 180;
            numericUpDown.Value = (decimal)value;
        }
    }

    /// <summary>
    /// 经度 DMS 输入变更：同步十进制值
    /// </summary>
    private void OnLongitudeDmsValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        SyncLongitudeFromDms();
    }

    private void OnLongitudeDmsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SyncLongitudeFromDms();
    }

    private void SyncLongitudeFromDms()
    {
        var d = (int)(LocalLongitudeDmsDegreesTextBox?.Value ?? 0);
        var m = (int)(LocalLongitudeDmsMinutesTextBox?.Value ?? 0);
        var s = (double)(LocalLongitudeDmsSecondsTextBox?.Value ?? 0);
        var isEast = LocalLongitudeDmsDirectionComboBox?.SelectedIndex == 0;
        if (LongitudeConverter.TryParseDms(d, m, s, isEast, out var lon))
        {
            LocalLongitudeTextBox.Value = (decimal)lon;
        }
    }

    private void OnBeijingCurrentTimeClick(object? sender, RoutedEventArgs e)
    {
        var now = Plugin.GetCurrentTime();
        BeijingYearTextBox.Value = now.Year;
        BeijingMonthComboBox.SelectedItem = $"{now.Month}月";
        BeijingDayComboBox.SelectedItem = $"{now.Day}日";
        BeijingTimePicker.SelectedTime = new TimeSpan(now.Hour, now.Minute, now.Second);
    }

    private void OnBeijingToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            SetResultText(BeijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }

        if (UnixTimeHelper.IsNonExistentDate1582October(dt))
        {
            SetResultText(BeijingResultTextBlock, "1582年10月5日至14日在历史上不存在，无法转换");
            return;
        }

        try
        {
            var timestamp = BigIntegerUnixTimeHelper.ToUnixTimestampBigInteger(dt);
            if (timestamp <= long.MaxValue && timestamp >= long.MinValue)
            {
                UnixInputTextBox.Value = (decimal)(long)timestamp / 1000m;
            }
            else
            {
                UnixInputTextBox.Value = null;
            }
        }
        catch (ArgumentException ex)
        {
            SetResultText(BeijingResultTextBlock, ex.Message);
        }
    }

    private void OnBeijingToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            SetResultText(BeijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }
        if (!LunarCalendarHelper.IsDateSupported(dt))
        {
            SetResultText(BeijingResultTextBlock, "农历不支持此日期范围(1901-02-19 ~ 2101-01-28)");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(dt);
        SetResultText(BeijingResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(dt);
    }

    private void FillLunarComboBoxes(DateTime dt)
    {
        var lunarYear = LunarCalendarHelper.GetLunarYear(dt);
        var lunarMonth = LunarCalendarHelper.GetLunarMonth(dt);
        var lunarDay = LunarCalendarHelper.GetLunarDay(dt);
        var isLeapMonth = LunarCalendarHelper.IsLeapMonth(dt);

        // 如果农历年份为0，说明超出支持范围
        if (lunarYear == 0)
        {
            SetResultText(LunarResultTextBlock, "转换结果超出有效范围(1901-02-19 ~ 2101-01-28)");
            return;
        }

        var tiangan = LunarCalendarHelper.GetTiangan(lunarYear);
        var dizhi = LunarCalendarHelper.GetDizhi(lunarYear);

        // 设置年份范围
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
            {
                if (lunarYear >= startYear && lunarYear <= endYear)
                {
                    LunarYearRangeComboBox.SelectedItem = range;
                    break;
                }
            }
        }

        // 设置天干地支
        LunarTianganComboBox.SelectedItem = tiangan;
        LunarDizhiComboBox.SelectedItem = dizhi;

        // 设置月份
        var monthText = isLeapMonth ? $"闰{lunarMonth}月" : $"{lunarMonth}月";
        LunarMonthComboBox.SelectedItem = monthText;

        // 设置日期（自动修正非法日期）
        var safeDay = ValidateAndFixDay(dt.Year, dt.Month, lunarDay);
        LunarDayComboBox.SelectedItem = $"{safeDay}日";

        // 设置时间
        LunarTimePicker.SelectedTime = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
    }

    private void OnBeijingToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            SetResultText(BeijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }
        if (ZoneComboBox.SelectedItem == null)
        {
            SetResultText(BeijingResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = ZoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var dstOffset = (ZoneDstCheckBox.IsChecked == true) ? 1 : 0;
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(offset - 8 + dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(BeijingResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        ZoneYearTextBox.Value = zoneTime.Year;
        ZoneMonthComboBox.SelectedItem = $"{zoneTime.Month}月";
        ZoneDayComboBox.SelectedItem = $"{safeDay}日";
        ZoneTimePicker.SelectedTime = new TimeSpan(zoneTime.Hour, zoneTime.Minute, zoneTime.Second);
    }

    private void OnBeijingToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseBeijingDateTime(out var dt))
        {
            SetResultText(BeijingResultTextBlock, "请输入有效的日期和时间");
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            SetResultText(BeijingResultTextBlock, "请输入有效的经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var dstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(offsetSeconds + dstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(BeijingResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        LocalYearTextBox.Value = localTime.Year;
        LocalMonthComboBox.SelectedItem = $"{localTime.Month}月";
        LocalDayComboBox.SelectedItem = $"{safeDay}日";
        LocalTimePicker.SelectedTime = new TimeSpan(localTime.Hour, localTime.Minute, localTime.Second);
    }

    private void OnUnixCopyClick(object? sender, RoutedEventArgs e)
    {
        if (UnixInputTextBox?.Value != null)
        {
            try
            {
                // 复制到剪贴板 - 显示秒级时间戳（精确到0.001）
                var text = UnixInputTextBox.Value.Value.ToString("0.###");
                System.Windows.Clipboard.SetText(text);
            }
            catch
            {
                // 忽略错误
            }
        }
    }

    private void OnUnixToBeijing(object? sender, RoutedEventArgs e)
    {
        if (UnixInputTextBox?.Value == null)
        {
            SetResultText(UnixResultTextBlock, "请输入有效的时间戳");
            return;
        }
        var timestamp = (BigInteger)Math.Round(UnixInputTextBox.Value.Value * 1000m);
        DateTime dt;
        try
        {
            dt = BigIntegerUnixTimeHelper.FromUnixTimestampBigInteger(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(UnixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(dt.Year, dt.Month, dt.Day);
        BeijingYearTextBox.Value = dt.Year;
        BeijingMonthComboBox.SelectedItem = $"{dt.Month}月";
        BeijingDayComboBox.SelectedItem = $"{safeDay}日";
        BeijingTimePicker.SelectedTime = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
    }

    private void OnUnixToLunar(object? sender, RoutedEventArgs e)
    {
        if (UnixInputTextBox?.Value == null)
        {
            SetResultText(UnixResultTextBlock, "请输入有效的时间戳");
            return;
        }
        var timestamp = (BigInteger)Math.Round(UnixInputTextBox.Value.Value * 1000m);
        DateTime dt;
        try
        {
            dt = BigIntegerUnixTimeHelper.FromUnixTimestampBigInteger(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(UnixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(dt);
        SetResultText(UnixResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(dt);
    }

    private void OnUnixToZone(object? sender, RoutedEventArgs e)
    {
        if (UnixInputTextBox?.Value == null)
        {
            SetResultText(UnixResultTextBlock, "请输入有效的时间戳");
            return;
        }
        var timestamp = (BigInteger)Math.Round(UnixInputTextBox.Value.Value * 1000m);
        if (ZoneComboBox.SelectedItem == null)
        {
            SetResultText(UnixResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = ZoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var dstOffset = (ZoneDstCheckBox.IsChecked == true) ? 1 : 0;
        DateTime dt;
        try
        {
            dt = BigIntegerUnixTimeHelper.FromUnixTimestampUtcBigInteger(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(UnixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(offset + dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(UnixResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        ZoneYearTextBox.Value = zoneTime.Year;
        ZoneMonthComboBox.SelectedItem = $"{zoneTime.Month}月";
        ZoneDayComboBox.SelectedItem = $"{safeDay}日";
        ZoneTimePicker.SelectedTime = new TimeSpan(zoneTime.Hour, zoneTime.Minute, zoneTime.Second);
    }

    private void OnUnixToLocal(object? sender, RoutedEventArgs e)
    {
        if (UnixInputTextBox?.Value == null)
        {
            SetResultText(UnixResultTextBlock, "请输入有效的时间戳");
            return;
        }
        var timestamp = (BigInteger)Math.Round(UnixInputTextBox.Value.Value * 1000m);
        if (!TryParseLongitude(out var longitude))
        {
            SetResultText(UnixResultTextBlock, "请输入有效的经度");
            return;
        }
        DateTime dt;
        try
        {
            dt = BigIntegerUnixTimeHelper.FromUnixTimestampBigInteger(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(UnixResultTextBlock, "时间戳超出表示范围");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(offsetSeconds + localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(UnixResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        LocalYearTextBox.Value = localTime.Year;
        LocalMonthComboBox.SelectedItem = $"{localTime.Month}月";
        LocalDayComboBox.SelectedItem = $"{safeDay}日";
        LocalTimePicker.SelectedTime = new TimeSpan(localTime.Hour, localTime.Minute, localTime.Second);
    }

    private void OnLunarToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(LunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }
        var safeDay = ValidateAndFixDay(dt.Year, dt.Month, dt.Day);
        BeijingYearTextBox.Value = dt.Year;
        BeijingMonthComboBox.SelectedItem = $"{dt.Month}月";
        BeijingDayComboBox.SelectedItem = $"{safeDay}日";
        BeijingTimePicker.SelectedTime = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
    }

    private void OnLunarToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(LunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }

        if (UnixTimeHelper.IsNonExistentDate1582October(dt))
        {
            SetResultText(LunarResultTextBlock, "1582年10月5日至14日在历史上不存在，无法转换");
            return;
        }

        try
        {
            var timestamp = BigIntegerUnixTimeHelper.ToUnixTimestampBigInteger(dt);
            if (timestamp <= long.MaxValue && timestamp >= long.MinValue)
            {
                UnixInputTextBox.Value = (decimal)(long)timestamp / 1000m;
            }
            else
            {
                UnixInputTextBox.Value = null;
            }
        }
        catch (ArgumentException ex)
        {
            SetResultText(LunarResultTextBlock, ex.Message);
        }
    }

    private void OnLunarToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(LunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }
        if (ZoneComboBox.SelectedItem == null)
        {
            SetResultText(LunarResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = ZoneComboBox.SelectedItem.ToString();
        var offset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var dstOffset = (ZoneDstCheckBox.IsChecked == true) ? 1 : 0;
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(offset - 8 + dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(LunarResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        ZoneYearTextBox.Value = zoneTime.Year;
        ZoneMonthComboBox.SelectedItem = $"{zoneTime.Month}月";
        ZoneDayComboBox.SelectedItem = $"{safeDay}日";
        ZoneTimePicker.SelectedTime = new TimeSpan(zoneTime.Hour, zoneTime.Minute, zoneTime.Second);
    }

    private void OnLunarToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLunarDateTime(out var dt))
        {
            SetResultText(LunarResultTextBlock, "请输入有效的农历日期和时间或超出转换范围");
            return;
        }
        if (!TryParseLongitude(out var longitude))
        {
            SetResultText(LunarResultTextBlock, "请输入有效的经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var dstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(offsetSeconds + dstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(LunarResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        LocalYearTextBox.Value = localTime.Year;
        LocalMonthComboBox.SelectedItem = $"{localTime.Month}月";
        LocalDayComboBox.SelectedItem = $"{safeDay}日";
        LocalTimePicker.SelectedTime = new TimeSpan(localTime.Hour, localTime.Minute, localTime.Second);
    }

    private void OnZoneToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(ZoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        // 验证结果年份在有效范围内
        var dstOffset = (ZoneDstCheckBox.IsChecked == true) ? 1 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(8 - offset - dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(ZoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        if (beijingTime.Year < 1 || beijingTime.Year > 9999)
        {
            SetResultText(ZoneResultTextBlock, "转换结果超出有效范围(1-9999年)");
            return;
        }
        var safeDay = ValidateAndFixDay(beijingTime.Year, beijingTime.Month, beijingTime.Day);
        BeijingYearTextBox.Value = beijingTime.Year;
        BeijingMonthComboBox.SelectedItem = $"{beijingTime.Month}月";
        BeijingDayComboBox.SelectedItem = $"{safeDay}日";
        BeijingTimePicker.SelectedTime = new TimeSpan(beijingTime.Hour, beijingTime.Minute, beijingTime.Second);
    }

    private void OnZoneToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(ZoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        var dstOffset = (ZoneDstCheckBox.IsChecked == true) ? 1 : 0;
        DateTime utcTime;
        try
        {
            utcTime = DateTime.SpecifyKind(LunarHelper.SolarAddHours(dt, -offset - dstOffset), DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(ZoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        if (UnixTimeHelper.IsNonExistentDate1582October(utcTime))
        {
            SetResultText(ZoneResultTextBlock, "1582年10月5日至14日在历史上不存在，无法转换");
            return;
        }

        try
        {
            var timestamp = BigIntegerUnixTimeHelper.ToUnixTimestampUtcBigInteger(utcTime);
            if (timestamp <= long.MaxValue && timestamp >= long.MinValue)
            {
                UnixInputTextBox.Value = (decimal)(long)timestamp / 1000m;
            }
            else
            {
                UnixInputTextBox.Value = null;
            }
        }
        catch (ArgumentException ex)
        {
            SetResultText(ZoneResultTextBlock, ex.Message);
        }
    }

    private void OnZoneToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(ZoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        var dstOffset = (ZoneDstCheckBox.IsChecked == true) ? 1 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(8 - offset - dstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(ZoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(beijingTime);
        SetResultText(ZoneResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(beijingTime);
    }

    private void OnZoneToLocal(object? sender, RoutedEventArgs e)
    {
        if (!TryParseZoneDateTime(out var dt, out var offset))
        {
            SetResultText(ZoneResultTextBlock, "请输入有效的日期、时间和时区");
            return;
        }
        if (!TryParseLongitude())
        {
            SetResultText(ZoneResultTextBlock, "请输入有效的经度");
            return;
        }
        var zoneDstOffset = (ZoneDstCheckBox.IsChecked == true) ? 1 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromHours(8 - offset - zoneDstOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(ZoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var longitude = (double)(LocalLongitudeTextBox.Value ?? 0);
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        DateTime localTime;
        try
        {
            localTime = DateValidationHelper.AdjustDateAfterAddition(beijingTime, TimeSpan.FromSeconds(offsetSeconds + localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(ZoneResultTextBlock, "转换结果超出表示范围");
            return;
        }
        // 验证结果年份在有效范围内
        if (localTime.Year < 1 || localTime.Year > 9999)
        {
            SetResultText(ZoneResultTextBlock, "转换结果超出有效范围(1-9999年)");
            return;
        }
        var safeDay = ValidateAndFixDay(localTime.Year, localTime.Month, localTime.Day);
        LocalYearTextBox.Value = localTime.Year;
        LocalMonthComboBox.SelectedItem = $"{localTime.Month}月";
        LocalDayComboBox.SelectedItem = $"{safeDay}日";
        LocalTimePicker.SelectedTime = new TimeSpan(localTime.Hour, localTime.Minute, localTime.Second);
    }

    private void OnLocalToBeijing(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(LocalResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(-offsetSeconds - localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(LocalResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(beijingTime.Year, beijingTime.Month, beijingTime.Day);
        BeijingYearTextBox.Value = beijingTime.Year;
        BeijingMonthComboBox.SelectedItem = $"{beijingTime.Month}月";
        BeijingDayComboBox.SelectedItem = $"{safeDay}日";
        BeijingTimePicker.SelectedTime = new TimeSpan(beijingTime.Hour, beijingTime.Minute, beijingTime.Second);
    }

    private void OnLocalToUnix(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(LocalResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(-offsetSeconds - localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(LocalResultTextBlock, "转换结果超出表示范围");
            return;
        }
        if (UnixTimeHelper.IsNonExistentDate1582October(beijingTime))
        {
            SetResultText(LocalResultTextBlock, "1582年10月5日至14日在历史上不存在，无法转换");
            return;
        }

        try
        {
            var timestamp = BigIntegerUnixTimeHelper.ToUnixTimestampBigInteger(beijingTime);
            if (timestamp <= long.MaxValue && timestamp >= long.MinValue)
            {
                UnixInputTextBox.Value = (decimal)(long)timestamp / 1000m;
            }
            else
            {
                UnixInputTextBox.Value = null;
            }
        }
        catch (ArgumentException ex)
        {
            SetResultText(LocalResultTextBlock, ex.Message);
        }
    }

    private void OnLocalToLunar(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(LocalResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        var offsetSeconds = (longitude - 120) * 240;
        var localDstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        DateTime beijingTime;
        try
        {
            beijingTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(-offsetSeconds - localDstOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(LocalResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var lunar = LunarCalendarHelper.SolarToLunar(beijingTime);
        SetResultText(LocalResultTextBlock, $"农历: {lunar}");
        FillLunarComboBoxes(beijingTime);
    }

    private void OnLocalToZone(object? sender, RoutedEventArgs e)
    {
        if (!TryParseLocalDateTime(out var dt, out var longitude))
        {
            SetResultText(LocalResultTextBlock, "请输入有效的日期、时间和经度");
            return;
        }
        if (ZoneComboBox.SelectedItem == null)
        {
            SetResultText(LocalResultTextBlock, "请先选择时区");
            return;
        }
        var zoneName = ZoneComboBox.SelectedItem.ToString();
        var zoneOffset = _timeZones.GetValueOrDefault(zoneName ?? "(UTC±00:00) 伦敦", 0);
        var offsetSeconds = (longitude - 120) * 240;
        var dstOffsetSeconds = (LocalDstCheckBox.IsChecked == true) ? 3600 : 0;
        var totalOffsetSeconds = (zoneOffset - 8) * 3600 + dstOffsetSeconds - offsetSeconds;
        DateTime zoneTime;
        try
        {
            zoneTime = DateValidationHelper.AdjustDateAfterAddition(dt, TimeSpan.FromSeconds(totalOffsetSeconds));
        }
        catch (ArgumentOutOfRangeException)
        {
            SetResultText(LocalResultTextBlock, "转换结果超出表示范围");
            return;
        }
        var safeDay = ValidateAndFixDay(zoneTime.Year, zoneTime.Month, zoneTime.Day);
        ZoneYearTextBox.Value = zoneTime.Year;
        ZoneMonthComboBox.SelectedItem = $"{zoneTime.Month}月";
        ZoneDayComboBox.SelectedItem = $"{safeDay}日";
        ZoneTimePicker.SelectedTime = new TimeSpan(zoneTime.Hour, zoneTime.Minute, zoneTime.Second);
    }

    #endregion

    #region 辅助方法

    private bool TryParseBeijingDateTime(out DateTime result)
    {
        result = DateTime.MinValue;

        if (BeijingYearTextBox?.Value == null ||
            BeijingMonthComboBox?.SelectedItem == null ||
            BeijingDayComboBox?.SelectedItem == null ||
            BeijingTimePicker?.SelectedTime == null)
            return false;

        var year = (int)BeijingYearTextBox.Value.Value;
        if (!int.TryParse(BeijingMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return false;
        if (!int.TryParse(BeijingDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day))
            return false;

        var time = BeijingTimePicker.SelectedTime.Value;
        var hour = time.Hours;
        var minute = time.Minutes;
        var second = time.Seconds;

        // 校验日期是否合法
        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;
        if (day > Lunar.Util.SolarUtil.GetDaysOfMonth(year, month))
            return false;
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(year, month, day))
            return false;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        try
        {
            if (year >= 1583)
            {
                result = new DateTime(year, month, day, hour, minute, second);
            }
            else
            {
                var solar = Lunar.Solar.FromYmdHms(year, month, day, hour, minute, second);
                result = new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
            }
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private bool TryParseZoneDateTime(out DateTime result, out double offset)
    {
        result = DateTime.MinValue;
        offset = 0;

        if (ZoneYearTextBox?.Value == null ||
            ZoneMonthComboBox?.SelectedItem == null ||
            ZoneDayComboBox?.SelectedItem == null ||
            ZoneTimePicker?.SelectedTime == null)
            return false;

        var year = (int)ZoneYearTextBox.Value.Value;
        if (!int.TryParse(ZoneMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return false;
        if (!int.TryParse(ZoneDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day))
            return false;

        var time = ZoneTimePicker.SelectedTime.Value;
        var hour = time.Hours;
        var minute = time.Minutes;
        var second = time.Seconds;

        var zoneName = ZoneComboBox?.SelectedItem?.ToString() ?? "(UTC±00:00) 伦敦";

        offset = _timeZones.GetValueOrDefault(zoneName, 0);

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;
        if (day > Lunar.Util.SolarUtil.GetDaysOfMonth(year, month))
            return false;
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(year, month, day))
            return false;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        try
        {
            if (year >= 1583)
            {
                result = new DateTime(year, month, day, hour, minute, second);
            }
            else
            {
                var solar = Lunar.Solar.FromYmdHms(year, month, day, hour, minute, second);
                result = new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
            }
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private bool TryParseLocalDateTime(out DateTime result, out double longitude)
    {
        result = DateTime.MinValue;
        longitude = 0;

        if (LocalYearTextBox?.Value == null ||
            LocalMonthComboBox?.SelectedItem == null ||
            LocalDayComboBox?.SelectedItem == null ||
            LocalTimePicker?.SelectedTime == null)
            return false;

        var year = (int)LocalYearTextBox.Value.Value;
        if (!int.TryParse(LocalMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return false;
        if (!int.TryParse(LocalDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day))
            return false;

        var time = LocalTimePicker.SelectedTime.Value;
        var hour = time.Hours;
        var minute = time.Minutes;
        var second = time.Seconds;

        if (!TryParseLongitude(out longitude))
            return false;

        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            return false;
        if (day > Lunar.Util.SolarUtil.GetDaysOfMonth(year, month))
            return false;
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(year, month, day))
            return false;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            return false;

        try
        {
            if (year >= 1583)
            {
                result = new DateTime(year, month, day, hour, minute, second);
            }
            else
            {
                var solar = Lunar.Solar.FromYmdHms(year, month, day, hour, minute, second);
                result = new DateTime(solar.Year, solar.Month, solar.Day, solar.Hour, solar.Minute, solar.Second);
            }
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private bool TryParseLongitude()
    {
        return TryParseLongitude(out _);
    }

    private bool TryParseLongitude(out double result)
    {
        result = 0;

        if (_settings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms)
        {
            var d = (int)(LocalLongitudeDmsDegreesTextBox?.Value ?? 0);
            var m = (int)(LocalLongitudeDmsMinutesTextBox?.Value ?? 0);
            var s = (double)(LocalLongitudeDmsSecondsTextBox?.Value ?? 0);
            var isEast = LocalLongitudeDmsDirectionComboBox?.SelectedIndex == 0;
            return LongitudeConverter.TryParseDms(d, m, s, isEast, out result);
        }
        else
        {
            if (LocalLongitudeTextBox?.Value == null) return false;
            result = (double)LocalLongitudeTextBox.Value.Value;
            return true;
        }
    }

    private bool TryParseLunarDateTime(out DateTime result)
    {
        result = DateTime.MinValue;

        if (LunarYearRangeComboBox?.SelectedItem == null ||
            LunarTianganComboBox?.SelectedItem == null ||
            LunarDizhiComboBox?.SelectedItem == null ||
            LunarMonthComboBox?.SelectedItem == null ||
            LunarDayComboBox?.SelectedItem == null)
            return false;

        // 解析年份范围
        var yearRange = LunarYearRangeComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(yearRange)) return false;

        var yearParts = yearRange.Split('-');
        if (yearParts.Length != 2) return false;
        if (!int.TryParse(yearParts[0], out var startYear)) return false;
        if (!int.TryParse(yearParts[1], out var endYear)) return false;

        // 获取天干地支对应年份
        var tiangan = LunarTianganComboBox.SelectedItem.ToString();
        var dizhi = LunarDizhiComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(tiangan) || string.IsNullOrEmpty(dizhi)) return false;

        // 计算天干地支对应的年份
        var tianganIndex = Array.IndexOf(_tiangan, tiangan);
        var dizhiIndex = Array.IndexOf(_dizhi, dizhi);
        if (tianganIndex < 0 || dizhiIndex < 0) return false;

        var baseYear = 4;
        var yearOffset = 0;
        while ((baseYear + yearOffset - 4) % 10 != tianganIndex ||
               (baseYear + yearOffset - 4) % 12 != dizhiIndex)
        {
            yearOffset++;
            if (yearOffset > 60) return false;
        }

        var baseLunarYear = baseYear + yearOffset;
        var lunarYear = baseLunarYear;

        while (lunarYear < startYear)
        {
            lunarYear += 60;
        }
        if (lunarYear > endYear)
        {
            lunarYear -= 60;
        }

        // 解析月份
        var monthText = LunarMonthComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(monthText)) return false;

        var isLeapMonth = monthText.StartsWith("闰");
        var monthStr = isLeapMonth ? monthText[1..].Replace("月", "") : monthText.Replace("月", "");
        if (!int.TryParse(monthStr, out var lunarMonth)) return false;

        // 解析日期
        var dayText = LunarDayComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(dayText)) return false;
        if (!int.TryParse(dayText.Replace("日", ""), out var lunarDay)) return false;

        // 解析时间
        if (LunarTimePicker?.SelectedTime == null) return false;
        var time = LunarTimePicker.SelectedTime.Value;
        var hour = time.Hours;
        var minute = time.Minutes;
        var second = time.Seconds;

        var solarDate = LunarCalendarHelper.LunarToSolar(lunarYear, lunarMonth, isLeapMonth, lunarDay, hour, minute, second);
        if (solarDate == null) return false;

        // 验证转换结果年份在有效范围内(1-9999)
        if (solarDate.Value.Year < 1 || solarDate.Value.Year > 9999) return false;

        // 验证转换结果不在1582年10月5-14日（历史上不存在的日期）
        if (DateValidationHelper.IsInvalidGregorianTransitionDate(solarDate.Value)) return false;

        result = solarDate.Value;
        return true;
    }

    /// <summary>
    /// 验证并调整日期，如果日期无效则自动调整为该月最后一天
    /// </summary>
    private int ValidateAndFixDay(int year, int month, int day)
    {
        var daysInMonth = Lunar.Util.SolarUtil.GetDaysOfMonth(year, month);
        return Math.Min(day, daysInMonth);
    }

    #endregion

    private void UpdateDayComboBox(WpfNumericUpDown yearTextBox, ComboBox monthComboBox, ComboBox dayComboBox)
    {
        if (yearTextBox.Value == null)
            return;
        var year = (int)yearTextBox.Value.Value;
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
            var safeDay = ValidateAndFixDay(year, month, selectedDay.Value);
            dayComboBox.SelectedItem = $"{safeDay}日";
        }
        else
        {
            dayComboBox.SelectedIndex = -1;
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.LongitudeDisplayMode))
        {
            UpdateLongitudeDisplay();
        }
    }

    private void UpdateLongitudeDisplay()
    {
        if (_settings == null || LocalLongitudeTextBox == null || LocalLongitudeDmsPanel == null)
            return;

        var currentLongitude = 116.4;
        if (_settings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal)
        {
            if (LocalLongitudeTextBox.Value != null)
                currentLongitude = (double)LocalLongitudeTextBox.Value.Value;
            else if (TryParseLongitude(out var lon))
                currentLongitude = lon;
        }
        else
        {
            if (TryParseLongitude(out var lon))
                currentLongitude = lon;
            else if (LocalLongitudeTextBox.Value != null)
                currentLongitude = (double)LocalLongitudeTextBox.Value.Value;
        }

        LocalLongitudeTextBox.Visibility = _settings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal ? Visibility.Visible : Visibility.Collapsed;
        LocalLongitudeDmsPanel.Visibility = _settings.LongitudeDisplayMode == LongitudeDisplayMode.Dms ? Visibility.Visible : Visibility.Collapsed;

        if (_settings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal)
        {
            LocalLongitudeTextBox.Value = (decimal)currentLongitude;
        }
        else
        {
            LongitudeConverter.DecomposeDms(currentLongitude, out int d, out int m, out double s, out bool isEast);
            LocalLongitudeDmsDegreesTextBox.Value = d;
            LocalLongitudeDmsMinutesTextBox.Value = m;
            LocalLongitudeDmsSecondsTextBox.Value = (decimal)s;
            LocalLongitudeDmsDirectionComboBox.SelectedIndex = isEast ? 0 : 1;
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        foreach (var timer in _resultTimers.Values)
        {
            timer.Stop();
            timer.Dispose();
        }
        _resultTimers.Clear();

        if (_settings != null)
        {
            _settings.PropertyChanged -= OnSettingsPropertyChanged;
        }
    }
}
