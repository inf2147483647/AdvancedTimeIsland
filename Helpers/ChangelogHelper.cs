using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 更新日志数据源（WPF 版）：读取插件目录的 CHANGELOG.md，按「## [x.y.z.w] - 日期」解析为版本段。
/// 每个版本段保留原始 markdown 文本（RawMarkdown），渲染统一交给 hanfu_page_template 的
/// RenderMarkdownToPanel（Markdig 全语法 + 模板扩展），以兼容将来 CHANGELOG 中任意格式。
/// 展示范围过滤：剔除旧代 1.7.x.x 版本，以及超过当前插件版本（manifest.yml 中 version）
/// 的未发布段落——后者在版本号升级后会自动进入展示范围。
/// 文件读取与解析均为纯文本操作，统一放在线程池执行，不阻塞 UI 线程。
/// </summary>
public static class ChangelogHelper
{
    /// <summary>一个版本的更新说明（正文为原始 markdown 文本）。</summary>
    public sealed record ChangelogEntry(string Version, string Date, string RawMarkdown);

    /// <summary>匹配「## [2.0.5.4] - 2026-10-18」形式的版本标题（日期可缺省）。</summary>
    private static readonly Regex VersionHeadingRegex = new(
        @"^\s*##\s*\[(?<version>\d+\.\d+\.\d+\.\d+)\](?:\s*-\s*(?<date>.+?))?\s*$",
        RegexOptions.Compiled);

    /// <summary>「未发布」段的版本标识（无数字版本号，随包分发后即对应当前版本的内容）。</summary>
    public const string UnreleasedVersion = "未发布";

    /// <summary>匹配「## [未发布]」（含 Unreleased）形式的未发布标题。</summary>
    private static readonly Regex UnreleasedHeadingRegex = new(
        @"^\s*##\s*\[?\s*(?<unreleased>未发布|Unreleased)\s*\]?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>CHANGELOG.md 不存在或读取失败时返回空串（由调用方呈现“暂无记录”）。</summary>
    private static string ReadChangelogMarkdown()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CHANGELOG.md");
            if (!File.Exists(path))
            {
                var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    path = Path.Combine(pluginDir, "CHANGELOG.md");
                }
            }

            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>读取 manifest.yml 的 version，失败返回 null（此时不做版本上限过滤）。</summary>
    private static Version? GetCurrentVersion()
    {
        try
        {
            var manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "manifest.yml");
            if (!File.Exists(manifestPath))
            {
                var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    manifestPath = Path.Combine(pluginDir, "manifest.yml");
                }
            }

            if (!File.Exists(manifestPath))
            {
                return null;
            }

            var content = File.ReadAllText(manifestPath);
            var match = Regex.Match(content, @"^\s*version\s*:\s*(.+?)\s*$", RegexOptions.Multiline);
            if (match.Success && Version.TryParse(match.Groups[1].Value.Trim().Trim('"', '\''), out var version))
            {
                return version;
            }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>异步加载（后台线程读取 + 解析）并按展示范围过滤后的版本段列表（保持 CHANGELOG 中新版本在前的顺序）。</summary>
    public static async Task<List<ChangelogEntry>> LoadEntriesAsync()
    {
        var markdown = await Task.Run(ReadChangelogMarkdown).ConfigureAwait(false);
        var currentVersion = await Task.Run(GetCurrentVersion).ConfigureAwait(false);
        var entries = await Task.Run(() => ParseEntries(markdown)).ConfigureAwait(false);

        return entries
            .Where(e => e.Version == UnreleasedVersion                // 未发布段保留（随包分发后即当前版本内容）
                        || (Version.TryParse(e.Version, out var v) && v != null
                            && !(v.Major == 1 && v.Minor == 7)          // 过滤 1.7.x.x 旧代版本
                            && (currentVersion == null || v <= currentVersion))) // 过滤超过当前插件版本的未发布段
            .ToList();
    }

    /// <summary>把 CHANGELOG.md 全文按版本标题切段，段内保留原始 markdown 行。</summary>
    private static List<ChangelogEntry> ParseEntries(string markdown)
    {
        var entries = new List<ChangelogEntry>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return entries;
        }

        string? currentVersion = null;
        var currentDate = string.Empty;
        var currentLines = new List<string>();

        void FlushEntry()
        {
            if (currentVersion != null)
            {
                entries.Add(new ChangelogEntry(currentVersion, currentDate, string.Join("\n", currentLines)));
            }
        }

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var match = VersionHeadingRegex.Match(rawLine);
            if (match.Success)
            {
                FlushEntry();
                currentVersion = match.Groups["version"].Value;
                currentDate = match.Groups["date"].Success ? match.Groups["date"].Value.Trim() : string.Empty;
                currentLines = new List<string>();
                continue;
            }

            // 「## [未发布]」段：同样切为新段（Version 标记为“未发布”，随包分发后即当前版本的内容）
            var unreleasedMatch = UnreleasedHeadingRegex.Match(rawLine);
            if (unreleasedMatch.Success)
            {
                FlushEntry();
                currentVersion = UnreleasedVersion;
                currentDate = string.Empty;
                currentLines = new List<string>();
                continue;
            }

            // 版本标题之前的行（文档头）与一级标题不属于任何版本段
            if (currentVersion == null || string.IsNullOrWhiteSpace(rawLine) || rawLine.TrimStart().StartsWith("# "))
            {
                continue;
            }

            currentLines.Add(rawLine.TrimEnd());
        }

        FlushEntry();
        return entries;
    }
}
