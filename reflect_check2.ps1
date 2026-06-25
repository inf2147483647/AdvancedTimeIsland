$ErrorActionPreference = 'Stop'
try {
    $assembly = [System.Reflection.Assembly]::LoadFrom('C:\Users\seewo\.nuget\packages\classisland.core\1.7.106.2-dev-v2\lib\net8.0-windows10.0.19041.0\ClassIsland.Core.dll')
    Write-Host "Assembly loaded: $($assembly.FullName)"
    
    $types = $assembly.GetTypes() | Where-Object { $_.Name -like '*Notification*' }
    Write-Host "Notification types found: $($types.Count)"
    foreach($t in $types) {
        Write-Host "  $($t.FullName)"
    }
    
    $type = $assembly.GetType('ClassIsland.Core.Abstractions.Services.NotificationProviders.NotificationProviderBase')
    if ($type -eq $null) {
        Write-Host "NotificationProviderBase not found in this assembly"
        exit 1
    }
    
    Write-Host ""
    Write-Host "=== Fields ==="
    foreach($f in $type.GetFields([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static')) {
        Write-Host ("  " + $f.FieldType.Name + " " + $f.Name)
    }
    
    Write-Host ""
    Write-Host "=== Properties ==="
    foreach($p in $type.GetProperties([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static')) {
        Write-Host ("  " + $p.PropertyType.Name + " " + $p.Name)
    }
    
    Write-Host ""
    Write-Host "=== Methods ==="
    foreach($m in $type.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static')) {
        $params = ($m.GetParameters() | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '
        Write-Host ("  " + $m.ReturnType.Name + " " + $m.Name + "(" + $params + ")")
    }
    
    Write-Host ""
    Write-Host "=== Constructors ==="
    foreach($c in $type.GetConstructors([System.Reflection.BindingFlags]'Public,NonPublic,Instance')) {
        $params = ($c.GetParameters() | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '
        Write-Host ("  .ctor(" + $params + ")")
    }
} catch {
    Write-Host "Error: $_"
    Write-Host $_.ScriptStackTrace
}