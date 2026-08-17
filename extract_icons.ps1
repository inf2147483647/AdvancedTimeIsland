$dll = 'c:\Users\Administrator\.nuget\packages\materialdesignthemes\4.8.0\lib\net462\MaterialDesignThemes.Wpf.dll'
[System.Reflection.Assembly]::LoadFrom($dll) | Out-Null
$enumType = [System.Type]::GetType('MaterialDesignThemes.Wpf.PackIconKind, MaterialDesignThemes.Wpf')
if ($enumType -ne $null) {
    [System.Enum]::GetNames($enumType) | Sort-Object
} else {
    Write-Host 'Type not found in net462 dll, trying net6.0...'
    $dll6 = 'c:\Users\Administrator\.nuget\packages\materialdesignthemes\4.8.0\lib\net6.0\MaterialDesignThemes.Wpf.dll'
    [System.Reflection.Assembly]::LoadFrom($dll6) | Out-Null
    $enumType = [System.Type]::GetType('MaterialDesignThemes.Wpf.PackIconKind, MaterialDesignThemes.Wpf')
    if ($enumType -ne $null) {
        [System.Enum]::GetNames($enumType) | Sort-Object
    } else {
        Write-Host 'Type not found'
    }
}