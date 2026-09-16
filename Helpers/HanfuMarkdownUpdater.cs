using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 汉服 Markdown 内容热更新。
/// 插件每次启动时在后台线程查询 GitHub 最新 release，无论本地版本是否最新都重新下载
/// AdvancedTimeIslandHanfu.zip，校验后解压到插件目录下的 Markdown 文件夹并整体替换，
/// 用远端权威内容覆盖本地可能被篡改的文件（防篡改）。
/// 下载回退链：GitHub 直链 → 高速镜像1 → 高速镜像2 → … → 本地缓存；
/// 每个来源都要求落地文件为合法 zip，否则切换下一来源，全部失败才保留本地缓存。
/// 全程异步执行、不阻塞 UI。
/// </summary>
public static class HanfuMarkdownUpdater
{
    private const string LatestReleaseApiUrl =
        "https://api.github.com/repos/inf2147483647/AdvancedTimeIslandHanfu/releases/latest";

    private const string ZipAssetName = "AdvancedTimeIslandHanfu.zip";
    private const string MarkdownFolderName = "Markdown";
    private const string VersionFileName = ".hanfu_version";
    private const string StagingFolderName = "Markdown.update.tmp";
    private const string BackupFolderName = "Markdown.old";

    /// <summary>
    /// GitHub release 高速下载源回退链：先尝试 GitHub 直链，再依次尝试镜像前缀。
    /// 顺序：GitHub → gh-proxy.com → gh-proxy.at9.net → ghproxy.net → ghfast.top → 本地缓存。
    /// 镜像均为「前缀 + 完整 GitHub URL」的透传代理；节点可用性会变化，
    /// 因此任一来源只要下载失败或内容不是合法 zip 就立即切换，全部失败才回退本地缓存。
    /// </summary>
    private static readonly string[] DownloadMirrorPrefixes =
    {
        "https://gh-proxy.com/",
        "https://gh-proxy.at9.net/",
        "https://ghproxy.net/",
        "https://ghfast.top/"
    };

    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly object UpdateLock = new();
    private static bool _isUpdating;

    /// <summary>在后台线程启动一次更新检查，立即返回，不阻塞插件加载。</summary>
    public static void Start()
    {
        _ = Task.Run(CheckAndUpdateAsync);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
        // GitHub API 要求所有请求携带 User-Agent，否则直接返回 403
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AdvancedTimeIsland-Plugin");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    /// <summary>插件根目录：优先使用程序集所在目录，拿不到时回退到 AppContext.BaseDirectory。</summary>
    private static string ResolvePluginRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        return string.IsNullOrEmpty(assemblyLocation)
            ? baseDir
            : Path.GetDirectoryName(assemblyLocation) ?? baseDir;
    }

    public static async Task CheckAndUpdateAsync()
    {
        lock (UpdateLock)
        {
            if (_isUpdating)
            {
                return;
            }
            _isUpdating = true;
        }

        try
        {
            var pluginRoot = ResolvePluginRoot();
            TryDelete(Path.Combine(pluginRoot, BackupFolderName));

            var finalDir = Path.Combine(pluginRoot, MarkdownFolderName);
            var hasLocalCache = Directory.Exists(finalDir) &&
                Directory.GetFiles(finalDir, "*.md", SearchOption.TopDirectoryOnly).Length > 0;

            var (tag, downloadUrl) = await QueryLatestReleaseAsync();
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(downloadUrl))
            {
                // 查询不到远端时不动本地目录，页面继续从本地缓存加载（此时防篡改无法保证）
                DebugLog(hasLocalCache
                    ? "未获取到最新 release 信息，继续使用本地缓存的汉服内容（防篡改可能失效）。"
                    : "未获取到最新 release 信息，且本地无缓存内容。");
                return;
            }

            // 无论本地版本是否最新，每次启动都强制重新下载-解压-替换，
            // 用远端权威内容覆盖本地可能被篡改过的文件。
            DebugLog($"开始拉取汉服内容（远端版本 {tag}）。");

            var stagingRoot = Path.Combine(pluginRoot, StagingFolderName);
            TryDelete(stagingRoot);
            var extractDir = Path.Combine(stagingRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(extractDir);

            var zipPath = Path.Combine(stagingRoot, ZipAssetName);
            var usedSource = await DownloadWithMirrorsAsync(downloadUrl, zipPath);
            if (!IsValidZip(zipPath))
            {
                throw new InvalidDataException($"来源 {usedSource} 下载的文件不是有效的 zip 压缩包。");
            }

            ExtractZip(zipPath, extractDir);
            var contentDir = FindContentDirectory(extractDir);
            if (contentDir == null ||
                Directory.GetFiles(contentDir, "*.md", SearchOption.TopDirectoryOnly).Length == 0)
            {
                throw new InvalidDataException("压缩包中未找到任何 Markdown 内容文件。");
            }

            await File.WriteAllTextAsync(Path.Combine(contentDir, VersionFileName), tag);

            SwapContentDirectory(pluginRoot, contentDir, finalDir);
            TryDelete(stagingRoot);

            DebugLog($"汉服内容已更新到 {tag}。");
        }
        catch (Exception ex)
        {
            // 下载/校验/解压/替换任一环节失败，都不能触碰现有 Markdown 目录，
            // 页面继续从本地缓存源加载（此时内容可能已被篡改，防篡改可能失效）。
            var cacheDir = Path.Combine(ResolvePluginRoot(), MarkdownFolderName);
            var hasCache = Directory.Exists(cacheDir) &&
                Directory.GetFiles(cacheDir, "*.md", SearchOption.TopDirectoryOnly).Length > 0;
            DebugLog(hasCache
                ? $"汉服内容拉取失败，已回退到本地缓存（防篡改可能失效）：{ex.Message}"
                : $"汉服内容拉取失败，且本地无缓存：{ex.Message}");
        }
        finally
        {
            lock (UpdateLock)
            {
                _isUpdating = false;
            }
        }
    }

    private static async Task<(string? tag, string? downloadUrl)> QueryLatestReleaseAsync()
    {
        using var response = await HttpClient.GetAsync(
            LatestReleaseApiUrl,
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        string? tag = null;
        if (root.TryGetProperty("tag_name", out var tagElement) &&
            tagElement.ValueKind == JsonValueKind.String)
        {
            tag = tagElement.GetString();
        }

        if (root.TryGetProperty("assets", out var assets) &&
            assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.TryGetProperty("name", out var name) &&
                    string.Equals(name.GetString(), ZipAssetName, StringComparison.OrdinalIgnoreCase) &&
                    asset.TryGetProperty("browser_download_url", out var url) &&
                    url.ValueKind == JsonValueKind.String)
                {
                    return (tag, url.GetString());
                }
            }
        }

        return (tag, null);
    }

    /// <summary>
    /// 按 GitHub 直链 → 各高速镜像的顺序依次尝试下载；返回实际成功的来源描述。
    /// 「成功」的定义不仅是 HTTP 200，还要求落地文件非空且为合法 zip，否则切换下一来源。
    /// </summary>
    private static async Task<string> DownloadWithMirrorsAsync(string originalUrl, string destinationPath)
    {
        var sources = new List<(string name, string url)> { ("GitHub", originalUrl) };
        foreach (var prefix in DownloadMirrorPrefixes)
        {
            sources.Add((new Uri(prefix).Host, prefix + originalUrl));
        }

        var errors = new List<string>();
        foreach (var (name, url) in sources)
        {
            try
            {
                await DownloadFileAsync(url, destinationPath);
                if (IsValidZip(destinationPath))
                {
                    DebugLog($"从来源 {name} 下载成功。");
                    return name;
                }
                errors.Add($"{name}：返回内容不是 zip");
            }
            catch (Exception ex)
            {
                errors.Add($"{name}：{ex.Message}");
                TryDeleteFile(destinationPath);
            }
        }

        throw new InvalidOperationException(
            "所有下载来源均失败：" + string.Join("；", errors));
    }

    private static async Task DownloadFileAsync(string url, string destinationPath)
    {
        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var response = await HttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);
        await contentStream.CopyToAsync(fileStream);

        // 落地校验：文件必须真实存在且非空，避免把错误页当成压缩包
        var info = new FileInfo(destinationPath);
        if (!info.Exists || info.Length == 0)
        {
            throw new InvalidDataException("压缩包下载完成但文件不存在或大小为 0。");
        }
    }

    private static bool IsValidZip(string zipPath)
    {
        try
        {
            using var fs = File.OpenRead(zipPath);
            Span<byte> header = stackalloc byte[2];
            if (fs.Read(header) < 2)
            {
                return false;
            }
            return header[0] == (byte)'P' && header[1] == (byte)'K';
        }
        catch
        {
            return false;
        }
    }

    private static void ExtractZip(string zipPath, string targetDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var fullTarget = Path.GetFullPath(targetDir)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        foreach (var entry in archive.Entries)
        {
            // 目录条目没有文件名，跳过
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var relativePath = entry.FullName
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var destinationPath = Path.GetFullPath(Path.Combine(targetDir, relativePath));

            // 防止 zip slip 路径穿越
            if (!destinationPath.StartsWith(fullTarget, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"压缩包包含非法路径：{entry.FullName}");
            }

            var parent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }

    /// <summary>
    /// 压缩包内容可能平铺在根目录，也可能整体包在单层文件夹中；定位真正含 .md 的目录。
    /// </summary>
    private static string? FindContentDirectory(string extractDir)
    {
        if (Directory.GetFiles(extractDir, "*.md", SearchOption.TopDirectoryOnly).Length > 0)
        {
            return extractDir;
        }

        foreach (var sub in Directory.GetDirectories(extractDir))
        {
            if (Directory.GetFiles(sub, "*.md", SearchOption.TopDirectoryOnly).Length > 0)
            {
                return sub;
            }
        }

        return null;
    }

    /// <summary>用暂存目录整体替换正式 Markdown 目录；旧目录先备份，替换成功再删除，失败则回滚。</summary>
    private static void SwapContentDirectory(string pluginRoot, string contentDir, string finalDir)
    {
        var backupDir = Path.Combine(pluginRoot, BackupFolderName);
        TryDelete(backupDir);

        var hadOld = Directory.Exists(finalDir);
        if (hadOld)
        {
            Directory.Move(finalDir, backupDir);
        }

        try
        {
            Directory.Move(contentDir, finalDir);
        }
        catch
        {
            if (hadOld && !Directory.Exists(finalDir))
            {
                try
                {
                    Directory.Move(backupDir, finalDir);
                }
                catch
                {
                    // 回滚失败也只能保留日志，不应吞掉原始异常
                }
            }
            throw;
        }

        if (hadOld)
        {
            TryDelete(backupDir);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception ex)
        {
            DebugLog($"清理目录失败：{path}，{ex.Message}");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            DebugLog($"清理文件失败：{path}，{ex.Message}");
        }
    }

    private static void DebugLog(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[HanfuMarkdownUpdater] {message}");
        Console.WriteLine($"[HanfuMarkdownUpdater] {message}");
    }
}
