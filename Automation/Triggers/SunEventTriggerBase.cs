using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Triggers;

/// <summary>
/// 日出 / 日落触发器的公共实现：按主界面「日出日落」组件给出的时间，每天触发一次。
/// </summary>
public abstract class SunEventTriggerBase : TimeTriggerBase<SunEventTriggerSettings>
{
    private DateTime _lastTriggeredDate = DateTime.MinValue;

    /// <summary>true 表示取日出时间，false 表示取日落时间。</summary>
    protected abstract bool IsSunrise { get; }

    protected override bool CheckTrigger(DateTime now)
    {
        if (_lastTriggeredDate.Date == now.Date)
        {
            return false;
        }

        var times = SunriseSunsetProvider.Calculate(now);
        var target = IsSunrise ? times.Sunrise : times.Sunset;

        // 极昼 / 极夜时计算器会返回 0 或 24 小时，表示当天没有对应事件。
        if (target <= TimeSpan.Zero || target >= TimeSpan.FromDays(1))
        {
            return false;
        }

        if (now.Hour != target.Hours || now.Minute != target.Minutes)
        {
            return false;
        }

        _lastTriggeredDate = now;
        return true;
    }
}
