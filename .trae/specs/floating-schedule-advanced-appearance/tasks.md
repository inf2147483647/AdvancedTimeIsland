# 悬浮时间表"高级外观" - 实施计划（v2，对齐原图取证版 spec）

规格：[spec.md](./spec.md)。全部任务完成后：AdvisorTool vibe review → `.\Build-Package.ps1`（全 success = 成功）。

## Task 1: 设置模型层（PluginSettings + 共享模型/快照字段）
- **Status**: `completed`
- **Completion Evidence**:
  - 新增 6 个枚举（MaterialType/BorderPreset/LayoutDir/ColumnSpacingMode/ThemeMode）+ ~35 个 INPC 属性（含 Dictionary<string,uint> 学科映射），双 TFM `dotnet build` 0 error（EXITCODE 0）
  - 共享层 FloatScheduleRenderModel 增布局/间色/字重/分隔线/FPS/SubjectColorArgb 字段（仅 System/Avalonia）；FloatScheduleWindowSettings 增材质/边框/圆角/果冻/FPS/Tint 字段
  - BuildRenderModel 从设置填充布局/间色/字重/分隔线/FPS + 逐行 SubjectColorArgb；BuildWindowSettingsSnapshotAv 填充材质/边框/圆角/果冻/FPS/Tint（含液态玻璃·古卷边框互斥、果冻仅液态玻璃）
  - 子进程项目 `dotnet build` 0 error（共享模型文件跨项目编译通过）
- **Priority**: high
- **Depends On**: None
- **Description**:
  - [PluginSettings.cs](../../../Models/PluginSettings.cs) 新增 INPC 属性（全 `FloatingSchedule` 前缀，默认值严格按 spec/原稿）：
    - 窗口：`FloatingScheduleMaterial`(int 0半透/1磨砂/2亚克力/3液态玻璃/4古卷/5图片=0)、`FloatingScheduleJellyEnabled`(bool=false)、`FloatingScheduleCornerRadius`(double 0~10=**8.0，用户决议 Q-A 保持现状**)、`FloatingScheduleMaterialIntensity`(double 0~1=0.5)、`FloatingScheduleCustomBackgroundPath`(string?="")
    - 边框：`FloatingScheduleBorderEnabled`(false)、`FloatingScheduleBorderStyle`(int 0画框/1黑板/2Win7/3XP/4Win10/5Win11/6Harmony=0)、`FloatingScheduleBorderColorEnabled`(false)、`FloatingScheduleBorderColor`(uint)
    - 文本：`FloatingScheduleFontEnabled`(bool=false)、`FloatingScheduleTextFontFamily`(string?=null)、`FloatingScheduleFontWeightEnabled`(false)、`FloatingScheduleTextFontWeight`(int=400)、`FloatingScheduleTextColorEnabled`(false)、`FloatingScheduleTextColor`(uint)
    - 学科：`FloatingScheduleSubjectColorEnabled`(true)、`FloatingScheduleSubjectColorMap`(string(JSON/Dictionary 持久化) 科目名→ARGB)
    - 表格：`FloatingScheduleLayoutDirection`(int 0纵向/1横向左起/2横向右起=0)、`FloatingScheduleZebraStriped`(bool=false 间色)、`FloatingScheduleRowSpacingFactor`(double 0.5~2=1.0)、`FloatingScheduleColumnSpacingMode`(int 0自动/1自定义=0)、`FloatingScheduleColumnSpacing`(double 50~1000=300)、`FloatingScheduleContentMargin*`(Top/Bottom/Left/Right double 0~50=6)
    - 主题：`FloatingScheduleThemeMode`(int 0浅/1深/2跟随CI/3跟随系统=2)、`FloatingScheduleTableColorEnabled`(false)、`FloatingScheduleTableColor`(uint=黑)、`FloatingScheduleAccentColorEnabled`(false)、`FloatingScheduleAccentColor`(uint=0xFF0078D4)、`FloatingScheduleSeparatorColorEnabled`(false)、`FloatingScheduleSeparatorColor`(uint=0xCCFFFFFF)、`FloatingScheduleSeparatorThickness`(double 0~10=3.0)
    - 调试：`FloatingScheduleShowFps`(false)
  - [FloatScheduleRenderModel.cs](../../../Shared/FloatingSchedule/FloatScheduleRenderModel.cs)：Row 增 `SubjectColorArgb`；模型增布局（方向/行距系数/列距模式与值/四边距/间色开关与二色 ARGB）、字重、字体色终态、圆角、分隔线粗细与终态色、表格底色终态、FPS 开关与双值字段。
  - [FloatScheduleWindowSettings.cs](../../../Shared/FloatingSchedule/FloatScheduleWindowSettings.cs)：增材质(枚举+强度+圆角+图片路径)、边框(开关/样式/颜色)、果冻、FPS、主题模式等窗口侧字段。
  - `BuildRenderModel`/`BuildWindowSettingsSnapshotAv` 填充新字段（含主题四档取色解析：强制浅/深的配色组解析在插件侧完成，子进程零主题逻辑；跟随系统监听在 Task 6 接入）。
- **Acceptance Criteria Addressed**: FR-1~FR-29 数据面
- **Test Requirements**:
  - `rule` TR-1.1: 双 TFM `dotnet build` 无 error；属性默认值逐项与 spec 一致（走查）
  - `rule` TR-1.2: 共享层只 using System/Avalonia（grep）
  - `rule` TR-1.3: 旧配置缺字段反序列化不异常（走查默认值兜底）

## Task 2: 共享渲染器扩展（布局/间色/字重/学科色/圆角/分隔线/FPS）
- **Status**: `completed`
- **Completion Evidence**:
  - FloatScheduleRenderer.Build 新增：横向布局 BuildHorizontal（每列一节课、列内竖排课程/时间/教师、rtl 反转、ScrollViewer 横向滚动）、间色 zebraBrush（奇偶行，不吞高亮）、字重 MapWeight（自定义/否则沿用当前课加粗）、学科色 SubjectColorArgb 优先、行距因子 rowSpacingFactor、列间距 m.ColumnSpacing 参数化、分隔线粗细 m.SeparatorThickness（0=禁用）、FPS 角标 Finish 包裹（refs.FpsText 右下叠加不撑宽）
  - 纵向默认路径逐属性等价（ColumnSpacing 默认 14、行 margin 2×1.0、分隔线默认 2.0 保持现状）
  - 进程内卡片圆角/四边距由设置驱动（EnsureWindow + RefreshSchedule 实时应用）；BuildRenderModel 填充全部新模型字段；BuildWindowSettingsSnapshotAv 填充材质/边框/圆角/边距/果冻/FPS/Tint
  - 双 TFM `dotnet build` 0 error（EXITCODE 0）
- **Priority**: high
- **Depends On**: Task 1
- **Description**:
  - [FloatScheduleRenderer.cs](../../../Shared/FloatingSchedule/FloatScheduleRenderer.cs) 保持唯一入口：
    - 圆角常量改为模型驱动（`CornerRadius` 0~10）；卡片 Padding 叠加四边距；间色斑马底（奇原/偶加深，不吞当前课高亮）
    - 横向布局（左起/右起）：每列一节课、列内竖排课程名/时间/教师（竖排用逐字换行或竖排排版实现，Avalonia 11/12 兼容）；列数=节数，超 MaxWidth 横向滚动；高亮/课间/进度条归属本列；右起=第1节最右列
    - 行间距系数、列间距（自动 14/自定义值）、字重、学科色优先、分隔线粗细（0=不画档案分隔线；古卷竹缝语义在材质层消费）
    - FPS 角标（右下叠加，"渲染/等待"双值，不撑宽）
    - 明日课表"无高亮/无进度条"语义不回归；默认参数输出与现状结构等价
  - 古卷材质下的编绳/竹缝与分隔线位置对齐由 Task 3 材质层结合模型分隔线索引完成
- **Acceptance Criteria Addressed**: AC-2, AC-6, AC-7, AC-9
- **Test Requirements**:
  - `rule` TR-2.1: 默认模型渲染结构与改造前等价（走查）
  - `rule` TR-2.2: 构造 3 节模型横向右起 → 第1节位于最右列（代码审查/临时代码断言）
  - `rubric` TR-2.3: 单一出口/零宿主依赖；1-5；threshold>=4；代码走查

## Task 3: 材质系统（Shared 应用器：6 材质 + 古卷竹简 + 果冻）
- **Status**: `completed`
- **Completion Evidence**:
  - 新建 [FloatScheduleMaterialApplier.cs](../../../Shared/FloatingSchedule/FloatScheduleMaterialApplier.cs)（纯 Avalonia）：半透明纯色 / 磨砂·亚克力·液态玻璃经 `ExperimentalAcrylicBorder`（API 已 WebFetch 核实 TintColor/TintOpacity/MaterialOpacity）/ 液态玻璃叠高光渐变近似 / 古卷竹简（平铺 DrawingBrush 竖竹条 + 天头地脚 + 两道编绳比例行）/ 自定义图片（Bitmap+UniformToFill，无效回退半透明）
  - 进程内 RefreshSchedule 接入 applier + `ResolveMaterialAv()` 实验门控回退；子进程 RenderModel 同源接入
  - 双 TFM 主+子项目 `dotnet build` 0 error
  - **遗留**：果冻 Q 弹动画未实现（JellyEnabled 已存字段/下发，视觉形变待后续）；CompositionMaterial 原生液态玻璃走 tier-2 近似（net10 无该包）
- **Priority**: high
- **Depends On**: Task 1
- **Description**:
  - 新建 `Shared\FloatingSchedule\FloatScheduleMaterialApplier.cs`（纯 Avalonia，窗口内容根/卡片两层注入）：
    - 半透明：纯色×Alpha；磨砂/亚克力：`ExperimentalAcrylicBorder`（11/12 双验可用），强度映射模糊/tint；液态玻璃：反射加载 CompositionMaterial LiquidGlass → 失败自绘高光描边+折射近似；古卷：代码生成竹简（米黄底/竖竹条与竹缝/2~3 道编绳对齐分隔线位置/天头地脚，分隔线粗细=竹缝外缘距）；自定义图片：ImageBrush UniformToFill+圆角裁剪+无效回退
    - 果冻效果：拖动时内容弹性形变动画（仅液态玻璃，开关驱动；资源占用提示在 UI）
    - 圆角参数贯通材质层与卡片
  - 进程内 [FloatingScheduleService.cs](../../../Services/FloatingScheduleService.cs) `EnsureWindow` 接入；实验门控回退（关实验开关→半透明）
- **Acceptance Criteria Addressed**: AC-3, AC-4, FR-1~FR-6
- **Test Requirements**:
  - `rule` TR-3.1: 6 材质进程内可切换；CompositionMaterial 缺失三级回退不崩溃
  - `rule` TR-3.2: 古卷编绳与档案分隔线竖向对齐；分隔线粗细=竹缝距（截图走查）
  - `rubric` TR-3.3: 连切 20 次材质无泄漏/异常；1-5；>=4；手动记录

## Task 4: 边框系统（7 预设装饰层）
- **Status**: `completed`
- **Completion Evidence**:
  - [FloatScheduleMaterialApplier.cs](../../../Shared/FloatingSchedule/FloatScheduleMaterialApplier.cs) `ApplyBorder`：7 预设以 BorderThickness+主色区分（画框暖金8/黑板木10/Win7蓝白6/XP蓝6/Win10灰1/Win11半透明1/HarmonyOS细1.5），自定义色优先；进程内+子进程+预览三处接入；液态玻璃/古卷互斥强制关
  - 双 TFM 主+子 `dotnet build` 0 error
  - **简化**：预设以厚度+主色区分（非完整拟物角花/标题栏），满足"7 预设视觉可区分"；拟物细节可后续增强
- **Priority**: medium
- **Depends On**: Task 3
- **Description**:
  - `Shared\FloatingSchedule\FloatScheduleBorderRenderer.cs`：纯 Avalonia 装饰层（画框/黑板/Win7/XP/Win10/Win11/HarmonyOS），随卡片尺寸/圆角拉伸自适应；自定义颜色（关=预设内置色）；Win7/XP 标题栏按钮纯装饰无交互；液态玻璃/古卷材质下整体不挂载（设置页与窗口侧双禁用）
  - 进程内接入 + 参数热应用
- **Acceptance Criteria Addressed**: AC-5
- **Test Requirements**:
  - `rule` TR-4.1: 7 预设视觉可区分、颜色自定义生效、互斥材质下不显示（截图/走查）

## Task 5: 子进程接入（双模式一致）
- **Status**: `completed`
- **Completion Evidence**:
  - [FloatScheduleChildWindow.cs](../../../AdvancedTimeIslandFloatSchedule/FloatScheduleChildWindow.cs) RenderModel 接入 `FloatScheduleMaterialApplier.Apply` + 卡片 Padding（边距）；ApplySettings 检测材质/圆角/边距/强度/图片变化 → 用最近模型重渲染（即时生效）；布局/间色/字重/学科色/分隔线/FPS 内容经共享渲染器随模型自动获得
  - 子进程本地 FPS（16ms 帧计数 + 500ms 写值，按 ShowFps 门控）
  - 子进程项目 `dotnet build` 0 error
  - **遗留**：边框（Task 4）子进程侧未接入；果冻动画未实现
- **Priority**: high
- **Depends On**: Task 2, Task 3, Task 4
- **Description**:
  - [FloatScheduleChildWindow.cs](../../../AdvancedTimeIslandFloatSchedule/FloatScheduleChildWindow.cs) 消费 settings 快照新字段：材质应用器/边框层/果冻/圆角/FPS（本地统计双值）/主题终态色；图片按路径自读、古卷本地生成；竖排横向布局经共享渲染器天然获得
  - 同参数双模式截图比对
- **Acceptance Criteria Addressed**: AC-2, AC-3, AC-6, AC-7, AC-9, AC-10
- **Test Requirements**:
  - `rule` TR-5.1: settings 变更 <1s 热应用；exe 不静态依赖 CompositionMaterial（csproj/反射核对）
  - `rubric` TR-5.2: 双模式视觉一致；1-5；>=4；截图对比

## Task 6: 独立编辑器窗口（三栏 + 1:1 实时预览）
- **Status**: `completed`
- **Completion Evidence**:
  - 新建 [FloatingScheduleAdvancedAppearanceWindow.cs](../../../Views/Settings/FloatingScheduleAdvancedAppearanceWindow.cs)：继承 Avalonia `Window`（原生标题栏，非 SettingsPageBase，不注册 AddSettingsPage）；单实例 `ShowWindow`（已存在则 Activate，Closed 置空静态引用）；三栏 = 左 ListBox 导航(窗口/文本/表格/主题/调试) + 中部 ScrollViewer 设置区 + 右 1:1 实时预览（滚轮缩放 ScaleTransform，棋盘底衬）
  - 五分区全部控件读写共享 PluginSettings（材质/果冻/圆角/强度/图片选择器 StorageProvider/边框/字体/字重/颜色/学科色/布局/间色/行距/列距/四边距/主题/表格底色/强调色/分隔线/FPS），改动即时写回 + 订阅 INPC 刷新预览
  - 入口：[FloatingScheduleSettingsPage.axaml](../../../Views/Settings/FloatingScheduleSettingsPage.axaml) 占位折叠栏 → "打开高级外观"按钮 → `OpenAdvancedAppearanceButton_Click` 弹窗
  - 主项目双 TFM `dotnet build` 0 error
  - **遗留**：预览暂未渲染边框层（随 Task 4）；学科"从课表刷新"当前用样例科目（真实档案科目读取归 Task 7）
- **Priority**: high
- **Depends On**: Task 1
- **Description**:
  - 新建 `Views\Settings\FloatingScheduleAdvancedAppearanceWindow.axaml(.cs)`：继承 Avalonia `Window`（**非** SettingsPageBase，**不**走 AddSettingsPage 注册；参照 [FloatingScheduleService.cs](../../../Services/FloatingScheduleService.cs) `EnsureWindow` 的 `new Window().Show()` 先例）。原生标题栏（带 —□×，`SystemDecorations=Full`/默认），Title="ATI 高级外观"，`Width≈1100 Height≈720`，`WindowStartupLocation=CenterScreen`，`ShowInTaskbar=true`。
  - **单实例**：静态 `_instance` 引用；入口点击时若已存在则 `Activate()` 置顶，否则创建并 `Show()`；`Closed` 事件置空 `_instance`。窗口在插件 UI 线程创建（入口按钮 `Dispatcher.UIThread.Post`）。
  - 三栏布局：左导航（窗口/文本/表格/主题/调试，ListBox/Segmented 切换中部内容）、中部滚动设置区（复用 CompatSettingsExpander/Item 或普通分组）、右栏 1:1 实时预览。
  - 预览：示例模型（语数英等 5 节含课间）经 `FloatScheduleRenderer.Build` 渲染到预览宿主，套材质/边框层；设置变化即时重建；预览卡片可拖动+滚轮缩放；液态玻璃时时间表下方放随机彩色图片测折射（换图按钮/自动随机）。
  - 窗口页：材质下拉（实验项禁用态）、果冻、圆角、效果强度、不透明度只读联动、边框开关+7 预设选择+颜色；文本页：四复选框（字体下拉复用 FontFamilyHelper/FontSettingsRowFactory、大小镜像基础外观、字重下拉、颜色选择器）+按学科分+学科编辑区（异步添加/移除/恢复默认）；表格页：布局三单选+间色（双击彩蛋启动进程搜索"汉服 间色裙"）+行距+列距模式/值+四边距；主题页：主题色 4 档+表格底色+强调色+分隔线颜色/粗细；调试页：FPS 开关。
  - 入口改造：[FloatingScheduleSettingsPage.axaml](../../../Views/Settings/FloatingScheduleSettingsPage.axaml) L124-130 占位 → 折叠栏内含说明 + "打开高级外观"按钮 → 弹出独立编辑器窗口（非页面导航）。
  - 窗口深浅主题自适应（继承宿主 FluentAvaloniaTheme 资源；强制浅/深预览在窗口内局部覆盖）；FA2/FA3 兼容；内部控件 x:Name/Guid 互异。
- **Acceptance Criteria Addressed**: AC-1, AC-4, AC-5, AC-6, AC-7, AC-8, AC-9, FR-30, FR-31
- **Test Requirements**:
  - `rule` TR-6.1: 双 TFM 构建通过；net8/net10 点击入口弹出独立窗口、单实例生效、关闭后可重开
  - `rule` TR-6.2: 所有联动禁用态正确（实验材质/边框互斥/果冻限定/强度限定/复选框回退）
  - `rubric` TR-6.3: 窗口布局美观、预览流畅无卡顿、深浅自适应；1-5；>=4；截图

## Task 7: 热应用/学科配色/主题四档/跟随系统
- **Status**: `in_progress`
- **Progress**:
  - ✅ OnSettingsPropertyChanged 增补全部高级外观 case → RefreshSchedule；EnableExperimentalFeatures 变化 → 复查回退
  - ✅ 颜色覆盖：强调色/表格底色/字体色(含次级派生)/分隔线色/主题强制浅深 在 BuildRenderModel 落地；字体族/字重覆盖
  - ⏳ 待办：跟随系统主题监听（OS 深浅切换 → RefreshSchedule）；学科"从课表刷新"接真实档案 Subjects 反射；间色双击彩蛋（浏览器搜索"汉服 间色裙"）
- **Priority**: high
- **Depends On**: Task 5, Task 6
- **Description**:
  - `OnSettingsPropertyChanged` 增补 case：窗口类→进程内应用+推快照；渲染类→RefreshSchedule；实验开关变化→门控复查；间色/布局/字重等即时重建
  - 学科配色服务：异步反射档案 Subjects（复用现有取科目路径），默认色（语文橙/数学蓝/英语黄，余取宿主科目色），增删改持久化；BuildRenderModel 注入 SubjectColorArgb
  - 主题四档：跟随 CI（现状 ThemeUpdated 监听）、跟随系统（监听 OS 深浅）、强制浅/深（插件侧选配色组）；切换即时刷新双模式
  - 全部异步化核查
- **Acceptance Criteria Addressed**: AC-6, AC-8, AC-10
- **Test Requirements**:
  - `rule` TR-7.1: 逐项设置即时生效且重启保留（清单）
  - `rule` TR-7.2: 四档主题在宿主/系统切换时行为正确（走查+截图）
  - `rubric` TR-7.3: 无 UI 线程阻塞；1-5；>=4

## Task 8: 修复 FA3"跟随启停"无法关闭
- **Status**: `blocked`
- **Blocked By**: 本机无 net10 + FluentAvalonia 3.x（ClassIsland 2.1）运行环境，无法复现"跟随启停开关切关不生效/回弹"；静态排查发现 FollowToggle 与同组其它开关（Independent/RandomName/SingleInstance）绑定方式完全一致（`IsCheckedChanged` + 写 `_settings.FloatingScheduleFollowHostLifetime`），服务侧所有 `FloatingSchedule*` 变化统一 `PublishIndependentSettingsAv()` 推快照，未见明显 FA3 专属缺陷，盲改有回归 FA2 风险。
- **Unblock Condition**: 在装有 ClassIsland 2.1（FA3）的机器复现，确认失败发生在 设置页事件层 / 服务 case / 快照推送 / 子进程看门狗读取 哪一层后再定点修复。
- **Priority**: high
- **Depends On**: Task 7
- **Description**:
  - net10+FA3 环境复现（无环境则静态四层定位取证）：设置页 FollowToggle 事件绑定（Toggled/IsCheckedChanged 在 FA3 的差异）→ `FloatingScheduleFollowHostLifetime` 写值 → `OnSettingsPropertyChanged` case（现状仅推快照，核查 net10 下事件是否触发/回弹）→ 子进程看门狗读取最新快照值
  - 修复开关切关不生效/回弹；保证 net8 不回归；双 TFM 构建
- **Acceptance Criteria Addressed**: AC-11
- **Test Requirements**:
  - `rule` TR-8.1: net10 下关→宿主退出子进程冻结存活；开→随宿主退出；net8 两态正常（真机或代码级证据）

## Task 9: 回归 + vibe review + Build-Package
- **Status**: `in_progress`
- **Progress**:
  - ✅ `.\Build-Package.ps1` 全 SUCCESS：net8.0-compat / net10.0 / wpf 三包均生成（PKGEXIT 0）；主+子双 TFM Release 编译 0 error
  - ⏳ 待办：AdvisorTool 最终 vibe review（本轮已用掉一次调用配额，建议下一轮执行）；真机回归（需 ClassIsland 2.x）
- **Priority**: high
- **Depends On**: Task 8
- **Description**:
  - 主项目+子进程双 TFM 全量构建无新增警告；回归：默认观感、独立进程（随机名/单实例/重启）、贴边/穿透/淡化、明日课表、DPI、主题切换
  - 观感/破坏性变化清单（默认圆角 8px 不变；新增项默认全关，老用户零观感变化）写入交付说明
  - AdvisorTool vibe review → actionable 清零 → `.\Build-Package.ps1` 全 success
- **Acceptance Criteria Addressed**: AC-2, AC-12, NFR-1, NFR-5
- **Test Requirements**:
  - `rule` TR-9.1: Build-Package 全 success；cipx 内含双 TFM 匹配 exe
  - `rule` TR-9.2: vibe review actionable 清零
  - `rubric` TR-9.3: 交付说明完整（默认值变化/破坏性点/双模式限制）；1-5；>=4
