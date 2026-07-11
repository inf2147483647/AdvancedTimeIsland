using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AdvancedTimeIsland.Helpers;

public static class RenderTimerHelper
{
    private static readonly Assembly? _avaloniaBaseAssembly = typeof(TopLevel).Assembly;
    private static readonly Assembly? _avaloniaControlsAssembly = typeof(Control).Assembly;

    private static Type? _iRenderTimerType;
    private static EventInfo? _tickEvent;
    private static Func<TopLevel, object?>? _getRenderTimerFunc;
    private static bool _initialized;
    private static bool _isAvailable;

    private static Func<TopLevel, double>? _getFpsFunc;
    private static bool _fpsPropertyFound;

    public static bool IsAvailable
    {
        get
        {
            EnsureInitialized();
            return _isAvailable;
        }
    }

    public static bool HasFpsProperty
    {
        get
        {
            EnsureInitialized();
            return _fpsPropertyFound;
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _iRenderTimerType = FindIRenderTimerType();
            if (_iRenderTimerType == null) return;

            _tickEvent = _iRenderTimerType.GetEvent("Tick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (_tickEvent == null) return;

            _getRenderTimerFunc = BuildGetRenderTimerFunc();
            if (_getRenderTimerFunc == null) return;

            _isAvailable = true;
        }
        catch
        {
            _isAvailable = false;
        }
    }

    private static Type? FindIRenderTimerType()
    {
        var typeNames = new[]
        {
            "Avalonia.Rendering.IRenderTimer",
            "Avalonia.Rendering.RenderTimer",
            "Avalonia.Rendering.IMediaContext",
        };

        foreach (var asm in new[] { _avaloniaBaseAssembly, _avaloniaControlsAssembly })
        {
            if (asm == null) continue;
            foreach (var typeName in typeNames)
            {
                var type = asm.GetType(typeName);
                if (type != null && type.GetEvent("Tick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                {
                    return type;
                }
            }
        }

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsInterface && type.Name.Contains("RenderTimer"))
                    {
                        if (type.GetEvent("Tick") != null)
                            return type;
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static Func<TopLevel, object?>? BuildGetRenderTimerFunc()
    {
        var accessorPaths = new List<Func<TopLevel, object?>>
        {
            tl =>
            {
                var prop = typeof(TopLevel).GetProperty("Renderer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null) return null;
                var renderer = prop.GetValue(tl);
                if (renderer == null) return null;
                var rlProp = renderer.GetType().GetProperty("RenderLoop", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (rlProp == null) return null;
                var renderLoop = rlProp.GetValue(renderer);
                if (renderLoop == null) return null;
                var timerProp = renderLoop.GetType().GetProperty("Timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (timerProp == null) return null;
                return timerProp.GetValue(renderLoop);
            },
            tl =>
            {
                var prop = typeof(TopLevel).GetProperty("PlatformImpl", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null) return null;
                var impl = prop.GetValue(tl);
                if (impl == null) return null;
                var timerProp = impl.GetType().GetProperty("RenderTimer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (timerProp == null) return null;
                return timerProp.GetValue(impl);
            },
            tl =>
            {
                var field = typeof(Dispatcher).GetField("s_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var dispatcher = field?.GetValue(null) ?? Dispatcher.UIThread;
                var timerProp = dispatcher?.GetType().GetProperty("RenderTimer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (timerProp == null) return null;
                return timerProp.GetValue(dispatcher);
            }
        };

        return topLevel =>
        {
            foreach (var accessor in accessorPaths)
            {
                try
                {
                    var timer = accessor(topLevel);
                    if (timer != null && _iRenderTimerType!.IsAssignableFrom(timer.GetType()))
                    {
                        return timer;
                    }
                }
                catch
                {
                }
            }
            return null;
        };
    }

    public static IDisposable? SubscribeTick(TopLevel topLevel, Action<TimeSpan> callback)
    {
        if (!IsAvailable) return null;
        if (topLevel == null) return null;

        try
        {
            var timer = _getRenderTimerFunc!(topLevel);
            if (timer == null) return null;

            var handler = new EventHandler<TimeSpan>((sender, ts) =>
            {
                try
                {
                    callback(ts);
                }
                catch
                {
                }
            });

            _tickEvent!.AddEventHandler(timer, handler);
            return new RenderTimerSubscription(timer, handler, _tickEvent);
        }
        catch
        {
            return null;
        }
    }

    public static double? GetFps(TopLevel topLevel)
    {
        if (topLevel == null) return null;

        try
        {
            if (_getFpsFunc == null)
            {
                _getFpsFunc = BuildGetFpsFunc(topLevel);
                _fpsPropertyFound = _getFpsFunc != null;
            }

            return _getFpsFunc?.Invoke(topLevel);
        }
        catch
        {
            return null;
        }
    }

    private static Func<TopLevel, double>? BuildGetFpsFunc(TopLevel topLevel)
    {
        var renderer = typeof(TopLevel).GetProperty("Renderer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(topLevel);
        if (renderer == null) return null;

        var fpsProps = renderer.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.Name.Contains("Fps") || p.Name.Contains("FrameRate") || p.Name.Contains("RenderFps"))
            .Where(p => p.PropertyType == typeof(double) || p.PropertyType == typeof(int));

        foreach (var prop in fpsProps)
        {
            try
            {
                var value = prop.GetValue(renderer);
                if (value != null)
                {
                    return tl =>
                    {
                        var r = typeof(TopLevel).GetProperty("Renderer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(tl);
                        if (r == null) return 0;
                        var val = prop.GetValue(r);
                        return val is double d ? d : (val is int i ? (double)i : 0);
                    };
                }
            }
            catch
            {
            }
        }

        var renderLoop = renderer.GetType().GetProperty("RenderLoop", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(renderer);
        if (renderLoop != null)
        {
            fpsProps = renderLoop.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.Name.Contains("Fps") || p.Name.Contains("FrameRate") || p.Name.Contains("RenderFps"))
                .Where(p => p.PropertyType == typeof(double) || p.PropertyType == typeof(int));

            foreach (var prop in fpsProps)
            {
                try
                {
                    var value = prop.GetValue(renderLoop);
                    if (value != null)
                    {
                        return tl =>
                        {
                            var r = typeof(TopLevel).GetProperty("Renderer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(tl);
                            if (r == null) return 0;
                            var rl = r.GetType().GetProperty("RenderLoop", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(r);
                            if (rl == null) return 0;
                            var val = prop.GetValue(rl);
                            return val is double d ? d : (val is int i ? (double)i : 0);
                        };
                    }
                }
                catch
                {
                }
            }
        }

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in asm.GetTypes())
                {
                    if (!type.Name.Contains("Renderer") && !type.Name.Contains("RenderLoop")) continue;

                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(p => p.Name.Contains("Fps") || p.Name.Contains("FrameRate") || p.Name.Contains("RenderFps"))
                        .Where(p => p.PropertyType == typeof(double) || p.PropertyType == typeof(int));

                    foreach (var prop in props)
                    {
                        try
                        {
                            return tl =>
                            {
                                var r = typeof(TopLevel).GetProperty("Renderer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(tl);
                                if (r == null || !type.IsAssignableFrom(r.GetType())) return 0;
                                var val = prop.GetValue(r);
                                return val is double d ? d : (val is int i ? (double)i : 0);
                            };
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private class RenderTimerSubscription : IDisposable
    {
        private readonly object _timer;
        private readonly Delegate _handler;
        private readonly EventInfo _tickEvent;
        private bool _isDisposed;

        public RenderTimerSubscription(object timer, Delegate handler, EventInfo tickEvent)
        {
            _timer = timer;
            _handler = handler;
            _tickEvent = tickEvent;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _tickEvent.RemoveEventHandler(_timer, _handler);
            }
            catch
            {
            }
        }
    }
}
