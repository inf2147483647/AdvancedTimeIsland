$dll = 'c:\Users\Administrator\.nuget\packages\materialdesignthemes\4.8.0\lib\net462\MaterialDesignThemes.Wpf.dll'
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($dll)
    Write-Host "Assembly loaded: $($asm.FullName)"
    $enumType = $asm.GetType('MaterialDesignThemes.Wpf.PackIconKind')
    if ($enumType -ne $null) {
        Write-Host "Found PackIconKind type"
        $names = [System.Enum]::GetNames($enumType)
        Write-Host "Total values: $($names.Count)"
        $names | Out-File 'c:\Users\Administrator\RiderProjects\AdvancedTimeIsland\packiconkind_all.txt'
        
        $check = @("SwapHorizontal","ClockTimeEightOutline","ClockStart","Numeric","CalendarStarFourPoints","CalendarRangeOutline","Zodiac","CalendarMonthOutline","CalendarTodayOutline","MapMarkerRadiusOutline","WeatherSunnyAlert","BookOpenVariant","BugOutline","Memory","Earth","Web","TimerOutline","ContentCopy","CalendarOutline","ClockOutline","MapMarkerOutline")
        foreach ($v in $check) {
            if ($names -contains $v) { Write-Host "$v : VALID" } else { Write-Host "$v : INVALID" }
        }
    } else {
        Write-Host "PackIconKind not found. Types:"
        $asm.GetTypes() | ForEach-Object { Write-Host $_.FullName }
    }
} catch {
    Write-Host "Error: $_"
}