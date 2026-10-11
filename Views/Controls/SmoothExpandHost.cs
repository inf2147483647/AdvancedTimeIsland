using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// 平滑展开/折叠宿主：把任意带有 <c>IsExpanded</c> 属性的控件（如 FluentAvalonia 的
/// SettingsExpander）包进一个裁剪容器，在其展开/折叠时对容器高度做过渡，
/// 从而让后续元素被平滑地推下/上移，而不是瞬间弹跳。
/// <para>
/// 参数参考 FemboyTest 结果页“展开明细”：高度过渡 260ms、<see cref="CubicEaseOut"/>。
/// 与之不同的是这里<b>不</b>做整体透明度过渡 —— 折叠栏折叠后仍会显示标题行（Header），
/// 整体淡出会把标题一起抹掉，故只对高度做过渡。
/// </para>
/// <para>
/// 与宿主保持一致：当 ClassIsland 动画级别为“禁用”（<see cref="IThemeService.AnimationLevel"/> &lt; 1）时，
/// 本宿主退化为一个普通包装容器，完全不干预尺寸，行为与未包裹时一致。
/// </para>
/// <para>
/// 本类型不依赖 FluentAvalonia 的内部模板结构（FA2 2.4 与 FA3 3.0 的内部命名不同），
/// 仅通过公开的 <c>IsExpanded</c> / <c>IsVisible</c> 属性工作，因此同一份代码在两个框架下均可用。
/// </para>
/// </summary>
public class SmoothExpandHost : Border
{
    private static readonly TimeSpan SizeDuration = TimeSpan.FromMilliseconds(260);

    private readonly Control _inner;
    private readonly bool _animationEnabled;

    private bool _initialized;
    private bool _isExpanded;
    // 展开前的折叠高度：收起时直接复用它，避免折叠后测量时点不可靠导致丢失收起过渡
    private double? _collapsedHeight;
    private double _lastMeasuredWidth = double.NaN;

    /// <summary>被包装的真实控件（如 FA 的 SettingsExpander）。</summary>
    public Control Inner => _inner;

    public SmoothExpandHost(Control inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        Child = inner;
        // 与内部控件初始可见性保持一致（如“女装”折叠栏默认隐藏）
        IsVisible = inner.IsVisible;

        _animationEnabled = IThemeService.AnimationLevel >= 1;
        if (!_animationEnabled)
        {
            return;
        }

        _isExpanded = ReadInnerExpanded();
        ClipToBounds = true;
        _inner.PropertyChanged += OnInnerPropertyChanged;
        PropertyChanged += OnHostPropertyChanged;
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    /// <summary>反射读取内部控件的 <c>IsExpanded</c> 初始值（FA2/FA3 均为公开属性）。</summary>
    private bool ReadInnerExpanded()
        => _inner.GetType().GetProperty("IsExpanded")?.GetValue(_inner) is true;

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // 布局完成后再锁定初始高度，保证测到的是真实尺寸
        Dispatcher.UIThread.Post(EnsureInitialized, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// 首次初始化：把容器高度锁定为当前自然高度（此时尚未装配过渡，因此不会播放动画），
    /// 之后展开/折叠才有可插值的起点。
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var natural = MeasureInnerHeight();
        Height = natural;
        if (!_isExpanded && !double.IsNaN(natural))
        {
            _collapsedHeight = natural;
        }

        Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Layoutable.HeightProperty,
                Duration = SizeDuration,
                Easing = new CubicEaseOut()
            }
        };
    }

    private void OnInnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            // 折叠栏整体被外部隐藏（如“女装”入口）时，宿主跟随
            case "IsVisible":
                IsVisible = _inner.IsVisible;
                if (_inner.IsVisible)
                {
                    Dispatcher.UIThread.Post(ApplyTargetHeight, DispatcherPriority.Loaded);
                }
                break;
            case "IsExpanded":
                var expanded = e.NewValue is true;
                if (expanded && e.OldValue is false && !double.IsNaN(Height))
                {
                    // 此刻高度仍是折叠高度，记录下来供收起时复用
                    _collapsedHeight = Height;
                }
                _isExpanded = expanded;
                // 推迟到下一帧：此时 FluentAvalonia 内部模板的展开态 / 内容可见性已刷新
                Dispatcher.UIThread.Post(ApplyTargetHeight, DispatcherPriority.Loaded);
                break;
        }
    }

    private void OnHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != BoundsProperty)
        {
            return;
        }

        var width = Bounds.Width;
        if (double.IsNaN(_lastMeasuredWidth) || Math.Abs(width - _lastMeasuredWidth) > 0.5)
        {
            _lastMeasuredWidth = width;
            Dispatcher.UIThread.Post(ApplyTargetHeight, DispatcherPriority.Loaded);
        }
    }

    /// <summary>按当前展开状态计算目标高度，并把容器高度过渡到该值。</summary>
    private void ApplyTargetHeight()
    {
        if (!_initialized)
        {
            EnsureInitialized();
            return;
        }

        // 展开：测量展开后的自然高度（此时内容已可见，测量可靠）
        // 收起：直接回到记录下来的折叠高度 —— 折叠后 FluentAvalonia 内容区的隐藏时点不可靠，
        //       现场测量可能仍得到展开高度，会让“收起”因为目标高度不变而丢失过渡。
        var target = _isExpanded ? MeasureInnerHeight() : _collapsedHeight ?? MeasureInnerHeight();
        if (double.IsNaN(target))
        {
            return;
        }

        // 高度（属性值）在过渡期间即等于目标值，故此处不会重复触发、打断进行中的过渡
        if (Math.Abs(target - Height) < 0.5)
        {
            return;
        }

        Height = target;
    }

    /// <summary>在“按可用宽度换行”的前提下测量被包裹控件的自然高度。</summary>
    private double MeasureInnerHeight()
    {
        var width = Bounds.Width;
        if (width <= 0 && Parent is Layoutable parent && parent.Bounds.Width > 0)
        {
            width = parent.Bounds.Width;
        }
        if (width <= 0)
        {
            width = _lastMeasuredWidth;
        }
        if (width <= 0 || double.IsNaN(width))
        {
            width = double.PositiveInfinity;
        }

        _inner.Measure(new Size(width, double.PositiveInfinity));
        var height = _inner.DesiredSize.Height;
        return double.IsNaN(height) || height < 0 ? double.NaN : height;
    }
}
