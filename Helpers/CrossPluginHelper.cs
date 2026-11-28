using System;
using System.Reflection;
using System.Text;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 跨插件联动辅助类
/// </summary>
public static class CrossPluginHelper
{
    /// <summary>
    /// 搜索已加载插件中所有“名称或简介命中 Femboy 相关关键词”的插件，
    /// 供崩溃归因使用：命中的插件会整体交给宿主的异常插件禁用机制处理。
    /// 与“真身双因子”判定不同，这里按用户可见的名称与简介做匹配，因此 FemboyTest 的各个
    /// 改名发行版（含名称/简介带“男娘”的变体）都能被一并覆盖。仅能覆盖以插件形式加载在
    /// 本进程内的插件，独立进程版/原生 exe 不在其中。
    /// </summary>
    public static List<PluginInfo> FindFemboyRelatedPlugins()
    {
        var result = new List<PluginInfo>();

        try
        {
            foreach (var plugin in IPluginService.LoadedPlugins)
            {
                // 只归因“真正加载成功且已启用”的插件。IPluginService.LoadedPlugins 在预处理阶段
                // 就会把已禁用的插件一并加入列表（LoadStatus=Disabled），若不过滤，已禁用插件会被
                // 重复列入归因、在崩溃报告里当作“问题插件”显示，并再次写入 .disabled（误报）；
                // 加载失败（Error）与未加载（NotLoaded / 非本地）的插件同理不参与归因。
                if (!plugin.IsEnabled || plugin.LoadStatus != PluginLoadStatus.Loaded)
                {
                    continue;
                }

                if (MatchesFemboyKeyword(plugin.Manifest?.Name) ||
                    MatchesFemboyKeyword(plugin.Manifest?.Description))
                {
                    result.Add(plugin);
                }
            }
        }
        catch
        {
            // 宿主插件服务不可用时按“无命中”处理
        }

        return result;
    }

    /// <summary>
    /// 命中关键词（小写，与规范化后的文本比较）。刻意只保留关键词本身，
    /// 不含 "fem"、"娘" 这类宽泛片段——放宽关键词会误伤名称/简介里恰好出现这些片段的正常插件。
    /// 分隔符、全角字符等把关键词拆散的写法由 <see cref="NormalizeForKeywordMatch"/> 归一化后覆盖，
    /// 不需要靠放宽关键词来兜。
    /// </summary>
    private static readonly string[] FemboyKeywords = new[] { "femboy", "男娘" };

    /// <summary>
    /// 判断插件名称/简介是否命中 Femboy 关键词。匹配分两步：
    /// 1. 字面匹配：先规范化文本，"Femboy"、"Fem·boy"、"F-e-m-b-o-y"、"Ｆｅｍｂｏｙ"、"男娘"、
    ///    "男·娘"、"男の娘" 等等价写法都会被命中；
    /// 2. 近音匹配（仅中文关键词，如 "南梁" ≈ "男娘"），见 <see cref="MatchesHomophoneKeyword"/>。
    /// </summary>
    private static bool MatchesFemboyKeyword(string? text)
    {
        var normalized = NormalizeForKeywordMatch(text);
        if (normalized.Length == 0)
        {
            return false;
        }

        if (FemboyKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal)))
        {
            return true;
        }

        foreach (var keyword in FemboyKeywords)
        {
            if (IsAllHanCharacters(keyword) && MatchesHomophoneKeyword(normalized, keyword))
            {
                return true;
            }
        }

        return false;
    }

    #region 近音匹配（南梁 ≈ 男娘）

    /// <summary>LibPinyin 插件（lrs2187.LibPinyin）暴露的拼音服务接口全名。</summary>
    private const string PinyinServiceTypeName = "LibPinyin4CI.Shared.Services.IPinyinService";

    /// <summary>
    /// 中文关键词字的近音字族，仅在 LibPinyin 插件不可用时作为回退。
    /// 按“宁漏不误伤”取舍：只收常见同音字，以及 n/l 不分这类高频混淆的常见字，不做无差别扩展
    /// ——否则 “男X娘” 的任意组合都会命中，正常插件会被误伤。
    /// </summary>
    private static readonly Dictionary<char, string> HomophoneGroups = new()
    {
        ['男'] = "男南难楠喃囡",                      // nán
        ['娘'] = "娘酿梁良量凉亮粮两俩粱辆谅晾靓踉魉",  // niáng / liáng（n、l 不分）
    };

    /// <summary>单字拼音读法缓存：同一字符的读音恒定，跨次匹配复用，避免重复反射。</summary>
    private static readonly Dictionary<char, List<string>> CharacterPinyinCache = new();

    private static readonly object PinyinAccessLock = new();

    private static object? _pinyinService;

    /// <summary>
    /// 判断文本中是否出现关键词的近音写法（按字数滑动窗口比对）。
    /// 优先用 LibPinyin 插件的拼音服务做模糊比较，服务不可用时回退到内置近音字族表；
    /// 两者都按“宁漏不误伤”取舍——拼音只接受编辑距离 ≤1，字族表只认显式列出的近音字。
    /// </summary>
    private static bool MatchesHomophoneKeyword(string normalizedText, string keyword)
    {
        if (keyword.Length == 0 || normalizedText.Length < keyword.Length)
        {
            return false;
        }

        return MatchesPinyinFuzzy(normalizedText, keyword)
            || MatchesHomophoneSequence(normalizedText, keyword);
    }

    /// <summary>
    /// 用 LibPinyin 插件的拼音服务做近音匹配：对每个等长窗口取各字读音的组合，
    /// 与关键词读音组合比较，编辑距离 ≤1 即视为近音（"nanniang" 与 "nanliang" 相差 1 次替换）。
    /// 服务未安装或调用失败时返回 false，由调用方回退到字族表。
    /// </summary>
    private static bool MatchesPinyinFuzzy(string normalizedText, string keyword)
    {
        var service = TryGetPinyinService();
        if (service == null)
        {
            return false;
        }

        var keywordCombos = BuildPinyinCombos(service, keyword);
        if (keywordCombos.Count == 0)
        {
            return false;
        }

        for (var start = 0; start + keyword.Length <= normalizedText.Length; start++)
        {
            var windowCombos = BuildPinyinCombos(service, normalizedText.Substring(start, keyword.Length));
            foreach (var windowCombo in windowCombos)
            {
                foreach (var keywordCombo in keywordCombos)
                {
                    if (IsWithinEditDistanceOne(windowCombo, keywordCombo))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 用内置近音字族表逐字比对等长窗口：窗口内每个字符都需与关键词对应字符同字或同族才算命中。
    /// </summary>
    private static bool MatchesHomophoneSequence(string normalizedText, string keyword)
    {
        for (var start = 0; start + keyword.Length <= normalizedText.Length; start++)
        {
            var allMatched = true;
            for (var offset = 0; offset < keyword.Length; offset++)
            {
                if (!IsHomophoneCharacter(normalizedText[start + offset], keyword[offset]))
                {
                    allMatched = false;
                    break;
                }
            }

            if (allMatched)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>候选字是否就是关键词字本身，或属于该关键词字的近音字族。</summary>
    private static bool IsHomophoneCharacter(char candidate, char keywordChar) =>
        candidate == keywordChar
        || (HomophoneGroups.TryGetValue(keywordChar, out var group) && group.Contains(candidate));

    /// <summary>
    /// 解析 LibPinyin 插件注册到宿主容器中的拼音服务实例（插件未安装/未启用时返回 null）。
    /// 成功结果缓存；失败不缓存，便于插件晚于本次调用加载时仍能解析到。
    /// </summary>
    private static object? TryGetPinyinService()
    {
        lock (PinyinAccessLock)
        {
            if (_pinyinService != null)
            {
                return _pinyinService;
            }

            try
            {
                var serviceType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType(PinyinServiceTypeName))
                    .FirstOrDefault(type => type != null);

                if (serviceType != null)
                {
                    _pinyinService = IAppHost.Host?.Services.GetService(serviceType);
                }
            }
            catch
            {
                // 宿主容器不可用时按“服务不可用”处理，调用方会回退到字族表
            }

            return _pinyinService;
        }
    }

    /// <summary>
    /// 把文本逐字转成读音并做组合（多音字会展开成多条），单窗口组合数有上限保护。
    /// 逐字调用而非整段调用，是因为 LibPinyin 的整段接口返回的是笛卡尔积，长文本会组合爆炸。
    /// </summary>
    private static List<string> BuildPinyinCombos(object pinyinService, string text)
    {
        const int maxCombos = 64;

        var combos = new List<string> { string.Empty };
        foreach (var ch in text)
        {
            var readings = GetCharacterPinyin(pinyinService, ch);
            var next = new List<string>(Math.Min(combos.Count * readings.Count, maxCombos));
            foreach (var prefix in combos)
            {
                foreach (var reading in readings)
                {
                    next.Add(prefix + reading);
                    if (next.Count >= maxCombos)
                    {
                        break;
                    }
                }

                if (next.Count >= maxCombos)
                {
                    break;
                }
            }

            combos = next;
        }

        return combos;
    }

    /// <summary>取单个字符的读音列表（带缓存）；服务返回空时退化为字符本身。</summary>
    private static List<string> GetCharacterPinyin(object pinyinService, char ch)
    {
        lock (PinyinAccessLock)
        {
            if (CharacterPinyinCache.TryGetValue(ch, out var cached))
            {
                return cached;
            }
        }

        var readings = new List<string>();
        try
        {
            var method = pinyinService.GetType().GetMethod(
                "GetPinyinList", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(char) }, null);
            if (method?.Invoke(pinyinService, new object[] { ch }) is IEnumerable<string> result)
            {
                readings.AddRange(result);
            }
        }
        catch
        {
            // 读取失败时退化为字符本身，保证仍有可比对的串
        }

        if (readings.Count == 0)
        {
            readings.Add(ch.ToString());
        }

        lock (PinyinAccessLock)
        {
            CharacterPinyinCache[ch] = readings;
        }

        return readings;
    }

    /// <summary>
    /// 判断两个拼音串的编辑距离是否 ≤1（允许一次替换 / 插入 / 删除）。
    /// 只处理这种最小差异，不做通用编辑距离——阈值放宽会开始误伤正常插件的拼音。
    /// </summary>
    private static bool IsWithinEditDistanceOne(string left, string right)
    {
        if (string.Equals(left, right, StringComparison.Ordinal))
        {
            return true;
        }

        var lengthDelta = left.Length - right.Length;
        if (lengthDelta is > 1 or < -1)
        {
            return false;
        }

        if (lengthDelta == 0)
        {
            var differenceCount = 0;
            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i] && ++differenceCount > 1)
                {
                    return false;
                }
            }

            return true;
        }

        var longer = lengthDelta > 0 ? left : right;
        var shorter = lengthDelta > 0 ? right : left;
        var longerIndex = 0;
        var skippedOnce = false;
        for (var shorterIndex = 0; shorterIndex < shorter.Length; shorterIndex++)
        {
            if (shorter[shorterIndex] == longer[longerIndex])
            {
                longerIndex++;
                continue;
            }

            if (skippedOnce)
            {
                return false;
            }

            skippedOnce = true;
            longerIndex++;
            shorterIndex--;
        }

        return true;
    }

    /// <summary>是否为纯汉字串（用于挑出需要做近音匹配的中文关键词）。</summary>
    private static bool IsAllHanCharacters(string text) => text.Length > 0 && text.All(IsHanCharacter);

    #endregion

    /// <summary>
    /// 关键词匹配前的规范化：全角字符转半角，丢弃字母/数字/汉字之外的所有字符
    /// （中点、连字符、空格、标点、表情等），并把字母统一为小写。
    /// 于是 "Fem·boy" 与 "Femboy" 归一到同一串：既能识别刻意插入分隔符规避的写法，
    /// 又因为关键词本身没有被放宽，不会误伤名称/简介里只含部分片段的正常插件。
    /// </summary>
    private static string NormalizeForKeywordMatch(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var rawChar in text)
        {
            var ch = ToHalfWidth(rawChar);
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
            else if (IsHanCharacter(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    /// <summary>全角 ASCII（U+FF01–U+FF5E）转半角；全角空格转为普通空格（随后会被丢弃）。</summary>
    private static char ToHalfWidth(char ch) => ch switch
    {
        >= '\uFF01' and <= '\uFF5E' => (char)(ch - 0xFEE0),
        '\u3000' => ' ',
        _ => ch
    };

    /// <summary>是否为汉字（CJK 统一表意文字基本区与扩展 A 区）。</summary>
    private static bool IsHanCharacter(char ch) =>
        ch is >= '\u4E00' and <= '\u9FFF' or >= '\u3400' and <= '\u4DBF';
}
