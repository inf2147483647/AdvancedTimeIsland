$url = "https://raw.githubusercontent.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/6cf1d62ad4aaa88d31321741f5065afe08956a44/MaterialDesignThemes.Wpf/PackIconKind.cs"
$output = "c:\Users\Administrator\RiderProjects\AdvancedTimeIsland\PackIconKind_full.cs"
try {
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($url, $output)
    $content = Get-Content $output -Raw
    Write-Host "Downloaded: $($content.Length) chars"
    
    $check = @("SwapHorizontal","ClockTimeEightOutline","ClockStart","Numeric","CalendarStarFourPoints","CalendarRangeOutline","Zodiac","CalendarMonthOutline","CalendarTodayOutline","MapMarkerRadiusOutline","WeatherSunnyAlert","BookOpenVariant","BugOutline","Memory","Earth","Web","TimerOutline","ContentCopy","CalendarOutline","ClockOutline","MapMarkerOutline")
    
    foreach ($v in $check) {
        if ($content -match "(?m)^\s+$v[,=\s]") {
            Write-Host "$v : VALID"
        } else {
            Write-Host "$v : INVALID"
        }
    }
} catch {
    Write-Host "Error: $_"
}