using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

/// <summary>
/// 液态玻璃采样的**高精度节拍器**：后台线程按目标频率喊拍子。
/// 【为什么不用 DispatcherTimer】其 Windows 后端本质是 SetTimer / WM_TIMER，粒度约 15.6ms，
///   目标 240Hz（4.17ms）会被硬卡到 60Hz 上下，用户把采样率调到 240 也毫无变化。
/// 【做法】timeBeginPeriod(1) 把系统计时精度提到 1ms → Stopwatch 记账 →「粗睡 + 最后 <1ms 自旋」补齐，
///   调度抖动约 1ms，足够支撑 240Hz；单帧耗时超过间隔时直接对齐到当前时刻，不做"追赶爆发"。
/// 【边界】只负责"到点喊一声"，回调里必须立刻返回（真正的抓帧/折射由调用方投递到自己的线程）。
/// </summary>
internal sealed class FloatScheduleHighResTicker : IDisposable
{
    const uint TimerResolutionMs = 1;
    const double SpinWindowMs = 0.6;   // 最后这段改用自旋，绕开 Sleep 的 1ms 量化误差

    readonly Func<double> _rateHz;
    readonly Action _onTick;
    readonly CancellationTokenSource _cts = new();
    Thread? _thread;

    /// <param name="rateHz">每次拍子重新取值 → 采样率设置变更后热跟随，无需重建节拍器。</param>
    /// <param name="onTick">拍子回调（在后台线程执行，必须快速返回）。</param>
    public FloatScheduleHighResTicker(Func<double> rateHz, Action onTick)
    {
        _rateHz = rateHz;
        _onTick = onTick;
        _thread = new Thread(Loop) { IsBackground = true, Name = "FloatScheduleGlassTicker" };
        _thread.Start();
    }

    void Loop()
    {
        bool raised = false;
        try
        {
            // Sleep(1) 只有在 1ms 计时精度下才会真的睡 1ms（默认精度下会被抬到 15.6ms）
            try { raised = timeBeginPeriod(TimerResolutionMs) == 0; } catch { }

            long start = Stopwatch.GetTimestamp();
            double nextMs = 0;
            while (!_cts.IsCancellationRequested)
            {
                try { _onTick(); } catch { }

                double hz = Math.Clamp(_rateHz(), 1, 240);
                double intervalMs = 1000.0 / hz;
                nextMs += intervalMs;
                double nowMs = ElapsedMs(start);
                // 落后超过一整帧（单帧抓帧耗时比间隔还长）→ 直接对齐到现在，避免连续补拍把 CPU 打满
                if (nextMs < nowMs - intervalMs) nextMs = nowMs;
                WaitUntil(start, nextMs);
            }
        }
        finally
        {
            if (raised) { try { timeEndPeriod(TimerResolutionMs); } catch { } }
        }
    }

    static double ElapsedMs(long startTicks)
        => (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;

    void WaitUntil(long start, double targetMs)
    {
        double left = targetMs - ElapsedMs(start);
        if (left > SpinWindowMs + 1.0)
        {
            int coarse = (int)(left - SpinWindowMs);
            if (coarse > 0) _cts.Token.WaitHandle.WaitOne(coarse);   // 可被 Dispose 立即唤醒
        }
        while (!_cts.IsCancellationRequested && targetMs - ElapsedMs(start) > 0)
            Thread.SpinWait(80);
    }

    public void Dispose()
    {
        try { _cts.Cancel(); } catch { }
        try { _thread?.Join(500); } catch { }
        _thread = null;
        try { _cts.Dispose(); } catch { }
    }

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    static extern uint timeEndPeriod(uint uPeriod);
}
