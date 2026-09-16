# 更新日志

本文件记录 AdvancedTimeIsland 的版本变更。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [2.0.4.3] - 2026-09-16

> 变更范围：`2.0.4.2` → `HEAD`（12 个提交，389 个文件，+9070 / -4021）

### 新增

- **周次 / 学期组件体系（WPF 版）**：新增「年周次」、「学期周次」两个主界面组件及其设置页；配套新增周次视图模型基类、`WeekSettings` 模型、`WeekNumberHelper` 助手与 `SemesterStartService` 服务，并完成插件注册。（[271f212](https://github.com/inf2147483647/AdvancedTimeIsland/commit/271f212)）
- **汉服设置页面大规模扩充**：新增 151 个汉服形制设置页面，全部继承 `hanfu_page_template.cs` 模板，覆盖先秦至明制各形制，并为每个页面补充对应文档占位文件。（[e4a95b4](https://github.com/inf2147483647/AdvancedTimeIsland/commit/e4a95b4)）
- **倒计时「差一矫正」**：为节日、节气、星座三个倒计时模块新增差一矫正开关，当格式化精度不足时自动将最小显示单位加一，并补充对应配置项与 UI 设置入口。（[bf5d4f0](https://github.com/inf2147483647/AdvancedTimeIsland/commit/bf5d4f0)、[5f6de3e](https://github.com/inf2147483647/AdvancedTimeIsland/commit/5f6de3e)）
- **汉服文档热更新**：新增汉服文档更新器，启动时从 GitHub Release 拉取最新文档并解压到插件目录，支持多镜像回退。（[c71b2e5](https://github.com/inf2147483647/AdvancedTimeIsland/commit/c71b2e5)）

### 修复

- 修复悬浮窗点击穿透失效的问题。（[00701ba](https://github.com/inf2147483647/AdvancedTimeIsland/commit/00701ba)）
- 修复窗口置底高频闪烁的问题。（[4e02276](https://github.com/inf2147483647/AdvancedTimeIsland/commit/4e02276)）
- 修正汉服模板文件读取路径逻辑，改为固定从插件目录读取，与更新器写入路径保持一致。（[95a9aca](https://github.com/inf2147483647/AdvancedTimeIsland/commit/95a9aca)）
- 汉服设置页默认打开「女装」标签页。（[181c06d](https://github.com/inf2147483647/AdvancedTimeIsland/commit/181c06d)）

### 变更

- 汉服文档加载方式重构：页面内嵌 markdown 文本统一迁移至独立 `.md` 文件，并删除废弃的 `jiahao.cs`。（[3c99c7e](https://github.com/inf2147483647/AdvancedTimeIsland/commit/3c99c7e)）
- 所有汉服页面的 markdown 路径统一改为中文命名，提升可读性与维护性。（[8190e63](https://github.com/inf2147483647/AdvancedTimeIsland/commit/8190e63)）
- 移除随包分发的本地汉服 markdown 文档，改为运行时自动更新获取，同时调整项目输出配置。（[c71b2e5](https://github.com/inf2147483647/AdvancedTimeIsland/commit/c71b2e5)）

### 文档

- README（根目录与 WPF 子目录）补充 AdvancedTimeIslandHanfu 仓库下载量徽章。（[b8d98c6](https://github.com/inf2147483647/AdvancedTimeIsland/commit/b8d98c6)）
