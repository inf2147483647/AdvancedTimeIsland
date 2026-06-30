using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AdvancedTimeIsland.Services;

public class LunarInstallerService
{
    private const string LunarAssemblyName = "lunar-csharp";
    private bool _isInstalled;

    public bool IsInstalled => _isInstalled;

    public string? LastError { get; private set; }

    public LunarInstallerService()
    {
        _isInstalled = CheckLunarInstalled();
    }

    private bool CheckLunarInstalled()
    {
        try
        {
            var assembly = Assembly.Load(LunarAssemblyName);
            return assembly != null;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            LastError = $"检查安装状态失败: {ex.Message}";
            return false;
        }
    }

    public async Task<bool> TryInstallAsync()
    {
        if (_isInstalled)
            return true;

        LastError = null;

        try
        {
            var result = await ExecuteNuGetInstallAsync().ConfigureAwait(false);
            if (result)
            {
                _isInstalled = CheckLunarInstalled();
            }
            return _isInstalled;
        }
        catch (Exception ex)
        {
            LastError = $"安装过程异常: {ex.Message}";
            return false;
        }
    }

    private async Task<bool> ExecuteNuGetInstallAsync()
    {
        try
        {
            var pluginDir = AppContext.BaseDirectory;
            var projectFile = FindProjectFile(pluginDir);
            
            if (string.IsNullOrEmpty(projectFile))
            {
                LastError = "未找到项目文件(.csproj)";
                return false;
            }

            var workingDir = Path.GetDirectoryName(projectFile) ?? pluginDir;

            using var process = new Process();
            process.StartInfo.FileName = "dotnet.exe";
            process.StartInfo.Arguments = $"add \"{projectFile}\" package lunar-csharp";
            process.StartInfo.WorkingDirectory = workingDir;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (_, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data))
                    outputBuilder.AppendLine(args.Data);
            };

            process.ErrorDataReceived += (_, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data))
                    errorBuilder.AppendLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync().ConfigureAwait(false);

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                LastError = $"安装失败 (ExitCode: {process.ExitCode})\n输出: {output}\n错误: {error}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = $"执行安装命令失败: {ex.Message}";
            return false;
        }
    }

    private string? FindProjectFile(string directory)
    {
        try
        {
            var files = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
                return files[0];

            var parentDir = Directory.GetParent(directory);
            if (parentDir != null)
                return FindProjectFile(parentDir.FullName);

            return null;
        }
        catch (Exception ex)
        {
            LastError = $"查找项目文件失败: {ex.Message}";
            return null;
        }
    }

    public void RefreshStatus()
    {
        _isInstalled = CheckLunarInstalled();
    }

    public static async void AutoInstallAsync()
    {
        var service = new LunarInstallerService();
        if (!service.IsInstalled)
        {
            await service.TryInstallAsync().ConfigureAwait(false);
        }
    }
}


