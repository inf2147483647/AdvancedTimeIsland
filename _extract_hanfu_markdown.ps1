# 临时脚本：把汉服页面 .cs 中的 verbatim markdown 字符串抽取为 Markdown/<name>.md，
# 并把页面代码中的 var markdown = @"..."; + RenderMarkdown(panel, markdown); 替换为
# RenderMarkdown(panel, LoadMarkdownFile("<name>.md"));。执行后删除本脚本。

$ErrorActionPreference = "Stop"

$root = "c:\Users\seewo\RiderProjects\AdvancedTimeIsland"
$settingsDir = Join-Path $root "Views\Settings"
$mdDir = Join-Path $root "Markdown"

if (-not (Test-Path $mdDir)) { New-Item -ItemType Directory -Path $mdDir -Force | Out-Null }

$names = @(
    "baidiequn", "baidiequn_male", "beizi", "changshan_ao_jiaoling", "changshan_ao_shuling",
    "duanshan_ao_jiaoling", "duanshan_ao_shuling", "mamianqun_baizhe", "mamianqun_cezhe",
    "mamianqunnan", "manzhequn_male", "nan_nv_tong_yong_han_fu_zhi_bei", "qixiong",
    "qixiong_jiaoyuqun", "qixiong_top", "ruqun", "songmo", "tieli_mingstyle",
    "tieli_mingstyle_male", "zhuyao_mingstyle"
)

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

foreach ($name in $names) {
    $csPath = Join-Path $settingsDir "$name.cs"
    if (-not (Test-Path $csPath)) { throw "missing file: $csPath" }

    $rawBytes = [System.IO.File]::ReadAllBytes($csPath)
    $hasBom = ($rawBytes.Length -ge 3 -and $rawBytes[0] -eq 0xEF -and $rawBytes[1] -eq 0xBB -and $rawBytes[2] -eq 0xBF)
    $csEncoding = New-Object System.Text.UTF8Encoding($hasBom)

    $text = [System.IO.File]::ReadAllText($csPath)

    $marker = 'var markdown = @"'
    $mi = $text.IndexOf($marker, [System.StringComparison]::Ordinal)
    if ($mi -lt 0) { throw "no markdown literal in $name" }

    $start = $mi + $marker.Length
    $sb = New-Object System.Text.StringBuilder
    $j = $start
    $end = -1
    while ($j -lt $text.Length) {
        if ($text[$j] -eq '"') {
            if (($j + 1) -lt $text.Length -and $text[$j + 1] -eq '"') {
                [void]$sb.Append('"')
                $j += 2
                continue
            }
            $end = $j
            break
        }
        [void]$sb.Append($text[$j])
        $j++
    }
    if ($end -lt 0) { throw "unterminated verbatim string in $name" }

    $after = $end + 1
    if ($after -ge $text.Length -or $text[$after] -ne ';') { throw "expected ';' after string in $name" }

    $content = $sb.ToString()
    [System.IO.File]::WriteAllText((Join-Path $mdDir "$name.md"), $content, $utf8NoBom)

    $renderMarker = 'RenderMarkdown(panel, markdown);'
    $ri = $text.IndexOf($renderMarker, $after, [System.StringComparison]::Ordinal)
    if ($ri -lt 0) { throw "RenderMarkdown(panel, markdown) not found in $name" }
    $renderEnd = $ri + $renderMarker.Length

    $replacement = 'RenderMarkdown(panel, LoadMarkdownFile("' + $name + '.md"));'
    $newText = $text.Substring(0, $mi) + $replacement + $text.Substring($renderEnd)
    [System.IO.File]::WriteAllText($csPath, $newText, $csEncoding)

    Write-Host ("OK {0}: {1} chars" -f $name, $content.Length)
}
