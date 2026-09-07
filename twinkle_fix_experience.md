# 常驻窗口"定时设置层级导致闪烁"修复经验

> 记录时间：2026-09-06
> 适用场景：需要"始终保持在桌面最底层（或最顶层）"的常驻小窗（右下角快捷启动、悬浮球、桌宠、状态条等）。
> 相关项目：HugoQuickStart（快捷启动）、ClassIsland（源码参考 D:\TEMP\CLASSISLAND）

---

## 1. 问题现象

主界面常驻屏幕右下角，但每隔约 1.5 秒出现一次**可见的闪烁 / 抖动**，
在窗口内容较多、DWM 合成、或与其它透明窗口叠加时尤其明显。

## 2. 根本原因

原实现用 `DispatcherTimer` 每 1500ms **无条件**调用一次 Win32 `SetWindowPos(HWND_BOTTOM)` 把窗口压到最底层：

```csharp
_bottomTimer.Tick += (_, _) =>
{
    if (!IsActive && _dialogCount == 0 && !_isCloseConfirmShown)
    {
        SendToBottom();          // ← 每 1.5s 真的执行一次 SetWindowPos
        PositionWindowBottomRight();
    }
};
```

`SetWindowPos` 即使带 `SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE`，
仍会触发一次 **Z 序变更 + `WM_WINDOWPOSCHANGING/CHANGED` + 合成器重排**。
DWM 在窗口"被压到别的窗口之下"的瞬间会产生一次图层重排，肉眼即表现为闪烁。

关键点：**只要窗口已经在最底层，再次调用 `SetWindowPos(HWND_BOTTOM)` 是纯副作用、零收益**，
但代价是一次重排。定时器把它变成周期性事件，于是"每秒闪一次"。

> 旁证：NetSpeedTray issue #111 完全同因——`_ensure_win32_topmost()` 每秒无条件执行
> `NOTOPMOST → TOPMOST` 的"重新提升"技巧，导致每秒一次可见掉层闪烁。
> 结论一致：**只在真正需要时反应式地设置层级，不要每 tick 都设。**

## 3. ClassIsland 是怎么做的（源码参考）

ClassIsland 的置底**主路径不是定时器轮询，而是消息驱动**：

- `MainWindow.axaml.cs` 里注册 WndProc 钩子：
  `Win32Properties.AddWndProcHookCallback(this, ProcWnd);`
- `ProcWnd` 拦截 `WM_WINDOWPOSCHANGED (0x0047)`：
  只有当**确实发生了 Z 序变化且新位置不是 `HWND_BOTTOM`** 时，才补一次 `SetBottom()`：

```csharp
if (msg == 0x0047) // WM_WINDOWPOSCHANGED
{
    var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
    if ((pos.flags & SWP_NOZORDER) == 0 && WindowTopmostRecheckMode == 0)
    {
        if (pos.hwndInsertAfter != HWND_TOPMOST) ReCheckTopmostState();
        if (pos.hwndInsertAfter != HWND_BOTTOM) SetBottom();   // 仅"被抬起来"时才压回
    }
}
```

- 底层 `SetWindowFeature(..., Bottommost, true)` 使用的标志位比常见写法更多，
  目的就是**削掉一次 Z 序变更的连带消息**：

```csharp
SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0,
    SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE
    | SWP_NOSENDCHANGING | SWP_NOOWNERZORDER | SWP_NOREPOSITION);
```

- ClassIsland 还留了 4 种"重查模式"（`WindowTopmostRecheckMode`）：
  0=消息驱动、1=前台窗口变化驱动、2=课程定时器驱动、3=高频定时器。
  **消息驱动是默认且最平滑的**，高频定时器是兜底选项。

## 4. 本项目的修复方案

Avalonia 12.1.2 已**移除** `Win32Properties.AddWndProcHookCallback`
（`Avalonia.Win32.dll` 里只剩内部 `WindowImpl.WndProcHookCallback`，无公开挂接点），
因此无法照搬 ClassIsland 的 WndProc 钩子。改用等效的**事件驱动 + 幂等标志**，
同样做到"不再周期性重设层级"：

### 4.1 幂等标志：已在底层就不再调用

```csharp
private bool _atBottomSent;   // 当前是否已置底

private void SendToBottom()
{
    if (_atBottomSent) return;          // ← 关键：重复调用直接短路，杜绝周期性重排
    var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
    if (hwnd == IntPtr.Zero) return;
    SetWindowPos(hwnd, new IntPtr(1) /*HWND_BOTTOM*/, 0, 0, 0, 0,
        SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE
        | SWP_NOSENDCHANGING | SWP_NOOWNERZORDER | SWP_NOREPOSITION);  // 对齐 ClassIsland 标志位
    _atBottomSent = true;
}
```

### 4.2 事件驱动：用 Activated / Deactivated 代替轮询

```csharp
private void OnLoaded(...)
{
    LayoutUpdated += OnLayoutUpdated;
    PositionWindowBottomRight();

    Activated   += (_, _) => _atBottomSent = false;  // 被激活=被抬到前面，标记需重新置底
    Deactivated += (_, _) => SendToBottom();         // 失焦瞬间立即压回底层（无延迟、无轮询）
    StartBottomMostMaintenance();
}
```

- 用户点别的窗口 → 本窗 `Deactivated` → 立即置底一次。
- 用户点回本窗 → `Activated` → 清除标志（此时允许它在前台，不打断交互）。
- 之后再次失焦才再置底。**每次真实的层级变化只对应一次 `SetWindowPos`。**

### 4.3 定时器降级为"兜底"，且不再无条件置底

保留 2s 定时器只为处理"没有焦点事件、却被外部程序抬层"的罕见情况；
tick 里若 `_atBottomSent` 为真，则**只校准位置、绝不碰 Z 序**：

```csharp
_bottomTimer.Tick += (_, _) =>
{
    if (IsActive || _dialogCount > 0 || _isCloseConfirmShown) return;
    if (_atBottomSent) { PositionWindowBottomRight(); return; }  // 已在底层：不重复置底
    SendToBottom();
};
```

## 5. 效果与验证

- 静止状态下 `SetWindowPos` 调用次数从"每 1.5s 一次"降到"仅在真实焦点切换时各一次"。
- 右下角常驻窗口不再周期性闪烁；点击、拖拽、弹模态框均不受影响。
- 位置仍随内容高度变化（`LayoutUpdated`）即时重锚右下角，无延迟抖动。

## 6. 可复用的通用准则

1. **不要用定时器无条件重设窗口层级**——这是"常驻置底/置顶窗口闪烁"的头号成因。
2. **优先事件驱动**：有 WndProc 钩子就监听 `WM_WINDOWPOSCHANGED`（ClassIsland 做法）；
   没有就用 `Activated/Deactivated` 等焦点事件。
3. **幂等标志有坑，优先真实探测**（见第 7 节修订）：布尔"已置底"标志会在无焦点事件的
   外部抬层后永久失真，导致窗口赖在前面不回落。应以 `GetWindow` 扫描真实 Z 序代替。
4. **精简 `SetWindowPos` 标志位**：附加 `SWP_NOSENDCHANGING | SWP_NOOWNERZORDER | SWP_NOREPOSITION`
   （必要时 `SWP_NOREDRAW | SWP_DEFERERASE`）能显著减少一次变更引发的连带消息与重绘。
5. **模态/交互期暂停**：有对话框、确认框或窗口处于活动态时，不要抢 Z 序，避免打断用户。

---

## 7. 第二轮修复：标志位失真导致"层级仍可能被拉上来"

### 7.1 新现象

第 4 节方案上线后，仍有概率出现：窗口被拉到其它窗口之上后**不再回落**到底层。

### 7.2 根因：`_atBottomSent` 会失真

布尔标志只在 `Activated` 事件里清零。但窗口被抬离底层的路径**不一定伴随 Activated**：

- 其它程序最小化/还原、系统 Shell 重排 Z 序、新窗口以特殊方式插入；
- Alt+Tab 循环、任务栏切换等由系统直接改 Z 序；
- 本窗从未获得焦点，`Activated` 根本不触发。

结果：标志停留在 `true`，定时器 tick 里 `if (_atBottomSent) return;` 永远短路，
窗口赖在前面再也压不回去。**"信任自己记的状态"是这类问题的通病。**

### 7.3 正确做法：探测真实 Z 序，而不是记住状态

用 `GetWindow(hwnd, GW_HWNDNEXT)` 沿 Z 序**向下**扫描本窗之下的窗口：

```csharp
private bool IsRaisedAboveForeignWindow()
{
    var below = GetWindow(hwnd, GW_HWNDNEXT);   // GW_HWNDNEXT = Z 序中更靠下的窗口
    while (below != IntPtr.Zero)
    {
        if (IsWindowVisible(below))
        {
            GetWindowThreadProcessId(below, out var pid);
            if (pid != _ownPid)
            {
                var exStyle = (int)(long)GetWindowLongPtr(below, GWL_EXSTYLE);
                if ((exStyle & WS_EX_TOPMOST) == 0)
                    return true;   // 本窗之下还有其它进程的普通窗口 → 不在底层
            }
        }
        below = GetWindow(below, GW_HWNDNEXT);
    }
    return false;
}
```

要点：

- **方向**：判断"被抬起"要看**下方**（GW_HWNDNEXT）有没有普通窗口。
  已在最底层时下方没有普通窗口 → 返回 false → 不调用 SetWindowPos → 不闪烁。
  （注意：若误扫上方 GW_HWNDPREV，正常置底状态也会命中"上方有普通窗口"，永远判定被抬起，回到闪烁。）
- **跳过置顶窗口**：任务栏、置顶播放器等带 `WS_EX_TOPMOST` 的窗口永远在普通层之上，
  不会也不该影响判断（扫描时排除）。
- **跳过本进程窗口**：悬浮球、对话框、托盘浮层同属本进程，不构成"被抬起"。

定时器 tick 与前台钩子都改为：**先探测，确实被抬起才 `SendToBottom()`**。

### 7.4 反应式触发：SetWinEventHook(EVENT_SYSTEM_FOREGROUND)

Avalonia 12 没有公开的 WndProc 钩子，用全局 WinEvent 钩子等效替代 ClassIsland 的
`WM_WINDOWPOSCHANGED` / `RegisterForegroundWindowChangedEvent`：

```csharp
_foregroundEventProc = OnWinEvent;   // 委托必须存字段防 GC
_foregroundEventHook = SetWinEventHook(
    EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
    IntPtr.Zero, _foregroundEventProc,
    0, 0, WINEVENT_OUTOFCONTEXT);    // 0,0 = 全进程全线程；回调在自己 UI 线程消息循环派发
```

任意窗口切换到前台时立即探测并按需置底——**零轮询延迟**；
2s 定时器降级为最后兜底（处理不产生前台事件的 Shell 重排）。
`Closed` 时 `UnhookWinEvent` 注销。

> 旁证：Windhawk 模块 "Keep Rainmeter Always on Desktop" 用完全相同的
> `EVENT_SYSTEM_FOREGROUND` + `SetWindowPos(HWND_BOTTOM, ..., SWP_NOSENDCHANGING)`
> 方案解决 Rainmeter 皮肤窗口在 Alt+Tab / 焦点切换时被抬到前面的同类问题。

### 7.5 最终架构（三层防线，全部"先探测后行动"）

| 层 | 触发时机 | 动作 |
|---|---|---|
| Deactivated 事件 | 本窗失焦 | 直接置底（此时必然需要） |
| EVENT_SYSTEM_FOREGROUND 钩子 | 任意窗口切换前台 | 探测 → 被抬起才置底（即时） |
| 2s 定时器 | 周期兜底 | 探测 → 被抬起才置底，否则只校准位置 |

验证：反复 Alt+Tab、最小化/还原其它窗口、开关新窗口，本窗始终在 ≤2s 内回到底层，
且静止期间无任何周期性 `SetWindowPos` 调用，不闪烁。

---

## 8. 第三轮：参考 AdvancedTimeIsland 悬浮窗实现，彻底置底

AdvancedTimeIsland 项目 `Services\FloatingScheduleService.cs` 的 `ApplyWindowLayer()`
在同类问题上走得更远，移植其三个关键点：

### 8.1 ★ WS_EX_NOACTIVATE —— "被拉上来"的真正主因

> 置底窗口一旦被**激活**，Windows 会**强制提升**其 z-order——SetWindowPos 压回后
> 仍会再被提升，即使每 1ms 重设也压不住（DispatcherTimer 受系统时钟 ~15.6ms 分辨率限制）。
> 加 `WS_EX_NOACTIVATE (0x08000000)` → 窗口点击/拖拽永不激活 → 永不被提升 → 真正彻底置底。

```csharp
var exStyle = (int)(long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
var target = exStyle | WS_EX_NOACTIVATE;
if (target != exStyle)                 // 值未变化不写 GWL_EXSTYLE：
    SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)target);   // 反复写会触发 DWM 重评估合成分层 → 闪烁
```

前提：主窗无文本输入框等必须键盘焦点的交互（HugoQuickStart 满足；按钮悬停/点击不需要激活）。
若某窗口确实需要键盘输入，则不能用此位，只能靠 7.x 的探测回压。

### 8.2 WndProc 子类化拦截 WM_WINDOWPOSCHANGED（Mode 0）

Avalonia 12 没有公开 WndProc 挂接点，用 `SetWindowLongPtr(GWLP_WNDPROC)` 手动子类化：

```csharp
_wndProcDelegate = WndProcHook;                       // 委托必须存字段防 GC
_oldWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC,
    Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
```

回调中解析 `WINDOWPOS.flags`（偏移 `IntPtr.Size*2 + 16`）：
`flags & SWP_NOZORDER == 0` 且**非自己压底引发**（`_inSendToBottom == 0` 重入计数）
→ 外部改了 Z 序 → Post 到 UI 线程探测并按需压回。
**任何异常都必须继续 `CallWindowProc` 转发旧过程**，否则窗口失去消息泵直接卡死。

### 8.3 探测的桌面 Shell 陷阱（本轮实测踩坑）

`GetWindow(GW_HWNDNEXT)` 向下扫描时，**正确置底的本窗之下仍有桌面窗口**
（`Progman` / `WorkerW`，可见、非置顶、非本进程）→ 会被误判"永远被抬起"→
每次事件都无谓 SetWindowPos → 回到闪烁。必须按类名排除：

```csharp
GetClassName(hwnd, buffer, len);
return cls is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";
```

（AdvancedTimeIsland 用 `GetWindow(GW_HWNDLAST)==hwnd` 判断"已在链底"，天然规避此问题，
但它把 `GW_HWNDLAST` 常量写成了 2——2 实际是 `GW_HWNDNEXT`，属笔误，移植时勿照抄。
同理 `SWP_NOREPOSITION` 是 0x0200 而非 0x0100——0x0100 是 `SWP_NOCOPYBITS`。）

### 8.4 最终防线（四层）

| 层 | 触发时机 | 动作 |
|---|---|---|
| WS_EX_NOACTIVATE | 常驻 | 从源头阻止"激活→强制提升" |
| WndProc 子类化 | 本窗 Z 序被改（含无焦点事件的重排） | 探测 → 被抬起才压回（即时） |
| EVENT_SYSTEM_FOREGROUND 钩子 | 任意窗口切换前台 | 探测 → 被抬起才压回 |
| 2s 定时器 | 周期兜底 | 探测 → 被抬起才压回，否则只校准位置 |

---

## 参考来源

- AdvancedTimeIsland 源码（本机 `C:\Users\seewo\RiderProjects\AdvancedTimeIsland`）：
  - `Services\FloatingScheduleService.cs`：`ApplyWindowLayer()`（WS_EX_NOACTIVATE 彻底置底 + GW_HWNDFIRST/LAST 条件断言）、`AttachTopmostRefreshAv` 4 模式、`TopmostWndProcHookAv` 子类化、`_suppressTopmostRefreshAv` 拖动期抑制
- ClassIsland 源码（本机 `D:\TEMP\CLASSISLAND`）：
  - `ClassIsland\MainWindow.axaml.cs`：`ProcWnd` 拦截 `WM_WINDOWPOSCHANGED`、`SetBottom()`、`UpdateWindowLayer()`、`Win32Properties.AddWndProcHookCallback`
  - `platforms\ClassIsland.Platforms.Windows\Services\WindowPlatformService.cs`：`SetWindowFeature(..., Bottommost, ...)` 的 `SetWindowPos` 标志位组合
- NetSpeedTray issue #111 "Flickering issue"：定时器无条件重新提升层级导致每秒闪烁（同因同治）
  https://github.com/erez-c137/NetSpeedTray/issues/111
- Windhawk mod "Keep Rainmeter Always on Desktop"：`EVENT_SYSTEM_FOREGROUND` 钩子 + `SetWindowPos(HWND_BOTTOM)` 保持置底
  https://github.com/ramensoftware/windhawk-mods/blob/main/mods/keep-rainmeter-always-bottom.wh.cpp
- CSDN《创建窗口始终置于窗口底部，并不被激活和带到前面》：置底 + 防激活的完整组合（WS_EX_NOACTIVATE、WM_MOUSEACTIVATE 返回 MA_NOACTIVATE）
  https://blog.csdn.net/QSCJOB/article/details/144846998
- Microsoft Learn《SetWindowPos 函数》：`HWND_BOTTOM` 与各 `SWP_*` 标志语义
  https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setwindowpos
