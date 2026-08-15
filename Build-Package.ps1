# ClassIsland Plugin Packaging Script
# Builds BOTH the .NET 8 compatible version (ClassIsland.PluginSdk 1.7.106.2-dev-v2)
# and the .NET 10 new version (Misha SDK 2.1.1), each with its .cipx package.
# Usage: .\Build-Package.ps1 [-Target both|compat|new]
# Supports: Windows x64

param(
    [ValidateSet("both", "compat", "new")]
    [string]$Target = "both"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($ProjectRoot)) {
    $ProjectRoot = "C:\Users\Administrator\RiderProjects\AdvancedTimeIsland"
}

# 清理残留的 MSBuild/编译器服务器句柄，避免其锁定 obj\Debug 输出 dll 导致后续默认构建失败
dotnet build-server shutdown 2>$null

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
    
    $OutputDir = Join-Path $ProjectRoot "bin\Release\$TargetFramework\win-x64"
    
    # 单值 TargetFramework 属性切换 TFM 时，资产文件不会自动包含新 TFM，
    # 必须先按目标 TFM + runtime 显式还原，再以 --no-restore 构建。
    Write-Host "`nRestoring packages ($TargetFramework / win-x64)..."
    dotnet restore "$ProjectRoot\AdvancedTimeIsland.csproj" -p:TargetFramework=$TargetFramework --runtime win-x64
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore failed for $Label ($TargetFramework)"
        return $false
    }
    
    Write-Host "`nBuilding project (Release, win-x64)..."
    dotnet build "$ProjectRoot\AdvancedTimeIsland.csproj" --configuration Release --runtime win-x64 -p:TargetFramework=$TargetFramework --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $Label ($TargetFramework)"
        return $false
    }
    
    return Create-CipxPackage -OutputDir $OutputDir -PackageName "$PackageBaseName.cipx"
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
    # 释放本次构建启动的 MSBuild/编译器服务器，避免残留锁影响后续默认构建
    dotnet build-server shutdown 2>$null
}
