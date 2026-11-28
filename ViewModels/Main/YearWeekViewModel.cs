using System;
using System.ComponentModel;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.ViewModels.Main;

/// <summary>
/// 今日周数（年）视图模型。
/// </summary>
public class YearWeekViewModel : WeekViewModelBase<YearWeekSettings>
{
    public YearWeekViewModel(TimeBaseService timeBaseService, YearWeekSettings settings,
        Action<string>? updateFontColor = null, Action<double>? updateFontSize = null)
        : base(timeBaseService, settings, updateFontColor, updateFontSize)
    {
        _settings.PropertyChanged += OnSettingsChanged;
    }

    protected override int ComputeWeekNumber(DateTime now) =>
        WeekNumberHelper.GetYearWeekNumber(now, _settings.FirstDayOfWeek);

    protected override string BuildDisplayText(int weekNumber) => $"第 {weekNumber} 周";

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(YearWeekSettings.FontColor) ||
            e.PropertyName == nameof(YearWeekSettings.EnableCustomFontColor))
        {
            NotifyFontColor(_settings.FontColor);
        }
        else if (e.PropertyName == nameof(YearWeekSettings.FontSize) ||
                 e.PropertyName == nameof(YearWeekSettings.EnableCustomFontSize))
        {
            NotifyFontSize(_settings.EnableCustomFontSize ? _settings.FontSize : 0);
        }
        else if (e.PropertyName == nameof(YearWeekSettings.FirstDayOfWeek) ||
                 e.PropertyName == nameof(YearWeekSettings.TimeBaseType))
        {
            // 更改一周第一天或时间基准后立即刷新显示。
            TriggerAsyncUpdate();
        }
    }

    public override void Dispose()
    {
        _settings.PropertyChanged -= OnSettingsChanged;
        base.Dispose();
    }
}