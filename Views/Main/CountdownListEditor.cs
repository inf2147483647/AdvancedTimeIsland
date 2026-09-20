using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 参考 ClassIsland「档案编辑」（ProfileSettingsWindow）的主从式编辑面板：
/// 左侧为 CommandBar 风格的工具栏（新建 / 删除 / 克隆）+ 列表，右侧为详情编辑区，
/// 未选中项时由 <see cref="Emptiable"/> 显示官方「啥都没有」占位符。
/// 取代原先的 ContentDialog 弹窗编辑方式。
/// </summary>
public sealed class CountdownListEditor<TItem> : UserControl where TItem : class
{
    private readonly Func<List<TItem>?> _itemsGetter;
    private readonly Action<List<TItem>> _itemsSetter;
    private readonly Func<TItem, string> _primaryTextSelector;
    private readonly Func<TItem, string?> _secondaryTextSelector;
    private readonly Func<TItem, Control> _detailFactory;
    private readonly Func<TItem> _itemFactory;
    private readonly Func<TItem, TItem> _itemCloner;

    private readonly List<TextBlock> _themeTextBlocks = new();
    private readonly Dictionary<TItem, TextBlock[]> _rowTextBlocks = new();

    private ListBox? _listBox;
    private Emptiable? _emptiable;
    private Button? _removeButton;
    private Button? _cloneButton;
    private TItem? _selected;
    private bool _suppressSelectionChanged;

    // 列表内拖动重排：宿主运行于 Avalonia 11.x、编译期引用 12.x，两版 DragDrop API 不兼容，
    // 故不使用系统级拖放，改用跨版本稳定的指针事件手动实现（详见 AttachReorderHandlers）。
    private ListBoxItem? _dragItem;
    private Visual? _dragPanel;         // 被拖容器的父级（Items 面板），作为指针坐标参照系
    private Point _dragStartPos;
    private bool _isDragging;
    private int _draggedIndex = -1;   // 被拖项在 Items 中的原始索引
    private int _dropIndex = -1;      // 释放后应落到的数据索引
    private const double DragStartThreshold = 3.0;

    public CountdownListEditor(
        Func<List<TItem>?> itemsGetter,
        Action<List<TItem>> itemsSetter,
        Func<TItem, string> primaryTextSelector,
        Func<TItem, string?> secondaryTextSelector,
        Func<TItem, Control> detailFactory,
        Func<TItem> itemFactory,
        Func<TItem, TItem> itemCloner,
        string newItemHint = "新建一个倒计时。",
        string cloneHint = "创建选中项的副本。",
        string removeHint = "删除选中的倒计时。")
    {
        _itemsGetter = itemsGetter;
        _itemsSetter = itemsSetter;
        _primaryTextSelector = primaryTextSelector;
        _secondaryTextSelector = secondaryTextSelector;
        _detailFactory = detailFactory;
        _itemFactory = itemFactory;
        _itemCloner = itemCloner;

        InitializeComponent(newItemHint, cloneHint, removeHint);
        // 注意：宿主 ComponentBase.Settings 在构造期间尚未注入，
        // 因此不在此处 Refresh()，由宿主在 OnLoaded 中调用 Refresh()。
    }

    private void InitializeComponent(string newItemHint, string cloneHint, string removeHint)
    {
        // 折叠栏内容区高度固定为 375px，超出部分由内部滚动容器（左列表 / 右详情）承载
        var root = new Grid
        {
            Height = 375,
            VerticalAlignment = VerticalAlignment.Top,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(210), MinWidth = 150, MaxWidth = 340 },
                new ColumnDefinition { Width = new GridLength(5) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 260 }
            }
        };

        // ---------- 左栏：工具栏 + 列表 ----------
        var leftPanel = new Grid
        {
            Margin = new Thickness(0, 0, 3, 0),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var addButton = CreateToolButton("\ue00d", "新建", newItemHint);
        addButton.Click += OnAddClick;
        toolbar.Children.Add(addButton);

        _cloneButton = CreateToolButton("\ue58d", "克隆", cloneHint);
        _cloneButton.Click += OnCloneClick;
        toolbar.Children.Add(_cloneButton);

        _removeButton = CreateToolButton("\ue61d", "删除", removeHint);
        _removeButton.Click += OnRemoveClick;
        toolbar.Children.Add(_removeButton);

        // 左栏拖窄时，工具栏按钮会超出列边界并与分隔条重叠；
        // 用横向 ScrollViewer 裁剪溢出内容（不再压到分隔条上），窄栏下可横向滚动访问全部按钮。
        var toolbarScroll = new ScrollViewer
        {
            Content = toolbar,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(toolbarScroll, 0);
        leftPanel.Children.Add(toolbarScroll);

        _listBox = new ListBox
        {
            SelectionMode = SelectionMode.Single,
            HorizontalAlignment = HorizontalAlignment.Stretch
            // Avalonia 的 ListBox 默认使用非虚拟化 StackPanel，所有容器均已实现，可直接读取其 Bounds 做重排命中判定。
        };
        _listBox.SelectionChanged += OnListSelectionChanged;
        AttachReorderHandlers(_listBox);
        Grid.SetRow(_listBox, 1);
        leftPanel.Children.Add(_listBox);

        Grid.SetColumn(leftPanel, 0);
        root.Children.Add(leftPanel);

        // ---------- 分隔条 ----------
        var splitter = new GridSplitter
        {
            Width = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ResizeDirection = GridResizeDirection.Columns,
            Margin = new Thickness(0)
        };
        Grid.SetColumn(splitter, 1);
        root.Children.Add(splitter);

        // ---------- 右栏：详情（未选中时显示官方「啥都没有」） ----------
        _emptiable = new Emptiable
        {
            IsDirectContentMode = true,
            Margin = new Thickness(3, 0, 0, 0)
        };
        Grid.SetColumn(_emptiable, 2);
        root.Children.Add(_emptiable);

        Content = root;
    }

    private Button CreateToolButton(string glyph, string label, string tooltip)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(new FluentIcon(glyph)
        {
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center
        });

        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _themeTextBlocks.Add(text);
        panel.Children.Add(text);

        var button = new Button
        {
            Content = panel,
            Padding = new Thickness(8, 5, 8, 5)
        };
        ToolTip.SetTip(button, tooltip);
        return button;
    }

    /// <summary>重建左侧列表（保留原选中项），并按当前选中项刷新右侧详情。</summary>
    public void Refresh()
    {
        if (_listBox == null || _emptiable == null)
            return;

        var items = _itemsGetter();
        var previous = _selected;

        _suppressSelectionChanged = true;
        _listBox.Items.Clear();
        _rowTextBlocks.Clear();

        if (items != null)
        {
            foreach (var item in items)
            {
                _listBox.Items.Add(BuildRow(item));
            }
        }

        // 尽量保持原选中项；已被删除则回退到首项 / 空。
        TItem? target = null;
        if (previous != null && items != null && items.Contains(previous))
        {
            target = previous;
        }
        else if (items != null && items.Count > 0)
        {
            target = items[0];
        }

        SetSelection(target);
        _suppressSelectionChanged = false;
    }

    /// <summary>仅刷新指定项所在行的文本（避免整表重建导致编辑控件失焦）。</summary>
    public void RefreshRow(TItem item)
    {
        if (item == null)
            return;

        if (_rowTextBlocks.TryGetValue(item, out var blocks))
        {
            blocks[0].Text = _primaryTextSelector(item);
            if (blocks.Length > 1)
            {
                blocks[1].Text = _secondaryTextSelector(item) ?? string.Empty;
            }
        }
    }

    /// <summary>把面板滚动到可视区域，并确保有选中项（供「前往编辑」按钮使用）。</summary>
    public void FocusEditor()
    {
        this.BringIntoView();

        var items = _itemsGetter();
        if (_selected == null && items != null && items.Count > 0)
        {
            SetSelection(items[0]);
        }
    }

    private ListBoxItem BuildRow(TItem item)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Margin = new Thickness(0, 4, 0, 4)
        };

        var primary = new TextBlock
        {
            Text = _primaryTextSelector(item),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = ThemeHelper.GetTextBrush()
        };
        panel.Children.Add(primary);

        var secondaryText = _secondaryTextSelector(item);
        TextBlock[] blocks;
        if (!string.IsNullOrEmpty(secondaryText))
        {
            var secondary = new TextBlock
            {
                Text = secondaryText,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = ThemeHelper.GetSubTextBrush()
            };
            panel.Children.Add(secondary);
            blocks = new[] { primary, secondary };
        }
        else
        {
            blocks = new[] { primary };
        }

        _rowTextBlocks[item] = blocks;

        return new ListBoxItem
        {
            Content = panel,
            Tag = item
        };
    }

    private void SetSelection(TItem? item)
    {
        _selected = item;

        if (_listBox != null)
        {
            var items = _itemsGetter();
            var index = item == null || items == null ? -1 : items.IndexOf(item);
            _listBox.SelectedIndex = index;
        }

        if (_emptiable != null)
        {
            if (item == null)
            {
                _emptiable.Content = null;
                _emptiable.Data = null;
            }
            else
            {
                _emptiable.Content = new ScrollViewer
                {
                    Content = _detailFactory(item),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };
                _emptiable.Data = item;
            }
        }

        if (_cloneButton != null)
            _cloneButton.IsEnabled = item != null;
        if (_removeButton != null)
            _removeButton.IsEnabled = item != null;
    }

    private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // 拖动重排期间，移除/插入被选项会短暂清空选中，忽略以免右侧详情面板闪烁/丢失。
        if (_suppressSelectionChanged || _isDragging)
            return;

        var item = _listBox?.SelectedItem is ListBoxItem lbi ? lbi.Tag as TItem : null;
        SetSelection(item);
    }

    // ==================== 列表内拖动重排 ====================
    // 交互模型参考 ClassIsland 编辑模式的组件拖动（ClassIsland.Core.Behaviors.AdvancedItemDragBehavior）：
    // 拖动期间绝不改动 Items（改 Items 会重建容器、被拖实例被孤立导致“项消失”），
    // 仅用 RenderTransform 平移容器做“让位”预览；释放时才重排底层数据并 Refresh()。

    private void AttachReorderHandlers(ListBox listBox)
    {
        // handledEventsToo=true：ListBox 内部会为选中处理按下事件，这里仍需拿到以记录拖动起点。
        listBox.AddHandler(InputElement.PointerPressedEvent, OnReorderPointerPressed,
            RoutingStrategies.Bubble, handledEventsToo: true);
        listBox.AddHandler(InputElement.PointerMovedEvent, OnReorderPointerMoved,
            RoutingStrategies.Bubble, handledEventsToo: true);
        listBox.AddHandler(InputElement.PointerReleasedEvent, OnReorderPointerReleased,
            RoutingStrategies.Bubble, handledEventsToo: true);
        listBox.AddHandler(InputElement.PointerCaptureLostEvent, OnReorderPointerCaptureLost,
            RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnReorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_listBox == null || !e.GetCurrentPoint(_listBox).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var item = FindAncestorListBoxItem(e.Source as Visual);
        if (item == null)
        {
            return;
        }

        _dragItem = item;
        _dragPanel = item.GetVisualParent();   // 容器 Bounds 的坐标系（Items 面板），指针位置以其为参照
        _isDragging = false;
        _draggedIndex = -1;
        _dropIndex = -1;
        _dragStartPos = _dragPanel != null ? e.GetPosition(_dragPanel) : e.GetPosition(_listBox);
    }

    private void OnReorderPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_listBox == null || _dragItem == null)
        {
            return;
        }

        if (!e.GetCurrentPoint(_listBox).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var pos = _dragPanel != null ? e.GetPosition(_dragPanel) : e.GetPosition(_listBox);

        if (!_isDragging)
        {
            // 超过阈值才认定为拖动，避免与单击选择冲突
            var dx = pos.X - _dragStartPos.X;
            var dy = pos.Y - _dragStartPos.Y;
            if (Math.Sqrt(dx * dx + dy * dy) < DragStartThreshold)
            {
                return;
            }

            // 先捕获到 ListBox、后置位 _isDragging：Capture() 会同步触发旧隐式捕获目标（被按下的子元素）
            // 的 PointerCaptureLost 并冒泡到本控件，此时 _isDragging 仍为 false，该回调被自然跳过、不误提交。
            e.Pointer?.Capture(_listBox);

            _isDragging = true;
            _draggedIndex = _listBox.Items.IndexOf(_dragItem);
            _dropIndex = _draggedIndex;
        }

        UpdateDragPreview(pos);
    }

    private void OnReorderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            CommitReorder();
            ResetReorderState();
        }
        e.Pointer?.Capture(null);
    }

    private void OnReorderPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        // 仅处理真正的外部丢捕获（如窗口失焦导致收不到释放事件）：提交并复位，避免状态卡死。
        // 改捕获瞬间的同步 lost 到达时 _isDragging 尚为 false，自然被跳过。
        if (_isDragging)
        {
            CommitReorder();
            ResetReorderState();
        }
    }

    /// <summary>
    /// 拖动预览：被拖项跟随指针平移，其余项按 ClassIsland 同款规则“让位”，
    /// 并计算释放时的落点索引 _dropIndex（原始集合坐标系）。
    /// </summary>
    private void UpdateDragPreview(Point pointerPos)
    {
        if (_listBox == null || _dragItem == null || _draggedIndex < 0)
            return;

        var items = _listBox.Items;

        // ListBoxItem.Bounds 不含 RenderTransform，即原始布局位置
        var draggedBounds = _dragItem.Bounds;
        var delta = pointerPos.Y - _dragStartPos.Y;
        SetTranslate(_dragItem, 0, delta);

        var draggedDeltaStart = draggedBounds.Y + delta;
        var draggedDeltaEnd = draggedDeltaStart + draggedBounds.Height;

        int newTarget = -1;

        for (int i = 0; i < items.Count; i++)
        {
            if (i == _draggedIndex || items[i] is not ListBoxItem container)
                continue;

            var bounds = container.Bounds;
            var mid = bounds.Y + bounds.Height / 2;

            if (bounds.Y > draggedBounds.Y && draggedDeltaEnd >= mid)
            {
                // 被拖项下移越过该项中线：该项上移让位
                SetTranslate(container, 0, -draggedBounds.Height);
                if (newTarget == -1 || i > newTarget)
                    newTarget = i;
            }
            else if (bounds.Y < draggedBounds.Y && draggedDeltaStart <= mid)
            {
                // 被拖项上移越过该项中线：该项下移让位
                SetTranslate(container, 0, draggedBounds.Height);
                if (newTarget == -1 || i < newTarget)
                    newTarget = i;
            }
            else
            {
                SetTranslate(container, 0, 0);
            }
        }

        _dropIndex = newTarget == -1 ? _draggedIndex : newTarget;
    }

    /// <summary>释放时把预览结果落盘：原地重排数据 List（随后由 ResetReorderState 统一 Refresh）。</summary>
    private void CommitReorder()
    {
        var data = _itemsGetter();
        if (data == null || _draggedIndex < 0 || _dropIndex < 0)
            return;
        if (_draggedIndex >= data.Count || _draggedIndex == _dropIndex)
            return;

        var moved = data[_draggedIndex];
        data.RemoveAt(_draggedIndex);
        data.Insert(Math.Clamp(_dropIndex, 0, data.Count), moved);
    }

    private static void SetTranslate(Control control, double x, double y)
    {
        var builder = new TransformOperations.Builder(1);
        builder.AppendTranslate(x, y);
        control.RenderTransform = builder.Build();
    }

    private void ClearDragTransforms()
    {
        if (_listBox == null)
            return;

        foreach (var obj in _listBox.Items)
        {
            if (obj is ListBoxItem container)
            {
                container.RenderTransform = null;
            }
        }
    }

    private void ResetReorderState()
    {
        ClearDragTransforms();

        _dragItem = null;
        _dragPanel = null;
        _isDragging = false;
        _draggedIndex = -1;
        _dropIndex = -1;

        // 按新顺序重建列表；Refresh() 会保持 _selected（被拖项），选中与详情自动对齐到新位置。
        Refresh();
    }

    private ListBoxItem? FindAncestorListBoxItem(Visual? source)
    {
        var current = source;
        while (current != null && current != _listBox)
        {
            if (current is ListBoxItem lbi)
                return lbi;
            current = current.GetVisualParent();
        }
        return null;
    }

    private void OnAddClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var items = _itemsGetter();
        if (items == null)
        {
            items = new List<TItem>();
            _itemsSetter(items);
        }

        var created = _itemFactory();
        items.Add(created);
        _selected = created;   // 让 Refresh 直接选中新项，避免重复构建详情面板
        Refresh();
    }

    private void OnCloneClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var items = _itemsGetter();
        if (items == null || _selected == null)
            return;

        var index = items.IndexOf(_selected);
        var clone = _itemCloner(_selected);
        if (index >= 0)
        {
            items.Insert(index + 1, clone);
        }
        else
        {
            items.Add(clone);
        }

        _selected = clone;   // 让 Refresh 直接选中副本，避免重复构建详情面板
        Refresh();
    }

    private void OnRemoveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var items = _itemsGetter();
        if (items == null || _selected == null)
            return;

        items.Remove(_selected);
        Refresh();
    }

    /// <summary>随主题深浅更新文字颜色。</summary>
    public void UpdateThemeColors()
    {
        foreach (var tb in _themeTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetTextBrush();
        }

        foreach (var blocks in _rowTextBlocks.Values)
        {
            blocks[0].Foreground = ThemeHelper.GetTextBrush();
            if (blocks.Length > 1)
            {
                blocks[1].Foreground = ThemeHelper.GetSubTextBrush();
            }
        }
    }
}
