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
    /// 插件包内独立程序 exe 的**内容指纹**（SHA256，由插件计算并下发）。
    /// 用途：插件更新后（exe 内容已变化），跟随启停=关 时仍冻结存活的旧子进程据此发现自己已被淘汰
    /// （它把自己的指纹与这里比对），主动以 <see cref="FloatScheduleIpc.ExitCodeSelfUpdate"/> 退出，
    /// 由插件立即用新构建重启，避免"更新插件后旧子进程继续接管"导致新功能/修复永远不生效。
    /// 空值表示宿主未下发指纹（更老的插件）→ 子进程不做校验。
    /// </summary>
    public string ExeStamp { get; set; } = "";

    // ===== 高级外观：窗口材质 / 边框 / 圆角 / 果冻 / FPS（子进程据此本地应用，插件侧已解析终态）=====
    /// <summary>材质类型：0半透明 1云母 2亚克力 3液态玻璃 4古卷 5自定义图片。</summary>
    public int Material { get; set; } = 0;
    /// <summary>效果强度（0~1；云母着色膜浓度、亚克力模糊度、液态玻璃折射度）。</summary>
    public double MaterialIntensity { get; set; } = 0.5;
    /// <summary>卡片圆角（0~10px，默认 8）。</summary>
    public double CornerRadius { get; set; } = 8;
    /// <summary>卡片四边内边距（替换式，默认 12/10 与现状一致；子进程据此设 _card.Padding）。</summary>
    public double MarginTop { get; set; } = 10;
    public double MarginBottom { get; set; } = 10;
    public double MarginLeft { get; set; } = 12;
    public double MarginRight { get; set; } = 12;
    /// <summary>自定义图片背景路径（材质=5 时子进程按路径自读；空/无效回退半透明）。</summary>
    public string CustomBackgroundPath { get; set; } = "";
    /// <summary>果冻效果开关（仅材质=3 液态玻璃生效）。</summary>
    public bool JellyEnabled { get; set; } = false;
    /// <summary>装饰边框开关（材质=3/4 时强制关）。</summary>
    public bool BorderEnabled { get; set; } = false;
    /// <summary>边框预设：0画框 1黑板 2Win7 3XP 4Win10 5Win11 6HarmonyOS。</summary>
    public int BorderPreset { get; set; } = 0;
    /// <summary>边框自定义颜色开关（关=用预设内置主色）。</summary>
    public bool BorderColorEnabled { get; set; } = false;
    /// <summary>边框自定义颜色（0xAARRGGBB）。</summary>
    public uint BorderColor { get; set; } = 0xFFB8860B;
    /// <summary>是否显示 FPS 角标（子进程本地统计渲染/无等待 FPS）。</summary>
    public bool ShowFps { get; set; } = false;

    // 液态玻璃：真折射背景的采样率（Hz，1~60，默认 30）——子进程按此节流"抓屏 + 折射"
    public double GlassSampleRateHz { get; set; } = 30.0;
    /// <summary>液态玻璃采集方案（子进程 FLOATING 采样后端）：
    /// 0=方案2（cf78b44 经典采集：排除自身后抓窗口区域，低占用/流畅，不支持截图）；
    /// 3=方案1（DWM/WGC 仅下方窗口，支持截图，资源占用高）。1/2 为内部保留模式。
    /// 与设置枚举 FloatingScheduleGlassCaptureMode 取值一一对应（以 int 传输避免跨程序集类型依赖）。</summary>
    public int GlassCaptureMode { get; set; } = 0;
    public bool GlassVSync { get; set; } = true;
    /// <summary>材质/背景 tint 基色（0xAARRGGBB，= 模型 CardBackgroundArgb 语义，供子进程材质层使用）。</summary>
    public uint TintArgb { get; set; } = 0xCC2D2D30;
}
