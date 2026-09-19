# 时间表悬浮窗 · 独立进程模式 实施计划

## Context（背景）

ClassIsland 进程内的时间表悬浮窗（[FloatingScheduleService.cs](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/Services/FloatingScheduleService.cs)，约 4500 行）有两个痛点：
1. 易被"弹窗拦截"类工具按宿主进程/窗口特征拦截；
2. ClassIsland 主程序卡顿时悬浮窗跟着卡。

方案：新增独立进程 `AdvancedTimeIslandFloatSchedule.exe` 渲染悬浮窗。exe 为框架依赖、不捆绑 Avalonia，运行时从 ClassIsland 安装目录借用 Avalonia/Skia DLL（严格控制包体积）。设置页新增"独立进程模式"折叠栏：独立进程模式 / 随机进程名 / 强制重启进程(按钮) / 单实例保护 / 跟随启停。仅 Windows 支持，其他平台禁用。

## 已确认决策

- 仅实现 CI2（Avalonia 主项目，net8+net10 两个包）；AdvancedTimeIslandWPF（CI 1.x）不做。
- 独立进程模式默认**关闭**；单实例保护、跟随启停默认**开**；随机进程名默认**关**。
- 跟随启停=关 时：宿主退出后子进程**冻结显示最后课表**（进度按本地时间续走），ClassIsland 重启后插件自动重连恢复推送。
- 跟随启停=开 时：ClassIsland **正常退出与异常退出（崩溃/被强杀）子进程都必须消失**——双保险：① 插件 StopAsync 主动发 shutdown+兜底 Kill（覆盖正常退出）；② 子进程侧宿主进程句柄看门狗（覆盖崩溃/强杀，见第 4 步）。
- 子进程启动失败（如宿主目录缺 DLL）→ 设置页状态文本提示，并**自动回退进程内渲染**。
- **立即生效原则**：所有新设置与既有悬浮窗设置在独立模式下均即时生效，**无需重启 ClassIsland**——① 悬浮窗样式/行为项（不透明度/字号/层级/点击穿透/防截图/贴边隐藏/悬停淡化/随机标题/隐藏模式/位置等）经管道 `settings/model/visible` 消息热应用（复用现有 `OnSettingsPropertyChanged` 逐属性分发，独立模式下自动转为"重发快照"）；② 独立进程模式开关→立即启动/停止子进程并切换渲染汇；③ 随机进程名→立即重启子进程（<2s 窗口短暂重建，非重启 CI）；④ 跟随启停→**经 settings 消息热更新**（看门狗触发时读取最近缓存值，连子进程都不必重启）；⑤ 单实例保护→存值，自下次子进程启动生效（Mutex 仅在启动时获取，无需任何重启）；⑥ 强制重启进程按钮→即时。

## 关键技术事实（已核实）

- 主项目双 TFM 对应不同 Avalonia 代际：net8.0 → ClassIsland.Core 2.0.1.1 → **Avalonia 11.3.x**；net10.0 → 2.1.1.1 → **Avalonia 12.1.1**（`obj\project.assets.json` 已证实）。Avalonia 程序集版本随包版本走，跨代二进制不兼容 → **子进程双 TFM 各出一个 exe**，每个 cipx 只装与宿主代际匹配的 exe。
- ClassIsland 2.x 官方发布为框架依赖、目录式发布，安装根目录必含全部 Avalonia/SkiaSharp 托管 DLL 与 `libSkiaSharp.dll`（保险起见同时探测 `runtimes\win-x64\native`）。
- 打包脚本 `Create-CipxPackage`（[Build-Package.ps1](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/Build-Package.ps1) L92-104）全量复制输出目录（仅排除 pdb/cipx/zip/RID 子目录）→ exe 拷入输出目录即自动进包。
- `Shared\` 目录已存在且被主项目默认 glob 编入 → 共享源文件主项目侧零 csproj 改动。
- 子进程**不引用** FluentAvalonia / ClassIsland.*，只用 Avalonia 基础控件 + System.Text.Json + System.IO.Pipes（全部零额外体积）。

## 架构

```
ClassIsland 进程                                        子进程 (FloatSchedule.exe)
┌─────────────────────────────────────┐   命名管道(JSON Lines)  ┌──────────────────────────┐
│ FloatingScheduleService              │ ─── init/settings/ ───▶ │ FloatScheduleChildWindow  │
│  ├─ 数据收集(反射宿主服务, 原样保留)   │     model/progress/     │  └─ FloatScheduleRenderer │
│  ├─ BuildRenderModel() ◀─ 新增单一出口│     visible/shutdown ─▶ │     (共享源码, model→控件) │
│  ├─ 进程内路径: Renderer.Build(model) │ ◀── ready/position ──── │ 本地: 拖拽/贴边/淡化/      │
│  └─ 独立路径: hostProcess.SendModel   │                         │  层级/穿透/防截图/随机标题 │
│ FloatScheduleHostProcessService 新增  │                         │ 断线→冻结+本地进度续走     │
│  管道服务器/启动/adopt/重启/状态机     │                         └──────────────────────────┘
└─────────────────────────────────────┘   程序集解析: ALD.Resolving + PATH → --host-dir
```

防漂移核心：`Shared\FloatingSchedule\` 源文件同时编入主项目与子进程；"数据→模型"只有 `BuildRenderModel()` 一处，"模型→控件"只有 `FloatScheduleRenderer` 一处，两种模式共用。

## 实施步骤

### 第 1 步：骨架
- 新建 `AdvancedTimeIslandFloatSchedule\AdvancedTimeIslandFloatSchedule.csproj`：双 TFM `net8.0-windows;net10.0-windows`，WinExe，`RollForward=LatestMajor`，`PublishSingleFile`+`SelfContained=false`，`AvaloniaVersion` 按 TFM 条件切换（net8→11.3.10，net10→12.1.1，实施时以 net8 restore 实际传递版本为准），三个 Avalonia 包均 `ExcludeAssets="runtime;native"`，`<Compile Include="..\Shared\FloatingSchedule\**\*.cs" LinkBase="Shared" />`，定义 `WINDOWS` 常量。
- `app.manifest`（PerMonitorV2 DPI）+ 空 `Program.cs`/`FloatScheduleApp.cs`。
- **进程图标与插件图标一致（压缩优先）**：从根目录 `icon.png`（353KB）一次性生成**极简压缩**的 `AdvancedTimeIslandFloatSchedule\Assets\app.ico`——仅含 16/32/256 三档（48 由 256 缩放覆盖）、PNG 编码 + 色彩量化（256 色以内），目标 **≤ 15-20KB**，随仓库提交；csproj 设 `<ApplicationIcon>Assets\app.ico</ApplicationIcon>` → exe 文件图标与任务管理器进程图标即插件图标（随机进程名的 %TEMP% 副本同样继承）；子进程窗口 `Window.Icon` 复用同一小 ico（EmbeddedResource 字节流构造，不再另存大图）。
- **exe 体积最小化**：子进程 csproj 追加 `<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>`（压缩单文件包内容）+ `<DebugType>none</DebugType><DebugSymbols>false</DebugSymbols>`（Release 不带 pdb/symbols）+ `<SatelliteResourceLanguages>en</SatelliteResourceLanguages>`；目标产物 exe **≤ ~400KB**（apphost 固定开销 + 压缩后的自身 dll + 小图标）。
- 主 [AdvancedTimeIsland.csproj](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/AdvancedTimeIsland.csproj) 加 `<Compile Remove="AdvancedTimeIslandFloatSchedule\**\*.cs" />` 等排除（仿现有 WPF 排除段 L100-107）。
- sln 添加新项目；验证 `dotnet build` 双项目通过。

### 第 2 步：共享层（`Shared\FloatingSchedule\`，namespace `AdvancedTimeIsland.Shared.FloatingSchedule`；只允许 using System/Avalonia，禁止 ClassIsland/PluginSettings/ThemeHelper 类型）
- `FloatScheduleRenderModel.cs`：渲染模型 POCO。字段：`FontSize`、预解析 ARGB 颜色组（`AccentArgb/CardBackgroundArgb/BorderArgb/TextArgb/SubTextArgb/HighlightArgb/BreakRowBackgroundArgb/SeparatorArgb`）、`ShowTomorrow`、`PlaceholderText`、`Rows[{Course,Teacher,TimeText}]`、`CurrentClassIndex`、`Break{AfterClassIndex,Name,TimeText}`、`SeparatorAfterClassIdx`、`ClassProgressRatio/BreakProgressRatio`、`Anchor{ClassStartSec/ClassEndSec/BreakStartSec/BreakEndSec/NowSecOfDay}`（冻结续走用）。
- `FloatScheduleRenderer.cs`：`static (Control root, ProgressRefs refs) Build(model)`；迁移 service 中 `CreateSelfDrawnProgressBar`/`ApplyProgressRatio`（L3050-3104）。仅用 Avalonia 11/12 均稳定的基础 API，必要时 `#if NET10_0_OR_GREATER` 兜差异。
- `FloatScheduleWindowSettings.cs`：窗口行为快照 POCO（Layer/TopmostRefreshMode/ClickThrough/HoverFade(+Reverse)/EdgeHide(+Delay)/RandomTitle(+Enhanced)/PreventCapture/PositionX/Y/Visible/Opacity/**FollowHostLifetime**——经 settings 消息热更新，供看门狗触发时读取最新值）。
- `FloatScheduleIpc.cs`：管道名常量 `AdvancedTimeIsland.FloatSchedule.v1`、协议版本、`enum IpcMsgType {Init,Settings,Model,Progress,Visible,Shutdown,Ready,Position,Error}`、JSON Lines 编解码。
- `FloatScheduleNative.cs`：Win32 帮助类（语义复制 service L33-160、L2107-2277：ExStyle 计算/应用、层级重设含竞争限频、`SetWindowDisplayAffinity`、`GetCursorPos`）。
- service 改为调用迁移后的 Renderer/Native 函数（行为等价重构），主项目构建通过。

### 第 3 步：模型拆分（对 FloatingScheduleService 的最小侵入改造）
- `RefreshSchedule`（L3159）：数据收集段（L3163-3436）原样保留；UI 构建段（L3438-3782）替换为 `BuildRenderModel(...)` → 进程内有窗口时 `FloatScheduleRenderer.Build(model)` 挂载，独立模式时 `hostProcess.SendModel(model)`。教师名解析（L3576-3598）留插件侧，模型只带终态字符串。
- 进度兜底段（L3842-3910）抽 `ComputeProgressRatios(out c, out b)`；`UpdateProgress`（L4000）逻辑不动，500ms ratio 按模式分流（`ApplyProgressRatio` vs `SendProgress`）。
- 新增唯一模式开关：`bool IndependentActive => hostProcess?.ShouldUseIndependent() == true && hostProcess.IsChildConnected`；`EnsureWindow`（L1361）首行 `if (IndependentActive) return;` → 下游 ShowWindow/层级/淡化/穿透/贴边/随机标题全部经既有 `_window == null` 守卫自然短路。
- `ApplyShouldHideAv`（L2773）独立模式分支：`SendVisible(!wantHide)` 后 return（HideMode 规则判定全留插件进程）。
- 回归验证课间行/分隔线/明日标题/进度条四场景渲染不变。

### 第 4 步：子进程渲染
- `Program.cs` 启动流程：① 解析 `--host-dir --pipe --parent-pid --follow-lifetime 0|1 --single-instance 0|1`；② `AssemblyLoadContext.Default.Resolving`：仅 Avalonia/SkiaSharp/HarfBuzzSharp/Tmds 前缀按**简单名**从 host-dir `LoadFrom`（桥接补丁版本差），预检 `Avalonia.Base.dll`+`libSkiaSharp.dll` 缺失则退出码 43；③ 进程级 PATH 注入 host-dir 及其 `runtimes\win-x64\native`（在任何 P/Invoke 前）；④ 单实例 Mutex `Global\AdvancedTimeIsland.FloatSchedule.v1`，冲突退出码 42；⑤ **宿主存活看门狗（保证"跟随启停=开"时正常退出与异常退出/被强杀都能让子进程消失）**：启动时立即 `OpenProcess(SYNCHRONIZE, parentPid)` 拿宿主进程句柄（后台线程 `WaitForSingleObject` 句柄变 signaled=宿主进程终止，无论正常退出、崩溃还是被强杀；句柄式等待规避 PID 复用误判，不用裸 `Process.WaitForExit(pid)`）；同时双保险等待宿主全局 Mutex `Global\ClassIsland.Lock` 被放弃（abandoned）。触发后：**读取最近一次 settings 消息缓存的 FollowHostLifetime**（`--follow-lifetime` 命令行参数仅作无连接时的初始值；管道断开场景用断开前最后值）：开 → 尽力 `Send(shutdown)` 后 `Environment.Exit(0)`（子进程正常消失）；关 → 置冻结（保留窗口本地续走进度）；⑥ **Main 在 ①-⑤ 不得触碰任何 Avalonia 类型**，Avalonia 启动放独立方法 `RunAvalonia()`；
- `FloatScheduleApp` + `FloatScheduleChildWindow`：Window 属性集复制 service L1359-1465（透明/无边框/SizeToContent/MaxWidth=820/ShowInTaskbar=false 等）；本地实现统一指针拖拽（L220-246/L1700-1840 语义）、贴边隐藏（L2279-2606）、悬停淡化（L2793-2871，DispatcherTimer 步进 250ms 过渡，不引 Avalonia.Animation）、随机标题（L1960-2015）、层级重设定时器（TopmostRefreshMode=1 依赖宿主反射，子进程退化为 Mode 0）、防截图、点击穿透；位置用物理像素 `SetWindowPos`。
- `FloatSchedulePipeClient`：1s×∞ 重连；收 `init/settings/model/progress/visible/shutdown`（一律 `Dispatcher.UIThread.Post`）；发 `ready{pid}`/`position{x,y}`（仅非拖拽、非动画、非隐藏态上报）。
- `FloatScheduleFreezeTimer`：断线+follow=0 时用 `Anchor` + 本地时间 500ms 推进 `ApplyProgress`。
- 验证：插件侧临时调试入口手动推一条硬编码 model，肉眼比对两种模式渲染一致。

### 第 5 步：插件侧 `Services\FloatScheduleHostProcessService.cs` + 设置属性 + 注册
- 新设置属性（[PluginSettings.cs](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/Models/PluginSettings.cs) L155 后，模式同现有属性）：`FloatingScheduleIndependentProcess=false`、`FloatingScheduleRandomProcessName=false`、`FloatingScheduleSingleInstanceProtection=true`、`FloatingScheduleFollowHostLifetime=true`。
- 新服务（IHostedService + 单例，`Instance` 静态供设置页取状态）：
  - 管道服务器循环 Accept（每次断开重建 listener → 宿主重启后孤儿子进程重连即 adopt）；启动时先等 2s 观察 incoming 连接，无则 `Process.Start`。
  - exe 定位：插件程序集目录下的 `AdvancedTimeIslandFloatSchedule.exe`；随机进程名= `File.Copy` 到 `%TEMP%\ati_<随机8位>.exe`（启动时清理无占用旧 `ati_*.exe`），进程名即随机（单文件发布故只需复制一个文件）。
  - 失败判定：3 次启动无 `ready`（10s 超时）或退出码 43 → `Status=Failed` + 状态文本 → 回退进程内；子进程意外退出（follow=1）→ 限频重启 3 次/30s。
  - `StopAsync`：follow=1 → 发 shutdown + 1.5s 等待 + `Kill(true)`；follow=0 → 仅关管道（子进程冻结）。
  - `PositionReported` → service 端 300ms 防抖写 `FloatingSchedulePositionX/Y`（值相等不写，杜绝回环）。
- [Plugin.cs](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/Plugin.cs) L252 后注册 `AddSingleton` + `AddHostedService`。
- service 的 `OnSettingsPropertyChanged`（L1843）新增 4 个 case：独立模式开→`EnsureChildAsync()`（`Connected` 事件里再 `HideWindow`，防白屏空窗期）；关→`SendShutdown` + 复用 L1849-1867 现成序列恢复进程内窗；**随机进程名变化→`RestartChildAsync()`（立即以新名副本重启子进程，<2s 恢复，不重启 CI）**；**跟随启停变化→仅 `SendSettings(快照)` 热更新（不重启子进程）**；**单实例保护变化→仅存值（Mutex 只在启动时获取，自下次子进程启动生效，无需重启）**。全部新设置即时生效，无任何"重启 ClassIsland 后生效"项。

### 第 6 步：设置页折叠栏
- [FloatingScheduleSettingsPage.cs](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/Views/Settings/FloatingScheduleSettingsPage.cs)：`InitializeComponent` 中调用新方法 `BuildIndependentProcessGroup`（结构仿 `BuildRandomTitleGroup` L524-604，复用 `CreateSettingsExpander/AddSettingsExpanderItem/CreateToggleSwitch` FA2/3 兼容封装）。
- 5 项按手写稿：独立进程模式(开关)、随机进程名(开关，与项 1 联动禁用，描述注明可能触发杀软误报)、强制重启进程(footer `Button{Content="重启"}`，仅模式开时可用)、单实例保护(开关)、跟随启停(开关，描述原文"开启后，ClassIsland 关闭或异常停止后，此程序将关闭")。
- 平台禁用：`if (!OperatingSystem.IsWindows()) group.IsEnabled = false;`（Linux/macOS/Android 整组禁用），Description 注明仅 Windows 支持。
- 状态文本：`TextBlock`（`ThemeHelper.GetSubTextBrush()` 深浅自适应）+ 1s `DispatcherTimer` 轮询 `StatusText`（未启用/启动中/已连接(PID)/冻结中/失败:原因）。

### 第 7 步：打包与离线源
- [Build-Package.ps1](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/Build-Package.ps1)：新增 `Publish-FloatExe($Tfm,$OutDir)`（`dotnet publish -c Release -f $Tfm -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`，产物 exe 拷入对应输出目录）；在 `Build-Variant` build 成功后、`Create-CipxPackage` 前调用（net8.0→`bin\Release\net8.0\win-x64`，net10.0→`bin\Release\net10.0\win-x64`）；`Build-WpfVariant` 不调用。排除规则无需改。
- [Sync-OfflinePackages.ps1](file:///c:/Users/Administrator/RiderProjects/AdvancedTimeIsland/Sync-OfflinePackages.ps1) L73 后补子进程 net8/net10 两行 `Sync-Target`，跑一次确认无新增缺失 nupkg。
- 体积估算：单文件框架依赖 exe ≈ 250-400KB，每包 +1 个 → cipx 约 1.6-1.7MB（现 1.35MB）。

## 风险与缓解

| 风险 | 缓解 |
|---|---|
| Avalonia 11/12 跨代不兼容 | 双 TFM 双 exe，代际精确配对；补丁差由 Resolving 按简单名桥接 |
| 宿主目录布局差异（MSIX/selfContained） | 启动预检缺 DLL → 退出码 43 → 状态文本明示 + 回退进程内渲染 |
| Skia 原生解析失败 | PATH 在首个 P/Invoke 前注入；备选 `NativeLibrary.SetDllImportResolver` |
| 随机名 exe 杀软误报 | 默认关；描述文字提示 |
| 双渲染漂移 | 单一 BuildRenderModel + 共享 Renderer（架构性消除） |
| 位置回写死循环 | 子进程仅稳定态上报 + 插件防抖 + 值相等不写 |
| 500ms progress 消息频率 | <200B/条，可忽略；断线丢弃不积压 |

**破坏性更改声明**：① `RefreshSchedule` UI 构建段（L3438-3782）被"模型+共享 Renderer"替换——行为等价重构，默认关闭时渲染结果应不变，但触及 4500 行核心文件，需重点回归；② `UpdateProgress`/进度兜底段拆出 `ComputeProgressRatios`。其余均为新增文件/属性，无破坏性。

## 验证

1. `dotnet build 2>&1 | Select-String -Pattern "error|Error"` 无输出（主项目双 TFM + 子进程双 TFM 均需通过，子进程单独 `dotnet build AdvancedTimeIslandFloatSchedule\`）。
2. `.\Build-Package.ps1` 打包成功；解压检查两个 CI2 cipx 各含且仅含匹配 TFM 的 `AdvancedTimeIslandFloatSchedule.exe`，**单个 exe ≤ ~400KB、ico ≤ 20KB**，cipx 体积 ≤ ~1.75MB；wpf cipx 不含 exe。
3. 人工测试（需装有 ClassIsland 2.x 的机器，本机仅有 CI 1.7 无法端到端验证）：开独立模式→子进程窗出现且与进程内渲染逐帧一致；随机进程名→任务管理器名字随机；强制重启→<2s 恢复；跟随启停关+杀 CI→冻结续走→重启 CI 自动重连；**跟随启停开+正常退出→子进程消失；跟随启停开+异常退出（崩溃/任务管理器强杀 ClassIsland）→子进程同样正常消失（看门狗句柄等待触发，无僵尸窗口）**；**exe 文件图标/任务管理器进程图标（含随机进程名副本）与插件 icon.png 一致**；**独立模式下逐项修改悬浮窗设置（不透明度/字号/层级/点击穿透/防截图/贴边/淡化/随机标题/跟随启停等）→ 全部立即生效，无需重启 ClassIsland**；HideMode 跟随主界面/调试时间偏移/主题切换/DPI 150%/点击穿透/防截图/贴边隐藏逐项回归；非 Windows 平台整组禁用。
4. 最后调用 AdvisorTool 进行 vibe review。

## Critical Files
- `Services\FloatingScheduleService.cs`（改造：RefreshSchedule/UpdateProgress/EnsureWindow/ApplyShouldHideAv/OnSettingsPropertyChanged）
- 新建 `AdvancedTimeIslandFloatSchedule\`（子进程项目）与 `Shared\FloatingSchedule\`（5 个共享文件）
- 新建 `Services\FloatScheduleHostProcessService.cs`
- `Models\PluginSettings.cs`、`Views\Settings\FloatingScheduleSettingsPage.cs`、`Plugin.cs`
- `Build-Package.ps1`、`Sync-OfflinePackages.ps1`、`AdvancedTimeIsland.sln`、`AdvancedTimeIsland.csproj`
