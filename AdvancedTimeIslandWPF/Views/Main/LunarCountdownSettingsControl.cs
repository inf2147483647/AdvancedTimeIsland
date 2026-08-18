using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class LunarCountdownSettingsControl : ComponentBase<LunarCountdownSettings>
{
    private TextBox _text1TextBox = null!;
    private bool _initCompleted;
    private TextBox _text3TextBox = null!;
    private TextBox _text4TextBox = null!;
    private TextBox _timeFormatTextBox = null!;
    private ComboBox _timeBaseComboBox = null!;
    private CheckBox _text1EnableCustomFontSizeToggle = null!;
    private CheckBox _text1EnableCustomFontColorToggle = null!;
    private CheckBox _nameEnableCustomFontSizeToggle = null!;
    private CheckBox _nameEnableCustomFontColorToggle = null!;
    private CheckBox _text3EnableCustomFontSizeToggle = null!;
    private CheckBox _text3EnableCustomFontColorToggle = null!;
    private CheckBox _timeEnableCustomFontSizeToggle = null!;
    private CheckBox _timeEnableCustomFontColorToggle = null!;
    private CheckBox _text4EnableCustomFontSizeToggle = null!;
    private CheckBox _text4EnableCustomFontColorToggle = null!;
    private ListBox _countdownListBox = null!;

    private TextBlock _selectionHintTextBlock = null!;
    private System.Timers.Timer? _hintTimer;

    private WpfNumericUpDown _text1FontSizeNumericUpDown = null!;
    private WpfColorPicker _text1FontColorPicker = null!;
    private WpfNumericUpDown _nameFontSizeNumericUpDown = null!;
    private WpfColorPicker _nameFontColorPicker = null!;
    private WpfNumericUpDown _text3FontSizeNumericUpDown = null!;
    private WpfColorPicker _text3FontColorPicker = null!;
    private WpfNumericUpDown _timeFontSizeNumericUpDown = null!;
    private WpfColorPicker _timeFontColorPicker = null!;
    private WpfNumericUpDown _text4FontSizeNumericUpDown = null!;
    private WpfColorPicker _text4FontColorPicker = null!;

    private ComboBox _progressDisplayModeComboBox = null!;
    private WpfColorPicker _progressBarColorPicker = null!;
    private WpfColorPicker _progressRingColorPicker = null!;

    private ComboBox _startYearRangeCombo = null!;
    private ComboBox _startTianganCombo = null!;
    private ComboBox _startDizhiCombo = null!;
    private ComboBox _startMonthCombo = null!;
    private CheckBox _startLeapToggle = null!;
    private ComboBox _startDayCombo = null!;
    private WpfTimePicker _startTimePicker = null!;
    private TextBox _startSolarYearTextBox = null!;
    private ComboBox _startSolarMonthComboBox = null!;
    private ComboBox _startSolarDayComboBox = null!;
    private ComboBox _startSolarHourComboBox = null!;
    private ComboBox _startSolarMinuteComboBox = null!;
    private ComboBox _startSolarSecondComboBox = null!;

    private bool _isUpdatingStartTime;

    public LunarCountdownSettingsControl()
    {
        InitializeComponent();
        InitializeControlReferences();
    }

    /// <summary>
    /// 从 XAML 的 SettingsControl.Switcher 中提取控件引用。
    /// （SettingsControl 模板内元素无法使用 x:Name，需通过 Switcher 属性访问）
    /// </summary>
    private void InitializeControlReferences()
    {
        _text1TextBox = (TextBox)((StackPanel)Text1Item.Switcher).Children[0];
        _text3TextBox = (TextBox)((StackPanel)Text3Item.Switcher).Children[0];
        _text4TextBox = (TextBox)((StackPanel)Text4Item.Switcher).Children[0];
        _timeFormatTextBox = (TextBox)((StackPanel)TimeFormatItem.Switcher).Children[0];
        _timeBaseComboBox = (ComboBox)((StackPanel)TimeBaseItem.Switcher).Children[0];

        _progressDisplayModeComboBox = (ComboBox)((StackPanel)ProgressDisplayModeItem.Switcher).Children[0];
        _progressBarColorPicker = (WpfColorPicker)((StackPanel)ProgressBarColorItem.Switcher).Children[0];
        _progressRingColorPicker = (WpfColorPicker)((StackPanel)ProgressRingColorItem.Switcher).Children[0];

        _text1FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text1FontSizeItem.Switcher).Children[0];
        _text1EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text1FontSizeItem.Switcher).Children[1];
        _text1FontColorPicker = (WpfColorPicker)((StackPanel)Text1ColorItem.Switcher).Children[0];
        _text1EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text1ColorItem.Switcher).Children[1];

        _nameFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)NameFontSizeItem.Switcher).Children[0];
        _nameEnableCustomFontSizeToggle = (CheckBox)((StackPanel)NameFontSizeItem.Switcher).Children[1];
        _nameFontColorPicker = (WpfColorPicker)((StackPanel)NameColorItem.Switcher).Children[0];
        _nameEnableCustomFontColorToggle = (CheckBox)((StackPanel)NameColorItem.Switcher).Children[1];

        _text3FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text3FontSizeItem.Switcher).Children[0];
        _text3EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text3FontSizeItem.Switcher).Children[1];
        _text3FontColorPicker = (WpfColorPicker)((StackPanel)Text3ColorItem.Switcher).Children[0];
        _text3EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text3ColorItem.Switcher).Children[1];

        _timeFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)TimeFontSizeItem.Switcher).Children[0];
        _timeEnableCustomFontSizeToggle = (CheckBox)((StackPanel)TimeFontSizeItem.Switcher).Children[1];
        _timeFontColorPicker = (WpfColorPicker)((StackPanel)TimeColorItem.Switcher).Children[0];
        _timeEnableCustomFontColorToggle = (CheckBox)((StackPanel)TimeColorItem.Switcher).Children[1];

        _text4FontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)Text4FontSizeItem.Switcher).Children[0];
        _text4EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text4FontSizeItem.Switcher).Children[1];
        _text4FontColorPicker = (WpfColorPicker)((StackPanel)Text4ColorItem.Switcher).Children[0];
        _text4EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text4ColorItem.Switcher).Children[1];

        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("ClassIsland时间");

        _progressDisplayModeComboBox.Items.Add("不显示");
        _progressDisplayModeComboBox.Items.Add("进度条");
        _progressDisplayModeComboBox.Items.Add("进度环");
        _progressDisplayModeComboBox.Items.Add("进度条和进度环");

        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            _startYearRangeCombo.Items.Add(range);
        }
        var tiangan = new[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        foreach (var t in tiangan) _startTianganCombo.Items.Add(t);
        var dizhi = new[] { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
        foreach (var d in dizhi) _startDizhiCombo.Items.Add(d);
        for (int i = 1; i <= 12; i++) _startMonthCombo.Items.Add(i.ToString());
        for (int i = 1; i <= 30; i++) _startDayCombo.Items.Add(i.ToString());

        for (int i = 1; i <= 12; i++) _startSolarMonthComboBox.Items.Add($"{i}月");
        for (int i = 1; i <= 31; i++) _startSolarDayComboBox.Items.Add($"{i}日");
        for (int i = 0; i < 24; i++) _startSolarHourComboBox.Items.Add(i.ToString("D2"));
        for (int i = 0; i < 60; i++) _startSolarMinuteComboBox.Items.Add(i.ToString("D2"));
        for (int i = 0; i < 60; i++) _startSolarSecondComboBox.Items.Add(i.ToString("D2"));

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startSolarYearTextBox, (s, e) => UpdateStartDayComboBox());
        _startSolarMonthComboBox.SelectionChanged += (s, e) => UpdateStartDayComboBox();

        // SettingsCard 无内置事件，通过 IsOn 依赖属性变化订阅开关切换
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(EnableCustomProgressColorCard, (s, e) =>
            {
                Settings.EnableCustomProgressColor = EnableCustomProgressColorCard.IsOn;
                UpdateProgressColorControlsEnabled();
            });
    }

    private void UpdateStartDayComboBox()
    {
        if (!int.TryParse(_startSolarYearTextBox.Text?.Trim(), out var year))
            return;
        if (_startSolarMonthComboBox.SelectedItem == null)
            return;
        if (!int.TryParse(_startSolarMonthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var month))
            return;

        var selectedDayText = _startSolarDayComboBox.SelectedItem?.ToString();
        int? selectedDay = null;
        if (selectedDayText != null && int.TryParse(selectedDayText.Replace("日", ""), out var d))
            selectedDay = d;

        _startSolarDayComboBox.Items.Clear();

        if (year == 1582 && month == 10)
        {
            for (int i = 1; i <= 4; i++)
            {
                _startSolarDayComboBox.Items.Add($"{i}日");
            }
            for (int i = 15; i <= 31; i++)
            {
                _startSolarDayComboBox.Items.Add($"{i}日");
            }
        }
        else
        {
            var daysInMonth = GetDaysInMonth(year, month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                _startSolarDayComboBox.Items.Add($"{i}日");
            }
        }

        if (selectedDay.HasValue)
        {
            var safeDay = Math.Min(selectedDay.Value, _startSolarDayComboBox.Items.Count);
            if (safeDay > 0)
            {
                _startSolarDayComboBox.SelectedItem = $"{safeDay}日";
            }
            else
            {
                _startSolarDayComboBox.SelectedIndex = -1;
            }
        }
        else
        {
            _startSolarDayComboBox.SelectedIndex = -1;
        }
    }

    private void OnStartSyncSolarToLunarClick(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(_startSolarYearTextBox.Text?.Trim(), out var year) ||
            _startSolarMonthComboBox.SelectedIndex < 0 ||
            _startSolarDayComboBox.SelectedItem == null ||
            !int.TryParse(_startSolarDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) ||
            _startSolarHourComboBox.SelectedIndex < 0 ||
            _startSolarMinuteComboBox.SelectedIndex < 0 ||
            _startSolarSecondComboBox.SelectedIndex < 0)
            return;

        var month = _startSolarMonthComboBox.SelectedIndex + 1;
        try
        {
            var solarDate = new DateTime(year, month, day,
                _startSolarHourComboBox.SelectedIndex, _startSolarMinuteComboBox.SelectedIndex, _startSolarSecondComboBox.SelectedIndex);

            if (!LunarCalendarHelper.IsDateSupported(solarDate))
                return;

            var lunarYear = LunarCalendarHelper.GetLunarYear(solarDate);
            var lunarMonth = LunarCalendarHelper.GetLunarMonth(solarDate);
            var isLeap = LunarCalendarHelper.IsLeapMonth(solarDate);
            var lunarDay = LunarCalendarHelper.GetLunarDay(solarDate);

            if (lunarYear == 0 || lunarMonth == 0 || lunarDay == 0)
                return;

            ApplyLunarToStartControls(lunarYear, lunarMonth, isLeap, lunarDay, solarDate.Hour, solarDate.Minute, solarDate.Second);
            CommitStartLunarToSettings();
        }
        catch { }
    }

    private void OnStartSyncLunarToSolarClick(object? sender, RoutedEventArgs e)
    {
        var lunarYearResult = TryResolveLunarYear();
        if (!lunarYearResult.HasValue) return;
        var lunarYearVal = lunarYearResult.Value;

        var lunarMonthVal = _startMonthCombo.SelectedIndex + 1;
        var isLeapVal = _startLeapToggle.IsChecked == true;
        var lunarDayVal = _startDayCombo.SelectedIndex + 1;
        var hourVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Hours : 0;
        var minuteVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Minutes : 0;
        var secondVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Seconds : 0;

        var solarResult = LunarCalendarHelper.LunarToSolar(lunarYearVal, lunarMonthVal, isLeapVal, lunarDayVal, hourVal, minuteVal, secondVal);
        if (solarResult.HasValue)
        {
            _startSolarYearTextBox.Text = solarResult.Value.Year.ToString();
            _startSolarMonthComboBox.SelectedIndex = solarResult.Value.Month - 1;
            _startSolarDayComboBox.SelectedItem = $"{solarResult.Value.Day}日";
            _startSolarHourComboBox.SelectedIndex = solarResult.Value.Hour;
            _startSolarMinuteComboBox.SelectedIndex = solarResult.Value.Minute;
            _startSolarSecondComboBox.SelectedIndex = solarResult.Value.Second;
            CommitStartLunarToSettings();
        }
    }

    private void ApplyLunarToStartControls(int lunarYear, int lunarMonth, bool isLeap, int lunarDay, int hour, int minute, int second)
    {
        _isUpdatingStartTime = true;
        try
        {
            var tgIndex = (lunarYear - 4) % 10;
            if (tgIndex < 0) tgIndex += 10;
            var dzIndex = (lunarYear - 4) % 12;
            if (dzIndex < 0) dzIndex += 12;

            _startTianganCombo.SelectedIndex = tgIndex;
            _startDizhiCombo.SelectedIndex = dzIndex;
            _startMonthCombo.SelectedIndex = lunarMonth - 1;
            _startLeapToggle.IsChecked = isLeap;
            _startDayCombo.SelectedIndex = lunarDay - 1;
            _startTimePicker.SelectedTime = new TimeSpan(hour, minute, second);

            foreach (var range in LunarCalendarHelper.GetAllYearRanges())
            {
                if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
                {
                    if (lunarYear >= startYear && lunarYear <= endYear)
                    {
                        _startYearRangeCombo.SelectedItem = range;
                        break;
                    }
                }
            }
        }
        finally
        {
            _isUpdatingStartTime = false;
        }
    }

    private void CommitStartLunarToSettings()
    {
        var lunarYearResult = TryResolveLunarYear();
        if (!lunarYearResult.HasValue) return;
        var lunarYearVal = lunarYearResult.Value;

        var lunarMonthVal = _startMonthCombo.SelectedIndex + 1;
        var isLeapVal = _startLeapToggle.IsChecked == true;
        var lunarDayVal = _startDayCombo.SelectedIndex + 1;
        var hourVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Hours : 0;
        var minuteVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Minutes : 0;
        var secondVal = _startTimePicker.SelectedTime.HasValue ? _startTimePicker.SelectedTime.Value.Seconds : 0;

        var solarResult = LunarCalendarHelper.LunarToSolar(lunarYearVal, lunarMonthVal, isLeapVal, lunarDayVal, hourVal, minuteVal, secondVal);
        if (solarResult.HasValue)
        {
            Settings.StartTime = UnixTimeHelper.ToUnixTimestamp(solarResult.Value);
            _startSolarYearTextBox.Text = solarResult.Value.Year.ToString();
            _startSolarMonthComboBox.SelectedIndex = solarResult.Value.Month - 1;
            _startSolarDayComboBox.SelectedItem = $"{solarResult.Value.Day}日";
            _startSolarHourComboBox.SelectedIndex = solarResult.Value.Hour;
            _startSolarMinuteComboBox.SelectedIndex = solarResult.Value.Minute;
            _startSolarSecondComboBox.SelectedIndex = solarResult.Value.Second;
        }
    }

    private int? TryResolveLunarYear()
    {
        if (_startYearRangeCombo.SelectedItem == null ||
            _startTianganCombo.SelectedIndex < 0 ||
            _startDizhiCombo.SelectedIndex < 0)
            return null;

        var yearRange = _startYearRangeCombo.SelectedItem.ToString();
        if (string.IsNullOrEmpty(yearRange)) return null;

        var yearParts = yearRange.Split('-');
        if (yearParts.Length != 2) return null;
        if (!int.TryParse(yearParts[0], out var startYear)) return null;
        if (!int.TryParse(yearParts[1], out var endYear)) return null;

        var tg = _startTianganCombo.SelectedIndex;
        var dz = _startDizhiCombo.SelectedIndex;

        var baseYear = 4;
        var yearOffset = 0;
        while ((baseYear + yearOffset - 4) % 10 != tg ||
               (baseYear + yearOffset - 4) % 12 != dz)
        {
            yearOffset++;
            if (yearOffset > 60) return null;
        }

        var lunarYearVal = baseYear + yearOffset;
        while (lunarYearVal < startYear)
        {
            lunarYearVal += 60;
        }
        if (lunarYearVal > endYear)
        {
            lunarYearVal -= 60;
        }

        return lunarYearVal;
    }

    private void OnText2ButtonClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
        }
        else
        {
            _countdownListBox.BringIntoView();
            ShowHint();
        }
    }

    private void OnText1EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontSize = _text1EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontColor = _text1EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.NameEnableCustomFontSize = _nameEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.NameEnableCustomFontColor = _nameEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontSize = _text3EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontColor = _text3EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontSize = _timeEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontColor = _timeEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontSize = _text4EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontColor = _text4EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _nameFontSizeNumericUpDown.IsEnabled = Settings.NameEnableCustomFontSize;
        _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
        _text3FontSizeNumericUpDown.IsEnabled = Settings.Text3EnableCustomFontSize;
        _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        _timeFontSizeNumericUpDown.IsEnabled = Settings.TimeEnableCustomFontSize;
        _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
        _text4FontSizeNumericUpDown.IsEnabled = Settings.Text4EnableCustomFontSize;
        _text4FontColorPicker.IsEnabled = Settings.Text4EnableCustomFontColor;
    }

    private void UpdateProgressColorControlsEnabled()
    {
        var isCustomEnabled = Settings.EnableCustomProgressColor;
        var showProgressBar = Settings.ProgressDisplayMode == ProgressDisplayMode.Bar ||
                              Settings.ProgressDisplayMode == ProgressDisplayMode.Both;

        _progressBarColorPicker.IsEnabled = isCustomEnabled && showProgressBar;
        _progressRingColorPicker.IsEnabled = isCustomEnabled;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }
        _initCompleted = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        _text1TextBox.Text = Settings.Text1;
        _text3TextBox.Text = Settings.Text3;
        _text4TextBox.Text = Settings.Text4;
        _timeFormatTextBox.Text = Settings.TimeFormat;
        // 迁移旧版时间基准值
        var migratedType = TimeBaseTypeHelper.Migrate((int)Settings.TimeBaseType);
        if (migratedType != Settings.TimeBaseType)
        {
            Settings.TimeBaseType = migratedType;
        }

        _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
        {
            TimeBaseType.PluginOffsetServerTime => 0,
            TimeBaseType.RawServerTime => 1,
            TimeBaseType.ClassIslandTime => 2,
            _ => 0
        };

        LoadStartTimeControls();

        _progressDisplayModeComboBox.SelectedIndex = (int)Settings.ProgressDisplayMode;

        _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        _nameFontSizeNumericUpDown.Value = (decimal)Settings.NameFontSize;
        _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        _text4FontSizeNumericUpDown.Value = (decimal)Settings.Text4FontSize;
        _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);

        UpdateCountdownList();

        AttachEventHandlers();

        _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        _nameEnableCustomFontSizeToggle.IsChecked = Settings.NameEnableCustomFontSize;
        _nameEnableCustomFontColorToggle.IsChecked = Settings.NameEnableCustomFontColor;
        _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;
        _text4EnableCustomFontSizeToggle.IsChecked = Settings.Text4EnableCustomFontSize;
        _text4EnableCustomFontColorToggle.IsChecked = Settings.Text4EnableCustomFontColor;
        UpdateControlsEnabled();

        EnableCustomProgressColorCard.IsOn = Settings.EnableCustomProgressColor;
        UpdateProgressColorControlsEnabled();
        _progressBarColorPicker.Color = ParseColor(Settings.ProgressBarColor);
        _progressRingColorPicker.Color = ParseColor(Settings.ProgressRingColor);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _hintTimer?.Stop();
        _hintTimer?.Dispose();
    }

    private void AttachEventHandlers()
    {
        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "");
        AttachTextHandler(_text3TextBox, v => Settings.Text3 = v ?? "还有");
        AttachTextHandler(_text4TextBox, v => Settings.Text4 = v ?? "");
        AttachTextHandler(_timeFormatTextBox, v => Settings.TimeFormat = v ?? "%d天%h小时%m分钟%s秒");
        AttachStartTimeHandlers();
    }

    private void LoadStartTimeControls()
    {
        var startTime = UnixTimeHelper.FromUnixTimestamp(Settings.StartTime);

        _startSolarYearTextBox.Text = startTime.Year.ToString();
        _startSolarMonthComboBox.SelectedIndex = startTime.Month - 1;
        _startSolarDayComboBox.SelectedItem = $"{startTime.Day}日";
        _startSolarHourComboBox.SelectedIndex = startTime.Hour;
        _startSolarMinuteComboBox.SelectedIndex = startTime.Minute;
        _startSolarSecondComboBox.SelectedIndex = startTime.Second;

        var lunarYear = LunarCalendarHelper.GetLunarYear(startTime);
        var lunarMonth = LunarCalendarHelper.GetLunarMonth(startTime);
        var isLeap = LunarCalendarHelper.IsLeapMonth(startTime);
        var lunarDay = LunarCalendarHelper.GetLunarDay(startTime);

        if (lunarYear == 0 || lunarMonth == 0 || lunarDay == 0)
        {
            _startYearRangeCombo.SelectedItem = "1984-2043";
            _startTianganCombo.SelectedIndex = 0;
            _startDizhiCombo.SelectedIndex = 0;
            _startMonthCombo.SelectedIndex = 0;
            _startLeapToggle.IsChecked = false;
            _startDayCombo.SelectedIndex = 0;
            _startTimePicker.SelectedTime = new TimeSpan(startTime.Hour, startTime.Minute, startTime.Second);
            return;
        }

        ApplyLunarToStartControls(lunarYear, lunarMonth, isLeap, lunarDay, startTime.Hour, startTime.Minute, startTime.Second);
    }

    private void AttachStartTimeHandlers()
    {
        void UpdateStartTimeFromLunar()
        {
            if (_isUpdatingStartTime) return;
            CommitStartLunarToSettings();
        }

        _startYearRangeCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        _startTianganCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        _startDizhiCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        _startMonthCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        _startLeapToggle.Checked += (s, e) => UpdateStartTimeFromLunar();
        _startLeapToggle.Unchecked += (s, e) => UpdateStartTimeFromLunar();
        _startDayCombo.SelectionChanged += (s, e) => UpdateStartTimeFromLunar();
        _startTimePicker.SelectedTimeChanged += (s, e) => UpdateStartTimeFromLunar();
    }

    private void AttachTextHandler(TextBox textBox, Action<string?> setter)
    {
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => setter(textBox.Text));
    }

    private void OnTimeBaseChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_timeBaseComboBox.SelectedIndex >= 0)
        {
            Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
            {
                0 => TimeBaseType.PluginOffsetServerTime,
                1 => TimeBaseType.RawServerTime,
                2 => TimeBaseType.ClassIslandTime,
                _ => TimeBaseType.PluginOffsetServerTime
            };
        }
    }

    private void OnProgressDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_progressDisplayModeComboBox.SelectedIndex >= 0)
        {
            Settings.ProgressDisplayMode = (ProgressDisplayMode)_progressDisplayModeComboBox.SelectedIndex;
            UpdateProgressColorControlsEnabled();
        }
    }

    private void OnProgressBarColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ProgressBarColor = _progressBarColorPicker.Color.ToString();
    }

    private void OnProgressRingColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ProgressRingColor = _progressRingColorPicker.Color.ToString();
    }

    private void OnCountdownListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_countdownListBox.SelectedIndex >= 0)
        {
            HideHint();
        }
    }

    private void OnText1FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text1FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text1FontSize = (double)_text1FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText1ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text1FontColor = _text1FontColorPicker.Color.ToString();
    }

    private void OnNameFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_nameFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.NameFontSize = (double)_nameFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnNameColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.NameFontColor = _nameFontColorPicker.Color.ToString();
    }

    private void OnText3FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text3FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text3FontSize = (double)_text3FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText3ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text3FontColor = _text3FontColorPicker.Color.ToString();
    }

    private void OnTimeFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_timeFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.TimeFontSize = (double)_timeFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.TimeFontColor = _timeFontColorPicker.Color.ToString();
    }

    private void OnText4FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text4FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text4FontSize = (double)_text4FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText4ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text4FontColor = _text4FontColorPicker.Color.ToString();
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return ThemeHelper.ParseColor(colorString);
        }
        catch
        {
            return Colors.White;
        }
    }

    private void UpdateCountdownList()
    {
        _countdownListBox.Items.Clear();
        if (Settings.CountdownItems != null)
        {
            foreach (var item in Settings.CountdownItems)
            {
                var targetSolar = item.GetTargetTimestamp() > 0 ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp()) : Plugin.GetCurrentTime();
                var lunarDesc = GetLunarDateDescription(item);

                var container = new Grid();
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                container.Tag = item;

                var lunarTextBlock = new TextBlock
                {
                    Text = $"{item.Name} - {lunarDesc}",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetTextBrush(),
                    Padding = new Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(lunarTextBlock, 0);
                container.Children.Add(lunarTextBlock);

                var solarTextBlock = new TextBlock
                {
                    Text = targetSolar.ToString("yyyy-MM-dd HH:mm:ss"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    Padding = new Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(solarTextBlock, 1);
                container.Children.Add(solarTextBlock);

                var notifyPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                var notifySwitch = new CheckBox
                {
                    IsChecked = item.EnableNotification,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                var currentItem = item;
                notifySwitch.Checked += (s, e) =>
                {
                    currentItem.EnableNotification = notifySwitch.IsChecked == true;
                };
                notifySwitch.Unchecked += (s, e) =>
                {
                    currentItem.EnableNotification = notifySwitch.IsChecked == true;
                };
                notifyPanel.Children.Add(notifySwitch);
                Grid.SetColumn(notifyPanel, 2);
                container.Children.Add(notifyPanel);

                var listBoxItem = new ListBoxItem
                {
                    Content = container,
                    Tag = item
                };

                _countdownListBox.Items.Add(listBoxItem);
            }
        }
    }

    private string GetLunarDateDescription(LunarCountdownItem item)
    {
        var yearName = LunarCalendarHelper.GetLunarYearName(item.LunarYear);
        var monthStr = item.IsLeapMonth ? $"闰{item.LunarMonth}月" : $"{item.LunarMonth}月";
        return $"{yearName}年 {monthStr} {GetLunarDayString(item.LunarDay)}";
    }

    private string GetLunarDayString(int day)
    {
        if (day <= 0 || day > 30) return "";
        var prefix = new[] { "初", "十", "廿", "三" };
        var nums = new[] { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
        if (day <= 10) return $"{prefix[0]}{nums[day - 1]}";
        else if (day < 20) return $"{prefix[1]}{nums[day - 11]}";
        else if (day == 20) return "二十";
        else if (day < 30) return $"{prefix[2]}{nums[day - 21]}";
        else if (day == 30) return "三十";
        return "";
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        HideHint();
        if (Settings.CountdownItems == null)
        {
            Settings.CountdownItems = new List<LunarCountdownItem>();
        }
        Settings.CountdownItems.Add(LunarCountdownItem.CreateDefault());
        UpdateCountdownList();
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            Settings.CountdownItems.RemoveAt(_countdownListBox.SelectedIndex);
            UpdateCountdownList();
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void ShowHint()
    {
        _selectionHintTextBlock.Visibility = Visibility.Visible;
        _hintTimer?.Stop();
        _hintTimer = new System.Timers.Timer(5000);
        _hintTimer.Elapsed += (s, e) =>
        {
            UIThread.Post(HideHint);
        };
        _hintTimer.AutoReset = false;
        _hintTimer.Start();
    }

    private void HideHint()
    {
        _selectionHintTextBlock.Visibility = Visibility.Collapsed;
        _hintTimer?.Stop();
    }

    private void ShowEditDialog(LunarCountdownItem item, int order = 0)
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", order > 0 ? $"正在编辑第{order}个农历倒计时" : "编辑农历倒计时");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "SecondaryButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical };

        var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "公历可用范围为1901-02-19 ~ 2101-01-28");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", false);
        contentPanel.Children.Add(infoBar);

        var nameLabel = new TextBlock { Text = "名称:", Foreground = ThemeHelper.GetTextBrush() };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var lunarGroup = new Expander { Header = new TextBlock { Text = "农历日期", Foreground = ThemeHelper.GetTextBrush() }, IsExpanded = true };
        var lunarPanel = new StackPanel { Orientation = Orientation.Vertical };

        var yearRangePanel = new StackPanel { Orientation = Orientation.Horizontal };
        yearRangePanel.Children.Add(new TextBlock { Text = "年份范围:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        var yearRangeCombo = new ComboBox { Width = 180 };
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            yearRangeCombo.Items.Add(range);
        }

        bool foundRange = false;
        foreach (var range in LunarCalendarHelper.GetAllYearRanges())
        {
            if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
            {
                if (item.LunarYear >= startYear && item.LunarYear <= endYear)
                {
                    yearRangeCombo.SelectedItem = range;
                    foundRange = true;
                    break;
                }
            }
        }
        if (!foundRange)
            yearRangeCombo.SelectedItem = "1984-2043";

        yearRangePanel.Children.Add(yearRangeCombo);
        lunarPanel.Children.Add(yearRangePanel);

        var yearRow = new Grid();
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yearRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var yearLabel = new TextBlock { Text = "天干地支年:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(yearLabel, 0);
        yearRow.Children.Add(yearLabel);

        var tianganCombo = new ComboBox { Width = 60 };
        var tiangan = new[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        foreach (var t in tiangan) tianganCombo.Items.Add(t);
        Grid.SetColumn(tianganCombo, 1);
        yearRow.Children.Add(tianganCombo);

        var dizhiLabel = new TextBlock { Text = "地支:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        Grid.SetColumn(dizhiLabel, 2);
        yearRow.Children.Add(dizhiLabel);

        var dizhiCombo = new ComboBox { Width = 60 };
        var dizhi = new[] { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
        foreach (var d in dizhi) dizhiCombo.Items.Add(d);
        Grid.SetColumn(dizhiCombo, 3);
        yearRow.Children.Add(dizhiCombo);

        var tgIndex = (item.LunarYear - 4) % 10;
        if (tgIndex < 0) tgIndex += 10;
        var dzIndex = (item.LunarYear - 4) % 12;
        if (dzIndex < 0) dzIndex += 12;
        tianganCombo.SelectedIndex = tgIndex;
        dizhiCombo.SelectedIndex = dzIndex;

        lunarPanel.Children.Add(yearRow);

        var monthRow = new Grid();
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        var monthLabel = new TextBlock { Text = "月:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(monthLabel, 0);
        monthRow.Children.Add(monthLabel);

        var monthCombo = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthCombo.Items.Add(i.ToString());
        monthCombo.SelectedIndex = item.LunarMonth - 1;
        Grid.SetColumn(monthCombo, 1);
        monthRow.Children.Add(monthCombo);

        var leapLabel = new TextBlock { Text = "闰月:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        Grid.SetColumn(leapLabel, 2);
        monthRow.Children.Add(leapLabel);

        var leapToggle = new CheckBox { IsChecked = item.IsLeapMonth, Margin = new Thickness(4, 0, 0, 0) };
        Grid.SetColumn(leapToggle, 3);
        monthRow.Children.Add(leapToggle);

        var dayLabel = new TextBlock { Text = "日:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        Grid.SetColumn(dayLabel, 4);
        monthRow.Children.Add(dayLabel);

        var dayCombo = new ComboBox { Width = 80 };
        for (int i = 1; i <= 30; i++) dayCombo.Items.Add(i.ToString());
        dayCombo.SelectedIndex = item.LunarDay - 1;
        Grid.SetColumn(dayCombo, 5);
        monthRow.Children.Add(dayCombo);

        lunarPanel.Children.Add(monthRow);

        var timeRow = new Grid();
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });

        var timeLabel = new TextBlock { Text = "时间:", Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(timeLabel, 0);
        timeRow.Children.Add(timeLabel);

        var lunarTimePicker = new WpfTimePicker
        {
            Width = 250,
            ClockIdentifier = "24HourClock",
            UseSeconds = true,
            SelectedTime = new TimeSpan(item.Hour, item.Minute, item.Second)
        };
        Grid.SetColumn(lunarTimePicker, 1);
        timeRow.Children.Add(lunarTimePicker);

        lunarPanel.Children.Add(timeRow);

        var solarGroup = new Expander { Header = new TextBlock { Text = "公历对照（可互转）", Foreground = ThemeHelper.GetTextBrush() }, IsExpanded = true };
        var solarPanel = new StackPanel { Orientation = Orientation.Vertical };

        var solarDateLabel = new TextBlock { Text = "公历日期:", Foreground = ThemeHelper.GetTextBrush() };
        solarPanel.Children.Add(solarDateLabel);

        var currentSolarDate = item.GetTargetTimestamp() > 0 ? UnixTimeHelper.FromUnixTimestamp(item.GetTargetTimestamp()) : Plugin.GetCurrentTime();

        var solarDatePanel = new StackPanel { Orientation = Orientation.Horizontal };
        var solarYearTextBox = new TextBox { Width = 80, Text = currentSolarDate.Year.ToString() };
        var solarMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) solarMonthComboBox.Items.Add($"{i}月");
        solarMonthComboBox.SelectedIndex = currentSolarDate.Month - 1;
        var solarDayComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) solarDayComboBox.Items.Add($"{i}日");
        solarDayComboBox.SelectedItem = $"{currentSolarDate.Day}日";

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(solarYearTextBox, (s, e) => UpdateDayComboBox(solarYearTextBox, solarMonthComboBox, solarDayComboBox));
        solarMonthComboBox.SelectionChanged += (s, e) => UpdateDayComboBox(solarYearTextBox, solarMonthComboBox, solarDayComboBox);

        solarDatePanel.Children.Add(solarYearTextBox);
        solarDatePanel.Children.Add(solarMonthComboBox);
        solarDatePanel.Children.Add(solarDayComboBox);
        solarPanel.Children.Add(solarDatePanel);

        var solarTimePanel = new StackPanel { Orientation = Orientation.Horizontal };
        var solarHourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) solarHourComboBox.Items.Add(i.ToString("D2"));
        solarHourComboBox.SelectedIndex = currentSolarDate.Hour;
        var solarMinuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) solarMinuteComboBox.Items.Add(i.ToString("D2"));
        solarMinuteComboBox.SelectedIndex = currentSolarDate.Minute;
        var solarSecondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) solarSecondComboBox.Items.Add(i.ToString("D2"));
        solarSecondComboBox.SelectedIndex = currentSolarDate.Second;

        solarTimePanel.Children.Add(solarHourComboBox);
        solarTimePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        solarTimePanel.Children.Add(solarMinuteComboBox);
        solarTimePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        solarTimePanel.Children.Add(solarSecondComboBox);
        solarPanel.Children.Add(solarTimePanel);

        var syncButton = new Button { Content = "同步公历→农历", Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        syncButton.Click += (s, e) =>
        {
            if (int.TryParse(solarYearTextBox.Text?.Trim(), out var year) &&
                solarMonthComboBox.SelectedIndex >= 0 &&
                solarDayComboBox.SelectedItem != null &&
                int.TryParse(solarDayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var day) &&
                solarHourComboBox.SelectedIndex >= 0 &&
                solarMinuteComboBox.SelectedIndex >= 0 &&
                solarSecondComboBox.SelectedIndex >= 0)
            {
                var month = solarMonthComboBox.SelectedIndex + 1;
                try
                {
                    var solarDate = new DateTime(year, month, day,
                        solarHourComboBox.SelectedIndex, solarMinuteComboBox.SelectedIndex, solarSecondComboBox.SelectedIndex);

                    if (!LunarCalendarHelper.IsDateSupported(solarDate))
                    {
                        return;
                    }

                    var lunarYear = LunarCalendarHelper.GetLunarYear(solarDate);
                    var lunarMonth = LunarCalendarHelper.GetLunarMonth(solarDate);
                    var isLeap = LunarCalendarHelper.IsLeapMonth(solarDate);
                    var lunarDay = LunarCalendarHelper.GetLunarDay(solarDate);

                    if (lunarYear == 0 || lunarMonth == 0 || lunarDay == 0)
                    {
                        return;
                    }

                    var tgIndexNew = (lunarYear - 4) % 10;
                    if (tgIndexNew < 0) tgIndexNew += 10;
                    var dzIndexNew = (lunarYear - 4) % 12;
                    if (dzIndexNew < 0) dzIndexNew += 12;

                    tianganCombo.SelectedIndex = tgIndexNew;
                    dizhiCombo.SelectedIndex = dzIndexNew;
                    monthCombo.SelectedIndex = lunarMonth - 1;
                    leapToggle.IsChecked = isLeap;
                    dayCombo.SelectedIndex = lunarDay - 1;
                    lunarTimePicker.SelectedTime = new TimeSpan(solarDate.Hour, solarDate.Minute, solarDate.Second);

                    foreach (var range in LunarCalendarHelper.GetAllYearRanges())
                    {
                        if (LunarCalendarHelper.ParseYearRange(range, out var startYear, out var endYear))
                        {
                            if (lunarYear >= startYear && lunarYear <= endYear)
                            {
                                yearRangeCombo.SelectedItem = range;
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
        };
        solarPanel.Children.Add(syncButton);

        var syncButton2 = new Button { Content = "同步农历→公历", Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        syncButton2.Click += (s, e) =>
        {
            if (yearRangeCombo.SelectedItem == null ||
                tianganCombo.SelectedIndex < 0 ||
                dizhiCombo.SelectedIndex < 0)
                return;

            var yearRange = yearRangeCombo.SelectedItem.ToString();
            if (string.IsNullOrEmpty(yearRange)) return;

            var yearParts = yearRange.Split('-');
            if (yearParts.Length != 2) return;
            if (!int.TryParse(yearParts[0], out var startYear)) return;
            if (!int.TryParse(yearParts[1], out var endYear)) return;

            var tg = tianganCombo.SelectedIndex;
            var dz = dizhiCombo.SelectedIndex;

            var baseYear = 4;
            var yearOffset = 0;
            while ((baseYear + yearOffset - 4) % 10 != tg ||
                   (baseYear + yearOffset - 4) % 12 != dz)
            {
                yearOffset++;
                if (yearOffset > 60) return;
            }

            var baseLunarYearVal = baseYear + yearOffset;
            var lunarYearVal = baseLunarYearVal;

            while (lunarYearVal < startYear)
            {
                lunarYearVal += 60;
            }
            if (lunarYearVal > endYear)
            {
                lunarYearVal -= 60;
            }

            var lunarMonthVal = monthCombo.SelectedIndex + 1;
            var isLeapVal = leapToggle.IsChecked == true;
            var lunarDayVal = dayCombo.SelectedIndex + 1;
            var hourVal = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Hours : 0;
            var minuteVal = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Minutes : 0;
            var secondVal = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Seconds : 0;

            var solarResult = LunarCalendarHelper.LunarToSolar(lunarYearVal, lunarMonthVal, isLeapVal, lunarDayVal, hourVal, minuteVal, secondVal);
            if (solarResult.HasValue)
            {
                solarYearTextBox.Text = solarResult.Value.Year.ToString();
                solarMonthComboBox.SelectedIndex = solarResult.Value.Month - 1;
                solarDayComboBox.SelectedItem = $"{solarResult.Value.Day}日";
                solarHourComboBox.SelectedIndex = solarResult.Value.Hour;
                solarMinuteComboBox.SelectedIndex = solarResult.Value.Minute;
                solarSecondComboBox.SelectedIndex = solarResult.Value.Second;
            }
        };
        solarPanel.Children.Add(syncButton2);

        var notifyToggle = new CheckBox { Content = "启用通知", IsChecked = item.EnableNotification };
        contentPanel.Children.Add(notifyToggle);

        lunarGroup.Content = lunarPanel;
        contentPanel.Children.Add(lunarGroup);
        solarGroup.Content = solarPanel;
        contentPanel.Children.Add(solarGroup);

        FluentAvaloniaCompatibilityHelper.AddContentDialogButtonClickHandler(dialog, "PrimaryButtonClick", (s, e) =>
        {
            item.Name = nameTextBox.Text ?? "新农历倒计时";

            if (yearRangeCombo.SelectedItem != null)
            {
                var yearRange = yearRangeCombo.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(yearRange))
                {
                    var yearParts = yearRange.Split('-');
                    if (yearParts.Length == 2 &&
                        int.TryParse(yearParts[0], out var startYear) &&
                        int.TryParse(yearParts[1], out var endYear))
                    {
                        var tg = tianganCombo.SelectedIndex >= 0 ? tianganCombo.SelectedIndex : 0;
                        var dz = dizhiCombo.SelectedIndex >= 0 ? dizhiCombo.SelectedIndex : 0;

                        var baseYear = 4;
                        var yearOffset = 0;
                        while ((baseYear + yearOffset - 4) % 10 != tg ||
                               (baseYear + yearOffset - 4) % 12 != dz)
                        {
                            yearOffset++;
                            if (yearOffset > 60) break;
                        }

                        var baseLunarYearVal = baseYear + yearOffset;
                        var lunarYearVal = baseLunarYearVal;

                        while (lunarYearVal < startYear)
                        {
                            lunarYearVal += 60;
                        }
                        if (lunarYearVal > endYear)
                        {
                            lunarYearVal -= 60;
                        }

                        item.LunarYear = lunarYearVal;
                    }
                }
            }
            item.LunarMonth = monthCombo.SelectedIndex + 1;
            item.IsLeapMonth = leapToggle.IsChecked == true;
            item.LunarDay = dayCombo.SelectedIndex + 1;
            item.Hour = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Hours : 0;
            item.Minute = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Minutes : 0;
            item.Second = lunarTimePicker.SelectedTime.HasValue ? lunarTimePicker.SelectedTime.Value.Seconds : 0;
            item.EnableNotification = notifyToggle.IsChecked == true;
            item.IsCompleted = false;

            UpdateCountdownList();
        });

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = contentPanel,
            Margin = new Thickness(12, 12, 12, 0)
        };

        var dialogMainPanel = new Grid();
        dialogMainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(scrollViewer, 0);
        dialogMainPanel.Children.Add(scrollViewer);

        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", dialogMainPanel);

        _ = FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, Window.GetWindow(this));
    }

    private static void UpdateDayComboBox(TextBox yearTextBox, ComboBox monthComboBox, ComboBox dayComboBox)
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
            var daysInMonth = GetDaysInMonth(year, month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                dayComboBox.Items.Add($"{i}日");
            }
        }

        if (selectedDay.HasValue)
        {
            var safeDay = Math.Min(selectedDay.Value, dayComboBox.Items.Count);
            if (safeDay > 0)
            {
                dayComboBox.SelectedItem = $"{safeDay}日";
            }
            else
            {
                dayComboBox.SelectedIndex = -1;
            }
        }
        else
        {
            dayComboBox.SelectedIndex = -1;
        }
    }

    private static int GetDaysInMonth(int year, int month)
    {
        if (year > 1582)
        {
            return Lunar.Util.SolarUtil.GetDaysOfMonth(year, month);
        }

        if (year == 1582 && month == 10)
        {
            return 21;
        }

        if (month == 2)
        {
            if (IsJulianLeapYear(year))
                return 29;
            return 28;
        }

        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        return 31;
    }

    private static bool IsJulianLeapYear(int year)
    {
        return year % 4 == 0;
    }
}
