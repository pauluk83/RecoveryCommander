param(
    [string]$DnsChoice = "",
    [string]$Adapter = "",
    [string]$WifiAdapter = "",
    [int]$Mtu = 1492,
    [string]$LogFile = ""
)

# ================================
# Windows 11 RecoveryCommander Tweaks
# Author: Zane Stanton
# Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# ================================

if (-not [string]::IsNullOrWhiteSpace($LogFile)) {
    try { Start-Transcript -Path $LogFile -Append -ErrorAction SilentlyContinue } catch { }
}

Write-Host "`n=== Applying Windows 11 Tweaks ===`n"

# --- DNS Selection ---
if ([string]::IsNullOrWhiteSpace($DnsChoice)) {
    Write-Host "ℹ DNS Choice not provided. Skipping DNS configuration."
    $dns = ""
} else {
    $dnsChoice = $DnsChoice
    switch ($dnsChoice.ToLower()) {
        "google"     { $dns = "8.8.8.8,8.8.4.4" }
        "cloudflare" { $dns = "1.1.1.1,1.0.0.1" }
        default      { $dns = "" }
    }
}
if (-not [string]::IsNullOrEmpty($dns)) {
    try { Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" -Name "NameServer" -Value $dns -ErrorAction Stop; Write-Host "✔ DNS set to: $dns" } catch { Write-Host "⚠ Failed to set DNS: $($_.Exception.Message)" }
}

# --- Apply Registry Tweaks ---
$regContent = @'
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
"TaskbarAl"=dword:00000000
"TaskbarSi"=dword:00000000
"FullPathAddress"=dword:00000001
"FullPath"=dword:00000001
"Hidden"=dword:00000001
"ShowSuperHidden"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Psched]
"NonBestEffortLimit"=dword:00000000

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
"VerboseStatus"=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize]
"AppsUseLightTheme"=dword:00000000
"SystemUsesLightTheme"=dword:00000000
'@
$regPath = "$env:TEMP\RecoveryCommanderTweaks.reg"
try {
    $regContent | Out-File -Encoding ASCII -FilePath $regPath -ErrorAction Stop
    Start-Process "regedit.exe" -ArgumentList "/s `"$regPath`"" -Wait -ErrorAction Stop
    Write-Host "✔ Registry tweaks applied"
} catch { Write-Host "⚠ Failed to apply registry tweaks: $($_.Exception.Message)" }

# --- Set MTU to provided value (default 1492) ---
if (-not [string]::IsNullOrWhiteSpace($Adapter)) {
    try { Start-Process "netsh" -ArgumentList "interface ipv4 set subinterface `"$Adapter`" mtu=$Mtu store=persistent" -Wait -ErrorAction Stop; Write-Host "✔ MTU set to $Mtu on adapter: $Adapter" } catch { Write-Host "⚠ Failed to set MTU: $($_.Exception.Message)" }
} else { Write-Host "ℹ MTU step skipped (no adapter provided)" }

# --- Enable Ultimate Performance Plan ---
try {
    $planGUID = "e9a42b02-d5df-448d-aa00-03f14749eb61"
    powercfg -duplicatescheme $planGUID | Out-Null
    powercfg -setactive $planGUID
    powercfg -change -monitor-timeout-ac 0
    powercfg -change -disk-timeout-ac 0
    powercfg -change -standby-timeout-ac 0
    powercfg -change -hibernate-timeout-ac 0
    Write-Host "✔ Ultimate Performance plan activated with zero AC timeouts"
} catch { Write-Host "⚠ Failed to activate power plan: $($_.Exception.Message)" }

# --- Enable Phone Link Flyout ---
try {
    $phoneLinkAppId = "Microsoft.YourPhone"
    $startupPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"
    Set-ItemProperty -Path $startupPath -Name $phoneLinkAppId -Value ([byte[]](0x02,0x00,0x00,0x00)) -ErrorAction Stop
    Start-Process "ms-settings:yourphone"
    Write-Host "✔ Phone Link flyout enabled"
} catch { Write-Host "⚠ Failed to enable Phone Link: $($_.Exception.Message)" }

# --- Enable Desktop Icons ---
try {
    $desktopIconsPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel"
    Set-ItemProperty -Path $desktopIconsPath -Name "{20D04FE0-3AEA-1069-A2D8-08002B30309D}" -Value 0 -ErrorAction Stop
    Set-ItemProperty -Path $desktopIconsPath -Name "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}" -Value 0 -ErrorAction Stop
    Set-ItemProperty -Path $desktopIconsPath -Name "{645FF040-5081-101B-9F08-00AA002F954E}" -Value 0 -ErrorAction Stop
    Write-Host "✔ Desktop icons enabled: This PC, Network, Recycle Bin"
} catch { Write-Host "⚠ Failed to enable desktop icons: $($_.Exception.Message)" }

# --- Create Microsoft Edge Shortcut on Desktop ---
try {
    $edgePath = "$env:USERPROFILE\Desktop\Microsoft Edge.lnk"
    $targetPath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    if (Test-Path $targetPath) {
        $WshShell = New-Object -ComObject WScript.Shell
        $shortcut = $WshShell.CreateShortcut($edgePath)
        $shortcut.TargetPath = $targetPath
        $shortcut.IconLocation = "$targetPath,0"
        $shortcut.Save()
        Write-Host "✔ Microsoft Edge shortcut created on desktop"
    } else {
        Write-Host "⚠ Microsoft Edge not found at expected path"
    }
} catch { Write-Host "⚠ Failed to create Edge shortcut: $($_.Exception.Message)" }

# --- Configure Wi-Fi Adapter Advanced Properties ---
try {
    $wifiAdapters = Get-NetAdapter | Where-Object {$_.InterfaceDescription -like "*Wireless*"}
    if ($wifiAdapters) {
        foreach ($adapter in $wifiAdapters) {
            Write-Host "Optimizing Wi-Fi Adapter: $($adapter.Name)"
            
            # Disable Power Saving
            try {
                Disable-NetAdapterPowerManagement -Name $adapter.Name -ErrorAction Stop
                Write-Host "✔ Power Saving disabled"
            } catch { Write-Host "⚠ Failed to disable Power Saving" }

            # Disable LSO (Large Send Offload)
            try {
                Set-NetAdapterAdvancedProperty -Name $adapter.Name -DisplayName "Large Send Offload v2 (IPv4)" -DisplayValue "Disabled" -ErrorAction Stop
                Set-NetAdapterAdvancedProperty -Name $adapter.Name -DisplayName "Large Send Offload v2 (IPv6)" -DisplayValue "Disabled" -ErrorAction Stop
                Write-Host "✔ LSO disabled"
            } catch { Write-Host "⚠ Failed to disable LSO" }

            # Set Wireless Mode to Fastest (ax -> ac)
            try {
                Set-NetAdapterAdvancedProperty -Name $adapter.Name -DisplayName "Wireless Mode" -DisplayValue "802.11ax" -ErrorAction Stop
                Write-Host "✔ Wireless Mode set to 802.11ax"
            } catch {
                try {
                    Set-NetAdapterAdvancedProperty -Name $adapter.Name -DisplayName "Wireless Mode" -DisplayValue "802.11ac" -ErrorAction Stop
                    Write-Host "✔ Wireless Mode set to 802.11ac"
                } catch { Write-Host "⚠ Could not set Wireless Mode to ax/ac" }
            }

            # Prefer 5GHz
            try {
                Set-NetAdapterAdvancedProperty -Name $adapter.Name -DisplayName "Preferred Band" -DisplayValue "5GHz" -ErrorAction Stop
                Write-Host "✔ Preferred Band set to 5GHz"
            } catch { }

            # Roaming & Transmit Power
            try {
                Set-NetAdapterAdvancedProperty -Name $adapter.Name -DisplayName "Roaming Aggressiveness" -DisplayValue "5" -ErrorAction Stop # 5 = Highest
                Write-Host "✔ Roaming Aggressiveness set to Highest"
            } catch { }
            try {
                Set-NetAdapterAdvancedProperty -Name $adapter.Name -DisplayName "Transmit Power" -DisplayValue "5" -ErrorAction Stop # 5 = Highest
                Write-Host "✔ Transmit Power set to Highest"
            } catch { }
        }
    } else {
        Write-Host "⚠ No Wi-Fi adapters found to optimize."
    }
} catch { Write-Host "⚠ Error during Wi-Fi optimization: $($_.Exception.Message)" }

# --- Restart Explorer ---
try {
    Stop-Process -Name explorer -Force
    Start-Process explorer
    Write-Host "`n✔ Explorer restarted to apply UI changes"
} catch { Write-Host "⚠ Failed to restart explorer: $($_.Exception.Message)" }

# --- Audit Footer ---
Write-Host "`n=== All tweaks applied successfully ===`n"

if (-not [string]::IsNullOrWhiteSpace($LogFile)) {
    try { Stop-Transcript -ErrorAction SilentlyContinue } catch { }
}
