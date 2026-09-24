// 独立进程模式 IPC 协议：JSON Lines（\n 分帧，UTF-8），System.Text.Json（零额外依赖）。
// 管道服务器 = 插件进程（ClassIsland 内），客户端 = 子进程；
// 服务器用固定命名管道，保证"跟随启停=关"时幸存的子进程在 ClassIsland 重启后能自动重连（adopt）。
// 消息封包：{"t":<int 类型>,"d":<payload 对象或 null>}
using System;
using System.Text.Json;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public enum FloatScheduleIpcMsgType
{
    // 插件 → 子进程
    Init = 0,        // 连接建立：完整 settings + model
    Settings = 1,    // 行为设置快照（含 FollowHostLifetime 热更新）
    Model = 2,       // 完整渲染模型（整表重建）
    Progress = 3,    // 500ms 进度增量 {ClassRatio, BreakRatio}
    Visible = 4,     // 宿主隐藏判定 {Visible}
    Shutdown = 5,    // 正常退出指令
    // 子进程 → 插件
    Ready = 16,      // {Pid}
    Position = 17,   // {X, Y} 拖拽稳定后的新位置（物理像素）
    Error = 18,      // {Code, Message}
    ExitRequested = 19,  // 用户通过子进程托盘图标选择"退出"：插件据此关闭悬浮时间表且不再自动重启
}

/// <summary>Init 消息体。</summary>
public sealed class FloatScheduleInitPayload
{
    public FloatScheduleWindowSettings? Settings { get; set; }
    public FloatScheduleRenderModel? Model { get; set; }
}

/// <summary>Progress 消息体。</summary>
public sealed class FloatScheduleProgressPayload
{
    public double ClassRatio { get; set; }
    public double BreakRatio { get; set; }
}

/// <summary>Visible 消息体。</summary>
public sealed class FloatScheduleVisiblePayload
{
    public bool Visible { get; set; } = true;
}

/// <summary>Ready 消息体。</summary>
public sealed class FloatScheduleReadyPayload
{
    public int Pid { get; set; }
}

/// <summary>Position 消息体。</summary>
public sealed class FloatSchedulePositionPayload
{
    public int X { get; set; }
    public int Y { get; set; }
}

/// <summary>Error 消息体。</summary>
public sealed class FloatScheduleErrorPayload
{
    public int Code { get; set; }
    public string? Message { get; set; }
}

public static class FloatScheduleIpc
{
    /// <summary>固定命名管道名（服务器=插件进程）。</summary>
    public const string PipeName = "AdvancedTimeIsland.FloatSchedule.v2";

    /// <summary>协议版本；不匹配时双方按兼容处理（当前仅 1）。</summary>
    public const int ProtocolVersion = 1;

    /// <summary>子进程单实例 Mutex 名（"单实例保护"开启时使用）。</summary>
    public const string SingleInstanceMutexName = @"Global\AdvancedTimeIsland.FloatSchedule.v2";

    /// <summary>宿主 ClassIsland 全局单实例 Mutex 名（子进程看门狗双保险：等待其 abandoned）。</summary>
    public const string HostInstanceMutexName = @"Global\ClassIsland.Lock";

    // 子进程约定退出码
    public const int ExitCodeAnotherInstance = 42;   // 单实例保护命中
    public const int ExitCodeMissingRuntime = 43;    // 宿主目录缺少 Avalonia/Skia 运行库
    public const int ExitCodeBadArgs = 44;           // 参数缺失/非法
    // 独立程序与宿主 Avalonia 代际不匹配（插件包选错）：
    //   本 exe 是"双 TFM 各一个"产物，Avalonia 跨代二进制不兼容，必须与宿主精确配对。
    //   单独开一个退出码，避免像以前那样与"缺少运行库"混在一起被误报。
    public const int ExitCodeAvaloniaMismatch = 45;
    // Avalonia 启动阶段抛异常（非"文件缺失"、也非"代际不匹配"）：单独退出码，
    //   让宿主端能提示"启动失败 + 子进程原始错误"，而不是笼统地把用户引向"缺少运行库"。
    public const int ExitCodeAvaloniaStartFailed = 46;
    // 子进程自检发现"自己已被新版插件淘汰"（自身 exe 内容指纹 ≠ 插件下发的指纹）→ 主动退出。
    // 宿主据此识别为**计划内重启**（不受"意外退出"限频约束），立即用新构建重启一个新子进程。
    public const int ExitCodeSelfUpdate = 47;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>编码一行 JSON（不含结尾 \n）。</summary>
    public static string Encode(FloatScheduleIpcMsgType type, object? payload)
    {
        var payloadJson = payload == null
            ? "null"
            : JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions);
        return "{\"t\":" + (int)type + ",\"d\":" + payloadJson + "}";
    }

    /// <summary>解码一行 JSON；失败返回 false。</summary>
    public static bool TryDecode(string line, out FloatScheduleIpcMsgType type, out JsonElement data)
    {
        type = default;
        data = default;
        if (string.IsNullOrWhiteSpace(line)) return false;
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (!doc.RootElement.TryGetProperty("t", out var tEl)) return false;
            // 兼容 camelCase（JsonSerializerDefaults.Web）与原始大小写
            if (!doc.RootElement.TryGetProperty("d", out var dEl) &&
                !doc.RootElement.TryGetProperty("D", out dEl))
                dEl = default;
            type = (FloatScheduleIpcMsgType)tEl.GetInt32();
            data = dEl.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>把 data 反序列化为 T；data 为 null/缺失时返回 default。</summary>
    public static T? Deserialize<T>(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Undefined || data.ValueKind == JsonValueKind.Null)
            return default;
        try
        {
            return data.Deserialize<T>(JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
