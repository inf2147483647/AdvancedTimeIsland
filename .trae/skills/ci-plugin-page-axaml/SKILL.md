---
name: ci-plugin-page-axaml
description: Migrate AdvancedTimeIsland Avalonia pages to axaml plus code-behind with official ClassIsland style, Fancy animation, and FA2/FA3 support. Use when asked to 改造 or 迁移 a page to axaml.
---

# CI 插件页面 axaml 化迁移

把 AdvancedTimeIsland（ClassIsland 2.0 插件，Avalonia 主项目）中纯 C# 构建 UI 的页面迁移为 `Page.axaml` + `Page.axaml.cs`，写法对齐 ClassIsland 官方设置页。

## 1. 迁移前判定（先做，不要直接动手）

1. **只处理 Avalonia 主项目**（仓库根目录 `Views/`、根 `AdvancedTimeIsland.csproj`）。`AdvancedTimeIslandWPF/` 是独立 WPF 变体，各有同名文件，**永不改动**。
2. 确认页面基类与调用方，再定方案：
   - `SettingsPageBase` 的普通设置/隐藏页 → 整页 axaml 化。
   - `UserControl` 等内嵌控件（如被 Tab/ContentControl 承载的页）→ axaml 根元素用对应控件类型。
   - **带 100+ 子类的基类或运行时内容引擎**（如 `HanfuPageTemplate` 的 Markdig 渲染、动态生成长文/多图页）→ **只迁移静态外壳**（Grid/ScrollViewer/标题/命名宿主容器），渲染引擎原样留在 code-behind；子类与 protected 契约（字段名、`BuildContent`、延迟构造函数）全部保持不变。
3. 迁移前 grep `new XxxPage`、页面 Id、构造函数调用点，确认删除旧 `.cs` 后调用方零改动。
4. 检查页面元素是否含 FA2/FA3 类型名不同的控件（见第 3 节），提前选兼容方案。

## 2. axaml 文件写法（对齐官方）

1. 设置页根元素：

   ```xml
   <ci:SettingsPageBase x:Class="AdvancedTimeIsland.Views.Settings.XxxPage"
       xmlns="https://github.com/avaloniaui"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
       xmlns:ci="clr-namespace:ClassIsland.Core.Abstractions.Controls;assembly=ClassIsland.Core">
   ```

2. 标准骨架：`ScrollViewer`（HorizontalScrollBarVisibility=Disabled, VerticalScrollBarVisibility=Auto, BringIntoViewOnFocusChange=False）→ `StackPanel Classes="settings-container animated-intro"`。
3. **华丽动画**：`settings-container animated-intro` 是宿主全局样式类，宿主动画等级为“华丽”时子项依次淡入上滑，低等级下不生效、静态正常。
   - 只让**轻量静态元素**（标题、开关卡片、折叠分组、少量按钮）参与。
   - **禁止**把几十上百个动态生成段落、20+ 异步网络图列表放进 animated-intro 容器（持续重排会卡顿）。重内容页只给标题/警告栏区加动画，图片/长文用独立命名宿主容器。
   - 注意区分：这是元素入场动画；TabControl 的 `PageTransition`（FA3/Avalonia12 切 Tab 整页过渡）是另一回事，重内容 Tab 页应通过反射置空禁用，两者不冲突。
4. 页面属性特性（`[SettingsPageInfo(...)]`、`[Group(...)]` 等）继续写在 code-behind 的 partial 类上，**Id、Guid、分类、标题保持不变**，Plugin.cs 注册无需改动。
5 参考样板：`Views/Settings/FloatingScheduleSettingsPage.axaml`（设置项分组）、`DebugPage.axaml`（InfoBar 宿主 + 简单行）、`WomenswearPage.axaml`（头部动画 + 动态内容区）、`EasterEggPage.axaml`（UserControl 外壳 + 多宿主 + 重内容不入场）。

## 3. FA2 / FA3 兼容（关键）

主项目双目标框架：net8 = Avalonia 11 + FluentAvalonia 2.4（`SettingsExpander`/`InfoBar`），net10 = Avalonia 12 + FluentAvalonia 3.0（改名 `FASettingsExpander`/`FAInfoBar`）。一份 axaml 不能按两个类型名声明同一控件。

1. **InfoBar**：axaml 中放 `<ContentControl x:Name="XxxHost" />`；code-behind 用 `FluentAvaloniaCompatibilityHelper.CreateInfoBar()` 创建真实控件，再用 `SetInfoBarProperty(...)` 设 Severity/Message/IsOpen/IsClosable/Margin，需要关闭回调时用 `AddInfoBarClosedHandler`，最后 `XxxHost.Content = bar`（不显示就赋 null，ContentControl 不占位）。
2. **设置分组 SettingsExpander**：优先复用现成包装 `Views/Controls/CompatSettingsExpander.cs`，axaml 中直接声明 `<compat:CompatSettingsExpander>` / `<compat:CompatSettingsExpanderItem>`（命名空间 `clr-namespace:AdvancedTimeIsland.Views.Controls`），它在运行时创建真实官方控件并转发 Header/Description/Footer/Items/IsEnabled，官方展开折叠动画完整保留。注意包装条目类型必须继承 `Control`（只有 Control 派生类型才会生成 x:Name 字段）。
3. 其他 FA 差异控件同样走“宿主容器 + Helper 运行时创建”模式；新差异先在 `FluentAvaloniaCompatibilityHelper` 中补创建/属性转发方法。

## 4. code-behind 写法

1. 类改为 `public partial class XxxPage : SettingsPageBase`（或 UserControl），构造函数中先 `InitializeComponent();` 再执行 `WireUI()`（或保留原初始化方法名）。
2. 事件处理器在 axaml 中用 `Click=` / `SelectionChanged=` / `IsCheckedChanged=` 等挂接，签名严格匹配 Avalonia 事件参数类型（如 `SelectionChangedEventArgs`、`NumericUpDownValueChangedEventArgs`、`TextChangedEventArgs`、`PointerPressedEventArgs`）。
3. **初始化顺序**：先在 axaml 或 WireUI 中赋初值，**再订阅**会联动改值的事件（TabStrip、ToggleSwitch 等），避免初始化误触发。
4. 动态内容（Markdown/网络图/对话框/反射宿主页面）逻辑原样平移到 code-behind；axaml 只留命名宿主（`x:Name` 字段）供填充。
5. 保留原构造函数重载与可空注入形式（如 `XxxPage(PluginSettings?) : this()`），宿主 DI 与手动 new 两条路径都要能走通；无参构造回退 `Plugin.Instance?.Settings`。
6. 主题自适应：硬编码画刷集中走 `ThemeHelper`；在 `Loaded`/`OnInitialized` 订阅 `Application.Current.ActualThemeVariantChanged`，在 `Unloaded`/`OnDetachedFromVisualTree` 成对退订；DispatcherTimer、服务事件（如 LeakUpdated、轮询）同样在离开页面时停止/退订，防泄漏。
7. 所有 I/O、网络、图片解码保持异步/后台线程（沿用既有实现，不要在迁移中改成同步）。

## 5. 从旧 .cs 平移大文件的做法

- 静态外壳（Grid/ScrollViewer/面板/固定标题）删掉改由 axaml 承载，对应字段换成 axaml 生成的 x:Name 字段；
- 先 `Copy-Item Xxx.cs Xxx.axaml.cs` 再删除旧文件，保留 90% 逻辑零改动，只做：`partial`、移除外壳构建代码、字段改名对齐 x:Name、容器填充改指向命名宿主；
- 文件可能被 IDE 短暂锁定（EBUSY），重试写入即可；大段替换优先用 PowerShell 按行索引精确切割。

## 6. 构建与验证（必须全部通过）

1. 双框架 Debug 编译（XAML 编译器会严格校验类型/属性/事件/命名，是最强验证）：

   ```powershell
   dotnet build AdvancedTimeIsland.csproj -c Debug -f net8.0  -p:TargetFramework=net8.0  --nologo
   dotnet build AdvancedTimeIsland.csproj -c Debug -f net10.0 -p:TargetFramework=net10.0 --nologo
   ```

2. 高频缺失 using：`Avalonia.Layout`（HorizontalAlignment/VerticalAlignment 枚举，缺失时 CS0176 报成实例属性冲突）、`Avalonia.Controls.Primitives`（ScrollBarVisibility、TemplatedControl）。
3. 打包验证（含 WPF 回归）：

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\Build-Package.ps1 -Target both -NoServerShutdown
   ```

   要求 `net8.0 compat`、`net10.0 new`、`wpf` 三行全部 **SUCCESS**。
4. 迁移后 grep 确认旧文件名无残留引用；GetDiagnostics 为 0；在交付说明中列出实际增删文件、动画取舍、是否有破坏性更改（默认应做到无破坏性）。
