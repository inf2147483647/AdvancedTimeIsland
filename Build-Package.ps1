# ClassIsland Plugin Packaging Script
# Builds THREE versions of the plugin:
#   1. .NET 8 compatible version (ClassIsland.PluginSdk 1.7.106.2-dev-v2) for CI2
#   2. .NET 10 new version (Misha SDK 2.1.1) for CI2
#   3. WPF version (ClassIsland 1.x, net8.0-windows) in AdvancedTimeIslandWPF\
# Usage: .\Build-Package.ps1 [-Target both|compat|new|wpf] [-NoServerShutdown]
# Supports: Windows x64
#
# 性能优化说明：
#   - 去掉了脚本开头的 build-server shutdown（原本前后各一次，共两次）；
#     只在 finally 中最后做一次，避免 compat/new/wpf 连续构建间冷启动 VBCSCompiler
#   - csproj 内 CreateCipxPackage Target 默认禁用（需 CreateCipxPackageEnabled=true），
#     只在本脚本一处打包，避免每次构建 ZIP 压缩两遍
#   - Build-WpfVariant 改用共享的 Create-CipxPackage 函数，三种变体输出文件名规则一致
#   - 对 assets 中已存在的 TFM+RID 目标跳过 restore（通常节省 1-2 秒）
#   - 支持 -NoServerShutdown 开关，开发中频繁跑构建时彻底跳过 build-server shutdown

param(
    [ValidateSet("both", "compat", "new", "wpf")]
    [string]$Target = "both",
    [switch]$NoServerShutdown
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($ProjectRoot)) {
    $ProjectRoot = "C:\Users\Administrator\RiderProjects\AdvancedTimeIsland"
}

function Test-RestoreNeeded {
    param(
        [string]$ProjectFile,
        [string]$TargetFramework,
        [string]$RuntimeIdentifier
    )

    $assetsPath = Join-Path (Split-Path -Parent $ProjectFile) "obj\project.assets.json"
    if (-not (Test-Path $assetsPath)) { return $true }

    try {
        $assets = Get-Content -Raw $assetsPath | ConvertFrom-Json -ErrorAction Stop
        if (-not $assets -or -not $assets.targets) { return $true }

        if ($RuntimeIdentifier) {
            $needle = "$TargetFramework/$RuntimeIdentifier"
            foreach ($t in $assets.targets.PSObject.Properties) {
                if ($t.Name -ieq $needle) { return $false }
            }
        } else {
            $needle = $TargetFramework + '/'
            foreach ($t in $assets.targets.PSObject.Properties) {
                if ($t.Name -ieq $TargetFramework) { return $false }
                if ($t.Name.StartsWith($needle, [StringComparison]::OrdinalIgnoreCase)) { return $false }
            }
        }
        return $true
    } catch {
        return $true
    }
}

function Create-CipxPackage {
    param(
        [string]$OutputDir,
        [string]$PackageName
    )

    $PackagePath = Join-Path $OutputDir $PackageName

    Write-Host "`nPackaging $PackageName..."
    Write-Host "Output directory: $OutputDir"
    Write-Host "Package file: $PackagePath"

    if (-not (Test-Path $OutputDir)) {
        Write-Error "Error: Output directory not found. Please run Release build first."
        return $false
    }

    $manifestPath = Join-Path $OutputDir "manifest.yml"
    if (-not (Test-Path $manifestPath)) {
        Write-Error "Error: manifest.yml not found."
        return $false
    }

    $tempDir = Join-Path $env:TEMP "AdvancedTimeIsland_Package_$(Get-Date -Format 'yyyyMMddHHmmss')_$(Get-Random)"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    try {
        Write-Host "Copying files..."
        Copy-Item "$OutputDir\*" $tempDir -Recurse -Force

        Remove-Item "$tempDir\*.pdb" -Force -ErrorAction SilentlyContinue
        Remove-Item "$tempDir\*.cipx" -Force -ErrorAction SilentlyContinue
        Remove-Item "$tempDir\*.zip" -Force -ErrorAction SilentlyContinue

        foreach ($ridSub in @("win-x64", "android-arm64")) {
            $ridPath = Join-Path $tempDir $ridSub
            if (Test-Path $ridPath) {
                Remove-Item $ridPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        Write-Host "Creating .cipx package..."

        Add-Type -AssemblyName System.IO.Compression.FileSystem

        if (Test-Path $PackagePath) {
            Remove-Item $PackagePath -Force
        }

        [System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $PackagePath)

        Write-Host "Packaging successful!"
        Write-Host "Package location: $PackagePath"
        return $true
    }
    finally {
        if (Test-Path $tempDir) {
            Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Build-Variant {
    param(
        [string]$TargetFramework,
        [string]$Label,
        [string]$PackageBaseName
    )

    Write-Host "`n========================================"
    Write-Host "Building $Label ($TargetFramework)..."
    Write-Host "========================================"

    $ProjectFile = Join-Path $ProjectRoot "AdvancedTimeIsland.csproj"
    $OutputDir = Join-Path $ProjectRoot "bin\Release\$TargetFramework\win-x64"

    if ((Test-RestoreNeeded -ProjectFile $ProjectFile -TargetFramework $TargetFramework -RuntimeIdentifier "win-x64")) {
        Write-Host "`nRestoring packages ($TargetFramework / win-x64)..."
        dotnet restore $ProjectFile -p:TargetFramework=$TargetFramework --runtime win-x64
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Restore failed for $Label ($TargetFramework)"
            return $false
        }
    }
    else {
        Write-Host "`nSkipping restore: assets already contain target '$TargetFramework/win-x64'."
    }

    Write-Host "`nBuilding project (Release, win-x64)..."
    dotnet build $ProjectFile --configuration Release --runtime win-x64 -p:TargetFramework=$TargetFramework --no-restore

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $Label ($TargetFramework)"
        return $false
    }

    return Create-CipxPackage -OutputDir $OutputDir -PackageName "$PackageBaseName.cipx"
}

function Build-WpfVariant {
    Write-Host "`n========================================"
    Write-Host "Building WPF version (ClassIsland 1.x, net8.0-windows)..."
    Write-Host "========================================"

    $ProjectPath = Join-Path $ProjectRoot "AdvancedTimeIslandWPF\AdvancedTimeIslandWPF.csproj"
    $OutputDir = Join-Path $ProjectRoot "AdvancedTimeIslandWPF\bin\Release\net8.0-windows"

    Write-Host "`nBuilding project (Release)..."
    dotnet build $ProjectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for WPF version"
        return $false
    }

    return Create-CipxPackage -OutputDir $OutputDir -PackageName "AdvancedTimeIsland-wpf.cipx"
}

Write-Host "AdvancedTimeIsland Plugin Build Script"
Write-Host "======================================"

try {
    Write-Host "Target: $Target"

    $cipxDir = Join-Path $ProjectRoot "cipx"
    if (-not (Test-Path $cipxDir)) {
        New-Item -ItemType Directory -Path $cipxDir -Force | Out-Null
    }

    $results = [ordered]@{}

    if ($Target -eq "both" -or $Target -eq "compat") {
        $ok = Build-Variant -TargetFramework "net8.0" -Label "compat (.NET 8)" -PackageBaseName "AdvancedTimeIsland-net8.0-compat"
        if ($ok) {
            $src = Join-Path $ProjectRoot "bin\Release\net8.0\win-x64\AdvancedTimeIsland-net8.0-compat.cipx"
            Copy-Item $src (Join-Path $cipxDir "AdvancedTimeIsland-net8.0-compat.cipx") -Force
        }
        $results["net8.0 compat"] = $(if ($ok) { 'SUCCESS' } else { 'FAILED' })
    }

    if ($Target -eq "both" -or $Target -eq "new") {
        $ok = Build-Variant -TargetFramework "net10.0" -Label "new (.NET 10 / Misha)" -PackageBaseName "AdvancedTimeIsland-net10.0"
        if ($ok) {
            $src = Join-Path $ProjectRoot "bin\Release\net10.0\win-x64\AdvancedTimeIsland-net10.0.cipx"
            Copy-Item $src (Join-Path $cipxDir "AdvancedTimeIsland-net10.0.cipx") -Force
        }
        $results["net10.0 new"] = $(if ($ok) { 'SUCCESS' } else { 'FAILED' })
    }

    if ($Target -eq "both" -or $Target -eq "wpf") {
        $ok = Build-WpfVariant
        if ($ok) {
            $src = Join-Path (Join-Path $ProjectRoot "AdvancedTimeIslandWPF\bin\Release\net8.0-windows") "AdvancedTimeIsland-wpf.cipx"
            if (Test-Path $src) {
                Copy-Item $src (Join-Path $cipxDir "AdvancedTimeIsland-wpf.cipx") -Force
            }
            Write-Host "`nWPF packaging successful!"
            Write-Host "Package location: $(Join-Path $cipxDir 'AdvancedTimeIsland-wpf.cipx')"
        }
        $results["wpf"] = $(if ($ok) { 'SUCCESS' } else { 'FAILED' })
    }

    Write-Host "`n========================================"
    Write-Host "Build Summary"
    Write-Host "========================================"
    foreach ($k in $results.Keys) {
        Write-Host "$k : $($results[$k])"
    }
    Write-Host "`nPackages output: $cipxDir"

    if ($results.Values -contains 'FAILED') { exit 1 } else { exit 0 }
}
finally {
    if (-not $NoServerShutdown) {
        dotnet build-server shutdown 2>$null
    }
}
