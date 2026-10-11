using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AdvancedTimeIsland.Shared;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 插件可复用的“华丽动画”工具集（Panel 子元素错峰入场 / 出场 / 横向滑动）。
/// <para>
/// 参数移植自 ClassIsland 的 <c>StackPanelIntroAnimation.axaml</c>（错峰间隔 25ms、单项 750ms、
/// 缓动 0,1,0,1、位移 50px）；实现取自 FemboyTest 的 <c>StackPanelIntroAnimationBehavior</c>，
/// 改为<b>纯代码本地值 + 手动插值时钟</b>：先把子元素写入隐藏态本地值，再用计时器逐帧插值写回，
/// 不依赖 Avalonia 样式动画系统，因此不存在“填充态清除延迟”导致的“先显示最终态再重播”抽动。
/// </para>
/// <para>
/// 与宿主保持一致：当 ClassIsland 动画级别为“禁用”（<see cref="IThemeService.AnimationLevel"/> &lt; 1）时，
/// 所有方法退化为“直接置为最终可见态”，不播放动画；其余档位均播放。
/// 同一时刻只允许一组错峰动画在跑，新调用会取代旧动画
/// （被取代时，前一组元素会被直接落到最终态，不会卡在半透明中途）。
/// </para>
/// <para>
/// 用法：页面变为可见<b>之前</b>调用 <see cref="Prepare"/> 让首帧即为隐藏态（避免闪现），
/// 页面可见时调用 <see cref="PlayAsync(Panel)"/> / <see cref="PlayExitAsync(Panel)"/>。
/// 整面板重载会自动排除自带 <see cref="RenderTransform"/> 的子元素。
/// </para>
/// </summary>
public static class IntroAnimationHelper
{
    private const double HiddenY = -25.0;
    private const double StartY = 50.0;        // 入场起点：位于下方 50px，向上滑入
    private const double ExitY = -50.0;        // 出场终点：继续向上滑出 50px（与入场同方向）
    private const double SlideDistance = 50.0; // 水平入场/出场位移（与纵向 50px 量级一致）

    private static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(25);
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(750);
    private static readonly IEasing IntroEasing = Easing.Parse("0.00,1.00,0.00,1.00");

    // 出场专用节奏：入场缓动 0,1,0,1 在 750ms 上的等效淡入时间约 150ms（x=0.2 时 y≈0.93），
    // 出场若沿用 750ms 会出现“已经看不见却仍在等待”的空档。故出场时长直接对齐到 150ms，
    // 线性缓动让位移与透明度在整段内均匀走完、结束时恰好到位，不吃尾巴。
    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(150);
    private static readonly IEasing ExitEasing = new LinearEasing();

    // 当前动画节奏（同一时刻只有一个动画在跑：RunAsync 写入、OnTick 读取）
    private static TimeSpan _activeDuration = Duration;
    private static IEasing _activeEasing = IntroEasing;

    private sealed class Entry
    {
        public required Control Control;
        public required TranslateTransform Transform;
        public long StartTimestamp;
        public double FromX;
        public double ToX;
        public double FromY;
        public double ToY;
        public double FromOpacity;
        public double ToOpacity;
    }

    private static readonly List<Entry> Entries = new();
    private static DispatcherTimer? _timer;
    private static TaskCompletionSource? _completion;

    /// <summary>当前宿主是否允许播放动画（动画级别非“禁用”）。</summary>
    public static bool IsEnabled => IThemeService.AnimationLevel >= 1;

    /// <summary>
    /// 面板错峰动画的参与元素：可见的直接子元素。
    /// <para>
    /// 注意：本工具会用 <see cref="TranslateTransform"/> 覆盖子元素自身的 <see cref="RenderTransform"/>，
    /// 因此整面板重载只应用于没有自定义变换的普通容器（设置页的根 StackPanel 即属此类）。
    /// </para>
    /// </summary>
    private static IEnumerable<Control> GetTargets(Panel panel)
        => panel.Children.Where(x => x.IsVisible);

    /// <summary>
    /// 把面板内所有可见子元素立即写入“未播放”的隐藏态（Opacity=0、Y=-25）。
    /// 应在页面变为可见之前调用，保证内容可见首帧即为隐藏态。幂等。
    /// </summary>
    public static void Prepare(Panel panel)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var c in GetTargets(panel))
        {
            c.Opacity = 0;
            c.RenderTransform = new TranslateTransform { Y = HiddenY };
        }
    }

    /// <summary>
    /// 播放面板的错峰入场动画：先把子元素写入隐藏态，再立即播放。
    /// 页面加载时调用（建议放在 <c>Loaded</c> 处理器最前面，避免后续初始化逻辑异常时中断）。
    /// </summary>
    public static void PlayIntro(Panel panel)
    {
        if (!IsEnabled)
        {
            return;
        }

        var targets = GetTargets(panel).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        // 先写入隐藏态，保证可见首帧即为隐藏态（避免闪现最终态）
        foreach (var c in targets)
        {
            c.Opacity = 0;
            c.RenderTransform = new TranslateTransform { Y = HiddenY };
        }

        GlobalConstants.HostInterfaces.PluginLogger?.LogInformation(
            "[IntroAnimation] 播放入场动画：目标 {Count} 个，动画级别 {Level}，面板可见 {Visible}",
            targets.Count, IThemeService.AnimationLevel, panel.IsVisible);

        _ = PlayAsync(targets);
    }

    /// <summary>停止并清空进行中的错峰动画（等待中的调用方会立即完成，避免永久挂起）。</summary>
    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;

        // 被取代 / 中止时，把进行中的元素直接落到最终态，
        // 否则前一组元素会永久停在半透明或位移中途（多面板连续播放入场动画时可见）。
        foreach (var entry in Entries)
        {
            entry.Control.Opacity = entry.ToOpacity;
            entry.Transform.X = entry.ToX;
            entry.Transform.Y = entry.ToY;
        }

        Entries.Clear();
        _completion?.TrySetResult();
        _completion = null;
    }

    /// <summary>以 25ms 间隔逐个触发子元素入场动画（自下而上滑入）。不等待动画结束。</summary>
    public static void Play(Panel panel) => _ = PlayAsync(panel);

    /// <summary>入场动画（自下而上，Y 50 → 0 + 淡入），可等待播放完毕。</summary>
    public static Task PlayAsync(Panel panel) => PlayAsync(GetTargets(panel));

    /// <summary>
    /// 入场动画，但只作用于指定的这组控件（用于页面中“局部参与动画”的场景）。
    /// </summary>
    public static Task PlayAsync(IEnumerable<Control> targets)
        => RunAsync(targets,
            0.0, 0.0, StartY, 0.0,
            0.0, 1.0, Duration, StepInterval, IntroEasing);

    /// <summary>
    /// 水平错峰入场：从右侧（X 50 → 0）或左侧（X -50 → 0）逐个滑入并淡入，
    /// 位移量、淡入时长、缓动与错峰间隔均与“向上入场”一致。
    /// </summary>
    public static Task PlaySlideInAsync(IEnumerable<Control> targets, bool fromRight)
        => RunAsync(targets,
            fromRight ? SlideDistance : -SlideDistance, 0.0, 0.0, 0.0,
            0.0, 1.0, Duration, StepInterval, IntroEasing);

    /// <summary>
    /// 出场动画：与入场相同的错峰顺序、间隔与运动方向（向上），子元素从当前位置继续
    /// 向上滑出（Y 0 → -50）并淡出。结束后元素停在隐藏态。
    /// </summary>
    public static Task PlayExitAsync(Panel panel) => PlayExitAsync(GetTargets(panel));

    /// <summary>出场动画，但只作用于指定的这组控件。</summary>
    public static Task PlayExitAsync(IEnumerable<Control> targets)
        => RunAsync(targets,
            0.0, 0.0, 0.0, ExitY,
            1.0, 0.0, ExitDuration, StepInterval, ExitEasing);

    /// <summary>
    /// 水平整体出场：所有目标在同一时刻向左（X 0 → -50）或向右（X 0 → 50）滑出并淡出，
    /// 无错峰。
    /// </summary>
    public static Task PlaySlideOutAsync(IEnumerable<Control> targets, bool toLeft)
        => RunAsync(targets,
            0.0, toLeft ? -SlideDistance : SlideDistance, 0.0, 0.0,
            1.0, 0.0, ExitDuration, TimeSpan.Zero, ExitEasing);

    /// <summary>
    /// 错峰动画通用实现：调用前先复位到 fromX/fromY/fromOpacity，再逐帧插值到 toX/toY/toOpacity。
    /// stepInterval 为 0 时所有目标同时起步；否则按索引错峰。
    /// </summary>
    private static Task RunAsync(IEnumerable<Control> targets,
        double fromX, double toX, double fromY, double toY,
        double fromOpacity, double toOpacity,
        TimeSpan duration, TimeSpan stepInterval, IEasing easing)
    {
        // 取代上一次动画，避免旧时钟干扰新页面
        Stop();

        var list = targets.Where(x => x.IsVisible).ToList();
        if (list.Count == 0)
        {
            return Task.CompletedTask;
        }

        // 宿主禁用动画：直接置为最终态，不做插值
        if (!IsEnabled)
        {
            foreach (var c in list)
            {
                c.Opacity = toOpacity;
                c.RenderTransform = new TranslateTransform { X = toX, Y = toY };
            }
            return Task.CompletedTask;
        }

        _activeDuration = duration;
        _activeEasing = easing;

        var now = Stopwatch.GetTimestamp();
        for (var i = 0; i < list.Count; i++)
        {
            var c = list[i];
            // 动画起点（页面若先前已 Prepare，此处首帧即被覆盖，不会闪现）
            var transform = new TranslateTransform { X = fromX, Y = fromY };
            c.RenderTransform = transform;
            c.Opacity = fromOpacity;

            Entries.Add(new Entry
            {
                Control = c,
                Transform = transform,
                StartTimestamp = now + stepInterval.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond * i,
                FromX = fromX,
                ToX = toX,
                FromY = fromY,
                ToY = toY,
                FromOpacity = fromOpacity,
                ToOpacity = toOpacity
            });
        }

        var completion = new TaskCompletionSource();
        _completion = completion;

        _timer = new DispatcherTimer(DispatcherPriority.Send)
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };
        _timer.Tick += OnTick;
        _timer.Start();

        return completion.Task;
    }

    private static void OnTick(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        var finished = true;

        foreach (var entry in Entries)
        {
            if (now < entry.StartTimestamp)
            {
                finished = false;
                continue;
            }

            var elapsed = (now - entry.StartTimestamp) / (double)Stopwatch.Frequency;
            var progress = elapsed / _activeDuration.TotalSeconds;
            if (progress >= 1.0)
            {
                progress = 1.0;
            }
            else
            {
                finished = false;
            }

            var eased = _activeEasing.Ease(progress);
            entry.Control.Opacity = entry.FromOpacity + (entry.ToOpacity - entry.FromOpacity) * eased;
            entry.Transform.X = entry.FromX + (entry.ToX - entry.FromX) * eased;
            entry.Transform.Y = entry.FromY + (entry.ToY - entry.FromY) * eased;
        }

        if (finished)
        {
            Stop();
        }
    }
}
