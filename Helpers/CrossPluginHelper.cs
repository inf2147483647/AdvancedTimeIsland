using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using AdvancedTimeIsland.Models;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 跨插件联动辅助类
/// </summary>
public static class CrossPluginHelper
{
    private const string FemboyTestIdentifier = "FemboyTest";
    private const string FemboyTestIdentifierSignature = "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F";

    /// <summary>
    /// FemboyTest 各"独立进程"版本的跨进程"正在运行"命名事件名（内嵌真身签名，冒充程序无法预知）。
    /// 需与 FemboyTestWpf 插件 Plugin.RunningEventName、FemboyTest_publish 独立版 Program.RunningEventName、
    /// FemboyTest_SecRandom 插件 Plugin.RunningEventName，以及三个原生 exe
    /// （FemboyTestWin32 / FemboyTestWin64 / FemboyTestWinOld）的 CreateEventW 名称保持一致。
    /// </summary>
    private const string FemboyTestRunningEventName = "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F-Running";

    /// <summary>
    /// 彩蛋被强制重置（FemboyTest 与女装彩蛋互斥）时触发
    /// </summary>
    public static event Action? EasterEggForceReset;

    /// <summary>
    /// 互斥判定的**唯一**入口：FemboyTest（任意发行版本）是否处于需要与"女装"彩蛋互斥的状态。
    /// 彩蛋锁（拦截触发、重置已触发状态）与"女装"页内的警告栏必须都调用本方法，
    /// 不得在调用点各自拼表达式：历史上警告栏曾被单独收窄为"仅运行中"，
    /// 与彩蛋锁的判定不一致，形成"锁已释放但警告栏仍在"（或反之）的绕过路径。
    /// 判定为三条路径的**并集**：
    /// 1. 真身双因子反射扫描（见 <see cref="IsFemboyTestPresentByIdentifier"/>）——
    ///    覆盖与本插件同进程的 CI2 版（FemboyTest / FemboyTestNet10）；
    /// 2. 跨进程命名事件（见 <see cref="IsFemboyTestRunningProcess"/>）——
    ///    覆盖运行在其他进程中的版本：WPF 插件版、独立 Avalonia 版（FemboyTest_publish 及其安装包）、
    ///    SecRandom 插件版，以及三个原生 exe（Win32 / win64 / win_old）；事件名内嵌真身签名、冒充者无法伪造；
    /// 3. 标识符文件（见 <see cref="IsFemboyTestIdentifierPresent"/>）——
    ///    兜底"不在本进程、又未发布命名事件"的 CI2 插件版（反射看不到的那一类）：
    ///    只要该插件已安装且未被 .disabled 标记禁用，即视为生效。
    /// 路径 1/2 回答"是否正在运行"，路径 3 回答"是否已启用"；取并集才能保证
    /// 多个 FemboyTest 实例并存时，关闭其中任意一个都不会让互斥提前解除。
    /// 本检测与插件加载顺序无关，无需修改 ClassIsland 或声明 manifest 依赖：
    /// ClassIsland 在插件加载阶段之前会先扫描所有插件目录并把它们（含禁用的）加入
    /// IPluginService.LoadedPlugins，因此本插件 Initialize 时检测立即可用。
    /// 原"程序集名精确匹配"与"清单 ID 匹配"的名称判定可被同名冒充，已不再单独作为互斥依据；
    /// "禁用后重启前程序集仍驻留内存"的场景由签名扫描覆盖：该程序集仍在
    /// AssemblyLoadContext 中，反射扫描依然可见，互斥在重启前持续生效。
    /// </summary>
    public static bool IsFemboyTestEnabled()
    {
        // 检测路径一：双因子签名反射扫描（同进程 CI2 版）
        if (IsFemboyTestPresentByIdentifier())
            return true;

        // 检测路径二：跨进程命名事件检测（WPF 插件版 / 独立 Avalonia 版 / SecRandom 插件版 / 三个原生 exe）
        if (IsFemboyTestRunningProcess())
            return true;

        // 检测路径三：标识符文件检测（不在本进程的 CI2 插件版兜底）
        if (IsFemboyTestIdentifierPresent())
            return true;

        return false;
    }

    /// <summary>
    /// 跨进程检测运行在独立进程中的 FemboyTest 是否正在运行。
    /// 以下版本均不在 ClassIsland 进程内，无法被本进程内的反射扫描覆盖，因此统一由各自进程启动时
    /// 创建命名事件 <see cref="FemboyTestRunningEventName"/> 作为运行信号：
    /// WPF 插件版（ClassIsland 1.7 独立宿主进程）、独立 Avalonia 版（FemboyTest_publish / 安装包）、
    /// SecRandom 插件版、三个原生 exe（Win32 / win64 / win_old）。
    /// - 事件名内嵌"真身双因子"签名，冒充程序无法预知事件名，无法伪造；
    /// - 命名事件是内核对象，创建它的进程退出后自动销毁，OpenExisting 随即失败，检测随之失效。
    /// </summary>
    public static bool IsFemboyTestRunningProcess()
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(FemboyTestRunningEventName);
            return signal != null;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    // 反射扫描结果缓存：以已加载程序集的名称集合为指纹，程序集集合未变化时直接返回缓存。
    // 避免在点击热路径（OnIconClicked 每次点击都调用）上反复执行昂贵的 GetTypes 反射扫描，
    // 否则快速连点会因 UI 卡顿而丢失点击，导致触发彩蛋需要比 11 次更多的点击。
    private static string? _identifierScanFingerprint;
    private static bool _identifierScanResult;

    /// <summary>
    /// 通过反射扫描已加载程序集中带 [PluginEntrance] 特性的插件入口类型，执行"真身双因子"校验：
    /// 要求同一类型上同时存在硬编码常量 <c>UniqueIdentifier == "FemboyTest"</c> 且
    /// <c>IdentifierSignature == "FemboyTest-Auth-9C4E7B1D-A2F3-4C5D-8E9F-1A2B3C4D5E6F"</c>。
    /// 双因子均匹配才判定为真正的 FemboyTest：
    /// - 真插件更换名称（程序集名/清单 ID 变化）后两个常量不变，仍可被识别；
    /// - 冒充插件即使起名为 "FemboyTest"，因缺少签名常量也会被拒绝。
    /// 结果按程序集名称集合缓存：插件加载/卸载会改变集合从而触发重新扫描。
    /// </summary>
    public static bool IsFemboyTestPresentByIdentifier()
    {
        try
        {
            // 计算当前已加载程序集的名称集合指纹（不含动态程序集）
            var names = new System.Collections.Generic.List<string>();
            foreach (var context in AssemblyLoadContext.All)
            {
                foreach (var asm in context.Assemblies)
                {
                    if (asm.IsDynamic) continue;
                    try { names.Add(asm.GetName().Name ?? ""); }
                    catch { }
                }
            }
            names.Sort(StringComparer.Ordinal);
            var fingerprint = string.Join("|", names);

            // 程序集集合未变化：直接返回上次扫描结果
            if (fingerprint == _identifierScanFingerprint)
                return _identifierScanResult;

            // 集合已变化：执行完整反射扫描并缓存结果
            var found = FindFemboyTestEntranceType() != null;

            _identifierScanFingerprint = fingerprint;
            _identifierScanResult = found;
            return found;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 在已加载程序集中扫描 FemboyTest 的插件入口类型（真身双因子校验：标识符与签名常量需同时匹配）。
    /// 找到返回该类型，否则返回 null。供 <see cref="IsFemboyTestPresentByIdentifier"/> 与
    /// <see cref="TryGetFemboyTestPluginInfo"/> 共用，保证两条路径判定完全一致。
    /// </summary>
    private static Type? FindFemboyTestEntranceType()
    {
        foreach (var context in AssemblyLoadContext.All)
        {
            foreach (var asm in context.Assemblies)
            {
                if (asm.IsDynamic)
                    continue;
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        // 只检查插件入口类型
                        if (type.GetCustomAttribute<PluginEntrance>() == null)
                            continue;
                        // 真身双因子：标识符与签名两个常量必须同时匹配
                        var idField = type.GetField("UniqueIdentifier",
                            BindingFlags.Public | BindingFlags.Static);
                        var sigField = type.GetField("IdentifierSignature",
                            BindingFlags.Public | BindingFlags.Static);
                        if (idField != null && idField.IsLiteral &&
                            idField.GetValue(null) is string id &&
                            string.Equals(id, FemboyTestIdentifier, StringComparison.Ordinal) &&
                            sigField != null && sigField.IsLiteral &&
                            sigField.GetValue(null) is string sig &&
                            string.Equals(sig, FemboyTestIdentifierSignature, StringComparison.Ordinal))
                        {
                            return type;
                        }
                    }
                }
                catch
                {
                    // GetTypes 可能因引用缺失等抛异常，跳过该程序集
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 解析 FemboyTest（同进程 CI2 插件版）对应的宿主 <see cref="PluginInfo"/>。
    /// 复用与 <see cref="IsFemboyTestEnabled"/> 路径一完全一致的真身双因子扫描，
    /// 再经 AssemblyLoadContext → PluginLoadContext.Info 映射——与宿主
    /// DiagnosticService.GetPluginsByStacktrace 的归因方式同源，因此可用于
    /// 让宿主按“错误由该插件引起”处理（即禁用该插件）。
    /// 仅当 FemboyTest 以插件形式加载在本进程内时可解析；独立进程版/原生 exe 返回 null。
    /// </summary>
    public static PluginInfo? TryGetFemboyTestPluginInfo()
    {
        try
        {
            var entranceType = FindFemboyTestEntranceType();
            if (entranceType == null)
                return null;

            var context = AssemblyLoadContext.GetLoadContext(entranceType.Assembly);
            if (context == null)
                return null;

            // PluginLoadContext.Info 为宿主类型成员，用反射读取以保持对宿主程序集零编译期依赖
            var infoProperty = context.GetType()
                .GetProperty("Info", BindingFlags.Public | BindingFlags.Instance);
            return infoProperty?.GetValue(context) as PluginInfo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 搜索已加载插件中所有“名称或简介命中 Femboy 相关关键词”的插件，
    /// 供崩溃归因使用：命中的插件会整体交给宿主的异常插件禁用机制处理。
    /// 与 <see cref="TryGetFemboyTestPluginInfo"/> 的真身双因子判定不同，这里按用户可见的
    /// 名称与简介做匹配，因此 FemboyTest 的各个改名发行版（含名称/简介带“男娘”的变体）
    /// 都能被一并覆盖。仅能覆盖以插件形式加载在本进程内的插件，独立进程版/原生 exe 不在其中。
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

    /// <summary>
    /// 检测 FemboyTest 插件是否"已安装且已启用"（唯一标识符文件 + .disabled 标记）。
    /// FemboyTest 在初始化时会在其配置目录写入 unique_identifier.txt
    /// （两行：UniqueIdentifier + IdentifierSignature），
    /// 本方法通过读取该文件并做双因子校验识别 FemboyTest 的真实存在。
    /// 它是 <see cref="IsFemboyTestEnabled"/> 三条路径里唯一能覆盖"跨进程 CI2 插件版"的一条：
    /// 反射扫描只看本进程、命名事件只看发布了信号的变种，都认不出跑在另一个进程里的 CI2 插件。
    /// 遍历插件配置根目录下的全部插件目录查找：FemboyTest 各 CI2 版本共用同一标识符文件，
    /// 但配置目录名随发行版本而异（FemboyTest / FemboyTestNet10 / FemboyTest_SecRandom），不能写死目录名。
    /// 注意：ClassIsland 禁用插件只写入 .disabled 标记、不删除该文件，
    /// 因此必须同时校验 .disabled 标记，避免 FemboyTest 禁用后仍因文件残留而误判为启用。
    /// </summary>
    public static bool IsFemboyTestIdentifierPresent()
    {
        try
        {
            var pluginsRoot = Path.GetDirectoryName(Plugin.Instance.PluginConfigFolder);
            if (string.IsNullOrEmpty(pluginsRoot) || !Directory.Exists(pluginsRoot))
                return false;

            foreach (var pluginDir in Directory.GetDirectories(pluginsRoot))
            {
                try
                {
                    var identifierPath = Path.Combine(pluginDir, "unique_identifier.txt");
                    if (!File.Exists(identifierPath))
                        continue;

                    // 插件已被禁用（.disabled 标记存在）：文件残留不代表插件在运行，跳过该目录
                    if (File.Exists(Path.Combine(pluginDir, ".disabled")))
                        continue;

                    // 双因子校验：第一行标识符 + 第二行签名，两者都匹配才算真身
                    var lines = File.ReadAllLines(identifierPath);
                    if (lines.Length >= 2 &&
                        lines[0].Trim() == FemboyTestIdentifier &&
                        lines[1].Trim() == FemboyTestIdentifierSignature)
                        return true;
                }
                catch
                {
                    // 单个插件目录读取失败（权限/占用等）不影响其余目录的检测
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// FemboyTest 与女装彩蛋互斥。
    /// 当 FemboyTest 已启用时，将彩蛋状态重置为未触发。
    /// </summary>
    /// <returns>是否执行了重置</returns>
    public static bool ResetEasterEggIfFemboyTestEnabled(PluginSettings settings)
    {
        if (!IsFemboyTestEnabled())
            return false;
        if (!settings.EnableEasterEgg)
            return false;

        settings.EnableEasterEgg = false;
        EasterEggForceReset?.Invoke();
        return true;
    }

    /// <summary>
    /// 安排一次彩蛋互斥检测，在指定延迟后执行一次。
    /// 用于插件初始化后延迟检测：此时所有插件均已加载完成，
    /// 对 FemboyTest 是否在运行的判断准确可靠。
    /// </summary>
    public static void ScheduleEasterEggMutexCheck(PluginSettings settings, TimeSpan delay)
    {
        var timer = new DispatcherTimer
        {
            Interval = delay
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            // 彩蛋未触发时无需检查，快速跳过
            if (!settings.EnableEasterEgg)
                return;
            ResetEasterEggIfFemboyTestEnabled(settings);
        };
        timer.Start();
    }
}
