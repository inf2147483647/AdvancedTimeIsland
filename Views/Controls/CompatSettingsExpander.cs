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
/// 创建真实官方控件并转发属性 / 子项，因此官方控件自带的展开、折叠等动画全部保留，
/// 同时插件可以在 axaml 中以声明式语法编写设置分组。
/// </para>
/// </summary>
public class CompatSettingsExpander : TemplatedControl
{
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

    public CompatSettingsExpander()
    {
        _inner = FluentAvaloniaCompatibilityHelper.CreateSettingsExpander();
        _innerItems = ResolveItems(_inner);

        // 模板只承载真实 SettingsExpander：交互、动画、主题样式全部走官方控件
        Template = new FuncControlTemplate<CompatSettingsExpander>((_, _) => _inner);

        Items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>分组内的设置条目（axaml 直接元素即写入此集合）。</summary>
    [Content]
    public AvaloniaList<CompatSettingsExpanderItem> Items { get; } = new();

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
        if (change.Property == HeaderProperty)
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

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (CompatSettingsExpanderItem item in e.OldItems)
            {
                DetachItem(item);
            }
        }
        if (e.NewItems != null)
        {
            foreach (CompatSettingsExpanderItem item in e.NewItems)
            {
                AttachItem(item);
            }
        }
    }

    private void AttachItem(CompatSettingsExpanderItem item)
    {
        var realItem = FluentAvaloniaCompatibilityHelper.CreateSettingsExpanderItem();
        item.Attach(realItem);
        _innerItems.Add(realItem);
    }

    private void DetachItem(CompatSettingsExpanderItem item)
    {
        if (item.RealItem != null)
        {
            _innerItems.Remove(item.RealItem);
        }
        item.Detach();
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
