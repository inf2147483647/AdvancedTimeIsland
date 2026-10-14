using System.Collections;
using System.Collections.Specialized;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// FluentAvalonia <c>SettingsExpander</c> 的 XAML 声明式兼容包装。
/// <para>
/// FA2 中类型名为 <c>SettingsExpander/SettingsExpanderItem</c>，FA3 改名为
/// <c>FASettingsExpander/FASettingsExpanderItem</c>，同一份 axaml 无法按类型名同时声明两者。
/// 本包装在代码中按运行时版本通过 <see cref="FluentAvaloniaCompatibilityHelper"/>
/// 创建真实官方控件并转发属性 / 子项，因此官方控件自带的样式与交互全部保留，
/// 同时插件可以在 axaml 中以声明式语法编写设置分组。
/// </para>
/// <para>
/// 折叠 / 展开由 <see cref="IntroAnimationHelper.CreateExpandHost"/> 提供平滑的高度过渡（后续元素被平滑推开 / 收拢），
/// 不再依赖 FluentAvalonia 各版本内部各异的展开动画实现。
/// </para>
/// </summary>
public class CompatSettingsExpander : TemplatedControl
{
    /// <summary>分组是否展开（转发给真实 FA SettingsExpander 的 IsExpanded）。</summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<CompatSettingsExpander, bool>(nameof(IsExpanded));

    /// <summary>分组标题。</summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<CompatSettingsExpander, string?>(nameof(Header));

    /// <summary>分组描述（标题下方的辅助说明）。</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<CompatSettingsExpander, string?>(nameof(Description));

    /// <summary>折叠栏头部右侧的 Footer 内容（如总开关）。</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<CompatSettingsExpander, object?>(nameof(Footer));

    private readonly Control _inner;
    private readonly IList _innerItems;
    private Control? _host;

    public CompatSettingsExpander()
    {
        _inner = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        _innerItems = ResolveItems(_inner);

        // 模板只承载“平滑展开宿主”：交互、官方样式全部走真实 SettingsExpander，
        // 宿主仅额外提供折叠/展开时的高度过渡。
        // 宿主延迟到模板 build 时才创建：axaml 的属性（IsExpanded）与子项填充都发生在模板应用之前，
        // 提前创建会让 ExpandHost 以“折叠 + 空 ItemCount”初始化，首帧多播放一次展开动画。
        Template = new FuncControlTemplate<CompatSettingsExpander>((owner, _) => owner.EnsureHostCreated());

        Items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>
    /// 分组内容（axaml 直接元素即写入此集合）：
    /// 既可以是 <see cref="CompatSettingsExpanderItem"/>（转发为官方 SettingsExpanderItem），
    /// 也可以是任意裸控件（如复合表单 StackPanel，FA 会自动包一层 SettingsExpanderItem 容器）。
    /// </summary>
    [Content]
    public AvaloniaList<object> Items { get; } = new();

    /// <summary>分组是否展开（转发给真实 FA SettingsExpander）。</summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>分组标题。</summary>
    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>分组描述。</summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>折叠栏 Footer 内容。</summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty)
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(_inner, "IsExpanded", IsExpanded);
        }
        else if (change.Property == HeaderProperty)
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(_inner, "Header", Header ?? string.Empty);
        }
        else if (change.Property == DescriptionProperty)
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(_inner, "Description", Description ?? string.Empty);
        }
        else if (change.Property == FooterProperty)
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderProperty(_inner, "Footer", BuildRightAligned(Footer));
        }
    }

    /// <summary>模板应用时懒创建平滑展开宿主（此时 axaml 属性与子项均已就位）。</summary>
    private Control EnsureHostCreated()
    {
        return _host ??= IntroAnimationHelper.CreateExpandHost(_inner);
    }

    /// <summary>
    /// 通知平滑展开宿主重算高度（内部动态表格重建 / 显隐切换后调用）。
    /// 模板尚未应用（宿主未创建）时为空操作——宿主创建时会按当前自然高度锁定。
    /// </summary>
    public void RefreshHost()
    {
        if (_host != null)
        {
            IntroAnimationHelper.RefreshExpandHost(_inner);
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                DetachItem(item);
            }
        }
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                AttachItem(item);
            }
        }
    }

    private void AttachItem(object item)
    {
        switch (item)
        {
            // 声明式设置条目：创建真实 FA SettingsExpanderItem 并转发属性
            case CompatSettingsExpanderItem wrapped:
            {
                var realItem = FluentAvaloniaCompatibilityHelper.CreateSettingsExpanderItem();
                wrapped.Attach(realItem);
                _innerItems.Add(realItem);
                break;
            }
            // 裸控件：直接加入真实 Items，FA 自动包一层 SettingsExpanderItem 容器
            case Control control:
                _innerItems.Add(control);
                break;
        }
    }

    private void DetachItem(object item)
    {
        switch (item)
        {
            case CompatSettingsExpanderItem wrapped:
                if (wrapped.RealItem != null)
                {
                    _innerItems.Remove(wrapped.RealItem);
                }
                wrapped.Detach();
                break;
            case Control control:
                _innerItems.Remove(control);
                break;
        }
    }

    /// <summary>
    /// 反射获取真实 SettingsExpander 的 Items 集合（FA2/FA3 均为 ItemsControl）。
    /// </summary>
    private static IList ResolveItems(Control expander)
    {
        var itemsProperty = expander.GetType().GetProperty("Items");
        if (itemsProperty?.GetValue(expander) is IList items)
        {
            return items;
        }
        // 兜底：理论上不会走到（真实控件始终暴露 Items），仅避免空引用
        return new AvaloniaList<object>();
    }

    /// <summary>
    /// Footer 统一包一层右对齐面板（与插件此前代码构建的布局保持一致）。
    /// </summary>
    internal static object? BuildRightAligned(object? footer)
    {
        if (footer == null)
        {
            return null;
        }
        if (footer is Control control)
        {
            control.HorizontalAlignment = HorizontalAlignment.Right;
            control.VerticalAlignment = VerticalAlignment.Center;
            return control;
        }
        return footer;
    }
}

/// <summary>
/// <see cref="CompatSettingsExpander"/> 中的条目。本身不参与可视化渲染，
/// 仅作为 axaml 声明载体，把属性转发给真实的 FA SettingsExpanderItem。
/// </summary>
public class CompatSettingsExpanderItem : Control
{
    /// <summary>条目标题（文本）。</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<CompatSettingsExpanderItem, object?>(nameof(Content));

    /// <summary>条目描述。</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<CompatSettingsExpanderItem, string?>(nameof(Description));

    /// <summary>条目右侧 Footer 控件（开关、输入框、下拉框等）。</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<CompatSettingsExpanderItem, object?>(nameof(Footer));

    internal Control? RealItem { get; private set; }

    /// <summary>条目标题。</summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>条目描述。</summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>条目 Footer 控件。</summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    internal void Attach(Control realItem)
    {
        RealItem = realItem;
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(realItem, "Content", Content ?? string.Empty);
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(realItem, "Description", Description ?? string.Empty);
        FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(realItem, "Footer", CompatSettingsExpander.BuildRightAligned(Footer));
        RealItem.IsEnabled = IsEnabled;
    }

    internal void Detach()
    {
        RealItem = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (RealItem == null)
        {
            return;
        }

        if (change.Property == ContentProperty)
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(RealItem, "Content", Content ?? string.Empty);
        }
        else if (change.Property == DescriptionProperty)
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(RealItem, "Description", Description ?? string.Empty);
        }
        else if (change.Property == FooterProperty)
        {
            FluentAvaloniaCompatibilityHelper.SetSettingsExpanderItemProperty(RealItem, "Footer", CompatSettingsExpander.BuildRightAligned(Footer));
        }
        else if (change.Property == InputElement.IsEnabledProperty)
        {
            RealItem.IsEnabled = IsEnabled;
        }
    }
}
