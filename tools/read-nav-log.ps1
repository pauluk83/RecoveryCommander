$p = [System.IO.Path]::Combine($env:TEMP, 'RecoveryCommander_Navigation.log')
if (Test-Path $p) { Get-Content $p -Tail 300 } else { Write-Host 'No navigation log found.' }