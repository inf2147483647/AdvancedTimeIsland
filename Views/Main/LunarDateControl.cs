using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "e7f5c1a2-3b4d-4e6f-8a90-1b2c3d4e5f01",
    "农历日期（ATI）",
    "\uE067",
    "在主界面显示今日的农历日期，形如“农历 丙午年正月十五”。"
)]
public class LunarDateControl : DailyTextComponentBase<LunarDateSettings>
{
    public LunarDateControl(TimeBaseService timeBaseService) : base(timeBaseService)
    {
    }

    protected override string BuildText(DateTime now)
    {
        var lunar = LunarHelper.SolarToLunar(now);
        return $"农历 {lunar.YearGanZhi}年{lunar.MonthInChinese}{lunar.DayInChinese}";
    }
}
