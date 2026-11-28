using System;
using System.ComponentModel;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.ViewModels.Main;

/// <summary>
/// 今日周数（学期）视图模型。
/// </summary>
public class SemesterWeekViewModel : WeekViewModelBase<SemesterWeekSettings>
{
    public SemesterWeekViewModel(TimeBaseService timeBaseService, SemesterWeekSettings settings,
        Action<string>? updateFontColor = null, Action<double>? updateFontSize = null)
        : base(timeBaseService, settings, updateFontColor, updateFontSize)
    {
        _settings.PropertyChanged += OnSettingsChanged;
        SemesterStartService.SemesterStartDateChanged += OnSemesterStartDateChanged;
    }

    protected override int ComputeWeekNumber(DateTime now)
    {
        var start = SemesterStartService.CurrentSemesterStartDate;
        if (!start.HasValue) return 0;
        return WeekNumberHelper.GetSemesterWeekNumber(now, start.Value);
    }

    protected override string BuildDisplayText(int weekNumber)
    {
        if (SemesterStartService.CurrentSemesterStartDate == null)
        {
            return "--";
        }
        return $"第 {weekNumber} 周";
    }

    private void OnSemesterStartDateChanged(object? sender, EventArgs e)
    {
        TriggerAsyncUpdate();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SemesterWeekSettings.FontColor) ||
            e.PropertyName == nameof(SemesterWeekSettings.EnableCustomFontColor))
        {
            NotifyFontColor(_settings.FontColor);
        }
        else if (e.PropertyName == nameof(SemesterWeekSettings.FontSize) ||
                 e.PropertyName == nameof(SemesterWeekSettings.EnableCustomFontSize))
        {
            NotifyFontSize(_settings.EnableCustomFontSize ? _settings.FontSize : 0);
        }
        else if (e.PropertyName == nameof(SemesterWeekSettings.TimeBaseType))
        {
            // 更改时间基准后立即刷新显示。
            TriggerAsyncUpdate();
        }
    }

    public override void Dispose()
    {
        _settings.PropertyChanged -= OnSettingsChanged;
        SemesterStartService.SemesterStartDateChanged -= OnSemesterStartDateChanged;
        base.Dispose();
    }
}