// 悬浮窗行为设置快照（独立进程模式）：插件进程按设置变更实时序列化推送，
// 子进程据此驱动本地窗口行为（层级/穿透/淡化/贴边/防截图/随机标题/位置）。
// 枚举以 int 传输避免跨程序集类型依赖：
//   Layer: 0=置底 1=置顶（FloatingScheduleWindowLayer）
//   TopmostRefreshMode: 0=Z序变化 1=前台变化(子进程退化为0) 2=50ms 3=1ms 4=2s（FloatingTopmostRefreshMode）
namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public sealed class FloatScheduleWindowSettings
{
    public int ProtocolVersion { get; set; } = FloatScheduleIpc.ProtocolVersion;

    public int Layer { get; set; }
    public int TopmostRefreshMode { get; set; } = 2;
    public bool ClickThrough { get; set; }
    public bool HoverFade { get; set; }
    public bool HoverFadeReverse { get; set; }
    public bool EdgeHide { get; set; }
    public double EdgeHideDelay { get; set; } = 3.0;
    public bool RandomTitle { get; set; }
    public bool RandomTitleEnhanced { get; set; }
    public bool PreventCapture { get; set; }
    /// <summary>窗口位置（物理像素）。仅初始放置；用户拖拽后由子进程回报、插件写回设置。</summary>
    public int PositionX { get; set; } = 100;
    public int PositionY { get; set; } = 100;
    /// <summary>宿主隐藏判定结果（HideMode 规则在插件进程计算，子进程只跟随显示/隐藏）。</summary>
    public bool HostVisible { get; set; } = true;
    /// <summary>跟随启停：插件进程退出（含崩溃，由子进程看门狗感知）后子进程是否随之退出。经 settings 消息热更新。</summary>
    public bool FollowHostLifetime { get; set; } = true;

    /// <summary>
    /// 子进程 exe 的"版本指纹"（长度-最后写入时间）。
    /// 用途：插件被更新后（exe 已变化），跟随启停=关 时仍在运行的旧子进程据此发现自己已过期并主动退出，
    /// 从而由插件启动与当前插件匹配的新版本实例，避免"更新插件后旧子进程继续接管"。
    /// 空值表示插件尚未启动过子进程（无需校验）。
    /// </summary>
    public string ExeStamp { get; set; } = "";
}
