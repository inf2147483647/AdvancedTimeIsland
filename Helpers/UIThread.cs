using System;
using System.Windows;
using System.Windows.Threading;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// WPF UI 线程辅助类。插件内部统一通过本类访问 UI 线程调度器，
/// 替代原 Avalonia 版中的 <c>Dispatcher.UIThread</c>。
/// </summary>
public static class UIThread
{
    /// <summary>
    /// 获取当前 WPF 应用的主线程调度器；应用尚未初始化时退回当前线程调度器。
    /// </summary>
    public static Dispatcher Dispatcher =>
        Application.Current?.Dispatcher ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;

    /// <summary>
    /// 在 UI 线程上异步执行操作。
    /// </summary>
    public static void Post(Action action)
    {
        Dispatcher.BeginInvoke(action);
    }

    /// <summary>
    /// 在 UI 线程上同步执行操作（若已在 UI 线程则直接执行）。
    /// </summary>
    public static void Invoke(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.Invoke(action);
        }
    }

    /// <summary>
    /// 在 UI 线程上异步执行操作并返回任务。
    /// </summary>
    public static System.Threading.Tasks.Task InvokeAsync(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
            return System.Threading.Tasks.Task.CompletedTask;
        }
        return Dispatcher.InvokeAsync(action).Task;
    }
}
