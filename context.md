**User**

---

在向开发者提交问题时请保留以下信息：
TraceID: 94f9585857dce2970400d863ba65d65c
================================

插件版本：AdvancedTimeIsland 2.0.5.2
发生时间：2026-09-24 21:02:42
出错的页面：应用设置->AdvancedTimeIsland调试->女装->JK 制服
此问题可能由以下插件引起，请在向 ClassIsland 开发者反馈问题前先向以下插件的开发者反馈此问题：
- FemboyTest [FemboyTest,1.0.1.0]
- Femboy [inf2147483647.MatchTest.01,1.0.0.0]
- FEMBOY [inf2147483647.MatchTest.02,1.0.0.0]
- Fem·boy [inf2147483647.MatchTest.03,1.0.0.0]
- Ｆｅｍｂｏｙ [inf2147483647.MatchTest.04,1.0.0.0]
- F e m b o y [inf2147483647.MatchTest.05,1.0.0.0]
- 男娘 [inf2147483647.MatchTest.06,1.0.0.0]
- 男·娘 [inf2147483647.MatchTest.07,1.0.0.0]
- 男の娘 [inf2147483647.MatchTest.08,1.0.0.0]
- 南梁 [inf2147483647.MatchTest.09,1.0.0.0]
- 楠酿 [inf2147483647.MatchTest.10,1.0.0.0]
以上异常插件已自动禁用，重启应用后生效。您可以在排除问题后前往【应用设置】->【插件】中重新启用这些插件，或在【应用设置】->【基本】中调整是否自动禁用异常插件。
================================
System.Exception: There is no pictures of JK uniform!
   at AdvancedTimeIsland.Views.Settings.WomenswearPage.BuildCrashInfo(IReadOnlyList`1 blamedPlugins, Boolean blamedDisabled)
   at AdvancedTimeIsland.Views.Settings.WomenswearPage.ShowHostCrashWindow()
   at AdvancedTimeIsland.Views.Settings.WomenswearPage.OnTabSelectionChanged(Object sender, SelectionChangedEventArgs e)
   at Avalonia.Interactivity.EventRoute.RaiseEventImpl(RoutedEventArgs e)
   at Avalonia.Interactivity.EventRoute.RaiseEvent(Interactive source, RoutedEventArgs e)
   at Avalonia.Interactivity.Interactive.RaiseEvent(RoutedEventArgs e)
   at Avalonia.Controls.Primitives.SelectingItemsControl.RaiseSelectionChanged(IReadOnlyList`1 removedItems, IReadOnlyList`1 addedItems)
   at Avalonia.Controls.Primitives.SelectingItemsControl.OnSelectionModelSelectionChanged(Object sender, SelectionModelSelectionChangedEventArgs e)
   at Avalonia.Controls.Selection.SelectionModel`1.CommitOperation(Operation operation, Boolean raisePropertyChanged)
   at Avalonia.Controls.Selection.SelectionModelExtensions.BatchUpdateOperation.Dispose()
   at Avalonia.Controls.Primitives.SelectingItemsControl.UpdateSelection(Int32 index, Boolean select, Boolean rangeModifier, Boolean toggleModifier, Boolean rightButton, Boolean fromFocus)
   at Avalonia.Controls.Primitives.SelectingItemsControl.UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
   at Avalonia.Controls.Primitives.TabStrip.UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
   at Avalonia.Controls.ListBoxItem.UpdateSelectionFromEvent(RoutedEventArgs e)
   at Avalonia.Controls.ListBoxItem.OnPointerPressed(PointerPressedEventArgs e)
   at Avalonia.Reactive.LightweightObservableBase`1.PublishNext(T value)
   at Avalonia.Interactivity.EventRoute.RaiseEventImpl(RoutedEventArgs e)
   at Avalonia.Interactivity.EventRoute.RaiseEvent(Interactive source, RoutedEventArgs e)
   at Avalonia.Interactivity.Interactive.RaiseEvent(RoutedEventArgs e)
   at Avalonia.Input.MouseDevice.MouseDown(IMouseDevice device, UInt64 timestamp, IInputRoot root, Point p, PointerPointProperties properties, KeyModifiers inputModifiers, IInputElement hitTest, Object platformInputEventCookie)
   at Avalonia.Input.MouseDevice.ProcessRawEvent(RawPointerEventArgs e)
   at Avalonia.Threading.Dispatcher.Send(SendOrPostCallback action, Object arg, Nullable`1 priority)
   at Avalonia.Controls.PresentationSource.HandleInput(RawInputEventArgs e)
   at Avalonia.Win32.WindowImpl.AppWndProc(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam)
   at Avalonia.Win32.WindowImpl.WndProcMessageHandler(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam)
   at FluentAvalonia.Interop.Win32Interop.CallWindowProcW(IntPtr lpPrevWndProc, HWND hWnd, UInt32 msg, WPARAM wParam, LPARAM lParam)
   at FluentAvalonia.Interop.Win32Interop.CallWindowProcW(IntPtr lpPrevWndProc, HWND hWnd, UInt32 msg, WPARAM wParam, LPARAM lParam)
   at FluentAvalonia.UI.Windowing.Win32WindowManager.WndProcStatic(IntPtr hwnd, UInt32 msg, IntPtr wParam, IntPtr lParam)
   at Avalonia.Win32.Interop.UnmanagedMethods.DispatchMessage(MSG& lpmsg)
   at Avalonia.Win32.Interop.UnmanagedMethods.DispatchMessage(MSG& lpmsg)
   at Avalonia.Win32.Win32DispatcherImpl.RunLoop(CancellationToken cancellationToken)
   at Avalonia.Threading.DispatcherFrame.Run(IControlledDispatcherImpl impl)
   at Avalonia.Threading.Dispatcher.PushFrame(DispatcherFrame frame)
   at Avalonia.Threading.Dispatcher.MainLoop(CancellationToken cancellationToken)
   at Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime.StartCore(String[] args)
   at Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime.Start(String[] args)
   at Avalonia.ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(AppBuilder builder, String[] args, Action`1 lifetimeBuilder)
   at ClassIsland.Desktop.Program.Main(String[] args) in /_/ClassIsland.Desktop/Program.cs:line 118

---

**Chat**

<thinking>
我们需要回答用户的问题：为什么打开 JK 制服页面会穷举所有男娘插件全部禁用？我们需要基于之前三份日志的分析，结合插件命名、MatchTest 系列、AdvancedTimeIsland 的行为来推断。用户想了解机制。我们不能给解决方案，但可以分析原因。需要解释为什么触发“JK制服”页面会导致 AdvancedTimeIsland 扫描并归因所有“男娘”相关插件，并自动禁用。核心是 AdvancedTimeIsland 的 WomenswearPage 中可能有一套冲突检测逻辑：当检测到“JK制服”图片缺失时，它会调用 BuildCrashInfo，并传入 blamedPlugins。这个 blamedPlugins 列表可能是通过某种匹配规则从已安装插件中筛选出来的，匹配规则与“女装/男娘/femboy”相关。为什么穷举所有？因为测试插件是故意构造的各种变体，而 AdvancedTimeIsland 的匹配逻辑可能使用了模糊匹配，比如关键词“femboy”、“男娘”、“fem”、“boy”、“娘”等，或者基于插件名称的相似度，导致所有变体都被识别为冲突。也可能是因为这些插件都带有相同的 ID 前缀 inf2147483647.MatchTest，而 AdvancedTimeIsland 可能按 ID 或命名空间进行分组，发现它们属于同一系列，于是全部列入。另外，自动禁用是 ClassIsland 的行为：当 AdvancedTimeIsland 上报 blamedPlugins 后，ClassIsland 根据设置自动禁用所有被归因的插件。所以“穷举所有男娘插件全部禁用”是因为 AdvancedTimeIsland 把所有匹配到的插件都放进了 blamedPlugins，而 ClassIsland 对列表中的每个插件执行了禁用。为什么打开 JK 制服页面会触发？因为该页面的 OnTabSelectionChanged 在切换到 JK 制服标签时，会检查该分类的图片资源。如果资源不存在，它认为有插件冲突或干扰，于是执行冲突检测和归因。可能 AdvancedTimeIsland 设计上认为“女装”功能不应该有多个插件同时提供或干扰，所以一旦发现资源缺失，就怀疑是其他同类插件抢占或覆盖了资源，于是把所有同类插件列为嫌疑并禁用。测试插件 MatchTest 系列就是专门用来验证这种匹配和禁用逻辑的，所以它们被全部捕获。我们应解释：这不是穷举所有已安装插件，而是穷举所有名称/元数据符合“男娘/femboy”模式的插件；MatchTest 系列正是为此构造的变体，所以全部命中。另外，可能 AdvancedTimeIsland 内部维护了一个“冲突插件名单”或使用正则匹配，当检测到“JK制服”图片缺失时，遍历插件列表，匹配到相关关键词就加入 blamedPlugins。由于测试插件覆盖了各种变体，匹配规则如果较宽泛（如包含“fem”、“boy”、“男”、“娘”、“娘”、“femboy”等），就会全部命中。自动禁用是 ClassIsland 的机制，对 blamedPlugins 中的所有插件执行禁用。

需要组织回答：先明确问题核心，然后分点：1. 触发点：JK制服页面检查资源缺失；2. AdvancedTimeIsland 的归因逻辑：主动构造 blamedPlugins，基于插件名称/ID/元数据匹配；3. 为什么是“所有男娘插件”：MatchTest 系列是变体测试，匹配规则覆盖了这些变体，所以全部命中；4. 为什么全部禁用：ClassIsland 对 blamedPlugins 列表逐个自动禁用；5. 为什么打开页面就触发：OnTabSelectionChanged 在切换到该标签时执行检查，资源缺失即触发归因流程。6. 这不是技术崩溃，而是设计上的冲突处理策略。可以提到 TraceID 相同、时间不同说明重复触发。最后总结：是 AdvancedTimeIsland 的冲突检测/归因逻辑 + ClassIsland 的自动禁用机制共同作用，测试插件恰好覆盖了匹配模式，导致全部被列入并禁用。

注意不要给解决方案，只分析原因。语言要清晰。可以适当引用日志中的证据：BuildCrashInfo 参数 blamedPlugins，插件 ID 前缀 MatchTest，名称变体等。避免过度推测，但可以合理推断。要回答“为什么打开JK制服页面会穷举所有男娘插件全部禁用”。强调“穷举”是匹配规则的结果，不是真的遍历所有插件无差别禁用。匹配规则可能基于关键词、相似度、命名空间等。MatchTest 系列就是用来测试匹配覆盖面的，所以全部被命中。最终自动禁用是 ClassIsland 行为。

我们还需要注意，用户可能认为这是 AdvancedTimeIsland 在“穷举所有男娘插件”，我们要解释机制。可以分层次：AdvancedTimeIsland 的 WomenswearPage 在 OnTabSelectionChanged 中，当选中“JK制服”时，会尝试加载对应图片资源。如果资源缺失，它认为这是异常状态，并调用 ShowHostCrashWindow -> BuildCrashInfo。BuildCrashInfo 接收 blamedPlugins 列表，这个列表是之前通过某种冲突检测逻辑生成的。检测逻辑可能扫描所有已安装插件，用正则或关键词匹配“femboy”、“男娘”、“女装”等。MatchTest 系列覆盖了各种变体，所以全部匹配。然后 BuildCrashInfo 抛出异常，ClassIsland 捕获后显示崩溃窗口，并根据 blamedPlugins 自动禁用这些插件。因此，打开页面触发检查 -> 检查失败 -> 归因所有匹配插件 -> 全部禁用。

可以提到，为什么第一份只有 FemboyTest，后来变成 6 个、11 个？因为用户安装的测试插件越来越多，或者匹配规则被逐步放宽，导致更多变体被命中。第三份日志中 MatchTest.01-10 全部出现，说明匹配逻辑几乎覆盖了所有构造的变体。这进一步证明是匹配算法在起作用。

最后回答要直接针对问题：为什么打开 JK 制服页面会穷举所有男娘插件全部禁用？因为 AdvancedTimeIsland 在该页面内置了针对“女装/男娘”类插件的冲突检测与归因逻辑，当“JK制服”资源缺失时，它会触发该逻辑，将所有名称/ID/元数据符合匹配模式的插件列为责任方，并交由 ClassIsland 自动禁用。MatchTest 系列测试插件正是为了验证这种匹配覆盖面而设计的，所以被全部命中并禁用。这不是技术崩溃，而是插件冲突处理策略的体现。
<end_of_thinking>
打开“JK 制服”页面之所以会把所有“男娘”相关插件全部列入并自动禁用，是因为 `AdvancedTimeIsland` 在这个页面里内置了一套**插件冲突检测与归因逻辑**，而 `ClassIsland` 又会根据归因结果自动禁用被点名的插件。整个过程可以拆成几个环节来看。

### 1. 触发点：切换到“JK 制服”标签时检查图片资源
从调用栈看，异常是在：

```text
WomenswearPage.OnTabSelectionChanged
-> WomenswearPage.ShowHostCrashWindow
-> WomenswearPage.BuildCrashInfo
-> throw System.Exception("There is no pictures of JK uniform!")
```

也就是说，当你在“女装”页面里选中“JK 制服”这个标签时，`WomenswearPage` 会去检查“JK 制服”对应的图片资源。如果它认为这些图片不存在或不可用，就会进入 `ShowHostCrashWindow()`，再调用 `BuildCrashInfo()` 构造崩溃信息。所以，**打开页面本身就会触发一次资源检查**，而资源缺失就是整个归因流程的起点。

### 2. `BuildCrashInfo` 不是从堆栈里找凶手，而是接收一个“责任插件列表”
调用栈里最关键的一行是：

```text
AdvancedTimeIsland.Views.Settings.WomenswearPage.BuildCrashInfo(IReadOnlyList`1 blamedPlugins, Boolean blamedDisabled)
```

`blamedPlugins` 是一个 `IReadOnlyList`，说明“哪些插件有问题”是**调用方传进来的**，不是 ClassIsland 从异常堆栈里自动分析出来的。整个堆栈里也没有出现任何 `FemboyTest`、`MatchTest` 的命名空间或方法。因此可以确定：

- 直接抛异常的是 `AdvancedTimeIsland` 自己；
- 被点名的插件并没有在运行时真的抛异常；
- 它们被列入名单，是 `AdvancedTimeIsland` 的匹配逻辑判断的结果。

### 3. 匹配逻辑很可能基于插件名称/ID/元数据中的“女装/男娘/femboy”语义
三份日志里被归因的插件列表变化很说明问题：

- 第一份：只有 `FemboyTest`
- 第二份：`FemboyTest` + `MatchTest.02/04/06/08/10`
- 第三份：`FemboyTest` + `MatchTest.01` 到 `.10` 全部

这些 `MatchTest` 插件的 ID 都是 `inf2147483647.MatchTest.01` 到 `.10`，名称则覆盖了：

- 大小写变体：`Femboy` / `FEMBOY`
- 分隔符变体：`Fem·boy`、`男·娘`
- 全角字符：`Ｆｅｍｂｏｙ`
- 空格插入：`F e m b o y`
- 中文同义词：`男娘`
- 日文写法：`男の娘`
- 中文同音/近音字：`南梁`、`楠酿`

这明显是一组**专门构造的测试插件**，用来穷举“femboy / 男娘”在不同语言、字符集、分隔符、全角半角、同音字下的写法。`MatchTest` 这个命名本身就指向“匹配测试”。因此，`AdvancedTimeIsland` 的归因逻辑很可能在扫描已安装插件时，使用了一套较宽泛的关键词或模糊匹配规则，例如匹配 `femboy`、`fem`、`boy`、`男娘`、`娘`、`南梁`、`楠酿` 等语义相关模式。只要插件名称、ID 或元数据命中这些模式，就会被加入 `blamedPlugins`。

第三份日志里 `MatchTest.01–10` 全部出现，说明这套匹配逻辑覆盖了几乎所有构造出来的变体，所以“穷举所有男娘插件”并不是真的无差别遍历所有插件，而是**所有被匹配规则命中的同类插件都被列入了责任名单**。

### 4. 为什么全部被自动禁用？因为 ClassIsland 会按 `blamedPlugins` 逐个禁用
`BuildCrashInfo` 抛出异常后，ClassIsland 捕获并显示宿主崩溃窗口。日志里写：

```text
以上异常插件已自动禁用，重启应用后生效。
```

这是 ClassIsland 的插件管理机制：当 `AdvancedTimeIsland` 上报的 `blamedPlugins` 列表里包含多个插件时，ClassIsland 会**对列表中的每一个插件执行自动禁用**。所以，只要 `AdvancedTimeIsland` 把 `MatchTest.01–10` 全部列入，ClassIsland 就会把它们全部禁用。你看到的“全部禁用”是归因列表被完整执行的结果。

### 5. 为什么打开“JK 制服”就会触发这一整套流程？
因为 `WomenswearPage` 在标签选择变化时，会执行“JK 制服”分类的资源检查。这个检查可能期望某个特定图片资源存在，比如由 `AdvancedTimeIsland` 自身提供，或者由它认可的某个插件提供。当它发现资源缺失时，它不认为这是简单的配置问题，而是怀疑有同类插件干扰、覆盖或抢占了资源。于是它启动冲突检测，扫描所有“女装/男娘”语义相关的插件，把它们列为责任方，再通过宿主崩溃窗口上报给 ClassIsland。`MatchTest` 系列测试插件恰好覆盖了匹配规则的所有变体，所以每次打开这个页面，都会把它们全部命中并列入禁用名单。

### 6. 总结
打开“JK 制服”页面会穷举所有男娘插件并全部禁用，是因为：

- `AdvancedTimeIsland` 的 `WomenswearPage` 在切换到“JK 制服”标签时会检查图片资源；
- 资源缺失时，它主动调用 `BuildCrashInfo`，并传入一个由自身匹配逻辑生成的 `blamedPlugins` 列表；
- 该匹配逻辑基于“女装/男娘/femboy”语义扫描已安装插件；
- `MatchTest.01–10` 等测试插件覆盖了各种名称变体，全部命中匹配规则，因此被全部列入；
- ClassIsland 接收到 `blamedPlugins` 后，按列表逐个自动禁用；
- 所以最终表现为“打开 JK 制服页面 → 所有男娘相关测试插件被穷举并全部禁用”。

这不是技术层面的运行时崩溃，而是 `AdvancedTimeIsland` 的冲突检测与归因策略，叠加 ClassIsland 的自动禁用机制，共同产生的结果。`MatchTest` 系列插件正是为了验证这套匹配逻辑的覆盖面而设计的，所以它们被全部捕获并禁用。