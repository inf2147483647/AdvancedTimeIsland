using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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
    /// 找到返回该类型，否则返回 null。供 <see cref="IsFemboyTestPresentByIdentifier"/> 使用。
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

    /// <summary>
    /// 扫描宿主中“入口程序集实际已加载到本进程”的“女装/男娘”类插件：名称 / 标识符 / 简介任意一项
    /// 命中语义规则（见 <see cref="IsFemboyLikeText"/>），<b>且</b>其入口 DLL 真的被宿主加载进了某个
    /// <see cref="AssemblyLoadContext"/>（见 <see cref="IsPluginAssemblyLoaded"/>）才计入结果。
    /// 女装图片加载拦截（EasterEggPage）与 JK 制服页崩溃归因（WomenswearPage）共用本入口，
    /// 两处判定必须保持一致，不得各自再实现一份匹配规则。
    /// 判定只认“DLL 是否在本进程内运行”，不再依赖 <see cref="PluginInfo.IsEnabled"/> /
    /// <see cref="PluginLoadStatus"/>：宿主禁用插件只是写入 .disabled 标记并置 RestartRequired，
    /// 程序集在本次运行期内仍驻留内存，故禁用后、重启前依然构成冲突；重启宿主后该 DLL 不再加载、
    /// 扫描结果自然为空——与错误面板“请禁用以下插件并重启”的提示一致。
    /// 实时扫描、不做缓存：用户重启后重新进入页面即恢复，无需任何手动刷新状态。
    /// 单次调用只枚举一次全部已加载程序集构建路径集合，再对插件集合做集合命中判定，
    /// 避免“插件数 × 程序集数”的嵌套枚举。
    /// </summary>
    public static IReadOnlyList<PluginInfo> GetRunningFemboyLikePlugins()
    {
        try
        {
            var loadedAssemblyLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loadedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectLoadedAssemblies(loadedAssemblyLocations, loadedAssemblyNames);

            return IPluginService.LoadedPlugins
                .Where(IsFemboyLikePlugin)
                .Where(plugin => IsPluginAssemblyLoaded(plugin, loadedAssemblyLocations, loadedAssemblyNames))
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetRunningFemboyLikePlugins failed: {ex}");
            return Array.Empty<PluginInfo>();
        }
    }

    /// <summary>
    /// 枚举当前进程全部 <see cref="AssemblyLoadContext"/> 中已加载的非动态程序集，
    /// 收集其磁盘完整路径（宿主用 <c>PluginLoadContext.LoadFromAssemblyPath</c> 加载插件，
    /// 正常情况下 <see cref="Assembly.Location"/> 即插件目录下的 DLL 完整路径）；
    /// Location 为空（极个别内存加载场景）时退而收集程序集名，供 <see cref="IsPluginAssemblyLoaded"/> 兜底。
    /// </summary>
    private static void CollectLoadedAssemblies(HashSet<string> locations, HashSet<string> assemblyNames)
    {
        foreach (var context in AssemblyLoadContext.All)
        {
            foreach (var assembly in context.Assemblies)
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                try
                {
                    var location = assembly.Location;
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        locations.Add(Path.GetFullPath(location));
                    }
                    else
                    {
                        var name = assembly.GetName().Name;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            assemblyNames.Add(name);
                        }
                    }
                }
                catch
                {
                    // 单个程序集信息读取失败不影响其余判定
                }
            }
        }
    }

    /// <summary>
    /// 判断插件入口 DLL 是否实际加载在本进程中：
    /// 优先用 <c>插件目录\Manifest.EntranceAssembly</c> 的完整路径与已加载程序集的 Location 比对
    ///（大小写不敏感）；路径比对不到（入口程序集为无 Location 的内存加载）时，
    /// 退化为程序集名（不含扩展名）精确比对。仅市场元数据、未落本地的插件
    ///（<see cref="PluginInfo.PluginFolderPath"/> 为空）直接返回 false。
    /// </summary>
    private static bool IsPluginAssemblyLoaded(
        PluginInfo plugin,
        HashSet<string> loadedAssemblyLocations,
        HashSet<string> loadedAssemblyNames)
    {
        try
        {
            var entranceAssembly = plugin.Manifest?.EntranceAssembly;
            if (string.IsNullOrWhiteSpace(entranceAssembly)
                || string.IsNullOrWhiteSpace(plugin.PluginFolderPath))
            {
                return false;
            }

            var targetPath = Path.GetFullPath(Path.Combine(plugin.PluginFolderPath, entranceAssembly));
            if (loadedAssemblyLocations.Contains(targetPath))
            {
                return true;
            }

            // 兜底：内存加载、Location 为空时按程序集名命中（宿主插件均为磁盘加载，正常走不到这里）
            var targetName = Path.GetFileNameWithoutExtension(targetPath);
            return !string.IsNullOrWhiteSpace(targetName) && loadedAssemblyNames.Contains(targetName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>名称、标识符或简介任意一项命中“女装/男娘”语义，即视为同类插件。</summary>
    private static bool IsFemboyLikePlugin(PluginInfo plugin)
    {
        try
        {
            return IsFemboyLikeText(plugin.Manifest.Name)
                   || IsFemboyLikeText(plugin.Manifest.Id)
                   || IsFemboyLikeText(plugin.Manifest.Description);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>拉丁写法关键词：femboy 不是汉字，拼音转写不适用，故直接按关键词命中。</summary>
    private const string FemboyLatinKeyword = "femboy";

    /// <summary>“男娘”的标准全拼，作为中文写法的匹配基准。</summary>
    private const string NanniangPinyin = "nanniang";

    /// <summary>允许的拼音编辑距离：0 为精确匹配，1 为允许一次增删改（如 南梁 → nanliang）。</summary>
    private const int NanniangMaxDistance = 1;

    /// <summary>
    /// 判断文本是否属于“女装/男娘”语义：
    /// 1) 拉丁写法（femboy 及其大小写/分隔符/全角变体）直接按关键词命中——拼音转写不适用于非汉字；
    /// 2) 中文写法转全拼后与 <see cref="NanniangPinyin"/> 做“精确 / 编辑距离 1”匹配，
    ///    覆盖 男娘（nanniang）、楠酿（nanniang）、南梁（nanliang）等同音/近音写法。
    /// 全拼由 <see cref="GetFullPinyinCandidates"/> 提供：已安装可选的 LibPinyin4CI 时
    /// 走其 IPinyinService，未安装时自动回退到内置的简易拼音匹配，故此处无需再做字面兜底。
    /// </summary>
    public static bool IsFemboyLikeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // 先归一化：全角转半角、剔除分隔符与助词、转小写
        // （男の娘 → 男娘；Ｆｅｍｂｏｙ → femboy；F e m b o y → femboy）
        var normalized = NormalizeForMatch(text);

        if (normalized.Contains(FemboyLatinKeyword))
        {
            return true;
        }

        // 候选可能来自 LibPinyin（TitleCase，如 NanNiang）或内置匹配器（小写），比较前统一转小写
        return GetFullPinyinCandidates(normalized).Any(candidate =>
            LevenshteinDistance(candidate.ToLowerInvariant(), NanniangPinyin, NanniangMaxDistance)
                <= NanniangMaxDistance);
    }

    /// <summary>
    /// 计算两字符串的编辑距离（Levenshtein，插入/删除/替换代价均为 1）。
    /// 若长度差已超过 <paramref name="maxDistance"/>，直接返回一个大于上限的值，省去完整的 DP。
    /// </summary>
    private static int LevenshteinDistance(string source, string target, int maxDistance)
    {
        if (Math.Abs(source.Length - target.Length) > maxDistance)
        {
            return maxDistance + 1;
        }

        var previous = new int[target.Length + 1];
        var current = new int[target.Length + 1];
        for (var j = 0; j <= target.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= target.Length; j++)
            {
                var substitutionCost = source[i - 1] == target[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }

    /// <summary>归一化时需要剔除的“装饰性”分隔符与助词。</summary>
    private static readonly HashSet<char> _ignoredMatchChars = new()
    {
        ' ', '\t', '\r', '\n',
        '·', '・', '•', '‧',              // 各类中点/间隔号
        '-', '_', '.', '~', '|', '/', '\\',
        'の',                              // 日文所属格助词（男の娘 → 男娘）
    };

    /// <summary>
    /// 把文本归一化到便于语义比对的形式：
    /// 1) NFKC 兼容规范化——全角字母/数字转半角（Ｆｅｍｂｏｙ → Femboy）；
    /// 2) 剔除分隔符与助词（见 <see cref="_ignoredMatchChars"/>），使 Fem·boy / F e m b o y /
    ///    男·娘 / 男の娘 与紧凑写法等价；
    /// 3) 转小写——消除大小写差异（FEMBOY → femboy）。
    /// </summary>
    private static string NormalizeForMatch(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text.Normalize(System.Text.NormalizationForm.FormKC))
        {
            if (_ignoredMatchChars.Contains(ch))
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static object? _pinyinService;
    private static MethodInfo? _getFullPinyinListMethod;
    private static bool _pinyinServiceLookupDone;

    /// <summary>走 LibPinyin 整段接口的最大文本长度：该接口返回多音字笛卡尔积，长文本会组合爆炸。</summary>
    private const int MaxLibPinyinTextLength = 24;

    /// <summary>
    /// 取文本的全拼候选（如“男娘”→<c>NanNiang</c>、“南梁”→<c>NanLiang</c>/<c>NaLiang</c>）。
    /// 优先使用 LibPinyin4CI（https://github.com/lrsgzs/LibPinyin4CI）——它在本插件清单中登记为
    /// <b>可选依赖</b>（<c>lrs2187.LibPinyin</c>，<c>isRequired: false</c>）：
    /// 该插件在 Initialize 中把服务以 <c>AddSingleton&lt;IPinyinService, PinyinService&gt;</c> 注册进宿主 DI；
    /// 其共享程序集 <c>LibPinyin4CI.Shared</c> 并非本插件的编译期依赖，故运行时按类型名反射取服务实例——
    /// 既不引入程序集依赖，也不受各插件独立 AssemblyLoadContext 的类型标识差异影响。
    /// 未安装、未启用或解析失败时回退到内置的 <see cref="SimplePinyinMatcher"/>。
    /// 注意：LibPinyin 返回 TitleCase（如 <c>NanNiang</c>），简单匹配器返回小写，比较时请忽略大小写。
    /// </summary>
    public static IReadOnlyList<string> GetFullPinyinCandidates(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        // LibPinyin 的 GetFullPinyinList(string) 返回逐字读音的笛卡尔积，文本越长组合数增长越快
        // （插件简介可达数十字），故只对短文本（名称 / 标识符）走该接口；超长文本直接用逐字替换的
        // 简易匹配器，避免无谓的指数级展开。此处长度约束对调用方透明。
        if (text.Length <= MaxLibPinyinTextLength)
        {
            var fromLibPinyin = TryGetFullPinyinFromLibPinyin(text);
            if (fromLibPinyin.Count > 0)
            {
                return fromLibPinyin;
            }
        }

        return SimplePinyinMatcher.GetFullPinyinCandidates(text);
    }

    /// <summary>通过 LibPinyin4CI 的 IPinyinService 取全拼候选；不可用时返回空列表（由调用方回退）。</summary>
    private static IReadOnlyList<string> TryGetFullPinyinFromLibPinyin(string text)
    {
        try
        {
            var service = TryGetPinyinService();
            var method = _getFullPinyinListMethod;
            if (service == null || method == null)
            {
                return Array.Empty<string>();
            }

            return method.Invoke(service, new object[] { text }) is IEnumerable<string> candidates
                ? candidates.ToList()
                : Array.Empty<string>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TryGetFullPinyinFromLibPinyin failed: {ex}");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// 解析 LibPinyin4CI 注册进宿主 DI 的 <c>IPinyinService</c> 实例。
    /// 只解析一次（插件集合在启动后固定）；未安装或解析失败时返回 null。
    /// </summary>
    private static object? TryGetPinyinService()
    {
        if (_pinyinServiceLookupDone)
        {
            return _pinyinService;
        }

        _pinyinServiceLookupDone = true;
        try
        {
            var serviceInterfaceType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("LibPinyin4CI.Shared.Services.IPinyinService"))
                .FirstOrDefault(type => type != null);
            if (serviceInterfaceType == null)
            {
                return null;
            }

            _getFullPinyinListMethod = serviceInterfaceType.GetMethod(
                "GetFullPinyinList", new[] { typeof(string) });
            _pinyinService = IAppHost.Host?.Services.GetService(serviceInterfaceType);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TryGetPinyinService failed: {ex}");
        }

        return _pinyinService;
    }

    /// <summary>
    /// 自制简易拼音匹配：未安装可选依赖 LibPinyin4CI 时的回退实现。
    /// 不做完整的汉字拼音表，只收录匹配基准 <c>nanniang</c> 及其编辑距离 1 邻域所需的常用字
    /// （nan / niang / liang 三族）的音节，其余字符原样透传——透传字符会参与编辑距离计算，
    /// 从而自然把无关文本排除在外。
    /// 相比逐一列举中文字面量，它具备组合能力：男酿、南娘、楠娘 等未列举的组合同样能命中。
    /// 返回单个候选（不展开多音字），统一为小写。
    /// </summary>
    private static class SimplePinyinMatcher
    {
        private static readonly Dictionary<char, string> _syllables = new()
        {
            // nan
            ['男'] = "nan", ['南'] = "nan", ['楠'] = "nan", ['囡'] = "nan", ['难'] = "nan", ['喃'] = "nan",
            // niang
            ['娘'] = "niang", ['酿'] = "niang",
            // liang：与 "nanniang" 编辑距离 1 的最常见落点
            ['梁'] = "liang", ['凉'] = "liang", ['良'] = "liang", ['两'] = "liang", ['亮'] = "liang",
            ['量'] = "liang", ['粮'] = "liang", ['谅'] = "liang",
        };

        public static IReadOnlyList<string> GetFullPinyinCandidates(string text)
        {
            var builder = new System.Text.StringBuilder(text.Length * 5);
            foreach (var ch in text)
            {
                builder.Append(_syllables.TryGetValue(ch, out var syllable)
                    ? syllable
                    : char.ToLowerInvariant(ch).ToString());
            }

            return new[] { builder.ToString() };
        }
    }
}
