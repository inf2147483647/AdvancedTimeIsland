# ClassIsland Plugin Packaging Script
# Usage: Run after Release build
# Supports: Windows x64

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($ProjectRoot)) {
    $ProjectRoot = "C:\Users\Administrator\RiderProjects\AdvancedTimeIsland"
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

function Build-Platform {
    param(
        [string]$RuntimeIdentifier,
        [string]$PackageName
    )
    
    Write-Host "`n========================================"
    Write-Host "Building for $RuntimeIdentifier..."
    Write-Host "========================================"
    
    $OutputDir = Join-Path $ProjectRoot "bin\Release\net8.0\$RuntimeIdentifier"
    
    Write-Host "`nBuilding project..."
    dotnet build "$ProjectRoot\AdvancedTimeIsland.csproj" --configuration Release --runtime $RuntimeIdentifier
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $RuntimeIdentifier"
        return $false
    }
    
    return Create-CipxPackage -OutputDir $OutputDir -PackageName $PackageName
}

Write-Host "AdvancedTimeIsland Plugin Build Script"
Write-Host "======================================"

$windowsSuccess = Build-Platform -RuntimeIdentifier "win-x64" -PackageName "AdvancedTimeIsland.cipx"

$cipxDir = Join-Path $ProjectRoot "cipx"
if (-not (Test-Path $cipxDir)) {
    New-Item -ItemType Directory -Path $cipxDir -Force | Out-Null
}

if ($windowsSuccess) {
    $windowsPackage = Join-Path $ProjectRoot "bin\Release\net8.0\win-x64\AdvancedTimeIsland.cipx"
    Copy-Item $windowsPackage $cipxDir -Force
    Write-Host "`nCopied Windows package to $cipxDir"
}

Write-Host "`n========================================"
Write-Host "Build Summary"
Write-Host "========================================"
Write-Host "Windows x64: $(if ($windowsSuccess) { 'SUCCESS' } else { 'FAILED' })"

if ($windowsSuccess) {
    Write-Host "`nHow to use:"
    Write-Host "1. Copy the .cipx file to ClassIsland plugins directory"
    Write-Host "   Windows: %APPDATA%\ClassIsland\Plugins\"
    Write-Host "2. Restart ClassIsland"
}

if ($windowsSuccess) { exit 0 } else { exit 1 }
