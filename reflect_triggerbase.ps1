$ErrorActionPreference = 'Stop'
try {
    $assembly = [System.Reflection.Assembly]::LoadFrom('C:\Users\seewo\.nuget\packages\classisland.core\1.7.106.2-dev-v2\lib\net8.0\ClassIsland.Core.dll')
    $type = $assembly.GetType('ClassIsland.Core.Abstractions.Controls.TriggerSettingsControlBase`1')
    
    Write-Host '=== TriggerSettingsControlBase<T> Members ==='
    foreach($m in $type.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Instance')) {
        $params = ($m.GetParameters() | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '
        Write-Host ('  ' + $m.ReturnType.Name + ' ' + $m.Name + '(' + $params + ')')
    }
    
    Write-Host ''
    Write-Host '=== Properties ==='
    foreach($p in $type.GetProperties()) {
        Write-Host ('  ' + $p.PropertyType.Name + ' ' + $p.Name)
    }
    
    Write-Host ''
    Write-Host '=== Events ==='
    foreach($e in $type.GetEvents()) {
        Write-Host ('  ' + $e.EventHandlerType.Name + ' ' + $e.Name)
    }
    
    Write-Host ''
    Write-Host '=== Fields ==='
    foreach($f in $type.GetFields([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static')) {
        Write-Host ('  ' + $f.FieldType.Name + ' ' + $f.Name)
    }
} catch {
    Write-Host 'Error: ' + $_.Exception.Message
}
