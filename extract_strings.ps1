$dll = 'c:\Users\Administrator\.nuget\packages\materialdesignthemes\4.8.0\lib\net7.0\MaterialDesignThemes.Wpf.dll'
$bytes = [System.IO.File]::ReadAllBytes($dll)
$text = [System.Text.Encoding]::Unicode.GetString($bytes)
$matches = [regex]::Matches($text, '[A-Z][a-zA-Z]{4,}')
$values = $matches | ForEach-Object { $_.Value } | Sort-Object -Unique
$values | Out-File -FilePath 'c:\Users\Administrator\RiderProjects\AdvancedTimeIsland\icon_values.txt' -Encoding UTF8
Write-Host "Done. Count: $($values.Count)"