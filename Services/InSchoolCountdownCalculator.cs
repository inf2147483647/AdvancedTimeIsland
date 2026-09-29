using System;
using System.Collections.Generic;
using System.Threading;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 「仅计在校时长」倒计时的剩余时间计算器。
/// 剩余时间只累计在校时段：每个在校日内「当天课表第一节课开始 → 最后一节课下课」之间的秒数；
/// 在校日判定沿用「在校时间统计」（设置 → AdvancedTimeIsland → 在校时间统计）的口径
/// （周末 / 法定节假日 / 调休补班 / 寒暑假 / 自定义计入·排除日期）。
/// 逐日课表窗口通过宿主反射查询（开销大），因此按日期区间缓存、后台线程重建；
/// UI 时钟 Tick 只做轻量的区间重叠累加。
/// </summary>
public sealed class InSchoolCountdownCalculator
{
    /// <summary>单日在校窗口：HasWindow=false 表示该日不计在校时长（非在校日或无课表）。</summary>
    public readonly struct DayWindow
    {
        public DayWindow(bool hasWindow, TimeSpan start, TimeSpan end)
        {
            HasWindow = hasWindow;
            Start = start;
            End = end;
        }

        public bool HasWindow { get; }
        public TimeSpan Start { get; }
        public TimeSpan End { get; }
    }

    /// <summary>区间扫描最大跨度（天），与 AttendanceStatisticsHelper.MaxSpanDays 同口径，防误配置超长遍历。</summary>
    private const int MaxSpanDays = 3650;

    /// <summary>缓存有效期：档案（课表）被编辑不一定触发数据变更事件，靠 TTL 定期后台重建兜底。</summary>
    private const int CacheTtlMs = 10 * 60 * 1000;

    private readonly AttendanceCalendarService _calendar;

    // volatile 整体替换：BuildRange 在后台线程构建新字典后一次性发布，UI 线程只读旧/新完整快照，无锁。
    private volatile Dictionary<DateTime, DayWindow> _windows = new();
    private DateTime _rangeStart = DateTime.MaxValue;
    private DateTime _rangeEnd = DateTime.MinValue;
    private long _builtStamp = -1;
    private long _builtTicks;

    public InSchoolCountdownCalculator(AttendanceCalendarService calendar)
    {
        _calendar = calendar;
    }

    /// <summary>缓存是否覆盖 [dayStart, dayEnd]、与给定数据快照戳一致、且未超过有效期。</summary>
    public bool IsCacheValid(DateTime dayStart, DateTime dayEnd, long stamp)
    {
        // 先读版本号（volatile）再读其余字段：Volatile.Write 具释放语义，保证读到匹配版本时
        //   _windows / _rangeStart / _rangeEnd / _builtTicks 都是该次发布的完整快照。
        if (Volatile.Read(ref _builtStamp) != stamp) return false;
        if (Environment.TickCount64 - Volatile.Read(ref _builtTicks) >= CacheTtlMs) return false;
        return _rangeStart <= dayStart && _rangeEnd >= dayEnd;
    }

    /// <summary>
    /// 重建 [dayStart, dayEnd] 的逐日在校窗口缓存。包含宿主反射查询，须在后台线程调用；
    /// 完成后以 Interlocked/Volatile 语义发布新字典供 UI 线程读取。
    /// </summary>
    public void BuildRange(DateTime dayStart, DateTime dayEnd, long stamp)
    {
        var start = dayStart.Date;
        var end = dayEnd.Date;
        if (end < start)
        {
            (start, end) = (end, start);
        }
        if ((end - start).TotalDays > MaxSpanDays)
        {
            end = start.AddDays(MaxSpanDays);
        }

        var config = _calendar.Data.Config;
        var data = _calendar.Data;
        var map = new Dictionary<DateTime, DayWindow>();

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            // 在校日判定沿用「在校时间统计」口径；非在校日整日不计。
            if (!AttendanceStatisticsHelper.IsInSchoolDay(date, config, data))
            {
                map[date] = default;
                continue;
            }
            // 在校时段窗口 = 当天课表首课开始 → 末课下课；无课表/无启用课程 → 不计。
            if (_calendar.TryGetScheduleWindow(date, out var s, out var e))
            {
                map[date] = new DayWindow(true, s, e);
            }
            else
            {
                map[date] = default;
            }
        }

        _windows = map;
        _rangeStart = start;
        _rangeEnd = end;
        Volatile.Write(ref _builtTicks, Environment.TickCount64);
        Volatile.Write(ref _builtStamp, stamp);
    }

    /// <summary>
    /// 用当前缓存计算 (now, target] 区间内的在校秒数。
    /// 缓存未覆盖或尚未建立时返回值不可信，调用方须先用 <see cref="IsCacheValid"/> 校验。
    /// </summary>
    public double ComputeRemainingSeconds(DateTime now, DateTime target)
    {
        if (target <= now) return 0;
        var windows = _windows;
        double total = 0;
        for (var d = now.Date; d <= target.Date; d = d.AddDays(1))
        {
            if (!windows.TryGetValue(d, out var w) || !w.HasWindow) continue;
            var winStart = d + w.Start;
            var winEnd = d + w.End;
            var segStart = d == now.Date && now > winStart ? now : winStart;
            var segEnd = d == target.Date && target < winEnd ? target : winEnd;
            if (segEnd > segStart)
            {
                total += (segEnd - segStart).TotalSeconds;
            }
        }
        return total;
    }
}
