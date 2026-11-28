$files = @(
    @{ Path = "c:\Users\Administrator\RiderProjects\AdvancedTimeIsland\AdvancedTimeIslandWPF\Views\Settings\PluginSettingsPage.cs"; Renames = @(
        @{ Doc = "创建经度输入面板（小数/度分秒切换）"; Name = "CreateLongitudePanel" },
        @{ Doc = "创建时间服务器下拉框（包含同步时间显示）"; Name = "CreateNtpServerComboBox" },
        @{ Doc = "创建同步时间周期输入框（包含立即同步按钮和状态提示）"; Name = "CreateNtpSyncIntervalTextBox" }
    )},
    @{ Path = "c:\Users\Administrator\RiderProjects\AdvancedTimeIsland\AdvancedTimeIslandWPF\Views\Settings\AboutPage.cs"; Renames = @(
        @{ Doc = "创建关于内容"; Name = "CreateAboutContent" }
    )}
)

foreach ($file in $files) {
    $f = $file.Path
    $c = [System.IO.File]::ReadAllText($f)
    $o = $c
    foreach ($r in $file.Renames) {
        $old = "/// " + $r.Doc + "`r`n    /// </summary>`r`n    private FrameworkElement Create()"
        $new = "/// " + $r.Doc + "`r`n    /// </summary>`r`n    private FrameworkElement " + $r.Name + "()"
        $c = $c.Replace($old, $new)
        $old2 = "/// " + $r.Doc + "`n    /// </summary>`n    private FrameworkElement Create()"
        $new2 = "/// " + $r.Doc + "`n    /// </summary>`n    private FrameworkElement " + $r.Name + "()"
        $c = $c.Replace($old2, $new2)
    }
    if ($c -ne $o) {
        [System.IO.File]::WriteAllText($f, $c, [System.Text.UTF8Encoding]::new($false))
        Write-Host "updated: $f"
    } else {
        Write-Host "no change: $f"
    }
}

# TimeCalculatorPage: 按调用顺序重命名三个 Create()
$f = "c:\Users\Administrator\RiderProjects\AdvancedTimeIsland\AdvancedTimeIslandWPF\Views\Settings\TimeCalculatorPage.cs"
$c = [System.IO.File]::ReadAllText($f)
$o = $c
$lines = $c -split "`r`n"
$count = 0
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match "^    private FrameworkElement Create\(\)$") {
        $count++
        if ($count -eq 1) { $lines[$i] = "    private FrameworkElement CreateMinuendSection()" }
        elseif ($count -eq 2) { $lines[$i] = "    private FrameworkElement CreateSubtrahendSection()" }
        elseif ($count -eq 3) { $lines[$i] = "    private FrameworkElement CreateResultSection()" }
    }
}
$nc = $lines -join "`r`n"
if ($nc -ne $o) {
    [System.IO.File]::WriteAllText($f, $nc, [System.Text.UTF8Encoding]::new($false))
    Write-Host "updated: $f"
} else {
    Write-Host "no change: $f"
}
