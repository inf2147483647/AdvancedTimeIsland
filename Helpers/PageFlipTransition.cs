using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 标签页切换的「翻页」过场：旧页向一侧滑出并淡出、新页从另一侧滑入并淡入。
/// 时长与缓动参考 ClassIsland（档案编辑页导航栏切换）所用的翻页动画：
/// 250ms、KeySpline(0,0)(0,1)。
/// <para>
/// 之所以自己实现而不是直接用框架/主题的过渡：TabControl 会用同一批评为演示器反复承载各标签页，
/// 若过场动画残留在演示器上（动画被“挂住”或晚到），后续任何显式赋值都会被它覆盖，
/// 表现为“切过去整页空白”。ClassIsland 的做法是在过渡结束 / 被打断后显式复位双方的视觉状态；
/// 本实现同样在结束时精确复位（位移归零、不透明度还原），并额外做了两点：
/// 一是改用布局线程上的手动插值时钟（不依赖合成器动画的异步批次），
/// 二是用代次（generation）守卫，避免旧的过渡把新过渡刚设好的视觉状态改回去。
/// </para>
/// </summary>
public sealed class PageFlipTransition : IPageTransition
{
    /// <summary>已被各过渡认领的视觉 → 代次；用于判断某次复位的“所有权”是否已被更新的切换接手。</summary>
    private static readonly ConditionalWeakTable<Visual, GenerationBox> Generations = new();

    private static long _currentGeneration;

    /// <summary>过渡时长。</summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>缓动曲线（KeySpline(0,0)(0,1)：先慢后快收尾，与 ClassIsland 翻页一致）。</summary>
    public Easing SlideEasing { get; set; } = Easing.Parse("0,0,0,1");

    /// <summary>
    /// 每帧间隔（毫秒）：与工具集其它动画一致，按约 60fps 手动插值。
    /// </summary>
    private const double FrameIntervalMs = 16;

    /// <inheritdoc />
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (from == null || to == null)
        {
            // 没有可对比的另一页（首次显示等）：直接切换可见性
            if (from != null)
            {
                from.IsVisible = false;
            }

            if (to != null)
            {
                to.IsVisible = true;
            }

            return;
        }

        var distance = (from.GetVisualParent() ?? to.GetVisualParent())?.Bounds.Width
                       ?? Math.Max(from.Bounds.Width, to.Bounds.Width);

        if (double.IsNaN(distance) || distance <= 0)
        {
            // 取不到可用宽度（布局尚未完成）：直接切换，避免做无意义的位移
            from.IsVisible = false;
            to.IsVisible = true;
            return;
        }

        // 记录原始位移，过渡结束 / 被打断时精确复位
        var fromPreviousTransform = from.RenderTransform;
        var toPreviousTransform = to.RenderTransform;
        var fromExisting = fromPreviousTransform as TranslateTransform;
        var toExisting = toPreviousTransform as TranslateTransform;
        var fromTransform = fromExisting ?? new TranslateTransform();
        var toTransform = toExisting ?? new TranslateTransform();
        var fromBaseX = fromExisting?.X ?? 0;
        var toBaseX = toExisting?.X ?? 0;

        to.IsVisible = true;
        from.RenderTransform = fromTransform;
        to.RenderTransform = toTransform;

        // 认领这两个演示器：若之后有更新的切换接手，本次过渡不再改动它们的视觉状态
        var generation = Interlocked.Increment(ref _currentGeneration);
        Generations.GetOrCreateValue(from).Value = generation;
        Generations.GetOrCreateValue(to).Value = generation;

        var durationMs = Math.Max(1, Duration.TotalMilliseconds);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var elapsedMs = (Stopwatch.GetTimestamp() - startedAt) * 1000.0 / Stopwatch.Frequency;
                var progress = Math.Clamp(elapsedMs / durationMs, 0.0, 1.0);
                var eased = SlideEasing.Ease(progress);
                var offset = distance * eased;

                // 旧页滑出、新页滑入，同时交叉淡入淡出
                fromTransform.X = forward ? fromBaseX - offset : fromBaseX + offset;
                toTransform.X = forward ? toBaseX + distance - offset : toBaseX - distance + offset;
                from.Opacity = 1 - eased;
                to.Opacity = eased;

                if (progress >= 1.0)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(FrameIntervalMs), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 被更新的切换打断：走下面的复位
        }
        finally
        {
            RestoreVisualIfOwned(from, generation, fromPreviousTransform, fromTransform, fromBaseX);
            RestoreVisualIfOwned(to, generation, toPreviousTransform, toTransform, toBaseX);
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;
        }
    }

    /// <summary>
    /// 精确复位：位移还原、不透明度还原为 1、恢复原有 RenderTransform。
    /// 演示器会被 TabControl 复用，残留位移 / 不透明度就会表现为“整页空白”。
    /// 注意不透明度要还原为 1 而不是“动画途中捕获到的值”：被打断时捕获到的可能是中间态，
    /// 把它固化下来会让页面永久半透明。
    /// </summary>
    private static void RestoreVisualIfOwned(
        Visual visual,
        long generation,
        ITransform? previousTransform,
        TranslateTransform transform,
        double baseX)
    {
        // 已被更新的一次切换接手：不要再改动它正在使用的视觉状态
        if (Generations.TryGetValue(visual, out var box) && box.Value != generation)
        {
            return;
        }

        transform.X = baseX;
        visual.Opacity = 1;
        visual.RenderTransform = previousTransform;
    }

    private sealed class GenerationBox
    {
        public long Value;
    }
}
