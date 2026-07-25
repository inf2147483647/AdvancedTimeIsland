using System;
using AdvancedTimeIsland.Automation.Rules;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Triggers;

[TriggerInfo("advancedtimeisland.unix_timestamp_interval", "绝对时间戳范围间隔触发", "\uecd7")]
public class UnixTimestampIntervalTrigger : TimeTriggerBase<UnixTimestampIntervalTriggerSettings>
{
    protected override bool CheckTrigger(DateTime now)
    {
        var startTimestamp = Settings.StartTimestamp;
        var endTimestamp = Settings.EndTimestamp;
        var interval = Settings.IntervalTimestamp;

        if (interval <= 0)
            return false;

        var currentTimestamp = UnixTimeHelper.ToUnixTimestampDouble(now);

        // 检查当前时间戳是否在范围内
        if (currentTimestamp < startTimestamp || currentTimestamp > endTimestamp)
            return false;

        // 检查是否是间隔的整数倍
        var timeSinceStart = currentTimestamp - startTimestamp;
        var remainder = timeSinceStart % interval;

        // 考虑到浮点数精度问题，使用一个小的容差
        return Math.Abs(remainder) < 0.001 || Math.Abs(remainder - interval) < 0.001;
    }
}
