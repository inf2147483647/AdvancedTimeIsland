// 【临时插桩·诊断 F3】悬浮窗拖动/按下抖动取证：**不驱动渲染帧**，日志在后台线程异步写。
//
// 与已移除的 F2（FloatScheduleFrameProbe）差别：
//   F2 用链式 RequestAnimationFrame 逐帧回调，会把渲染循环强行保持在"热"状态，反而掩盖了抖动（观察者效应）。
//   本实现只用调用方的 DispatcherTimer 采样窗口状态 + 后台线程写 NDJSON，不请求任何动画帧，
//   也不在 UI 线程做文件 I/O，尽量不改变原有渲染节拍。
//
// 日志：%TEMP%\trae-debug\glass-drag-jitter\trae-debug-log-drag-trace.ndjson（TRAE_DEBUG_DIR 可覆盖）。
// 事件：sample（定时采样）、setpos（每次写窗口位置，带来源标签）、press / release。
// 定位后整段移除（含调用方 F3 插桩）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public static class FloatScheduleDragTrace
{
    public static readonly string FilePath = Path.Combine(
        Environment.GetEnvironmentVariable("TRAE_DEBUG_DIR")
            ?? Path.Combine(Path.GetTempPath(), "trae-debug", "glass-drag-jitter"),
        "trae-debug-log-drag-trace.ndjson");

    static readonly object s_lock = new();
    static readonly Queue<string> s_queue = new();
    static Thread? s_thread;

    /// <summary>入队一行 NDJSON（UI 线程零 I/O；异常静默）。</summary>
    public static void Log(string ev, string fields)
    {
        try
        {
            lock (s_lock)
            {
                s_queue.Enqueue("{\"ts\":" + Environment.TickCount64 + ",\"pid\":" + Environment.ProcessId +
                                ",\"ev\":\"" + ev + "\"," + fields + "}");
                if (s_thread == null)
                {
                    s_thread = new Thread(WriterLoop) { IsBackground = true, Name = "ATI-DragTrace" };
                    s_thread.Start();
                }
                Monitor.Pulse(s_lock);
            }
        }
        catch { }
    }

    static void WriterLoop()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var sw = new StreamWriter(fs);
            while (true)
            {
                string? line = null;
                lock (s_lock)
                {
                    if (s_queue.Count == 0) Monitor.Wait(s_lock, 500);
                    if (s_queue.Count > 0) line = s_queue.Dequeue();
                }
                if (line != null)
                {
                    sw.WriteLine(line);
                    sw.Flush();
                }
            }
        }
        catch { /* 插桩自身异常不得影响业务 */ }
    }
}
