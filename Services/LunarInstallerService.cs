using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AdvancedTimeIsland.Services;

public class LunarInstallerService
{
    private const string LunarAssemblyName = "lunar-csharp";
    private bool _isInstalled;

    public bool IsInstalled => _isInstalled;

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
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> TryInstallAsync()
    {
        if (_isInstalled)
            return true;

        try
        {
            var result = await ExecuteNuGetInstallAsync().ConfigureAwait(false);
            if (result)
            {
                _isInstalled = CheckLunarInstalled();
            }
            return _isInstalled;
        }
        catch
        {
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
                return false;
            }

            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"add \"{projectFile}\" package lunar-csharp";
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(projectFile) ?? pluginDir;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            await process.WaitForExitAsync().ConfigureAwait(false);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string? FindProjectFile(string directory)
    {
        var files = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (files.Length > 0)
            return files[0];

        var parentDir = Directory.GetParent(directory);
        if (parentDir != null)
            return FindProjectFile(parentDir.FullName);

        return null;
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