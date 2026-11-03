using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "e7f5c1a2-3b4d-4e6f-8a90-1b2c3d4e5f01",
    "农历日期（ATI）",
    "\uE354",
    "在主界面显示今日的农历日期，形如“农历 丙午年正月十五”。"
)]
public class LunarDateControl : DailyTextComponentBase<LunarDateSettings>
{
    public LunarDateControl(TimeBaseService timeBaseService) : base(timeBaseService)
    {
    }

    protected override string BuildText(DateTime now)
    {
        // 复用 lunar.dll（经 LunarHelper 封装）的农历转换与格式化，避免在组件里重复拼装月/日名。
        return $"农历 {LunarHelper.SolarToLunar(now)}";
    }
}
