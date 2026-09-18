# Sync-OfflinePackages.ps1
# Synchronize the plugin's NuGet offline source (repo-root 'packages\') so the
# project can be built & packaged with no internet connection.
#
# Usage: .\Sync-OfflinePackages.ps1
#
# Why:
#   - The CI2 main project is multi-targeted (net8.0;net10.0) and selects a
#     different ClassIsland SDK per TFM:
#         net8.0  -> ClassIsland.PluginSdk / Core 2.0.1.1
#         net10.0 -> ClassIsland.PluginSdk / Core 2.1.1.1
#     The WPF build uses the ClassIsland 1.x SDK 1.7.0.1.
#   - This script restores EACH target separately, snapshots its complete
#     dependency graph from obj\project.assets.json, merges all three graphs,
#     and copies the matching .nupkg files from the local global cache into
#     'packages\'. 'nuget.config' then prefers this offline source.
#
# When to re-run:
#   - After adding / upgrading any NuGet dependency.
#   - After switching the ClassIsland SDK version or a target framework.
#   (Alternatively, drop the new .nupkg into 'packages\' manually.)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($ProjectRoot)) {
    $ProjectRoot = "C:\Users\seewo\RiderProjects\AdvancedTimeIsland"
}

$Dest = Join-Path $ProjectRoot "packages"
$GlobalCache = "C:\Users\seewo\.nuget\packages"
if (-not (Test-Path $GlobalCache)) {
    $GlobalCache = Join-Path $HOME ".nuget\packages"
}

New-Item -ItemType Directory -Path $Dest -Force | Out-Null

Write-Host "`n=== Sync-OfflinePackages ==="
Write-Host "Offline source : $Dest"
Write-Host "Global cache   : $GlobalCache"
Write-Host ""

# Also self-cleaning: remove any stray nupkg from previous runs that are NOT in
# a supported graph is intentionally NOT done; a superset is harmless and avoids
# deleting a package another machine might still need.

$script:pkgMap = @{}
function Sync-Target {
    param([string]$Project, [string]$TfmArg, [string]$RidArg, [string]$Assets, [string]$Label)
    Write-Host ""
    Write-Host "Restore [$Label] ..."
    $restoreArgs = @($Project)
    if ($TfmArg) { $restoreArgs += $TfmArg }
    if ($RidArg) { $restoreArgs += $RidArg }
    dotnet restore @restoreArgs 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Restore failed for [$Label] - internet may be required once. Reusing what is already cached."
    }
    if (Test-Path $Assets) {
        $a = Get-Content -Raw -Encoding UTF8 $Assets | ConvertFrom-Json
        foreach ($lib in $a.libraries.PSObject.Properties) {
            if ($lib.Name -match '^(.+)/([^/]+)$') {
                $script:pkgMap["$($matches[1])/$($matches[2])"] = $true
            }
        }
    }
}

# 1) CI2 net8.0 compat  - ClassIsland SDK 2.0.1.1
Sync-Target (Join-Path $ProjectRoot "AdvancedTimeIsland.csproj") "-p:TargetFramework=net8.0" "-r win-x64" (Join-Path $ProjectRoot "obj\project.assets.json") "net8-compat"
# 2) CI2 net10.0        - ClassIsland SDK 2.1.1.1
Sync-Target (Join-Path $ProjectRoot "AdvancedTimeIsland.csproj") "-p:TargetFramework=net10.0" "-r win-x64" (Join-Path $ProjectRoot "obj\project.assets.json") "net10"
# 3) WPF                - ClassIsland 1.x SDK 1.7.0.1
Sync-Target (Join-Path $ProjectRoot "AdvancedTimeIslandWPF\AdvancedTimeIslandWPF.csproj") $null $null (Join-Path $ProjectRoot "AdvancedTimeIslandWPF\obj\project.assets.json") "wpf"
Write-Host ""

Write-Host "Distinct package refs : $($pkgMap.Count)"
$copied = 0; $missing = @()
foreach ($key in $pkgMap.Keys) {
    $parts = $key.Split('/')
    $id = $parts[0]; $ver = $parts[1]
    $src = Join-Path $GlobalCache (Join-Path $id (Join-Path $ver "$id.$ver.nupkg"))
    if (-not (Test-Path $src)) { $missing += $key; continue }
    Copy-Item $src (Join-Path $Dest "$id.$ver.nupkg") -Force
    $copied++
}
Write-Host "Copied to offline source: $copied"
Write-Host "Offline source total    : $((Get-ChildItem $Dest -Filter *.nupkg).Count)"

if ($missing.Count -gt 0) {
    Write-Host "`nMissing from global cache (build online once, then re-run):"
    $missing | Sort-Object | ForEach-Object { Write-Host "  - $_" }
    Write-Host ""
}

Write-Host "Done. The repo-root nuget.config now uses this offline source first."