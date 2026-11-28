using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 输入校验失败时的定位提示：把出错的控件临时标红，并给出具体的错误原因。
/// </summary>
public static class InvalidInputHighlighter
{
    // 标红持续时间
    private static readonly TimeSpan FlashDuration = TimeSpan.FromMilliseconds(1200);
    private static readonly SolidColorBrush InvalidBorderBrush = new(Color.FromRgb(0xE8, 0x11, 0x23));
    private static readonly Thickness InvalidBorderThickness = new(2);

    // 每个控件当前的标红批次号，连续触发时只由最后一次负责恢复原样式
    private static readonly Dictionary<TemplatedControl, int> FlashTokens = new();

    /// <summary>
    /// 校验日期时间输入（年 / 月 / 日 / 时间），返回**所有**与正确输入不符的控件及原因；输入全部合法时返回空列表。
    /// </summary>
    public static List<(Control? Control, string Reason)> ValidateDateTimeInput(
        NumericUpDown? yearTextBox,
        ComboBox? monthComboBox,
        ComboBox? dayComboBox,
        TimePicker? timePicker)
    {
        var errors = new List<(Control? Control, string Reason)>();

        int? year = null;
        if (yearTextBox?.Value == null)
            errors.Add((yearTextBox, "年份未填写"));
        else if (yearTextBox.Value.Value < 1 || yearTextBox.Value.Value > 9999)
            errors.Add((yearTextBox, "年份需在 1-9999 之间"));
        else
            year = (int)yearTextBox.Value.Value;

        int? month = null;
        if (monthComboBox?.SelectedItem == null)
            errors.Add((monthComboBox, "月份未选择"));
        else if (!int.TryParse(monthComboBox.SelectedItem.ToString()?.Replace("月", ""), out var parsedMonth) || parsedMonth < 1 || parsedMonth > 12)
            errors.Add((monthComboBox, "月份无效"));
        else
            month = parsedMonth;

        int? day = null;
        if (dayComboBox?.SelectedItem == null)
            errors.Add((dayComboBox, "日期未选择"));
        else if (!int.TryParse(dayComboBox.SelectedItem.ToString()?.Replace("日", ""), out var parsedDay) || parsedDay < 1)
            errors.Add((dayComboBox, "日期无效"));
        else
            day = parsedDay;

        // 仅当年月日都可用时，才能判断该月天数与 1582 年 10 月的空缺
        if (year.HasValue && month.HasValue && day.HasValue)
        {
            var daysInMonth = Lunar.Util.SolarUtil.GetDaysOfMonth(year.Value, month.Value);
            if (day.Value > daysInMonth)
                errors.Add((dayComboBox, $"{year.Value}年{month.Value}月只有{daysInMonth}天"));
            else if (DateValidationHelper.IsInvalidGregorianTransitionDate(year.Value, month.Value, day.Value))
                errors.Add((dayComboBox, "1582年10月5日至14日在历史上不存在"));
        }

        if (timePicker?.SelectedTime == null)
        {
            errors.Add((timePicker, "时间未选择"));
        }
        else
        {
            var time = timePicker.SelectedTime.Value;
            if (time.Hours is < 0 or > 23 || time.Minutes is < 0 or > 59 || time.Seconds is < 0 or > 59)
                errors.Add((timePicker, "时间无效"));
        }

        return errors;
    }

    /// <summary>
    /// 把指定控件标红并保持 1.2 秒，随后恢复原有的主题样式。
    /// </summary>
    public static async void Flash(Control? control)
    {
        if (control is not TemplatedControl templatedControl)
            return;

        var token = FlashTokens.TryGetValue(templatedControl, out var previous) ? previous + 1 : 1;
        FlashTokens[templatedControl] = token;

        templatedControl.BorderBrush = InvalidBorderBrush;
        templatedControl.BorderThickness = InvalidBorderThickness;

        await Task.Delay(FlashDuration);

        // 期间又被触发了一次标红，交给最新的那次负责恢复
        if (!FlashTokens.TryGetValue(templatedControl, out var current) || current != token)
            return;

        FlashTokens.Remove(templatedControl);
        templatedControl.ClearValue(TemplatedControl.BorderBrushProperty);
        templatedControl.ClearValue(TemplatedControl.BorderThicknessProperty);
    }
}
