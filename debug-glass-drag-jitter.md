# debug-glass-drag-jitter

## 症状
液态玻璃悬浮窗**拖动时，透射（折射）内容抖动**。桌面环境实测可复现；纯离屏数学无法复现。

## 环境
- 项目：AdvancedTimeIsland（ClassIsland 插件），Windows，DPI 150%
- 相关：DXGI Desktop Duplication 抓帧（~8ms）/ GDI BitBlt 回退（~24ms）
- 关键实现：拖动逐帧用 `ImageBrush.SourceRect` 做 GPU 平移；折射按 8px 阈值后台重烘焙

## 已排除（前几轮）
1. 折射数学：离屏证明统一坐标下相邻帧位移严格单调（-1px），无 0/2 跳变 → **REJECTED**
2. 坐标源混用（Avalonia Position vs Win32 GetWindowRect）→ 已统一为 GetWindowRect，用户实测仍抖 → **REJECTED**
3. UI 线程占帧（5ms 折射）→ 已移后台 + 双缓冲，用户实测仍抖 → **REJECTED**

## 假设（本轮，均为可证伪）
- **H1 · SourceRect 映射不 1:1**：`SourceRect`(绝对像素) 与实际渲染的卡片尺寸/DPI 存在缩放或取整偏差，导致每帧应用偏移 ≠ 期望偏移（例如 ±1px 交替）。
  - 观测点：每帧记录 `期望(dx,dy)`、`实际写入 SourceRect 的(dx,dy)`、卡片物理尺寸、`RenderScaling`。
- **H2 · 重烘焙基准漂移**：重烘焙后 `_refWinX/_refWinY` 与"当帧窗口真实位置"差 1px（抓帧与读数不同源/不同时刻），使归零后偏移在 0 与 ±1 间交替。
  - 观测点：每次烘焙记录 抓帧时窗口原点、烘焙完成时窗口原点、归零后的实际偏移。
- **H3 · 重烘焙与拖动帧竞争**：后台发布与 UI 每帧 `SourceRect` 写入交错，同一合成帧内先被旧偏移覆盖又被新偏移覆盖 → 视觉回退。
  - 观测点：每帧记录 发布版本号、当前 commit 版本、偏移序列是否出现非单调回退。
- **H4 · 窗口自身位置非线性**：拖动时窗口实际屏幕位置本身是 0/2 交替（SetWindowPos 节流与节流器 `_lastDragMoveTick` 16ms 合并导致）。
  - 观测点：连续记录 Win32 `GetWindowRect` 序列，检查是否本身非单调。
- **H5 · 内容与窗口不同帧（结构性）**：GPU 变换与窗口移动仍未落在同一合成帧。
  - 观测点：把「窗口位移序列」与「内容位移序列」逐帧对齐比较，看是否存在恒定 1 帧相位差。

## 插桩点
（待补：见下）

## 证据
（待补）

## 结论
（待补）
