using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "e7f5c1a2-3b4d-4e6f-8a90-1b2c3d4e5f02",
    "本周单双（ATI）",
    "\uE2B9",
    "显示本周在学期中的单双性（以ClassIsland学期开始日为第一周）"
)]
public class WeekParityControl : DailyTextComponentBase<WeekParitySettings>
{
    public WeekParityControl(TimeBaseService timeBaseService) : base(timeBaseService)
    {
    }

    // 学期开始日可能在运行中变化，不做日期缓存，每次时钟回调都重新计算。
    protected override bool UseDateCache => false;

    protected override string BuildText(DateTime now)
    {
        var start = SemesterStartService.CurrentSemesterStartDate;
        if (!start.HasValue)
        {
            return "--";
        }

        var weekNumber = WeekNumberHelper.GetSemesterWeekNumber(now, start.Value);
        if (weekNumber <= 0)
        {
            return "--";
        }

        return weekNumber % 2 == 1 ? "单周" : "双周";
    }
}
