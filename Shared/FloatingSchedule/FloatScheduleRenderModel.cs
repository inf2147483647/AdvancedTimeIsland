// 时间表悬浮窗渲染模型（独立进程模式共享层）。
// 约束：本目录文件只允许 using System / Avalonia / System.Text.Json，
// 禁止引用 ClassIsland.*、PluginSettings、ThemeHelper 等插件内部类型——
// 以便同一份源码同时编入主项目（进程内渲染）与 AdvancedTimeIslandFloatSchedule 子进程。
// 所有颜色以"插件进程已解析的 ARGB"携带，子进程零主题/零业务逻辑。
using System.Collections.Generic;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

/// <summary>悬浮窗课表渲染模型：数据→模型的唯一出口（BuildRenderModel）产出，模型→控件的唯一入口（FloatScheduleRenderer）消费。</summary>
public sealed class FloatScheduleRenderModel
{
    /// <summary>课程名字号（已 clamp 8~32 取整；表头-2/时间-1/教师-3 由渲染器内部按差值计算）。</summary>
    public int FontSize { get; set; } = 18;

    /// <summary>
    /// 宿主实际生效字体族名（可为 null）。子进程无宿主主题资源：null 时子进程窗口自行落到
    /// "HarmonyOS Sans SC, Microsoft YaHei UI" 兜底链；非 null（如 "Microsoft YaHei UI"）时渲染器
    /// 在根控件显式设置该字体（追加兜底链），保证两种模式字形一致。嵌入资源字体（avares://）无法跨进程
    /// 解析，插件侧应传 null。
    /// </summary>
    public string? FontFamilySource { get; set; }

    // ===== 已解析颜色（0xAARRGGBB，即 Avalonia Color.ToUint32() 布局）=====
    /// <summary>强调色（当前课进度条前景）。</summary>
    public uint AccentArgb { get; set; } = 0xFF0078D4;
    /// <summary>卡片背景（主题卡片色 × 用户不透明度 Alpha）。</summary>
    public uint CardBackgroundArgb { get; set; } = 0xCC2D2D30;
    /// <summary>卡片边框 / 表头分隔线颜色。</summary>
    public uint BorderArgb { get; set; } = 0xFF555555;
    /// <summary>主文字颜色。</summary>
    public uint TextArgb { get; set; } = 0xFFFFFFFF;
    /// <summary>次级文字颜色（教师/课间/占位）。</summary>
    public uint SubTextArgb { get; set; } = 0xFFD3D3D3;
    /// <summary>当前课行高亮背景（强调色 × 40% Alpha）。</summary>
    public uint HighlightArgb { get; set; } = 0x660078D4;
    /// <summary>课间行背景（0x14 + 深浅灰底）。</summary>
    public uint BreakRowBackgroundArgb { get; set; } = 0x14FFFFFF;
    /// <summary>档案分隔线颜色（深色主题白 / 浅色主题黑）。</summary>
    public uint SeparatorArgb { get; set; } = 0xFFFFFFFF;

    /// <summary>是否展示"明天课表"（true=标题行"明日时间表"）。</summary>
    public bool ShowTomorrow { get; set; }

    /// <summary>非空=空表占位模式（仅渲染标题(可选)+占位文字，不渲染 Rows）。</summary>
    public string? PlaceholderText { get; set; }

    /// <summary>课程行（按时间升序，仅已启用课程）。</summary>
    public List<FloatScheduleRowModel> Rows { get; set; } = new();

    /// <summary>当前上课高亮行索引（-1=无）。</summary>
    public int CurrentClassIndex { get; set; } = -1;

    /// <summary>当前课间插入信息（null=不显示课间行）。</summary>
    public FloatScheduleBreakModel? Break { get; set; }

    /// <summary>
    /// 当天全部课间项（含时间区间与插入位置）。
    /// 用途：宿主推送中断（ClassIsland 异常/卡死/崩溃）时，子进程可据此按本地时钟自主判断
    /// "现在是否处于课间、该显示哪个课间行"，从而维持课表状态而不只是冻结最后一帧。
    /// </summary>
    public List<FloatScheduleBreakModel> AllBreaks { get; set; } = new();

    /// <summary>档案分隔线插入位置集合：在第 i 行之后插一条线（值为行索引）。</summary>
    public List<int> SeparatorAfterClassIndex { get; set; } = new();

    /// <summary>构建时刻的当前课进度（0~1，插件权威值）。</summary>
    public double ClassProgressRatio { get; set; }

    /// <summary>构建时刻的课间进度（0~1，插件权威值）。</summary>
    public double BreakProgressRatio { get; set; }

    /// <summary>冻结续走锚点（子进程在管道断开时按本地时钟继续推进进度条）。</summary>
    public FloatScheduleAnchor? Anchor { get; set; }
}

/// <summary>单节课程行（终态显示字符串，教师名解析已在插件侧完成）。</summary>
public sealed class FloatScheduleRowModel
{
    /// <summary>课程名（无课时 "(未安排)"）。</summary>
    public string Course { get; set; } = "";
    /// <summary>教师显示串（null/空=不显示教师列）。</summary>
    public string? Teacher { get; set; }
    /// <summary>时间区间文本 "HH:mm - HH:mm"。</summary>
    public string TimeText { get; set; } = "";
    /// <summary>课程开始（当日秒）。子进程据此在宿主推送中断时按本地时钟自主定位"当前课"。</summary>
    public double StartSec { get; set; }
    /// <summary>课程结束（当日秒）。</summary>
    public double EndSec { get; set; }
}

/// <summary>课间休息项（既是"当前课间插入行"的载体，也用于子进程自主判断课间时段）。</summary>
public sealed class FloatScheduleBreakModel
{
    /// <summary>插入在哪一节课程行之后（0 基；-1 表示位置未定）。</summary>
    public int AfterClassIndex { get; set; } = -1;
    /// <summary>课间名称（默认 "课间休息"）。</summary>
    public string Name { get; set; } = "课间休息";
    /// <summary>课间时间区间文本 "HH:mm - HH:mm"。</summary>
    public string TimeText { get; set; } = "";
    /// <summary>课间开始（当日秒）。</summary>
    public double StartSec { get; set; }
    /// <summary>课间结束（当日秒）。</summary>
    public double EndSec { get; set; }
}

/// <summary>冻结续走锚点：记录构建时刻的进度区间端点与"当时时间"，子进程按本地时钟流逝推算后续进度。</summary>
public sealed class FloatScheduleAnchor
{
    /// <summary>当前高亮课开始（当日秒）。</summary>
    public double ClassStartSec { get; set; }
    /// <summary>当前高亮课结束（当日秒）。</summary>
    public double ClassEndSec { get; set; }
    /// <summary>当前课间开始（当日秒，无课间时 0）。</summary>
    public double BreakStartSec { get; set; }
    /// <summary>当前课间结束（当日秒，无课间时 0）。</summary>
    public double BreakEndSec { get; set; }
    /// <summary>锚点采集时刻的"ClassIsland 当前时间"（当日秒，含调试偏移）。</summary>
    public double NowSecOfDay { get; set; }
    /// <summary>锚点采集时刻是否处于上课状态。</summary>
    public bool OnClass { get; set; }
    /// <summary>锚点采集时刻是否处于课间状态。</summary>
    public bool Breaking { get; set; }
    /// <summary>锚点采集时刻的 UTC 毫秒（子进程用本地 UtcNow 差值推算流逝时间）。</summary>
    public long SentUtcMs { get; set; }
}
