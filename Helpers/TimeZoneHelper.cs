using System;
using Avalonia.Controls;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 时区下拉框的通用逻辑（下拉项均为 <see cref="TimeZoneInfo"/>）。
/// </summary>
public static class TimeZoneHelper
{
    /// <summary>
    /// 按设置中的时区 Id 反显选中项。Id 缺失或本机不存在时（旧档案遗留的非法值、
    /// 跨系统导入的档案等）依次回退到 <paramref name="fallbackTimeZoneId"/>、列表第一项，
    /// 避免下拉框留空导致「反显值」与「运行时时区」不一致。
    /// </summary>
    /// <param name="comboBox">已填充 <see cref="TimeZoneInfo"/> 项的时区下拉框。</param>
    /// <param name="timeZoneId">要反显的时区 Id。</param>
    /// <param name="fallbackTimeZoneId">找不到时回退使用的时区 Id。</param>
    /// <returns>最终选中的时区；下拉项为空时返回 <c>null</c>。</returns>
    public static TimeZoneInfo? SelectTimeZone(ComboBox comboBox, string? timeZoneId, string fallbackTimeZoneId)
    {
        ArgumentNullException.ThrowIfNull(comboBox);

        var selected = FindById(comboBox, timeZoneId) ?? FindById(comboBox, fallbackTimeZoneId);
        if (selected != null)
        {
            comboBox.SelectedItem = selected;
            return selected;
        }

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
            return comboBox.SelectedItem as TimeZoneInfo;
        }

        return null;
    }

    private static TimeZoneInfo? FindById(ComboBox comboBox, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        foreach (var item in comboBox.Items)
        {
            if (item is TimeZoneInfo timeZone && timeZone.Id == timeZoneId)
            {
                return timeZone;
            }
        }

        return null;
    }
}
