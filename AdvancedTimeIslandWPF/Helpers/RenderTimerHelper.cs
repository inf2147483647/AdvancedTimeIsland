using System;
using System.Windows.Media;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 渲染帧计数辅助类（WPF 版）。
/// WPF 没有 Avalonia 那样的 RenderTimer 内部 FPS 属性，因此：
/// - <see cref="GetFps"/> 始终返回 null，提示调用方回退到帧计数法；
/// - <see cref="SubscribeTick"/> 基于 <see cref="CompositionTarget.Rendering"/> 事件统计渲染帧数。
/// </summary>
public static class RenderTimerHelper
{
    public static bool IsAvailable => true;

    public static bool HasFpsProperty => false;

    /// <summary>
    /// WPF 无内部 FPS 属性，返回 null。
    /// </summary>
    public static double? GetFps(object? window)
    {
        return null;
    }

    /// <summary>
    /// 订阅渲染帧回调。每个渲染帧调用一次 <paramref name="callback"/>。
    /// </summary>
    public static IDisposable? SubscribeTick(object? window, Action<TimeSpan> callback)
    {
        if (callback == null) return null;

        EventHandler handler = (_, _) =>
        {
            try
            {
                callback(TimeSpan.Zero);
            }
            catch
            {
            }
        };
        CompositionTarget.Rendering += handler;
        return new RenderTimerSubscription(handler);
    }

    private class RenderTimerSubscription : IDisposable
    {
        private readonly EventHandler _handler;
        private bool _isDisposed;

        public RenderTimerSubscription(EventHandler handler)
        {
            _handler = handler;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            try
            {
                CompositionTarget.Rendering -= _handler;
            }
            catch
            {
            }
        }
    }
}
