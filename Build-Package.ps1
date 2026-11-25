# ClassIsland Plugin Packaging Script
# Builds TWO Avalonia versions of the plugin:
#   1. .NET 8 compatible version (ClassIsland.PluginSdk 2.0.1.1) for CI2
#   2. .NET 10 new version (Misha SDK 2.1.1) for CI2
# 其中 Avalonia 两版的 manifest version 按目标框架区分：
#   net8.0  -> 2.0.x.x（取自 manifest.yml）
#   net10.0 -> 2.2.x.x（2.2 + manifest.yml 的后两段）
# Usage: .\Build-Package.ps1 [-Target both|compat|new] [-NoServerShutdown]
# Supports: Windows x64
#
# 打包结束后会在 cipx\checksum.md 生成本次产出插件包的 MD5 校验值，
# 每个包一行，格式为 ClassIsland 可识别的 CLASSISLAND_PKG_MD5 注释。
#
# 性能优化说明：
#   - 去掉了脚本开头的 build-server shutdown（原本前后各一次，共两次）；
#     只在 finally 中最后做一次，避免 compat/new 连续构建间冷启动 VBCSCompiler
#   - csproj 内 CreateCipxPackage Target 默认禁用（需 CreateCipxPackageEnabled=true），
#     只在本脚本一处打包，避免每次构建 ZIP 压缩两遍
#   - 两个变体共用 Create-CipxPackage 函数，输出文件名规则一致
#   - 对 assets 中已存在的 TFM+RID 目标跳过 restore（通常节省 1-2 秒）
#   - 支持 -NoServerShutdown 开关，开发中频繁跑构建时彻底跳过 build-server shutdown

param(
    [ValidateSet("both", "compat", "new")]
    [string]$Target = "both",
    [switch]$NoServerShutdown
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($ProjectRoot)) {
    $ProjectRoot = "C:\Users\Administrator\RiderProjects\AdvancedTimeIsland"
}

# 耗时统计：各阶段登记到 StageTimings，脚本总计由 TotalStopwatch 记录
$script:StageTimings = New-Object System.Collections.Generic.List[object]
$script:TotalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

function Format-Elapsed {
    param([TimeSpan]$Elapsed)

    if ($Elapsed.TotalMinutes -ge 1) {
        return ('{0}m{1:00}s' -f [int][math]::Floor($Elapsed.TotalMinutes), $Elapsed.Seconds)
    }
    return ('{0:N2}s' -f $Elapsed.TotalSeconds)
}

function Add-StageTime {
    # 停止计时器，打印本阶段耗时并登记到汇总列表
    param(
        [string]$Name,
        [System.Diagnostics.Stopwatch]$Timer
    )

    if ($Timer.IsRunning) { $Timer.Stop() }
    $script:StageTimings.Add([pscustomobject]@{ Name = $Name; Elapsed = $Timer.Elapsed })
    Write-Host ("    [time] {0}: {1}" -f $Name, (Format-Elapsed -Elapsed $Timer.Elapsed))
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

function Test-PackageDependencies {
    # 校验 deps.json 里登记的所有运行时资产（runtime / native）都真的在包目录里。
    # 背景：ClassIsland 用 AssemblyDependencyResolver 按插件 deps.json 解析依赖，
    # 缺 DLL 会让插件在 GetExportedTypes() 时抛 FileNotFoundException 并整体加载失败
    # （参见 2.0.5.3 的 Vortice.DirectX 事故）。此处把问题挡在打包阶段。
    param(
        [string]$PackageDir
    )

    $depsPath = Join-Path $PackageDir "AdvancedTimeIsland.deps.json"
    if (-not (Test-Path $depsPath)) {
        Write-Warning "Dependency check skipped: AdvancedTimeIsland.deps.json not found."
        return $true
    }

    try {
        $deps = Get-Content -Raw $depsPath | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        Write-Error "Dependency check failed: cannot parse AdvancedTimeIsland.deps.json - $($_.Exception.Message)"
        return $false
    }

    $checked = 0
    $missing = New-Object System.Collections.Generic.List[string]
    if (-not $deps.targets) {
        Write-Error "Dependency check failed: no 'targets' section in AdvancedTimeIsland.deps.json"
        return $false
    }

    foreach ($target in $deps.targets.PSObject.Properties) {
        foreach ($lib in $target.Value.PSObject.Properties) {
            foreach ($assetSection in @("runtime", "native")) {
                $section = $lib.Value.$assetSection
                if (-not $section) { continue }
                foreach ($asset in $section.PSObject.Properties) {
                    $assetPath = $asset.Name
                    if ($assetPath -eq "_._") { continue }
                    $fileName = ($assetPath -split '[\\/]')[-1]
                    if ([System.IO.Path]::GetExtension($fileName) -notin @(".dll", ".exe")) { continue }

                    $checked++
                    # 输出目录为扁平结构；若资产在 runtimes\ 等子目录则按相对路径检查
                    $flatPath = Join-Path $PackageDir $fileName
                    $relativePath = Join-Path $PackageDir ($assetPath -replace '/', '\')
                    if (-not (Test-Path $flatPath) -and -not (Test-Path $relativePath)) {
                        $missing.Add("$($lib.Name) -> $assetPath")
                    }
                }
            }
        }
    }

    if ($missing.Count -gt 0) {
        Write-Error "Dependency check FAILED: $($missing.Count) runtime asset(s) declared in AdvancedTimeIsland.deps.json are missing from the package. ClassIsland's AssemblyDependencyResolver will not find them and the plugin will fail to load:"
        foreach ($m in $missing) { Write-Error "  missing: $m" }
        return $false
    }

    Write-Host "Dependency check passed: $checked runtime asset(s) declared in deps.json are packaged."
    return $true
}

function New-ZipPackage {
    # 以最高压缩等级（deflate level 9）生成 .cipx。
    # 背景：Windows PowerShell 5.1 跑在 .NET Framework 上，其 CompressionLevel 枚举只有
    # Fastest / Optimal / NoCompression，没有 SmallestSize，所以 CreateFromDirectory 最高只能到
    # Optimal。此处改用一个由 dotnet 运行的临时 file-based C# 程序调用 SmallestSize 打包；
    # 若该方式不可用（旧 SDK / 运行失败）则回退到 .NET Framework 的 Optimal（体积约 +1.7%）。
    # 输出先写临时文件再改名，保证打包目录里不会出现半截 zip。
    param(
        [string]$SourceDir,
        [string]$DestinationFile
    )

    $tempZip = "$DestinationFile.tmp"
    if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

    # 固定路径的辅助程序目录：.NET 的 file-based app 会按文件路径缓存编译产物，
    # 路径固定后第一次打包需编译（约 20 秒），后续打包只需约 3 秒；
    # 若用随机目录则每次都要重新编译，得不偿失。
    $helperDir = Join-Path $env:TEMP "AdvancedTimeIsland.ZipHelper"
    New-Item -ItemType Directory -Path $helperDir -Force | Out-Null
    $helperPath = Join-Path $helperDir "ziphelper.cs"
    $helperSource = @'
using System;
using System.IO;
using System.IO.Compression;

// ClassIsland 插件包（标准 zip）打包器：使用 SmallestSize（deflate level 9）压缩。
var source = args[0];
var destination = args[1];
if (File.Exists(destination)) File.Delete(destination);
ZipFile.CreateFromDirectory(source, destination, CompressionLevel.SmallestSize, false);
'@
    if (-not (Test-Path $helperPath) -or ([System.IO.File]::ReadAllText($helperPath) -ne $helperSource)) {
        [System.IO.File]::WriteAllText($helperPath, $helperSource, (New-Object System.Text.UTF8Encoding $false))
    }

    $pressed = $false
    try {
        Push-Location $helperDir
        $output = dotnet run --file $helperPath -- $SourceDir $tempZip 2>&1
        $pressed = ($LASTEXITCODE -eq 0) -and (Test-Path $tempZip)
        if (-not $pressed) {
            Write-Warning "SmallestSize zip helper failed: $($output | Out-String)"
        }
    }
    catch {
        Write-Warning "SmallestSize zip helper failed: $($_.Exception.Message)"
    }
    finally {
        Pop-Location
    }

    if (-not $pressed) {
        Write-Host "Falling back to Optimal compression (no SmallestSize support available)."
        if (Test-Path $tempZip) { Remove-Item $tempZip -Force }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory($SourceDir, $tempZip)
    }

    if (-not (Test-Path $tempZip)) {
        return $false
    }

    if (Test-Path $DestinationFile) { Remove-Item $DestinationFile -Force }
    Move-Item $tempZip $DestinationFile -Force
    return $true
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

    $packageTimer = [System.Diagnostics.Stopwatch]::StartNew()
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

        if (-not (Test-PackageDependencies -PackageDir $tempDir)) {
            Write-Error "Error: package dependency check failed. The .cipx was NOT created."
            return $false
        }

        Write-Host "Creating .cipx package..."

        if (-not (New-ZipPackage -SourceDir $tempDir -DestinationFile $PackagePath)) {
            Write-Error "Error: failed to create the .cipx archive."
            return $false
        }

        Write-Host "Packaging successful!"
        Write-Host "Package location: $PackagePath"
        return $true
    }
    finally {
        if (Test-Path $tempDir) {
            Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        Add-StageTime "package $PackageName" $packageTimer
    }
}

function Set-ManifestVersion {
    # Avalonia 版两个包共用一份 manifest.yml（版本号也共用以保持单一版本来源），
    # 但对外发布的版本号要求按目标框架区分：
    #   net8.0  -> 保持 manifest.yml 中的版本（2.0.x.x，ClassIsland 2.0.x / FA2 宿主）
    #   net10.0 -> 主版本固定为 2.2，保留后两段（2.2.x.x，新版 ClassIsland / FA3 宿主）
    # 仅在构建产物目录里改写，源文件 manifest.yml 不动。
    param(
        [string]$OutputDir,
        [string]$TargetFramework
    )

    $manifestPath = Join-Path $OutputDir "manifest.yml"
    if (-not (Test-Path $manifestPath)) {
        Write-Warning "Version rewrite skipped: manifest.yml not found in $OutputDir"
        return
    }

    $text = [System.IO.File]::ReadAllText($manifestPath)
    $match = [regex]::Match($text, '(?m)^version\s*:\s*(\d+)\.(\d+)\.(\d+)\.(\d+)\s*$')
    if (-not $match.Success) {
        Write-Warning "Version rewrite skipped: no 4-part version line in $manifestPath"
        return
    }

    $baseVersion = "$($match.Groups[1].Value).$($match.Groups[2].Value).$($match.Groups[3].Value).$($match.Groups[4].Value)"

    if ($TargetFramework -eq "net10.0") {
        $newVersion = "2.2.$($match.Groups[3].Value).$($match.Groups[4].Value)"
    }
    else {
        # net8 包必须落在 2.0.x.x；源 manifest 若已偏离则提示（不改写，避免擅自变更版本号）
        if (-not $baseVersion.StartsWith("2.0.")) {
            Write-Warning "net8 version '$baseVersion' does not start with 2.0; expected 2.0.x.x."
        }
        Write-Host "Manifest version ($TargetFramework): $baseVersion"
        return
    }

    $updated = $text.Substring(0, $match.Index) + "version: $newVersion" + $text.Substring($match.Index + $match.Length)

    # 原子写入：先写同目录临时文件，再替换目标文件
    $tempPath = "$manifestPath.tmp"
    [System.IO.File]::WriteAllText($tempPath, $updated, (New-Object System.Text.UTF8Encoding $false))
    Move-Item $tempPath $manifestPath -Force

    Write-Host "Manifest version ($TargetFramework): $baseVersion -> $newVersion"
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
    $variantTimer = [System.Diagnostics.Stopwatch]::StartNew()

    if ((Test-RestoreNeeded -ProjectFile $ProjectFile -TargetFramework $TargetFramework -RuntimeIdentifier "win-x64")) {
        Write-Host "`nRestoring packages ($TargetFramework / win-x64)..."
        $restoreTimer = [System.Diagnostics.Stopwatch]::StartNew()
        dotnet restore $ProjectFile -p:TargetFramework=$TargetFramework --runtime win-x64
        Add-StageTime "restore ($TargetFramework)" $restoreTimer
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Restore failed for $Label ($TargetFramework)"
            return $false
        }
    }
    else {
        Write-Host "`nSkipping restore: assets already contain target '$TargetFramework/win-x64'."
    }

    Write-Host "`nBuilding project (Release, win-x64)..."
    $buildTimer = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet build $ProjectFile --configuration Release --runtime win-x64 -p:TargetFramework=$TargetFramework --no-restore
    Add-StageTime "build ($TargetFramework)" $buildTimer

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $Label ($TargetFramework)"
        return $false
    }

    # 按目标框架改写产物目录里的 manifest.yml 版本（net8 保持 2.0.x.x，net10 为 2.2.x.x）
    $manifestTimer = [System.Diagnostics.Stopwatch]::StartNew()
    Set-ManifestVersion -OutputDir $OutputDir -TargetFramework $TargetFramework
    Add-StageTime "manifest version ($TargetFramework)" $manifestTimer


    # Copy FloatSchedule child exe into plugin output dir -> Create-CipxPackage (full copy) picks it up
    if (-not (Publish-FloatExe -TargetFramework $TargetFramework -OutputDir $OutputDir)) {
        Write-Error "FloatSchedule child exe publish failed"
        return $false
    }

    $packed = Create-CipxPackage -OutputDir $OutputDir -PackageName "$PackageBaseName.cipx"
    Add-StageTime "variant $Label (total)" $variantTimer
    return $packed
}

function Publish-FloatExe {
    param(
        [string]$TargetFramework,
        [string]$OutputDir
    )

    # Schedule floating-window "independent process mode" child exe.
    # Framework-dependent + single file: output is ONE exe only; Avalonia/Skia runtime DLLs are
    # resolved by the child process from the ClassIsland install directory at runtime (never packaged).
    # Each cipx carries the TFM matching its host Avalonia generation:
    #   net8.0  -> net8.0-windows  (Avalonia 11.3.x)
    #   net10.0 -> net10.0-windows (Avalonia 12.1.x)
    $childTfm = if ($TargetFramework -eq "net10.0") { "net10.0-windows" } else { "net8.0-windows" }
    $proj = Join-Path $ProjectRoot "AdvancedTimeIslandFloatSchedule\AdvancedTimeIslandFloatSchedule.csproj"
    $pubDir = Join-Path $ProjectRoot "AdvancedTimeIslandFloatSchedule\bin\publish\$childTfm"

    Write-Host "`nPublishing FloatSchedule child exe ($childTfm, framework-dependent single-file)..."
    $publishTimer = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet publish $proj -c Release -f $childTfm -r win-x64 --self-contained false `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $pubDir
    Add-StageTime "publish FloatSchedule ($childTfm)" $publishTimer
    if ($LASTEXITCODE -ne 0) {
        Write-Error "FloatSchedule publish failed for $childTfm"
        return $false
    }
    $exe = Join-Path $pubDir "AdvancedTimeIslandFloatSchedule.exe"
    if (-not (Test-Path $exe)) {
        Write-Error "FloatSchedule exe not found: $exe"
        return $false
    }

    # 子进程 exe 为减小包体已把 Vortice / SharpGen / System.Text.Json 等从单文件包里排除
    # （见 AdvancedTimeIslandFloatSchedule.csproj 的 ExcludeSharedDependenciesFromSingleFileBundle），
    # 运行时从 exe 所在目录（即插件包目录）解析它们。此处校验这些 DLL 确实已在主插件输出目录里，
    # 且程序集名称 / 版本一致（补丁版本差异不影响加载，故按 AssemblyName 而非文件哈希比对），
    # 避免出现"子进程启动即缺 DLL / 版本不兼容"。
    $sharedDeps = Get-ChildItem $pubDir -File | Where-Object {
        $_.Name -ne "AdvancedTimeIslandFloatSchedule.exe" -and
        $_.Name -ne "AdvancedTimeIslandFloatSchedule.dll" -and
        $_.Extension -eq ".dll"
    }
    $missingDeps = New-Object System.Collections.Generic.List[string]
    foreach ($dep in $sharedDeps) {
        $inPackage = Join-Path $OutputDir $dep.Name
        if (-not (Test-Path $inPackage)) {
            $missingDeps.Add("$($dep.Name) (not in plugin output)")
            continue
        }

        $childAsm = [System.Reflection.AssemblyName]::GetAssemblyName($dep.FullName)
        $packageAsm = [System.Reflection.AssemblyName]::GetAssemblyName($inPackage)
        if ($childAsm.Version -ne $packageAsm.Version) {
            $missingDeps.Add("$($dep.Name) (assembly version mismatch: package $($packageAsm.Version) vs child $($childAsm.Version))")
        }
    }
    if ($missingDeps.Count -gt 0) {
        Write-Error "FloatSchedule shared dependency check FAILED: the child exe no longer bundles these assemblies, so they must ship in the plugin package with a compatible version:"
        foreach ($m in $missingDeps) { Write-Error "  missing/mismatch: $m" }
        return $false
    }
    Write-Host "FloatSchedule shared dependency check passed: $($sharedDeps.Count) extern assembly(ies) provided by the plugin package."

    Copy-Item $exe (Join-Path $OutputDir "AdvancedTimeIslandFloatSchedule.exe") -Force
    $sizeKB = [math]::Round((Get-Item $exe).Length / 1KB)
    Write-Host "FloatSchedule exe: $sizeKB KB -> $OutputDir"
    return $true
}


function New-ChecksumFile {
    param(
        [string]$OutputDir,
        [string[]]$PackageNames
    )

    $lines = @()
    foreach ($name in $PackageNames) {
        $packagePath = Join-Path $OutputDir $name
        if (-not (Test-Path $packagePath)) {
            Write-Host "Skipping checksum: package not found - $name"
            continue
        }
        $hash = (Get-FileHash $packagePath -Algorithm MD5).Hash
        $lines += '<!-- CLASSISLAND_PKG_MD5 {"' + $name + '": "' + $hash + '"} -->'
    }

    if ($lines.Count -eq 0) {
        Write-Host "`nNo package generated, checksum file skipped."
        return
    }

    $content = ($lines -join "`r`n") + "`r`n"
    $checksumPath = Join-Path $OutputDir "checksum.md"

    # 用无 BOM 的 UTF8 写出，避免发布日志粘贴时出现多余字符
    [System.IO.File]::WriteAllText($checksumPath, $content, (New-Object System.Text.UTF8Encoding $false))

    Write-Host "`nChecksums:"
    Write-Host $content
    Write-Host "Checksum file: $checksumPath"
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
    $packageNames = @()

    if ($Target -eq "both" -or $Target -eq "compat") {
        $ok = Build-Variant -TargetFramework "net8.0" -Label "compat (.NET 8)" -PackageBaseName "AdvancedTimeIsland-net8.0-compat"
        if ($ok) {
            $src = Join-Path $ProjectRoot "bin\Release\net8.0\win-x64\AdvancedTimeIsland-net8.0-compat.cipx"
            Copy-Item $src (Join-Path $cipxDir "AdvancedTimeIsland-net8.0-compat.cipx") -Force
            $packageNames += "AdvancedTimeIsland-net8.0-compat.cipx"
        }
        $results["net8.0 compat"] = $(if ($ok) { 'SUCCESS' } else { 'FAILED' })
    }

    if ($Target -eq "both" -or $Target -eq "new") {
        $ok = Build-Variant -TargetFramework "net10.0" -Label "new (.NET 10 / Misha)" -PackageBaseName "AdvancedTimeIsland-net10.0"
        if ($ok) {
            $src = Join-Path $ProjectRoot "bin\Release\net10.0\win-x64\AdvancedTimeIsland-net10.0.cipx"
            Copy-Item $src (Join-Path $cipxDir "AdvancedTimeIsland-net10.0.cipx") -Force
            $packageNames += "AdvancedTimeIsland-net10.0.cipx"
        }
        $results["net10.0 new"] = $(if ($ok) { 'SUCCESS' } else { 'FAILED' })
    }

    if ($packageNames.Count -gt 0) {
        $checksumTimer = [System.Diagnostics.Stopwatch]::StartNew()
        New-ChecksumFile -OutputDir $cipxDir -PackageNames $packageNames
        Add-StageTime "checksum" $checksumTimer
    }

    Write-Host "`n========================================"
    Write-Host "Build Summary"
    Write-Host "========================================"
    foreach ($k in $results.Keys) {
        Write-Host "$k : $($results[$k])"
    }

    if ($script:StageTimings.Count -gt 0) {
        Write-Host "`nStage timings"
        Write-Host "----------------------------------------"
        foreach ($t in $script:StageTimings) {
            Write-Host ("{0} : {1}" -f $t.Name, (Format-Elapsed -Elapsed $t.Elapsed))
        }
    }

    Write-Host "`nPackages output: $cipxDir"

    if ($results.Values -contains 'FAILED') { exit 1 } else { exit 0 }
}
finally {
    if (-not $NoServerShutdown) {
        dotnet build-server shutdown 2>$null
    }
    $script:TotalStopwatch.Stop()
    Write-Host ("`nTotal elapsed (incl. build-server shutdown): {0}" -f (Format-Elapsed -Elapsed $script:TotalStopwatch.Elapsed))
}
