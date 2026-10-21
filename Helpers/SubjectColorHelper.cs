// ATI 学科主题色辅助：
//  - ClassIsland 2.1.1.1（FA3/net10）已在档案 Subject 中原生实现学科主题色（Subject.ColorHex），
//    插件直接复用宿主档案色，不再维护自有体系；编译期 NuGet SDK 2.1.1.1 的 ClassIsland.Shared
//    尚无 ColorHex 属性，故运行时经反射读取。
//  - 旧版 ClassIsland（FA2/net8）无原生学科主题色，插件本地映射预置下方官方默认色表
//    （逐字复制自 ClassIsland Assets/default-subjects.json），并保留插件端编辑能力。
using System;
using System.Collections.Generic;
using System.Globalization;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Helpers;

public static class SubjectColorHelper
{
    /// <summary>
    /// ClassIsland 官方默认学科主题色表（复制自 ClassIsland 2.1.1.1 Assets/default-subjects.json，
    /// 值为 0xAARRGGBB）。供 FA2 端预置/恢复默认，以及 FA3 端预览示例课程使用。
    /// </summary>
    public static readonly IReadOnlyDictionary<string, uint> DefaultSubjectColors = new Dictionary<string, uint>
    {
        ["语文"] = 0xFFE85D68,
        ["数学"] = 0xFF5577E8,
        ["英语"] = 0xFF7667D9,
        ["历史"] = 0xFFB78338,
        ["政治"] = 0xFFD95772,
        ["物理"] = 0xFF369BD7,
        ["化学"] = 0xFF26A69A,
        ["生物"] = 0xFF55A955,
        ["地理"] = 0xFF83A94C,
        ["信息技术"] = 0xFF258FB8,
        ["体育"] = 0xFFE9823D,
        ["自习"] = 0xFF718096,
        ["通用技术"] = 0xFF9A7148,
        ["音乐"] = 0xFF9B62D0,
        ["美术"] = 0xFFD95C9D,
        ["选修课"] = 0xFF70829A,
        ["社团"] = 0xFF35A3B8,
        ["心理"] = 0xFF8A7CC7,
        ["早读"] = 0xFFDDA83B,
        ["晚读"] = 0xFF326CDD,  // 复制自当前运行实例（CI 2.1.1.1）用户档案中自定义的晚读学科色
        ["班会"] = 0xFF477BC4,
        ["周测"] = 0xFFD95A50,
    };

    /// <summary>构造官方默认色表的可变副本（供 PluginSettings 预置与「恢复默认」写入）。</summary>
    public static Dictionary<string, uint> BuildDefaultSubjectColorMap() => new(DefaultSubjectColors);

    /// <summary>
    /// 解析 ClassIsland 档案学科 ColorHex（"#RRGGBB" / "#AARRGGBB"，可省略 '#'）为 0xAARRGGBB；
    /// 6 位十六进制视为不透明（Alpha=FF）。解析失败返回 false。
    /// </summary>
    public static bool TryParseColorHex(string? hex, out uint argb)
    {
        argb = 0;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var t = hex.Trim().TrimStart('#');
        try
        {
            switch (t.Length)
            {
                case 6:
                    if (!uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb6)) return false;
                    argb = 0xFF000000u | rgb6;
                    return true;
                case 8:
                    if (!uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb8)) return false;
                    argb = argb8;
                    return true;
                default:
                    return false;
            }
        }
        catch { return false; }
    }

    /// <summary>
    /// 反射读取宿主档案 Subject 实例的 ColorHex 并解析为 0xAARRGGBB。
    /// 编译期 SDK（≤2.1.1.1 NuGet）无该属性，运行时宿主（2.1.1.1+）才有；不存在或解析失败返回 0
    /// （渲染器回退全局字体色）。
    /// </summary>
    public static uint GetSubjectColorArgb(object? subject)
    {
        if (subject == null) return 0;
        try
        {
            if (ReflectionCache.GetProperty(subject.GetType(), "ColorHex")?.GetValue(subject) is string hex
                && TryParseColorHex(hex, out var argb))
            {
                return argb;
            }
        }
        catch { }
        return 0;
    }
}
