Add-Type -AssemblyName PresentationFramework

function Show-Error($msg) {
    [System.Windows.MessageBox]::Show($msg, "Office 2024 LTSC Installer Error", "OK", "Error") | Out-Null
}

function Show-Info($msg) {
    [System.Windows.MessageBox]::Show($msg, "Office 2024 LTSC Installer", "OK", "Information") | Out-Null
}

# Ensure running as admin
if (-not ([bool](New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))) {
    Show-Error "Please run this script as Administrator."
    exit 1
}

try {
    # --- USER SET DOWNLOAD LINK HERE ---
    $DownloadURL = "https://dl.licensetom.com/downloads/download.php?file=Office_2024_Professional_Plus.exe&pw=JW2e9hYewu8W7pQrIl"  # <-- Put your download link here
    # ----------------------------------

    # --- USER SET PRODUCT KEY HERE ---
    $ProductKey = "V6BK8-NH8PF-GFGGQ-V6F38-WB883"  # <-- Put your Office 2024 LTSC product key here
    # ----------------------------------

    if ([string]::IsNullOrEmpty($DownloadURL)) {
        throw "No download URL provided. Please set `$DownloadURL in the script."
    }

    if ([string]::IsNullOrEmpty($ProductKey)) {
        throw "No product key provided. Please set `$ProductKey in the script."
    }

    # Download installer
    $TempDir = Join-Path $env:TEMP "Office2024Installer"
    New-Item -Path $TempDir -ItemType Directory -Force | Out-Null
    $InstallerPath = Join-Path $TempDir "Office_2024_Professional_Plus.exe"

    Invoke-WebRequest -Uri $DownloadURL -OutFile $InstallerPath -UseBasicParsing -ErrorAction Stop

    # Install Office silently
    Start-Process -FilePath $InstallerPath -ArgumentList "/quiet /norestart" -Wait -ErrorAction Stop

    # Locate ospp.vbs for activation
    $ospp = Get-ChildItem -Path "$env:ProgramFiles*" -Filter ospp.vbs -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $ospp) { throw "ospp.vbs not found. Cannot activate." }

    $cscript = Join-Path $env:SystemRoot "System32\cscript.exe"

    # Install product key
    & $cscript $ospp.FullName /inpkey:$ProductKey
    # Activate
    & $cscript $ospp.FullName /act

    Show-Info "Office 2024 LTSC installed and activated successfully."

} catch {
    Show-Error "Installation failed.`nError:`n$($_.Exception.Message)"
}
