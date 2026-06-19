# ClassIsland Plugin Packaging Script
# Usage: Run after Release build

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($ProjectRoot)) {
    $ProjectRoot = "C:\Users\Administrator\RiderProjects\AdvancedTimeIsland"
}
$OutputDir = Join-Path $ProjectRoot "bin\Release\net8.0-windows"
$PackageName = "AdvancedTimeIsland.cipx"
$PackagePath = Join-Path $OutputDir $PackageName

Write-Host "Packaging AdvancedTimeIsland plugin..."
Write-Host "Output directory: $OutputDir"
Write-Host "Package file: $PackagePath"

# Check output directory
if (-not (Test-Path $OutputDir)) {
    Write-Error "Error: Output directory not found. Please run Release build first."
    exit 1
}

# Check manifest.yml
$manifestPath = Join-Path $OutputDir "manifest.yml"
if (-not (Test-Path $manifestPath)) {
    Write-Error "Error: manifest.yml not found."
    exit 1
}

# Create temp directory
$tempDir = Join-Path $env:TEMP "AdvancedTimeIsland_Package_$(Get-Date -Format 'yyyyMMddHHmmss')"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    # Copy all files to temp directory
    Write-Host "Copying files..."
    Copy-Item "$OutputDir\*" $tempDir -Recurse -Force

    # Remove pdb debug files (optional)
    Remove-Item "$tempDir\*.pdb" -Force -ErrorAction SilentlyContinue

    # Create .cipx package (actually ZIP format)
    Write-Host "Creating .cipx package..."

    # Use .NET compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    # Remove existing package file
    if (Test-Path $PackagePath) {
        Remove-Item $PackagePath -Force
    }

    # Create ZIP and rename to .cipx
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $PackagePath)

    Write-Host "Packaging successful!"
    Write-Host "Package location: $PackagePath"
    Write-Host ""
    Write-Host "How to use:"
    Write-Host "1. Copy AdvancedTimeIsland.cipx to ClassIsland plugins directory"
    Write-Host "   Usually: %APPDATA%\ClassIsland\Plugins\"
    Write-Host "2. Restart ClassIsland"
}
finally {
    # Clean up temp directory
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
