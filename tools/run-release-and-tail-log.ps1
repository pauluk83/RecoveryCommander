$log = Join-Path $env:TEMP 'RecoveryCommander_Crash.log'
Start-Process '.\publish\Release\RecoveryCommander.WinUI.exe'
Start-Sleep -Seconds 4
if (Test-Path $log) { Get-Content -Path $log -Tail 200 } else { Write-Host 'No crash log found.' }