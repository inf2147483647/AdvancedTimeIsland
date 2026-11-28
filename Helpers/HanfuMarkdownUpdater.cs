using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 汉服 Markdown 内容热更新。
/// 插件每次启动时在后台线程从 GitHub 最新 release 下载 AdvancedTimeIslandHanfu.zip，
/// 校验后解压到插件目录下的 Markdown 文件夹并逐个文件覆盖，用远端权威内容覆盖本地
/// 可能被篡改的文件（防篡改）。
/// 下载地址使用 releases/latest/download/&lt;asset&gt; 形式，位于 github.com 域，
/// 因此可以整体套用高速镜像前缀，不依赖 api.github.com。
/// 下载回退链：GitHub 直链 → 高速镜像1 → 高速镜像2 → … ；全部失败则保留本地缓存。
/// 全程异步执行、不阻塞 UI。
/// </summary>
public static class HanfuMarkdownUpdater
{
    /// <summary>
    /// GitHub「最新 release」资产直链。该地址会在服务端重定向到最新 release 中的同名资产，
    /// 语义与 api.github.com/repos/.../releases/latest 一致，但域名不会被单独阻断，
    /// 且可以整体套用下面的镜像前缀。
    /// </summary>
    private const string LatestReleaseDownloadUrl =
        "https://github.com/inf2147483647/AdvancedTimeIslandHanfu/releases/latest/download/AdvancedTimeIslandHanfu.zip";

    private const string ZipAssetName = "AdvancedTimeIslandHanfu.zip";
    private const string MarkdownFolderName = "Markdown";
    private const string StagingFolderName = "Markdown.update.tmp";

    /// <summary>
    /// GitHub release 高速下载源回退链：先尝试 GitHub 直链，再依次尝试镜像前缀。
    /// 顺序：GitHub → gh-proxy.com → gh-proxy.at9.net → ghproxy.net → ghfast.top。
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
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AdvancedTimeIsland-Plugin");
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
            var finalDir = Path.Combine(pluginRoot, MarkdownFolderName);

            // 无论本地版本是否最新，每次启动都强制重新下载-解压-覆盖，
            // 用远端权威内容覆盖本地可能被篡改过的文件。
            DebugLog("开始拉取汉服内容。");

            var stagingRoot = Path.Combine(pluginRoot, StagingFolderName);
            TryDelete(stagingRoot);
            var extractDir = Path.Combine(stagingRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(extractDir);

            var zipPath = Path.Combine(stagingRoot, ZipAssetName);
            var usedSource = await DownloadWithMirrorsAsync(LatestReleaseDownloadUrl, zipPath);
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

            await ApplyContentAsync(contentDir, finalDir);
            TryDelete(stagingRoot);

            DebugLog($"汉服内容已更新（来源 {usedSource}）。");
        }
        catch (Exception ex)
        {
            // 下载/校验/解压/覆盖任一环节失败，都尽量不破坏现有 Markdown 目录，
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

    /// <summary>
    /// 用暂存内容更新正式 Markdown 目录。
    /// 这里逐个文件原地覆盖，而不是整体移动目录：整体替换会让 Markdown 目录短暂消失，
    /// 此时正在打开汉服页面会读取失败（内容空白、条目被误判为未开发）。
    /// 覆盖完成后删除远端已不存在的文件，维持「远端内容覆盖本地」的防篡改语义。
    /// </summary>
    private static async Task ApplyContentAsync(string contentDir, string finalDir)
    {
        Directory.CreateDirectory(finalDir);

        var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceFile in Directory.GetFiles(contentDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(contentDir, sourceFile);
            var destinationPath = Path.Combine(finalDir, relativePath);

            var parent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            await MoveWithRetryAsync(sourceFile, destinationPath);
            currentFiles.Add(Path.GetFullPath(destinationPath));
        }

        foreach (var existingFile in Directory.GetFiles(finalDir, "*", SearchOption.AllDirectories))
        {
            if (currentFiles.Contains(Path.GetFullPath(existingFile)))
            {
                continue;
            }
            TryDeleteFile(existingFile);
        }
    }

    /// <summary>
    /// 源文件与目标目录在同一卷上，File.Move(overwrite: true) 是原子替换，
    /// 页面读取只会看到完整的旧内容或新内容，不会读到「文件不存在」或半截内容。
    /// 但目标文件正被页面读取时会抛出占用异常，因此做短暂重试。
    /// </summary>
    private static async Task MoveWithRetryAsync(string sourcePath, string destinationPath)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(sourcePath, destinationPath, overwrite: true);
                return;
            }
            catch (Exception ex) when (
                (ex is IOException || ex is UnauthorizedAccessException) && attempt < maxAttempts)
            {
                await Task.Delay(50);
            }
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
