---
name: fluentavalonia-compat
description: 排查并修复 AdvancedTimeIsland 折叠栏 / Expander / SettingsExpander 等 FluentAvalonia 控件只在 net8（FA2）或只在 net10（FA3）异常的兼容问题。Use when 同一控件行为在两个宿主版本不一致、反射双发未生效、或需要确认 FA 内部类型名 / 模板部件名 / 动画机制。不用于纯 ClassIsland 宿主 API 差异或页面迁移。
---

# FluentAvalonia FA2/FA3 兼容排查

本插件同一份代码要同时跑在两套运行时上：

| 目标框架 | ClassIsland SDK | FluentAvalonia | Avalonia |
|---|---|---|---|
| net8.0（compat） | 2.0.1.1 | 2.4.1（FA2） | 11.3.x |
| net10.0（Misha） | 2.1.1.1 | 3.0.0（FA3） | 12.1.x |

FA2 与 FA3 经常是**同功能、不同类型名/不同实现**，只按其中一个版本写代码，另一个版本就会静默失效（`Type.GetType` 找不到返回 null，反射调用被跳过）。

## 排查流程

1. **确认版本归属**：读 `obj/project.assets.json` 里 `FluentAvaloniaUI/x.y.z` 与 `Avalonia/x.y.z`，确定每个 TFM 实际解析到的包版本（不要凭记忆）。
2. **定位本机包缓存**：`C:\Users\seewo\.nuget\packages\fluentavaloniaui\<版本>\lib\`（FA2 取 `net6.0` 目录资产）。
3. **反编译比对**（不要靠猜字符串搜索——编译型 XAML 的部件名在 #US 堆里是 UTF16，直接对 DLL 做 UTF8 `Contains` 会漏检）：
   - 单类型：`ilspycmd <dll> -t <Full.Type.Name> -o <临时目录>`（ilspycmd 已全局安装，PATH 在 `$env:USERPROFILE\.dotnet\tools`）
   - 找模板部件名：先 `ilspycmd <dll> --list c | Select-String <关键词>` 定位编译 XAML 类，再全量反编译后在产物里 Grep（如 `ExpanderContent`、`ContentHost`）
   - 临时目录用完必须删除（放 `D:\temp\`，勿放工作区）
4. **修复模式**（按场景选一）：
   - **反射双发**：对 FA2/FA3 各自的静态类型名依次 `Type.GetType` + 反射调用，存在哪个用哪个（见 `IntroAnimationHelper.PrepareInnerExpander` 对 `FAExpanderExt` / `ExpanderExt` 的循环）。
   - **版本判定**：用 `FluentAvaloniaCompatibilityHelper.IsV3` 选择类型名/创建控件（`CreateSettingsExpander` 等）。
   - **属性/方法不存在**：Avalonia 11 没有的属性（如 `TabControl.PageTransition`、`CompositionVisual.Translation`）一律反射访问 + null 检查，两个 TFM 共用一份代码。
5. **验证**：`pwsh -NoProfile -ExecutionPolicy Bypass -File .\Build-Package.ps1 -NoServerShutdown`，三个变体全部 SUCCESS 才算完成。

## 已知 FA2/FA3 差异（持续累积）

- **SettingsExpander 类型名**：FA2 `SettingsExpander` / `SettingsExpanderItem`；FA3 改名 `FASettingsExpander` / `FASettingsExpanderItem`。axaml 里一律用 `CompatSettingsExpander` 包装。
- **Expander 内置 WinUI 滑动动画**：FA2 由 `FluentAvalonia.UI.Controls.ExpanderExt` 承载（附加属性 `ExpanderAnimationType="FluentV2"` 启用，展开 333ms / 收起 167ms，直接对模板 `ExpanderContent` 的 `TranslateTransform` 跑 `Animation`，`FillMode=Forward`）；FA3 是 `FAExpanderExt`。两者都有静态 `SetExpanderAnimationType(expander, null)` 可关闭——**必须两个类型名都试**，只关一个会导致另一版本上"宿主高度过渡 + 内置滑动 + 自建位移"三方抢同一属性，表现为抽动/错位/卡住。
- **空内容折叠栏点击**：FA2/FA3 都是先置 `IsExpanded=true` 再 `Dispatcher.Post(IsExpanded=false, Send)` 夹回；**同步夹回会让内部 Expander 的 `:expanded` 伪类卡住**，残留内容区顶部约 1px 分隔线。正确做法：订阅内部 `Expander.Expanding`（Avalonia 11.3/12 都有），`ItemCount==0` 时 `e.Cancel=true`，属性根本不变 true。
- **模板内容块命名**：FA2/FA3 的 Expander 模板内容块都叫 `ExpanderContent`（`Border`），可统一接管其 `RenderTransform`。
- **FA2 的 ExpanderExt 是全局的**：FA2 主题给**所有** `Expander`（不止 SettingsExpander，含页面 axaml 直接声明的原生 Expander）都设了 `ExpanderAnimationType="FluentV2"`（反编译见 ControlTheme(Expander) 的 Setter）。所以接管原生 Expander 时同样要关它，否则 FA2 内置动画与宿主抢 `ExpanderContent`。FA3 主题不给原生 Expander 设该动画，故 net10 正常、net8 异常。
- **`_inner` 本身就是 Expander 时找不到内部结构**：`GetVisualDescendants()` **不含自身**。页面直接 `CreateExpandHost(expander)` 包原生 Expander 时，查找内部 Expander 必须先 `_inner as Expander ?? 后代查找`，否则 `PrepareInnerExpander` 整段跳过（关动画、挂 Expanding 钩子、接管位移全不生效）。
- **关掉 FA2 ExpanderExt 后的残留状态**：`ExpanderInfo` 在收起动画里把 `ExpanderContent.IsVisible=false`、并在模板应用时给 `ExpanderContentClip` 设 `ClipToBounds=true`。`SetExpanderAnimationType(null)` 触发 `Dispose` 后，前者残留 `false`（宿主引脚 IsExpanded=true 时期望内容挂载 → 内容空白）、后者裁剪丢失（收起内容上滑盖到标题）。接管后需自行补：`ExpanderContent.IsVisible=true` + `ExpanderContentClip.ClipToBounds=true`（找不到 Clip 就退而裁 `ExpanderContent`）。
- **内容站点命名**：FA 主题（FA2/FA3）是 `Border#ExpanderContent`；Avalonia 原生 Expander 主题是 `ContentPresenter#PART_ExpandSite`。axaml 直接声明原生 Expander 的页面（如 AttendanceCalendarPage）两个都要认。
- **初始即展开（IsExpanded="True"）时折叠高度从未被测过**：`_collapsedHeight` 只在静止折叠态缓存；收起流程先把内部引脚回展开，若缓存是 NaN，`MeasureInnerHeight` 测出的是展开全高 → host 高度目标不变，收起后留下整块空白（箭头已收起但内容区高度还在）。修复：Loaded / 首次有效宽度时，临时把内容站点 `IsVisible=false` 做一次同步离屏测量缓存标题行高度再恢复（无渲染帧、无闪烁）。
- **收起提交后的高度校正不能立即测、更不能带动画**：`PinInnerExpanded(false)` 后立即 `Measure`，内部 Expander 的模板绑定 / `:expanded` 伪类 / 布局尚未走完，拿到的是“半折叠”的中间高度（接近展开全高）；若再走带 `DoubleTransition` 的 SetHeight，容器会反向播放、“收起后又打开一点”留下空白条。正确做法：`Dispatcher.UIThread.Post(..., DispatcherPriority.Background)` 等本轮布局完成后再测，并临时清空 `_host.Transitions` 做**无过渡吸附**（snap）落位；回调里还要检查期间用户是否已重新展开（`_desiredExpanded || ReadInnerExpanded()` 则放弃）。
- **关掉 ExpanderExt 后 ExpanderContent 可见性必须自己接管**：FA2 模板里 `ExpanderContent` 的 IsVisible 全靠 ExpanderExt 打的 `:noAnimation` 伪类切换（折叠态隐藏）。ExpanderExt 被 dispose 后伪类不再切换，折叠态内容站点回到默认可见 → 内部测量仍含内容，折叠缓存被污染成“标题+部分内容”（实测 48→96，空白 48px）。规则：prepare 时按 `IsExpanded` 设初值；展开/收起动画期间置 true（由 host 高度裁剪）；`OnCollapseCommit` 真实折叠后置 false，此后测量才是纯标题高度。
- **收起途中再点“展开”无 IsExpanded 事件**：收起动画期间内部被引脚为 IsExpanded=true，此时用户点展开（true→true）不触发 PropertyChanged，旧逻辑“靠 IsExpanded 翻转目标”失效。改用内部 Expander 的 `Expanding` 事件（coerce 阶段无条件触发，值未变也会发）补翻转；PropertyChanged 处理器改为按 `ReadInnerExpanded()` 实际值同步目标，值与目标一致即跳过（与 Expanding 去重，避免双翻）。
- **headless 验证姿势**：`Avalonia.Headless.XUnit` 的 `[AvaloniaFact]` + 真实 `FluentAvaloniaTheme` 时，默认 headless 绘制无法创建 FA 的 Symbols 内嵌图标字体（"Could not create glyphTypeface. Font family: Symbols"）；需 `.UseHeadless(...).UseSkia()`（headless 窗口 + Skia 真渲染，注意顺序，Skia 在后）。可反射读取私有 `HostStates`/`ExpandHostState` 字段断言 `_collapsedHeight`/`_fullHeight`/host.Bounds.Height，动画等待 ≥900ms（收起提交定时器 333ms）。
- **ClassIsland.Core 类型**：`CompositionPageTransition`、`ReusableTabControl` 只存在于宿主源码/新版，2.0.1.1 与 2.1.1.1 两个 NuGet 包里都没有 `CompositionPageTransition`——需要该效果时自行实现（参考 `IntroAnimationHelper.EnablePageFlip`）。

## 注意事项

- 修改 `IntroAnimationHelper` / `CompatSettingsExpander` 时保持"找不到内部结构就静默退化"的容错风格，不要抛异常。
- 反射结果可缓存（`FluentAvaloniaCompatibilityHelper.FaTypeCache` 已有记忆化先例），汉服页等高频页面会反复触发类型解析。
